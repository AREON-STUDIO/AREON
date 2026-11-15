using AREON.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace AREON.UI
{
    /// <summary>
    /// TEMPORARY gameplay prompt (not the final HUD). Shows one line such as "[ E ] Interact".
    /// It only displays what PlayerInteractor reports; it never detects targets itself,
    /// so two prompts can never appear at once.
    /// Uses the built-in uGUI Text component, so no extra packages or TMP resources are needed.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractor interactor;
        [Tooltip("The panel that is shown/hidden (parent of the label).")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptLabel;

        [Header("Appearance")]
        [Tooltip("{0} is replaced with the key label, e.g. E.")]
        [SerializeField] private string keyFormat = "[ {0} ]";
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color unavailableColor = new Color(1f, 0.75f, 0.3f, 1f);

        private bool referencesValid;

        private void Awake()
        {
            referencesValid = interactor != null && promptRoot != null && promptLabel != null;

            if (!referencesValid)
            {
                Debug.LogError("[InteractionPromptUI] Missing reference. Assign Interactor, Prompt Root and Prompt Label in the Inspector.", this);
                if (promptRoot != null) promptRoot.SetActive(false);
                enabled = false;
                return;
            }

            if (promptLabel.font == null)
            {
                promptLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        private void OnEnable()
        {
            if (!referencesValid) return;
            interactor.PromptChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (interactor != null) interactor.PromptChanged -= Refresh;
            if (promptRoot != null) promptRoot.SetActive(false);
        }

        private void Refresh()
        {
            if (interactor == null || !interactor.HasTarget)
            {
                promptRoot.SetActive(false);
                return;
            }

            if (interactor.IsTargetAvailable)
            {
                promptLabel.text = string.Format(keyFormat, interactor.KeyLabel) + " " + interactor.PromptText;
                promptLabel.color = availableColor;
            }
            else
            {
                string reason = interactor.UnavailableReason;
                promptLabel.text = string.IsNullOrEmpty(reason)
                    ? interactor.PromptText
                    : interactor.PromptText + " - " + reason;
                promptLabel.color = unavailableColor;
            }

            promptRoot.SetActive(true);
        }
    }
}
