# 計畫：DatabaseItem 新增 CategoryId 欄位

**狀態：✅ 已完成（2026-05-10）**

## 背景

`DatabaseSettings.Items` 內的 `DatabaseItem` 目前以 `Id` 為實體資料庫識別（搭配 connection string）。`Id` 字面值（例如 `common_sqlserver`、`common_postgresql`）僅是**約定俗成的命名慣例**（`TestDbConventions.GetDatabaseId` 用 `{categoryId}_{dbtype}` 拼字串），並沒有第一級欄位記錄此 DatabaseItem 對應到哪個邏輯資料庫分類。

這造成兩個問題：

1. 從 `DatabaseItem` 無法回推它屬於哪個 `DbCategory`（common / company / log），需要解析 Id 字串或外部約定
2. 若要實現「對某 `DatabaseItem` 自動建立其所屬分類的所有 TableSchema」（用 `DbCategorySettings.<DbCategory>.<Tables>` 列出的清單一次建表），需要先有此 metadata

新增 `CategoryId` 欄位讓邏輯／實體資料庫關係明確，是後續「依 DatabaseItem 建立其所有 TableSchema」這類自動化能力的前提。

## 變更清單

### 1. `DatabaseItem` 新增 `CategoryId` 欄位

[src/Bee.Definition/Settings/DatabaseSettings/DatabaseItem.cs](../../src/Bee.Definition/Settings/DatabaseSettings/DatabaseItem.cs)

- 新增 `CategoryId`（`XmlAttribute`，`string`，`DefaultValue("")`）
- 位置：放在 `Id` 之後、`DisplayName` 之前，邏輯上「分類 → 顯示 → 型別」遞進
- `Clone()` 同步複製 `CategoryId`
- XML 註解（英文）：`The logical database category id this item belongs to (e.g. "common", "company", "log").`

### 2. 測試端 `GlobalFixture.AddDatabaseItemIfMissing` 更新

[tests/Bee.Tests.Shared/GlobalFixture.cs](../../tests/Bee.Tests.Shared/GlobalFixture.cs)

`AddDatabaseItemIfMissing(string id, DatabaseType dbType, string connStr)` 加入 `string categoryId` 參數；所有呼叫端目前都針對 common 分類，傳 `"common"`。

呼叫端：`RegisterSqlServer` / `RegisterPostgreSql` / `RegisterSqlite` / `RegisterMySql` / `RegisterOracle` 各 1～2 行。

### 3. 測試補強

[tests/Bee.Definition.UnitTests/DtoSerializationTests.cs](../../tests/Bee.Definition.UnitTests/DtoSerializationTests.cs)

擴充既有 `DatabaseSettings_XmlRoundtrip_PreservesCollections` 把 `CategoryId = "common"` 加進測資並斷言 round-trip 後相等，避免序列化漏接。

### 不在本次範圍

以下能力可在後續以 `CategoryId` 為基礎延伸，本次不實作以避免範圍蔓延：

- 「依 DatabaseItem 建立所有 TableSchema」的高階 API（例：`TableSchemaBuilder.ExecuteAll(databaseId)` 或 `DatabaseItemExtensions.CreateAllTables()`）
- 任何 `CategoryId` 必填／枚舉化驗證（目前 `DbCategory.Id` 也是純字串，先維持一致）
- `DbConnectionManager` 流程改寫（仍以 `Id` 查表，`CategoryId` 只是附加 metadata）

## 驗證

```bash
dotnet build --configuration Release --no-restore
./test.sh tests/Bee.Definition.UnitTests/Bee.Definition.UnitTests.csproj
./test.sh tests/Bee.ObjectCaching.UnitTests/Bee.ObjectCaching.UnitTests.csproj
```

期望：所有受影響測試通過，特別是 `DatabaseSettings_XmlRoundtrip_PreservesCollections` 在加上 `CategoryId` 後仍 round-trip 正確。

## 提交

桌面環境本機可驗證 → 直接提交到 `main` 並 push，遠端 CI 跑驗證。
