using Bee.Api.Client;
using Bee.Api.Core;
using Bee.Base;
using Bee.Definition;
using Bee.Hosting;
using Bee.LoadTests.Caching;
using Bee.LoadTests.Configuration;
using Bee.ObjectCaching;
using Bee.ObjectCaching.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bee.LoadTests.Bootstrap
{
    /// <summary>
    /// An in-process Bee.NET backend for a load-test run, plus the cache counters wrapped
    /// around it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what makes <c>LocalApiProvider</c> able to dispatch: it needs a built service
    /// provider on <see cref="ApiClientInfo.LocalServiceProvider"/> holding a
    /// <c>JsonRpcExecutor</c>, and everything that executor reaches — definitions, cache,
    /// database access, business-object resolution — has to be registered first.
    /// </para>
    /// <para>
    /// A Remote run needs the same bootstrap, just in the process being measured rather than this
    /// one. Nothing here is Local-only.
    /// </para>
    /// <para>
    /// NOTE: this is the fourth place in the repository that brings the framework up, alongside
    /// <c>DemoBackend</c> (samples), <c>NorthwindBackend</c> (apps) and <c>SharedDatabaseState</c>
    /// (tests). None of them could be reused: the first two register SQLite and nothing else, and
    /// the third belongs to the xUnit test assets. A change to the startup sequence — the
    /// <c>AddBeeFramework</c> signature, the order of the static initializers — has to be applied
    /// to all four.
    /// </para>
    /// </remarks>
    public sealed class LoadTestHost : IDisposable
    {
        private readonly DefineWorkspace _workspace;
        private readonly ServiceProvider _services;
        private readonly ICacheProvider _originalCacheProvider;

        private LoadTestHost(
            DefineWorkspace workspace,
            ServiceProvider services,
            CountingCacheProvider cacheCounters,
            ICacheProvider originalCacheProvider,
            string connectionStringTemplate)
        {
            _workspace = workspace;
            _services = services;
            _originalCacheProvider = originalCacheProvider;
            CacheCounters = cacheCounters;
            ConnectionStringTemplate = connectionStringTemplate;
        }

        /// <summary>
        /// Gets the connection string as configured, with the <c>{@DbName}</c> placeholder still
        /// in place. Creating a database needs to connect to the engine's admin database, which
        /// means substituting a different name than the one the run itself uses.
        /// </summary>
        public string ConnectionStringTemplate { get; }

        /// <summary>
        /// Gets the counting wrapper installed around the framework's cache provider.
        /// </summary>
        public CountingCacheProvider CacheCounters { get; }

        /// <summary>
        /// Gets the service provider the backend was built into.
        /// </summary>
        public IServiceProvider Services => _services;

        /// <summary>
        /// Gets the definition directory this run is using.
        /// </summary>
        public string DefinePath => _workspace.DefinePath;

        /// <summary>
        /// Gets the program bindings dropped because their assembly could not be loaded.
        /// </summary>
        public IReadOnlyList<string> DroppedBindings => _workspace.DroppedBindings;

        /// <summary>
        /// Brings up the backend for a run.
        /// </summary>
        /// <param name="options">The run configuration; must already have passed validation.</param>
        /// <returns>A started host. Dispose it to tear the backend down again.</returns>
        public static LoadTestHost Start(LoadTestOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var connectionString = DefineWorkspace.ResolveConnectionString(options.Database.Provider);
            var workspace = CreateWorkspace(options);
            try
            {
                var services = new ServiceCollection();

                // AddBeeFramework registers services that ctor-inject ILogger<T>; the audit sink is
                // the one that fails first. An ASP.NET Core host gets logging from
                // WebApplicationBuilder, which is why the other bootstraps in this repository never
                // hit this. No provider is added on purpose: log I/O during a measured window would
                // show up in the numbers as latency that a production host, writing elsewhere,
                // would not have.
                services.AddLogging();
                ConfigureFramework(services, options, workspace);

                var provider = services.BuildServiceProvider();

                ApiClientInfo.LocalServiceProvider = provider;

                // IMPORTANT: this has to come after AddBeeFramework. That call runs
                // CacheInfo.Initialize synchronously, which assigns the configured provider —
                // wrapping first would simply be overwritten, and the failure is silent: the run
                // completes and reports a hit rate of zero with no error anywhere.
                var original = CacheInfo.Provider;
                var counters = new CountingCacheProvider(original);
                CacheInfo.Provider = counters;

                return new LoadTestHost(workspace, provider, counters, original, connectionString);
            }
            catch
            {
                workspace.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Tears the backend down and removes the temporary definition copy.
        /// </summary>
        public void Dispose()
        {
            CacheInfo.Provider = _originalCacheProvider;
            ApiClientInfo.LocalServiceProvider = null;
            _services.Dispose();
            _workspace.Dispose();
        }

        /// <summary>
        /// Applies the framework's startup sequence to a service collection.
        /// </summary>
        /// <param name="services">The collection to register into.</param>
        /// <param name="options">The run configuration.</param>
        /// <param name="workspace">The prepared definition workspace.</param>
        /// <remarks>
        /// Shared with the self-hosted server so both sides of a Remote run start the framework
        /// exactly the same way. If they diverged, the difference between a Local and a Remote
        /// measurement would no longer be only the transport.
        /// </remarks>
        public static void ConfigureFramework(
            IServiceCollection services, LoadTestOptions options, DefineWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(workspace);

            DbProviderRegistrar.Register(options.Database.Provider);

            var paths = new PathOptions { DefinePath = workspace.DefinePath };
            var settings = SystemSettingsLoader.Load(paths);

            SysInfo.Initialize(settings.CommonConfiguration);
            ApiServiceOptions.Initialize(
                settings.CommonConfiguration.ApiPayloadOptions,
                settings.CommonConfiguration.IsDebugMode);

            // autoCreateMasterKey generates a key when BEE_MASTER_KEY is unset, so a run needs no
            // key material of its own and none is hard-coded here.
            services.AddBeeFramework(settings.BackendConfiguration, paths, autoCreateMasterKey: true);
        }

        /// <summary>
        /// Prepares the definition workspace for a run, resolving the source and connection string.
        /// </summary>
        /// <param name="options">The run configuration.</param>
        /// <returns>The prepared workspace; dispose it to remove the temporary copy.</returns>
        public static DefineWorkspace CreateWorkspace(LoadTestOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var sourceDefinePath = string.IsNullOrWhiteSpace(options.DefinePath)
                ? LocateDefaultDefinePath()
                : options.DefinePath;
            var connectionString = DefineWorkspace.ResolveConnectionString(options.Database.Provider);

            return DefineWorkspace.CreateFrom(sourceDefinePath, options, connectionString);
        }

        /// <summary>
        /// Walks up from the executable looking for the Northwind definitions in the checkout.
        /// </summary>
        /// <returns>The definition directory.</returns>
        private static string LocateDefaultDefinePath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "apps", "Bee.Northwind", "Define");
                if (Directory.Exists(candidate)) { return candidate; }
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate 'apps/Bee.Northwind/Define' walking up from " +
                $"'{AppContext.BaseDirectory}'. Run from inside the bee-library checkout, or set " +
                "definePath in the configuration.");
        }
    }
}
