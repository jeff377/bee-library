using System.Data;
using System.Text;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Database;

namespace Bee.Db.Dml
{
    /// <summary>
    /// Builds UPDATE command specifications from a form schema.
    /// </summary>
    public class UpdateCommandBuilder
    {
        private readonly FormSchema _formSchema;
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new instance of <see cref="UpdateCommandBuilder"/>.
        /// </summary>
        /// <param name="formSchema">The form schema definition.</param>
        /// <param name="databaseType">The database type.</param>
        public UpdateCommandBuilder(FormSchema formSchema, DatabaseType databaseType)
        {
            _formSchema = formSchema ?? throw new ArgumentNullException(nameof(formSchema));
            _databaseType = databaseType;
        }

        /// <summary>
        /// Builds an UPDATE <see cref="DbCommandSpec"/> for the specified table from a <see cref="DataRow"/>.
        /// The row must be in the <see cref="DataRowState.Modified"/> state and have at least one
        /// changed writable column; otherwise an <see cref="InvalidOperationException"/> is thrown,
        /// because no valid SQL exists for the caller to issue.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="row">The modified data row.</param>
        public DbCommandSpec Build(string tableName, DataRow row)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName cannot be null or whitespace.", nameof(tableName));
            ArgumentNullException.ThrowIfNull(row);
            if (row.RowState != DataRowState.Modified)
                throw new InvalidOperationException(
                    $"BuildUpdate requires a row in Modified state; received {row.RowState}.");

            if (!_formSchema.Tables!.Contains(tableName))
                throw new InvalidOperationException($"Cannot find the specified table: {tableName}");
            var formTable = _formSchema.Tables[tableName];

            if (!row.Table.Columns.Contains(SysFields.RowId))
                throw new InvalidOperationException(
                    $"DataRow for table '{tableName}' is missing the required '{SysFields.RowId}' primary key column.");

            var setClauses = new List<string>();
            var values = new List<object>();

            foreach (var field in formTable.Fields!)
            {
                if (!IsWritable(field)) continue;
                if (field.FieldName == SysFields.RowId) continue;
                if (!row.Table.Columns.Contains(field.FieldName)) continue;

                var original = row[field.FieldName, DataRowVersion.Original];
                var current = row[field.FieldName, DataRowVersion.Current];
                if (Equals(original, current)) continue;

                string quotedCol = DbFunc.QuoteIdentifier(_databaseType, field.FieldName);
                setClauses.Add($"{quotedCol} = {{{values.Count}}}");
                values.Add(current);
            }

            if (setClauses.Count == 0)
                throw new InvalidOperationException(
                    $"No column changes detected on row for table '{tableName}'; UPDATE would be empty.");

            string dbTableName = !string.IsNullOrWhiteSpace(formTable.DbTableName)
                ? formTable.DbTableName
                : formTable.TableName;
            string quotedTable = DbFunc.QuoteIdentifier(_databaseType, dbTableName);
            string quotedRowId = DbFunc.QuoteIdentifier(_databaseType, SysFields.RowId);

            values.Add(row[SysFields.RowId, DataRowVersion.Original]);

            var sql = new StringBuilder();
            sql.Append("UPDATE ").Append(quotedTable).Append(" SET ");
            sql.Append(string.Join(", ", setClauses));
            sql.Append(" WHERE ").Append(quotedRowId).Append(" = {").Append(values.Count - 1).Append('}');

            return new DbCommandSpec(DbCommandKind.NonQuery, sql.ToString(), values.ToArray());
        }

        private static bool IsWritable(FormField field)
        {
            return field.Type == FieldType.DbField && field.DbType != FieldDbType.AutoIncrement;
        }
    }
}
