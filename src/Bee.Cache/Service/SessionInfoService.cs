using System;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 連線資訊存取服務。
    /// </summary>
    public class SessionInfoService : ISessionInfoService
    {
        /// <summary>
        /// 由快取（未命中可回退至資料庫）取得連線資訊。
        /// </summary>
        public SessionInfo Get(Guid accessToken)
        {
            return CacheFunc.GetSessionInfo(accessToken);
        }

        /// <summary>
        /// 將連線資訊置入快取（必要時一併持久化）。
        /// </summary>
        public void Set(SessionInfo sessionInfo)
        {
            CacheFunc.SetSessionInfo(sessionInfo);
        }

        /// <summary>
        /// 自快取移除指定連線資訊（必要時一併失效持久化狀態）。
        /// </summary>
        public void Remove(Guid accessToken)
        {
            CacheFunc.RemoveSessionInfo(accessToken);   
        }
    }
}
