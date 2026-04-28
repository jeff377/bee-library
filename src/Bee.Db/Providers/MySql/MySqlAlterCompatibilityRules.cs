using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// Rules for determining whether a MySQL column type change can be applied in place via
    /// <c>ALTER TABLE ... MODIFY COLUMN</c>, or whether it requires a table rebuild.
    /// Counterpart to <see cref="PostgreSql.PgAlterCompatibilityRules"/> and
    /// <see cref="SqlServer.SqlAlterCompatibilityRules"/>; MySQL 8.0+ supports most
    /// in-place column changes, so the rule shape mirrors PG (same-family → ALTER,
    /// cross-family → Rebuild).
    /// </summary>
    internal static class MySqlAlterCompatibilityRules
    {
        /// <summary>
        /// Coarse type family used to decide ALTER vs rebuild. Same-family changes are considered
        /// ALTER-compatible; cross-family changes fall back to rebuild.
        /// </summary>
        private enum TypeFamily
        {
            String,
            Numeric,
            Boolean,
            DateTime,
            Guid,
            Binary,
            AutoIncrement,
            Unknown,
        }

        /// <summary>
        /// Returns the execution kind for changing a column from <paramref name="from"/> to <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The source type (current DB column type).</param>
        /// <param name="to">The target type (defined column type).</param>
        public static ChangeExecutionKind GetKindForTypeChange(FieldDbType from, FieldDbType to)
        {
            var fromFamily = GetFamily(from);
            var toFamily = GetFamily(to);

            if (fromFamily == TypeFamily.Unknown || toFamily == TypeFamily.Unknown)
                return ChangeExecutionKind.NotSupported;

            // AutoIncrement status change (on either side) cannot be applied via in-place ALTER:
            // MySQL requires the AUTO_INCREMENT attribute be added/removed via column re-definition,
            // and switching across types around it is safer to rebuild.
            if (fromFamily == TypeFamily.AutoIncrement || toFamily == TypeFamily.AutoIncrement)
            {
                return from == to ? ChangeExecutionKind.Alter : ChangeExecutionKind.Rebuild;
            }

            return fromFamily == toFamily ? ChangeExecutionKind.Alter : ChangeExecutionKind.Rebuild;
        }

        /// <summary>
        /// Determines whether altering <paramref name="oldField"/> to <paramref name="newField"/> narrows the column.
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

        private static TypeFamily GetFamily(FieldDbType type)
        {
            switch (type)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return TypeFamily.String;
                case FieldDbType.Short:
                case FieldDbType.Integer:
                case FieldDbType.Long:
                case FieldDbType.Decimal:
                case FieldDbType.Currency:
                    return TypeFamily.Numeric;
                case FieldDbType.Boolean:
                    return TypeFamily.Boolean;
                case FieldDbType.Date:
                case FieldDbType.DateTime:
                    return TypeFamily.DateTime;
                case FieldDbType.Guid:
                    return TypeFamily.Guid;
                case FieldDbType.Binary:
                    return TypeFamily.Binary;
                case FieldDbType.AutoIncrement:
                    return TypeFamily.AutoIncrement;
                default:
                    return TypeFamily.Unknown;
            }
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
        /// MySQL's <c>LONGTEXT</c> or unsized text storage.
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
