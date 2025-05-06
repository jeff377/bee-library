using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 表示處理 JSON-RPC 請求時所發生的例外狀況。
    /// </summary>
    public class JsonRpcException : Exception
    {
        /// <summary>
        /// 取得 HTTP 錯誤狀態碼。
        /// </summary>
        public int HttpStatusCode { get; }

        /// <summary>
        /// 取得 JSON-RPC 錯誤代碼。
        /// </summary>
        public EJsonRpcErrorCode ErrorCode { get; }

        /// <summary>
        /// 取得 JSON-RPC 錯誤訊息。
        /// </summary>
        public string RpcMessage { get; }

        /// <summary>
        /// 使用指定的錯誤狀態碼、錯誤代碼與錯誤訊息，初始化 <see cref="JsonRpcException"/> 類別的新執行個體。
        /// </summary>
        /// <param name="httpStatusCode">HTTP 狀態碼。</param>
        /// <param name="errorCode">JSON-RPC 錯誤代碼。</param>
        /// <param name="rpcMessage">JSON-RPC 錯誤訊息。</param>
        public JsonRpcException(int httpStatusCode, EJsonRpcErrorCode errorCode, string rpcMessage)
            : base(rpcMessage)
        {
            HttpStatusCode = httpStatusCode;
            ErrorCode = errorCode;
            RpcMessage = rpcMessage;
        }
    }
}
