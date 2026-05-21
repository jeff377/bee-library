using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.DefineAccess;
using Bee.Api.Core.Messages.System;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Definition.Identity;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using System.Reflection;

namespace Bee.UI.Core
{
    /// <summary>
    /// Provides client-side connection state and access to API connectors.
    /// </summary>
    public static class ClientInfo
    {
        private static ClientSettings? _clientSettings;
        private static SystemApiConnector? _systemConnector;
        private static IDefineAccess? _defineAccess;
        private static Guid _accessToken = Guid.Empty;

        /// <summary>
        /// Command-line arguments parsed at <see cref="Initialize(IUIViewService, SupportedConnectTypes)"/>.
        /// </summary>
        public static Dictionary<string, string>? Arguments { get; private set; }

        /// <summary>
        /// Endpoint persistence backend.
        /// </summary>
        public static IEndpointStorage EndpointStorage { get; set; } = new EndpointStorage();

        /// <summary>
        /// Client settings loaded from <c>{ExeName}.Settings.xml</c>.
        /// </summary>
        public static ClientSettings ClientSettings
        {
            get
            {
                _clientSettings ??= LoadClientSettings();
                return _clientSettings;
            }
        }

        private static ClientSettings LoadClientSettings()
        {
            string exeName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Client";
            string fileName = $"{exeName}.Settings.xml";
            string filePath = Path.Combine(FileUtilities.GetAssemblyPath(), fileName);

            if (File.Exists(filePath))
            {
                return XmlCodec.DeserializeFromFile<ClientSettings>(filePath)
                    ?? throw new InvalidOperationException($"Failed to deserialize client settings: {filePath}");
            }

            var settings = new ClientSettings();
            settings.SetObjectFilePath(filePath);
            return settings;
        }

        /// <summary>
        /// Access token issued on a successful login.
        /// </summary>
        public static Guid AccessToken
        {
            get { return _accessToken; }
            private set
            {
                // NOTE: 重設 AccessToken 必須同步清掉 SystemApiConnector 與 IDefineAccess 快取，
                // 否則後續呼叫會帶舊 token 對 server 失敗。
                if (value != _accessToken)
                {
                    _accessToken = value;
                    _systemConnector = null;
                    _defineAccess = null;
                }
            }
        }

        /// <summary>
        /// System-level API connector. Recreated whenever the endpoint changes.
        /// </summary>
        public static SystemApiConnector SystemApiConnector
        {
            get
            {
                _systemConnector ??= CreateSystemApiConnector();
                return _systemConnector;
            }
        }

        private static SystemApiConnector CreateSystemApiConnector()
        {
            return ApiClientInfo.ConnectType == ConnectType.Local
                ? new SystemApiConnector(AccessToken)
                : new SystemApiConnector(ApiClientInfo.Endpoint, AccessToken);
        }

        /// <summary>
        /// Creates a form-level API connector for the specified program.
        /// </summary>
        /// <param name="progId">Program identifier.</param>
        public static FormApiConnector CreateFormApiConnector(string progId)
        {
            return ApiClientInfo.ConnectType == ConnectType.Local
                ? new FormApiConnector(AccessToken, progId)
                : new FormApiConnector(ApiClientInfo.Endpoint, AccessToken, progId);
        }

        /// <summary>
        /// Definition-data accessor. Recreated whenever the endpoint changes.
        /// </summary>
        public static IDefineAccess DefineAccess
        {
            get
            {
                _defineAccess ??= new RemoteDefineAccess(SystemApiConnector);
                return _defineAccess;
            }
        }

        /// <summary>
        /// UI view service supplied by the host application.
        /// </summary>
        public static IUIViewService? UIViewService { get; private set; }

        /// <summary>
        /// Whether <c>System.Settings.xml</c> and <c>Database.Settings.xml</c> should be auto-generated
        /// when the local endpoint is missing the expected files.
        /// </summary>
        public static bool AllowGenerateSettings { get; set; }

        /// <summary>
        /// Authenticated user information set by <see cref="ApplyLoginResult"/>.
        /// </summary>
        public static UserInfo? UserInfo { get; private set; }

        private static void SetConnectType(ConnectType connectType, string endpoint)
        {
            if (connectType == ConnectType.Local)
            {
                ApiClientInfo.ConnectType = ConnectType.Local;
                ApiClientInfo.Endpoint = string.Empty;
            }
            else
            {
                ApiClientInfo.ConnectType = ConnectType.Remote;
                ApiClientInfo.Endpoint = endpoint;
            }
            // NOTE: 連線方式變更必定使既有 token 失效，強制重登。
            AccessToken = Guid.Empty;
        }

        /// <summary>
        /// Sets the service endpoint and persists it.
        /// </summary>
        /// <param name="endpoint">URL for remote connections; local file path for local connections.</param>
        public static void SetEndpoint(string endpoint)
        {
            var connectType = ApiConnectValidator.Validate(endpoint, AllowGenerateSettings);
            SetConnectType(connectType, endpoint);
            SyncExecutor.Run(() => SystemApiConnector.InitializeAsync());
            EndpointStorage.SaveEndpoint(endpoint);
        }

        /// <summary>
        /// Returns the currently configured service endpoint.
        /// </summary>
        public static string GetEndpoint()
        {
            return EndpointStorage.LoadEndpoint();
        }

        private static bool InitializeConnect(SupportedConnectTypes supportedConnectTypes)
        {
            ApiClientInfo.SupportedConnectTypes = supportedConnectTypes;
            try
            {
                string endpoint = GetEndpoint();
                var connectType = ApiConnectValidator.Validate(endpoint, AllowGenerateSettings);
                SetConnectType(connectType, endpoint);
                SyncExecutor.Run(() => SystemApiConnector.InitializeAsync());
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Initializes from settings. Falls back to the connection setup view when the endpoint is
        /// missing or unreachable.
        /// </summary>
        /// <param name="service">UI view service supplied by the host application.</param>
        /// <param name="connectTypes">Connection types supported by the application.</param>
        public static bool Initialize(IUIViewService service, SupportedConnectTypes connectTypes)
        {
            UIViewService = service;
            Arguments = ParseCommandLineArgs();
            if (Arguments.TryGetValue("Endpoint", out string? endpointArg))
            {
                EndpointStorage.SetEndpoint(endpointArg);
            }
            if (!InitializeConnect(connectTypes))
            {
                if (!UIViewService.ShowApiConnect()) { return false; }
            }
            return true;
        }

        /// <summary>
        /// Initializes with an explicit endpoint.
        /// </summary>
        /// <param name="endpoint">URL for remote connections; local file path for local connections.</param>
        public static void Initialize(string endpoint)
        {
            SetEndpoint(endpoint);
        }

        /// <summary>
        /// Applies the login response, populating <see cref="AccessToken"/> and <see cref="UserInfo"/>.
        /// </summary>
        /// <param name="loginResponse">Result returned from the login API.</param>
        public static void ApplyLoginResult(LoginResponse loginResponse)
        {
            ArgumentNullException.ThrowIfNull(loginResponse);

            AccessToken = loginResponse.AccessToken;
            UserInfo = new UserInfo()
            {
                UserId = loginResponse.UserId,
                UserName = loginResponse.UserName
            };
            // TODO: 未來如有其他登入後需設定的屬性，請於此處擴充
        }

        private static Dictionary<string, string> ParseCommandLineArgs()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                int sep = args[i].IndexOf('=');
                if (sep > 0)
                {
                    string key = args[i].Substring(0, sep);
                    string value = args[i].Substring(sep + 1);
                    result[key] = value;
                }
            }
            return result;
        }

    }
}
