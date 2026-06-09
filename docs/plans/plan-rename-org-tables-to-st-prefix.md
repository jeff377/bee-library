# 計畫：`ft_department` / `ft_employee` 改名為 `st_` 前綴 + 框架保留命名文件

**狀態：✅ 已完成（2026-06-09）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `ft_department` / `ft_employee` rename 為 `st_*`（XML / C# / docs / 測試） | ✅ 已完成（2026-06-09） |
| 2 | 新增 `docs/framework-reserved-names.md`（含 `.zh-TW.md`）—— 框架保留 namespace registry | ✅ 已完成（2026-06-09） |

> 階段順序：1 → 2。Phase 2 等 rename 完成後寫，可直接列上正確的 `st_department` / `st_employee`，避免文件先寫 `ft_*` 再回頭改。

## 背景

| 項目 | 現況 |
|------|------|
| 表名 | `ft_department` / `ft_employee` |
| 實際所在 DB | company DB（per-tenant） |
| 框架是否依賴 | **是**——`Bee.Definition/Organization/`、`Bee.Definition/Identity/`、`Bee.ObjectCaching/Services/EmployeeContextResolver`、`DepartmentTreeService` 等核心模組都直接依賴這兩張表存在 |
| 前綴語意慣例 | `st_` = 框架/系統層級、`ft_` = 業務層級（**正交於 DB 位置**——`st_role` / `st_role_grant` / `st_user_role` 即已是「住在 company DB 的 st_ 表」前例） |

依現有慣例，框架啟動需要的表應為 `st_` 前綴。`ft_department` / `ft_employee` 前綴與其角色不符，本 plan 將其修正為 `st_department` / `st_employee`。

## Phase 1：表名 rename

### 目標與不變項

#### 改

- DB 表名：`ft_department` → `st_department`、`ft_employee` → `st_employee`
- 所有 XML 中的 `TableName=` / `DbTableName=` 字串、TableSchema 檔名、`DbCategorySettings.xml` 內 `<TableItem>`
- 所有 C# 程式碼內的字串字面值 `"ft_department"` / `"ft_employee"`（共 27 處，含 src / tests / tools / samples）
- 受影響的 in-repo 文件（`docs/*.md`，不含 `docs/archive/`）

#### 不改（重要：守住邏輯名稱不動）

- **FormSchema progId**：`Department` / `Employee` 保留——progId 是邏輯名稱，與 DB 表名解耦
- **C# 類別 / 介面 / 檔名 / 資料夾名**：`EmployeeRepository` / `DepartmentRepository` / `EmployeeContext` / `EmployeeRow` / `DepartmentRow` / `DepartmentNode` / `IEmployeeRepository` / `IDepartmentRepository` / `IEmployeeContextResolver` / `IDepartmentTreeService` / `EmployeeContextResolver` / `DepartmentTreeService` / `Bee.Definition/Organization/` 資料夾 / `Bee.Definition/Identity/` 資料夾——全部不動
- **欄位名**：`dept_id` / `emp_id` 等欄位是欄位名不是表名，不在本 plan 範圍
- **`docs/archive/` 內的歷史 plan**：保留 `ft_*` 原始描述（封存文件，不回頭改寫）

### 變更清單

#### 1. TableSchema XML（檔名 + 內容）

| 原檔 | 新檔 |
|------|------|
| `tests/Define/TableSchema/company/ft_department.TableSchema.xml` | `st_department.TableSchema.xml` |
| `tests/Define/TableSchema/company/ft_employee.TableSchema.xml` | `st_employee.TableSchema.xml` |

檔內 `<TableSchema TableName="ft_xxx" ...>` 同步改為 `"st_xxx"`。

#### 2. FormSchema XML（內容；檔名不動）

| 檔案 | 改動 |
|------|------|
| `tests/Define/FormSchema/Department.FormSchema.xml` | `DbTableName="ft_department"` → `"st_department"` |
| `tests/Define/FormSchema/Employee.FormSchema.xml` | `DbTableName="ft_employee"` → `"st_employee"` |

注意 `FormTable TableName="Employee"` 是 FormSchema 內部 alias，不改。

#### 3. DbCategorySettings

| 檔案 | 改動 |
|------|------|
| `tests/Define/DbCategorySettings.xml` | `<TableItem TableName="ft_(department\|employee)">` → `"st_..."` |

#### 4. C# 字串字面值（27 處）

| 區域 | 檔案 | 處理 |
|------|------|------|
| src | `Bee.Repository/System/DepartmentRepository.cs`、`EmployeeRepository.cs` | `QuoteIdentifier("ft_...")` → `"st_..."` |
| tests | 22 處（`FormSchemaTests` / `DtoSerializationTests` / `TableSchemaGeneratorTests` / `FormTableTests` / `EmployeeBuildSelect(Integration)?Tests` / `MySqlTableAlterCommandBuilderTests` / `Dml/(Insert\|Update\|Delete)CommandBuilderTests` / `(Employee\|Department)RepositoryTests`） | 全部字面值替換 |
| tools | `tools/DefineEditor/Smoke.cs:389` | `TableName = "ft_employee"` → `"st_employee"` |
| samples | `samples/Bee.Samples.Shared/DemoSchemaSeeder.cs:24` `EmployeeTable` const | 維持指向同一邏輯 entity，改為 `"st_employee"`（與框架對齊；見下方「待確認」#1） |

#### 5. 文件（`docs/*.md`，不含 archive）

| 檔案 | 處理 |
|------|------|
| `docs/database-settings-guide.md` / `.zh-TW.md` | 文中 `ft_department` / `ft_employee` 引用全改 |
| `docs/formmap.md` / `.zh-TW.md` | 同上 |
| `docs/permission-authorization.md` / `.zh-TW.md` | 同上 |
| `docs/database-schema-upgrade.md` / `.zh-TW.md` | 同上 |
| `docs/adr/adr-019-permission-authorization-model.md` | 同上 |

ADR-019 為 accepted 狀態的 ADR，依「ADR 不改既往內容、只 supersede」慣例，原則上只更新例子段落（保留結論與決策邏輯）；若涉及核心論述需重寫，改為新增一份 ADR-021 並把 019 標 superseded。**先以單純例子替換處理，review 時再判斷**。

### 待確認問題

#### 1. `samples/Define/TableSchema/common/ft_employee.TableSchema.xml` 怎麼辦？

樣本檔在 **common DB**（非 company），且配套 `ft_employee_phone` 作 master-detail 範例，明顯定位為**「Blazor WASM demo 自己的業務範例表」**而非框架表。

| 選項 | 說明 |
|------|------|
| **a. 保留為 `ft_employee` 不動**（建議） | demo 是消費者視角的業務表，正好示範「框架有 st_employee，消費者也可以有自己的 ft_employee」的並存 |
| b. 也改 `st_employee` 並合併到 company DB | 與框架 `st_employee` 重複，且擾動 demo 既有 master-detail 結構 |
| c. 改名為 `ft_demo_employee` 之類 | 避免名稱與框架混淆，但動到的檔案更多 |

**預設走 a**，除非你有不同想法。

#### 2. `DemoSchemaSeeder.cs` 的 `EmployeeTable` 常數要不要動？

- 它在 `samples/Bee.Samples.Shared/` 是 sample 共用 helper
- 若 #1 走 a（保留 `ft_employee`），這個常數**也應保留為 `ft_employee`**（因為它就是在 seed demo 自己的表，不是 seed 框架的 `st_employee`）
- 若 #1 走 b 或 c，這個常數對應同步調整

> 上面「變更清單 #4」的 `DemoSchemaSeeder` 行為依此決議反向修正——若採 a，那一行**不改**。

#### 3. ADR-019 處理深度？

- **單純替換例子文字**（建議）：ADR 結論未變，只是其中提及的表名是 `st_*`
- **新增 ADR-021 supersedes ADR-019**：太重，本 plan 改的不是決策本體

預設走「單純替換」，若實作時發現論述真的繞著 `ft_*` 前綴語意展開，再升級為新 ADR。

### CHANGELOG

下一版的 CHANGELOG 標 **Breaking Change**：

```
### Breaking Changes

- 框架系統表 `ft_department` / `ft_employee` 改名為 `st_department` / `st_employee`，
  與 `st_role` / `st_user_role` 等其他框架表前綴對齊。已有部署需自行
  `RENAME TABLE` 至新名稱（4 種 dialect 範例見 docs/database-schema-upgrade.md）。
```

`database-schema-upgrade.md` 內補一節 rename 範例（4 dialect）。

## Phase 2：新增 `framework-reserved-names.md` 文件

### 範圍與不變項

#### 做

- 新增 `docs/framework-reserved-names.md` + `docs/framework-reserved-names.zh-TW.md`（雙語、頂部互連）
- 文件定位：**框架 namespace registry** —— 列出框架實際 own 哪些 `st_*` 表與 progId，不重複既有規則 / API reference 內容
- 從 `docs/README.md` / `.zh-TW.md` 加索引條目連到新文件
- 既有文件加交叉連結（見下方「交叉連結」一節）

#### 不做

- 不重新撰寫 SystemBO action 清單（已存在 `api-method-reference.md`，只 link 不複製）
- 不重新撰寫 `st_/ft_` 命名規則（已存在 `database-naming-conventions.md`，只 link 不複製）
- 不寫 `st_*` 表的完整 schema（已存在於對應 `.TableSchema.xml`；新文件只列「表名 + 一句話用途」）

### 新增文件大綱

```markdown
# Framework-Reserved Names

[繁體中文](framework-reserved-names.zh-TW.md)

> Registry of names owned by the bee-library framework.
> Naming **rules** live in [database-naming-conventions.md](...).
> Full API method reference lives in [api-method-reference.md](...).

## 1. System tables (`st_*`)

### 1.1 common database
| Table | Purpose |
|-------|---------|
| st_user           | Global user master |
| st_company        | Company list |
| st_user_company   | User-to-company membership |
| st_session        | Sessions / access tokens |
| st_define         | Definition persistence |
| st_cache_notify   | Cross-node cache invalidation |

### 1.2 company database (per-tenant)
| Table | Purpose |
|-------|---------|
| st_role           | Role definitions |
| st_role_grant     | Role-to-resource grants |
| st_user_role      | User-to-role bindings |
| st_department     | Organizational departments |
| st_employee       | Employees |

## 2. Reserved progIds

### 2.1 System axis
- `System` (formalized as `SysProgIds.System`) — SystemBO endpoint.
  See [api-method-reference.md#axis-system](...) for the full action list.

### 2.2 Framework-shipped forms
| progId | Backing table |
|--------|---------------|
| Department | st_department |
| Employee   | st_employee   |

See [api-method-reference.md#axis-form](...) for the standard form actions
inherited by every progId.

## 3. Consumer guidelines
- Use `ft_` for your business tables; never `st_`.
- Avoid `System` / `Department` / `Employee` as your own progIds.
- To extend an `st_*` table (e.g. add a column to `st_employee`):
  drop a same-named `.TableSchema.xml` in your `DefinePath` to fully override.
```

繁中版 1:1 對應翻譯。

### 交叉連結與既有文件調整

| 既有文件 | 改動 |
|---------|------|
| `docs/README.md` / `.zh-TW.md` | 索引加一條 `framework-reserved-names.md` |
| `docs/database-naming-conventions.md` / `.zh-TW.md` | 末段加一行 "see framework-reserved-names.md for the actual list of `st_*` tables" |
| `docs/api-method-reference.md` / `.zh-TW.md` | 開頭加一行 "see framework-reserved-names.md for reserved progIds" |
| `docs/database-settings-guide.md` / `.zh-TW.md` | 若內文列出具體 `st_*` 表，改 link 過去（避免雙清單漂移）—— Phase 1 修這份檔時順帶處理 |

### 待確認問題

#### 1. progId 是否在程式碼中常數化？

目前只有 `SysProgIds.System` 一條常數。`Department` / `Employee` 散在 FormSchema 檔名中無 C# 常數。

| 選項 | 說明 |
|------|------|
| **a. 維持現狀**（建議） | progId 是「定義檔識別字」，本來就以字串自由命名；強行常數化會造成框架程式碼對特定 progId 硬編碼（反耦合方向） |
| b. 加 `SysProgIds.Department` / `Employee` 常數 | 文件可 link 到常數定義；但框架 src 不該對這些 progId 寫死邏輯 |

**預設走 a**，文件直接列字串即可。

#### 2. 文件擺放位置？

- **`docs/framework-reserved-names.md`**（建議，與既有 docs 同層）
- `docs/reference/framework-reserved-names.md`（新建 reference 子目錄）—— bee-library 目前 docs 是平鋪結構，不為單一檔開子目錄

預設走平鋪。

## 驗證計畫（跨階段）

### Phase 1

1. **靜態檢查**：
   - 改完後跑 `grep -rE 'ft_(department|employee)' --include='*.cs' --include='*.xml' --include='*.md' .` ——除 `docs/archive/` 外應為 0 hit
   - `dotnet build --configuration Release` 過綠（strict warnings）
2. **測試**：
   - `./test.sh` 全綠（含 `[DbFact]` 在 4 種 dialect 上）
   - 重點測試專案：`Bee.Repository.UnitTests` / `Bee.Definition.UnitTests` / `Bee.Db.UnitTests` / `Bee.Business.UnitTests`
3. **Smoke**：
   - `tools/DefineEditor/Smoke.cs` 跑過——這支是用 inline `TableSchema { TableName = "..." }` 建表，要確認改完後 Smoke 仍跑得通
4. **Sample 啟動**（針對 Phase 1 待確認 #1 決議的選項作對應檢查）：
   - 若採 a：Blazor WASM demo 仍能跑（用自己的 `ft_employee`）
   - 若採 b/c：對應 demo 跑得通

### Phase 2

1. **連結檢查**：新文件內所有相對連結（到 `api-method-reference.md` 章節錨點、`database-naming-conventions.md` 等）可點通
2. **雙語對照**：英中兩版列出的 11 張表、2 個 framework progId、3 條 consumer guideline 完全一致
3. **既有文件未壞**：`database-naming-conventions.md` / `api-method-reference.md` / `database-settings-guide.md` / `docs/README.md` 加 link 後仍能正常閱讀

## Out of scope

- 把系統表搬進 `src/Bee.Definition/Defaults/` + `<EmbeddedResource>` 的 packaging 設計（前一輪討論的「框架預設 defines」）——**另起 plan，本 plan 完成後再做**
- progId / C# 類別 / 欄位名變動
- `ft_project` 改名（它是業務範例，正確使用 `ft_` 前綴）
- `ft_employee_phone`（樣本內的擴充示範，正確使用 `ft_` 前綴；依 Phase 1 待確認 #1 決議連動）
- 新增 `SysProgIds.Department` / `SysProgIds.Employee` 常數（見 Phase 2 待確認 #1）

## 風險

| 風險 | 緩解 |
|------|------|
| Repository 的 SQL 字串拼錯導致 runtime 才發現 | 27 個字面值替換後跑 `[DbFact]` 4 dialect 確認 |
| 既有部署如有 prod 資料表會跑掉 | CHANGELOG 標 breaking + 提供 rename DDL 範例；目前 repo 尚未發行 v1.0，無外部消費者，影響可控 |
| ADR-019 改動過頭破壞歷史脈絡 | 只替換例子文字，論述段落不動；若發現需重寫即升級為新 ADR |
| `framework-reserved-names` 與既有文件清單漂移（同一 `st_*` 表在兩份文件出現） | Phase 2 把既有文件中具體列出 `st_*` 的位置改為 link，本檔成為唯一 registry |
