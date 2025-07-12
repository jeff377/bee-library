namespace Bee.Base
{
    /// <summary>
    /// 系統日誌寫入介面。
    /// </summary>
    public interface ILogWriter
    {
        /// <summary>
        /// 寫入一筆日誌紀錄。
        /// </summary>
        /// <param name="entry">日誌內容。</param>
        void Write(LogEntry entry);
    }
}
