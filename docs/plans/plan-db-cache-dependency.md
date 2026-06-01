# 計畫：資料庫快取相依/失效機制（DB 通知表 + 輪詢）

**狀態：🚧 進行中（2026-06-01）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 通知表 `st_cache_notify` 的 `TableSchema.xml` 定義 + `ICacheNotifyService.Touch`（同 tx UPSERT 自增，置於 `Bee.Db`） | ✅ 已完成（2026-06-01） |
| 2 | 輪詢器 `CacheNotifyPoller`（IHostedService）+ 靜態路由 registry | 📝 待做 |
| 3 | 補完業務資料 DB 載入路徑（`CompanyInfoCache` / 組織快取 `CreateInstance`） | 📝 待做 |

> 「定義快取在 DB 儲存模式的失效整合」原階段 4 已抽出為獨立子計畫 [plan-define-cache-db-invalidation.md](plan-define-cache-db-invalidation.md)（blocked，等 `DbDefineStorage` 立項）。本計畫只交付通用機制（階段 1–2）+ 業務快取消費端（階段 3）。

## 背景

快取目前為純 in-process `MemoryCacheProvider`，失效機制只有兩種：

- `IDefineAccess.SaveX() → _cache.XXX.Remove()` —— 主動，但**只在發生寫入的那個 process 生效**。
- XML 定義檔的 `ChangeMonitorFilePaths` 輪詢 —— 只涵蓋檔案來源的快取。

由**資料庫**載入的快取物件（`CompanyInfoCache`、未來的組織快取等）目前沒有「來源資料異動 → 快取重取」的通道。`CompanyInfoCache.CreateInstance` 目前 `return null`，這條路本來就還沒接通。

需求：當來源資料（可能跨多張系統表）異動、且**應用程式判斷該異動有意義**時，能通知（可能在別的 process / 別的節點上的）對應快取進行重置。

### 另一個驅動因素：定義儲存由檔案搬到資料庫（消費端，已抽出子計畫）

定義快取目前靠 `ChangeMonitorFilePaths`（file-watch）取得跨 process 失效；未來改存 DB（`DbDefineStorage`）後沒有檔案可 watch，正好由本機制的通知表補上。因此本機制**設計上對「業務資料快取」與「定義快取」兩個來源一視同仁**（cache_group 涵蓋兩域，見 §5）。

定義快取這個消費端的整合 blocked 於 `DbDefineStorage` 立項，已抽出為 [plan-define-cache-db-invalidation.md](plan-define-cache-db-invalidation.md)；本計畫只交付通用機制 + 業務快取消費端。

## 設計決策（已與使用者確認）

| 決策 | 選擇 | 理由 |
|------|------|------|
| 由誰 bump 通知列 | **只在框架 Repository/BO 層主動 bump**，不用 DB trigger | 相依是「語意」而非「整張表」：來源可跨多表、且常是特定欄位異動才需重置。只有應用程式碼能判斷「這次異動是否有意義」，trigger 無法表達此粒度 |
| 異動標記欄型別 | **單調遞增 `bigint` 版本號** | 避開時鐘偏移、同毫秒多次更新、輪詢邊界三個雷；逐 key 比版本最穩 |
| 相依 key 語意 | **語意快取標籤（cache tag）**，非表名 | 多表/多寫入點可 bump 同一 key；一張表的無關欄位異動可選擇不 bump |
| 失效動作 | **evict（`Remove`），lazy 重載** | 沒人在讀的 key 不白白重載；複用既有 lazy `CreateInstance` |
| 註冊模型 | **靜態路由（cache_group → 失效動作）**，不做 per-instance 動態註冊 | 快取本身即 registry；`Remove` 對未快取 key 為 no-op，免維護反註冊狀態 |
| 部署假設 | 目前單節點，**為多節點預留** | DB 通知表天然支援多節點（各節點獨立輪詢同一表）；同時解決單節點下「跨 process 寫入」失效問題 |
| `ICacheNotifyService` 歸屬 | **`Bee.Db`** | 「定義儲存層（未來 `DbDefineStorage`）」與「業務 `Bee.Repository`」共同的最低交會點。`Bee.Definition` 不相依 `Bee.Db`，故 `DbDefineStorage` 必落在 `Bee.Db`；`DbAccess` 已支援 `Execute(cmd, transaction)`，bump 同 tx 提交天然成立 |
| 通知表 / 欄名 | 表 `st_cache_notify`、**單一表三欄**：`sys_cache_key`(PK) / `sys_cache_version` / `sys_update_time` | 表用 `st_` 前綴、欄用 `sys_` 前綴（對齊框架慣例）。不拆 group/key 兩欄、不另立計數表或 registry 表（路由是程式碼） |
| 版本號產生 | **每-key 自增**（`version = version + 1`），runtime-session-scoped | 無全域 sequence/計數表、無寫入熱點、4 dialect 一致。首輪只取基準游標 → 版本只需 runtime session 內單調 |
| 輪詢模型 | **poller 持 in-memory 鏡像 + `sys_update_time` 增量抓取** | 首輪只取 `max(update_time)` 基準（不抓全表）；之後抓 `update_time >= highWater - margin` 覆蓋鏡像 |
| evict 判定 | **比鏡像中該 key 的 `version` 是否變大** | delta 用 `>=` 邊界重疊不漏；用 `version` 冪等判定真異動，避免同時間刻度漏 evict / 重複 evict |
| 殘留邊界 | **安全餘量**：每輪查 `update_time >= highWater - margin`（預設 5s，可覆寫） | cover 長交易 `update_time` 早於 commit 可見時間；`version` 冪等使重疊重抓不誤刪 |
| `update_time` 來源 | **DB 伺服器時間**（`CURRENT_TIMESTAMP`） | 多節點下統一時鐘，避免節點時鐘偏移 |
| 啟動行為 | **不清空表**；poller 首輪只取基準游標 `highWater = max(update_time)`，不抓全表、不 evict | 清表在多節點會破壞跨節點 version 單調；歷史全表資料對「啟動時為空的本地快取」過時無用，無需抓取（見 §2b） |
| bump 接 tx | **顯式傳 `DbTransaction` 參數** | tx 邊界明確，與既有 `DbAccess.Execute(cmd, transaction)` 一致 |
| 路由宣告 | **程式碼註冊** | 與 `ICacheContainer` 快取註冊放一起，編譯期型別檢查、refactor 友善 |
| 輪詢間隔 | **預設 5 秒，`BackendConfiguration` 可覆寫** | 對齊既有輪詢容忍度，失效夠即時又不壓 DB |

## 架構

### 1. 通知表 `st_cache_notify`（單一表、三欄）

每個**邏輯快取 key 一列**（UPSERT，非 append log → 表大小有界、免清理 job）：

| 實體欄名 | 型別 | 說明 |
|---------|------|------|
| `sys_cache_key` | varchar (**PK**) | `"群組:實體"` 慣例字串，如 `"OrgInfo:0001"`、`"FormSchema:Employee"`；單物件快取用 `"SystemSettings:*"` |
| `sys_cache_version` | bigint | 每-key 自增版本號（真異動判定） |
| `sys_update_time` | datetime | DB 伺服器時間（`CURRENT_TIMESTAMP`），增量抓取游標；沿用框架標準欄名 |

- 欄名對齊框架慣例（系統欄 `sys_` 前綴）；**下文虛擬碼為求精簡以短名 `cache_key` / `version` / `update_time` 代稱**。
- `sys_cache_key` 把原本的 group/key 折成單欄字串；路由器以**前綴（群組）**比對對應失效動作。
- 兩欄分工：`sys_update_time` 負責「便宜地抓增量」，`sys_cache_version` 負責「冪等地判定真異動」（見 §2a）。

**定義方式（定案）**：與其他系統表（`st_user` / `st_session` 等）一致，提供一份 `st_cache_notify.TableSchema.xml` 定義，放系統表 define 目錄（對照 `tests/Define/TableSchema/common/st_*.TableSchema.xml`）。

**實際建表路徑（未定）**：拿到 TableSchema 後**如何產生實體表**（走既有 schema generation 自動產四 dialect DDL、或其他方式）尚未定案，留待後續決定。本計畫先確保有 TableSchema 定義即可。

> ⚠️ **實作期待確認（階段 1）**：本表 PK 是 `sys_cache_key`（字串），與框架標準表「`sys_no` 自增 PK + `sys_rowid` GUID」慣例不同。需確認 `TableSchema` 是否支援此精簡形狀（自訂字串 PK、不帶 `sys_no`/`sys_rowid`），或須折衷補上標準欄並把 `sys_cache_key` 設為 unique key。先讀現有 `st_*.TableSchema.xml` 確認框架假設。

### 2. 版本號產生（每-key 自增）

bump 時以**單一 UPSERT** 在行鎖內原子遞增 `version`、刷新 `update_time`：

```
UPSERT st_cache_notify (cache_key):
  存在 → UPDATE ... SET version = version + 1, update_time = CURRENT_TIMESTAMP WHERE cache_key = ..
  不存在 → INSERT ... version = 1, update_time = CURRENT_TIMESTAMP
```

- 遞增由 DB 在行鎖內計算（非 app 端 read-then-write），並發 bump 自動序列化 → 無 lost update。
- **無全域 sequence、無計數表、無寫入熱點**；四個 dialect 寫法一致（UPSERT 語法差異由既有 dialect 抽象處理）。
- 不清表；poller 首輪只取基準游標（見 §2b）→ `version` 只需 runtime session 內單調，不需跨重啟持久。
- 失效語意只需「自上次以來是否變過」：一個 key 兩次 bump → 版本跳 2，poller 仍 evict 一次，正確。

### 2a. 輪詢比對（鏡像 + `update_time` 增量 + `version` 冪等判定）

poller 持一份 in-memory 鏡像 `{cache_key → version}`（使用者所指的「中介」），用 `update_time` 做增量抓取、用 `version` 做真異動判定：

```
第一輪：SELECT max(update_time) FROM st_cache_notify   ← 只取基準游標，不抓全表
        → highWater = 該值（表空則用 DB CURRENT_TIMESTAMP）；鏡像留空、不 evict
之後每輪：SELECT cache_key, version, update_time WHERE update_time >= highWater - margin
        for each row:
          version > 鏡像[key]（或鏡像無此 key）→ cache.Remove(key)，鏡像[key] = version
        highWater = max(highWater, 本輪看到的 update_time)
```

- delta 用 **`>=` 而非 `>`**：同一時間刻度（同秒/同毫秒）的多筆異動不會被邊界漏掉。
- 重複抓到的邊界列以 **`version` 比對**保證冪等：版本沒變大就不 evict（不誤刪、不漏刪）。
- **安全餘量 `margin`（定案採用）**：每輪查 `update_time >= highWater - margin`，永遠重疊回看一小段，cover「長交易 `update_time`（= `CURRENT_TIMESTAMP` 取交易/敘述開始時間）早於 commit 可見時間」的殘留邊界。
  - 餘量大小需 **≥ bump 路徑交易從 `Touch` 到 `COMMIT` 的最長延遲**（`Touch` 通常是 commit 前最後一筆寫入，間隔小）。
  - **預設 5 秒**，`BackendConfiguration` 可覆寫（與輪詢間隔同一組設定）。
  - 成本：重疊窗每輪多掃幾列，因表小 + `version` 比對冪等，可忽略。

### 2b. 不清表 + 首輪只取基準游標

- **本計畫不做清表動作**。通知表為 UPSERT 單列/key，大小本就有界，不需靠清表控量。
  - ⚠️ 清表反而在**多節點**出問題：A 節點重啟清空表，正在跑的 B 節點鏡像仍停在舊 `version`；清空後新 bump 從 `version = 1` 起 → `1 < 鏡像值` → B **漏 evict**。故一律不清。
- **首輪不抓全表**：poller 啟動時本地快取為空，無物件可 evict；歷史全表 `version` 對空快取過時無用。首輪只取 `highWater = max(update_time)` 當基準，從「現在」往後追蹤。
  - 啟動後才建立的快取物件都從來源讀**當前資料**，本就最新；只要追蹤啟動後的新 bump 即可。
  - 鏡像為空時首次見某 key → 當「新 key」evict（快取存在則重置、不存在則 no-op）；之後該 key 進鏡像、正常比 `version`。鏡像自然只含「啟動後變動過的 key」。

### 3. bump 原語 `ICacheNotifyService.Touch(cacheKey, transaction)`（置於 `Bee.Db`）

```
Touch(string cacheKey, DbTransaction transaction):
  在傳入的 transaction 內 UPSERT st_cache_notify（version 自增、update_time 刷新）
```

- `cacheKey` 為 `"群組:實體"` 慣例字串（如 `"OrgInfo:0001"`）。

- 呼叫端（業務 Repository / 未來 `DbDefineStorage`）本就持有寫入用的 `DbTransaction`，**顯式傳入**，tx 邊界清晰，與既有 `DbAccess.Execute(cmd, transaction)` 一致。
- ⚠️ **正確性約束**：bump **必須與資料異動在同一 transaction 提交**。否則通知先被輪詢看到、資料 commit 尚未可見 → reload 讀到舊值並標記為新鮮 → 永久 stale。顯式傳同一 `transaction` 即保證此點。
- 呼叫端在**判斷異動有意義後**才呼叫；框架**不**對每次表寫入自動 bump。

### 4. 輪詢器 `CacheNotifyPoller`（`IHostedService`）

repo 目前無 `IHostedService` 接線（僅有 `Bee.Base/BackgroundServices/BackgroundService` 工作佇列基類）。新增輕量 hosted service：

```
首輪：只取 highWater = max(update_time) 基準，不抓全表、不 evict（見 §2a/§2b）
每隔 5 秒（預設，BackendConfiguration 可覆寫）：
  抓 update_time >= highWater - margin 的異動 → 逐列比對鏡像 version
  for each 版本變大或鏡像無此 key 的 cache_key：
     依「靜態路由」（前綴=群組）找出對應失效動作 → cache.Remove(實體key)，更新鏡像
```

- 多節點：每個節點獨立持有鏡像，獨立輪詢，天然 fan-out。
- `Remove` 對未快取 key 為 no-op。

### 5. 靜態路由 registry

`cache_group → 一或多個失效動作`（many-to-one 或 one-to-many：一個 group 異動可同時 evict 多個衍生快取）。**以程式碼註冊**，與 `ICacheContainer` 的快取註冊放在一起維護（編譯期型別檢查、refactor 友善）。

路由器以 `cache_key` 的**前綴（群組）**比對，涵蓋**兩個來源域**、一視同仁：

| 來源域 | 群組前綴範例 | `cache_key` 範例 | bump 觸發點 |
|--------|-----------|-----------------|------------|
| 業務資料快取 | `CompanyInfo`、`OrgInfo` | `"OrgInfo:0001"` | 業務 Repository/BO 判斷有意義欄位異動後 |
| 定義快取（DB 模式）※ | `FormSchema`、`TableSchema`、`SystemSettings`、`FormLayout`、`DbCategorySettings`、`Language`、`ProgramSettings`、`DatabaseSettings` | `"FormSchema:Employee"`、`"SystemSettings:*"` | `DbDefineStorage.SaveX` 在同 tx 內 bump |

※ 定義快取列為設計預留；其實際接線在子計畫 [plan-define-cache-db-invalidation.md](plan-define-cache-db-invalidation.md)，本計畫不交付。

### 6. 補完 DB 載入路徑

`CompanyInfoCache.CreateInstance` / 組織快取目前 `return null`。本機制落地時一併接通：

- 經 `ICacheDataSourceProvider`（現有 `Business/Providers/CacheDataSourceProvider`，目前僅 `GetSessionUser`）擴充對應載入方法。
- 載入可跨多表聚合（組織快取場景）。

### 7. 定義快取 storage-aware 失效 → 見子計畫

定義快取在 DB 儲存模式下「以通知表取代 file-watch」的整合，已抽出為 [plan-define-cache-db-invalidation.md](plan-define-cache-db-invalidation.md)（blocked 於 `DbDefineStorage` 立項）。本計畫的通用機制（§1–§6）設計上已為其預留（cache_group 涵蓋定義快取群組、`ICacheNotifyService` 置於 `Bee.Db` 供 `DbDefineStorage` 呼叫），落地時直接接上。

## 端到端流程範例（組織快取）

```
1. 外部/內部程式經 Repository 更新 st_org 的某個「會影響組織快取」的欄位
2. Repository 判斷此欄位有意義 → 在同一 tx 內 ICacheNotifyService.Touch($"OrgInfo:{orgId}", tx)
   （無關欄位的更新則不呼叫）
3. 各節點 CacheNotifyPoller 下次輪詢抓到 "OrgInfo:{orgId}" 版本變大
4. 依路由對 OrgInfoCache.Remove(orgId)
5. 下次 Get(orgId) → CreateInstance 跨多表重新聚合載入
```

## 不做 / 排除

- ❌ DB trigger 自動 bump —— 無法表達「特定欄位 + 多表語意」粒度（見決策表）。
- ❌ `SqlDependency` / `LISTEN/NOTIFY` —— 綁單一 dialect，破壞多 DB 支援。
- ❌ Redis pub/sub / 訊息匯流排 —— 與現有 in-process 單節點架構不符，過度。
- ❌ per-instance 動態註冊 —— 改用靜態路由 + lazy evict。

## 待確認 / 開放問題

設計決策已全數敲定（見決策表）：版本號=每-key 自增、bump 接 tx=顯式 `DbTransaction` 參數、路由=程式碼註冊、輪詢間隔=預設 5s 可覆寫、`ICacheNotifyService` 置於 `Bee.Db`。

剩餘為實作期細節，不阻擋動工：

1. 通知表：**定案提供 `st_cache_notify.TableSchema.xml` 定義**；但**實際建表路徑未定**（既有 schema generation 自動產 DDL vs 其他）—— 留待後續。實作子問題：本表 PK 為字串 `sys_cache_key`、不帶 `sys_no`/`sys_rowid`，須確認 `TableSchema` 支援此精簡形狀或折衷補標準欄。
   - ✅ **階段 1 已驗證**：`CREATE TABLE` builder 直接從 `Indexes` 讀 PK 欄組（不假設 `AutoIncrement`/`sys_no`），字串 PK 完全支援，**無需補標準欄**。`st_cache_notify.TableSchema.xml` + `tests/Define/DbCategorySettings.xml` 已落地，四方言 schema build + `[DbFact]` 通過。
   - ⚠️ **本機 Oracle stale-container 注意**：既有 Oracle 容器若殘留舊 `st_*` 表，`SharedDatabaseState` 重跑 schema upgrade 會在 `st_company` 撞**既有** ORA-01442（已 NOT NULL 欄再 MODIFY NOT NULL），整個 Oracle setup abort → 新表 `st_cache_notify` 來不及建。fresh 容器（CI）走全新 CREATE 無此問題。此 ORA-01442 為既有 Oracle alter-path 議題（見 plan-oracle-string-nullability.md），非本階段引入。

   階段 1 兩項實作決策（已與使用者確認）：UPSERT 採**各方言原生**（PG/MySQL/SQLite `ON CONFLICT`/`ON DUPLICATE KEY`、SQL Server/Oracle `MERGE WITH (HOLDLOCK)`/`MERGE`）；`Touch` 簽章採 **3 參數**（加 `DatabaseType`，因 `DbTransaction` 不帶方言資訊）。
2. `BackendConfiguration` 暴露輪詢間隔的設定鍵名與是否可停用 poller（純單節點 + 全框架寫入時可關）。
3. 定義快取 storage-aware 失效已抽出 [plan-define-cache-db-invalidation.md](plan-define-cache-db-invalidation.md)（blocked 於 `DbDefineStorage`），不在本計畫範圍。

## 驗證

- 單元測試：版本號單調遞增、UPSERT 行為、輪詢 delta 計算、路由 evict。
- 首輪行為測試：表中已有歷史 row 時，poller 首輪只取基準游標、**不抓全表、不 evict**；僅「啟動後的新 bump」才觸發 evict。
- 安全餘量測試：同一時間刻度多筆 bump、`>= highWater - margin` 重疊窗，皆不漏 evict 且不重複 evict（version 冪等）。
- `[DbFact]` 整合測試（4 dialect）：Touch 同 tx 提交、跨「模擬第二 process」的 poller 看到版本變化並 evict。
- 負面測試：無關欄位不 bump → 快取不被 evict。
