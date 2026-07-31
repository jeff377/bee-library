# 資料庫規範

> 各 provider 的踩雷細節與推導過程見 `docs/repo-ops/gotchas/database.md`（按需讀，不常駐）。

## 兩個正交維度：表前綴 vs CategoryId

| 軸 | 值 | 意義 |
|----|----|------|
| **表前綴** | `st_` / `ft_` | 框架機制 vs 業務資料（**誰用**） |
| **CategoryId** | `common` / `company` / `log` | 資料落在哪個 DB scope（**哪裡**） |

**前綴不綁定 DB 位置。** 決定性例證：`st_department` / `st_employee` 是框架所有
（record-scope 與組織樹功能所需）卻位於**公司資料庫**；權限的 `st_role` / `st_role_grant` /
`st_user_role` 同理，`user_rowid` 跨 DB 邏輯指向 common 的 `st_user.sys_rowid`。
「st_ 在 common、ft_ 在 company」只是常見組合，不是規則。權威清單見
`docs/framework-reserved-names.zh-TW.md`。

`FormSchema.CategoryId`（與 `DbCategory.Id`、`DatabaseItem.CategoryId`）**不是自由字串**：
`FormRepositoryFactory.ParseCategoryId` 只認三值，其餘丟 `Unknown schema.CategoryId`。

- **`company`** = 各公司獨立資料。**業務表（`ft_*`）與應用組織表（`st_department`/`st_employee`）
  都必須是 company**。router 走 `session.CompanyId → ICompanyInfoService.Get → CompanyInfo.CompanyDatabaseId`。
- **`common`** = 跨公司共享框架表（`st_session`、`st_cache_notify`）。框架強制
  `DatabaseItem.Id == CategoryId == "common"`。**把業務表掛 common 是錯的。**
- `TableSchema/{categoryId}/` 資料夾名 = CategoryId（seeder 用；form runtime 的 DML 只讀 FormSchema）。

## 欄位可空性：文字與數值欄一律 NOT NULL

沒指定預設值就是空字串或 `0`，**不用 nullable**。理由：DB 內有 NULL，未來手寫 SQL 要處處防 null
（`WHERE col=''` 不匹配 NULL row）。框架在 SQL Server / MySQL / PostgreSQL / SQLite 已內建此機制
（各 `SchemaSyntax.GetDefaultValue`）。**加欄時預設標 `AllowNull=false`，別反射性加 `AllowNull="true"`。**

**加欄 checklist**：

1. 標 `AllowNull=false`。
2. 確認**所有** INSERT（含 `SharedDatabaseState` seed 與測試 helper）都給值。
3. `DbType="Text"` 的欄：MySQL 的 TEXT/BLOB **不能有 DEFAULT**，框架因此不輸出 DEFAULT
   → 每個 hand-written INSERT 必須顯式帶值（`''`）。**不要因為 MySQL 就改成 nullable。**
4. 「常態值為空」的 String 欄若要支援 Oracle：Oracle `''` == `NULL`，
   `VARCHAR2(n) DEFAULT '' NOT NULL` 是自相矛盾的，fresh CREATE 下省略該欄的 INSERT 會 `ORA-01400`。
   在 dialect 修正落地前暫用 `AllowNull="true"` 過渡。
5. **別只靠本機判定**：本機持久容器走 ALTER ADD（會讓欄變 nullable），重現不出 CI fresh CREATE 的行為。

## 數值精度：round-then-sum，且必須由框架顯式 round

**ERP 鐵則：明細加總 = 總合，無前後誤差。** 每筆明細**先**四捨五入到該欄位位數**再**相加；
**禁止**全精度加總後才捨入總合。

- **不捨入類（單價／成本／匯率）**：以輸入精度原樣保存、框架不套捨入（對來源值捨入會把誤差
  注入下游），位數僅供顯示。
- **四捨五入類（數量／重量／金額／百分比）**：寫入時 `AwayFromZero` 捨到該欄位位數；可加總者
  round-then-sum。算金額用單價的完整精度相乘、金額算出再捨。位數由**公司層級**自訂。

**框架不設參數的 Precision/Scale，也不在寫入前 round** —— `DbCommandSpec.CreateCommand` 只設
`Value`/`DbType`/`Size`/`IsNullable`，`DbField.Scale` 只用於 DDL。DB 隱式轉換行為不一致
（4 大 DB 會四捨五入，**SQLite 完全不強制、原樣保留全精度**）。
→ 任何「寫入前要捨到固定 scale」的語意，**必須由 Repository 寫入層顯式
`decimal.Round(value, dbField.Scale, MidpointRounding.AwayFromZero)`**，不可依賴 DB。

## 跨 provider 型別調整走 `NormalizeDbType`

參數層才是跨 provider 精度／型別的實際決定點，且各 driver 行為差異大（Npgsql 對 `DateTimeKind`、
Oracle 對型別各有規則）。**凡 provider-specific 的參數型別改寫，一律在
`DbCommandSpec.NormalizeDbType`（provider-gated）做，不要動全域 `DbTypeMapper.Infer`。**

既有實例：SQL Server-only `DateTime → DateTime2`（拿到 datetime2(7) 的亞毫秒精度與 pre-1753 範圍）、
Oracle `Guid → Binary`。曾嘗試全域改 `Infer`，結果炸掉 PostgreSQL/Oracle 的 seed。
