using UnityEngine;
using HudText = UnityEngine.UI.Text;
using AREON.ResourceSystem;

namespace AREON.UI
{
    /// <summary>
    /// Stage 5 HUD. DISPLAY ONLY.
    /// - Receives a serialized ResourceManager reference.
    /// - Reads initial values, then listens to ResourceManager.ResourceChanged.
    /// - Decides "is this resource low?" from Inspector thresholds and tells each ResourceBarView how to draw.
    /// - Holds placeholder text area for weather (Stage 8) and connects to DaylightManager (Stage 7).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Resource Source (single authority)")]
        [SerializeField] private ResourceManager resourceManager;

        [Header("Stage 7 - Daylight")]
        [SerializeField] private DaylightManager daylightManager;

        [Header("Stage 8 - Weather")]
        [SerializeField] private WeatherManager weatherManager;

        [Header("Resource Rows")]
        [SerializeField] private ResourceBarView oxygenView;
        [SerializeField] private ResourceBarView powerView;
        [SerializeField] private ResourceBarView shieldView;

        [Header("Display Scale")]
        [Tooltip("Value that equals 100% on the bars. Must match ResourceManager's maximum (Stage 4 clamps to 0-100).")]
        [SerializeField, Min(1f)] private float resourceMaximum = 100f;

        [Header("Low-Resource Warning Thresholds (percent of maximum)")]
        [Tooltip("Warning shows while Oxygen is BELOW this percentage. 0 disables the warning. Prototype tuning value.")]
        [SerializeField, Range(0f, 100f)] private float lowOxygenThreshold = 30f;
        [Tooltip("Warning shows while Power is BELOW this percentage. 0 disables the warning. Prototype tuning value.")]
        [SerializeField, Range(0f, 100f)] private float lowPowerThreshold = 25f;
        [Tooltip("Warning shows while Shield is BELOW this percentage. 0 disables the warning. Prototype tuning value.")]
        [SerializeField, Range(0f, 100f)] private float lowShieldThreshold = 25f;

        [Header("Status Placeholders")]
        [SerializeField] private HudText weatherStatusText;
        [SerializeField] private HudText daylightText;
        [SerializeField] private string weatherPlaceholder = "WEATHER STATUS: AWAITING SCENARIO DATA";
        [SerializeField] private string daylightPlaceholder = "DAYLIGHT: WAITING FOR MISSION DATA";

        [Header("Debug")]
        [Tooltip("Logs one line when a resource enters or leaves its warning state.")]
        [SerializeField] private bool logWarningChanges = false;

        private bool referencesValid;

        public DaylightManager DaylightManager => daylightManager;
        public WeatherManager WeatherManager => weatherManager;
        public string CurrentWeatherText => weatherStatusText != null ? weatherStatusText.text : string.Empty;

        // ----- Public display-only API for later stages -----

        /// <summary>Stage 8 (WeatherManager) will call this. Display only.</summary>
        public void SetWeatherStatus(string message)
        {
            if (weatherStatusText != null) weatherStatusText.text = message;
        }

        /// <summary>Stage 7 (DaylightManager) will call this. Display only.</summary>
        public void SetDaylightText(string message)
        {
            if (daylightText != null) daylightText.text = message;
        }

        // ----- Unity lifecycle -----

        private void Awake()
        {
            SetWeatherStatus(weatherPlaceholder);
            if (daylightManager == null)
            {
                SetDaylightText(daylightPlaceholder);
            }
            else
            {
                RefreshDaylightText();
            }
        }

        private void OnEnable()
        {
            referencesValid = ValidateReferences();
            if (!referencesValid)
            {
                enabled = false;
                return;
            }

            Subscribe();
            RefreshAll();

            if (daylightManager != null)
            {
                daylightManager.DaylightStarted += RefreshDaylightText;
                daylightManager.DaylightEnded += RefreshDaylightText;
                daylightManager.RemainingTimeChanged += RefreshDaylightText;
            }
            RefreshDaylightText();

            if (weatherManager != null)
            {
                weatherManager.WeatherStatusChanged += HandleWeatherStatusChanged;
                if (weatherManager.HasScenario)
                {
                    HandleWeatherStatusChanged(weatherManager.StatusText);
                }
            }
        }

        private void Start()
        {
            if (referencesValid) RefreshAll();
            RefreshDaylightText();
            if (weatherManager != null && weatherManager.HasScenario)
            {
                HandleWeatherStatusChanged(weatherManager.StatusText);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (daylightManager != null)
            {
                daylightManager.DaylightStarted -= RefreshDaylightText;
                daylightManager.DaylightEnded -= RefreshDaylightText;
                daylightManager.RemainingTimeChanged -= RefreshDaylightText;
            }

            if (weatherManager != null)
            {
                weatherManager.WeatherStatusChanged -= HandleWeatherStatusChanged;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();

            if (daylightManager != null)
            {
                daylightManager.DaylightStarted -= RefreshDaylightText;
                daylightManager.DaylightEnded -= RefreshDaylightText;
                daylightManager.RemainingTimeChanged -= RefreshDaylightText;
            }

            if (weatherManager != null)
            {
                weatherManager.WeatherStatusChanged -= HandleWeatherStatusChanged;
            }
        }

        private void HandleWeatherStatusChanged(string text)
        {
            SetWeatherStatus(text);
        }

        private void OnValidate()
        {
            resourceMaximum = Mathf.Max(1f, resourceMaximum);

            if (Application.isPlaying && referencesValid && isActiveAndEnabled)
            {
                RefreshAll();
                RefreshDaylightText();
            }
        }

        private void RefreshDaylightText()
        {
            if (daylightText == null) return;
            daylightText.text = daylightManager != null
                ? daylightManager.GetDisplayText()
                : "DAYLIGHT: --:--";
        }

        // ----- Subscription -----

        private void Subscribe()
        {
            if (resourceManager != null)
            {
                resourceManager.ResourceChanged += HandleResourceChanged;
            }
        }

        private void Unsubscribe()
        {
            if (resourceManager != null)
            {
                resourceManager.ResourceChanged -= HandleResourceChanged;
            }
        }

        private void HandleResourceChanged(ResourceType type, float oldValue, float newValue)
        {
            RenderResource(type, newValue);
        }

        private float ReadResource(ResourceType type)
        {
            return resourceManager.GetValue(type);
        }

        // ----- Rendering -----

        private void RefreshAll()
        {
            RenderResource(ResourceType.Oxygen, ReadResource(ResourceType.Oxygen));
            RenderResource(ResourceType.Power, ReadResource(ResourceType.Power));
            RenderResource(ResourceType.Shield, ReadResource(ResourceType.Shield));
        }

        private void RenderResource(ResourceType type, float value)
        {
            switch (type)
            {
                case ResourceType.Oxygen:
                    RenderBar(oxygenView, "Oxygen", value, lowOxygenThreshold);
                    break;
                case ResourceType.Power:
                    RenderBar(powerView, "Power", value, lowPowerThreshold);
                    break;
                case ResourceType.Shield:
                    RenderBar(shieldView, "Shield", value, lowShieldThreshold);
                    break;
            }
        }

        private void RenderBar(ResourceBarView view, string label, float value, float thresholdPercent)
        {
            float percent = Mathf.Clamp(value, 0f, resourceMaximum) / resourceMaximum * 100f;
            bool isLow = percent < thresholdPercent;
            bool wasLow = view.IsWarningActive;

            view.Render(value, resourceMaximum, isLow);

            if (logWarningChanges && wasLow != isLow)
            {
                Debug.Log($"[HUD] {label} warning {(isLow ? "ENTERED" : "CLEARED")} ({value:F1}/{resourceMaximum:F1})", this);
            }
        }

        private bool ValidateReferences()
        {
            bool ok = true;
            ok &= Require(resourceManager, nameof(resourceManager));
            ok &= Require(oxygenView, nameof(oxygenView));
            ok &= Require(powerView, nameof(powerView));
            ok &= Require(shieldView, nameof(shieldView));

            if (weatherStatusText == null)
            {
                Debug.LogWarning("[HUDController] Weather Status Text is not assigned. Weather updates will be skipped.", this);
            }

            if (daylightText == null)
            {
                Debug.LogWarning("[HUDController] Daylight Text is not assigned. Daylight updates will be skipped.", this);
            }

            return ok;
        }

        private bool Require(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null) return true;
            Debug.LogError($"[HUDController] Required reference '{fieldName}' is missing. HUD disabled.", this);
            return false;
        }
    }
}
