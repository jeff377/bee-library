using System;
using System.Threading.Tasks;
using Bee.Api.Core;

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
        public JsonRpcResponse Execute(JsonRpcRequest request)
        {
            // 執行 API 方法
            var executor = new JsonRpcExecutor(AccessToken, true);
            var response = executor.Execute(request);
            return response;
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public async Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
        {
            // 執行 API 方法
            var executor = new JsonRpcExecutor(AccessToken, true);
            var response = await executor.ExecuteAsync(request);
            return response;
        }
    }
}
