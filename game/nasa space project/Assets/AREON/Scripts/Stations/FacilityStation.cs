using AREON.Interaction;
using System;
using UnityEngine;
using AREON.UI;

namespace AREON.Stations
{
    /// <summary>
    /// Shared base class for every facility interactable (Shield Control, Science Terminal,
    /// Solar Generator). It is the ONLY place in the Stations folder that talks to the
    /// IInteractable contract, so the three stations never duplicate interaction handling.
    /// </summary>
    public abstract class FacilityStation : MonoBehaviour, IInteractable
    {
        [Header("Facility Information")]
        [SerializeField] private FacilityInfo info = new FacilityInfo();

        [Tooltip("Short name used in the interaction prompt, e.g. 'Shield Control'.")]
        [SerializeField] private string interactionName = "Facility";

        [TextArea(1, 3)]
        [Tooltip("Small note shown at the bottom of the panel. Leave empty to hide it.")]
        [SerializeField] private string previewNote = "";

        [Header("References")]
        [Tooltip("The ONE shared panel used by all facilities.")]
        [SerializeField] protected FacilityInteractionPanel panel;

        [Header("Availability")]
        [SerializeField] private bool interactionEnabled = true;
        [SerializeField] private string disabledReason = "Currently unavailable.";

        /// <summary>Raised after a successful interaction. Stage 7 / objectives can subscribe.</summary>
        public event Action<FacilityStation> Interacted;

        public FacilityInfo Info => info;
        public string InteractionName => interactionName;
        public string PreviewNote => previewNote;
        public bool InteractionEnabled => interactionEnabled;

        /// <summary>True when the shared panel is set up, so this station can work safely.</summary>
        public bool IsConfigured => panel != null && panel.IsReady;

        // -------------------------------------------------------------------------
        // Overridable hooks (used by subclasses and, in Stage 7, by real actions)
        // -------------------------------------------------------------------------

        /// <summary>Live status line shown on the panel. Override for dynamic status.</summary>
        public virtual string GetStatusText() => info.statusText;

        /// <summary>Called after the panel has been opened/toggled.</summary>
        protected virtual void OnInteracted() { }

        /// <summary>Called when the player interacts while the panel is already open (confirm action).</summary>
        protected virtual void OnConfirm() { }

        /// <summary>Default facility data applied when the component is first added.</summary>
        protected abstract FacilityInfo CreateDefaultInfo();

        protected abstract string DefaultInteractionName { get; }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        public void SetInteractionEnabled(bool enabledState, string reason = null)
        {
            interactionEnabled = enabledState;
            if (!string.IsNullOrEmpty(reason)) disabledReason = reason;
        }

        /// <summary>Why the station cannot be used right now ("" when it can).</summary>
        public string GetAvailabilityReason()
        {
            if (panel == null) return "Facility panel is not assigned.";
            if (!panel.IsReady) return "Facility panel is not set up.";
            if (!interactionEnabled) return disabledReason;
            return string.Empty;
        }

        // -------------------------------------------------------------------------
        // IInteractable implementation
        // -------------------------------------------------------------------------

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (panel != null && panel.IsOpen && panel.Current == this && !string.IsNullOrEmpty(info.actionName))
            {
                return $"Confirm: {info.actionName}";
            }
            return interactionName;
        }

        public string GetPrompt() => interactionName;

        public bool CanInteract(PlayerInteractor interactor, out string unavailableReason)
        {
            unavailableReason = GetAvailabilityReason();
            return string.IsNullOrEmpty(unavailableReason);
        }

        public bool CanInteract(PlayerInteractor interactor) => string.IsNullOrEmpty(GetAvailabilityReason());

        public string GetUnavailableReason() => GetAvailabilityReason();

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor, out _)) return;

            if (panel != null)
            {
                if (panel.IsOpen && panel.Current == this)
                {
                    OnConfirm();
                    Interacted?.Invoke(this);
                    return;
                }

                panel.Toggle(this);
            }

            OnInteracted();
            Interacted?.Invoke(this);
        }

        // -------------------------------------------------------------------------
        // Unity messages
        // -------------------------------------------------------------------------

        protected virtual void Reset()
        {
            info = CreateDefaultInfo();
            interactionName = DefaultInteractionName;
        }

        protected virtual void Awake()
        {
            if (panel == null)
            {
                Debug.LogError($"[{GetType().Name}] No FacilityInteractionPanel assigned on '{name}'. " +
                               "Assign it in the Inspector. This station will be unavailable.", this);
            }
        }

        protected virtual void OnValidate()
        {
            if (info != null && info.powerCost < 0f) info.powerCost = 0f;
        }

        protected virtual void OnDisable()
        {
            if (panel != null) panel.HideIfOwner(this);
        }
    }
}
