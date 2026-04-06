using System;
using Bee.Base;
using Bee.Base.Serialization;

namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// API payload serializer that uses BinaryFormatter.
    /// </summary>
    public class BinaryFormatterPayloadSerializer : IApiPayloadSerializer
    {
        /// <summary>
        /// Gets the identifier string for the serialization format.
        /// </summary>
        public string SerializationMethod => "binaryformatter";

        /// <summary>
        /// Serializes the object to a byte array.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="type">The type of the object.</param>
        public byte[] Serialize(object value, Type type)
        {
            return SerializeFunc.ObjectToBinary(value);
        }

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <param name="bytes">The byte array to deserialize.</param>
        /// <param name="type">The type of the deserialized object.</param>
        public object Deserialize(byte[] bytes, Type type)
        {
            return SerializeFunc.BinaryToObject(bytes);
        }

        
    }



}
