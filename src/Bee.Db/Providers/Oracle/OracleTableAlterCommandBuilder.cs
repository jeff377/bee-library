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
    /// changes flagged by <see cref="OracleAlterCompatibilityRules"/>. Differences from
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
                    return OracleAlterCompatibilityRules.GetKindForTypeChange(alter.OldField.DbType, alter.NewField.DbType);
                default:
                    return ChangeExecutionKind.NotSupported;
            }
        }

        /// <inheritdoc />
        public bool IsNarrowingChange(ITableChange change)
        {
            if (change is AlterFieldChange alter)
                return OracleAlterCompatibilityRules.IsNarrowing(alter.OldField, alter.NewField);
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
                    return new[] { BuildAlterFieldStatement(tableName, alter.NewField) };
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
        /// Builds the Oracle <c>ALTER TABLE ... MODIFY (column-definition)</c> statement.
        /// The full column definition (type + default + nullability) is re-emitted in one go.
        /// </summary>
        /// <remarks>
        /// Oracle differs from MySQL in that MODIFY rejects redundant nullability hints
        /// (e.g. specifying <c>NOT NULL</c> on an already-NOT-NULL column raises ORA-01442);
        /// the full re-definition output here may need diff-based trimming when integration
        /// tests cover the upgrade path. See docs/plans/plan-oracle-support.md.
        /// </remarks>
        private static string BuildAlterFieldStatement(string tableName, DbField newField)
        {
            string newDef = OracleSchemaSyntax.GetColumnDefinition(newField);
            return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} MODIFY ({newDef});";
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
        private static string BuildAddIndexStatement(string tableName, TableSchemaIndex index)
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
        private static string BuildDropIndexStatement(string tableName, TableSchemaIndex index)
        {
            if (index.PrimaryKey)
                return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} DROP PRIMARY KEY;";

            return $"DROP INDEX {OracleSchemaSyntax.QuoteName(index.Name)};";
        }

        /// <summary>
        /// Builds the comma-separated index field list. Sort direction (ASC/DESC) is
        /// only valid on regular indexes; PK constraints reject it on Oracle.
        /// </summary>
        private static string BuildIndexFieldList(TableSchemaIndex index, bool includeSortDirection)
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
