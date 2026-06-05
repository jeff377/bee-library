using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Business;
using Bee.Business.Permission;
using Bee.Business.Providers;
using Bee.Db;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Hosting.CacheNotify;
using Bee.Definition;
using Bee.ObjectCaching;
using Bee.ObjectCaching.Services;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Definition.Language;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting
{
    /// <summary>
    /// Registers Bee.NET framework services in the DI container.
    /// </summary>
    public static class BeeFrameworkServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Bee.NET framework services and decrypts security keys from
        /// <paramref name="configuration"/>. <c>app.UseBeeFramework()</c> remains available
        /// as an ASP.NET Core integration extension point but no longer performs any
        /// bootstrap work after Phase 7 removed the transitional <c>DbConnectionManager</c>
        /// static shim — callers obtain <see cref="DbAccess"/> via
        /// <see cref="IDbAccessFactory"/> (ctor injected).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The backend configuration (from SystemSettings.xml).</param>
        /// <param name="pathOptions">
        /// Path configuration that locates definition files (SystemSettings.xml, FormSchema/, etc.).
        /// Registered as a singleton so framework services can ctor-inject it directly.
        /// </param>
        /// <param name="autoCreateMasterKey">Whether to auto-create the master key file if missing.</param>
        public static IServiceCollection AddBeeFramework(
            this IServiceCollection services,
            BackendConfiguration configuration,
            PathOptions pathOptions,
            bool autoCreateMasterKey = false)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(pathOptions);

            // 1. Decrypt the security keys once and thread them through downstream ctors.
            //    Each key is byte[]; empty means "not configured" (callers gracefully no-op
            //    when the relevant crypto path runs without a key).
            var keys = DecryptSecurityKeys(configuration.SecurityKeySettings, pathOptions.DefinePath, autoCreateMasterKey);

            var components = configuration.Components;

            // 2. PathOptions — registered as a singleton so consumers can ctor-inject it.
            services.AddSingleton(pathOptions);

            // 3. Underlying cache provider (in-memory / Redis / ...). Idempotent — no-op
            //    when the configured type matches the current provider's runtime type.
            CacheInfo.Initialize(configuration);

            // 4. IDefineStorage / IDefineAccess / ICacheContainer — singletons.
            services.AddSingleton<IDefineStorage>(sp => CreateDefineStorage(
                components.DefineStorage, BackendDefaultTypes.DefineStorage, sp, sp.GetRequiredService<PathOptions>()));
            services.AddSingleton<ICacheContainer>(sp =>
                new CacheContainerService(
                    sp.GetRequiredService<IDefineStorage>(),
                    sp.GetRequiredService<PathOptions>()));

            // 4b. Tenant customization-override layer: per-customizeId cache provider + reader.
            //     Both honour PathOptions.CustomizePath — when it is empty (the standard,
            //     non-customized deployment) the reader short-circuits every lookup to null, so
            //     all consumers degrade to pure base, bit-for-bit identical to before. Always
            //     registered; behaviour is gated entirely by CustomizePath, not by presence.
            services.AddSingleton<ICacheContainerProvider>(sp =>
                new CacheContainerProvider(sp.GetRequiredService<PathOptions>()));
            services.AddSingleton<ICustomizeDefineReader>(sp =>
                new CustomizeDefineReader(
                    sp.GetRequiredService<ICacheContainerProvider>(),
                    sp.GetRequiredService<PathOptions>()));

            services.AddSingleton<IDefineAccess>(sp =>
                ResolveDefineAccess(
                    components.DefineAccess,
                    sp.GetRequiredService<IDefineStorage>(),
                    sp.GetRequiredService<PathOptions>(),
                    sp.GetRequiredService<ICacheContainer>(),
                    keys.ConfigEncryptionKey,
                    sp.GetRequiredService<ICustomizeDefineReader>()));

            // 5. Database settings provider (used by DbConnectionManager bootstrap).
            services.AddSingleton<IDatabaseSettingsProvider>(sp =>
                new DefineAccessDatabaseSettingsProvider(sp.GetRequiredService<IDefineAccess>()));

            // 6. IDbConnectionManager + IDbAccessFactory — DI-injectable singletons.
            //    Callers ctor-inject IDbAccessFactory.Create(databaseId) instead of using
            //    the removed static DbConnectionManager facade.
            services.AddSingleton<IDbConnectionManager>(sp =>
                new DbConnectionManagerService(sp.GetRequiredService<IDatabaseSettingsProvider>()));
            services.AddSingleton<IDbAccessFactory>(sp =>
                new DbAccessFactory(sp.GetRequiredService<IDbConnectionManager>()));

            // 6b. Cache-notify bump primitive — stateless; builds dialect SQL per call and runs
            //     it on the caller's transaction. No consumer wired yet (poller / business
            //     repositories arrive in later stages); registered now so it is injectable.
            services.AddSingleton<ICacheNotifyService, CacheNotifyService>();

            // 6c. Cache-notify polling hosted service. Evictions dispatch by convention through
            //     ICacheContainer.TryEvict (cache group → owned cache), so no route table is
            //     registered here. The poller is only registered when enabled; hosts without an
            //     IHost (e.g. unit-test service providers) simply never start the hosted service.
            services.AddSingleton(configuration.CacheNotifyOptions);
            if (configuration.CacheNotifyOptions.Enabled)
            {
                services.AddHostedService<CacheNotifyPoller>();
            }

            // 5. Replaceable core services. Lifetimes default to Singleton in Phase 4 —
            //    no consumer requires per-request scope today, and registering as Scoped
            //    would block resolution through the singleton BusinessObjectFactory.
            //    Phase 5/6 will revisit per-request scope when a real need emerges.
            services.AddSingleton<IAccessTokenValidator>(sp =>
                CreateConfigurableService<IAccessTokenValidator>(sp,
                    components.AccessTokenValidator, BackendDefaultTypes.AccessTokenValidator));
            services.AddSingleton<ISessionInfoService>(sp =>
                CreateConfigurableService<ISessionInfoService>(sp,
                    components.SessionInfoService, BackendDefaultTypes.SessionInfoService));
            services.AddSingleton<ILanguageService>(sp =>
                new LanguageService(
                    sp.GetRequiredService<IDefineAccess>(),
                    sp.GetRequiredService<ICustomizeDefineReader>()));
            services.AddSingleton<ICompanyInfoService>(sp =>
                CreateConfigurableService<ICompanyInfoService>(sp,
                    components.CompanyInfoService, BackendDefaultTypes.CompanyInfoService));
            services.AddSingleton<ICacheDataSourceProvider>(sp =>
                CreateConfigurableService<ICacheDataSourceProvider>(sp,
                    components.CacheDataSourceProvider, BackendDefaultTypes.CacheDataSourceProvider));
            services.AddSingleton<IEnterpriseObjectService>(_ => CreateOrDefault<IEnterpriseObjectService>(
                components.EnterpriseObjectService, BackendDefaultTypes.EnterpriseObjectService));

            // 6. IApiEncryptionKeyProvider — Static needs the configured key byte[]; Dynamic
            //    needs ISessionInfoService. Phase 5/6 unifies via IOptions<T> + DI ctor.
            services.AddSingleton<IApiEncryptionKeyProvider>(sp =>
                CreateApiEncryptionKeyProvider(sp, components.ApiEncryptionKeyProvider, keys.ApiEncryptionKey));

            // 7. Login attempt tracker — optional service with no default impl. Apps wanting
            //    brute-force protection register their own impl via
            //    services.AddSingleton<ILoginAttemptTracker, MyTracker>() after AddBeeFramework.
            //    Tests inject per-call via TestOverrideServiceProvider; see plan-backendinfo-di-phase4.md.

            // 8. Business-object factory + form-bo type resolver.
            //    ProgramSettingsFormBoTypeResolver looks up ProgramItem.TypeName from
            //    ProgramSettings.xml; ProgIds without TypeName fall back to FormBusinessObject.
            services.AddSingleton<IFormBoTypeResolver>(sp =>
                new ProgramSettingsFormBoTypeResolver(
                    sp.GetRequiredService<IDefineAccess>(),
                    sp.GetRequiredService<ICustomizeDefineReader>()));
            services.AddSingleton<IBusinessObjectFactory>(sp =>
                CreateBusinessObjectFactory(sp, components.BusinessObjectFactory));

            // 9. Repository factories — consumed via ctor injection (PR 5.3a dropped the
            //    RepositoryInfo static + bootstrapper).
            services.AddSingleton<IRepositoryDatabaseRouter, RepositoryDatabaseRouter>();
            services.AddSingleton<ISystemRepositoryFactory>(sp =>
                CreateConfigurableService<ISystemRepositoryFactory>(sp,
                    components.SystemRepositoryFactory, BackendDefaultTypes.SystemRepositoryFactory));
            services.AddSingleton<IFormRepositoryFactory>(sp =>
                CreateConfigurableService<IFormRepositoryFactory>(sp,
                    components.FormRepositoryFactory, BackendDefaultTypes.FormRepositoryFactory));

            // Repositories that ctor-inject into upstream services (CompanyInfoService,
            // EnterCompany permission check). Owned by the factory but exposed to DI so
            // ActivatorUtilities can resolve them without a service-locator pattern.
            services.AddSingleton<ICompanyRepository>(sp =>
                sp.GetRequiredService<ISystemRepositoryFactory>().CreateCompanyRepository());
            services.AddSingleton<IUserCompanyRepository>(sp =>
                sp.GetRequiredService<ISystemRepositoryFactory>().CreateUserCompanyRepository());
            services.AddSingleton<IRolePermissionRepository>(sp =>
                sp.GetRequiredService<ISystemRepositoryFactory>().CreateRolePermissionRepository());
            services.AddSingleton<IDepartmentRepository>(sp =>
                sp.GetRequiredService<ISystemRepositoryFactory>().CreateDepartmentRepository());
            services.AddSingleton<IUserRepository>(sp =>
                sp.GetRequiredService<ISystemRepositoryFactory>().CreateUserRepository());
            services.AddSingleton<IEmployeeRepository>(sp =>
                sp.GetRequiredService<ISystemRepositoryFactory>().CreateEmployeeRepository());

            // Permission services: per-company role-permission snapshot cache + layer-1 Can check.
            services.AddSingleton<IRolePermissionService>(sp =>
                new RolePermissionService(
                    sp.GetRequiredService<ICacheContainer>(),
                    sp.GetRequiredService<ICompanyInfoService>(),
                    sp.GetRequiredService<IRolePermissionRepository>()));
            services.AddSingleton<IAuthorizationService>(sp =>
                new AuthorizationService(
                    sp.GetRequiredService<ISessionInfoService>(),
                    sp.GetRequiredService<IRolePermissionService>()));

            // Organization: per-company department-tree snapshot cache (record-scope source).
            services.AddSingleton<IDepartmentTreeService>(sp =>
                new DepartmentTreeService(
                    sp.GetRequiredService<ICacheContainer>(),
                    sp.GetRequiredService<ICompanyInfoService>(),
                    sp.GetRequiredService<IDepartmentRepository>()));
            // Record-scope identity: resolves the current user's employee/department (EnterCompany
            // snapshots the result onto SessionInfo for zero-DB scope filtering).
            services.AddSingleton<IEmployeeContextResolver>(sp =>
                new EmployeeContextResolver(
                    sp.GetRequiredService<IUserRepository>(),
                    sp.GetRequiredService<IEmployeeRepository>()));
            // Record-scope (layer-2): resolves (model, action) + session identity + grants + model
            // default + department tree into a read filter / per-row verdict.
            services.AddSingleton<IScopeResolver>(sp =>
                new ScopeResolver(
                    sp.GetRequiredService<ISessionInfoService>(),
                    sp.GetRequiredService<IRolePermissionService>(),
                    sp.GetRequiredService<IDepartmentTreeService>(),
                    sp.GetRequiredService<IDefineAccess>()));

            // 10. JsonRpcExecutor — transient (per request); its dependencies (factories,
            //     validators, key providers) are resolved from the container at construction.
            services.AddTransient<JsonRpcExecutor>();

            return services;
        }

        /// <summary>
        /// Resolves the configured <see cref="IDefineAccess"/> implementation. Supports
        /// <c>(IDefineStorage, PathOptions, ICacheContainer, byte[], ICustomizeDefineReader)</c>
        /// (used by <c>LocalDefineAccess</c> with the customization overlay),
        /// <c>(IDefineStorage, PathOptions, ICacheContainer, byte[])</c>,
        /// <c>(IDefineStorage, PathOptions)</c>, <c>(IDefineStorage)</c> (legacy), and
        /// parameterless ctors.
        /// </summary>
        private static IDefineAccess ResolveDefineAccess(string? typeName, IDefineStorage storage, PathOptions paths, ICacheContainer cache, byte[] configEncryptionKey, ICustomizeDefineReader customizeReader)
        {
            var resolvedName = string.IsNullOrWhiteSpace(typeName) ? BackendDefaultTypes.DefineAccess : typeName;
            var type = AssemblyLoader.GetType(resolvedName)
                ?? throw new InvalidOperationException($"IDefineAccess type '{resolvedName}' not found.");

            var ctorWithReader = type.GetConstructor(new[] { typeof(IDefineStorage), typeof(PathOptions), typeof(ICacheContainer), typeof(byte[]), typeof(ICustomizeDefineReader) });
            if (ctorWithReader != null)
                return (IDefineAccess)ctorWithReader.Invoke(new object[] { storage, paths, cache, configEncryptionKey, customizeReader });

            var ctorFull = type.GetConstructor(new[] { typeof(IDefineStorage), typeof(PathOptions), typeof(ICacheContainer), typeof(byte[]) });
            if (ctorFull != null)
                return (IDefineAccess)ctorFull.Invoke(new object[] { storage, paths, cache, configEncryptionKey });

            var ctorPaths = type.GetConstructor(new[] { typeof(IDefineStorage), typeof(PathOptions) });
            if (ctorPaths != null)
                return (IDefineAccess)ctorPaths.Invoke(new object[] { storage, paths });

            var ctorWithStorage = type.GetConstructor(new[] { typeof(IDefineStorage) });
            if (ctorWithStorage != null)
                return (IDefineAccess)ctorWithStorage.Invoke(new object[] { storage });

            return (IDefineAccess?)Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Failed to construct IDefineAccess: {resolvedName}");
        }

        /// <summary>
        /// Constructs the configured <see cref="IDefineStorage"/> implementation. Prefers
        /// the <c>(PathOptions)</c> ctor (used by <see cref="FileDefineStorage"/> after
        /// Phase 5 PR 5.2); falls back to a parameterless ctor for legacy implementations.
        /// </summary>
        private static IDefineStorage CreateDefineStorage(string? configured, string fallback, IServiceProvider sp, PathOptions paths)
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            var type = AssemblyLoader.GetType(typeName)
                ?? throw new InvalidOperationException($"IDefineStorage type '{typeName}' not found.");

            // Prefer an (IServiceProvider) ctor — used by DB-backed storage (e.g. DbDefineStorage),
            // which resolves its dependencies lazily to avoid a construction cycle through
            // IDbConnectionManager → IDatabaseSettingsProvider → IDefineAccess → IDefineStorage.
            var ctorWithServiceProvider = type.GetConstructor(new[] { typeof(IServiceProvider) });
            if (ctorWithServiceProvider != null)
                return (IDefineStorage)ctorWithServiceProvider.Invoke(new object[] { sp });

            var ctorWithPaths = type.GetConstructor(new[] { typeof(PathOptions) });
            if (ctorWithPaths != null)
                return (IDefineStorage)ctorWithPaths.Invoke(new object[] { paths });

            return (AssemblyLoader.CreateInstance(typeName) as IDefineStorage)
                ?? throw new InvalidOperationException($"Failed to construct IDefineStorage: {typeName}");
        }

        /// <summary>
        /// Creates a configurable service whose implementation type is read from configuration.
        /// Tries DI-aware construction first (ctor params resolved from <paramref name="sp"/>);
        /// falls back to parameterless construction via <see cref="AssemblyLoader"/>.
        /// </summary>
        private static T CreateConfigurableService<T>(IServiceProvider sp, string? configured, string fallback)
            where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            var type = AssemblyLoader.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found for service '{typeof(T).Name}'.");

            // Try DI-aware construction first — ActivatorUtilities resolves any ctor parameters
            // from the service provider. Falls back to AssemblyLoader.CreateInstance for legacy
            // parameterless ctors.
            try
            {
                return (T)ActivatorUtilities.CreateInstance(sp, type);
            }
            catch (InvalidOperationException)
            {
                return (AssemblyLoader.CreateInstance(typeName) as T)
                    ?? throw new InvalidOperationException($"Failed to construct {typeof(T).Name}: {typeName}");
            }
        }

        /// <summary>
        /// Creates the configured <see cref="IApiEncryptionKeyProvider"/>. The static provider
        /// receives the decrypted API key byte[] directly; the dynamic provider relies on
        /// <see cref="ISessionInfoService"/> resolved through DI.
        /// </summary>
        private static IApiEncryptionKeyProvider CreateApiEncryptionKeyProvider(IServiceProvider sp, string? configured, byte[] apiEncryptionKey)
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? BackendDefaultTypes.ApiEncryptionKeyProvider : configured;
            var type = AssemblyLoader.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found for IApiEncryptionKeyProvider.");

            if (type == typeof(StaticApiEncryptionKeyProvider))
                return new StaticApiEncryptionKeyProvider(apiEncryptionKey);
            return (IApiEncryptionKeyProvider)ActivatorUtilities.CreateInstance(sp, type);
        }

        /// <summary>
        /// Creates the configured <see cref="IBusinessObjectFactory"/>. The default
        /// <see cref="BusinessObjectFactory"/> ctor needs <see cref="IServiceProvider"/>,
        /// <see cref="IDefineAccess"/>, <see cref="ISessionInfoService"/>, and
        /// <see cref="IFormBoTypeResolver"/>.
        /// </summary>
        private static IBusinessObjectFactory CreateBusinessObjectFactory(IServiceProvider sp, string? configured)
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? BackendDefaultTypes.BusinessObjectFactory : configured;
            var type = AssemblyLoader.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found for IBusinessObjectFactory.");
            return (IBusinessObjectFactory)ActivatorUtilities.CreateInstance(sp, type);
        }

        /// <summary>
        /// Convenience wrapper around <see cref="AssemblyLoader.CreateInstance(string, object[])"/>
        /// for parameterless services.
        /// </summary>
        private static T CreateOrDefault<T>(string? configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return (AssemblyLoader.CreateInstance(typeName) as T)
                ?? throw new InvalidOperationException($"Failed to construct {typeof(T).Name}: {typeName}");
        }

        /// <summary>
        /// Decrypts the four security keys from <paramref name="settings"/> in one pass
        /// using the master key. Empty entries map to empty byte arrays so downstream
        /// crypto paths see a consistent "no key configured" sentinel.
        /// </summary>
        private static SecurityKeys DecryptSecurityKeys(SecurityKeySettings settings, string definePath, bool autoCreateMasterKey)
        {
            byte[] masterKey = MasterKeyProvider.GetMasterKey(settings.MasterKeySource, definePath, autoCreateMasterKey);

            return new SecurityKeys(
                ApiEncryptionKey: Decrypt(masterKey, settings.ApiEncryptionKey),
                CookieEncryptionKey: Decrypt(masterKey, settings.CookieEncryptionKey),
                ConfigEncryptionKey: Decrypt(masterKey, settings.ConfigEncryptionKey),
                DatabaseEncryptionKey: Decrypt(masterKey, settings.DatabaseEncryptionKey));

            static byte[] Decrypt(byte[] masterKey, string? encryptedKey)
                => StringUtilities.IsNotEmpty(encryptedKey)
                    ? EncryptionKeyProtector.DecryptEncryptedKey(masterKey, encryptedKey!)
                    : Array.Empty<byte>();
        }

        /// <summary>
        /// Decrypted security keys bundle. Each field is the 64-byte combined AES + HMAC
        /// key (or empty when not configured).
        /// </summary>
        private readonly record struct SecurityKeys(
            byte[] ApiEncryptionKey,
            byte[] CookieEncryptionKey,
            byte[] ConfigEncryptionKey,
            byte[] DatabaseEncryptionKey);
    }
}
