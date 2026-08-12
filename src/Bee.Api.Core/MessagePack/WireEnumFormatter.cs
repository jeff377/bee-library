using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializes an enum as its underlying <see cref="int"/> value.
    /// </summary>
    /// <remarks>
    /// MessagePack's own enum formatter is built on demand through
    /// <c>MakeGenericType</c>, which has no native code on an AOT target — the mobile heads got
    /// <c>MissingMethodException</c> for <c>GenericEnumFormatter&lt;ComparisonOperator&gt;</c>
    /// rather than a serialized value. Registering a closed instantiation of this formatter per
    /// enum roots it at compile time instead.
    /// <para>
    /// The wire format is unchanged: MessagePack also writes the underlying integer.
    /// </para>
    /// <para>
    /// NOTE: The framework's enums never declare an underlying type (SonarCloud S2344), so
    /// <see cref="int"/> is safe. The constructor asserts it rather than trusting the
    /// convention, because a wrong guess here would silently truncate values. Every instance is
    /// created while the formatter list is being assembled, so the assert still fires at
    /// registration time — a type initializer would have raised the same failure wrapped in a
    /// <see cref="TypeInitializationException"/> and left the type permanently unusable
    /// (SonarCloud S3877).
    /// </para>
    /// </remarks>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    internal sealed class WireEnumFormatter<TEnum> : IMessagePackFormatter<TEnum>
        where TEnum : unmanaged, Enum
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WireEnumFormatter{TEnum}"/> class.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Thrown when <typeparamref name="TEnum"/> does not have an <see cref="int"/> underlying type.
        /// </exception>
        public WireEnumFormatter()
        {
            if (Enum.GetUnderlyingType(typeof(TEnum)) != typeof(int))
            {
                throw new NotSupportedException(
                    $"Wire enum '{typeof(TEnum).FullName}' must have an Int32 underlying type.");
            }
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, TEnum value, MessagePackSerializerOptions options)
            => writer.Write(Unsafe.As<TEnum, int>(ref value));

        /// <summary>
        /// Deserializes the value.
        /// </summary>
        public TEnum Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var value = reader.ReadInt32();
            return Unsafe.As<int, TEnum>(ref value);
        }
    }
}
