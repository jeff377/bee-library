using System;
using System.Threading.Tasks;
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
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        public TJsonRpcExecutor(Guid accessToken, bool isLocalCall =false)
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
        public TJsonRpcResponse Execute(TJsonRpcRequest request)
        {
            return ExecuteAsyncCore(request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public Task<TJsonRpcResponse> ExecuteAsync(TJsonRpcRequest request)
        {
            return ExecuteAsyncCore(request);
        }

        /// <summary>
        /// 內部非同步執行核心邏輯。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        private async Task<TJsonRpcResponse> ExecuteAsyncCore(TJsonRpcRequest request)
        {
            var response = new TJsonRpcResponse(request);
            try
            {
                // 傳輸資料是否進行編碼
                bool isEncoded = request.Params.IsEncoded;
                // 若為編碼狀態，則進行解碼
                if (isEncoded) { request.Decode(); }

                // 從 Method 屬性解析出 ProgId 與 Action
                var (progId, action) = ParseMethod(request.Method);
                // 建立業務邏輯物件，執行指定方法
                var value = await ExecuteMethodAsync(progId, action, request.Params.Value, isEncoded);

                // 傳出結果
                response.Result = new TJsonRpcResult { Value = value };
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
        /// 從 Method 屬性解析出 progId 與 Action。
        /// </summary>
        /// <returns>Tuple，包含 progId 與 Action。若格式錯誤則回傳空字串。</returns>
        public (string progId, string action) ParseMethod(string method)
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
        /// <param name="isEncoded">傳輸資料是否進行編碼。</param>
        public async Task<object> ExecuteMethodAsync(string progId, string action, object value, bool isEncoded)
        {
            // 建立指定 progId 的業務邏輯物件實例
            var businessObject = CreateBusinessObject(AccessToken, progId);
            var method = businessObject.GetType().GetMethod(action);
            if (method == null)
                throw new MissingMethodException($"Method '{action}' not found in business object '{progId}'.");

            // 存取驗證
            ApiAccessValidator.ValidateAccess(method, new TApiCallContext(IsLocalCall, isEncoded));

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
        public object CreateBusinessObject(Guid accessToken, string progId)
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
