using System.Globalization;
using System.Text;
using Bee.Base;
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
            BaseFunc.EnsureNotNullOrWhiteSpace((tableName, nameof(tableName)));
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
            return $"ALTER TABLE {PgSchemaHelper.QuoteName(tableName)} ADD COLUMN {PgSchemaHelper.GetColumnDefinition(field)};";
        }

        /// <summary>
        /// Builds the PG <c>ALTER TABLE ... RENAME COLUMN</c> statement.
        /// </summary>
        private static string BuildRenameFieldStatement(string tableName, RenameFieldChange change)
        {
            return $"ALTER TABLE {PgSchemaHelper.QuoteName(tableName)} RENAME COLUMN " +
                   $"{PgSchemaHelper.QuoteName(change.OldFieldName)} TO {PgSchemaHelper.QuoteName(change.NewField.FieldName)};";
        }

        /// <summary>
        /// Builds the (possibly multi-statement) sequence to apply a column alteration.
        /// PG splits type / nullability / default into separate ALTER COLUMN clauses.
        /// </summary>
        private static List<string> BuildAlterFieldStatements(string tableName, DbField oldField, DbField newField)
        {
            var statements = new List<string>();
            string quotedTable = PgSchemaHelper.QuoteName(tableName);
            string quotedColumn = PgSchemaHelper.QuoteName(newField.FieldName);

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
            // PgSchemaHelper.GetDefaultExpression)
            bool defaultChanged = !StrFunc.IsEquals(oldField.DefaultValue, newField.DefaultValue)
                || oldField.AllowNull != newField.AllowNull;
            if (defaultChanged)
            {
                string newDefault = PgSchemaHelper.GetDefaultExpression(newField);
                if (StrFunc.IsNotEmpty(newDefault))
                    statements.Add($"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} SET DEFAULT {newDefault};");
                else
                    statements.Add($"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} DROP DEFAULT;");
            }

            return statements;
        }

        private static string BuildAddIndexStatement(string tableName, TableSchemaIndex index)
        {
            string indexName = StrFunc.Format(index.Name, tableName);
            string fields = BuildIndexFieldList(index);

            if (index.PrimaryKey)
                return $"ALTER TABLE {PgSchemaHelper.QuoteName(tableName)} ADD CONSTRAINT {PgSchemaHelper.QuoteName(indexName)} PRIMARY KEY ({fields});";

            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            return $"CREATE {uniqueClause}INDEX {PgSchemaHelper.QuoteName(indexName)} ON {PgSchemaHelper.QuoteName(tableName)} ({fields});";
        }

        private static string BuildDropIndexStatement(string tableName, TableSchemaIndex index)
        {
            // Primary keys are constraints; everything else is an index.
            if (index.PrimaryKey)
                return $"ALTER TABLE {PgSchemaHelper.QuoteName(tableName)} DROP CONSTRAINT {PgSchemaHelper.QuoteName(index.Name)};";

            return $"DROP INDEX {PgSchemaHelper.QuoteName(index.Name)};";
        }

        private static string BuildIndexFieldList(TableSchemaIndex index)
        {
            var sb = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(CultureInfo.InvariantCulture,
                    $"{PgSchemaHelper.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }
            return sb.ToString();
        }
    }
}
