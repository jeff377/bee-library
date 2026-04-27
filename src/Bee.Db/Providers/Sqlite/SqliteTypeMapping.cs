using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// Converts between <see cref="FieldDbType"/> and SQLite column type expressions.
    /// SQLite uses TYPE AFFINITY rather than strict types: the declared type string is stored
    /// verbatim and only influences how values are converted (see
    /// https://www.sqlite.org/datatype3.html). The mappings below preserve the logical type
    /// so the schema reader can round-trip them.
    /// </summary>
    /// <remarks>
    /// <see cref="FieldDbType.AutoIncrement"/> always maps to <c>INTEGER</c> here, but the
    /// CREATE TABLE builder must additionally inline it as
    /// <c>INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL</c> — SQLite refuses to attach
    /// <c>AUTOINCREMENT</c> via an external <c>CONSTRAINT ... PRIMARY KEY</c> clause.
    /// </remarks>
    internal static class SqliteTypeMapping
    {
        /// <summary>
        /// Returns the SQLite column type expression for the given field definition
        /// (e.g. <c>VARCHAR(50)</c>, <c>NUMERIC(18,2)</c>, <c>UUID</c>).
        /// </summary>
        /// <param name="field">The field definition.</param>
        public static string GetSqliteType(DbField field)
        {
            switch (field.DbType)
            {
                case FieldDbType.String:
                    return $"VARCHAR({field.Length})";
                case FieldDbType.Text:
                    return "TEXT";
                case FieldDbType.Boolean:
                    return "BOOLEAN";
                case FieldDbType.AutoIncrement:
                    return "INTEGER";
                case FieldDbType.Short:
                    return "SMALLINT";
                case FieldDbType.Integer:
                    return "INTEGER";
                case FieldDbType.Long:
                    return "BIGINT";
                case FieldDbType.Decimal:
                    {
                        int precision = field.Precision > 0 ? field.Precision : 18;
                        int scale = field.Scale > 0 ? field.Scale : 0;
                        return $"NUMERIC({precision},{scale})";
                    }
                case FieldDbType.Currency:
                    return "NUMERIC(19,4)";
                case FieldDbType.Date:
                    return "DATE";
                case FieldDbType.DateTime:
                    return "DATETIME";
                case FieldDbType.Guid:
                    return "UUID";
                case FieldDbType.Binary:
                    return "BLOB";
                default:
                    throw new InvalidOperationException($"DbType={field.DbType} is not supported");
            }
        }
    }
}
