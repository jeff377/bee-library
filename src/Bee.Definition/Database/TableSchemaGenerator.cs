using Bee.Definition.Forms;
using Bee.Base;

namespace Bee.Definition.Database
{
    /// <summary>
    /// Table schema generator.
    /// Responsible for converting a <see cref="FormTable"/> into a <see cref="TableSchema"/> structure.
    /// </summary>
    public static class TableSchemaGenerator
    {
        /// <summary>
        /// Generates a table schema from a form table definition.
        /// </summary>
        /// <param name="formTable">The form table.</param>
        /// <returns>The generated table schema.</returns>
        public static TableSchema Generate(FormTable formTable)
        {
            ArgumentNullException.ThrowIfNull(formTable);

            var tableSchema = new TableSchema
            {
                TableName = StrFunc.IsEmpty(formTable.DbTableName) ? formTable.TableName : formTable.DbTableName,
                DisplayName = formTable.DisplayName
            };

            AddFields(formTable, tableSchema);
            AddIndexes(formTable, tableSchema);

            return tableSchema;
        }

        /// <summary>
        /// Adds fields to the table schema.
        /// </summary>
        private static void AddFields(FormTable formTable, TableSchema tableSchema)
        {
            if (formTable.Fields == null) return;

            foreach (var field in formTable.Fields)
            {
                // Only process database-related fields
                if (field.Type != FieldType.DbField)
                    continue;

                var dbField = new DbField(field.FieldName, field.Caption, field.DbType)
                {
                    Length = field.MaxLength,
                    DefaultValue = field.DefaultValue
                };

                tableSchema.Fields!.Add(dbField);
            }
        }

        /// <summary>
        /// Adds indexes to the table schema.
        /// </summary>
        private static void AddIndexes(FormTable formTable, TableSchema tableSchema)
        {
            // Pre-check to avoid null reference
            if (tableSchema.Fields == null) return;

            // Create primary key index
            if (tableSchema.Fields!.Contains(SysFields.No))
                tableSchema.Indexes!.AddPrimaryKey(SysFields.No);

            // Create unique row identifier index
            if (tableSchema.Fields.Contains(SysFields.RowId))
                tableSchema.Indexes!.Add("rx_{0}", SysFields.RowId, true);

            // Create unique sequential number index
            if (tableSchema.Fields.Contains(SysFields.Id))
                tableSchema.Indexes!.Add("uk_{0}", SysFields.Id, true);

            // Create foreign key indexes
            if (formTable.Fields == null) { return; }
            foreach (var field in formTable.Fields.Where(f => StrFunc.IsNotEmpty(f.RelationProgId)))
            {
                // Include field name to avoid duplicates
                tableSchema.Indexes!.Add("fk_{0}_{field.FieldName}", field.FieldName, false);
            }
        }
    }
}
