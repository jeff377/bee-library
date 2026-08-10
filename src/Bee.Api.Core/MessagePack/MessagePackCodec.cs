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
        /// Statically initialized MessagePack serialization options, including custom formatters and resolvers.
        /// SafeMessagePackSerializerOptions overrides ThrowIfDeserializingTypeIsDisallowed
        /// to block disallowed types BEFORE object instantiation inside TypelessFormatter.
        /// </summary>
        private static readonly MessagePackSerializerOptions Options = new SafeMessagePackSerializerOptions(
            CompositeResolver.Create(
                new IMessagePackFormatter[]
                {
                    new DataTableFormatter(),          // Custom DataTable formatter
                    new DataSetFormatter(),             // Custom DataSet formatter
                    // Hand-written per-type formatters: they honour [WireIgnore] by naming the
                    // wire members explicitly, and stay generic all the way down so the mobile
                    // heads (reflection-only AOT) take the same path as the desktop.
                    new SortFieldFormatter(),
                    new DepartmentNodeFormatter(),
                    new NumberFormatItemFormatter(),
                    new CashRoundingItemFormatter(),
                    new AllowedCurrencyItemFormatter(),
                    new ParameterFormatter(),
                    new FilterNodeFormatter(),
                    // Keyed collections are not optional the way the plain ones are: contractless
                    // binds a KeyedCollection as a dictionary and loses the item type.
                    new KeyCollectionBaseFormatter<ParameterCollection, Parameter>(),
                    SafeTypelessFormatter.Instance      // Type-validated polymorphic formatter
                },
                // Plain collections need no entry above. The contractless resolver recognises a
                // `Collection<T>` and writes it as an array — byte-for-byte what the old
                // CollectionBaseFormatter registrations produced. Those registrations were only
                // ever needed because the collections carried `[MessagePackObject]`, whose opt-in
                // membership left them with zero members and so an empty map; with the attribute
                // gone the need went with it.
                //
                // Keyed collections are the exception, and they are registered above: contractless
                // binds a `KeyedCollection<TKey, TItem>` as a dictionary and hands back
                // `Dictionary<object, object>` in place of the item type.
                new IFormatterResolver[]
                {
                    ContractlessStandardResolver.Instance, // Contractless resolver (without unsafe Typeless support)
                    FormatterResolver.Instance,            // Custom resolver
                    StandardResolver.Instance              // Standard resolver
                }));

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <returns>The serialized byte array.</returns>
        public static byte[] Serialize<T>(T value)
        {
            return MessagePackSerializer.Serialize(value, Options);
        }

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="type">The type of the object.</param>
        /// <returns>The serialized byte array.</returns>
        public static byte[] Serialize(object value, Type type)
        {
            return MessagePackSerializer.Serialize(type, value, Options);
        }

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <typeparam name="T">The type of the deserialized object.</typeparam>
        /// <param name="data">The byte array to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, Options);
        }

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <param name="data">The byte array to deserialize.</param>
        /// <param name="type">The target object type.</param>
        /// <returns>The deserialized object.</returns>
        public static object? Deserialize(byte[] data, Type type)
        {
            return MessagePackSerializer.Deserialize(type, data, Options);
        }
    }

}
