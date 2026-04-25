using Bee.Definition.Database;

namespace Bee.Db.Schema.Changes
{
    /// <summary>
    /// Represents an index that must be dropped from the database prior to re-creation.
    /// Emitted when an index definition differs between the defined schema and the actual database.
    /// Indexes present only in the database (and not in the defined schema) are preserved and do not produce this change.
    /// </summary>
    public sealed class DropIndexChange : TableChange
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DropIndexChange"/>.
        /// </summary>
        /// <param name="index">The index as it currently exists in the database.</param>
        public DropIndexChange(TableSchemaIndex index)
        {
            Index = index;
        }

        /// <summary>
        /// Gets the index as it currently exists in the database (name is already the actual DB name, not a template).
        /// </summary>
        public TableSchemaIndex Index { get; }

        /// <inheritdoc />
        public override string Describe() => $"DropIndexChange on '{Index.Name}'";
    }
}
