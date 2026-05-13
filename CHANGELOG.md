# Changelog

[繁體中文](CHANGELOG.zh-TW.md)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [5.0.0]

### Added

- **New package `Bee.Hosting`** — composition root for the Bee.NET framework. Registers all backend services (`IDefineAccess`, `IDbAccessFactory`, `IBusinessObjectFactory`, `JsonRpcExecutor`, etc.) into any `IServiceCollection` without depending on ASP.NET Core. Non-ASP.NET Core hosts (WinForms, WPF, Console, Worker Service, integration tests) can now register the framework without pulling in `Microsoft.AspNetCore.App`.

### Changed (Breaking)

- **`BeeFrameworkServiceCollectionExtensions.AddBeeFramework` moved from `Bee.Api.AspNetCore` to `Bee.Hosting`.**
  - Namespace changed from `Bee.Api.AspNetCore` to `Bee.Hosting`.
  - ASP.NET Core hosts: `Bee.Api.AspNetCore` now references `Bee.Hosting`, so the package is brought in transitively. Add `using Bee.Hosting;` next to the existing `using Bee.Api.AspNetCore;`.
  - Non-ASP.NET Core hosts: reference `Bee.Hosting` directly instead of `Bee.Api.AspNetCore`. No more transitive `Microsoft.AspNetCore.App` dependency.
- `Bee.Api.AspNetCore` now only contains ASP.NET Core integration (`UseBeeFramework` middleware hook + `ApiServiceController`); its 4 previous project references (`Bee.Api.Core`, `Bee.Business`, `Bee.Db`, `Bee.ObjectCaching`, `Bee.Repository`) are now consolidated under `Bee.Hosting`.

### Migration

**ASP.NET Core web host:**

```diff
+ using Bee.Hosting;
  using Bee.Api.AspNetCore;

  var settings = SystemSettingsLoader.Load(pathOptions);
  services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
  app.UseBeeFramework();
```

**Non-ASP.NET Core host (WinForms / WPF / Console / Worker / integration test):**

```diff
  <!-- *.csproj -->
- <PackageReference Include="Bee.Api.AspNetCore" Version="4.*" />
+ <PackageReference Include="Bee.Hosting" Version="5.*" />
```

```csharp
using Bee.Hosting;
using Bee.Api.Client;

var services = new ServiceCollection();
var settings = SystemSettingsLoader.Load(pathOptions);
services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
var sp = services.BuildServiceProvider();

// Feed the backend provider to the UI tier's local-connection adapter.
ApiClientInfo.LocalServiceProvider = sp;
ApiClientInfo.ConnectType = ConnectType.Local;
```

## [4.2.0] and earlier

See git history (`git log --oneline`).
