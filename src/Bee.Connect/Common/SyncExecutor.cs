using System;
using System.Threading.Tasks;

namespace Bee.Connect
{
    /// <summary>
    /// 同步執行非同步方法的執行器。
    /// </summary>
    /// <remarks>
    /// 用於將 <c>async/await</c> 方法轉換為同步方式執行，
    /// 適用於無法使用 <c>await</c> 的情境，例如：建構函式、WinForms 初始化、封裝同步 API。
    /// </remarks>
    public static class SyncExecutor
    {
        /// <summary>
        /// 同步執行非同步方法（不回傳結果）。
        /// </summary>
        /// <param name="asyncFunc">非同步方法委派。</param>
        /// <exception cref="AggregateException">若執行失敗，將拋出例外。</exception>
        public static void Run(Func<Task> asyncFunc)
        {
            if (asyncFunc == null)
                throw new ArgumentNullException(nameof(asyncFunc));

            Task.Run(asyncFunc).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 同步執行非同步方法並回傳結果。
        /// </summary>
        /// <typeparam name="TResult">結果型別。</typeparam>
        /// <param name="asyncFunc">非同步方法委派。</param>
        /// <returns>非同步方法的執行結果。</returns>
        /// <exception cref="AggregateException">若執行失敗，將拋出例外。</exception>
        public static TResult Run<TResult>(Func<Task<TResult>> asyncFunc)
        {
            if (asyncFunc == null)
                throw new ArgumentNullException(nameof(asyncFunc));

            return Task.Run(asyncFunc).GetAwaiter().GetResult();
        }
    }
}
