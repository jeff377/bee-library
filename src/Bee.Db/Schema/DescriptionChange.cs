namespace Bee.Db.Schema
{
    /// <summary>
    /// Represents a description (MS_Description / COMMENT) drift between the defined and the actual schema.
    /// </summary>
    public class DescriptionChange
    {
        /// <summary>
        /// Gets or sets the metadata level.
        /// </summary>
        public DescriptionLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the column name; used when <see cref="Level"/> is <see cref="DescriptionLevel.Column"/>.
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new description value to be applied.
        /// </summary>
        public string NewValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this description does not yet exist in the database
        /// (true → use sp_addextendedproperty; false → use sp_updateextendedproperty).
        /// </summary>
        public bool IsNew { get; set; }
    }
}
