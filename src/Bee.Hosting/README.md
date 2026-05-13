# Bee.Hosting

> Composition root for the Bee.NET framework — registers all backend services into any `IServiceCollection`, with no ASP.NET Core dependency.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Composition root (DI registration)
- **Downstream** (dependents): `Bee.Api.AspNetCore`; non-ASP.NET Core hosts (WinForms / WPF / Console / Worker Service / integration tests)
- **Upstream** (dependencies): `Bee.Api.Core`, `Bee.Business`, `Bee.Repository`, `Bee.ObjectCaching` (which transitively bring in `Bee.Definition`, `Bee.Base`, `Bee.Db`, `Bee.Repository.Abstractions`, `Bee.Api.Contracts`)

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
```
