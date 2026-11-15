// PATH: Assets/AREON/Scripts/Missions/SolarFlareClass.cs

/// <summary>
/// Explicit solar-flare classification of a curated scenario.
/// Source records: None (Sol 800), C (Sol 852), X (Sol 2085).
/// Do not reorder: the value order is serialized into scenario assets.
/// </summary>
public enum SolarFlareClass
{
    None = 0,
    C = 1,
    X = 2
}
