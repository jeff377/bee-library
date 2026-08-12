using System.Text;
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
        private static readonly HashSet<string> s_allowedPrimitiveTypes = new HashSet<string>(StringComparer.Ordinal)
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
            => s_allowedPrimitiveTypes.Contains(fullName);

        /// <summary>
        /// Validates whether the specified type full name is in the allowed whitelist.
        /// </summary>
        /// <remarks>
        /// WARNING: This takes a <b>single</b> type name, not an assembly-qualified name that may
        /// carry generic arguments. Callers holding a name that came off the wire must use
        /// <see cref="IsAssemblyQualifiedNameAllowed"/> instead — a generic argument's own comma
        /// sits <i>before</i> the assembly separator, so naive splitting hands this method a
        /// fragment such as <c>Bee.Base.Collections.Dictionary`1[[Evil.Type</c>, which passes the
        /// namespace prefix test while the argument goes unchecked.
        /// </remarks>
        /// <param name="fullName">The full name of the type to validate.</param>
        /// <returns><c>true</c> if the type is allowed; otherwise, <c>false</c>.</returns>
        public static bool IsTypeAllowed(string fullName)
        {
            // Allow well-known primitive types
            if (s_allowedPrimitiveTypes.Contains(fullName))
                return true;

            // Delegate to the application-level namespace whitelist
            return SysInfo.IsTypeNameAllowed(fullName);
        }

        /// <summary>
        /// Maximum nesting depth accepted while parsing an assembly-qualified name. Generic
        /// arguments may nest, and a crafted name could otherwise drive unbounded recursion.
        /// </summary>
        private const int MaxNestingDepth = 8;

        /// <summary>
        /// Maximum accepted length of an assembly-qualified name. No legitimate wire type comes
        /// close; the cap bounds the work a single payload can ask the parser to do.
        /// </summary>
        private const int MaxNameLength = 1024;

        /// <summary>
        /// Validates every type named by an assembly-qualified name, including each generic
        /// argument and array element type.
        /// </summary>
        /// <remarks>
        /// WARNING: This is a security boundary and it fails closed. A name that cannot be parsed
        /// is rejected rather than passed through — the previous implementation split on the first
        /// comma, which for a generic type lands inside <c>[[...]]</c> and yields a fragment that
        /// still carries an allowed namespace prefix. The generic argument was therefore never
        /// screened, and <c>Type.GetType</c> went on to resolve it.
        /// </remarks>
        /// <param name="assemblyQualifiedName">The name as it arrived on the wire.</param>
        /// <returns><c>true</c> when every named type is allowed; otherwise, <c>false</c>.</returns>
        public static bool IsAssemblyQualifiedNameAllowed(string? assemblyQualifiedName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
                return false;
            if (assemblyQualifiedName.Length > MaxNameLength)
                return false;

            var names = new List<string>();
            var position = 0;
            if (!TryParseTypeSpec(assemblyQualifiedName, ref position, names, depth: 0))
                return false;

            // Anything left at the top level must be the assembly name, introduced by a comma.
            SkipWhitespace(assemblyQualifiedName, ref position);
            if (position < assemblyQualifiedName.Length && assemblyQualifiedName[position] != ',')
                return false;

            if (names.Count == 0)
                return false;

            return names.TrueForAll(IsTypeAllowed);
        }

        /// <summary>
        /// Validates an already-resolved <see cref="Type"/>, recursing through generic arguments
        /// and array element types.
        /// </summary>
        /// <remarks>
        /// A constructed generic type's <see cref="Type.FullName"/> embeds its arguments, so
        /// testing that single string against the namespace whitelist screens the outer type only.
        /// This walks the shape instead.
        /// </remarks>
        /// <param name="type">The type about to be instantiated.</param>
        /// <returns><c>true</c> when the type and everything it is built from are allowed.</returns>
        public static bool IsRuntimeTypeAllowed(Type type) => IsRuntimeTypeAllowed(type, depth: 0);

        private static bool IsRuntimeTypeAllowed(Type type, int depth)
        {
            if (depth > MaxNestingDepth)
                return false;
            if (type.IsPointer || type.IsByRef || type.IsGenericParameter)
                return false;

            var fullName = type.FullName;
            if (fullName == null)
                return false;

            // Whole-name matches win first: `System.Byte[]` and `System.Object[]` are whitelisted
            // as arrays, not as an element type plus a rank.
            if (s_allowedPrimitiveTypes.Contains(fullName))
                return true;

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return elementType != null && IsRuntimeTypeAllowed(elementType, depth + 1);
            }

            if (type.IsConstructedGenericType)
            {
                var definitionName = type.GetGenericTypeDefinition().FullName;
                if (definitionName == null || !IsTypeAllowed(definitionName))
                    return false;

                return type.GetGenericArguments()
                    .All(argument => IsRuntimeTypeAllowed(argument, depth + 1));
            }

            return IsTypeAllowed(fullName);
        }

        /// <summary>
        /// Parses one type specification, appending the plain type names it contains to
        /// <paramref name="names"/>. Advances <paramref name="position"/> past what it consumed.
        /// </summary>
        private static bool TryParseTypeSpec(string text, ref int position, List<string> names, int depth)
        {
            if (depth > MaxNestingDepth)
                return false;

            SkipWhitespace(text, ref position);

            var start = position;
            while (position < text.Length && !IsTypeSpecTerminator(text[position]))
            {
                position++;
            }

            var name = new StringBuilder(text[start..position].Trim());
            if (name.Length == 0)
                return false;

            // Pointers and by-ref types never appear on this wire; reject rather than strip.
            if (position < text.Length && (text[position] == '*' || text[position] == '&'))
                return false;

            if (!TryParseNameSuffixes(text, ref position, name, names, depth))
                return false;

            names.Add(name.ToString());
            return true;
        }

        /// <summary>
        /// Consumes the bracket groups that follow a type name. An array rank binds to
        /// <paramref name="name"/>; a generic-argument list contributes its own names to
        /// <paramref name="names"/>.
        /// </summary>
        private static bool TryParseNameSuffixes(
            string text, ref int position, StringBuilder name, List<string> names, int depth)
        {
            var sawGenericArguments = false;
            while (position < text.Length && text[position] == '[')
            {
                var close = FindMatchingBracket(text, position);
                if (close < 0)
                    return false;

                var inner = text[(position + 1)..close];
                if (IsArrayRank(inner))
                {
                    // An array rank binds to the name: `System.Byte` + `[]` is the whitelisted
                    // `System.Byte[]`, which is a different entry from `System.Byte`.
                    name.Append('[').Append(inner).Append(']');
                }
                else
                {
                    // Only one generic-argument list may follow a name; a second non-rank bracket
                    // group is malformed.
                    if (sawGenericArguments)
                        return false;
                    if (!TryParseGenericArguments(text, position + 1, close, names, depth + 1))
                        return false;
                    sawGenericArguments = true;
                }

                position = close + 1;
            }

            return true;
        }

        /// <summary>
        /// Parses the comma-separated argument list between a generic type's brackets. Each
        /// argument is either bracketed (assembly-qualified) or bare.
        /// </summary>
        private static bool TryParseGenericArguments(string text, int start, int end, List<string> names, int depth)
        {
            if (depth > MaxNestingDepth)
                return false;

            var position = start;
            while (position < end)
            {
                if (!TryParseGenericArgument(text, ref position, end, names, depth))
                    return false;

                SkipWhitespace(text, ref position);
                if (position < end)
                {
                    if (text[position] != ',')
                        return false;
                    position++;
                }
            }

            return true;
        }

        /// <summary>
        /// Parses one generic argument, which is either bracketed (and therefore
        /// assembly-qualified) or bare.
        /// </summary>
        private static bool TryParseGenericArgument(
            string text, ref int position, int end, List<string> names, int depth)
        {
            SkipWhitespace(text, ref position);
            if (position >= end)
                return false;

            if (text[position] != '[')
                return TryParseTypeSpec(text, ref position, names, depth);

            var close = FindMatchingBracket(text, position);
            if (close < 0 || close > end)
                return false;

            var inner = position + 1;
            if (!TryParseTypeSpec(text, ref inner, names, depth))
                return false;

            // Whatever remains inside the brackets is the argument's assembly name.
            SkipWhitespace(text, ref inner);
            if (inner < close && text[inner] != ',')
                return false;

            position = close + 1;
            return true;
        }

        /// <summary>
        /// Determines whether a bracket group is an array rank (empty, or commas only) rather than
        /// a generic-argument list.
        /// </summary>
        private static bool IsArrayRank(string inner)
            => inner.All(c => c == ',' || char.IsWhiteSpace(c));

        /// <summary>
        /// Returns the index of the bracket closing the one at <paramref name="open"/>, or -1.
        /// </summary>
        private static int FindMatchingBracket(string text, int open)
        {
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                if (text[i] == '[')
                {
                    depth++;
                }
                else if (text[i] == ']')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private static bool IsTypeSpecTerminator(char c)
            => c is '[' or ']' or ',' or '*' or '&';

        private static void SkipWhitespace(string text, ref int position)
        {
            while (position < text.Length && char.IsWhiteSpace(text[position]))
            {
                position++;
            }
        }
    }
}
