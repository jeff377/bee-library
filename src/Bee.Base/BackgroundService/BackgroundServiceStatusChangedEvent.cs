using System;

namespace Bee.Base
{
    /// <summary>
    /// BackgroundServiceStatusChanged 事件委派宣告，背景服務狀態變更引發的事件。
    /// </summary>
    public delegate void BackgroundServiceStatusChangedEventHandler(object sender, BackgroundServiceStatusChangedEventArgs e);

    /// <summary>
    /// BackgroundServiceStatusChanged 事件引數。
    /// </summary>
    public class BackgroundServiceStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 背景服務狀態。
        /// </summary>
        public BackgroundServiceStatus Status { get; set; } = BackgroundServiceStatus.Stopped;
    }
}
