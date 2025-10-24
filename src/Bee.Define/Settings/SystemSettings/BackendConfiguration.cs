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
        /// API encryption key provider type.
        /// </summary>
        [Category("Providers")]
        [Description("API encryption key provider type, defines how to obtain the API data encryption key.")]
        [DefaultValue(DefaultProviderTypes.ApiEncryptionKeyProvider)]
        public string ApiEncryptionKeyProvider { get; set; } = DefaultProviderTypes.ApiEncryptionKeyProvider;

        /// <summary>
        /// Business object provider type.
        /// </summary>
        [Category("Providers")]
        [Description("Business object provider type, defines how to obtain all BusinessObjects.")]
        [DefaultValue(DefaultProviderTypes.BusinessObjectProvider)]
        public string BusinessObjectProvider { get; set; } = DefaultProviderTypes.BusinessObjectProvider;

        /// <summary>
        /// Define provider type.
        /// </summary>
        [Category("Providers")]
        [Description("Define provider type, specifies how to load system definition files (e.g., file, database, etc.).")]
        [DefaultValue(DefaultProviderTypes.DefineProvider)]
        public string DefineProvider { get; set; } = DefaultProviderTypes.DefineProvider;

        /// <summary>
        /// Cache data source provider type.
        /// </summary>
        [Category("Providers")]
        [Description("Cache data source provider type, defines the source of cached data (such as preloaded definition data).")]
        [DefaultValue(DefaultProviderTypes.CacheDataSourceProvider)]
        public string CacheDataSourceProvider { get; set; } = DefaultProviderTypes.CacheDataSourceProvider;

        /// <summary>
        /// AccessToken validation provider type.
        /// </summary>
        [Category("Providers")]
        [Description("AccessToken validation provider type, used to validate the validity of AccessTokens.")]
        [DefaultValue(DefaultProviderTypes.AccessTokenValidationProvider)]
        public string AccessTokenValidationProvider { get; set; } = DefaultProviderTypes.AccessTokenValidationProvider;

        /// <summary>
        /// System level repository provider type.
        /// </summary>
        [Category("Providers")]
        [Description("System level Repository provider type.")]
        [DefaultValue(DefaultProviderTypes.SystemRepositoryProvider)]
        public string SystemRepositoryProvider { get; set; } = DefaultProviderTypes.SystemRepositoryProvider;

        /// <summary>
        /// Form level repository provider type.
        /// </summary>
        [Category("Providers")]
        [Description("Form level Repository provider type.")]
        [DefaultValue(DefaultProviderTypes.SystemRepositoryProvider)]
        public string FormRepositoryProvider { get; set; } = DefaultProviderTypes.FormRepositoryProvider;

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

            // Specify API encryption key provider type
            BackendInfo.ApiEncryptionKeyProvider = BaseFunc.CreateInstance(
                string.IsNullOrWhiteSpace(ApiEncryptionKeyProvider)
                    ? DefaultProviderTypes.ApiEncryptionKeyProvider
                    : ApiEncryptionKeyProvider
            ) as IApiEncryptionKeyProvider;

            // Specify business object provider
            BackendInfo.BusinessObjectProvider = BaseFunc.CreateInstance(
                string.IsNullOrWhiteSpace(BusinessObjectProvider)
                    ? DefaultProviderTypes.BusinessObjectProvider
                    : BusinessObjectProvider
            ) as IBusinessObjectProvider;

            // Specify cache data source provider type
            BackendInfo.CacheDataSourceProvider = BaseFunc.CreateInstance(
                string.IsNullOrWhiteSpace(CacheDataSourceProvider)
                    ? DefaultProviderTypes.CacheDataSourceProvider
                    : CacheDataSourceProvider
            ) as ICacheDataSourceProvider;

            // Specify define provider type
            BackendInfo.DefineProvider = BaseFunc.CreateInstance(
                string.IsNullOrWhiteSpace(DefineProvider)
                    ? DefaultProviderTypes.DefineProvider
                    : DefineProvider
            ) as IDefineProvider;

            // Specify AccessToken validation provider type
            BackendInfo.AccessTokenValidationProvider = BaseFunc.CreateInstance(
                string.IsNullOrWhiteSpace(AccessTokenValidationProvider)
                    ? DefaultProviderTypes.AccessTokenValidationProvider
                    : AccessTokenValidationProvider
            ) as IAccessTokenValidationProvider;

            // Initialize keys
            InitializeSecurityKeys();
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
