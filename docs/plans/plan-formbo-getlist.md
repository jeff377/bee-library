# 計畫：FormBusinessObject.GetList — BO 方法 + API 合約

**狀態：📝 擬定中（本版不含分頁；分頁拆至後續 plan 處理）**

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
    public interface IGetListRequest
    {
        /// <summary>FormTable name; empty falls back to master table.</summary>
        string TableName { get; }

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
        [Key(100)] public string TableName { get; set; } = string.Empty;
        [Key(101)] public string SelectFields { get; set; } = string.Empty;
        [Key(102)] public FilterNode? Filter { get; set; }
        [Key(103)] public SortFieldCollection? SortFields { get; set; }
        // Key(104) 保留給未來 PagingOptions（見「範圍外 / 分頁」段落）。
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

> **`[Key]` 預留約定**：本 plan 在 `GetListArgs` / `GetListRequest` 預留
> `Key(104)`、在 `GetListResult` / `GetListResponse` 預留 `Key(101)` 給未來的
> 分頁欄位。同樣的編號約定須一併套用在 `Bee.Business/Form/GetListArgs.cs` /
> `GetListResult.cs` —— 雖然 BO 端的 POCO 目前不標 `[Key]`（沒有 MessagePack
> 屬性），但若日後改成走 contract registry 對映，編號需與 wire DTO 同步。

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
        public string TableName { get; set; } = string.Empty;
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

> P1 階段需確認的依賴：
> - `FormBusinessObject` 是否已可取得 `IDefineAccess`、`IDbAccessFactory`、
>   `IFormCommandBuilderFactory`（透過 `BusinessObject.Context` / DI ctor）
> - `IFormCommandBuilderFactory` 是否已存在；若無則本計畫**不**新建，改在
>   `GetList` 內以 `databaseType` switch 取對應 builder（與既有
>   `FormCommandBuilderIudIntegrationTests` 模式一致）。傾向新建 factory，但
>   先 P1 探勘決定。
> - `BusinessObject.DefineAccess` 屬性是否存在；不存在則改走 `Context.DefineAccess`。

## Client connector 包裝

### `FormApiConnector.GetListAsync` / `GetList`

```csharp
public async Task<GetListResponse> GetListAsync(
    string tableName = "",
    string selectFields = "",
    FilterNode? filter = null,
    SortFieldCollection? sortFields = null)
{
    var request = new GetListRequest
    {
        TableName = tableName,
        SelectFields = selectFields,
        Filter = filter,
        SortFields = sortFields
    };
    return await ExecuteAsync<GetListResponse>(FormActions.GetList, request).ConfigureAwait(false);
}

public GetListResponse GetList(
    string tableName = "",
    string selectFields = "",
    FilterNode? filter = null,
    SortFieldCollection? sortFields = null)
    => SyncExecutor.Run(() => GetListAsync(tableName, selectFields, filter, sortFields));
```

> 預設 `PayloadFormat.Encrypted`（base 預設值；對齊 GetDefine）。

## ApiContractRegistry 註冊

`ApiContractRegistry.Register<TContract, TApi>()` 目前在 src/ 內**尚無任何呼叫**
（grep 結果為空），代表現行 BO 直接回傳 `GetDefineResult` 等型別走非 typed
serialization 路徑（依賴 transformer 對未標 MessagePack attr 的物件的容忍度）。

P0 探勘任務：

1. 跑一次既有 IUD 整合測試 + 開啟對應 BO method 真實 round-trip，驗證未註冊
   contract 的物件能否正確跨 wire。
2. 若不能 → 本計畫範圍包含建立統一註冊點（建議放在
   `Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` 的 `AddBeeFramework`
   初始化路徑，集中註冊：`ILoginResponse → LoginResponse`、
   `IGetDefineResponse → GetDefineResponse`、`IGetListResponse → GetListResponse`
   等），並同步補齊既有對外型別。
3. 若可（typed registry 純為可選優化）→ 本計畫**不**動 registry，只新增
   `GetListResponse` 一條，依現行慣例。

> 此處的不確定性會影響 P3 步驟的工作量；P0 探勘完成後再定 commit 切分。

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

### P0：探勘（read-only，不改碼）

1. 確認 `FormBusinessObject` 可取得 `DefineAccess` / `DbAccessFactory` /
   `FormCommandBuilderFactory`（或等價物）。
2. 確認 `FilterNode` 是否已有 `[Union]` 設定可直接 MessagePack 序列化。
3. 確認 `ApiContractRegistry` 是否需要呼叫 `Register<>` 才能正確跨 wire。

**產出**：本計畫文件 P0 補完「P1 階段需確認的依賴」段落實際狀況，並校準
後續階段的工作量。

### P1：合約層落地（單一 PR）

新增 4 個檔案：
- `src/Bee.Api.Contracts/IGetListRequest.cs`
- `src/Bee.Api.Contracts/IGetListResponse.cs`
- `src/Bee.Api.Core/Messages/Form/GetListRequest.cs`
- `src/Bee.Api.Core/Messages/Form/GetListResponse.cs`
- `src/Bee.Definition/FormActions.cs`

若 P0 發現 `FilterNode` 缺 `[Union]` → 同 PR 補上（含對應序列化測試）。

### P2：BO 實作（單一 PR）

- `src/Bee.Business/Form/GetListArgs.cs`
- `src/Bee.Business/Form/GetListResult.cs`
- `src/Bee.Business/Form/FormBusinessObject.cs` 新增 `GetList` 方法
- BO 端測試（P0 確認可行的 DB 子集）

### P3：Client 包裝（單一 PR）

- `src/Bee.Api.Client/Connectors/FormApiConnector.cs` 新增 `GetListAsync` / `GetList`
- API round-trip 測試

### P4（可選）：ApiContractRegistry 統一註冊

僅當 P0 確認 registry 路徑為必需時才執行；否則略過。

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
2. **DbAccess / FormCommandBuilder 取得方式**：若 FormBusinessObject 尚未注入
   對應服務（目前看起來只有 `ProgId` 屬性），需在 P2 階段擴充 BO 建構或
   `IBeeContext` 暴露；此調整若涉及公開簽章可能會擴大本計畫範圍，需於 P0
   評估。
3. **ApiContractRegistry 一致性**：目前無註冊呼叫的事實若會擾動既有測試，
   P0 須排除，避免本計畫被既有問題拖累。

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
