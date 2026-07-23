---
name: bee-sample-add
description: 為 bee-library 加一個新的 samples/ 專案，內含前端類型選擇（Console / Blazor Server / Blazor Wasm / MAUI / WinForms）、後端配對決策樹（QuickStart.Server / Blazor.Wasm.Demo.Host / in-process Local）、auth 需求判斷、`Bee.Samples.slnx` 整合、README 樣板與 ProjectReference 預設值。當使用者要「新增一個 sample」、「加一個 demo」、「為某個 src 套件做 demo」之類需求時使用。
---

# bee-library 新增 sample

新增一個 `samples/<Sample.Name>/` demo 涉及 5 個關聯決定：前端類型、後端、auth 模式、共用 Define 引用、slnx folder。每個 decision 都有「正確答案表」可查；本 skill 把這張表寫死，避免每次重新探勘。

## 適用場景

- 為某個 src 套件做新的 demo（例：`Bee.WinForms` 出來時做 `WinForms.Demo`）
- 為某個情境做 demo（例：「Wasm + remote API 走 OAuth」、「Console 跑批次匯入」）
- 既有 sample 太單薄，想新拆出獨立 demo（例：把 `Blazor.Server.Demo` 的進階功能拆 `Blazor.Server.Advanced.Demo`）

## 不適用

- 改既有 sample 的小功能 — 直接編輯，不需 scaffold
- 純 library code 沒 UI / 沒入口的展示 — 寫文件 / ADR 比較合適
- 不在 samples 目錄、要正式 ship 的應用 — 走 `src/` 與 NuGet 流程，不在本 skill 範圍

## 與相關 skill 的分工

| Skill | 處理什麼 |
|-------|---------|
| **`maui-app-scaffold`**（global） | 純 MAUI 骨架（csproj / Platforms / Resources） |
| **`bee-sample-add`**（本 skill） | sample 的 Bee.NET 整合：選哪個後端、auth、slnx、README、相依設定 |
| **`demo-smoke`**（global） | scaffold 完之後驗證 demo 跑得通 |
| **`changelog-draft`** | 發版時把 sample 改動列進 CHANGELOG |

MAUI sample 的流程：先呼叫 `maui-app-scaffold` 起骨架 → 再呼叫本 skill 把 Bee 後端配進去。

## 5 個必問決定

### Decision 1：前端類型

```
1. Console        → 一個 Bee.Api.Client 消費端示範（Ping / Login / 呼叫 BO）
2. Blazor Server  → in-process Local provider，server-rendered，最快上手
3. Blazor Wasm    → 走 remote API，需要搭配一個 Host
4. MAUI           → 走 remote API，需要 maui-app-scaffold 先起骨架
5. WinForms       → Local 或 Remote 都可（Bee.UI.WinForms 還沒落地時略過）
```

用 `AskUserQuestion` 選一個。

### Decision 2：後端配對

依前端決定 + auth 需求：

| 前端 | auth=否（純 Public BO） | auth=是（FormBO 等需登入） |
|------|------------------------|-----------------------------|
| Console | 連 `QuickStart.Server` (5050)，呼叫 Echo BO | 連 `QuickStart.Server` (5050)，用 `demo/demo` 登入 |
| Blazor Server | in-process（`builder.AddBeeBackend()`） | in-process + `DemoAuthenticatingSystemBusinessObject`（已含於 `DemoBackend`） |
| Blazor Wasm | 需配 Host：`Blazor.Wasm.Demo.Host` (5060) | 同左，Host 已掛 `DemoBackend` |
| MAUI | 連 `QuickStart.Server` (5050) | 連 `QuickStart.Server` (5050)，`demo/demo` |
| WinForms | Local 或 Remote 都可 | 同上 |

**關鍵知識**：
- `QuickStart.Server` 目前已掛 `Bee.Samples.Shared.DemoBackend`，**有** `demo/demo` 登入 + Employee schema 種子。Console 之外的 sample 想用 auth 都連這台
- `Blazor.Wasm.Demo.Host` 同樣掛 `DemoBackend`，與 QuickStart.Server 內容對等；Wasm demo 連這台是為了「同 process 也跑 Blazor host」
- in-process 模式只給 Blazor Server 用；MAUI / Wasm 不能 in-process（沒有 `WebApplicationBuilder`）

### Decision 3：要登入嗎？

問使用者 sample 要展示什麼 BO 動作：

| 動作類別 | 範例 BO method | auth? |
|---------|---------------|-------|
| `system.ping`（測連線） | `SystemApiConnector.PingAsync` | 否（Plain） |
| `Echo` 自訂 BO | `EchoBusinessObject.Echo` | 否（Public / Anonymous） |
| `GetDefine` 讀 schema | `SystemApiConnector.GetDefineAsync` | **是**（Encrypted / Authenticated） |
| FormBO CRUD（`GetList` / `GetData` / `Save` / `Delete`） | `FormApiConnector.*` | **是**（Encrypted / Authenticated） |
| 自訂 ExecFunc | `Bee.Business` 的客製 method | 看 attribute 標示 |

任一動作含 `Authenticated` → 後端必須掛 `DemoBackend`（給 demo/demo），demo 程式碼必須含 Login 步驟。

### Decision 4：共用 Define 引用

| 需要的 Define | 引用方式 |
|-------------|---------|
| Employee FormSchema（既有） | 後端讀 `samples/Define/FormSchema/Employee.FormSchema.xml`（已被 `DemoBackend.ResolveDefinePath()` 處理） |
| 自訂 FormSchema | 加 XML 到 `samples/Define/FormSchema/<ProgId>.FormSchema.xml`、TableSchema 加 `samples/Define/TableSchema/common/`、`DemoSchemaSeeder` 補對應種子（編輯 `Bee.Samples.Shared`） |
| 完全不需 schema | 純 Echo / Ping demo，無 Define 相依 |

加新 FormSchema 時，順手把 `Bee.Samples.slnx` 的 `/Define/` folder 內檔案列表更新（不是必要，但 IDE 樹乾淨些）。

### Decision 5：放 slnx 哪個 folder

`samples/Bee.Samples.slnx` 既有 folder：

```
/QuickStart/        → 入門級 demo（純 API client / 純 server）
/Blazor/            → Blazor 家族 + Bee.Samples.Shared
/Maui/              → MAUI 家族
```

新 sample 對應放：
- Console / API host 類 → `/QuickStart/`
- Blazor 任一家 → `/Blazor/`
- MAUI 任一家 → `/Maui/`
- 其他（WinForms / WPF 等）→ 開新 folder `/<Family>/`

## 執行流程

### Step 1：跑 5 問

用 `AskUserQuestion` 連續問 5 個 decision（或合併為 2-3 個多選題）。

### Step 2：sanity check 既有 sample 是否已涵蓋

```bash
ls samples/
grep -r "ProgId.*=.*\"<NewSample>\"" samples/ 2>/dev/null
```

若同類已存在（例 sample 想做「Blazor Wasm 連 QuickStart.Server」但已有 `Blazor.Wasm.Demo`） → 停下來，問使用者是要新增還是擴充既有。

### Step 3：起骨架

**MAUI sample**：先呼叫 `maui-app-scaffold` skill，再回到本流程加 Bee 整合。
**其他**：直接寫 csproj + 程式碼，照下面樣板：

#### `samples/<Sample.Name>/<Sample.Name>.csproj` 樣板

**Console**：
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{Sample.Name}</RootNamespace>
    <AssemblyName>{Sample.Name}</AssemblyName>
    <IsPackable>false</IsPackable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Bee.Api.Client\Bee.Api.Client.csproj" />
  </ItemGroup>
</Project>
```

**Blazor Server**：
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{Sample.Name}</RootNamespace>
    <AssemblyName>{Sample.Name}</AssemblyName>
    <IsPackable>false</IsPackable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Bee.Web.Blazor.Server\Bee.Web.Blazor.Server.csproj" />
    <ProjectReference Include="..\Bee.Samples.Shared\Bee.Samples.Shared.csproj" />
  </ItemGroup>
</Project>
```

**Blazor Wasm**（client，要搭 Host）：
```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{Sample.Name}</RootNamespace>
    <AssemblyName>{Sample.Name}</AssemblyName>
    <IsPackable>false</IsPackable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Bee.Web.Blazor.Wasm\Bee.Web.Blazor.Wasm.csproj" />
  </ItemGroup>
</Project>
```

**MAUI**（在 `maui-app-scaffold` 完成的 csproj 後追加）：
```xml
  <ItemGroup>
    <ProjectReference Include="..\..\src\Bee.UI.Maui\Bee.UI.Maui.csproj" />
    <!-- 若要直接 reference DemoCredentials 常數，無 ProjectReference 也可（自行 hardcode "demo"） -->
  </ItemGroup>
```

> ⚠️ MAUI sample **不能** ProjectReference `Bee.Samples.Shared`，因為後者有 `FrameworkReference Microsoft.AspNetCore.App`。直接在 MAUI sample 內 hardcode demo credentials（`demo`/`demo`），或日後把 credentials 拆獨立小 project。

### Step 4：Program.cs / MauiProgram.cs 樣板

#### Console 樣板（auth=否）

參考 `samples/QuickStart.Console/Program.cs`：直接 `new SystemApiConnector(endpoint, Guid.Empty)` + `PingAsync()` + `Echo.Echo(...)`。

#### Console 樣板（auth=是）

```csharp
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.UI.Core;

namespace {Sample.Name};

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        ApiClientInfo.ApiKey = "{sample-key}";
        ClientInfo.Initialize("http://localhost:5050/api"); // QuickStart.Server

        var login = await ClientInfo.SystemApiConnector.LoginAsync("demo", "demo");
        ClientInfo.ApplyLoginResult(login);

        // ... do authenticated work via ClientInfo.SystemApiConnector / CreateFormApiConnector
        return 0;
    }
}
```

#### Blazor Server 樣板

```csharp
using Bee.Samples.Shared;
using Bee.Web.Blazor.Server.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.AddBeeBackend();
builder.Services.AddBeeBlazor(options => options.UseLocalProvider());
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
var app = builder.Build();
app.UseBeeBackend();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
```

#### MAUI 樣板（auth=是，連 QuickStart.Server）

`MauiProgram.CreateMauiApp` 內：

```csharp
ApiClientInfo.ApiKey = "{sample-key}";
ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;

// Sandboxed app 寫不進 .app bundle → 用 Preferences-backed storage
ClientInfo.EndpointStorage = new Bee.UI.Maui.Storage.MauiPreferenceEndpointStorage();
```

UI 三頁參考 `samples/Maui.Demo/Pages/`（Connection → Login → Employee FormPage）。

### Step 5：slnx 整合

編輯 `samples/Bee.Samples.slnx`，在對應 folder 加：

```xml
<Folder Name="/{Family}/">
  ...
  <Project Path="{Sample.Name}/{Sample.Name}.csproj" />
</Folder>
```

### Step 6：README

依下面樣板寫 `samples/<Sample.Name>/README.md`：

```markdown
# {Sample.DisplayName}

{一句話說 sample 想證明什麼，例：「同一份 FormSchema 在 MAUI 端渲染」、「Console 透過 JSON-RPC 呼叫遠端 BO」}

## 前置條件

{Console / Blazor / MAUI 各自的環境需求，例 maui workload、Xcode、SQL container 等}

## 跑起來

{逐步指令；後端需另起時，先列「另一個 terminal 跑 X」}

## 預期畫面 / 輸出

{對應每步看到什麼；包含 demo/demo 帳密提示（若需要）}

## 對應 library 元件

| Demo 行為 | library 元件 |
|-----------|--------------|
| {step 1} | [src/Bee.X/Y.cs](../../src/Bee.X/Y.cs) |
| ... | ... |

## 與其他 sample 的關係

{若與既有 sample 重疊或共用 host，這裡明確標出}

## 不做的事

{刻意排除的範圍，避免被誤解為 missing feature}
```

### Step 7：build + 跑一次

```bash
dotnet build samples/{Sample.Name}/{Sample.Name}.csproj --configuration Debug
```

MAUI sample 加 `-f net10.0-maccatalyst`。Build 失敗就停下修；成功就**不**自動 `dotnet run`，讓使用者自己跑（避免綁定 background process 在 session 內）。

### Step 8：commit 建議

skill 完成後輸出建議的 commit message，**不** 自動 commit：

```
feat(samples): 新增 {Sample.Name} —— {一句話描述}

- 引用 Bee.X / Bee.Y
- 對應 plan-samples-structure.md 第 P? 階段（如有對應）
- 後端：{QuickStart.Server / Blazor.Wasm.Demo.Host / in-process}
- auth：{demo/demo / 匿名}
```

## 知道的雷

- **MAUI sample 不能 reference Bee.Samples.Shared**（後者有 AspNetCore framework reference）— 共用常數要另想辦法
- **DemoBackend 連環依賴**：QuickStart.Server 與 Blazor.Wasm.Demo.Host 兩個 host 共享 `DemoBackend`，改 DemoBackend 會同時影響兩台
- **`Bee.Samples.slnx` 與 `Bee.Library.slnx` 是分開的 solution**：sample 不掛主 solution，CI 不跑 sample build，本機要驗證 sample 改動須手動跑
- **`samples/Define/Master.key` 與 `samples/**/quickstart.db` 都 gitignored**：第一次跑會自動產生，不要 commit 進來
- **Echo BO 是 anonymous Public** — 想加新 anonymous BO 仿 `EchoBusinessObject` + 註冊 `QuickStartFormBoTypeResolver`；想加 authenticated BO 走 `samples/Bee.Samples.Shared` 的 DemoBackend 路徑

## 不在本 skill 範圍

- MAUI 骨架本身（用 `maui-app-scaffold`）
- 真實 BO 實作（用 `bee-add-bo-method`）
- 跑 demo 的 UI 驗證（用 `demo-smoke`）
- 改主 README 加 Quick Start 連結（人工編輯，需 review）
- 把 sample 改動列進 CHANGELOG（用 `changelog-draft`）
