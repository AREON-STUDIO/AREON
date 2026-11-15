using System;
using UnityEngine;

namespace AREON.Stations
{
    /// <summary>
    /// Plain serializable data describing what a facility shows to the player.
    /// This holds DISPLAY data only. It never stores Oxygen, Power or Shield values
    /// (ResourceManager is the only owner of those).
    /// </summary>
    [Serializable]
    public class FacilityInfo
    {
        [Tooltip("Large title shown on the panel, e.g. 'Base Dome'.")]
        public string facilityName = "Facility";

        [TextArea(2, 4)]
        [Tooltip("One or two sentences explaining what this facility is for.")]
        public string purpose = "";

        [Tooltip("Name of the action offered here, e.g. 'Reinforce Protective Shield'.")]
        public string actionName = "";

        [Min(0f)]
        [Tooltip("Power cost of the action in Power percentage points. 0 = no cost.")]
        public float powerCost = 0f;

        [Tooltip("Default status line. Stations may override this with live text.")]
        public string statusText = "";

        [Tooltip("Colour of the accent bar at the top of the panel.")]
        public Color accentColor = Color.white;

        public bool HasPowerCost => powerCost > 0f;
    }
}
