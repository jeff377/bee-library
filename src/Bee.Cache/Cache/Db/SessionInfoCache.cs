using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 連線資訊快取。
    /// </summary>
    internal class SessionInfoCache : KeyObjectCache<SessionInfo>
    {
        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">存取令牌。</param>
        protected override SessionInfo CreateInstance(string key)
        {
            var accessToken = BaseFunc.CGuid(key);
            if (BaseFunc.IsEmpty(accessToken)) { return null; }

            // 取得暫存連線的用戶資料
            var user = BackendInfo.CacheDataSourceProvider.GetSessionUser(accessToken);
            if (user == null) { return null; }

            // 傳回連線資訊
            return new SessionInfo()
            {
                AccessToken = accessToken,
                UserID = user.UserID,
                UserName = user.UserName
            };
        }

        /// <summary>
        /// 取得連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public SessionInfo Get(Guid accessToken)
        {
            return Get(accessToken.ToString());
        }

        /// <summary>
        /// 由快取區移除連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public void Remove(Guid accessToken)
        {
            Remove(accessToken.ToString());
        }
    }
}
