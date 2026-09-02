namespace Bee.Api.Core.Wire
{
    /// <summary>
    /// Discriminators for the value types the framework carries on its <see cref="object"/>-typed
    /// wire members. Values are part of the wire format and must not be renumbered.
    /// <para>
    /// Shared by every body codec: the MessagePack formatter writes the code as an int ahead of
    /// the value, and the JSON codec writes the same code as the first element of a two-element
    /// array. A code therefore means the same thing on both wires, and
    /// <c>WireValueCodePinTests</c> pins the numbers for both at once.
    /// </para>
    /// </summary>
    internal static class WireValueCode
    {
        public const int Boolean = 1;
        public const int Byte = 2;
        public const int SByte = 3;
        public const int Int16 = 4;
        public const int UInt16 = 5;
        public const int Int32 = 6;
        public const int UInt32 = 7;
        public const int Int64 = 8;
        public const int UInt64 = 9;
        public const int Single = 10;
        public const int Double = 11;
        public const int Decimal = 12;
        public const int String = 13;
        public const int DateTime = 14;
        public const int DateTimeOffset = 15;
        public const int TimeSpan = 16;
        public const int DateOnly = 17;
        public const int Guid = 18;
        public const int ByteArray = 19;
        public const int DBNull = 20;
        public const int DataTable = 21;
        public const int ObjectArray = 22;

        /// <summary>
        /// One past the highest code; the size of the dispatch tables.
        /// </summary>
        public const int Count = 23;
    }
}
