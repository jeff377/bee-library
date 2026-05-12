using Bee.Definition.Logging;
using Bee.Definition.Storage;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Base;
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
        /// Gets or sets the API key provider, used to obtain the AES+HMAC keys required for transport data encryption/decryption.
        /// Supports shared keys and session keys dynamically generated at each login.
        /// </summary>
        public static IApiEncryptionKeyProvider ApiEncryptionKeyProvider { get; set; } = null!;

        /// <summary>
        /// Gets or sets the access token validator, used to verify the validity of access tokens.
        /// </summary>
        public static IAccessTokenValidator AccessTokenValidator { get; set; } = null!;

        /// <summary>
        /// Gets or sets the business object factory, defining how all BusinessObjects are created
        /// for incoming API calls.
        /// </summary>
        public static IBusinessObjectFactory BusinessObjectFactory { get; set; } = null!;

        /// <summary>
        /// Gets or sets the cache data source provider.
        /// </summary>
        public static ICacheDataSourceProvider CacheDataSourceProvider { get; set; } = null!;

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
            LogOptions = configuration.LogOptions;

            if (!SysInfo.IsSingleFile)
            {
                // Initialize backend service instances (DefineAccess builds first; OC and DbConnectionManager
                // wire up against the resulting IDefineAccess + IDefineStorage)
                InitializeComponents(configuration);
                ValidateComponents();
                ValidateDatabaseSettings();
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

        private const string InitializeMethodName = "Initialize";

        /// <summary>
        /// Initializes backend service instances and wires up <c>Bee.ObjectCaching</c>
        /// + <c>Bee.Db</c> static state. Cross-layer wire-up uses reflection to avoid a
        /// compile-time dependency from <c>Bee.Definition</c> into higher layers.
        /// </summary>
        /// <param name="configuration">Backend parameters and environment settings.</param>
        private static void InitializeComponents(BackendConfiguration configuration)
        {
            var Components = configuration.Components;

            // 1. Build IDefineStorage (no dependencies). Held only as a local; not exposed as static.
            var storage = CreateOrDefault<IDefineStorage>(
                Components.DefineStorage, BackendDefaultTypes.DefineStorage);

            // 2. Build IDefineAccess. Two ctor shapes are supported — (IDefineStorage) and
            // parameterless — covering the dominant patterns without locking implementations
            // to a specific dependency shape.
            DefineAccess = ResolveDefineAccess(Components.DefineAccess, storage);

            // 3. Wire up Bee.ObjectCaching static state via reflection.
            InvokeStaticMethod("Bee.ObjectCaching.CacheContainer, Bee.ObjectCaching",
                InitializeMethodName, new object[] { storage });
            InvokeStaticMethod("Bee.ObjectCaching.CacheInfo, Bee.ObjectCaching",
                InitializeMethodName, new object[] { configuration });

            // 4. Wire up Bee.Db.DbConnectionManager via reflection.
            var dbProvider = new DefineAccessDatabaseSettingsProvider(DefineAccess);
            InvokeStaticMethod("Bee.Db.Manager.DbConnectionManager, Bee.Db",
                InitializeMethodName, new object[] { dbProvider });

            // 5. Other services (no inter-dependencies).
            ApiEncryptionKeyProvider = CreateOrDefault<IApiEncryptionKeyProvider>
                (Components.ApiEncryptionKeyProvider, BackendDefaultTypes.ApiEncryptionKeyProvider);
            AccessTokenValidator = CreateOrDefault<IAccessTokenValidator>
                (Components.AccessTokenValidator, BackendDefaultTypes.AccessTokenValidator);
            BusinessObjectFactory = CreateOrDefault<IBusinessObjectFactory>
                (Components.BusinessObjectFactory, BackendDefaultTypes.BusinessObjectFactory);
            CacheDataSourceProvider = CreateOrDefault<ICacheDataSourceProvider>
                (Components.CacheDataSourceProvider, BackendDefaultTypes.CacheDataSourceProvider);
            SessionInfoService = CreateOrDefault<ISessionInfoService>
                (Components.SessionInfoService, BackendDefaultTypes.SessionInfoService);
            EnterpriseObjectService = CreateOrDefault<IEnterpriseObjectService>
                (Components.EnterpriseObjectService, BackendDefaultTypes.EnterpriseObjectService);

            // 6. Wire up Phase 3 BusinessObjectFactory + RepositoryInfo per-call context plumbing.
            InvokeStaticMethod("Bee.Business.BusinessObjectFactory, Bee.Business",
                InitializeMethodName, new object[] { DefineAccess, SessionInfoService });
            InvokeStaticMethod("Bee.Repository.Abstractions.RepositoryInfo, Bee.Repository.Abstractions",
                InitializeMethodName, new object[] { configuration });
        }

        /// <summary>
        /// Resolves the configured <see cref="IDefineAccess"/> implementation. Supports
        /// <c>(IDefineStorage)</c> ctor (used by <c>LocalDefineAccess</c>) and parameterless
        /// ctor (used by implementations that manage their own dependencies).
        /// </summary>
        private static IDefineAccess ResolveDefineAccess(string? typeName, IDefineStorage storage)
        {
            var resolvedName = string.IsNullOrWhiteSpace(typeName) ? BackendDefaultTypes.DefineAccess : typeName;
            var type = Type.GetType(resolvedName)
                ?? throw new InvalidOperationException($"IDefineAccess type '{resolvedName}' not found.");

            var ctorWithStorage = type.GetConstructor(new[] { typeof(IDefineStorage) });
            if (ctorWithStorage != null)
                return (IDefineAccess)ctorWithStorage.Invoke(new object[] { storage });

            return (IDefineAccess?)Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Failed to construct IDefineAccess: {resolvedName}");
        }

        /// <summary>
        /// Invokes a public static method on a type referenced only by name. Used to call
        /// <c>Initialize</c> hooks in <c>Bee.ObjectCaching</c> and <c>Bee.Db</c> without
        /// taking compile-time references on those higher layers.
        /// </summary>
        private static void InvokeStaticMethod(string typeName, string methodName, object[] args)
        {
            // Use AssemblyLoader so we resolve the SAME Type identity as services built via
            // CreateOrDefault (which goes through AssemblyLoader). Mixing Type.GetType (default
            // load context) with AssemblyLoader's byte-loaded assembly would create two distinct
            // Type identities, splitting the static-field state.
            var type = AssemblyLoader.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found for static wire-up.");
            var method = type.GetMethod(methodName)
                ?? throw new InvalidOperationException($"Method '{methodName}' not found on '{typeName}'.");
            method.Invoke(null, args);
        }

        /// <summary>
        /// Validates that the database settings contain the framework-required
        /// system database entry. The framework expects a <c>DatabaseItem</c> with
        /// <c>Id="common"</c> for shared system tables (e.g., st_user, st_session).
        /// Throws <see cref="InvalidOperationException"/> at startup if missing.
        /// </summary>
        internal static void ValidateDatabaseSettings()
        {
            new DefineAccessDatabaseSettingsProvider(DefineAccess).ValidateRequired();
        }

        /// <summary>
        /// Validates that all required backend components are configured.
        /// Throws <see cref="InvalidOperationException"/> at startup if any required component is missing.
        /// </summary>
        internal static void ValidateComponents()
        {
            if (ApiEncryptionKeyProvider == null)
                throw new InvalidOperationException(
                    $"BackendInfo.{nameof(ApiEncryptionKeyProvider)} is not configured. Ensure BackendInfo.Initialize() is called with a valid configuration.");
            if (AccessTokenValidator == null)
                throw new InvalidOperationException(
                    $"BackendInfo.{nameof(AccessTokenValidator)} is not configured. Ensure BackendInfo.Initialize() is called with a valid configuration.");
            if (BusinessObjectFactory == null)
                throw new InvalidOperationException(
                    $"BackendInfo.{nameof(BusinessObjectFactory)} is not configured. Ensure BackendInfo.Initialize() is called with a valid configuration.");
        }

        /// <summary>
        /// Creates an instance of the specified type; uses <paramref name="fallback"/> if <paramref name="configured"/> is empty.
        /// </summary>
        /// <param name="configured">The type name specified in configuration.</param>
        /// <param name="fallback">The default type name.</param>
        private static T CreateOrDefault<T>(string configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return (AssemblyLoader.CreateInstance(typeName) as T)!;
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

            if (StringUtilities.IsNotEmpty(settings.ApiEncryptionKey))
                BackendInfo.ApiEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.ApiEncryptionKey);

            if (StringUtilities.IsNotEmpty(settings.CookieEncryptionKey))
                BackendInfo.CookieEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.CookieEncryptionKey);

            if (StringUtilities.IsNotEmpty(settings.ConfigEncryptionKey))
                BackendInfo.ConfigEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.ConfigEncryptionKey);

            if (StringUtilities.IsNotEmpty(settings.DatabaseEncryptionKey))
                BackendInfo.DatabaseEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.DatabaseEncryptionKey);
        }
    }
}
