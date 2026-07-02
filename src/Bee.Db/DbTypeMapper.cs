using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// Maps CLR types to <see cref="DbType"/> for ADO.NET parameter inference.
    /// </summary>
    public static class DbTypeMapper
    {
        /// <summary>
        /// Infers the <see cref="DbType"/> from the given value's runtime type.
        /// Returns <c>null</c> for <c>null</c>, <see cref="DBNull"/>, or unmapped types,
        /// letting the database provider infer automatically.
        /// </summary>
        /// <param name="value">The value to infer the type from.</param>
        public static DbType? Infer(object value)
        {
            if (value == null || value is DBNull) return null;

            var type = value.GetType();

            if (type == typeof(string)) return DbType.String;
            if (type == typeof(int)) return DbType.Int32;
            if (type == typeof(long)) return DbType.Int64;
            if (type == typeof(short)) return DbType.Int16;
            if (type == typeof(byte)) return DbType.Byte;
            if (type == typeof(bool)) return DbType.Boolean;
            // Map to DateTime2 (not DateTime) so the full .NET DateTime range and 100 ns precision
            // survive the parameter layer. DbType.DateTime would round to ~3.33 ms and reject
            // pre-1753 values before transmission, defeating datetime2 columns on SQL Server.
            if (type == typeof(DateTime)) return DbType.DateTime2;
            if (type == typeof(decimal)) return DbType.Decimal;
            if (type == typeof(double)) return DbType.Double;
            if (type == typeof(float)) return DbType.Single;
            if (type == typeof(Guid)) return DbType.Guid;
            if (type == typeof(byte[])) return DbType.Binary;
            if (type == typeof(TimeSpan)) return DbType.Time;

            // Fallback: let the provider infer the type automatically
            return null;
        }
    }
}
