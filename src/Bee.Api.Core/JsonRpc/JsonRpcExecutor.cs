using System;
using System.Threading.Tasks;
using Bee.Define;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 請求執行器。
    /// </summary>
    public class JsonRpcExecutor
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        public JsonRpcExecutor(Guid accessToken, bool isLocalCall = false)
        {
            AccessToken = accessToken;
            IsLocalCall = isLocalCall;
        }

        /// <summary>
        /// 存取令牌，用於識別目前使用者或工作階段。
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 呼叫是否為近端來源（例如與伺服器同一進程或主機）。
        /// </summary>
        public bool IsLocalCall { get; set; } = false;

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public JsonRpcResponse Execute(JsonRpcRequest request)
        {
            return ExecuteAsyncCore(request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
        {
            return ExecuteAsyncCore(request);
        }

        /// <summary>
        /// 內部非同步執行核心邏輯。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        private async Task<JsonRpcResponse> ExecuteAsyncCore(JsonRpcRequest request)
        {
            var response = new JsonRpcResponse(request);
            try
            {
                // 傳輸資料格式
                var format = request.Params.Format;
                // 取得 API 加密金鑰
                byte[] apiEncryptionKey = GetApiEncryptionKey(format);
                // 還原請求資料內容
                ApiPayloadConverter.RestoreFrom(request.Params, format, apiEncryptionKey);

                // 從 Method 屬性解析出 ProgId 與 Action
                var (progId, action) = ParseMethod(request.Method);
                // 建立業務邏輯物件，執行指定方法
                var value = await ExecuteMethodAsync(progId, action, request.Params.Value, format);

                // 傳出結果
                response.Result = new JsonRpcResult { Value = value };
                // 設定回應的資料格式
                ApiPayloadConverter.TransformTo(response.Result, format, apiEncryptionKey);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    response.Error = new JsonRpcError(-1, ex.InnerException.Message);
                else
                    response.Error = new JsonRpcError(-1, ex.Message);
            }
            return response;
        }

        /// <summary>
        /// 取得 API 加密金鑰。
        /// </summary>
        /// <param name="format">傳輸資料的封裝格式。</param>
        private byte[] GetApiEncryptionKey(PayloadFormat format)
        {
            return format == PayloadFormat.Encrypted
                ? BackendInfo.ApiEncryptionKeyProvider.GetKey(Guid.Empty)  // 未實作 Session-based API key，目前使用共用金鑰
                : null;
        }


        /// <summary>
        /// 從 Method 屬性解析出 progId 與 Action。
        /// </summary>
        /// <returns>Tuple，包含 progId 與 Action。若格式錯誤則回傳空字串。</returns>
        private (string progId, string action) ParseMethod(string method)
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
        /// 建立業務邏輯物件，非同步執行指定方法。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">執行動作的傳入引數。</param>
        /// <param name="format">傳輸資料的封裝格式。</param>
        private async Task<object> ExecuteMethodAsync(string progId, string action, object value, PayloadFormat format)
        {
            // 建立指定 progId 的業務邏輯物件實例
            var businessObject = CreateBusinessObject(AccessToken, progId);
            var method = businessObject.GetType().GetMethod(action);
            if (method == null)
                throw new MissingMethodException($"Method '{action}' not found in business object '{progId}'.");

            // 存取驗證
            ApiAccessValidator.ValidateAccess(method, new ApiCallContext(IsLocalCall, format));

            var result = method.Invoke(businessObject, new object[] { value });

            // 若方法為非同步方法（Task 或 Task<T>），則進行 await
            if (result is Task task)
            {
                // 等待該非同步任務完成，（避免死鎖，在後端環境推薦使用）
                await task.ConfigureAwait(false);
                // 若為 Task<T> 則取出 Result；否則為 Task (void)，回傳 null
                var taskType = task.GetType();
                var isGeneric = taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>);
                return isGeneric
                    ? taskType.GetProperty("Result")?.GetValue(task)
                    : null;
            }

            return result;
        }

        /// <summary>
        /// 建立指定 progId 的業務邏輯物件實例。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        /// <returns>業務邏輯物件實例。</returns>
        private object CreateBusinessObject(Guid accessToken, string progId)
        {
            if (string.IsNullOrWhiteSpace(progId))
                throw new ArgumentException("ProgId cannot be null or empty.", nameof(progId));

            if (progId == SysProgIds.System)
                return BackendInfo.BusinessObjectProvider.CreateSystemBusinessObject(accessToken);
            else
                return BackendInfo.BusinessObjectProvider.CreateFormBusinessObject(accessToken, progId);
        }
    }

}
