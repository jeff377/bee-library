using Bee.Base;
using Bee.Contracts;
using Bee.Define;
using Bee.UI.Core;
using Bee.UI.WinForms;

namespace SettingsEditor
{
    /// <summary>
    /// �D�����C
    /// </summary>
    public partial class frmMainForm : BaseForm
    {
        /// <summary>
        /// �غc�禡�C
        /// </summary>
        public frmMainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load �ƥ�C
        /// </summary>
        private void frmMainForm_Load(object sender, EventArgs e)
        {
            // �]�w���D�C��
            this.Text = $"{VersionInfo.Product} v{VersionInfo.Version}";
            // �]�w�s�u�覡����ܤ�r
            SetConnectText();
            // �w�]���J�t�γ]�w
            LoadDefine(DefineType.SystemSettings);
        }

        /// <summary>
        /// Load System Settings�C
        /// </summary>
        private void menuLoadSystemSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(DefineType.SystemSettings);
        }


        /// <summary>
        /// ��Ʈw�]�w�C
        /// </summary>
        private void menuLoadDatabaseSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(DefineType.DatabaseSettings);
        }

        /// <summary>
        /// �x�s�C
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
        /// �����C
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
        /// �]�w API �s�u���I�C
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
        /// ���ո�Ʈw�s�u�C
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
        /// ���J�w�q��ơC
        /// </summary>
        /// <param name="defineType">�w�q��������C</param>
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
        /// �]�w�s�u�覡����ܤ�r�C
        /// </summary>
        private void SetConnectText()
        {
            lblConnectType.Text = UIFunc.GetConnectText();
        }

        /// <summary>
        /// ���ո�Ʈw�s�u�C
        /// </summary>
        /// <param name="databaseItem">��Ʈw���ءC</param>
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
        /// ���� Master.Key�C
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
        /// �z�L Master Key ���ͥ[�K���_�C
        /// </summary>
        private string GenerateEncryptionKey()
        {
            var settings = treeView.Tag as SystemSettings;
            if (settings == null) { return string.Empty; }

            // ���o Master Key
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
        /// �ھڳ]�w�������A�]�w��檺��ܪ��A�C
        /// </summary>
        /// <param name="settings">�w���J���]�w�ɪ���C</param>
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
