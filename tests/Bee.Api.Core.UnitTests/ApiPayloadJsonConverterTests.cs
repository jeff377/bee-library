using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiPayloadJsonConverter 的 Read/Write 測試。
    /// </summary>
    public class ApiPayloadJsonConverterTests
    {
        private static JsonRpcParams? Deserialize(string json) =>
            JsonSerializer.Deserialize<JsonRpcParams>(json);

        private static string Serialize(JsonRpcParams payload) =>
            JsonSerializer.Serialize(payload);

        [Fact]
        [DisplayName("Read 於 null token 應回傳 null")]
        public void Read_NullToken_ReturnsNull()
        {
            var result = Deserialize("null");
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Read 於非 StartObject token 應拋出 JsonException")]
        public void Read_NonStartObject_ThrowsJsonException()
        {
            Assert.Throws<JsonException>(() => Deserialize("123"));
        }

        [Fact]
        [DisplayName("Read Plain 字串 value 應取出字串")]
        public void Read_PlainStringValue_ReturnsString()
        {
            var json = """{"format":0,"value":"hello","type":""}""";
            var payload = Deserialize(json);

            Assert.NotNull(payload);
            Assert.Equal(PayloadFormat.Plain, payload!.Format);
            Assert.Equal("hello", payload.Value);
        }

        [Fact]
        [DisplayName("Read Plain 整數 value 應解析為 long")]
        public void Read_PlainIntegerValue_ReturnsLong()
        {
            var json = """{"format":0,"value":42,"type":""}""";
            var payload = Deserialize(json);

            Assert.NotNull(payload);
            Assert.Equal(42L, payload!.Value);
        }

        [Fact]
        [DisplayName("Read Plain 浮點數 value 應解析為 double")]
        public void Read_PlainDoubleValue_ReturnsDouble()
        {
            var json = """{"format":0,"value":3.14,"type":""}""";
            var payload = Deserialize(json);

            Assert.NotNull(payload);
            Assert.Equal(3.14d, payload!.Value);
        }

        [Fact]
        [DisplayName("Read Plain true 應解析為 bool")]
        public void Read_PlainTrueValue_ReturnsTrue()
        {
            var json = """{"format":0,"value":true,"type":""}""";
            var payload = Deserialize(json);

            Assert.True((bool)payload!.Value!);
        }

        [Fact]
        [DisplayName("Read Plain false 應解析為 bool")]
        public void Read_PlainFalseValue_ReturnsFalse()
        {
            var json = """{"format":0,"value":false,"type":""}""";
            var payload = Deserialize(json);

            Assert.False((bool)payload!.Value!);
        }

        [Fact]
        [DisplayName("Read Plain null value 應回傳 null Value")]
        public void Read_PlainNullValue_ReturnsNullValue()
        {
            var json = """{"format":0,"value":null,"type":""}""";
            var payload = Deserialize(json);

            Assert.NotNull(payload);
            Assert.Null(payload!.Value);
        }

        [Fact]
        [DisplayName("Read Plain 物件 value 應保留 JsonElement")]
        public void Read_PlainObjectValue_ReturnsJsonElement()
        {
            var json = """{"format":0,"value":{"a":1},"type":""}""";
            var payload = Deserialize(json);

            Assert.NotNull(payload);
            Assert.IsType<JsonElement>(payload!.Value);
            var elem = (JsonElement)payload.Value!;
            Assert.Equal(JsonValueKind.Object, elem.ValueKind);
        }

        [Fact]
        [DisplayName("Read Encoded base64 字串 value 應解為 byte[]")]
        public void Read_EncodedBase64Value_ReturnsByteArray()
        {
            var original = Encoding.UTF8.GetBytes("raw-bytes");
            var base64 = Convert.ToBase64String(original);
            var json = $$"""{"format":1,"value":"{{base64}}","type":"System.Byte[]"}""";

            var payload = Deserialize(json);

            Assert.NotNull(payload);
            Assert.Equal(PayloadFormat.Encoded, payload!.Format);
            Assert.IsType<byte[]>(payload.Value);
            Assert.Equal(original, (byte[])payload.Value!);
        }

        [Fact]
        [DisplayName("Read Encoded 非 base64 字串 value 應回傳字串")]
        public void Read_EncodedInvalidBase64Value_ReturnsString()
        {
            // 使用含非 base64 合法字元的字串觸發 FormatException
            var json = """{"format":1,"value":"not base64!@#","type":""}""";
            var payload = Deserialize(json);

            Assert.NotNull(payload);
            Assert.Equal(PayloadFormat.Encoded, payload!.Format);
            Assert.Equal("not base64!@#", payload.Value);
        }

        [Fact]
        [DisplayName("Read Encoded 非字串 value 應走 Plain 解析路徑")]
        public void Read_EncodedNonStringValue_FallsBackToPlain()
        {
            var json = """{"format":1,"value":99,"type":""}""";
            var payload = Deserialize(json);

            Assert.NotNull(payload);
            Assert.Equal(PayloadFormat.Encoded, payload!.Format);
            Assert.Equal(99L, payload.Value);
        }

        [Fact]
        [DisplayName("Read 未知屬性應被略過")]
        public void Read_UnknownProperty_IsSkipped()
        {
            var json = """{"format":0,"value":"x","type":"","extra":{"nested":true}}""";
            var payload = Deserialize(json);

            Assert.NotNull(payload);
            Assert.Equal("x", payload!.Value);
        }

        [Fact]
        [DisplayName("Write 於 null payload 應輸出 null")]
        public void Write_NullPayload_WritesNull()
        {
            JsonRpcParams? payload = null;
            var json = JsonSerializer.Serialize(payload);
            Assert.Equal("null", json);
        }

        [Fact]
        [DisplayName("Write 於 Value 為 null 應輸出 value=null")]
        public void Write_NullValue_WritesNullValue()
        {
            var payload = new JsonRpcParams { Value = null, TypeName = "" };
            var json = Serialize(payload);
            Assert.Contains("\"value\":null", json);
            Assert.Contains("\"format\":0", json);
        }

        [Fact]
        [DisplayName("Write 於 Value 為字串應輸出 JSON 字串")]
        public void Write_StringValue_WritesStringValue()
        {
            var payload = new JsonRpcParams { Value = "hello", TypeName = "System.String" };
            var json = Serialize(payload);
            Assert.Contains("\"value\":\"hello\"", json);
            Assert.Contains("\"type\":\"System.String\"", json);
        }

        [Fact]
        [DisplayName("Read/Write round-trip 應保留字串 value")]
        public void ReadWrite_RoundTrip_PreservesStringValue()
        {
            var original = new JsonRpcParams { Value = "round-trip", TypeName = "" };
            var json = Serialize(original);
            var restored = Deserialize(json);

            Assert.NotNull(restored);
            Assert.Equal("round-trip", restored!.Value);
        }

        [Fact]
        [DisplayName("ApiPayloadJsonConverterFactory 可轉換具體 ApiPayload 子類")]
        public void Factory_CanConvert_ConcreteSubtype()
        {
            var factory = new ApiPayloadJsonConverterFactory();
            Assert.True(factory.CanConvert(typeof(JsonRpcParams)));
            Assert.True(factory.CanConvert(typeof(JsonRpcResult)));
        }

        [Fact]
        [DisplayName("ApiPayloadJsonConverterFactory 不應可轉換抽象 ApiPayload 或無關型別")]
        public void Factory_CanConvert_RejectsAbstractOrUnrelated()
        {
            var factory = new ApiPayloadJsonConverterFactory();
            Assert.False(factory.CanConvert(typeof(ApiPayload)));
            Assert.False(factory.CanConvert(typeof(string)));
        }

        [Fact]
        [DisplayName("ApiPayloadJsonConverterFactory.CreateConverter 應回傳對應泛型 converter")]
        public void Factory_CreateConverter_ReturnsGenericConverter()
        {
            var factory = new ApiPayloadJsonConverterFactory();
            var converter = factory.CreateConverter(typeof(JsonRpcParams), new JsonSerializerOptions());

            Assert.NotNull(converter);
            Assert.IsType<ApiPayloadJsonConverter<JsonRpcParams>>(converter);
        }
    }
}
