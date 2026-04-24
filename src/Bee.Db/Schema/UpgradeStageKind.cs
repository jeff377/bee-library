namespace Bee.Db.Schema
{
    /// <summary>
    /// Identifies which step of the upgrade pipeline a <see cref="UpgradeStage"/> represents.
    /// Each stage runs inside its own transaction.
    /// </summary>
    public enum UpgradeStageKind
    {
        /// <summary>
        /// Drops existing indexes whose definition will be changed (recreated in <see cref="CreateIndexes"/>).
        /// </summary>
        DropIndexes,

        /// <summary>
        /// Renames and alters existing columns (rename happens before alter when both are emitted for a field).
        /// </summary>
        AlterColumns,

        /// <summary>
        /// Adds new columns.
        /// </summary>
        AddColumns,

        /// <summary>
        /// Creates new or recreated indexes.
        /// </summary>
        CreateIndexes,

        /// <summary>
        /// Synchronizes table / column descriptions via extended properties.
        /// </summary>
        SyncDescriptions,

        /// <summary>
        /// Creates a new table from scratch (used only when the table does not yet exist).
        /// </summary>
        CreateTable,

        /// <summary>
        /// Performs a full rebuild (temp table + copy + swap) as fallback for incompatible changes.
        /// </summary>
        Rebuild,
    }
}
