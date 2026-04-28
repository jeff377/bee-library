using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Settings for replaceable backend components, defining the type names for various backend services.
    /// </summary>
    [Serializable]
    [XmlType("BackendComponents")]
    [Description("Settings for replaceable backend components, defining the type names for various backend services.")]
    [TreeNode("Components")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BackendComponents
    {
        /// <summary>
        /// API encryption key provider type.
        /// </summary>
        [Category("Providers")]
        [Description("API encryption key provider type, defines how to obtain the API data encryption key.")]
        [DefaultValue(BackendDefaultTypes.ApiEncryptionKeyProvider)]
        public string ApiEncryptionKeyProvider { get; set; } = BackendDefaultTypes.ApiEncryptionKeyProvider;

        /// <summary>
        /// Access token validator type.
        /// </summary>
        [Category("Validators")]
        [Description("Access token validator type, used to verify the validity of access tokens.")]
        [DefaultValue(BackendDefaultTypes.AccessTokenValidator)]
        public string AccessTokenValidator { get; set; } = BackendDefaultTypes.AccessTokenValidator;

        /// <summary>
        /// Business object factory type.
        /// </summary>
        [Category("Factories")]
        [Description("Business object factory type, defines how to create all BusinessObjects per API call.")]
        [DefaultValue(BackendDefaultTypes.BusinessObjectFactory)]
        public string BusinessObjectFactory { get; set; } = BackendDefaultTypes.BusinessObjectFactory;

        /// <summary>
        /// Cache provider type.
        /// </summary>
        [Category("Cache")]
        [Description("Cache provider type, defines the cache mechanism implementation (e.g., MemoryCache, Redis).")]
        [DefaultValue(BackendDefaultTypes.CacheProvider)]
        public string CacheProvider { get; set; } = BackendDefaultTypes.CacheProvider;

        /// <summary>
        /// Cache data source provider type.
        /// </summary>
        [Category("Cache")]
        [Description("Cache data source provider type, defines the source of cached data (such as preloaded definition data).")]
        [DefaultValue(BackendDefaultTypes.CacheDataSourceProvider)]
        public string CacheDataSourceProvider { get; set; } = BackendDefaultTypes.CacheDataSourceProvider;

        /// <summary>
        /// Define storage type.
        /// </summary>
        [Category("Define")]
        [Description("Define storage type, specifies how to load system definition files (e.g., file, database, etc.).")]
        [DefaultValue(BackendDefaultTypes.DefineStorage)]
        public string DefineStorage { get; set; } = BackendDefaultTypes.DefineStorage;

        /// <summary>
        /// Define access type.
        /// </summary>
        [Category("Define")]
        [Description("Define access type.")]
        [DefaultValue(BackendDefaultTypes.DefineAccess)]
        public string DefineAccess { get; set; } = BackendDefaultTypes.DefineAccess;

        /// <summary>
        /// Session info service type.
        /// </summary>
        [Category("Service")]
        [Description("Session info service type.")]
        [DefaultValue(BackendDefaultTypes.SessionInfoService)]
        public string SessionInfoService { get; set; } = BackendDefaultTypes.SessionInfoService;

        /// <summary>
        /// Unified access service type for commonly used enterprise business objects.
        /// </summary>
        [Category("Service")]
        [Description("Unified access service type for commonly used enterprise business objects.")]
        [DefaultValue(BackendDefaultTypes.EnterpriseObjectService)]
        public string EnterpriseObjectService { get; set; } = BackendDefaultTypes.EnterpriseObjectService;

        /// <summary>
        /// System level repository provider type.
        /// </summary>
        [Category("Repository")]
        [Description("System level Repository provider type.")]
        [DefaultValue(BackendDefaultTypes.SystemRepositoryProvider)]
        public string SystemRepositoryProvider { get; set; } = BackendDefaultTypes.SystemRepositoryProvider;

        /// <summary>
        /// Form level repository provider type.
        /// </summary>
        [Category("Repository")]
        [Description("Form level Repository provider type.")]
        [DefaultValue(BackendDefaultTypes.FormRepositoryProvider)]
        public string FormRepositoryProvider { get; set; } = BackendDefaultTypes.FormRepositoryProvider;


    }
}
