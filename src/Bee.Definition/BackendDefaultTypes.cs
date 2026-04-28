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
        /// Default API encryption key provider type.
        /// </summary>
        public const string ApiEncryptionKeyProvider = "Bee.Business.Providers.DynamicApiEncryptionKeyProvider, Bee.Business";
        /// <summary>
        /// Default access token validator, used to verify the validity of access tokens.
        /// </summary>
        public const string AccessTokenValidator = "Bee.Business.Validator.AccessTokenValidator, Bee.Business";
        /// <summary>
        /// Default business object provider type, used for dynamically creating BusinessObjects.
        /// </summary>
        public const string BusinessObjectProvider = "Bee.Business.BusinessObjectProvider, Bee.Business";

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
        public const string DefineAccess = "Bee.ObjectCaching.LocalDefineAccess, Bee.ObjectCaching";

        // ---------------- Services ----------------
        /// <summary>
        /// Default session info service type.
        /// </summary>
        public const string SessionInfoService = "Bee.ObjectCaching.Services.SessionInfoService, Bee.ObjectCaching";
        /// <summary>
        /// Default unified access service type for commonly used enterprise business objects.
        /// </summary>
        public const string EnterpriseObjectService = "Bee.ObjectCaching.Services.EnterpriseObjectService, Bee.ObjectCaching";

        // ---------------- Repository ----------------
        /// <summary>
        /// Default system-level repository provider type.
        /// </summary>
        public const string SystemRepositoryProvider = "Bee.Repository.Providers.SystemRepositoryProvider, Bee.Repository";
        /// <summary>
        /// Default form-level repository provider type.
        /// </summary>
        public const string FormRepositoryProvider = "Bee.Repository.Providers.FormRepositoryProvider, Bee.Repository";
    }
}
