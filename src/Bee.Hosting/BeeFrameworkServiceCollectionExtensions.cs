using Bee.Api.Core.JsonRpc;
using Bee.Base.Expressions;
using Bee.Business;
using Bee.Business.Form;
using Bee.Business.Permission;
using Bee.Business.Security;
using Bee.Business.Session;
using Bee.Expressions;
using Bee.Db;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Hosting.Audit;
using Bee.Hosting.CacheNotify;
using Bee.Hosting.Session;
using Bee.Definition;
using Bee.Definition.Logging;
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
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting
{
    /// <summary>
    /// Registers Bee.NET framework services in the DI container.
    /// </summary>
    public static partial class BeeFrameworkServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Bee.NET framework services and decrypts security keys from
        /// <paramref name="configuration"/>. This call alone brings the framework up:
        /// <c>app.UseBeeFramework()</c> carries no bootstrap work of its own since Phase 7
        /// removed the transitional <c>DbConnectionManager</c> static shim — callers obtain
        /// <see cref="DbAccess"/> via <see cref="IDbAccessFactory"/> (ctor injected). It does
        /// still run host-side startup checks, so an ASP.NET Core host should keep calling it.
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
            //    The data source is passed as a factory, not an instance. Resolving it here would
            //    close the cycle ICacheContainer → ICacheDataSourceProvider → repositories →
            //    IDefineAccess → ICacheContainer; deferring to the first cache miss breaks it.
            services.AddSingleton<ICacheContainer>(sp =>
                new CacheContainerService(
                    sp.GetRequiredService<IDefineStorage>(),
                    sp.GetRequiredService<PathOptions>(),
                    string.Empty,
                    sp.GetRequiredService<ICacheDataSourceProvider>));

            // 4b. Tenant customization-override layer: per-customizeId cache provider + reader.
            //     Both honour PathOptions.CustomizePath — when it is empty (the standard,
            //     non-customized deployment) the reader short-circuits every lookup to null, so
            //     all consumers degrade to pure base, bit-for-bit identical to before. Always
            //     registered; behaviour is gated entirely by CustomizePath, not by presence.
            services.AddSingleton<ICacheContainerProvider>(sp =>
                new CacheContainerProvider(sp.GetRequiredService<PathOptions>()));
            //     The reader follows the storage. A DB-backed IDefineStorage keeps base and
            //     customization rows in one table, told apart by customize_id, so it implements
            //     ICustomizeDefineReader itself and must be preferred — registering the file-based
            //     reader unconditionally would send every customization lookup to the filesystem and
            //     leave DB customizations permanently unreachable.
            services.AddSingleton<ICustomizeDefineReader>(sp =>
                sp.GetRequiredService<IDefineStorage>() as ICustomizeDefineReader
                ?? new CustomizeDefineReader(
                    sp.GetRequiredService<ICacheContainerProvider>(),
                    sp.GetRequiredService<PathOptions>()));
            //     The writer follows the storage for the same reason. It exists for exactly one
            //     artifact — the plugin bindings a deployment maintains through the framework's own
            //     local-only API — so the rest of the customization layer stays read-only.
            services.AddSingleton<ICustomizeDefineWriter>(sp =>
                sp.GetRequiredService<IDefineStorage>() as ICustomizeDefineWriter
                ?? new CustomizeDefineWriter(
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
            {
                // DB anomaly logging (opt-in). Lazy writer resolver breaks the construction cycle
                // IDbAccessFactory → IAuditLogWriter → AuditLogDbSink → IDbAccessFactory.
                var audit = configuration.AuditLogOptions;
                Func<IAuditLogWriter?>? anomalyWriterFactory =
                    audit.AnomalyEnabled ? () => sp.GetService<IAuditLogWriter>() : null;
                var anomalyOptions = audit.AnomalyEnabled ? configuration.LogOptions.DbAccess : null;
                return new DbAccessFactory(
                    sp.GetRequiredService<IDbConnectionManager>(), 0, anomalyWriterFactory, anomalyOptions);
            });

            // 6b. Cache-notify bump primitive — stateless; builds dialect SQL per call and runs
            //     it on the caller's transaction. No consumer wired yet (poller / business
            //     repositories arrive in later stages); registered now so it is injectable.
            services.AddSingleton<ICacheNotifyService, CacheNotifyService>();
            services.AddSingleton<ICacheNotifyReader, CacheNotifyReader>();

            // 6b-2. Reserved progId self-registration. Registered ahead of every other hosted
            //       service on purpose: hosted services start in registration order (the default,
            //       HostOptions.ServicesStartConcurrently being false), and this one both writes the
            //       registry and refuses to start a host whose reserved progIds do not resolve — a
            //       check worth failing before anything else comes up.
            services.AddHostedService<Registry.ReservedProgIdRegistrationService>();

            // 6c. Cache-notify polling hosted service. The poller publishes observed versions to
            //     CacheInfo.NotifyVersions; each cache entry carrying a matching ChangeNotifyKey
            //     expires on its next read. The poller is only registered when enabled; hosts without
            //     an IHost (e.g. unit-test service providers) simply never start the hosted service.
            services.AddSingleton(configuration.CacheNotifyOptions);
            if (configuration.CacheNotifyOptions.Enabled)
            {
                services.AddHostedService<CacheNotifyPoller>();
            }

            // 6c-2. Expired session cleanup. Reads no longer delete expired rows on the way past,
            //       and every sign-in inserts one, so this is what bounds st_session. Same shape as
            //       the poller above: registered only when enabled, inert without an IHost.
            services.AddSingleton(configuration.SessionCleanupOptions);
            if (configuration.SessionCleanupOptions.Enabled)
            {
                services.AddHostedService<ExpiredSessionCleanupService>();
            }

            // 6d. Audit-trail (data-history) logging. Opt-in: when disabled every consumer gets the
            //     no-op writer so IAuditLogWriter is always injectable with zero behavioural change.
            //     When enabled, the background writer batches to the log database; hosts without an
            //     IHost (e.g. in-process local) set UseBackgroundWriter=false for synchronous writes.
            services.AddSingleton(configuration.AuditLogOptions);
            if (configuration.AuditLogOptions.Enabled)
            {
                // Built through the factory, not by the container: every repository now takes
                // (IRepositoryContext, Guid, string), and the container can supply none of those
                // three. Registering the concrete type directly would resolve at first use, not
                // at registration — and only in a host that has audit logging on.
                services.AddSingleton<IAuditLogWriteRepository>(sp =>
                    sp.GetRequiredService<IRepositoryFactory>().Create<IAuditLogWriteRepository>());
                services.AddSingleton<IAuditLogSink, AuditLogDbSink>();
                if (configuration.AuditLogOptions.UseBackgroundWriter)
                {
                    services.AddSingleton<AuditLogWriterService>();
                    services.AddSingleton<IAuditLogWriter>(sp => sp.GetRequiredService<AuditLogWriterService>());
                    services.AddHostedService(sp => sp.GetRequiredService<AuditLogWriterService>());
                }
                else
                {
                    services.AddSingleton<IAuditLogWriter, SynchronousAuditLogWriter>();
                }
            }
            else
            {
                services.AddSingleton<IAuditLogWriter>(NullAuditLogWriter.Instance);
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

            // Company binding shared by EnterCompany and session rebuild — both must land on the
            // same session state, so the derivation lives in one place.
            services.AddSingleton<SessionCompanyBinder>();

            // Expression engine + form rule processor. Both are stateless singletons; the evaluator
            // caches compiled expressions process-wide, so a single instance is preferred.
            services.AddSingleton<IExpressionEvaluator, DynamicExpressoEvaluator>();
            services.AddSingleton<IFormRuleProcessor, FormRuleProcessor>();

            // 6. IApiEncryptionKeyProvider — Static and Derived need key material from the
            //    settings; Dynamic needs ISessionInfoService. Phase 5/6 unifies via
            //    IOptions<T> + DI ctor.
            services.AddSingleton<IApiEncryptionKeyProvider>(sp =>
                CreateApiEncryptionKeyProvider(sp, components.ApiEncryptionKeyProvider, keys));

            // 7. Login attempt tracker — registered by default.
            //    WARNING: Login is the one credential-checking method reachable anonymously, so
            //    leaving this unregistered meant a stock deployment had no account lockout at all
            //    while a complete implementation sat unused in the box. Defaults are 5 consecutive
            //    failures and a 15-minute lockout (LoginAttemptTracker.Default* constants).
            //    Registration is TryAdd, so an app that wants a different policy — or none —
            //    registers its own ILoginAttemptTracker and that one wins. Tests inject per-call
            //    via TestOverrideServiceProvider.
            services.TryAddSingleton<ILoginAttemptTracker, LoginAttemptTracker>();

            // 8. Business-object factory + progId → BO type resolver.
            //    ProgramSettingsBoTypeResolver looks up ProgramItem.BusinessObject in
            //    ProgramSettings.xml. Ordinary progIds without one fall back to FormBusinessObject;
            //    the reserved progIds resolve to the framework's own objects and fail fast when the
            //    registry names something that will not load (see ProgramSettingsBoTypeResolver).
            services.AddSingleton<IBoTypeResolver>(sp =>
                new ProgramSettingsBoTypeResolver(
                    sp.GetRequiredService<IDefineAccess>(),
                    sp.GetRequiredService<ICustomizeDefineReader>(),
                    // Optional: a host with no logging configured still resolves types, it just
                    // reports nothing when a binding degrades.
                    sp.GetService<ILogger<ProgramSettingsBoTypeResolver>>()));
            //    The factory is no longer replaceable through BackendComponents: what it builds is
            //    decided by the registry, one progId at a time, which is both finer-grained and
            //    per-tenant. Swapping the whole factory was the only way to change a system business
            //    object before, and it was process-wide.
            services.AddSingleton<IBusinessObjectFactory, BusinessObjectFactory>();

            //    Business plugin chain resolver. Reads PluginSettings, base plus the tenant
            //    customization, and caches the resolved chain per (customizationCode, progId).
            services.AddSingleton<IFormPluginResolver>(sp =>
                new PluginSettingsResolver(
                    sp.GetRequiredService<IDefineAccess>(),
                    sp.GetRequiredService<ICustomizeDefineReader>()));

            // 9. Repository factory — one registration for every repository, on both axes.
            services.AddSingleton<IRepositoryDatabaseRouter, RepositoryDatabaseRouter>();
            services.AddSingleton<IRepositoryFactory>(sp =>
                CreateConfigurableService<IRepositoryFactory>(sp,
                    components.RepositoryFactory, BackendDefaultTypes.RepositoryFactory));

            // NOTE: individual repositories are deliberately NOT registered here. Consumers
            // ctor-inject IRepositoryFactory and create what they need per call, the same way
            // FormBusinessObject obtains its form repository by progId. Registering them one by one
            // made every new system table a three-place edit (factory method, DI line, consumer
            // ctor); this keeps it to the one factory registration above.

            // Permission services: per-company role-permission snapshot cache + layer-1 Can check.
            services.AddSingleton<IRolePermissionService>(sp =>
                new RolePermissionService(sp.GetRequiredService<ICacheContainer>()));
            services.AddSingleton<ICompanyAuthorizationService>(sp =>
                new CompanyAuthorizationService(
                    sp.GetRequiredService<ISessionInfoService>(),
                    sp.GetRequiredService<IRolePermissionService>()));
            // Deployment-level authorization: a separate axis from the company-scoped service above.
            // The two never fall back to one another — see IDeploymentAuthorizationService.
            services.AddSingleton<IDeploymentAuthorizationService>(sp =>
                new DeploymentAuthorizationService(
                    sp.GetRequiredService<ISessionInfoService>(),
                    sp.GetRequiredService<IRepositoryFactory>()));

            // API key validation (application identity). Registered plainly rather than through the
            // configurable-component path: a host that wants different key storage replaces it with
            // services.AddSingleton<IApiKeyValidator, MyValidator>() after AddBeeFramework.
            services.AddSingleton<IApiKeyValidator>(sp =>
                new ApiKeyValidator(sp.GetRequiredService<ICacheContainer>()));

            // Organization: per-company department-tree snapshot cache (record-scope source).
            services.AddSingleton<IDepartmentTreeService>(sp =>
                new DepartmentTreeService(sp.GetRequiredService<ICacheContainer>()));
            // Record-scope identity: resolves the current user's employee/department (EnterCompany
            // snapshots the result onto SessionInfo for zero-DB scope filtering).
            services.AddSingleton<IEmployeeContextResolver>(sp =>
                new EmployeeContextResolver(sp.GetRequiredService<IRepositoryFactory>()));
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
    }
}
