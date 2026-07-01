[English](database-dialect-differences.md)

# 資料庫方言差異（DDL）

Bee.NET 由單一份 `TableSchema` 定義產生 DDL（CREATE TABLE / ALTER TABLE），並把各資料庫的差異封裝在 `src/Bee.Db/Providers/<Dialect>/` 底下的方言 adapter 內。應用開發者通常感覺不到這些差異——框架自身的 CRUD、seed 與 schema 升級路徑都已統一處理。

本文件彙整「當你手動撰寫 schema 定義或 hand-written SQL（例如測試 helper 的 `INSERT`、migration 腳本）時**會外露**」的 DDL 規則與**例外**。涵蓋五個支援的引擎：**SQL Server、PostgreSQL、MySQL、Oracle、SQLite**。

> 相關、聚焦於單一主題的文件：
> - [database-naming-conventions.zh-TW.md](database-naming-conventions.zh-TW.md) §5 — 識別符大小寫敏感性與引號方式。
> - [database-schema-upgrade.zh-TW.md](database-schema-upgrade.zh-TW.md) §4 — ALTER vs 重建的決策與各方言 ALTER 能力。
> - [src/Bee.Db/README.zh-TW.md](../src/Bee.Db/README.zh-TW.md) — SQLite 限制與 Oracle 識別符策略。

---

## 1. 為什麼文字與數值欄位預設 `NOT NULL`

這是框架刻意的設計決策，放在最前面說明，因為它是底下多數例外的根源。

`DbField.AllowNull` **預設為 `false`**（[DbField.cs](../src/Bee.Definition/Database/DbField.cs)）。規則如下：

| 欄位類別 | 是否可為 null | 預設值 |
|---------|-------------|-------|
| **文字**（`String`、`Text`） | `NOT NULL` | 空字串 `''` |
| **數值**（`Short`、`Integer`、`Long`、`Decimal`、`Currency`、`Boolean`） | `NOT NULL` | `0` |
| **Date / DateTime** | 未特別標示則 `NOT NULL` | 當下時間 |
| 真正需要 null 的欄位（例如「失效／到期時間」、選填的二進位） | **明確**設 `AllowNull="true"` | 無 |

### 原由

讓文字與數值欄位允許 `NULL`，會強迫每個使用端——應用程式碼**與** hand-written SQL——都得防範 null：

- `WHERE col = ''` 不會匹配 `NULL` 列，你得處處寫 `WHERE col IS NULL OR col = ''`。
- 聚合、join、字串運算都需要 `COALESCE` 或 null 判斷。
- C# 使用端只要漏了 null 檢查就可能 `NullReferenceException`。

透過保證「文字永不為 null、數值永不為 null」，框架讓你可以把空字串與 `0` 當成唯一的「空值」語意，完全省去 null 處理。**不要反射性地加 `AllowNull="true"`**——只有當欄位存在空字串或 `0` 無法表達的、真正的「未知／未設定」狀態時才設（典型是 session 到期時間之類的 `DateTime`，或選填的二進位 payload）。

### 資料庫牴觸此原則時如何仍守住保證

有兩個引擎無法直接實現「文字 `NOT NULL` 且預設空字串」。框架仍守住這個**契約**——見 [§3](#3-兩個硬性的-nullability-例外) 的兩個硬性例外。

---

## 2. 內建預設值運算式

對於沒有明確 `DefaultValue` 的 `NOT NULL` 欄位，各方言會輸出各自的預設值運算式。（當 `AllowNull="true"` 時，任何方言都**不**輸出預設值。）

| `FieldDbType` | SQL Server | PostgreSQL | MySQL | Oracle | SQLite |
|---------------|-----------|------------|-------|--------|--------|
| `String` / `Text` | `N''` | `''` | `''` | *（nullable — 見 §3）* | `''` |
| `Short`/`Integer`/`Long`/`Decimal`/`Currency`/`Boolean` | `0` | `0` | `0` | `0` | `0` |
| `Date` | `getdate()` | `CURRENT_TIMESTAMP` | `(CURRENT_DATE)` | `SYSTIMESTAMP` | `CURRENT_TIMESTAMP` |
| `DateTime` | `getdate()` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP(6)` | `SYSTIMESTAMP` | `CURRENT_TIMESTAMP` |
| `Guid` | `newid()` | `gen_random_uuid()` | `(UUID())` | `SYS_GUID()` | `(hex(randomblob(16)))` |

備註：

- **MySQL** 的函式型預設值需用括號包成*運算式*形式（`(UUID())`、`(CURRENT_DATE)`），因為 MySQL 只允許非字面值的預設值以括號運算式呈現。
- **SQLite** 無原生 UUID 產生器；`hex(randomblob(16))` 是「唯一但非嚴格 v4」的替代，對框架託管的預設值已足夠。
- **Boolean 字面值**：框架的標準形式是 `"1"` / `"0"`。PostgreSQL 的 `BOOLEAN` 欄不接受這兩者，故 PG 方言在輸出 SQL 的邊界將其轉為 `TRUE` / `FALSE`。其他方言皆接受 `1` / `0`。

---

## 3. 兩個硬性的 nullability 例外

這是最容易造成「4 個資料庫過、第 5 個掛」的規則。

### 3.1 Oracle：`''` 就是 `NULL`

Oracle 沒有「非 null 的空字串」——`''` **就是** `NULL`。所以 `VARCHAR2(n) DEFAULT '' NOT NULL` 是自相矛盾的（`DEFAULT ''` 即 `DEFAULT NULL`，與 `NOT NULL` 衝突）。

**框架的處理方式**（[OracleSchemaSyntax.cs](../src/Bee.Db/Providers/Oracle/OracleSchemaSyntax.cs)）：

- 僅在 Oracle 上，`String` / `Text` 欄位建為 **nullable**（不加 `NOT NULL`、不加 `DEFAULT ''`）。
- 「文字永不為 null」的契約在 C# 層守住：`ValueUtilities.CStr(null)` 回傳 `""`，故使用端仍只會看到空字串。
- `OracleTableSchemaProvider` 讀回這類欄位時報為 `AllowNull = false`，讓與定義的 schema diff 保持穩定。
- 明確的*非空*預設值在 Oracle 的 nullable 欄上仍是合法的非 null 字面值，故予以保留。
- `CLOB` / `BLOB` 在框架的 `CREATE TABLE` 形式下也不接受 inline 字面值 `DEFAULT`。

### 3.2 MySQL：`TEXT` / `BLOB` 不能有 `DEFAULT`

MySQL 禁止 `TEXT` / `BLOB` 欄位帶 `DEFAULT` 子句。因此 `AllowNull=false` 的 `Text` 欄位會輸出成 `TEXT NOT NULL` 且**無預設值**（[MySqlSchemaSyntax.cs](../src/Bee.Db/Providers/MySql/MySqlSchemaSyntax.cs)）——欄位維持 `NOT NULL`，但沒有 DB 端的 fallback 值。

**對 hand-written SQL 的後果：** 任何**省略**某個 `NOT NULL` `Text` 欄位的 `INSERT`，在 strict mode 下**只有 MySQL** 會失敗：

```
Field 'x' doesn't have a default value
```

同樣的 partial `INSERT` 在其他四個引擎會成功，因為它們的 `NOT NULL` 文字欄有隱式的 `DEFAULT ''`。框架自身的 CRUD 與 seed 一律列出每個欄位，故不受影響——會踩雷的是 **hand-written 原生 SQL**（測試 helper、seed 腳本、migration 片段）。

> **新增 `NOT NULL` `Text` 欄位時的規則：** 欄位維持 `NOT NULL`（**不要**為了繞過 MySQL 而改成 nullable），並確保每個 hand-written `INSERT` 都顯式提供值（空字串即可）。這與 §1 的框架原則一致；修正屬於 INSERT，不是欄位的 nullability。

> **測試注意事項：** *持久的*本機 MySQL 容器是以 `ALTER TABLE ... ADD COLUMN` 加新欄，這會**強制新欄為 nullable**，與 `AllowNull` 無關。這會在本機遮蔽此 bug，而 CI（全新 `CREATE TABLE`）會把它建為 `NOT NULL` 而失敗。要在本機重現 CI：先 `UPDATE ... SET <col> = '' WHERE <col> IS NULL`，再 `ALTER TABLE <t> MODIFY <col> LONGTEXT NOT NULL;`。

---

## 4. 識別符引號與大小寫

| | 引號形式 | 大小寫行為 |
|---|---------|-----------|
| SQL Server | `[name]`（`]` → `]]`） | quoted 小寫 |
| PostgreSQL | `"name"`（`"` → `""`） | quoted 小寫 |
| MySQL | `` `name` `` | quoted 小寫；以表級 `COLLATE utf8mb4_0900_ai_ci` 做大小寫不敏感比對 |
| Oracle | `"NAME"`（`"` → `""`） | **quoted 大寫**——Oracle 保留字範圍很廣（`COMMENT`、`SIZE`、`LEVEL`、`SESSION`…），故所有識別符都加引號；adapter 折成大寫以對齊 Oracle 原生 unquoted 行為，讀回時再正規化回小寫 |
| SQLite | `"name"`（`"` → `""`） | quoted 小寫；文字欄加 `COLLATE NOCASE` 做大小寫不敏感比對 |

完整的大小寫敏感性對照（識別符折疊 vs 資料比對）見 [database-naming-conventions.zh-TW.md](database-naming-conventions.zh-TW.md) §5。

---

## 5. AutoIncrement（自增）語法

`AutoIncrement` 在各引擎對應不同建構，且部分引擎要求與主鍵**內聯**在同一欄定義行：

| | 語法 | 需與 PK 內聯？ |
|---|------|--------------|
| SQL Server | `[int] IDENTITY(1,1)` | 否 |
| PostgreSQL | `GENERATED BY DEFAULT AS IDENTITY` | 否 |
| MySQL | `BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY` | **是** |
| Oracle | `NUMBER(19) GENERATED BY DEFAULT AS IDENTITY` | 否（PK 另外加） |
| SQLite | `INTEGER PRIMARY KEY AUTOINCREMENT` | **是**（無法用外部 PK 約束附加 `AUTOINCREMENT`） |

---

## 6. ALTER vs 重建資料表

當 schema 升級變更某欄位時，部分變更可用 `ALTER` 完成，其餘需重建資料表（建新表 + 複製 + 交換）。決策與各方言能力見 [database-schema-upgrade.zh-TW.md](database-schema-upgrade.zh-TW.md) §4。重點：

- **SQLite** 的 `ALTER` 只支援 `ADD COLUMN` / `RENAME COLUMN` / `DROP COLUMN`；其他（改型別、改 nullability、改約束）都需重建。
- **SQL Server** 在跨型別族變更與切換 AutoIncrement 時重建。
- **Oracle** 的 `ALTER TABLE ... MODIFY` 對 nullability 必須**基於差異**：對已經 `NOT NULL` 的欄再次下 `NOT NULL` 會觸發 `ORA-01442`，故 adapter 只在實際變化時才帶 `NULL` / `NOT NULL` 提示。
- **MySQL** 對帶非確定性預設值（`UUID()`）的 `Guid` 欄做 `ALTER ADD` 時，會拆成兩段語句以在 statement-based binlog 下維持 replication-safe。
- 資料表改名也不同：`sp_rename`（SQL Server）vs `RENAME TABLE`（MySQL）vs `ALTER TABLE ... RENAME`（PostgreSQL / Oracle / SQLite）。

---

## 7. 檢查清單：跨方言新增欄位

1. **依原則而非反射決定 nullability。** 文字／數值 → 保持 `AllowNull=false`（NOT NULL、預設 `''` / `0`）。只有真正的「未知／未設定」狀態才設 `AllowNull="true"`（例如 `DateTime` 到期時間、選填二進位）。
2. **更新每一個指向該表的 hand-written `INSERT`**，把新欄列進去——對 `NOT NULL` 的 `Text` 欄是**必須**的，因為 MySQL 不給它 DB 端預設值（§3.2）。框架自身的 CRUD/seed 已列出所有欄位。
3. **若欄位常態為空且需支援 Oracle**，要知道它在 Oracle 上會是實際 nullable（§3.1）；C# 層仍讀成空字串，故應用端不需改動。
4. **不要只憑持久的本機容器判定正確性。** 以 `ALTER` 加的新欄在本機被強制 nullable，會遮蔽 CI（與 production 首次建置）所走的全新 `CREATE` `NOT NULL` 行為。

---

## 參考

- 方言實作：`src/Bee.Db/Providers/<Dialect>/<Dialect>SchemaSyntax.cs`、`…TableSchemaProvider.cs`、`…AlterCompatibilityRules.cs`。
- 欄位模型：[DbField.cs](../src/Bee.Definition/Database/DbField.cs)。
- 相關文件：[database-naming-conventions.zh-TW.md](database-naming-conventions.zh-TW.md)、[database-schema-upgrade.zh-TW.md](database-schema-upgrade.zh-TW.md)、[src/Bee.Db/README.zh-TW.md](../src/Bee.Db/README.zh-TW.md)。
