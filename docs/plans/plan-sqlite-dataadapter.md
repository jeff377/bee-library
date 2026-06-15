# 計畫：自製 SQLite 專用 DataAdapter，統一 SQLite 的讀寫路徑

**狀態：✅ 已完成（2026-06-15，待 review；決策 D1=Factory 包裝、D2=讀寫都收斂、D3=移除 manual fallback）**

實作摘要：
- 新增 [SqliteDataAdapter](../../src/Bee.Db/Providers/Sqlite/SqliteDataAdapter.cs)（`: DbDataAdapter`，4 個樣板成員）
  與 [SqliteProviderFactory](../../src/Bee.Db/Providers/Sqlite/SqliteProviderFactory.cs)（包裝 inner Sqlite factory、
  override `CreateDataAdapter`；inner 以抽象 `DbProviderFactory` 傳入，Bee.Db 不需引用 Microsoft.Data.Sqlite）。
- 3 處註冊改為 `new SqliteProviderFactory(SqliteFactory.Instance)`（NorthwindBackend / DemoBackend / SharedDatabaseState）。
- `DbAccess`：寫入 `ApplySpec` 移除 manual fallback（`ApplySpecManually`/`BindRowParameters`/`ResolveRowVersion` 刪除），
  改 adapter-only（`?? throw`）+ 命令設 `UpdatedRowSource = None`；同步讀 `ExecuteDataTableCore` 移除 DataReader fallback，
  改 `Fill`（async 讀仍走 reader.Load，DbDataAdapter 無 FillAsync）。
- 驗證：`./test.sh` 全 15 專案綠（含全部 SQLite `[DbFact]` 讀經 Fill、寫經 adapter）；Northwind 六情境實機通過。

## 背景

`Microsoft.Data.Sqlite` 是 5 個註冊 provider 中**唯一**不提供 `DbDataAdapter` 的
（`SqliteFactory.CreateDataAdapter()` 回 null）。框架因此對 SQLite 走了兩條「無 adapter fallback」：

- **讀**：`DbAccess.ExecuteDataTableCore` 偵測無 adapter → 改用 `DbDataReader` + `DataTable.Load`。
- **寫**：`DbAccess.ApplySpec`（本次 Save 修正新增）偵測無 adapter → `ApplySpecManually` 逐列套用
  預建命令、用 `SourceColumn`/`SourceVersion` 手動綁值（等同自己重做一遍 DataAdapter 的內部邏輯）。

本計畫評估：**自製一個 `SqliteDataAdapter : DbDataAdapter`**，讓 SQLite 也有 adapter，
從而統一回 adapter 路徑、移除上述兩條 bespoke fallback。

> 注意：這是**可選的精簡 / 標準化**，不是修 bug——目前 `ApplySpecManually` 已可正常運作且有測試。
> 價值在於：改用「久經驗證的 base `DbDataAdapter.Update`/`Fill`」取代自寫綁定，並把讀寫 fallback 收斂成一處。

## 可行性（已查證）

- `DbDataAdapter` 為抽象類別，但 `Update(DataTable)` / `Fill(DataTable)` 的核心邏輯都在 base 實作；
  自訂子類別只需補 4 個 protected 樣板成員：
  `CreateRowUpdatingEvent` / `CreateRowUpdatedEvent` / `OnRowUpdating` / `OnRowUpdated`（外加對應 event）。
- `DbDataAdapter` 已公開 `InsertCommand` / `UpdateCommand` / `DeleteCommand` / `SelectCommand`
  （`DbCommand?` 型別）——現有 `UpdateDataTable` 程式碼即直接設定它們並在 SQL Server 上跑通，故
  自訂 adapter 設好這四個命令後，base `Update` 會自動依 RowState 派發、依 SourceColumn/SourceVersion
  綁值（Deleted→Original、Added→Current 等版本處理由 base 負責，等同取代我們手寫的 `ResolveRowVersion`）。
- `Microsoft.Data.Sqlite` 的 `DbCommand`/`DbParameter`/`DbConnection` 都是標準 ADO.NET 型別，
  與 base adapter 相容；它不附 adapter 是「刻意精簡」而非技術阻礙。

**結論：技術上可行，約 50–70 行樣板。**

## 整合方式（待確認決策）

### D1 — 框架怎麼取得這個自訂 adapter？

- **(a)【建議】Factory 包裝**：做一個 `BeeSqliteProviderFactory : DbProviderFactory`，
  `CanCreateDataAdapter => true`、`CreateDataAdapter() => new SqliteDataAdapter()`，
  其餘成員（CreateConnection/CreateCommand/CreateParameter…）全部委派 `SqliteFactory.Instance`。
  host 端把 `DbProviderRegistry.Register(SQLite, SqliteFactory.Instance)` 改成註冊這個包裝。
  - **優點**：`Provider.CreateDataAdapter()` 直接回非 null → **DbAccess 完全不用改**，
    讀（Fill）寫（Update）兩條路徑自動都走 adapter；可移除 `ApplySpecManually` 與 SELECT 的 DataReader fallback。
  - **代價**：改 3–4 處註冊呼叫（NorthwindBackend、DemoBackend、SharedDatabaseState、其他 sample/app）。
    框架可提供 `BeeSqliteProviderFactory.Instance` 供統一引用。
- **(b) Adapter 註冊表 / inline**：維持註冊 `SqliteFactory.Instance`，另加
  `Provider.CreateDataAdapter() ?? FrameworkAdapter(DatabaseType)` 的取得點（或一張
  `DbType→Func<DbDataAdapter>` 表）。host 不用改，但 DbAccess 要加判斷、且讀路徑的 fallback 要一併改走 adapter。

### D2 — 範圍：只統一「寫」，還是「讀寫」都收斂？

- **(a)【建議】讀寫都收斂**：有了 adapter，`Fill` 也能用，移除 `ExecuteDataTableCore` 的 DataReader fallback
  與 `ApplySpecManually`，SQLite 與其他 provider 走同一路徑。最一致，但要驗 `Fill` 的 schema 對映行為。
- **(b) 只統一寫**：先把寫換成 adapter、移除 `ApplySpecManually`；讀維持現有 DataReader fallback（它本來就穩）。
  改動小、風險低。

### D3 — 是否保留 `ApplySpecManually` 作為「終極 fallback」？

- **(a)【建議】移除**：自訂 adapter 後 SQLite 也有 adapter，5 provider 全有，fallback 無觸發者 → 刪掉減重。
- **(b) 保留**：萬一未來引入別的無-adapter provider 仍有保底。但目前 0 觸發者、屬死碼。

## 風險

- **R1 — base `Update` 的 identity/RowUpdated 行為**：base `DbDataAdapter` 在 `OnRowUpdated` 後可能嘗試取回
  自增鍵（`UpdateRowSource`）。我們的命令不需回填 `sys_no`（AutoIncrement，Save 後另以 `GetData` 重撈），
  須確認 `UpdateCommand.UpdatedRowSource = UpdatedRowSource.None` 以免 base 嘗試讀回結果集而出錯。
- **R2 — `Fill` 的 schema/型別對映**（僅 D2=a 時）：base `Fill` 用 `SelectCommand` 自行建欄位；
  須驗證與既有 `DataTable.Load` 路徑在欄位型別、`UppercaseColumnNames`、Guid/Date 對映上一致。
- **R3 — 註冊面遺漏**（D1=a）：若某個 host/sample 仍註冊原始 `SqliteFactory.Instance`，該處會落回
  「無 adapter」。需全 repo 掃 `Register(DatabaseType.SQLite, SqliteFactory.Instance)` 全部換掉，
  並在無-adapter 寫入時給明確錯誤（或保留 manual fallback 一段過渡——與 D3 取捨相關）。

## 實作範圍（待 D1–D3 確認後）

1. `src/Bee.Db/Providers/Sqlite/SqliteDataAdapter.cs`：`: DbDataAdapter` + 4 個 protected 樣板 + event。
2. （D1=a）`src/Bee.Db/Providers/Sqlite/BeeSqliteProviderFactory.cs`：包裝 `SqliteFactory.Instance`，
   override `CanCreateDataAdapter`/`CreateDataAdapter`。
3. 改各 host 註冊（NorthwindBackend / DemoBackend / SharedDatabaseState / 其他）為新 factory。
4. 設定 I/U/D 命令的 `UpdatedRowSource = None`（在 TableSchemaCommandBuilder 或 adapter 套用時）。
5. （D2/D3）移除 `ApplySpecManually` / `ResolveRowVersion` /（選）SELECT DataReader fallback。
6. 測試：既有 `UpdateDataTablesManualApplyTests` 改名/改為驗 adapter 路徑；補 SQLite Fill round-trip；
   Northwind 六情境實機回歸。

## 驗證

- `./test.sh` 全綠（特別是 `Bee.Db` / `Bee.Repository` / `Bee.Business` 的 SQLite `[DbFact]`）。
- Northwind（SQLite）六情境：新增、改主表、改明細、刪明細、no-op 重存、刪明細+no-op sibling。

## 不在範圍

- 不改其他 provider 的 adapter 行為（它們本就有 adapter）。
- 不動 `UpdateDataTables` 的多表共享交易設計（與本計畫正交）。
