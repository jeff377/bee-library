# 計畫：Bee.Db.UnitTests 測試環境建置

## 目標

使 `Bee.Db.UnitTests` 的所有測試方法（`[LocalOnlyFact]` 及純邏輯 `[Fact]`）全部通過，並利用 `TableSchemaBuilder` 實現「資料庫空結構時自動建立資料表」的機制。

## 分析

### 測試依賴

| 測試類別 | 所需資料表 | 需要測試資料 |
|----------|-----------|-------------|
| `DbConnectionTests` | 無（只測連線） | 否 |
| `DbAccessTests` | `st_user` | 是（`sys_id='001'`） |
| `SqlTableSchemaProviderTests` | `st_user`（需存在） | 否 |
| `BuildSelectCommandTests` | 無（只建 SQL，不執行） | 否 |
| `TableSchemaBuilderTests`（新增） | `st_user` | 否 |
| `WhereBuilderTests` | 無 | 否 |
| `SortBuilderTests` | 無 | 否 |
| `DbAccessLoggerTests` | 無 | 否 |
| `DbFuncTests` | 無 | 否 |

### 測試對測試資料的具體要求

- `ExecuteDataTableAsync_ValidQuery_ReturnsNonEmptyDataTable`：`st_user` 需有 **≥1 筆資料**
- `UpdateDataTable_ModifiedRow_AffectsRows`：`st_user` 需有 **≥1 筆資料**
- `ExecuteNonQuery_UpdateRow_Executes`：需有 `sys_id='001'` 的資料列
- `ExecuteNonQueryAsync_UpdateRow_Executes`：同上
- `ExecuteScalar_SelectSingleValue_ReturnsScalar`：同上
- `ExecuteBatch_WithTransaction_Succeeds`：同上
- `ExecuteBatchAsync_WithTransaction_Succeeds`：同上

## 架構設計

### 為何不直接修改 `GlobalFixture`

`GlobalFixture`（位於 `Bee.Tests.Shared`）是所有測試專案共用的基礎設施，不應引入 `Bee.Db` 的具體相依，以保持通用性。

### 採用繼承擴充方式

在 `Bee.Db.UnitTests` 專案內建立 `DbGlobalFixture : GlobalFixture`，繼承基礎初始化後，追加：
1. 呼叫 `TableSchemaBuilder.Execute()` 自動建立資料表（表不存在時建立，已存在且一致時跳過）
2. 插入最小必要的種子資料（`sys_id='001'`）

同時修改 `GlobalCollection` 改用 `DbGlobalFixture`。

### 流程圖

```
GlobalFixture()              ← 來自 Bee.Tests.Shared
  └─ 設定 DefinePath
  └─ 初始化 BackendInfo
  └─ 註冊 SQL Server Provider
  └─ 從環境變數載入連線字串

DbGlobalFixture() : base()   ← 新增於 Bee.Db.UnitTests
  └─ 呼叫 base() (上述所有步驟)
  └─ EnsureTestDatabase()
       └─ TableSchemaBuilder.Execute("common", "st_user")
       └─ TableSchemaBuilder.Execute("common", "st_session")
       └─ 若 st_user 無 sys_id='001'，則 INSERT 種子資料
```

## 執行步驟

### 步驟 1：新增 `DbGlobalFixture.cs`

**路徑**：`tests/Bee.Db.UnitTests/DbGlobalFixture.cs`

```csharp
using Bee.Base;
using Bee.Db.DbAccess;
using Bee.Db.Schema;
using Bee.Tests.Shared;
using DbAccessObject = Bee.Db.DbAccess.DbAccess;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 繼承 GlobalFixture，在基礎初始化後自動建立測試所需的資料表結構與種子資料。
    /// </summary>
    public class DbGlobalFixture : GlobalFixture
    {
        public DbGlobalFixture() : base()
        {
            // 僅在資料庫連線已設定時執行
            var connStr = Environment.GetEnvironmentVariable("BEE_TEST_DB_CONNSTR");
            if (string.IsNullOrEmpty(connStr)) return;
            EnsureTestDatabase();
        }

        private static void EnsureTestDatabase()
        {
            // 根據 TableSchema 定義自動建立/更新資料表結構
            var schemaBuilder = new TableSchemaBuilder("common");
            schemaBuilder.Execute("common", "st_user");
            schemaBuilder.Execute("common", "st_session");

            // 插入種子資料：確保 sys_id='001' 的測試用戶存在
            var dbAccess = new DbAccessObject("common");
            var checkCmd = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001");
            var result = dbAccess.Execute(checkCmd);
            if (BaseFunc.CInt(result.Scalar) == 0)
            {
                var insertCmd = new DbCommandSpec(DbCommandKind.NonQuery,
                    "INSERT INTO st_user (sys_rowid, sys_id, sys_name, password, email, note, sys_insert_time) " +
                    "VALUES (NEWID(), {0}, {1}, '', '', '', GETDATE())",
                    "001", "測試管理員");
                dbAccess.Execute(insertCmd);
            }
        }
    }
}
```

### 步驟 2：修改 `GlobalCollection.cs`

改用 `DbGlobalFixture`（僅 `Bee.Db.UnitTests` 內的 `GlobalCollection`）：

```csharp
[CollectionDefinition("Initialize")]
public class GlobalCollection : ICollectionFixture<DbGlobalFixture>
{
    // 不需要任何程式碼
}
```

### 步驟 3：新增 `TableSchemaBuilderTests.cs`

測試 `TableSchemaBuilder` 的核心行為：

```csharp
[Collection("Initialize")]
public class TableSchemaBuilderTests
{
    [LocalOnlyFact]
    [DisplayName("TableSchemaBuilder 比對結構一致的資料表應回傳 None")]
    public void Compare_UpToDateTable_ReturnsNoneAction()

    [LocalOnlyFact]
    [DisplayName("TableSchemaBuilder 取得命令文字應回傳空字串（結構已同步）")]
    public void GetCommandText_UpToDateTable_ReturnsEmpty()

    [LocalOnlyFact]
    [DisplayName("TableSchemaBuilder Execute 結構已同步時應回傳 false")]
    public void Execute_UpToDateTable_ReturnsFalse()
}
```

### 步驟 4：建置並執行測試

```bash
dotnet build tests/Bee.Db.UnitTests/Bee.Db.UnitTests.csproj --configuration Release
dotnet test tests/Bee.Db.UnitTests/Bee.Db.UnitTests.csproj --configuration Release --settings .runsettings --verbosity normal
```

## 注意事項

- `TableSchemaBuilder.Execute()` 使用 `IF NOT EXISTS` 邏輯（透過 `SqlTableSchemaProvider.TableExists`），重複執行安全
- `st_session` 表目前沒有測試直接使用，但為系統完整性仍需建立
- 若 `common` 資料庫本身不存在，需手動先建立資料庫（`CREATE DATABASE common`）
- xUnit 在同一 assembly 內，同名 `[CollectionDefinition("Initialize")]` 以本 assembly 的定義優先，`Bee.Db.UnitTests` 的 `GlobalCollection` 會覆蓋 `Bee.Tests.Shared` 的定義
