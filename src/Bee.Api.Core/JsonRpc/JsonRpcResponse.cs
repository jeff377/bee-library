using Bee.Core;
using Bee.Core.Serialization;
using Newtonsoft.Json;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// JSON-RPC response model.
    /// </summary>
    public class JsonRpcResponse : IObjectSerialize
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcResponse"/> class.
        /// </summary>
        public JsonRpcResponse()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcResponse"/> class based on the specified request.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        public JsonRpcResponse(JsonRpcRequest request)
        {
            Method = request.Method; // Echo back the invoked method name
            Id = request.Id;  // Set the unique identifier from the request
        }

        #endregion

        #region IObjectSerialize 介面

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [JsonIgnore]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(Result, serializeState);
        }

        #endregion

        /// <summary>
        /// Gets or sets the JSON-RPC version.
        /// </summary>
        [JsonProperty("jsonrpc", NullValueHandling = NullValueHandling.Include)]
        public string Jsonrpc { get; set; } = "2.0";

        /// <summary>
        /// Gets or sets the name of the invoked method.
        /// </summary>
        [JsonProperty("method", NullValueHandling = NullValueHandling.Include)]
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the method execution result.
        /// </summary>
        [JsonProperty("result")]
        public JsonRpcResult Result { get; set; }

        /// <summary>
        /// Gets or sets the error information.
        /// </summary>
        [JsonProperty("error")]
        public JsonRpcError Error { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the request.
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Include)]
        public string Id { get; set; }
    }
}
