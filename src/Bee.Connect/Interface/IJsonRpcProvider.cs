
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
        /// <param name="enableEncoding">是否啟用資料編碼（序列化、壓縮與加密）。</param>
        TJsonRpcResponse Execute(TJsonRpcRequest request, bool enableEncoding);

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        /// <param name="enableEncoding">是否啟用資料編碼（序列化、壓縮與加密）。</param>
        Task<TJsonRpcResponse> ExecuteAsync(TJsonRpcRequest request, bool enableEncoding);
    }
}
