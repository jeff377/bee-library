# 版本變更記錄

[English](CHANGELOG.md)

本檔記錄專案的所有重要變更。

格式遵循 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.1.0/)，版本號採用 [語意化版本](https://semver.org/lang/zh-TW/)。

## [5.0.0]

### 新增

- **新套件 `Bee.Hosting`** — Bee.NET 框架的 composition root。將所有後端服務（`IDefineAccess`、`IDbAccessFactory`、`IBusinessObjectFactory`、`JsonRpcExecutor` 等）註冊到任意 `IServiceCollection`，不依賴 ASP.NET Core。非 ASP.NET Core 宿主（WinForms、WPF、Console、Worker Service、整合測試）現在可以註冊框架而不必拖入 `Microsoft.AspNetCore.App`。

### 變更（破壞性）

- **`BeeFrameworkServiceCollectionExtensions.AddBeeFramework` 從 `Bee.Api.AspNetCore` 搬至 `Bee.Hosting`。**
  - 命名空間從 `Bee.Api.AspNetCore` 改為 `Bee.Hosting`
  - ASP.NET Core 宿主：`Bee.Api.AspNetCore` 已改為引用 `Bee.Hosting`，會以遞移方式帶入。啟動程式需在既有 `using Bee.Api.AspNetCore;` 旁加上 `using Bee.Hosting;`
  - 非 ASP.NET Core 宿主：直接引用 `Bee.Hosting` 取代 `Bee.Api.AspNetCore`，不再透過遞移帶入 `Microsoft.AspNetCore.App`
- `Bee.Api.AspNetCore` 現在僅包含 ASP.NET Core 整合（`UseBeeFramework` middleware hook + `ApiServiceController`）；原有 4 個 ProjectReference（`Bee.Api.Core`、`Bee.Business`、`Bee.Db`、`Bee.ObjectCaching`、`Bee.Repository`）全部合併至 `Bee.Hosting`

### 升級指引

**ASP.NET Core web host：**

```diff
+ using Bee.Hosting;
  using Bee.Api.AspNetCore;

  var settings = SystemSettingsLoader.Load(pathOptions);
  services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
  app.UseBeeFramework();
```

**非 ASP.NET Core 宿主（WinForms / WPF / Console / Worker / 整合測試）：**

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

// 把後端 service provider 注入給 UI 層作為近端連線來源。
ApiClientInfo.LocalServiceProvider = sp;
ApiClientInfo.ConnectType = ConnectType.Local;
```

## [4.2.0] 與更早版本

見 git 歷史（`git log --oneline`）。
