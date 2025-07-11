using System;
using System.Collections.Generic;
using System.Reflection;
using Bee.Api.Core;
using Bee.Base;
using Bee.Connect;
using Bee.Define;

namespace Bee.UI.Core
{
    /// <summary>
    /// 用戶端資訊。
    /// </summary>
    public class ClientInfo
    {
        private static ClientSettings _clientSettings = null;
        private static SystemApiConnector _systemConnector = null;
        private static IDefineAccess _defineAccess = null;

        /// <summary>
        /// 命令列引數。
        /// </summary>
        public static Dictionary<string, string> Arguments { get; private set; } = null;

        /// <summary>
        /// 服務端點儲存區。
        /// </summary>
        public static IEndpointStorage EndpointStorage { get; private set; } = new EndpointStorage();

        /// <summary>
        /// 用戶端設定。
        /// </summary>
        public static ClientSettings ClientSettings
        {
            get
            {
                if (_clientSettings == null)
                {
                    _clientSettings = GetClientSettings();
                }
                return _clientSettings;
            }
        }

        /// <summary>
        /// 取得用戶端設定。
        /// </summary>
        private static ClientSettings GetClientSettings()
        {
            // 取得執行檔名稱（不含副檔名）
            string exeName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Client";
            string fileName = $"{exeName}.Settings.xml";
            string filePath = FileFunc.GetAppPath(fileName);

            ClientSettings settings;
            if (FileFunc.FileExists(filePath))
            {
                settings = SerializeFunc.XmlFileToObject<ClientSettings>(filePath);
            }
            else
            {
                settings = new ClientSettings();
                settings.SetObjectFilePath(filePath);
            }
            return settings;
        }

        /// <summary>
        /// 系統層級 API 服務連接器，服務端點異動時需重新建立。
        /// </summary>
        public static SystemApiConnector SystemApiConnector
        {
            get
            {
                if (_systemConnector == null)
                {
                    _systemConnector = CreateSystemApiConnector();
                }
                return _systemConnector;
            }

        }

        /// <summary>
        /// 建立系統層級 API 服務連接器。
        /// </summary>
        /// <returns></returns>
        private static SystemApiConnector CreateSystemApiConnector()
        {
            if (FrontendInfo.ConnectType == ConnectType.Local)
                return new SystemApiConnector(FrontendInfo.AccessToken);
            else
                return new SystemApiConnector(FrontendInfo.Endpoint, FrontendInfo.AccessToken);
        }

        /// <summary>
        /// 建立表單層級 API 服務連接器。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public static FormApiConnector CreateFormApiConnector(string progId)
        {
            if (FrontendInfo.ConnectType == ConnectType.Local)
                return new FormApiConnector(FrontendInfo.AccessToken, progId);
            else
                return new FormApiConnector(FrontendInfo.Endpoint, FrontendInfo.AccessToken, progId);
        }

        /// <summary>
        /// 定義資料存取，服務端點異動時需重新建立。
        /// </summary>
        public static IDefineAccess DefineAccess
        {
            get
            {
                if (_defineAccess == null)
                {
                    _defineAccess = new ApiDefineAccess(SystemApiConnector);
                }
                return _defineAccess;
            }
        }

        /// <summary>
        /// UI 相關的視窗 (View) 服務。
        /// </summary>
        public static IUIViewService UIViewService { get; private set; }

        /// <summary>
        /// 近端連線指定端點不在存設定檔時，是否自動生成 System.Settings.xml 及 Database.Settings.xml 設定檔。
        /// </summary>
        public static bool AllowGenerateSettings { get; set; }

        /// <summary>
        /// 連線資訊，需登入驗證的應用程式才會有連線資訊。
        /// </summary>
        public static SessionInfo SessionInfo { get; private set; }

        /// <summary>
        /// 設定連線資訊，用戶登入後使用。
        /// </summary>
        /// <param name="sessionInfo">連線資訊。</param>
        public static void SetSessionInfo(SessionInfo sessionInfo)
        {
            SessionInfo = sessionInfo;
            FrontendInfo.AccessToken = sessionInfo.AccessToken;
            // 更新 AccessToken 需重置 SystemConnector 及 DefineAccess
            _systemConnector = CreateSystemApiConnector();
            _defineAccess = new ApiDefineAccess(SystemApiConnector);
        }

        /// <summary>
        /// 設置連線方式，連線設定時使用。
        /// </summary>
        /// <param name="connectType">服務連線方式。</param>
        /// <param name="endpoint">服端端點，遠端連線為網址，近端連線為本地路徑。</param>
        private static void SetConnectType(ConnectType connectType, string endpoint)
        {
            if (connectType == ConnectType.Local)
            {
                // 設定近端連線相關屬性
                FrontendInfo.ConnectType = ConnectType.Local;
                FrontendInfo.Endpoint = string.Empty;
                BackendInfo.DefinePath = endpoint;
            }
            else
            {
                // 設定遠端連線相關屬性
                FrontendInfo.ConnectType = ConnectType.Remote;
                FrontendInfo.Endpoint = endpoint;
                BackendInfo.DefinePath = string.Empty;
            }
            // 變更連線需重置 SystemConnector 及 DefineAccess
            _systemConnector = null;
            _defineAccess = null;
            FrontendInfo.AccessToken = Guid.Empty;
        }

        /// <summary>
        /// 初始化 API 服務選項，API 服務端點異動時需重新建立。
        /// </summary>
        private static void ApiServiceOptionsInitialize()
        {
            var args = new GetApiPayloadOptionsArgs();
            var result = SystemApiConnector.Execute<GetApiPayloadOptionsResult>(SystemActions.GetApiPayloadOptions, args, false);
            var payloadOptions = new ApiPayloadOptions()
            {
                Serializer = result.Serializer,
                Compressor = result.Compressor,
                Encryptor = result.Encryptor
            };
            ApiServiceOptions.Initialize(payloadOptions);
        }

        /// <summary>
        /// 設置服務端點。
        /// </summary>
        /// <param name="endpoint">服務端點位置，遠端連線為網址，近端連線為本地路徑。</param>
        public static void SetEndpoint(string endpoint)
        {
            // 判斷服務端點位置為本地路徑或網址，傳回對應的連線方式
            var validator = new ApiConnectValidator();
            var connectType = validator.Validate(endpoint, AllowGenerateSettings);
            // 設置連線方式
            SetConnectType(connectType, endpoint);
            // 初始化 API 服務選項
            ApiServiceOptionsInitialize();
            // 儲存服務端點
            EndpointStorage.SaveEndpoint(endpoint);
        }

        /// <summary>
        /// 取得目前設置服務端點。
        /// </summary>
        public static string GetEndpoint()
        {
            return EndpointStorage.LoadEndpoint();
        }

        /// <summary>
        /// 初始化連線設置。
        /// </summary>
        /// <param name="supportedConnectTypes">程式支援的服務連線方式。</param>
        private static bool InitializeConnect(SupportedConnectTypes supportedConnectTypes)
        {
            FrontendInfo.SupportedConnectTypes = supportedConnectTypes;
            var validator = new ApiConnectValidator();
            try
            {
                // 取得目前設置服務端點
                string endpoint = GetEndpoint();
                // 驗證服務端點，傳回對應的連線方式
                var connectType = validator.Validate(endpoint, AllowGenerateSettings);
                // 設置連線方式
                SetConnectType(connectType, endpoint);
                // 初始化 API 服務選項
                ApiServiceOptionsInitialize();
                return true;
            }
            catch
            {
                // 若連線初始化失敗，傳回 false，要求用戶重新設定
                return false;
            }
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        /// <param name="service">UI 相關的視窗 (View) 服務。</param>
        /// <param name="connectTypes">程式支援的服務連線方式。</param>
        public static bool Initialize(IUIViewService service, SupportedConnectTypes connectTypes)
        {
            // 設置 UI 相關的視窗 (View) 服務
            UIViewService = service;
            // 取得命令列引數
            Arguments = BaseFunc.GetCommandLineArgs();
            if (Arguments.ContainsKey("Endpoint"))
            {
                // 由命令列引數設定服務端點
                EndpointStorage.SetEndpoint(Arguments["Endpoint"]);
            }
            // 初始化連線設置
            if (!ClientInfo.InitializeConnect(connectTypes))
            {
                // 初始化連線設置失敗，要求重新設定連線
                if (!UIViewService.ShowApiConnect()) { return false; }
            }
            return true;
        }
    }
}
