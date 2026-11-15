using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace AREON.Missions
{
    /// <summary>
    /// Provides the selected scenario's CURATED telemetry (Sol, pressure, solar flare, storm) as
    /// records and readable text. It reads only local MissionScenarioData assets.
    /// It makes NO network calls. WeatherManager still owns the environmental STATE; this class
    /// only presents the scientific record to the UI and the science-data report.
    /// </summary>
    public class TelemetryManager : MonoBehaviour
    {
        [Header("Curated records (the three scenario assets)")]
        [SerializeField] private MissionScenarioData[] knownScenarios;

        [Header("Text")]
        [TextArea(2, 3)]
        [SerializeField] private string disclosureText =
            "CURATED HISTORICAL TRAINING DATA.\\nNOT LIVE TELEMETRY.";
        [Tooltip("Blueprint 1.7: pressure below 800 Pa may be flagged as a dust-front hint.")]
        [SerializeField] private float referencePressurePa = 800f;

        private TelemetryRecord current;

        public bool HasData { get; private set; }
        public TelemetryRecord Current => current;
        public string DisclosureText => disclosureText;

        /// <summary>Always false. Stage 9 never calls NASA or any other external API.</summary>
        public bool UsesExternalApi => false;

        /// <summary>Raised when the active record changes or is cleared.</summary>
        public event Action TelemetryChanged;

        // ------------------------------------------------------------------ lifecycle (called by MissionManager)

        public void Initialize(MissionScenarioData scenario)
        {
            if (scenario == null) { Clear(); return; }
            current = FromScenario(scenario);
            HasData = true;
            TelemetryChanged?.Invoke();
        }

        public void Clear()
        {
            HasData = false;
            current = default;
            TelemetryChanged?.Invoke();
        }

        // ------------------------------------------------------------------ records

        // ADAPT: the only place that reads MissionScenarioData fields for telemetry.
        public static TelemetryRecord FromScenario(MissionScenarioData s) =>
            new TelemetryRecord(s.solNumber, s.displayName, s.pressurePa, s.solarFlareClass, s.stormStatus);

        /// <summary>Finds a curated record by Sol number among the known scenarios.</summary>
        public bool TryGetRecord(int sol, out TelemetryRecord record)
        {
            if (knownScenarios != null)
            {
                foreach (MissionScenarioData s in knownScenarios)
                {
                    if (s != null && s.solNumber == sol) { record = FromScenario(s); return true; }
                }
            }
            record = default;
            return false;
        }

        // ------------------------------------------------------------------ readable text

        public string SolText(TelemetryRecord r) => r.Sol.ToString(CultureInfo.InvariantCulture);

        public string PressureText(TelemetryRecord r) =>
            r.PressurePa.ToString("0.#", CultureInfo.InvariantCulture) + " Pa";

        public string SolarEventText(TelemetryRecord r)
        {
            switch (r.SolarFlare)
            {
                case SolarFlareClass.C: return "C-CLASS FLARE";
                case SolarFlareClass.X: return "X-CLASS FLARE";
                default: return "NONE";
            }
        }

        public string WeatherText(TelemetryRecord r)
        {
            switch (r.Storm)
            {
                case StormStatus.Regional: return "REGIONAL DUST STORM";
                case StormStatus.SevereGlobal: return "SEVERE GLOBAL DUST STORM";
                default: return "CLEAR";
            }
        }

        /// <summary>Text for the always-visible telemetry panel.</summary>
        public string BuildPanelText(TelemetryRecord r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("NASA MARS TELEMETRY").AppendLine();
            sb.Append("SOL: ").AppendLine(SolText(r));
            sb.Append("PRESSURE: ").AppendLine(PressureText(r));
            sb.Append("SOLAR EVENT: ").AppendLine(SolarEventText(r));
            sb.Append("WEATHER: ").AppendLine(WeatherText(r)).AppendLine();
            sb.AppendLine("DATA DISCLOSURE:");
            sb.Append(disclosureText);
            return sb.ToString();
        }

        /// <summary>Text for the science data packet shown after a successful science download.</summary>
        public string BuildScienceReportText(TelemetryRecord r)
        {
            var sb = new StringBuilder();
            sb.Append("SCIENCE DATA PACKET - SOL ").Append(SolText(r))
              .Append(" (").Append(string.IsNullOrEmpty(r.DisplayName) ? "SCENARIO" : r.DisplayName.ToUpperInvariant())
              .AppendLine(")").AppendLine();
            sb.Append("Atmospheric pressure: ").AppendLine(PressureText(r));
            sb.Append("Solar event: ").AppendLine(SolarEventText(r));
            sb.Append("Weather: ").AppendLine(WeatherText(r));
            sb.Append("Alert level: ").AppendLine(r.AlertLevel).AppendLine();

            if (r.PressurePa < referencePressurePa)
                sb.Append("Pressure is below the ").Append(referencePressurePa.ToString("0", CultureInfo.InvariantCulture))
                  .AppendLine(" Pa reference noted by the project (dust fronts are possible).");
            else if (r.Storm != StormStatus.Clear)
                sb.AppendLine("Pressure alone does not decide the storm state; this scenario's storm status is set explicitly.");

            sb.AppendLine("Source context cited by the project: pressure - Curiosity REMS / NASA PDS; solar events - NASA DONKI.");
            sb.AppendLine().AppendLine("DATA DISCLOSURE:");
            sb.Append(disclosureText);
            return sb.ToString();
        }
    }
}
