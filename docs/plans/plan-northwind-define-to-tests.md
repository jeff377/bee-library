# 計畫：將 Bee.Northwind/Define 引入 tests/Define 作為框架單元測試案例

**狀態：📝 待執行（2026-06-15，決策已定，待使用者下令開工）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 新增 6 業務實體 FormSchema/FormLayout/TableSchema + DbCategorySettings 追加 ft_* | 📝 待做 |
| 2 | 升級重疊實體 Employee/Department/st_employee/st_department 為 Northwind 超集（D1） | 📝 待做 |
| 3 | 產 6 實體雙語 Language（D2） | 📝 待做 |
| 4 | 擴充 SharedDatabaseState 加 Northwind seed（D3） | 📝 待做 |

## 目標

`apps/Bee.Northwind/Define` 的定義比 `tests/Define` 完整（master-detail 單據 Order、
多重 lookup、ReadOnly 計算欄、DropDownEdit、RelationField 等真實 ERP 結構）。希望把這份
較完整的定義引入 `tests/Define`，讓框架單元測試能涵蓋更豐富的案例。

本文件**只評估可行性與範圍**，不執行；待使用者選定策略後再動工。

## 可行性結論（先講重點）

**「把 Bee.Northwind/Define 原封不動整份覆蓋 tests/Define」不可行** —— 會破壞既有測試套件。
原因見下方「約束」。**建議改採加法式合併（additive superset）**：保留 tests/Define 既有的
框架系統定義，疊加 Northwind 獨有的業務實體，並把重疊實體升級為 Northwind 的超集版本。

## 兩份 Define 的差集（盤點結果）

### 只在 tests/Define（測試必需、Northwind 沒有）

| 類別 | 檔案 |
|------|------|
| FormSchema | `PermGateForm`、`Project` |
| FormLayout | `Project` |
| Language（全部） | `zh-TW` / `en-US` 的 `Department`、`Employee`、`Project`（Northwind 無任何 Language 檔） |
| TableSchema/common | `st_user`、`st_session`、`st_company`、`st_user_company`、`st_define`（框架系統表） |
| TableSchema/company | `ft_project`、`st_role`、`st_role_grant`、`st_user_role` |

### 只在 Northwind/Define（要引入的「比較完整」部分）

| 類別 | 檔案 |
|------|------|
| FormSchema / FormLayout | `Category`、`Customer`、`Supplier`、`Shipper`、`Product`、`Order` |
| TableSchema/company | `ft_category`、`ft_customer`、`ft_supplier`、`ft_shipper`、`ft_product`、`ft_order`、`ft_order_detail` |
| 其他 | `ProgramSettings.xml`（tests 無） |

### 兩邊都有但**內容不同**（重疊衝突）

| 檔案 | 差異 | 風險 |
|------|------|------|
| `FormSchema/Employee` | Northwind 是**嚴格超集**：關聯結構（`dept_rowid`→Department、含 supervisor 鏈式 mapping）完全相同，額外加 `title`/`hire_date`、英文 caption、ListFields/LookupFields | 低（超集） |
| `FormSchema/Department` | 同上，關聯結構相同，caption/ListFields 改英文 | 低 |
| `TableSchema/st_employee` | Northwind 多 `title`(String)、`hire_date`(Date) 兩欄 | **中**（見風險 R2） |
| `TableSchema/st_department` | 欄位集合相同，僅 caption 改英文 | 低 |
| `FormLayout/Employee`、`FormLayout/Department` | Northwind 用 ButtonEdit + DisplayFields、英文 caption | 低 |
| `DbCategorySettings.xml` | tests 有 common/company/log 三類 + 框架系統表；Northwind 只有 company 類、無框架表 | **高**（見約束 C1） |
| `DatabaseSettings.xml` | tests **刻意留空**（連線走環境變數）；Northwind 寫死 SQLite | **高**（見約束 C2） |
| `SystemSettings.xml` | 完全相同 | 無 |
| `TableSchema/common/st_cache_notify` | 完全相同 | 無 |

## 測試載入機制的硬約束（決定可行範圍）

> 來源：`tests/Bee.Tests.Shared/`（`TestProcessBootstrap`、`BeeTestFixtureBuilder`、
> `SharedDatabaseState`、`TestDbConventions`）。

- **C1 — `DbCategorySettings.xml` 同時驅動「建表」與「seed」**：`SharedDatabaseState.EnsureSchemaAndSeed`
  逐一讀 DbCategorySettings 的每個 category／table 去建 schema；接著 seed `st_user` / `st_company` /
  `st_user_company`（登入測試的種子資料）。**若改用 Northwind 版（無 common 類、無框架系統表），
  所有 `[DbFact]` 整合測試的 seed 會直接失敗。** → `common` 類與框架系統表**必須保留**。
- **C2 — `DatabaseSettings.xml` 必須維持空**：測試連線由 `.runsettings` 的 `BEE_TEST_CONNSTR_*`
  + `SharedDatabaseState.EnsureRegistered`（依 DbCategorySettings 的 category 動態組 DatabaseItem）
  決定，**不讀 DatabaseSettings.xml**。Northwind 那份寫死 SQLite 會破壞多 DB 環境變數機制。 → **不可覆蓋**。
- **C3 — DefinePath 寫死指向 `tests/Define`**：`TestProcessBootstrap.CreateSharedDefinePath` 把
  `repoRoot/tests/Define` 複製到 temp 再 materialize 框架 defaults（skip-existing）。所以
  **凡放進 tests/Define 的檔案都會被全程式載入**，新增的 FormSchema/TableSchema 會自動生效。
- **C4 — `DefaultsTests` 斷言的是「embedded 資源」不是 tests/Define**：`DefaultsTests.cs:85`
  讀的是 `Bee.Definition` 內嵌的精簡 DbCategorySettings（禁止 `ft_project`）。**改 tests/Define/DbCategorySettings
  不會踩到這條** —— 在 tests/Define 的 company 類加 `ft_*` 是安全的。

## 既有測試對重疊實體的依賴（覆蓋風險來源）

- `Bee.Db.UnitTests/EmployeeBuildSelect*Tests`：強依賴 Employee/Department 的**關聯結構**
  （`dept_rowid`→Department、`ref_dept_name`、`ref_supervisor_name` 鏈式 JOIN）。Northwind 版
  **完整保留**此結構 → 升級可行。
- `Bee.Repository.UnitTests/{Employee,Department}RepositoryTests`、
  `Bee.Business.UnitTests/SystemBusinessObjectEnterCompanyTests.cs:96`：以**明確欄位清單** INSERT
  `st_employee`（`sys_rowid,sys_id,sys_name,dept_rowid,user_rowid`），未含 `title`/`hire_date`。
- `FormBusinessObjectPermissionGateTests`：依賴 `PermGateForm` 的 `PermissionModelId="PermGateModel"`。
- 全測試套件**無任何**載入 Northwind ProgId（Customer/Order/...）的 fixture 依賴 —— 新增為純加法。

## 建議策略：加法式合併（superset merge）

保留 tests/Define 一切既有檔案，疊加 Northwind 內容；重疊實體升級為 Northwind 超集版。

### 動作清單

| # | 動作 | 檔案 | 備註 |
|---|------|------|------|
| 1 | **新增** 6 個業務 FormSchema | `Category/Customer/Supplier/Shipper/Product/Order` | 直接複製 |
| 2 | **新增** 6 個 FormLayout | 同上 | 直接複製 |
| 3 | **新增** 7 張 TableSchema/company | `ft_category/ft_customer/ft_supplier/ft_shipper/ft_product/ft_order/ft_order_detail` | 直接複製 |
| 4 | **升級** 重疊實體為超集 | `Employee/Department`（FormSchema+FormLayout）、`st_employee/st_department`（TableSchema） | 視決策 D1 |
| 5 | **合併** DbCategorySettings | 在既有 company 類**追加** `ft_*` 7 張表（保留 common/log 與所有框架表） | 不踩 C4 |
| 6 | **不動** | `DatabaseSettings.xml`（維持空）、common 類框架表、Project/PermGateForm、Language 既有檔 | C1/C2 |
| 7 | **不複製** | Northwind `ProgramSettings.xml` | 綁 `OrderBO` 型別，測試行程內無該組件，原樣載入會型別解析失敗（見 R3） |
| 8 | **選配** 產生新實體 Language | `zh-TW`/`en-US` × 6 實體 | 視決策 D2，可用 `bee-scaffold-from-formschema` |

### Order 進 tests/Define 的語意

Order 的自訂邏輯（金額計算、狀態轉移）在 `Bee.Northwind.Server.OrderBO`，**不在測試行程內**。
進 tests/Define 後，框架以**預設 FormBusinessObject** 解析 Order = 純定義驅動 CRUD。對「測試框架本身」
（schema 解析、master-detail 持久化、lookup JOIN、FormLayout 產生）這正是想要的；不涉及 OrderBO 的業務規則。

## 風險

- **R1（C1/C2 已述）**：誤覆蓋 DbCategorySettings／DatabaseSettings 會全面打掛 `[DbFact]`。動作 5/6 明確規避。
- **R2 — st_employee 加欄位**：`title`/`hire_date` 進 st_employee 後，既有「明確欄位清單 INSERT」
  （`SystemBusinessObjectEnterCompanyTests.cs:96` 等）若新欄位為 NOT NULL 且無 DB 預設值 → INSERT 失敗。
  **緩解**：確認框架 `TableSchemaBuilder` 對未標 AllowNull 的 String/Date 預設可空，或在 schema 標
  `AllowNull`／給預設值；執行階段以實機跑 `Bee.Business`/`Bee.Repository` 測試驗證。
- **R3 — ProgramSettings.xml**：含 `BusinessObject="Bee.Northwind.Server...OrderBO"`。若複製且有測試
  讀 ProgramSettings 解析 BO 型別會失敗。動作 7 直接不複製（或複製去掉 OrderBO 綁定的精簡版）。
- **R4 — 重疊升級的連帶**：D1 選「升級」時，Employee/Department 的 caption 由中文轉英文。已查無測試
  斷言這些中文 caption（皆為資料、非斷言），風險低；仍以全套件實跑為準。

## 已定決策（2026-06-15 使用者確認）

- **D1 = 升級為 Northwind 超集版**：Employee/Department/st_employee/st_department 升級為 Northwind 版。
  需處理 R2（st_employee 加欄位的 NOT-NULL INSERT 風險）。
- **D2 = 產 zh-TW + en-US**：為 6 個新實體（Category/Customer/Supplier/Shipper/Product/Order）產雙語
  Language，用 `bee-scaffold-from-formschema`。Order 為 master-detail，含明細 table 的 sub-key。
- **D3 = 加 Northwind seed**：擴充 `SharedDatabaseState` 為新 `ft_*` 表寫種子資料。
  - **種子來源**：`apps/Bee.Northwind/Bee.Northwind.Server/SeedData/*.json`（Category/Customer/Supplier/
    Shipper/Product/Order/OrderDetail/Department/Employee）。需評估是否直接搬 JSON 進
    `tests/Bee.Tests.Shared/`（或嵌入資源）+ 仿 `NorthwindSchemaSeeder.InsertRows` 的 deferred-relation 邏輯。
  - **擴充點**：`SharedDatabaseState.EnsureSeedData`（目前只 seed st_user/st_company/st_user_company）
    加一段 Northwind 業務表 seed；需處理 FK 關連（sys_id → sys_rowid 對應）與 `[DbFact]` 既有測試的
    資料量假設（**風險 R5**：既有測試若 `SELECT COUNT(*)` 斷言空表，新 seed 會破壞）。
  - **冪等**：仿 Server 端 `InsertRows`「COUNT>0 則 skip」，避免重複 seed。

## 執行階段（待決策後）

| 階段 | 範圍 | 驗證 |
|------|------|------|
| 1 | 動作 1-3、5、7（純新增 + DbCategorySettings 追加） | `dotnet build` + 全套件 `./test.sh`，確認既有測試全綠、新 schema 可載入 |
| 2 | 動作 4（依 D1 決定是否升級重疊實體） | 重點跑 `Bee.Db`/`Bee.Repository`/`Bee.Business`，驗 R2/R4 |
| 3 | 動作 8（依 D2 產 Language）+ 視 D3 擴 seed | 跑 localization / 整合測試 |

## 不在範圍

- 不改測試 fixture 基礎建設（除非 D3 選 (b) 才動 SharedDatabaseState）
- 不引入 OrderBO 業務邏輯到測試（Order 在 tests 為純定義 CRUD）
- 不動 apps/Bee.Northwind（單向：Northwind → tests，來源不變）
