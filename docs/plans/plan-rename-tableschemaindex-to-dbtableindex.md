# 計畫：將 `TableSchemaIndex` 更名回 `DbTableIndex`

**狀態：✅ 已完成（2026-05-08）**

## 背景

`Bee.Definition.Database` namespace 中的索引類別目前命名為 `TableSchemaIndex` / `TableSchemaIndexCollection`，但破壞了同 namespace 下既有的 `Db` 前綴慣例：

| 類別 | 前綴 |
|------|------|
| `DbField` | `Db` |
| `DbFieldCollection` | `Db` |
| `TableSchemaIndex` | ❌ 用容器名前綴 |
| `TableSchemaIndexCollection` | ❌ 用容器名前綴 |

現存 XML 測試 fixture 中 `st_user.TableSchema.xml`、`st_session.TableSchema.xml` 仍使用舊元素名 `<DbTableIndex>`（反序列化後索引集合會為空），這也佐證原命名 `DbTableIndex` 才是專案早期的正式名稱。

## 目標

將以下類別更名以恢復命名一致性：

- `TableSchemaIndex` → `DbTableIndex`
- `TableSchemaIndexCollection` → `DbTableIndexCollection`

`IndexField` / `IndexFieldCollection` 不動（為索引子結構，不需要 `Db` 前綴）。

## 影響範圍

### 1. 類別檔案（重新命名 + class rename）

- `src/Bee.Definition/Database/TableSchemaIndex.cs` → `DbTableIndex.cs`
- `src/Bee.Definition/Database/TableSchemaIndexCollection.cs` → `DbTableIndexCollection.cs`

### 2. 引用更新（C# 原始檔，約 37 個檔案）

**Bee.Definition**
- `Database/TableSchema.cs`（屬性型別、Clone、GetPrimaryKey）

**Bee.Db / Providers**（每家 DB 各 2–4 檔）
- `Schema/TableSchemaComparer.cs`
- `Schema/Changes/AddIndexChange.cs`、`DropIndexChange.cs`
- `Providers/SqlServer/*`、`Providers/PostgreSql/*`、`Providers/MySql/*`、`Providers/Oracle/*`、`Providers/Sqlite/*`

**Bee.Db.UnitTests**（約 13 個測試檔）
- `SqlTableAlterCommandBuilderTests.cs` 等

### 3. XML 測試 fixture 更新

目前混用兩種元素名：

| 檔案 | 元素名 |
|------|--------|
| `tests/Define/TableSchema/common/ft_department.TableSchema.xml` | `<TableSchemaIndex>`（要改） |
| `tests/Define/TableSchema/common/ft_employee.TableSchema.xml` | `<TableSchemaIndex>`（要改） |
| `tests/Define/TableSchema/common/ft_project.TableSchema.xml` | `<TableSchemaIndex>`（要改） |
| `tests/Define/TableSchema/common/st_session.TableSchema.xml` | `<DbTableIndex>`（已正確） |
| `tests/Define/TableSchema/common/st_user.TableSchema.xml` | `<DbTableIndex>`（已正確） |

**注意**：3 個 `ft_*` fixture 必須改回 `<DbTableIndex>`，否則類別更名後反序列化會跳過所有索引節點。

### 4. 文件更新

- `docs/terminology.md`（`TableSchemaIndex` 詞條）
- `docs/plans/plan-xmltype-cleanup.md` 內提到 `TableSchemaIndex.cs` 的歷史紀錄則保留原文，僅在計畫文件本身的影響表中說明

## 執行步驟

1. 重新命名 `.cs` 檔案（git mv 保留歷史）
2. 在檔案內進行 class、constructor、collection generic argument 全 replace
3. 全專案 `TableSchemaIndex` → `DbTableIndex` 字串取代（含 `TableSchemaIndexCollection` → `DbTableIndexCollection`）
4. 更新 3 個 `ft_*` XML fixture
5. 更新 `docs/terminology.md`
6. `dotnet build` 驗證編譯通過
7. `./test.sh` 跑單元測試（特別關注 `TableSchemaComparerTests`、各 Provider 的 `*TableAlterCommandBuilderTests`、反序列化相關測試）

## 風險與注意事項

- **XML 序列化相容性**：類別更名後，預設 XML element name 也會變（無 `[XmlType]` 標記）。任何外部已部署的 `*.TableSchema.xml` 中若元素名為 `<TableSchemaIndex>`，將反序列化失敗，索引集合變空。
  - 此專案目前還在 4.x 開發階段，未對外正式釋出 schema，影響範圍僅限本 repo 內 fixture
  - 不打算用 `[XmlType("TableSchemaIndex")]` 維持舊兼容（與 `plan-xmltype-cleanup` 方向衝突）
- **Class rename 不可只用文字取代**：必須確認 `TableSchemaIndexCollection` 也跟著改，避免 orphan 引用
- **public API 變動**：對外型別重新命名屬於 breaking change，下次發版需在 release notes 標明

## 不在此計畫內

- `IndexField` / `IndexFieldCollection` 維持原名
- `TableSchema` 自身不更名
- 不處理 `TableSchema` 設計檢視中其他議題（FK 中繼資料、null 安全、`DbName` 屬性等）
