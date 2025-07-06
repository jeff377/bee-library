using System;
using System.Threading.Tasks;
using Bee.Api.Core;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// 近端 API 服務提供者（在同一進程內直接存取後端業務邏輯）。
    /// </summary>
    public class LocalApiServiceProvider : IJsonRpcProvider
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public LocalApiServiceProvider(Guid accessToken)
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
        /// <param name="enableEncoding">是否啟用資料編碼（序列化、壓縮與加密）。</param>
        public JsonRpcResponse Execute(JsonRpcRequest request, bool enableEncoding)
        {
            // 檢查是否啟用編碼，近端連線只有在偵錯模式下才進行編碼
            bool useEncoding = enableEncoding && SysInfo.IsDebugMode;

            // 傳入資料進行編碼
            if (useEncoding) { request.Encode(FrontendInfo.ApiEncryptionKey); }

            // 執行 API 方法
            var executor = new JsonRpcExecutor(AccessToken, true);
            var response = executor.Execute(request);

            // 傳出結果進行解碼
            if (useEncoding) { response.Decode(FrontendInfo.ApiEncryptionKey); }
            return response;
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        /// <param name="enableEncoding">是否啟用資料編碼（序列化、壓縮與加密）。</param>
        public async Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request, bool enableEncoding)
        {
            // 檢查是否啟用編碼，近端連線只有在偵錯模式下才進行編碼
            bool useEncoding = enableEncoding && SysInfo.IsDebugMode;

            // 傳入資料進行編碼
            if (useEncoding) { request.Encode(FrontendInfo.ApiEncryptionKey); }

            // 執行 API 方法
            var executor = new JsonRpcExecutor(AccessToken);
            var response = await executor.ExecuteAsync(request);

            // 傳出結果進行解碼
            if (useEncoding) { response.Decode(FrontendInfo.ApiEncryptionKey); }
            return response;
        }
    }
}
