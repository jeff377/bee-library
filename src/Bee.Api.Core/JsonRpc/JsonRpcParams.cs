using System.Text.Json.Serialization;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Represents the input parameters for a JSON-RPC method invocation.
    /// </summary>
    [JsonConverter(typeof(ApiPayloadJsonConverter<JsonRpcParams>))]
    public class JsonRpcParams : ApiPayload
    {
    }
}
