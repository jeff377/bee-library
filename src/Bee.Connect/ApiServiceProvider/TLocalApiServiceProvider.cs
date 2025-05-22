using System;
using System.Threading.Tasks;
using Bee.Api.Core;

namespace Bee.Connect
{
    /// <summary>
    /// 近端 API 服務提供者（在同一進程內直接存取後端業務邏輯）。
    /// </summary>
    public class TLocalApiServiceProvider : IJsonRpcProvider
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public TLocalApiServiceProvider(Guid accessToken)
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
        public TJsonRpcResponse Execute(TJsonRpcRequest request, bool enableEncoding)
        {
            // 傳入資料進行編碼
            if (enableEncoding) { request.Encode(); }

            // 執行 API 方法
            var executor = new TJsonRpcExecutor(AccessToken);
            var response = executor.Execute(request);

            // 傳出結果進行解碼
            if (enableEncoding) { response.Decode(); }
            return response;
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        /// <param name="enableEncoding">是否啟用資料編碼（序列化、壓縮與加密）。</param>
        public async Task<TJsonRpcResponse> ExecuteAsync(TJsonRpcRequest request, bool enableEncoding)
        {
            // 傳入資料進行編碼
            if (enableEncoding) { request.Encode(); }

            // 執行 API 方法
            var executor = new TJsonRpcExecutor(AccessToken);
            var response = await executor.ExecuteAsync(request);

            // 傳出結果進行解碼
            if (enableEncoding) { response.Decode(); }
            return response;
        }
    }
}
