using System.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Define
{
    /// <summary>
    /// 用於序列化與反序列化 DataSet 的格式化器。
    /// </summary>
    public class TDataSetFormatter : IMessagePackFormatter<DataSet>
    {
        /// <summary>
        /// 序列化 DataSet 物件。
        /// </summary>
        /// <param name="writer">MessagePack 寫入器。</param>
        /// <param name="value">要序列化的 DataSet 物件。</param>
        /// <param name="options">序列化選項。</param>
        public void Serialize(ref MessagePackWriter writer, DataSet value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var sds = TSerializableDataSet.FromDataSet(value);

            // 序列化 TSerializableDataSet
            MessagePackSerializer.Serialize(ref writer, sds, options);
        }

        /// <summary>
        /// 反序列化 DataSet 物件。
        /// </summary>
        /// <param name="reader">MessagePack 讀取器。</param>
        /// <param name="options">序列化選項。</param>
        /// <returns>還原的 DataSet 物件。</returns>
        public DataSet Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            var sds = MessagePackSerializer.Deserialize<TSerializableDataSet>(ref reader, options);
            return TSerializableDataSet.ToDataSet(sds);
        }
    }
}
