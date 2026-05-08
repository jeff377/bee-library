# 計畫：將 `TableSchemaIndex` 更名為 `DbTableIndex`

**狀態：✅ 已完成（2026-05-08）**

## 背景

`Bee.Definition.Database` namespace 下的索引類別目前命名為 `TableSchemaIndex` / `TableSchemaIndexCollection`，與同 namespace 既有的「`Db` 前綴」慣例不一致：

| 類別 | 命名形式 |
|------|---------|
| `DbField` | ✅ `Db` 前綴 |
| `DbFieldCollection` | ✅ `Db` 前綴 |
| `TableSchemaIndex` | ❌ 用容器類別名（`TableSchema`）作前綴 |
| `TableSchemaIndexCollection` | ❌ 同上 |

容器名前綴會讓 type 名稱跟著容器類別綁死；改用 `Db` 前綴後與 `DbField` 對稱，未來若 `TableSchema` 又被重新命名，索引類別也不需跟著動。

> **歷史補充**：此 repo 曾經就是 `DbTableIndex`，先前在某次重構中改成 `TableSchemaIndex`；本次是把它改回原命名（更貼近專案整體慣例）。

## 目標

- `TableSchemaIndex` → `DbTableIndex`
- `TableSchemaIndexCollection` → `DbTableIndexCollection`
- 同步更新所有 src / tests / fixture XML / docs 引用
- 不保留 `[XmlType("TableSchemaIndex")]` 之類的舊兼容 alias（與 `plan-xmltype-cleanup` 方向一致：類別名直接做 XML element 名，不額外標 `XmlType`）

## 影響範圍盤點

> 僅列源碼與固定資源；`bin/`、`obj/`、`TestResults/` 為 build artefact，重新 build 後自動更新，不在本計畫範圍。

### A. 類別檔（更名 + rename file）

`src/Bee.Definition/Database/`：

- `TableSchemaIndex.cs` → `DbTableIndex.cs`（類別名、`Clone()` 回傳型別、`Compare(...)` 參數型別均同步改）
- `TableSchemaIndexCollection.cs` → `DbTableIndexCollection.cs`（類別名、繼承 `KeyCollectionBase<DbTableIndex>`、ctor、`AddPrimaryKey`、`Add` 方法簽章；`<see cref>` 註解同步）

### B. `TableSchema.cs`（同 namespace，引用上述兩個型別）

`src/Bee.Definition/Database/TableSchema.cs`：

- 私有欄位 `_indexes` 型別
- `Indexes` 屬性的型別與 `new` 表達式
- `GetPrimaryKey()` 回傳型別與 foreach 變數型別
- `Clone()` 內 foreach 變數型別

### C. `Bee.Db` 引用點（共 17 檔）

| 區塊 | 檔案 |
|------|------|
| Schema | `Schema/Changes/AddIndexChange.cs`、`Schema/Changes/DropIndexChange.cs`、`Schema/TableSchemaComparer.cs` |
| SqlServer | `Providers/SqlServer/SqlCreateTableCommandBuilder.cs`、`Providers/SqlServer/SqlTableAlterCommandBuilder.cs`、`Providers/SqlServer/SqlTableSchemaProvider.cs` |
| PostgreSQL | `Providers/PostgreSql/PgCreateTableCommandBuilder.cs`、`Providers/PostgreSql/PgTableAlterCommandBuilder.cs`、`Providers/PostgreSql/PgTableSchemaProvider.cs` |
| MySQL | `Providers/MySql/MySqlCreateTableCommandBuilder.cs`、`Providers/MySql/MySqlTableAlterCommandBuilder.cs`、`Providers/MySql/MySqlTableRebuildCommandBuilder.cs`、`Providers/MySql/MySqlTableSchemaProvider.cs` |
| Sqlite | `Providers/Sqlite/SqliteCreateTableCommandBuilder.cs`、`Providers/Sqlite/SqliteTableAlterCommandBuilder.cs`、`Providers/Sqlite/SqliteTableRebuildCommandBuilder.cs`、`Providers/Sqlite/SqliteTableSchemaProvider.cs` |
| Oracle | `Providers/Oracle/OracleCreateTableCommandBuilder.cs`、`Providers/Oracle/OracleTableAlterCommandBuilder.cs`、`Providers/Oracle/OracleTableRebuildCommandBuilder.cs`、`Providers/Oracle/OracleTableSchemaProvider.cs` |

> 註：MySql/Oracle 都各 4 檔；Sqlite 4 檔；PostgreSQL 3 檔；SqlServer 3 檔；Schema 3 檔。合計 17 檔（不含上述 A、B 區）。

引用形式都是型別宣告或 `new TableSchemaIndex(...)`，無 `[XmlType]` 之類標記，純更名即可。

### D. 測試引用點（共 13 檔）

`tests/Bee.Db.UnitTests/`：

- `MySqlTableAlterCommandBuilderTests.cs`、`MySqlTableRebuildCommandBuilderTests.cs`
- `OracleTableAlterCommandBuilderTests.cs`、`OracleTableRebuildCommandBuilderTests.cs`
- `PgTableAlterCommandBuilderTests.cs`、`PgTableRebuildCommandBuilderTests.cs`、`PgTableSchemaProviderTests.cs`
- `SqlTableAlterCommandBuilderTests.cs`、`SqlTableRebuildCommandBuilderTests.cs`
- `SqliteTableAlterCommandBuilderTests.cs`、`SqliteTableRebuildCommandBuilderTests.cs`
- `TableChangeTests.cs`、`TableSchemaComparerTests.cs`、`TableUpgradeOrchestratorTests.cs`

### E. Fixture XML（共 3 檔）

類別更名後預設 XML element name 也會變。fixture 檔的 `<TableSchemaIndex>` 必須跟著改為 `<DbTableIndex>`，否則 `XmlSerializer.Deserialize` 會把整個 `<Indexes>` 集合解析為空。

- `tests/Define/TableSchema/common/ft_department.TableSchema.xml`
- `tests/Define/TableSchema/common/ft_employee.TableSchema.xml`
- `tests/Define/TableSchema/common/ft_project.TableSchema.xml`

### F. 文件（共 3 檔）

- `docs/terminology.md`：詞條第 55 行的 `TableSchemaIndex` 改為 `DbTableIndex`
- `src/Bee.Definition/README.md`：第 68 行目錄列表改為 `DbTableIndex`
- `src/Bee.Definition/README.zh-TW.md`：第 68 行目錄列表改為 `DbTableIndex`

### G. 不動的檔案（保留歷史原文）

下列檔案出現 `TableSchemaIndex` 屬於**歷史紀錄**，不在本計畫修改範圍：

- `docs/archive/plan-oracle-uppercase-strategy.md`：archive 內已封存的舊 plan
- `docs/plans/plan-xmltype-cleanup.md` 第 54 行：紀錄了當時影響範圍盤點，保留原文反映該 plan 完成當下的狀態

## 執行步驟

1. **更名類別檔**（git mv 保留 history）
   - `git mv src/Bee.Definition/Database/TableSchemaIndex.cs src/Bee.Definition/Database/DbTableIndex.cs`
   - `git mv src/Bee.Definition/Database/TableSchemaIndexCollection.cs src/Bee.Definition/Database/DbTableIndexCollection.cs`
2. **更新 src（A + B + C 區）所有引用**：
   - `TableSchemaIndexCollection` → `DbTableIndexCollection`（先取代長字串避免 `TableSchemaIndex` 提前命中）
   - `TableSchemaIndex` → `DbTableIndex`
3. **更新 tests（D 區）所有引用**：同樣順序
4. **更新 fixture XML（E 區）三檔**：`<TableSchemaIndex` → `<DbTableIndex`、`</TableSchemaIndex>` → `</DbTableIndex>`
5. **更新文件（F 區）三檔**
6. **build 驗證**：`dotnet build --configuration Release`，要求 0 警告 0 錯誤
7. **單元測試驗證**：跑 `tests/Bee.Definition.UnitTests` + `tests/Bee.Db.UnitTests`（不需 DB container 的部分），fixture 反序列化路徑會被覆蓋
8. **完成標記**：本 plan 文件頂部狀態改為 ✅ 已完成
9. **commit**：訊息 `refactor(Bee.Definition): TableSchemaIndex 更名為 DbTableIndex`

## 風險與權衡

- **XML 序列化破壞性**：類別更名後預設 XML element name 也會變，已部署於外部的 `*.TableSchema.xml`（若存在 `<TableSchemaIndex>` 元素）將反序列化為空集合
  - 本 repo 是純 NuGet library，內部 fixture 同步更新即可；外部使用者若有自存 schema XML，需在 release notes 提示
  - 不打算用 `[XmlType("TableSchemaIndex")]` 維持舊兼容（與 `plan-xmltype-cleanup` 方向衝突）
- **取代順序**：`TableSchemaIndex` 是 `TableSchemaIndexCollection` 的 substring，必須先取代長字串、再取代短字串，否則會生出 `DbTableIndexCollection` 中夾雜未動到的部分
- **Class rename 不可只用文字取代**：必須同時 rename 兩個檔案，避免 git diff 留下 orphan 檔
- **驗證盲點**：本機未啟用 DB container 時，`[DbFact(...)]` 測試會自動 skip；fixture XML 反序列化路徑由 `Bee.Definition.UnitTests` / `Bee.Db.UnitTests` 中不需 DB 的測試覆蓋（如 `TableSchemaComparerTests`、`SqlCreateTableCommandBuilderTests` 等純邏輯測試）

## 驗證方式

build + 純邏輯測試（非 `[DbFact]`）的子集足以證明：

- 類別更名沒有遺漏的引用點（編譯期）
- fixture XML 改名同步生效（fixture 反序列化路徑）
- builder 產出的 SQL 字串不變（純邏輯測試）

`[DbFact]` / `./test.sh` 全套不在本計畫驗證範圍（DB container 啟動成本高且本次更名不影響任何 SQL 字串輸出）。
