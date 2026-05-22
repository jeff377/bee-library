using Bee.Api.Core;
using Bee.Base;
using Bee.Business;
using Bee.Db.Manager;
using Bee.Db.Providers.Sqlite;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Hosting;
using Microsoft.Data.Sqlite;
using QuickStart.Server.BusinessObjects;

namespace QuickStart.Server;

internal static class Program
{
    public static void Main(string[] args)
    {
        // Resolve the shared Define directory (walks up from the running binary
        // until it finds Define/SystemSettings.xml). This lets the sample work
        // both from `dotnet run` and from a published binary, and lets every
        // demo under samples/ share the same Define seed.
        var paths = new PathOptions { DefinePath = ResolveDefinePath() };

        // SQLite is registered manually because Bee.Db only ships dialect logic;
        // ADO.NET providers (SqliteFactory, NpgsqlFactory, etc.) stay opt-in so
        // the framework does not force every host to pull every DB driver.
        DbProviderRegistry.Register(DatabaseType.SQLite, SqliteFactory.Instance);
        DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());

        var settings = SystemSettingsLoader.Load(paths);
        SysInfo.Initialize(settings.CommonConfiguration);
        ApiServiceOptions.Initialize(
            settings.CommonConfiguration.ApiPayloadOptions,
            settings.CommonConfiguration.IsDebugMode);

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddBeeFramework(
            settings.BackendConfiguration,
            paths,
            autoCreateMasterKey: true);

        // Override the default resolver so progId "Echo" dispatches to the
        // sample's EchoBusinessObject; every other progId still falls back to
        // FormBusinessObject as the framework expects.
        builder.Services.AddSingleton<IFormBoTypeResolver, QuickStartFormBoTypeResolver>();

        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapControllers();
        app.Run();
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
