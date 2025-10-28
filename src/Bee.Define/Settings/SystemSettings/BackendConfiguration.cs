using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// Backend parameters and environment settings.
    /// </summary>
    [Serializable]
    [XmlType("BackendConfiguration")]
    [Description("Backend parameters and environment settings.")]
    [TreeNode("Backend")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BackendConfiguration
    {
        /// <summary>
        /// Database type.
        /// </summary>
        [Category("Database")]
        [Description("Database type.")]
        [DefaultValue(DatabaseType.SQLServer)]
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;

        /// <summary>
        /// Default database ID.
        /// </summary>
        [Category("Database")]
        [Description("Default database ID.")]
        [DefaultValue("")]
        public string DatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Maximum DbCommand timeout (seconds). 
        /// Default is 60 seconds. Set to 0 for unlimited.
        /// </summary>
        [Category("Database")]
        [Description("Maximum DbCommand timeout (seconds). Default is 60 seconds. Set to 0 for unlimited.")]
        [DefaultValue(60)]
        public int MaxDbCommandTimeout { get; set; } = 60;

        /// <summary>
        /// Logging options for configuring log parameters.
        /// </summary>
        [Category("Logging")]
        [Description("Provides logging options, such as log level and output format.")]
        [Browsable(false)]
        public LogOptions LogOptions { get; set; } = new LogOptions();

        /// <summary>
        /// API KEY.
        /// </summary>
        [Category("API")]
        [Description("API KEY.")]
        [DefaultValue("")]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Encryption key settings.
        /// </summary>
        [Category("Security")]
        [Description("Encryption key settings.")]
        [Browsable(false)]
        public SecurityKeySettings SecurityKeySettings { get; set; } = new SecurityKeySettings();

        /// <summary>
        /// 後端可替換組。
        /// </summary>
        [Category("Components")]
        [Description("後端可替換組")]
        [Browsable(false)]
        public BackendComponents Components { get; set; } = new BackendComponents();

        /// <summary>
        /// Initialization.
        /// </summary>
        public void Initialize()
        {
            // Database type
            BackendInfo.DatabaseType = DatabaseType;
            // Default database ID
            BackendInfo.DatabaseId = DatabaseId;
            // Maximum DbCommand timeout
            BackendInfo.MaxDbCommandTimeout = MaxDbCommandTimeout;
            // Logging options
            BackendInfo.LogOptions = LogOptions;
            // 初始化後端服務的實例
            InitializeComponents();
            // Initialize keys
            InitializeSecurityKeys();
        }

        /// <summary>
        /// 初始化後端服務的實例。
        /// </summary>
        public void InitializeComponents()
        {
            BackendInfo.ApiEncryptionKeyProvider = CreateOrDefault<IApiEncryptionKeyProvider>
                (Components.ApiEncryptionKeyProvider, BackendDefaultTypes.ApiEncryptionKeyProvider);
            BackendInfo.AccessTokenValidationProvider = CreateOrDefault<IAccessTokenValidationProvider>
                (Components.AccessTokenValidationProvider, BackendDefaultTypes.AccessTokenValidationProvider);
            BackendInfo.BusinessObjectProvider = CreateOrDefault<IBusinessObjectProvider>
                (Components.BusinessObjectProvider, BackendDefaultTypes.BusinessObjectProvider);
            BackendInfo.CacheDataSourceProvider = CreateOrDefault<ICacheDataSourceProvider>
                (Components.CacheDataSourceProvider, BackendDefaultTypes.CacheDataSourceProvider);
            BackendInfo.DefineStorage = CreateOrDefault<IDefineStorage>
                (Components.DefineStorage, BackendDefaultTypes.DefineStorage);
            BackendInfo.DefineAccess = CreateOrDefault<IDefineAccess>
                (Components.DefineAccess, BackendDefaultTypes.DefineAccess);
            BackendInfo.SessionInfoService = CreateOrDefault<ISessionInfoService>
                (Components.SessionInfoService, BackendDefaultTypes.SessionInfoService);
            BackendInfo.EnterpriseObjectService = CreateOrDefault<IEnterpriseObjectService>
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
        /// Initialize keys.
        /// </summary>
        /// <param name="autoCreate">Whether to automatically create the master key if it does not exist.</param>
        public void InitializeSecurityKeys(bool autoCreate = false)
        {
            var settings = SecurityKeySettings;
            byte[] masterKey = MasterKeyProvider.GetMasterKey(settings.MasterKeySource, autoCreate);

            // Decrypt API encryption key if provided in settings.
            if (StrFunc.IsNotEmpty(settings.ApiEncryptionKey))
            {
                BackendInfo.ApiEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.ApiEncryptionKey);
            }

            // Decrypt Cookie key if provided in settings.
            if (StrFunc.IsNotEmpty(settings.CookieEncryptionKey))
            {
                BackendInfo.CookieEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.CookieEncryptionKey);
            }

            // Decrypt config file key if provided in settings.
            if (StrFunc.IsNotEmpty(settings.ConfigEncryptionKey))
            {
                BackendInfo.ConfigEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.ConfigEncryptionKey);
            }

            // Decrypt database key if provided in settings.
            if (StrFunc.IsNotEmpty(settings.DatabaseEncryptionKey))
            {
                BackendInfo.DatabaseEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.DatabaseEncryptionKey);
            }
        }

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
