# 計畫：FormBO.GetList 分頁支援（BO / API / Client 層）

**狀態：✅ 已完成（2026-05-15）**

## 背景

`FormBusinessObject.GetList`（plan-formbo-getlist.md，已完成 2026-05-14）目前
**不支援分頁**，呼叫端必須以 `Filter` 限縮結果量，否則大表會被整批載入記憶體。
本 plan 補上 BO / API / Client 層的分頁能力 ——SQL 生成已由
[`plan-selectcommand-paging.md`](plan-selectcommand-paging.md) 提供。

當初 plan-formbo-getlist 已預留 wire `[Key]` 編號（Request `Key(103)`、Response
`Key(101)`）給此擴充；不會破壞既有合約面。

### 與 SQL 層 plan 的分工

| 層 | Plan | 內容 |
|----|------|------|
| SQL 生成（5 dialect 分頁語法） | **plan-selectcommand-paging.md** | `LimitBuilder` / `SelectCommandBuilder.Build(..., skip, take)` / `BuildCount` |
| 高層型別 / 合約 / Repository / BO / Client | **本 plan** | `PagingOptions` / `PagingInfo` / `DataFormListResult` / `IDataFormRepository.GetList` 簽名 / BO Args/Result / `FormApiConnector` |

本 plan 假設 SQL 層 plan 已落地、可呼叫 `IFormCommandBuilder.BuildSelect(...,
skip, take)` 與 `IFormCommandBuilder.BuildCount(...)`。

## 設計決策（已確認）

| 決策點 | 結果 | 理由 |
|--------|------|------|
| PagingOptions API | `Page` / `PageSize`（1-based） | ERP 慣用語意；呼叫端不需自己算 Skip |
| Repository 回傳型態 | 新 wrapper `DataFormListResult { Table, Paging }` | 未來加 ResultMetadata 不動簽名；IDE 不會誤導 |
| 不分頁時 `Paging = null` | 行為與 P3 完全一致（向後相容） | `paging == null` 走原路徑、不加 LIMIT/OFFSET |
| `IncludeTotalCount` 預設 | `false` | COUNT 是額外往返，省下；用 `take = PageSize + 1` 推算 `HasMore` |

待 P0 探勘確認的決策：

| 待確認 | 候選 | 備註 |
|--------|------|------|
| `SortFields == null` 分頁時 fallback | `sys_no ASC`（推薦） / `sys_rowid ASC` / throw | 看 FormSchema 是否強制 `sys_no` |
| PageSize 上限 | 1000（建議）/ 可配置 | 防呼叫端誤填 `int.MaxValue` |

## 整體流程（5 階段）

```
[前置：plan-selectcommand-paging.md 必須先完成]
  ↓
P0 探勘
  ↓
P1 Definition 型別：PagingOptions / PagingInfo / DataFormListResult
  ↓
P2 合約 + Wire DTO：填入預留的 Key(103) / Key(101)
  ↓
P3 BO + Repository：IDataFormRepository 簽名擴張、實作 COUNT + paged SELECT
  ↓
P4 Client + 測試：FormApiConnector overload + 三層 round-trip 測試
```

## P0 探勘（read-only）

1. **`FormSchema.MasterTable` 是否強制 `sys_no` 欄位**
   - 看 Employee / Department / Project 三個 fixture 是否都有；看 `FormSchema` validation
   - 影響 Repository 端的分頁 fallback：`sortFields == null` → 是否能用 `sys_no ASC`
   - 若不保證，fallback 改為「明確 throw 並要求呼叫端提供 SortFields」
2. **`SelectContextBuilder` 在 count 模式的 join 集合**
   - 看 SQL 層 plan 的 `BuildCount` 實作（plan-selectcommand-paging.md P0 #4 同議題）
   - 若 count 仍會帶 select / sort 觸發的多餘 join，COUNT 性能下降；功能仍正確
3. **`ApiOutputConverter` 命名慣例對 `Paging` 欄位的處理**
   - BO 回 `GetListResult { Table, Paging }`、wire `GetListResponse { Table, Paging }`，
     命名對齊應自動 match；但 `PagingInfo` 為 `Bee.Definition.Paging` 命名空間，
     必須在 `SysInfo.AllowedTypeNamespaces` 之列（`Bee.Definition.*` 已涵蓋，沒問題）
4. **`DataFormListResult` 是否需 MessagePack 標註**
   - 此 wrapper 在 Repository 抽象層，**不**經 wire 傳輸（wire 走 `GetListResponse`）
   - 但若未來其他 method 也用到、走 wire，需加 `[MessagePackObject]`
   - 現階段保持 plain POCO，需要時再加
5. **MaxPageSize 該硬編碼還是配置化**
   - 簡單起見先 const（建議 1000）；未來 ERP 行業特化需要時再抽 host config
6. **`IDataFormRepository.GetList` 既有呼叫端**
   - 既有 `FormBusinessObject.GetList`（P3 落地版）
   - 既有測試 `FormBusinessObjectGetListTests` 用的 stub `IDataFormRepository`
   - 簽名變更後上述兩處需同步改

## 設計細節

### Layer 1: `Bee.Definition.Paging` 新型別

```csharp
// src/Bee.Definition/Paging/PagingOptions.cs
namespace Bee.Definition.Paging
{
    /// <summary>
    /// Page-based paging request. 1-based page index.
    /// </summary>
    [MessagePackObject]
    public sealed class PagingOptions
    {
        /// <summary>1-based page index; values below 1 are clamped to 1.</summary>
        [Key(0)] public int Page { get; set; } = 1;

        /// <summary>Rows per page; values above the framework cap are clamped.</summary>
        [Key(1)] public int PageSize { get; set; } = 50;

        /// <summary>
        /// When true, the response carries the total row count (an extra COUNT query
        /// runs against the same connection). Defaults to false because COUNT is an
        /// extra round-trip; <c>HasMore</c> alone is sufficient for most UI flows.
        /// </summary>
        [Key(2)] public bool IncludeTotalCount { get; set; } = false;
    }
}

// src/Bee.Definition/Paging/PagingInfo.cs
namespace Bee.Definition.Paging
{
    /// <summary>
    /// Paging metadata accompanying a paged query result.
    /// </summary>
    [MessagePackObject]
    public sealed class PagingInfo
    {
        [Key(0)] public int Page { get; set; }
        [Key(1)] public int PageSize { get; set; }
        /// <summary>The total matching row count; null when not requested.</summary>
        [Key(2)] public int? TotalCount { get; set; }
        /// <summary>True when more rows exist beyond the current page.</summary>
        [Key(3)] public bool HasMore { get; set; }
    }
}
```

### Layer 2: `DataFormListResult` wrapper（`Bee.Repository.Abstractions`）

```csharp
// src/Bee.Repository.Abstractions/Form/DataFormListResult.cs
namespace Bee.Repository.Abstractions.Form
{
    /// <summary>
    /// Result of a list query. Carries the data table plus optional paging
    /// metadata when the underlying query was paged.
    /// </summary>
    public sealed class DataFormListResult
    {
        public DataTable? Table { get; init; }
        public PagingInfo? Paging { get; init; }
    }
}
```

放 `Bee.Repository.Abstractions/Form/` —— 該 csproj 只依賴 `Bee.Definition`，
能拿到 `PagingInfo`、`System.Data.DataTable`。

### Layer 3: 合約 + Wire DTO

#### `Bee.Api.Contracts/IGetListRequest.cs` 加屬性

```csharp
public interface IGetListRequest
{
    string SelectFields { get; }
    FilterNode? Filter { get; }
    SortFieldCollection? SortFields { get; }
    PagingOptions? Paging { get; }   // ← 新增
}
```

#### `Bee.Api.Core/Messages/Form/GetListRequest.cs` 加 `[Key(103)]`

```csharp
[MessagePackObject]
public class GetListRequest : ApiRequest, IGetListRequest
{
    [Key(100)] public string SelectFields { get; set; } = string.Empty;
    [Key(101)] public FilterNode? Filter { get; set; }
    [Key(102)] public SortFieldCollection? SortFields { get; set; }
    [Key(103)] public PagingOptions? Paging { get; set; }   // ← 預留編號落地
}
```

#### `IGetListResponse` + `GetListResponse` 同理加 `PagingInfo? Paging`

```csharp
public interface IGetListResponse
{
    DataTable? Table { get; }
    PagingInfo? Paging { get; }   // ← 新增
}

[MessagePackObject]
public class GetListResponse : ApiResponse, IGetListResponse
{
    [Key(100)] public DataTable? Table { get; set; }
    [Key(101)] public PagingInfo? Paging { get; set; }   // ← 預留編號落地
}
```

### Layer 4: BO + Repository

#### `IDataFormRepository.GetList` 簽名變更（breaking）

```csharp
public interface IDataFormRepository
{
    DataFormListResult GetList(   // ← 回傳改 wrapper
        string selectFields,
        FilterNode? filter,
        SortFieldCollection? sortFields,
        PagingOptions? paging = null);   // ← 新增
}
```

#### `DataFormRepository.GetList` 實作

```csharp
private const int MaxPageSize = 1000;

public DataFormListResult GetList(
    string selectFields, FilterNode? filter,
    SortFieldCollection? sortFields, PagingOptions? paging = null)
{
    var resolvedSelectFields = StringUtilities.IsNotEmpty(selectFields)
        ? selectFields : _schema.ListFields;

    var connInfo = _connectionManager.GetConnectionInfo(_databaseId);
    var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
        .CreateFormCommandBuilder(_schema, _defineAccess);
    var dbAccess = _dbAccessFactory.Create(_databaseId);

    if (paging == null)
    {
        // 不分頁 —— 行為與 P3 完全一致
        var spec = builder.BuildSelect(_schema.ProgId, resolvedSelectFields,
            filter, sortFields);
        return new DataFormListResult { Table = dbAccess.Execute(spec).Table };
    }

    // 分頁：cap PageSize、補 default sort、跑 COUNT (optional) + paged SELECT
    var pageSize = Math.Min(Math.Max(paging.PageSize, 1), MaxPageSize);
    var page = Math.Max(paging.Page, 1);
    var skip = (page - 1) * pageSize;

    // 分頁時必須有確定的 ORDER BY；sortFields 為 null 套 sys_no ASC fallback。
    // 此 fallback 是 Repository 責任，SQL 層不知道 sys_no 約定。
    var effectiveSort = sortFields ?? DefaultSortForPaging(_schema);
    // ↑ sys_no ASC（待 P0 確認 fallback 規則；無 sys_no 則 throw）

    int? totalCount = null;
    if (paging.IncludeTotalCount)
    {
        var countSpec = builder.BuildCount(_schema.ProgId, filter);
        totalCount = ValueUtilities.CInt(dbAccess.Execute(countSpec).Scalar!);
    }

    // 不要 TotalCount → take = pageSize + 1 推算 HasMore
    var take = paging.IncludeTotalCount ? pageSize : pageSize + 1;
    var pagedSpec = builder.BuildSelect(_schema.ProgId,
        resolvedSelectFields, filter, effectiveSort, skip, take);
    var table = dbAccess.Execute(pagedSpec).Table!;

    bool hasMore;
    if (paging.IncludeTotalCount)
    {
        hasMore = totalCount > skip + table.Rows.Count;
    }
    else
    {
        hasMore = table.Rows.Count > pageSize;
        if (hasMore) table.Rows.RemoveAt(table.Rows.Count - 1);  // slice off probe row
    }

    return new DataFormListResult
    {
        Table = table,
        Paging = new PagingInfo
        {
            Page = page, PageSize = pageSize,
            TotalCount = totalCount, HasMore = hasMore,
        },
    };
}

private static SortFieldCollection DefaultSortForPaging(FormSchema schema)
{
    // P0 探勘 #1 確認 sys_no 存在後落實此 fallback；
    // 若 schema.MasterTable 無 sys_no，throw InvalidOperationException 要求呼叫端傳 SortFields
    ...
}
```

#### BO Args / Result 加 `Paging` 欄位

```csharp
public class GetListArgs : BusinessArgs, IGetListRequest
{
    public string SelectFields { get; set; } = string.Empty;
    public FilterNode? Filter { get; set; }
    public SortFieldCollection? SortFields { get; set; }
    public PagingOptions? Paging { get; set; }   // ← 新增
}

public class GetListResult : BusinessResult, IGetListResponse
{
    public DataTable? Table { get; set; }
    public PagingInfo? Paging { get; set; }   // ← 新增
}
```

#### `FormBusinessObject.GetList` 透傳

```csharp
public virtual GetListResult GetList(GetListArgs args)
{
    ArgumentNullException.ThrowIfNull(args);
    var factory = Services.GetRequiredService<IFormRepositoryFactory>();
    var repository = factory.CreateDataFormRepository(ProgId);
    var listResult = repository.GetList(
        args.SelectFields, args.Filter, args.SortFields, args.Paging);
    return new GetListResult
    {
        Table = listResult.Table,
        Paging = listResult.Paging,
    };
}
```

### Layer 5: Client connector

```csharp
public async Task<GetListResponse> GetListAsync(
    string selectFields = "",
    FilterNode? filter = null,
    SortFieldCollection? sortFields = null,
    PagingOptions? paging = null)   // ← 新增
{
    var request = new GetListRequest
    {
        SelectFields = selectFields,
        Filter = filter,
        SortFields = sortFields,
        Paging = paging,
    };
    return await ExecuteAsync<GetListResponse>(FormActions.GetList, request)
        .ConfigureAwait(false);
}

public GetListResponse GetList(
    string selectFields = "",
    FilterNode? filter = null,
    SortFieldCollection? sortFields = null,
    PagingOptions? paging = null)
    => SyncExecutor.Run(() => GetListAsync(selectFields, filter, sortFields, paging));
```

## 測試計畫

### Wire-level round-trip（`tests/Bee.Api.Core.UnitTests/Form/`）

擴充既有 `GetListMessagePackTests`：
- `GetListRequest` 帶 `PagingOptions { Page=2, PageSize=50, IncludeTotalCount=true }` round-trip
- `GetListResponse` 帶 `PagingInfo { Page=2, PageSize=50, TotalCount=237, HasMore=true }` round-trip
- 兩者 `Paging = null` 的 default round-trip（驗證 nullable）

### Executor dispatch round-trip

擴充 `GetListJsonRpcRoundTripTests`：
- stub repository 收到 `args.Paging` 與請求對齊
- stub 回傳 `DataFormListResult { Table, Paging = ... }`，驗證 response 的
  `Paging` 欄位透過 `ApiOutputConverter` 命名慣例 copy 到 `GetListResponse.Paging`

### BO + DB integration

新增測試類 `FormBusinessObjectGetListPagingTests`（或擴 `FormBusinessObjectGetListTests`）：

| 情境 | 期望 |
|------|------|
| 不分頁（`args.Paging = null`） | `Result.Paging = null`；行為與既有 P3 測試一致 |
| 分頁含 TotalCount（`IncludeTotalCount = true`） | `Paging.TotalCount` 等於實際符合條件總列數；`HasMore` 正確 |
| 分頁不含 TotalCount（`IncludeTotalCount = false`） | `Paging.TotalCount = null`；`HasMore` 用 probe 列推算 |
| `Page` 超過總頁數 | 回空 table、`HasMore = false`、`TotalCount` 若有要求則正確 |
| `PageSize > MaxPageSize` | clamp 到 cap、不丟例外 |
| `SortFields = null` + 分頁 | 自動 fallback `sys_no ASC`（P0 #1 確認後落實） |

每 DB（至少 SQLite + SQL Server）跑一輪。

## 階段切分（PR / commit 粒度）

| 階段 | 內容 | 跨多少檔 |
|------|------|----------|
| P0 探勘 | 補待確認決策；不改碼 | 0 |
| P1 Definition | PagingOptions / PagingInfo / DataFormListResult | 3 |
| P2 Wire + 合約 | Contract 介面 + GetListRequest/Response 加欄位 + wire round-trip 測試 | 5 |
| P3 BO + Repository | IDataFormRepository / DataFormRepository / GetListArgs / Result / FormBusinessObject / BO 測試 | ~8 |
| P4 Client + Executor 測試 | FormApiConnector + executor round-trip 測試 | 2 |

預估總共 ~18 檔。可以分 P1-P4 commit 或合為單一 commit。

## 已知風險

1. **同連線多 query 的 transaction 語意** —— COUNT + paged SELECT 之間若有
   並行 INSERT/DELETE，結果可能對不上。預設使用 READ COMMITTED（不開
   transaction），accept 此弱一致性；如需 SNAPSHOT 一致性，host 端可開
   transaction 包住 BO 呼叫
2. **`SortFields == null` 的 fallback 行為** —— 若 FormSchema 沒 `sys_no`，
   要明確 throw 而非預設 `sys_rowid ASC`（Guid 排序對用戶無意義且難 debug）
3. **`IDataFormRepository.GetList` 簽名變更是 breaking API change** ——
   回傳從 `DataTable?` 改為 `DataFormListResult`；既有實作（只有
   `DataFormRepository`）需同步改；既有測試 stub（`FormBusinessObjectGetListTests`
   內的 `StubDataFormRepository`）需同步改
4. **`MaxPageSize` cap 屬於框架硬編碼** —— 不同部署可能需要不同上限。
   先用 const（如 1000），未來再抽 host config
5. **Probe 列推算 HasMore 的副作用** —— 取 `PageSize + 1` 後可能撈到不需要
   的最後一列；網路傳輸成本 +1 列，可接受。`table.Rows.RemoveAt(...)` 在
   client 端 DataTable 操作，不會觸發 DB 變動

## 範圍外

- **SQL 層分頁實作** —— 見 `plan-selectcommand-paging.md`
- **跨表 JOIN 的分頁複雜度**（master + detail 同時分頁） — 仍屬 ExecFunc 範疇
- **Cursor-based pagination** —— offset-based 已夠 ERP 用，cursor 留待 future plan
- **`Insert / Update / Delete` 的 API 方法** — 不在本 plan 範圍

## 為後續 session 提示的暖機資訊

實作時最有用的閱讀起點：

| 用途 | 檔案 |
|------|------|
| 依賴的 SQL 層 plan | `docs/plans/plan-selectcommand-paging.md` |
| 主 plan（已完成、含已預留 Key 編號） | `docs/plans/plan-formbo-getlist.md` |
| 樣板：BO 整合測試 | `tests/Bee.Business.UnitTests/Form/FormBusinessObjectGetListTests.cs` |
| 樣板：wire round-trip | `tests/Bee.Api.Core.UnitTests/Form/GetListMessagePackTests.cs` |
| 樣板：executor round-trip | `tests/Bee.Api.Core.UnitTests/Form/GetListJsonRpcRoundTripTests.cs` |
| 既有 BO method | `src/Bee.Business/Form/FormBusinessObject.cs`（`GetList`） |
| 既有 Repository | `src/Bee.Repository/Form/DataFormRepository.cs` |
| Skill | `.claude/skills/bee-add-bo-method/SKILL.md`（本機，gitignored） |

Plan 載入順序：
1. 先確認 `plan-selectcommand-paging.md` 已完成（前置依賴）
2. 讀本 plan 全文
3. 跑 P0 探勘 6 題
4. 由 P1 開始實作；每階段 Release build 0w/0e 才往下走
