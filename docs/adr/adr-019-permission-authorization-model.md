# ADR-019：權限授權模型（兩層 enforcement + record scope）

## 狀態

已採納（2026-06-05）

## 背景

Bee.NET 原本只有**身分驗證**（[ADR-012](adr-012-session-company-context.md) 的 `Login` / `EnterCompany`）與 API 層的加密／登入把關（`ApiAccessControlAttribute`）。但 ERP 真正需要的是**授權**——「誰能對哪個業務實體做什麼動作」，以及「能對哪些資料列做」。兩者是不同關注點：

- **動作軸（能不能做）**：採購員不能刪採購單。
- **資料範圍軸（對哪些列做）**：採購員只能看／改自己部門的採購單。

設計時的硬性約束：

1. **多租戶 per-company**：角色與授權是公司內配置，必然 per-company（與 `st_employee` 同隔離）。
2. **判權限零 DB**：每個 API 請求都要判權限，不能每次查 DB——必須走記憶體快取 + session 快照。
3. **與 form 解耦**（對齊 Odoo `ir.model.access` / `ir.rule`）：權限綁「業務實體（model）」不綁表單（progId）也不綁資料表／欄；一個 model 可被多個 progId 消費，授一次三功能生效。
4. **安全邊界在後端**：前端送來的 payload 不可信，授權判定不能依賴 client 提供的值。
5. **可宣告、可載入期驗證**：scope 用業務語意選單（具名策略），不手寫 predicate，能在載入期驗證。

## 決策

採**雙軸、三段**的權限模型；資料流見下：

```
線 A（定義層）      宣告「功能需要哪個 model、欄位的 scope 角色」
線 B 層一（動作）    判 (model, action) —— 能不能做（zero-DB）
線 B 層二（範圍）    依 (model, action) 的 scope 過濾／把關資料列（zero-DB）
```

### 線 A — 定義層（可宣告、可驗證、零 enforcement）

- **`PermissionModels`**（單檔 registry，`DefineType.PermissionModels`）：每個 `PermissionModel`（`ModelId` = 業務實體 PascalCase，如 `PurchaseOrder`）宣告其 `PermissionRule` 集合——每個 rule = `(PermissionAction, ScopeStrategy)`。`ModelId` 刻意與表單 progId 區別，**綁 model 不綁 form**。
- **`FormSchema.PermissionModelId`**：表單宣告它消費哪個主 model（BO 方法層手邊即有 FormSchema → enforce 最直接）。
- **`FormField.ScopeRole`**（`None` / `Owner` / `Dept`）：標記「哪個欄是擁有者 / 部門」。**scope 策略保持純語意**（`Own`→`Owner` 欄、`Dept`/`DeptAndSub`→`Dept` 欄），欄名留在 FormSchema、model 與表／欄脫鉤。
- **scope 僅限主表**：`ScopeRole` 只在主表；明細表標 `ScopeRole` 由 `PermissionBindingValidator` 於載入期報錯。

### 線 B 層一 — 動作 gate（zero-DB）

- 資料模型（各 company DB，`st_` 框架層表）：`st_role` / `st_role_grant` / `st_user_role`。
- **per-company 權限快取**（`CompanyRolePermissions`，比照 `CompanyInfoCache`，DB 來源 + cache-notify 失效）：整份載入 user→role / role→grant，**判權限完全走記憶體**。
- `EnterCompany` 從快取取 user 在此公司的 role 清單，**快照進 `SessionInfo.Roles`**；之後 `AuthorizationService.Can(token, model, action)` 全程零 DB。
- 接入點 = **`FormBusinessObject` 方法層 gate**：`GetList`/`GetData`→Read、`Save`→逐列 RowState（Added→Create / Modified→Update / Deleted→Delete）、`Delete`→Delete。多角色 mask 取 **OR 聯集**（能力累加）。

### 線 B 層二 — record scope（zero-DB query time）

- **scope per-action**（`st_role_grant` = per-`(role, model, action)` 一列帶 `scope`）：落實「可以看不一定可以改」——Read scope 可為 `Dept`、Update scope 可為 `Own`。`Inherit` → 取 `PermissionModel.Rules[action].Scope`（model 預設）。
- **具名策略**（`ScopeStrategy`）：`All`（不限）／`Own`／`Dept`／`DeptAndSub`／`Inherit`。
- **`IScopeResolver`**：依 `(model, action, session, FormSchema)` 解析。多角色合併規則：**任一角色 `All` → 不過濾**；否則各角色 predicate 取 **OR 聯集**。
  - `Own` = owner 欄 `IN {UserRowId, EmployeeRowId}`（**二身分**：欄可能存 user rowid〔如登打者〕或 employee rowid〔如請假人員〕，Guid 不碰撞、單一策略涵蓋；user 未必對應 employee）。
  - `Dept` = `dept 欄 = DeptRowId` **OR Own**（隱含 Own）。
  - `DeptAndSub` = `dept 欄 IN GetSelfAndDescendants(DeptRowId)` **OR Own**，用 per-company `DepartmentTree` 快取展開。
- **「user → 部門」零 DB**：`st_employee.user_rowid` 連結 common `st_user` ↔ company `st_employee`；`EnterCompany` 一次性解析 `user→employee→dept`，把 `UserRowId`/`EmployeeRowId`/`DeptRowId` **快照進 `SessionInfo`**，查詢時零 DB。
- **讀取端**：`GetList`/`GetData` 把 scope filter `AND` 進查詢（越範圍列被過濾 / 越範圍單列回 `null`，與「查無」不可區分）。
- **寫入端（Update / Delete）= 後端權威 re-query**：對 target rowId 下 `sys_rowid = id AND scope` 的存在性查詢（`ExistsInScope`），確認 **DB 那筆**在範圍內——**不評估 client 送來的列值**（偽造 payload relabel 也繞不過）。`Save` 先判主表列為「既存記錄存檔」（非 `Added`）才查；`Delete` 越範圍 → 刪 0、不 cascade。
- **Create 不套 scope**：新列無「既存範圍」可違反，由動作授權（層一）把關。
- **scope 僅主表 / 整筆完整性**：只判主表列、只查主表 → 主檔過了明細隨整筆放行，不會「主檔過、某明細被擋」的半套。

## 後果與取捨

- ✅ **判權限零 DB**：身分快照 + per-company 快取（角色／權限／部門樹）；DB 只在登入、進公司、改配置時碰。
- ✅ **動作與範圍正交**：層一管 (model, action)、層二管資料列；各自獨立、可組合。
- ✅ **per-action 精度**：看與改的範圍可不同。
- ✅ **讀寫對稱、安全邊界在後端**：寫入端用權威 re-query，不信任 payload。
- ✅ **與 form/table 解耦**：一個 model 多個 progId 共用一次授權。
- ⚠️ **快照語意**：`Roles` / employee / dept 在已進公司的 session 是快照，配置中途變動不即時反映（可接受；需即時可加重進公司刷新或 cache-notify）。
- ⚠️ **fail-closed 邊界**：scope 需要的欄缺失或身分為空 → 不匹配任何列（安全預設）+ 載入期驗證緩解。
- ✅ **前端 capability（element 細粒度降級）已實作（2026-07-03）**：層一／層二仍在後端方法層權威 enforce、不靠前端。權限可視為**三維度 × 兩把關點**——**動作**維度在後端權威 gate、同時投影到前端決定工具列命令／按鈕狀態；**列**維度僅後端；新增**欄**維度（`FormField.SensitiveCategory` → well-known 分類 model，依 Read/Update 隱藏／唯讀）僅前端。capability 快照搭 `EnterCompany` 回傳（`EnterCompanyResponse.Capabilities`）、快取於 `ClientInfo.Capabilities`、由 `Bee.UI.Core.Permissions.ElementCapabilityResolver` 解析。前端**純 UX、非資料邊界**（後端未遮罩敏感欄值）。詳見[使用者指南](../permission-authorization.zh-TW.md)第二部分。

## 參考

- 定義層：[plan-erp-permission.md](../archive/plan-erp-permission.md)（已封存）
- 層一：[plan-permission-line-b.md](../archive/plan-permission-line-b.md)（已封存）
- 組織部門樹：[plan-org-department-tree.md](../archive/plan-org-department-tree.md)（已封存）
- 層二 record scope：[plan-record-scope-enforcement.md](../archive/plan-record-scope-enforcement.md)（已封存）
- 相關 ADR：[ADR-005（FormSchema 驅動）](adr-005-formschema-driven.md)、[ADR-010（邏輯 DB 分類）](adr-010-logical-database-category.md)、[ADR-012（Session 公司情境）](adr-012-session-company-context.md)、[ADR-017（DB 快取失效）](adr-017-db-cache-invalidation.md)
- 使用者指南：[permission-authorization.zh-TW.md](../permission-authorization.zh-TW.md)
