using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Base.Data;
using Bee.Db.Ddl;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// Builds the Oracle 19c+ rebuild script (drop tmp / create tmp / copy data / drop old /
    /// rename tmp / recreate indexes) used as the orchestrator fallback when in-place ALTER
    /// cannot apply all changes. Counterpart to
    /// <see cref="MySql.MySqlTableRebuildCommandBuilder"/> and
    /// <see cref="Sqlite.SqliteTableRebuildCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Oracle 19c+ supports most schema changes via <c>ALTER TABLE ... MODIFY</c>, so this
    /// path is rarely invoked — typically for cross-family type changes flagged by
    /// <see cref="OracleAlterCompatibilityRules"/>, <c>NUMBER</c> precision reduction with
    /// non-empty data, or IDENTITY add/remove. The pattern mirrors MySQL: temp table is
    /// created with PK only, secondary indexes are recreated against the renamed table.
    /// New fields and IDENTITY columns are excluded from the data copy so existing rows pick
    /// up their default and Oracle re-allocates IDENTITY values cleanly.
    /// </remarks>
    /// <para>
    /// Differences from MySQL: Oracle has no <c>DROP TABLE IF EXISTS</c>, so each drop is
    /// wrapped in a PL/SQL anonymous block that suppresses ORA-00942 (table or view does
    /// not exist). <c>CASCADE CONSTRAINTS</c> is appended to <c>DROP TABLE</c> so FKs
    /// referencing the table are removed atomically.
    /// </para>
    internal class OracleTableRebuildCommandBuilder : ITableRebuildCommandBuilder
    {
        /// <summary>
        /// Produces the rebuild SQL script for the given diff. Extension fields (real-only)
        /// are preserved; newly added fields are excluded from the INSERT ... SELECT data
        /// copy so existing rows get their default.
        /// </summary>
        /// <param name="diff">The schema diff; must not be a new-table diff (use
        /// <see cref="ICreateTableCommandBuilder"/> for that).</param>
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
            //    final table, avoiding the need to use ALTER INDEX ... RENAME.
            sb.AppendLine("-- Create temporary table");
            var tmpSchema = CloneWithTableName(effectiveSchema, tmpTableName);
            StripNonPrimaryKeyIndexes(tmpSchema);
            var createBuilder = new OracleCreateTableCommandBuilder();
            sb.AppendLine(createBuilder.GetCommandText(tmpSchema));

            // 3) Copy data from the original table (excluding newly-added and identity columns).
            sb.AppendLine("-- Move data");
            sb.AppendLine(BuildInsertSelectStatement(tableName, tmpTableName, effectiveSchema, addedFieldNames));

            // 4) Drop the original table — CASCADE CONSTRAINTS removes referencing FKs and
            //    drops every index / auto-named PK constraint, freeing those names for
            //    recreation in step 6.
            sb.AppendLine("-- Drop old table");
            sb.AppendLine(BuildDropIfExistsStatement(tableName));

            // 5) Rename the temp table to the original name. Oracle preserves the PK
            //    constraint and indexes through ALTER TABLE ... RENAME TO.
            sb.AppendLine("-- Rename temporary table");
            sb.AppendLine(BuildRenameTableStatement(tmpTableName, tableName));

            // 6) Recreate each non-PK index on the renamed table with its canonical name.
            sb.Append(BuildRecreateIndexStatements(tableName, effectiveSchema));

            return sb.ToString();
        }

        /// <summary>
        /// Builds the effective rebuild schema: defined table + real-only fields appended
        /// (extension field policy).
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

        /// <summary>
        /// Builds an Oracle "drop-if-exists" equivalent: a PL/SQL anonymous block that
        /// catches ORA-00942 (table or view does not exist) and rethrows anything else.
        /// </summary>
        /// <remarks>
        /// Oracle has no <c>DROP TABLE IF EXISTS</c> syntax. The conventional alternative
        /// is to query <c>ALL_TABLES</c> first, but the PL/SQL block keeps the rebuild
        /// script as a single self-contained sequence with no dictionary-lookup round trips.
        /// <c>CASCADE CONSTRAINTS</c> ensures referencing FKs are removed.
        /// </remarks>
        private static string BuildDropIfExistsStatement(string tableName)
        {
            string quoted = OracleSchemaHelper.QuoteName(tableName);
            return "BEGIN\r\n" +
                   $"  EXECUTE IMMEDIATE 'DROP TABLE {quoted} CASCADE CONSTRAINTS';\r\n" +
                   "EXCEPTION\r\n" +
                   "  WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;\r\n" +
                   "END;";
        }

        private static string BuildInsertSelectStatement(string sourceTable, string targetTable, TableSchema schema, HashSet<string> addedFieldNames)
        {
            var fieldBuilder = new StringBuilder();
            foreach (DbField field in schema.Fields!)
            {
                if (addedFieldNames.Contains(field.FieldName)) continue;
                if (field.DbType == FieldDbType.AutoIncrement) continue;
                if (fieldBuilder.Length > 0) fieldBuilder.Append(", ");
                fieldBuilder.Append(OracleSchemaHelper.QuoteName(field.FieldName));
            }
            string fields = fieldBuilder.ToString();
            return $"INSERT INTO {OracleSchemaHelper.QuoteName(targetTable)} ({fields}) \nSELECT {fields} FROM {OracleSchemaHelper.QuoteName(sourceTable)};";
        }

        private static string BuildRenameTableStatement(string oldTable, string newTable)
        {
            return $"ALTER TABLE {OracleSchemaHelper.QuoteName(oldTable)} RENAME TO {OracleSchemaHelper.QuoteName(newTable)};";
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
                    $"CREATE {uniqueClause}INDEX {OracleSchemaHelper.QuoteName(name)} ON {OracleSchemaHelper.QuoteName(tableName)} ({fields});\n");
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
                    $"{OracleSchemaHelper.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }
            return sb.ToString();
        }
    }
}
