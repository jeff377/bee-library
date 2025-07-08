using Bee.Base;
using Bee.Define;
using Bee.UI.Core;
using Bee.UI.WinForms;

namespace SettingsEditor
{
    /// <summary>
    /// 主視窗。
    /// </summary>
    public partial class frmMainForm : BaseForm
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public frmMainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load 事件。
        /// </summary>
        private void frmMainForm_Load(object sender, EventArgs e)
        {
            // 設定連線方式的顯示文字
            SetConnectText();
        }

        /// <summary>
        /// 
        /// </summary>
        private void menuLoadSystemSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(DefineType.SystemSettings);
        }

        /// <summary>
        /// 系統設定。
        /// </summary>
        private void tbSystemSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(DefineType.SystemSettings);
        }

        /// <summary>
        /// 資料庫設定。
        /// </summary>
        private void tbDatabaseSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(DefineType.DatabaseSettings);
        }

        /// <summary>
        /// 儲存。
        /// </summary>
        private void tbSave_Click(object sender, EventArgs e)
        {
            object? settings = treeView.Tag;
            if (settings == null) { return; }

            if (settings is SystemSettings)
            {
                ClientInfo.DefineAccess.SaveSystemSettings(settings as SystemSettings);
                UIFunc.MsgBox("The file System.Settings.xml was saved successfully.");
            }
            else if (settings is DatabaseSettings)
            {
                ClientInfo.DefineAccess.SaveDatabaseSettings(settings as DatabaseSettings);
                UIFunc.MsgBox("The file Database.Settings.xml was saved successfully.");
            }
        }

        /// <summary>
        /// 結束。
        /// </summary>
        private void tbExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// menuApiConnect。
        /// </summary>
        private void tbApiConnect_Click(object sender, EventArgs e)
        {
            if (ClientInfo.UIViewService.ShowConnect())
                SetConnectText();
        }

        /// <summary>
        /// 測試資料庫連線。
        /// </summary>
        private void tbTestConnection_Click(object sender, EventArgs e)
        {
            if (treeView.SelectedNode == null) { return; }

            var databaseItem = treeView.SelectedNode.Tag as DatabaseItem;
            if (databaseItem == null)
            {
                UIFunc.ErrorMsgBox("Please select a database node to test.");
                return;
            }
            TestConnection(databaseItem);
        }

        /// <summary>
        /// 載入定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        private void LoadDefine(DefineType defineType)
        {
            object settings;
            switch (defineType)
            {
                case DefineType.SystemSettings:
                    settings = ClientInfo.DefineAccess.GetSystemSettings();
                    break;
                case DefineType.DatabaseSettings:
                    settings = ClientInfo.DefineAccess.GetDatabaseSettings();
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (settings != null)
            {
                treeView.BuildObjectTree(settings);
                tbSave.Enabled = true;
            }

        }

        /// <summary>
        /// 設定連線方式的顯示文字。
        /// </summary>
        private void SetConnectText()
        {
            lblConnectType.Text = UIFunc.GetConnectText();
        }

        /// <summary>
        /// 測試資料庫連線。
        /// </summary>
        /// <param name="databaseItem">資料庫項目。</param>
        private void TestConnection(DatabaseItem databaseItem)
        {
            try
            {
                var args = new ExecFuncArgs(SysFuncIDs.TestConnection);
                args.Parameters.Add("DatabaseItem", databaseItem);
                ClientInfo.SystemApiConnector.ExecFunc(args);
                UIFunc.MsgBox("Database connection test succeeded.");
            }
            catch (Exception ex)
            {
                UIFunc.ErrorMsgBox(ex.Message);
            }
        }

        /// <summary>
        /// 產生 Master.Key。
        /// </summary>
        private void menuGenerateMasterKey_Click(object sender, EventArgs e)
        {
            string key = AesCbcHmacKeyGenerator.GenerateBase64CombinedKey();
            string filePath = UIFunc.ShowSaveFileDialog("Master Key|*.key", "Master.Key");
            if (StrFunc.IsEmpty(filePath)) { return; }

            File.WriteAllText(filePath, key);
            UIFunc.MsgBox($"Master.Key has been saved to: {filePath}");
        }


    }
}
