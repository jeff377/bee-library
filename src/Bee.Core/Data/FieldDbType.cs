namespace Bee.Core.Data
{
    /// <summary>
    /// Abstract field database type for cross-database mapping.
    /// </summary>
    public enum FieldDbType
    {
        /// <summary>
        /// String value.
        /// </summary>
        String,
        /// <summary>
        /// Long text value.
        /// </summary>
        Text,
        /// <summary>
        /// Boolean value.
        /// </summary>
        Boolean,
        /// <summary>
        /// Auto-increment integer.
        /// </summary>
        AutoIncrement,
        /// <summary>
        /// 16-bit integer (-32,768 to 32,767).
        /// </summary>
        Short,
        /// <summary>
        /// 32-bit integer (-2,147,483,648 to 2,147,483,647).
        /// </summary>
        Integer,
        /// <summary>
        /// 64-bit integer (long).
        /// </summary>
        Long,
        /// <summary>
        /// High-precision decimal value.
        /// </summary>
        Decimal,
        /// <summary>
        /// Currency value.
        /// </summary>
        Currency,
        /// <summary>
        /// Date value.
        /// </summary>
        Date,
        /// <summary>
        /// Date and time value.
        /// </summary>
        DateTime,
        /// <summary>
        /// GUID value.
        /// </summary>
        Guid,
        /// <summary>
        /// Binary data.
        /// </summary>
        Binary,
        /// <summary>
        /// Unknown type.
        /// </summary>
        Unknown
    }
}
