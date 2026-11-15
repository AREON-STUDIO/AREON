using UnityEngine;
using AREON.UI;
using AREON.Missions;

namespace AREON.Stations
{
    /// <summary>
    /// Base Dome - Shield Control.
    /// Stage 7: runs the shield reinforcement transaction through ShieldReinforcementAction
    /// and displays feedback on the shared FacilityInteractionPanel.
    /// </summary>
    public class ShieldConsole : FacilityStation, IMissionResettable
    {
        public void ResetRuntimeState()
        {
            if (reinforcementAction != null) reinforcementAction.ResetReinforcements();
        }
        [Header("Stage 7 - Action Component")]
        [SerializeField] private ShieldReinforcementAction reinforcementAction;

        public ShieldReinforcementAction ReinforcementAction => reinforcementAction;

        protected override string DefaultInteractionName => "Shield Control";

        protected override FacilityInfo CreateDefaultInfo()
        {
            float cost = reinforcementAction != null ? reinforcementAction.PowerCost : 10f;
            return new FacilityInfo
            {
                facilityName = "Base Dome",
                purpose = "Habitat and life support. Use the shield control to protect the outpost from radiation.",
                actionName = "Reinforce Protective Shield",
                powerCost = cost,
                statusText = "Shield control online",
                accentColor = new Color(0.30f, 0.70f, 1.00f, 1f)
            };
        }

        protected override void Awake()
        {
            base.Awake();
            if (reinforcementAction == null)
            {
                reinforcementAction = GetComponent<ShieldReinforcementAction>();
                if (reinforcementAction == null)
                {
                    Debug.LogError("[ShieldConsole] ShieldReinforcementAction reference is missing. Assign it in the Inspector.", this);
                }
            }
        }

        protected override void OnConfirm()
        {
            ConfirmReinforcement();
        }

        public void ConfirmReinforcement()
        {
            if (reinforcementAction == null)
            {
                Debug.LogError("[ShieldConsole] ShieldReinforcementAction reference is missing. Cannot perform reinforcement.", this);
                return;
            }

            ActionResult result = reinforcementAction.TryReinforce();
            if (panel != null)
            {
                panel.ShowFeedback(result.Message, result.Succeeded);
            }
        }
    }
}
