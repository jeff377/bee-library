namespace Bee.Definition.Database
{
    /// <summary>
    /// Database schema upgrade action.
    /// </summary>
    public enum DbUpgradeAction
    {
        /// <summary>
        /// Schema is consistent; no upgrade needed.
        /// </summary>
        None,
        /// <summary>
        /// New schema element to be created.
        /// </summary>
        New,
        /// <summary>
        /// Existing schema element to be upgraded.
        /// </summary>
        Upgrade
    }
}
