namespace Bee.Db.Schema
{
    /// <summary>
    /// Represents the metadata level of a description drift.
    /// </summary>
    public enum DescriptionLevel
    {
        /// <summary>
        /// Table-level description (sourced from <see cref="Bee.Definition.Database.TableSchema.DisplayName"/>).
        /// </summary>
        Table,

        /// <summary>
        /// Column-level description (sourced from <see cref="Bee.Definition.Database.DbField.Caption"/>).
        /// </summary>
        Column,
    }
}
