namespace Bee.Base.Data
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
        Unknown,
        /// <summary>
        /// Time of day (<c>00:00</c>–<c>23:59</c>, minute precision), stored as a fixed-width
        /// <c>"HH:mm"</c> string.
        /// </summary>
        /// <remarks>
        /// A time of day is a wall-clock position within a day, not an instant and not a duration:
        /// use it for shift boundaries, opening hours and reminder times. It is never shifted by a
        /// time zone. Values ride in a <c>char(5)</c> column and a <c>string</c> DataColumn — see
        /// <c>docs/adr/adr-033-time-of-day-semantics.md</c> for why a string rather than a native
        /// database time type. New members must be appended here: the value rides the MessagePack
        /// wire as its underlying integer, so inserting one mid-enum breaks existing payloads.
        /// </remarks>
        Time
    }
}
