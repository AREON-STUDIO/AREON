// PATH: Assets/AREON/Scripts/Missions/StormStatus.cs

/// <summary>
/// Explicit storm state of a curated scenario. This value is AUTHORITATIVE.
/// It is never derived from atmospheric pressure (Sol 852 is Regional at 924 Pa).
/// Do not reorder: the value order is serialized into scenario assets.
/// </summary>
public enum StormStatus
{
    Clear = 0,
    Regional = 1,
    SevereGlobal = 2
}
