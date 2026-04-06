using System;
using Bee.Api.Core.MessagePack;
using Bee.Define;

namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// API payload serializer that uses MessagePack.
    /// </summary>
    public class MessagePackPayloadSerializer : IApiPayloadSerializer
    {
        /// <summary>
        /// Gets the identifier string for the serialization format.
        /// </summary>
        public string SerializationMethod => "messagepack";

        /// <summary>
        /// Serializes the object to a byte array.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="type">The type of the object.</param>
        public byte[] Serialize(object value, Type type)
        {
            return MessagePackHelper.Serialize(value, type);
        }

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <param name="bytes">The byte array to deserialize.</param>
        /// <param name="type">The type of the deserialized object.</param>
        public object Deserialize(byte[] bytes, Type type)
        {
            return MessagePackHelper.Deserialize(bytes, type);
        }
    }

}
