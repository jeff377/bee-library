# 版本變更記錄

[English](CHANGELOG.md)

本檔記錄專案的所有重要變更。

格式遵循 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.1.0/)，版本號採用 [語意化版本](https://semver.org/lang/zh-TW/)。

## [4.6.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「開放 JSON-RPC 給 JS 前端」：FormBO / SystemBO 共 7 個 CRUD / Session 方法 `ProtectionLevel` 降為 `Public`、新增兩個 JSON-native 取得方法（`GetFormSchema` / `GetFormLayout`），並修正 Plain 路徑 DataSet 反序列化與 Blazor WebAssembly RSA 相關阻塞問題。`MasterKeySource` 預設值改為 `Environment`，依嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

### 新增

- **`SystemBO.GetFormSchema` / `GetFormLayout`** — 新增兩個 JSON-friendly 取得方法，給 JS / TypeScript 前端走 Plain wire format 直接拿到強型別 `FormSchema` / `FormLayout` 的 JSON tree，做 schema-driven 渲染。`.NET` client 端對應 `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync`；既有 `GetDefineAsync<FormSchema>`（XML 字串中介）仍可繼續使用。兩個方法皆為 `Public + Authenticated`。對應決策：[ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md)。
- **JSON-RPC 前端整合指引** — 新增 [`docs/jsonrpc-frontend-integration.md`](docs/jsonrpc-frontend-integration.md)（中英雙語），涵蓋 wire format、headers、認證流程、可呼叫方法清單、`JsonRpcErrorCode` 對應表與 TypeScript wrapper snippet，供 React / Vue / Angular / vanilla JS 前端參照。

### 變更

- **`MasterKeySource` 預設改為 `Environment`**（**breaking**）— 新部署一律從環境變數 `$BEE_MASTER_KEY` 讀取 master key,不再於 `DefinePath` 下產生 `Master.key` 檔案。對齊 12-factor「config in env」原則,使 container / Kubernetes / cloud function 等 host 可直接注入金鑰,不需額外 mount secret volume。既有部署若 `SystemSettings.xml` 已明確設定 `<Type>File</Type>` 不受影響;要遷移既有 host:把現有 `Master.key` 的 Base64 內容 set 給 `BEE_MASTER_KEY`,並把 `SystemSettings.xml` 改為 `<Type>Environment</Type><Value>BEE_MASTER_KEY</Value>`。對應決策：[ADR-015](docs/adr/adr-015-master-key-environment-default.md)。
- **7 個 BO 方法 `ProtectionLevel` 降為 `Public`** — `FormBO.GetNewData` / `GetData` / `Save` / `Delete` 與 `SystemBO.EnterCompany` / `LeaveCompany` / `Logout` 由 `Encrypted + Authenticated` 降為 `Public + Authenticated`，讓 JS 前端可透過 `PayloadFormat.Plain` 以原生 JSON over HTTPS 直接呼叫。`Authenticated` 仍守身分門檻、application-layer 業務權限檢查不變。**向下相容**：既有 `Encrypted` 格式的 `.NET` client 仍可正常呼叫（`ApiAccessValidator` 允許高保護格式呼叫低保護方法）。對應決策：[ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md)。
- **`FormSchema.MasterTable` 加 `[JsonIgnore]`** — JSON 序列化時 `MasterTable` 屬性不再輸出（其值永遠等於 `Tables[ProgId]`，原本對 JS Plain wire format 是 ~30% 無效 payload）。XML 序列化與 MessagePack 路徑**不受影響**（未加 `XmlIgnore`、`.NET` client 仍走原路徑）。JS / TS 客戶端若曾從 JSON 內 `masterTable` 取 master schema，需改從 `tables` 陣列第一個元素讀。

### 修正

- **Blazor WebAssembly 登入解封** — `RsaCryptor` 改用 PEM (SPKI public / PKCS#1 private) 取代 XML key 格式，移除對 Windows-only `RSA.ToXmlString` / `FromXmlString` 的依賴。WASM 平台再透過 `OperatingSystem.IsBrowser()` fallback：client 送空 `ClientPublicKey`（server 端短路加密、後續 `Encrypted` 請求自動降級為 `Encoded`），`HttpClient` 使用預設實例以走 `BrowserHttpHandler`。端到端驗證 Wasm demo（Sign in → Employee CRUD）通過，`Blazor.Server.Demo` 與 `QuickStart.Console` 無 regression。
- **`ApiInputConverter` 補齊 Plain 路徑反序列化的 converters** — 原本只設 `PropertyNameCaseInsensitive`，缺 `DataTableJsonConverter` / `DataSetJsonConverter` / `JsonStringEnumConverter`，導致 Plain 格式的 `SaveArgs` 進來後 DataTable rows 全部為空，`Save` 一律回 "DataSet has no pending changes"。對齊 write 端 `JsonCodec` 的註冊；任何 Plain 路徑承載 `DataSet` 的呼叫現皆可正常運作。
- **MAUI `DynamicForm` 在 in-place `DataSet` mutation 後正確重繪** — `FormDataObject` 在 New / Load / Save / Delete 流程內就地改寫同一個 `DataSet` 參考，但 `BindableProperty` 只在 reference change 時觸發 `propertyChanged`，導致 `FormPage.RefreshFormView` 看似無效（按 New 後 form 不清空、Save / Delete 按鈕不啟用）。`DynamicForm` 新增 public `Refresh()` 入口直接驅動既有 `Rebuild()`。Blazor 不受影響（Razor 每次 event handler 完成都會 re-render）。

### 升級指引

**`MasterKeySource` 遷移既有 host（File → Environment）：**

```bash
# 1. 把現有 Master.key 的 Base64 內容設為環境變數
export BEE_MASTER_KEY="$(cat $DEFINE_PATH/Master.key)"
```

```xml
<!-- 2. SystemSettings.xml 內把 MasterKeySource 改為 Environment -->
<MasterKeySource>
  <Type>Environment</Type>
  <Value>BEE_MASTER_KEY</Value>
</MasterKeySource>
```

新 host 直接 `export BEE_MASTER_KEY=<base64>` 即可，預設 `SystemSettings.xml` 已是 `Environment`。

**JS / TS 客戶端取 `FormSchema` master schema：**

```diff
- const masterTable = formSchema.masterTable;
+ const masterTable = formSchema.tables[0];
```

`.NET` 客戶端（MessagePack / XML）不需修改。

## [4.5.0]

> Bee.NET 仍處於 pre-stable 演進階段。本次新增三層前端套件（`Bee.UI.Core` 跨平台共通層、`Bee.UI.Maui` MAUI 行動／桌面控制項、`Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm` 兩個 Blazor RCL），並把 API connector 介面整批轉為 async-only。介面簽名變動由嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

### 新增

- **新套件 `Bee.UI.Core`** — 跨平台 UI 共通層（Web / MAUI 共用 ViewModel、`FormDataObject`、`SystemApiConnector`、`ClientInfo`）。前身為獨立 repo `bee-ui-core`，本版併入 monorepo 統一發佈。對應決策：[ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md)。
- **新套件 `Bee.UI.Maui`** — .NET MAUI 跨平台控制項層（iOS / Android / macOS / Windows）；提供 `DynamicForm` / `DynamicGrid` / `FormPage` 與 sandbox-friendly `MauiPreferenceEndpointStorage`。同步附範例 `samples/Maui.Demo` 接 `QuickStart.Server`。預設 TFM `net10.0`（library 端不需 MAUI workload 即可 build／consume），平台 TFM 透過 `-p:BeeUiMauiFullPlatforms=true` 開啟。
- **新套件 `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`** — Blazor Razor Class Library 雙端對稱實作；提供 `DynamicForm` / `DynamicGrid` / `FormPage` 控制項、`BeeAccessTokenProvider` token 注入、`BeeLoginPanel` 登入面板、`AddBeeBlazor` 服務註冊。
- **`UserMessageException` + `JsonRpcErrorCode.UserMessage`** — 統一「需要顯示給使用者的訊息」傳遞管道；後端 throw 後 `ApiConnector` 依 error code 重建為 client 端 `UserMessageException`，呼叫端可直接以訊息呈現。
- **`FormBO` 補齊 CRUD action** — `FormBusinessObject` 新增 `GetNewData` / `GetData` / `Save` / `Delete` 四個 action,使 `IFormBusinessObject` 涵蓋完整單筆 CRUD 流程(`GetList` 已於 v4.4.0 引入)。
- **`samples/` 範例專案家族** — 新增三組 demo:`QuickStart.Server` + `QuickStart.Console`(P0,本地 + JSON-RPC 雙模式)、`Blazor.Server.Demo` + `Blazor.Wasm.Demo`(P1)、`Maui.Demo`(P2)。各 demo 共用 `Bee.Samples.Shared` 的 define seed 與認證常數,並備有 `.smoke.yaml` 供端到端冒煙測試。

### 變更

- **API connector 介面轉為 async-only** — `IApiConnector` / `IFormApiConnector` / `ISystemApiConnector` 移除全部同步方法,所有對外呼叫一律走 `*Async` 版本。呼叫端需把 `connector.GetData(...)` 改為 `await connector.GetDataAsync(...)`。
- **`ExceptionExtensions` 命名空間搬遷** — 由 `Bee.Base` 改至 `Bee.Base.Exceptions`;呼叫端需新增 `using Bee.Base.Exceptions;`。
- **`ClientInfo` 改為 static class** — `ClientInfo.SystemApiConnector.Initialize()` 改為 async;此變更明確化「單一前端使用者」假設(多用戶 web 情境應改由 per-request DI 提供 token,見 [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md) 後續延伸段落)。

### 升級指引

**API connector 呼叫端(同步 → async):**

```diff
- var data = connector.GetData(progId, formData);
+ var data = await connector.GetDataAsync(progId, formData);
```

**`ExceptionExtensions` 命名空間:**

```diff
  using Bee.Base;
+ using Bee.Base.Exceptions;

  ex.Unwrap();
```

**`UserMessageException` 處理(推薦):**

```csharp
try
{
    await connector.SaveAsync(progId, dataSet);
}
catch (UserMessageException ex)
{
    // 訊息已格式化為「給使用者看」的字串;直接顯示
    ShowToast(ex.Message);
}
```

## [4.4.0]

> Bee.NET 仍處於 pre-stable 演進階段；對外公開 API 表面尚無外部消費者，minor 版本允許包含 API 搬遷與少量 breaking change。本次包含介面簽名變動（`IFormRepositoryFactory.CreateDataFormRepository`、`IDataFormRepository.GetList`）與屬性移除（`CompanyInfo.LogDatabaseId`），嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

### 新增

- **`FormBO.GetList` 統一查詢入口** — `IFormBusinessObject` 宣告 `GetList` 簽名；`FormBusinessObject` 透過 `IDataFormRepository` 實作並支援 `PagingOptions` / `PagingInfo` 分頁；`FormApiConnector.GetList` / `GetListAsync` 提供用戶端入口。跨 5 種 dialect（SQL Server / PostgreSQL / SQLite / MySQL / Oracle）皆已整合測試驗證。
- **`SystemBO` Session Lifecycle 完整化** — 新增 `EnterCompany` / `LeaveCompany` / `Logout` 三個方法，搭配既有 `Login` 構成兩對對稱方法（`Login` ↔ `Logout`、`EnterCompany` ↔ `LeaveCompany`）；`SessionInfo` 加 `CompanyId`（nullable），`Login` 簽名補入 `ISystemBusinessObject`。新增 `CompanyInfo` 型別與 `ICompanyInfoService` 快取服務。對應決策：[ADR-012](docs/adr/adr-012-session-company-context.md)。
- **bo repo DB 路由** — 新增 `DbScope` enum（`Common` / `Company` / `Log`）與 `IRepositoryDatabaseRouter`，BO 不再手寫 databaseId 字串；`BusinessObject` 新增 `ResolveDatabaseId(DbScope)` 與 `CreateDataFormRepository(string progId)` 兩個 protected helper。對應決策：[ADR-010](docs/adr/adr-010-logical-database-category.md)(後續延伸段落)。
- **`SelectCommandBuilder` 分頁與 COUNT** — 跨 5 dialect 支援 `OFFSET/FETCH` 或 `LIMIT/OFFSET`；新增 `BuildCount` 方法產出獨立 `SELECT COUNT(*)`，可獨立於 SELECT 流程使用。
- **`KeyObjectCache<T>` 負向快取** — 預設啟用 5 分鐘絕對過期負向快取以避免 cache penetration；可透過 virtual `GetNegativePolicy` 覆寫或停用（`SessionInfoCache` 已 override 停用，避免匿名流量灌爆快取）。對應決策:[ADR-009](docs/adr/adr-009-cache-implementation.md)(後續延伸段落)。
- **`IBusinessObjectFactory` typed wrapper** — `Bee.Business` 新增 `CreateFormBO(token, progId)` / `CreateSystemBO(token)` 擴充方法，消除呼叫端手動 cast 的噪音。
- **`st_company` / `st_user_company` 兩張系統表** — 落於 common 庫，搭配 `ICompanyRepository` / `IUserCompanyRepository`，讓 `EnterCompany` 對「公司不存在 / 公司停用 / 未授權」一律回 `CompanyAccessDenied`。`DbCategorySettings` 預設 common 分類已包含此兩表。
- **JsonRpcErrorCode 新增兩碼** — `CompanyNotEntered` (-32002, HTTP 409)、`CompanyAccessDenied` (-32003, HTTP 403)。後者刻意合併「無權限」與「不存在」以防止 user enumeration。

### 變更

- **`IFormRepositoryFactory.CreateDataFormRepository`** — 簽名加入 `Guid accessToken` 參數，配合 `IRepositoryDatabaseRouter` 自動路由 databaseId。BO 端建議改用 `BusinessObject.CreateDataFormRepository(progId)` helper，自動帶 token。
- **`IDataFormRepository.GetList`** — 回傳型別改為 `DataFormListResult`（含 `Table` + `Paging`），並加入 `PagingOptions? paging` default 參數。
- **`CompanyInfo.LogDatabaseId` 移除** — `DbScope.Log` 改為固定路由 `databaseId = "log"`（pre-EnterCompany 方法可寫 audit log）。跨公司 log 隔離由後續的 `sys_company_rowid` 列級分區處理，不再需要每家公司獨立 log DB。
- **`SelectCommandBuilder` 未知表名行為一致化** — 由 `KeyNotFoundException` 改為 `InvalidOperationException`，與 Insert / Update / Delete builder 對齊。

### 升級指引

**`IFormRepositoryFactory` 呼叫端：**

```diff
- var repo = factory.CreateDataFormRepository("Employee");
+ var repo = factory.CreateDataFormRepository("Employee", accessToken);
```

> BO 內部建議改用 `BusinessObject.CreateDataFormRepository(progId)` helper，自動帶 token，無需手動傳遞。

**`IDataFormRepository.GetList` 呼叫端：**

```diff
- DataTable table = repo.GetList(filter, sortFields, fields);
+ DataFormListResult result = repo.GetList(filter, sortFields, fields, paging: null);
+ DataTable table = result.Table;
```

**`CompanyInfo.LogDatabaseId` 引用點：**

```diff
- var logDbId = companyInfo.LogDatabaseId;
+ var logDbId = "log";  // 框架固定路由；跨公司隔離改用 sys_company_rowid 列級分區
```

或透過 `BusinessObject.ResolveDatabaseId(DbScope.Log)` 取得。

## [4.3.0]

> Bee.NET 仍處於 pre-stable 演進階段；對外公開 API 表面尚無外部消費者，minor 版本允許包含命名空間搬遷。本次調整以嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

### 新增

- **新套件 `Bee.Hosting`** — Bee.NET 框架的 composition root。將所有後端服務（`IDefineAccess`、`IDbAccessFactory`、`IBusinessObjectFactory`、`JsonRpcExecutor` 等）註冊到任意 `IServiceCollection`，不依賴 ASP.NET Core。非 ASP.NET Core 宿主（WinForms、WPF、Console、Worker Service、整合測試）現在可以註冊框架而不必拖入 `Microsoft.AspNetCore.App`。

### 變更

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
- <PackageReference Include="Bee.Api.AspNetCore" Version="4.2.*" />
+ <PackageReference Include="Bee.Hosting" Version="4.3.*" />
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
