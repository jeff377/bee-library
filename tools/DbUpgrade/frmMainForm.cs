using System.Text;
using Bee.Base;
using Bee.Define;
using Bee.UI.Core;
using Bee.UI.WinForms;

namespace DbUpgrade
{
    /// <summary>
    /// 主視窗。
    /// </summary>
    public partial class frmMainForm : TForm
    {
        private StringBuilder _buffer;  // 執行訊息輸出暫存區

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public frmMainForm()
        {
            InitializeComponent();
            _buffer = new StringBuilder();
        }

        #endregion

        private void frmMainForm_Load(object sender, EventArgs e)
        {
            // 載入資料庫清單
            LoadDatabases();
            // 設定連線方式的顯示文字
            SetConnectText();
        }

        /// <summary>
        /// 設定連線方式的顯示文字。
        /// </summary>
        private void SetConnectText()
        {
            lblConnectType.Text = UIFunc.GetConnectText();
        }

        /// <summary>
        /// 載入資料庫清單。
        /// </summary>
        private void LoadDatabases()
        {
            edtDatabases.Items.Clear();
            var settings = ClientInfo.DefineAccess.GetDatabaseSettings();
            foreach (TDatabaseItem item in settings.Items)
                edtDatabases.Items.Add(item);
            if (edtDatabases.Items.Count > 0) { edtDatabases.SelectedIndex = 0; }
        }

        /// <summary>
        /// 執行升級。
        /// </summary>
        private void btnExecute_Click(object sender, EventArgs e)
        {
            TDbTableItem oTable;

            btnExecute.Enabled = false;
            try
            {
                _buffer.Clear();
                lblMessage.Visible = true;
                int upgradeCount = 0;

                var item = edtDatabases.SelectedItem as TDatabaseItem;
                if (item == null) { return; }

                var settings = ClientInfo.DefineAccess.GetDbSchemaSettings();
                var database = settings.Databases[item.DbName];
                int totalCount = database.Tables.Count;
                for (int N1 = 0; N1 < database.Tables.Count; N1++)
                {
                    oTable = database.Tables[N1];
                    SetMessage($"{N1 + 1}/{totalCount} : {oTable.TableName}");
                    if (this.UpgradeTableSchema(item.DbName, oTable.TableName))
                    {
                        upgradeCount++;
                        _buffer.AppendLine($"{oTable.TableName} : 結構升級");
                    }
                    else
                    {
                        _buffer.AppendLine($"{oTable.TableName} : 結構一致");
                    }
                }
                UIFunc.MsgBox($"執行完成，共升級 {upgradeCount} 個資料表");
                btnSaveLog.Visible = true;
            }
            catch (Exception ex)
            {
                UIFunc.ErrorMsgBox(ex.Message);
            }
            finally
            {
                btnExecute.Enabled = true;
            }
        }

        /// <summary>
        /// 儲存記錄。
        /// </summary>
        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "儲存記錄";
            dialog.Filter = "文字檔|*.txt";
            dialog.FileName = $"DbToolsLog_{DateTime.Today:yyyyMMdd}.txt";
            if (dialog.ShowDialog() != DialogResult.OK) { return; }

            FileFunc.FileWriteText(dialog.FileName, "DbTools 升級記錄\n" + _buffer.ToString());
            System.Diagnostics.Process.Start(dialog.FileName);
        }

        /// <summary>
        /// 升級資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        private bool UpgradeTableSchema(string dbName, string tableName)
        {
            var args = new TExecFuncArgs();
            args.FuncID = SysFuncIDs.UpgradeTableSchema;
            args.Parameters.Add("DatabaseID", BackendInfo.DatabaseID);
            args.Parameters.Add("DbName", dbName);
            args.Parameters.Add("TableName", tableName);
            var result = ClientInfo.SystemConnector.ExecFunc(args);
            return result.Parameters.GetValue<bool>("Upgraded");
        }

        /// <summary>
        /// 設定訊息文字。
        /// </summary>
        /// <param name="message"></param>
        private void SetMessage(string message)
        {
            lblMessage.Text = message;
            Application.DoEvents();
        }
    }
}
