using System;
using System.Threading.Tasks;

namespace Bee.Api.Client
{
    /// <summary>
    /// Executor for running asynchronous methods synchronously.
    /// </summary>
    /// <remarks>
    /// Used to convert <c>async/await</c> methods into synchronous calls,
    /// suitable for contexts where <c>await</c> cannot be used, such as constructors, WinForms initialization, or synchronous API wrappers.
    /// </remarks>
    public static class SyncExecutor
    {
        /// <summary>
        /// Synchronously runs an asynchronous method that does not return a result.
        /// </summary>
        /// <param name="asyncFunc">The asynchronous method delegate.</param>
        /// <exception cref="AggregateException">Thrown if the execution fails.</exception>
        public static void Run(Func<Task> asyncFunc)
        {
            if (asyncFunc == null)
                throw new ArgumentNullException(nameof(asyncFunc));

            Task.Run(asyncFunc).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronously runs an asynchronous method and returns its result.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="asyncFunc">The asynchronous method delegate.</param>
        /// <returns>The result of the asynchronous method.</returns>
        /// <exception cref="AggregateException">Thrown if the execution fails.</exception>
        public static TResult Run<TResult>(Func<Task<TResult>> asyncFunc)
        {
            if (asyncFunc == null)
                throw new ArgumentNullException(nameof(asyncFunc));

            return Task.Run(asyncFunc).GetAwaiter().GetResult();
        }
    }
}
