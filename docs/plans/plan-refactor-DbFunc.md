# 計畫：重構 `DbFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> ⚠️ 執行中 `InferDbType` 從原計畫的 **path D**(搬入 `DbParameterSpec` 內 private static)改為 **path C**(獨立 `DbTypeMapper` 類)。原因:`DbParameterSpec` 已有公開屬性 `DbType`(型別 `System.Data.DbType?`),private static method 內 `DbType.String` 等引用會被優先解析為屬性而非 enum,造成 `CS0120` 編譯錯誤。命名衝突是 path D 不適用的訊號,改用獨立類更乾淨。下方 audit 表已更新為實際結果。

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Db/DbFunc.cs`(148 行,**6 個 public 方法**)

```csharp
namespace Bee.Db;

public static class DbFunc
{
    private static readonly Dictionary<DatabaseType, string> DbParameterPrefixes;
    private static readonly Dictionary<DatabaseType, Func<string, string>> QuoteIdentifiers;

    public static string GetParameterPrefix(DatabaseType);                          // dispatch table → prefix char
    public static string GetParameterName(DatabaseType, string);                    // prefix + name
    public static string QuoteIdentifier(DatabaseType, string);                     // DB-specific 跳脫
    public static DbType? InferDbType(object);                                      // CLR type → DbType
    public static DbConnection CreateConnection(string databaseId);                 // factory by ID
    public static string SqlFormat(string, DbParameterCollection);                  // {0}/{1} → param 名稱
}
```

> 主計畫進度表寫 7,實際 audit 後 6 個。

## Method Audit 表

| # | 方法簽章 | Prod callers | 處理路徑 | 新位置/名稱 |
|---|---------|------------|--------|------------|
| 1 | `GetParameterPrefix(DatabaseType)` | 2 | **B** | `DatabaseTypeExtensions.GetParameterPrefix(this DatabaseType)` |
| 2 | `GetParameterName(DatabaseType, string)` | 1 | **B** | `DatabaseTypeExtensions.GetParameterName(this DatabaseType, string)` |
| 3 | `QuoteIdentifier(DatabaseType, string)` | 10 | **B** | `DatabaseTypeExtensions.QuoteIdentifier(this DatabaseType, string)` |
| 4 | `InferDbType(object)` | 1(`DbParameterSpec`)| **C** | `DbTypeMapper.Infer(object)`(獨立類,避開 `DbType` 屬性 vs enum 命名衝突)|
| 5 | `CreateConnection(string)` | 1(`TableUpgradeOrchestrator`)| **D** | `DbConnectionManager.CreateConnection(string databaseId)` |
| 6 | `SqlFormat(string, DbParameterCollection)` | 0 | **A 刪除** | — |

6 個方法處理完後,`DbFunc.cs` 整個刪除。

### 1-3. `Get*Prefix` / `GetParameterName` / `QuoteIdentifier` — path B 細節

3 個都以 `DatabaseType` enum 為第一參數,自然轉擴充方法:

```csharp
// 改前
DbFunc.QuoteIdentifier(_databaseType, identifier)
DbFunc.GetParameterPrefix(_databaseType)
DbFunc.GetParameterName(DatabaseType, name)

// 改後
_databaseType.QuoteIdentifier(identifier)
_databaseType.GetParameterPrefix()
DatabaseType.GetParameterName(name)
```

**新類別**:`public static class DatabaseTypeExtensions`,放 `src/Bee.Db/DatabaseTypeExtensions.cs`,namespace `Bee.Db`。
- 兩個 private dictionary(`DbParameterPrefixes`、`QuoteIdentifiers`)隨方法搬入新類別,內容不動

**為何放 `Bee.Db` 而非 `Bee.Definition.Database`(`DatabaseType` 所在 namespace)**:
- 邏輯本身是 SQL syntax-specific(parameter prefix / identifier quoting),屬於 DB 訪問層
- `Bee.Db` 已 reference `Bee.Definition`,擴充方法放這側不會循環依賴
- `DatabaseType` 的擴充住在 `Bee.Db` 對使用者來說是「進入 DB 操作後才會用到」的合理位置

**CA1724 確認**:`DatabaseTypeExtensions` 不對應任何 BCL namespace 末段,安全。

### 4. `InferDbType(object)` — path D 細節

**現行 1 處 prod caller**(`DbParameterSpec.cs:27`):
```csharp
public DbParameterSpec(string name, object value)
{
    Name = name;
    Value = value;
    DbType = DbFunc.InferDbType(value);
}
```

**改後**:把 14 行 type-mapping 邏輯收進 `DbParameterSpec` 自己的 `private static`:
```csharp
public DbParameterSpec(string name, object value)
{
    Name = name;
    Value = value;
    DbType = InferDbType(value);
}

private static DbType? InferDbType(object value) { /* 14 行 */ }
```

**為何走 path D 而非 path C(`DbTypeMapper.Infer(...)` 等獨立類)**:
- 唯一 caller 就是 `DbParameterSpec.ctor`,語意上「值的 DbType 推斷」是 `DbParameterSpec` 的內部關注
- 抽獨立類等於建立另一個 type 卻只一個方法、一個 caller
- Body 14 行不算極短 helper,inline 到 ctor 會讓 ctor 失焦,所以保留為 private static method 而非 inline

**為何不走 path B(`object` 擴充)**:
- 同先前 `DateTimeFunc.IsDate(object)` 教訓 —— 擴充 `object` 污染所有 IntelliSense

### 5. `CreateConnection(string databaseId)` — path D 細節

**現行 1 處 prod caller**(`TableUpgradeOrchestrator.cs:98`)+ 多處測試 caller。

**改後**:搬入 `Bee.Db.Manager.DbConnectionManager`:
```csharp
public static class DbConnectionManager
{
    public static DbConnectionInfo GetConnectionInfo(string databaseId);   // 既有
    public static DbConnection CreateConnection(string databaseId);        // 新增,搬自 DbFunc
    public static bool Remove(string databaseId);                          // 既有
    public static void Clear();                                            // 既有
}
```

**為何選 `DbConnectionManager` 而非新建 `DbConnectionFactory`**:
- `DbConnectionManager` 已負責「依 databaseId 取得 connection info」(快取、設定變更時清空),`CreateConnection` 是這個職責的自然延伸
- 不另開 factory class,避免在「manager / factory / pool」之間的概念碎片化
- 方法內部用到的 `DbProviderRegistry`、`DbConnectionInfo` 都在同一 namespace,聚合度高

**caller 更新**:
- `TableUpgradeOrchestrator.cs:98`:`DbFunc.CreateConnection(databaseId)` → `DbConnectionManager.CreateConnection(databaseId)`
- 測試端 caller(8+ 處,跨 `DbGlobalFixture.cs`、`DbAccessTests.cs`、`DbAccessStringMethodTests.cs`、`DbAccessTransactionTests.cs`、`DbConnectionTests.cs`):同樣替換

### 6. `SqlFormat(string, DbParameterCollection)` — path A 刪除

**現況**:**0 個 prod caller**,僅 `DbFuncTests.cs` 內 2 個測試自我驗證。

**為何刪除**:
- 真正零用途,沒有任何生產端會把 SQL 模板的 `{0}`/`{1}` 替換成參數名(框架自己用 `DbCommandSpec` 的命名參數,不需 placeholder 替換)
- 保留未使用的 public API 是技術債,等真有需求再以正確設計加回

2 個 SqlFormat 測試一併刪除。

## 影響範圍

**全 repo grep `DbFunc` 結果(扣除 `bin/obj`)**:

| 類型 | 檔案 | 出現次數 |
|------|------|---------|
| 產品(類別定義) | `src/Bee.Db/DbFunc.cs` | 1 |
| 產品 caller(`QuoteIdentifier` x 10) | `src/Bee.Db/Dml/*` 7 個檔 | 10 |
| 產品 caller(`GetParameterPrefix` x 2) | `DbCommandSpec.cs`、`Dml/WhereBuilder.cs` | 2 |
| 產品 caller(`GetParameterName` x 1) | `Dml/TableSchemaCommandBuilder.cs` | 1 |
| 產品 caller(`InferDbType` x 1) | `DbParameterSpec.cs` | 1 |
| 產品 caller(`CreateConnection` x 1) | `Schema/TableUpgradeOrchestrator.cs` | 1 |
| 測試 | `tests/Bee.Tests.Shared/DbGlobalFixture.cs` | 8(`QuoteIdentifier` x 7、`CreateConnection` x 1) |
| 測試 | `tests/Bee.Db.UnitTests/*` 7+ 檔 | 多處(`CreateConnection` 居多) |
| 測試(自我測試) | `tests/Bee.Db.UnitTests/DbFuncTests.cs` | 14 |
| 文件 | `docs/plans/plan-funcs-to-net-idiomatic.md` | 1 |

合計 15 處生產端 caller + 30+ 處測試 caller。範圍偏大,但都是機械式 sed 替換。

## 執行步驟

### 1. 新增 `DatabaseTypeExtensions.cs`

`src/Bee.Db/DatabaseTypeExtensions.cs`(新檔):
- `public static class DatabaseTypeExtensions` 內 3 個 extension methods + 2 個 private dictionary
- XML doc 文字沿用原 `DbFunc` 註解

### 2. 在 `DbConnectionManager` 加 `CreateConnection`

`src/Bee.Db/Manager/DbConnectionManager.cs`:
- 新增 `public static DbConnection CreateConnection(string databaseId)`,body 完整搬自 `DbFunc.CreateConnection`
- 內部 `DbConnectionManager.GetConnectionInfo(...)` 從外部呼叫變自我呼叫(本來就是同 class)

### 3. 在 `DbParameterSpec` 加 `private static InferDbType`

`src/Bee.Db/DbParameterSpec.cs`:
- 新增 `private static DbType? InferDbType(object value)`,body 完整搬自 `DbFunc.InferDbType`
- ctor 內 `DbFunc.InferDbType(value)` 改 `InferDbType(value)`(自我呼叫)

### 4. 更新所有產品端 caller

15 處 `DbFunc.X` → 對應新位置:
- `QuoteIdentifier` x 10 → `_databaseType.QuoteIdentifier(...)` 等(extension 形式)
- `GetParameterPrefix` x 2 → `_databaseType.GetParameterPrefix()`
- `GetParameterName` x 1 → `DatabaseType.GetParameterName(...)`
- `CreateConnection` x 1 → `DbConnectionManager.CreateConnection(...)`
- `InferDbType` x 1 → 同類自我呼叫(已在步驟 3 處理)

### 5. 刪除 `DbFunc.cs`

```bash
git rm src/Bee.Db/DbFunc.cs
```

### 6. 更新測試端 caller(30+ 處)

#### 6a. 跨檔的 `DbFunc.X` 全替換(非 `DbFuncTests.cs`)

| 檔案 | 替換內容 |
|------|---------|
| `tests/Bee.Tests.Shared/DbGlobalFixture.cs` | `DbFunc.QuoteIdentifier(dbType, "x")` → `dbType.QuoteIdentifier("x")`;`DbFunc.CreateConnection(...)` → `DbConnectionManager.CreateConnection(...)` |
| `tests/Bee.Db.UnitTests/DbAccessStringMethodTests.cs` | `DbFunc.CreateConnection(...)` → `DbConnectionManager.CreateConnection(...)` |
| `tests/Bee.Db.UnitTests/DbAccessTests.cs` | 同上 |
| `tests/Bee.Db.UnitTests/DbAccessTransactionTests.cs` | 同上(4 處) |
| `tests/Bee.Db.UnitTests/DbConnectionTests.cs` | 同上 |
| `tests/Bee.Db.UnitTests/OracleFormCommandBuilderTests.cs` | 註解 cref 改 `DatabaseTypeExtensions.QuoteIdentifier` 與 `DatabaseTypeExtensions.GetParameterPrefix`(裡面還誤寫成 `GetDbParameterPrefix`,順手修正) |
| `tests/Bee.Db.UnitTests/MySqlFormCommandBuilderTests.cs` | 註解 cref 同上 |

#### 6b. 拆解 `DbFuncTests.cs`

原 `tests/Bee.Db.UnitTests/DbFuncTests.cs`(192 行,5 大區塊)拆成 2 處:

**新檔 `DatabaseTypeExtensionsTests.cs`**:`QuoteIdentifier`(5 個 test method)、`GetParameterPrefix`(2 個)、`GetParameterName`(1 個)共 8 個 test method,呼叫改 extension 形式。

**新檔 `DbParameterSpecTests.cs`**(若不存在則建立):`InferDbType` 4 個 test method 改為 `DbParameterSpec` 建構測試:
```csharp
// 改前
var result = DbFunc.InferDbType(value);
Assert.Equal(DbType.String, result);

// 改後
var spec = new DbParameterSpec("p", value);
Assert.Equal(DbType.String, spec.DbType);
```

null / DBNull / 不支援型別 3 個 test 同樣轉換。

**`SqlFormat` 2 個測試**:隨方法刪除而刪除。

#### 6c. 刪除原 `DbFuncTests.cs`

### 7. 更新主計畫

進度表第 6 列:`📝` → `✅`,完成日填入,方法數 `7` → `6`,處理路徑記為 `A+B+D`。

## 驗證

```bash
# 確認沒有遺漏的 DbFunc 引用
grep -rn "DbFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj

# Build
dotnet build src/Bee.Db/Bee.Db.csproj --configuration Release --no-restore

# Test(主要影響的 project)
./test.sh tests/Bee.Db.UnitTests/Bee.Db.UnitTests.csproj
./test.sh tests/Bee.Tests.Shared/Bee.Tests.Shared.csproj  # DbGlobalFixture 在此
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內的歷史紀錄
- Build 0 warning, 0 error
- 測試:`Bee.Db.UnitTests` **少 2 個 case**(`SqlFormat_*` 刪除),其餘全綠;若本機 SQL/PG container 已啟動,`[DbFact(...)]` 測試也通過

## Commit 訊息草稿

```
refactor(db): split DbFunc — DatabaseType extensions, DbConnectionManager absorbs CreateConnection

The five remaining methods all find natural homes:
- GetParameterPrefix / GetParameterName / QuoteIdentifier become
  DatabaseTypeExtensions methods (path B — domain enum extensions,
  callers read dbType.QuoteIdentifier(name) etc.)
- CreateConnection moves to DbConnectionManager (path D — same class
  already owns connection-info caching, CreateConnection is the
  natural lifecycle extension)
- InferDbType becomes a private static method on DbParameterSpec
  (path D — DbParameterSpec is the only consumer; DbType inference
  is its internal concern)
- SqlFormat is deleted (path A — zero production callers, callers
  use DbCommandSpec's named parameters; not worth keeping a
  placeholder-substitution helper that nobody invokes)

DbFunc.cs is removed entirely. Tests split: DatabaseTypeExtensions
tests stay together; InferDbType tests rewritten as
DbParameterSpec construction tests (testing observable behavior
through the public API instead of a private helper).

Sixth class executed under the *Func → .NET idiomatic refactor (see
docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

無新原則,沿用既有 idiom:

- 同 `DefineFunc.GetDefineType` → `DefineTypeExtensions.ToClrType` 的 path B 慣例,domain enum 的轉換/查詢方法用 `<EnumName>Extensions.X(this EnumName)`
- 同 `BusinessFunc.GetDatabaseItem` → `BackendInfo.GetDatabaseItem` 的 path D,方法搬到「已有同類資料的 owning class」
- 同 `DateTimeFunc.IsDate` 與 `Format` 的 path A 刪除原則,0 prod caller 的方法不保留
- 私有 helper 的測試移轉:從「直接測 helper」改為「透過公開 API 測可觀察行為」(如 `InferDbType` 的測試從 `DbFunc.InferDbType(value)` 改為 `new DbParameterSpec("p", value).DbType`),測得更貼近實際使用情境

## 風險與回滾

- 變動範圍:15 處生產端 caller + 30+ 處測試 caller(機械替換)
- Public API breaking:`DbFunc` 整個移除;`SqlFormat` 真正消失;其他 5 個方法變更位置
- 無外部 NuGet 消費者,可接受
- 若失敗單一 `git revert` 即可回滾
