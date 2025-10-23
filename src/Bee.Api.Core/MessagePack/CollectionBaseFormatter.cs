using Bee.Base;
using Bee.Define;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core
{
    /// <summary>
    /// 用於序列化與反序列化繼承自 <see cref="MessagePackCollectionBase{T}"/> 的強型別集合格式化器。
    /// </summary>
    /// <typeparam name="TCollection">具體集合型別，需繼承自 <see cref="MessagePackCollectionBase{TElement}"/>，且具備無參數建構式。</typeparam>
    /// <typeparam name="TElement">集合成員型別。</typeparam>
    internal class CollectionBaseFormatter<TCollection, TElement> : IMessagePackFormatter<TCollection>
        where TCollection : MessagePackCollectionBase<TElement>, new()
        where TElement : class, ICollectionItem
    {
        /// <summary>
        /// 將集合物件序列化為 MessagePack 格式資料。
        /// </summary>
        /// <param name="writer">MessagePack 寫入器。</param>
        /// <param name="value">要序列化的集合。</param>
        /// <param name="options">序列化選項。</param>
        public void Serialize(ref MessagePackWriter writer, TCollection value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(value.Count);

            foreach (var item in value)
            {
                MessagePackSerializer.Serialize(ref writer, item, options);
            }
        }

        /// <summary>
        /// 將 MessagePack 格式資料反序列化為集合物件。
        /// </summary>
        /// <param name="reader">MessagePack 讀取器。</param>
        /// <param name="options">反序列化選項。</param>
        /// <returns>反序列化後的集合物件。</returns>
        public TCollection Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();
            var collection = new TCollection();

            for (int i = 0; i < count; i++)
            {
                var element = MessagePackSerializer.Deserialize<TElement>(ref reader, options);
                collection.Add(element);
            }

            return collection;
        }
    }

}
