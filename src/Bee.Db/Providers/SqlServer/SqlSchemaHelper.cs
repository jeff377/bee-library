using Bee.Base;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Shared SQL Server identifier, literal, and column-definition primitives used by
    /// the CREATE and ALTER schema builders.
    /// </summary>
    internal static class SqlSchemaHelper
    {
        /// <summary>
        /// Quotes a SQL Server identifier by escaping <c>]</c> as <c>]]</c> and wrapping in square brackets.
        /// </summary>
        /// <param name="identifier">The identifier to quote.</param>
        public static string QuoteName(string identifier)
        {
            return $"[{identifier.Replace("]", "]]")}]";
        }

        /// <summary>
        /// Escapes a string value for use inside an N'...' literal by doubling single quotes.
        /// </summary>
        /// <param name="value">The string value to escape.</param>
        public static string EscapeSqlString(string value)
        {
            return value.Replace("'", "''");
        }

        /// <summary>
        /// Converts a field definition to the corresponding SQL Server column type string
        /// (e.g. <c>[nvarchar](50)</c>, <c>[decimal](18,2)</c>).
        /// </summary>
        /// <param name="field">The field definition.</param>
        public static string ConvertDbType(DbField field)
        {
            switch (field.DbType)
            {
                case FieldDbType.String:
                    return $"[nvarchar]({field.Length})";
                case FieldDbType.Text:
                    return "[nvarchar](max)";
                case FieldDbType.Boolean:
                    return "[bit]";
                case FieldDbType.AutoIncrement:
                    return "[int] IDENTITY(1,1)";
                case FieldDbType.Short:
                    return "[smallint]";
                case FieldDbType.Integer:
                    return "[int]";
                case FieldDbType.Long:
                    return "[bigint]";
                case FieldDbType.Decimal:
                    {
                        int precision = field.Precision > 0 ? field.Precision : 18;
                        int scale = field.Scale > 0 ? field.Scale : 0;
                        return $"[decimal]({precision},{scale})";
                    }
                case FieldDbType.Currency:
                    return "[decimal](19,4)";
                case FieldDbType.Date:
                    return "[date]";
                case FieldDbType.DateTime:
                    return "[datetime]";
                case FieldDbType.Guid:
                    return "[uniqueidentifier]";
                case FieldDbType.Binary:
                    return "[varbinary](max)";
                default:
                    throw new InvalidOperationException($"DbType={field.DbType} is not supported");
            }
        }

        /// <summary>
        /// Gets the default value expression for a field, honoring <see cref="DbField.AllowNull"/>
        /// (nullable fields have no default). Returns an empty string when no default should be applied.
        /// </summary>
        /// <param name="field">The field definition.</param>
        public static string GetDefaultExpression(DbField field)
        {
            if (field.AllowNull)
                return string.Empty;
            string originalDefaultValue = DbFunc.GetSqlDefaultValue(field.DbType);
            switch (field.DbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return StrFunc.Format("N'{0}'", StrFunc.IsEmpty(field.DefaultValue) ? originalDefaultValue : field.DefaultValue);
                case FieldDbType.AutoIncrement:
                    return string.Empty;
                default:
                    return StrFunc.IsEmpty(field.DefaultValue) ? originalDefaultValue : field.DefaultValue;
            }
        }

        /// <summary>
        /// Generates a full column definition fragment (name + type + nullability + optional inline DEFAULT).
        /// </summary>
        /// <param name="field">The field definition.</param>
        public static string GetColumnDefinition(DbField field)
        {
            string dbType = ConvertDbType(field);
            string nullability = field.AllowNull ? "NULL" : "NOT NULL";
            string defaultExpression = GetDefaultExpression(field);
            string defaultClause = StrFunc.IsNotEmpty(defaultExpression) ? $" DEFAULT ({defaultExpression})" : string.Empty;
            return $"{QuoteName(field.FieldName)} {dbType} {nullability}{defaultClause}";
        }
    }
}
