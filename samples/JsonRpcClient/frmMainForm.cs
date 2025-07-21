using Bee.Api.Core;

using Bee.Base;
using Bee.Cache;
using Bee.Connect;
using Bee.Define;
using Bee.UI.WinForms;

namespace JsonRpcClient
{
    /// <summary>
    /// Main form for the JSON-RPC client application.
    /// </summary>
    public partial class frmMainForm : Form, ILogDisplayForm
    {
        public frmMainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Endpoint for the API service.
        /// </summary>
        private string Endpoint { get; set; } = string.Empty;

        private void frmMainForm_Load(object sender, EventArgs e)
        {
            SysInfo.LogWriter = new FormLogWriter(this);
        }

        /// <summary>
        /// Display a log entry in the form.
        /// </summary>
        public void AppendLog(LogEntry entry)
        {
            string message = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}\n{entry.Message}";
            edtLog.AppendText(message + Environment.NewLine);
        }

        /// <summary>
        /// Initialize the system settings and API service options.
        /// </summary>
        private void btnInitialize_Click(object sender, EventArgs e)
        {
            // 判斷服務端點位置為本地路徑或網址，傳回對應的連線方式
            string endpoint = edtEndpoint.Text;
            var validator = new ApiConnectValidator();
            var connectType = validator.Validate(endpoint);

            // 設置連線方式
            SetConnectType(connectType, endpoint);

            // 初始化 API 服務選項，設定序列化器、壓縮器與加密器的實作
            var connector = CreateSystemApiConnector();
            connector.Initialize();

            MessageBox.Show("系統設定初始化完成。");
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // 產生 RSA 對稱金鑰
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml, out var privateKeyXml);

            var connector = CreateSystemApiConnector();
            connector.Login("jeff", "1234");
        }

        /// <summary>
        /// 設置連線方式，連線設定時使用。
        /// </summary>
        /// <param name="connectType">服務連線方式。</param>
        /// <param name="endpoint">服端端點，遠端連線為網址，近端連線為本地路徑。</param>
        private void SetConnectType(ConnectType connectType, string endpoint)
        {
            Endpoint = endpoint;
            if (connectType == ConnectType.Local)
            {
                // 設定近端連線相關屬性
                FrontendInfo.ConnectType = ConnectType.Local;
                FrontendInfo.Endpoint = string.Empty;
                BackendInfo.DefinePath = endpoint;

                // 若為近端連線，需在用戶端模擬伺服端的初始化
                var settings = CacheFunc.GetSystemSettings();
                settings.Initialize();
            }
            else
            {
                // 設定遠端連線相關屬性
                FrontendInfo.ConnectType = ConnectType.Remote;
                FrontendInfo.Endpoint = endpoint;
                BackendInfo.DefinePath = string.Empty;
            }
        }

        /// <summary>
        /// 建立系統層級 API 服務連接器。 
        /// </summary>
        private SystemApiConnector CreateSystemApiConnector()
        {
            if (FrontendInfo.ConnectType == ConnectType.Local)
                return new SystemApiConnector(Guid.Empty);  // 連端連線
            else
                return new SystemApiConnector(Endpoint, Guid.Empty);
        }

        /// <summary>
        /// 建立表單層級 API 服務連接器。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        private FormApiConnector CreateFormApiConnector(string progId)
        {
            Guid accessToken = Guid.NewGuid();
            if (FrontendInfo.ConnectType == ConnectType.Local)
                return new FormApiConnector(accessToken, progId);  // 連端連線
            else
                return new FormApiConnector(Endpoint, accessToken, progId);
        }

        private void btnHello_Click(object sender, EventArgs e)
        {
            try
            {
                // 建立表單層級連接單，ProgId=Demo 未自訂業務邏輯物件，對應至共用的 FormBusinessObject
                var connector = CreateFormApiConnector("Demo");
                var args = new ExecFuncArgs("Hello");
                var result = connector.Execute<ExecFuncResult>("ExecFunc", args);
                string message = result.Parameters.GetValue<string>("Hello");
                MessageBox.Show($"Message: {message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"發生錯誤: {ex.Message}");
            }

        }
    }
}
