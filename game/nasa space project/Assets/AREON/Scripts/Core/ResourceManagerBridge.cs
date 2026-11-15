// AREON - Stage 7
// Path: Assets/AREON/Scripts/Core/ResourceManagerBridge.cs
//
// THE ONLY FILE IN STAGE 7 THAT TOUCHES ResourceManager's method names.
//
// Why it exists: ResourceManager (Stage 4) is the single owner of Oxygen, Power and Shield.
// Stage 7 scripts must never store their own copy of those values. They go through this
// bridge, which only calls ResourceManager's public API.

using UnityEngine;
using AREON.ResourceSystem;

public static class ResourceManagerBridge
{
    /// <summary>Upper bound for every percentage resource (blueprint 2.5: normally 0-100).</summary>
    public const float MaxResource = 100f;

    /// <summary>Tolerance used when deciding a value is "full".</summary>
    public const float FullEpsilon = 0.0001f;

    // ---- READ -------------------------------------------------------------------------

    // ASSUMPTION A1: ResourceManager exposes a getter that takes a ResourceType.
    public static float GetPower(ResourceManager rm)  { return rm.GetValue(ResourceType.Power); }
    public static float GetShield(ResourceManager rm) { return rm.GetValue(ResourceType.Shield); }

    public static bool IsShieldFull(ResourceManager rm)
    {
        return GetShield(rm) >= MaxResource - FullEpsilon;
    }

    public static bool IsPowerFull(ResourceManager rm)
    {
        return GetPower(rm) >= MaxResource - FullEpsilon;
    }

    // ---- WRITE (gameplay) -------------------------------------------------------------

    // ASSUMPTION A2: bool TrySpendPower(float). Atomic: either spends all of it or none. (Stage 4)
    public static bool TrySpendPower(ResourceManager rm, float amount)
    {
        return rm.TrySpendPower(amount);
    }

    // ASSUMPTION A3: TryRechargePower(float) clamps at 100. Its return type is ignored on purpose,
    // so it may return bool or float. The real amount added is measured before/after, so this
    // bridge works with either.
    public static float RechargePower(ResourceManager rm, float amount)
    {
        float before = GetPower(rm);
        rm.TryRechargePower(amount);
        return Mathf.Max(0f, GetPower(rm) - before);
    }

    // ASSUMPTION A4: TryRestoreShield(float) clamps at 100 (blueprint 2.3 names this method).
    // Returns the amount actually restored (measured, not trusted).
    public static float RestoreShield(ResourceManager rm, float amount)
    {
        float before = GetShield(rm);
        rm.TryRestoreShield(amount);
        return Mathf.Max(0f, GetShield(rm) - before);
    }

    // ---- WRITE (tests only) -----------------------------------------------------------

    // ASSUMPTION A5: a setter used by Stage 4's ResourceTestHarness. Used ONLY by
    // ActionEconomyTestHarness to put resources in a known state.
    public static void SetValueForTests(ResourceManager rm, ResourceType type, float value)
    {
        float o = rm.Oxygen;
        float p = rm.Power;
        float s = rm.Shield;
        float clamped = Mathf.Clamp(value, 0f, MaxResource);
        switch (type)
        {
            case ResourceType.Oxygen: o = clamped; break;
            case ResourceType.Power: p = clamped; break;
            case ResourceType.Shield: s = clamped; break;
        }
        rm.InitializeResources(o, p, s);
    }
}
