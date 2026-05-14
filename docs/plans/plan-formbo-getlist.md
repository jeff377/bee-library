# 計畫：FormBusinessObject.GetList — BO 方法 + API 合約

**狀態：✅ 已完成（2026-05-14；P0–P3 全部落地。本版不含分頁，分頁拆至後續 plan 處理）**

> **設計簡化（2026-05-14）**：移除 `TableName` 屬性 —— 框架已強制
> `FormSchema.MasterTable.TableName == ProgId`，GetList 的目標表永遠是
> master table，由 `ProgId` 直接決定，不需要在 request 上重複攜帶。
> 詳列子表查詢屬未來分階段擴充範疇，不在本 plan 內。

## 背景

`SqlFormCommandBuilder.BuildSelect` 已驗證可依 FormSchema 產生正確 SELECT 語法
（含單階／多階關聯 JOIN、Filter、Sort、跨 5 個 DB dialect 整合測試通過）。
下一步需在 BO 層暴露對應方法，讓呼叫端以 `progId + 查詢條件 + 顯示欄位` 取得
清單資料 DataTable。本方法同時需開放給 API，給遠端 client 使用。

## 命名

- BO 方法：`FormBusinessObject.GetList(GetListArgs args) -> GetListResult`
- API action：`<progId>.GetList`（JSON-RPC method）
- 對齊既有概念：
  - `FormSchema.ListFields` 屬性：預設清單顯示欄位
  - `FormSchema.GetListLayout()`：UI 清單欄位排版
  - `BO.GetList(...)`：取得清單**資料**（本計畫新增）
- 既有 [ApiConnector.cs:122](../../src/Bee.Api.Client/Connectors/ApiConnector.cs) 註解已將 `GetList` 列為標準 action 範例。

## 既有對照樣板

挑 `SystemBusinessObject.GetDefine` 作為跨層合約樣板：

| 層級 | 既有 | 本計畫新增 |
|------|------|-----------|
| Contract 介面 | `Bee.Api.Contracts/IGetDefineRequest.cs` / `IGetDefineResponse.cs` | `IGetListRequest.cs` / `IGetListResponse.cs` |
| Wire DTO（MessagePack） | `Bee.Api.Core/Messages/System/GetDefineRequest.cs` / `GetDefineResponse.cs` | `Bee.Api.Core/Messages/Form/GetListRequest.cs` / `GetListResponse.cs` |
| Action 常數 | `Bee.Definition/SystemActions.GetDefine` | `Bee.Definition/FormActions.GetList`（新檔） |
| BO Args/Result | `Bee.Business/System/GetDefineArgs.cs` / `GetDefineResult.cs` | `Bee.Business/Form/GetListArgs.cs` / `GetListResult.cs` |
| BO 方法 | `SystemBusinessObject.GetDefine(...)` | `FormBusinessObject.GetList(...)` |
| Client connector 包裝 | `SystemApiConnector.GetDefineAsync` / `GetDefine` | `FormApiConnector.GetListAsync` / `GetList` |

## 合約定義

### `Bee.Api.Contracts/IGetListRequest.cs`

```csharp
namespace Bee.Api.Contracts
{
    /// <summary>Contract interface for the GetList request.</summary>
    /// <remarks>
    /// Target table is always the master table of the schema identified by ProgId
    /// (framework invariant: FormSchema.MasterTable.TableName == ProgId).
    /// </remarks>
    public interface IGetListRequest
    {
        /// <summary>Comma-separated field names; empty falls back to FormSchema.ListFields, then all fields.</summary>
        string SelectFields { get; }

        /// <summary>Filter condition tree; null for unfiltered query.</summary>
        FilterNode? Filter { get; }

        /// <summary>Sort field collection; null for default ordering.</summary>
        SortFieldCollection? SortFields { get; }
    }
}
```

### `Bee.Api.Contracts/IGetListResponse.cs`

```csharp
using System.Data;

namespace Bee.Api.Contracts
{
    /// <summary>Contract interface for the GetList response.</summary>
    public interface IGetListResponse
    {
        /// <summary>The result rows.</summary>
        DataTable? Table { get; }
    }
}
```

> `FilterNode` / `SortFieldCollection` 落在 `Bee.Definition.Filters` /
> `Bee.Definition.Sorting`。`Bee.Api.Contracts.csproj` 已 reference `Bee.Definition`
> （從 IGetDefineRequest 引用 DefineType 可確認），無需新增相依。
>
> `DataTable` 由 `Bee.Api.Core/MessagePack/DataTableFormatter.cs` 提供 MessagePack
> 序列化支援，無需額外處理。
>
> **`FilterNode` MessagePack union 序列化已預先驗證**
> （見 `tests/Bee.Api.Core.UnitTests/Filters/FilterNodeMessagePackSpike.cs`
> — 探勘 spike，實作階段可改名為正式回歸測試）：
> `FilterNode` 抽象基類已具備 `[MessagePackObject]` + `[Union(0, FilterCondition)]`
> + `[Union(1, FilterGroup)]`，子型別亦完整 `[Key]` 標註。
> `FilterNodeCollection` 與 `SortFieldCollection` 透過
> `Bee.Api.Core/MessagePack/MessagePackCodec.cs:26-27` 註冊的
> `CollectionBaseFormatter<,>` 處理；**必須**透過 `MessagePackPayloadSerializer`
> 或 `MessagePackCodec`（公開 API）走框架的 composite resolver，不可直接
> 用裸 `MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options)`
> （後者會讓 collection 內元素掉光、無錯誤拋出，是隱形的坑）。

## Wire DTO（MessagePack）

### `Bee.Api.Core/Messages/Form/GetListRequest.cs`

```csharp
namespace Bee.Api.Core.Messages.Form
{
    [MessagePackObject]
    public class GetListRequest : ApiRequest, IGetListRequest
    {
        [Key(100)] public string SelectFields { get; set; } = string.Empty;
        [Key(101)] public FilterNode? Filter { get; set; }
        [Key(102)] public SortFieldCollection? SortFields { get; set; }
        // Key(103) 保留給未來 PagingOptions（見「範圍外 / 分頁」段落）。
        // 實作時請勿佔用此編號。
    }
}
```

### `Bee.Api.Core/Messages/Form/GetListResponse.cs`

```csharp
namespace Bee.Api.Core.Messages.Form
{
    [MessagePackObject]
    public class GetListResponse : ApiResponse, IGetListResponse
    {
        [Key(100)] public DataTable? Table { get; set; }
        // Key(101) 保留給未來 PagingInfo（見「範圍外 / 分頁」段落）。
        // 實作時請勿佔用此編號。
    }
}
```

> **`[Key]` 預留約定**：本 plan 在 `GetListRequest` 預留 `Key(103)`、在
> `GetListResponse` 預留 `Key(101)` 給未來的分頁欄位。同樣的編號約定須一併
> 套用在 `Bee.Business/Form/GetListArgs.cs` / `GetListResult.cs` —— 雖然 BO 端
> 的 POCO 目前不標 `[Key]`（沒有 MessagePack 屬性），但若日後改成走 contract
> registry 對映，編號需與 wire DTO 同步。

> 新建資料夾 `Bee.Api.Core/Messages/Form/`（既有 `Messages/System/` 同層）。
>
> `Key` 編號從 100 起，繼承自 `ApiRequest` / `ApiResponse` 的 base 欄位佔 0–99。
> 對照既有 `GetDefineRequest [Key(100/101)]` 維持一致。
>
> `FilterNode` 為抽象基類（兩個具體子型別 `FilterGroup` / `FilterCondition`），
> MessagePack 預設 union 機制需在基底型別加 `[Union(...)]` 屬性宣告子類；
> 須在 P1 階段檢查 `Bee.Definition.Filters.FilterNode` 是否已有 union 設定，
> 若無則本計畫範圍**包含**補上必要的 `[Union]` attribute（在 contract 層
> 暫時無解時改傳 XML 字串視為 fallback，但優先選 typed union 路線）。

## Action 常數

### `Bee.Definition/FormActions.cs`（新檔）

```csharp
namespace Bee.Definition
{
    /// <summary>Action name constants for FormBusinessObject methods.</summary>
    public static class FormActions
    {
        /// <summary>Retrieves list-view rows by FormSchema-driven SELECT.</summary>
        public const string GetList = "GetList";
    }
}
```

> 與 `SystemActions` 對稱、平行命名空間，方便未來補 `Insert / Update / Delete`
> 等 FormBO 動作；本計畫不延伸這些。

## BO 層

### `Bee.Business/Form/GetListArgs.cs`

```csharp
namespace Bee.Business.Form
{
    public class GetListArgs : BusinessArgs, IGetListRequest
    {
        public string SelectFields { get; set; } = string.Empty;
        public FilterNode? Filter { get; set; }
        public SortFieldCollection? SortFields { get; set; }
    }
}
```

### `Bee.Business/Form/GetListResult.cs`

```csharp
namespace Bee.Business.Form
{
    public class GetListResult : BusinessResult, IGetListResponse
    {
        public DataTable? Table { get; set; }
    }
}
```

### `FormBusinessObject.GetList`

```csharp
/// <summary>
/// Retrieves list-view rows by FormSchema-driven SELECT.
/// </summary>
/// <remarks>
/// <b>This version does NOT paginate.</b> Callers MUST supply a <c>Filter</c>
/// that bounds the result set; an unbounded query against a large table will
/// load every matching row into memory on both the server and the client.
/// Pagination support is planned and tracked separately
/// (see <c>docs/plans/plan-formbo-getlist-paging.md</c> when opened) and will
/// be added as an optional <c>PagingOptions</c> field on <see cref="GetListArgs"/>
/// — this is an additive, non-breaking change to the wire contract.
/// </remarks>
[ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
public virtual GetListResult GetList(GetListArgs args)
{
    ArgumentNullException.ThrowIfNull(args);

    // 1. 解析 FormSchema + FormTable
    var schema = DefineAccess.GetFormSchema(ProgId);
    var tableName = StringUtilities.IsEmpty(args.TableName)
        ? schema.MasterTable!.TableName
        : args.TableName;

    // 2. 解析 SelectFields：args → FormSchema.ListFields → 全欄位（empty）
    var selectFields = ResolveSelectFields(args.SelectFields, schema);

    // 3. 依 CategoryId 決定 databaseId（透過 DbCategory 路由）
    var databaseId = ResolveDatabaseId(schema.CategoryId);

    // 4. 取 dialect-specific FormCommandBuilder
    var builder = FormCommandBuilderFactory.Create(schema, databaseId, DefineAccess);
    var spec = builder.BuildSelect(tableName, selectFields, args.Filter, args.SortFields);

    // 5. 執行
    var dbAccess = DbAccessFactory.Create(databaseId);
    var table = dbAccess.Execute(spec).Table;

    return new GetListResult { Table = table };
}

private static string ResolveSelectFields(string requested, FormSchema schema)
{
    if (StringUtilities.IsNotEmpty(requested)) return requested;
    if (StringUtilities.IsNotEmpty(schema.ListFields)) return schema.ListFields;
    return string.Empty; // SelectCommandBuilder 視為全欄位
}
```

> **本 plan 不在 BO 端加入結果筆數截斷保護**。理由：避免引入「截斷而非分頁」
> 的第二條 API 路徑，造成未來分頁 plan 落地時要回頭移除截斷邏輯。呼叫端責任：
> 以 `Filter` 控制結果量；XML `<remarks>` 段落已明示此限制。

> P0 已確認的依賴（2026-05-14）：
> - `BusinessObject.DefineAccess` ✅ 已存在（[BusinessObject.cs:49](../../src/Bee.Business/BusinessObject.cs:49)）。
> - `IDbAccessFactory` ✅ 已 DI 註冊，BO 透過 `Services.GetRequiredService<IDbAccessFactory>()` 取得。
> - **不**新建 `IFormCommandBuilderFactory`：直接走既有
>   `DbDialectRegistry.Get(databaseType).CreateFormCommandBuilder(schema, DefineAccess)`，
>   與 `TableSchemaBuilder` / `TableUpgradeOrchestrator` 取 dialect 的方式一致。
> - `databaseId` 解析：直接使用 `schema.CategoryId`（框架僅強制
>   `common`==`common`；多租戶部署 host 端覆寫）。

## Client connector 包裝

### `FormApiConnector.GetListAsync` / `GetList`

```csharp
public async Task<GetListResponse> GetListAsync(
    string selectFields = "",
    FilterNode? filter = null,
    SortFieldCollection? sortFields = null)
{
    var request = new GetListRequest
    {
        SelectFields = selectFields,
        Filter = filter,
        SortFields = sortFields
    };
    return await ExecuteAsync<GetListResponse>(FormActions.GetList, request).ConfigureAwait(false);
}

public GetListResponse GetList(
    string selectFields = "",
    FilterNode? filter = null,
    SortFieldCollection? sortFields = null)
    => SyncExecutor.Run(() => GetListAsync(selectFields, filter, sortFields));
```

> 預設 `PayloadFormat.Encrypted`（base 預設值；對齊 GetDefine）。

## ApiContractRegistry 註冊 — **不需要（P0 已釐清）**

`ApiContractRegistry.Register<TContract, TApi>()` 在 `src/` 內**無任何呼叫**
（僅測試檔案使用）。BO→Wire 的型別轉換由
[ApiOutputConverter.cs:74-86](../../src/Bee.Api.Core/Conversion/ApiOutputConverter.cs:74)
的命名慣例反射處理（`XxxResult` → `XxxResponse`，僅限 `Bee.Api.Core` assembly）。

`ApiContractRegistry` 是針對非慣例命名 POCO 的 fallback 機制（未實際啟用）。
GetList 走慣例路徑即可：BO 回 `GetListResult`、Wire DTO 為 `GetListResponse`，
名稱對齊自然 match。

型別白名單（`SysInfo.AllowedTypeNamespaces`，[SysInfo.cs:49](../../src/Bee.Base/SysInfo.cs:49)）
預設已含 `Bee.Api.Core` / `Bee.Business` / `Bee.Definition`，
`SafeTypelessFormatter` 反序列化可通過 GetList 涉及之所有型別。

## 測試計畫

### 字串級單元測試（已有先例）

`tests/Bee.Business.UnitTests/Form/GetListArgsTests.cs`、
`GetListResultTests.cs` — 簡單 POCO 驗證（不需要 fixture）。

### BO method 整合測試（核心）

`tests/Bee.Business.UnitTests/Form/FormBusinessObjectGetListTests.cs`：

- 使用 `IClassFixture<SharedDbFixture>`、`[DbFact(DatabaseType.SQLServer)]` 開頭。
- 與 `EmployeeBuildSelectIntegrationTests` 共用 seed 模式（Supervisor → Department → Employee）。
- 情境（每個 DB 各跑）：
  1. `GetList_DefaultSelectFields_FallsBackToListFields` — args.SelectFields=""，
     回傳的 DataTable 欄位等同於 FormSchema.ListFields（先在 Employee 上加 ListFields）。
  2. `GetList_ExplicitSelectFields_ReturnsOnlyRequested` — 指定 `sys_id,ref_dept_name`，
     僅這兩欄、值正確。
  3. `GetList_FilterAndSort_AppliesBoth` — Filter + Sort 同時，回傳列數與順序正確。
  4. `GetList_NullArgs_Throws` — ArgumentNullException。
- 至少 SQL Server + SQLite 各跑一輪（MySQL/PG/Oracle 可加，視 BO 內 dialect
  選擇是否需驗證 routing）。

### API round-trip 整合測試

`tests/Bee.Api.Core.UnitTests/Form/GetListJsonRpcRoundTripTests.cs`：

- 走 `JsonRpcExecutor`，模擬 wire format：序列化 `GetListRequest` → 進
  executor → BO 執行 → 序列化 `GetListResponse` → client 端反序列化。
- 驗證 `FilterNode` 子型別正確 round-trip（測 `FilterGroup` + `FilterCondition`
  混合樹）。
- 驗證 `DataTable` 透過 `DataTableFormatter` 正確 round-trip。

## 階段切分（PR 粒度）

### P0：探勘（read-only，不改碼）— **已完成（2026-05-14）**

1. **FormBusinessObject 取服務** ✅
   - `BusinessObject` 已有 `DefineAccess` 屬性（透過 `IBeeContext._ctx.DefineAccess`）。
   - `IDbAccessFactory` / `IDbConnectionManager` 已在
     `BeeFrameworkServiceCollectionExtensions.AddBeeFramework` 註冊為 singleton；
     BO 透過 `Services.GetRequiredService<IDbAccessFactory>()`（escape hatch）取得。
   - `IFormCommandBuilder` 工廠：走 `DbDialectRegistry.Get(databaseType)
     .CreateFormCommandBuilder(schema, defineAccess)`（靜態 registry，無需 DI）。
   - **結論**：本計畫**不**需擴 `IBeeContext` 公開簽章；P2 走 `Services` 取
     `IDbAccessFactory` + `DbDialectRegistry` 靜態取 dialect。
2. **FilterNode union 序列化** ✅ 已預先驗證（見「已知風險」段落 #1）。
3. **ApiContractRegistry** ✅ **不需顯式註冊**
   - `src/` 內 0 處 `ApiContractRegistry.Register<>` 呼叫。
   - `ApiOutputConverter`（[ApiOutputConverter.cs:74-86](../../src/Bee.Api.Core/Conversion/ApiOutputConverter.cs:74)）
     採命名慣例反射 `XxxResult` → `XxxResponse`（限 `Bee.Api.Core` assembly），
     自動完成 BO Result → Wire Response 的型別轉換。
   - `SysInfo.AllowedTypeNamespaces`（[SysInfo.cs:49](../../src/Bee.Base/SysInfo.cs:49)）
     預設已含 `Bee.Api.Core` / `Bee.Business` / `Bee.Definition`，
     `SafeTypelessFormatter` 反序列化白名單可直接通過 GetList 涉及的所有型別。
   - **結論**：原 P4「ApiContractRegistry 統一註冊」**不執行**（移除）。

**CategoryId → databaseId 路由說明**：
- 框架僅強制 `common` 類別的 `DatabaseItem.Id == "common"`；其他類別（`company` /
  `log`）的 Id 可在多租戶部署中分歧（`company001`、`log2025`）。
- 本計畫 P2 採最簡實作：`databaseId = schema.CategoryId`。多租戶部署可由 host
  override `FormBusinessObject` 並改寫 `ResolveDatabaseId`（或日後抽
  `IDbCategoryResolver` 服務，超出本計畫範圍）。

### P1：合約層落地（單一 PR）

新增 4 個檔案：
- `src/Bee.Api.Contracts/IGetListRequest.cs`
- `src/Bee.Api.Contracts/IGetListResponse.cs`
- `src/Bee.Api.Core/Messages/Form/GetListRequest.cs`
- `src/Bee.Api.Core/Messages/Form/GetListResponse.cs`
- `src/Bee.Definition/FormActions.cs`

若 P0 發現 `FilterNode` 缺 `[Union]` → 同 PR 補上（含對應序列化測試）。

### P2：BO 實作（單一 PR）— **已完成（2026-05-14）**

**設計變更 A**：BO **不**直接呼叫 `Bee.Db`，走 Repository 抽象
(`IDataFormRepository`)。理由：`Bee.Business.csproj` 預設不依賴 `Bee.Db`
（依 [dependency-map.md:46](../dependency-map.md:46)），FormSchema-driven CRUD
的執行該由 Repository 層承接。

**設計變更 B**：移除 `TableName` 屬性。框架已強制
`FormSchema.MasterTable.TableName == ProgId`，GetList 永遠針對 master table，
由 `ProgId` 決定目標表，不需在 request 上重複攜帶。Wire DTO `Key` 編號
往下移為 `100-102`（保留 `103` 給未來分頁）。

新增檔案：
- `src/Bee.Business/Form/GetListArgs.cs`
- `src/Bee.Business/Form/GetListResult.cs`

修改檔案：
- `src/Bee.Business/Form/FormBusinessObject.cs` 新增 `GetList`，透過
  `Services.GetRequiredService<IFormRepositoryFactory>()` 取 Repository
- `src/Bee.Repository.Abstractions/Form/IDataFormRepository.cs` 加
  `GetList(tableName, selectFields, filter, sortFields) → DataTable?`
- `src/Bee.Repository/Form/DataFormRepository.cs` 實作 GetList：
  - ctor 注入 FormSchema / IDefineAccess / IDbAccessFactory /
    IDbConnectionManager / databaseId
  - `MasterTable.TableName` / `ListFields` fallback 邏輯放在 Repository（不在 BO）
- `src/Bee.Repository/Factories/FormRepositoryFactory.cs` ctor 改注入
  IDefineAccess / IDbAccessFactory / IDbConnectionManager；`CreateDataFormRepository`
  解析 schema、用 `schema.CategoryId` 當 databaseId
- `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` 把
  `IFormRepositoryFactory` 註冊從 `CreateOrDefault`（parameterless）
  改為 `CreateConfigurableService`（DI-aware）

測試：
- `tests/Bee.Business.UnitTests/Form/FormBusinessObjectGetListTests.cs`
  （SQLite + SQL Server × ExplicitSelectFields / FilterAndSort + null args）
  — Bee.Business.UnitTests.csproj 加 `Bee.Repository` ProjectReference
- 刪除過時的 `tests/Bee.Repository.UnitTests/DataFormRepositoryTests.cs` /
  `FormRepositoryFactoryTests.cs`（trivial 屬性 setter 測試，重構後需 DI
  服務才能建構、覆蓋價值低，整合測試承接）

Release build 0 warning / 0 error；SQLite + null args 測試通過
（SQL Server 失敗為本機 Docker daemon 未啟動的環境問題，與本 plan 無關）。

### P3：Client 包裝（單一 PR）— **已完成（2026-05-14）**

- [FormApiConnector.cs](../../src/Bee.Api.Client/Connectors/FormApiConnector.cs)
  新增 `GetListAsync` / `GetList`，預設 `PayloadFormat.Encrypted`
- 拆兩層 round-trip 測試：
  - 純 wire-level：
    [GetListMessagePackTests.cs](../../tests/Bee.Api.Core.UnitTests/Form/GetListMessagePackTests.cs)
    驗證 `GetListRequest` 帶 `FilterGroup + FilterCondition` 巢狀 union 與
    `GetListResponse.Table` (`DataTable`) 經 `MessagePackCodec` round-trip
    完整還原；default 值（Filter / SortFields 皆 null）亦可正常 round-trip
  - JsonRpcExecutor dispatch：
    [GetListJsonRpcRoundTripTests.cs](../../tests/Bee.Api.Core.UnitTests/Form/GetListJsonRpcRoundTripTests.cs)
    用 stub `IDataFormRepository` + `TestOverrideServiceProvider` 覆蓋 DI，
    驗證 `Employee.GetList` 經 executor 派發到 BO method、`ApiInputConverter`
    保留 Filter/SortFields、`ApiOutputConverter` 命名慣例反射有作用

**P3 附帶修正**：[CollectionBaseFormatter.cs](../../src/Bee.Api.Core/MessagePack/CollectionBaseFormatter.cs)
新增 nil 進 nil 出處理。原本對 nullable collection 欄位（如
`SortFieldCollection? SortFields = null`）會 NRE，使 default `GetListRequest`
無法序列化。本修正影響面僅限於透過 `CollectionBaseFormatter<,>` 註冊的
collection 型別（`FilterNodeCollection` / `SortFieldCollection`），其他既有
測試行為不變。

Release build 0w/0e；5 個 wire 層測試 + 3 個 P2 BO 層測試（SQLite + null
args）全部通過。

### ~~P4（可選）：ApiContractRegistry 統一註冊~~ — **不執行**

P0 探勘確認 `ApiOutputConverter` 的命名慣例反射（`XxxResult` → `XxxResponse`）
已涵蓋 GetList round-trip 需求；`ApiContractRegistry.Register<>` 在 `src/`
完全無 caller，現有 BO（含 `SystemBusinessObject.GetDefine`）皆以慣例路徑跨 wire。
本計畫不引入額外 registry 註冊。

## 範圍外

- **分頁（Skip / Take / TotalCount）— 拆至後續 plan**
  `docs/plans/plan-formbo-getlist-paging.md`（未開）。
  待本 plan P0~P3 落地後再啟動。後續 plan 需處理：
  - `Bee.Db.Dml.SelectCommandBuilder` 新增 `int? skip` / `int? take` 參數
  - 同類擴 `SelectCommandBuilder.BuildCount(...)` 對應 COUNT(*) 查詢
  - 5 個 dialect FormCommandBuilder 補 OFFSET/FETCH（SQL Server / Oracle）
    或 LIMIT/OFFSET（PostgreSQL / SQLite / MySQL）語法
  - `Bee.Definition.Paging.PagingOptions` / `PagingInfo` 新型別
  - `GetListArgs` 加 `[Key(104)] PagingOptions? Paging`；
    `GetListResult` 加 `[Key(101)] PagingInfo? Paging`（本 plan 已預留編號）
  - BO 端：`PageSize` 上限保護、`SortFields == null` 時自動 fallback `sys_no ASC`、
    `IncludeTotalCount` 處理（同連線跑兩個 query：COUNT + 分頁 SELECT）、
    `HasMore` 推算（`IncludeTotalCount=false` 時暗中多取 1 筆判斷）
  - 5 DB × 「分頁含 TotalCount」「分頁不含 TotalCount」「不分頁」三情境的
    round-trip 整合測試
- Master-Detail 同步查（一次抓主表 + 多個明細表）— 留待後續 plan。
- 結果欄位中繼資料（欄位顯示名、寬度、格式）— 由 `GetListLayout` 另行提供。
- `Insert / Update / Delete` 對應 API 方法 — 不在本計畫範圍，未來以對稱 plan
  延伸（`Bee.Db.Dml.*CommandBuilder` 已就緒）。
- 多 ProgId 跨表 JOIN 自訂查詢 — 屬於 ExecFunc 範疇。

## 已知風險

1. ~~**FilterNode union 序列化**~~ **已預先驗證可運作**（2026-05-14）：
   `FilterNode` 抽象基類已具備 `[MessagePackObject]` + `[Union]` 標註；
   `FilterNodeCollection` 透過框架 composite resolver 中註冊的
   `CollectionBaseFormatter<,>` 處理（見
   `Bee.Api.Core/MessagePack/MessagePackCodec.cs:26`）。
   spike 測試見 `tests/Bee.Api.Core.UnitTests/Filters/FilterNodeMessagePackSpike.cs`，
   實作階段請改名為正式回歸測試（檔名建議 `FilterNodeMessagePackTests.cs`）。
   **唯一坑點**：必須透過 `MessagePackPayloadSerializer` / `MessagePackCodec`
   走框架 resolver，不可直接用裸 `MessagePackSerializer.Serialize(...,
   ContractlessStandardResolver.Options)` —— 後者會讓 collection 元素掉光、
   無錯誤拋出。
2. ~~**DbAccess / FormCommandBuilder 取得方式**~~ — **已釐清（2026-05-14）**：
   不擴 `IBeeContext` 公開簽章；P2 透過 `BusinessObject.Services` escape hatch 取
   `IDbAccessFactory`，並走 `DbDialectRegistry.Get(...)` 靜態 registry 取
   dialect。`BusinessObject.DefineAccess` 已存在可直接用。
3. ~~**ApiContractRegistry 一致性**~~ — **已釐清（2026-05-14）**：
   `src/` 內無任何 `Register<>` 呼叫；`ApiOutputConverter` 慣例反射涵蓋
   `GetListResult` → `GetListResponse` 的 BO→Wire 轉換；型別白名單預設已含
   涉及之命名空間。本計畫無需動 registry。

## 為後續 session 提示的暖機資訊

實作此 plan 時，下列檔案是最有用的閱讀起點（已驗證掌握，可直接對照寫程式）：

| 用途 | 檔案 |
|------|------|
| Contract 介面樣板 | `src/Bee.Api.Contracts/IGetDefineRequest.cs` / `IGetDefineResponse.cs` |
| Wire DTO 樣板 | `src/Bee.Api.Core/Messages/System/GetDefineRequest.cs` / `GetDefineResponse.cs` |
| Action 常數樣板 | `src/Bee.Definition/SystemActions.cs` |
| BO Args/Result 樣板 | `src/Bee.Business/System/GetDefineArgs.cs` / `GetDefineResult.cs` |
| BO 方法簽章樣板 | `src/Bee.Business/System/SystemBusinessObject.cs:191-198`（GetDefine） |
| Client connector 樣板 | `src/Bee.Api.Client/Connectors/SystemApiConnector.cs:217-242`（GetDefineAsync / GetDefine） |
| SQL 建構整合測試樣板 | `tests/Bee.Db.UnitTests/EmployeeBuildSelectIntegrationTests.cs`（5 DB round-trip） |
| MessagePack spike 結論 | `tests/Bee.Api.Core.UnitTests/Filters/FilterNodeMessagePackSpike.cs` |
| JSON-RPC dispatch 邏輯 | `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:144-150`（反射依 action 找 BO method） |
| FormBusinessObject 現況 | `src/Bee.Business/Form/FormBusinessObject.cs`（只有 ExecFunc dispatch，無業務方法） |
| FormSchema 屬性 | `src/Bee.Definition/Forms/FormSchema.cs`（`ListFields` 屬性、`MasterTable` 屬性、`GetListLayout()`） |
| `IFormCommandBuilder` 既有 | `src/Bee.Db/Dml/IFormCommandBuilder.cs`（5 個 dialect 各自實作此介面） |

Plan 載入順序：
1. 先讀本 plan 全文
2. 開上表中的「樣板」檔對齊命名與檔案位置
3. 跑 P0 探勘三題（FormBusinessObject 取服務、`FilterNode` union—已驗證、
   `ApiContractRegistry` 是否需顯式註冊）
4. 由 P1 開始實作
