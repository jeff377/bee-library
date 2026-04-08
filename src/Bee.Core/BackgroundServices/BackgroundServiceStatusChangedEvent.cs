using System;

namespace Bee.Core.BackgroundServices
{
    /// <summary>
    /// Delegate declaration for the BackgroundServiceStatusChanged event, raised when the background service status changes.
    /// </summary>
    public delegate void BackgroundServiceStatusChangedEventHandler(object sender, BackgroundServiceStatusChangedEventArgs e);

    /// <summary>
    /// Event arguments for the BackgroundServiceStatusChanged event.
    /// </summary>
    public class BackgroundServiceStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the background service status.
        /// </summary>
        public BackgroundServiceStatus Status { get; set; } = BackgroundServiceStatus.Stopped;
    }
}
