// AREON - Stage 7 / Task 33
// Path: Assets/AREON/Scripts/Environment/DaylightManager.cs
//
// Owns the compressed "five-minute daylight window" (blueprint 1.6).
// It is a gameplay timer, NOT a real Martian day/night cycle.
//
// - Counts down from a configurable duration (default 300 s) using Time.deltaTime.
// - Stops at exactly 0 and never goes negative.
// - Raises events when daylight starts and ends so other systems (SolarGenerator, HUD)
//   react immediately instead of polling.
// - Does not know about Power, scenarios or the HUD.

using System;
using UnityEngine;

// Runs before other scripts each frame, so "daylight just ended" is already known when
// SolarGenerator ticks in the same frame.
[DefaultExecutionOrder(-100)]
public class DaylightManager : MonoBehaviour
{
    public const string NightMessage = "NIGHT \u2014 SOLAR CHARGING DISABLED";
    public const string CountdownPrefix = "DAYLIGHT REMAINING: ";

    [Header("Configuration (prototype tuning - blueprint default is 300 s)")]
    [SerializeField, Min(1f)] private float configuredDurationSeconds = 300f;

    [Tooltip("Prototype session start. Stage 9's MissionManager will later call StartDaylight(...) " +
             "itself and this box can be turned off.")]
    [SerializeField] private bool startOnPlay = true;

    private bool simulationRunning = true;

    private float remainingSeconds;
    private float sessionDurationSeconds;
    private bool isDaylightActive;
    private bool hasStarted;
    private int lastAnnouncedSecond = -1;

    // ---- Public read API --------------------------------------------------------------

    /// <summary>True only while the countdown is running.</summary>
    public bool IsDaylightActive => isDaylightActive;

    /// <summary>True once a daylight session has been started (even after it ended).</summary>
    public bool HasStarted => hasStarted;

    /// <summary>Seconds left. Always &gt;= 0 and exactly 0 after expiry.</summary>
    public float RemainingSeconds => remainingSeconds;

    /// <summary>Length of the current/last session.</summary>
    public float DurationSeconds => sessionDurationSeconds;

    /// <summary>Inspector/default duration used by StartDaylight().</summary>
    public float ConfiguredDurationSeconds => configuredDurationSeconds;

    /// <summary>1 at the start of daylight, 0 at expiry.</summary>
    public float NormalizedRemaining =>
        sessionDurationSeconds > 0f ? Mathf.Clamp01(remainingSeconds / sessionDurationSeconds) : 0f;

    // ---- Events -----------------------------------------------------------------------

    /// <summary>Raised when a daylight session starts (or restarts).</summary>
    public event Action DaylightStarted;

    /// <summary>Raised exactly once when a session reaches zero or is force-ended.</summary>
    public event Action DaylightEnded;

    /// <summary>Raised when the whole-second value shown on the HUD changes (about once per second).</summary>
    public event Action RemainingTimeChanged;

    // ---- Unity lifecycle --------------------------------------------------------------

    private void Awake()
    {
        if (configuredDurationSeconds < 1f)
        {
            Debug.LogError("[DaylightManager] Configured duration must be at least 1 second. Using 300.", this);
            configuredDurationSeconds = 300f;
        }

        sessionDurationSeconds = configuredDurationSeconds;
        remainingSeconds = configuredDurationSeconds;
    }

    private void Start()
    {
        // Start() (not Awake) so every HUD/SolarGenerator has already subscribed in OnEnable.
        if (startOnPlay)
        {
            StartDaylight();
        }
    }

    private void Update()
    {
        if (!isDaylightActive || !simulationRunning)
        {
            return;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);

        if (remainingSeconds <= 0f)
        {
            EndDaylightInternal();
            return;
        }

        AnnounceIfWholeSecondChanged();
    }

    private void OnValidate()
    {
        if (configuredDurationSeconds < 1f)
        {
            configuredDurationSeconds = 1f;
        }
    }

    // ---- Public control API -----------------------------------------------------------

    /// <summary>Starts (or restarts) daylight using the configured duration.</summary>
    public bool StartDaylight()
    {
        return StartDaylight(configuredDurationSeconds);
    }

    /// <summary>
    /// Starts (or restarts) daylight with an explicit duration. Future MissionScenarioData
    /// will supply this value. Returns false and logs an error if durationSeconds is not positive.
    /// </summary>
    public bool StartDaylight(float durationSeconds)
    {
        if (durationSeconds <= 0f || float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds))
        {
            Debug.LogError("[DaylightManager] StartDaylight needs a positive, finite duration. Got " +
                           durationSeconds + ".", this);
            return false;
        }

        sessionDurationSeconds = durationSeconds;
        remainingSeconds = durationSeconds;
        isDaylightActive = true;
        hasStarted = true;
        lastAnnouncedSecond = Mathf.CeilToInt(remainingSeconds);

        DaylightStarted?.Invoke();
        RemainingTimeChanged?.Invoke();
        return true;
    }

    /// <summary>Sets the default duration for future StartDaylight() calls. Does not affect a running session.</summary>
    public void SetConfiguredDuration(float durationSeconds)
    {
        if (durationSeconds < 1f)
        {
            Debug.LogError("[DaylightManager] Configured duration must be at least 1 second.", this);
            return;
        }

        configuredDurationSeconds = durationSeconds;
    }

    // ---- Stage 8: scenario configuration -------------------------------------------------
    /// <summary>Sets the daylight window length and restarts the countdown.</summary>
    public void ConfigureDuration(float seconds)
    {
        float valid = Mathf.Max(1f, seconds);
        SetConfiguredDuration(valid);
        StartDaylight(valid);
    }

    // ---- Stage 9: mission simulation gate and scenario configuration ---------------------
    public void SetRunning(bool running) => simulationRunning = running;

    public void ConfigureFromScenario(MissionScenarioData data)
    {
        if (data == null) return;
        ConfigureDuration(data.DaylightDurationSeconds);
    }

    /// <summary>Ends daylight immediately (mission end, debug, tests). No-op if daylight is not active.</summary>
    public void ForceEndDaylight()
    {
        if (!isDaylightActive)
        {
            return;
        }

        remainingSeconds = 0f;
        EndDaylightInternal();
    }

    // ---- Display helpers (HUDController calls these; it does not compute time itself) -

    /// <summary>"DAYLIGHT REMAINING: 04:59" while active, "NIGHT — SOLAR CHARGING DISABLED" after expiry.</summary>
    public string GetDisplayText()
    {
        if (isDaylightActive)
        {
            return CountdownPrefix + FormatTime(remainingSeconds);
        }

        if (hasStarted)
        {
            return NightMessage;
        }

        // Not started yet: show the full window rather than an empty label.
        return CountdownPrefix + FormatTime(configuredDurationSeconds);
    }

    /// <summary>Formats seconds as MM:SS, rounding UP so "00:00" appears only at true zero.</summary>
    public static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = total / 60;
        int secs = total % 60;
        return minutes.ToString("00") + ":" + secs.ToString("00");
    }

    // ---- Internals --------------------------------------------------------------------

    private void AnnounceIfWholeSecondChanged()
    {
        int wholeSeconds = Mathf.CeilToInt(remainingSeconds);
        if (wholeSeconds == lastAnnouncedSecond)
        {
            return;
        }

        lastAnnouncedSecond = wholeSeconds;
        RemainingTimeChanged?.Invoke();
    }

    private void EndDaylightInternal()
    {
        remainingSeconds = 0f;
        isDaylightActive = false;
        lastAnnouncedSecond = 0;

        // Order matters: a subscriber reading IsDaylightActive inside either event sees false.
        RemainingTimeChanged?.Invoke();
        DaylightEnded?.Invoke();
    }
}
