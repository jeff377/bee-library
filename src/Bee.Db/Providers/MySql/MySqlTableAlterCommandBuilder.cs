using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// Generates MySQL 8.0+ <c>ALTER TABLE</c> statements for a <see cref="ITableChange"/>.
    /// Counterpart to <see cref="Sqlite.SqliteTableAlterCommandBuilder"/> and
    /// <see cref="PostgreSql.PgTableAlterCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// MySQL 8.0+ natively supports <c>ADD COLUMN</c>, <c>MODIFY COLUMN</c> (full
    /// re-definition in one statement), <c>RENAME COLUMN</c>, and index management;
    /// the rebuild fallback is only invoked for cross-family type changes flagged by
    /// <see cref="MySqlAlterCompatibilityRules"/>. Compared with PostgreSQL, MySQL's
    /// <c>MODIFY COLUMN</c> takes the full column definition in one shot, so the
    /// PG-style three-part ALTER (TYPE + NULLABILITY + DEFAULT) collapses into a
    /// single statement here.
    /// </remarks>
    public class MySqlTableAlterCommandBuilder : ITableAlterCommandBuilder
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
                    return MySqlAlterCompatibilityRules.GetKindForTypeChange(alter.OldField.DbType, alter.NewField.DbType);
                default:
                    return ChangeExecutionKind.NotSupported;
            }
        }

        /// <inheritdoc />
        public bool IsNarrowingChange(ITableChange change)
        {
            if (change is AlterFieldChange alter)
                return MySqlAlterCompatibilityRules.IsNarrowing(alter.OldField, alter.NewField);
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetStatements(string tableName, ITableChange change)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            switch (change)
            {
                case AddFieldChange add:
                    return BuildAddFieldStatements(tableName, add.Field);
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
        /// Builds the <c>ADD COLUMN</c> statement(s). A NOT NULL column whose default is a
        /// non-deterministic function (<c>UUID()</c> for Guid) is split into two: MySQL rejects
        /// <c>ADD COLUMN ... NOT NULL DEFAULT (UUID())</c> on an existing table under statement-based
        /// binlog (it evaluates the function per existing row → replication-unsafe). The column is
        /// first added with a constant empty-Guid default (safe; existing rows become "no link"),
        /// then the real default is restored as a metadata-only change so new rows still get a UUID
        /// and the column definition matches the fresh-CREATE schema (no comparer drift).
        /// </summary>
        private static string[] BuildAddFieldStatements(string tableName, DbField field)
        {
            string tbl = MySqlSchemaSyntax.QuoteName(tableName);
            string realDefault = MySqlSchemaSyntax.GetDefaultExpression(field);

            if (realDefault.Contains("UUID()", StringComparison.OrdinalIgnoreCase))
            {
                string col = MySqlSchemaSyntax.QuoteName(field.FieldName);
                string seeded = MySqlSchemaSyntax.GetColumnDefinition(field, $"'{Guid.Empty}'");
                return new[]
                {
                    $"ALTER TABLE {tbl} ADD COLUMN {seeded};",
                    $"ALTER TABLE {tbl} ALTER COLUMN {col} SET DEFAULT {realDefault};",
                };
            }
            return new[] { $"ALTER TABLE {tbl} ADD COLUMN {MySqlSchemaSyntax.GetColumnDefinition(field)};" };
        }

        /// <summary>
        /// Builds the MySQL <c>ALTER TABLE ... MODIFY COLUMN</c> statement. The full column
        /// definition (type + nullability + default) is re-emitted in one go.
        /// </summary>
        private static string BuildAlterFieldStatement(string tableName, DbField newField)
        {
            string newDef = MySqlSchemaSyntax.GetColumnDefinition(newField);
            return $"ALTER TABLE {MySqlSchemaSyntax.QuoteName(tableName)} MODIFY COLUMN {newDef};";
        }

        /// <summary>
        /// Builds the MySQL 8.0+ <c>ALTER TABLE ... RENAME COLUMN</c> statement.
        /// (MySQL 5.7 needs <c>CHANGE COLUMN</c>; we require 8.0+ per
        /// docs/plans/plan-mysql-support.md.)
        /// </summary>
        private static string BuildRenameFieldStatement(string tableName, RenameFieldChange change)
        {
            return $"ALTER TABLE {MySqlSchemaSyntax.QuoteName(tableName)} RENAME COLUMN " +
                   $"{MySqlSchemaSyntax.QuoteName(change.OldFieldName)} TO {MySqlSchemaSyntax.QuoteName(change.NewField.FieldName)};";
        }

        /// <summary>
        /// Builds the index creation statement. Primary keys go through
        /// <c>ALTER TABLE ... ADD CONSTRAINT name PRIMARY KEY</c>; everything else uses
        /// <c>CREATE [UNIQUE] INDEX</c>.
        /// </summary>
        private static string BuildAddIndexStatement(string tableName, DbTableIndex index)
        {
            string indexName = StringUtilities.Format(index.Name, tableName);
            string fields = BuildIndexFieldList(index);

            if (index.PrimaryKey)
                return $"ALTER TABLE {MySqlSchemaSyntax.QuoteName(tableName)} ADD CONSTRAINT {MySqlSchemaSyntax.QuoteName(indexName)} PRIMARY KEY ({fields});";

            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            return $"CREATE {uniqueClause}INDEX {MySqlSchemaSyntax.QuoteName(indexName)} ON {MySqlSchemaSyntax.QuoteName(tableName)} ({fields});";
        }

        /// <summary>
        /// Builds the index drop statement. Primary keys use
        /// <c>ALTER TABLE ... DROP PRIMARY KEY</c> (MySQL has no PK name to reference);
        /// regular indexes use <c>DROP INDEX name ON table</c>.
        /// </summary>
        private static string BuildDropIndexStatement(string tableName, DbTableIndex index)
        {
            if (index.PrimaryKey)
                return $"ALTER TABLE {MySqlSchemaSyntax.QuoteName(tableName)} DROP PRIMARY KEY;";

            return $"DROP INDEX {MySqlSchemaSyntax.QuoteName(index.Name)} ON {MySqlSchemaSyntax.QuoteName(tableName)};";
        }

        private static string BuildIndexFieldList(DbTableIndex index)
        {
            var sb = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(CultureInfo.InvariantCulture,
                    $"{MySqlSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }
            return sb.ToString();
        }
    }
}
