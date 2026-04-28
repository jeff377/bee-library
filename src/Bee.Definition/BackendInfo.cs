using Bee.Definition.Logging;
using Bee.Definition.Storage;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Base;
using Bee.Definition.Database;
using Bee.Definition.Identity;

namespace Bee.Definition
{
    /// <summary>
    /// Backend information, recording the server-side parameters and environment settings at runtime.
    /// </summary>
    public static class BackendInfo
    {
        /// <summary>
        /// Gets or sets the log writer.
        /// </summary>
        public static ILogWriter LogWriter { get; set; } = new NullLogWriter();

        /// <summary>
        /// Gets or sets the logging options for configuring log-related parameters.
        /// </summary>
        public static LogOptions LogOptions { get; set; } = new LogOptions();

        /// <summary>
        /// Gets or sets the definition data path.
        /// </summary>
        public static string DefinePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API transport encryption key.
        /// </summary>
        public static byte[] ApiEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the Cookie data encryption key.
        /// </summary>
        public static byte[] CookieEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the encryption key for sensitive data in configuration files.
        /// </summary>
        public static byte[] ConfigEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the encryption key for sensitive database fields.
        /// </summary>
        public static byte[] DatabaseEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the database type.
        /// </summary>
        public static DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;

        /// <summary>
        /// Gets or sets the default database identifier.
        /// </summary>
        public static string DatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum DbCommand timeout (seconds). Default is 60 seconds; 0 means unlimited.
        /// </summary>
        public static int MaxDbCommandTimeout { get; set; } = 60;

        /// <summary>
        /// Gets or sets the API key provider, used to obtain the AES+HMAC keys required for transport data encryption/decryption.
        /// Supports shared keys and session keys dynamically generated at each login.
        /// </summary>
        public static IApiEncryptionKeyProvider ApiEncryptionKeyProvider { get; set; } = null!;

        /// <summary>
        /// Gets or sets the access token validator, used to verify the validity of access tokens.
        /// </summary>
        public static IAccessTokenValidator AccessTokenValidator { get; set; } = null!;

        /// <summary>
        /// Gets or sets the business object provider, defining how all BusinessObjects are obtained.
        /// </summary>
        public static IBusinessObjectProvider BusinessObjectProvider { get; set; } = null!;

        /// <summary>
        /// Gets or sets the cache data source provider.
        /// </summary>
        public static ICacheDataSourceProvider CacheDataSourceProvider { get; set; } = null!;

        /// <summary>
        /// Gets or sets the define data storage.
        /// </summary>
        public static IDefineStorage DefineStorage { get; set; } = null!;

        /// <summary>
        /// Gets or sets the define data access.
        /// </summary>
        public static IDefineAccess DefineAccess { get; set; } = null!;

        /// <summary>
        /// Gets or sets the session info access service.
        /// </summary>
        public static ISessionInfoService SessionInfoService { get; set; } = null!;

        /// <summary>
        /// Gets or sets the unified access service for commonly used business objects in the enterprise system.
        /// </summary>
        public static IEnterpriseObjectService EnterpriseObjectService { get; set; } = null!;

        /// <summary>
        /// Gets or sets the login attempt tracker for enforcing brute-force protection policies.
        /// When null, login brute-force protection is disabled.
        /// </summary>
        public static ILoginAttemptTracker? LoginAttemptTracker { get; set; }

        /// <summary>
        /// Initializes the backend with the specified configuration.
        /// </summary>
        /// <param name="configuration">Backend parameters and environment settings.</param>
        /// <param name="autoCreateMasterKey">Whether to automatically create the master key if it does not exist.</param>
        public static void Initialize(BackendConfiguration configuration, bool autoCreateMasterKey)
        {
            DatabaseType = configuration.DatabaseType;
            DatabaseId = configuration.DatabaseId;
            MaxDbCommandTimeout = configuration.MaxDbCommandTimeout;
            LogOptions = configuration.LogOptions;
                       
            if (!SysInfo.IsSingleFile)
            {
                // Initialize backend service instances
                InitializeComponents(configuration);
            }

            // Initialize security keys
            InitializeSecurityKeys(configuration, autoCreateMasterKey);
        }

        /// <summary>
        /// Initializes the backend with the specified configuration.
        /// </summary>
        /// <param name="configuration">Backend parameters and environment settings.</param>
        public static void Initialize(BackendConfiguration configuration)
        {
            Initialize(configuration, false);
        }

        /// <summary>
        /// Initializes backend service instances.
        /// </summary>
        /// <param name="configuration">Backend parameters and environment settings.</param>
        private static void InitializeComponents(BackendConfiguration configuration)
        {
            var Components = configuration.Components;
            ApiEncryptionKeyProvider = CreateOrDefault<IApiEncryptionKeyProvider>
                (Components.ApiEncryptionKeyProvider, BackendDefaultTypes.ApiEncryptionKeyProvider);
            AccessTokenValidator = CreateOrDefault<IAccessTokenValidator>
                (Components.AccessTokenValidator, BackendDefaultTypes.AccessTokenValidator);
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
        /// Creates an instance of the specified type; uses <paramref name="fallback"/> if <paramref name="configured"/> is empty.
        /// </summary>
        /// <param name="configured">The type name specified in configuration.</param>
        /// <param name="fallback">The default type name.</param>
        private static T CreateOrDefault<T>(string configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return (BaseFunc.CreateInstance(typeName) as T)!;
        }

        /// <summary>
        /// Initializes security keys.
        /// </summary>
        /// <param name="configuration">Backend parameters and environment settings.</param>
        /// <param name="autoCreateMasterKey">Whether to automatically create the master key if it does not exist.</param>
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
