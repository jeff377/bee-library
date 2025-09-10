using Bee.Base;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 將追蹤事件輸出至實作 <see cref="ITraceDisplayForm"/> 的表單。
    /// </summary>
    public class FormTraceWriter : ITraceWriter
    {
        private readonly ITraceDisplayForm _form;
        private readonly SynchronizationContext _uiContext;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="form">接收追蹤事件的表單。</param>
        public FormTraceWriter(ITraceDisplayForm form)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        }

        /// <summary>
        /// 寫入一筆追蹤事件。
        /// </summary>
        /// <param name="evt">追蹤事件內容。</param>
        public void Write(TraceEvent evt)
        {
            _uiContext.Post(_ => _form.AppendTrace(evt), null);
        }
    }
}

