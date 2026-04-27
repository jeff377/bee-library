using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// Rules for determining whether a SQLite column type change can be applied in place via
    /// <c>ALTER TABLE</c>, or whether it requires a table rebuild.
    /// </summary>
    /// <remarks>
    /// SQLite's <c>ALTER TABLE</c> only supports <c>ADD COLUMN</c>, <c>RENAME COLUMN</c>
    /// (3.25+) and <c>DROP COLUMN</c> (3.35+); changing a column's type, nullability, default,
    /// or primary-key membership is not supported. Therefore <em>every</em> alter-field change
    /// resolves to <see cref="ChangeExecutionKind.Rebuild"/> on this provider — the
    /// <c>IsNarrowing</c> hint follows the same shape as the SQL Server / PostgreSQL rules
    /// for caller observability, even though the actual upgrade always falls back to rebuild.
    /// </remarks>
    internal static class SqliteAlterCompatibilityRules
    {
        /// <summary>
        /// Returns <see cref="ChangeExecutionKind.Rebuild"/> for any non-AutoIncrement type
        /// change and <see cref="ChangeExecutionKind.NotSupported"/> for unknown types.
        /// </summary>
        /// <param name="from">The source type (current DB column type).</param>
        /// <param name="to">The target type (defined column type).</param>
        public static ChangeExecutionKind GetKindForTypeChange(FieldDbType from, FieldDbType to)
        {
            if (from == FieldDbType.Unknown || to == FieldDbType.Unknown)
                return ChangeExecutionKind.NotSupported;
            return ChangeExecutionKind.Rebuild;
        }

        /// <summary>
        /// Determines whether altering <paramref name="oldField"/> to <paramref name="newField"/>
        /// narrows the column. Mirrors the SQL Server / PostgreSQL implementations so that
        /// callers see consistent narrowing semantics across providers.
        /// </summary>
        /// <param name="oldField">The current field definition in the database.</param>
        /// <param name="newField">The target field definition.</param>
        public static bool IsNarrowing(DbField oldField, DbField newField)
        {
            if (IsStringLike(oldField.DbType) && IsStringLike(newField.DbType))
                return GetStringCapacity(oldField) > GetStringCapacity(newField);

            if (IsNumeric(oldField.DbType) && IsNumeric(newField.DbType))
                return IsNumericNarrowing(oldField, newField);

            if (IsDateTimeLike(oldField.DbType) && IsDateTimeLike(newField.DbType))
                return oldField.DbType == FieldDbType.DateTime && newField.DbType == FieldDbType.Date;

            return false;
        }

        private static bool IsStringLike(FieldDbType type) =>
            type == FieldDbType.String || type == FieldDbType.Text;

        private static bool IsNumeric(FieldDbType type) =>
            type == FieldDbType.Short || type == FieldDbType.Integer || type == FieldDbType.Long
            || type == FieldDbType.Decimal || type == FieldDbType.Currency;

        private static bool IsDateTimeLike(FieldDbType type) =>
            type == FieldDbType.Date || type == FieldDbType.DateTime;

        /// <summary>
        /// Returns the effective string capacity; <see cref="int.MaxValue"/> represents
        /// SQLite's <c>TEXT</c> or unsized <c>VARCHAR</c>.
        /// </summary>
        private static int GetStringCapacity(DbField field)
        {
            if (field.DbType == FieldDbType.Text) return int.MaxValue;
            return field.Length <= 0 ? int.MaxValue : field.Length;
        }

        private static bool IsNumericNarrowing(DbField oldField, DbField newField)
        {
            int oldRank = GetNumericRank(oldField);
            int newRank = GetNumericRank(newField);
            if (newRank < oldRank) return true;
            if (oldField.DbType == FieldDbType.Decimal && newField.DbType == FieldDbType.Decimal)
            {
                if (newField.Precision < oldField.Precision) return true;
                if (newField.Scale < oldField.Scale) return true;
            }
            return false;
        }

        private static int GetNumericRank(DbField field)
        {
            switch (field.DbType)
            {
                case FieldDbType.Short: return 1;
                case FieldDbType.Integer: return 2;
                case FieldDbType.Long: return 3;
                case FieldDbType.Currency: return 4;
                case FieldDbType.Decimal: return 4;
                default: return 0;
            }
        }
    }
}
