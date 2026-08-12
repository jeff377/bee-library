# Backend bootstrap 樣板

`Program.cs` + `XxxBackend`。改 `Xxx` 為你的專案名。基於 `Bee.Samples.Shared/DemoBackend.cs`(與框架同版),並已在實際專案的 server 上驗證過。

## Program.cs

```csharp
using Xxx.Server;

var builder = WebApplication.CreateBuilder(args);
builder.AddXxxBackend();
builder.Services.AddControllers();

var app = builder.Build();
app.UseXxxBackend();
app.MapControllers();
app.Run();
```

## XxxBackend.cs

```csharp
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
using Microsoft.Data.Sqlite;

namespace Xxx.Server;

public static class XxxBackend
{
    // Dev-only fixed master key: a fresh clone runs with zero setup and encrypted rows stay
    // decryptable across runs. Production MUST inject a real BEE_MASTER_KEY before this runs.
    private const string DevMasterKey = "<base64-64-byte-aes-cbc-hmac-key>";
    private const string CommonDatabaseId = "common";

    public static PathOptions AddXxxBackend(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BEE_MASTER_KEY")))
        {
            Environment.SetEnvironmentVariable("BEE_MASTER_KEY", DevMasterKey);
        }

        var paths = new PathOptions { DefinePath = ResolveDefinePath() };

        // AddBeeFramework 註冊的 cache-notify poller 會讀 st_cache_notify;其 TableSchema 是框架
        // 內嵌預設,materialize 進 DefinePath(skip-if-exists)讓 IDefineAccess 找得到、seeder 建得出。
        Defaults.MaterializeTo(paths.DefinePath, new MaterializeOptions
        {
            Filter = rel => rel == "TableSchema/common/st_cache_notify.TableSchema.xml",
        });

        DbProviderRegistry.Register(DatabaseType.SQLite, new SqliteProviderFactory(SqliteFactory.Instance));
        DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());

        var settings = SystemSettingsLoader.Load(paths);
        SysInfo.Initialize(settings.CommonConfiguration);
        ApiServiceOptions.Initialize(
            settings.CommonConfiguration.ApiPayloadOptions,
            settings.CommonConfiguration.IsDebugMode);

        builder.Services.AddBeeFramework(settings.BackendConfiguration, paths, autoCreateMasterKey: true);

        // AddBeeFramework 之後註冊,last-wins:factory 把 System.Login 導到 demo 認證;resolver 綁 progId→BO。
        builder.Services.AddSingleton<IFormBoTypeResolver, BusinessObjects.XxxFormBoTypeResolver>();
        builder.Services.AddSingleton<IBusinessObjectFactory, BusinessObjects.XxxBusinessObjectFactory>();

        return paths;
    }

    /// <summary>Host built 之後:建框架表一次(有真 seeder 時在這裡跑)。</summary>
    public static void UseXxxBackend(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var defineAccess = app.Services.GetRequiredService<IDefineAccess>();
        var connectionManager = app.Services.GetRequiredService<IDbConnectionManager>();

        var builder = new TableSchemaBuilder(CommonDatabaseId, defineAccess, connectionManager);
        builder.Execute(CommonDatabaseId, "st_cache_notify");
        // 之後在此為每張業務表 builder.Execute(categoryId, table) + seed。
    }

    private static string ResolveDefinePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Define", "SystemSettings.xml");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"Could not locate 'Define/SystemSettings.xml' walking up from '{AppContext.BaseDirectory}'.");
    }
}
```

## 產生 dev master key

64-byte AES-CBC-HMAC combined key(base64)。可從框架的 sample credentials 借一組 dev 值,或用
`RandomNumberGenerator.GetBytes(64)` 產生後 base64。**只用於 dev**;正式走部署機制注入。

## CORS(僅當有 WASM/瀏覽器頭跨源呼叫)

`app.UseCors(...)` 要放在 `UseXxxBackend()` **之前**,讓 OPTIONS preflight 先被答覆,不被 access-control 擋。
純桌面 / 行動端不需要。
