namespace Bee.Definition
{
    /// <summary>
    /// Defines default type name constants for commonly used backend implementations.
    /// Can be used for type specification in the SystemSettings.xml configuration file or as default fallback values.
    /// </summary>
    public static class BackendDefaultTypes
    {
        // ---------------- Providers ----------------
        /// <summary>
        /// Default API encryption key provider type. The deriving provider is the default because
        /// its key survives cache eviction and is identical on every node, which session rebuild
        /// depends on; it requires <c>SecurityKeySettings.ApiEncryptionKey</c> to be configured.
        /// </summary>
        public const string ApiEncryptionKeyProvider = "Bee.Business.Providers.DerivedApiEncryptionKeyProvider, Bee.Business";
        /// <summary>
        /// Default access token validator, used to verify the validity of access tokens.
        /// </summary>
        public const string AccessTokenValidator = "Bee.Business.Validator.AccessTokenValidator, Bee.Business";
        // ---------------- Cache ----------------
        /// <summary>
        /// Default cache provider type.
        /// </summary>
        public const string CacheProvider = "Bee.ObjectCaching.Providers.MemoryCacheProvider, Bee.ObjectCaching";
        /// <summary>
        /// Default cache data source provider type.
        /// </summary>
        public const string CacheDataSourceProvider = "Bee.Business.Providers.CacheDataSourceProvider, Bee.Business";

        // ---------------- Define ----------------
        /// <summary>
        /// Default define storage type.
        /// </summary>
        public const string DefineStorage = "Bee.Definition.Storage.FileDefineStorage, Bee.Definition";
        /// <summary>
        /// Default define access type.
        /// </summary>
        public const string DefineAccess = "Bee.ObjectCaching.CacheDefineAccess, Bee.ObjectCaching";

        // ---------------- Services ----------------
        /// <summary>
        /// Default session info service type.
        /// </summary>
        public const string SessionInfoService = "Bee.ObjectCaching.Services.SessionInfoService, Bee.ObjectCaching";
        /// <summary>
        /// Default company info service type.
        /// </summary>
        public const string CompanyInfoService = "Bee.ObjectCaching.Services.CompanyInfoService, Bee.ObjectCaching";

        // ---------------- Repository ----------------
        /// <summary>
        /// Default repository factory type, used for creating every repository on both axes.
        /// </summary>
        public const string RepositoryFactory = "Bee.Repository.Factories.RepositoryFactory, Bee.Repository";
    }
}
