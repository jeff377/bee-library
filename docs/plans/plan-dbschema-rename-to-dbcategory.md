# 計畫：DbSchema 結構重命名為 DbCategory

**狀態：📝 擬定中**

## 背景

### 命名歧義

`DbSchemaSettings` 體系目前借用「Schema」一詞來表達「邏輯資料庫分類」(common / company / log) 概念，與下列既有命名形成衝突：

| 名稱 | 在本專案的含義 | 與 DbSchema 的衝突 |
|------|---------------|-------------------|
| `TableSchema` | 一張資料表的完整結構（欄位、PK、索引） | 兩者都用 "Schema" 但層次不同 |
| SQL 世界的 schema | namespace（`dbo` / `public`） | 與本專案 DbSchema 概念無關 |
| `DatabaseSettings.DatabaseId` | 實體連線代碼（部署期決定） | 邏輯分類 ID 不能借用 |

讀者看到 `DbSchema` 直覺會聯想為「整個資料庫的綱要」，但它實際上只是個分類節點，不含結構資訊；真正承載結構的是 `TableSchema`。

### 現況確認

`DbSchema` / `DbSchemaSettings` 目前**沒有 production 業務消費者**，只有框架層 (`IDefineAccess` / `LocalDefineAccess` / `RemoteDefineAccess` / `CacheContainer` / `FileDefineStorage`) 與測試引用。改動相對安全，可一次完成，不需保留向下相容。

### 命名候選評估

| 候選 | 中文 | 評估 |
|------|------|------|
| `DbCategory` | 資料庫類別 | ✅ 直覺、無衝突，common/company/log 本質是分類 |
| `DbScope` | 資料庫範圍 | ⚠️ 容易與「runtime scope」混淆 |
| `DbGroup` | 資料庫分組 | ⚠️ 弱於 Category，含義較鬆 |
| `LogicalDatabase` | 邏輯資料庫 | ⚠️ 最精確但太長，呼叫端冗餘 |

**結論：採 `DbCategory`**

## 目標命名映射

### 類別與屬性

| 現有 | 新名 | 備註 |
|------|------|------|
| `DbSchemaSettings` | `DbCategorySettings` | 根容器 |
| `DbSchemaCollection` | `DbCategoryCollection` | DbCategory 的集合 |
| `DbSchema` | `DbCategory` | 邏輯資料庫類別節點 |
| `DbSchema.DbName` | `DbCategory.Id` | 類別代碼（"common" / "company" / "log"），key 對應 `KeyCollectionItem.Key` |
| `DbSchemaSettings.Databases` | `DbCategorySettings.Categories` | 與類別名一致 |
| `TableItem` | 不動 | 維持「資料表節點」語意，對應 `TableSchema` |
| `TableItemCollection` | 不動 | 與上同 |
| `TableItemCollection(DbSchema category)` | 參數名 `dbCategory`（建構子簽章本來就要因為型別更名而改） | — |

### 檔案

| 現有 | 新名 |
|------|------|
| `src/Bee.Definition/Settings/DbSchemaSettings/` (資料夾) | `src/Bee.Definition/Settings/DbCategorySettings/` |
| `DbSchemaSettings.cs` | `DbCategorySettings.cs` |
| `DbSchema.cs` | `DbCategory.cs` |
| `DbSchemaCollection.cs` | `DbCategoryCollection.cs` |
| `TableItem.cs` | 不動 |
| `TableItemCollection.cs` | 不動（內部 `DbSchema` 型別參數需改 `DbCategory`） |
| `tests/Define/DbSchemaSettings.xml` | `tests/Define/DbCategorySettings.xml` |
| `src/Bee.ObjectCaching/Define/DbSchemaSettingsCache.cs` | `DbCategorySettingsCache.cs` |

### 列舉與 API

| 現有 | 新名 |
|------|------|
| `DefineType.DbSchemaSettings` | `DefineType.DbCategorySettings` |
| `IDefineAccess.GetDbSchemaSettings()` | `GetDbCategorySettings()` |
| `IDefineAccess.SaveDbSchemaSettings(DbSchemaSettings)` | `SaveDbCategorySettings(DbCategorySettings)` |
| `IDefineStorage.GetDbSchemaSettings()` / `SaveDbSchemaSettings(...)` | 同上 |
| `CacheContainer.DbSchemaSettings` | `CacheContainer.DbCategorySettings` |
| `DefinePathInfo.GetDbTableSettingsFilePath()` | `GetDbCategorySettingsFilePath()`（順便修正既有命名與內容對不起來的問題） |

### 參數名（GetTableSchema / GetTableSchemaFilePath 系列）

`dbName` 在這些 method 內實際上傳的就是 `DbSchema.DbName`（即新的 `DbCategory.Id`）。一併改名以保持語意一致：

- `IDefineAccess.GetTableSchema(string dbName, ...)` → `GetTableSchema(string categoryId, ...)`
- `IDefineAccess.SaveTableSchema(string dbName, ...)` → `SaveTableSchema(string categoryId, ...)`
- `RemoteDefineAccess.GetTableSchema(string dbName, ...)` → 同上
- `LocalDefineAccess.GetTableSchema(...)` → 同上
- `DefinePathInfo.GetTableSchemaFilePath(string dbName, string tableName)` → `GetTableSchemaFilePath(string categoryId, string tableName)`
- `Bee.Db.Schema.TableSchemaBuilder` 系列方法 → 參數名 `categoryId`

> ⚠️ 注意：實體目錄結構 `Define/TableSchema/<categoryId>/<table>.TableSchema.xml` 不變（`<categoryId>` 仍然是 "common" / "company" 等字串值），只是參數命名語意對齊。

## 影響檔案清單

### Production code（13 個檔案 + 1 個資料夾改名）

| 檔案 | 改動類型 |
|------|---------|
| `src/Bee.Definition/Settings/DbSchemaSettings/DbSchemaSettings.cs` | 移到新資料夾 + 重命名 + 內容更新 |
| `src/Bee.Definition/Settings/DbSchemaSettings/DbSchema.cs` | 同上 |
| `src/Bee.Definition/Settings/DbSchemaSettings/DbSchemaCollection.cs` | 同上 |
| `src/Bee.Definition/Settings/DbSchemaSettings/TableItem.cs` | 移到新資料夾 |
| `src/Bee.Definition/Settings/DbSchemaSettings/TableItemCollection.cs` | 移到新資料夾 + 建構式型別參數改名 |
| `src/Bee.Definition/DefineType.cs` | enum 值改名 |
| `src/Bee.Definition/DefineTypeExtensions.cs` | enum mapping 更新 |
| `src/Bee.Definition/DefinePathInfo.cs` | method 改名 + 參數改名 + xml 檔名 |
| `src/Bee.Definition/Storage/IDefineAccess.cs` | method 簽章更新 |
| `src/Bee.Definition/Storage/IDefineStorage.cs` | method 簽章更新 |
| `src/Bee.Definition/Storage/FileDefineStorage.cs` | 實作對齊 |
| `src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs` | 實作對齊 |
| `src/Bee.ObjectCaching/CacheContainer.cs` | 屬性改名 |
| `src/Bee.ObjectCaching/Define/DbSchemaSettingsCache.cs` | 重命名 |
| `src/Bee.ObjectCaching/LocalDefineAccess.cs` | 實作對齊 |
| `src/Bee.Db/Schema/TableSchemaBuilder.cs` | 參數名 `dbName` → `categoryId` |
| 其他用到 `dbName` 參數的 `Bee.Db` 檔案 | 同上（執行時統一掃描補齊）|

### Test code（14 個檔案）

| 檔案 | 改動 |
|------|------|
| `tests/Bee.Api.Client.UnitTests/RemoteDefineAccessTests.cs` | symbol 更新 |
| `tests/Bee.Business.UnitTests/Fakes/FakeDefineAccess.cs` | interface 對齊 |
| `tests/Bee.Business.UnitTests/SystemBusinessObjectDefineTests.cs` | symbol 更新 |
| `tests/Bee.Definition.UnitTests/BackendInfoTests.cs` | symbol 更新 |
| `tests/Bee.Definition.UnitTests/DefinePathInfoTests.cs` | method 名與檔名更新 |
| `tests/Bee.Definition.UnitTests/DefineTypeExtensionsTests.cs` | enum 值更新 |
| `tests/Bee.Definition.UnitTests/DtoSerializationTests.cs` | type 更新 |
| `tests/Bee.Definition.UnitTests/Settings/DbSchemaTests.cs` | 重命名為 `DbCategoryTests.cs`，內容更新 |
| `tests/Bee.Definition.UnitTests/Settings/TableItemTests.cs` | 不動（class 沒改）|
| `tests/Bee.Definition.UnitTests/Storage/FileDefineStorageTests.cs` | symbol 更新 |
| `tests/Bee.ObjectCaching.UnitTests/CacheContainerTests.cs` | symbol 更新 |
| `tests/Bee.ObjectCaching.UnitTests/LocalDefineAccessSaveTests.cs` | symbol 更新 |
| `tests/Bee.ObjectCaching.UnitTests/LocalDefineAccessTests.cs` | symbol 更新 |
| `tests/Bee.Tests.Shared/TempDefinePath.cs` | doc 更新 |
| `tests/Define/DbSchemaSettings.xml` | 改名 + 內容重寫 |

### 文件

| 檔案 | 改動 |
|------|------|
| `docs/terminology.md` | 加入 `DbCategorySettings` / `DbCategory` 條目；更新「定義類型」與「設定檔」表格 |
| `src/Bee.Definition/README.md` / `README.zh-TW.md` | 引用更新 |
| `src/Bee.ObjectCaching/README.md` / `README.zh-TW.md` | 引用更新 |
| `.claude/rules/testing.md` | `DbSchemaSettings.xml` 範例更新為 `DbCategorySettings.xml` |

> `.claude/logs/*` 為歷史記錄，不修改。
> `docs/plans/plan-xmltype-cleanup.md` 若已標記完成則維持原樣。

## 執行步驟

### Step 1 — 核心類別與資料夾重命名

1. 建立新資料夾 `src/Bee.Definition/Settings/DbCategorySettings/`
2. 將 5 個檔案移入並重命名：
   - `DbSchemaSettings.cs` → `DbCategorySettings.cs`
   - `DbSchema.cs` → `DbCategory.cs`
   - `DbSchemaCollection.cs` → `DbCategoryCollection.cs`
   - `TableItem.cs`、`TableItemCollection.cs` 直接搬
3. 更新檔案內容：類別名、屬性名、引用、`[Description]` / `[TreeNode]` 文字
4. 移除空資料夾 `DbSchemaSettings/`

### Step 2 — Definition 層擴散

5. `DefineType.cs`：`DbSchemaSettings` → `DbCategorySettings`
6. `DefineTypeExtensions.cs`：mapping 對齊
7. `DefinePathInfo.cs`：
   - `GetDbTableSettingsFilePath()` → `GetDbCategorySettingsFilePath()`，回傳 `DbCategorySettings.xml`
   - `GetTableSchemaFilePath` 參數 `dbName` → `categoryId`
8. `IDefineAccess.cs` / `IDefineStorage.cs`：method 簽章更新
9. `FileDefineStorage.cs`：實作對齊

### Step 3 — Caching / Client / Db 擴散

10. `CacheContainer.cs`：`DbSchemaSettings` 屬性 → `DbCategorySettings`
11. `DbSchemaSettingsCache.cs` → `DbCategorySettingsCache.cs`，內容對齊
12. `LocalDefineAccess.cs` / `RemoteDefineAccess.cs`：實作對齊
13. `Bee.Db/Schema/TableSchemaBuilder.cs` 與相關呼叫端：`dbName` 參數 → `categoryId`

### Step 4 — 測試對齊

14. 更新 14 個測試檔案的 symbol
15. `DbSchemaTests.cs` 重命名為 `DbCategoryTests.cs`
16. `tests/Define/DbSchemaSettings.xml` → `DbCategorySettings.xml`，內容重寫：

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <DbCategorySettings xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
      <Categories>
        <DbCategory Id="common" DisplayName="共用資料庫">
          <Tables>
            <TableItem TableName="st_user" DisplayName="用戶" />
            <TableItem TableName="st_session" DisplayName="連線資料" />
          </Tables>
        </DbCategory>
      </Categories>
    </DbCategorySettings>
    ```

### Step 5 — 文件對齊

17. `docs/terminology.md`：加入新條目、移除舊條目
18. README（Bee.Definition、Bee.ObjectCaching 各兩語版）
19. `.claude/rules/testing.md` 範例

### Step 6 — 驗證

20. `dotnet build --configuration Release`
21. `./test.sh`（本機 SQL Server / PostgreSQL container 啟動下）
22. 抽查若干關鍵測試：`DtoSerializationTests` round-trip、`CacheContainerTests`、`LocalDefineAccessSaveTests`、`DbCategoryTests`

## 風險與相容性

- **無向下相容需求** — 既然 `DbSchema` 無實際 production 消費者，可一次斷掉所有舊命名。
- **既有 production XML 檔案** —— 若部署環境已有 `DbSchemaSettings.xml`，需手動 rename 為 `DbCategorySettings.xml` 並更新 root element / 子元素標籤；可寫一次性遷移腳本，但不納入此 plan（按使用者目前說法是無此資料）。
- **build-ci.yml** —— 預期 strict build 仍能通過；唯一風險是漏改某個引用點。執行步驟結尾的 build + test 為把關。
- **SonarCloud** —— 改動只是 rename，不引入新 code smell；rename 後 coverage line 計算會有少量浮動，可接受。

## 不在本 plan 範圍

- 設計 `DbCategorySettings` 與 `DatabaseSettings` 之間的對映機制（邏輯類別 ↔ 實體連線）。目前推測由 `DatabaseSettings` 單向決定，並未顯式對映；若後續需要顯式對映表，另立 plan。
- `TableItem` 與 `TableSchema` 之間的對應關係改造（例：是否要在 `TableItem` 直接嵌入 schema reference）。維持現狀。
