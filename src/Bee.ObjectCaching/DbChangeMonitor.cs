using System;
using System.Timers;
using System.Runtime.Caching;
using Bee.Base;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Monitors changes to the ST_Cache database table and notifies the cache when monitored items have changed.
    /// </summary>
    public class DbChangeMonitor : ChangeMonitor
    {
        private readonly string _UniqueId = string.Empty;
        private readonly string _Key = string.Empty;
        private DateTime _UpdateTime = DateTime.MinValue;
        private System.Timers.Timer _Timer = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbChangeMonitor"/> class.
        /// </summary>
        /// <param name="key">The cache dependency key.</param>
        private DbChangeMonitor(string key)
        {
            _UniqueId = BaseFunc.NewGuidString();
            _Key = key;
            _UpdateTime = GetUpdateTime(key);
            _Timer = new System.Timers.Timer(1000);
            _Timer.Elapsed += new ElapsedEventHandler(Elapsed_EventHandler);
            _Timer.Start();
            base.InitializationComplete();
        }

        /// <summary>
        /// Gets the unique identifier for this change monitor.
        /// </summary>
        public override string UniqueId
        {
            get { return _UniqueId; }
        }

        /// <summary>
        /// Gets the reference key for the database cache dependency data.
        /// </summary>
        public string Key
        {
            get { return _Key; }
        }

        /// <summary>
        /// Gets or sets the last update time of the database cache dependency data.
        /// </summary>
        /// <remarks>For example, if the company organization data is cached, this represents the update time of organization-related data such as departments and employees.</remarks>
        public DateTime UpdateTime
        {
            get { return _UpdateTime; }
            set { _UpdateTime = value; }
        }

        /// <summary>
        /// Gets the update time of the cache dependency data.
        /// </summary>
        /// <param name="key">The cache key.</param>
        private DateTime GetUpdateTime(string key)
        {
            // TODO : 實作取得資料庫相關資料的更新時間
            return DateTime.MinValue;
        }

        /// <summary>
        /// Gets the timer used to poll for changes.
        /// </summary>
        public System.Timers.Timer Timer
        {
            get { return _Timer; }
        }

        /// <summary>
        /// Handles the Timer Elapsed event.
        /// </summary>
        private void Elapsed_EventHandler(object sender, ElapsedEventArgs e)
        {
            DateTime oUpdateTime;

            // If the update time of the dependency data has changed, the source data has been modified and the cache must be invalidated
            oUpdateTime = GetUpdateTime(this.Key);
            if (this.UpdateTime != oUpdateTime)
            {
                this.UpdateTime = oUpdateTime;
                // Call OnChanged to notify the cache of a dependency change
                base.OnChanged(this.Key);
            }
        }

        /// <summary>
        /// Releases resources used by this change monitor.
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
