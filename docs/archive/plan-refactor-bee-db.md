# Bee.Db 命名空間重構計畫

## 目標

將 Bee.Db 所有 `.cs` 檔案的命名空間由統一的 `Bee.Db` 調整為與資料夾結構對應，
同時移除語意不明的 `Common/`、`Interface/` 資料夾，確保每個資料夾對應一個明確的子命名空間。
`Query/` 下的所有子資料夾統一使用 `Bee.Db.Query`，資料夾結構僅作為程式碼組織之用，不再細分命名空間。

---

## 一、資料夾與命名空間對應

| 資料夾 | 命名空間 |
|--------|---------|
| 專案根目錄 | `Bee.Db` |
| `DbAccess/` | `Bee.Db.DbAccess` |
| `Logging/` | `Bee.Db.Logging` |
| `Manager/` | `Bee.Db.Manager` |
| `Providers/` | `Bee.Db.Providers` |
| `Providers/SqlServer/` | `Bee.Db.Providers.SqlServer` |
| `Query/Context/` | `Bee.Db.Query` |
| `Query/From/` | `Bee.Db.Query` |
| `Query/Select/` | `Bee.Db.Query` |
| `Query/Sort/` | `Bee.Db.Query` |
| `Query/Where/` | `Bee.Db.Query` |
| `Schema/` | `Bee.Db.Schema` |

---

## 二、各資料夾異動明細

### 2.1 專案根目錄（`Bee.Db`）

**從 `Common/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/DbFunc.cs` | 移至根目錄，namespace 不變（`Bee.Db`） |
| `Common/DbTableCommandBuilder.cs` | 移至根目錄，namespace 不變 |
| `Common/ILMapper.cs` | 移至根目錄，namespace 不變 |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 |
|------|--------|
| `CommandTextVariable` | `CommandTextVariable.cs` |
| `DbCommandKind` | `DbCommandKind.cs` |
| `JoinType` | `JoinType.cs` |

---

### 2.2 `DbAccess/`（`Bee.Db.DbAccess`）

| 檔案 | 異動 |
|------|------|
| `DbAccess/DataTableUpdateSpec.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbAccess.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbBatchResult.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbBatchSpec.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbCommandResult.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbCommandResultCollection.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbCommandSpec.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbCommandSpecCollection.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbConnectionScope.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbParameterSpec.cs` | namespace 改為 `Bee.Db.DbAccess` |
| `DbAccess/DbParameterSpecCollection.cs` | namespace 改為 `Bee.Db.DbAccess` |

---

### 2.3 `Logging/`（`Bee.Db.Logging`）

| 檔案 | 異動 |
|------|------|
| `Logging/DbAccessLogger.cs` | namespace 改為 `Bee.Db.Logging` |
| `Logging/DbLogContext.cs` | namespace 改為 `Bee.Db.Logging` |

---

### 2.4 `Manager/`（`Bee.Db.Manager`）

| 檔案 | 異動 |
|------|------|
| `Manager/DbConnectionInfo.cs` | namespace 改為 `Bee.Db.Manager` |
| `Manager/DbConnectionManager.cs` | namespace 改為 `Bee.Db.Manager` |
| `Manager/DbProviderManager.cs` | namespace 改為 `Bee.Db.Manager` |

---

### 2.5 `Providers/`（`Bee.Db.Providers`）

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/ICreateTableCommandBuilder.cs` | 移至 `Providers/`，namespace 改為 `Bee.Db.Providers` |
| `Interface/IFormCommandBuilder.cs` | 移至 `Providers/`，namespace 改為 `Bee.Db.Providers` |

**從 `Providers/Core/` 移入（移除 Core 子資料夾）：**

| 原始路徑 | 異動 |
|---------|------|
| `Providers/Core/SelectCommandBuilder.cs` | 移至 `Providers/`，namespace 改為 `Bee.Db.Providers` |

**`Providers/SqlServer/`：**

| 檔案 | 異動 |
|------|------|
| `Providers/SqlServer/SqlCreateTableCommandBuilder.cs` | namespace 改為 `Bee.Db.Providers.SqlServer` |
| `Providers/SqlServer/SqlFormCommandBuilder.cs` | namespace 改為 `Bee.Db.Providers.SqlServer` |
| `Providers/SqlServer/SqlTableSchemaProvider.cs` | namespace 改為 `Bee.Db.Providers.SqlServer` |

---

### 2.6 `Query/`（`Bee.Db.Query`）

所有子資料夾（`Context/`、`From/`、`Select/`、`Sort/`、`Where/`）統一使用 `Bee.Db.Query`，
資料夾僅作為程式碼組織之用。

| 檔案 | 異動 |
|------|------|
| `Query/Context/QueryFieldMapping.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Context/QueryFieldMappingCollection.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Context/SelectContext.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Context/SelectContextBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Context/TableJoin.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Context/TableJoinCollection.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/From/IFromBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/From/FromBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Select/ISelectBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Select/SelectBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Sort/ISortBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Sort/SortBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Where/DefaultParameterCollector.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Where/IParameterCollector.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Where/IWhereBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Where/InternalWhereBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Where/WhereBuilder.cs` | namespace 改為 `Bee.Db.Query` |
| `Query/Where/WhereBuildResult.cs` | namespace 改為 `Bee.Db.Query` |

---

### 2.7 `Schema/`（`Bee.Db.Schema`）

| 檔案 | 異動 |
|------|------|
| `Schema/TableSchemaBuilder.cs` | namespace 改為 `Bee.Db.Schema` |
| `Schema/TableSchemaComparer.cs` | namespace 改為 `Bee.Db.Schema` |

---

## 三、移除的資料夾

| 資料夾 | 原因 |
|--------|------|
| `Common/` | 語意不明，內容已分散至根目錄 |
| `Interface/` | 非 .NET 慣例，介面已移至對應實作所在的 `Providers/` 資料夾 |
| `Providers/Core/` | 僅含單一檔案，已併入 `Providers/` |

---

## 四、影響範圍

### Bee.Db 內部
- 修改命名空間宣告：~40 個 `.cs` 檔
- 新建檔案（從 `Common/Common.cs` 拆出）：3 個
- 刪除檔案：`Common/Common.cs`
- 移除空資料夾：`Common/`、`Interface/`、`Providers/Core/`
- 移動檔案：3 個（`Common/` → 根目錄），2 個（`Interface/` → `Providers/`），1 個（`Providers/Core/` → `Providers/`）

### 下游專案（需補 `using`）

| 專案 | 類型 |
|------|------|
| `tests/Bee.Db.UnitTests` | 測試（直接使用 `WhereBuilder`、`SortBuilder`、`SqlFormCommandBuilder` 等） |
| `src/Bee.Repository` | 核心相依 |
| `tests/Bee.Tests.Shared` | 測試共用 |
| `samples/JsonRpcServerAspNet` | 範例 |
| `samples/JsonRpcServer` | 範例 |
| `tools/BeeDbUpgrade` | 工具 |
| `tools/BeeSettingsEditor` | 工具 |

---

## 五、執行順序

1. 修改 Bee.Db 內部所有命名空間宣告與檔案位置
2. 執行 `dotnet build src/Bee.Db/Bee.Db.csproj --configuration Release` 確認無錯誤
3. 逐一更新下游專案的 `using` 陳述式
4. 執行 `dotnet build --configuration Release` 全方案建置確認
5. 執行 `dotnet test --configuration Release` 確認測試全數通過
