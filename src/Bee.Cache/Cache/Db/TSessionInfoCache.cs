using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 連線資訊快取。
    /// </summary>
    internal class TSessionInfoCache : TKeyObjectCache<TSessionInfo>
    {
        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">存取令牌。</param>
        protected override TSessionInfo CreateInstance(string key)
        {
            var accessToken = BaseFunc.CGuid(key);
            if (BaseFunc.IsEmpty(accessToken)) { return null; }

            // 取得暫存連線的用戶資料
            var provider = SysFunc.CreateCacheDataSourceProvider();
            var user = provider.GetSessionUser(accessToken);
            if (user == null) { return null; }

            // 傳回連線資訊
            return new TSessionInfo()
            {
                AccessToken = accessToken,
                UserID = user.UserID,
                UserName = user.UserName
            };
        }
    }
}
