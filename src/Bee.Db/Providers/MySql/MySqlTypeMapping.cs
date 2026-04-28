using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// Converts <see cref="FieldDbType"/> to MySQL 8.0+ column type expressions.
    /// </summary>
    /// <remarks>
    /// Type choices follow docs/plans/plan-mysql-support.md:
    /// <list type="bullet">
    /// <item><see cref="FieldDbType.String"/> → <c>VARCHAR(n)</c></item>
    /// <item><see cref="FieldDbType.Text"/> → <c>LONGTEXT</c> (avoids TEXT/MEDIUMTEXT length cliffs)</item>
    /// <item><see cref="FieldDbType.Boolean"/> → <c>TINYINT(1)</c> (MySQL's BOOLEAN alias; unambiguous form)</item>
    /// <item><see cref="FieldDbType.AutoIncrement"/> → <c>BIGINT</c> (the CREATE TABLE builder emits the
    ///       <c>AUTO_INCREMENT PRIMARY KEY</c> keywords inline on the column line)</item>
    /// <item><see cref="FieldDbType.Decimal"/> → <c>DECIMAL(p,s)</c></item>
    /// <item><see cref="FieldDbType.Currency"/> → <c>DECIMAL(19,4)</c></item>
    /// <item><see cref="FieldDbType.DateTime"/> → <c>DATETIME(6)</c> (microsecond precision; avoids TIMESTAMP timezone surprises)</item>
    /// <item><see cref="FieldDbType.Guid"/> → <c>CHAR(36)</c> (readable; aligns with <c>UUID()</c> default)</item>
    /// <item><see cref="FieldDbType.Binary"/> → <c>LONGBLOB</c></item>
    /// </list>
    /// </remarks>
    internal static class MySqlTypeMapping
    {
        /// <summary>
        /// Returns the MySQL column type expression for the given field definition.
        /// </summary>
        /// <param name="field">The field definition.</param>
        public static string GetMySqlType(DbField field)
        {
            switch (field.DbType)
            {
                case FieldDbType.String:
                    return $"VARCHAR({field.Length})";
                case FieldDbType.Text:
                    return "LONGTEXT";
                case FieldDbType.Boolean:
                    return "TINYINT(1)";
                case FieldDbType.AutoIncrement:
                    return "BIGINT";
                case FieldDbType.Short:
                    return "SMALLINT";
                case FieldDbType.Integer:
                    return "INT";
                case FieldDbType.Long:
                    return "BIGINT";
                case FieldDbType.Decimal:
                    {
                        int precision = field.Precision > 0 ? field.Precision : 18;
                        int scale = field.Scale > 0 ? field.Scale : 0;
                        return $"DECIMAL({precision},{scale})";
                    }
                case FieldDbType.Currency:
                    return "DECIMAL(19,4)";
                case FieldDbType.Date:
                    return "DATE";
                case FieldDbType.DateTime:
                    return "DATETIME(6)";
                case FieldDbType.Guid:
                    return "CHAR(36)";
                case FieldDbType.Binary:
                    return "LONGBLOB";
                default:
                    throw new InvalidOperationException($"DbType={field.DbType} is not supported");
            }
        }
    }
}
