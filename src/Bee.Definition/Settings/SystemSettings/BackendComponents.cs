using System.ComponentModel;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Settings for replaceable backend components, defining the type names for various backend services.
    /// </summary>
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
        /// Company info service type.
        /// </summary>
        [Category("Service")]
        [Description("Company info service type.")]
        [DefaultValue(BackendDefaultTypes.CompanyInfoService)]
        public string CompanyInfoService { get; set; } = BackendDefaultTypes.CompanyInfoService;

        /// <summary>
        /// Repository factory type.
        /// </summary>
        /// <remarks>
        /// One entry for both axes: the same factory serves progId-bound repositories and framework
        /// ones, so replacing it replaces all repository creation at once. It used to take two
        /// entries, which meant a host overriding one silently kept the framework's own for the other.
        /// </remarks>
        [Category("Repository")]
        [Description("Repository factory type, defines how every repository is created.")]
        [DefaultValue(BackendDefaultTypes.RepositoryFactory)]
        public string RepositoryFactory { get; set; } = BackendDefaultTypes.RepositoryFactory;


    }
}
