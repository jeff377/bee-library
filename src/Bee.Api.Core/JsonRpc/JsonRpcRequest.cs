using System;
using Bee.Base;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// JSON-RPC request model.
    /// </summary>
    [Serializable]
    public class JsonRpcRequest : IObjectSerialize
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcRequest"/> class.
        /// </summary>
        public JsonRpcRequest()
        { }

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
            BaseFunc.SetSerializeState(Params, serializeState);
        }

        #endregion

        /// <summary>
        /// Gets or sets the JSON-RPC version.
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

        /// <summary>
        /// Gets or sets the name of the method to invoke.
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the method parameters.
        /// </summary>
        [JsonPropertyName("params")]
        public JsonRpcParams Params { get; set; } = new JsonRpcParams();

        /// <summary>
        /// Gets or sets the unique identifier for the request.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
