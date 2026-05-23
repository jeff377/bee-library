# 網站前端整合 JSON-RPC 後端：Blazor Server 元件庫方案

**狀態：✅ 已完成（2026-05-23）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1a | `DynamicForm` + 基底 `FormDataObject`（in-memory 雙向 binding） | ✅ 已完成 |
| 1b | `FormDataObject` 接 BO：`LoadAsync` / `SaveAsync` / `DeleteAsync` / `NewAsync` | ✅ 已完成（2026-05-23） |
| 1c | `DynamicGrid` + `AddBeeBlazor` DI + `FormPage` 整合頁 | ✅ 已完成（2026-05-23） |
| 1d | 宿主 Login + `CascadingValue<Guid>` AccessToken 範例 | ✅ 已完成（2026-05-23） |
| 2 | `samples/Blazor.Server.Demo` + `Blazor.Wasm.Demo` + `.Host`（對應 plan-samples-structure P1） | ✅ 已完成（2026-05-23） |

## 背景與需求

Bee.NET 目前是純後端 JSON-RPC 框架，無前端實作。
需求：企業內部 CRUD 管理介面，C# 優先，快速開發。
目標：製作**可複用的獨立 Blazor 元件庫**，供宿主 ASP.NET Core 網站直接引用。

### 前端生態全貌

```
bee-library          ← 後端 JSON-RPC 框架 + Web 前端元件（本 repo）
                       提供 LocalApiProvider / RemoteApiProvider / JsonRpcExecutor
                       同時包含 Blazor Web 元件庫（FormLayout 的天然消費者）
  ├─ Bee.Web.Blazor.Server   固定接 LocalApiProvider（與後端同進程）
  └─ Bee.Web.Blazor.Wasm     固定接 RemoteApiProvider（Browser → HTTP → 後端）

bee.ui.core          ← Desktop/MAUI/Windows 前端（獨立 repo）
                       提供 Local ↔ Remote 連線切換進入點，僅供桌面端使用
```

**決策：先設計介面，再同時實作 Blazor Server 與 WASM**

1. 先將 `IFormDataObject`、元件參數、事件簽名等介面在計畫中穩定下來
2. 介面確定後兩個專案同時實作（AI 並行處理，成本低）
3. 若設計需調整，兩邊同步修改

> 主要風險不在實作速度，而在設計不確定性的傳播——介面穩定後此風險消除。

**實作前置條件：先調整後端 BO 對接方法**

DataObject 開發前，需先完成後端 BO 對接方式的調整，確保 Connector 的呼叫介面穩定，
DataObject 才能以正確的方式介接。

**決策：Blazor 元件庫放入 `bee-library`**，理由：
- `FormLayout` 目前在 repo 內無消費者，Blazor 元件是它的天然消費者，放同一 repo 讓設計閉合
- `FormSchema`/`FormLayout` 異動時可在同一 PR 同步更新 UI，無需跨 repo 協調版本
- Blazor 元件庫為純 C#/Razor，無 npm/webpack 等前端建置工具，與現有 .NET 建置流程相容
- 等 `FormLayout` API 穩定後，再評估是否拆出獨立 repo

三者連接 API 的方式本質不同：

| | bee.ui.core | Blazor Server | Blazor WASM |
|--|--|--|--|
| Provider | 可切換（Local/Remote） | 固定 LocalApiProvider | 固定 RemoteApiProvider |
| 執行環境 | Desktop/MAUI/Windows | ASP.NET Core Server | Browser（.NET WASM） |
| 連線決策時機 | 執行期動態切換 | 部署架構決定 | 天生只能 Remote |

Blazor Web 元件庫**不放入 `bee.ui.core`**，原因：
- `bee.ui.core` 定位為桌面端，混入 Web 元件模糊定位
- Blazor 的 Provider 由部署架構固定，不需要切換機制
- 建置、測試、發布流程與桌面端不同

---

## 推薦方案：Blazor Server

### 架構圖

```
宿主 ASP.NET Core 網站
  ├─ 引用 Bee.Web.Blazor（元件庫）
  ├─ Program.cs 設定 DI
  └─ Pages/*.razor 使用元件

Bee.Web.Blazor（元件庫）
  ├─ DynamicForm.razor       ← FormLayout 驅動動態表單
  ├─ DynamicGrid.razor       ← FormLayout 驅動動態列表
  └─ FormPage.razor          ← 整合頁（列表 + 明細）

      ↕ code-behind（MVVM）
  FormDataObject（暫存 DataSet，介接 UI 與 Connector）

      ↕
  FormApiConnector / SystemApiConnector
      ↕
  LocalApiProvider（進程內，無 HTTP）
      ↕
  JsonRpcExecutor → Business Objects（BO）
```

### 為何選 Blazor Server（而非 WASM 或 SPA）

| 考量 | Blazor Server | Blazor WASM | SPA（JS） |
|------|--------------|------------|---------|
| 複用 LocalApiProvider | ✅ 直接 | ❌（需 HTTP） | ❌（需 HTTP） |
| 無 HTTP round-trip | ✅ | ❌ | ❌ |
| FormSchema 驅動 UI | ✅ C# 直接解析 | ✅ | 需 JS 重實作 |
| 開發複雜度 | 低 | 中 | 高 |
| 企業內網並發 | 足夠 | 足夠 | 足夠 |

---

## 專案定位

Blazor Server 與 Blazor WASM 是前端的**不同分支**，分拆為兩個獨立元件庫（RCL NuGet 套件）：

| 套件 | Hosting 模式 | Provider | 宿主類型 |
|------|------------|---------|---------|
| `Bee.Web.Blazor.Server` | Blazor Server | `LocalApiProvider`（進程內） | ASP.NET Core Server |
| `Bee.Web.Blazor.Wasm` | Blazor WASM | `RemoteApiProvider`（HTTP） | Browser（.NET WASM） |

```
src/
├─ Bee.Api.Client/               ← 現有（Connector、Provider）
├─ Bee.Definition/               ← 現有（FormSchema、FormLayout 定義）
├─ Bee.Web.Blazor.Server/        ← 新增：Blazor Server 元件庫
│   ├─ Bee.Web.Blazor.Server.csproj
│   ├─ Components/
│   │   ├─ DynamicForm.razor / .razor.cs
│   │   ├─ DynamicGrid.razor / .razor.cs
│   │   └─ FormPage.razor / .razor.cs
│   └─ DataObjects/
│       └─ FormDataObject.cs
└─ Bee.Web.Blazor.Wasm/          ← 新增：Blazor WASM 元件庫
    ├─ Bee.Web.Blazor.Wasm.csproj
    ├─ Components/
    │   ├─ DynamicForm.razor / .razor.cs
    │   ├─ DynamicGrid.razor / .razor.cs
    │   └─ FormPage.razor / .razor.cs
    └─ DataObjects/
        └─ FormDataObject.cs

samples/
└─ BlazorHostApp/                ← 新增：驗證用宿主範例（Server 或 WASM）
```

> **決策：兩套元件庫各自獨立維護，不共用 UI 元件。**
>
> Blazor WASM 本質上是跑在 Browser 的執行檔（.NET WASM runtime），
> 與 Blazor Server（元件邏輯跑在 Server，瀏覽器只負責渲染 diff）執行環境根本不同：
> - 執行緒模型不同（WASM 單執行緒 / Server 多執行緒）
> - JS Interop 行為不同（WASM 同進程即時 / Server 需 SignalR round-trip）
> - 可用服務不同（WASM 無 `IHttpContextAccessor` 等 Server 端服務）
>
> 帶有事件處理的 `.razor` 元件在兩個環境的行為差異足以讓共用產生隱性問題，
> 各自獨立可讓兩個分支依各自環境特性最佳化，不互相牽制。

---

## 相依矩陣

兩個 RCL 一律採**最小相依**策略：只引用 `Bee.Api.Client`，由宿主決定是否再呼叫 `AddBeeFramework` 註冊後端服務。

| 專案 | ProjectReference | 外部相依 | 目標框架 |
|------|------------------|---------|---------|
| `Bee.Web.Blazor.Server` | `Bee.Api.Client` | `Microsoft.AspNetCore.Components.Web` 等 Blazor Server 套件 | `net10.0` |
| `Bee.Web.Blazor.Wasm` | `Bee.Api.Client` | `Microsoft.AspNetCore.Components.WebAssembly` 等 WASM 套件 | `net10.0`（需 `wasm-tools` workload） |

### 硬性約束

- **`Bee.Web.Blazor.Wasm` 嚴禁相依任何後端組件**（`Bee.Business`、`Bee.Repository`、`Bee.Hosting`、`Bee.Db` 等）
  - Browser 執行環境無法載入後端組件
  - 約束已由相依鏈強制：`Bee.Api.Client → Bee.Api.Core → Bee.Api.Contracts/Definition → Bee.Base` 全為純資料/協定層，無 server-only 程式碼
- **`Bee.Web.Blazor.Server` 雖物理上可相依後端組件，但仍維持只相依 `Bee.Api.Client`**
  - 與 Wasm 對稱，元件庫程式碼可在兩專案間複製/重構不受相依限制
  - composition 決定權交還宿主（由宿主自行 `AddBeeFramework`）

### DI 註冊分工

```csharp
// 宿主程式：分兩步註冊
builder.Services.AddBeeFramework();              // ← 後端服務（僅 Blazor Server 宿主需要）
builder.Services.AddBeeBlazor(o => o.UseLocal()); // ← Blazor 元件用 services（含 IApiProvider 解析策略）
```

`AddBeeBlazor` **不** bundle `AddBeeFramework`，理由：
- 維持 `Bee.Hosting` 為唯一 composition root 的原則
- WASM 宿主不可能呼叫 `AddBeeFramework`（Browser 跑不起來），若 bundle 進去會破壞對稱性
- 宿主可選擇進階配置（如 `AddBeeFramework` 後再覆寫某些服務），bundle 會剝奪這個彈性

---

## MVVM 設計

### 模式選擇：code-behind（方式 1）

每個元件的 `.razor.cs` 即為 ViewModel，DataObject 負責：
1. 暫存從 Connector 取得的 `DataSet`（Master DataRow + Detail DataTable）
2. 作為 UI 與 Connector 的中介
3. 提供 UI 雙向 binding 的存取介面

### FormDataObject

```csharp
// DataObjects/FormDataObject.cs
public class FormDataObject
{
    private readonly IFormApiConnector _connector;

    public DataSet DataSet { get; private set; } = new();
    public DataRow? MasterRow => DataSet.Master.Rows.Count > 0
        ? DataSet.Master.Rows[0] : null;
    public DataTable DetailTable => DataSet.Detail;

    public bool IsLoading { get; private set; }
    public bool IsDirty { get; private set; }

    public string GetField(string column)
        => MasterRow?[column]?.ToString() ?? "";

    public void SetField(string column, string value)
    {
        if (MasterRow is null) return;
        MasterRow[column] = value;
        IsDirty = true;
    }

    public async Task LoadAsync(object queryArgs) { ... }
    public async Task SaveAsync() { ... }
    public async Task DeleteAsync() { ... }
    public async Task NewAsync() { ... }
}
```

### 雙向 Binding 策略

**不使用強型別 DTO**，理由：
- FormLayout 在 runtime 才知道欄位清單，強型別是反模式
- FormSchema 新增欄位時，前端自動長出對應 input，零程式碼變更

**Binding 語法**：`@bind-Value` 無法用於 runtime 索引，改用原生 `<input>` 拆開 Value/onchange：

```razor
@* DynamicForm.razor — 依欄位型別 dispatch *@
@foreach (var field in _layout.Fields)
{
    switch (field.FieldType)
    {
        case FieldType.Text:
            <input type="text"
                   value="@DataObject.GetField(field.ColumnName)"
                   @onchange="e => DataObject.SetField(field.ColumnName, e.Value?.ToString())" />
            break;

        case FieldType.Date:
            <input type="date"
                   value="@DataObject.GetField(field.ColumnName)"
                   @onchange="e => DataObject.SetField(field.ColumnName, e.Value?.ToString())" />
            break;

        case FieldType.Boolean:
            <input type="checkbox"
                   checked="@(DataObject.GetField(field.ColumnName) == "True")"
                   @onchange="e => DataObject.SetField(field.ColumnName, e.Value?.ToString())" />
            break;

        case FieldType.Lookup:
            <select value="@DataObject.GetField(field.ColumnName)"
                    @onchange="e => DataObject.SetField(field.ColumnName, e.Value?.ToString())">
                @foreach (var opt in field.LookupItems)
                {
                    <option value="@opt.Value">@opt.Text</option>
                }
            </select>
            break;
    }
}
```

型別轉換集中在 `GetField` / `SetField`，其餘地方不散落轉型邏輯。

---

## 資料流

```
FormLayout（欄位清單、型別、標籤）
    ↓ 驅動
DynamicForm.razor（foreach field → 產生對應 input）
    ↓ 雙向 binding（Value / onchange）
FormDataObject.GetField / SetField
    ↓ 讀寫
DataSet.Master DataRow（以 ColumnName 為 key）
    ↓ 打包
FormApiConnector.ExecFuncAsync("Save", dataSet)
    ↓
LocalApiProvider → JsonRpcExecutor → BO → DB
```

---

## 認證整合

```
Login 頁
  → SystemApiConnector.LoginAsync(userId, password)
  → 取得 AccessToken（Guid）
  → 存入 Blazor CascadingParameter 或 AuthenticationStateProvider
  → 各元件透過 [CascadingParameter] 取得 AccessToken
  → FormApiConnector 使用此 Token 呼叫後端
```

---

## 宿主網站使用方式

```csharp
// Program.cs（宿主 ASP.NET Core）
builder.Services.AddBeeBlazor(options =>
{
    // LocalApiProvider（進程內，宿主本身就是 API 後端）
    options.UseLocalProvider();
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
```

```razor
@* 宿主頁面 — 直接使用元件，零 CRUD 邏輯 *@
<FormPage ProgId="Employee" />
```

---

## Hosting 模式選擇

元件庫程式碼（`FormDataObject`、`DynamicForm`、`DynamicGrid`）**不感知** Provider 實作，
差別僅在宿主的 DI 設定：

```csharp
// Blazor Server — 宿主本身就是 API 後端，進程內直呼
builder.Services.AddBeeBlazor(options => options.UseLocalProvider());

// Blazor WASM — 宿主與 API 後端分離部署，走 HTTP
builder.Services.AddBeeBlazor(options => options.UseRemoteProvider("https://api.example.com"));
```

| | Blazor Server | Blazor WASM |
|--|--|--|
| Provider | `LocalApiProvider`（進程內） | `RemoteApiProvider`（HTTP） |
| 加密金鑰 | Login 時由伺服端產生，回傳綁定 Session，持有於記憶體 | 同左，安全模型一致 |
| 部署 | 單一程序 | 前後端分離 |
| 首次載入 | 快 | 需下載 WASM runtime（數 MB） |
| 適用 | 企業內部、單一部署 | 前後端分離、CDN 托管前端 |

> 金鑰由伺服端動態產生並於 Login 回傳，僅存於前端記憶體，不持久化。
> 兩種模式均可安全使用 `Encrypted` level，無需降級至 `Encoded`。

---

## 備選方案（若未來需要）

| 情境 | 方案 |
|------|------|
| 前端需高互動性（如拖拉、即時） | Blazor Server + JS Interop |
| 需要 SEO | 改用 Blazor Static SSR |
| 對外公開 REST API | 加 BFF 層（D 方案，獨立處理） |

---

## 驗證方式

1. 建立 `samples/BlazorHostApp`（宿主範例）引用 `Bee.Web.Blazor`
2. `Program.cs` 呼叫 `AddBeeBlazor(UseLocalProvider)`
3. 頁面加入 `<FormPage ProgId="Employee" />`
4. 瀏覽器開啟，確認：
   - 依 FormLayout 動態渲染欄位
   - 欄位編輯後 DataRow 值同步更新
   - 點擊儲存後 DataSet 傳入 Connector，資料寫入 DB
   - 新增 FormSchema 欄位後，重啟即自動出現於 UI

---

## 關鍵檔案（現有，供實作參考）

| 檔案 | 用途 |
|------|------|
| `src/Bee.Api.Client/SystemApiConnector.cs` | Login、GetDefine、ExecFunc |
| `src/Bee.Api.Client/FormApiConnector.cs` | Form 級 ExecFunc，DataObject 主要依賴 |
| `src/Bee.Api.Client/LocalApiProvider.cs` | 進程內呼叫，Blazor Server 核心 |
| `src/Bee.Api.Client/RemoteDefineAccess.cs` | FormSchema / FormLayout 取得與快取 |
| `src/Bee.Api.AspNetCore/ApiServiceController.cs` | 若宿主需同時暴露 HTTP endpoint |
| `docs/development-cookbook.md` | ExecFunc 模式、FormSchema 驅動流程 |
