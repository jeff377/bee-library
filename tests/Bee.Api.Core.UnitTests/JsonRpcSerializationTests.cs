using System.ComponentModel;
using System.Text.Json;
using Bee.Api.Core.JsonRpc;
using Bee.Base.Serialization;
using Bee.Definition;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// JSON-RPC 模型的 JSON 序列化測試，確保前後端 JSON 傳輸格式正確。
    /// </summary>
    public class JsonRpcSerializationTests
    {
        /// <summary>
        /// 測試 JsonRpcRequest 序列化後保留所有屬性，且 JSON key 名稱符合 JSON-RPC 2.0 規範。
        /// </summary>
        [Fact]
        [DisplayName("JsonRpcRequest JSON 序列化保留所有屬性")]
        public void JsonRpcRequest_Serialize_PreservesAllProperties()
        {
            var request = new JsonRpcRequest
            {
                Jsonrpc = "2.0",
                Method = "system.ping",
                Id = "req-001"
            };
            request.Params.Value = "test-payload";
            request.Params.TypeName = "System.String";

            var json = JsonSerializer.Serialize(request);
            var deserialized = JsonSerializer.Deserialize<JsonRpcRequest>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("2.0", deserialized.Jsonrpc);
            Assert.Equal("system.ping", deserialized.Method);
            Assert.Equal("req-001", deserialized.Id);
            Assert.NotNull(deserialized.Params);
            Assert.Equal("test-payload", deserialized.Params.Value);
            Assert.Equal("System.String", deserialized.Params.TypeName);

            // 驗證 SerializeState 不會出現在 JSON 中（標記了 [JsonIgnore]）
            using var jDoc = JsonDocument.Parse(json);
            var root = jDoc.RootElement;
            Assert.False(root.TryGetProperty("SerializeState", out _));
            Assert.False(root.TryGetProperty("serializeState", out _));
        }

        /// <summary>
        /// 測試 JsonRpcResponse 成功回應的序列化。
        /// </summary>
        [Fact]
        [DisplayName("JsonRpcResponse 成功回應 JSON 序列化")]
        public void JsonRpcResponse_Success_Serialize()
        {
            var result = new JsonRpcResult
            {
                Value = "pong",
                TypeName = "System.String"
            };
            var response = new JsonRpcResponse
            {
                Jsonrpc = "2.0",
                Method = "system.ping",
                Id = "req-001",
                Result = result,
                Error = null
            };

            var json = JsonSerializer.Serialize(response);
            var deserialized = JsonSerializer.Deserialize<JsonRpcResponse>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("2.0", deserialized.Jsonrpc);
            Assert.Equal("system.ping", deserialized.Method);
            Assert.Equal("req-001", deserialized.Id);
            Assert.NotNull(deserialized.Result);
            Assert.Equal("pong", deserialized.Result.Value);
            Assert.Equal("System.String", deserialized.Result.TypeName);
            Assert.Null(deserialized.Error);
        }

        /// <summary>
        /// 測試 JsonRpcResponse 錯誤回應的序列化，含 JsonRpcErrorCode 數值驗證。
        /// </summary>
        [Fact]
        [DisplayName("JsonRpcResponse 錯誤回應 JSON 序列化")]
        public void JsonRpcResponse_Error_Serialize()
        {
            var response = new JsonRpcResponse
            {
                Jsonrpc = "2.0",
                Method = "system.login",
                Id = "req-002",
                Result = null,
                Error = new JsonRpcError(
                    (int)JsonRpcErrorCode.Unauthorized,
                    "Access denied",
                    new { detail = "Invalid token" })
            };

            var json = JsonSerializer.Serialize(response);
            var deserialized = JsonSerializer.Deserialize<JsonRpcResponse>(json);

            Assert.NotNull(deserialized);
            Assert.Null(deserialized.Result);
            Assert.NotNull(deserialized.Error);
            Assert.Equal((int)JsonRpcErrorCode.Unauthorized, deserialized.Error.Code);
            Assert.Equal(-32001, deserialized.Error.Code);
            Assert.Equal("Access denied", deserialized.Error.Message);
            Assert.NotNull(deserialized.Error.Data);
        }

        /// <summary>
        /// 測試 JsonRpcError 所有屬性的 round-trip。
        /// </summary>
        [Fact]
        [DisplayName("JsonRpcError JSON 序列化保留所有屬性")]
        public void JsonRpcError_Serialize_PreservesAllProperties()
        {
            var error = new JsonRpcError(
                (int)JsonRpcErrorCode.ParseError,
                "Parse error",
                "Unexpected token at position 42");

            var json = JsonSerializer.Serialize(error);
            var deserialized = JsonSerializer.Deserialize<JsonRpcError>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(-32700, deserialized.Code);
            Assert.Equal("Parse error", deserialized.Message);
            Assert.Equal("Unexpected token at position 42", deserialized.Data?.ToString());
        }

        /// <summary>
        /// 測試 ApiPayload（透過 JsonRpcParams / JsonRpcResult）的 format、value、type 屬性序列化。
        /// </summary>
        [Theory]
        [InlineData(PayloadFormat.Plain)]
        [InlineData(PayloadFormat.Encoded)]
        [InlineData(PayloadFormat.Encrypted)]
        [DisplayName("ApiPayload JSON 序列化保留 Format 和 TypeName")]
        public void ApiPayload_Serialize_PreservesFormatAndTypeName(PayloadFormat format)
        {
            // Create payload with the specified format via JSON round-trip
            var tempJson = JsonSerializer.Serialize(new { format = (int)format, value = "sample-data", type = "Bee.Api.Core.System.PingRequest" });
            var payload = JsonSerializer.Deserialize<JsonRpcParams>(tempJson)!;

            var json = JsonSerializer.Serialize(payload);
            var deserialized = JsonSerializer.Deserialize<JsonRpcParams>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(format, deserialized.Format);
            Assert.Equal("sample-data", deserialized.Value);
            Assert.Equal("Bee.Api.Core.System.PingRequest", deserialized.TypeName);

            // 驗證 format 序列化為數值
            using var jDoc = JsonDocument.Parse(json);
            var root = jDoc.RootElement;
            Assert.Equal((int)format, root.GetProperty("format").GetInt32());
        }

        /// <summary>
        /// 模擬前端送出的 JSON 字串，驗證能正確反序列化為 JsonRpcRequest。
        /// </summary>
        [Fact]
        [DisplayName("前端 JSON 字串反序列化為 JsonRpcRequest")]
        public void JsonRpcRequest_FromJsonString_Deserialize()
        {
            // 模擬前端送出的 JSON-RPC 請求
            const string json = """
                {
                    "jsonrpc": "2.0",
                    "method": "system.execFunc",
                    "params": {
                        "format": 1,
                        "value": "base64-encoded-payload",
                        "type": "Bee.Api.Core.System.ExecFuncRequest"
                    },
                    "id": "client-req-42"
                }
                """;

            var request = JsonSerializer.Deserialize<JsonRpcRequest>(json);

            Assert.NotNull(request);
            Assert.Equal("2.0", request.Jsonrpc);
            Assert.Equal("system.execFunc", request.Method);
            Assert.Equal("client-req-42", request.Id);
            Assert.NotNull(request.Params);
            Assert.Equal(PayloadFormat.Encoded, request.Params.Format);
            Assert.Equal("base64-encoded-payload", request.Params.Value);
            Assert.Equal("Bee.Api.Core.System.ExecFuncRequest", request.Params.TypeName);
        }

        /// <summary>
        /// 驗證 JsonRpcResponse 序列化後的 JSON key 名稱符合 JSON-RPC 2.0 規範。
        /// </summary>
        [Fact]
        [DisplayName("JsonRpcResponse 序列化 JSON key 名稱符合 JSON-RPC 2.0 規範")]
        public void JsonRpcResponse_ToJsonString_MatchesExpectedFormat()
        {
            var response = new JsonRpcResponse
            {
                Jsonrpc = "2.0",
                Method = "system.ping",
                Id = "req-100",
                Result = new JsonRpcResult
                {
                    Value = "result-data",
                    TypeName = "Bee.Api.Core.System.PingResponse"
                }
            };

            var json = JsonSerializer.Serialize(response);
            using var jDoc = JsonDocument.Parse(json);
            var root = jDoc.RootElement;

            // 驗證 JSON key 名稱為小寫（符合 JSON-RPC 2.0 規範）
            Assert.True(root.TryGetProperty("jsonrpc", out _));
            Assert.True(root.TryGetProperty("method", out _));
            Assert.True(root.TryGetProperty("id", out _));
            Assert.True(root.TryGetProperty("result", out _));

            // 驗證 result 內部 key 名稱
            var resultObj = root.GetProperty("result");
            Assert.True(resultObj.TryGetProperty("format", out _));
            Assert.True(resultObj.TryGetProperty("value", out _));
            Assert.True(resultObj.TryGetProperty("type", out _));

            // 驗證不應出現 PascalCase key
            Assert.False(root.TryGetProperty("Jsonrpc", out _));
            Assert.False(root.TryGetProperty("Method", out _));
            Assert.False(root.TryGetProperty("Id", out _));
            Assert.False(root.TryGetProperty("Result", out _));
        }

        /// <summary>
        /// JSON-RPC 請求模型序列化。
        /// </summary>
        [Fact]
        [DisplayName("JsonRpcRequest 序列化應產生有效 JSON 並支援編碼與解碼")]
        public void JsonRpcRequest_Serialize_ReturnsValidJson()
        {
            var request = new JsonRpcRequest()
            {
                Method = $"{SysProgIds.System}.ExecFunc",
                Params = new JsonRpcParams()
                {
                    Value = new ExecFuncRequest("Hello")
                },
                Id = Guid.NewGuid().ToString()
            };
            string json = request.ToJson();
            Assert.NotEmpty(json);

            // 測試編碼
            ApiPayloadConverter.TransformTo(request.Params, PayloadFormat.Encoded);
            string encodedJson = request.ToJson();
            Assert.NotEmpty(encodedJson);

            // 測試解碼
            ApiPayloadConverter.RestoreFrom(request.Params, PayloadFormat.Encoded);
            string decodedJson = request.ToJson();
            Assert.NotEmpty(decodedJson);
        }
    }
}
