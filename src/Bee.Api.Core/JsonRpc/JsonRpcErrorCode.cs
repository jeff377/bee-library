
namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Defines standard JSON-RPC error codes used to indicate error conditions during request processing.
    /// </summary>
    public enum JsonRpcErrorCode
    {
        /// <summary>
        /// Invalid JSON was received by the server, typically due to malformed syntax (-32700).
        /// </summary>
        ParseError = -32700,

        /// <summary>
        /// The JSON request is not a valid request object, possibly due to missing required fields or structural errors (-32600).
        /// </summary>
        InvalidRequest = -32600,

        /// <summary>
        /// The method does not exist or is not available (-32601).
        /// </summary>
        MethodNotFound = -32601,

        /// <summary>
        /// Invalid method parameters or incorrect format (-32602).
        /// </summary>
        InvalidParams = -32602,

        /// <summary>
        /// An internal server error occurred that prevented the request from being completed (-32000).
        /// </summary>
        InternalError = -32000,

        /// <summary>
        /// Unauthorized access, typically due to credential validation failure (-32001).
        /// </summary>
        Unauthorized = -32001
    }
}
