using System;

namespace Bee.Cache
{
    /// <summary>
    /// 快取項目到期條件。
    /// </summary>
    public class CacheItemPolicy
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public CacheItemPolicy()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="kind">快取條件相依的時間類型，AbsoluteExpiration 及 SlidingExpiration 只能擇一設定。</param>
        /// <param name="minutes">分鐘數。</param>
        public CacheItemPolicy(CacheTimeKind  kind, int minutes)
        {
            if (kind == CacheTimeKind.AbsoluteTime)
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(minutes);  // 絕對時間到期
            else
                SlidingExpiration = TimeSpan.FromMinutes(minutes);  // 相對時間到期
        }

        #endregion

        /// <summary>
        /// 絕對時間到期，在指定的時間點是否應收回快取項目。
        /// </summary>
        public DateTimeOffset AbsoluteExpiration { get; set; } = DateTimeOffset.MaxValue;
 
        /// <summary>
        /// 相對時間到期，是否清除已經有一段時間沒有存取過的快取項目。
        /// </summary>
        public TimeSpan SlidingExpiration { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// 監視目錄和檔案路徑陣列。
        /// </summary>
        public string[] ChangeMonitorFilePaths { get; set; } = null;

        /// <summary>
        /// 監視資料庫 ST_Cache 資料表的異動鍵值陣列。
        /// </summary>
        public string[] ChangeMonitorDbKeys { get; set; } = null;
    }
}
