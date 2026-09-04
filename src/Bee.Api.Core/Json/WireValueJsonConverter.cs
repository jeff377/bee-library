using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Wire;

namespace Bee.Api.Core.Json
{
    /// <summary>
    /// Serializes an <see cref="object"/>-typed wire member (a filter value, a parameter value) as a
    /// two-element JSON array: a discriminator followed by the value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WARNING: JSON cannot carry the value's type on its own, and without this envelope the
    /// distinctions the framework depends on are silently lost — a <see cref="decimal"/> and a
    /// <see cref="double"/> are both <c>1.0</c>, and a <see cref="Guid"/>, a
    /// <see cref="DateTime"/> and a <see cref="string"/> are all quoted text. System.Text.Json
    /// reads every one of them back as a <see cref="JsonElement"/>, so the loss surfaces later as
    /// a wrong value rather than as an error.
    /// </para>
    /// <para>
    /// The discriminators are <see cref="WireValueCode"/>, the same numbers the MessagePack
    /// formatter writes, so a code means the same thing on both wires.
    /// </para>
    /// <para>
    /// IMPORTANT: <see cref="decimal"/>, <see cref="long"/> and <see cref="ulong"/> are written as
    /// JSON strings, not numbers. A JSON number is a double to every JavaScript reader, which
    /// cannot hold a decimal's precision nor an integer past 2^53 — writing them unquoted would
    /// corrupt money and identifiers in the very clients this codec exists to serve.
    /// </para>
    /// <para>
    /// Values whose type is not in the known set take the same escape hatch as the MessagePack
    /// formatter: the discriminator is an assembly-qualified type name, screened against
    /// <see cref="WireTypeWhitelist"/> before the type is resolved.
    /// </para>
    /// </remarks>
    internal sealed class WireValueJsonConverter : JsonConverter<object>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly WireValueJsonConverter Instance = new WireValueJsonConverter();

        private WireValueJsonConverter() { }

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(object);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();

            var type = value.GetType();
            var code = ResolveCode(type);
            if (code != null)
            {
                writer.WriteNumberValue(code.Value);
                WriteKnownValue(writer, code.Value, value, options);
                writer.WriteEndArray();
                return;
            }

            // NOTE: Both ends of this escape hatch must screen the same shape. The writer holds a
            // `Type` and the reader holds a name, so they call different overloads — but both walk
            // generic arguments and array element types rather than testing one flat string.
            var fullName = type.FullName
                ?? throw new InvalidOperationException("Cannot serialize a type with no FullName.");
            if (!WireTypeWhitelist.IsRuntimeTypeAllowed(type))
            {
                throw new InvalidOperationException(
                    $"JSON serialization blocked: type '{fullName}' is not in the allowed type whitelist.");
            }

            writer.WriteStringValue(type.AssemblyQualifiedName);
            JsonSerializer.Serialize(writer, value, type, options);
            writer.WriteEndArray();
        }

        /// <inheritdoc />
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"Unexpected token '{reader.TokenType}' when reading a wire value envelope.");

            reader.Read();
            object? value;

            if (reader.TokenType == JsonTokenType.Number)
            {
                var code = reader.GetInt32();
                reader.Read();
                value = ReadKnownValue(ref reader, code, options);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var typeName = reader.GetString()
                    ?? throw new JsonException("Wire value envelope has no type name.");
                reader.Read();
                value = ReadNamedValue(ref reader, typeName, options);
            }
            else
            {
                throw new JsonException(
                    $"Unexpected discriminator token '{reader.TokenType}' in a wire value envelope.");
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException("A wire value envelope must hold exactly two elements.");

            return value;
        }

        /// <summary>
        /// Maps a runtime type to its discriminator, or null when the type takes the escape hatch.
        /// </summary>
        private static int? ResolveCode(Type type)
        {
            if (type == typeof(bool)) return WireValueCode.Boolean;
            if (type == typeof(byte)) return WireValueCode.Byte;
            if (type == typeof(sbyte)) return WireValueCode.SByte;
            if (type == typeof(short)) return WireValueCode.Int16;
            if (type == typeof(ushort)) return WireValueCode.UInt16;
            if (type == typeof(int)) return WireValueCode.Int32;
            if (type == typeof(uint)) return WireValueCode.UInt32;
            if (type == typeof(long)) return WireValueCode.Int64;
            if (type == typeof(ulong)) return WireValueCode.UInt64;
            if (type == typeof(float)) return WireValueCode.Single;
            if (type == typeof(double)) return WireValueCode.Double;
            if (type == typeof(decimal)) return WireValueCode.Decimal;
            if (type == typeof(string)) return WireValueCode.String;
            if (type == typeof(DateTime)) return WireValueCode.DateTime;
            if (type == typeof(DateTimeOffset)) return WireValueCode.DateTimeOffset;
            if (type == typeof(TimeSpan)) return WireValueCode.TimeSpan;
            if (type == typeof(DateOnly)) return WireValueCode.DateOnly;
            if (type == typeof(Guid)) return WireValueCode.Guid;
            if (type == typeof(byte[])) return WireValueCode.ByteArray;
            if (type == typeof(DBNull)) return WireValueCode.DBNull;
            if (type == typeof(DataTable)) return WireValueCode.DataTable;
            if (type == typeof(object[])) return WireValueCode.ObjectArray;
            return null;
        }

        private static void WriteKnownValue(Utf8JsonWriter writer, int code, object value, JsonSerializerOptions options)
        {
            var culture = CultureInfo.InvariantCulture;
            switch (code)
            {
                case WireValueCode.Boolean: writer.WriteBooleanValue((bool)value); break;
                case WireValueCode.Byte: writer.WriteNumberValue((byte)value); break;
                case WireValueCode.SByte: writer.WriteNumberValue((sbyte)value); break;
                case WireValueCode.Int16: writer.WriteNumberValue((short)value); break;
                case WireValueCode.UInt16: writer.WriteNumberValue((ushort)value); break;
                case WireValueCode.Int32: writer.WriteNumberValue((int)value); break;
                case WireValueCode.UInt32: writer.WriteNumberValue((uint)value); break;
                case WireValueCode.Single: writer.WriteNumberValue((float)value); break;
                case WireValueCode.Double: writer.WriteNumberValue((double)value); break;

                // Quoted on purpose — see the IMPORTANT note on the class.
                case WireValueCode.Int64: writer.WriteStringValue(((long)value).ToString(culture)); break;
                case WireValueCode.UInt64: writer.WriteStringValue(((ulong)value).ToString(culture)); break;
                case WireValueCode.Decimal: writer.WriteStringValue(((decimal)value).ToString(culture)); break;

                case WireValueCode.String: writer.WriteStringValue((string)value); break;

                // Round-trip formats: "O" keeps DateTimeKind, which ADR-032 depends on.
                case WireValueCode.DateTime: writer.WriteStringValue(((DateTime)value).ToString("O", culture)); break;
                case WireValueCode.DateTimeOffset: writer.WriteStringValue(((DateTimeOffset)value).ToString("O", culture)); break;
                case WireValueCode.TimeSpan: writer.WriteStringValue(((TimeSpan)value).ToString("c", culture)); break;
                case WireValueCode.DateOnly: writer.WriteStringValue(((DateOnly)value).ToString("O", culture)); break;
                case WireValueCode.Guid: writer.WriteStringValue(((Guid)value).ToString("D", culture)); break;

                case WireValueCode.ByteArray: writer.WriteBase64StringValue((byte[])value); break;
                case WireValueCode.DBNull: writer.WriteNullValue(); break;
                case WireValueCode.DataTable: JsonSerializer.Serialize(writer, (DataTable)value, options); break;

                case WireValueCode.ObjectArray:
                    writer.WriteStartArray();
                    foreach (var element in (object?[])value)
                        Instance.Write(writer, element, options);
                    writer.WriteEndArray();
                    break;

                default:
                    throw new JsonException($"Unknown wire value code {code}.");
            }
        }

        private static object? ReadKnownValue(ref Utf8JsonReader reader, int code, JsonSerializerOptions options)
        {
            var culture = CultureInfo.InvariantCulture;
            switch (code)
            {
                case WireValueCode.Boolean: return reader.GetBoolean();
                case WireValueCode.Byte: return reader.GetByte();
                case WireValueCode.SByte: return reader.GetSByte();
                case WireValueCode.Int16: return reader.GetInt16();
                case WireValueCode.UInt16: return reader.GetUInt16();
                case WireValueCode.Int32: return reader.GetInt32();
                case WireValueCode.UInt32: return reader.GetUInt32();
                case WireValueCode.Single: return reader.GetSingle();
                case WireValueCode.Double: return reader.GetDouble();

                case WireValueCode.Int64: return long.Parse(RequireString(ref reader, code), culture);
                case WireValueCode.UInt64: return ulong.Parse(RequireString(ref reader, code), culture);
                case WireValueCode.Decimal: return decimal.Parse(RequireString(ref reader, code), NumberStyles.Number, culture);

                case WireValueCode.String: return reader.GetString();

                case WireValueCode.DateTime:
                    return DateTime.ParseExact(RequireString(ref reader, code), "O", culture,
                        DateTimeStyles.RoundtripKind);
                case WireValueCode.DateTimeOffset:
                    return DateTimeOffset.ParseExact(RequireString(ref reader, code), "O", culture,
                        DateTimeStyles.RoundtripKind);
                case WireValueCode.TimeSpan:
                    return TimeSpan.ParseExact(RequireString(ref reader, code), "c", culture);
                case WireValueCode.DateOnly:
                    return DateOnly.ParseExact(RequireString(ref reader, code), "O", culture);
                case WireValueCode.Guid:
                    return Guid.ParseExact(RequireString(ref reader, code), "D");

                case WireValueCode.ByteArray: return reader.GetBytesFromBase64();
                case WireValueCode.DBNull: return DBNull.Value;
                case WireValueCode.DataTable: return JsonSerializer.Deserialize<DataTable>(ref reader, options);

                case WireValueCode.ObjectArray: return ReadObjectArray(ref reader, options);

                default:
                    throw new JsonException($"Unknown wire value code {code}.");
            }
        }

        private static object?[] ReadObjectArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("A wire object array must be a JSON array.");

            var items = new List<object?>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                items.Add(Instance.Read(ref reader, typeof(object), options));

            return items.ToArray();
        }

        private static object? ReadNamedValue(ref Utf8JsonReader reader, string typeName, JsonSerializerOptions options)
        {
            // WARNING: The name is screened before `Type.GetType` resolves it, so a disallowed type
            // is never even loaded. Do not reorder these two — the whole point of the whitelist is
            // that it runs ahead of anything the payload can influence. The screen covers generic
            // arguments as well; screening only the text before the first comma leaves them
            // unchecked, because a generic argument's own comma comes first (see
            // `WireTypeWhitelist.IsAssemblyQualifiedNameAllowed`).
            if (!WireTypeWhitelist.IsAssemblyQualifiedNameAllowed(typeName))
            {
                throw new InvalidOperationException(
                    $"JSON deserialization blocked: type '{typeName}' is not in the allowed type whitelist.");
            }

            var type = Type.GetType(typeName)
                ?? throw new InvalidOperationException($"JSON deserialization blocked: unknown type '{typeName}'.");

            // Second layer, on the resolved type rather than the name it arrived as. The MessagePack
            // side has always had one (`ThrowIfDeserializingTypeIsDisallowed`); this wire only had
            // the name screen, so the two escape hatches were not equally deep — which matters
            // exactly when a deployment widens `AllowedTypeNamespaces` and the name screen stops
            // being the narrow thing it is by default. Re-checking the shape also catches a name
            // that resolved to something other than what it spelled.
            if (!WireTypeWhitelist.IsRuntimeTypeAllowed(type))
            {
                throw new InvalidOperationException(
                    $"JSON deserialization blocked: resolved type '{type.FullName}' is not in the allowed type whitelist.");
            }

            return JsonSerializer.Deserialize(ref reader, type, options);
        }

        /// <summary>
        /// Reads a value that must be a JSON string, naming the code in the failure so a wire
        /// mismatch says which value went wrong.
        /// </summary>
        private static string RequireString(ref Utf8JsonReader reader, int code)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Wire value code {code} must carry a JSON string.");

            return reader.GetString()!;
        }
    }
}
