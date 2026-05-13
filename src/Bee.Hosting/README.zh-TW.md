# Bee.Hosting

> Bee.NET 框架的 composition root — 將所有後端服務註冊至任意 `IServiceCollection`，不依賴 ASP.NET Core。

[English](README.md)

## 架構定位

- **層級**：Composition root（DI 註冊）
- **下游**（消費者）：`Bee.Api.AspNetCore`；非 ASP.NET Core 宿主（WinForms / WPF / Console / Worker Service / 整合測試）
- **上游**（相依）：`Bee.Api.Core`、`Bee.Business`、`Bee.Repository`、`Bee.ObjectCaching`（透過遞移帶入 `Bee.Definition`、`Bee.Base`、`Bee.Db`、`Bee.Repository.Abstractions`、`Bee.Api.Contracts`）

## 目標框架

- `net10.0`

## 何時引用此套件

| 宿主類型 | 引用方式 |
|---------|---------|
| ASP.NET Core web host | `Bee.Api.AspNetCore`（透過遞移帶入 `Bee.Hosting`）|
| WinForms / WPF / Console / Worker Service | 直接引用 `Bee.Hosting` |
| 整合測試 | 直接引用 `Bee.Hosting`（透過 `Bee.Tests.Shared`）|

UI / 客戶端層（`Bee.Api.Client` 的消費者）**不應**引用 `Bee.Hosting`。客戶端層透過 [`ApiClientInfo.LocalServiceProvider`](../Bee.Api.Client/ApiClientInfo.cs) 取得後端 `IServiceProvider`，由宿主應用程式注入。

## 主要公開 API

| 類別 / 成員 | 用途 |
|------------|------|
| `BeeFrameworkServiceCollectionExtensions.AddBeeFramework` | 將所有框架服務（`IDefineAccess`、`IDbAccessFactory`、`IBusinessObjectFactory`、`JsonRpcExecutor` 等）註冊至傳入的 `IServiceCollection` |

## 使用方式

### ASP.NET Core 宿主

```csharp
using Bee.Hosting;
using Bee.Api.AspNetCore;

var settings = SystemSettingsLoader.Load(pathOptions);
services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
app.UseBeeFramework();
```

### 非 ASP.NET Core 宿主（例如 WinForms 桌面近端連線）

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

## 設計慣例

- **Composition root** — DI 註冊集中於此，與 ASP.NET Core middleware（留在 `Bee.Api.AspNetCore`）切開
- **不依賴 ASP.NET Core** — 不引用 `Microsoft.AspNetCore.App`，非 web 宿主可註冊框架而不必拖入整個 web stack
- **反射載入實作** — `IDefineAccess`、`ISessionInfoService`、`IBusinessObjectFactory`、`I*RepositoryFactory` 等由 `SystemSettings.xml` 中的 `BackendComponents` 以型別名指定，啟動時反射載入，未設定時退回 `BackendDefaultTypes` 的預設值。`Bee.Repository` 列為 ProjectReference 是為了保證其 DLL 隨宿主部署，反射才能找到預設 factory

## 目錄結構

```
Bee.Hosting/
  BeeFrameworkServiceCollectionExtensions.cs   # AddBeeFramework 與輔助方法
```
