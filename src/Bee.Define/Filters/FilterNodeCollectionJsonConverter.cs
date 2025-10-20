using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bee.Define
{
    /// <summary>
    /// FilterNodeCollection 的自訂 JSON 轉換器。
    /// </summary>
    public class FilterNodeCollectionJsonConverter : JsonConverter<FilterNodeCollection>
    {
        /// <summary>
        /// 將 <see cref="FilterNodeCollection"/> 物件序列化為 JSON。
        /// </summary>
        /// <param name="writer">JSON 寫入器。</param>
        /// <param name="value">要序列化的 <see cref="FilterNodeCollection"/> 物件。</param>
        /// <param name="serializer">JSON 序列化器。</param>
        public override void WriteJson(JsonWriter writer, FilterNodeCollection value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();
            foreach (var node in value)
            {
                serializer.Serialize(writer, node);
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// 反序列化 JSON 為 <see cref="FilterNodeCollection"/> 物件。
        /// </summary>
        /// <param name="reader">JSON 讀取器。</param>
        /// <param name="objectType">目標物件型別。</param>
        /// <param name="existingValue">現有的 <see cref="FilterNodeCollection"/> 物件。</param>
        /// <param name="hasExistingValue">是否有現有值。</param>
        /// <param name="serializer">JSON 反序列化器。</param>
        /// <returns>反序列化後的 <see cref="FilterNodeCollection"/> 物件。</returns>
        public override FilterNodeCollection ReadJson(JsonReader reader, Type objectType, FilterNodeCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var array = JArray.Load(reader);
            var nodes = new List<FilterNode>();
            foreach (var item in array)
            {
                // 根據 Kind 屬性判斷要反序列化成哪個型別
                var kindToken = item["kind"];
                FilterNode node = null;
                if (kindToken != null)
                {
                    var kindValue = kindToken.ToObject<FilterNodeKind>();
                    switch (kindValue)
                    {
                        case FilterNodeKind.Condition:
                            node = item.ToObject<FilterCondition>(serializer);
                            break;
                        case FilterNodeKind.Group:
                            node = item.ToObject<FilterGroup>(serializer);
                            break;
                        default:
                            // 可以選擇丟出例外或忽略
                            throw new JsonSerializationException($"Unknown FilterNodeKind: {kindValue}");
                    }
                }
                else
                {
                    // 沒有 Kind 屬性，預設為 FilterCondition
                    node = item.ToObject<FilterCondition>(serializer);
                }
                if (node != null)
                    nodes.Add(node);
            }
            var collection = new FilterNodeCollection();
            collection.AddRange(nodes);
            return collection;
        }
    }
}
