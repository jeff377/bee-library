using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 後端資訊，記錄伺服端在運行期間的參數及環境設置。
    /// </summary>
    public static class BackendInfo
    {
        /// <summary>
        /// 日誌寫入器。
        /// </summary>
        public static ILogWriter LogWriter { get; set; } = new NullLogWriter();

        /// <summary>
        /// 記錄選項，用於設定日誌記錄的相關參數。
        /// </summary>
        public static LogOptions LogOptions { get; set; } = new LogOptions();

        /// <summary>
        /// 定義資料路徑。
        /// </summary>
        public static string DefinePath { get; set; } = string.Empty;

        /// <summary>
        /// API 傳輸加密金鑰。
        /// </summary>
        public static byte[] ApiEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Cookie 資料加密金鑰。
        /// </summary>
        public static byte[] CookieEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 設定檔機敏資料加密金鑰。
        /// </summary>
        public static byte[] ConfigEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 資料庫機敏欄位加密金鑰。
        /// </summary>
        public static byte[] DatabaseEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        public static DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;

        /// <summary>
        /// 預設資料庫識別。
        /// </summary>
        public static string DatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// 最大 DbCommand 逾時（秒），預設 60 秒，0 表示不限制。
        /// </summary>
        public static int MaxDbCommandTimeout { get; set; } = 60;

        /// <summary>
        /// API 金鑰提供者，用於取得傳輸資料加解密所需的 AES+HMAC 金鑰。
        /// 支援共用金鑰與每次登入動態產生的 Session 金鑰。
        /// </summary>
        public static IApiEncryptionKeyProvider ApiEncryptionKeyProvider { get; set; }

        /// <summary>
        /// AccessToken 驗證提供者，用於驗證 AccessToken 的有效性。
        /// </summary>
        public static IAccessTokenValidationProvider AccessTokenValidationProvider { get; set; }

        /// <summary>
        /// 業務邏輯物件提供者，定義所有 BusinessObject 的取得方式。
        /// </summary>
        public static IBusinessObjectProvider BusinessObjectProvider { get; set; }

        /// <summary>
        /// 快取資料來源提供者。
        /// </summary>
        public static ICacheDataSourceProvider CacheDataSourceProvider { get; set; }

        /// <summary>
        /// 定義資料儲存區。
        /// </summary>
        public static IDefineStorage DefineStorage { get; set; }

        /// <summary>
        /// 定義資料存取。
        /// </summary>
        public static IDefineAccess DefineAccess { get; set; }

        /// <summary>
        /// 連線資訊存取服務。
        /// </summary>
        public static ISessionInfoService SessionInfoService { get; set; }

        /// <summary>
        /// 提供企業系統中常用業務物件的統一存取服務。
        /// </summary>
        public static IEnterpriseObjectService EnterpriseObjectService { get; set; }

        /// <summary>
        /// 初始化。
        /// </summary>
        /// <param name="configuration">後端參數與環境設置。</param>
        /// <param name="autoCreateMasterKey">若主金鑰不存在時是否自動建立。</param>
        public static void Initialize(BackendConfiguration configuration, bool autoCreateMasterKey)
        {
            DatabaseType = configuration.DatabaseType;
            DatabaseId = configuration.DatabaseId;
            MaxDbCommandTimeout = configuration.MaxDbCommandTimeout;
            LogOptions = configuration.LogOptions;
                       
            if (!SysInfo.IsSingleFile)
            {
                // 初始化後端服務的實例
                InitializeComponents(configuration);
            }
  
            // 初始化安全性金鑰
            InitializeSecurityKeys(configuration, autoCreateMasterKey);
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        /// <param name="configuration">後端參數與環境設置。</param>
        public static void Initialize(BackendConfiguration configuration)
        {
            Initialize(configuration, false);
        }

        /// <summary>
        /// 初始化後端服務的實例。
        /// </summary>
        /// <param name="configuration">後端參數與環境設置。</param>
        private static void InitializeComponents(BackendConfiguration configuration)
        {
            var Components = configuration.Components;
            ApiEncryptionKeyProvider = CreateOrDefault<IApiEncryptionKeyProvider>
                (Components.ApiEncryptionKeyProvider, BackendDefaultTypes.ApiEncryptionKeyProvider);
            AccessTokenValidationProvider = CreateOrDefault<IAccessTokenValidationProvider>
                (Components.AccessTokenValidationProvider, BackendDefaultTypes.AccessTokenValidationProvider);
            BusinessObjectProvider = CreateOrDefault<IBusinessObjectProvider>
                (Components.BusinessObjectProvider, BackendDefaultTypes.BusinessObjectProvider);
            CacheDataSourceProvider = CreateOrDefault<ICacheDataSourceProvider>
                (Components.CacheDataSourceProvider, BackendDefaultTypes.CacheDataSourceProvider);
            DefineStorage = CreateOrDefault<IDefineStorage>
                (Components.DefineStorage, BackendDefaultTypes.DefineStorage);
            DefineAccess = CreateOrDefault<IDefineAccess>
                (Components.DefineAccess, BackendDefaultTypes.DefineAccess);
            SessionInfoService = CreateOrDefault<ISessionInfoService>
                (Components.SessionInfoService, BackendDefaultTypes.SessionInfoService);
            EnterpriseObjectService = CreateOrDefault<IEnterpriseObjectService>
                (Components.EnterpriseObjectService, BackendDefaultTypes.EnterpriseObjectService);
        }

        /// <summary>
        /// 建立指定型別的實例，若 <paramref name="configured"/> 為空則使用 <paramref name="fallback"/>。
        /// </summary>
        /// <param name="configured">組態指定的型別名稱。</param>
        /// <param name="fallback">預設型別名稱。</param>
        private static T CreateOrDefault<T>(string configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return BaseFunc.CreateInstance(typeName) as T;
        }

        /// <summary>
        /// 初始化安全性金鑰。
        /// </summary>
        /// <param name="configuration">後端參數與環境設置。</param>
        /// <param name="autoCreateMasterKey">若主金鑰不存在時是否自動建立。</param>
        private static void InitializeSecurityKeys(BackendConfiguration configuration, bool autoCreateMasterKey)
        {
            var settings = configuration.SecurityKeySettings;
            byte[] masterKey = MasterKeyProvider.GetMasterKey(settings.MasterKeySource, autoCreateMasterKey);

            if (StrFunc.IsNotEmpty(settings.ApiEncryptionKey))
                BackendInfo.ApiEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.ApiEncryptionKey);

            if (StrFunc.IsNotEmpty(settings.CookieEncryptionKey))
                BackendInfo.CookieEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.CookieEncryptionKey);

            if (StrFunc.IsNotEmpty(settings.ConfigEncryptionKey))
                BackendInfo.ConfigEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.ConfigEncryptionKey);

            if (StrFunc.IsNotEmpty(settings.DatabaseEncryptionKey))
                BackendInfo.DatabaseEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.DatabaseEncryptionKey);
        }
    }
}
