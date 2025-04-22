
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
        /// <param name="args">傳入參數。</param>
        TApiServiceResult Execute(TApiServiceArgs args);

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="args">傳入參數。</param>
        Task<string> ExecuteAsync(TApiServiceArgs args);
    }
}
