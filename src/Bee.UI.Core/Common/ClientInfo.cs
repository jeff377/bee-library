using System;
using System.Collections.Generic;
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
        private static TClientSettings _clientSettings = null;
        private static TSystemConnector _systemConnector = null;
        private static IDefineAccess _defineAccess = null;

        /// <summary>
        /// 命令列引數。
        /// </summary>
        public static Dictionary<string, string> Arguments { get; private set; } = null;

        /// <summary>
        /// 用戶端設定。
        /// </summary>
        public static TClientSettings ClientSettings
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
        private static TClientSettings GetClientSettings()
        {
            string filePath = FileFunc.GetAppPath("Client.Settings.xml");

            TClientSettings settings;
            if (FileFunc.FileExists(filePath))
            {
                settings = SerializeFunc.XmlFileToObject<TClientSettings>(filePath);
            }
            else
            {
                settings = new TClientSettings();
                settings.SetObjectFilePath(filePath);
            }
            return settings;
        }

        /// <summary>
        /// 系統層級服務連線器，服務端點異動時需重新建立。
        /// </summary>
        public static TSystemConnector SystemConnector
        {
            get
            {
                if (_systemConnector == null)
                {
                    _systemConnector = CreateSystemConnector();
                }
                return _systemConnector;
            }

        }

        /// <summary>
        /// 建立系統層級服務連線器。
        /// </summary>
        /// <returns></returns>
        private static TSystemConnector CreateSystemConnector()
        {
            if (FrontendInfo.ConnectType == EConnectType.Local)
                return new TSystemConnector(FrontendInfo.AccessToken);
            else
                return new TSystemConnector(FrontendInfo.Endpoint, FrontendInfo.AccessToken);
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
                    _defineAccess = new TApiDefineAccess(SystemConnector);
                }
                return _defineAccess;
            }
        }

        /// <summary>
        /// UI 相關的視窗 (View) 服務。
        /// </summary>
        public static IUIViewService UIViewService { get; private set;}

        /// <summary>
        /// 近端連線指定端點不在存設定檔時，是否自動生成 System.Settings.xml 及 Database.Settings.xml 設定檔。
        /// </summary>
        public static bool AllowGenerateSettings { get; private set; }

        /// <summary>
        /// 連線資訊，需登入驗證的應用程式才會有連線資訊。
        /// </summary>
        public static TSessionInfo SessionInfo { get; private set; }

        /// <summary>
        /// 設定連線資訊，用戶登入後使用。
        /// </summary>
        /// <param name="sessionInfo">連線資訊。</param>
        public static void SetSessionInfo(TSessionInfo sessionInfo)
        {
            SessionInfo = sessionInfo;
            FrontendInfo.AccessToken = sessionInfo.AccessToken;
            // 更新 AccessToken 需重置 SystemConnector 及 DefineAccess
            _systemConnector = CreateSystemConnector();
            _defineAccess = new TApiDefineAccess(SystemConnector);
        }

        /// <summary>
        /// 設置連線方式，連線設定時使用。
        /// </summary>
        /// <param name="connectType">服務連線方式。</param>
        /// <param name="endpoint">服端端點，遠端連線為網址，近端連線為本地路徑。</param>
        private static void SetConnectType(EConnectType connectType, string endpoint)
        {
            if (connectType == EConnectType.Local)
            {
                // 設定近端連線相關屬性
                FrontendInfo.ConnectType = EConnectType.Local;
                FrontendInfo.Endpoint = string.Empty;
                BackendInfo.DefinePath = endpoint;
            }
            else
            {
                // 設定遠端連線相關屬性
                FrontendInfo.ConnectType = EConnectType.Remote;
                FrontendInfo.Endpoint = endpoint;
                BackendInfo.DefinePath = string.Empty;
            }
            // 變更連線需重置 SystemConnector 及 DefineAccess
            _systemConnector = null;
            _defineAccess = null;
            FrontendInfo.AccessToken = Guid.Empty;
        }

        /// <summary>
        /// 設置服務端點。
        /// </summary>
        /// <param name="endpoint">服務端點位置，遠端連線為網址，近端連線為本地路徑。</param>
        public static void SetEndpoint(string endpoint)
        {
            // 判斷服務端點位置為本地路徑或網址，傳回對應的連線方式
            var validator = new TApiConnectValidator();
            var connectType = validator.Validate(endpoint, AllowGenerateSettings);
            // 設置連線方式
            SetConnectType(connectType, endpoint);
            // 儲存用戶端設定
            ClientSettings.Endpoint = endpoint;
            ClientSettings.Save();
        }

        /// <summary>
        /// 初始化連線設置。
        /// </summary>
        /// <param name="supportedConnectTypes">程式支援的服務連線方式。</param>
        private static bool InitializeConnect(ESupportedConnectTypes supportedConnectTypes)
        {
            FrontendInfo.SupportedConnectTypes = supportedConnectTypes;
            var validator = new TApiConnectValidator();
            try
            {
                // 驗證服務端點，傳回對應的連線方式
                var connectType = validator.Validate(ClientSettings.Endpoint, AllowGenerateSettings);
                // 設置連線方式
                SetConnectType(connectType, ClientSettings.Endpoint);
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
        /// <param name="allowGenerateSettings">近端連結是否允許自動生成設定檔，包含 System.Settings.xml 及 Database.Settings.xml 設定檔。</param>
        public static bool Initialize(IUIViewService service, ESupportedConnectTypes connectTypes, bool allowGenerateSettings)
        {
            // 設置 UI 相關的視窗 (View) 服務
            UIViewService = service;
            // 近端連結是否允許自動生成設定檔
            AllowGenerateSettings = allowGenerateSettings;
            // 取得命令列引數
            Arguments = BaseFunc.GetCommandLineArgs();
            if (Arguments.ContainsKey("Endpoint"))
            {
                ClientSettings.Endpoint = Arguments["Endpoint"];
            }
            // 初始化連線設置
            if (!ClientInfo.InitializeConnect(connectTypes))
            {
                // 初始化連線設置失敗，要求重新設定連線
                if (!UIViewService.ShowConnect()) { return false; }
            }
            return true;
        }
    }
}
