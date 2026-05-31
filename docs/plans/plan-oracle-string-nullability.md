# 計畫：Oracle VARCHAR2 對 string `AllowNull=false` 的 nullability 修正

**狀態：✅ 已完成（2026-05-31）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | DDL 生成：`OracleSchemaSyntax` 對 String/Text 欄一律建 nullable、移除 `DEFAULT ''` | ✅ 已完成（2026-05-31） |
| 2 | nullability 比對：Oracle diff 視「definition NOT NULL ≡ 實際 nullable」為相等，避免反覆 ALTER | ✅ 已完成（2026-05-31） |
| 3 | 測試更新 + `customize_id` 改回 `AllowNull=false`（Oracle DDL 單元測試 + 5 DB 整合） | ✅ 已完成（2026-05-31） |

## 背景

Oracle 把空字串 `''` 等同 `NULL`。框架目前對 definition String 欄 `AllowNull=false`，在 Oracle 生成：

```sql
"col" VARCHAR2(n) DEFAULT '' NOT NULL
```

（`OracleSchemaSyntax.GetColumnDefinition` + `GetDefaultExpression`，`src/Bee.Db/Providers/Oracle/OracleSchemaSyntax.cs`）。

這是**自相矛盾**的：`DEFAULT ''` 在 Oracle 即 `DEFAULT NULL`，與 `NOT NULL` 直接衝突 —— 任何省略該欄的 `INSERT` → `ORA-01400: cannot insert NULL`。

**觸發點**：多租戶 `st_company.customize_id`（標準版常態為空）。其他既有 String NOT NULL 欄不踩雷，純因業務值總是非空、所有 INSERT 都顯式給值，從不依賴 `DEFAULT ''`。`customize_id` 是第一個「常態值為空、INSERT 常省略」的 String 欄，撞出這個既有矛盾。

> 現況以 interim 處理：`customize_id` 暫標 `AllowNull="true"`（CI 已綠）。本 plan 階段 3 完成後改回 `AllowNull=false`。

## 使用者設計原則（須遵循）

文字/數值欄**不允許 null**：沒指定預設值就是空字串或 0。框架在 SQL Server / MySQL / PostgreSQL / SQLite 已內建此機制（各 `SchemaSyntax.GetDefaultValue`：NOT NULL String → `DEFAULT ''`、數值 → `DEFAULT 0`、日期 → 當下時間）。

Oracle 因 `''=NULL` 無法表達「非 null 的空字串」，故對 String/VARCHAR2 欄的**正確對應**是：建為 nullable（Oracle 以 NULL 表示空字串），讀取端 `ValueUtilities.CStr(null)→""` 正規化，**上層 C# 永遠看到空字串、不見 null**。definition 仍標 `AllowNull=false`（語意上「不該是 null」），由 dialect 層吸收 Oracle 的特殊性。

## 核心設計

**Oracle dialect**：String（`VARCHAR2`）與 Text（`CLOB`）欄，**不論 definition `AllowNull`**，DDL 一律建 nullable（不加 `NOT NULL`）、不加 `DEFAULT ''`。其餘型別（數值 / 日期 / Guid / Boolean）維持現狀（`NOT NULL` + `DEFAULT 0`/`SYSTIMESTAMP`/…）。

**其他 4 個方言不動**：維持 `NOT NULL` + `DEFAULT ''`（符合使用者偏好）。本 plan 只動 Oracle。

## 關鍵風險：schema diff 反覆 ALTER（必須一併處理）

`TableSchemaComparer.CollectFieldChanges` 以 `DbField.Compare` 判定欄位差異，`Compare` 會比對 `AllowNull`（`DbField.cs` line ~170）。`OracleTableSchemaProvider` 從 `ALL_TAB_COLUMNS.NULLABLE` 讀回真實 nullability（`NULLABLE='Y' → AllowNull=true`，line 227/258）。

若只改 DDL 生成（String 建 nullable）而不動比對：definition `AllowNull=false` vs DB 實際 `AllowNull=true` → `Compare` 不一致 → **每次 schema 升級都產生 `AlterFieldChange`、反覆嘗試 `ALTER ... MODIFY NOT NULL`**（且 Oracle 對已含 NULL 的欄改 NOT NULL 會失敗）。這是 plan 必須解掉的回歸點。

**解法選項（plan 內定案）**：
- **(A) provider 讀回對齊（建議，集中、風險低）**：`OracleTableSchemaProvider` 讀回 String/Text 欄時，一律回報 `AllowNull=false` 以對齊 definition，使 `Compare` 視為相等。代價：反向讀出的 schema 不反映「DB 實際 nullable」，但對 diff 目的足夠。
- **(B) dialect-aware 比對**：在 `TableSchemaComparer` / `DbField.Compare` 注入方言旗標，對 Oracle String/Text 欄忽略 nullability 差異。較通用但動到共用比對路徑，影響面廣。

> **實作結論（採方案 A）**：`OracleTableSchemaProvider.ParseDbField` 對 String/Text 欄一律 `AllowNull=false`，與 definition 對齊，`Compare` 視為相等、不再反覆 ALTER。
> 另：String 欄若帶**非空**顯式 `DefaultValue`（valid non-null literal），DDL 仍保留 `DEFAULT '<literal>'`（只丟棄空字串預設），避免讀回端 `DefaultValue` 與 definition 不符而觸發 diff loop。

## 影響面

- **src**：`OracleSchemaSyntax`（`GetColumnDefinition` / `GetDefaultExpression`）；`OracleTableSchemaProvider`（nullability 對齊，方案 A）；視方案可能含 `TableSchemaComparer` / `DbField.Compare`。
- **tests（~10+）**：`OracleCreateTableCommandBuilderTests`、`OracleTableAlterCommandBuilderTests`、`OracleTableSchemaProvider*Tests`、`OracleIntegrationTests` 等斷言含 `DEFAULT '' NOT NULL` 的 Oracle String 欄 DDL，需更新。
- **整合驗證**：5 DB 升級冪等性（對既有 Oracle 表連跑兩次升級，第二次應 0 變更）。

## 驗證重點

- **升級冪等**：既有 Oracle 表連跑兩次 schema 升級，第二次 diff 應為空（證明無反覆 ALTER）。
- `customize_id` 設 `AllowNull=false` 下，Oracle `INSERT` 省略該欄成功、讀回空字串。
- 其餘 4 DB 維持 `NOT NULL` + `DEFAULT ''`，行為不變。

## 與多租戶 plan 的關係

`plan-multitenant-customization.md` 的 `customize_id` 現為 interim `AllowNull="true"`。本 plan 階段 3 完成後，把 `st_company.customize_id` 改回 `AllowNull=false`（definition 語意正確；Oracle 自動 nullable、其餘 DB NOT NULL+`''`）。多租戶 plan 階段 4-5 不依賴此項，可平行推進。
