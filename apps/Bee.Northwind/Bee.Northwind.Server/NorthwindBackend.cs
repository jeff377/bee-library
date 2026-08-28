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
/// directory, registers SQLite, loads SystemSettings and wires <c>AddBeeFramework</c>.
/// </summary>
/// <remarks>
/// Nothing on the session axis is overridden or substituted. The seeder writes
/// <see cref="NorthwindCredentials"/> into <c>st_user</c>, <c>st_company</c> and
/// <c>st_user_company</c>, and the demo then runs the framework's own sign-in and company entry
/// against those rows — the same two steps a multi-company deployment takes. A single company
/// makes the second step look redundant; it is not, and taking a shortcut past it costs more
/// than it saves (see <c>apps/Bee.Northwind/README.md</c>).
/// </remarks>
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

        string definePath = ResolveDefinePath();
        var paths = new PathOptions
        {
            DefinePath = definePath,
            // Turns the tenant customization layer on. A non-empty CustomizePath is one half of the
            // gate (SessionInfo.CustomizeId is the other), and the demo company names
            // NorthwindCredentials.CustomizeId, so definition lookups consult
            // Customize/northwind-demo/ before the packaged Define/ tree. Clearing either half
            // returns the demo to a pure packaged deployment with no other change.
            CustomizePath = ResolveCustomizePath(definePath),
        };

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

        // Nothing is registered past AddBeeFramework. Sign-in, company entry and company lookup
        // all run the framework's own implementations: NorthwindSchemaSeeder writes the st_user,
        // st_company and st_user_company rows they read, and the client calls EnterCompany after
        // Login like any other deployment.

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

    /// <summary>
    /// Resolves the tenant customization root as the sibling of the <c>Define</c> directory.
    /// </summary>
    /// <param name="definePath">The resolved <c>Define</c> directory.</param>
    /// <remarks>
    /// Derived from <paramref name="definePath"/> rather than walked for independently: the two
    /// roots are siblings by layout, and a second walk could pair a <c>Define</c> from one checkout
    /// with a <c>Customize</c> from an enclosing one. The directory need not exist — a missing
    /// override file is the normal answer everywhere in the customization layer.
    /// </remarks>
    private static string ResolveCustomizePath(string definePath)
        => Path.Combine(Path.GetDirectoryName(definePath)!, "Customize");

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
