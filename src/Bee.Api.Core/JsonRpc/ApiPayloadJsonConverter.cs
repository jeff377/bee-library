using System;
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
            PayloadFormat format = PayloadFormat.Plain;
            JsonElement? valueElement = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propName = reader.GetString();
                reader.Read();

                switch (propName)
                {
                    case "format":
                        format = (PayloadFormat)reader.GetInt32();
                        payload.Format = format;
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

            // Resolve Value based on format
            if (valueElement.HasValue)
            {
                var elem = valueElement.Value;
                switch (format)
                {
                    case PayloadFormat.Encoded:
                    case PayloadFormat.Encrypted:
                        // Encoded/Encrypted payloads store byte[] as base64 string
                        if (elem.ValueKind == JsonValueKind.String)
                        {
                            try
                            {
                                payload.Value = elem.GetBytesFromBase64();
                            }
                            catch (FormatException)
                            {
                                // Not a valid base64 string — keep as plain string
                                payload.Value = (object?)elem.GetString();
                            }
                        }
                        else
                        {
                            payload.Value = ResolvePlainValue(elem);
                        }
                        break;


                    case PayloadFormat.Plain:
                    default:
                        // For Plain format, resolve based on JSON value kind
                        payload.Value = ResolvePlainValue(elem);
                        break;
                }
            }

            return payload;
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
