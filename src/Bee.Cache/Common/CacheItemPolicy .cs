using System;

namespace Bee.Cache
{
    /// <summary>
    /// 快取項目到期條件。
    /// </summary>
    public class TCacheItemPolicy
    {
        private DateTimeOffset _AbsoluteExpiration = DateTimeOffset.MaxValue;
        private TimeSpan _SlidingExpiration = TimeSpan.Zero;
        private string[] _ChangeMonitorFilePaths = null;
        private string[] _ChangeMonitorDbKeys = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TCacheItemPolicy()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="kind">快取條件相依的時間類型，AbsoluteExpiration 及 SlidingExpiration 只能擇一設定。</param>
        /// <param name="minutes">分鐘數。</param>
        public TCacheItemPolicy(ECacheTimeKind  kind, int minutes)
        {
            SetCacheTime(kind, minutes);
        }

        #endregion

        /// <summary>
        /// 設定快取時間。
        /// </summary>
        /// <param name="kind">快取條件相依的時間類型，AbsoluteExpiration 及 SlidingExpiration 只能擇一設定。</param>
        /// <param name="minutes">分鐘數。</param>
        public void SetCacheTime(ECacheTimeKind kind, int minutes)
        {
           if (kind == ECacheTimeKind.AbsoluteTime)
                _AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(minutes);  // 絕對時間到期
           else
                _SlidingExpiration =  TimeSpan.FromMinutes(minutes);  // 相對時間到期
        }

        /// <summary>
        /// 絕對時間到期，在指定的時間點是否應收回快取項目。
        /// </summary>
        public DateTimeOffset AbsoluteExpiration
        {
            get { return _AbsoluteExpiration; }
            set { _AbsoluteExpiration = value; }
        }

        /// <summary>
        /// 相對時間到期，是否清除已經有一段時間沒有存取過的快取項目。
        /// </summary>
        public TimeSpan SlidingExpiration
        {
            get { return _SlidingExpiration; }
            set { _SlidingExpiration = value; }
        }

        /// <summary>
        /// 監視目錄和檔案路徑陣列。
        /// </summary>
        public string[] ChangeMonitorFilePaths
        {
            get { return _ChangeMonitorFilePaths; }
            set { _ChangeMonitorFilePaths = value; }
        }

        /// <summary>
        /// 監視資料庫 ST_Cache 資料表的異動鍵值陣列。
        /// </summary>
        public string[] ChangeMonitorDbKeys
        {
            get { return _ChangeMonitorDbKeys; }
            set { _ChangeMonitorDbKeys = value; }
        }
    }
}
