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
    }
}
