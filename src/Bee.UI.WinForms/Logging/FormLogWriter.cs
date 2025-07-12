using Bee.Base;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 將日誌輸出至實作 <see cref="ILogDisplayForm"/> 的表單。
    /// </summary>
    public class FormLogWriter : ILogWriter
    {
        private readonly ILogDisplayForm _form;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="form">接收日誌的表單。</param>
        public FormLogWriter(ILogDisplayForm form)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
        }

        /// <summary>
        /// 寫入一筆日誌紀錄。
        /// </summary>
        /// <param name="entry">日誌內容。</param>
        public void Write(LogEntry entry)
        {
            _form.AppendLog(entry);
        }
    }

}
