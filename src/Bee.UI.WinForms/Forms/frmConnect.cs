using Bee.Base;
using Bee.Define;
using Bee.UI.Core;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 連線設定。
    /// </summary>
    internal partial class frmConnect : TForm
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public frmConnect()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load 事件。
        /// </summary>
        private void frmConnect_Load(object sender, EventArgs e)
        {
            edtEndpoint.Text = ClientInfo.GetEndpoint();
            if (!FrontendInfo.SupportedConnectTypes.HasFlag(SupportedConnectTypes.Remote))
            {
                lblEndpoint.Text = "Definition path";
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
