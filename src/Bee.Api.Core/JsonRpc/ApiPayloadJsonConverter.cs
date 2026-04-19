using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Custom JSON converter for <see cref="ApiPayload"/> and its derived types
    /// (<see cref="JsonRpcParams"/>, <see cref="JsonRpcResult"/>).
    /// Handles polymorphic deserialization of <see cref="ApiPayload.Value"/>
    /// based on the <see cref="ApiPayload.Format"/> property.
    /// </summary>
    /// <typeparam name="T">The concrete <see cref="ApiPayload"/> subtype.</typeparam>
    public class ApiPayloadJsonConverter<T> : JsonConverter<T> where T : ApiPayload, new()
    {
        /// <inheritdoc />
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Unexpected token type '{reader.TokenType}' when reading ApiPayload.");

            var payload = new T();
            JsonElement? valueElement = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propName = reader.GetString();
                reader.Read();
                ReadProperty(ref reader, payload, propName, ref valueElement);
            }

            if (valueElement.HasValue)
                payload.Value = ResolvePayloadValue(payload.Format, valueElement.Value);

            return payload;
        }

        private static void ReadProperty(ref Utf8JsonReader reader, T payload, string? propName, ref JsonElement? valueElement)
        {
            switch (propName)
            {
                case "format":
                    payload.Format = (PayloadFormat)reader.GetInt32();
                    break;
                case "value":
                    if (reader.TokenType != JsonTokenType.Null)
                    {
                        using var doc = JsonDocument.ParseValue(ref reader);
                        valueElement = doc.RootElement.Clone();
                    }
                    break;
                case "type":
                    payload.TypeName = reader.GetString() ?? string.Empty;
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        private static object? ResolvePayloadValue(PayloadFormat format, JsonElement elem)
        {
            if (format is PayloadFormat.Encoded or PayloadFormat.Encrypted)
                return ResolveEncodedValue(elem);

            return ResolvePlainValue(elem);
        }

        private static object? ResolveEncodedValue(JsonElement elem)
        {
            if (elem.ValueKind != JsonValueKind.String)
                return ResolvePlainValue(elem);

            try
            {
                return elem.GetBytesFromBase64();
            }
            catch (FormatException)
            {
                return elem.GetString();
            }
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteNumber("format", (int)value.Format);

            writer.WritePropertyName("value");
            if (value.Value == null)
                writer.WriteNullValue();
            else
                JsonSerializer.Serialize(writer, value.Value, value.Value.GetType(), options);

            writer.WriteString("type", value.TypeName);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Resolves a <see cref="JsonElement"/> to a .NET primitive value for Plain format payloads.
        /// </summary>
        private static object? ResolvePlainValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return (object?)element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var l))
                        return l;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    // For objects/arrays, return the JsonElement itself for downstream processing
                    return element;
            }
        }
    }

    /// <summary>
    /// Factory that creates <see cref="ApiPayloadJsonConverter{T}"/> for any <see cref="ApiPayload"/>-derived type.
    /// </summary>
    public class ApiPayloadJsonConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(ApiPayload).IsAssignableFrom(typeToConvert) && !typeToConvert.IsAbstract;
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(ApiPayloadJsonConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}
