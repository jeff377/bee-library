using System.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// MessagePack formatter for serializing and deserializing DataSet objects.
    /// </summary>
    internal class DataSetFormatter : IMessagePackFormatter<DataSet?>
    {
        /// <summary>
        /// Serializes the DataSet object to MessagePack format.
        /// </summary>
        /// <param name="writer">The MessagePack writer.</param>
        /// <param name="value">The DataSet object to serialize.</param>
        /// <param name="options">The serialization options.</param>
        public void Serialize(ref MessagePackWriter writer, DataSet? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var sds = SerializableDataSet.FromDataSet(value);

            // Serialize the SerializableDataSet
            MessagePackSerializer.Serialize(ref writer, sds, options);
        }

        /// <summary>
        /// Deserializes a DataSet object from MessagePack format.
        /// </summary>
        /// <param name="reader">The MessagePack reader.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>The restored DataSet object.</returns>
        public DataSet? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;;

            var sds = MessagePackSerializer.Deserialize<SerializableDataSet>(ref reader, options);
            return SerializableDataSet.ToDataSet(sds);
        }
    }
}
