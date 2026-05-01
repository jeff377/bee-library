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
                case FieldDbType.Date:
                    return DateTime.Today;
                case FieldDbType.DateTime:
                    return DateTime.Now;
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
                    return ValueUtilities.CDate(value);
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
