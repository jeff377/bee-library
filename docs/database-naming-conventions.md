# 資料庫命名規範  

本文件定義資料庫表結構及系統欄位的命名規則，適用於所有資料庫物件設計。  
統一命名風格可避免跨資料庫時產生大小寫與語意不一致問題，並提升可維護性。  

---

## 1️⃣ 資料庫整體規範  

- 採用 **snake_case** 命名（小寫 + 底線）。  
- **避免** 使用 PascalCase 或 camelCase，因為部分資料庫（例如 PostgreSQL、Oracle）在有引號時會區分大小寫。  
- 所有 **資料表** 與 **欄位名稱** 均使用 **小寫字母**。  
- 系統層級表（例如設定、登入、稽核等）以 `st_` 開頭。  
- 業務／表單層級表以 `ft_` 開頭。  

---

## 2️⃣ 資料表與欄位命名  

| 類型 | 命名格式 | 範例 | 備註 |
|------|-----------|------|------|
| **表名稱** | `小寫_底線` | `user_accounts` | 明確描述資料內容，表名以名詞為主，不使用動詞。 |
| **欄位名稱** | `小寫_底線` | `user_id`, `created_at`, `dept_code` | 一律小寫，可使用必要且一致的縮寫；命名應能清楚表達用途。 |
| **主鍵欄位** | `sys_no` | `sys_no` | 系統自動遞增流水號。 |
| **唯一識別欄位** | `sys_rowid` | `sys_rowid` | GUID，全域唯一，不可修改。 |
| **外鍵欄位** | `{entity}_rowid` | `manager_rowid` | 關聯至他表之 `sys_rowid`，由應用層維護關聯一致性。 |

---

## 3️⃣ 索引與預設值命名  

| 類型 | 命名格式 | 範例 | 備註 |
|------|-----------|------|------|
| **主鍵索引** | `pk_表名` | `pk_employee` | `sys_no`，資料庫主鍵（流水號） |
| **唯一識別索引** | `rx_表名` | `rx_employee` | `sys_rowid`，系統同步唯一鍵（全域唯一） |
| **唯一資料索引** | `uk_表名` | `uk_employee` | `sys_id`，業務編號或單號唯一性 |
| **外鍵索引** | `fk_表名_欄名` | `fk_employee_dept` | 關聯一致性（欄位對應他表 `sys_rowid`） |
| **一般索引** | `ix_表名_欄位名` | `ix_users_email` | 查詢加速用（可複合或部分索引） |
| **預設值約束** | `df_表名_欄位名` | `df_users_created_at` | 欄位預設值定義 |


---

## 4️⃣ 系統欄位定義  

| 欄位名稱 | 說明 | 備註 |
|-----------|------|------|
| `sys_no` | 系統流水號，自動遞增 | 主鍵 |
| `sys_rowid` | 唯一識別碼（GUID），寫入後不可變 | 全域唯一識別 |
| `sys_master_rowid` | 明細關聯主檔之邏輯外鍵 | 由應用層控管關聯 |
| `sys_id` | 資料編號／單據號碼 | 業務層使用 |
| `sys_name` | 資料名稱／顯示名稱 | 業務層使用 |
| `sys_valid_date` | 生效日期（含當日） | 區間起始 |
| `sys_invalid_date` | 失效日期（不含當日） | 區間結束 |
| `sys_insert_time` | 寫入時間 | 系統自動填入 |
| `sys_update_time` | 更新時間 | 系統自動填入 |
| `sys_insert_user_rowid` | 建立者識別（邏輯外鍵） | 指向使用者 sys_rowid |
| `sys_update_user_rowid` | 更新者識別（邏輯外鍵） | 指向使用者 sys_rowid |

---

## 5️⃣ 跨資料庫大小寫敏感性對照  

不同資料庫對「識別符」（table、column、index 名）與「資料內容」（字串值比對）的大小寫處理規則差異很大。  
此節列出 Bee.NET 支援的 5 種資料庫的實際行為，作為 schema 設計與手寫 SQL 的參考依據。  

### 5.1 識別符（table、column、index 名）  

| 資料庫 | 未加引號識別符 | 加引號識別符 | 內部儲存 |
|--------|--------------|------------|---------|
| **SQL Server** | 不分大小寫¹ | 不分大小寫¹ | 保留原寫法，比對時不分大小寫 |
| **PostgreSQL** | fold 至 **lowercase** | case-sensitive，原樣保留 | 依規則決定 |
| **MySQL** | Table 名視 OS / `lower_case_table_names`²；Column / Index 名**永遠**不分大小寫 | 同左 | 同左 |
| **SQLite** | 不分大小寫 | 不分大小寫 | 保留原寫法 |
| **Oracle** | fold 至 **UPPERCASE** | case-sensitive，原樣保留 | 依規則決定 |

¹ 取決於 database / column collation；Bee.NET 預期使用 `_CI_`（Case-Insensitive）類 collation，預設值即符合。  
² Linux 預設敏感（table 名對應 filesystem 檔名）；Windows / macOS 預設不敏感。可由 `lower_case_table_names` 全域設定覆寫。  

#### 同一 DB 內三者是否一致  

| 資料庫 | Table / Column / Index 行為一致性 |
|--------|------------------------------|
| SQL Server | ✅ 一致（皆由 collation 決定） |
| PostgreSQL | ✅ 一致（皆走「未引號 → lower、引號 → 原樣」） |
| **MySQL** | ❌ **不一致**（table 視 OS、column / index 永遠不分） |
| SQLite | ✅ 一致（皆不分大小寫） |
| Oracle | ✅ 一致（皆走「未引號 → UPPER、引號 → 原樣」） |

PostgreSQL 與 Oracle 是「**相反方向**」的 fold：同一支未加引號 SQL，PG 視為 lowercase、Oracle 視為 UPPERCASE。  
這是這兩個 DB 在跨資料庫場景中最常造成問題的根因。  

### 5.2 資料內容（字串值比對）  

| 資料庫 | 預設字串比對 | Bee.NET 處理 |
|--------|----------|-----------|
| **SQL Server** | 不分大小寫¹ | 維持預設 |
| **PostgreSQL** | **分大小寫**（byte 比對） | 維持預設；應用層需顯式使用 `ILIKE` 或 `LOWER()` |
| **MySQL** | 不分大小寫¹ | 維持預設 |
| **SQLite** | **分大小寫**（`BINARY`） | 可在 column 上加 `COLLATE NOCASE` |
| **Oracle** | **分大小寫**（`BINARY`） | 連線啟動時設定 session NLS：`NLS_COMP='LINGUISTIC'` + `NLS_SORT='BINARY_CI'`，使 `=`、`LIKE` 改為不分大小寫 |

¹ 取決於 column collation，預設 collation（SQL Server `*_CI_*`、MySQL `*_ci`）即不分大小寫。  

> **注意：識別符與資料內容是兩個獨立層次**  
> - 識別符敏感性由 SQL parser 階段決定，影響 schema 物件的查找  
> - 資料內容敏感性由 SQL 執行階段決定，受 collation / NLS / `COLLATE` 子句影響  
> - 兩層的設定彼此獨立，不會互相影響  

### 5.3 Bee.NET adapter 的識別符策略  

Framework 對 5 DB 採「**統一加引號 + DB 各自最自然的大小寫**」策略，由 adapter 在邊界處理 case 翻譯：  

| DB | DDL/DML 中識別符儲存形式 | 引號方式 |
|----|---------------------|---------|
| SQL Server | 原樣（小寫，案例不敏感） | `[name]` |
| PostgreSQL | **小寫**（fold 一致） | `"name"` |
| MySQL | 原樣（小寫） | `` `name` `` |
| SQLite | 原樣（小寫） | `"name"` |
| **Oracle** | **UPPERCASE**（與 Oracle 預設 fold 方向一致） | `"NAME"` |

Oracle 是 outlier：framework 在 emit DDL/DML 時將識別符 `.ToUpperInvariant()` 後再加引號（`OracleSchemaHelper.QuoteName` / `DbFunc.QuoteIdentifier(Oracle, ...)`），因此 Oracle 內部 data dictionary 看到的是 UPPERCASE（如 `ST_USER`、`SYS_NO`、`COMMENT`）。  
讀取端（`OracleTableSchemaProvider`）在從 `USER_TAB_COLUMNS` 等 view 取回後，將識別符 `.ToLowerInvariant()` 才裝進 `TableSchema` / `DbField`。  

> **抽象一致性**：`FormSchema`、`Bee.Repository`、`Bee.Business` 等上層只看得到 lowercase 識別符（如 `st_user`），不需要知道 Oracle 內部存的是 UPPERCASE。case 翻譯被完全封裝在 Oracle adapter 內部。  

#### 為什麼 Oracle 採 UPPERCASE 儲存  

1. **與 Oracle 慣例對齊**：DBA 在 SQL Developer / DBeaver / sqlplus 看到的物件名為 `ST_USER`，符合 Oracle 預設 unquoted-fold-to-UPPER 行為的視覺結果  
2. **手寫 SQL 對 Oracle 友善**：`SELECT * FROM st_user` 由 Oracle parser fold 為 `ST_USER` 剛好對應 storage，**不必每處都加引號**（與其他 4 DB 維持 lowercase 直觀寫法一致）  
3. **Reserved word 仍能用作欄位**：因為 framework 仍 emit `"COMMENT"`、`"ORDER"` 加引號形式，Oracle parser 視為 quoted identifier 而非 reserved word token，命名上不需禁用 reserved words  

#### 命名規範的驗證面  

第 1～4 節的「全小寫 + snake_case」命名規範，背後對應以下觀察：  

1. **跨 DB 抽象上一律小寫**：FormSchema 中的識別符宣告永遠是 lowercase，由 framework 各 DB adapter 翻譯成該 DB 的儲存形式  
2. **Oracle 內部 UPPERCASE 是封裝細節**：開發者寫 FormSchema、Repository、BO 時不需感知 Oracle 用 UPPERCASE，這是 adapter 邊界內部行為  
3. **Reserved words 建議避開**：雖然 framework 加引號可規避 reserved word 衝突（5 DB 都豁免於 quoted identifier 規則），但仍建議避開 `comment`、`order`、`user`、`size`、`group`、`level`、`number` 等常見 reserved words，降低手寫 SQL 與工具相容的麻煩  
4. **資料內容比對 case-insensitive**：framework 已抹平 5 DB 差異（Oracle 以 session NLS 補齊），手寫 SQL 寫 `WHERE name = 'jeff'` 在 5 DB 上行為一致  

---

## ✅ 結語  

統一的命名規範可確保資料庫結構清晰、一致，降低開發與維護的溝通成本，並在多系統整合時提升可靠性與擴充性。  
