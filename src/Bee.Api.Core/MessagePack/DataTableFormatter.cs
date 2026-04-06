using System.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// MessagePack formatter for serializing and deserializing DataTable objects.
    /// </summary>
    internal class DataTableFormatter : IMessagePackFormatter<DataTable>
    {
        /// <summary>
        /// Serializes the DataTable object to MessagePack format.
        /// </summary>
        /// <param name="writer">The MessagePack writer.</param>
        /// <param name="value">The DataTable object to serialize.</param>
        /// <param name="options">The serialization options.</param>
        public void Serialize(ref MessagePackWriter writer, DataTable value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var sdt = SerializableDataTable.FromDataTable(value);

            // Serialize the SerializableDataTable
            MessagePackSerializer.Serialize(ref writer, sdt, options);
        }

        /// <summary>
        /// Deserializes a DataTable object from MessagePack format.
        /// </summary>
        /// <param name="reader">The MessagePack reader.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>The restored DataTable object.</returns>
        public DataTable Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            var sdt = MessagePackSerializer.Deserialize<SerializableDataTable>(ref reader, options);
            return SerializableDataTable.ToDataTable(sdt);
        }
    }
}
