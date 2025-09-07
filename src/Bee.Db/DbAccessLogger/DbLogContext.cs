using System;
using System.Diagnostics;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫存取日誌的上下文資訊。
    /// 用於記錄命令內容、資料庫識別、執行計時與開始時間等資訊。
    /// </summary>
    public sealed class DbLogContext
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="commandText">SQL 命令內容。</param>
        /// <param name="databaseId">資料庫識別。</param>
        internal DbLogContext(string commandText, string databaseId)
        {
            CommandText = commandText ?? string.Empty;
            DatabaseId = databaseId ?? string.Empty;
            Stopwatch = Stopwatch.StartNew();
            StartedAtUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// SQL 命令內容。
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// 資料庫識別。
        /// </summary>
        public string DatabaseId { get; }

        /// <summary>
        /// 執行計時器，記錄命令執行所花費的時間。
        /// </summary>
        public Stopwatch Stopwatch { get; }

        /// <summary>
        /// 命令開始執行的 UTC 時間。
        /// </summary>
        public DateTime StartedAtUtc { get; }
    }
}
