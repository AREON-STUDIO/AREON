using AREON.Interaction;
using AREON.ResourceSystem;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AREON.Stations;
using AREON.Missions;

namespace AREON.UI
{
    /// <summary>
    /// ONE shared information panel for all facilities. It shows whichever station currently
    /// "owns" it, so two panels can never overlap. It only READS Power from ResourceManager
    /// for display; it never stores or changes resource values.
    /// Closes automatically when the player walks away from the owning station.
    /// </summary>
    public class FacilityInteractionPanel : MonoBehaviour, IMissionResettable
    {
        public void ResetRuntimeState() => Hide();
        [Header("Panel Objects")]
        [Tooltip("Child object that is shown/hidden. The canvas object itself stays active.")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image accentBar;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text purposeText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text availabilityText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text noteText;

        [Header("Stage 7 - Action feedback")]
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Color successColor = new Color(0.45f, 0.95f, 0.55f);
        [SerializeField] private Color failureColor = new Color(1f, 0.55f, 0.45f);
        [SerializeField, Min(0.5f)] private float feedbackSeconds = 3f;

        [Header("Dependencies")]
        [SerializeField] private ResourceManager resourceManager;
        [Tooltip("Player root transform, used only to auto-close the panel by distance.")]
        [SerializeField] private Transform player;

        [Header("Behaviour")]
        [Tooltip("Must be larger than the PlayerInteractor range. Panel closes beyond this distance.")]
        [SerializeField, Min(1f)] private float autoCloseDistance = 5f;

        private FacilityStation current;
        private bool isShowing;
        private bool isReady;
        private Coroutine feedbackRoutine;

        // Cached display state so we only rewrite text when something actually changed.
        private int lastPowerShown = int.MinValue;
        private string lastStatusShown;
        private string lastReasonShown;

        public bool IsReady => isReady;
        public bool IsOpen => isShowing;
        public FacilityStation Current => isShowing ? current : null;

        public event Action<FacilityStation> Opened;
        public event Action Closed;

        private void Awake()
        {
            isReady = ValidateReferences();
            SetVisible(false);
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isShowing) return;

            // Station was destroyed or disabled while open.
            if (current == null || !current.isActiveAndEnabled)
            {
                Hide();
                return;
            }

            if (player != null)
            {
                Vector3 delta = player.position - current.transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > autoCloseDistance * autoCloseDistance)
                {
                    Hide();
                    return;
                }
            }

            RefreshDynamic();
        }

        // ---------------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------------

        /// <summary>Show this station's info, replacing whichever station was shown before.</summary>
        public void Show(FacilityStation station)
        {
            if (!isReady || station == null) return;

            current = station;
            isShowing = true;
            lastPowerShown = int.MinValue;
            lastStatusShown = null;
            lastReasonShown = null;

            RefreshStatic();
            RefreshDynamic();
            SetVisible(true);
            Opened?.Invoke(station);
        }

        public void Hide()
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }

            bool wasShowing = isShowing;
            isShowing = false;
            current = null;
            SetVisible(false);
            if (wasShowing) Closed?.Invoke();
        }

        /// <summary>Pressing Interact on the open station closes it; otherwise opens it.</summary>
        public void Toggle(FacilityStation station)
        {
            if (station == null) return;
            if (isShowing && current == station) Hide();
            else Show(station);
        }

        /// <summary>Hide only if this station currently owns the panel.</summary>
        public void HideIfOwner(FacilityStation station)
        {
            if (isShowing && current == station) Hide();
        }

        public void ShowFeedback(string message, bool success)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            feedbackText.color = success ? successColor : failureColor;
            feedbackText.gameObject.SetActive(true);
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            if (isActiveAndEnabled) feedbackRoutine = StartCoroutine(HideFeedbackLater());
        }

        private IEnumerator HideFeedbackLater()
        {
            yield return new WaitForSecondsRealtime(feedbackSeconds);
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
            feedbackRoutine = null;
        }

        // ---------------------------------------------------------------------
        // Refresh
        // ---------------------------------------------------------------------

        private void RefreshStatic()
        {
            FacilityInfo info = current.Info;

            SetText(titleText, info.facilityName);
            SetText(purposeText, info.purpose);
            SetText(actionText, string.IsNullOrEmpty(info.actionName) ? "Action: None" : "Action: " + info.actionName);

            if (costText != null)
            {
                costText.text = info.HasPowerCost
                    ? $"Power Cost: {info.powerCost:0.##}"
                    : "Power Cost: None";
            }

            if (accentBar != null) accentBar.color = info.accentColor;

            if (noteText != null)
            {
                bool hasNote = !string.IsNullOrEmpty(current.PreviewNote);
                noteText.gameObject.SetActive(hasNote);
                if (hasNote) noteText.text = current.PreviewNote;
            }
        }

        private void RefreshDynamic()
        {
            // Status line (stations such as the generator provide live text).
            string status = current.GetStatusText();
            if (status != lastStatusShown)
            {
                lastStatusShown = status;
                SetText(statusText, string.IsNullOrEmpty(status) ? string.Empty : "Status: " + status);
                if (statusText != null) statusText.gameObject.SetActive(!string.IsNullOrEmpty(status));
            }

            // Availability line.
            string reason = current.GetAvailabilityReason();
            FacilityInfo info = current.Info;
            string availability;
            int powerNow = int.MinValue;

            if (!string.IsNullOrEmpty(reason))
            {
                availability = "Unavailable: " + reason;
            }
            else if (info.HasPowerCost)
            {
                if (TryReadPower(out float power))
                {
                    powerNow = Mathf.RoundToInt(power);
                    availability = power >= info.powerCost
                        ? $"Power available: {powerNow}% - enough for this action"
                        : $"Power available: {powerNow}% - not enough (need {info.powerCost:0.##})";
                }
                else
                {
                    availability = "Power reading unavailable";
                }
            }
            else
            {
                availability = "Interaction available";
            }

            if (powerNow != lastPowerShown || reason != lastReasonShown || availabilityText == null || availabilityText.text != availability)
            {
                lastPowerShown = powerNow;
                lastReasonShown = reason;
                SetText(availabilityText, availability);
            }
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private bool TryReadPower(out float power)
        {
            power = 0f;
            if (resourceManager == null) return false;
            power = resourceManager.GetValue(ResourceType.Power);
            return true;
        }

        private bool ValidateReferences()
        {
            bool ok = true;
            ok &= Require(panelRoot, nameof(panelRoot));
            ok &= Require(titleText, nameof(titleText));
            ok &= Require(purposeText, nameof(purposeText));
            ok &= Require(actionText, nameof(actionText));
            ok &= Require(costText, nameof(costText));
            ok &= Require(availabilityText, nameof(availabilityText));
            ok &= Require(statusText, nameof(statusText));

            // Optional references: warn once, panel still works.
            if (resourceManager == null)
                Debug.LogWarning("[FacilityInteractionPanel] ResourceManager not assigned. Power availability will not be shown.", this);
            if (player == null)
                Debug.LogWarning("[FacilityInteractionPanel] Player not assigned. Panel will not auto-close by distance.", this);

            return ok;
        }

        private bool Require(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null) return true;
            Debug.LogError($"[FacilityInteractionPanel] Missing reference '{fieldName}'. Facility panel disabled.", this);
            return false;
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null && panelRoot.activeSelf != visible) panelRoot.SetActive(visible);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }
    }
}
