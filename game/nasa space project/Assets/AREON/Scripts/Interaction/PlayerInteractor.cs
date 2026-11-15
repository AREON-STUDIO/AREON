using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AREON.Interaction
{
    /// <summary>
    /// Detects the single active interaction target in front of the player and forwards
    /// the interact input to it.
    ///
    /// Detection: one forward SphereCast from the player. The CLOSEST non-player collider hit
    /// decides the result. If that collider belongs to an IInteractable it becomes the target;
    /// if it is anything else (a wall, a facility, a boundary) it blocks the view, so nothing
    /// behind it can be targeted. Because only one hit is ever selected, only one prompt exists.
    ///
    /// Input: uses the EXISTING PlayerInput component on the Player. It reads the action
    /// "Player/Interact" from that component's actions. No second PlayerInput is created and
    /// the key is changed in the Input Actions asset, never in this script.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Input (existing PlayerInput on the Player)")]
        [Tooltip("The existing PlayerInput. If empty, one on this same GameObject is used.")]
        [SerializeField] private PlayerInput playerInput;

        [Tooltip("Action path inside the Input Actions asset, as 'Map/Action'.")]
        [SerializeField] private string interactActionName = "Player/Interact";

        [Header("Detection")]
        [Tooltip("Defines the origin and facing direction. If empty, this Transform is used.")]
        [SerializeField] private Transform aimTransform;

        [Tooltip("Maximum distance, in metres, measured from the aim origin to the target.")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;

        [Tooltip("Radius of the sweep. A small radius makes aiming forgiving.")]
        [SerializeField, Min(0.01f)] private float detectionRadius = 0.3f;

        [Tooltip("Vertical offset of the ray origin from the aim Transform position.")]
        [SerializeField] private float originHeightOffset = 0f;

        [Tooltip("Layers that can be targeted or can block the view.")]
        [SerializeField] private LayerMask interactionLayers = ~0;

        [SerializeField] private bool drawDebugRay = true;

        // ---------------------------------------------------------------- public read API

        /// <summary>Raised whenever the target, its prompt text or its availability changes (including losing the target).</summary>
        public event Action PromptChanged;

        /// <summary>Raised after a target's Interact() ran without error.</summary>
        public event Action<IInteractable> InteractionSucceeded;

        /// <summary>Raised when E was pressed but the target was unavailable or failed. Carries a reason.</summary>
        public event Action<IInteractable, string> InteractionRejected;

        public bool HasTarget => currentTarget != null;
        public IInteractable CurrentTarget => currentTarget;
        public string PromptText => promptText;
        public string KeyLabel => keyLabel;
        public bool IsTargetAvailable => targetAvailable;
        public string UnavailableReason => unavailableReason;

        public float InteractionDistance
        {
            get => interactionDistance;
            set => interactionDistance = Mathf.Max(0.1f, value);
        }

        // ---------------------------------------------------------------- private state

        private readonly RaycastHit[] hitBuffer = new RaycastHit[32];

        private IInteractable currentTarget;
        private IInteractable faultedTarget;   // a target that threw an exception; ignored until it changes
        private InputAction interactAction;
        private string keyLabel = "E";
        private string promptText = string.Empty;
        private string unavailableReason = string.Empty;
        private bool targetAvailable;
        private bool started;

        // ---------------------------------------------------------------- Unity lifecycle

        private void Awake()
        {
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (aimTransform == null) aimTransform = transform;
        }

        // Binding happens in Start (not OnEnable) so PlayerInput has finished its own setup first.
        private void Start()
        {
            started = true;
            BindInput();
        }

        private void OnEnable()
        {
            if (started) BindInput();
        }

        private void OnDisable()
        {
            UnbindInput();
            ClearTarget();
        }

        private void Update()
        {
            RefreshTarget();
        }

        // ---------------------------------------------------------------- input

        private void BindInput()
        {
            if (interactAction != null) return; // already bound

            if (playerInput == null || playerInput.actions == null)
            {
                Debug.LogError("[PlayerInteractor] No PlayerInput (or no actions asset) found. " +
                               "Assign the existing PlayerInput component of the Player.", this);
                enabled = false;
                return;
            }

            interactAction = playerInput.actions.FindAction(interactActionName, false);
            if (interactAction == null)
            {
                Debug.LogError($"[PlayerInteractor] Action '{interactActionName}' was not found in the " +
                               "Input Actions asset used by PlayerInput.", this);
                enabled = false;
                return;
            }

            interactAction.performed += OnInteractPerformed;
            keyLabel = ResolveKeyLabel(interactAction);
        }

        private void UnbindInput()
        {
            if (interactAction == null) return;
            interactAction.performed -= OnInteractPerformed;
            interactAction = null;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            TryInteract();
        }

        /// <summary>Shows the keyboard binding (e.g. "E") so the prompt always matches the Input Actions asset.</summary>
        private static string ResolveKeyLabel(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite) continue;
                if (binding.effectivePath.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase))
                {
                    string label = action.GetBindingDisplayString(i);
                    if (!string.IsNullOrEmpty(label)) return label.ToUpperInvariant();
                }
            }

            string fallback = action.GetBindingDisplayString();
            return string.IsNullOrEmpty(fallback) ? "?" : fallback;
        }

        // ---------------------------------------------------------------- interaction

        /// <summary>Attempts to interact with the current target. Safe to call at any time.</summary>
        public bool TryInteract()
        {
            RefreshTarget(); // never act on a stale target

            IInteractable target = currentTarget;
            if (target == null) return false;

            if (!targetAvailable)
            {
                InteractionRejected?.Invoke(target, unavailableReason);
                return false;
            }

            try
            {
                target.Interact(this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                faultedTarget = target;
                InteractionRejected?.Invoke(target, "Interaction failed.");
                RefreshTarget();
                return false;
            }

            InteractionSucceeded?.Invoke(target);
            RefreshTarget();
            return true;
        }

        // ---------------------------------------------------------------- targeting

        /// <summary>Re-evaluates the target and raises PromptChanged if anything visible changed.</summary>
        public void RefreshTarget()
        {
            IInteractable found = FindTarget();
            bool targetChanged = !ReferenceEquals(found, currentTarget);
            currentTarget = found;

            bool available = false;
            string reason = string.Empty;
            string prompt = string.Empty;

            if (currentTarget != null)
            {
                try
                {
                    available = currentTarget.CanInteract(this, out reason);
                    prompt = currentTarget.GetPrompt(this);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    faultedTarget = currentTarget;
                    currentTarget = null;
                    targetChanged = true;
                    available = false;
                    reason = string.Empty;
                    prompt = string.Empty;
                }
            }

            if (currentTarget != null && string.IsNullOrWhiteSpace(prompt)) prompt = "Interact";
            reason = reason ?? string.Empty;

            bool visibleStateChanged = targetChanged
                                       || available != targetAvailable
                                       || prompt != promptText
                                       || reason != unavailableReason;

            targetAvailable = available;
            promptText = prompt;
            unavailableReason = reason;

            if (visibleStateChanged) PromptChanged?.Invoke();
        }

        private IInteractable FindTarget()
        {
            if (aimTransform == null) return null;

            Vector3 origin = aimTransform.position + Vector3.up * originHeightOffset;
            Vector3 direction = aimTransform.forward;

            if (drawDebugRay) Debug.DrawRay(origin, direction * interactionDistance, Color.cyan);

            int count = Physics.SphereCastNonAlloc(
                origin, detectionRadius, direction, hitBuffer,
                interactionDistance, interactionLayers, QueryTriggerInteraction.Ignore);

            // The buffer is unordered, so find the closest hit that is not part of the player.
            int closest = -1;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = hitBuffer[i].collider;
                if (hitCollider == null) continue;
                if (hitCollider.transform.IsChildOf(transform)) continue; // ignore the player's own colliders

                if (hitBuffer[i].distance < closestDistance)
                {
                    closestDistance = hitBuffer[i].distance;
                    closest = i;
                }
            }

            if (closest < 0) return null;

            // The closest thing is either an interactable or an obstacle that blocks the view.
            IInteractable candidate = hitBuffer[closest].collider.GetComponentInParent<IInteractable>();
            if (candidate == null) return null;
            if (ReferenceEquals(candidate, faultedTarget)) return null;

            Behaviour behaviour = candidate as Behaviour;
            if (behaviour != null && !behaviour.isActiveAndEnabled) return null;

            return candidate;
        }

        private void ClearTarget()
        {
            if (currentTarget == null && !targetAvailable && promptText.Length == 0) return;

            currentTarget = null;
            targetAvailable = false;
            promptText = string.Empty;
            unavailableReason = string.Empty;
            PromptChanged?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Transform source = aimTransform != null ? aimTransform : transform;
            Vector3 origin = source.position + Vector3.up * originHeightOffset;
            Vector3 end = origin + source.forward * interactionDistance;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(end, detectionRadius);
        }
    }
}
