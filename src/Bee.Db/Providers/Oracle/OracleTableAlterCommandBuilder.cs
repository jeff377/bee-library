using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// Generates Oracle 19c+ <c>ALTER TABLE</c> statements for a <see cref="ITableChange"/>.
    /// Counterpart to <see cref="MySql.MySqlTableAlterCommandBuilder"/> and
    /// <see cref="PostgreSql.PgTableAlterCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Oracle 19c+ natively supports <c>ADD</c>, <c>MODIFY</c>, <c>RENAME COLUMN</c> and
    /// index management; the rebuild fallback is only invoked for cross-family type
    /// changes flagged by <see cref="Schema.AlterCompatibilityRules"/>. Differences from
    /// MySQL: column lists for <c>ADD</c> / <c>MODIFY</c> use Oracle's parenthesised form
    /// (<c>ADD ("col" type ...)</c>), index drops do not take an <c>ON tablename</c> clause,
    /// and <c>MODIFY</c> emits the full column definition in one statement (PG-style
    /// three-part ALTER is not used).
    /// </remarks>
    public class OracleTableAlterCommandBuilder : ITableAlterCommandBuilder
    {
        /// <inheritdoc />
        public ChangeExecutionKind GetExecutionKind(ITableChange change)
        {
            switch (change)
            {
                case AddFieldChange _:
                case RenameFieldChange _:
                case AddIndexChange _:
                case DropIndexChange _:
                    return ChangeExecutionKind.Alter;
                case AlterFieldChange alter:
                    {
                        var kind = AlterCompatibilityRules.GetKindForTypeChange(alter.OldField.DbType, alter.NewField.DbType);
                        // Oracle cannot MODIFY a column across the LOB boundary: VARCHAR2 to CLOB raises
                        // ORA-22858 and CLOB to VARCHAR2 raises ORA-22859. The dialect-neutral rules put
                        // both types in the same String family and would otherwise pick the in-place path.
                        if (kind == ChangeExecutionKind.Alter
                            && OracleTypeMapping.IsLobType(alter.OldField) != OracleTypeMapping.IsLobType(alter.NewField))
                            return ChangeExecutionKind.Rebuild;
                        return kind;
                    }
                default:
                    return ChangeExecutionKind.NotSupported;
            }
        }

        /// <inheritdoc />
        public bool IsNarrowingChange(ITableChange change)
        {
            if (change is AlterFieldChange alter)
                return AlterCompatibilityRules.IsNarrowing(alter.OldField, alter.NewField);
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetStatements(string tableName, ITableChange change)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            switch (change)
            {
                case AddFieldChange add:
                    return new[] { BuildAddFieldStatement(tableName, add.Field) };
                case AlterFieldChange alter:
                    {
                        // A LOB whose only modifiable clauses already match yields no statement at all.
                        string sql = BuildAlterFieldStatement(tableName, alter.OldField, alter.NewField);
                        return StringUtilities.IsEmpty(sql) ? Array.Empty<string>() : new[] { sql };
                    }
                case RenameFieldChange rename:
                    return new[] { BuildRenameFieldStatement(tableName, rename) };
                case AddIndexChange addIndex:
                    return new[] { BuildAddIndexStatement(tableName, addIndex.Index) };
                case DropIndexChange dropIndex:
                    return new[] { BuildDropIndexStatement(tableName, dropIndex.Index) };
                default:
                    throw new InvalidOperationException($"Unsupported change type: {change.GetType().Name}");
            }
        }

        /// <summary>
        /// Builds the Oracle <c>ALTER TABLE ... ADD (column-definition)</c> statement.
        /// Oracle accepts both bare and parenthesised forms for single-column ADD; the
        /// parenthesised form is used for visual consistency with MODIFY and to keep the
        /// shape stable if multi-column ADD is added later.
        /// </summary>
        private static string BuildAddFieldStatement(string tableName, DbField field)
        {
            return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} ADD ({OracleSchemaSyntax.GetColumnDefinition(field)});";
        }

        /// <summary>
        /// Builds the Oracle <c>ALTER TABLE ... MODIFY (column-definition)</c> statement. Type and
        /// default are re-emitted; the nullability clause is appended only when the effective
        /// nullability actually changes between <paramref name="oldField"/> and <paramref name="newField"/>.
        /// LOB columns take the reduced form built by <see cref="BuildLobAlterFieldStatement"/>.
        /// </summary>
        /// <remarks>
        /// Oracle rejects a redundant nullability hint — specifying <c>NOT NULL</c> on an
        /// already-NOT-NULL column raises <c>ORA-01442</c> — so MODIFY omits the clause when the
        /// nullability is unchanged (the common upgrade case where only type/length/default differ).
        /// See <c>docs/database-dialect-differences.md</c> §3.1.
        /// </remarks>
        private static string BuildAlterFieldStatement(string tableName, DbField oldField, DbField newField)
        {
            string oldNull = OracleSchemaSyntax.GetNullabilityClause(oldField);
            string newNull = OracleSchemaSyntax.GetNullabilityClause(newField);
            string nullClause = oldNull != newNull ? $" {newNull}" : string.Empty;

            if (OracleTypeMapping.IsLobType(newField))
                return BuildLobAlterFieldStatement(tableName, oldField, newField, nullClause);

            string typeDef = OracleSchemaSyntax.GetColumnTypeAndDefault(newField);
            return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} MODIFY ({typeDef}{nullClause});";
        }

        /// <summary>
        /// Builds the MODIFY statement for a column that is a LOB on both sides of the change, or an
        /// empty string when nothing modifiable differs.
        /// </summary>
        /// <remarks>
        /// Oracle rejects any MODIFY that restates a LOB column's type with <c>ORA-22859</c>, even when
        /// the type is unchanged, so the type is dropped from the statement and only DEFAULT and
        /// nullability are emitted. Both sides are LOBs here by construction: a change that crosses the
        /// LOB boundary is routed to a rebuild by <see cref="GetExecutionKind"/> and never reaches this
        /// method.
        /// </remarks>
        private static string BuildLobAlterFieldStatement(string tableName, DbField oldField, DbField newField, string nullClause)
        {
            string newFragment = OracleSchemaSyntax.GetLobColumnDefaultFragment(newField);
            string oldFragment = OracleSchemaSyntax.GetLobColumnDefaultFragment(oldField);
            // Nothing Oracle will accept on a LOB actually differs; `MODIFY ("COL")` alone is not valid syntax.
            if (string.Equals(newFragment, oldFragment, StringComparison.Ordinal) && nullClause.Length == 0)
                return string.Empty;

            return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} MODIFY ({newFragment}{nullClause});";
        }

        /// <summary>
        /// Builds the Oracle <c>ALTER TABLE ... RENAME COLUMN</c> statement (12c+).
        /// </summary>
        private static string BuildRenameFieldStatement(string tableName, RenameFieldChange change)
        {
            return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} RENAME COLUMN " +
                   $"{OracleSchemaSyntax.QuoteName(change.OldFieldName)} TO {OracleSchemaSyntax.QuoteName(change.NewField.FieldName)};";
        }

        /// <summary>
        /// Builds the index creation statement. Primary keys go through
        /// <c>ALTER TABLE ... ADD CONSTRAINT name PRIMARY KEY</c>; everything else uses
        /// <c>CREATE [UNIQUE] INDEX</c>. Oracle PK constraints reject ASC/DESC inside
        /// the column list, so PK column lists are emitted without sort direction.
        /// </summary>
        private static string BuildAddIndexStatement(string tableName, DbTableIndex index)
        {
            string indexName = StringUtilities.Format(index.Name, tableName);

            if (index.PrimaryKey)
            {
                string pkFields = BuildIndexFieldList(index, includeSortDirection: false);
                return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} ADD CONSTRAINT {OracleSchemaSyntax.QuoteName(indexName)} PRIMARY KEY ({pkFields});";
            }

            string fields = BuildIndexFieldList(index, includeSortDirection: true);
            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            return $"CREATE {uniqueClause}INDEX {OracleSchemaSyntax.QuoteName(indexName)} ON {OracleSchemaSyntax.QuoteName(tableName)} ({fields});";
        }

        /// <summary>
        /// Builds the index drop statement. Primary keys use
        /// <c>ALTER TABLE ... DROP PRIMARY KEY</c> (Oracle accepts this without naming
        /// the constraint); regular indexes use <c>DROP INDEX name</c> — note Oracle does
        /// **not** take an <c>ON tablename</c> clause, unlike MySQL.
        /// </summary>
        private static string BuildDropIndexStatement(string tableName, DbTableIndex index)
        {
            if (index.PrimaryKey)
                return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} DROP PRIMARY KEY;";

            return $"DROP INDEX {OracleSchemaSyntax.QuoteName(index.Name)};";
        }

        /// <summary>
        /// Builds the comma-separated index field list. Sort direction (ASC/DESC) is
        /// only valid on regular indexes; PK constraints reject it on Oracle.
        /// </summary>
        private static string BuildIndexFieldList(DbTableIndex index, bool includeSortDirection)
        {
            var sb = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (sb.Length > 0) sb.Append(", ");
                if (includeSortDirection)
                {
                    sb.Append(CultureInfo.InvariantCulture,
                        $"{OracleSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
                }
                else
                {
                    sb.Append(OracleSchemaSyntax.QuoteName(field.FieldName));
                }
            }
            return sb.ToString();
        }
    }
}
