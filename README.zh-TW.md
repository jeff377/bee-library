# Bee.UI.Core

[English](README.md)

[![NuGet](https://img.shields.io/nuget/v/Bee.UI.Core.svg)](https://www.nuget.org/packages/Bee.UI.Core/)
[![Build CI](https://github.com/jeff377/bee-ui-core/actions/workflows/build-ci.yml/badge.svg)](https://github.com/jeff377/bee-ui-core/actions/workflows/build-ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

[Bee.NET](https://github.com/jeff377/bee-library) UI 應用的用戶端橋接層套件。`Bee.UI.Core` 將 `Bee.Api.Client` 的 API 連接器封裝在單一靜態進入點 `ClientInfo` 之下，統一管理連線設定、登入狀態與定義資料存取，讓 WinForms / WPF / Avalonia 等用戶端應用專注於畫面，不必自行處理連線初始化與 token 生命週期。

目標框架 **`net10.0`**。

## ✨ 特色

- **一行啟動**：`ClientInfo.Initialize(...)` 會自動載入已存的服務端點、驗證、必要時叫出設定畫面，並完成 API 連接器初始化。
- **近端 / 遠端連線抽象**：透過服務端點字串切換近端（本地檔案路徑）或遠端（JSON-RPC URL），其餘 API 介面完全相同。
- **連接器快取**：`SystemApiConnector` 與 `IDefineAccess` 依 token 快取；重設 access token 時自動失效。
- **可抽換的設定儲存**：預設的 `EndpointStorage`（XML 寫入 `<exe>.Settings.xml`）可替換為自訂的 `IEndpointStorage` 實作。
- **由用戶端決定 UI**：連線設定畫面藉 `IUIViewService` 抽象出去，套件本身不綁定特定 UI 框架。

## 📦 安裝

```bash
dotnet add package Bee.UI.Core
```

或於 `.csproj`：

```xml
<PackageReference Include="Bee.UI.Core" Version="4.3.0" />
```

相依套件：

| 套件 | 版本 |
|---|---|
| `Bee.Api.Client` | `4.3.0` |

（傳遞性帶入 `Bee.Base`、`Bee.Definition`、`Bee.Api.Core`、`Bee.Api.Contracts`。）

## 🚀 快速開始

### 1. 實作 UI 服務

`IUIViewService` 在已存服務端點失效或不存在時被呼叫，提示使用者重新設定連線：

```csharp
using Bee.UI.Core;

public class MainFormUIViewService : IUIViewService
{
    public bool ShowApiConnect()
    {
        using var dialog = new ApiConnectDialog();
        return dialog.ShowDialog() == DialogResult.OK;
    }
}
```

### 2. 啟動時初始化

```csharp
using Bee.UI.Core;
using Bee.Api.Client;

// 應用程式啟動：
if (!ClientInfo.Initialize(new MainFormUIViewService(), SupportedConnectTypes.Both))
{
    // 使用者取消設定 — 結束程式
    return;
}
```

或直接指定服務端點（不啟動 UI fallback）：

```csharp
ClientInfo.Initialize("http://localhost:5000/jsonrpc/api");
```

### 3. 套用登入結果

成功透過 `SystemApiConnector` 登入後，將回應交給 `ClientInfo` 寫入 access token 與使用者資訊：

```csharp
var response = await ClientInfo.SystemApiConnector.LoginAsync(userId, password);
ClientInfo.ApplyLoginResult(response);
```

### 4. 使用連接器

```csharp
// 系統層連接器（依 token 自動快取）：
var system = ClientInfo.SystemApiConnector;

// 表單層連接器（指定程式代碼）：
var employee = ClientInfo.CreateFormApiConnector("Employee");

// 定義資料存取：
var formSchema = ClientInfo.DefineAccess.GetFormSchema("Employee");
```

### 5.（選用）近端連線：註冊後端服務

**遠端連線**（JSON-RPC URL）以上步驟即足夠 —— API 連接器透過 HTTP 與 server 溝通。

**近端連線**（in-process、不走 HTTP）需要宿主應用建立 Bee.NET 服務容器，並在 `ClientInfo.Initialize(...)` 之前注入給 `ApiClientInfo.LocalServiceProvider`。Composition root 位於 [`Bee.Hosting`](https://www.nuget.org/packages/Bee.Hosting/) 套件，這是**宿主端的相依**，`Bee.UI.Core` 本身不會（也不應）參考它。

在宿主專案（WinForms / WPF / Avalonia / Console）安裝 `Bee.Hosting`：

```bash
dotnet add package Bee.Hosting
```

啟動時組裝：

```csharp
using Bee.Api.Client;          // ApiClientInfo
using Bee.Hosting;             // AddBeeFramework
using Bee.UI.Core;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddBeeFramework(options =>
{
    // 依需要設定後端選項（define path、資料庫等）
});
var serviceProvider = services.BuildServiceProvider();

ApiClientInfo.LocalServiceProvider = serviceProvider;

if (!ClientInfo.Initialize(new MainFormUIViewService(), SupportedConnectTypes.Local))
{
    return;
}
```

`ClientInfo` 透過 `ApiClientInfo.LocalServiceProvider` 取得 in-process 執行器，不需直接相依後端型別，維持 UI 層 / 後端層的分離。

> 純遠端連線的宿主**不需要** `Bee.Hosting`，若應用只走 JSON-RPC URL 可略過此步驟。

## 🔧 進階自訂

### 替換服務端點儲存

預設 `EndpointStorage` 會將端點寫入執行檔同層的 `<ExeName>.Settings.xml`。若改用其他儲存方式（環境變數、Registry、記憶體等），於 `Initialize` 之前指定：

```csharp
public class EnvVarEndpointStorage : IEndpointStorage
{
    public string LoadEndpoint() => Environment.GetEnvironmentVariable("API_ENDPOINT") ?? "";
    public void SetEndpoint(string endpoint) { /* ... */ }
    public void SaveEndpoint(string endpoint) => Environment.SetEnvironmentVariable("API_ENDPOINT", endpoint);
}

ClientInfo.EndpointStorage = new EnvVarEndpointStorage();
ClientInfo.Initialize(uiViewService, SupportedConnectTypes.Both);
```

### 由命令列覆寫服務端點

`Initialize(IUIViewService, SupportedConnectTypes)` 會解析命令列上的 `Key=Value` 參數。傳入 `Endpoint=...` 即可在啟動時覆寫已存端點：

```bash
MyApp.exe Endpoint=http://staging.example.com/jsonrpc/api
```

完整的解析結果可由 `ClientInfo.Arguments` 取得。

## 🧩 公開 API 一覽

| 成員 | 說明 |
|---|---|
| `ClientInfo.Initialize(IUIViewService, SupportedConnectTypes)` | 由設定載入並初始化（含 UI fallback） |
| `ClientInfo.Initialize(string endpoint)` | 直接指定服務端點初始化 |
| `ClientInfo.SetEndpoint(string)` / `GetEndpoint()` | 執行期切換服務端點 |
| `ClientInfo.ApplyLoginResult(LoginResponse)` | 套用登入結果（token + 使用者資訊） |
| `ClientInfo.SystemApiConnector` | 系統層連接器（自動快取） |
| `ClientInfo.CreateFormApiConnector(progId)` | 取得指定程式的表單層連接器 |
| `ClientInfo.DefineAccess` | 遠端定義資料存取器 |
| `ClientInfo.UserInfo` / `AccessToken` | 已登入使用者狀態 |
| `IUIViewService` | 用戶端視窗服務契約 |
| `IEndpointStorage` / `EndpointStorage` | 服務端點儲存契約與預設實作 |
| `VersionInfo` | 進入點組件的版本／產品資訊 |

## 🌐 Bee.NET 生態系

| 倉庫 | 角色 |
|---|---|
| [bee-library](https://github.com/jeff377/bee-library) | 核心框架（`Bee.Base`、`Bee.Definition`、`Bee.Api.*`、`Bee.Business` 等） |
| [bee-jsonrpc-sample](https://github.com/jeff377/bee-jsonrpc-sample) | JSON-RPC 完整 server / client 範例 |
| **bee-ui-core**（本倉庫） | 用戶端連線狀態與連接器封裝 |

## 📬 聯絡

[Facebook](https://www.facebook.com/profile.php?id=61574839666569) ｜ [HackMD](https://hackmd.io/@jeff377) ｜ [GitHub](https://github.com/jeff377) ｜ [NuGet](https://www.nuget.org/profiles/jeff377)

## 📄 授權

[MIT](LICENSE.txt) © Bee.NET
