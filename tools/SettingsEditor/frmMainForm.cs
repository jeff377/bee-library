using Bee.Base;
using Bee.Define;
using Bee.UI.Core;
using Bee.UI.WinForms;

namespace SettingsEditor
{
    /// <summary>
    /// �D�����C
    /// </summary>
    public partial class frmMainForm : TForm
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
        /// �t�γ]�w�C
        /// </summary>
        private void tbSystemSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(EDefineType.SystemSettings);
        }

        /// <summary>
        /// ��Ʈw�]�w�C
        /// </summary>
        private void tbDatabaseSettings_Click(object sender, EventArgs e)
        {
            LoadDefine(EDefineType.DatabaseSettings);
        }

        /// <summary>
        /// �x�s�C
        /// </summary>
        private void tbSave_Click(object sender, EventArgs e)
        {
            object? settings = treeView.Tag;
            if (settings == null) { return; }

            if (settings is TSystemSettings)
            {
                ClientInfo.DefineAccess.SaveSystemSettings(settings as TSystemSettings);
                UIFunc.MsgBox("�t�γ]�w�x�s����");
            }
            else if (settings is TDatabaseSettings)
            {
                ClientInfo.DefineAccess.SaveDatabaseSettings(settings as TDatabaseSettings);
                UIFunc.MsgBox("��Ʈw�]�w�x�s����");
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
        /// �s�u�]�w�C
        /// </summary>
        private void tbConnect_Click(object sender, EventArgs e)
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

            var databaseItem = treeView.SelectedNode.Tag as TDatabaseItem;
            if (databaseItem == null)
            {
                UIFunc.ErrorMsgBox("�п���n���ժ���Ʈw�`�I");
                return;
            }
            TestConnection(databaseItem);
        }

        /// <summary>
        /// ���J�w�q��ơC
        /// </summary>
        /// <param name="defineType">�w�q��������C</param>
        private void LoadDefine(EDefineType defineType)
        {
            string displayName;
            object settings;
            switch (defineType)
            {
                case EDefineType.SystemSettings:
                    settings = ClientInfo.DefineAccess.GetSystemSettings();
                    displayName = "�t�γ]�w";
                    break;
                case EDefineType.DatabaseSettings:
                    settings = ClientInfo.DefineAccess.GetDatabaseSettings();
                    displayName = "��Ʈw�]�w";
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (settings != null)
            {
                treeView.BuildObjectTree(settings);
                tbSave.Text = "�x�s " + displayName;
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
        private void TestConnection(TDatabaseItem databaseItem)
        {
            try
            {
                var args = new TExecFuncArgs(SysFuncIDs.TestConnection);
                args.Parameters.Add("DatabaseItem", databaseItem);
                ClientInfo.SystemConnector.ExecFunc(args);
                UIFunc.MsgBox("��Ʈw�s�u���զ��\");
            }
            catch (Exception ex)
            {
                UIFunc.ErrorMsgBox(ex.Message);
            }
        }
    }
}
