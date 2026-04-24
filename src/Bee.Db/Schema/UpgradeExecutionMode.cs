namespace Bee.Db.Schema
{
    /// <summary>
    /// Describes how an <see cref="UpgradePlan"/> will be carried out.
    /// </summary>
    public enum UpgradeExecutionMode
    {
        /// <summary>
        /// No structural or description changes are needed.
        /// </summary>
        NoChange,

        /// <summary>
        /// The table does not yet exist and will be created in full.
        /// </summary>
        Create,

        /// <summary>
        /// The table exists and will be modified in place via ALTER statements.
        /// </summary>
        Alter,

        /// <summary>
        /// At least one change requires a full table rebuild (temporary table, copy, swap).
        /// </summary>
        Rebuild,
    }
}
