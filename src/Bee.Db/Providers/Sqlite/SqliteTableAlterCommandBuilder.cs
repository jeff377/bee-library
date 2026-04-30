using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// Generates SQLite <c>ALTER TABLE</c> statements for a <see cref="ITableChange"/>.
    /// SQLite supports <c>ADD COLUMN</c>, <c>RENAME COLUMN</c>, and index management in place;
    /// every other column-level mutation falls back to a full table rebuild via
    /// <see cref="SqliteTableRebuildCommandBuilder"/>.
    /// </summary>
    public class SqliteTableAlterCommandBuilder : ITableAlterCommandBuilder
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
                    return SqliteAlterCompatibilityRules.GetKindForTypeChange(alter.OldField.DbType, alter.NewField.DbType);
                default:
                    return ChangeExecutionKind.NotSupported;
            }
        }

        /// <inheritdoc />
        public bool IsNarrowingChange(ITableChange change)
        {
            if (change is AlterFieldChange alter)
                return SqliteAlterCompatibilityRules.IsNarrowing(alter.OldField, alter.NewField);
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetStatements(string tableName, ITableChange change)
        {
            BaseFunc.EnsureNotNullOrWhiteSpace((tableName, nameof(tableName)));
            switch (change)
            {
                case AddFieldChange add:
                    return new[] { BuildAddFieldStatement(tableName, add.Field) };
                case RenameFieldChange rename:
                    return new[] { BuildRenameFieldStatement(tableName, rename) };
                case AddIndexChange addIndex:
                    return new[] { BuildAddIndexStatement(tableName, addIndex.Index) };
                case DropIndexChange dropIndex:
                    return new[] { BuildDropIndexStatement(dropIndex.Index) };
                case AlterFieldChange _:
                    throw new InvalidOperationException(
                        "SQLite does not support ALTER COLUMN; AlterFieldChange must go through the rebuild path.");
                default:
                    throw new InvalidOperationException($"Unsupported change type: {change.GetType().Name}");
            }
        }

        private static string BuildAddFieldStatement(string tableName, DbField field)
        {
            return $"ALTER TABLE {SqliteSchemaSyntax.QuoteName(tableName)} ADD COLUMN {SqliteSchemaSyntax.GetColumnDefinition(field)};";
        }

        /// <summary>
        /// Builds the SQLite <c>ALTER TABLE ... RENAME COLUMN</c> statement (requires SQLite 3.25+).
        /// </summary>
        private static string BuildRenameFieldStatement(string tableName, RenameFieldChange change)
        {
            return $"ALTER TABLE {SqliteSchemaSyntax.QuoteName(tableName)} RENAME COLUMN " +
                   $"{SqliteSchemaSyntax.QuoteName(change.OldFieldName)} TO {SqliteSchemaSyntax.QuoteName(change.NewField.FieldName)};";
        }

        /// <summary>
        /// Builds the index creation statement. SQLite has no <c>ADD CONSTRAINT PRIMARY KEY</c>
        /// for an existing table; declaring a new primary key on a populated table requires a
        /// rebuild and so should not reach this method as an Alter change.
        /// </summary>
        private static string BuildAddIndexStatement(string tableName, TableSchemaIndex index)
        {
            if (index.PrimaryKey)
                throw new NotSupportedException(
                    "SQLite cannot add a PRIMARY KEY to an existing table via ALTER; this requires a rebuild.");

            string indexName = StrFunc.Format(index.Name, tableName);
            string fields = BuildIndexFieldList(index);
            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            return $"CREATE {uniqueClause}INDEX {SqliteSchemaSyntax.QuoteName(indexName)} ON {SqliteSchemaSyntax.QuoteName(tableName)} ({fields});";
        }

        /// <summary>
        /// Builds the index drop statement. SQLite drops PRIMARY KEY only via table rebuild,
        /// so a PK in DropIndexChange is rejected here.
        /// </summary>
        private static string BuildDropIndexStatement(TableSchemaIndex index)
        {
            if (index.PrimaryKey)
                throw new NotSupportedException(
                    "SQLite cannot drop a PRIMARY KEY from an existing table via ALTER; this requires a rebuild.");

            return $"DROP INDEX {SqliteSchemaSyntax.QuoteName(index.Name)};";
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
