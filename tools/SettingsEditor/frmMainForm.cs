using Bee.Base;
using Bee.Define;
using Bee.UI.Core;
using Bee.UI.WinForms;

namespace SettingsEditor
{
    /// <summary>
    /// 主視窗。
    /// </summary>
    public partial class frmMainForm : TForm
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
        /// 系統設定。
        /// </summary>
        private void tbSystemSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(EDefineType.SystemSettings);
        }

        /// <summary>
        /// 資料庫設定。
        /// </summary>
        private void tbDatabaseSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(EDefineType.DatabaseSettings);
        }

        /// <summary>
        /// 儲存。
        /// </summary>
        private void tbSave_Click(object sender, EventArgs e)
        {
            object? settings = treeView.Tag;
            if (settings == null) { return; }

            if (settings is TSystemSettings)
            {
                ClientInfo.DefineAccess.SaveSystemSettings(settings as TSystemSettings);
                UIFunc.MsgBox("系統設定儲存完成");
            }
            else if (settings is TDatabaseSettings)
            {
                ClientInfo.DefineAccess.SaveDatabaseSettings(settings as TDatabaseSettings);
                UIFunc.MsgBox("資料庫設定儲存完成");
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
        /// 連線設定。
        /// </summary>
        private void tbConnect_Click(object sender, EventArgs e)
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

            var databaseItem = treeView.SelectedNode.Tag as TDatabaseItem;
            if (databaseItem == null)
            {
                UIFunc.ErrorMsgBox("請選取要測試的資料庫節點");
                return;
            }
            TestConnection(databaseItem);
        }

        /// <summary>
        /// 載入定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        private void LoadDefine(EDefineType defineType)
        {
            string displayName;
            object settings;
            switch (defineType)
            {
                case EDefineType.SystemSettings:
                    settings = ClientInfo.DefineAccess.GetSystemSettings();
                    displayName = "系統設定";
                    break;
                case EDefineType.DatabaseSettings:
                    settings = ClientInfo.DefineAccess.GetDatabaseSettings();
                    displayName = "資料庫設定";
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (settings != null)
            {
                treeView.BuildObjectTree(settings);
                tbSave.Text = "儲存 " + displayName;
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
        private void TestConnection(TDatabaseItem databaseItem)
        {
            try
            {
                var args = new TExecFuncArgs(SysFuncIDs.TestConnection);
                args.Parameters.Add("DatabaseItem", databaseItem);
                ClientInfo.SystemConnector.ExecFunc(args);
                UIFunc.MsgBox("資料庫連線測試成功");
            }
            catch (Exception ex)
            {
                UIFunc.ErrorMsgBox(ex.Message);
            }
        }
    }
}
