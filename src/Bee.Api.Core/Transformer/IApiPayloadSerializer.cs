using System;

namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// Interface for the API transport layer payload serialization strategy.
    /// </summary>
    public interface IApiPayloadSerializer
    {
        /// <summary>
        /// Gets the identifier string for the serialization format.
        /// </summary>
        string SerializationMethod { get; }

        /// <summary>
        /// Serializes the object to a byte array.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="type">The type of the object.</param>
        byte[] Serialize(object value, Type type);

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <param name="bytes">The byte array to deserialize.</param>
        /// <param name="type">The type of the deserialized object.</param>
        object Deserialize(byte[] bytes, Type type);
    }
}
