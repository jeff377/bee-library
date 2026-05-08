using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Providers.PostgreSql
{
    /// <summary>
    /// Generates PostgreSQL <c>ALTER TABLE</c> statements for a <see cref="ITableChange"/>.
    /// PG-specific dialect: <c>ALTER COLUMN ... TYPE</c>, <c>SET / DROP NOT NULL</c>,
    /// <c>SET / DROP DEFAULT</c>, <c>RENAME COLUMN</c>; constraint-based primary keys.
    /// Counterpart to <see cref="SqlServer.SqlTableAlterCommandBuilder"/>.
    /// </summary>
    public class PgTableAlterCommandBuilder : ITableAlterCommandBuilder
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
                    return PgAlterCompatibilityRules.GetKindForTypeChange(alter.OldField.DbType, alter.NewField.DbType);
                default:
                    return ChangeExecutionKind.NotSupported;
            }
        }

        /// <inheritdoc />
        public bool IsNarrowingChange(ITableChange change)
        {
            if (change is AlterFieldChange alter)
                return PgAlterCompatibilityRules.IsNarrowing(alter.OldField, alter.NewField);
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
                    return BuildAlterFieldStatements(tableName, alter.OldField, alter.NewField);
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

        private static string BuildAddFieldStatement(string tableName, DbField field)
        {
            return $"ALTER TABLE {PgSchemaSyntax.QuoteName(tableName)} ADD COLUMN {PgSchemaSyntax.GetColumnDefinition(field)};";
        }

        /// <summary>
        /// Builds the PG <c>ALTER TABLE ... RENAME COLUMN</c> statement.
        /// </summary>
        private static string BuildRenameFieldStatement(string tableName, RenameFieldChange change)
        {
            return $"ALTER TABLE {PgSchemaSyntax.QuoteName(tableName)} RENAME COLUMN " +
                   $"{PgSchemaSyntax.QuoteName(change.OldFieldName)} TO {PgSchemaSyntax.QuoteName(change.NewField.FieldName)};";
        }

        /// <summary>
        /// Builds the (possibly multi-statement) sequence to apply a column alteration.
        /// PG splits type / nullability / default into separate ALTER COLUMN clauses.
        /// </summary>
        private static List<string> BuildAlterFieldStatements(string tableName, DbField oldField, DbField newField)
        {
            var statements = new List<string>();
            string quotedTable = PgSchemaSyntax.QuoteName(tableName);
            string quotedColumn = PgSchemaSyntax.QuoteName(newField.FieldName);

            // 1) Type change (length / precision / scale included)
            bool typeChanged = oldField.DbType != newField.DbType
                || oldField.Length != newField.Length
                || oldField.Precision != newField.Precision
                || oldField.Scale != newField.Scale;
            if (typeChanged)
            {
                string newType = PgTypeMapping.GetPgType(newField);
                statements.Add($"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} TYPE {newType};");
            }

            // 2) Nullability change
            if (oldField.AllowNull != newField.AllowNull)
            {
                statements.Add(newField.AllowNull
                    ? $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} DROP NOT NULL;"
                    : $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} SET NOT NULL;");
            }

            // 3) Default change (a nullable column has no DEFAULT in this framework, see
            // PgSchemaSyntax.GetDefaultExpression)
            bool defaultChanged = !StringUtilities.IsEquals(oldField.DefaultValue, newField.DefaultValue)
                || oldField.AllowNull != newField.AllowNull;
            if (defaultChanged)
            {
                string newDefault = PgSchemaSyntax.GetDefaultExpression(newField);
                if (StringUtilities.IsNotEmpty(newDefault))
                    statements.Add($"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} SET DEFAULT {newDefault};");
                else
                    statements.Add($"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} DROP DEFAULT;");
            }

            return statements;
        }

        private static string BuildAddIndexStatement(string tableName, DbTableIndex index)
        {
            string indexName = StringUtilities.Format(index.Name, tableName);

            if (index.PrimaryKey)
            {
                string pkFields = BuildIndexFieldList(index, includeSortDirection: false);
                return $"ALTER TABLE {PgSchemaSyntax.QuoteName(tableName)} ADD CONSTRAINT {PgSchemaSyntax.QuoteName(indexName)} PRIMARY KEY ({pkFields});";
            }

            string fields = BuildIndexFieldList(index, includeSortDirection: true);
            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            return $"CREATE {uniqueClause}INDEX {PgSchemaSyntax.QuoteName(indexName)} ON {PgSchemaSyntax.QuoteName(tableName)} ({fields});";
        }

        private static string BuildDropIndexStatement(string tableName, DbTableIndex index)
        {
            // Primary keys are constraints; everything else is an index.
            if (index.PrimaryKey)
                return $"ALTER TABLE {PgSchemaSyntax.QuoteName(tableName)} DROP CONSTRAINT {PgSchemaSyntax.QuoteName(index.Name)};";

            return $"DROP INDEX {PgSchemaSyntax.QuoteName(index.Name)};";
        }

        /// <summary>
        /// Builds the comma-separated index field list. PostgreSQL rejects ASC/DESC inside
        /// PRIMARY KEY / UNIQUE constraints; only regular indexes accept per-column sort
        /// direction.
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
                        $"{PgSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
                }
                else
                {
                    sb.Append(PgSchemaSyntax.QuoteName(field.FieldName));
                }
            }
            return sb.ToString();
        }
    }
}
