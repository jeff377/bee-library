using System;
using System.Threading;

namespace Bee.Base.BackgroundServices
{
    /// <summary>
    /// Represents a delegate action for a background task.
    /// </summary>
    public class BackgroundAction
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BackgroundAction"/>.
        /// </summary>
        /// <param name="action">The delegate to execute.</param>
        /// <param name="timeout">The timeout interval in milliseconds after which the task will be cancelled.</param>
        public BackgroundAction(Action<CancellationToken> action, int timeout)
        {
            Action = action;
            Timeout = timeout;
        }

        /// <summary>
        /// Gets the delegate to execute.
        /// </summary>
        public Action<CancellationToken> Action { get; private set; }

        /// <summary>
        /// Gets the timeout interval in milliseconds after which the task will be cancelled.
        /// </summary>
        public int Timeout { get; private set; } = 0;
    }
}
