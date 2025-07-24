using System.Threading.Tasks;
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

        /// <summary>
        /// Load event handler for the main form.
        /// </summary>
        private void frmMainForm_Load(object sender, EventArgs e)
        {
            SysInfo.LogWriter = new FormLogWriter(this);
            SysInfo.LogOptions = new LogOptions()
            {
                ApiConnector = new ApiConnectorLogOptions(true, true)
            };
        }

        /// <summary>
        /// Display a log entry in the form.
        /// </summary>
        public void AppendLog(LogEntry entry)
        {
            string message = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}\r\n{entry.Message}\r\n" +
                                            "-------------------------------------------------------------------------\r\n";
            edtLog.AppendText(message + Environment.NewLine);
        }

        /// <summary>
        /// Initialize the system settings and API service options.
        /// </summary>
        private async void btnInitialize_Click(object sender, EventArgs e)
        {
            edtLog.Text = string.Empty;
            try
            {
                // Determine whether the endpoint is a local path or URL, and return the corresponding connection type
                string endpoint = edtEndpoint.Text;
                var validator = new ApiConnectValidator();
                var connectType = validator.Validate(endpoint);

                // Set the connection type
                SetConnectType(connectType, endpoint);

                // Retrieve general parameters and environment settings, and initialize the system
                var connector = CreateSystemApiConnector();
                await connector.InitializeAsync();

                MessageBox.Show("Initialization complete.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Login to the system.
        /// </summary>
        private async Task btnLogin_Click(object sender, EventArgs e)
        {
            edtLog.Text = string.Empty;
            try
            {
                // Log in to the system; no real credential validation here, for demonstration purposes only
                var connector = CreateSystemApiConnector();
                await connector.LoginAsync("jeff", "1234");
                MessageBox.Show($"AccessToken : {FrontendInfo.AccessToken}\nApiEncryptionKey : {Convert.ToBase64String(FrontendInfo.ApiEncryptionKey)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// execute a simple "Hello" function on the server.
        /// </summary>
        private async void btnHello_Click(object sender, EventArgs e)
        {
            edtLog.Text = string.Empty;

            if (FrontendInfo.AccessToken == Guid.Empty)
            {
                MessageBox.Show("Please login first.");
                return;
            }

            try
            {
                // Create a form-level connector. ProgId = "Demo" is not mapped to a custom business object and will use the shared FormBusinessObject.
                var connector = CreateFormApiConnector("Demo");
                var args = new ExecFuncArgs("Hello");
                var result = await connector.ExecuteAsync<ExecFuncResult>("ExecFunc", args);
                string message = result.Parameters.GetValue<string>("Hello");
                MessageBox.Show($"Message: {message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }

        }

        /// <summary>
        /// Set the connection type. Used during initialization.
        /// </summary>
        /// <param name="connectType">Connection type for the service.</param>
        /// <param name="endpoint">Service endpoint. URL for remote, local path for local mode.</param>
        private void SetConnectType(ConnectType connectType, string endpoint)
        {
            Endpoint = endpoint;

            // Set static connection information
            ConnectFunc.SetConnectType(connectType, endpoint);

            // If it is a local connection, simulate server initialization on the client
            if (connectType == ConnectType.Local)
            {
                var settings = CacheFunc.GetSystemSettings();
                settings.Initialize();
            }
        }

        /// <summary>
        /// Create a system-level API connector.
        /// </summary>
        private SystemApiConnector CreateSystemApiConnector()
        {
            if (FrontendInfo.ConnectType == ConnectType.Local)
                return new SystemApiConnector(Guid.Empty);
            else
                return new SystemApiConnector(Endpoint, Guid.Empty);
        }

        /// <summary>
        /// Create a form-level API connector.
        /// </summary>
        /// <param name="progId">Program ID used to identify the function or form.</param>
        private FormApiConnector CreateFormApiConnector(string progId)
        {
            Guid accessToken = Guid.NewGuid();
            if (FrontendInfo.ConnectType == ConnectType.Local)
                return new FormApiConnector(accessToken, progId);
            else
                return new FormApiConnector(Endpoint, accessToken, progId);
        }

    }
}
