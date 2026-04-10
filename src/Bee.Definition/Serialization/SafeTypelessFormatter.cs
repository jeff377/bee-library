using System;
using System.Collections.Generic;
using Bee.Base;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Definition.Serialization
{
    /// <summary>
    /// A type-safe wrapper around <see cref="TypelessFormatter"/> that validates deserialized types
    /// against the allowed namespace whitelist defined in <see cref="SysInfo.IsTypeNameAllowed"/>.
    /// Prevents deserialization of arbitrary types to mitigate remote code execution risks.
    /// </summary>
    /// <remarks>
    /// This formatter applies two layers of defense:
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <b>Pre-instantiation:</b> A custom <see cref="MessagePackSerializerOptions"/> override of
    ///       <see cref="MessagePackSerializerOptions.ThrowIfDeserializingTypeIsDisallowed"/>
    ///       validates the type BEFORE object construction inside <see cref="TypelessFormatter"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Post-instantiation (defense-in-depth):</b> After <see cref="TypelessFormatter"/> returns,
    ///       this formatter validates the resulting object's type as a second safety net.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public sealed class SafeTypelessFormatter : IMessagePackFormatter<object>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly SafeTypelessFormatter Instance = new SafeTypelessFormatter();

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
            "System.Guid",
            "System.Byte[]",
            "System.DBNull"
        };

        /// <summary>
        /// Initializes a new instance. Public constructor required for <c>[MessagePackFormatter]</c> attribute usage.
        /// Prefer using <see cref="Instance"/> for direct registration.
        /// </summary>
        public SafeTypelessFormatter() { }

        /// <summary>
        /// Serializes the object value using the underlying <see cref="TypelessFormatter"/>.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options)
        {
            TypelessFormatter.Instance.Serialize(ref writer, value, options);
        }

        /// <summary>
        /// Deserializes an object value using the underlying <see cref="TypelessFormatter"/>,
        /// then validates the resulting type against the allowed type whitelist.
        /// </summary>
        /// <remarks>
        /// The primary pre-instantiation check is performed by the
        /// <see cref="MessagePackSerializerOptions.ThrowIfDeserializingTypeIsDisallowed"/> override
        /// inside <see cref="TypelessFormatter"/>. This post-deserialization check serves as a defense-in-depth safety net.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the deserialized type is not in the allowed whitelist.
        /// </exception>
        public object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            // Handle nil
            if (reader.TryReadNil())
                return null;

            var result = TypelessFormatter.Instance.Deserialize(ref reader, options);

            // Defense-in-depth: validate the deserialized type even though
            // SafeMessagePackSerializerOptions already performs pre-instantiation checks.
            if (result != null)
            {
                ValidateType(result.GetType());
            }

            return result;
        }

        /// <summary>
        /// Validates whether the specified type full name is in the allowed whitelist.
        /// Used by both the formatter (post-check) and <see cref="SafeMessagePackSerializerOptions"/> (pre-check).
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

        /// <summary>
        /// Validates that the specified type is allowed for deserialization.
        /// </summary>
        /// <param name="type">The type to validate.</param>
        private static void ValidateType(Type type)
        {
            var fullName = type.FullName;
            if (fullName == null)
                throw new InvalidOperationException("Cannot deserialize a type with no FullName.");

            if (!IsTypeAllowed(fullName))
            {
                throw new InvalidOperationException(
                    $"MessagePack deserialization blocked: type '{fullName}' is not in the allowed type whitelist.");
            }
        }
    }
}
