using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Db.Ddl;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Builds the SQL Server rebuild script (drop tmp / create tmp / copy data / drop old / rename tmp)
    /// used as the orchestrator fallback when ALTER cannot apply all changes.
    /// </summary>
    internal class SqlTableRebuildCommandBuilder : ITableRebuildCommandBuilder
    {
        /// <summary>
        /// Produces the rebuild SQL script for the given diff. Extension fields (real-only) are preserved;
        /// newly added fields are excluded from the INSERT ... SELECT data copy so existing rows get their default.
        /// </summary>
        /// <param name="diff">The schema diff; must not be a new-table diff (use <see cref="ICreateTableCommandBuilder"/> for that).</param>
        public string GetCommandText(TableSchemaDiff diff)
        {
            if (diff.IsNewTable)
                throw new InvalidOperationException("Rebuild is not applicable for a new table; use CREATE TABLE instead.");

            string tableName = diff.DefineTable.TableName;
            string tmpTableName = $"tmp_{tableName}";

            var effectiveSchema = BuildEffectiveSchema(diff);
            var addedFieldNames = diff.Changes.OfType<AddFieldChange>()
                .Select(c => c.Field.FieldName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            sb.AppendLine(CultureInfo.InvariantCulture, $"-- Rebuild table {tableName}");

            // 1) Drop any leftover temp table from a prior failed run.
            sb.AppendLine("-- Drop temporary table");
            sb.AppendLine(BuildDropIfExistsStatement(tmpTableName));

            // 2) Create the temp table using the CREATE builder (with the tmp name).
            sb.AppendLine("-- Create temporary table");
            var tmpSchema = CloneWithTableName(effectiveSchema, tmpTableName);
            var createBuilder = new SqlCreateTableCommandBuilder();
            sb.AppendLine(createBuilder.GetCommandText(tmpSchema));

            // 3) Copy data from the original table (excluding newly-added and identity columns).
            sb.AppendLine("-- Move data");
            sb.AppendLine(BuildInsertSelectStatement(tableName, tmpTableName, effectiveSchema, addedFieldNames));

            // 4) Drop the original table.
            sb.AppendLine("-- Drop old table");
            sb.AppendLine(BuildDropIfExistsStatement(tableName));

            // 5) Rename indexes, then rename the table.
            sb.AppendLine("-- Rename temporary table");
            sb.Append(BuildRenameStatements(tmpTableName, tableName, effectiveSchema));

            return sb.ToString();
        }

        /// <summary>
        /// Builds the effective rebuild schema: defined table + real-only fields appended (extension field policy).
        /// </summary>
        private static TableSchema BuildEffectiveSchema(TableSchemaDiff diff)
        {
            var cloned = diff.DefineTable.Clone();
            if (diff.RealTable != null)
            {
                foreach (var realField in diff.RealTable.Fields!.Where(f => !cloned.Fields!.Contains(f.FieldName)))
                    cloned.Fields!.Add(realField.Clone());
            }
            return cloned;
        }

        private static TableSchema CloneWithTableName(TableSchema schema, string tableName)
        {
            var tmpSchema = schema.Clone();
            tmpSchema.TableName = tableName;
            // DisplayName is carried over so the tmp table gets the same extended property;
            // after rename the properties remain attached to the (renamed) object.
            tmpSchema.DisplayName = schema.DisplayName;
            return tmpSchema;
        }

        private static string BuildDropIfExistsStatement(string tableName)
        {
            string escaped = SqlSchemaSyntax.EscapeSqlString(tableName);
            string quoted = SqlSchemaSyntax.QuoteName(tableName);
            return $"IF (SELECT COUNT(*) From sys.tables WHERE name=N'{escaped}')>0\n  DROP TABLE {quoted};";
        }

        private static string BuildInsertSelectStatement(string sourceTable, string targetTable, TableSchema schema, HashSet<string> addedFieldNames)
        {
            var fieldBuilder = new StringBuilder();
            foreach (DbField field in schema.Fields!)
            {
                if (addedFieldNames.Contains(field.FieldName)) continue;
                if (field.DbType == FieldDbType.AutoIncrement) continue;
                if (fieldBuilder.Length > 0) fieldBuilder.Append(", ");
                fieldBuilder.Append(SqlSchemaSyntax.QuoteName(field.FieldName));
            }
            string fields = fieldBuilder.ToString();
            return $"INSERT INTO {SqlSchemaSyntax.QuoteName(targetTable)} ({fields}) \nSELECT {fields} FROM {SqlSchemaSyntax.QuoteName(sourceTable)};";
        }

        private static string BuildRenameStatements(string oldTable, string newTable, TableSchema schema)
        {
            var sb = new StringBuilder();
            // Rename indexes (including PK) so they follow the table.
            foreach (var indexName in schema.Indexes!.Select(index => index.Name))
            {
                string oldIndexName = StringUtilities.Format(indexName, oldTable);
                string newIndexName = StringUtilities.Format(indexName, newTable);
                string oldQualified = SqlSchemaSyntax.EscapeSqlString($"dbo.{oldTable}.{oldIndexName}");
                string escapedNew = SqlSchemaSyntax.EscapeSqlString(newIndexName);
                sb.Append(CultureInfo.InvariantCulture, $"EXEC sp_rename N'{oldQualified}', N'{escapedNew}', N'INDEX';\n");
            }
            // Rename the table.
            string oldEscaped = SqlSchemaSyntax.EscapeSqlString(oldTable);
            string newEscaped = SqlSchemaSyntax.EscapeSqlString(newTable);
            sb.Append(CultureInfo.InvariantCulture, $"EXEC sp_rename N'{oldEscaped}', N'{newEscaped}';\n");
            return sb.ToString();
        }
    }
}
