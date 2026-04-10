using System;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Serialization;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Provides static methods for MessagePack serialization and deserialization using custom formatters and resolvers.
    /// </summary>
    public static class MessagePackHelper
    {
        /// <summary>
        /// Statically initialized MessagePack serialization options, including custom formatters and resolvers.
        /// </summary>
        private static readonly MessagePackSerializerOptions Options;

        /// <summary>
        /// Static constructor that initializes the MessagePack serialization options.
        /// </summary>
        static MessagePackHelper()
        {
            // Create the custom formatter and resolver composition
            var resolver = CompositeResolver.Create(
                new IMessagePackFormatter[]
                {
                    new DataTableFormatter(),          // Custom DataTable formatter
                    new DataSetFormatter(),             // Custom DataSet formatter
                    new CollectionBaseFormatter<FilterNodeCollection, FilterNode>(),   // FilterNodeCollection as array
                    new CollectionBaseFormatter<SortFieldCollection, SortField>(),     // SortFieldCollection as array
                    SafeTypelessFormatter.Instance      // Type-validated polymorphic formatter
                },
                new IFormatterResolver[]
                {
                    ContractlessStandardResolver.Instance, // Contractless resolver (without unsafe Typeless support)
                    FormatterResolver.Instance,            // Custom resolver
                    StandardResolver.Instance              // Standard resolver
                });

            // Configure the MessagePack serialization options
            Options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

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
        public static object Deserialize(byte[] data, Type type)
        {
            return MessagePackSerializer.Deserialize(type, data, Options);  
        }
    }

}
