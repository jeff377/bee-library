using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// WinForms UI 環境資訊。
    /// </summary>
    public static class UIInfo
    {
        /// <summary>
        /// 靜態建構函數，會在第一次使用類別時呼叫。
        /// </summary>
        static UIInfo()
        {
            // 取得主程式 EXE 路徑
            string? exePath = Environment.ProcessPath;
            if (exePath != null)
            {
                AppIcon = Icon.ExtractAssociatedIcon(exePath);
            }
        }

        /// <summary>
        /// 應用程式圖示。
        /// </summary>
        public static Icon? AppIcon { get; set; }
    }
}
