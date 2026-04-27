using System.Data;
using System.Text;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Database;

namespace Bee.Db.Dml
{
    /// <summary>
    /// Builds INSERT command specifications from a form schema.
    /// </summary>
    public class InsertCommandBuilder
    {
        private readonly FormSchema _formSchema;
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new instance of <see cref="InsertCommandBuilder"/>.
        /// </summary>
        /// <param name="formSchema">The form schema definition.</param>
        /// <param name="databaseType">The database type.</param>
        public InsertCommandBuilder(FormSchema formSchema, DatabaseType databaseType)
        {
            _formSchema = formSchema ?? throw new ArgumentNullException(nameof(formSchema));
            _databaseType = databaseType;
        }

        /// <summary>
        /// Builds an INSERT <see cref="DbCommandSpec"/> for the specified table from a <see cref="DataRow"/>.
        /// Only fields that exist in <paramref name="row"/> with non-DBNull values are written; DBNull
        /// and missing columns fall through to the database default.
        /// Relation fields and auto-increment columns are always excluded.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="row">The data row to insert.</param>
        public DbCommandSpec Build(string tableName, DataRow row)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName cannot be null or whitespace.", nameof(tableName));
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            if (!_formSchema.Tables!.Contains(tableName))
                throw new InvalidOperationException($"Cannot find the specified table: {tableName}");
            var formTable = _formSchema.Tables[tableName];

            var columns = new List<string>();
            var values = new List<object>();

            foreach (var field in formTable.Fields!)
            {
                if (!IsWritable(field)) continue;
                if (!row.Table.Columns.Contains(field.FieldName)) continue;

                var value = row[field.FieldName];
                if (value == DBNull.Value) continue;

                columns.Add(DbFunc.QuoteIdentifier(_databaseType, field.FieldName));
                values.Add(value);
            }

            if (columns.Count == 0)
                throw new InvalidOperationException(
                    $"No writable fields found in row for table '{tableName}'. INSERT would be empty.");

            string dbTableName = !string.IsNullOrWhiteSpace(formTable.DbTableName)
                ? formTable.DbTableName
                : formTable.TableName;
            string quotedTable = DbFunc.QuoteIdentifier(_databaseType, dbTableName);

            var sql = new StringBuilder();
            sql.Append("INSERT INTO ").Append(quotedTable).Append(" (");
            sql.Append(string.Join(", ", columns));
            sql.Append(") VALUES (");
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) sql.Append(", ");
                sql.Append('{').Append(i).Append('}');
            }
            sql.Append(')');

            return new DbCommandSpec(DbCommandKind.NonQuery, sql.ToString(), values.ToArray());
        }

        private static bool IsWritable(FormField field)
        {
            return field.Type == FieldType.DbField && field.DbType != FieldDbType.AutoIncrement;
        }
    }
}
