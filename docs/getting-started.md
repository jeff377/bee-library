# Getting Started

[繁體中文](getting-started.zh-TW.md) · [← Docs Index](README.md)

> Build your first Bee.NET backend from an empty folder: install the packages, materialise a `DefinePath`, wire the DI container, publish the JSON-RPC endpoint, add one business object, and call it from a client.

This walkthrough builds **your own project**. If you would rather see the framework running before writing anything, the repository's [`samples/`](../samples/README.md) folder has ready-to-run demos — `QuickStart.Server` + `QuickStart.Console` are the two this page mirrors.

Each step links to the document that covers it in depth. Everything shown here is the minimum that runs; nothing is repeated from those documents.

---

## Prerequisites

- **.NET 10 SDK**
- **A database.** Any of SQL Server, PostgreSQL, MySQL, Oracle or SQLite. SQLite needs no server and is used below.

## 1. Create the project and add the packages

```bash
dotnet new web -o MyApp.Server
cd MyApp.Server
dotnet add package Bee.Api.AspNetCore
dotnet add package Bee.Db
```

**Which host package?** `Bee.Api.AspNetCore` transitively pulls in `Bee.Hosting`, the composition root. If you are hosting outside ASP.NET Core — WinForms, WPF, Console, Worker Service — reference `Bee.Hosting` directly instead and skip step 4's `UseBeeFramework` call.

## 2. Materialise the `DefinePath`

The framework boots from a directory of XML definition files (its `DefinePath`). The framework's own minimum set — the `st_*` TableSchemas, `SystemSettings.xml`, `DatabaseSettings.xml`, `DbCategorySettings.xml` and the shipped Department / Employee forms — is embedded in `Bee.Definition.dll`. Materialise it once:

```bash
dotnet tool install -g Bee.Cli
dotnet bee defines materialize --path ./Define
```

Skip-existing is the default, so re-running never overwrites your own edits. The same operation is available programmatically via `Bee.Definition.Defaults.MaterializeTo(...)`.

Then edit two files under `./Define`:

- **`SystemSettings.xml`** — set `MasterKeySource`. `Environment` is the default and reads the key from `BEE_MASTER_KEY`.
- **`DatabaseSettings.xml`** — add your connection string.

→ Every definition file and what it owns: [Definition Files Overview](definition-files-overview.md). The full file list and consumer extension rules: [Framework-Reserved Names](framework-reserved-names.md).

## 3. Register your database dialect

The framework does not force every host to pull in every ADO.NET driver, so the dialect you use is registered explicitly:

```csharp
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Providers.Sqlite;
using Microsoft.Data.Sqlite;

DbProviderRegistry.Register(DatabaseType.SQLite, new SqliteProviderFactory(SqliteFactory.Instance));
DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());
```

Swap `Sqlite` for `SqlServer`, `PostgreSql`, `MySql` or `Oracle` as needed.

## 4. Wire the DI container

```csharp
using Bee.Api.AspNetCore;
using Bee.Api.Core;
using Bee.Base;
using Bee.Definition;
using Bee.Hosting;

var builder = WebApplication.CreateBuilder(args);

var paths = new PathOptions { DefinePath = "./Define" };
var settings = SystemSettingsLoader.Load(paths);

SysInfo.Initialize(settings.CommonConfiguration);
ApiServiceOptions.Initialize(
    settings.CommonConfiguration.ApiPayloadOptions,
    settings.CommonConfiguration.IsDebugMode);

builder.Services.AddBeeFramework(
    settings.BackendConfiguration,
    paths,
    autoCreateMasterKey: true);

builder.Services.AddControllers();

var app = builder.Build();
app.UseBeeFramework();
app.MapControllers();
app.Run();
```

**The order matters.** `SystemSettingsLoader.Load` must precede `SysInfo.Initialize`, which must precede `AddBeeFramework`. `UseBeeFramework` registers no middleware and no endpoint — it only runs startup checks.

→ The startup flow diagram and what `AddBeeFramework` registers: [Development Cookbook § Framework Initialization Order](development-cookbook.md#framework-initialization-order). The constraints behind the ordering: [Development Constraints § Initialization Order](development-constraints.md#initialization-order-constraints).

## 5. Publish the JSON-RPC endpoint

`ApiServiceController` already declares `[Route("api")]` and the POST handler, so an empty subclass is the whole endpoint:

```csharp
using Bee.Api.AspNetCore.Controllers;

namespace MyApp.Server.Controllers;

public class ApiController : ApiServiceController
{
}
```

`POST /api` now speaks JSON-RPC 2.0.

## 6. Write your first business object

A business object is reached by its **progId**. Anything other than `"System"` is dispatched as a form business object, so inherit `FormBusinessObject` and mirror its constructor signature:

```csharp
using Bee.Business;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;

namespace MyApp.Server.BusinessObjects;

public class EchoArgs : BusinessArgs
{
    public string Message { get; set; } = string.Empty;
}

public class EchoResult : BusinessResult
{
    public string Response { get; set; } = string.Empty;
}

public class EchoBusinessObject : FormBusinessObject
{
    public EchoBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
        : base(ctx, accessToken, progId, isLocalCall)
    {
    }

    [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
    public virtual EchoResult Echo(EchoArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return new EchoResult { Response = $"echo: {args.Message}" };
    }
}
```

`[ApiAccessControl]` is what makes the method reachable and decides its protection level. `Public` + `Anonymous` needs neither an access token nor the encryption handshake — appropriate for a first call, and **not** for real data.

The progId-to-type binding lives in `ProgramSettings.xml` — the framework-wide type registry. No resolution code is required:

```xml
<ProgramSettings>
  <Items>
    <ProgramItem ProgId="Echo" DisplayName="Echo"
                 BusinessObject="MyApp.EchoBusinessObject, MyApp" />
  </Items>
</ProgramSettings>
```

`BusinessObject` is an assembly-qualified type name. Any progId not listed resolves to the
framework's default `FormBusinessObject`, so **only progIds that need custom logic belong here**.
The same entry can also bind a dedicated repository through the `Repository` attribute; the two
attributes are independent.

The framework self-registers missing reserved progIds at startup, so this file is created
automatically when absent. See [ADR-034](adr/adr-034-progid-type-registry.md).

→ Naming rules for `Args` / `Result` and the three-tier contract separation: [API ↔ BO Contract Design](api-bo-contract-design.md). Which methods belong on an interface: [Development Constraints](development-constraints.md).

## 7. Call it from a client

From .NET, use `Bee.Api.Client`:

```csharp
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages;

ApiClientInfo.ApiKey = "my-demo-key";

var connector = new FormApiConnector("http://localhost:5050/api", Guid.Empty, "Echo");
var result = await connector.ExecuteAsync<EchoResponse>(
    "Echo",
    new EchoRequest { Message = "hello" },
    PayloadFormat.Plain);
```

Keep the client's request / response DTOs separate from the server's `Args` / `Result` — that is how a third-party integrator sees the contract, and it keeps the wire shape honest.

`PayloadFormat.Plain` matches the `Public` + `Anonymous` declaration above. Anything protected requires `Login` first, which issues the access token and the RSA handshake.

→ Calling from JavaScript / TypeScript with no .NET on the client: [JSON-RPC Frontend Integration](jsonrpc-frontend-integration.md). Every exposed method and its access control: [API Method Reference](api-method-reference.md).

## 8. Define a form instead of writing code

The Echo object above is hand-written on purpose — it is the smallest thing that proves the pipe works. **Ordinary CRUD needs no business object at all**: declare a `FormSchema` plus its `TableSchema`, and the framework generates the SQL, the list, and the save path from the definition.

That is the actual point of the framework, and it starts here → [Definition Files Overview](definition-files-overview.md), then [Architecture Overview](architecture-overview.md).

---

## Where to go next

| You want to | Read |
|-------------|------|
| Understand the design before going further | [Architecture Overview](architecture-overview.md) |
| Know what every definition file does | [Definition Files Overview](definition-files-overview.md) |
| Follow the full definition → API flow | [Development Cookbook](development-cookbook.md) |
| Compute fields and validate without code | [Expressions and Rules](expression-rules.md) |
| Add authentication and permissions | [Permission & Authorization](permission-authorization.md) |
| Push definition changes to a live database | [Database Schema Upgrade](database-schema-upgrade.md) |

A working end-to-end version of everything above lives in [`samples/QuickStart.Server`](../samples/QuickStart.Server/README.md) and [`samples/QuickStart.Console`](../samples/QuickStart.Console/README.md). For a full application built almost entirely from definitions, see [`apps/Bee.Northwind`](../apps/Bee.Northwind/README.md).
