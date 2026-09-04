# Bee.Hosting

> Composition root for the Bee.NET framework — registers all backend services into any `IServiceCollection`, with no ASP.NET Core dependency.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Composition root (DI registration)
- **Position in the dependency graph**: see [Project Dependency Map](../../docs/dependency-map.md). Not enumerated here — the csproj files are the authority, and a prose copy in every package README drifts with nothing to catch it. These did: `Bee.Hosting` was missing as a dependent from four of them for months after it was extracted.
- Also consumed by non-ASP.NET Core hosts: WinForms / WPF / Console / Worker Service / integration tests.

A composition root reaches across every layer by definition, so the "API layer must not reference the
Repository layer" constraint does not apply here. What does apply: **this package holds no data access
of its own.** Its hosted services are shells — the cache-notify poller reads through
`ICacheNotifyReader` (`Bee.Db`) and the audit sink writes through `IAuditLogWriteRepository`
(`Bee.Repository`). Statement construction and execution belong to those layers; new SQL added here
would be a layering regression.

## Target Framework

- `net10.0`

## When to Reference This Package

| Host type | Reference |
|-----------|-----------|
| ASP.NET Core web host | `Bee.Api.AspNetCore` (transitively brings in `Bee.Hosting`) |
| WinForms / WPF / Console / Worker Service | `Bee.Hosting` directly |
| Integration tests | `Bee.Hosting` directly (via `Bee.Tests.Shared`) |

Do **not** reference `Bee.Hosting` from `Bee.Api.Client` consumers (UI / client tier). Client tier obtains the backend service provider via [`ApiClientInfo.LocalServiceProvider`](../Bee.Api.Client/ApiClientInfo.cs), populated by the host application.

## Key Public APIs

| Class / Member | Purpose |
|----------------|---------|
| `BeeFrameworkServiceCollectionExtensions.AddBeeFramework` | Registers all framework services (`IDefineAccess`, `IDbAccessFactory`, `IBusinessObjectFactory`, `JsonRpcExecutor`, etc.) into the supplied `IServiceCollection` |

## Usage

### ASP.NET Core host

```csharp
using Bee.Hosting;
using Bee.Api.AspNetCore;

var settings = SystemSettingsLoader.Load(pathOptions);
services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
app.UseBeeFramework();
```

### Non-ASP.NET Core host (e.g. WinForms desktop with near-end mode)

```csharp
using Bee.Hosting;
using Bee.Api.Client;

var services = new ServiceCollection();
var settings = SystemSettingsLoader.Load(pathOptions);
services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
var sp = services.BuildServiceProvider();

// Feed the backend provider to the UI tier's local connection adapter.
ApiClientInfo.LocalServiceProvider = sp;
ApiClientInfo.ConnectType = ConnectType.Local;
```

## Design Conventions

- **Composition root** — DI registration lives here, separated from ASP.NET Core middleware (which stays in `Bee.Api.AspNetCore`)
- **No ASP.NET Core dependency** — does not reference `Microsoft.AspNetCore.App`, so non-web hosts can register the framework without pulling in the web stack
- **Reflection-loaded implementations** — `IDefineAccess`, `ISessionInfoService`, `IBusinessObjectFactory`, `I*RepositoryFactory` and others are resolved at startup by type name from `BackendComponents` (in `SystemSettings.xml`), falling back to defaults in `BackendDefaultTypes`. The `Bee.Repository` ProjectReference ensures its DLL ships with the host so default factories can be reflection-loaded.

## Directory Structure

```
Bee.Hosting/
  BeeFrameworkServiceCollectionExtensions.cs   # AddBeeFramework + helpers
  Audit/                                       # IAuditLogSink, AuditLogDbSink,
                                               # AuditLogWriterService, SynchronousAuditLogWriter
  CacheNotify/                                 # CacheNotifyPoller, CacheNotifyPollSession
```
