using Bee.Api.Core;
using Bee.Base;
using Bee.Business;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Providers.Sqlite;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Definition.Storage;
using Bee.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Northwind.Server;

/// <summary>
/// One-line bootstrap for the Bee.Northwind demo. Resolves the sibling <c>Define</c>
/// directory, registers SQLite, loads SystemSettings, wires <c>AddBeeFramework</c>, then
/// binds the reserved "System" progId in ProgramSettings.xml so login authenticates against
/// <see cref="NorthwindCredentials"/> without seeding system tables.
/// </summary>
/// <remarks>
/// This is the self-contained mirror of the <c>samples/Bee.Samples.Shared</c> demo backend:
/// the app depends only on the published <c>Bee.*</c> packages so it can graduate to its own
/// repository without dragging the samples shared project along.
/// </remarks>
public static class NorthwindBackend
{
    /// <summary>
    /// Registers Bee backend services into <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The resolved <see cref="PathOptions"/> so callers can locate Define files later if needed.</returns>
    public static PathOptions AddNorthwindBackend(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Demo-only: ensure a master key is available so a fresh clone can run with zero
        // setup. Production hosts MUST set BEE_MASTER_KEY via the real deployment mechanism.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BEE_MASTER_KEY")))
        {
            Environment.SetEnvironmentVariable("BEE_MASTER_KEY", NorthwindCredentials.DemoMasterKey);
        }

        var paths = new PathOptions { DefinePath = ResolveDefinePath() };

        // The framework owns two whole categories of table the demo does not define itself: the
        // cross-company tables in common (st_user, st_session, st_cache_notify, ...) and the audit
        // trail in log. Their TableSchema ships as embedded defaults in Bee.Definition, so
        // materialize both folders into the demo DefinePath (skip-if-exists) for IDefineAccess to
        // resolve. DbCategorySettings then registers them like any other table, and the ordinary
        // category loop builds them. Taking the folders wholesale rather than naming files is
        // deliberate: see GetFrameworkCommonTables for why, and for what the demo pays in return.
        Defaults.MaterializeTo(paths.DefinePath, new MaterializeOptions
        {
            Filter = rel => NorthwindSchemaSeeder.FrameworkTableSchemaPrefixes
                .Any(prefix => rel.StartsWith(prefix, StringComparison.Ordinal))
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
        // progId "System" to NorthwindSystemBusinessObject, and the framework
        // resolves it from there like any other progId.

        // Replace the default ICompanyInfoService (which reads st_company) with a hard-coded
        // demo company, so company-scoped forms route to the company database without seeding
        // the company / user-access tables. Registered after AddBeeFramework so it wins.
        builder.Services.AddSingleton<ICompanyInfoService, NorthwindCompanyInfoService>();

        return paths;
    }

    /// <summary>
    /// After the host is built: runs the schema seeder once.
    /// </summary>
    /// <param name="app">The built web application.</param>
    public static void UseNorthwindBackend(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var defineAccess = app.Services.GetRequiredService<IDefineAccess>();
        var connectionManager = app.Services.GetRequiredService<IDbConnectionManager>();
        var dbAccessFactory = app.Services.GetRequiredService<IDbAccessFactory>();
        NorthwindSchemaSeeder.EnsureSchemaAndSeed(defineAccess, connectionManager, dbAccessFactory);
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
            $"'{AppContext.BaseDirectory}'. Run the demo from inside the checkout.");
    }
}
