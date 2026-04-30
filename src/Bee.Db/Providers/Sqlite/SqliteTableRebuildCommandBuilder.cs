using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Db.Ddl;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// Builds the SQLite rebuild script (drop tmp / create tmp / copy data / drop old / rename
    /// tmp / recreate indexes) used as the orchestrator fallback. SQLite has no
    /// <c>ALTER INDEX RENAME</c>, so non-PK indexes are recreated against the renamed table
    /// instead of being renamed in place. Counterpart to
    /// <see cref="PostgreSql.PgTableRebuildCommandBuilder"/>.
    /// </summary>
    internal class SqliteTableRebuildCommandBuilder : ITableRebuildCommandBuilder
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

            // 2) Create the temp table with only the primary key (no secondary indexes).
            //    Secondary indexes are recreated in step 6 with their real names against the
            //    final table — SQLite cannot rename indexes.
            sb.AppendLine("-- Create temporary table");
            var tmpSchema = CloneWithTableName(effectiveSchema, tmpTableName);
            StripNonPrimaryKeyIndexes(tmpSchema);
            var createBuilder = new SqliteCreateTableCommandBuilder();
            sb.AppendLine(createBuilder.GetCommandText(tmpSchema));

            // 3) Copy data from the original table (excluding newly-added and identity columns).
            sb.AppendLine("-- Move data");
            sb.AppendLine(BuildInsertSelectStatement(tableName, tmpTableName, effectiveSchema, addedFieldNames));

            // 4) Drop the original table — this also drops every index and the autoindex backing
            //    its primary key, freeing those names for recreation in step 6.
            sb.AppendLine("-- Drop old table");
            sb.AppendLine(BuildDropIfExistsStatement(tableName));

            // 5) Rename the temp table to the original name. SQLite carries indexes through the
            //    rename, so the temporary table's autoindex PK is preserved (its name is opaque).
            sb.AppendLine("-- Rename temporary table");
            sb.AppendLine(BuildRenameTableStatement(tmpTableName, tableName));

            // 6) Recreate each non-PK index on the renamed table with its canonical name.
            sb.Append(BuildRecreateIndexStatements(tableName, effectiveSchema));

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
            tmpSchema.DisplayName = schema.DisplayName;
            return tmpSchema;
        }

        /// <summary>
        /// Removes all non-primary-key indexes from the schema. Used when generating the
        /// temp table so secondary indexes can later be created with their real names.
        /// </summary>
        private static void StripNonPrimaryKeyIndexes(TableSchema schema)
        {
            var toRemove = schema.Indexes!.Where(i => !i.PrimaryKey).ToList();
            foreach (var index in toRemove)
                schema.Indexes!.Remove(index);
        }

        private static string BuildDropIfExistsStatement(string tableName)
        {
            return $"DROP TABLE IF EXISTS {SqliteSchemaSyntax.QuoteName(tableName)};";
        }

        private static string BuildInsertSelectStatement(string sourceTable, string targetTable, TableSchema schema, HashSet<string> addedFieldNames)
        {
            var fieldBuilder = new StringBuilder();
            foreach (DbField field in schema.Fields!)
            {
                if (addedFieldNames.Contains(field.FieldName)) continue;
                if (field.DbType == FieldDbType.AutoIncrement) continue;
                if (fieldBuilder.Length > 0) fieldBuilder.Append(", ");
                fieldBuilder.Append(SqliteSchemaSyntax.QuoteName(field.FieldName));
            }
            string fields = fieldBuilder.ToString();
            return $"INSERT INTO {SqliteSchemaSyntax.QuoteName(targetTable)} ({fields}) \nSELECT {fields} FROM {SqliteSchemaSyntax.QuoteName(sourceTable)};";
        }

        private static string BuildRenameTableStatement(string oldTable, string newTable)
        {
            return $"ALTER TABLE {SqliteSchemaSyntax.QuoteName(oldTable)} RENAME TO {SqliteSchemaSyntax.QuoteName(newTable)};";
        }

        private static string BuildRecreateIndexStatements(string tableName, TableSchema schema)
        {
            var sb = new StringBuilder();
            foreach (var index in schema.Indexes!.Where(i => !i.PrimaryKey))
            {
                string name = StrFunc.Format(index.Name, tableName);
                string fields = BuildIndexFieldList(index);
                string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
                sb.Append(CultureInfo.InvariantCulture,
                    $"CREATE {uniqueClause}INDEX {SqliteSchemaSyntax.QuoteName(name)} ON {SqliteSchemaSyntax.QuoteName(tableName)} ({fields});\n");
            }
            return sb.ToString();
        }

        private static string BuildIndexFieldList(TableSchemaIndex index)
        {
            var sb = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(CultureInfo.InvariantCulture,
                    $"{SqliteSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }
            return sb.ToString();
        }
    }
}
