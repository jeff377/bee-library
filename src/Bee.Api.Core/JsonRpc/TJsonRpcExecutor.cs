using System;
using System.Threading.Tasks;
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
        /// 存取令牌，用於識別目前使用者或工作階段。
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
                // 傳輸資料是否進行編碼
                bool isEncoded = request.Params.IsEncoded;
                // 若為編碼狀態，則進行解碼
                if (isEncoded) { request.Decode(); }

                // 從 Method 屬性解析出 ProgID 與 Action
                var (progID, action) = ParseMethod(request.Method);
                // 建立商業邏輯物件，執行指定方法
                var value = ExecuteMethod(progID, action, request.Params.Value);

                // 傳出結果
                response.Result = new TJsonRpcResult() { Value = value };
                // 若傳出結果需要編碼，則進行編碼
                if (isEncoded) { response.Encode(); }
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
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public Task<TJsonRpcResponse> ExecuteAsync(TJsonRpcRequest request)
        {
            return Task.FromResult(Execute(request));
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
            // 建立指定 progID 的商業邏輯物件實例
            var businessObject = ApiServiceOptions.BusinessObjectResolver.CreateBusinessObject(AccessToken, progID);
            var method = businessObject.GetType().GetMethod(action);
            if (method == null)
                throw new MissingMethodException($"Method '{action}' not found in business object '{progID}'.");
            return method.Invoke(businessObject, new object[] { value });
        }

    }
}
