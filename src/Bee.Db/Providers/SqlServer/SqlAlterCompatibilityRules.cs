using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Rules for determining whether a SQL Server column type change can be applied in place via ALTER COLUMN,
    /// or whether it requires a table rebuild.
    /// </summary>
    internal static class SqlAlterCompatibilityRules
    {
        /// <summary>
        /// Coarse type family used to decide ALTER vs rebuild. Same-family changes are considered ALTER-compatible;
        /// cross-family changes fall back to rebuild.
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

            // AutoIncrement status change (on either side) cannot be applied via ALTER.
            if (fromFamily == TypeFamily.AutoIncrement || toFamily == TypeFamily.AutoIncrement)
            {
                return from == to ? ChangeExecutionKind.Alter : ChangeExecutionKind.Rebuild;
            }

            return fromFamily == toFamily ? ChangeExecutionKind.Alter : ChangeExecutionKind.Rebuild;
        }

        /// <summary>
        /// Determines whether altering <paramref name="oldField"/> to <paramref name="newField"/> narrows the column.
        /// Narrowing is defined as a reduction in maximum representable range (length, precision, or numeric width).
        /// </summary>
        /// <param name="oldField">The current field definition in the database.</param>
        /// <param name="newField">The target field definition.</param>
        public static bool IsNarrowing(DbField oldField, DbField newField)
        {
            // Within String family: compare lengths (0 / Text / negative treated as MAX).
            if (IsStringLike(oldField.DbType) && IsStringLike(newField.DbType))
                return GetStringCapacity(oldField) > GetStringCapacity(newField);

            // Within Numeric family: compare rank; Decimal uses precision/scale.
            if (IsNumeric(oldField.DbType) && IsNumeric(newField.DbType))
                return IsNumericNarrowing(oldField, newField);

            // Within DateTime family: DateTime → Date narrows (loses time component).
            if (IsDateTimeLike(oldField.DbType) && IsDateTimeLike(newField.DbType))
                return oldField.DbType == FieldDbType.DateTime && newField.DbType == FieldDbType.Date;

            // Same type (or cross-family changes already handled as Rebuild) — not narrowing here.
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
        /// Returns the effective string capacity; <see cref="int.MaxValue"/> represents MAX
        /// (Text or String with non-positive Length) so that natural comparison yields the correct ordering.
        /// </summary>
        private static int GetStringCapacity(DbField field)
        {
            if (field.DbType == FieldDbType.Text) return int.MaxValue;
            // String with Length <= 0 is treated as MAX for comparison purposes.
            return field.Length <= 0 ? int.MaxValue : field.Length;
        }

        private static bool IsNumericNarrowing(DbField oldField, DbField newField)
        {
            int oldRank = GetNumericRank(oldField);
            int newRank = GetNumericRank(newField);
            if (newRank < oldRank) return true;
            // Same rank Decimal: narrowing if precision or scale reduced.
            if (oldField.DbType == FieldDbType.Decimal && newField.DbType == FieldDbType.Decimal)
            {
                if (newField.Precision < oldField.Precision) return true;
                if (newField.Scale < oldField.Scale) return true;
            }
            return false;
        }

        /// <summary>
        /// Approximate numeric rank (wider = higher). Currency maps alongside Decimal.
        /// </summary>
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
