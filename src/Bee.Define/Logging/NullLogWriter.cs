namespace Bee.Define
{
    /// <summary>
    /// 空的日誌寫入器，不執行任何操作。
    /// 用於預設情境避免 Null 檢查。
    /// </summary>
    public class NullLogWriter : ILogWriter
    {
        /// <summary>
        /// 寫入日誌（本實作不進行任何操作）。
        /// </summary>
        /// <param name="entry">日誌內容。</param>
        public void Write(LogEntry entry)
        {
            // Do nothing.
        }
    }
}

