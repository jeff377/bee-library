using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

            var json = JsonConvert.SerializeObject(request);
            var deserialized = JsonConvert.DeserializeObject<JsonRpcRequest>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("2.0", deserialized.Jsonrpc);
            Assert.Equal("system.ping", deserialized.Method);
            Assert.Equal("req-001", deserialized.Id);
            Assert.NotNull(deserialized.Params);
            Assert.Equal("test-payload", deserialized.Params.Value);
            Assert.Equal("System.String", deserialized.Params.TypeName);

            // 驗證 SerializeState 不會出現在 JSON 中（標記了 [JsonIgnore]）
            var jObj = JObject.Parse(json);
            Assert.Null(jObj["SerializeState"]);
            Assert.Null(jObj["serializeState"]);
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

            var json = JsonConvert.SerializeObject(response);
            var deserialized = JsonConvert.DeserializeObject<JsonRpcResponse>(json);

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

            var json = JsonConvert.SerializeObject(response);
            var deserialized = JsonConvert.DeserializeObject<JsonRpcResponse>(json);

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

            var json = JsonConvert.SerializeObject(error);
            var deserialized = JsonConvert.DeserializeObject<JsonRpcError>(json);

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
            var payload = new JsonRpcParams
            {
                Value = "sample-data",
                TypeName = "Bee.Api.Core.System.PingRequest"
            };
            // Format 為 internal set，需透過 JSON 反序列化間接設定
            var tempJson = JsonConvert.SerializeObject(new { format = (int)format, value = "sample-data", type = "Bee.Api.Core.System.PingRequest" });
            payload = JsonConvert.DeserializeObject<JsonRpcParams>(tempJson)!;

            var json = JsonConvert.SerializeObject(payload);
            var deserialized = JsonConvert.DeserializeObject<JsonRpcParams>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(format, deserialized.Format);
            Assert.Equal("sample-data", deserialized.Value);
            Assert.Equal("Bee.Api.Core.System.PingRequest", deserialized.TypeName);

            // 驗證 format 序列化為數值
            var jObj = JObject.Parse(json);
            Assert.Equal((int)format, jObj["format"]!.Value<int>());
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

            var request = JsonConvert.DeserializeObject<JsonRpcRequest>(json);

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

            var json = JsonConvert.SerializeObject(response);
            var jObj = JObject.Parse(json);

            // 驗證 JSON key 名稱為小寫（符合 JSON-RPC 2.0 規範）
            Assert.NotNull(jObj["jsonrpc"]);
            Assert.NotNull(jObj["method"]);
            Assert.NotNull(jObj["id"]);
            Assert.NotNull(jObj["result"]);

            // 驗證 result 內部 key 名稱
            var resultObj = jObj["result"] as JObject;
            Assert.NotNull(resultObj);
            Assert.NotNull(resultObj["format"]);
            Assert.NotNull(resultObj["value"]);
            Assert.NotNull(resultObj["type"]);

            // 驗證不應出現 PascalCase key
            Assert.Null(jObj["Jsonrpc"]);
            Assert.Null(jObj["Method"]);
            Assert.Null(jObj["Id"]);
            Assert.Null(jObj["Result"]);
        }
    }
}
