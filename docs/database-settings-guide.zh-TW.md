# DatabaseSettings 與 DbCategorySettings 指引

[English](database-settings-guide.md)

> 本文件說明 Bee.NET 框架中兩個資料庫相關設定檔的結構、定位、存取方式與運作流程，協助開發者理解設定 → 連線 → 分類路由的完整串接。

## 目錄

1. [概覽](#1-概覽)
2. [DatabaseSettings](#2-databasesettings)
3. [DbCategorySettings](#3-dbcategorysettings)
4. [存取入口與快取](#4-存取入口與快取)
5. [CategoryId 串接](#5-categoryid-串接)
6. [檔案位置與範例](#6-檔案位置與範例)

---

## 1. 概覽

兩個設定檔共同支撐「FormSchema 定義 → 邏輯分類 → 實體連線」的串接。職責分工如下：

| 設定檔 | 回答的問題 | 對應實體 |
|--------|-----------|---------|
| **DatabaseSettings** | 系統有哪些「實體」資料庫連線？ | DatabaseServer（伺服器組態）+ DatabaseItem（連線項目） |
| **DbCategorySettings** | 系統有哪些「邏輯」資料庫分類？分類下登錄哪些表？ | DbCategory（分類）+ TableItem（表登錄） |

### 串接關係圖

```text
FormSchema.CategoryId ─────┐
                           │
                           ├──► DbCategory.Id  (in DbCategorySettings)
                           │       └─ Tables  (該分類登錄的表清單)
DatabaseItem.CategoryId ───┘

DatabaseItem.ServerId  ────► DatabaseServer.Id  (in DatabaseSettings.Servers)
```

關鍵概念：
- **FormSchema.CategoryId** 用於設計階段：宣告由此 schema 衍生的 TableSchema 應歸入哪個邏輯分類（決定 TableSchema 落檔目錄）
- **DatabaseItem.CategoryId** 用於部署階段：宣告該實體連線屬於哪個邏輯分類，藉此推導「該實體 DB 應建立哪些表」
- **DbCategory** 是兩者共同的對應目標，`Id` 為純字串（如 `common` / `company` / `log`）
- ⚠️ **執行時取連線完全透過 `DatabaseItem.Id`，不經過 CategoryId 與 DbCategorySettings**

### 邏輯 vs 實體：對應模型

`DbCategory` 是**純邏輯抽象**，只定義「這個分類包含哪些資料表」；它不對應實體資料庫的數量，也不指定要部署在哪。`DatabaseItem.CategoryId` 與 `DbCategory.Id` 是 **多對一** 關係 —— 同一分類可有多筆 DatabaseItem，常見觸發情境：

- **單一實體載體**（如 `common`）：1 筆 DatabaseItem
- **多租戶切分**（如 `company`）：N 家公司有多筆 DatabaseItem（`company001`、`company002`...），各對應一家公司的實體 DB
- **時間封存切分**（如 `log`）：依年份切分有多筆 DatabaseItem（`log_2024`、`log_2025`...），各對應該年份獨立的實體 DB
- 兩種切分維度可疊加（如某分類同時依公司 + 年份切分）

多筆 DatabaseItem 可指向同一個實體 DB（合併部署），也可各自指向獨立實體 DB（分散或多載體部署）。

四種典型部署狀況（皆以三個邏輯分類 common / company / log 為例）：

**狀況 1：合併部署（單一實體資料庫包含全部三類）**

```text
DatabaseSettings.Items (3 筆)                       實體資料庫 (1 個)
─────────────────────────────                       ────────────────
DatabaseItem  CategoryId="common"   DbName=erp ──┐
DatabaseItem  CategoryId="company"  DbName=erp ──┼──► erp（含 st_user、st_session、
DatabaseItem  CategoryId="log"      DbName=erp ──┘     ft_department、ft_employee、
                                                       ft_project、log tables）
```

**狀況 2：分散部署（三個實體資料庫，各對應一個邏輯分類）**

```text
DatabaseSettings.Items (3 筆)                       實體資料庫 (3 個)
─────────────────────────────                       ────────────────
DatabaseItem  CategoryId="common"   DbName=erp_common  ──► erp_common  (st_user、st_session)
DatabaseItem  CategoryId="company"  DbName=erp_company ──► erp_company (ft_department、ft_employee、ft_project)
DatabaseItem  CategoryId="log"      DbName=erp_log     ──► erp_log     (log tables)
```

**狀況 3：多租戶部署（共用分類各 1 筆 + company 分類每家公司 1 筆）**

```text
DatabaseSettings.Items (2 + N 筆)                   實體資料庫 (2 + N 個)
─────────────────────────────                       ────────────────
DatabaseItem  Id="common"      CategoryId="common"   ──► erp_common
DatabaseItem  Id="company001"  CategoryId="company"  ──► company001  (ft_department、ft_employee、ft_project)
DatabaseItem  Id="company002"  CategoryId="company"  ──► company002  (ft_department、ft_employee、ft_project)
DatabaseItem  Id="company003"  CategoryId="company"  ──► company003  (ft_department、ft_employee、ft_project)
   ⋮          (每家公司一筆)
DatabaseItem  Id="log"         CategoryId="log"      ──► erp_log
```

每家公司的 `companyXXX` 實體 DB 表結構完全相同（皆來自 `DbCategory["company"].Tables`），但資料各自獨立。

**狀況 4：log 按年封存（log 分類每年一筆）**

```text
DatabaseSettings.Items (2 + Y 筆)                   實體資料庫 (2 + Y 個)
─────────────────────────────                       ────────────────
DatabaseItem  Id="common"     CategoryId="common"   ──► erp_common
DatabaseItem  Id="company"    CategoryId="company"  ──► erp_company
DatabaseItem  Id="log_2024"   CategoryId="log"      ──► log_2024  (log tables)
DatabaseItem  Id="log_2025"   CategoryId="log"      ──► log_2025  (log tables)
DatabaseItem  Id="log_2026"   CategoryId="log"      ──► log_2026  (log tables)
   ⋮          (每年新增一筆)
```

每年的 `log_YYYY` 實體 DB 表結構完全相同（皆來自 `DbCategory["log"].Tables`），業務寫入時用當前年份對應的 DatabaseId，查詢可跨多筆 DatabaseItem 聚合。狀況 3 與 4 可疊加（如業務上同時依公司 + 年份切分）。

業務程式對部署形態無感：永遠透過 `IDatabaseSettingsProvider.GetItem(databaseId)`（DI ctor 注入）取連線。差異只在業務層怎麼決定要傳哪個 `databaseId`：

- 狀況 1、2：固定對照（分類 → DatabaseId）
- 狀況 3：依當前租戶 ID 推導（如 `$"company{tenantId:D3}"`）
- 狀況 4：依當前年份推導（如 `$"log_{DateTime.UtcNow.Year}"`），跨年查詢需聚合多筆

**狀況可任意組合**：以上四種僅為典型基本模式，實際部署可依成本 / 效能 / 維運考量任意組合。例如多租戶情境下，為避免「log 集中所有租戶」造成效能瓶頸，可改採「**每租戶的 company 與 log 共用同一實體 DB**」：

```text
DatabaseSettings.Items                                實體資料庫
─────────────────────────                             ────────
DatabaseItem  Id="common"           CategoryId="common"   ──► erp_common
DatabaseItem  Id="company001"       CategoryId="company"  ──┐
DatabaseItem  Id="log_company001"   CategoryId="log"      ──┴► company001  (含 ft_* 與 log_* 兩類表)
DatabaseItem  Id="company002"       CategoryId="company"  ──┐
DatabaseItem  Id="log_company002"   CategoryId="log"      ──┴► company002  (含 ft_* 與 log_* 兩類表)
   ⋮          (每家公司 2 筆 DatabaseItem，共用同一實體 DB)
```

每家公司 2 筆 DatabaseItem，分別宣告 company 與 log 分類，但 DbName 指向同一實體 DB —— 實體 DB 內同時包含 ft_* 與 log_* 兩類表。log 資料隨租戶分散，避免跨租戶集中。

關鍵觀念：**邏輯分類與實體部署是兩個獨立維度，可任意組合**。設計時依資料量、查詢模式、維運成本決定切分策略，框架不限制組合方式。

---

## 2. DatabaseSettings

定義位置：[`src/Bee.Definition/Settings/DatabaseSettings/`](../src/Bee.Definition/Settings/DatabaseSettings/)

### 2.1 階層結構

```text
DatabaseSettings
├── Servers : DatabaseServerCollection   共用伺服器組態（連線範本）
│     └── DatabaseServer
└── Items   : DatabaseItemCollection     實際資料庫連線項目
      └── DatabaseItem
```

### 2.2 DatabaseServer 欄位

定義「共用伺服器組態」，多個 DatabaseItem 可引用同一個 Server 共享連線範本與帳密。

| 欄位 | 型別 | 用途 |
|------|------|------|
| `Id` | string | 伺服器識別碼（Key） |
| `DisplayName` | string | 顯示名稱 |
| `DatabaseType` | DatabaseType | `SQLServer` / `PostgreSQL` 等 |
| `ConnectionString` | string | 連線字串範本，可含 `{@DbName}` / `{@UserId}` / `{@Password}` 佔位符 |
| `UserId` | string | 登入 ID，會替換 `{@UserId}` |
| `Password` | string | 登入密碼，會替換 `{@Password}`；序列化時自動加密 |

### 2.3 DatabaseItem 欄位

定義「實體連線項目」，是執行時實際建立連線的單位。

| 欄位 | 型別 | 用途 |
|------|------|------|
| `Id` | string | 連線識別碼（Key），呼叫端用此 Id 取得連線 |
| `CategoryId` | string | 所屬邏輯分類 Id（對應 `DbCategory.Id`） |
| `DisplayName` | string | 顯示名稱 |
| `DatabaseType` | DatabaseType | `SQLServer` / `PostgreSQL` 等 |
| `ServerId` | string | 引用的 DatabaseServer Id（選用） |
| `ConnectionString` | string | 獨立連線字串（不引用 Server 時使用） |
| `DbName` | string | 資料庫名稱，會替換 `{@DbName}` |
| `UserId` | string | 登入 ID（覆蓋 Server 設定） |
| `Password` | string | 登入密碼（覆蓋 Server 設定）；序列化時自動加密 |

> 📌 **DatabaseItem 與邏輯分類為多對一關係**：`CategoryId` 為單一字串而非集合，每筆 DatabaseItem 只屬於一個分類；但**同一分類可有多筆 DatabaseItem**，常見觸發情境包括多租戶切分（如 company 每家公司一筆）、時間封存切分（如 log 每年一筆）等。詳見 [§1 邏輯 vs 實體：對應模型](#邏輯-vs-實體對應模型)。

### 2.4 Server 與 Item 的選擇

兩種使用模式：

- **引用 Server**：`DatabaseItem.ServerId` 指定 Server，連線字串範本來自 Server，Item 只覆寫 `DbName`、（必要時）`UserId` / `Password`。適合多個 Item 共用同一台伺服器、多個 DB 的場景
- **獨立設定**：Item 的 `ServerId` 留空，直接在 Item 上指定 `ConnectionString`、`UserId`、`Password`。適合單一連線或連線設定差異大的場景

### 2.5 連線字串範本替換

連線字串中可使用三個佔位符，框架在建立連線時會替換：

| 佔位符 | 替換來源 |
|--------|---------|
| `{@DbName}` | `DatabaseItem.DbName` |
| `{@UserId}` | `DatabaseItem.UserId`（為空則 fallback `DatabaseServer.UserId`） |
| `{@Password}` | `DatabaseItem.Password`（為空則 fallback `DatabaseServer.Password`） |

範例：
```xml
<DatabaseServer Id="sql_main" DatabaseType="SQLServer"
                ConnectionString="Server=sql.example;Database={@DbName};User ID={@UserId};Password={@Password};" />
<DatabaseItem Id="company_main" CategoryId="company" ServerId="sql_main"
              DbName="erp_company" UserId="erp_user" Password="..." />
```

### 2.6 Password 加密

`DatabaseServer.Password` 與 `DatabaseItem.Password` 在 XML 序列化時自動加密：

- **加密方式**：AES-CBC-HMAC（金鑰由 `ConfigEncryptionKey` 提供，於 `AddBeeFramework` 時 ctor 注入給 `LocalDefineAccess`）
- **儲存格式**：`enc:` + Base64 編碼的密文
- **時機**：
  - 序列化前（`BeforeSerialize`）：將未加密的 Password 自動加密
  - 反序列化後（`AfterDeserialize`）：將 `enc:` 開頭的 Password 自動解密
- **行為**：若 `ConfigEncryptionKey` 為空，跳過加解密（明文儲存，僅限開發環境）

實作位置：[`DatabaseSettings.cs`](../src/Bee.Definition/Settings/DatabaseSettings/DatabaseSettings.cs) `BeforeSerialize` / `AfterDeserialize` / `DecryptPassword`。

---

## 3. DbCategorySettings

定義位置：[`src/Bee.Definition/Settings/DbCategorySettings/`](../src/Bee.Definition/Settings/DbCategorySettings/)

### 3.1 階層結構

```text
DbCategorySettings
└── Categories : DbCategoryCollection
      └── DbCategory
            └── Tables : TableItemCollection
                  └── TableItem
```

### 3.2 DbCategory 欄位

| 欄位 | 型別 | 用途 |
|------|------|------|
| `Id` | string | 分類識別碼（Key），FormSchema / DatabaseItem 透過此 Id 對應 |
| `DisplayName` | string | 顯示名稱（如「共用資料庫」） |
| `Tables` | TableItemCollection | 該分類下登錄的表清單 |

### 3.3 TableItem 欄位

| 欄位 | 型別 | 用途 |
|------|------|------|
| `TableName` | string | 資料表名稱（Key） |
| `DisplayName` | string | 顯示名稱（如「用戶」） |

`Tables` 子節點目前作為**該分類下登錄的表索引**，提供文件性的查找入口；實際 schema 仍以 `TableSchema` 與 `FormSchema` 檔案為權威來源。

### 3.4 預設三類分類

框架預設使用三種邏輯分類：

| 分類 Id | 用途 | 典型表 |
|---------|------|--------|
| `common` | 共用資料庫 — 跨公司共用的系統表 | `st_user`、`st_session` |
| `company` | 公司資料庫 — 業務資料、各公司獨立 | `ft_department`、`ft_employee`、`ft_project` |
| `log` | 日誌資料庫 — 寫入頻繁的稽核 / 操作記錄 | （視應用而定） |

**這三類為慣例，非框架硬性限制**；專案可依需要自訂分類，但 FormSchema 與 DatabaseItem 中的 `CategoryId` 必須對得上 `DbCategorySettings` 中宣告的分類 Id。

---

## 4. 存取入口與快取

### 4.1 統一入口

兩個 settings 都透過 `IDefineAccess`（DI ctor 注入）存取：

```csharp
public class MyService(IDefineAccess defineAccess)
{
    public void Demo()
    {
        // 讀取
        DatabaseSettings dbSettings = defineAccess.GetDatabaseSettings();
        DbCategorySettings catSettings = defineAccess.GetDbCategorySettings();

        // 寫入
        defineAccess.SaveDatabaseSettings(dbSettings);
        defineAccess.SaveDbCategorySettings(catSettings);
    }
}
```

`IDefineAccess` 由 `AddBeeFramework` 註冊為 singleton，預設 `LocalDefineAccess`（檔案系統）；專案可透過 XML `Components` 設定改為 `RemoteDefineAccess`（透過 API 取得）。

### 4.2 快取機制

兩個 settings 由 DI 註冊的 [`ICacheContainer`](../src/Bee.ObjectCaching/ICacheContainer.cs)（預設實作 `CacheContainerService`）集中持有，採 `Lazy<T>` 延遲建立：

| 快取 | 持有者 |
|------|--------|
| `DatabaseSettings` | `ICacheContainer.DatabaseSettings`（`Lazy<DatabaseSettingsCache>`） |
| `DbCategorySettings` | `ICacheContainer.DbCategorySettings`（`Lazy<DbCategorySettingsCache>`） |

行為：
- **20 秒 sliding expiration**：未存取超過 20 秒則重新載入
- **檔案變更監測**：底層檔案異動時快取自動失效
- **存檔即失效**：呼叫 `Save*` 後立即清除對應快取，下次 `Get*` 重新載入

### 4.3 常用查找運算

```csharp
// 取單一連線項目（DI 注入 IDatabaseSettingsProvider）
DatabaseItem item = dbSettingsProvider.GetItem("company_main");

// 取分類下所有表（透過索引器）
DbCategory company = catSettings.Categories!["company"];
foreach (var table in company.Tables!) { ... }
```

`IDatabaseSettingsProvider.GetItem` 在找不到 Id 時拋 `KeyNotFoundException`，呼叫端可依此判斷未知連線。

### 4.4 API 存取限制

| Settings | 可從 API 遠端存取？ |
|----------|-------------------|
| DatabaseSettings | ❌ 否（含敏感連線資訊，`SystemBusinessObject.GetDefine` 會拒絕非 local call） |
| DbCategorySettings | ✅ 是（無敏感資料，可透過 `RemoteDefineAccess` 取得） |

---

## 5. CategoryId 串接

`CategoryId` 是這套設計的**核心關聯鍵**，貫穿三層：

### 5.1 FormSchema 定義階段

每個 FormSchema 必須宣告所屬分類：

```xml
<FormSchema ProgId="EmployeeForm" CategoryId="company" ...>
  <FormTable TableName="ft_employee" ...>
    ...
  </FormTable>
</FormSchema>
```

落檔時由 [`LocalDefineAccess.SaveFormSchema`](../src/Bee.ObjectCaching/LocalDefineAccess.cs) 強制檢查 `CategoryId` 非空（透過 [`TableSchemaGenerator.GetCategoryId`](../src/Bee.Definition/Database/TableSchemaGenerator.cs)），否則拋 `InvalidOperationException`。

### 5.2 TableSchema 落檔路徑

FormSchema 衍生的 TableSchema 依 CategoryId 分目錄存放：

```text
<DefinePath>/TableSchema/
              ├── common/
              │     ├── st_user.TableSchema.xml
              │     └── st_session.TableSchema.xml
              ├── company/
              │     ├── ft_department.TableSchema.xml
              │     └── ft_employee.TableSchema.xml
              └── log/
```

路徑解析：[`PathOptions.GetTableSchemaFilePath(categoryId, tableName)`](../src/Bee.Definition/PathOptions.cs)（DI ctor 注入）。

### 5.3 部署階段：實體 DB 的表清單推導

`DatabaseItem.CategoryId` 在 schema 部署階段（建立或升級實體資料庫表結構時）使用。**運算單位是 DatabaseItem**：對每筆 DatabaseItem 各自跑一次推導與建表流程，建到該 DatabaseItem 連線指向的實體 DB 上。

對單筆 DatabaseItem 的推導流程：

1. 讀 `DatabaseItem.CategoryId`（如 `"company"`）
2. 從 `DbCategorySettings.Categories["company"].Tables` 取得該分類下登錄的表清單
3. 對每個 `TableName`，從 `<DefinePath>/TableSchema/company/{TableName}.TableSchema.xml` 取得實體表結構
4. 透過該 DatabaseItem 的連線資訊在實體 DB 上執行 DDL（建表或 schema 升級）

```text
[DatabaseItem]            [DbCategorySettings]              [TableSchema 檔案]
 └─ CategoryId ─────────► DbCategory[id]                    └─ TableSchema/{cid}/{table}.xml
   ("company")             └─ Tables (該分類應有的表清單)        └─ 提供實體表結構
```

由於對每筆 DatabaseItem 獨立推導，兩種部署狀況（單一實體 DB / 多個實體 DB，見 §1）的處理流程完全相同：

- **狀況 1**（3 筆 DatabaseItem 都指向同一個實體 DB）：跑 3 次推導，3 次都連到同一個實體 DB，建出 common / company / log 三類表共存於該 DB
- **狀況 2**（3 筆 DatabaseItem 指向 3 個獨立實體 DB）：跑 3 次推導，分別建到 3 個實體 DB，每個實體 DB 只有對應分類的表

此設計讓 schema 定義（哪些表、表的結構）與實體部署（要不要切分、切幾個 DB）完全解耦。新增一個邏輯分類只需動 `DbCategorySettings.xml` 與對應 FormSchema；改變實體部署切分只需動 `DatabaseSettings.xml` 中各 DatabaseItem 的連線資訊，定義檔不必動。

### 5.4 執行時：資料庫存取

⚠️ **執行時取得連線完全透過 `DatabaseItem.Id`，與 `DbCategorySettings` 完全無關**：

```csharp
DatabaseItem item = dbSettingsProvider.GetItem(databaseId);
// 用 item.ConnectionString / DbName / UserId / Password 建立連線、執行 SQL
```

業務程式存取資料時依「資料屬於哪個邏輯分類」 + 「當前情境（租戶、時間等）」決定要傳哪個 `databaseId`：

| 要存取的資料 | 屬於分類 | 部署情境 | 使用的 DatabaseId |
|------------|---------|---------|------------------|
| `st_user`、`st_session` | common | 任何 | `common` 分類那筆 DatabaseItem 的 `Id` |
| `ft_employee`、`ft_department` | company | 單一公司 | `company` 分類那筆的 `Id` |
| `ft_employee`、`ft_department` | company | 多租戶 | 依當前租戶選 `companyXXX` 對應的 `Id`（如 `$"company{tenantId:D3}"`） |
| 日誌寫入 | log | 單一 DB | `log` 分類那筆的 `Id` |
| 日誌寫入 | log | 按年封存 | 依當前年份選 `log_YYYY`（如 `$"log_{DateTime.UtcNow.Year}"`） |
| 日誌跨年查詢 | log | 按年封存 | 依查詢年份範圍取多筆 DatabaseId，分別查詢後聚合 |

無論底層是哪種狀況，業務端程式碼都是同一個入口 `IDatabaseSettingsProvider.GetItem(databaseId)`，差異只在「依當前情境推導 databaseId 字串」這一步。

對於 bo repo（BO 層消費的 Repository），框架透過 `IRepositoryDatabaseRouter`（見 [ADR-010 §「後續延伸：執行時路由」](adr/adr-010-logical-database-category.md)）統一推導，BO 程式碼不需手寫：

| 來源 | databaseId 推導方式 |
|------|---------------------|
| `DbScope.Common` | 固定字串 `"common"`（不需 session） |
| `DbScope.Log` | 固定字串 `"log"`（不需 session；Login / Logout 等 pre-EnterCompany 方法也能寫 audit log） |
| `DbScope.Company` | `SessionInfo.CompanyId`（由 `EnterCompany` 寫入）→ `CompanyInfo.CompanyDatabaseId`（由 `ICompanyInfoService` 取得） |

BO 方法透過 `BusinessObject.ResolveDatabaseId(DbScope)` 或（FormSchema-driven CRUD 用）`CreateDataFormRepository(progId)` helper 消費 router；後者依 schema 的 `CategoryId` 自動路由。

跨 DatabaseItem 聚合（如 log 跨年查詢）與封存資料的明確存取（如查 `log_2024`）不在預設路由範圍內——業務層直接指定目標 databaseId、透過 `IDbAccessFactory` 建自訂 bo repo。

CategoryId 與 DbCategorySettings 只在前述的設計階段（5.1–5.2）與部署階段（5.3）使用。執行時 BO 方法用 `DbScope`（執行時存取意圖）表達，不再操作 CategoryId 字串。

---

## 6. 檔案位置與範例

### 6.1 檔案路徑

兩個設定檔皆位於 `PathOptions.DefinePath` 根目錄：

| 設定 | 檔案路徑 | 路徑解析 |
|------|---------|---------|
| DatabaseSettings | `<DefinePath>/DatabaseSettings.xml` | `PathOptions.GetDatabaseSettingsFilePath()` |
| DbCategorySettings | `<DefinePath>/DbCategorySettings.xml` | `PathOptions.GetDbCategorySettingsFilePath()` |

### 6.2 DatabaseSettings.xml 範例

```xml
<?xml version="1.0" encoding="utf-8"?>
<DatabaseSettings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                  xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Servers>
    <DatabaseServer Id="sql_main" DisplayName="主要 SQL Server"
                    DatabaseType="SQLServer"
                    ConnectionString="Server=sql.example;Database={@DbName};User ID={@UserId};Password={@Password};" />
  </Servers>
  <Items>
    <!-- common 分類：單一實體載體，Id 與 CategoryId 同名 -->
    <DatabaseItem Id="common" CategoryId="common" DisplayName="共用資料庫"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="erp_common" UserId="erp_user"
                  Password="enc:base64encodeddata..." />

    <!-- company 分類：多租戶切分，每家公司一筆 -->
    <DatabaseItem Id="company001" CategoryId="company" DisplayName="01 公司資料庫"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="company001" UserId="erp_user"
                  Password="enc:base64encodeddata..." />
    <DatabaseItem Id="company002" CategoryId="company" DisplayName="02 公司資料庫"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="company002" UserId="erp_user"
                  Password="enc:base64encodeddata..." />

    <!-- log 分類：時間封存切分，每年一筆 -->
    <DatabaseItem Id="log2025" CategoryId="log" DisplayName="2025 記錄資料庫"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="log2025" UserId="erp_user"
                  Password="enc:base64encodeddata..." />
    <DatabaseItem Id="log2026" CategoryId="log" DisplayName="2026 記錄資料庫"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="log2026" UserId="erp_user"
                  Password="enc:base64encodeddata..." />
  </Items>
</DatabaseSettings>
```

### 6.3 DbCategorySettings.xml 範例

```xml
<?xml version="1.0" encoding="utf-8"?>
<DbCategorySettings xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Categories>
    <DbCategory Id="common" DisplayName="共用資料庫">
      <Tables>
        <TableItem TableName="st_user" DisplayName="用戶" />
        <TableItem TableName="st_session" DisplayName="連線資料" />
      </Tables>
    </DbCategory>
    <DbCategory Id="company" DisplayName="公司資料庫">
      <Tables>
        <TableItem TableName="ft_department" DisplayName="部門" />
        <TableItem TableName="ft_employee" DisplayName="員工" />
        <TableItem TableName="ft_project" DisplayName="專案" />
      </Tables>
    </DbCategory>
    <DbCategory Id="log" DisplayName="日誌資料庫">
      <Tables />
    </DbCategory>
  </Categories>
</DbCategorySettings>
```

---

## 相關文件

- [架構總覽](architecture-overview.zh-TW.md) — Definition-Driven 架構全貌
- [開發指引](development-cookbook.md) — 框架初始化順序與開發流程
- [資料庫命名規範](database-naming-conventions.md) — 表名 / 欄位命名規則
- [ADR-005：FormSchema 定義驅動架構](adr/adr-005-formschema-driven.md)
- [ADR-010：邏輯資料庫分類設計](adr/adr-010-logical-database-category.md) — 為何引入 DbCategory 抽象層
