using Bee.Definition.Database;

namespace Bee.Db.Schema.Changes
{
    /// <summary>
    /// Represents a field whose definition differs between the defined schema and the actual database.
    /// </summary>
    public sealed class AlterFieldChange : TableChange
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AlterFieldChange"/>.
        /// </summary>
        /// <param name="oldField">The field definition as it currently exists in the database.</param>
        /// <param name="newField">The target field definition from the defined schema.</param>
        public AlterFieldChange(DbField oldField, DbField newField)
        {
            OldField = oldField;
            NewField = newField;
        }

        /// <summary>
        /// Gets the field definition as it currently exists in the database.
        /// </summary>
        public DbField OldField { get; }

        /// <summary>
        /// Gets the target field definition sourced from the defined schema.
        /// </summary>
        public DbField NewField { get; }

        /// <inheritdoc />
        public override string Describe() => $"AlterFieldChange on '{NewField.FieldName}'";
    }
}
