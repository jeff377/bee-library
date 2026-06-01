# 計畫：定義快取在 DB 儲存模式的失效整合（storage-aware）

**狀態：📝 待做（blocked，等 `DbDefineStorage` 立項）**

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

1. 主計畫階段 1–3 完成。
2. `DbDefineStorage` 立項並實作（含 `SaveX` 的 tx 邊界，供 bump 掛載）。
3. 各定義快取 `GetPolicy()` 改 storage-aware；route registry 補定義群組。

## 驗證

- 切到 `DbDefineStorage` 後：A process `SaveFormSchema` → B process（模擬第二節點）poller 抓到 `"FormSchema:X"` 版本變化 → evict → 下次 `GetFormSchema` 重載。
- 維持 `FileDefineStorage` 時：file-watch 行為不變（回歸測試）。
- storage 切換時 `GetPolicy()` 正確選擇失效來源（DB 模式不設 file path）。
