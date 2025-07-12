
using System.Threading.Tasks;
using Bee.Api.Core;

namespace Bee.Connect
{
    /// <summary>
    /// JSON-RPC 服務提供者介面。
    /// </summary>
    public interface IJsonRpcProvider
    {
        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        JsonRpcResponse Execute(JsonRpcRequest request);

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request);
    }
}
