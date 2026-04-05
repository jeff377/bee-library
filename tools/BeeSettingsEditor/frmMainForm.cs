using Bee.Define.Settings;
using Bee.Base;
using Bee.Base.Security;
using Bee.Api.Contracts;
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
            // 設定標題列文
            this.Text = $"{VersionInfo.Product} v{VersionInfo.Version}";
            // 設定連線方式的顯示文字
            SetConnectText();
            // 預設載入系統設定
            LoadDefine(DefineType.SystemSettings);
        }

        /// <summary>
        /// Load System Settings。
        /// </summary>
        private void menuLoadSystemSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(DefineType.SystemSettings);
        }


        /// <summary>
        /// 資料庫設定。
        /// </summary>
        private void menuLoadDatabaseSettings_Click(object sender, EventArgs e)
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
            var result = MessageBox.Show(
                    "Are you sure you want to exit?",
                    "Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// 設定 API 連線端點。
        /// </summary>
        private void tbApiConnect_Click(object sender, EventArgs e)
        {
            if (ClientInfo.UIViewService.ShowApiConnect())
            {
                Application.Restart();
                Application.Exit();
            }
        }

        /// <summary>
        /// 測試資料庫連線。
        /// </summary>
        private void menuTestDbConnection_Click(object sender, EventArgs e)
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

            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "Settings cannot be null.");

            treeView.BuildObjectTree(settings);
            UpdateMenuVisibility(settings);
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
            var settings = treeView.Tag as DatabaseSettings;
            if (settings == null) { return; }
            var item = databaseItem.Clone();
            if (StrFunc.IsNotEmpty(item.ServerId))
            {
                var server = settings.Servers[item.ServerId];
                if (server != null)
                {
                    item.DatabaseType = server.DatabaseType;
                    item.ConnectionString = server.ConnectionString;
                    if (StrFunc.IsEmpty(item.UserId))
                        item.UserId = server.UserId;
                    if (StrFunc.IsEmpty(item.Password))
                        item.Password = server.Password;
                }
            }

            try
            {
                var args = new ExecFuncArgs(SysFuncIDs.TestConnection);
                args.Parameters.Add("DatabaseItem", item);
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
            string filePath = UIFunc.ShowSaveFileDialog("Master Key|*.key", "*.key", BackendInfo.DefinePath, "Master.key");
            if (StrFunc.IsEmpty(filePath)) { return; }

            File.WriteAllText(filePath, key);
            UIFunc.MsgBox($"Master.key has been saved to: {filePath}");
        }

        /// <summary>
        /// Generate API encryption key.
        /// </summary>
        private void menuGenApiEncryptionKey_Click(object sender, EventArgs e)
        {
            GenerateEncryptionKey("Api");
        }

        /// <summary>
        /// Generate cookie encryption key.
        /// </summary>
        private void menuGenCookieEncryptionKey_Click(object sender, EventArgs e)
        {
            GenerateEncryptionKey("Cookie");
        }

        /// <summary>
        /// Generate configuration encryption key.
        /// </summary>
        private void menuGenConfigEncryptionKey_Click(object sender, EventArgs e)
        {
            GenerateEncryptionKey("Config");
        }

        /// <summary>
        /// Generate database encryption key.
        /// </summary>
        private void menuGenDatabaseEncryptionKey_Click(object sender, EventArgs e)
        {
            GenerateEncryptionKey("Database");
        }

        /// <summary>
        /// generate encryption key based on the specified key type.
        /// </summary>
        /// <param name="keyType">key type</param>
        private void GenerateEncryptionKey(string keyType)
        {
            string encryptionKey = GenerateEncryptionKey();
            if (string.IsNullOrEmpty(encryptionKey)) { return; }

            var settings = treeView.Tag as SystemSettings;
            if (settings == null) { return; }
            var keySettings = settings.BackendConfiguration.SecurityKeySettings;
            switch (keyType)
            {
                case "Api":
                    keySettings.ApiEncryptionKey = encryptionKey;
                    break;
                case "Cookie":
                    keySettings.CookieEncryptionKey = encryptionKey;
                    break;
                case "Config":
                    keySettings.ConfigEncryptionKey = encryptionKey;
                    break;
                case "Database":
                    keySettings.DatabaseEncryptionKey = encryptionKey;
                    break;
                default:
                    UIFunc.ErrorMsgBox("Invalid key type specified.");
                    return;
            }
            this.propertyGrid.SelectedObject = null;
            this.propertyGrid.SelectedObject = keySettings;
            UIFunc.MsgBox($"{keyType} encryption key generated successfully.");
        }

        /// <summary>
        /// 透過 Master Key 產生加密金鑰。
        /// </summary>
        private string GenerateEncryptionKey()
        {
            var settings = treeView.Tag as SystemSettings;
            if (settings == null) { return string.Empty; }

            // 取得 Master Key
            var keySource = settings.BackendConfiguration.SecurityKeySettings.MasterKeySource;
            byte[] masterKey = MasterKeyProvider.GetMasterKey(keySource);
            if (BaseFunc.IsEmpty(masterKey))
            {
                UIFunc.ErrorMsgBox("Unable to retrieve the master key.");
                return string.Empty;
            }

            string encryptionKey = EncryptionKeyProtector.GenerateEncryptedKey(masterKey);
            if (string.IsNullOrEmpty(encryptionKey))
            {
                UIFunc.ErrorMsgBox("Failed to generate encryption key.");
                return string.Empty;
            }
            return encryptionKey;
        }

        /// <summary>
        /// 根據設定檔類型，設定選單的顯示狀態。
        /// </summary>
        /// <param name="settings">已載入的設定檔物件。</param>
        private void UpdateMenuVisibility(object? settings)
        {
            bool isSystemSettings = settings is SystemSettings;
            bool isDatabaseSettings = settings is DatabaseSettings;

            menuTestDbConnection.Visible = isDatabaseSettings;
            menuGenApiEncryptionKey.Visible = isSystemSettings;
            menuGenCookieEncryptionKey.Visible = isSystemSettings;
            menuGenConfigEncryptionKey.Visible = isSystemSettings;
            menuGenDatabaseEncryptionKey.Visible = isSystemSettings;
        }


    }
}
