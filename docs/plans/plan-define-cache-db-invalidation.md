# 計畫：定義快取在 DB 儲存模式的失效整合（storage-aware）

**狀態：✅ 已完成（2026-06-01）**

> 機制與程式碼已就緒並通過端到端驗證(跨節點 `DbDefineStorage` 存定義 → poller evict → 重載新版,見 `tests/Bee.Hosting.UnitTests/DbDefineCacheInvalidationTests.cs`)。實際切換成 DB 儲存(把 `BackendComponents.DefineStorage` 設為 `Bee.Db.Storage.DbDefineStorage`)為**部署步驟**,且需先將既有定義資料**遷移進 `st_define`**(否則 `GetDbCategorySettings` 等讀空表會丟例外)——此資料遷移屬部署作業,不在程式範圍。

> **相依關係**：
> - 機制面相依 [plan-db-cache-dependency.md](plan-db-cache-dependency.md) 的階段 1–3（`st_cache_notify` + `ICacheNotifyService` + `CacheNotifyPoller` + 靜態路由）必須先就緒。
> - 功能面相依「定義儲存改存資料庫」的 `DbDefineStorage` 實作（尚未立項）。
> 本子計畫是「定義快取」這個**消費端**對通知機制的整合，與業務快取（主計畫階段 3）平行，只是它被 `DbDefineStorage` 擋住，故獨立成檔。

## 背景

目前定義（FormSchema、TableSchema、SystemSettings 等 8 個快取，位於 `Bee.ObjectCaching/Define/`）儲存於 XML 檔，跨 process 失效靠各快取 `GetPolicy()` 內設定的 `ChangeMonitorFilePaths`（任一 process 改檔，所有 process 的 `FileSystemWatcher` 都會看到 → evict）。

未來定義會有「儲存於資料庫」的實作（`DbDefineStorage`）。**一旦改存 DB，就沒有檔案可 watch，這個免費的跨 process 失效信號隨之消失** —— 正好由主計畫的通知表（`st_cache_notify`）補上。因此通知機制**不只是業務資料快取的附加功能，更是定義快取在 DB 儲存模式下「取代 file-watch」的失效通道**。

## 設計：定義快取 storage-aware 失效

8 個定義快取目前在各自 `GetPolicy()` 內**硬寫** `ChangeMonitorFilePaths`。要讓同一批快取類別在兩種儲存後端下都正確失效，`GetPolicy()` 需改成**隨 active `IDefineStorage` 而變**：

| 儲存後端 | 失效來源 | `GetPolicy()` 行為 |
|----------|---------|-------------------|
| `FileDefineStorage`（現狀） | file-watch | 設 `ChangeMonitorFilePaths`（維持現狀） |
| `DbDefineStorage`（未來） | 通知表 | **不設** file path；該 group 註冊到 poller 路由 |

- 本機寫入路徑 `SaveX → Remove()` 兩種模式都保留（即時失效本 process）；差別只在**跨 process** 通道：file-watch（檔案模式）↔ 通知表（DB 模式）。
- `DbDefineStorage` 落在 `Bee.Db`（`Bee.Definition` 不能相依 `Bee.Db`）；其 `SaveX` 在寫定義的同一 tx 內呼叫 `ICacheNotifyService.Touch(cacheKey, transaction)`。

### 定義快取的 cache_group 對映

沿用主計畫的 `cache_key = "群組:實體"` 慣例與前綴路由：

| 群組前綴 | `cache_key` 範例 | bump 觸發點 |
|---------|-----------------|------------|
| `FormSchema` | `"FormSchema:Employee"` | `DbDefineStorage.SaveFormSchema` 同 tx |
| `TableSchema` | `"TableSchema:st_user"` | `DbDefineStorage.SaveTableSchema` 同 tx |
| `SystemSettings` | `"SystemSettings:*"` | `DbDefineStorage.SaveSystemSettings` 同 tx |
| `FormLayout` / `DbCategorySettings` / `Language` / `ProgramSettings` / `DatabaseSettings` | 對應實體 | 對應 `SaveX` 同 tx |

## 落地條件

1. ✅ 主計畫階段 1–3 完成。
2. ✅ `DbDefineStorage` 實作完成（含 `SaveX` 同 tx bump）——見 [plan-db-define-storage.md](plan-db-define-storage.md)。
3. ✅ 各定義快取 `GetPolicy()` 已 storage-aware；路由改為慣例分派(主計畫階段 3),**不需** route registry。
4. ✅ DI 啟用機制:`DbDefineStorage` 提供 `(IServiceProvider)` 延遲解析建構子打破建構循環;`AddBeeFramework` 的 `CreateDefineStorage` 支援之。
5. ⏳ 部署:把 `BackendComponents.DefineStorage` 設為 `DbDefineStorage` + 將定義資料遷入 `st_define`(部署作業)。

## 驗證

- ✅ 切到 `DbDefineStorage` 後:A 節點 `SaveFormSchema` → B 節點(模擬第二節點)poller 抓到 `"FormSchema:X"` 版本變化 → evict → 下次 `GetFormSchema` 從 DB 重載新版。(`DbDefineCacheInvalidationTests`,SQL Server + PostgreSQL)
- ✅ DI 建構循環:`(IServiceProvider)` 建構子不於建構時解析相依(`DbDefineStorageTests.Constructor_ServiceProvider_DefersDependencyResolution`)。
- ✅ 維持 `FileDefineStorage` 時 file-watch 行為不變(既有定義快取測試全綠)。
