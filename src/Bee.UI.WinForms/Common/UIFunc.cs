using Bee.Base;
using Bee.Define;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// WinForms UI 函式庫。
    /// </summary>
    public static class UIFunc
    {
        /// <summary>
        /// 顯示訊息視窗。
        /// </summary>
        /// <param name="message">訊息。</param>
        public static void MsgBox(string message)
        {
            MessageBox.Show(message);
        }

        /// <summary>
        /// 顯示錯誤訊息視窗。
        /// </summary>
        /// <param name="message">訊息。</param>
        public static void ErrorMsgBox(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 顯示儲存檔案對話框。
        /// </summary>
        /// <param name="filter">檔案過濾條件，範例如 "Excel 檔案|*.xlsx|Excel 97-2003 檔案|*.xls"。</param>
        /// <param name="defaultExt">預設副檔名。</param>
        /// <param name="fileName">預設檔案名稱。</param>
        public static string ShowSaveFileDialog(string filter, string defaultExt, string fileName = "")
        {
            var dialog = new SaveFileDialog()
            {
                Filter = filter,
                DefaultExt = defaultExt
            };

            if (StrFunc.IsNotEmpty(fileName))
            {
                dialog.FileName = fileName;
                dialog.InitialDirectory = FileFunc.GetDirectory(fileName);
            }

            if (dialog.ShowDialog() == DialogResult.OK)
                return dialog.FileName;
            else
                return string.Empty;
        }

        /// <summary>
        /// 顯示開啟檔案對話框。
        /// </summary>
        /// <param name="filter">檔名篩選字串，例如 "文字檔 (*.txt)|*.txt"。</param>
        /// <param name="defaultExt">預設副檔案。</param>
        /// <param name="fileName">預設檔案名稱。</param>
        public static string ShowOpenFileDialog(string filter, string defaultExt, string fileName = "")
        {
            var dialog = new OpenFileDialog()
            {
                Multiselect = false,
                Filter = filter,
                DefaultExt = defaultExt
            };

            if (StrFunc.IsNotEmpty(fileName))
            {
                dialog.FileName = fileName;
                dialog.InitialDirectory = FileFunc.GetDirectory(fileName);
            }

            if (dialog.ShowDialog() == DialogResult.OK)
                return dialog.FileName;
            else
                return string.Empty;
        }

        /// <summary>
        /// Displays a folder browser dialog that allows the user to select a folder.
        /// </summary>
        /// <remarks>
        /// The dialog allows the user to create a new folder if needed. If the user cancels the
        /// dialog, the method returns an empty string.
        /// </remarks>
        /// <param name="description">The description text displayed in the dialog.</param>
        /// <returns>The full path of the selected folder if the user confirms the selection; otherwise, an empty string.</returns>
        public static string ShowFolderBrowserDialog(string description = "")
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = description;
                dialog.ShowNewFolderButton = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                    return dialog.SelectedPath;
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// 顯示連線設定。
        /// </summary>
        public static DialogResult ShowConnect()
        {
            frmConnect oForm;

            oForm = new frmConnect();
            return oForm.ShowDialog();
        }

        /// <summary>
        /// 取得目前連線方式的顯示文字。
        /// </summary>
        public static string GetConnectText()
        {
            if (FrontendInfo.ConnectType == ConnectType.Local)
                return $"近端連線 : {BackendInfo.DefinePath}";
            else
                return $"遠端連線 : {FrontendInfo.Endpoint}";
        }
    }
}
