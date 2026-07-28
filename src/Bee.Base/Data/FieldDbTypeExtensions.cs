namespace Bee.Base.Data
{
    /// <summary>
    /// Extension methods for <see cref="FieldDbType"/>.
    /// </summary>
    public static class FieldDbTypeExtensions
    {
        /// <summary>
        /// Returns the default value for the specified field database type.
        /// </summary>
        /// <param name="dbType">The field database type.</param>
        public static object GetDefaultValue(this FieldDbType dbType)
        {
            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return string.Empty;
                case FieldDbType.Boolean:
                    return false;
                case FieldDbType.Integer:
                case FieldDbType.Decimal:
                case FieldDbType.Currency:
                    return 0;
                // No user context reaches here — `AddColumn` and `DbParameterSpecCollection` both
                // call this to satisfy a NOT NULL column that was given no value. That is a data
                // integrity backstop, not a value the user reads, so it uses UTC rather than a user
                // zone (ADR-032 D12). User-facing new-row defaults come from `FormRowDefaults`,
                // which is given the session's zone.
                case FieldDbType.Date:
                    return DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                case FieldDbType.DateTime:
                    return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                case FieldDbType.Guid:
                    return Guid.Empty;
                default:
                    return DBNull.Value;
            }
        }

        /// <summary>
        /// Converts the specified value to the CLR type that corresponds to this
        /// <see cref="FieldDbType"/>. Unmapped types return the value unchanged.
        /// </summary>
        /// <param name="dbType">The field database type.</param>
        /// <param name="value">The input value.</param>
        public static object ToFieldValue(this FieldDbType dbType, object value)
        {
            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return ValueUtilities.CStr(value);
                case FieldDbType.Boolean:
                    return ValueUtilities.CBool(value);
                case FieldDbType.Integer:
                    return ValueUtilities.CInt(value);
                case FieldDbType.Decimal:
                case FieldDbType.Currency:
                    return ValueUtilities.CDecimal(value);
                case FieldDbType.Date:
                    // Deliberately not `CDateOnly`, which returns `DateOnly`. A calendar-day column is a
                    // `DateTime` column carrying a marker, and a `DataColumn` of that type rejects a
                    // `DateOnly` value outright — `DateOnly` does not implement `IConvertible`, so the
                    // usual conversion never runs.
                    return ValueUtilities.CDateTime(value).Date;
                case FieldDbType.DateTime:
                    return ValueUtilities.CDateTime(value);
                case FieldDbType.Guid:
                    return ValueUtilities.CGuid(value);
                default:
                    return value;
            }
        }

        /// <summary>
        /// Converts the specified value to a database-ready field value for this
        /// <see cref="FieldDbType"/>. Empty <see cref="DateTime"/> values are mapped to
        /// <see cref="DBNull.Value"/>; other values are processed through
        /// <see cref="ToFieldValue"/>.
        /// </summary>
        /// <param name="dbType">The field database type.</param>
        /// <param name="value">The input value.</param>
        public static object ToDbFieldValue(this FieldDbType dbType, object value)
        {
            if (value is DateTime && ValueUtilities.CDateTime(value) == DateTime.MinValue)
                return DBNull.Value;
            return dbType.ToFieldValue(value);
        }
    }
}
