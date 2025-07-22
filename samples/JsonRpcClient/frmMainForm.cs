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
        private void btnInitialize_Click(object sender, EventArgs e)
        {
            edtLog.Text = string.Empty; // �M����x���
            try
            {
                // �P�_�A�Ⱥ��I��m�����a���|�κ��}�A�Ǧ^�������s�u�覡
                string endpoint = edtEndpoint.Text;
                var validator = new ApiConnectValidator();
                var connectType = validator.Validate(endpoint);

                // �]�m�s�u�覡
                SetConnectType(connectType, endpoint);

                // ���o�q�ΰѼƤ����ҳ]�m�A�i���l��
                var connector = CreateSystemApiConnector();
                connector.Initialize();

                MessageBox.Show("��l�Ƨ����C");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Login to the system.
        /// </summary>
        private void btnLogin_Click(object sender, EventArgs e)
        {
            edtLog.Text = string.Empty; // �M����x���
            try
            {
                // �n�J�t�ΡA�b�K�������ҡA�Ȭ��ܽd��
                var connector = CreateSystemApiConnector();
                connector.Login("jeff", "1234");
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
        private void btnHello_Click(object sender, EventArgs e)
        {
            edtLog.Text = string.Empty; // �M����x���

            if (FrontendInfo.AccessToken == Guid.Empty)
            {
                MessageBox.Show("�Х�����n�J��k�C");
                return;
            }

            try
            {
                // �إߪ��h�ųs����AProgId=Demo ���ۭq�~���޿誫��A�����ܦ@�Ϊ� FormBusinessObject
                var connector = CreateFormApiConnector("Demo");
                var args = new ExecFuncArgs("Hello");
                var result = connector.Execute<ExecFuncResult>("ExecFunc", args);
                string message = result.Parameters.GetValue<string>("Hello");
                MessageBox.Show($"Message: {message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }

        }

        /// <summary>
        /// �]�m�s�u�覡�A�s�u�]�w�ɨϥΡC
        /// </summary>
        /// <param name="connectType">�A�ȳs�u�覡�C</param>
        /// <param name="endpoint">�A�ݺ��I�A���ݳs�u�����}�A��ݳs�u�����a���|�C</param>
        private void SetConnectType(ConnectType connectType, string endpoint)
        {
            Endpoint = endpoint;

            // �]�m�s�u�覡�����R�A�ݩ�
            ConnectFunc.SetConnectType(connectType, endpoint);

            // �Y����ݳs�u�A�ݦb�Τ�ݼ������A�ݪ���l��
            if (connectType == ConnectType.Local)
            {
                var settings = CacheFunc.GetSystemSettings();
                settings.Initialize();
            }
        }

        /// <summary>
        /// �إߨt�μh�� API �A�ȳs�����C 
        /// </summary>
        private SystemApiConnector CreateSystemApiConnector()
        {
            if (FrontendInfo.ConnectType == ConnectType.Local)
                return new SystemApiConnector(Guid.Empty);  // �s�ݳs�u
            else
                return new SystemApiConnector(Endpoint, Guid.Empty);
        }

        /// <summary>
        /// �إߪ��h�� API �A�ȳs�����C
        /// </summary>
        /// <param name="progId">�{���N�X�C</param>
        private FormApiConnector CreateFormApiConnector(string progId)
        {
            Guid accessToken = Guid.NewGuid();
            if (FrontendInfo.ConnectType == ConnectType.Local)
                return new FormApiConnector(accessToken, progId);  // �s�ݳs�u
            else
                return new FormApiConnector(Endpoint, accessToken, progId);
        }


    }
}
