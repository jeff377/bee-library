using System;
using System.Threading.Tasks;
using Bee.Api.Core;
using Bee.Define;

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
            // 註2：近端連線傳遞資料做加解密，是為了驗證開發階段傳遞的資料型別都能正常序列化
            var executor = new TJsonRpcExecutor(AccessToken);
            request.Encrypt();  // 傳入資料進行加密
            var result = executor.Execute(request);
            result.Decrypt();  // 傳出結果進行解密
            return result;
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public Task<string> ExecuteAsync(TJsonRpcRequest request)
        {
            throw new NotSupportedException();
        }
    }
}
