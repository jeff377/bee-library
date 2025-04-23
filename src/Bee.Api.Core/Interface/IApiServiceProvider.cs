
using System.Threading.Tasks;

namespace Bee.Api.Core
{
    /// <summary>
    /// API 服務提供者介面。
    /// </summary>
    public interface IApiServiceProvider
    {
        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        TJsonRpcResponse Execute(TJsonRpcRequest request);

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        Task<string> ExecuteAsync(TJsonRpcRequest request);
    }
}
