using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bee.Definition.Filters
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
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, FilterNodeCollection value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            foreach (var node in value)
            {
                JsonSerializer.Serialize(writer, node, node.GetType(), options);
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// Deserializes JSON into a <see cref="FilterNodeCollection"/> object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The target object type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The deserialized <see cref="FilterNodeCollection"/> object.</returns>
        public override FilterNodeCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"Unexpected token type '{reader.TokenType}' when reading FilterNodeCollection.");

            // Parse the array into a JsonDocument to enable property-based type discrimination
            using var doc = JsonDocument.ParseValue(ref reader);
            var nodes = new List<FilterNode>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                // Determine the target type based on the Kind property
                FilterNode? node;
                if (element.TryGetProperty("kind", out var kindProp))
                {
                    var kindValue = kindProp.ValueKind == JsonValueKind.String
                        ? Enum.Parse<FilterNodeKind>(kindProp.GetString()!)
                        : (FilterNodeKind)kindProp.GetInt32();
                    switch (kindValue)
                    {
                        case FilterNodeKind.Condition:
                            node = element.Deserialize<FilterCondition>(options);
                            break;
                        case FilterNodeKind.Group:
                            node = element.Deserialize<FilterGroup>(options);
                            break;
                        default:
                            throw new JsonException($"Unknown FilterNodeKind: {kindValue}");
                    }
                }
                else
                {
                    // No Kind property — default to FilterCondition
                    node = element.Deserialize<FilterCondition>(options);
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
