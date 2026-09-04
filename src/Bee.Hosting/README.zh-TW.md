# Bee.Hosting

> Bee.NET 框架的 composition root — 將所有後端服務註冊至任意 `IServiceCollection`，不依賴 ASP.NET Core。

[English](README.md)

## 架構定位

- **層級**：Composition root（DI 註冊）
- **在相依圖中的位置**：見[專案相依性全景圖](../../docs/dependency-map.zh-TW.md)。**此處不逐一列出** —— 權威來源是 csproj，而散落在每份套件 README 的散文拷貝會漂且無人察覺。它們確實漂了：`Bee.Hosting` 抽出後，有四份 README 的下游數個月都沒把它補上。
- 亦由非 ASP.NET Core 宿主消費：WinForms / WPF / Console / Worker Service / 整合測試。

組合根橫跨各層本就是其職責，故「API 層不得引用 Repository 層」的限制不適用於此套件。真正適用的限制是
**本套件不自帶資料存取**：hosted service 只是外殼——cache-notify 輪詢透過 `ICacheNotifyReader`
（`Bee.Db`）讀取，稽核 sink 透過 `IAuditLogWriteRepository`（`Bee.Repository`）寫入。語句的組建與執行
屬於那兩層，在此新增 SQL 即為分層回歸。

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
  Audit/                                       # IAuditLogSink、AuditLogDbSink、
                                               # AuditLogWriterService、SynchronousAuditLogWriter
  CacheNotify/                                 # CacheNotifyPoller、CacheNotifyPollSession
```
