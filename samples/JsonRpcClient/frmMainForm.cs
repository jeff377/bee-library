using Bee.Api.Core;
using Bee.UI.Core;
using Custom.Contracts;

namespace JsonRpcClient
{
    /// <summary>
    /// Main form for the JSON-RPC client application.
    /// </summary>
    public partial class frmMainForm : Form
    {
        public frmMainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Indicates whether the object has been initialized.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// Load event handler for the main form.
        /// </summary>
        private void frmMainForm_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Initialize the system settings and API service options.
        /// </summary>
        private void btnInitialize_Click(object sender, EventArgs e)
        {
            string endpoint = edtEndpoint.Text;

            try
            {
                // Specify the service endpoint for initialization
                ClientInfo.Initialize(endpoint);
                _isInitialized = true;
                AddMessage("Initialization complete.");
            }
            catch (Exception ex)
            {
                AddMessage($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Login to the system.
        /// </summary>
        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (!ValidateInitialize()) { return; }

            try
            {
                // Log in to the system; no real credential validation here, for demonstration purposes only
                var result = ClientInfo.SystemApiConnector.Login("jeff", "1234");
                // After successful login, set related properties (AccessToken, UserInfo, etc.)
                ClientInfo.ApplyLoginResult(result);
                AddMessage("Login complete.");
            }
            catch (Exception ex)
            {
                AddMessage($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Public hello test method (no login, no encoding/encryption).
        /// </summary>
        private async void btnHello_Click(object sender, EventArgs e)
        {
            await CallEmployeeHelloAsync("Hello", PayloadFormat.Plain);
        }

        /// <summary>
        /// Encoded request ¡X remote call must be serialized and compressed.
        /// Requires login authentication.
        /// </summary>
        private async void btnHelloEncoded_Click(object sender, EventArgs e)
        {
            await CallEmployeeHelloAsync("HelloEncoded", PayloadFormat.Encoded);
        }

        /// <summary>
        /// Encrypted request ¡X remote call must be serialized, compressed, and encrypted.
        /// Requires login authentication.
        /// </summary>
        private async void btnHelloEncrypted_Click(object sender, EventArgs e)
        {
            await CallEmployeeHelloAsync("HelloEncrypted", PayloadFormat.Encrypted);
        }

        /// <summary>
        /// Local only ¡X can only be invoked from local server (no remote API access).
        /// </summary>
        private async void btnHelloLocal_Click(object sender, EventArgs e)
        {
            await CallEmployeeHelloAsync("HelloLocal", PayloadFormat.Plain);
        }

        /// <summary>
        /// Calls the specified Hello test method of the Employee BusinessObject.
        /// </summary>
        /// <param name="method">Hello method name, e.g., "Hello" or "HelloEncoded".</param>
        /// <param name="format">Payload format, e.g., PayloadFormat.Plain or PayloadFormat.Encoded.</param>
        private async Task CallEmployeeHelloAsync(string method, PayloadFormat format)
        {
            if (!ValidateInitialize()) { return; }

            try
            {
                // Create a form-level API connector. ProgId = "Employee" corresponds to the TEmployeeBusinessObject logic class.
                var connector = ClientInfo.CreateFormApiConnector("Employee");
                var args = new HelloArgs { UserName = "Jeff" };

                var result = await connector.ExecuteAsync<HelloResult>(method, args, format);
                AddMessage($"Message: {result.Message}");
            }
            catch (Exception ex)
            {
                AddMessage($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates whether initialization has been completed.
        /// </summary>
        private bool ValidateInitialize()
        {
            if (!_isInitialized)
            {
                AddMessage("Please initialize first.");
                return false;
            }
            return true;
        }

        private void btnShowTraceViewer_Click(object sender, EventArgs e)
        {
            foreach (Form openForm in Application.OpenForms)
            {
                if (openForm is frmTraceViewer traceViewer)
                {
                    traceViewer.WindowState = FormWindowState.Normal;
                    traceViewer.BringToFront();
                    traceViewer.Activate();
                    return;
                }
            }
            var form = new frmTraceViewer();
            form.Show();
        }

        private void AddMessage(string message)
        {
            edtMessage.Items.Add(message);
        }
    }
}
