namespace Magitek.Enumerations
{
    /// <summary>
    /// Defines which Gunbreaker rotation implementation to use.
    /// </summary>
    public enum GunbreakerImplementation
    {
        /// <summary>
        /// Latest implementation (default) - current codebase version
        /// </summary>
        Latest,
        
        /// <summary>
        /// Restored v49 version from tag v1.0.49 (the working version before the problematic commit)
        /// </summary>
        V49,
        
        /// <summary>
        /// Experimental implementation
        /// </summary>
        Experimental
    }
}

