using System.Text.Json.Serialization;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Represents the return result of a JSON-RPC method invocation.
    /// </summary>
    [JsonConverter(typeof(ApiPayloadJsonConverter<JsonRpcResult>))]
    public class JsonRpcResult : ApiPayload
    {
    }
}
