# 計畫：修正 Oracle ALTER 冗餘 NOT NULL（ORA-01442）

**狀態：✅ 已完成（2026-06-04）**

## 背景

### 症狀
本機**持久 Oracle container** 重跑 schema upgrade 時，對**已經是 `NOT NULL` 的欄位**再下一次 `ALTER TABLE ... MODIFY (col ... NOT NULL)`，Oracle 拋 **`ORA-01442`**（_column to be modified to NOT NULL is already NOT NULL_）。MySQL 容忍這種冗餘、PG 走拆句 diff，唯獨 Oracle 嚴格拒絕。

### 連鎖後果
`SharedDatabaseState.EnsureSchemaAndSeed`（測試 fixture）對某張既有表 upgrade 撞 `ORA-01442` → 該 DB 的 setup **中斷並 skip 後續** → 排在後面的新表（如 line-b 的 `st_role_grant`）來不及建 → 測試查詢時變 `ORA-00942`（表不存在）。line-b 開發期間每次改 schema 都得「drop 整個 Oracle schema 讓它全走 CREATE」才能繞過。

### 為何 CI 不踩、只有本機踩
- **CI**：每次全新 container，第一次是 `CREATE TABLE`（不走 ALTER path）→ 永不 `MODIFY NOT NULL` → 不踩。
- **本機**：Oracle container 跑很久、表已存在，`EnsureSchema` 走 upgrade(ALTER) path → 對既有 `NOT NULL` 欄重發 `MODIFY NOT NULL` → 炸。

### 根因
`OracleTableAlterCommandBuilder.BuildAlterFieldStatement`（`src/Bee.Db/Providers/Oracle/OracleTableAlterCommandBuilder.cs:94-98`）只接 `newField`、無條件呼叫 `OracleSchemaSyntax.GetColumnDefinition(newField)` 重發**完整**欄位定義（type + default + **nullability**）：

```csharp
private static string BuildAlterFieldStatement(string tableName, DbField newField)
{
    string newDef = OracleSchemaSyntax.GetColumnDefinition(newField);
    return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} MODIFY ({newDef});";
}
```

`AlterFieldChange` 由 `TableSchemaComparer` 在 `DbField.Compare` 偵測到**任一**屬性差異（type / length / precision / scale / **AllowNull** / default）時產生；builder 卻不知「哪個變了」，一律連 nullability 整段重發。當差異只在 type/length（nullability 沒變）時，仍帶上冗餘 `NOT NULL` → `ORA-01442`。

> 這支方法的 XML `<remarks>`（第 88-92 行）早已預告此雷，並指出「the full re-definition output here may need **diff-based trimming** when integration tests cover the upgrade path」。

### 已修好的相關設計（不在本計畫範圍，作為對照）
`plan-oracle-string-nullability.md`（已完成）已處理 **String/Text** 欄：Oracle `'' == NULL`，故字串欄一律建為 nullable，且 `OracleTableSchemaProvider` 讀回時把 `AllowNull` 報成 `false` 對齊定義，避免字串欄反覆 ALTER。本計畫補的是**非字串欄**（Guid / Integer / DateTime / Decimal）的冗餘 `NOT NULL`。

## 修正方案

對齊 PG dialect（`PgTableAlterCommandBuilder.BuildAlterFieldStatements`）的 diff-based 精神，但**保留 Oracle 單句 `MODIFY` 語法**：只在 nullability **實際改變**時才帶 `NULL` / `NOT NULL` hint；沒變則省略（Oracle `MODIFY` 省略 nullability 子句時保留現狀）。

### 觸點

1. **`OracleSchemaSyntax`**（`src/Bee.Db/Providers/Oracle/OracleSchemaSyntax.cs`）— 拆出可重用片段：
   - `GetColumnTypeAndDefault(field)` → `"name" TYPE[ DEFAULT ...]`（不含 nullability）
   - `GetNullabilityClause(field)` → `NULL` / `NOT NULL`（沿用既有 `isNullableText` 邏輯：String/Text 一律 `NULL`）
   - `GetColumnDefinition(field)` 改為 `GetColumnTypeAndDefault + " " + GetNullabilityClause`（CREATE / ADD 行為**不變**）

2. **`OracleTableAlterCommandBuilder.BuildAlterFieldStatement`** — 改簽章接 `oldField + newField`，diff nullability：
   ```csharp
   private static string BuildAlterFieldStatement(string tableName, DbField oldField, DbField newField)
   {
       string typeDef = OracleSchemaSyntax.GetColumnTypeAndDefault(newField);
       // 只在 effective nullability 真正改變時帶 hint；對已 NOT NULL 欄重發 NOT NULL → ORA-01442。
       string oldNull = OracleSchemaSyntax.GetNullabilityClause(oldField);
       string newNull = OracleSchemaSyntax.GetNullabilityClause(newField);
       string nullClause = oldNull != newNull ? $" {newNull}" : string.Empty;
       return $"ALTER TABLE {OracleSchemaSyntax.QuoteName(tableName)} MODIFY ({typeDef}{nullClause});";
   }
   ```
   用 `GetNullabilityClause`（含 String 一律 `NULL` 的 effective 值）而非裸 `AllowNull`，確保字串欄 old/new 都算 `NULL` → 永遠省略 hint，與既有讀回對齊邏輯一致。

3. **`GetStatements`** case `AlterFieldChange` — 傳 `alter.OldField, alter.NewField`。

### 為何不拆成 PG 的多句
Oracle `MODIFY` 一句即可同時改 type + default，且省略 nullability 子句會保留現狀；不需要 PG 那種 type / nullability / default 三句拆分。最小改動、語意清楚。

## 測試

### 單元（`tests/Bee.Db.UnitTests/OracleTableAlterCommandBuilderTests.cs`）
- **更新既有** `GetStatements_AlterField_EmitsModify`：String length 50→100、nullability 不變 → 斷言 `MODIFY` 帶新 type、**不帶** `NULL`/`NOT NULL`（現行斷言 `Contains("... NULL")` 需改為 `DoesNotContain` nullability hint）。
- **新增** nullability 真正改變：Integer `AllowNull true→false` → 斷言帶 `NOT NULL`；`false→true` → 帶 `NULL`。
- **新增** 冗餘案例（回歸）：Integer `AllowNull false→false`、僅 default 變 → 斷言**不帶** `NOT NULL`（防 ORA-01442 重現）。

### 整合（`[DbFact(DatabaseType.Oracle)]`）
新增 upgrade-path 測試：CREATE 一張帶非字串 `NOT NULL` 欄的表 → 對**同定義**再跑一次 alter（type 不變、nullability 不變）→ 斷言**不拋** `ORA-01442`（`Record.Exception` + `Assert.Null`）；另跑一次「只改 length」驗證 MODIFY type 成功且 nullability 未受影響。

## 邊界與非目標
- **NOT NULL on existing NULL data（`ORA-02296`）**：當 nullability 真的 `NULL→NOT NULL` 但欄內已有 NULL 值，Oracle 會拒。這屬 narrowing/資料遷移議題，由 `UpgradeOptions.AllowColumnNarrowing` / rebuild path 處理，**不在本計畫範圍**。
- 本計畫**只**消除「nullability 未變卻重發 hint」的冗餘，不改變任何「真的要改 nullability」的行為。
- MySQL / PG dialect **不動**（MySQL 本就容忍、PG 已 diff）。

## 驗收
- slnx build 0/0、`TreatWarningsAsErrors` 通過。
- `Bee.Db.UnitTests` 全綠（含更新 + 新增的 nullability 案例）。
- Oracle upgrade-path 整合測試：對既有 `NOT NULL` 欄重跑 alter 不再 ORA-01442。
- 本機**不需再 drop 整個 Oracle schema** 即可重跑 `EnsureSchema`（手動驗證：持久 container 連續兩次跑 Oracle 測試皆綠）。
- CI 全 5 DB 綠 + SonarCloud quality gate passed。
