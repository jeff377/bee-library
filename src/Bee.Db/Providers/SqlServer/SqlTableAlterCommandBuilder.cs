using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Generates SQL Server ALTER statements for a <see cref="ITableChange"/>.
    /// Supports <see cref="AddFieldChange"/>, <see cref="AlterFieldChange"/>,
    /// <see cref="AddIndexChange"/>, and <see cref="DropIndexChange"/> (including primary keys).
    /// </summary>
    public class SqlTableAlterCommandBuilder : ITableAlterCommandBuilder
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
                    return SqlAlterCompatibilityRules.GetKindForTypeChange(alter.OldField.DbType, alter.NewField.DbType);
                default:
                    return ChangeExecutionKind.NotSupported;
            }
        }

        /// <inheritdoc />
        public bool IsNarrowingChange(ITableChange change)
        {
            if (change is AlterFieldChange alter)
                return SqlAlterCompatibilityRules.IsNarrowing(alter.OldField, alter.NewField);
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
            return $"ALTER TABLE {SqlSchemaHelper.QuoteName(tableName)} ADD {SqlSchemaHelper.GetColumnDefinition(field)};";
        }

        /// <summary>
        /// Builds the <c>sp_rename</c> invocation for a column rename. The source is identified as
        /// <c>schema.table.column</c>; the target is the new column name (bare identifier).
        /// </summary>
        private static string BuildRenameFieldStatement(string tableName, RenameFieldChange change)
        {
            string sourceObject = $"{tableName}.{change.OldFieldName}";
            string sourceLiteral = SqlSchemaHelper.EscapeSqlString(sourceObject);
            string targetLiteral = SqlSchemaHelper.EscapeSqlString(change.NewField.FieldName);
            return $"EXEC sp_rename N'{sourceLiteral}', N'{targetLiteral}', N'COLUMN';";
        }

        private static List<string> BuildAlterFieldStatements(string tableName, DbField oldField, DbField newField)
        {
            var statements = new List<string>();
            bool columnSpecChanged = HasColumnSpecChanged(oldField, newField);
            bool defaultChanged = HasDefaultChanged(oldField, newField);

            // Drop the existing DEFAULT constraint if either the default value or the nullability/type changed
            // (SQL Server requires dropping the constraint before ALTER COLUMN that changes type).
            if (defaultChanged || columnSpecChanged)
                statements.Add(BuildDropDefaultConstraintStatement(tableName, newField.FieldName));

            if (columnSpecChanged)
                statements.Add(BuildAlterColumnStatement(tableName, newField));

            // Re-add default when the new field has a default expression (non-nullable fields always do).
            string newDefaultExpression = SqlSchemaHelper.GetDefaultExpression(newField);
            if ((defaultChanged || columnSpecChanged) && StrFunc.IsNotEmpty(newDefaultExpression))
                statements.Add(BuildAddDefaultConstraintStatement(tableName, newField, newDefaultExpression));

            return statements;
        }

        private static bool HasColumnSpecChanged(DbField oldField, DbField newField)
        {
            return oldField.DbType != newField.DbType
                || oldField.Length != newField.Length
                || oldField.Precision != newField.Precision
                || oldField.Scale != newField.Scale
                || oldField.AllowNull != newField.AllowNull;
        }

        private static bool HasDefaultChanged(DbField oldField, DbField newField)
        {
            return !StrFunc.IsEquals(oldField.DefaultValue, newField.DefaultValue)
                || oldField.AllowNull != newField.AllowNull;
        }

        private static string BuildAlterColumnStatement(string tableName, DbField newField)
        {
            string dbType = SqlSchemaHelper.ConvertDbType(newField);
            string nullability = newField.AllowNull ? "NULL" : "NOT NULL";
            return $"ALTER TABLE {SqlSchemaHelper.QuoteName(tableName)} ALTER COLUMN {SqlSchemaHelper.QuoteName(newField.FieldName)} {dbType} {nullability};";
        }

        /// <summary>
        /// Emits a T-SQL batch that drops the existing DEFAULT constraint for the column, if one exists.
        /// Uses a dynamic lookup against sys.default_constraints since default constraints created inline by
        /// CREATE TABLE have auto-generated names.
        /// </summary>
        private static string BuildDropDefaultConstraintStatement(string tableName, string columnName)
        {
            string tableLiteral = SqlSchemaHelper.EscapeSqlString(tableName);
            string columnLiteral = SqlSchemaHelper.EscapeSqlString(columnName);
            string quotedTable = SqlSchemaHelper.QuoteName(tableName);
            var sb = new StringBuilder();
            sb.AppendLine("DECLARE @df_name NVARCHAR(256);");
            sb.AppendLine("SELECT @df_name = dc.name");
            sb.AppendLine("FROM sys.default_constraints dc");
            sb.AppendLine("INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"WHERE dc.parent_object_id = OBJECT_ID(N'{tableLiteral}') AND c.name = N'{columnLiteral}';");
            sb.AppendLine("IF @df_name IS NOT NULL");
            sb.Append(CultureInfo.InvariantCulture,
                $"    EXEC('ALTER TABLE {quotedTable} DROP CONSTRAINT [' + @df_name + ']');");
            return sb.ToString();
        }

        private static string BuildAddDefaultConstraintStatement(string tableName, DbField newField, string defaultExpression)
        {
            string constraintName = $"DF_{tableName}_{newField.FieldName}";
            return $"ALTER TABLE {SqlSchemaHelper.QuoteName(tableName)} ADD CONSTRAINT {SqlSchemaHelper.QuoteName(constraintName)} DEFAULT ({defaultExpression}) FOR {SqlSchemaHelper.QuoteName(newField.FieldName)};";
        }

        private static string BuildAddIndexStatement(string tableName, TableSchemaIndex index)
        {
            string indexName = StrFunc.Format(index.Name, tableName);
            string fields = BuildIndexFieldList(index);

            if (index.PrimaryKey)
                return $"ALTER TABLE {SqlSchemaHelper.QuoteName(tableName)} ADD CONSTRAINT {SqlSchemaHelper.QuoteName(indexName)} PRIMARY KEY ({fields});";

            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            return $"CREATE {uniqueClause}INDEX {SqlSchemaHelper.QuoteName(indexName)} ON {SqlSchemaHelper.QuoteName(tableName)} ({fields});";
        }

        private static string BuildDropIndexStatement(string tableName, TableSchemaIndex index)
        {
            // Primary key must be dropped as a constraint.
            if (index.PrimaryKey)
                return $"ALTER TABLE {SqlSchemaHelper.QuoteName(tableName)} DROP CONSTRAINT {SqlSchemaHelper.QuoteName(index.Name)};";

            return $"DROP INDEX {SqlSchemaHelper.QuoteName(index.Name)} ON {SqlSchemaHelper.QuoteName(tableName)};";
        }

        private static string BuildIndexFieldList(TableSchemaIndex index)
        {
            var sb = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(CultureInfo.InvariantCulture,
                    $"{SqlSchemaHelper.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }
            return sb.ToString();
        }
    }
}
