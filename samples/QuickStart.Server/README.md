# QuickStart.Server

**English** | [繁體中文](README.zh-TW.md)

The minimal runnable Bee.NET JSON-RPC API host. It exposes:

- `System.Ping` — framework built-in, anonymous; confirms the host is reachable.
- `Echo.Echo` — sample BO, anonymous; returns the request message decorated with an `"echo: "` prefix.

## How to run

```bash
cd samples/QuickStart.Server
dotnet run
```

The host listens on `http://localhost:5050`; the JSON-RPC endpoint is `POST /api`.

## What to expect

On first startup it will:

1. Load `SystemSettings.xml` / `DbCategorySettings.xml` / `DatabaseSettings.xml` from `samples/Define/`
2. Auto-generate a master key at `samples/Define/Master.key` (gitignored)
3. Create `quickstart.db` in the working directory (gitignored)

The console should print `Now listening on: http://localhost:5050`.

## What this maps to in the library

| Code | Library feature |
|------|-----------------|
| `DbProviderRegistry.Register(DatabaseType.SQLite, SqliteFactory.Instance)` | `Bee.Db.Manager.DbProviderRegistry` — pluggable ADO.NET providers |
| `DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory())` | `Bee.Db.Providers.Sqlite` — SQLite dialect (form CRUD, schema reflection, DDL) |
| `SystemSettingsLoader.Load(paths)` | `Bee.Definition.SystemSettingsLoader` — boot-time XML loading |
| `services.AddBeeFramework(...)` | `Bee.Hosting.BeeFrameworkServiceCollectionExtensions` — backend composition root |
| `IFormBoTypeResolver` override | `Bee.Business.IFormBoTypeResolver` — custom progId → BO type mapping |
| Empty `: ApiServiceController` controller | `Bee.Api.AspNetCore.Controllers.ApiServiceController` — `[Route("api")]` JSON-RPC endpoint |
| `[ApiAccessControl(Public, Anonymous)]` | `Bee.Definition.Attributes.ApiAccessControlAttribute` — API access control |

## Try it without the console demo

```bash
curl -s -X POST http://localhost:5050/api \
  -H 'Content-Type: application/json' \
  -H 'X-Api-Key: quickstart-demo' \
  -d '{
        "jsonrpc": "2.0",
        "id": "1",
        "method": "Echo.Echo",
        "params": { "Value": { "Message": "hello" } }
      }'
```

The response should be a JSON-RPC envelope whose `result.value` contains `Response = "echo: hello"`.
