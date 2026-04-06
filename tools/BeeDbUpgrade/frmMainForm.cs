using Bee.Define.Settings;
using System.Text;
using Bee.Base;
using Bee.Api.Contracts;
using Bee.Define;
using Bee.UI.Core;
using Bee.UI.WinForms;

namespace DbUpgrade
{
    /// <summary>
    /// �D�����C
    /// </summary>
    public partial class frmMainForm : BaseForm
    {
        private StringBuilder _buffer;  // ����T����X�Ȧs��

        #region �غc�禡

        /// <summary>
        /// �غc�禡�C
        /// </summary>
        public frmMainForm()
        {
            InitializeComponent();
            _buffer = new StringBuilder();
        }

        #endregion

        private void frmMainForm_Load(object sender, EventArgs e)
        {
            // ���J��Ʈw�M��
            LoadDatabases();
            // �]�w�s�u�覡����ܤ�r
            SetConnectText();
        }

        /// <summary>
        /// �]�w�s�u�覡����ܤ�r�C
        /// </summary>
        private void SetConnectText()
        {
            lblConnectType.Text = UIFunc.GetConnectText();
        }

        /// <summary>
        /// ���J��Ʈw�M��C
        /// </summary>
        private void LoadDatabases()
        {
            edtDatabases.Items.Clear();
            var settings = ClientInfo.DefineAccess.GetDatabaseSettings();
            foreach (DatabaseItem item in settings.Items)
                edtDatabases.Items.Add(item);
            if (edtDatabases.Items.Count > 0) { edtDatabases.SelectedIndex = 0; }
        }

        /// <summary>
        /// ����ɯšC
        /// </summary>
        private void btnExecute_Click(object sender, EventArgs e)
        {
            TableItem oTable;

            btnExecute.Enabled = false;
            try
            {
                _buffer.Clear();
                lblMessage.Visible = true;
                int upgradeCount = 0;

                var item = edtDatabases.SelectedItem as DatabaseItem;
                if (item == null) { return; }

                var settings = ClientInfo.DefineAccess.GetDbSchemaSettings();
                var database = settings.Databases[item.DbName];
                int totalCount = database.Tables.Count;
                for (int N1 = 0; N1 < database.Tables.Count; N1++)
                {
                    oTable = database.Tables[N1];
                    SetMessage($"{N1 + 1}/{totalCount} : {oTable.TableName}");
                    if (UpgradeTableSchema(item.DbName, oTable.TableName))
                    {
                        upgradeCount++;
                        _buffer.AppendLine($"{oTable.TableName} : ���c�ɯ�");
                    }
                    else
                    {
                        _buffer.AppendLine($"{oTable.TableName} : ���c�@�P");
                    }
                }
                UIFunc.MsgBox($"���槹���A�@�ɯ� {upgradeCount} �Ӹ�ƪ�");
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
        /// �x�s�O���C
        /// </summary>
        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "�x�s�O��";
            dialog.Filter = "��r��|*.txt";
            dialog.FileName = $"DbToolsLog_{DateTime.Today:yyyyMMdd}.txt";
            if (dialog.ShowDialog() != DialogResult.OK) { return; }

            FileFunc.FileWriteText(dialog.FileName, "DbTools �ɯŰO��\n" + _buffer.ToString());
            System.Diagnostics.Process.Start(dialog.FileName);
        }

        /// <summary>
        /// �ɯŸ�ƪ����c�C
        /// </summary>
        /// <param name="dbName">��Ʈw�W�١C</param>
        /// <param name="tableName">��ƪ��W�١C</param>
        private bool UpgradeTableSchema(string dbName, string tableName)
        {
            var args = new ExecFuncArgs();
            args.FuncId = SysFuncIDs.UpgradeTableSchema;
            args.Parameters.Add("DatabaseId", BackendInfo.DatabaseId);
            args.Parameters.Add("DbName", dbName);
            args.Parameters.Add("TableName", tableName);
            var result = ClientInfo.SystemApiConnector.ExecFunc(args);
            return result.Parameters.GetValue<bool>("Upgraded");
        }

        /// <summary>
        /// �]�w�T����r�C
        /// </summary>
        /// <param name="message"></param>
        private void SetMessage(string message)
        {
            lblMessage.Text = message;
            Application.DoEvents();
        }
    }
}
