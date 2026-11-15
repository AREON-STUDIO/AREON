using UnityEngine;
using UnityEngine.Events;

namespace AREON.Interaction
{
    /// <summary>
    /// TEMPORARY Stage 3 test target. Remove (or replace with real stations) in Stage 6.
    /// Proves the IInteractable contract: prompt, availability, reason and interaction.
    /// Confirmation: a Console log, a colour change, and a short cooldown that shows the
    /// "unavailable" prompt state.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TestInteractableCube : MonoBehaviour, IInteractable
    {
        [Header("Prompt")]
        [SerializeField] private string prompt = "Activate Test Cube";
        [SerializeField] private string cooldownReason = "Recharging...";
        [SerializeField, Min(0f)] private float cooldownSeconds = 1f;

        [Header("Visual confirmation")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color idleColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color activatedColor = new Color(0.2f, 1f, 0.4f, 1f);

        [Header("Events")]
        [SerializeField] private UnityEvent onInteracted;

        private Material materialInstance;
        private float availableAtTime;
        private int interactionCount;
        private bool showingActivated;

        public int InteractionCount => interactionCount;

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();

            if (targetRenderer != null)
            {
                materialInstance = targetRenderer.material; // per-object instance
                materialInstance.color = idleColor;
            }
            else
            {
                Debug.LogError("[TestInteractableCube] No Renderer found; visual confirmation disabled.", this);
            }
        }

        private void Update()
        {
            bool cooling = Time.time < availableAtTime;
            if (cooling == showingActivated) return;

            showingActivated = cooling;
            if (materialInstance != null) materialInstance.color = cooling ? activatedColor : idleColor;
        }

        private void OnDestroy()
        {
            if (materialInstance != null) Destroy(materialInstance);
        }

        // ------------------------------------------------------------ IInteractable

        public string GetPrompt(PlayerInteractor interactor)
        {
            return prompt;
        }

        public bool CanInteract(PlayerInteractor interactor, out string unavailableReason)
        {
            if (Time.time < availableAtTime)
            {
                unavailableReason = cooldownReason;
                return false;
            }

            unavailableReason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            interactionCount++;
            availableAtTime = Time.time + cooldownSeconds;

            string who = interactor != null ? interactor.name : "unknown";
            Debug.Log($"[AREON] Test cube interacted with by '{who}'. Total interactions: {interactionCount}", this);

            onInteracted?.Invoke();
        }
    }
}
