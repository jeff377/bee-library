# ADR-018：定義儲存於資料庫（`st_define` 單表 XML blob）

## 狀態

已採納（2026-06-01）

## 背景

定義資料（`FormSchema`、`TableSchema`、`FormLayout`、`Language`、`DbCategorySettings`、`ProgramSettings` 等）原本以「一定義一 XML 檔」儲存（`FileDefineStorage` over `PathOptions`），跨 process 失效靠 `ChangeMonitorFilePaths`（file-watch）。

多節點 / 雲端部署不應依賴共享檔案系統。需要一種「定義存資料庫」的後端，並由 [ADR-017](adr-017-db-cache-invalidation.md) 的通知表取代 file-watch 作為跨 process 失效通道。

`BackendComponents.DefineStorage` 已是可設定的型別名（`AddBeeFramework` 依此建 `IDefineStorage`），故切換 `FileDefineStorage` ↔ DB 後端是設定變更，不動呼叫端。

## 決策

採「**單表、所有可存型別、一定義一列、XML blob**」：`DbDefineStorage : IDefineStorage`（並實作 `ICustomizeDefineReader`），置於 `Bee.Db`。

### 通知/儲存表 `st_define`

| 欄 | 型別 | 說明 |
|----|------|------|
| `define_type` | varchar | 鑑別字 = **被快取型別名**（`typeof(T).Name`） |
| `customize_id` | varchar | base 層用 sentinel `"*"`；其他 = 租戶客製代碼 |
| `define_key` | varchar | 型別內識別（單例用 `"*"`） |
| `content` | Text/CLOB | 定義的 XML 序列化內容 |
| `sys_update_time` | datetime | DB 伺服器時間（系統欄，`SysFields.UpdateTime`） |

`PK(define_type, customize_id, define_key)` + `define_type` 索引（供「列舉某型別全部定義」）。

### 五個關鍵設計點

1. **Bootstrap 切分**：`SystemSettings` / `DatabaseSettings` **維持檔案**（啟動必要設定；`DatabaseSettings` 是「怎麼連 DB」本身，不可能存在它所描述的 DB 裡）。其餘 6 型進 DB。

2. **`define_type` = `typeof(T).Name`** → 同時是儲存鑑別字、[ADR-017](adr-017-db-cache-invalidation.md) 慣例分派的快取群組、bump 群組三者一致（注意 Language 的快取型別為 `LanguageResource`，故 `define_type` = `"LanguageResource"`）。`SaveX` 在同 tx 內 `Touch("<typeof(T).Name>:<define_key>")` → 失效自動路由到對應快取。

3. **`define_key` 對齊快取 Remove key**：複合鍵用 **`.`**（`TableSchema` → `"common.st_user"`、`Language` → `"zh-TW.common"`），與 `TableSchemaCache` / `LanguageResourceCache` 內部 key 編碼一致 —— 否則 `TryEvict(群組, 實體)` 會踢錯/踢不到。

4. **sentinel `"*"`**：`customize_id`(base) 與單例 `define_key` 用非空 `"*"`。因 **Oracle 把 `''` 當 `NULL` 且 PK 欄不可為 NULL**（dialect 層的 String→nullable 修正救不了 PK 欄），不能用空字串；`"*"` 也對齊既有 `"Type:*"` 單例慣例。

5. **DI 啟用以延遲解析打破建構循環**：`DbDefineStorage(IServiceProvider)` 建構子**首次讀寫才**解析 `IDbConnectionManager` / `ICacheNotifyService`。否則 `DbDefineStorage → IDbConnectionManager → IDatabaseSettingsProvider → IDefineAccess → IDefineStorage(=DbDefineStorage)` 會在建構期成環。bootstrap 切分已打破語意環（`DatabaseSettingsCache` 直讀檔、不經 `IDefineStorage`），延遲解析再打破 DI 物件圖建構環。

### 客製化 overlay 收進同表

租戶客製覆寫（`Language` / `FormLayout` / `ProgramSettings`）以 `customize_id` 欄收進同一張 `st_define`（`"*"` = base）；`ICustomizeDefineReader` 的三個讀取以該 `customize_id` 查詢，缺漏回 `null`（與檔案版唯讀語意一致）。比檔案模型的雙目錄更統一。

## 結果

### 進 DB / 留檔案

| DefineType | 去向 |
|------------|------|
| `SystemSettings` / `DatabaseSettings` | **留檔案**（bootstrap） |
| `DbCategorySettings` / `ProgramSettings` / `TableSchema` / `FormSchema` / `FormLayout` / `Language` | 進 DB |

### 失效整合（承 ADR-017）

定義快取皆為 `IEvictableCache`（群組 = 型別名），各快取 `GetPolicy()` 已 storage-aware：`FileDefineStorage` 設 file-watch、`DbDefineStorage` 不設（改靠通知表）。`DbDefineStorage.SaveX` 同 tx bump 後，跨節點失效由 poller + 慣例分派自動完成 —— **無需註冊任何路由**。

### 啟用前置（部署作業，非程式）

切換成 DB 儲存（`BackendComponents.DefineStorage = Bee.Db.Storage.DbDefineStorage`）需先把既有定義資料**遷移進 `st_define`**；否則 `GetDbCategorySettings()` 等讀空表會丟例外。此資料遷移屬部署作業。

## 替代方案（已評估後不採納）

1. **每型別一張表 / 欄位正規化**（把 FormSchema 拆欄）—— 拒絕：定義讀多寫少且前置快取，blob 粒度 = 檔案粒度最簡單；正規化徒增 schema 與映射複雜度。

2. **MessagePack 取代 XML** —— 拒絕：定義已 XML 可序列化，沿用 `XmlCodec` 與檔案版 round-trip 一致、DB 內可人眼檢視、無新相依；size 非瓶頸（有快取）。

3. **客製化另立 `st_define_customize` 表** —— 拒絕：與「單表統一」初衷相違；`customize_id` 一欄即可區分,查詢 `WHERE customize_id IN ('*', @tenant)` 一次取兩層。

4. **把 `CreateInstance` 接 `ICacheDataSourceProvider`** —— 拒絕：DB 載入已由 service 層（`CompanyInfoService` 等）的 load-on-miss 負責，cache 維持「笨儲存」；接 provider 會重工並把 DB 依賴塞進 `Bee.ObjectCaching`。

5. **`key1`/`key2` 兩欄拆複合鍵** —— 拒絕：與 [ADR-017](adr-017-db-cache-invalidation.md) 的單一字串 cache key 不一致；單一 `define_key` 對齊通知 key、未來複合度增加也不需改 schema。

## 相關文件

- [ADR-017](adr-017-db-cache-invalidation.md)：資料庫快取失效機制（本儲存的失效通道）
- [ADR-016](adr-016-multitenant-customization-overlay.md)：多租戶客製化覆蓋層（`customize_id` 語意來源）
- [ADR-009](adr-009-cache-implementation.md)：快取實作（cache 笨儲存 / service 載入分層）
- 計畫：[`plan-db-define-storage.md`](../plans/plan-db-define-storage.md)
- 命名慣例：[`database-naming-conventions.md`](../database-naming-conventions.md)
