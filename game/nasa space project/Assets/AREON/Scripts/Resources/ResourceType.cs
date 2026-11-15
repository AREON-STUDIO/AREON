namespace AREON.ResourceSystem
{
    /// <summary>
    /// The three runtime resources owned by <see cref="ResourceManager"/>.
    /// Used as the identifier in resource events and generic queries.
    /// </summary>
    public enum ResourceType
    {
        Oxygen = 0,
        Power = 1,
        Shield = 2
    }
}
