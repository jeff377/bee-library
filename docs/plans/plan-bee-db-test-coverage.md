# Bee.Db 測試覆蓋率提升計畫書

**狀態：✅ 已完成（2026-04-18）**

## 完成摘要

- **新增測試**：階段 1（13）+ 階段 2（74）+ 階段 3（31）= **118 個新測試**
- **Bee.Db.UnitTests 總計**：248 通過 / 22 略過（CI 模式）
- **新增測試檔（共 11 個）**：
  - 階段 1：`DbCommandSpecTests`（補強）、`TableSchemaComparerTests`（補強）
  - 階段 2：`DbCommandResultTests`、`DbParameterSpecTests`、`DbParameterSpecCollectionTests`、`DbConnectionScopeTests`、`DefaultParameterCollectorTests`、`FromBuilderTests`、`ILMapperTests`、`DbProviderManagerTests`、`SqlTableSchemaProviderStaticTests`、`SqlFormCommandBuilderTests`
  - 階段 3：`DbAccessExtraTests`（17 tests）、`SelectCommandBuilderTests`（5 tests）、`DbAccessLoggerTests`（補強 +3）、`TableJoinCollectionTests`（6 tests）
- **執行方式**：所有新測試皆為純邏輯測試，CI 模式（`CI=true`）下完整執行；不新增任何 `[LocalOnlyFact]`。

## 1. 背景與目標

`Bee.Db` 是 Bee.NET 框架的資料存取層，提供 ADO.NET 抽象、動態 SQL 生成、多資料庫識別符跳脫、以 IL emit 實現的 `DbDataReader → POCO` 高效映射、以及 Schema 探查與比較。下游被 `Bee.Repository`、`Bee.Business`、`Bee.Api.*` 等所有業務層套件依賴，是源碼掃描（SonarCloud / SAST）審視的高風險區。

現況觀察：

- 大量現有測試標記為 `[LocalOnlyFact]`／`[LocalOnlyTheory]`，在 CI（`CI=true`）時會被跳過，導致 SonarCloud 顯示的覆蓋率偏低。
- 部分純邏輯類別（`DbCommandSpec`、`DbCommandResult`、`DbParameterSpec`、`DefaultParameterCollector`、`FromBuilder`、`SelectBuilder` 等）完全可在無資料庫情況下測試，目前完全或部分缺漏。
- `DbAccessLogger`、`DbConnectionScope`、`InternalWhereBuilder` 邊界分支尚未涵蓋。

**目標**：

1. 在不依賴外部資料庫的前提下，補齊 `Bee.Db` 所有可純邏輯測試類別，使 SonarCloud 顯示的 `Bee.Db` Line Coverage 顯著提升（預期 ≥ 70%；扣除必須連 DB 的部分後 ≥ 85%）。
2. 所有 `public` 類別至少有一個對應測試檔。
3. 不新增任何需連接真實資料庫的測試；既有 `[LocalOnlyFact]` 測試保持原樣，不在本計畫範圍。
4. 通過源碼掃描所列規則（`scanning.md` / `sonarcloud.md`）。

## 2. 現況盤點

下表以「資料夾分組」列出 `Bee.Db` 所有公開類別與目前測試狀態。

圖例：✅ 已完整覆蓋 / ⚠ 部分覆蓋（多為 LocalOnly）／ ❌ 完全無測試

### 2.1 根目錄

| 類別 | 狀態 | 現有測試 | 備註 |
|------|------|----------|------|
| `DbAccess` | ⚠ | DbAccessTests（LocalOnly） | 純邏輯部分（建構子驗證、ToString、空 batch、null 參數）未測 |
| `DbFunc` | ⚠ | DbFuncTests | 已涵蓋 `QuoteIdentifier`、`GetParameterPrefix`；`InferDbType`、`GetParameterName`、`SqlFormat`、`GetSqlDefaultValue` 未測 |
| `DbCommandSpec` | ❌ | — | 三個建構子、佔位符解析（`{0}`、`{Name}`、`{{Name}}`、`{@Parameters}`）、`CommandTimeout` 限制邏輯未測 |
| `DbCommandResult` | ❌ | — | 三個工廠方法 `ForRowsAffected`、`ForScalar`、`ForTable` 未測 |
| `DbParameterSpec` | ❌ | — | 建構子推斷 DbType、`Name` 與 `Key` 同步、`ToString` 未測 |
| `DbParameterSpecCollection` | ❌ | — | `Add(name, value)` 與 `Add(DbField)` 未測 |
| `DbBatchSpec` / `DbBatchResult` / `DbCommandSpecCollection` / `DbCommandResultCollection` | ❌ | — | 屬性預設值即可 |
| `DbConnectionScope` | ❌ | — | 純邏輯：null factory、null connection 防護；對「擁有連線」與「外部連線」的 Dispose 行為差異 |
| `DataTableUpdateSpec` | ❌ | — | 屬性與預設值 |
| `TableSchemaCommandBuilder` | ❌ | — | `BuildInsertCommand`、`BuildUpdateCommand`、`BuildDeleteCommand`、`BuildUpdateSpec` 可用記憶體 `TableSchema` 測試 |
| `ILMapper<T>` | ❌ | — | `CreateMapFunc`／`MapToList`／`MapToEnumerable`／`ClearCache` 可用 `DataTable.CreateDataReader()` 測試 |
| `DbCommandKind` / `JoinType`（enum） | n/a | — | enum 不單獨測試 |
| `CommandTextVariable` | ❌ | — | 常數即可 |

### 2.2 Logging

| 類別 | 狀態 | 現有測試 | 備註 |
|------|------|----------|------|
| `DbAccessLogger` | ⚠ | DbAccessLoggerTests | `LogStart` 已測，`LogEnd` 與內部閾值分支未測 |
| `DbLogContext` | ❌ | — | 屬性預設值與計時器初始化 |

### 2.3 Manager

| 類別 | 狀態 | 現有測試 | 備註 |
|------|------|----------|------|
| `DbConnectionManager` | ❌ | — | `GetConnectionInfo` 快取／重複呼叫／`Remove`／`Clear`／`Contains`／`Count`、空字串拋例外 |
| `DbProviderManager` | ❌ | — | `RegisterProvider` 防 null、`GetFactory` 找不到時 `KeyNotFoundException` |
| `DbConnectionInfo` | ❌ | — | 透過 `DbConnectionManager` 間接覆蓋；屬性 getter |

### 2.4 Providers

| 類別 | 狀態 | 現有測試 | 備註 |
|------|------|----------|------|
| `SelectCommandBuilder` | ⚠ | BuildSelectCommandTests（LocalOnly） | 純邏輯部分（空 tableName 拋例外、找不到 table 拋例外）可不依賴 DB |
| `SqlFormCommandBuilder` | ⚠ | 同上 | `BuildInsertCommand`／`Update`／`Delete` 必拋 `NotSupportedException`；建構子 null 防護未測 |
| `SqlCreateTableCommandBuilder` | ❌ | — | 透過記憶體 `TableSchema` 測試 `GetCommandText`（New / Upgrade 分支）、`ConverDbType` 各型別、`GetDefaultValue` 各型別 |
| `SqlTableSchemaProvider` | ⚠ | LocalOnly | `GetFieldDbType` 與 `ParseDBDefaultValue` 為 `public static`，可純邏輯測試（覆蓋所有 SQL Server 型別映射分支） |
| `IFormCommandBuilder` / `ICreateTableCommandBuilder` | n/a | — | 介面 |

### 2.5 Query

| 類別 | 狀態 | 現有測試 | 備註 |
|------|------|----------|------|
| `WhereBuilder` | ✅ | WhereBuilderTests | 已涵蓋多數情境；可補 `>`、`<`、`>=`、`<=`、`Like`、`StartsWith`、`EndsWith`、`NotEqual`、`IS NOT NULL`、`Between` 缺第二值＋`IgnoreIfNull`、未知 operator 等剩餘分支 |
| `SortBuilder` | ✅ | SortBuilderTests | 已涵蓋；`selectContext` 重映射分支可補 |
| `FromBuilder` | ❌ | — | `null`／空 joins 路徑、單一 join、多 join 排序 |
| `SelectBuilder` | ❌ | — | 需要 `FormTable` fixture，可在 `tests/Define` 既有定義基礎上做 |
| `SelectContextBuilder` | ❌ | — | 邏輯複雜，非 LocalOnly 仍可測 alias 進位（`A → B → … → Z → ZA`）與 SQL keyword 跳過 |
| `SelectContext` / `QueryFieldMapping` / `QueryFieldMappingCollection` / `TableJoin` / `TableJoinCollection` / `WhereBuildResult` | ❌ | — | 屬性與 `ToString`、`FindRightAlias` |
| `DefaultParameterCollector` | ❌ | — | 建構子防 null/empty prefix、`Add` 編號、`GetAll` 內容 |
| `InternalWhereBuilder` | ❌ | — | 已被 `WhereBuilder` 間接覆蓋；剩餘錯誤分支（未知節點型別、IN 非 enumerable、空 FieldName）可直接針對 `WhereBuilder` 測 |
| `IFromBuilder` / `ISelectBuilder` / `ISortBuilder` / `IWhereBuilder` / `IParameterCollector` | n/a | — | 介面 |

### 2.6 Schema

| 類別 | 狀態 | 現有測試 | 備註 |
|------|------|----------|------|
| `TableSchemaBuilder` | ⚠ | TableSchemaBuilderTests（LocalOnly） | 主要走 DB 路徑，純邏輯部分有限 |
| `TableSchemaComparer` | ❌ | — | 可用記憶體 `TableSchema` 測試 New / Upgrade / 欄位差異 / 索引差異 / extension fields |

## 3. 測試缺口優先級

### P0（必補，純邏輯且影響 SonarCloud 覆蓋率最大）

| 項目 | 理由 |
|------|------|
| `DbCommandSpec` | 程式碼最大且含正規式解析；佔位符 bug 直接造成 SQL 注入或語法錯誤 |
| `DbFunc` 補完 | `InferDbType`／`SqlFormat`／`GetParameterName`／`GetSqlDefaultValue` 為 utility，全部可純邏輯測試 |
| `TableSchemaCommandBuilder` | INSERT/UPDATE/DELETE 命令生成是 CRUD 核心，必須有單元測試保護 |
| `SqlCreateTableCommandBuilder` | DDL 生成；`ConverDbType` 涵蓋所有 `FieldDbType` 分支才安全 |
| `TableSchemaComparer` | Schema 升級邏輯，純邏輯且容易設計 fixture |
| `WhereBuilder` 缺漏分支 | 補齊比較運算符與錯誤路徑 |

### P1（應補）

| 項目 | 理由 |
|------|------|
| `DbCommandResult` / `DbParameterSpec` / `DbParameterSpecCollection` | 結構簡單但被廣泛引用；測試成本低 |
| `DbConnectionScope` | null factory、null connection、`Dispose` 不關閉外部連線等防護 |
| `DefaultParameterCollector` | 純邏輯；含建構子防護分支 |
| `FromBuilder` | 單純的字串組合，搭配 `TableJoinCollection` 即可測 |
| `ILMapper<T>` | 用 `DataTable.CreateDataReader()` 即可建立記憶體 reader 進行測試 |
| `DbProviderManager` / `DbConnectionManager` | 靜態管理器，需注意全域狀態還原（測試結束 `Clear()`） |
| `SqlTableSchemaProvider` 純邏輯部分 | `GetFieldDbType`、`ParseDBDefaultValue` 為 `public static`，可表格驅動測試 |
| `SqlFormCommandBuilder` `NotSupportedException` 分支 | 三個方法各補一行測試即可 |

### P2（選補）

- `SelectCommandBuilder` 純邏輯防護（空 tableName、找不到 table）
- `SelectBuilder` / `SelectContextBuilder`（需 FormTable fixture）
- 剩餘 collection/DTO 屬性測試
- `DbAccessLogger.LogEnd` 閾值分支（涉及 `BackendInfo.LogOptions`，需注意全域狀態）

## 4. 實施階段

採三階段，每階段結束後執行 `dotnet test --configuration Release --settings .runsettings` 並觀察 SonarCloud 覆蓋率。

### 階段 1：P0 核心（預估 2~3 日）

1. **`DbCommandSpecTests`**
   - 三個建構子：空 commandText 拋 `ArgumentNullException`、positional values 自動命名為 `p0/p1...`、named parameters 寫入 `Parameters`
   - 佔位符解析：`{0}` → `@p0`、`{Name}` → `@Name`、`{{Name}}` → `{Name}`（escape）、`{@Parameters}` 展開為逗號清單
   - `CommandTimeout` setter：≤0 用預設、超過 cap 套用 cap、合法值原樣使用
   - `ResolveParameters` 例外：索引越界、空 name、找不到 named key
   - `CreateCommand`：`null` connection 拋例外、`StoredProcedure` 跳過解析、參數名不重複加 prefix
   - `ToString`

2. **`DbFuncTests` 擴充**
   - `InferDbType`：所有原生型別 + `null`/`DBNull` → `null` + 不支援型別 → `null`
   - `GetParameterName`：四種 DB
   - `SqlFormat`：用 `SqlClientFactory` 建立記憶體 `DbParameterCollection`
   - `GetSqlDefaultValue`：所有 `FieldDbType` 分支（透過 `SqlCreateTableCommandBuilder` 間接覆蓋亦可，但直接測較直觀；此方法為 internal，需透過 `InternalsVisibleTo` 或經由 `SqlCreateTableCommandBuilder` 間接）

3. **`TableSchemaCommandBuilderTests`**
   - 用記憶體 `TableSchema`（含 `sys_rowid` PK + 數個 `DbField`）測試三個 `Build*Command`
   - `BuildUpdateSpec` 包裝完整、null `DataTable` 拋例外
   - 兩個建構子（顯式 DatabaseType / 隱式取 BackendInfo）

4. **`SqlCreateTableCommandBuilderTests`**
   - `ConverDbType` 各 `FieldDbType` 分支：`String`、`Text`、`Boolean`、`AutoIncrement`、`Short`、`Integer`、`Long`、`Decimal`（自訂 precision/scale）、`Currency`、`Date`、`DateTime`、`Guid`、`Binary`
   - 不支援 `FieldDbType.Unknown` 拋例外
   - `GetCommandText`：`UpgradeAction.New` 走 `CREATE TABLE`、`UpgradeAction.Upgrade` 走 drop/create/insert/rename 五段
   - 主索引存在時包含 `CONSTRAINT ... PRIMARY KEY (...)` 字樣
   - 含獨立索引時包含 `CREATE INDEX`／`CREATE UNIQUE INDEX`

5. **`TableSchemaComparerTests`**
   - `realTable == null` → `UpgradeAction.New`
   - 完全相同 → `UpgradeAction.None`
   - 欄位差異 → `UpgradeAction.Upgrade` 並標記欄位 `New`／`Upgrade`
   - 索引差異 → `UpgradeAction.Upgrade` 並標記索引
   - 實際表中多出的欄位 → `AddExtensionFields` 帶入結果

6. **`WhereBuilderTests` 擴充**
   - 比較運算符：`>`、`<`、`>=`、`<=`、`!=` 各一條測試
   - `Like`、`StartsWith`、`EndsWith`
   - 值為 null 且 operator 為 `NotEqual` → `IS NOT NULL`
   - 值為 null 且 operator 不支援 → 拋 `InvalidOperationException`
   - `Between` 缺 `SecondValue` 且 `IgnoreIfNull=true` → 空字串
   - `Between` 缺 `SecondValue` 且 `IgnoreIfNull=false` → 拋例外
   - `IN` 傳入非 enumerable → 拋例外
   - 空 FieldName → 拋例外

### 階段 2：P1 公開 API（預估 2 日）

1. **`DbCommandResultTests`** — 三個 factory method 各回傳對應 Kind 與資料
2. **`DbParameterSpecTests`** — 建構子正確推斷 DbType、Name/Key 同步、`ToString`
3. **`DbParameterSpecCollectionTests`** — `Add(name, value)` 防空字串、`Add(DbField)` 設定來源欄位
4. **`DbConnectionScopeTests`**
   - `Create` null factory + null externalConnection → 拋例外
   - 外部連線：State=Open → 不重新開啟、Dispose 不關閉連線
   - 外部連線：State=Closed → 開啟後 Dispose 不關閉
   - 用 SQLite in-memory（`Microsoft.Data.Sqlite`）作為輕量測試 DbProviderFactory；若引入 nuget 嫌重，改用一個 fake `DbConnection` 實作
   - **替代方案**：直接以反射或測試替身（Moq 不在依賴內）實作最小 `DbConnection` mock；以 `System.Data.Common.DbProviderFactory` 子類別承載
   - **更佳替代**：跳過此檔，僅測 null 參數防護分支（避免引入 mock 框架）
5. **`DefaultParameterCollectorTests`**
   - 建構子 null/empty prefix → `ArgumentException`
   - `Add` 連續呼叫產生 `@p0`、`@p1`
   - `GetAll` 回傳值與 key 對應正確
6. **`FromBuilderTests`**
   - 空 joins → `FROM [Main] A`
   - 單一 join → 含 `LEFT JOIN [Right] B ON A.x = B.y`
   - 多 join 依 RightAlias 排序
7. **`ILMapperTests`**
   - 用 `DataTable.CreateDataReader()` 製作 reader
   - 屬性大小寫不一致仍能對映（`StringComparer.OrdinalIgnoreCase`）
   - DBNull 欄位不寫入屬性（保留預設值）
   - `MapToList` 與 `MapToEnumerable` 結果一致
   - `ClearCache` 與 `CacheCount`：建立 → cache > 0 → 清空 → 0
   - 沒有無參數建構子的型別 → 拋 `InvalidOperationException`
8. **`DbProviderManagerTests`**
   - `RegisterProvider` null factory → `ArgumentNullException`
   - `GetFactory` 未註冊 → `KeyNotFoundException`
   - 重複 `RegisterProvider` 後新值取代舊值
9. **`SqlTableSchemaProviderStaticTests`**
   - `GetFieldDbType` 各 SQL 型別分支（含 `NVARCHAR(-1)` → `Text`、`DECIMAL(19,4)` → `Currency`）
   - `ParseDBDefaultValue` 各型別分支（`NVARCHAR` 雙層剝除、`INT`／`BIT`／`DATETIME`、不支援型別 → 空字串、與內建預設一致 → 空字串）
10. **`SqlFormCommandBuilderTests`**
    - 三個 `BuildXxxCommand` 都拋 `NotSupportedException`
    - 建構子 null FormSchema → `ArgumentNullException`
    - 建構子 progID 不存在 → `ArgumentException`

### 階段 3：P2 補強與收斂（預估 1 日）

1. **`DbAccessTests` 擴充純邏輯部分**
   - 建構子：空 databaseId 拋 `ArgumentException`、null external connection 拋 `ArgumentNullException`
   - `Execute(null)` / `ExecuteBatch(null)` / `ExecuteBatch(空 commands)` / `UpdateDataTable(null)` 各拋對應例外
   - `ToString` 包含 DatabaseType 名稱
2. **`SqlFormCommandBuilder` / `SelectCommandBuilder` 純邏輯防護**
   - `Build` 空 tableName → `ArgumentException`
   - 找不到 table → `InvalidOperationException`
3. **`DbAccessLoggerTests` 擴充**
   - `LogEnd` null context → `ArgumentNullException`
   - `LogEnd` 在 `LogOptions == null` 時不擲例外
4. **`TableJoinCollectionTests`** — `FindRightAlias`：null/empty/找不到/找到
5. **覆蓋率收斂**
   - 本機產出 coverage 報告：
     ```bash
     dotnet test tests/Bee.Db.UnitTests/Bee.Db.UnitTests.csproj \
         --configuration Release \
         --settings .runsettings \
         /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
     ```
   - 對照 SonarCloud 的 Bee.Db Line Coverage，確認顯著提升
   - 對於走不到的 `[LocalOnlyFact]` 路徑（如 ADO.NET 連線、IL emit 對不存在 column 的處理），在 PR 描述中註記

## 5. 測試撰寫規範（遵循 `.claude/rules/testing.md`）

- 測試檔放在 `tests/Bee.Db.UnitTests/` 根目錄，檔名 `<SutClassName>Tests.cs`
- 方法命名：`<方法>_<情境>_<預期結果>`
- 使用 `[Fact]` / `[Theory]`；本計畫範圍內**不再新增** `[LocalOnlyFact]`
- 每個測試一律加 `[DisplayName]` 中文描述
- 測試傳入 `null` 驗邊界時使用 `null!`
- 不依賴外部資料庫；需要 `BackendInfo`／`SysInfo` 的測試使用 `[Collection("Initialize")]` 共用既有 `DbGlobalFixture`
- `DbProviderManager` 等靜態狀態相關測試：若會修改全域狀態，置於獨立 collection 避免汙染其他測試
- 安全相關測試：佔位符解析必須驗證**不會將 raw value 拼接進 SQL**

## 6. 風險與注意事項

1. **`DbConnectionManager` / `DbProviderManager` 為靜態類**：被全域 `DbGlobalFixture` 預先初始化。新增測試若呼叫 `Clear()` 或 `RegisterProvider()` 會汙染既有測試；建議只測「預期失敗」的分支（null、KeyNotFound），不破壞既有註冊。
2. **`DbCommandSpec.CreateCommand` 需要 `DbConnection`**：可透過 `Microsoft.Data.SqlClient.SqlConnection` 不開啟連線即可建立 `SqlCommand`；測試中只檢查 `CommandText`、`Parameters`，不執行。
3. **`ILMapper<T>` 的 IL emit**：`DataTable.CreateDataReader()` 產生的 `DataTableReader` 是 `DbDataReader` 的實作，可直接餵入。
4. **`TableSchemaComparer` 與 `TableSchema`**：需要建立 `TableSchema` 物件，注意 `Indexes` 與 `Fields` 的初始化順序。
5. **覆蓋率不等於品質**：每個測試至少 1 個 `Assert`；佔位符解析等安全相關測試需要驗證實際輸出而非僅檢查不擲例外。
6. **`DbFunc.GetSqlDefaultValue` 為 internal**：可透過已對 `Bee.Db.UnitTests` 開放的 `InternalsVisibleTo`（如已設定）直接測；若未設定，改透過 `SqlCreateTableCommandBuilder.GetCommandText` 間接覆蓋。需先確認 `Bee.Db.csproj` 是否有 `InternalsVisibleTo`。

## 7. 預期成果

- Bee.Db 覆蓋率從目前（受 LocalOnly 影響）的低水位顯著提升，預期 SonarCloud 顯示 ≥ 70%
- 所有 P0 類別 ≥ 90% 行覆蓋
- 新增約 10~12 個測試檔，約 80~120 個測試案例
- 通過源碼掃描（無新增 SonarCloud / Roslyn 警告）
- 對 `DbCommandSpec` 佔位符解析（最易出錯的安全相關邏輯）建立完整安全網

## 8. 執行後續

- 本計畫執行完畢且合併後，依 `CLAUDE.md` 工作流程由使用者要求才將本文件移至 `docs/plans/archive/`
- 若階段 1 完成後發現估時偏差過大，回報並調整階段 2、3 範圍
