
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
        Unauthorized = -32001,

        /// <summary>
        /// The session has no company context (EnterCompany was not called or LeaveCompany has cleared it),
        /// but the requested operation requires one (-32002). Maps to HTTP 409 Conflict.
        /// </summary>
        CompanyNotEntered = -32002,

        /// <summary>
        /// The caller cannot enter the requested company because the company does not exist
        /// or the user has no permission to access it (-32003). Maps to HTTP 403 Forbidden.
        /// </summary>
        /// <remarks>
        /// The two cases are intentionally merged into a single error code to prevent
        /// anonymous enumeration of valid company ids via error-code differences.
        /// </remarks>
        CompanyAccessDenied = -32003
    }
}
