using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bee.Define.Filters
{
    /// <summary>
    /// Custom JSON converter for <see cref="FilterNodeCollection"/>.
    /// </summary>
    public class FilterNodeCollectionJsonConverter : JsonConverter<FilterNodeCollection>
    {
        /// <summary>
        /// Serializes a <see cref="FilterNodeCollection"/> object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The <see cref="FilterNodeCollection"/> object to serialize.</param>
        /// <param name="serializer">The JSON serializer.</param>
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
        /// Deserializes JSON into a <see cref="FilterNodeCollection"/> object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The target object type.</param>
        /// <param name="existingValue">The existing <see cref="FilterNodeCollection"/> object.</param>
        /// <param name="hasExistingValue">Indicates whether there is an existing value.</param>
        /// <param name="serializer">The JSON deserializer.</param>
        /// <returns>The deserialized <see cref="FilterNodeCollection"/> object.</returns>
        public override FilterNodeCollection ReadJson(JsonReader reader, Type objectType, FilterNodeCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var array = JArray.Load(reader);
            var nodes = new List<FilterNode>();
            foreach (var item in array)
            {
                // Determine the target type based on the Kind property
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
                            // Throw an exception or ignore unknown kinds
                            throw new JsonSerializationException($"Unknown FilterNodeKind: {kindValue}");
                    }
                }
                else
                {
                    // No Kind property — default to FilterCondition
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
