using System;

namespace Bee.Define
{
    /// <summary>
    /// 連線資訊存取服務介面。
    /// 以快取為主要來源，必要時可回退至資料庫載入或持久化。
    /// </summary>
    public interface ISessionInfoService
    {
        /// <summary>
        /// 由快取（未命中可回退至資料庫）取得連線資訊。
        /// </summary>
        SessionInfo Get(Guid accessToken);

        /// <summary>
        /// 將連線資訊置入快取（必要時一併持久化）。
        /// </summary>
        void Set(SessionInfo sessionInfo);

        /// <summary>
        /// 自快取移除指定連線資訊（必要時一併失效持久化狀態）。
        /// </summary>
        void Remove(Guid accessToken);
    }
}
