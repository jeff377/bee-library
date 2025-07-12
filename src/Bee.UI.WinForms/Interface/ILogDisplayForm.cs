using Bee.Base;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 定義可接收日誌的表單介面。
    /// </summary>
    public interface ILogDisplayForm
    {
        /// <summary>
        /// 顯示一筆日誌紀錄。
        /// </summary>
        /// <param name="entry">日誌內容。</param>
        void AppendLog(LogEntry entry);
    }

}
