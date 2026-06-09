[English](permission-authorization.md)

# 權限與授權指南

Bee.NET 的授權是**兩層**、且與表單解耦：

| 層 | 問題 | 由什麼驅動 |
|----|------|-----------|
| **層一 — 動作 gate** | 這個使用者能不能對該 model 做這個*動作*？ | 角色授權（action mask） |
| **層二 — record scope** | 能對*哪些列*做？ | per-action scope 策略 + 使用者的身分／部門 |

兩層在請求時都**完全走記憶體快照**——DB 只在載入快取、登入、`EnterCompany`、改配置時碰。授權與 `ApiAccessControlAttribute`（管加密等級與是否需登入）**正交**。

設計理由見 [ADR-019](adr/adr-019-permission-authorization-model.md)。

## 1. 定義權限模型

**權限模型**是業務實體（如 `PurchaseOrder`），刻意與表單 `progId` 區別。所有 model 集中於單一 registry（`PermissionModels`，`DefineType.PermissionModels`）。每個 model 逐 action 宣告預設的 record-scope 策略：

```xml
<PermissionModels>
  <PermissionModel ModelId="PurchaseOrder" DisplayName="採購單">
    <Rules>
      <PermissionRule Action="Read"   Scope="DeptAndSub" />
      <PermissionRule Action="Update" Scope="Own" />
      <PermissionRule Action="Delete" Scope="Own" />
      <PermissionRule Action="Create" Scope="All" />
      <!-- Print / Export 省略 Scope → 繼承 model 的 Read scope -->
    </Rules>
  </PermissionModel>
</PermissionModels>
```

- `ModelId` 為業務實體 PascalCase。**一個 model 可被多個表單消費**（`PO001` 建立、`PO002` 查詢、`PO009` 報表都引用 `PurchaseOrder`）——授一次三功能一起生效。
- model 不綁資料表也不綁欄。scope 策略保持純語意，具體欄位由 FormSchema 提供（見下節）。

## 2. 表單綁定 model

`FormSchema` 宣告它消費哪個 model，並標記**主表**哪個欄是擁有者 / 部門：

```xml
<FormSchema ProgId="PO001" PermissionModelId="PurchaseOrder" ...>
  <Tables>
    <FormTable TableName="PO001" ...>
      <Fields>
        <FormField FieldName="buyer_rowid" Caption="採購員" ScopeRole="Owner" />
        <FormField FieldName="dept_rowid"  Caption="部門"   ScopeRole="Dept" />
        <!-- ... 其他欄位 ... -->
      </Fields>
    </FormTable>
  </Tables>
</FormSchema>
```

規則：

- `ScopeRole` **僅限主表**。明細表標 `ScopeRole` 會在載入期被 `PermissionBindingValidator` 報錯——record scope 由主檔決定、明細隨之。
- 每張主表最多一個 `Owner`、一個 `Dept` 欄。
- 空的 `PermissionModelId` → 表單**不套權限**，兩層皆跳過（漸進導入／向後相容）。

## 3. 授權角色

角色、授權、指派存於各**company 資料庫**（`st_` 框架表，公司內配置）：

| 表 | 欄位 | 意義 |
|----|------|------|
| `st_role` | `sys_id`, `sys_name` | 角色（指派的打包單位） |
| `st_role_grant` | `role_id`, `model_id`, `action`, `scope` | 角色對某 model 某 action 的授權 + 其 record scope |
| `st_user_role` | `user_id`, `role_id` | user ↔ role 指派（`user_id` 即 `st_user.sys_id`） |

`st_role_grant` 是 **per-action**：有列即為層一授權；其 `scope`（`ScopeStrategy`）驅動層二。這正是「某角色看得到整個部門、卻只能改自己的」之所以可能。

```sql
-- 「採購員」可看部門及子部門的採購單，但只能改／刪自己的：
INSERT INTO st_role_grant (role_id, model_id, action, scope) VALUES
  ('Buyer', 'PurchaseOrder', 2 /*Read*/,   4 /*DeptAndSub*/),
  ('Buyer', 'PurchaseOrder', 4 /*Update*/, 2 /*Own*/),
  ('Buyer', 'PurchaseOrder', 8 /*Delete*/, 2 /*Own*/);
-- scope = ScopeStrategy：Inherit=0, All=1, Own=2, Dept=3, DeptAndSub=4
-- action = PermissionAction（flags）：Create=1, Read=2, Update=4, Delete=8, Print=16, Export=32
```

`scope = Inherit (0)` → 取 model 該 action 的預設（第 1 節）。

## 4. 連結使用者與員工（部門 scope 需要）

部門／擁有者 scope 需解析**當前使用者 → 所屬部門**。使用者（`st_user`，common DB）透過 `st_employee.user_rowid` 連到員工（`st_employee`，company DB）：

```
st_user.sys_rowid  ──(st_employee.user_rowid)──▶  st_employee  ──(dept_rowid)──▶  st_department
```

`EnterCompany` 時框架一次性解析 `user → employee → 部門`，把 `UserRowId`、`EmployeeRowId`、`DeptRowId` 快照進 session，之後 scope 過濾零 DB。未綁員工的使用者其 employee/部門為空——`Own` 仍會比對 `UserRowId`，`Dept`/`DeptAndSub` 則不匹配任何列（fail-closed）。

## 5. enforcement 行為

### 層一 — 動作 gate

`FormBusinessObject` 執行前先判 `(model, action)`：

- `GetList` / `GetData` → `Read`
- `Save` → 逐列依 `RowState`：`Added`→`Create`、`Modified`→`Update`、`Deleted`→`Delete`
- `Delete` → `Delete`

多角色 **OR 聯集**（能力累加）。未過 → 拋 `ForbiddenException`。

### 層二 — record scope

**讀取**（`GetList`、`GetData`）把 scope filter `AND` 進查詢。越範圍列被過濾掉；越範圍的單列查詢回 `null`（與「查無」不可區分，呼叫端無法探測看不到的列）。

**寫入**（`Update`、`Delete`）由對 DB 的**權威 re-query** 把關——`WHERE sys_rowid = id AND <scope>`——而**非**評估送來的 payload。偽造的 DataSet 無法 relabel 繞過。

- `Save` 只在主表列為「既存記錄存檔」（非 `Added`）時 re-check。只改表身、主表 `Unchanged` 仍算 `Update`。
- `Delete(rowId)` 越範圍 → 回 0、不 cascade。
- **`Create` 不套 scope**——新列無既存範圍可違反，由動作授權管。
- scope **僅主表**：主檔過了，整筆（含明細）一併放行。

### scope 策略

| 策略 | 讀取 filter（與寫入 re-query 同源） |
|------|----------------------------------|
| `All` | 不限制 |
| `Own` | `owner 欄 IN {UserRowId, EmployeeRowId}` |
| `Dept` | `dept 欄 = DeptRowId` **OR** Own |
| `DeptAndSub` | `dept 欄 IN（部門 + 所有後代）` **OR** Own |
| `Inherit` | model 該 action 的預設（再退 Read scope，再退 `All`） |

- `Dept` / `DeptAndSub` **隱含 `Own`**——使用者永遠看得到自己擁有的列。
- **多角色合併**：任一角色對該 action 授 `All` → 不過濾；否則各策略 **OR 聯集**。
- `Own` 的 owner 欄可能存 user rowid 或 employee rowid（如*登打者* vs *請假人員*）；`IN {UserRowId, EmployeeRowId}` 同時涵蓋兩者，且使用者未必對應員工。

## 6. 快取與失效

- 角色／授權／user-role 載入 per-company `CompanyRolePermissions` 快取；部門樹載入 per-company `DepartmentTree` 快取。兩者皆 DB 來源，由 common cache-notify poller 失效。
- `SessionInfo` 持有請求時快照（`Roles`、`UserRowId`、`EmployeeRowId`、`DeptRowId`），`EnterCompany` 填入、`LeaveCompany` / `Logout` 清除。
- 快照為某時間點：改配置對「走快取的判定」即時反映（`Can` 現查快取）；已進公司 session 的 role/employee/部門快照於下次 `EnterCompany` 更新。

## 非目標

- **element 細粒度 capability**（UI 按鈕→action 降級）屬獨立的前端關注點；後端在方法層 enforce、不靠前端。
