using Bee.Base;
using Bee.Connect;
using Bee.Define;
using Bee.UI.Core;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 連線設定。
    /// </summary>
    internal partial class ApiConnectForm : BaseForm
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public ApiConnectForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load 事件。
        /// </summary>
        private void frmConnect_Load(object sender, EventArgs e)
        {
            edtEndpoint.Text = ClientInfo.GetEndpoint();
            if (!ApiClientContext.SupportedConnectTypes.HasFlag(SupportedConnectTypes.Remote))
            {
                Text += " (Local Only)";
            }
            else if (!ApiClientContext.SupportedConnectTypes.HasFlag(SupportedConnectTypes.Local))
            {
                Text += " (Remote Only)";
            }
            else
            {
                Text += " (Local/Remote)";
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string endpoint = StrFunc.Trim(edtEndpoint.Text);
            try
            {
                // 設置服務端點
                ClientInfo.SetEndpoint(endpoint);
            }
            catch (Exception ex)
            {
                UIFunc.ErrorMsgBox(ex.Message);
                return;
            }
            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
