using Bee.Definition.Database;

namespace Bee.Db.Schema.Changes
{
    /// <summary>
    /// Represents an index that exists in the defined schema but not in the actual database,
    /// or a recreated index following a <see cref="DropIndexChange"/>.
    /// </summary>
    public sealed class AddIndexChange : ITableChange
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AddIndexChange"/>.
        /// </summary>
        /// <param name="index">The index to be created.</param>
        public AddIndexChange(TableSchemaIndex index)
        {
            Index = index;
        }

        /// <summary>
        /// Gets the index to be created (sourced from the defined schema; name may be a template).
        /// </summary>
        public TableSchemaIndex Index { get; }

        /// <inheritdoc />
        public string Describe() => $"AddIndexChange on '{Index.Name}'";
    }
}
