using Bee.Definition.Database;
using Bee.Base.Data;
using Bee.Definition;
using System.Data;
using System.Globalization;
using System.Text;

namespace Bee.Db.Dml
{
    /// <summary>
    /// Generates Insert, Update, and Delete database commands based on a <see cref="TableSchema"/>;
    /// can also package them directly into a <see cref="DataTableUpdateSpec"/>.
    /// </summary>
    public class TableSchemaCommandBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TableSchemaCommandBuilder"/>.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        /// <param name="tableSchema">The table schema definition.</param>
        public TableSchemaCommandBuilder(DatabaseType databaseType, TableSchema tableSchema)
        {
            DatabaseType = databaseType;
            TableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema), "TableSchema cannot be null.");
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TableSchemaCommandBuilder"/> using the globally configured database type.
        /// </summary>
        /// <param name="tableSchema">The table schema definition.</param>
        public TableSchemaCommandBuilder(TableSchema tableSchema)
            : this(BackendInfo.DatabaseType, tableSchema)
        { }

        /// <summary>
        /// Gets the database type.
        /// </summary>
        public DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets the table schema definition.
        /// </summary>
        public TableSchema TableSchema { get; }

        /// <summary>
        /// Returns the properly quoted identifier string for the current database type.
        /// </summary>
        /// <param name="identifier">The identifier name to quote.</param>
        /// <returns>The quoted identifier.</returns>
        private string QuoteIdentifier(string identifier)
        {
            return DatabaseType.QuoteIdentifier(identifier);
        }

        /// <summary>
        /// Gets the full parameter name including its prefix character.
        /// </summary>
        /// <param name="name">The parameter name without the prefix character.</param>
        private string GetParameterName(string name)
        {
            return DatabaseType.GetParameterName(name);
        }

        /// <summary>
        /// Builds the INSERT command specification.
        /// </summary>
        public DbCommandSpec BuildInsertCommand()
        {
            var command = new DbCommandSpec();
            var buffer = new StringBuilder();
            string tableName = QuoteIdentifier(this.TableSchema.TableName);
            buffer.AppendLine(CultureInfo.InvariantCulture, $"Insert Into {tableName} ");

            // Build the INSERT column list
            buffer.Append('(');
            int count = 0;
            foreach (DbField field in this.TableSchema.Fields!)
            {
                if (field.DbType != FieldDbType.AutoIncrement)
                {
                    if (count > 0)
                        buffer.Append(", ");
                    buffer.Append(QuoteIdentifier(field.FieldName));
                    count++;
                }
            }
            buffer.AppendLine(")");

            // Build the INSERT values list
            buffer.AppendLine(" Values ");
            buffer.Append('(');
            count = 0;
            foreach (DbField field in this.TableSchema.Fields!)
            {
                if (field.DbType != FieldDbType.AutoIncrement)
                {
                    if (count > 0)
                        buffer.Append(", ");
                    buffer.Append(GetParameterName(field.FieldName));
                    command.Parameters.Add(field); // Add command parameter
                    count++;
                }
            }
            buffer.AppendLine(")");

            command.CommandText = buffer.ToString();
            return command;
        }

        /// <summary>
        /// Builds the UPDATE command specification.
        /// </summary>
        public DbCommandSpec BuildUpdateCommand()
        {
            var command = new DbCommandSpec();
            var buffer = new StringBuilder();
            string tableName = QuoteIdentifier(this.TableSchema.TableName);
            buffer.AppendLine(CultureInfo.InvariantCulture, $"Update {tableName} Set ");

            string fieldName;
            // Get the primary key field
            var keyField = this.TableSchema.Fields![SysFields.RowId];
            // Build the SET clause with field names and parameter values
            int iCount = 0;
            foreach (DbField field in this.TableSchema.Fields)
            {
                if (field != keyField && field.DbType != FieldDbType.AutoIncrement)
                {
                    fieldName = QuoteIdentifier(field.FieldName);
                    // Add command parameter
                    command.Parameters.Add(field);
                    if (iCount > 0)
                        buffer.Append(", ");
                    buffer.Append(CultureInfo.InvariantCulture, $"{fieldName}={GetParameterName(field.FieldName)}");
                    iCount++;
                }
            }
            // Add primary key condition to WHERE clause
            fieldName = QuoteIdentifier(keyField!.FieldName);
            command.Parameters.Add(keyField, System.Data.DataRowVersion.Original);
            buffer.AppendLine();
            buffer.AppendLine(CultureInfo.InvariantCulture, $"Where {fieldName}={GetParameterName(keyField.FieldName)}");

            command.CommandText = buffer.ToString();
            return command;
        }

        /// <summary>
        /// Builds the DELETE command specification.
        /// </summary>
        public DbCommandSpec BuildDeleteCommand()
        {
            var command = new DbCommandSpec();
            var buffer = new StringBuilder();
            string tableName = QuoteIdentifier(this.TableSchema.TableName);
            buffer.AppendLine(CultureInfo.InvariantCulture, $"Delete From {tableName} ");

            // Add primary key condition to WHERE clause
            var keyField = this.TableSchema.Fields![SysFields.RowId];
            string fieldName = QuoteIdentifier(keyField!.FieldName);
            command.Parameters.Add(keyField, System.Data.DataRowVersion.Original);
            buffer.AppendLine(CultureInfo.InvariantCulture, $"Where {fieldName}={GetParameterName(keyField.FieldName)}");

            command.CommandText = buffer.ToString();
            return command;
        }

        /// <summary>
        /// Builds a <see cref="DataTableUpdateSpec"/> by packaging the Insert, Update, and Delete commands together with the specified DataTable.
        /// </summary>
        /// <param name="dataTable">The DataTable to write back to the database.</param>
        /// <returns>A <see cref="DataTableUpdateSpec"/> containing the three command specifications.</returns>
        public DataTableUpdateSpec BuildUpdateSpec(DataTable dataTable)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable), "DataTable cannot be null.");

            var insertCmd = BuildInsertCommand();
            var updateCmd = BuildUpdateCommand();
            var deleteCmd = BuildDeleteCommand();

            return new DataTableUpdateSpec()
            {
                DataTable = dataTable,
                InsertCommand = insertCmd,
                UpdateCommand = updateCmd,
                DeleteCommand = deleteCmd
            };
        }
    }
}
