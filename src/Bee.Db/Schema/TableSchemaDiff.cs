using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Schema
{
    /// <summary>
    /// Structured diff result produced by <see cref="TableSchemaComparer.CompareToDiff"/>.
    /// Provider-agnostic: describes what changed, not how the changes should be executed.
    /// </summary>
    public class TableSchemaDiff
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TableSchemaDiff"/>.
        /// </summary>
        /// <param name="defineTable">The defined table schema.</param>
        /// <param name="realTable">The actual table schema from the database; null when the table does not yet exist.</param>
        public TableSchemaDiff(TableSchema defineTable, TableSchema? realTable)
        {
            DefineTable = defineTable;
            RealTable = realTable;
        }

        /// <summary>
        /// Gets the defined table schema.
        /// </summary>
        public TableSchema DefineTable { get; }

        /// <summary>
        /// Gets the actual table schema from the database; null when the table does not yet exist.
        /// </summary>
        public TableSchema? RealTable { get; }

        /// <summary>
        /// Gets a value indicating whether the table is entirely new (no actual table exists yet).
        /// </summary>
        public bool IsNewTable => RealTable == null;

        /// <summary>
        /// Gets the structural changes required to align the real table with the defined schema.
        /// </summary>
        public List<ITableChange> Changes { get; } = [];

        /// <summary>
        /// Gets the description (MS_Description) drift between the defined and actual schema.
        /// </summary>
        public List<DescriptionChange> DescriptionChanges { get; } = [];

        /// <summary>
        /// Gets a value indicating whether the diff contains no changes.
        /// For a brand-new table (<see cref="IsNewTable"/> = true) this returns false.
        /// </summary>
        public bool IsEmpty => !IsNewTable && Changes.Count == 0 && DescriptionChanges.Count == 0;
    }
}
