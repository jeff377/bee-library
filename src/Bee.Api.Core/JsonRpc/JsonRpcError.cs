using System.Text.Json.Serialization;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// JSON-RPC error model.
    /// </summary>
    public class JsonRpcError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcError"/> class.
        /// </summary>
        public JsonRpcError()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcError"/> class.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="data">Additional error information.</param>
        public JsonRpcError(int code, string message, object? data = null)
        {
            Code = code;
            Message = message;
            Data = data;
        }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional error information.
        /// </summary>
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}
