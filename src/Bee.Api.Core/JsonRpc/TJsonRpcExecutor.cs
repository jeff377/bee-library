using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 執行器。
    /// </summary>
    public class TJsonRpcExecutor
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public TJsonRpcExecutor(Guid accessToken)
        {
            AccessToken = accessToken;
        }

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public TJsonRpcResponse Execute(TJsonRpcRequest request)
        {
            var response = new TJsonRpcResponse(request);
            try
            {
                // 傳輸資料是否加密
                bool encrypted = request.Params.Encrypted;
                // 傳入參數進行解密
                if (encrypted)
                    request.Decrypt();

                // 從 Method 屬性解析出 ProgID 與 Action
                var (progID, action) = ParseMethod(request.Method);
                // 建立商業邏輯物件，執行指定方法
                var value = ExecuteMethod(progID, action, request.Params.Value);

                // 傳出結果
                response.Result = new TJsonRpcResult()
                {
                    Value = value
                };
                // 傳出結果進行加密
                if (encrypted)
                    response.Encrypt();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    response.Error = new TJsonRpcError(-1, ex.InnerException.Message);
                else
                    response.Error = new TJsonRpcError(-1, ex.Message);
            }
            return response;
        }

        /// <summary>
        /// 從 Method 屬性解析出 ProgID 與 Action。
        /// </summary>
        /// <returns>Tuple，包含 ProgID 與 Action。若格式錯誤則回傳空字串。</returns>
        public (string progID, string action) ParseMethod(string method)
        {
            if (!string.IsNullOrEmpty(method))
            {
                var parts = method.Split(new[] { '.' }, 2);
                if (parts.Length == 2)
                {
                    return (parts[0], parts[1]);
                }
            }
            throw new FormatException($"Invalid method format: {method}");
        }

        /// <summary>
        /// 建立商業邏輯物件，執行指定方法。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">執行動作的傳入引數。</param>
        public object ExecuteMethod(string progID, string action, object value)
        {
            var businessObject = ResolveBusinessObject(progID);
            var method = businessObject.GetType().GetMethod(action);
            if (method == null)
                throw new MissingMethodException($"Method '{action}' not found in business object '{progID}'.");
            return method.Invoke(businessObject, new object[] { value });
        }

        /// <summary>
        /// 解析並取得實體商業邏輯物件。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        private object ResolveBusinessObject(string progID)
        {
            if (string.IsNullOrWhiteSpace(progID))
                throw new ArgumentException("ProgID cannot be null or empty.", nameof(progID));

            if (StrFunc.IsEquals(progID, SysProgIDs.System))
                return BackendInfo.BusinessObjectProvider.CreateSystemObject(AccessToken);
            else
                return BackendInfo.BusinessObjectProvider.CreateBusinessObject(AccessToken, progID);
        }
    }
}
