# ADR-010：邏輯資料庫分類（DbCategory）解耦資料庫部署彈性

> **註記**：本文中 `BackendInfo.GetDatabaseItem(databaseId)` 範例在 v5.0 後等價為 `IDatabaseSettingsProvider.GetItem(databaseId)`（DI ctor 注入）。設計理念不變。

## 狀態

已採納（2026-05-10）

## 背景

企業應用系統通常包含多種用途的資料表：跨公司共用的系統表（使用者、Session）、各公司獨立的業務資料、寫入頻繁的稽核 / 操作記錄等。這些表在實體部署上有兩種典型需求：

- **合併部署**：所有表放在單一實體資料庫，省維運成本（中小型導入）
- **分散部署**：依用途切到不同實體資料庫（如業務 DB 走主從複寫、日誌 DB 走獨立寫入優化）

同一份系統定義也常同時面對多公司 / 多租戶部署、跨環境部署（dev / staging / prod）等場景，實體資料庫的數量與命名會隨之變動。

如果框架只有「實體資料庫」這一層概念（即 `DatabaseSettings.Items` 直接對應實體 DB），會碰到三個結構性問題：

1. **schema 部署沒有依據**：建表 / 升級工具需要回答「這個實體 DB 應該包含哪些表」。沒有分類維度，工具只能掃描所有 schema 檔案靠命名前綴或 hardcode 規則歸類。
2. **部署彈性靠呼叫端寫死**：「合併」或「分散」這個決策若無框架支援，每個專案都要自寫派送邏輯（哪些表寫入哪個 DB）。
3. **業務程式對部署細節敏感**：實體 DB 命名（如 `erp_acme_main_v2`）會隨公司、環境變動，業務程式若直接綁實體 DB 名稱，跨環境部署需修程式或大量字串替換。

需要一層**穩定、與環境無關、語意表達用途**的中介，把「資料的用途分類」與「實體部署設定」徹底解耦。

## 決策

引入 `DbCategory`（邏輯資料庫分類），集中宣告於 `DbCategorySettings.xml`，作為實體部署的對應中介。`DbCategory` 與 `DatabaseItem` 採 **多對一** 字串對應，僅於 schema 設計與部署階段使用，執行時不參與。

### 四項核心要點

1. **DbCategory 是純邏輯抽象**

   DbCategory 只回答「**這個分類包含哪些表**」，不對應任何實體連線。預設提供三類慣用分類，但**不硬編碼於框架**：

   | Id | 用途 |
   |----|------|
   | `common` | 跨公司共用的系統表（如使用者、Session） |
   | `company` | 業務資料、各公司獨立 |
   | `log` | 寫入頻繁的稽核 / 操作記錄 |

   分類定義完全由 `DbCategorySettings.xml` 控制，專案可自訂分類。

2. **DatabaseItem 是邏輯分類的實體載體；同一分類可對應多筆 DatabaseItem**

   `DatabaseItem.CategoryId` 為單一字串，宣告該實體連線承載哪個邏輯分類的所有表。但 `DbCategory` 與 `DatabaseItem` 是 **多對一** 關係 —— 同一個分類可以有多筆 DatabaseItem 對應，每筆指向不同實體 DB，但所有 DB 內含的表結構完全相同（皆來自 `DbCategory[cid].Tables`）。常見觸發情境：

   - **單一實體載體**（如 `common`）：1 筆 DatabaseItem
   - **多租戶切分**（如 `company`）：N 家公司有 N 筆 DatabaseItem（`company001`、`company002`...），各指向該公司獨立的實體 DB
   - **時間封存切分**（如 `log`）：依年份切分有多筆 DatabaseItem（`log_2024`、`log_2025`...），各指向該年份獨立的實體 DB
   - 兩種切分維度可疊加（如某分類同時依公司 + 年份切分）

3. **實體資料庫數量與部署切分完全自由**

   結合上述設計，DatabaseSettings 可表達多種部署形態：

   - **合併部署**：每分類各 1 筆 DatabaseItem，全部 DbName 相同 → 所有分類表共存於一個實體 DB
   - **分散部署**：每分類各 1 筆 DatabaseItem，各自獨立 DbName → 每分類一個實體 DB
   - **同一分類多載體**：依切分維度（租戶 / 時間 / 其他）為某分類新增 DatabaseItem，每筆指向獨立實體 DB

   業務程式對部署形態無感：永遠透過 `BackendInfo.GetDatabaseItem(databaseId)` 取連線，由業務層依當前情境（租戶、時間、其他維度）決定要傳哪個 `databaseId`。

4. **執行時取連線只用 `DatabaseItem.Id`，不經過 CategoryId**

   `BackendInfo.GetDatabaseItem(databaseId)` 直接以 Id 取出 DatabaseItem 並建立連線。CategoryId 與 DbCategorySettings 在執行時完全不參與。

> **附帶說明**：FormSchema 也帶 `CategoryId` 屬性，但 FormSchema 本身仍是純表單結構描述、不與資料庫直接關聯。這個 `CategoryId` 純粹是設計階段需要：在 schema 編輯工具從 FormSchema 推導 TableSchema 時，知道應落於 `TableSchema/{categoryId}/` 哪個分類目錄。它不影響本 ADR 的核心設計（DbCategory ↔ DatabaseItem 的解耦關係）。

## 理由

### 為何要邏輯抽象層而非直接用實體 DB

直接讓業務 /  schema 工具操作實體 DB 概念，會把「用途分類」「實體切分」「部署環境」三件事糾纏在一起：

- 修改實體 DB 命名（如新環境 `erp_v2`）會牽動 schema 工具與業務程式
- 「合併或分散」變更需要改動每個對 DB 操作的呼叫點
- 多公司部署時無法用統一抽象表達「這是某公司的業務 DB」

DbCategory 把「分類用途」抽出後，「結構定義 / 分類宣告 / 實體部署」三件事互不干涉，各自獨立演進。邏輯分類的語意（`company` 永遠是公司資料）跨環境穩定；部署現場資訊（`erp_acme_main` 等具體 DatabaseId 命名）只存在於 DatabaseSettings。

### 為何用字串對應而非強型別

`DbCategory.Id` 為字串，DatabaseItem 透過字串值對應。考慮過的替代方案：

- **enum**：硬編於框架，破壞「分類由 settings 控制」的目標，專案無法擴充
- **強型別引用**（如 `[XmlReference]`）：System.Xml.Serialization 不支援跨檔案引用解析；自寫 resolver 複雜度高

字串對應的優點：
- **XML 序列化原生支援**：`[XmlAttribute]` 直出，無需自訂解析
- **跨層解耦**：`Bee.Definition.Settings.DatabaseItem` 與 `Bee.Definition.Settings.DbCategory` 不需互相引用，只透過字串值對應
- **NoCode / LowCode 友善**：定義檔可由非工程師編輯，字串值直觀

## 結果

### 對應關係定型

```text
DatabaseItem.CategoryId  ──►  DbCategory.Id  (in DbCategorySettings)
                                 └─ Tables  (該分類包含的表清單)

DatabaseItem.Id          ──►  業務程式取連線的入口（執行時不經 CategoryId）
```

### 三階段角色對照

| 階段 | 主體 | 用途 |
|------|------|------|
| Schema 設計 | `FormSchema.CategoryId`（附帶用途） | 推導 TableSchema 時，標示應落於 `TableSchema/{cid}/` 哪個分類目錄 |
| 部署 / 建表 | `DatabaseItem.CategoryId` | 對每筆 DatabaseItem 推導應建立的表清單（從 `DbCategory.Tables` 查 → 從 `TableSchema/{cid}/` 取結構 → 在該 DatabaseItem 連線指向的實體 DB 上 DDL） |
| **執行時** | **不使用** | 透過 `DatabaseItem.Id` 直接取連線，與 CategoryId、DbCategorySettings 完全無關 |

### 部署彈性實例

假設邏輯分類為 `common` / `company` / `log`，N 為租戶（公司）數，Y 為 log 封存年份數：

| 模式 | DatabaseItem 數量 | 連線配置 | 實體 DB 數量 |
|------|------------------|---------|-------------|
| 合併部署 | 3（每分類 1 筆） | 三筆 DbName / Server 相同 | 1 個（含全部表） |
| 分散部署 | 3（每分類 1 筆） | 三筆各自獨立 DbName | 3 個 |
| 多租戶部署 | 2 + N（common 1 + company N + log 1） | `company` 分類 N 筆，各對應 `company001`、`company002`... | 2 + N 個 |
| log 按年封存 | 2 + Y（common 1 + company 1 + log Y） | `log` 分類 Y 筆，各對應 `log_2024`、`log_2025`... | 2 + Y 個 |
| 多租戶 + log 封存 | 1 + N + Y | 兩個維度疊加 | 1 + N + Y 個 |

業務程式無感於部署形態，差異只在「業務層怎麼決定要傳哪個 `databaseId`」：

- **合併 / 分散**：固定對照（分類 → DatabaseId），可寫成常數
- **多租戶**：依當前租戶 ID 推導（如 `$"company{tenantId:D3}"`）
- **log 按年封存**：依當前年份（寫入用 `$"log_{DateTime.UtcNow.Year}"`）或查詢年份範圍（跨多筆 DatabaseItem 聚合）推導

**上述為典型基本模式，實際可任意組合**。例如多租戶下為避免 log 集中造成效能瓶頸，可讓「每租戶的 company 與 log 共用同一實體 DB」（`company001` + `log_company001` 兩筆 DatabaseItem 的 DbName 都指向 `company001` 實體 DB）。**邏輯分類與實體部署是兩個獨立維度**，框架不限制組合方式，由部署設計者依資料量、查詢模式、維運成本決定切分策略。

### TableSchema 目錄分層

落檔結構：
```text
<DefinePath>/TableSchema/
              ├── common/
              ├── company/
              └── log/
```

部署腳本可依目錄分批處理（如僅同步 `company` 分類的 schema）。

### 對外 API 變更

- 新增 `DatabaseItem.CategoryId`（commit `f4cc1bd7`），預設值 `""`
- 既有 `DatabaseSettings.xml` 不需強制遷移；但若希望未來啟用驗證（見「取捨」），現有 Item 都應補上 CategoryId

## 取捨

### 失去編譯期關聯檢查

字串對應無法在編譯期偵測 typo（如 `commen` 而非 `common`）。目前的補救：

- `LocalDefineAccess.SaveFormSchema` 檢查 `FormSchema.CategoryId` **非空**（透過 `TableSchemaGenerator.GetCategoryId`）
- **但目前不檢查 CategoryId 是否實際存在於 `DbCategorySettings`**

存在性驗證為已知 trade-off，未來可加入 `DbCategoryValidator` 在落檔前統一驗證，避免錯誤分類 Id 寫入定義檔，導致部署 / 建表階段找不到對應分類。

### 執行時連線選定由業務層處理

承「為何允許多對一」的代價：當同一分類有多筆 DatabaseItem 時（多租戶、時間封存等情境），「業務操作該用哪筆 DatabaseItem」必須由業務層依當前情境決定。本 ADR 不規範這個選擇邏輯，但以下幾點是設計時應當意識到的：

- 業務程式碼在跨分類存取時（如同時讀 common 的使用者與 company 的員工）需要持有當前情境的 context（租戶 ID、操作時間等）
- 框架不提供 context → databaseId 的對照管理機制，由業務層自行維護
- 跨多筆 DatabaseItem 聚合（如 log 跨多年查詢）需業務層協調多次連線取資料後合併
- DbCategorySettings 完全與切分維度無關，新增租戶或新年度封存不需改動分類定義

### `DbCategory.Tables` 子節點為文件性索引

`DbCategorySettings.xml` 中每個 `DbCategory` 帶有 `Tables` 子節點，目前作為「該分類下登錄的表清單」之文件性索引，與 `TableSchema/{cid}/` 下實際檔案、FormSchema 的 FormTable 沒有自動同步機制。若需嚴格一致需另立規範。

## 影響範圍

| 範圍 | 影響 |
|------|------|
| `Bee.Definition.Settings.DbCategorySettings` | 集中定義所有邏輯分類；`DbCategorySettings.xml` 為單一真相來源 |
| `Bee.Definition.Settings.DatabaseItem` | 新增 `CategoryId` 欄位（commit `f4cc1bd7`） |
| `Bee.Definition.Forms.FormSchema` | 附帶須宣告 `CategoryId`，否則 SaveFormSchema 拒絕 |
| `Bee.Definition.DefinePathInfo.GetTableSchemaFilePath` | 路徑加入 `categoryId` 區段 |
| `Bee.ObjectCaching.LocalDefineAccess.SaveFormSchema` | 落檔前檢查 CategoryId 非空 |

## 後續延伸：執行時路由（DbScope + IRepositoryDatabaseRouter，2026-05-15）

本 ADR 原本明言「執行時取連線只用 `DatabaseItem.Id`，不經過 CategoryId」——但留下了「業務層怎麼從當前情境決定 `databaseId`」這個未規範的空白。[`plan-bo-repo-db-routing`](../plans/plan-bo-repo-db-routing.md) 補上這層，配合 [ADR-012](adr-012-session-company-context.md) 的 session 公司情境模型成形：

### `DbScope` enum：bo repo 的執行時存取意圖

`schema.CategoryId` 是 schema 屬性（XML 配置），`DbScope` 是執行時意圖（程式碼決策）——兩者**概念上完全脫勾**，雖然目前值對應一致。`DbScope` 提供型別安全的 enum 取代 magic string：

```csharp
namespace Bee.Definition;

public enum DbScope { Common, Company, Log }
```

### `IRepositoryDatabaseRouter`：解析的單一來源

`DbScope` → `databaseId` 的映射由 `IRepositoryDatabaseRouter.Resolve(scope, accessToken)` 統一執行：

| `DbScope` | 解析路徑 |
|-----------|---------|
| `Common` | 固定 `"common"`，不需 accessToken |
| `Log` | 固定 `"log"`，不需 accessToken（讓 `Login` / `Logout` 等 pre-EnterCompany 方法也能寫 audit log） |
| `Company` | accessToken → `SessionInfo.CompanyId` → `CompanyInfo.CompanyDatabaseId` |

多公司情境下，多家公司可共享同一 `CompanyDatabaseId` 字串（譬如多家中小公司共用 `"biz_shared_01"` 實體 DB），靠表上 `sys_company_rowid` 欄位做列級分區。Router 對「同 databaseId 多公司」與「獨立 databaseId」兩種設定都一視同仁——CompanyInfo 設定彈性決定，路由邏輯本身不變。

### 與 FormSchema 的銜接

`FormRepositoryFactory.CreateDataFormRepository(progId, accessToken)` 內部把 `schema.CategoryId` 轉成 `DbScope`，再呼叫 router 解析出實際 databaseId：

```text
schema.CategoryId (string)
    ↓ FormRepositoryFactory.ParseCategoryId(string)
DbScope (enum)
    ↓ IRepositoryDatabaseRouter.Resolve(DbScope, accessToken)
databaseId (string, DatabaseItem.Id)
```

BO 端不需要操心這層——`BusinessObject` 加 `ResolveDatabaseId(DbScope)` 與 `CreateDataFormRepository(progId)` 兩個 protected helper，自動帶入當前 `AccessToken`。

### 多 `DatabaseItem` per category 的執行時選定

本 ADR 原本指出「同一分類多載體時，業務操作該用哪筆 DatabaseItem 由業務層決定」。在 session 模型確立後（[ADR-012](adr-012-session-company-context.md)），這個選定邏輯具體化為：

- **`company` 分類多載體（多公司獨立 DB）**：由 `EnterCompany` 寫入 `SessionInfo.CompanyId`，後續 router 從 `CompanyInfo.CompanyDatabaseId` 取對應 DatabaseItem
- **`log` 分類多載體（如年份封存 `log_2024` / `log_2025`）**：本 ADR 範圍只 cover 「current active log」（固定 `"log"`）；查詢封存資料屬另一個議題，由自訂 bo repo 顯式傳入封存年份對應的 databaseId
- **`common` 分類**：永遠單一 databaseId（`"common"`），無多載體場景

### `CompanyInfo.LogDatabaseId` 移除

P1 落地 `CompanyInfo` 時原本含 `LogDatabaseId` 欄位，預期某些公司想用獨立 log DB。但後續決定 `DbScope.Log` 固定 `"log"` 以支援 pre-EnterCompany 寫 log，`LogDatabaseId` 變 dead field 並移除。多公司 log 隔離由列級 `sys_company_rowid` 處理（與 company DB 一致），不需實體 DB 隔離。

## 相關文件

- [ADR-005：FormSchema 定義驅動架構](adr-005-formschema-driven.md)
- [ADR-012：Session 公司情境模型](adr-012-session-company-context.md) — `DbScope.Company` 路由依賴的 session 模型
- [DatabaseSettings 與 DbCategorySettings 指引](../database-settings-guide.zh-TW.md) — 結構與運作細節
- 計畫：[`plan-bo-repo-db-routing.md`](../plans/plan-bo-repo-db-routing.md) — `DbScope` + router 實作細節
