# 計畫：重構 `DataSetFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Base/Data/DataSetFunc.cs`(112 行,**7 個 public 方法**)

```csharp
namespace Bee.Base.Data;

public static class DataSetFunc
{
    public static DataSet CreateDataSet(string datasetName);          // new DataSet(name)
    public static DataSet CreateDataSet();                            // overload, "DataSet"
    public static DataTable CreateDataTable(string tableName);        // new DataTable(name)
    public static DataTable CreateDataTable();                        // overload, "DataTable"
    public static DataTable CopyDataTable(DataTable, string[]);       // 篩選欄位後 copy
    public static void UpperColumnName(DataTable);                    // 全欄改大寫
    public static object GetDefaultValue(FieldDbType);                // 依型別取預設值
}
```

> 主計畫進度表寫 8 個方法,實際 audit 後 7 個。

## Method Audit 表

| # | 方法簽章 | Prod callers | 處理路徑 | 新位置/名稱 |
|---|---------|------------|--------|------------|
| 1 | `CreateDataSet(string)` | 0 | **A 刪除** | 純 `new DataSet(name)` 包裝 |
| 2 | `CreateDataSet()` | 0 | **A 刪除** | 同上(僅是 overload) |
| 3 | `CreateDataTable(string)` | 0 | **A 刪除** | 純 `new DataTable(name)` 包裝 |
| 4 | `CreateDataTable()` | 0 | **A 刪除** | 同上 |
| 5 | `CopyDataTable(DataTable, string[])` | 0 | **A 刪除** | 22 行邏輯但無 prod caller;若未來有需要再加回 |
| 6 | `UpperColumnName(DataTable)` | 2(`DbAccess.cs` x 2) | **B** | `DataTableExtensions.UppercaseColumnNames(this DataTable)`(順手改名,plural 更精確) |
| 7 | `GetDefaultValue(FieldDbType)` | 2(`DataTableExtensions`、`DbParameterSpecCollection`) | **B** | `FieldDbTypeExtensions.GetDefaultValue(this FieldDbType)`(新檔) |

7 個方法處理完後,`DataSetFunc.cs` 整個刪除。

### 1-4. 4 個 `Create*` 方法 — path A 刪除

**現況**:全部 0 prod caller。Body 都是 `new DataSet(name)` / `new DataTable(name)` 純 BCL 包裝,無加值。

**為何刪除**:
- 沿 `DateTimeFunc.Format` 的 path A 原則:0 prod caller + 純 BCL 包裝 = 刪除
- `new DataSet()` BCL 預設名稱是 `"NewDataSet"`(非框架的 `"DataSet"`),稍有差異但**沒有 prod 端依賴此命名**
- 若未來有需要,`new DataSet("MyName")` 是 BCL idiomatic 寫法

**測試處置**:`CreateDataSet_*`、`CreateDataTable_*` 4 個測試直接刪除。

### 5. `CopyDataTable(DataTable, string[])` — path A 刪除

**現況**:0 prod caller,22 行邏輯做「依欄位名稱清單篩選並重排欄位」。

**為何刪除而非保留**:
- 雖然 body 不只是純包裝,但**完全沒有 prod 用例**
- 如果未來真的需要,可以正確命名(`SelectColumns` / `ProjectColumns`)再加為 `DataTableExtensions` 方法
- 沿「不為假設未來設計」原則:現在不知道使用情境,設計命名容易錯

**測試處置**:1 個 `CopyDataTable_*` 測試刪除。

### 6. `UpperColumnName(DataTable)` — path B 改名

**現況**:2 個 prod caller(`DbAccess.cs:287, 629`),1 個測試。

**搬遷**:加到既有 `src/Bee.Base/Data/DataTableExtensions.cs`,改名 `UppercaseColumnNames`(複數,動詞更清楚)。

```csharp
// 改前
DataSetFunc.UpperColumnName(table);

// 改後
table.UppercaseColumnNames();
```

**為何改名**:
- 原方法處理**所有欄位**,單數 `UpperColumnName` 名不符實
- `Upper` 作動詞在 .NET 不常見,`Uppercase` 更標準
- 順手改名是 path B 轉換時的合理時機

**測試處置**:1 個 `UpperColumnName_*` 測試搬到 `DataExtensionsTests.cs`(已存在),呼叫改 `table.UppercaseColumnNames()`。

### 7. `GetDefaultValue(FieldDbType)` — path B

**現況**:2 個 prod caller(`DataTableExtensions.cs:59`、`DbParameterSpecCollection.cs:41`)+ 多個測試。

**搬遷**:新檔 `src/Bee.Base/Data/FieldDbTypeExtensions.cs`,擴充 `FieldDbType` enum:
```csharp
namespace Bee.Base.Data;

public static class FieldDbTypeExtensions
{
    public static object GetDefaultValue(this FieldDbType dbType) { /* 同邏輯 */ }
}
```

呼叫變:
```csharp
// 改前
DataSetFunc.GetDefaultValue(dbType)

// 改後
dbType.GetDefaultValue()
```

**CA1724 檢查**:`FieldDbTypeExtensions` 不對應任何 BCL namespace 末段,安全。

**測試處置**:`GetDefaultValue_*` 系列(2 個 test method,共 ~12 cases)→ 新檔 `tests/Bee.Base.UnitTests/FieldDbTypeExtensionsTests.cs`,呼叫改 extension 形式。

## 影響範圍

**全 repo grep `DataSetFunc` 結果(扣除 `bin/obj`)**:

| 類型 | 檔案 | 出現次數 |
|------|------|---------|
| 產品(類別定義) | `src/Bee.Base/Data/DataSetFunc.cs` | 1 |
| 產品(`UpperColumnName` x 2) | `src/Bee.Db/DbAccess.cs` | 2 |
| 產品(`GetDefaultValue` x 1) | `src/Bee.Base/Data/DataTableExtensions.cs` | 1 |
| 產品(`GetDefaultValue` x 1) | `src/Bee.Db/DbParameterSpecCollection.cs` | 1 |
| 測試 | `tests/Bee.Base.UnitTests/DataSetFuncTests.cs` | ~14 |
| 文件 | `docs/plans/plan-funcs-to-net-idiomatic.md` | 1 |

合計 4 處生產端 caller + ~14 處測試 caller。

## 執行步驟

### 1. 加 `UppercaseColumnNames` 到 `DataTableExtensions.cs`

`src/Bee.Base/Data/DataTableExtensions.cs`:加新方法:
```csharp
/// <summary>
/// Converts all column names in the table to uppercase.
/// </summary>
/// <param name="dataTable">The target table.</param>
public static void UppercaseColumnNames(this DataTable dataTable)
{
    foreach (DataColumn column in dataTable.Columns)
        column.ColumnName = column.ColumnName.ToUpper();
}
```

### 2. 新增 `FieldDbTypeExtensions.cs`

`src/Bee.Base/Data/FieldDbTypeExtensions.cs`(新檔):整段 `GetDefaultValue` 邏輯,加 `this` 修飾。

### 3. 更新生產端 caller(4 處)

- `src/Bee.Db/DbAccess.cs:287, 629`:`DataSetFunc.UpperColumnName(table)` → `table.UppercaseColumnNames()`
- `src/Bee.Base/Data/DataTableExtensions.cs:59`:`DataSetFunc.GetDefaultValue(dbType)` → `dbType.GetDefaultValue()`
- `src/Bee.Db/DbParameterSpecCollection.cs:41`:`DataSetFunc.GetDefaultValue(field.DbType)` → `field.DbType.GetDefaultValue()`

### 4. 刪除 `DataSetFunc.cs`

```bash
git rm src/Bee.Base/Data/DataSetFunc.cs
```

### 5. 拆解測試

`tests/Bee.Base.UnitTests/DataSetFuncTests.cs` 拆成 3 處:
- 4 個 `Create*` 測試 + 1 個 `CopyDataTable_*` 測試 → 刪除
- 1 個 `UpperColumnName_*` 測試 → 搬到既有 `DataExtensionsTests.cs`,改名 `UppercaseColumnNames_*`,呼叫改 extension 形式
- `GetDefaultValue_*` 2 個測試 → 新檔 `FieldDbTypeExtensionsTests.cs`
- 刪除原 `DataSetFuncTests.cs`

### 6. 更新主計畫

進度表第 7 列:`📝` → `✅`,完成日填入,方法數 `8` → `7`,處理路徑記為 `A+B`。

## 驗證

```bash
grep -rn "DataSetFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj
dotnet build src/Bee.Base/Bee.Base.csproj --configuration Release --no-restore
dotnet build src/Bee.Db/Bee.Db.csproj --configuration Release --no-restore
./test.sh tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
./test.sh tests/Bee.Db.UnitTests/Bee.Db.UnitTests.csproj
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內的歷史紀錄
- Build 0 warning, 0 error
- Bee.Base.UnitTests **少 5 個 case**(4 Create* + 1 CopyDataTable),其餘全綠

## Commit 訊息草稿

```
refactor(base): split DataSetFunc — extensions on DataTable and FieldDbType, drop unused wrappers

UpperColumnName moves to DataTableExtensions.UppercaseColumnNames
(path B; renamed to plural+verb form since it processes all columns,
"Upper" is uncommon as a verb in .NET).

GetDefaultValue moves to FieldDbTypeExtensions (path B; new file
mirroring DefineTypeExtensions / DatabaseTypeExtensions idioms).

CreateDataSet, CreateDataTable (both overloads), and CopyDataTable
are deleted (path A — zero production callers, the Create* methods
are pure BCL wrappers, CopyDataTable can be re-added as a properly
named DataTable extension when actually needed).

DataSetFunc.cs is removed entirely. Tests split: UpperColumnName
joins DataExtensionsTests; GetDefaultValue moves to a new
FieldDbTypeExtensionsTests; Create* and CopyDataTable tests are
deleted.

Seventh class executed under the *Func to .NET idiomatic refactor
(see docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

無新原則,沿用既有 idiom:

- 同 `DateTimeFunc.Format` 的 path A 刪除原則,純 BCL 包裝 + 0 prod caller 的方法不保留
- 同 `DefineTypeExtensions` / `DatabaseTypeExtensions` 的 path B,domain enum 擴充用 `<EnumName>Extensions`
- 既有 `*Extensions` 類存在時(本例 `DataTableExtensions`),新方法直接加進去,不另開新類
- 改名是 path B 轉換時的合理時機(`UpperColumnName` → `UppercaseColumnNames`),只要原名不準確就改

## 風險與回滾

- 變動範圍:4 處生產端 caller(機械替換)+ 測試重組
- Public API breaking:5 個方法刪除 + 1 個改名 + 1 個改 namespace
- 無外部 NuGet 消費者,可接受
- 若失敗單一 `git revert` 即可回滾
