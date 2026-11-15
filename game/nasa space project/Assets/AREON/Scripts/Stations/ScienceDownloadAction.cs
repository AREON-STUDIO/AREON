// AREON - Stage 7 / Task 31
// Path: Assets/AREON/Scripts/Stations/ScienceDownloadAction.cs
//
// The real "Download Science Telemetry" transaction. ScienceTerminal (Stage 6) keeps the
// interaction + panel flow and calls TryDownload() when the player confirms.
//
// Rules (blueprint 1.4 / Task 31):
//  - Costs exactly 15 Power (Inspector default; the Stage 7 tests assert 15).
//  - Rejected, with NO Power spent, when Power is insufficient.
//  - A configured science packet can be downloaded once, unless repeat downloads are enabled.
//  - Power is spent first through ResourceManager's atomic TrySpendPower, and the packet is
//    only marked "downloaded" after the spend succeeded.

using System;
using System.Collections.Generic;
using UnityEngine;
using AREON.ResourceSystem;
using AREON.Missions;

public class ScienceDownloadAction : MonoBehaviour, IMissionResettable
{
    public void ResetRuntimeState() => ResetDownloads();
    [Header("Dependencies")]
    [SerializeField] private ResourceManager resourceManager;

    [Header("Science packet")]
    [Tooltip("Unique id of the packet this terminal serves. Stage 9 telemetry will supply real ids.")]
    [SerializeField] private string packetId = "prototype-packet-01";

    [SerializeField] private string packetDisplayName = "Science Telemetry Packet";

    [Tooltip("Blueprint: only once per packet unless the scenario explicitly allows repeats.")]
    [SerializeField] private bool allowRepeatDownloads = false;

    [Header("Cost (blueprint 1.4: exactly 15)")]
    [SerializeField, Min(0f)] private float powerCost = 15f;

    // Runtime state only. Cleared by ResetDownloads() when a new mission/session starts.
    private readonly HashSet<string> downloadedPacketIds = new HashSet<string>();

    public float PowerCost => powerCost;
    public void SetPowerCost(float cost) => powerCost = Mathf.Max(0f, cost);
    public string PacketId => packetId;
    public string PacketDisplayName => packetDisplayName;
    public bool AllowRepeatDownloads => allowRepeatDownloads;
    public bool HasDownloadedCurrentPacket => downloadedPacketIds.Contains(packetId);

    /// <summary>Raised after a successful download. Argument = packet id.</summary>
    public event Action<string> ScienceDownloaded;

    /// <summary>Raised after a successful download (Stage 9 objective tracking).</summary>
    public event Action Succeeded;

    /// <summary>Raised after every attempt, success or failure, so UI can show feedback.</summary>
    public event Action<ActionResult> DownloadAttempted;

    private void Awake()
    {
        if (resourceManager == null)
        {
            Debug.LogError("[ScienceDownloadAction] ResourceManager reference is missing. " +
                           "Assign it in the Inspector.", this);
        }
    }

    /// <summary>
    /// Checks availability without spending anything. Use it to grey out a confirm button.
    /// </summary>
    public bool CanDownload(out string reason)
    {
        if (resourceManager == null || string.IsNullOrWhiteSpace(packetId))
        {
            reason = "Science terminal is not configured.";
            return false;
        }

        if (HasDownloadedCurrentPacket && !allowRepeatDownloads)
        {
            reason = "Science packet already downloaded.";
            return false;
        }

        float power = ResourceManagerBridge.GetPower(resourceManager);
        if (power < powerCost)
        {
            reason = BuildInsufficientPowerMessage(power);
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>Performs the download. Never throws; always returns a result with a message.</summary>
    public ActionResult TryDownload()
    {
        ActionResult result = Execute();
        DownloadAttempted?.Invoke(result);
        if (result.Succeeded)
        {
            ScienceDownloaded?.Invoke(packetId);
            Succeeded?.Invoke();
        }

        return result;
    }

    /// <summary>Supplies packet settings from a future MissionScenarioData / TelemetryManager.</summary>
    public void ConfigurePacket(string newPacketId, string newDisplayName, bool allowRepeat)
    {
        packetId = newPacketId;
        packetDisplayName = newDisplayName;
        allowRepeatDownloads = allowRepeat;
    }

    public void SetAllowRepeatDownloads(bool allow)
    {
        allowRepeatDownloads = allow;
    }

    public bool HasDownloaded(string id)
    {
        return downloadedPacketIds.Contains(id);
    }

    /// <summary>Forgets all downloaded packets (new mission/session, or tests).</summary>
    public void ResetDownloads()
    {
        downloadedPacketIds.Clear();
    }

    // ---- Internals --------------------------------------------------------------------

    private ActionResult Execute()
    {
        if (resourceManager == null || string.IsNullOrWhiteSpace(packetId))
        {
            return ActionResult.Failure(ActionOutcome.NotConfigured, "Science terminal is not configured.");
        }

        // 1. Duplicate check FIRST: a duplicate must never spend Power.
        if (HasDownloadedCurrentPacket && !allowRepeatDownloads)
        {
            return ActionResult.Failure(ActionOutcome.AlreadyDownloaded, "Science packet already downloaded.");
        }

        // 2. Friendly pre-check, then the atomic spend (the real authority).
        float power = ResourceManagerBridge.GetPower(resourceManager);
        if (power < powerCost || !ResourceManagerBridge.TrySpendPower(resourceManager, powerCost))
        {
            return ActionResult.Failure(ActionOutcome.InsufficientPower, BuildInsufficientPowerMessage(power));
        }

        // 3. Only after a successful spend is the packet recorded.
        downloadedPacketIds.Add(packetId);

        return ActionResult.Success(
            packetDisplayName + " downloaded. -" + FormatAmount(powerCost) + " Power.",
            powerCost);
    }

    private string BuildInsufficientPowerMessage(float currentPower)
    {
        return "Not enough Power. Need " + FormatAmount(powerCost) + ", have " + FormatAmount(currentPower) + ".";
    }

    private static string FormatAmount(float value)
    {
        return value.ToString("0.#");
    }
}
