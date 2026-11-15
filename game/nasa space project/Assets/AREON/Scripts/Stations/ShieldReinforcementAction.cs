// AREON - Stage 7 / Task 32
// Path: Assets/AREON/Scripts/Stations/ShieldReinforcementAction.cs
//
// The real "Reinforce Protective Shield" transaction. ShieldConsole (Stage 6) keeps the
// interaction + panel flow and calls TryReinforce() when the player confirms.
//
// Rules (blueprint 1.4 / Task 32):
//  - Costs exactly 10 Power.
//  - Restores a configurable amount of Shield (prototype tuning value - needs playtesting).
//  - Rejected, with NO Power spent, when Shield is already full.
//  - Rejected, with NO Power spent, when Power is insufficient.
//  - Shield can never exceed 100 (ResourceManager clamps; the result reports the real amount).

using System;
using UnityEngine;
using AREON.ResourceSystem;
using AREON.Missions;

public class ShieldReinforcementAction : MonoBehaviour, IMissionResettable
{
    [Header("Dependencies")]
    [SerializeField] private ResourceManager resourceManager;

    [Header("Cost (blueprint 1.4: exactly 10)")]
    [SerializeField, Min(0f)] private float powerCost = 10f;

    [Header("Effect (prototype tuning value - requires playtesting)")]
    [SerializeField, Range(1f, 100f)] private float shieldRestoreAmount = 25f;

    public float PowerCost => powerCost;
    public float ShieldRestoreAmount => shieldRestoreAmount;
    public int ReinforceCount { get; private set; }
    public void ResetReinforcements() => ReinforceCount = 0;
    public void ResetRuntimeState() => ResetReinforcements();

    /// <summary>Raised after a successful reinforcement. Argument = Shield points actually restored.</summary>
    public event Action<float> ShieldReinforced;

    /// <summary>Raised after a successful reinforcement (Stage 9 objective tracking).</summary>
    public event Action Succeeded;

    /// <summary>Raised after every attempt, success or failure, so UI can show feedback.</summary>
    public event Action<ActionResult> ReinforcementAttempted;

    private void Awake()
    {
        if (resourceManager == null)
        {
            Debug.LogError("[ShieldReinforcementAction] ResourceManager reference is missing. " +
                           "Assign it in the Inspector.", this);
        }
    }

    /// <summary>Checks availability without changing anything. Use it to grey out a confirm button.</summary>
    public bool CanReinforce(out string reason)
    {
        if (resourceManager == null)
        {
            reason = "Shield console is not configured.";
            return false;
        }

        if (ResourceManagerBridge.IsShieldFull(resourceManager))
        {
            reason = "Shield is already full.";
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

    /// <summary>Performs the reinforcement. Never throws; always returns a result with a message.</summary>
    public ActionResult TryReinforce()
    {
        ActionResult result = Execute();
        ReinforcementAttempted?.Invoke(result);
        if (result.Succeeded)
        {
            ShieldReinforced?.Invoke(result.AmountApplied);
            Succeeded?.Invoke();
        }

        return result;
    }

    public void SetPowerCost(float cost) => powerCost = Mathf.Max(0f, cost);
    public void SetRestoreAmount(float amount) => SetShieldRestoreAmount(amount);

    public void SetShieldRestoreAmount(float amount)
    {
        shieldRestoreAmount = Mathf.Clamp(amount, 1f, ResourceManagerBridge.MaxResource);
    }

    // ---- Internals --------------------------------------------------------------------

    private ActionResult Execute()
    {
        if (resourceManager == null)
        {
            return ActionResult.Failure(ActionOutcome.NotConfigured, "Shield console is not configured.");
        }

        // 1. Shield full is checked first: nothing to reinforce, so never spend Power.
        if (ResourceManagerBridge.IsShieldFull(resourceManager))
        {
            return ActionResult.Failure(ActionOutcome.ShieldFull, "Shield is already full.");
        }

        // 2. Friendly pre-check, then the atomic Power spend (the real authority).
        float power = ResourceManagerBridge.GetPower(resourceManager);
        if (power < powerCost || !ResourceManagerBridge.TrySpendPower(resourceManager, powerCost))
        {
            return ActionResult.Failure(ActionOutcome.InsufficientPower, BuildInsufficientPowerMessage(power));
        }

        // 3. Apply the effect. ResourceManager clamps at 100; we report what really changed.
        float restored = ResourceManagerBridge.RestoreShield(resourceManager, shieldRestoreAmount);

        if (restored <= 0f)
        {
            // Should be impossible (Shield was below max). Refund so Power is never lost for nothing.
            ResourceManagerBridge.RechargePower(resourceManager, powerCost);
            Debug.LogError("[ShieldReinforcementAction] Shield did not change after Power was spent. " +
                           "Power was refunded. Check ResourceManager.TryRestoreShield.", this);
            return ActionResult.Failure(ActionOutcome.InternalError, "Reinforcement failed. No Power was spent.");
        }

        ReinforceCount++;
        return ActionResult.Success(
            "Shield reinforced. +" + restored.ToString("0.#") + "% Shield, -" +
            powerCost.ToString("0.#") + " Power.",
            powerCost,
            restored);
    }

    private string BuildInsufficientPowerMessage(float currentPower)
    {
        return "Not enough Power. Need " + powerCost.ToString("0.#") + ", have " +
               currentPower.ToString("0.#") + ".";
    }
}
