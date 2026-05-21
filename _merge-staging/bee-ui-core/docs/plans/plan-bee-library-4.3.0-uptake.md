# 計畫：因應 bee-library 4.3.0 升級 bee-ui-core

**狀態：✅ 已完成（2026-05-13）**

> 待確認事項已確認：
> - 待確認 1：採選項 A（升 bee-ui-core 自身至 4.3.0）
> - 待確認 2：採選項 A（不加 facade helper）
> - 待確認 3：採選項 加（雙語 README 新增近端連線 + DI 啟動範例）
> - **2026-05-13 追加**：發現 `BackendInfo` 在 4.3.0 已從 client surface 移除，build 失敗。經確認擴大任務範圍至「移除 ClientInfo 對 BackendInfo 的相依」（見「補充：BackendInfo 相依清除」段）。
>
> 完成驗證：`dotnet build --configuration Release` 0 warning 0 error，`Bee.UI.Core.4.3.0.nupkg` 已成功 pack。

## 背景

bee-library 4.3.0 主要變更：

1. 新增 `Bee.Hosting` 套件 —— DI composition root，不依賴 ASP.NET Core
2. `AddBeeFramework` 從 `Bee.Api.AspNetCore` 命名空間搬至 `Bee.Hosting`
3. `Bee.Api.AspNetCore` 透過遞移 reference `Bee.Hosting`；web host 仍只需 ref 一個套件，新增 `using Bee.Hosting;` 即可

詳細 changelog：<https://github.com/jeff377/bee-library/blob/v4.3.0/CHANGELOG.md>
完整 plan：<https://github.com/jeff377/bee-library/blob/v4.3.0/docs/plans/plan-extract-bee-hosting.md>

## 架構約束（不可違反）

- bee-ui-core 只 ref `Bee.Api.Client`，**不直接 ref** 任何後端組件（含新的 `Bee.Hosting`）
- 「近端連線」場景由**宿主應用程式**負責 `AddBeeFramework(...)` 並把 `IServiceProvider` 注入 `ApiClientInfo.LocalServiceProvider`
- `LocalApiProvider` 透過反射取得 `JsonRpcExecutor` —— 既有反射契約**不動**

## Explore 結論

| 位置 | 現況 | 是否需改動 |
|---|---|---|
| `src/Bee.UI.Core/Bee.UI.Core.csproj` | `Bee.Api.Client` 4.1.0 | ✅ 升 4.3.0 |
| `src/Directory.Build.props` | `<Version>` / `<AssemblyVersion>` / `<FileVersion>` = `4.1.0` | ⚠️ 視發佈節奏決定（見「待確認 1」） |
| `README.md` / `README.zh-TW.md` 相依表 | `Bee.Api.Client` 版本 `4.1.0` | ✅ 同步升 4.3.0 |
| README 是否含 `AddBeeFramework` 範例 | 不含 | — 無遷移工作 |
| 程式碼是否 ref `Bee.Api.AspNetCore` / `Bee.Hosting` | grep 不到 | — 無遷移工作 |
| `samples/`、`tests/` | 不存在 | — |
| 便利 helper（注入 `ApiClientInfo.LocalServiceProvider`） | 不存在 | ⚠️ 待決（見「待確認 2」） |

## 影響檔案清單（確定要改的）

1. `src/Bee.UI.Core/Bee.UI.Core.csproj` —— `Bee.Api.Client` `4.1.0` → `4.3.0`
2. `README.md` —— 相依表 `Bee.Api.Client` 欄位版本
3. `README.zh-TW.md` —— 相依表 `Bee.Api.Client` 欄位版本

## 待確認事項

### 待確認 1：是否同步升 bee-ui-core 自身版號至 4.3.0？

- **選項 A（推薦）**：升至 `4.3.0`，與 bee-library 主線版號對齊。發佈節奏：本次 commit 升 csproj + Directory.Build.props 至 4.3.0，CI 通過後 `git tag v4.3.0` 觸發 `nuget-publish.yml`。
- **選項 B**：只升 `Bee.Api.Client` 相依版號，bee-ui-core 自身留在 `4.1.0`；下一個版本（如 `4.1.1`）再發佈。
- **選項 C**：升 `Bee.Api.Client` + bee-ui-core 自身版號至 `4.1.1`（不對齊主線，只當 patch）。

> **預設取 A**，符合 bee-ui-core 與 bee-library 隨主線同步升版的歷史慣例（4.1.0 對 4.1.0）。

### 待確認 2：是否新增「近端服務注入」便利 helper？

bee-library 4.3.0 之後，host（WinForms / Console 等）使用近端連線的啟動序列大致如下：

```csharp
// 宿主端（WinForms）
var services = new ServiceCollection();
services.AddBeeFramework(opts => { /* ... */ });   // 來自 Bee.Hosting
var sp = services.BuildServiceProvider();

ApiClientInfo.LocalServiceProvider = sp;            // ← 這一行
ClientInfo.Initialize(uiViewService, SupportedConnectTypes.Local);
```

最後一段「`ApiClientInfo.LocalServiceProvider = sp;`」屬於 `Bee.Api.Client` 的公開 API，host 直接寫沒問題；但若想避免 host 端記住「要設這個屬性」，可在 bee-ui-core 提供 1-line passthrough：

```csharp
// 在 ClientInfo.cs 新增
public static void ConfigureLocalServices(IServiceProvider serviceProvider)
{
    ArgumentNullException.ThrowIfNull(serviceProvider);
    ApiClientInfo.LocalServiceProvider = serviceProvider;
}
```

- **選項 A（推薦不加）**：純 facade、1-caller 即可的 passthrough，依 code-style「消除純 facade」原則直接由 host 設定 `ApiClientInfo.LocalServiceProvider`。
- **選項 B（加）**：若希望 README 啟動範例能在「bee-ui-core 命名空間之內」一條 API 完成，提供 `ClientInfo.ConfigureLocalServices(IServiceProvider)`。
- **選項 C（延後）**：先不加，等實際宿主應用（bee-jsonrpc-sample 或 client app）拉進來後再決定。

> **預設取 A**，依循 `~/.claude/rules/code-style.md` Path C 細部原則「消除純 facade —— 1-line delegation wrapper 不保留」。

### 待確認 3：是否同時補一段「近端連線啟動」README 段落？

目前 README 的近端連線只描述用「local file path」當 endpoint，沒有展示 host 端 DI 註冊步驟。若要為 4.3.0 補充：

- 加在 README 的 `🚀 Quick Start` 與 `🔧 Customization` 之間，標題類似 `### 5. (Optional) Local connection: register backend services`
- 雙語同步（README.md + README.zh-TW.md）
- 範例需明確說明：`Bee.Hosting` 是**宿主**的相依、不是 bee-ui-core 的

> **預設取 加**，能讓升級到 4.3.0 的使用者有方向；若選 [待確認 1 = B]，則延後到下個發版。

## 變更摘要（依「待確認」皆採預設值的情況）

1. `src/Bee.UI.Core/Bee.UI.Core.csproj`：`Bee.Api.Client` `4.1.0` → `4.3.0`
2. `src/Directory.Build.props`：`<Version>` / `<AssemblyVersion>` / `<FileVersion>` `4.1.0` → `4.3.0`
3. `README.md`：
   - 安裝段 `<PackageReference Include="Bee.UI.Core" Version="4.1.0" />` → `4.3.0`
   - 相依表 `Bee.Api.Client` 版本 `4.1.0` → `4.3.0`
   - 新增「Local connection: register backend services」段落（英文）
4. `README.zh-TW.md`：
   - 安裝段 `4.1.0` → `4.3.0`
   - 相依表 `4.1.0` → `4.3.0`
   - 新增「近端連線：註冊後端服務」段落（繁中）

## 驗收標準

- [ ] `dotnet restore Bee.UI.Core.slnx` 成功
- [ ] `dotnet build src/Bee.UI.Core/Bee.UI.Core.csproj --configuration Release --no-restore` 成功（在 `TreatWarningsAsErrors=true` 下）
- [ ] `dotnet pack src/Bee.UI.Core/Bee.UI.Core.csproj --configuration Release --output ./nupkgs --no-build` 成功（驗證 metadata 正確、版號為 4.3.0）
- [ ] bee-ui-core csproj **不出現** `Bee.Hosting` / `Bee.Api.AspNetCore` 任何 PackageReference
- [ ] 程式碼層無新增 `using Bee.Hosting;` / `using Bee.Api.AspNetCore;`（grep 應仍為 0 命中）
- [ ] README 兩語版本內容對應，版號一致
- [ ] CI `build-ci.yml` 通過

## 執行流程（plan 通過後）

1. 依「待確認」3 項回覆套用設定
2. 改 csproj + Directory.Build.props + README 雙語
3. 本機 `dotnet build --configuration Release` 驗證
4. 本機 `dotnet pack` 驗證
5. 依 `~/.claude/rules/pull-request.md`：本機可驗證環境（macOS）→ 預設**直接 commit 到 main + push**；遠端 CI 監看
6. （若 [待確認 1] 採 A）push CI 通過後，使用者主導觸發 `git tag v4.3.0 && git push origin v4.3.0` 發佈

## 補充：BackendInfo 相依清除（2026-05-13 追加）

### 發現

升 `Bee.Api.Client` 4.1.0 → 4.3.0 後本機 build 失敗：

```
error CS0103: 名稱 'BackendInfo' 不存在於目前的內容中
  ClientInfo.cs(147,17)   BackendInfo.DefinePath = endpoint;
  ClientInfo.cs(153,17)   BackendInfo.DefinePath = string.Empty;
  ClientInfo.cs(273,13)   BackendInfo.Initialize(settings.BackendConfiguration);
  ClientInfo.cs(275,13)   BackendInfo.DefineAccess.GetSystemSettings();
```

對照 dll 內公開符號：

- 4.1.0 `BackendInfo` 出現在 `Bee.Definition.dll` / `Bee.Api.Core.dll` / `Bee.Db.dll` / `Bee.Business.dll`
- 4.3.0 完全從 client-surface 套件移除（搬入 server-side `Bee.Hosting` 範疇）
- 4.3.0 `ApiClientInfo` 新增 `LocalServiceProvider` 屬性，配合 host 端 `AddBeeFramework` 完成 backend 初始化

### 架構意涵

`ClientInfo.cs` 對 `BackendInfo` 的 4 處呼叫，本質上就是「在 client 端操作 server-side 狀態」—— 違反「UI 層不碰後端」分層。4.1.0 沒被擋是因為 `BackendInfo` 在 client surface 中暴露；4.3.0 把它從 client surface 移走，分層改成強制執行。

新分層下，**近端模式 backend 初始化全部由 host 透過 `AddBeeFramework` 完成**，`ClientInfo` 只負責：
- ConnectType / Endpoint / AccessToken / UserInfo 狀態管理
- `SystemApiConnector` 與 `IDefineAccess` 快取
- 透過 `ApiClientInfo.LocalServiceProvider` 由 `Bee.Api.Client` 內部反射拿到 JsonRpcExecutor（**bee-ui-core 不直接觸碰**）

### ClientInfo.cs 改動細節

| 位置 | 原內容 | 新內容 |
|---|---|---|
| `SetConnectType` (L147) | `BackendInfo.DefinePath = endpoint;` | **刪除** |
| `SetConnectType` (L153) | `BackendInfo.DefinePath = string.Empty;` | **刪除** |
| `InitializeLocalConnect` 整段 (L268–276) | 含 `BackendInfo.Initialize` + `BackendInfo.DefineAccess.GetSystemSettings()` | **整段刪除** |
| `Initialize(IUIViewService, ...)` (L214–217) | `if (Local) InitializeLocalConnect();` | **刪除整段 if** |
| `Initialize(string endpoint)` (L228–231) | 同上 | **刪除整段 if** |
| using directives | `Bee.Definition`、`Bee.Definition.Settings`、`Bee.Api.Core.Messages.System` 等 | 視實際引用情況清除無用 using（依 `TreatWarningsAsErrors=true` 必須處理 IDE0005） |

### 對外行為差異

**遠端模式**：完全無差異（原本就沒走 BackendInfo path）。

**近端模式**：
- **4.1.0**：`ClientInfo.Initialize(...)` 內部自動完成 `BackendInfo.DefinePath` 設定與 `BackendInfo.Initialize(BackendConfiguration)`
- **4.3.0**：host 必須在呼叫 `ClientInfo.Initialize(...)` **之前**：
  1. 建立 `IServiceCollection`，呼叫 `services.AddBeeFramework(options => { options.DefinePath = "..."; ...})`
  2. `BuildServiceProvider()` 並設 `ApiClientInfo.LocalServiceProvider = sp;`

這個 host 端責任在 README 雙語的「5.（選用）近端連線：註冊後端服務」段落已說明。

### 風險與相容性

- **API surface 不變**：`ClientInfo.Initialize(...)`、`SetEndpoint(...)`、`SystemApiConnector` 等公開簽章全部保留
- **行為變更**：近端模式下，host 端**必須**先注入 `LocalServiceProvider`；4.1.0 host 端可不做此步、由 ClientInfo 自動初始化 backend
- **這是 4.3.0 升版的必要 breaking change**（bee-library 已強制執行）
- **無單元測試覆蓋**（repo 無 tests/），靠 compile + 公開 surface diff 驗證
- 下游 sample / client app（如 bee-jsonrpc-sample）若使用近端模式需同步調整啟動序列 —— **不在本任務範圍**

### 驗收標準（追加）

- [ ] `dotnet build` 在 `TreatWarningsAsErrors=true` 下 0 error 0 warning
- [ ] `ClientInfo.cs` grep `BackendInfo` 為 0 命中
- [ ] `ClientInfo.cs` 不出現 `using Bee.Hosting`、`using Bee.Api.AspNetCore`、`using Microsoft.Extensions.DependencyInjection` 等不該有的 using
- [ ] 公開 API 簽章未變（`Initialize` / `SetEndpoint` / `SystemApiConnector` 等）

## 不在範圍內

- `LocalApiProvider` 反射機制不動
- 不引入 `Bee.Hosting` 直接相依
- 不新增 `samples/` 或 `tests/`（既有 repo 都沒有，不在本次任務擴大範圍）
- bee-jsonrpc-sample 等下游 repo 的對應升級不在本次任務範圍
- 不重構 `ClientInfo` 為非 static（即使 DI-friendly 設計更現代化，超出本次任務範疇）
