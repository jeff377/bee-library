using Bee.Base;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// The type whitelist applied to values that name their own type on the wire.
    /// </summary>
    /// <remarks>
    /// Lives in the API layer, not the definition layer: this is a transport-side security
    /// boundary, and keeping it here is what lets `Bee.Definition` stay free of any MessagePack
    /// reference.
    /// <para>
    /// Two callers apply it. <see cref="SafeMessagePackSerializerOptions"/> checks
    /// <b>before</b> the object is constructed, and <see cref="WireValueFormatter"/> checks the
    /// name it is about to resolve. The whitelist itself was previously carried by
    /// <c>SafeTypelessFormatter</c>, which is gone: its formatter half resolved values through
    /// <c>MessagePackSerializer.NonGeneric</c> and so could not run on the mobile heads.
    /// </para>
    /// </remarks>
    internal static class WireTypeWhitelist
    {
        /// <summary>
        /// Well-known system primitive types that are always allowed for deserialization.
        /// </summary>
        private static readonly HashSet<string> AllowedPrimitiveTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Boolean",
            "System.Byte",
            "System.SByte",
            "System.Int16",
            "System.UInt16",
            "System.Int32",
            "System.UInt32",
            "System.Int64",
            "System.UInt64",
            "System.Single",
            "System.Double",
            "System.Decimal",
            "System.String",
            "System.DateTime",
            "System.DateTimeOffset",
            "System.TimeSpan",
            // Calendar-day values ride the wire as `DateOnly` so they describe their own semantics
            // without a schema lookup. `ValueUtilities.CDateOnly` returns `DateOnly`, so any filter
            // condition built from it lands here (see docs/adr/adr-031-calendar-day-column-semantics.md).
            "System.DateOnly",
            "System.Guid",
            "System.Byte[]",
            "System.DBNull",
            "System.Data.DataTable",
            // In-clause filter values (e.g. `field IN (a, b, c)`) ride over the wire as an
            // `object[]`, so both the array and its `System.Object` element descriptor must be
            // allowed. This does not widen the deserialization gadget surface: every array element
            // is still recursively validated against this same whitelist, so only already-trusted
            // primitives (`String`, `Int32`, `Guid`, and the like) can populate the array.
            "System.Object",
            "System.Object[]"
        };

        /// <summary>
        /// Validates whether the specified type full name is in the fixed,
        /// framework-controlled whitelist (well-known primitives plus
        /// <c>System.Data.DataTable</c>).
        /// </summary>
        /// <remarks>
        /// These types stay trusted even where the MessagePack built-in blocklist
        /// disagrees: since 3.1.5 the blocklist rejects <c>System.Data.DataTable</c>
        /// as a classic BinaryFormatter gadget, but on this wire the table is
        /// rebuilt column-by-column by the framework's own formatter, so that
        /// attack path does not exist here.
        /// </remarks>
        /// <param name="fullName">The full name of the type to validate.</param>
        /// <returns><c>true</c> if the type is in the fixed whitelist.</returns>
        public static bool IsExplicitlyTrustedType(string fullName)
            => AllowedPrimitiveTypes.Contains(fullName);

        /// <summary>
        /// Validates whether the specified type full name is in the allowed whitelist.
        /// </summary>
        /// <param name="fullName">The full name of the type to validate.</param>
        /// <returns><c>true</c> if the type is allowed; otherwise, <c>false</c>.</returns>
        public static bool IsTypeAllowed(string fullName)
        {
            // Allow well-known primitive types
            if (AllowedPrimitiveTypes.Contains(fullName))
                return true;

            // Delegate to the application-level namespace whitelist
            return SysInfo.IsTypeNameAllowed(fullName);
        }
    }
}
