using Bee.UI.WinForms;

namespace DbUpgrade
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationHandleException.Initialize();  // 應用程式全域例外處理
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            if (!AppInfo.Initialize()) { return; }  // 應用程式初始化
            Application.Run(new frmMainForm());
        }
    }
}