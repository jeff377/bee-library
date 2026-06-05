# ADR-017：資料庫快取相依/失效機制（通知表 + 輪詢 + 慣例分派）

## 狀態

已採納（2026-06-01）

## 背景

`Bee.ObjectCaching` 為 in-process 快取（[ADR-009](adr-009-cache-implementation.md)）。落地時失效機制只有兩種：

1. `IDefineAccess.SaveX() → _cache.XXX.Remove()` —— 主動失效，但**只在發生寫入的那個 process 生效**。
2. XML 定義檔的 `ChangeMonitorFilePaths`（file-watch）—— 只涵蓋**檔案來源**的快取，且依賴共享檔案系統。

兩個缺口：

- **由資料庫載入的快取**（`CompanyInfoCache`、未來的組織快取等）沒有「來源資料異動 → 跨 process/節點重取」的通道。
- **定義改存資料庫後**（[ADR-018](adr-018-db-define-storage.md) 的 `DbDefineStorage`）沒有檔案可 watch，file-watch 這條免費的跨 process 失效信號隨之消失。

多節點 / 雲端部署不應依賴共享檔案系統。需求：當來源資料（可能跨多張表）異動、且**應用程式判斷該異動有意義**時，能通知（可能在別的 process / 別的節點上的）對應快取重置。

## 決策

採「**資料庫通知表 + 各節點輪詢 + 版本號冪等 + 慣例式分派**」：

1. **通知表 `st_cache_notify`**（單表三欄，一個邏輯快取 key 一列、UPSERT）：
   - `cache_key`(PK) = `"群組:實體"` 慣例字串（如 `"OrgInfo:0001"`、`"FormSchema:Employee"`、單物件快取 `"SystemSettings:*"`）。
   - `cache_version`(bigint) = 每-key 單調自增版本號。
   - `sys_update_time` = DB 伺服器時間，增量抓取游標（欄名 `sys_` 前綴因其為 `SysFields.UpdateTime` 系統欄；`cache_key`/`cache_version` 非系統欄不加前綴，見 [database-naming-conventions](../database-naming-conventions.md)）。

2. **bump 原語 `ICacheNotifyService.Touch(cacheKey, transaction, databaseType)`**（置於 `Bee.Db`）：在**呼叫端傳入的同一 transaction** 內以單一 UPSERT 原子遞增 `cache_version`、刷新 `sys_update_time`。各方言用原生 UPSERT（PG/SQLite `ON CONFLICT`、MySQL `ON DUPLICATE KEY`、SQL Server / Oracle `MERGE`）。

3. **輪詢器 `CacheNotifyPoller : BackgroundService`**（置於 `Bee.Hosting`，`PeriodicTimer` 迴圈，預設 5 秒）：各節點持 in-memory 鏡像 `{cache_key → version}`，首輪只取基準游標不 evict；之後抓 `sys_update_time >= highWater - margin` 的列，`version` 變大才 evict，並推進 highWater。

4. **慣例式分派**（取代手動路由 registry）：每個快取實作 `IEvictableCache`（`CacheGroup` 預設 = 被快取型別名），`CacheContainerService` 自動建「群組 → 快取」表並提供 `ICacheContainer.TryEvict(cacheKey)`。poller 只丟 `container.TryEvict(cacheKey)`，依群組分派到對應快取 `Remove`。`Bee.ObjectCaching` 不需引用 DB 層。

### 核心不變式（凌駕一切實作便利）

1. **bump 必須與資料變更在同一 transaction 提交。** 否則通知先被輪詢看到、資料 commit 尚未可見 → reload 讀到舊值並標記為新鮮 → **永久 stale**。`Touch` 顯式收 `DbTransaction` 即保證此點。

2. **以 `version` 而非時間判定真異動。** `sys_update_time` 只負責「便宜地抓增量」；`cache_version` 單調自增 + 鏡像比對負責「冪等地判定」。delta 用 `>=` + 安全餘量 `margin` 重疊回看 → 不漏；`version` 沒變大就不 evict → 不重。兩者搭配在「同一時間刻度多筆異動」「長交易 update_time 早於 commit 可見」等邊界都正確。

3. **時間一律用 DB 伺服器時鐘，絕不用 app 端時鐘。** 寫入、highWater、threshold 三者全部同源（DB 時鐘）且全程不轉時區；多節點各自讀同一台 DB 的時鐘，避免節點時鐘偏移。DB 伺服器設為 **UTC+0** 時所有值即 UTC。機制不依賴 app 主機時區。

4. **失效動作為 evict（`Remove`）+ lazy 重載。** 沒人在讀的 key 不白白重載；複用既有 lazy `CreateInstance` / service 層 load-on-miss。`Remove` 對未快取 key 為 no-op。

5. **新增快取零註冊。** 群組名 = 型別名的慣例使任何加進 `ICacheContainer` 的快取自動可失效，不必維護路由表 —— 對 ERP 大量 DB 相依快取可擴展。

## 結果

### 元件與落點

| 元件 | 落點 |
|------|------|
| `st_cache_notify.TableSchema.xml` | `tests/Define/TableSchema/common/`（系統表 define 目錄） |
| `ICacheNotifyService` / `CacheNotifyService` | `Bee.Db`（與 `DbDefineStorage` 共同的最低交會點） |
| `IEvictableCache` / `ICacheContainer.TryEvict` | `Bee.ObjectCaching`（與快取註冊同層，無 DB 依賴） |
| `CacheNotifyPoller` / `CacheNotifyPollSession` | `Bee.Hosting`（`Microsoft.Extensions.Hosting.Abstractions`） |
| `CacheNotifyOptions` | `Bee.Definition.Settings`（`BackendConfiguration.CacheNotifyOptions`） |

### 設定（`BackendConfiguration.CacheNotifyOptions`）

| 鍵 | 預設 | 說明 |
|----|------|------|
| `Enabled` | `true` | 啟用 poller；純單一 process 單節點可關（本地寫入即時失效） |
| `IntervalSeconds` | `5` | 輪詢間隔。穩態每輪只是一筆走索引、多回 0 列的查詢，成本可忽略；此值本質是「跨節點失效延遲」旋鈕 |
| `MarginSeconds` | `5` | 增量重疊回看餘量，cover 長交易的殘留邊界 |
| `DatabaseId` | `common` | 被輪詢的通知表所在資料庫 |

> ⚠️ 「單機」不等於「單 process」：同機多 app pool / 多 process 各有獨立 in-memory 快取，仍需 poller 做跨 process 失效。只有確定**單一 process** 才停用。

### 韌性

`CacheNotifyPoller` 每輪以 `try/catch (DbException / InvalidOperationException)` 包住，DB 瞬斷只記 log、不中斷迴圈（.NET 預設 `BackgroundServiceExceptionBehavior.StopHost` 會因未處理例外停掉整個 Host）。

## 替代方案（已評估後不採納）

1. **DB trigger 自動 bump** —— 拒絕：相依是「語意」非「整張表」，來源可跨多表、常是特定欄位異動才需重置；只有應用程式碼能判斷「這次異動是否有意義」，trigger 無法表達此粒度。

2. **`SqlDependency`（SQL Server）/ `LISTEN`/`NOTIFY`（PostgreSQL）** —— 拒絕：綁單一方言，破壞 Bee.NET 的多 DB 支援；且 `SqlDependency` 需 Service Broker、營運複雜。

3. **Redis pub/sub 或訊息匯流排** —— 拒絕：與現有 in-process 單節點架構不符、引入額外基礎設施相依，過度。DB 通知表天然支援多節點（各節點獨立輪詢同一表）。

4. **per-instance 動態註冊路由** —— 拒絕：ERP 大量 DB 相依快取下逐一註冊不可擴展，且會把路由狀態與反註冊生命週期帶進來。改用慣例式分派（群組 = 型別名）+ lazy evict，快取本身即 registry。

5. **以時間戳（而非版本號）判定異動** —— 拒絕：時鐘偏移、同毫秒多次更新、輪詢邊界三個雷。改用單調 `version` 逐 key 比對最穩（時間只作便宜的增量游標）。

## 相關文件

- [ADR-009](adr-009-cache-implementation.md)：`Bee.ObjectCaching` 快取實作
- [ADR-018](adr-018-db-define-storage.md)：定義儲存於資料庫（本機制的主要消費端之一）
- 計畫：[`plan-db-cache-dependency.md`](../archive/plan-db-cache-dependency.md)、[`plan-define-cache-db-invalidation.md`](../archive/plan-define-cache-db-invalidation.md)（已封存）
- 使用指引：[`development-cookbook.md`](../development-cookbook.md) §跨 process 快取失效
- 命名慣例：[`database-naming-conventions.md`](../database-naming-conventions.md)
