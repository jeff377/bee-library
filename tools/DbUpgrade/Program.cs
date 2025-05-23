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
            ApplicationHandleException.Initialize();  // ���ε{������ҥ~�B�z
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            if (!AppInfo.Initialize()) { return; }  // ���ε{����l��
            Application.Run(new frmMainForm());
        }
    }
}