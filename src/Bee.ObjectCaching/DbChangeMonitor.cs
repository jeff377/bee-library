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
        private System.Timers.Timer? _Timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbChangeMonitor"/> class.
        /// </summary>
        /// <param name="key">The cache dependency key.</param>
        public DbChangeMonitor(string key)
        {
            UniqueId = BaseFunc.NewGuidString();
            Key = key;
            UpdateTime = GetUpdateTime();
            _Timer = new System.Timers.Timer(1000);
            _Timer.Elapsed += new ElapsedEventHandler(Elapsed_EventHandler);
            _Timer.Start();
            base.InitializationComplete();
        }

        /// <summary>
        /// Gets the unique identifier for this change monitor.
        /// </summary>
        public override string UniqueId { get; }

        /// <summary>
        /// Gets the reference key for the database cache dependency data.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets or sets the last update time of the database cache dependency data.
        /// </summary>
        /// <remarks>For example, if the company organization data is cached, this represents the update time of organization-related data such as departments and employees.</remarks>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// Gets the update time of the cache dependency data.
        /// </summary>
        private static DateTime GetUpdateTime()
        {
            // Placeholder: returns MinValue until database-backed change tracking is wired in.
            return DateTime.MinValue;
        }

        /// <summary>
        /// Gets the timer used to poll for changes.
        /// </summary>
        public System.Timers.Timer? Timer
        {
            get { return _Timer; }
        }

        /// <summary>
        /// Handles the Timer Elapsed event.
        /// </summary>
        private void Elapsed_EventHandler(object? sender, ElapsedEventArgs e)
        {
            DateTime oUpdateTime;

            // If the update time of the dependency data has changed, the source data has been modified and the cache must be invalidated
            oUpdateTime = GetUpdateTime();
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
        }
    }
}
