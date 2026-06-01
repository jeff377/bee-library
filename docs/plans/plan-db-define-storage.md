# 計畫：定義儲存改存資料庫（DbDefineStorage，單表 XML blob）

**狀態：📝 擬定中（2026-06-01）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `st_define` 儲存表 `TableSchema.xml` 定義 + 註冊進 `common` 類別 | ✅ 已完成（2026-06-01） |
| 2 | `DbDefineStorage : IDefineStorage`（base 層,XML blob,內部 tx + 同 tx bump） | ✅ 已完成（2026-06-01） |
| 3 | 客製化 overlay（DB 版 `ICustomizeDefineReader`,讀 `customize_id` 列） | 📝 待做 |
| 4 | `ProgramSettings` 納入 DB（擴充 `IDefineStorage` + `ProgramSettingsCache` 改走 storage） | 📝 待做 |

> 本計畫**解 block** [plan-define-cache-db-invalidation.md](plan-define-cache-db-invalidation.md)。失效路由因主計畫階段 3 的慣例式分派（`IEvictableCache` + `ICacheContainer.TryEvict`)已自動成立 —— 定義快取皆為 `KeyObjectCache<T>`,群組 = 型別名,`DbDefineStorage.SaveX` bump `"<Type>:<key>"` 後 poller 自動 evict,**無需註冊路由**。子計畫剩餘工作僅「快取 `GetPolicy()` 改 storage-aware（DB 模式不設 file-watch）」。

## 背景

定義目前以「一定義一 XML 檔」儲存（`FileDefineStorage`），跨 process 失效靠 `ChangeMonitorFilePaths`（file-watch）。需求:定義改存資料庫，讓多節點/雲端部署不依賴共享檔案系統,並由 `st_cache_notify` 通知機制取代 file-watch 作為跨 process 失效通道。

`BackendComponents.DefineStorage` 已是可設定的型別名（`AddBeeFramework` 依此建 `IDefineStorage`）,故切換 `FileDefineStorage` ↔ `DbDefineStorage` 是設定變更,不動呼叫端。

## 設計決策（已與使用者確認）

| 決策 | 選擇 | 理由 |
|------|------|------|
| 儲存形狀 | **單表、所有型別、一定義一列、XML blob** | 與既有 XML 序列化一致;定義讀多寫少且前置快取,blob 粒度 = 檔案粒度,最簡單 |
| Bootstrap 切分 | **`DatabaseSettings` + `SystemSettings` 留設定檔**,其餘 6 型進 DB | 此二為啟動必要設定（`SystemSettings` 含安全金鑰/`BackendConfiguration`;`DatabaseSettings` 是「怎麼連 DB」本身）,不可能存在它所描述的 DB 裡 |
| Key 模型 | **單一 `define_key` 字串**,`PK(define_type, customize_id, define_key)` | 對齊 cache-notify 的 key;`SaveX` 同 tx「UPSERT + Touch」與 load 共用同一把 key |
| 客製化 overlay | **收進同表 `customize_id` 欄**（base 用 sentinel `"*"`） | 單表統一;`WHERE customize_id IN ('*', @tenant)` 一次取兩層、記憶體疊,比雙目錄乾淨 |
| base/單例 sentinel | **`"*"`**（非空字串） | ⚠️ Oracle `''`=NULL 且 PK 欄不可為 NULL（dialect 層的 String→nullable 修正救不了 PK 欄）→ `customize_id`(base) 與單例 `define_key` 必須非空;`"*"` 對齊既有 `"Type:*"` 單例慣例 |
| 稽核/並發欄 | **只加 `sys_update_time`**（不做樂觀並發） | 後台編輯衝突機率低;跨 process 失效由 `st_cache_notify` 負責,define 列不需自帶版本 |
| 序列化 | **XML（重用 `XmlCodec`）** | 與檔案 round-trip 一致、DB 內可人眼檢視、無新相依 |
| `DbDefineStorage` 歸屬 | **`Bee.Db`** | `Bee.Definition` 不相依 `Bee.Db`;需 `DbAccess` + `ICacheNotifyService`（皆在 `Bee.Db`） |

## 進 DB / 留檔案 的型別切分

| DefineType | 去向 | 現況備註 |
|------------|------|---------|
| `SystemSettings` | **留檔案**（bootstrap） | 不在 `IDefineStorage`;`SystemSettingsCache` 直讀檔 |
| `DatabaseSettings` | **留檔案**（bootstrap） | 不在 `IDefineStorage`;`DatabaseSettingsCache` 直讀檔 |
| `DbCategorySettings` | 進 DB | 已在 `IDefineStorage`（單例) |
| `ProgramSettings` | 進 DB（階段 4） | **不在 `IDefineStorage`**;`ProgramSettingsCache` 直讀檔 → 需擴充介面 + 改快取 |
| `TableSchema` | 進 DB | 已在 `IDefineStorage`（複合鍵） |
| `FormSchema` | 進 DB | 已在 `IDefineStorage` |
| `FormLayout` | 進 DB | 已在 `IDefineStorage` |
| `Language` | 進 DB | 已在 `IDefineStorage`（複合鍵） |

## `st_define` 表（單表、XML blob）

| 實體欄名 | 型別 | 說明 |
|---------|------|------|
| `define_type` | varchar | 鑑別字 = **被快取型別名**(`typeof(T).Name`),使 `define_type` = 快取群組 = bump 群組三者一致:`FormSchema` / `TableSchema` / `FormLayout` / `DbCategorySettings` / **`LanguageResource`**(注意 Language 的快取型別為 `LanguageResource`) / `ProgramSettings` |
| `customize_id` | varchar | `"*"` = base 層;其他 = 租戶客製覆寫代碼 |
| `define_key` | varchar | 型別內識別（見下方編碼表）;單例型別用 `"*"` |
| `content` | Text（CLOB） | 定義的 XML 序列化內容（`FieldDbType.Text` → SQL Server `nvarchar(max)` / PG `text` / MySQL `LONGTEXT` / Oracle `CLOB`） |
| `sys_update_time` | datetime | DB 伺服器時間,稽核用 |

- **PK**:`(define_type, customize_id, define_key)`。
- **索引**:`define_type`（供「列舉某型別全部定義」如 schema-gen;`WHERE define_type=? AND customize_id=''`）。
- 與 `st_cache_notify` 同置於 `common` DB → `SaveX` 的 UPSERT 與 `Touch` 同一連線/同 tx 天然成立。
- 表定義循 `st_*` 慣例提供 `st_define.TableSchema.xml`,註冊進 `tests/Define/DbCategorySettings.xml` 的 `common` 類別（同 `st_cache_notify` 做法）。

### `define_key` 編碼（須與各快取的 Remove key 對齊）

| DefineType | `define_key` | cache_key 範例 |
|------------|-------------|---------------|
| `DbCategorySettings` | `"*"` | `"DbCategorySettings:*"` |
| `ProgramSettings` | `"*"` | `"ProgramSettings:*"` |
| `FormSchema` | progId | `"FormSchema:Employee"` |
| `FormLayout` | layoutId | `"FormLayout:EmployeeList"` |
| `TableSchema` | `categoryId.tableName` | `"TableSchema:common.st_user"` |
| `Language` | `lang.ns` | `"Language:zh-TW.common"` |

> ⚠️ **正確性約束（已驗證）**:`define_key`（= cache-notify 的 entity 部分）必須**等於對應快取 `Remove(key)` 所用的 key**,否則 `TryEvict(group, entity)` 會踢錯/踢不到。實查確認複合鍵快取用 **`.`（點）** 分隔:`TableSchemaCache` key = `"{categoryId}.{tableName}"`、`LanguageResourceCache` key = `"{lang}.{namespace}"`。故 `define_key` 複合鍵一律用 `.`(非 `/`)。

## `DbDefineStorage`（`Bee.Db`）

實作 `IDefineStorage`,base 層即 `customize_id=''`:

- **讀**:`GetFormSchema(progId)` → `SELECT content FROM st_define WHERE define_type='FormSchema' AND customize_id='' AND define_key={0}` → `XmlCodec.Deserialize<FormSchema>`。其餘型別同形。
- **寫**:`SaveFormSchema` 在**內部開連線 + tx**內:
  1. UPSERT `st_define`(`content` = XML、`sys_update_time` = 伺服器時間),走階段 1 既有的各方言 UPSERT。
  2. 同 tx `ICacheNotifyService.Touch("FormSchema:" + define_key, tx, dbType)`。
  3. commit。
- bump 後跨 process 失效由 poller + 慣例分派自動完成（定義快取群組 = 型別名,已是 `IEvictableCache`）。
- `IDefineStorage.SaveX` 簽章不帶 transaction;tx 由 `DbDefineStorage` 內部自管（與 `FileDefineStorage` 介面相容,呼叫端無感）。

> ⚠️ **DI 啟用循環(wiring 時處理,非階段 2)**:`DbDefineStorage`(需 `IDbConnectionManager`)→ `IDbConnectionManager` → `IDatabaseSettingsProvider` → `IDefineAccess` → `IDefineStorage`(=`DbDefineStorage`)構成建構期循環。**bootstrap 切分已打破語意循環**(`DatabaseSettingsCache` 直讀檔、不經 `IDefineStorage`),但 DI 物件圖建構仍會遞迴。啟用時(把 `components.DefineStorage` 設為 `DbDefineStorage`)需以**延遲解析**(注入 `IServiceProvider` / `Func<IDbConnectionManager>`,首次 `GetX/SaveX` 才取)打破。**階段 2 只交付類別 + 直接建構的 `[DbFact]` 測試,不動 `AddBeeFramework` 的 storage 選擇**,故無循環;DI 啟用留待後續 wiring 步驟。

## 客製化 overlay（階段 3）

DB 版 `ICustomizeDefineReader`:`GetCustomizeFormLayout(customizeId, layoutId)` → `WHERE define_type='FormLayout' AND customize_id={customizeId} AND define_key={layoutId}`;無列回 `null`(不 fallback base,與檔案版語意一致)。`Language` 同形。`ProgramSettings` 客製化待階段 4 一併。

## 階段 4:ProgramSettings 進 DB

- `IDefineStorage` 加 `GetProgramSettings()` / `SaveProgramSettings(...)`（單例,`define_key=''`）。
- `ProgramSettingsCache` 由「直讀檔（`PathOptions`)」改為「走 `IDefineStorage`」,與 `DbCategorySettingsCache` 一致。
- 此步驟動到快取建構相依,獨立為一階段降低風險。

## 不做 / 排除

- ❌ `SystemSettings`/`DatabaseSettings` 進 DB（bootstrap,見決策表）。
- ❌ 把 FormSchema 等拆欄正規化（granularity 維持「一定義一列」）。
- ❌ 樂觀並發版本欄（只留 `sys_update_time`）。
- ❌ 定義快取路由註冊(主計畫階段 3 慣例分派已自動涵蓋)。

## 落地後對子計畫的影響

[plan-define-cache-db-invalidation.md](plan-define-cache-db-invalidation.md) 解 block 後僅剩:
1. 各定義快取 `GetPolicy()` 改 **storage-aware**:`FileDefineStorage` 設 `ChangeMonitorFilePaths`(現狀);`DbDefineStorage` 不設(改靠通知表)。
2. 回歸:維持 `FileDefineStorage` 時 file-watch 行為不變。

> 失效分派為**慣例式**(群組 = 被快取型別名,`IEvictableCache` 自動成立),**沒有 cache_group 對映表/registry 要維護**。哪些定義會觸發失效,完全取決於 `DbDefineStorage` 實際寫入並 bump 哪些型別;`SystemSettings`/`DatabaseSettings` 留檔案故不 bump。子計畫文件內那段 cache_group markdown 表格僅為早期示意,已被慣例分派取代。

## 驗證

- 單元:`define_key` 編碼與各快取 Remove key 對齊;XML round-trip 與檔案版一致。
- `[DbFact]` 四方言:`SaveFormSchema` → 同 tx UPSERT + Touch 提交 → 模擬第二 process 的 poller 抓到版本變化 → `TryEvict` → 下次 `GetFormSchema` 重載。
- 客製化:`customize_id` 兩層查詢正確、base 與 overlay 不互相污染。
- bootstrap:切到 `DbDefineStorage` 後,`SystemSettings`/`DatabaseSettings` 仍由檔案載入、啟動正常。
