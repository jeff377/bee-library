# 專案相依性全景圖

[English](dependency-map.md)

本文件以視覺化方式呈現 Bee.NET 框架中 18 個 `src/` 專案之間的相依關係。

**閱讀方式**：箭頭方向 A → B 表示「A 依賴 B」；圖表由下而上排列，最底層為無相依性的基礎套件。

## 相依性圖表

```mermaid
graph BT
  subgraph 基礎設施層
    Base["Bee.Base"]
    Expressions["Bee.Expressions"]
    Definition["Bee.Definition"]
    Caching["Bee.ObjectCaching"]
  end

  subgraph 資料存取層
    RepoAbs["Bee.Repository.Abstractions"]
    Db["Bee.Db"]
    Repo["Bee.Repository"]
  end

  subgraph 商業邏輯層
    Business["Bee.Business"]
  end

  subgraph SharedContracts [共用契約層]
    Contracts["Bee.Api.Contracts"]
  end

  subgraph API 層
    Core["Bee.Api.Core"]
    Hosting["Bee.Hosting"]
    AspNet["Bee.Api.AspNetCore"]
  end

  subgraph 用戶端
    Client["Bee.Api.Client"]
  end

  subgraph 跨平台 UI 共通
    UICore["Bee.UI.Core"]
    UIAvalonia["Bee.UI.Avalonia"]
    UIMaui["Bee.UI.Maui"]
  end

  subgraph Web 前端層
    BlazorSrv["Bee.Web.Blazor.Server"]
    BlazorWasm["Bee.Web.Blazor.Wasm"]
  end

  Definition --> Base
  Expressions --> Base
  Definition --> Expressions
  Business --> Expressions
  Hosting --> Expressions
  UIAvalonia --> Expressions
  Contracts --> Definition
  Db --> Definition
  RepoAbs --> Definition
  Caching --> Definition
  Caching --> RepoAbs
  Business --> Contracts
  Business --> Definition
  Business --> RepoAbs
  Repo --> Db
  Repo --> RepoAbs
  Core --> Contracts
  Core --> Definition
  Hosting --> Core
  Hosting --> Business
  Hosting --> Repo
  Hosting --> Caching
  AspNet --> Hosting
  Client --> Core
  UICore --> Client
  UIAvalonia --> UICore
  UIAvalonia --> Client
  UIAvalonia --> Definition
  UIMaui --> UICore
  BlazorSrv --> Client
  BlazorWasm --> Client
```

## 外部相依套件

| 專案 | 外部套件 |
|------|----------|
| Bee.Base | *(none)* |
| Bee.Expressions | DynamicExpresso.Core 2.x |
| Bee.Definition | MessagePack 3.x、Microsoft.Extensions.Localization.Abstractions 10.x |
| Bee.Db | *(none)* |
| Bee.ObjectCaching | Microsoft.Extensions.Caching.Memory 10.x、Microsoft.Extensions.FileProviders.Physical 10.x |
| Bee.Hosting | Microsoft.Extensions.DependencyInjection 10.x |
| Bee.Api.AspNetCore | `FrameworkReference: Microsoft.AspNetCore.App` |
| Bee.Web.Blazor.Server | `Microsoft.AspNetCore.Components.Web` 等 Blazor Server 套件 |
| Bee.Web.Blazor.Wasm | `Microsoft.AspNetCore.Components.WebAssembly` 等 WASM 套件 |
| Bee.UI.Avalonia | Avalonia 12.0.x、Avalonia.Controls.DataGrid 12.0.x |
| Bee.Api.Contracts / Bee.Api.Core / Bee.Api.Client / Bee.Business / Bee.Repository / Bee.Repository.Abstractions / Bee.UI.Core / Bee.UI.Maui | *(none)* |

## 目標框架摘要

除 `Bee.Web.Blazor.Wasm` 需 `wasm-tools` workload 外，所有專案皆以 `net10.0` 單一目標發布。

## 工具套件（獨立發行）

不屬於上方 `src/` 套件相依圖——以 `dotnet tool` 全域工具形式 ship 在 NuGet：

| 套件 | 命令 | 說明 |
|------|------|------|
| **Bee.Cli**（`tools/Bee.Cli/`） | `dotnet bee` | 框架 CLI。本版 ship 出 `defines` subcommand group。Reference `Bee.Definition` 呼叫其公開 `Defaults` API，處理 embedded 框架預設的 materialize / list。版本與框架 lock-step。 |

同位於 `tools/` 但不上 NuGet：

- **Bee.DefineEditor**（`tools/DefineEditor/`）—— Avalonia 桌面工具，可視覺化編輯九種定義類型。以 `.app` / `.exe` 形式對外發行而非套件或 dotnet tool。開啟資料夾時 in-process 呼叫 `Bee.Definition.Defaults.MaterializeTo(...)`。

## 架構要點

- **Bee.Base** 為最底層基礎套件，無任何內部相依性。
- **Bee.Expressions** 為可攜、沙箱化的運算式求值引擎（DynamicExpresso 封裝），只依賴 `Bee.Base`。由 `Bee.Definition`（`FormExpressionCalculator`）、`Bee.Business`（規則處理器）、`Bee.Hosting`（DI 註冊）與 `Bee.UI.Avalonia`（前端即時預覽）共用，使前端算值與後端存檔一致。見 [adr-028](adr/adr-028-expression-rule-engine.md)。
- **Bee.Definition** 為被依賴次數最多的專案，共有 6 個直接相依者（Contracts、Db、RepoAbs、Caching、Business、Core）。
- **Bee.Api.Contracts** 是共用契約／抽象層，並非應用層級的 API 專案。雖名為「API」，但 `Bee.Business` 與 `Bee.Api.Core` 都相依於它（`Business → Contracts`、`Core → Contracts`），故其位置在兩者**之下** —— 圖上歸入 **共用契約層**，而非 API 應用層。
- **Bee.Hosting** 為 composition root：將後端服務（`Bee.Api.Core`、`Bee.Business`、`Bee.Repository`、`Bee.ObjectCaching`）整合於一個 `IServiceCollection.AddBeeFramework` 擴充入口，不依賴 ASP.NET Core。非 web 宿主（WinForms、Console、Worker Service）直接引用此套件。
- **Bee.Api.AspNetCore** 為 ASP.NET Core 整合層（`UseBeeFramework` middleware 與 `ApiServiceController`），透過遞移引用 `Bee.Hosting`，使 web 宿主一次引用即取得 DI 註冊與 middleware。
- 用戶端（Bee.Api.Client）與伺服器端（Bee.Api.AspNetCore）皆透過 **Bee.Api.Core** 共享協定邏輯，確保序列化與加解密行為一致。
- **Bee.UI.Core** 為跨平台 UI 共通層（`ClientInfo` / `IEndpointStorage` / `IUIViewService` / `VersionInfo`），供所有 native UI family（Avalonia 桌面 / MAUI 行動 / 未來 WinForms / WPF）共用 client-side 連線狀態與 endpoint 持久化邏輯；不含任何平台專屬 UI 程式碼，只依 `Bee.Api.Client`。
- **Bee.UI.Avalonia** 為 Avalonia 桌面控制項套件（Windows / macOS / Linux）。內含 FormSchema 驅動控制項（`FormView` 單筆、`ListView` 清單、`GridControl` 表格，加上一組 field editor 與 `FormScope` ambient 綁定，皆以 `FormDataObject` 為資料中樞）與檔案後端 `FileEndpointStorage`，單一 `net10.0` TFM。下限版本鎖在 `Avalonia 12.0.0` + `Avalonia.Controls.DataGrid 12.0.0`（後者目前 stable 最高就是 12.0.0），host 可以透過 transitive 帶更新的 12.0.x。DataGrid 為何不走 `Binding "[FieldName]"` 詳見 [adr-020](adr/adr-020-avalonia-datagrid-binding-strategy.md)，編輯策略詳見 [adr-021](adr/adr-021-avalonia-datagrid-editing-strategy.md)。
- **Bee.UI.Maui** 為 MAUI 跨平台控制項套件（iOS / Android / macOS / Windows）；Phase 1 已交付首版 FormSchema 驅動控制項（`DynamicForm` + `FormDataObject`），csproj 以 `net10.0` 共通邏輯 TFM 為預設並引用 `Microsoft.Maui.Controls`，平台 TFM（`net10.0-android` / `net10.0-ios` / `net10.0-maccatalyst` / `net10.0-windows`）透過 `-p:BeeUiMauiFullPlatforms=true` opt-in（需安裝對應 workload）。NuGet 發版仍延後至控制項套件完整時統一處理。詳見 `src/Bee.UI.Maui/README.md`。
- **`Bee.UI.*` family 判別準則**：是否消費 `Bee.UI.Core` 抽象（`ClientInfo` / `IEndpointStorage` / `IUIViewService` 等）。
  - 消費 → 歸 `Bee.UI.*`（目前：`Bee.UI.Core`、`Bee.UI.Avalonia`、`Bee.UI.Maui`；未來：`Bee.UI.WinForms`、`Bee.UI.Wpf` 等同理）
  - 不消費，自有狀態管理 → 走獨立 family prefix（如 `Bee.Web.Blazor.*`：Blazor circuit / WASM 環境無檔案 IO 與 dialog service 概念，獨立路線合理）
- **Web 前端層**（`Bee.Web.Blazor.Server`、`Bee.Web.Blazor.Wasm`）為 RCL（Razor Class Library）元件庫，兩者一律只相依 `Bee.Api.Client`，由宿主決定 `IApiProvider` 實作（`LocalApiProvider` / `RemoteApiProvider`）與是否呼叫 `AddBeeFramework`。
- **Bee.Web.Blazor.Wasm 嚴禁相依任何後端組件**（Repository / Business / Hosting 等）：Browser 執行環境無法載入後端組件，此約束由相依鏈強制（`Bee.Api.Client → Bee.Api.Core → Bee.Api.Contracts/Definition` 全為純資料/協定層，無 server-only 程式碼）。
