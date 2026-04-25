using Bee.Definition.Database;

namespace Bee.Db.Schema.Changes
{
    /// <summary>
    /// Represents a field rename: the actual database column currently has <see cref="OldFieldName"/>
    /// and should be renamed to match the defined schema's current <see cref="DbField.FieldName"/>.
    /// Detected by <see cref="TableSchemaComparer"/> when <see cref="DbField.OriginalFieldName"/> is set
    /// on a defined field whose current name does not yet exist in the database.
    /// </summary>
    public sealed class RenameFieldChange : TableChange
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RenameFieldChange"/>.
        /// </summary>
        /// <param name="oldFieldName">The current column name in the database.</param>
        /// <param name="newField">The target field definition (its <see cref="DbField.FieldName"/> is the new name).</param>
        public RenameFieldChange(string oldFieldName, DbField newField)
        {
            OldFieldName = oldFieldName;
            NewField = newField;
        }

        /// <summary>
        /// Gets the current column name in the database that will be renamed.
        /// </summary>
        public string OldFieldName { get; }

        /// <summary>
        /// Gets the target field definition; <see cref="DbField.FieldName"/> is the new column name.
        /// </summary>
        public DbField NewField { get; }

        /// <inheritdoc />
        public override string Describe() => $"RenameFieldChange '{OldFieldName}' -> '{NewField.FieldName}'";
    }
}
