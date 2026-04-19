namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Represents an exception that occurs while processing a JSON-RPC request.
    /// </summary>
    public class JsonRpcException : Exception
    {
        /// <summary>
        /// Gets the HTTP error status code.
        /// </summary>
        public int HttpStatusCode { get; }

        /// <summary>
        /// Gets the JSON-RPC error code.
        /// </summary>
        public JsonRpcErrorCode ErrorCode { get; }

        /// <summary>
        /// Gets the JSON-RPC error message.
        /// </summary>
        public string RpcMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcException"/> class with the specified HTTP status code, error code, and error message.
        /// </summary>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="errorCode">The JSON-RPC error code.</param>
        /// <param name="rpcMessage">The JSON-RPC error message.</param>
        public JsonRpcException(int httpStatusCode, JsonRpcErrorCode errorCode, string rpcMessage)
            : base(rpcMessage)
        {
            HttpStatusCode = httpStatusCode;
            ErrorCode = errorCode;
            RpcMessage = rpcMessage;
        }
    }
}
