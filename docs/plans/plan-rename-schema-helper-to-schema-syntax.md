# 計畫：將 `*SchemaHelper` 重新命名為 `*SchemaSyntax`

**狀態：✅ 已完成（2026-04-30）**

## 背景

`Bee.Db/Providers/<Dialect>/` 下有 5 個 `*SchemaHelper` 靜態類別,負責特定資料庫方言的 SQL 語法 primitive(quote 識別字、字串 escape、預設值表達式、欄位定義字串、auto-increment 定義、comment statement 等)。

`Helper` 後綴是業界公認的 weak naming —— 把「雜七雜八的工具」一股腦丟進去的暗號,讀者必須翻檔才知道在做什麼。本專案 `code-style.md` 明列「過於籠統的 `Helper` / `Manager` / `Util`」為命名反例,但這 5 個類別漏網了。

實際內容是該 dialect 的 **SQL 語法規則**,跟既有 `IDialectFactory` 系列的 `Dialect` 概念呼應。`Syntax` 後綴比 `Helper` 精準很多倍。

## 範圍

### 重新命名清單(5 個類別)

| 目前名稱 | 新名稱 | 路徑 |
|---------|--------|------|
| `MySqlSchemaHelper` | `MySqlSchemaSyntax` | `src/Bee.Db/Providers/MySql/` |
| `PgSchemaHelper` | `PgSchemaSyntax` | `src/Bee.Db/Providers/PostgreSql/` |
| `OracleSchemaHelper` | `OracleSchemaSyntax` | `src/Bee.Db/Providers/Oracle/` |
| `SqliteSchemaHelper` | `SqliteSchemaSyntax` | `src/Bee.Db/Providers/Sqlite/` |
| `SqlSchemaHelper` | `SqlSchemaSyntax` | `src/Bee.Db/Providers/SqlServer/` |

### `Sql` vs `SqlServer` 前綴的取捨(維持原樣)

`SqlServer` provider 內既有檔案統一用 `Sql*` 前綴(`SqlDialectFactory`、`SqlTableAlterCommandBuilder`、`SqlCreateTableCommandBuilder`、`SqlTableRebuildCommandBuilder`、`SqlTableSchemaProvider`),這是專案既有慣例。

雖然 `Sql` 在語意上跟「通用 SQL」會混淆,但若改成 `SqlServer*` 前綴需動 6+ 個檔案、且超出本次「rename helper → syntax」的主題。

**本次計畫:`SqlSchemaHelper` → `SqlSchemaSyntax`,保留 `Sql` 前綴對齊既有慣例。** SqlServer 前綴正規化留作另一個獨立計畫,本次不涉及。

### 連動更新

#### Source 引用(7 個檔案)

| 檔案 | 引用類別 |
|------|---------|
| `src/Bee.Db/Providers/MySql/MySqlDialectFactory.cs` | `MySqlSchemaHelper` |
| `src/Bee.Db/Providers/MySql/MySqlCreateTableCommandBuilder.cs` | `MySqlSchemaHelper` |
| `src/Bee.Db/Providers/MySql/MySqlTableAlterCommandBuilder.cs` | `MySqlSchemaHelper` |
| `src/Bee.Db/Providers/MySql/MySqlTableRebuildCommandBuilder.cs` | `MySqlSchemaHelper` |
| `src/Bee.Db/Providers/MySql/MySqlTableSchemaProvider.cs` | `MySqlSchemaHelper` |
| `src/Bee.Db/Providers/PostgreSql/PgDialectFactory.cs` | `PgSchemaHelper` |
| `src/Bee.Db/Providers/PostgreSql/PgCreateTableCommandBuilder.cs` | `PgSchemaHelper` |
| `src/Bee.Db/Providers/PostgreSql/PgTableAlterCommandBuilder.cs` | `PgSchemaHelper` |
| `src/Bee.Db/Providers/PostgreSql/PgTableRebuildCommandBuilder.cs` | `PgSchemaHelper` |
| `src/Bee.Db/Providers/PostgreSql/PgTableSchemaProvider.cs` | `PgSchemaHelper` |
| `src/Bee.Db/Providers/Oracle/OracleDialectFactory.cs` | `OracleSchemaHelper` |
| `src/Bee.Db/Providers/Oracle/OracleCreateTableCommandBuilder.cs` | `OracleSchemaHelper` |
| `src/Bee.Db/Providers/Oracle/OracleTableAlterCommandBuilder.cs` | `OracleSchemaHelper` |
| `src/Bee.Db/Providers/Oracle/OracleTableRebuildCommandBuilder.cs` | `OracleSchemaHelper` |
| `src/Bee.Db/Providers/Oracle/OracleTableSchemaProvider.cs` | `OracleSchemaHelper` |
| `src/Bee.Db/Providers/Sqlite/SqliteDialectFactory.cs` | `SqliteSchemaHelper` |
| `src/Bee.Db/Providers/Sqlite/SqliteCreateTableCommandBuilder.cs` | `SqliteSchemaHelper` |
| `src/Bee.Db/Providers/Sqlite/SqliteTableAlterCommandBuilder.cs` | `SqliteSchemaHelper` |
| `src/Bee.Db/Providers/Sqlite/SqliteTableRebuildCommandBuilder.cs` | `SqliteSchemaHelper` |
| `src/Bee.Db/Providers/Sqlite/SqliteTableSchemaProvider.cs` | `SqliteSchemaHelper` |
| `src/Bee.Db/Providers/SqlServer/SqlDialectFactory.cs` | `SqlSchemaHelper` |
| `src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs` | `SqlSchemaHelper` |
| `src/Bee.Db/Providers/SqlServer/SqlTableAlterCommandBuilder.cs` | `SqlSchemaHelper` |
| `src/Bee.Db/Providers/SqlServer/SqlTableRebuildCommandBuilder.cs` | `SqlSchemaHelper` |
| `src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs` | `SqlSchemaHelper` |

#### XML 註解 `<see cref>`(本次受影響)

- `src/Bee.Db/Providers/MySql/MySqlSchemaHelper.cs` — 引用 `Sqlite.SqliteSchemaHelper`、`PostgreSql.PgSchemaHelper`
- `src/Bee.Db/Providers/Oracle/OracleSchemaHelper.cs` — 引用 `MySql.MySqlSchemaHelper`、`PostgreSql.PgSchemaHelper`
- `src/Bee.Db/Providers/PostgreSql/PgSchemaHelper.cs` — 引用 `SqlServer.SqlSchemaHelper`
- `src/Bee.Db/Providers/Sqlite/SqliteSchemaHelper.cs` — 引用 `PostgreSql.PgSchemaHelper`
- `src/Bee.Db/Providers/Oracle/OracleTableSchemaProvider.cs` — `<see cref="OracleSchemaHelper.QuoteName"/>` + 一處註解描述
- `src/Bee.Db/Providers/Oracle/OracleCreateTableCommandBuilder.cs` — `<see cref="OracleSchemaHelper.GetAutoIncrementColumnDefinition"/>`

#### 測試專案(4 個檔案)

| 檔案 | 動作 |
|------|------|
| `tests/Bee.Db.UnitTests/SqliteSchemaHelperTests.cs` | **改檔名 + 改類別名** → `SqliteSchemaSyntaxTests` |
| `tests/Bee.Db.UnitTests/SqliteDialectFactoryTests.cs` | 更新註解與 `[DisplayName]` 中的類別名 |
| `tests/Bee.Db.UnitTests/SqliteTableAlterCommandBuilderTests.cs` | 更新註解中的類別名 |
| `tests/Bee.Db.UnitTests/OracleIntegrationTests.cs` | 更新註解中的類別名 |

#### 計畫文件中的歷史引用(不動)

`docs/plans/plan-mysql-support.md`、`docs/plans/plan-oracle-support.md` 等已歸檔的計畫文件如果提到 `MySqlSchemaHelper`/`OracleSchemaHelper` 名稱,**保留不改** —— 這些是歷史紀錄,改了會與 commit 訊息脫鉤。

`docs/plans/` 目錄掃描範圍由執行階段確認(若無引用則略過)。

## 執行步驟

1. **重新命名 5 個 SchemaHelper 檔案與類別**
   - `git mv` 確保 git 認得是 rename(保留 blame 歷史)
   - 更新類別名稱
   - 更新 `<see cref>` 互相引用

2. **更新 Source 引用**(25 處左右)
   - 用 grep / replace_all 逐個 dialect 處理,避免跨 dialect 誤替換
   - SqlServer 的 `SqlSchemaHelper` 因 grep 字串較短,需確認不會誤中其他 `Sql` 開頭識別字

3. **更新測試專案**
   - `SqliteSchemaHelperTests.cs` 檔案改名 + 類別改名
   - 其他三個測試檔的註解 / `[DisplayName]` 字串更新

4. **本機驗證**
   - `dotnet build --configuration Release --no-restore` —— 確認無編譯錯誤
   - `./test.sh tests/Bee.Db.UnitTests/Bee.Db.UnitTests.csproj` —— 確認測試通過

5. **Plan 標記完成**
   - 文件頂部狀態改 `✅ 已完成（YYYY-MM-DD）`

6. **Commit & push**
   - 單一 commit,訊息範例:
     ```
     refactor(db): rename *SchemaHelper to *SchemaSyntax

     "Helper" 後綴語意過弱;這 5 個類別實際是各 dialect 的 SQL 語法 primitive。
     對齊既有 IDialectFactory 用語,改用 *Syntax 後綴。

     - MySqlSchemaHelper → MySqlSchemaSyntax
     - PgSchemaHelper → PgSchemaSyntax
     - OracleSchemaHelper → OracleSchemaSyntax
     - SqliteSchemaHelper → SqliteSchemaSyntax
     - SqlSchemaHelper → SqlSchemaSyntax
     ```
   - 直接 push to main(本機可驗證的桌面環境)

## 風險與注意事項

- **`SqlSchemaHelper` 替換時要小心** —— grep `SqlSchemaHelper` 是精準字串,不會誤中,但 IDE 的「全域 rename」若沒鎖檔案範圍可能誤動。建議用 grep + Edit replace_all,逐個 dialect 處理。
- **Public API 影響** —— 5 個類別都是 `internal static`,**沒有對外暴露**,不影響 NuGet 套件使用者。安全。
- **測試類別改名** —— 注意 testing.md 規則:測試類別命名跟被測類別對齊,所以 `SqliteSchemaHelperTests` 必改 `SqliteSchemaSyntaxTests`。

## 後續可考慮(本計畫不做)

- `MessagePackHelper` → `BeeMessagePackSerializer` 或 `SafeMessagePackSerializer`(下一個 plan)
- `DbConnectionManager` → `DbConnectionInfoCache` + 資料夾 `Manager/` → `Caching/`(下一個 plan)
- SqlServer provider 前綴 `Sql*` → `SqlServer*` 正規化(獨立 plan,範圍較大)
- 12 個 `*Func` 類別命名重新考量(設計層級議題,需先決策)
