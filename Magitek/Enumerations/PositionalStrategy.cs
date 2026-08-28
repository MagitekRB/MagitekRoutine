namespace Magitek.Enumerations
{
    /// <summary>
    /// How a job should treat its target's positional requirements.
    /// </summary>
    public enum PositionalStrategy
    {
        /// <summary>Use the target's own IsOmnidirectional flag to decide.</summary>
        Auto,

        /// <summary>Always work positionals, even on targets that do not have them.</summary>
        Always,

        /// <summary>Never work positionals - no True North, no repositioning, no nagging.</summary>
        Never
    }
}
