using System.Globalization;
using Bee.Base.Data;

namespace Bee.Expressions
{
    /// <summary>
    /// The shared type/null policy applied when feeding field values into the expression engine.
    /// Both the server (before save) and UI clients (live preview) route values through this policy
    /// so that a computed field yields an identical result on either side.
    /// </summary>
    /// <remarks>
    /// Numeric rounding is deliberately not handled here: a computed value is produced at full
    /// precision and rounded at write-back by the number subsystem (keyed by the field's
    /// <c>NumberKind</c>), so the round-then-store invariant stays in one place.
    /// </remarks>
    public static class ExpressionPolicy
    {
        /// <summary>
        /// Maps a <see cref="FieldDbType"/> to the CLR type used for the corresponding expression
        /// variable (for example <see cref="FieldDbType.Currency"/> maps to <see cref="decimal"/>).
        /// </summary>
        /// <param name="dbType">The database field type.</param>
        public static Type ToClrType(FieldDbType dbType) => DbTypeConverter.ToType(dbType);

        /// <summary>
        /// Coerces a raw field value into a non-null value of the field's CLR type. A
        /// <c>null</c>/<see cref="DBNull"/> value becomes the type's default (0 for numerics,
        /// empty string for text, <see cref="Guid.Empty"/>, and so on), so an arithmetic expression
        /// such as <c>unit_price * qty</c> treats a blank operand as zero rather than throwing.
        /// </summary>
        /// <param name="value">The raw value (for example a <see cref="System.Data.DataRow"/> cell).</param>
        /// <param name="dbType">The field's database type.</param>
        public static object CoerceValue(object? value, FieldDbType dbType)
        {
            var clrType = DbTypeConverter.ToType(dbType);
            if (value is null || value is DBNull) { return DefaultOf(clrType); }
            if (clrType.IsInstanceOfType(value)) { return value; }
            // The value's runtime type differs from the field's CLR type (for example an int cell
            // feeding a decimal field). Convert when possible; incompatible types surface as an
            // InvalidCastException from the framework, which the caller treats as a config error.
            return Convert.ChangeType(value, clrType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the non-null default value for a CLR type: empty string for
        /// <see cref="string"/>, an empty array for <c>byte[]</c>, otherwise the value type's
        /// zero-initialized default.
        /// </summary>
        private static object DefaultOf(Type clrType)
        {
            if (clrType == typeof(string)) { return string.Empty; }
            if (clrType == typeof(byte[])) { return Array.Empty<byte>(); }
            return Activator.CreateInstance(clrType)!;
        }
    }
}
