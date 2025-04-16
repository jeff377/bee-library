using System;
using System.Timers;
using System.Runtime.Caching;
using Bee.Base;

namespace Bee.Cache
{
    /// <summary>
    /// 監控資料庫 ST_Cahce 資料表異動，並通知快取受監控項目發生變更的資訊。
    /// </summary>
    public class TDbChangeMonitor : ChangeMonitor
    {
        private readonly string _UniqueId = string.Empty;
        private readonly string _Key = string.Empty;
        private DateTime _UpdateTime = DateTime.MinValue;
        private Timer _Timer = null;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="key"></param>
        private TDbChangeMonitor(string key)
        {
            _UniqueId = BaseFunc.NewGuidString();
            _Key = key;
            _UpdateTime = GetUpateTime(key);
            _Timer = new Timer(1000);
            _Timer.Elapsed += new ElapsedEventHandler(Elapsed_EventHandler);
            _Timer.Start();
            base.InitializationComplete();
        }

        /// <summary>
        /// 識別項。
        /// </summary>
        public override string UniqueId
        {
            get { return _UniqueId; }
        }

        /// <summary>
        /// 資料庫快取相依資料的參照鍵值。
        /// </summary>
        public string Key
        {
            get { return _Key; }
        }

        /// <summary>
        /// 資料庫快取相依資料的更新時間。
        /// </summary>
        /// <remarks>例如公司組織需快取，即組織相關資料(如部門、員工)的更新時間</remarks>
        public DateTime UpdateTime
        {
            get { return _UpdateTime; }
            set { _UpdateTime = value; }
        }

        /// <summary>
        /// 取得快取相依資料的更新時間。
        /// </summary>
        /// <param name="key">鍵值。</param>
        private DateTime GetUpateTime(string key)
        {
            // TODO : 實作取得資料庫相關資料的更新時間
            return DateTime.MinValue;
        }

        /// <summary>
        /// 計時器。
        /// </summary>
        public Timer Timer
        {
            get { return _Timer; }
        }

        /// <summary>
        /// Timer Elapsed 事件處理方法。
        /// </summary>
        private void Elapsed_EventHandler(object sender, ElapsedEventArgs e)
        {
            DateTime oUpdateTime;

            // 相依資料的更新時間若不同，表示原始資料有異動，需釋放快取
            oUpdateTime = GetUpateTime(this.Key);
            if (this.UpdateTime != oUpdateTime)
            {
                this.UpdateTime = oUpdateTime;
                // 有異動要呼叫 OnChanged 方法，通知 Cache 相依變更
                base.OnChanged(this.Key);
            }
        }

        /// <summary>
        /// 解構函式。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_Timer != null)
            {
                _Timer.Stop();
                _Timer.Dispose();
                _Timer = null;
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
