# 計畫：Oracle 改採 quoted-UPPERCASE 識別符策略

**狀態：✅ 已完成（2026-04-30）**

## 背景

Bee.NET 目前對所有 5 種支援的資料庫（SQL Server / PostgreSQL / MySQL / SQLite / Oracle）統一採用 **quoted-lowercase** 識別符策略：framework 產生的 DDL / DML 對 table、column、index 等識別符一律加雙引號（或對應 quote 字符），且名稱本身為小寫（如 `"st_user"`、`"sys_id"`）。

這對 SQL Server / PostgreSQL / MySQL / SQLite 都是自然且最安全的選擇，但**對 Oracle 是反向走**：

- Oracle 對未加引號的識別符會 fold 至 **UPPERCASE**（PostgreSQL 是 lowercase，方向相反）
- Oracle 慣例下，`ALL_TABLES`、SQL Developer、DBeaver、sqlplus 看到的物件名都是 UPPERCASE
- Bee.NET 目前在 Oracle 內部存的是 quoted-lowercase（`st_user`），與 Oracle 生態不符
- 任何手寫 SQL（即使只是 DBA debug 用的 ad-hoc query）必須加引號才能對得上

更直接的問題（已在 [plan-oracle-integration-tests.md](plan-oracle-integration-tests.md) 落地過程中浮現）：
- `OracleSchemaHelper.QuoteName` / `DbFunc.QuoteIdentifier(Oracle)` 的小寫策略，導致 Oracle DBA 看到「不像 Oracle」的 schema
- 整合測試需要在所有 SQL 中明確加 `"..."`，可讀性差
- 雖然 framework 自身 SQL 是正確的，但與 Oracle 工具生態的一致性差

### 為什麼不直接「Oracle 不加引號」（unquoted UPPERCASE）

評估過 unquoted 方案（DDL 直接 `CREATE TABLE st_user (...)`，由 Oracle 自動 fold 為 UPPERCASE 儲存），但有兩個無法接受的副作用：

1. **Reserved word 欄位無法使用**：`comment`、`order`、`user`、`size`、`group`、`level`、`number`、`date`、`type`、`value` 等常見命名都是 5 DB 之一的 reserved word；unquoted 形式會直接 syntax error，必須在 FormSchema 載入時加禁用清單
2. **特殊字元欄位無法使用**：含空白、雙引號等的 identifier 完全無解
3. 命名規範限制本身可以接受（其他 DB 也建議避開 reserved word），但與「framework 必須穩定處理任意合法 schema 名稱」的定位衝突

換言之：**保留 quoting 是 framework 健壯性的剛性需求**。

## 目標

讓 Oracle 路徑採 **quoted-UPPERCASE** 識別符策略：framework 產生的 SQL 對 Oracle 識別符仍然加雙引號，但名稱本身轉為 UPPERCASE。其他 4 DB 維持 quoted-lowercase 不變。

達到的效果：
- Oracle 內部存的物件名稱為 UPPERCASE（`ST_USER`、`SYS_ID`、`COMMENT`），符合 Oracle 慣例
- DBA 在 SQL Developer / DBeaver / sqlplus 看到的 schema 與 Oracle 預設行為一致
- Reserved word 欄位仍可用（quoted 形式對所有 5 DB 豁免於 reserved word 規則）
- 特殊字元欄位仍可用
- 任何手寫 SQL（裸寫 unquoted）也能對得上 Oracle storage（Oracle 自動 fold 至 UPPER 剛好對齊），雖然手寫 SQL 不在本計畫主要範圍

不在範圍：
- 改動 SQL Server / PostgreSQL / MySQL / SQLite 的 quoting 策略（皆維持現狀）
- 移除 framework 的 quoting 機制（quoting 是健壯性需求，必須保留）
- FormSchema 加 reserved word 禁用清單（quoted-UPPERCASE 不需要這個約束）

## 現況評估

### 已掃描的 Oracle 路徑（[plan-oracle-integration-tests.md](plan-oracle-integration-tests.md) 的探勘成果）

兩個核心 quoting 入口：

1. **[`DbFunc.QuoteIdentifier(DatabaseType, string)`](../../src/Bee.Db/DbFunc.cs)**（行 52-72）
   - CRUD / Dml builders（INSERT / UPDATE / DELETE / SELECT / FROM / WHERE / ORDER BY）統一走這
   - Oracle entry：`s => $"\"{s.Replace("\"", "\"\"")}\""`

2. **[`OracleSchemaHelper.QuoteName(string)`](../../src/Bee.Db/Providers/Oracle/OracleSchemaHelper.cs)**（行 24-27）
   - DDL（`OracleCreateTableCommandBuilder` / `OracleTableAlterCommandBuilder` / `OracleTableRebuildCommandBuilder`）走這
   - 實作：`$"\"{identifier.Replace("\"", "\"\"")}\""`

兩處皆**無 case 轉換**，是這次的修改點。

### Oracle Schema 讀取路徑

[`OracleTableSchemaProvider`](../../src/Bee.Db/Providers/Oracle/OracleTableSchemaProvider.cs) 從 `ALL_TAB_COLUMNS` / `ALL_CONSTRAINTS` / `ALL_INDEXES` 讀回 schema 結構。Oracle 資料字典回傳的識別符在新策略下將是 UPPERCASE，需要在 boundary 將結果 lowercase 化，讓上層（FormSchema、Repository、Business）感覺不到內部 case 差異。

### 測試與 production 部署

- **production 部署**：framework 還在開發階段，無正式 Oracle production，無 migration 成本
- **既有 Oracle 整合測試**（剛落地的 4 個 + 原本的 3 個）：需要小幅斷言調整，估計 10–20 行
- **Oracle 純語法單元測試**（80 個於 `OracleCreateTableCommandBuilderTests` / `OracleTableAlterCommandBuilderTests` / `OracleTableRebuildCommandBuilderTests` / `OracleFormCommandBuilderTests`）：所有期望 SQL 字串中的小寫 identifier 都要改為 UPPERCASE

## 範圍：修改清單

### Production code（核心改動，極小）

#### 1. `DbFunc.QuoteIdentifier` Oracle entry

```csharp
// 行 57：
{ DatabaseType.Oracle, s => $"\"{s.ToUpperInvariant().Replace("\"", "\"\"")}\"" },
```

差異：加 `.ToUpperInvariant()`。

#### 2. `OracleSchemaHelper.QuoteName`

```csharp
public static string QuoteName(string identifier)
    => $"\"{identifier.ToUpperInvariant().Replace("\"", "\"\"")}\"";
```

差異：同上，加 `.ToUpperInvariant()`。

#### 3. `OracleTableSchemaProvider` — 讀回 boundary 做 lowercase 正規化

需處理的點：
- `GetTableSchema(string tableName)` 入參：查詢 `ALL_TABLES.TABLE_NAME` 時將參數值 `.ToUpperInvariant()`
- 同理對 `ALL_TAB_COLUMNS` 等的 owner / table_name / column_name 等查詢條件
- 解析回傳結果時，將 `TABLE_NAME` / `COLUMN_NAME` / `INDEX_NAME` / `CONSTRAINT_NAME` 等識別符 `.ToLowerInvariant()` 後再裝入 `TableSchema` / `DbField` / `TableSchemaIndex`
- 維持 `schema.TableName = tableName`（呼叫端傳入的小寫值）以保證跨 DB 抽象一致

具體修改範圍待實作時逐一檢視，估計 5–10 處小調整。

### Production code（無需改動）

- `OracleCreateTableCommandBuilder` / `OracleTableAlterCommandBuilder` / `OracleTableRebuildCommandBuilder`：透過 `OracleSchemaHelper.QuoteName` 走，自動受益
- `OracleFormCommandBuilder` 與所有 `Bee.Db.Dml` builders：透過 `DbFunc.QuoteIdentifier(Oracle, ...)` 走，自動受益
- `DbCommandSpec.NormalizeParameterValue` / `NormalizeDbType`：與 quoting 無關，不動

### 測試調整

#### 純語法測試（`OracleCreateTableCommandBuilderTests` 等）

所有 `Assert.Contains("\"st_demo\"", sql)` 形式的斷言要改為 `Assert.Contains("\"ST_DEMO\"", sql)`。範圍：
- `OracleCreateTableCommandBuilderTests.cs`（28 個測試）
- `OracleTableAlterCommandBuilderTests.cs`（搭配 `OracleTableRebuildCommandBuilderTests.cs`，28 個）
- `OracleFormCommandBuilderTests.cs`（7 個）
- 其他相關純語法測試

機械改動，可一次 search-replace。

#### 整合測試（`OracleIntegrationTests`，剛落地）

| 測試 | 調整 |
|------|------|
| `SchemaProvider_ReadsFixtureTable` | `Assert.Equal("st_user", schema.TableName)` 仍正確（adapter 內部 lower normalize 後傳出 lowercase） |
| `SchemaProvider_UnknownTable_ReturnsNull` | 不需改 |
| `StringComparison_IsCaseInsensitive` | 手寫 DDL 字串中的 `"ci_test"` 改為 `"CI_TEST"`（或保留 lowercase 但要驗證新 adapter 是否對小寫 quoted 名稱不認識） |
| `FormCrud_QuotedLowercaseTable_*` | 走 framework，不需改邏輯；測試名稱建議改為 `FormCrud_QuotedIdentifiers_*`（不再以 case 命名） |
| `Join_QuotedLowercaseTables_*` | `FromBuilder` 斷言 `"tb_it_master"` → `"TB_IT_MASTER"`、ON 條件 field 同步改 |
| `ReservedWordFieldName_*` | 仍通過（quoted 形式對 reserved word 豁免），測試名稱可改為 `ReservedWordFieldName_QuotedIdentifiers_*` |
| `AlterAddColumn_*` | 手寫驗證 SQL（`INSERT INTO \"tb_it_alter\"`、`SELECT \"name\" FROM ...`）UPPER 化 |

#### Mutation test 重設

新策略下的 mutation 測試組合：
1. 拿掉 `OracleSchemaHelper.QuoteName` 的 `.ToUpperInvariant()` → DDL 產出 lowercase quoted → 與 Oracle data dictionary（UPPERCASE）對不上 → 整合測試應全失敗
2. 拿掉 `DbFunc.QuoteIdentifier(Oracle)` 的 `.ToUpperInvariant()` → DML 同樣對不上 → 失敗
3. 拿掉 quote 包裝（保留 UPPERCASE）→ reserved word 測試失敗（`COMMENT` 直接是 reserved token）

執行流程同 [plan-oracle-integration-tests.md](plan-oracle-integration-tests.md) 既有的 mutation 步驟。

### 文件更新

- **[`docs/database-naming-conventions.md`](../database-naming-conventions.md)** 第 5 節：補一句說明 Oracle adapter 內部以 UPPERCASE 儲存，case 翻譯由 adapter 在邊界處理，FormSchema / Repository / Business 等上層感覺不到
- **[`docs/archive/plan-oracle-support.md`](../archive/plan-oracle-support.md)**（已封存）：不動，但可在新 plan 補一條 cross-reference
- **[`src/Bee.Db/README.md`](../../src/Bee.Db/README.md)** / **`README.zh-TW.md`**：若有提到 Oracle quoting 細節，同步說明新策略

## 驗收條件

- [ ] `DbFunc.QuoteIdentifier(Oracle, "st_user")` 回傳 `"ST_USER"`
- [ ] `OracleSchemaHelper.QuoteName("st_user")` 回傳 `"ST_USER"`
- [ ] `OracleTableSchemaProvider.GetTableSchema("st_user")` 仍回傳 `TableSchema { TableName = "st_user", ... }`（lowercase 對外）
- [ ] 80 個 Oracle 純語法測試全綠（assertion 改為 UPPERCASE）
- [ ] 7 個 Oracle 整合測試全綠（含 schema reader、CRUD、JOIN、reserved word、ALTER）
- [ ] Mutation test：拿掉 Oracle 路徑的 `.ToUpperInvariant()`，整合測試應全部失敗；revert 後全綠
- [ ] 跨 DB 既有測試（SQL Server / PostgreSQL / SQLite / MySQL 對應的 unit + integration）行為不變
- [ ] `database-naming-conventions.md` 第 5 節補上 adapter 邊界 case 翻譯說明

## 預期工時

- production code 改動：~30 分鐘（兩處 `.ToUpperInvariant()` + schema provider 邊界正規化）
- 純語法測試 assertion 改寫：~30 分鐘（search-replace + 抽樣驗證）
- 整合測試調整：~20 分鐘
- mutation 驗證：~10 分鐘
- 文件更新：~10 分鐘

合計約 1.5–2 小時。

## 後續（不在本計畫範圍）

- 若 framework 將開放給開發者寫**手寫 SQL** 對 Oracle，本策略下 unquoted 寫法（`SELECT * FROM st_user`）會被 Oracle 自動 fold 至 `ST_USER` 對得上 storage。屆時可在 `database-naming-conventions.md` / `code-style.md` 補一條手寫 SQL 規範
- 若未來支援第 6 種 DB，需評估其 fold 方向與 reserved word 集合是否仍能用同樣策略
