using System;
using System.Threading.Tasks;
using Bee.Api.Core;

namespace Bee.Connect
{
    /// <summary>
    /// 近端 API 服務提供者（在同一進程內直接存取後端商業邏輯）。
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
        public TJsonRpcResponse Execute(TJsonRpcRequest request)
        {
            // 註1：開發階段使用近端連線，簡化運行環境及方便偵錯；運行階段則使用遠端連線
            // 註2：近端連線傳遞資料進行編碼，是為了驗證開發階段傳遞的資料型別都能正常序列化

            // 傳入資料進行編碼
            request.Encode();
            // 執行 API 方法
            var executor = new TJsonRpcExecutor(AccessToken);
            var response = executor.Execute(request);
            // 傳出結果進行解碼
            response.Decode();
            return response;
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public async Task<TJsonRpcResponse> ExecuteAsync(TJsonRpcRequest request)
        {
            // 傳入資料進行編碼
            request.Encode();
            // 執行 API 方法
            var executor = new TJsonRpcExecutor(AccessToken);
            var response = await executor.ExecuteAsync(request);
            // 傳出結果進行解碼
            response.Decode();
            return response;
        }
    }
}
