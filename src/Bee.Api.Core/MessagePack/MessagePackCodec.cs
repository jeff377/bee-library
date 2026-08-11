using Bee.Definition.Collections;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Provides static methods for MessagePack serialization and deserialization using custom formatters and resolvers.
    /// </summary>
    internal static class MessagePackCodec
    {
        /// <summary>
        /// The explicit formatter list, exposed so <c>WireContractDriftTests</c> can check it
        /// against the wire type closure.
        /// </summary>
        /// <remarks>
        /// WARNING: Every wire type is registered explicitly, and that is not belt-and-braces.
        /// The contractless resolver still trails the list, but it can only build formatters where
        /// dynamic code is available — .NET for iOS sets `DynamicCodeSupport=false` for every
        /// build, and there an unregistered type raises `FormatterNotRegisteredException` instead
        /// of falling back to anything. See <see cref="WireContracts"/>.
        /// <para>
        /// NOTE: Declared before <c>s_options</c> on purpose — static field initialisers run in
        /// declaration order, and <c>s_options</c> reads this one.
        /// </para>
        /// </remarks>
        internal static IReadOnlyList<IMessagePackFormatter> RegisteredFormatters { get; } = BuildFormatters();

        /// <summary>
        /// Statically initialized MessagePack serialization options, including custom formatters and resolvers.
        /// SafeMessagePackSerializerOptions overrides ThrowIfDeserializingTypeIsDisallowed
        /// to block disallowed types before object instantiation.
        /// </summary>
        private static readonly MessagePackSerializerOptions s_options = new SafeMessagePackSerializerOptions(
            CompositeResolver.Create(
                RegisteredFormatters,
                // The contractless resolver stays last as a desktop-only convenience for types the
                // registrations have not reached (a host's own `Parameter.Value` payload, say).
                // It is not a safety net on the mobile heads.
                new IFormatterResolver[]
                {
                    ContractlessStandardResolver.Instance, // Contractless resolver (without unsafe Typeless support)
                    StandardResolver.Instance              // Standard resolver
                }));

        /// <summary>
        /// The options every serialization on this codec uses, exposed so
        /// <c>WireValueCodePinTests</c> can drive a single formatter through the same resolver the
        /// real path uses. Reading it from the test is the point: options assembled in the test
        /// would pin an encoding nobody ships.
        /// </summary>
        internal static MessagePackSerializerOptions SerializerOptions => s_options;

        /// <summary>
        /// Assembles the explicit formatter list: the hand-written ones first, then the generated
        /// wire contracts.
        /// </summary>
        private static IReadOnlyList<IMessagePackFormatter> BuildFormatters()
        {
            var formatters = new List<IMessagePackFormatter>
            {
                new DataTableFormatter(),           // Custom DataTable formatter
                new DataSetFormatter(),             // Custom DataSet formatter
                // Hand-written per-type formatters: they exclude the framework-managed members by
                // naming the wire members explicitly, and stay generic all the way down so the
                // mobile heads (reflection-only AOT) take the same path as the desktop.
                new SortFieldFormatter(),
                new DepartmentNodeFormatter(),
                new NumberFormatItemFormatter(),
                new CashRoundingItemFormatter(),
                new AllowedCurrencyItemFormatter(),
                new ParameterFormatter(),
                new FilterNodeFormatter(),
                new FilterConditionFormatter(),
                new FilterGroupFormatter(),
                // Keyed collections are bound as a dictionary by contractless, losing the item type.
                new KeyCollectionBaseFormatter<ParameterCollection, Parameter>(),
                WireValueFormatter.Instance         // Discriminated `object` values, AOT-safe
            };

            formatters.AddRange(WireContracts.Create());
            return formatters;
        }

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <returns>The serialized byte array.</returns>
        public static byte[] Serialize<T>(T value)
        {
            return MessagePackSerializer.Serialize(value, s_options);
        }

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="type">The type of the object.</param>
        /// <returns>The serialized byte array.</returns>
        public static byte[] Serialize(object value, Type type)
        {
            return MessagePackSerializer.Serialize(type, value, s_options);
        }

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <typeparam name="T">The type of the deserialized object.</typeparam>
        /// <param name="data">The byte array to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, s_options);
        }

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <param name="data">The byte array to deserialize.</param>
        /// <param name="type">The target object type.</param>
        /// <returns>The deserialized object.</returns>
        public static object? Deserialize(byte[] data, Type type)
        {
            return MessagePackSerializer.Deserialize(type, data, s_options);
        }
    }

}
