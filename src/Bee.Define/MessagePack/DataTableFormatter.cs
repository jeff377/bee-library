using System.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Define
{
    /// <summary>
    /// 用於序列化與反序列化 DataTable 的格式化器。
    /// </summary>
    public class DataTableFormatter : IMessagePackFormatter<DataTable>
    {
        /// <summary>
        /// 序列化 DataTable 物件。
        /// </summary>
        /// <param name="writer">MessagePack 寫入器。</param>
        /// <param name="value">要序列化的 DataTable 物件。</param>
        /// <param name="options">序列化選項。</param>
        public void Serialize(ref MessagePackWriter writer, DataTable value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var sdt = SerializableDataTable.FromDataTable(value);

            // 序列化 TSerializableDataTable
            MessagePackSerializer.Serialize(ref writer, sdt, options);
        }

        /// <summary>
        /// 反序列化 DataTable 物件。
        /// </summary>
        /// <param name="reader">MessagePack 讀取器。</param>
        /// <param name="options">序列化選項。</param>
        /// <returns>還原的 DataTable 物件。</returns>
        public DataTable Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            var sdt = MessagePackSerializer.Deserialize<SerializableDataTable>(ref reader, options);
            return SerializableDataTable.ToDataTable(sdt);
        }
    }
}
