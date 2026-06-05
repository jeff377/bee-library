# 計畫：ERP 權限機制（層二 — record-scope enforcement）

**狀態：✅ 已完成（2026-06-05）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | user↔employee 連結（`ft_employee.user_rowid`）+ 「user→部門」解析 + EnterCompany 快照進 SessionInfo | ✅ 已完成（2026-06-05） |
| 2 | grant per-action scope（重構 `st_role_grant`）+ `ScopeResolver`（具名策略→FilterNode + 逐列判定）+ 多角色合併 | ✅ 已完成（2026-06-05） |
| 3 | 接入 `FormBusinessObject`：讀取（GetList/GetData）+ 寫入（Update/Delete 權威 re-query）套 scope | ✅ 已完成（2026-06-05） |

> **Phase 3 完成註記（2026-06-05）**：讀寫兩端皆套 record scope。
>
> **讀取端**：`GetList` 把 `ResolveFilter` 結果與呼叫端 filter 經 `FilterGroup.All` AND 併（`CombineWithScope`）；`GetData` 把 scope filter 傳 `repository.GetData(rowId, scopeFilter)`（越範圍→`null`，與「查無」不可區分）。
>
> **寫入端（Update / Delete）—— 權威 re-query（後端控管安全邊界）**：經 review 定案，寫入端對「既存記錄的異動」套 scope，且用**權威 re-query** 而非評估 client 送來的列值（避免偽造 payload relabel 繞過）。`IDataFormRepository` 加 `ExistsInScope(rowId, scopeFilter)`（對 DB 下 `sys_rowid = id AND scope` 的存在性查詢）。`FormBusinessObject.EnforceWriteScope`：Save 對每個 master 列 `Modified→Update`/`Deleted→Delete`，以 target rowId 經 `ExistsInScope` 確認在範圍內，否則 `ForbiddenException`。`Delete(rowId)` → `repository.Delete(rowId, scopeFilter)`：先 `ExistsInScope`，越範圍 → 刪 0、不 cascade（與「查無」不可區分）。
>
> **Create 不套**：`Added` 列由 Create 動作授權把關（新列無「既存範圍」可違反；檢查新列 owner/dept 會造成摩擦）。
>
> **`IsRowInScope` 已移除**：原規劃的記憶體逐列檢查可被偽造的 payload 繞過，改用權威 re-query 後不需要、且移除以免成為「看似可用卻不安全」的陷阱。
>
> 空 `PermissionModelId` / 無限制 scope → 不套（向後相容）；`GetNewData` 不過濾。
>
> 測試：`FormBusinessObjectGetListTests` 加 In-filter（scope 形狀）GetList（SQLite+SQL Server，驗 WhereBuilder remap）+ GetData scope + Delete/ExistsInScope 權威 re-query（SQLite+SQL Server）整合測試；`FormBusinessObjectPermissionGateTests` 加 Save Modified 越範圍→`ForbiddenException` / 在範圍→放行；`ScopeResolverTests` 移除 4 個 IsRowInScope 測試。既有 Form BO 測試（含 gate）全綠。

> **Phase 2 完成註記（2026-06-05）**：`st_role_grant` 由 `(role_id, model_id, allowed_actions[mask])` 重構為 **per-(role, model, action) 一列帶 `scope`**（UK 改 `role_id+model_id+action`）；`RoleGrantRow` → `(RoleId, ModelId, Action, Scope)`；`CompanyRolePermissions.GetAllowed` 改以 action presence OR 出 mask（layer-1 不變）+ 新增 `GetEffectiveScopes`；`RolePermissionRepository` SQL 對應。`FormTable.GetOwnerField()/GetDeptField()` helper。`IScopeResolver` + `ScopeResolver`（Bee.Business.Permission）：多角色 effective scope（Inherit→model 預設）→ 任一 All 不過濾 / 否則 OR 聯集；`Own` = owner 欄 `IN {UserRowId, EmployeeRowId}`、`Dept`/`DeptAndSub` 隱含 Own；空 In 清單天然 `1=0`（deny / 空身分 / 空子樹）；fail-closed 邊界。讀取端 `ResolveFilter→FilterNode?`、寫入端逐列 `IsRowInScope`。DI 註冊於 Bee.Hosting。
>
> 測試：`ScopeResolver` 14（各策略、多角色合併、Inherit、Owner 二身分、隱含 Own、fail-closed、逐列）、`CompanyRolePermissions` +4（GetEffectiveScopes）、`RolePermissionRepository` 5 DB（action+scope round-trip）。line-b 既有測試（Authorization/CompanyRolePermissions）全數對齊綠。

> **Phase 1 完成註記（2026-06-05）**：新增 `DepartmentRow` 同類的 `EmployeeRow`（flat 載體）、`EmployeeContext` + `IEmployeeContextResolver`（user→employee→dept 解析）、`IUserRepository`（common 取 rowid）/`IEmployeeRepository`（company 取 employee）+ 工廠/DI；`SessionInfo` 加 `UserRowId`/`EmployeeRowId`/`DeptRowId`（記憶體快照，比照 `Roles`，不持久化）；`EnterCompany` 解析快照、`LeaveCompany`/`Logout` 經 `ClearCompanyContext` 清除；`ft_employee` 加 `user_rowid`。
>
> ⚠️ **發現的既有框架缺陷（與本 plan 正交，待決）**：MySQL 對**既有表** `ALTER TABLE ADD COLUMN <guid> NOT NULL DEFAULT (UUID())` 在 statement-binlog 下被視為 replication-unsafe → 報錯，導致 schema 升級整段跳過。只影響「既有 MySQL 表新增 Guid NOT NULL 欄」；**fresh CREATE 安全**（故 CI 全新容器、本機 drop 重建均正常）。任何未來在既有 MySQL 表加 Guid 欄都會中。修法另議（MySQL dialect：ALTER ADD 改用常數預設或 nullable→backfill→NOT NULL 三段）。

## 承接與範圍

這是整套 ERP 權限的**最後一塊**,承接：
- [plan-erp-permission.md](plan-erp-permission.md)（線 A 定義層,✅）：`PermissionModels` registry、`ScopeStrategy`、`ScopeRole`、`FormField.ScopeRole`、`PermissionRule(Action+Scope)`。
- [plan-permission-line-b.md](plan-permission-line-b.md)（層一 enforcement,✅）：per-company 權限快取 + `AuthorizationService.Can(model, action)` + `FormBusinessObject` 方法層 gate。
- [plan-org-department-tree.md](plan-org-department-tree.md)（組織部門樹,✅）：`DepartmentTree` 三棲 + per-company 快取 + `IDepartmentTreeService.Get(companyId)`，提供 `GetSelfAndDescendants(deptRowId)`。

本計畫把 `ScopeStrategy` 的 **Own / Dept / DeptAndSub** 真正接進資料查詢與寫入,讓越範圍的資料**讀取被過濾、寫入被擋**。

> 一句話：層一做完「採購員不能刪訂單」（model+action gate）;本計畫做完「採購員只能看／改自己部門的訂單」（record scope）。

## 已拍板決策（本次 review 確認）

| # | 決策 | 選定 |
|---|------|------|
| D1 | **user↔employee 對應** | `ft_employee` 加 `user_rowid`（Guid，邏輯指向 common `st_user.sys_rowid`），per-company 一對一。比照 `st_user_company` 的跨 DB 邏輯參照。 |
| D2 | **「user→部門」解析時機** | `EnterCompany` 一次性解析 `user→employee→dept`，快照進 `SessionInfo`（比照 `Roles` 快照）。查詢時零 DB。 |
| D3 | **GetData / Delete 定點守衛** | scope filter 併進 WHERE（`rowId AND scope`）。越範圍 → 查不到（回 `null`）/ 刪 0 列。與 GetList 同源、單一過濾邏輯。 |
| D4 | **Owner 欄語意（二種身分）** | `Own` = owner 欄 `IN {UserRowId, EmployeeRowId}`。欄位存 user rowid 配 `UserRowId`、存 employee rowid 配 `EmployeeRowId`（Guid 不碰撞，單一策略涵蓋兩種；user 無 employee 對應時集合只含 `UserRowId`）。 |
| D5 | **scope 來源 = per-action** | 重構 `st_role_grant` 為 per-(role, model, action) 一列帶 scope。理由：「可以看不一定可以改、範圍不同」（Read scope 可為 Dept、Update scope 可為 Own）。`PermissionModel.Rules[action].Scope` 退為「grant scope = Inherit 時的預設」。 |

## 核心資料流（端到端）

```
登入            → SessionInfo.UserId (sys_id 字串)                          ← 既有
EnterCompany    → 解析 user→employee→dept，快照三個 Guid 進 SessionInfo：     ← Phase 1（新）
                    UserRowId      (st_user.sys_rowid，common DB)
                    EmployeeRowId  (ft_employee.sys_rowid，company DB；無對應=Empty)
                    DeptRowId      (ft_employee.dept_rowid；無對應=Empty)
                  同時快照 Roles（既有 line-b）

查詢/寫入時（零 DB，全走 session 快照 + 記憶體快取）：
  ScopeResolver(modelId, action, session, formSchema)                        ← Phase 2（新）
    1. 多角色取 effective scope：對每個 grant 該 (model, action) 的 scope，
       Inherit → 取 PermissionModel.Rules[action].Scope（model 預設）
    2. 合併：任一 All → 不過濾（回 null）；否則各角色 predicate 取 OR 聯集
    3. 單一 scope → predicate：
         Own        : ownerField IN {UserRowId, EmployeeRowId}
         Dept       : deptField = DeptRowId            OR Own        （隱含 Own）
         DeptAndSub : deptField IN GetSelfAndDescendants(DeptRowId) OR Own  （隱含 Own）
    → FilterNode（讀取端） / 逐列 bool 判定（寫入端）

  FormBusinessObject 接入：                                                  ← Phase 3（新）
    GetList/GetData/GetNewData → Read filter AND args.Filter
    Save                       → 逐列依 RowState 取 write action，逐列判 scope
    Delete                     → Delete filter 併進 rowId WHERE
```

## Phase 1 — user↔employee 連結 + 部門解析快照

### 1a. `ft_employee` 加 `user_rowid`

| 欄位 | 型別 | 說明 |
|------|------|------|
| （既有） | | `sys_no` / `sys_rowid` / `sys_id`（員編） / `sys_name` / `dept_rowid` |
| `user_rowid` | Guid | 邏輯指向 common `st_user.sys_rowid`；`Guid.Empty` = 未綁定帳號 |

- company-category 表（各 company DB）。比照既有 `dept_rowid` / `manager_rowid`：Guid NOT NULL、`Guid.Empty` 表「無」。
- TableSchema fixture（`tests/Define/TableSchema/company/ft_employee.TableSchema.xml`）+ seed 補 `user_rowid`（seed 把 demo user `001` 綁到一個 demo employee + 部門，供 Phase 3 整合測試）。
- 5 DB fresh CREATE 驗證（注意 Oracle `''`=NULL 雷不適用——此欄 Guid NOT NULL 預設 `Guid.Empty`，非字串空值，見 [[tableschema-addcolumn-allownull]]）。

### 1b. 「user→employee→dept」解析

新增解析鏈（跨 common + company DB，仿 `UserCompanyRepository.HasAccess` 的跨 DB 先例）：

```
EmployeeContext { UserRowId, EmployeeRowId, DeptRowId }

resolve(userId[sys_id], companyId):
  UserRowId  = st_user(common).sys_rowid WHERE sys_id = userId            // common DB
  若 UserRowId = Empty → 回 { Empty, Empty, Empty }
  employee   = ft_employee(company) WHERE user_rowid = UserRowId          // company DB（經 ICompanyInfoService 解析 databaseId）
  若 無 → EmployeeRowId = Empty, DeptRowId = Empty（user 未綁員工，合法）
  否則 → EmployeeRowId = employee.sys_rowid, DeptRowId = employee.dept_rowid
```

- Repository：`IUserRepository.GetRowIdBySysId(userId)`（common；若已有則複用）+ `IEmployeeRepository.GetByUserRowId(databaseId, userRowId)`（company scope，比照 `IDepartmentRepository`／`RolePermissionRepository` 的 `ISystemRepositoryFactory` 模式）。
- 解析器：`IEmployeeContextResolver.Resolve(userId, companyId)`（或直接放在 `SystemBusinessObject.EnterCompany` 內以兩個 repository 組裝；傾向抽 resolver 以便純單元測試）。
- **不需快取**：只在 `EnterCompany` 呼叫一次（比照 `HasAccess`／roles 快照都在 enter 時碰 DB），查詢時走 session 快照零 DB（D2）。

### 1c. `SessionInfo` 加三個 Guid + EnterCompany 快照

`SessionInfo` 新增（皆 Guid，預設 `Guid.Empty`）：

| 屬性 | 來源 | 說明 |
|------|------|------|
| `UserRowId` | `st_user.sys_rowid` | 當前 user 的 rowid（Own 比對用） |
| `EmployeeRowId` | `ft_employee.sys_rowid` | 當前 user 對應員工 rowid（Own 比對用；Empty=無對應） |
| `DeptRowId` | `ft_employee.dept_rowid` | 當前 user 所屬部門（Dept/DeptAndSub 用；Empty=無） |

- `EnterCompany`：在既有「快照 Roles」後段，呼叫 resolver 填入三個 Guid（同一處、同一次 enter）。
- `LeaveCompany` / `Logout`：清為 `Guid.Empty`（與 `CompanyId` / `Roles` 同步）。
- **序列化**：三個 Guid 屬性納入 `st_session` 序列化（確認 SessionInfo 序列化器涵蓋新欄位）。

### Phase 1 測試
- `ft_employee.user_rowid` 5 DB fresh CREATE + insert/select round-trip（`[DbFact]`）。
- `EmployeeContextResolver`：fake repository——有對應員工、無對應員工（Empty）、未知 user（Empty）、無部門員工（DeptRowId Empty）。
- `EnterCompany` 快照：enter 後 SessionInfo 三 Guid 正確；leave 後清空。
- SessionInfo 三棲/序列化 round-trip 保留新欄位。

## Phase 2 — grant per-action scope + ScopeResolver

### 2a. 重構 `st_role_grant`：mask → per-action scope（D5）

line-b 現況：`st_role_grant(role_rowid, model_id, allowed_actions[int mask])`，一列涵蓋多 action。
本計畫改為 **per-(role, model, action) 一列帶 scope**：

| 表（各 company DB） | 欄位 | 說明 |
|---|---|---|
| `st_role_grant` | `role_rowid`, `model_id`, `action`(int 單一 `PermissionAction`), `scope`(int `ScopeStrategy`) | 一列 = 角色對某 model 某 action 的授權 + scope |

- **line-1 allowed 判定**：某 (role, model, action) 有列 = 允許（presence-based，取代 mask `HasFlag`）。
- **line-2 scope**：列的 `scope`；`Inherit` → 取 `PermissionModel.Rules[action].Scope`（model 預設）。
- **影響 line-b 既有碼（pre-release，可重構）**：
  - `RoleGrantRow` record：`(RoleId, ModelId, Action, Scope)`（從 `(RoleId, ModelId, AllowedActions)`）。
  - `RolePermissionRepository` SQL：`SELECT role_id, model_id, action, scope`。
  - `CompanyRolePermissions`：`GetAllowed(roles, modelId)` 改為「列出有列的 action OR 成 mask」（line-1 不變語意）；新增 `GetEffectiveScopes(roles, modelId, action)` → `IReadOnlyList<ScopeStrategy>`（多角色、已解 Inherit）。
  - seed（`SharedDatabaseState` / `DemoSchemaSeeder`）：把既有 mask 列展開為 per-action 列。
  - line-b 既有測試（`CompanyRolePermissions` / repository / 方法層 gate）對應調整。

### 2b. FormSchema/FormTable scope 欄 helper

`FormTable`（或 `FormSchema`）加 helper，找主表上標 `ScopeRole` 的欄：

```csharp
public FormField? GetOwnerField();   // ScopeRole == Owner（至多一個，PermissionBindingValidator 已限制）
public FormField? GetDeptField();    // ScopeRole == Dept
```

### 2c. `IScopeResolver`

```csharp
// 介面放 Bee.Definition.Identity（比照 IAuthorizationService）；實作放 Bee.ObjectCaching/Services。
public interface IScopeResolver
{
    // 讀取端：產生 record-scope FilterNode；null = 不過濾（All 或無 scope）。
    FilterNode? ResolveFilter(Guid accessToken, string modelId, PermissionAction action, FormSchema formSchema);

    // 寫入端逐列：該列是否落在 (model, action) 的有效 scope 內。
    bool IsRowInScope(Guid accessToken, string modelId, PermissionAction action, FormSchema formSchema, DataRow row);
}
```

依賴注入：`ISessionInfoService`（取快照三 Guid + Roles）、`IRolePermissionService`（`GetEffectiveScopes`）、`IDefineAccess`（`PermissionModels` 取 model 預設）、`IDepartmentTreeService`（DeptAndSub 展開）。

**合併規則（D5 + 任務描述）**：
1. `GetEffectiveScopes(roles, modelId, action)` → 各角色 effective scope（Inherit 已解為 model 預設）。
2. 任一 = `All` → 回 `null`（不過濾）/ 逐列恆 true。
3. 否則各 scope 建 predicate，OR 聯集（`FilterGroup.Any`）。

**單一 scope → predicate**（`ownerField` = `GetOwnerField().FieldName`，`deptField` = `GetDeptField().FieldName`）：

| scope | FilterNode（讀取） | 逐列判定（寫入） |
|-------|-------------------|----------------|
| `Own` | `ownerField IN {UserRowId, EmployeeRowId}`（去除 Empty） | `row[ownerField] ∈ {UserRowId, EmployeeRowId}` |
| `Dept` | `(deptField = DeptRowId) OR Own` | `row[deptField] == DeptRowId` 或 Own |
| `DeptAndSub` | `deptField IN GetSelfAndDescendants(DeptRowId) OR Own` | `row[deptField] ∈ subtree` 或 Own |

- **隱含 Own**：Dept/DeptAndSub 一律 OR 上 Own predicate（你永遠看得到自己擁有的列，即使被標到別部門）。
- **邊界**：scope 需要的欄缺失（無 `ScopeRole=Owner`/`Dept` 欄）或對應身分 Empty（user 無 employee/無部門）→ 該 predicate 退化為「不匹配任何列」（fail-closed，安全預設），並記一筆 warning log（避免靜默全擋難 debug）。

### Phase 2 測試
- `CompanyRolePermissions.GetEffectiveScopes`：單角色、多角色（不同 scope）、Inherit 解 model 預設、未授予 action。
- `ScopeResolver.ResolveFilter`（純單元，合成 session + grants + FormSchema + DepartmentTree）：
  - 各 strategy（Own / Dept / DeptAndSub）FilterNode 結構正確。
  - 多角色：任一 All → null；否則 OR 聯集。
  - 隱含 Own（Dept/DeptAndSub 含 Own 分支）。
  - Owner 二種身分：欄值 = UserRowId / EmployeeRowId 各命中。
  - 邊界：無 owner/dept 欄、user 無 employee（EmployeeRowId Empty）、user 無部門（DeptRowId Empty）→ fail-closed。
- `IsRowInScope`：逐列各 strategy 命中/不命中。

## Phase 3 — 接入 FormBusinessObject

接入點（[FormBusinessObject.cs](../../src/Bee.Business/Form/FormBusinessObject.cs)，`Authorize`/`AuthorizeSave` 後、`repository.*` 前）：

| 方法 | 接入 |
|------|------|
| `GetList` | `scopeFilter = resolver.ResolveFilter(token, model, Read, schema)`；非 null → `FilterGroup.All(scopeFilter, args.Filter)`（去 null）傳 repository。 |
| `GetNewData` | 不過濾（新列尚無資料；建立權限由 line-1 + Save 逐列把關）。 |
| `GetData` | `scopeFilter` 併進 `repository.GetData(rowId, scopeFilter)`；越範圍 → 回 `null`（D3）。 |
| `Save` | 逐列：依 `RowState` 取 action（Added→Create / Modified→Update / Deleted→Delete），`resolver.IsRowInScope(...)` false → `ForbiddenException`。 |
| `Delete` | `scopeFilter`（Delete action）併進 `repository.Delete(rowId, scopeFilter)`；越範圍 → 刪 0 列（D3）。 |

- **Repository 簽名擴充**：`DataFormRepository.GetData/Delete` 加 optional `FilterNode? scopeFilter`，組進定點 WHERE（`sys_rowid = {0} AND <scope>`）。GetList 已收 `FilterNode`，直接 AND。
- **空 `PermissionModelId` → 全跳過**（向後相容，比照 line-1 gate；未標 model 的 form 不套 scope）。
- **scope = All / 無 grant scope override 且 model 預設 All** → `ResolveFilter` 回 null，等同不過濾。
- **FilterNode 欄名 remap**：scope condition 用主表 `FormField.FieldName`，確認經 `WhereBuilder.RemapFilterNodeFields` 正確 remap 為 `[alias].[field]`（風險點，見下）。

### Phase 3 測試
- 整合（`[DbFact]`，用 Phase 1 seed 的 demo user→employee→dept）：
  - GetList：越範圍列被過濾（Own / Dept / DeptAndSub 各驗）。
  - GetData：越範圍 rowId 回 null；範圍內正常。
  - Save：越範圍寫入（Added/Modified/Deleted 各一）被 `ForbiddenException` 擋；範圍內通過。
  - Delete：越範圍 rowId 刪 0 列。
  - 多角色合併（含一角色 All → 不過濾）。
  - 空 `PermissionModelId` → 不套 scope（向後相容）。

## 風險與取捨

- **重構 line-b grant 表（D5）**：`st_role_grant` 由 mask 改 per-action，牽動 line-b 已交付的 `RoleGrantRow`/`RolePermissionRepository`/`CompanyRolePermissions`/seed/測試。pre-release 可重構，但需一次到位、line-b 測試全綠。
- **快照 vs 即時（D2）**：部門/員工異動在已 enter 的 session 不即時反映（快照語意，比照 Roles）。可接受；需要即時可後續加「重進公司刷新」或 cache-notify。
- **fail-closed 邊界**：缺 scope 欄 / 身分 Empty → 全擋。安全但可能「設定漏標 ScopeRole → 使用者看不到資料」，靠 warning log + 載入期驗證（line A 已驗 ScopeRole 唯一性）緩解。
- **FilterNode 欄名 remap**：scope filter 注入 GetList 既有管線，須確認 master-table 欄名在 select context 下正確 remap（Phase 3 先寫一個 remap 驗證測試）。
- **Save 逐列效能**：逐列 `IsRowInScope` 走記憶體（session 快照 + dept tree 快取），無額外 DB；大量列時為純記憶體比對，可接受。
- **Print/Export egress**：`Rules[Print/Export].Scope` 慣例為 `Inherit` → 解為 model 的 Read scope（line A 已定 egress 繼承 Read）。本計畫 egress 不另接入（報表/匯出走各自 BO，屬後續）。

## 非目標（明確劃線）
- **前端 capability 降級**（element 細粒度、按鈕→action）→ plan-permission-capability。
- **參數化策略**（OwnerField / Nodes / CustomRef + predicate 逃生口）→ 後續純附加。
- **報表/批次（AnyCode）BO 的 scope 接入** → 各 BO 自行套 `IScopeResolver`，本計畫只接 `FormBusinessObject` CRUD。
- **部門/員工異動的 session 即時刷新** → 快照語意，另案。
- **employee 匯報樹（直屬主管）** → 與部門樹正交，非當前需求。

## 驗收
- `ft_employee.user_rowid` 5 DB fresh CREATE 正常；resolver 全綠。
- `EnterCompany` 後 SessionInfo 三 Guid 正確、leave 清空；序列化 round-trip 保留。
- `st_role_grant` 重構後 line-b 全測試 + 新 `GetEffectiveScopes` 綠。
- `ScopeResolver` 純單元全綠（各 strategy、多角色合併、隱含 Own、Owner 二身分、fail-closed 邊界）。
- Phase 3 整合：越範圍 read 過濾、write 擋、delete 0 列；多角色 All 短路；空 model 跳過。
- slnx build 0w/0e；本機全套 + CI 全 5 DB + SonarCloud quality gate passed。
