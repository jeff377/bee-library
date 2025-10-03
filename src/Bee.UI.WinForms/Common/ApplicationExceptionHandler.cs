using Bee.Base;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 應用程式全域例外處理器。
    /// </summary>
    public static class ApplicationExceptionHandler
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public static void Initialize()
        {
            // 設定應用程式會捕捉所有未處理的例外狀況
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            // 應用程式的某個執行緒 (主執行緒除外) 發生未處理的例外狀況時觸發
            Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);
            // 未被捕捉的例外狀況發生時，這個事件會被觸發
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);
        }

        /// <summary>
        /// ThreadException 事件處理函式。
        /// </summary>
        private static void ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            // 處理執行緒發生的例外狀況
            ShowException(e.Exception);
        }

        /// <summary>
        /// UnhandledException 事件處理函式。
        /// </summary>
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // 處理未處理的例外狀況
            Exception ex = (Exception)e.ExceptionObject;
            ShowException(ex);
        }

        /// <summary>
        /// 顯示例外錯誤。
        /// </summary>
        /// <param name="exception">例外錯誤。</param>
        private static void ShowException(Exception exception)
        {
            // 取得最根本的例外
            var rootEx = BaseFunc.UnwrapException(exception);

            var message = $"Exception Type: {rootEx.GetType().FullName}\n" +
                          $"Exception Message: {rootEx.Message}\n\n" +
                          $"Stack Trace: {rootEx.StackTrace}";

            UIFunc.ErrorMsgBox(message);
        }
    }
}
