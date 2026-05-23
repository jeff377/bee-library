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

namespace Bee.Samples.Shared;

/// <summary>
/// One-line bootstrap for the Blazor demos. Resolves the shared
/// <c>samples/Define</c> directory, registers SQLite, loads SystemSettings,
/// wires <c>AddBeeFramework</c>, then swaps in <see cref="DemoBusinessObjectFactory"/>
/// so the login panel can authenticate against <see cref="DemoCredentials"/>
/// without seeding system tables.
/// </summary>
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

        var paths = new PathOptions { DefinePath = ResolveDefinePath() };

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
        // to DemoAuthenticatingSystemBusinessObject. The default IFormBoTypeResolver is left
        // in place — Employee CRUD continues to resolve via FormBusinessObject.
        builder.Services.AddSingleton<IBusinessObjectFactory, DemoBusinessObjectFactory>();

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
}
