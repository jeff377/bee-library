using Bee.Api.Client;
using Bee.Api.Core;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Providers.Sqlite;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Storage;
using Bee.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Samples.Shared;

/// <summary>
/// One-line bootstrap for the Blazor demos. Resolves the shared
/// <c>samples/Define</c> directory, registers SQLite, loads SystemSettings,
/// wires <c>AddBeeFramework</c>; <c>Define/ProgramSettings.xml</c> binds the reserved "System" progId
/// so the login panel can authenticate against <see cref="DemoCredentials"/> rather than
/// against stored credentials.
/// </summary>
/// <remarks>
/// Overriding authentication removes the need for stored credentials, but not the need for the
/// common system tables themselves: <c>Login</c> still reads the user's locale from
/// <c>st_user</c> and persists the session seed to <c>st_session</c> on every successful sign-in.
/// Both are therefore materialized and created here — without them the demo authenticates fine
/// and then fails inside session construction.
/// </remarks>
public static class DemoBackend
{
    /// <summary>
    /// Registers Bee backend services into <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The resolved <see cref="PathOptions"/> so callers can locate Define files later if needed.</returns>
    public static PathOptions AddBeeBackend(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Demo-only: ensure a master key is available so the bundled demos can run
        // with zero setup. Production hosts MUST set BEE_MASTER_KEY via the real
        // deployment mechanism (K8s Secret, env file, Vault, etc.) — see README.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BEE_MASTER_KEY")))
        {
            Environment.SetEnvironmentVariable("BEE_MASTER_KEY", DemoCredentials.DemoMasterKey);
        }

        // CustomizePath turns on the tenant customization-override layer. It is set the same way
        // DefinePath is — the host computes it and hands both to AddBeeFramework; the framework has
        // no configuration binding of its own. The directory need not exist: a tenant with no
        // override files resolves every lookup to the base layer, which is what this demo does
        // (it is single-company and never calls EnterCompany, so SessionInfo.CustomizeId stays
        // empty and the overlay short-circuits before it ever touches the filesystem).
        string definePath = ResolveDefinePath();
        var paths = new PathOptions
        {
            DefinePath = definePath,
            CustomizePath = ResolveCustomizePath(definePath),
        };

        // Framework tables the demo cannot run without. Their TableSchemas ship as embedded
        // defaults in Bee.Definition, so materialize them into the demo DefinePath
        // (skip-if-exists) for IDefineAccess to resolve; DemoSchemaSeeder then creates them
        // alongside the Employee tables.
        //   st_cache_notify — polled by the cache-notify poller AddBeeFramework registers.
        //   st_session      — the session seed every successful Login persists.
        //   st_user         — read for the signing-in user's time zone and culture.
        var requiredFrameworkTables = new HashSet<string>(StringComparer.Ordinal)
        {
            "TableSchema/common/st_cache_notify.TableSchema.xml",
            "TableSchema/common/st_session.TableSchema.xml",
            "TableSchema/common/st_user.TableSchema.xml",
        };
        Defaults.MaterializeTo(paths.DefinePath, new MaterializeOptions
        {
            Filter = requiredFrameworkTables.Contains
        });

        // SQLite providers — keep dialect registration explicit so the framework does
        // not force every host to pull every ADO.NET driver.
        DbProviderRegistry.Register(DatabaseType.SQLite, new SqliteProviderFactory(SqliteFactory.Instance));
        DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());

        var settings = SystemSettingsLoader.Load(paths);
        SysInfo.Initialize(settings.CommonConfiguration);
        ApiServiceOptions.Initialize(
            settings.CommonConfiguration.ApiPayloadOptions,
            settings.CommonConfiguration.IsDebugMode);

        builder.Services.AddBeeFramework(
            settings.BackendConfiguration,
            paths,
            autoCreateMasterKey: true);

        // Nothing to register for the custom login: Define/ProgramSettings.xml binds the reserved
        // progId "System" to DemoAuthenticatingSystemBusinessObject, and the framework resolves it
        // from there like any other progId.

        return paths;
    }

    /// <summary>
    /// After the host is built: hooks <see cref="ApiClientInfo.LocalServiceProvider"/>
    /// so connectors created by Blazor components can route in-process calls, and runs
    /// the schema seeder once.
    /// </summary>
    /// <param name="app">The built web application.</param>
    public static void UseBeeBackend(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        ApiClientInfo.LocalServiceProvider = app.Services;

        var defineAccess = app.Services.GetRequiredService<IDefineAccess>();
        var connectionManager = app.Services.GetRequiredService<IDbConnectionManager>();
        var dbAccessFactory = app.Services.GetRequiredService<IDbAccessFactory>();
        DemoSchemaSeeder.EnsureSchemaAndSeed(defineAccess, connectionManager, dbAccessFactory);
    }

    private static string ResolveDefinePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Define", "SystemSettings.xml");
            if (File.Exists(candidate))
                return Path.GetDirectoryName(candidate)!;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            "Could not locate 'Define/SystemSettings.xml' walking up from " +
            $"'{AppContext.BaseDirectory}'. Run the sample from inside the bee-library checkout.");
    }

    /// <summary>
    /// Places the customization root as a sibling of <c>Define/</c>, mirroring the layout a real
    /// deployment would use: <c>Define/</c> holds the base definitions everyone shares,
    /// <c>Customize/{customizeId}/</c> holds the per-tenant overrides on top of them.
    /// </summary>
    /// <param name="definePath">The resolved base definition directory.</param>
    private static string ResolveCustomizePath(string definePath)
        => Path.Combine(Path.GetDirectoryName(definePath)!, "Customize");
}
