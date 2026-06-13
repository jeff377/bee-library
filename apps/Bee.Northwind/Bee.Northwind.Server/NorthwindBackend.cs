using Bee.Api.Client;
using Bee.Api.Core;
using Bee.Base;
using Bee.Business;
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

namespace Bee.Northwind.Server;

/// <summary>
/// One-line bootstrap for the Bee.Northwind demo. Resolves the sibling <c>Define</c>
/// directory, registers SQLite, loads SystemSettings, wires <c>AddBeeFramework</c>, then
/// swaps in <see cref="NorthwindBusinessObjectFactory"/> so login authenticates against
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

        // AddBeeFramework registers the cache-notify poller, which reads st_cache_notify.
        // Its TableSchema ships as an embedded framework default in Bee.Definition, so
        // materialize it into the demo DefinePath (skip-if-exists) for IDefineAccess to
        // resolve; NorthwindSchemaSeeder then creates the table.
        Defaults.MaterializeTo(paths.DefinePath, new MaterializeOptions
        {
            Filter = rel => rel == "TableSchema/common/st_cache_notify.TableSchema.xml"
        });

        // SQLite providers — keep dialect registration explicit so the framework does
        // not force every host to pull every ADO.NET driver.
        DbProviderRegistry.Register(DatabaseType.SQLite, SqliteFactory.Instance);
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

        // Replace the default factory so SystemBusinessObject calls (Login etc.) dispatch
        // to NorthwindAuthenticatingSystemBusinessObject. The default IFormBoTypeResolver
        // is left in place — Category CRUD continues to resolve via FormBusinessObject.
        builder.Services.AddSingleton<IBusinessObjectFactory, NorthwindBusinessObjectFactory>();

        return paths;
    }

    /// <summary>
    /// After the host is built: hooks <see cref="ApiClientInfo.LocalServiceProvider"/> and
    /// runs the schema seeder once.
    /// </summary>
    /// <param name="app">The built web application.</param>
    public static void UseNorthwindBackend(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        ApiClientInfo.LocalServiceProvider = app.Services;

        var defineAccess = app.Services.GetRequiredService<IDefineAccess>();
        var connectionManager = app.Services.GetRequiredService<IDbConnectionManager>();
        NorthwindSchemaSeeder.EnsureSchema(defineAccess, connectionManager);
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
