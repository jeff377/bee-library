using Bee.Definition.Database;

namespace Bee.Db.Schema.Changes
{
    /// <summary>
    /// Represents a field that exists in the defined schema but not in the actual database.
    /// </summary>
    public sealed class AddFieldChange : TableChange
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AddFieldChange"/>.
        /// </summary>
        /// <param name="field">The field to be added.</param>
        public AddFieldChange(DbField field)
        {
            Field = field;
        }

        /// <summary>
        /// Gets the field to be added (sourced from the defined schema).
        /// </summary>
        public DbField Field { get; }

        /// <inheritdoc />
        public override string Describe() => $"AddFieldChange on '{Field.FieldName}'";
    }
}
