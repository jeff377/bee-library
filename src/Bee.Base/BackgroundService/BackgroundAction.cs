using System;
using System.Threading;

namespace Bee.Base
{
    /// <summary>
    /// 背景工作的委派方法。
    /// </summary>
    public class BackgroundAction
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="action">委派方法。</param>
        /// <param name="timeout">逾時取消工作的時間間隔，以毫秒為單位。</param>
        public BackgroundAction(Action<CancellationToken> action, int timeout)
        {
            Action = action;
            Timeout = timeout;
        }

        /// <summary>
        /// 委派方法。
        /// </summary>
        public Action<CancellationToken> Action { get; private set; }

        /// <summary>
        /// 逾時取消工作的時間間隔，以毫秒為單位。
        /// </summary>
        public int Timeout { get; private set; } = 0;
    }
}
