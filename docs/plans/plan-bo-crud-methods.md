# 計畫:`FormBusinessObject` CRUD action 介面與跨層實作

**狀態:📝 擬定中**

## 背景

本計畫是 [plan-blazor-web-integration.md](plan-blazor-web-integration.md) 的 **Phase 0(實作前置條件)**。
Blazor 元件庫的 `FormDataObject` 需要 `LoadAsync` / `SaveAsync` / `DeleteAsync` / `NewAsync`,
但目前後端與 Connector 沒有對應的 CRUD action。

### 流程觀點:Insert / Update 不是 atomic action

「新增存檔」與「修改存檔」是 **client 端的多步驟流程**,不是 BO 的單一方法:

| 流程 | BO 上的原子方法 |
|------|----------------|
| **新增存檔** = 新增一筆空資料 → 填入欄位 → 儲存 | `GetNewData` + `Save` |
| **修改存檔** = 取得一筆現有資料 → 修改欄位 → 儲存 | `GetData` + `Save` |
| **直接刪除** | `Delete` |
| **列表查詢** | `GetList`(既有) |

「儲存」在新增與修改是**同一個 `Save` method** —— 它接收 DataSet 後依各 row 的 `RowState`
(Added / Modified / Deleted)派發到對應的 `IFormCommandBuilder.Build{Insert,Update,Delete}`,
client 端不需區分新增或更新。

### 現況 gap

| 層 | 現有 | 缺漏 |
|----|------|------|
| `IFormBusinessObject` | `GetList(GetListArgs)` | `GetNewData` / `GetData` / `Save` / `Delete` |
| `FormApiConnector` | 同步 `ExecFunc` + async/同步 `GetList` | 上述 4 個 action 的同步 + async 入口 |
| `FormActions` | `"GetList"` | `"GetNewData"` / `"GetData"` / `"Save"` / `"Delete"` |
| `Bee.Api.Core/Messages/Form/` | `GetListRequest` / `GetListResponse` | 對應 4 個 action 的 wire 型別 |
| `Bee.Api.Contracts/` | `IGetListRequest` / `IGetListResponse` | 對應 4 個 action 的 contract 介面 |
| `Bee.Repository.Abstractions/Form/` | `IDataFormRepository.GetList` | `GetNewData` / `GetData` / `Save` / `Delete` |

> **重要前提:CRUD action 不走 `FormExecFuncHandler`**
>
> [src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:147](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs#L147)
> 的派發是「`progId.action` → 反射查 `boType.GetMethod(action)`」,
> 因此 `GetList` 是 BO 上直接公開的方法,**不是經由 `FormExecFuncHandler` 路由**。
> 新增的 4 個 action 比照 `GetList`,**直接成為 `FormBusinessObject` 的 public method**,
> `FormExecFuncHandler` 維持只服務 `ExecFunc/ExecFuncAnonymous` 的內部派發,本計畫不動。

---

## 1. CRUD action 方法簽名與命名

### 1.1 範圍與命名

| Action | 用途 | 對映 SQL | 屬於哪個流程 |
|--------|------|---------|------------|
| `GetNewData` | 取得一筆「全新空白」的 DataSet 模板(含預設值、預填 `sys_rowid`) | 無 SQL,純資料組裝 | 新增存檔(第 1 步) |
| `GetData` | 依鍵值讀取單筆(Master + 全部 Detail) | `SELECT … WHERE` | 修改存檔(第 1 步) |
| `Save` | 依 DataSet 內 row 的 `RowState` 統一派發 INSERT/UPDATE/DELETE | `INSERT` / `UPDATE` / `DELETE` 混合 | 新增存檔 + 修改存檔(共用,第 2 步) |
| `Delete` | 直接依 `RowId` 刪除單筆(不需先載入 DataSet) | `DELETE` | 直接刪除 |

> **為何不採用「Insert / Update」這兩個名字**?
>
> Insert / Update 在這套框架是「**流程**」而非 BO 上的原子方法。把流程名綁到 BO method 會誤導
> 呼叫端以為「Insert 就是一行打到 DB」—— 實際上 server 端只能看到「拿到 DataSet 就寫」,
> 至於是新增還是更新,**由 DataSet 內 row 的 `RowState` 決定**。一個 `Save` action 對應到
> 「儲存」這個語意原子操作,client 端 UI 才能寫成「按下儲存,不論新增或修改都呼叫同一個」。

### 1.2 命名節奏

維持與既有 `GetList` 一致的「動詞 + 名詞」風格:

- `Get*`(取得):`GetList`、`GetData`、`GetNewData`
- 單一動詞(已隱含「對 form data 操作」,因為 connector 帶 ProgId):`Save`、`Delete`

> 不在 `Save` / `Delete` 後綴 `Data`,理由:
> - `FormApiConnector` 已限定 form data 上下文(`connector.SaveAsync(ds)` 讀起來就是「儲存這份 form data」)。
> - 與既有 `SystemActions.SaveDefine`(`Save + Define`)節奏一致 —— Form 級的 form data 本身就是預設受詞,不需綴 `Data`。

### 1.3 同步 vs 非同步

**BO 端維持純同步**(沿用既有 `GetList` 樣式):

```csharp
public virtual GetNewDataResult GetNewData(GetNewDataArgs args);
public virtual GetDataResult    GetData(GetDataArgs args);
public virtual SaveResult       Save(SaveArgs args);
public virtual DeleteResult     Delete(DeleteArgs args);
```

理由:
- `JsonRpcExecutor.InvokeMethodAsync`([src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:159](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs#L159))已自動處理 sync/async BO method;BO 端是否 async 對 wire 行為無影響。
- 既有 Repository(`DataFormRepository.GetList`)及底層 `DbAccess` 全部同步;為了 BO async 而改寫整條 chain 不划算。
- ADO.NET 的 `DbCommand.ExecuteNonQuery` 在多數 provider 下並未真正 IO-bound,加上 `Task.Run` 包裝只是假 async,反而引入 thread-pool 開銷。
- 若日後底層 Repository / `DbAccess` 改成真正 async,再以 `*Async` 為新公開 method,既有同步 method 保留(同 `FormApiConnector` 雙模做法)。

**Connector 端 async-only**(新慣例,不再加同步包裝):

```csharp
// 只提供 async
public Task<SaveResponse> SaveAsync(DataSet dataSet);
```

理由:

1. **主要消費者是 Blazor**。本計畫是 [plan-blazor-web-integration.md](plan-blazor-web-integration.md) 的 Phase 0,Blazor 元件全棧 async,絕對不會呼叫同步版。
2. **Sync-over-async 在 Blazor Server 特別痛**。`SyncExecutor.Run` 用 `Task.Run(...).GetAwaiter().GetResult()` 避免了 SynchronizationContext 死鎖,但代價是「當前 thread 阻塞 + thread pool 借一個 thread 跑 work」**雙倍 thread 佔用**;Blazor Server 用 thread pool 處理 SignalR,同步 connector 會直接砍半並發能力。
3. **同步版毫無獨立邏輯**。既有 `GetList` 同步版只是 `SyncExecutor.Run(() => GetListAsync(...))` 一行包裝,功能 0 損失。
4. **API 表面減半**。4 個 action × 2 模 = 8 個 method → 改為 4 個,IDE 補完、文件、IntelliSense 干擾少。
5. **桌面端也應 async-first**。.NET 6+ WinForms / WPF / MAUI 都支援 async UI,bee.ui.core 沒有理由再用同步呼叫。

> **既有同步版維持不動**(`GetListAsync` + `GetList`、`LoginAsync` + `Login` 等)。
> 整個 repo 真正使用 connector 同步版的 prod code 只有 3 處(`ApiConnectValidator.Ping`、
> `RemoteDefineAccess.SaveDefine`/`GetDefine`、加上各種 BO 同名方法);完整清理(包含同步版打
> `[Obsolete]`、`IDefineAccess` async 化、整條 BO/Repository/Db chain 遷移)交由獨立計畫
> **`docs/plans/plan-deprecate-sync-api.md`**(預計起草)處理,不卡本計畫。
> **本計畫確立「新加的 connector method 預設 async-only」為今後慣例。**

### 1.4 是否新增泛用 `ExecFuncAsync` strongly-typed 變體

**不新增**。`FormApiConnector` 已經有 `ExecFuncAsync(ExecFuncRequest)`([src/Bee.Api.Client/Connectors/FormApiConnector.cs:60](../../src/Bee.Api.Client/Connectors/FormApiConnector.cs#L60));CRUD 改用各自 strongly-typed `GetNewDataAsync` / `GetDataAsync` / `SaveAsync` / `DeleteAsync`,理由:
- 強型別 Action 比 `ExecFunc("Save", parameters)` 對使用者更明確(IDE 自動補齊、參數型別檢查、回傳值型別不需 cast)。
- `ExecFunc` 留作框架擴充機制,讓使用者寫**自訂業務邏輯**(對應 [development-cookbook.md §ExecFunc Custom Function Pattern](../development-cookbook.md#execfunc-custom-function-pattern));CRUD 屬於框架核心 action,不該擠進 `ExecFunc` 命名空間。

---

## 2. Args / Result / Request / Response DTO 設計

### 2.1 跨層命名慣例

對照 `GetList` 既有四件套(命名 + 所屬專案):

| 層 | BO 端(`Bee.Business.Form`) | Wire 端(`Bee.Api.Core.Messages.Form`) | Contract 介面(`Bee.Api.Contracts`) |
|----|---------------------------|--------------------------------------|-----------------------------------|
| 輸入 | `GetListArgs : BusinessArgs, IGetListRequest` | `GetListRequest : ApiRequest, IGetListRequest` | `IGetListRequest` |
| 輸出 | `GetListResult : BusinessResult, IGetListResponse` | `GetListResponse : ApiResponse, IGetListResponse` | `IGetListResponse` |

新增 4 套全部沿用此模式,共需:

```
Bee.Business.Form/
  GetNewDataArgs.cs   GetNewDataResult.cs
  GetDataArgs.cs      GetDataResult.cs
  SaveArgs.cs         SaveResult.cs
  DeleteArgs.cs       DeleteResult.cs

Bee.Api.Core.Messages.Form/
  GetNewDataRequest.cs   GetNewDataResponse.cs
  GetDataRequest.cs      GetDataResponse.cs
  SaveRequest.cs         SaveResponse.cs
  DeleteRequest.cs       DeleteResponse.cs

Bee.Api.Contracts/
  IGetNewDataRequest.cs   IGetNewDataResponse.cs
  IGetDataRequest.cs      IGetDataResponse.cs
  ISaveRequest.cs         ISaveResponse.cs
  IDeleteRequest.cs       IDeleteResponse.cs
```

> `ApiInputConverter` / `ApiOutputConverter` 走「命名相同就反射對拷」的慣例
> ([src/Bee.Api.Core/Conversion/ApiOutputConverter.cs:74](../../src/Bee.Api.Core/Conversion/ApiOutputConverter.cs#L74)),
> 因此 wire 與 BO 兩邊**屬性名稱必須完全一致**(包含大小寫)。命名一致性由 contract 介面強制。

### 2.2 必要欄位

> `ProgId` **不重複放進 Args/Request**。`JsonRpcRequest.Method = "{progId}.{action}"` 已攜帶 progId
> ([src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:127](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs#L127)),
> `FormBusinessObject.ProgId` 在建構時注入,Args 內不需要也不該再帶。

#### 2.2.1 `GetNewData`(新增存檔的第 1 步)

```csharp
public interface IGetNewDataRequest
{
    /// 是否一併產出 Detail tables(空 table,structure 完整);false 時只回 Master。
    bool IncludeDetails { get; }
}

public interface IGetNewDataResponse
{
    /// 完整 DataSet skeleton:
    /// - Master:1 row,RowState = Added,欄位依 FormSchema 填預設值,sys_rowid 預先由 server 端產出
    /// - Detail(IncludeDetails == true):各 detail table structure 完整、無 row
    DataSet DataSet { get; }
}
```

**為何 server 端產 `sys_rowid` 而不留給 client?**

讓 client 在新增 Detail row 時可直接 reference Master 的 `sys_rowid` 當 FK,
不用等 `Save` 完成才能拿到新 ID 補 Detail。這個設計讓「新增 Master + 同時新增多筆 Detail」變成單一 `Save` round-trip。

**為何要把「取空白」做成 server 端 action,而不是 client 自行用 FormSchema 組?**

- Client 端組 skeleton 需要先取 FormSchema(往返 1 次),server 端組 skeleton 則直接組好回傳(往返 1 次),round-trip 數相同。
- 預設值可能含 server-context 資訊(建立者 = current user、預設部門 = session.CompanyId、外部序號)—— client 端組不出來。
- 統一由 server 組,日後若加上 server-side 預設值(如審核流程的預設狀態欄位)無需修改 client。

#### 2.2.2 `GetData`(修改存檔的第 1 步)

```csharp
public interface IGetDataRequest
{
    /// 主檔列鍵值(sys_rowid:Guid)。
    Guid RowId { get; }

    /// 是否一併載入 Detail tables;false 時只回 Master。
    bool IncludeDetails { get; }

    /// 限定載入哪些 Detail(table 名清單);null 表示全部。
    string[]? DetailTables { get; }

    /// 欄位投影,逗號分隔;空字串走 FormSchema.EditFields → all。
    string SelectFields { get; }
}

public interface IGetDataResponse
{
    /// Master + Detail 全部裝入單一 DataSet 回傳。
    /// Master 表名 == ProgId(框架不變式);Detail 各自以 schema.DetailTables 的名稱為 key。
    /// 所有 row 載入後 AcceptChanges,RowState = Unchanged。
    DataSet? DataSet { get; }
}
```

#### 2.2.3 `Save`(新增與修改共用的第 2 步)

```csharp
public interface ISaveRequest
{
    /// 完整 DataSet(Master 與所有 Detail tables)。
    /// 每個 row 依其 RowState 決定執行哪條 SQL:
    /// - Added    → INSERT
    /// - Modified → UPDATE
    /// - Deleted  → DELETE
    /// - Unchanged / Detached → 略過
    /// 若整份 DataSet 無任何變更,Save 拋 InvalidOperationException(不靜默 no-op,避免呼叫端誤判)。
    DataSet DataSet { get; }
}

public interface ISaveResponse
{
    /// 寫回後重新查得的 DataSet:
    /// - 把 server-side trigger / DB default / 計算欄位(timestamp、版本號、sys_modified_time …)併入
    /// - 已刪除的 row 不再出現
    /// - 所有保留 row 的 RowState 重置為 Unchanged
    /// Client 應以此份 DataSet 替換本地副本。
    DataSet? DataSet { get; }

    /// 各 table 實際受影響的列數(table 名 → 列數),便於呼叫端統計與日誌。
    /// 例如:{ ["Employee"] = 1, ["EmployeeDept"] = 3 } 表示 Master 1 列、Detail 3 列。
    Dictionary<string, int> AffectedRows { get; }
}
```

> **`Save` 為何不分 `Insert` / `Update`?** 框架不變式:row state 是 single source of truth。
> 拆成兩個 action 後 client 仍需自己判斷該打哪個 endpoint,等於把「判斷」這件事推給呼叫端;
> 統一走 `Save` 由 server 依 row state 派發,呼叫端 UI 變成「按下儲存」一行到底。

#### 2.2.4 `Delete`(直接刪除)

```csharp
public interface IDeleteRequest
{
    /// 要刪除的主檔列鍵值(批次刪除留待 Phase 1)。
    Guid RowId { get; }
}

public interface IDeleteResponse
{
    /// 實際被刪除的列數(0 表 row 已不存在;呼叫端可決定是否視為錯誤)。
    int RowsAffected { get; }
}
```

> **為何 `Save` 已能處理 Delete,還要保留獨立 `Delete` action**?
> - 場景:UI「刪除按鈕」直接帶 RowId,不必先 `GetData` 取整份 DataSet 才能刪。
> - 1 次 round-trip vs 2 次 round-trip 的差異。
> - 與「Master + Detail 中刪除某些 Detail row」場景(走 `Save`,以 row state = Deleted 表達)互不衝突。

### 2.3 MessagePack 序列化注意事項

- 所有 `*Request` / `*Response` 加 `[MessagePackObject]` + 屬性 `[Key(N)]`,N 從 100 開始(對照 [src/Bee.Api.Core/Messages/Form/GetListRequest.cs](../../src/Bee.Api.Core/Messages/Form/GetListRequest.cs))。
- `DataSet` / `DataTable` 已有 `DataSetFormatter` / `DataTableFormatter` 註冊在 composite resolver([src/Bee.Api.Core/MessagePack/](../../src/Bee.Api.Core/MessagePack/)),不需自寫 formatter。
- `Guid` 為 MessagePack 內建型別,直接使用。
- `Dictionary<string, int>`(`SaveResponse.AffectedRows`)為 MessagePack 內建支援。
- 跨版本相容:**不要重用已棄用的 `[Key(N)]`**,新欄位用新 N。

### 2.4 為何沿用 `System.Data.DataSet` 作為 wire format

- 既有 `DataSetFormatter` / `DataTableFormatter` 已支援(`Bee.Api.Core.MessagePack`)。
- BO 端 Repository 操作以 `DataRow` / `DataTable` 為基礎([src/Bee.Db/Dml/IFormCommandBuilder.cs:36](../../src/Bee.Db/Dml/IFormCommandBuilder.cs#L36));若 wire 改 POCO 反而要多一層映射。
- DataSet 自帶 `RowState`,正好對應 `Save` 派發到 `IFormCommandBuilder.Build{Insert,Update,Delete}`。
- **缺點**:DataSet 對非 .NET 客戶端不友善。本框架既定假設「客戶端皆為 .NET」,於 [docs/architecture-overview.md](../architecture-overview.md) 已確認;若日後出 JS / Python client,再加 POCO 視圖層轉換。

---

## 3. 路由:JsonRpcExecutor 自動派發,不動 `FormExecFuncHandler`

### 3.1 派發機制

[src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:82-92](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs#L82-L92) 的流程:

```
JsonRpcRequest.Method = "Employee.Save"
  → ParseMethod()           → (progId="Employee", action="Save")
  → CreateBusinessObject()  → FormBusinessObject(progId="Employee")
  → GetMethod()             → boType.GetMethod("Save")     ← 反射查表
  → ApiAccessValidator      → 檢查 [ApiAccessControl]
  → ApiInputConverter       → SaveRequest → SaveArgs(命名 convention 反射對拷)
  → MethodInfo.Invoke(bo, [args])
  → ApiOutputConverter      → SaveResult → SaveResponse
```

新增 action 只需在 `FormBusinessObject` 公開對應 `public virtual` method,
**executor 端 0 程式碼變更**。

### 3.2 `FormBusinessObject` 新方法樣板

```csharp
[ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
public virtual GetNewDataResult GetNewData(GetNewDataArgs args)
{
    ArgumentNullException.ThrowIfNull(args);
    var repository = CreateDataFormRepository(ProgId);
    var dataSet = repository.GetNewData(args.IncludeDetails);
    return new GetNewDataResult { DataSet = dataSet };
}

[ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
public virtual GetDataResult GetData(GetDataArgs args)
{
    ArgumentNullException.ThrowIfNull(args);
    var repository = CreateDataFormRepository(ProgId);
    var dataSet = repository.GetData(
        args.RowId, args.SelectFields, args.IncludeDetails, args.DetailTables);
    return new GetDataResult { DataSet = dataSet };
}

[ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
public virtual SaveResult Save(SaveArgs args)
{
    ArgumentNullException.ThrowIfNull(args);
    ArgumentNullException.ThrowIfNull(args.DataSet);
    var repository = CreateDataFormRepository(ProgId);
    var (refreshed, affected) = repository.Save(args.DataSet);
    return new SaveResult { DataSet = refreshed, AffectedRows = affected };
}

[ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
public virtual DeleteResult Delete(DeleteArgs args)
{
    ArgumentNullException.ThrowIfNull(args);
    var repository = CreateDataFormRepository(ProgId);
    var rowsAffected = repository.Delete(args.RowId);
    return new DeleteResult { RowsAffected = rowsAffected };
}
```

### 3.3 `ApiProtectionLevel` 預設

- CRUD 一律走 `Encrypted`:DataSet 可能包含個資/敏感欄位,且既有 `GetList` 已為 `Encrypted`(對照 [src/Bee.Business/Form/FormBusinessObject.cs:64](../../src/Bee.Business/Form/FormBusinessObject.cs#L64))。
- 若某 progId 需要降階(如公開查詢頁),日後以 `override` 個別覆蓋,不在框架預設層放寬。

### 3.4 `FormActions` 常數補齊

[src/Bee.Definition/FormActions.cs](../../src/Bee.Definition/FormActions.cs) 加入:

```csharp
public const string GetNewData = "GetNewData";
public const string GetData    = "GetData";
public const string Save       = "Save";
public const string Delete     = "Delete";
```

> 客戶端與測試一律以常數引用,字串只在常數定義出現一次,避免 typo。

---

## 4. `FormApiConnector` 對外 API

### 4.1 加入四個 async 方法(無同步包裝)

```csharp
// GetNewData — 新增存檔的第 1 步
public async Task<GetNewDataResponse> GetNewDataAsync(bool includeDetails = true)
{
    var req = new GetNewDataRequest { IncludeDetails = includeDetails };
    return await ExecuteAsync<GetNewDataResponse>(FormActions.GetNewData, req).ConfigureAwait(false);
}

// GetData — 修改存檔的第 1 步
public async Task<GetDataResponse> GetDataAsync(Guid rowId, bool includeDetails = true,
    string[]? detailTables = null, string selectFields = "")
{
    var req = new GetDataRequest { RowId = rowId, IncludeDetails = includeDetails,
                                   DetailTables = detailTables, SelectFields = selectFields };
    return await ExecuteAsync<GetDataResponse>(FormActions.GetData, req).ConfigureAwait(false);
}

// Save — 新增與修改共用的第 2 步
public async Task<SaveResponse> SaveAsync(DataSet dataSet)
{
    var req = new SaveRequest { DataSet = dataSet };
    return await ExecuteAsync<SaveResponse>(FormActions.Save, req).ConfigureAwait(false);
}

// Delete
public async Task<DeleteResponse> DeleteAsync(Guid rowId)
{
    var req = new DeleteRequest { RowId = rowId };
    return await ExecuteAsync<DeleteResponse>(FormActions.Delete, req).ConfigureAwait(false);
}
```

> 詳見 §1.3 對「Connector 端 async-only」的決策說明與既有同步版的處理策略。

### 4.2 與既有 `ExecFunc(Request)` 並存策略

- 既有 `ExecFunc` / `ExecFuncAsync` 不動,維持給使用者寫自訂方法用(包含既有同步 `ExecFunc` 不會被新計畫移除)。
- CRUD 4 個 action **不**透過 `ExecFunc` 包裝;呼叫端寫 `await connector.SaveAsync(ds)` 一行到底,不必先建 `ExecFuncRequest`。

### 4.3 為 Blazor `FormDataObject` 預留的呼叫面

對應 [plan-blazor-web-integration.md:188-215](plan-blazor-web-integration.md#L188-L215) 的 `LoadAsync` / `SaveAsync` / `DeleteAsync` / `NewAsync`:

| `FormDataObject` 方法 | Connector 呼叫 | 流程說明 |
|---------------------|----------------|---------|
| `NewAsync()` | `await connector.GetNewDataAsync()` | **新增存檔流程**第 1 步:取空白 DataSet 模板 |
| `LoadAsync(rowId)` | `await connector.GetDataAsync(rowId)` | **修改存檔流程**第 1 步:取現有 DataSet |
| `SaveAsync()` | `await connector.SaveAsync(DataSet)` | **新增 + 修改共用**第 2 步:儲存(由 DataSet row state 決定 INSERT/UPDATE) |
| `DeleteAsync()` | `await connector.DeleteAsync(rowId)` | 直接刪除(不需先 Load) |

> **`NewAsync` 改為走 server**(對比舊版規劃):server 端產 skeleton 才能拿到 sys_rowid 預填、session 相關預設值(建立者、部門)、與審核流程相關欄位。Client 端不再憑 FormSchema 自組 skeleton。

---

## 5. Repository 層

### 5.1 `IDataFormRepository` 介面擴充

[src/Bee.Repository.Abstractions/Form/IDataFormRepository.cs](../../src/Bee.Repository.Abstractions/Form/IDataFormRepository.cs) 加入:

```csharp
/// 產出一份「新建空白」的 DataSet 模板(Master 1 row Added、Detail 空 tables)。
DataSet GetNewData(bool includeDetails);

/// 依鍵值載入單筆完整 DataSet(Master + Detail);所有 row 進行 AcceptChanges。
DataSet GetData(Guid rowId, string selectFields, bool includeDetails, string[]? detailTables);

/// 依各 row 的 RowState 派發 INSERT/UPDATE/DELETE;回傳重新查得的 DataSet 與每張 table 的影響列數。
/// 整份 DataSet 無變更時拋 InvalidOperationException。
(DataSet Refreshed, Dictionary<string, int> AffectedRows) Save(DataSet dataSet);

/// 依鍵值刪除單筆(交易包覆 Master + Detail);回傳實際刪除的 Master 列數(通常 0 或 1)。
int Delete(Guid rowId);
```

### 5.2 雙軌策略

[plan-blazor-web-integration.md:81-82](plan-blazor-web-integration.md) 提到的雙軌:

| 軌道 | 適用情境 | 實作位置 |
|------|---------|---------|
| **FormSchema-driven** | 標準 CRUD(本計畫範圍) | `DataFormRepository.GetNewData/GetData/Save/Delete`,內部呼叫 `IFormCommandBuilder.Build*` |
| **BO 自實作(AnyCode)** | 報表/批次/複雜 join | BO 自建 `DbCommandSpec`,不經 `IDataFormRepository`;沿用既有 `ReportFormRepository` 模式 |

### 5.3 既有 helper 盤點(可直接接上)

| Helper | 路徑 | 用途 |
|--------|------|------|
| `IFormCommandBuilder.BuildSelect(...)` | [src/Bee.Db/Dml/IFormCommandBuilder.cs:21](../../src/Bee.Db/Dml/IFormCommandBuilder.cs#L21) | `GetData` 的 SELECT(套 filter) |
| `IFormCommandBuilder.BuildInsert(table, DataRow)` | 同上:36 | `Save` 處理 Added row 時呼叫 |
| `IFormCommandBuilder.BuildUpdate(table, DataRow)` | 同上:43 | `Save` 處理 Modified row 時呼叫 |
| `IFormCommandBuilder.BuildDelete(table, FilterNode)` | 同上:50 | `Save` 處理 Deleted row + 獨立 `Delete` action 共用 |
| `TableSchemaCommandBuilder.BuildUpdateSpec(DataTable)` | [src/Bee.Db/Dml/TableSchemaCommandBuilder.cs:165](../../src/Bee.Db/Dml/TableSchemaCommandBuilder.cs#L165) | 一次回三條 INSERT/UPDATE/DELETE,可直接餵 `DbAccess.UpdateDataTable(spec)` |
| `DbAccess.UpdateDataTable(DataTableUpdateSpec)` | [src/Bee.Db/DbAccess.cs:359](../../src/Bee.Db/DbAccess.cs#L359) | 跑 batch update,內含 transaction wrapping |
| `BusinessObject.CreateDataFormRepository(progId)` | [src/Bee.Business/BusinessObject.cs:81](../../src/Bee.Business/BusinessObject.cs#L81) | BO 取 repository 的單行入口 |

### 5.4 `Save` 內部寫入流程

`Save` 在 Repository 內部統一以「交易包覆 + 逐 table 處理」:

```csharp
public (DataSet, Dictionary<string, int>) Save(DataSet dataSet)
{
    if (!HasAnyChange(dataSet))
        throw new InvalidOperationException("DataSet 無任何變更;無需呼叫 Save。");

    var affected = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    using var conn = _connectionManager.CreateConnection(_databaseId);
    conn.Open();
    using var tx = conn.BeginTransaction();
    try
    {
        // Master 先寫(Added/Modified);若 Master 為 Deleted,則先處理 Detail 再 Master
        // (避免 FK 違規)。具體順序由 schema 與 RowState 決定。
        affected[_schema.MasterTableName] = ApplyChanges(dataSet.Tables[_schema.MasterTableName], tx);

        foreach (var detail in _schema.DetailTables)
            affected[detail.TableName] = ApplyChanges(dataSet.Tables[detail.TableName], tx);

        tx.Commit();
    }
    catch { tx.Rollback(); throw; }

    // 寫回後重新查得 server-side 計算欄位(timestamp / sys_no …)
    var rowId = ExtractMasterRowId(dataSet);
    var refreshed = GetData(rowId, "", includeDetails: true, detailTables: null);
    return (refreshed, affected);
}
```

> `ApplyChanges` 內部依 row state 派發到 `IFormCommandBuilder.Build{Insert,Update,Delete}` 並累計成功列數。
> 細部交易管理可改以 `IDbAccess` 抽象;具體實作交實作階段決定,計畫只先標出責任邊界。

### 5.5 `GetNewData` 內部組裝流程

```csharp
public DataSet GetNewData(bool includeDetails)
{
    var ds = new DataSet(ProgId);

    // 1. Master skeleton —— 由 FormSchema 反推 DataTable 結構
    var masterTable = BuildEmptyDataTable(_schema.MasterTable);
    var masterRow = masterTable.NewRow();
    ApplyDefaults(masterRow, _schema.MasterTable, _sessionInfo);
    masterRow["sys_rowid"] = Guid.NewGuid();        // server-side 預先產
    masterTable.Rows.Add(masterRow);                 // RowState = Added
    ds.Tables.Add(masterTable);

    // 2. Detail tables(skeleton,structure 完整、無 row)
    if (includeDetails)
        foreach (var detail in _schema.DetailTables)
            ds.Tables.Add(BuildEmptyDataTable(detail));

    return ds;
}
```

> 預設值來源優先序:`FormField.DefaultValue`(schema 定義)> session-context 補值(建立者、部門)>
> CLR 型別預設值(`string.Empty`、`0`、`DateTime.MinValue` 等)。
> 細部 default-resolver 可獨立成 helper,實作階段決定。

---

## 6. 驗證流程(兩層 round-trip 測試)

對應 `bee-add-bo-method` skill 的測試模板:

### 6.1 Wire 層 MessagePack round-trip(快,純記憶體)

每個新 wire 型別配一個檔,放 `tests/Bee.Api.Core.UnitTests/Form/`:

| 測試檔 | 驗證點 |
|--------|--------|
| `GetNewDataMessagePackTests.cs` | `GetNewDataRequest`/`Response` round-trip,空 skeleton DataSet 的 structure 完整還原 |
| `GetDataMessagePackTests.cs` | `GetDataRequest`/`Response` round-trip,DataSet 內 Master + Detail 還原(row state 為 Unchanged) |
| `SaveMessagePackTests.cs` | 三種 RowState(Added/Modified/Deleted)混合的 DataSet 還原 + `AffectedRows` Dictionary |
| `DeleteMessagePackTests.cs` | 單一 `RowId` + `RowsAffected` 欄位的最小 round-trip |

對照範本 [tests/Bee.Api.Core.UnitTests/Form/GetListMessagePackTests.cs](../../tests/Bee.Api.Core.UnitTests/Form/GetListMessagePackTests.cs)。

> **關鍵驗證點**:`DataSet` 經 MessagePack 還原後 `RowState` 是否能正確還原。若 formatter 把
> 所有 row 還原為 `Unchanged`,`Save` 端就無法依 row state 派發 —— 這在 P1 就要發現,不能等到
> DB 整合測試。

### 6.2 JsonRpcExecutor end-to-end(中,Stub Repository)

每個 action 一個檔,放同目錄:

| 測試檔 | 驗證點 |
|--------|--------|
| `GetNewDataJsonRpcRoundTripTests.cs` | `Employee.GetNewData` → BO → stub repo 回固定 skeleton DataSet → wire 回 `GetNewDataResponse` |
| `GetDataJsonRpcRoundTripTests.cs` | `Employee.GetData` → BO → stub repo 回固定 DataSet → wire 回 `GetDataResponse` |
| `SaveJsonRpcRoundTripTests.cs` | `ApiInputConverter` 把 `SaveRequest.DataSet` 對拷到 `SaveArgs.DataSet`,row state 保留 |
| `DeleteJsonRpcRoundTripTests.cs` | 驗證 `RowId` 與 `RowsAffected` 經 wire 還原 |

對照範本 [tests/Bee.Api.Core.UnitTests/Form/GetListJsonRpcRoundTripTests.cs](../../tests/Bee.Api.Core.UnitTests/Form/GetListJsonRpcRoundTripTests.cs)。

### 6.3 BO 端含實體 DB(慢,`[DbFact]`)

放 `tests/Bee.Business.UnitTests/Form/`:

| 測試檔 | 驗證點 |
|--------|--------|
| `FormBusinessObjectGetNewDataTests.cs` | `[DbFact]` 取 skeleton → 確認 sys_rowid 已預填、預設值正確 |
| `FormBusinessObjectGetDataTests.cs` | `[DbFact]` Save 一筆 → GetData 拿回完整 DataSet,欄位值比對 |
| `FormBusinessObjectSaveTests.cs` | `[DbFact]` 三種情境:全新 Added、Modified 寫回、Deleted 刪除;Master + Detail 混合 row state |
| `FormBusinessObjectDeleteTests.cs` | `[DbFact]` Save 一筆 → Delete → SELECT 應為空 |

對照範本 [tests/Bee.Business.UnitTests/Form/FormBusinessObjectGetListTests.cs](../../tests/Bee.Business.UnitTests/Form/FormBusinessObjectGetListTests.cs)。
未設 `BEE_TEST_CONNSTR_*` 的 DB 會自動跳過(見 [.claude/rules/testing.md](../../.claude/rules/testing.md))。

### 6.4 Connector 端建構/參數驗證(快,純邏輯)

放 `tests/Bee.Api.Client.UnitTests/`,延伸 [FormApiConnectorTests.cs](../../tests/Bee.Api.Client.UnitTests/FormApiConnectorTests.cs):

- 各 async 方法在 `RowId == Guid.Empty` / `dataSet == null` 等邊界的參數驗證(假設我們在 connector 做 fail-fast)
- 確認沒有「順手」加同步包裝(本計畫明確 async-only,測試應守住這個邊界)

### 6.5 全流程整合驗證(在 BO `[DbFact]` 測試中)

至少一個檔覆蓋「**新增存檔**」與「**修改存檔**」兩個完整流程,證明 4 個 action 串起來正確:

```csharp
[DbFact(DatabaseType.SQLServer)]
[DisplayName("新增存檔流程:GetNewData → 修改欄位 → Save → GetData 比對")]
public void NewAndSaveFlow_RoundTrip()
{
    var bo = new FormBusinessObject(...);
    var skeleton = bo.GetNewData(new GetNewDataArgs { IncludeDetails = true }).DataSet;
    skeleton.Tables["Employee"].Rows[0]["sys_name"] = "測試員工";

    var saved = bo.Save(new SaveArgs { DataSet = skeleton }).DataSet;
    var rowId = (Guid)saved.Tables["Employee"].Rows[0]["sys_rowid"];

    var reloaded = bo.GetData(new GetDataArgs { RowId = rowId }).DataSet;
    Assert.Equal("測試員工", reloaded.Tables["Employee"].Rows[0]["sys_name"]);
}

[DbFact(DatabaseType.SQLServer)]
[DisplayName("修改存檔流程:GetData → 修改欄位 → Save → GetData 比對")]
public void LoadAndSaveFlow_RoundTrip() { ... }
```

---

## 7. 檔案盤點:本計畫實際需新增/修改的檔案

> **計畫階段不寫程式**,僅列出實作階段的工作量。

### 7.1 新增(共 ~28 個)

```
src/Bee.Api.Contracts/
  IGetNewDataRequest.cs   IGetNewDataResponse.cs
  IGetDataRequest.cs      IGetDataResponse.cs
  ISaveRequest.cs         ISaveResponse.cs
  IDeleteRequest.cs       IDeleteResponse.cs

src/Bee.Api.Core/Messages/Form/
  GetNewDataRequest.cs    GetNewDataResponse.cs
  GetDataRequest.cs       GetDataResponse.cs
  SaveRequest.cs          SaveResponse.cs
  DeleteRequest.cs        DeleteResponse.cs

src/Bee.Business/Form/
  GetNewDataArgs.cs       GetNewDataResult.cs
  GetDataArgs.cs          GetDataResult.cs
  SaveArgs.cs             SaveResult.cs
  DeleteArgs.cs           DeleteResult.cs

tests/Bee.Api.Core.UnitTests/Form/
  GetNewDataMessagePackTests.cs    GetNewDataJsonRpcRoundTripTests.cs
  GetDataMessagePackTests.cs       GetDataJsonRpcRoundTripTests.cs
  SaveMessagePackTests.cs          SaveJsonRpcRoundTripTests.cs
  DeleteMessagePackTests.cs        DeleteJsonRpcRoundTripTests.cs

tests/Bee.Business.UnitTests/Form/
  FormBusinessObjectGetNewDataTests.cs
  FormBusinessObjectGetDataTests.cs
  FormBusinessObjectSaveTests.cs
  FormBusinessObjectDeleteTests.cs
  FormBusinessObjectCrudFlowTests.cs   ← §6.5 的整合流程測試
```

### 7.2 修改(7 個)

```
src/Bee.Definition/FormActions.cs                       (+ 4 const)
src/Bee.Business/Form/IFormBusinessObject.cs            (+ 4 method)
src/Bee.Business/Form/FormBusinessObject.cs             (+ 4 method)
src/Bee.Api.Client/Connectors/FormApiConnector.cs       (+ 4 async,新慣例不加同步包裝)
src/Bee.Repository.Abstractions/Form/IDataFormRepository.cs  (+ 4 method)
src/Bee.Repository/Form/DataFormRepository.cs           (+ 4 method 實作)
tests/Bee.Api.Client.UnitTests/FormApiConnectorTests.cs (+ 邊界測試)
```

### 7.3 **不動**

```
src/Bee.Business/Form/FormExecFuncHandler.cs   ← CRUD 不走 ExecFuncHandler
src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs    ← 反射派發自動接住新 action
src/Bee.Api.Core/Conversion/*                  ← 命名 convention 自動對映
src/Bee.Api.AspNetCore/Controllers/*           ← HTTP 端透通,無需改
```

---

## 8. 與 [plan-blazor-web-integration.md](plan-blazor-web-integration.md) 的銜接點

| 本計畫產出 | Blazor 計畫消費點 |
|----------|------------------|
| `connector.GetNewDataAsync()` | `FormDataObject.NewAsync()`(新增存檔流程第 1 步) |
| `connector.GetDataAsync(rowId)` | `FormDataObject.LoadAsync(queryArgs)`(修改存檔流程第 1 步) |
| `connector.SaveAsync(ds)` | `FormDataObject.SaveAsync()`(新增 + 修改共用第 2 步) |
| `connector.DeleteAsync(rowId)` | `FormDataObject.DeleteAsync()` |
| `GetNewData/GetDataResponse.DataSet` | `FormDataObject.DataSet` 賦值來源 |
| `SaveResponse.DataSet` | server-generated 欄位(`sys_no`、`sys_creator`、`sys_modified_time` …)寫回 |
| `SaveResponse.AffectedRows` | 提供 UI「成功儲存 N 筆」之類訊息 |

本計畫完成後,`plan-blazor-web-integration.md` 的 `FormDataObject` 即可直接呼叫 connector,
無需在 Blazor 端再 workaround。

---

## 9. 未在本計畫範圍內(留待 Phase 1+)

- **批次 Save / Delete**(`SaveMany` / `DeleteByFilter`):需要設計 transaction 邊界,本期暫不開。
- **Soft delete**:目前 `Delete` 與 `Save` 處理的 row state = Deleted 皆為硬刪;若有需求改以 BO 層判斷 schema 是否有 `sys_deleted` 旗標,在 BO 層攔截改為 update,**不在 Repository 加新 method**。
- **Optimistic concurrency**(`Save` 帶 `rowVersion` 檢查):需要 schema 標註版本欄位,先收進待辦,不在本期 BO 簽名加參數(避免之後 API 又改)。
- **GetData 帶 multi-row** / **bulk download**:統一走 `GetList`,不在 `GetData` 多載。
- **server-side default 進階規則**(如審核流程預設狀態、產生外部序號):本期 `GetNewData` 僅吐 FormSchema 預設值 + sys_rowid + session-context 補值;進階預設值規則交由後續 schema-driven default-resolver 統一處理。
- **單元測試以外的 e2e(走真實 HTTP)**:由 `Bee.Api.AspNetCore` 既有 Controller 測試覆蓋,本期不新增。
- **既有 connector 同步方法清理**(13~15 個 method):另起 `plan-deprecate-sync-api.md`(用新 session 起草)處理,不在本計畫範圍。本計畫只確立「新加 connector method 預設 async-only」的慣例,未動既有同步版。

  **已知架構觀察(為下次起 plan 預留 anchor)**:`RemoteDefineAccess` 是 sync-over-async 的「同心圓中心」—— 它呼叫 `Connector.GetDefine` / `SaveDefine` 同步版,只是因為 `IDefineAccess` 介面要求同步。若**`IDefineAccess` 維持 sync 不動**,只需:
    1. `RemoteDefineAccess` 內部用 `SyncExecutor.Run(() => Connector.XxxAsync(...))` 包覆,把 sync-over-async **集中在這一個檔**
    2. 移除 `SystemApiConnector` / `FormApiConnector` / `ApiConnector` 的 13~15 個同步方法
    3. BO / Repository / Db chain **完全不動**,bee.ui.core 桌面端也不受影響

  對比「async 化整條 chain」方案,工程量壓縮顯著(從整條 chain → 2 個檔);且 `RemoteDefineAccess` 有快取,`SyncExecutor.Run` 只在 cache miss 觸發,實務性能影響可忽略。`IDefineAccess` 設計為 sync 有其脈絡(本地檔案 access、快取吸收 HTTP 延遲),強行 async 化反而 over-engineering。下次起 plan 時建議以此方案為 baseline。

---

## 10. 計畫完成後的 Checklist

實作收尾時逐項勾選:

- [ ] `FormActions` 4 個 const(`GetNewData` / `GetData` / `Save` / `Delete`)加齊
- [ ] 8 個 contract interface 加齊,屬性命名與 BO/wire 完全一致
- [ ] 8 對 wire `Request`/`Response` 加齊,全部 `[MessagePackObject]` + `[Key(N)]`
- [ ] 8 對 BO `Args`/`Result` 加齊,實作對應 contract interface
- [ ] `IFormBusinessObject` 與 `FormBusinessObject` 4 個 method 加齊,全部標 `[ApiAccessControl(Encrypted, Authenticated)]`
- [ ] `IDataFormRepository` 與 `DataFormRepository` 4 個 method 加齊,`Save` 內部 row-state 派發 + transaction 包覆
- [ ] `FormApiConnector` 4 個 async 方法加齊(無同步包裝;確認與 §1.3 決策一致)
- [ ] Wire 層 MessagePack round-trip 測試 4 個檔加齊,**特別確認 `DataSet.RowState` 經序列化還原**
- [ ] JsonRpcExecutor round-trip 測試 4 個檔加齊,皆 pass
- [ ] BO 端 `[DbFact]` 測試 4 個檔 + §6.5 整合流程測試 1 檔加齊,於本機可連線的 DB 跑 pass、未連線 DB 正確 skip
- [ ] `FormApiConnectorTests` 邊界測試加齊
- [ ] `dotnet build --configuration Release` 與 `./test.sh` 全 pass
- [ ] `docs/architecture-overview.md` / `docs/development-cookbook.md` 若涉及 CRUD 慣例,同步補上一節對應說明(若有雙語版本同步更新 `.zh-TW.md`)
- [ ] 計畫文件頂部狀態列改為 `**狀態:✅ 已完成(YYYY-MM-DD)**`
