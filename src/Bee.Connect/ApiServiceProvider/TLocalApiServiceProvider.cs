using System;
using System.Threading.Tasks;
using Bee.Api.Core;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// 近端 API 服務提供者（在同一進程內直接存取後端商業邏輯）。
    /// </summary>
    public class TLocalApiServiceProvider : IApiServiceProvider
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
        /// <param name="args">傳入參數。</param>
        public TApiServiceResult Execute(TApiServiceArgs args)
        {
            // 註1：開發階段使用近端連線，簡化運行環境及方便偵錯；運行階段則使用遠端連線
            // 註2：近端連線傳遞資料做加解密，是為了驗證開發階段傳遞的資料型別都能正常序列化
            var executor = new TApiServiceExecutor(AccessToken);
            args.Encrypt();  // 傳入資料進行加密
            var result = executor.Execute(args);
            result.Decrypt();  // 傳出結果進行解密
            return result;
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="args">傳入參數。</param>
        public Task<string> ExecuteAsync(TApiServiceArgs args)
        {
            throw new NotSupportedException();
        }
    }
}
