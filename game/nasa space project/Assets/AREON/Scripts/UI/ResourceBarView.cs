using UnityEngine;
using UnityEngine.UI;

// To use TextMeshPro instead of legacy UI Text, change ONLY this alias line to:
//   using HudText = TMPro.TMP_Text;
using HudText = UnityEngine.UI.Text;

namespace AREON.UI
{
    /// <summary>
    /// Display-only view for ONE resource row (label, percentage, bar, warning feedback).
    /// It stores no gameplay data. HUDController tells it what to draw via Render().
    /// </summary>
    public class ResourceBarView : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private HudText labelText;
        [SerializeField] private HudText percentText;
        [SerializeField] private Slider slider;
        [SerializeField] private Image fillImage;
        [Tooltip("Optional row background that is tinted while the resource is low.")]
        [SerializeField] private Image rowBackground;
        [Tooltip("Optional object (for example a 'LOW' text) that is shown only while the resource is low.")]
        [SerializeField] private GameObject warningIndicator;

        [Header("Text")]
        [SerializeField] private string displayName = "RESOURCE";

        [Header("Normal Appearance")]
        [SerializeField] private Color normalFillColor = new Color(0.20f, 0.75f, 0.35f, 1f);
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color normalBackgroundColor = new Color(0f, 0f, 0f, 0f);

        [Header("Warning Appearance")]
        [SerializeField] private Color warningFillColor = new Color(0.90f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color warningTextColor = new Color(1.00f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color warningBackgroundColor = new Color(0.60f, 0.05f, 0.05f, 0.40f);

        private bool initialized;

        /// <summary>True when the row is currently drawn in its warning style (read-only, for tests).</summary>
        public bool IsWarningActive { get; private set; }

        /// <summary>True when every required reference is assigned.</summary>
        public bool HasRequiredReferences =>
            labelText != null && percentText != null && slider != null && fillImage != null;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized) return;
            if (!HasRequiredReferences)
            {
                Debug.LogError($"[ResourceBarView] '{name}' is missing a required reference " +
                               "(labelText, percentText, slider or fillImage).", this);
                return;
            }

            initialized = true;

            // The slider is a pure display: normalized 0..1, not user-controlled.
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.interactable = false;

            labelText.text = displayName;
        }

        /// <summary>
        /// Draws the given value. Called by HUDController whenever ResourceManager reports a value.
        /// </summary>
        /// <param name="value">Current resource value (from ResourceManager).</param>
        /// <param name="maximum">Scale used to turn the value into a fraction (e.g. 100).</param>
        /// <param name="isLow">True when the value is below its warning threshold.</param>
        public void Render(float value, float maximum, bool isLow)
        {
            Initialize();
            if (!initialized) return;

            float normalized = maximum > 0f ? Mathf.Clamp01(value / maximum) : 0f;

            slider.value = normalized;
            percentText.text = FormatPercent(normalized) + "%";
            ApplyStyle(isLow);
        }

        private void ApplyStyle(bool isLow)
        {
            IsWarningActive = isLow;

            fillImage.color = isLow ? warningFillColor : normalFillColor;
            percentText.color = isLow ? warningTextColor : normalTextColor;
            labelText.color = isLow ? warningTextColor : normalTextColor;

            if (rowBackground != null)
            {
                rowBackground.color = isLow ? warningBackgroundColor : normalBackgroundColor;
            }

            if (warningIndicator != null && warningIndicator.activeSelf != isLow)
            {
                warningIndicator.SetActive(isLow);
            }
        }

        /// <summary>
        /// Rounds to a whole percent, but only shows exactly 0% when the value is really 0
        /// and exactly 100% when the value is really full.
        /// </summary>
        private static int FormatPercent(float normalized)
        {
            float percent = normalized * 100f;
            int rounded = Mathf.RoundToInt(percent);

            if (percent > 0f && rounded < 1) rounded = 1;
            if (percent < 100f && rounded > 99) rounded = 99;

            return rounded;
        }
    }
}
