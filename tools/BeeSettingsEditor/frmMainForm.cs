using Bee.Base;
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
            // �]�w�s�u�覡����ܤ�r
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
        /// �t�γ]�w�C
        /// </summary>
        private void tbSystemSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(DefineType.SystemSettings);
        }

        /// <summary>
        /// ��Ʈw�]�w�C
        /// </summary>
        private void tbDatabaseSettings_Click(object sender, EventArgs e)
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
            Application.Exit();
        }

        /// <summary>
        /// menuApiConnect�C
        /// </summary>
        private void tbApiConnect_Click(object sender, EventArgs e)
        {
            if (ClientInfo.UIViewService.ShowConnect())
                SetConnectText();
        }

        /// <summary>
        /// ���ո�Ʈw�s�u�C
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

            if (settings != null)
            {
                treeView.BuildObjectTree(settings);
                tbSave.Enabled = true;
            }

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
            string filePath = UIFunc.ShowSaveFileDialog("Master Key|*.key", "Master.Key");
            if (StrFunc.IsEmpty(filePath)) { return; }

            File.WriteAllText(filePath, key);
            UIFunc.MsgBox($"Master.Key has been saved to: {filePath}");
        }


    }
}
