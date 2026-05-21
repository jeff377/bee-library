namespace Bee.Api.Client
{
    /// <summary>
    /// Executor for running asynchronous methods synchronously.
    /// </summary>
    /// <remarks>
    /// Intended only for bridging synchronous interfaces (e.g., <c>IDefineAccess</c>) over
    /// asynchronous connector calls. New code should use the connector's <c>*Async</c>
    /// methods directly. Do not introduce new <c>SyncExecutor.Run</c> call sites without
    /// architectural review.
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
            ArgumentNullException.ThrowIfNull(asyncFunc);

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
            ArgumentNullException.ThrowIfNull(asyncFunc);

            return Task.Run(asyncFunc).GetAwaiter().GetResult();
        }
    }
}
