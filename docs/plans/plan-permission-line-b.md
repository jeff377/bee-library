# 計畫：ERP 權限機制（線 B — 角色指派 + 層一 enforcement）

**狀態：🚧 進行中（2026-06-04）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | grant 資料模型（`st_role` / `st_role_grant` / `st_user_role`）+ TableSchema + Repository | ✅ 已完成（2026-06-04） |
| 2 | `SessionInfo.Roles` + per-company 權限快取（三表,判權限零 DB）+ `AuthorizationService.Can` | 📝 待做 |
| 3 | 方法層 enforcement（`FormBusinessObject` gate + FormActions↔PermissionAction 映射 + Save 逐列） | 📝 待做 |

## 承接與範圍

承接 [plan-erp-permission.md](plan-erp-permission.md)（線 A 定義層,已完成）。線 A 做完「功能宣告需要什麼權限」（registry + `FormSchema.PermissionModelId`）;本計畫做**線 B 的供給側**（角色擁有什麼權限、指派給誰）**並接上層一 enforcement**,讓後端**真正開始擋越權請求**。

- ✅ **做（層一）**：role / grant / user 資料模型（per-company）、per-company 權限快取（三表,判權限零 DB）、`SessionInfo.Roles`、`Can(model, action)` 判定、方法層 gate（用 `FormSchema.PermissionModelId` + 方法映射）。
- ❌ **不做（層二,留 record-scope plan）**：record scope 的實際過濾（`ScopeResolver`、組織階層、多角色 OR 合併、model vs grant 層 scope 覆寫）。本計畫的 grant **只存 `(model, action)` mask,不含 scope**。
- ❌ **不做（前端,留 capability plan）**：element 細粒度降級。

> 一句話：本計畫做完,「採購員不能刪訂單」這類**層一越權**後端會直接擋;但「只能看自己部門的訂單」（層二 scope）還不會過濾。

## 為何先做本計畫（三個延伸 plan 中）

- **依賴最少**：只靠線 A 已交付的 registry,無組織結構等額外前置。
- **是地基**：record-scope（層二）與 capability（前端）都需要本計畫的 grant + `Can`。
- **價值即時**：層一 enforcement 一上線,權限機制從「能設定」變成「真生效」(安全邊界)。

## 資料模型（各 company DB，`st_` 框架層表）

角色/授權是**架構底層機制**（`st_` 前綴 = 框架表,`ft_` 才是業務資料表）,但屬**公司內配置**,故存**各 company DB**（與 `ft_department` / `ft_employee` 同隔離原則,前綴用 `st_` 表示它是框架機制而非業務資料）。多租戶下角色必然 per-company：

| 表（各 company DB） | 主要欄位 | 說明 |
|---|---|---|
| `st_role` | `sys_id`(角色代碼), `sys_name` | 公司內角色定義（打包單位） |
| `st_role_grant` | `role_rowid`, `model_id`, `allowed_actions`(int mask) | 一筆 grant：角色對某 model 的 `PermissionAction` mask |
| `st_user_role` | `user_rowid`, `role_rowid` | user↔role 多對多指派；`user_rowid` 跨 DB 邏輯指向 common `st_user.sys_rowid` |

- `allowed_actions` 是 `PermissionAction`（`[Flags]`）的 int mask,沿用線 A 的 enum（型別安全、bitmask 檢查快）。
- `model_id` 對應線 A `PermissionModel.ModelId`（PascalCase 業務實體）。
- **scope 不在此**（層二留 record-scope plan）。

## 身分快照 + per-company 權限快取 → 判權限零 DB

權限三張表（`user→role` / `role→grant` / `role`）都是 per-company 的**相對穩定配置**,整份載入成 **per-company 權限快取**（DB 來源、比照 `CompanyInfoCache`,失效走 **cache-notify**——非 file-watch）。**user/role/permission 判權限完全走記憶體、零 DB**——DB 只在載入快取、改配置時碰:

```
per-company 權限快取（從 st_user_role / st_role / st_role_grant 載入,改配置刷快取）
  user → roles :  userId → roleId[]
  role → grants:  roleId → (modelId → action mask)

EnterCompany
  → 從權限快取取 user 在此公司的 role id 清單（不查 DB）
  → 記入 SessionInfo.Roles（多個 roleid,輕量 string）
  → LeaveCompany / Logout 隨 SessionInfo 清除

Can(SessionInfo.Roles, modelId, action):
  → 對每個 roleId 從權限快取取 modelId 的 mask → OR 合併 → HasFlag(action)   // 全記憶體 bitmask
```

- **三表全快取 → 判權限完全不碰 DB**;跟線 A「`SessionInfo` 存身分、`PermissionModels` 快取共用」同一套分層。
- **改配置刷快取即生效**:`role→grant` 改 → 已登入 session 立即反映（`Can` 現查快取）;`user→role` 改 → 影響之後進公司者（已存 `Roles` 的 session 是快照,可選擇一併刷新）。
- 多角色合併:同一 model 的 mask 取 **OR（聯集）**——能力累加（與線 A 合併語意一致）。
- `AuthorizationService.Can(roles, modelId, action)` 透過注入的權限快取解析,純邏輯可用合成 roles + 定義單元測試。

## 方法層 enforcement 接入

後端只在**方法層** enforce（設計文件 §6.3,元素降級是前端的事）。用線 A/Phase 2 已備好的兩塊:

```
FormSchema.PermissionModelId  +  FormActions↔PermissionAction 映射:
  GetList / GetData / GetNewData → Read
  Save  → 逐列依 RowState（Added→Create / Modified→Update / Deleted→Delete）
  Delete → Delete
→ Can(主model, action) 不過 → 擋（ForbiddenException）
```

- **接入點 = `FormBusinessObject` 基類 guard**（已定案）：在 GetList/GetData/Save/Delete 入口統一檢查,手邊即有 `FormSchema`（含 `PermissionModelId`）。
- `Save` **逐列判定**：對 DataSet 每列依 `RowState` 對應 action,逐列過 `Can`——這是層一逐列（不涉及 scope）,落實設計文件「寫入端逐列重算」。
- 與既有 `ApiAccessControlAttribute` 正交:它管加密等級 + 是否登入;本 gate 管 per-user 的 `(model, action)`。

## 與現有框架對應

| 概念 | 現有結構 | 本計畫作法 |
|---|---|---|
| 使用者身分 | `SessionInfo`（UserId/CompanyId,無 role） | `SessionInfo` 增 `Roles`（roleid 清單）,`EnterCompany` 填入、`LeaveCompany` 清除 |
| 角色 / 權限定義 | `CompanyInfoCache`（DB 來源 + cache-notify 失效） | per-company `CompanyRolePermissionsCache : KeyObjectCache<T>`（DB 來源,**cache-notify 失效**,比照 `CompanyInfoCache`） |
| 既有授權先例 | `st_user_company` / `UserCompanyRepository.HasAccess` | 平行新增 role/grant Repository |
| API 管線 | `JsonRpcExecutor` → `ApiAccessValidator` | permission gate 接 BO 基類（非管線,因需 FormSchema） |
| 方法分派 | progId.action → `FormBusinessObject` 方法 | 方法入口 guard |
| 動作 enum | `PermissionAction`（線 A 已交付） | grant mask 沿用 |

## 階段拆解

### Phase 1 — grant 資料模型
- `st_role` / `st_role_grant` / `st_user_role` 的 TableSchema（company category，各 company DB）。
- `Role` / `RoleGrant` 等 domain 物件 + Repository（仿 `UserCompanyRepository`，但走 company DB scope）。
- 驗收：CRUD round-trip;`st_user_role` → `st_role_grant` 查詢取得使用者在此公司的 grant 清單。

### Phase 2 — 權限快取 + roles 存 session + Can 判定
- per-company **權限快取** `CompanyRolePermissionsCache : KeyObjectCache<T>`（從 `st_user_role`/`st_role`/`st_role_grant` 載入;判權限零 DB）。失效走 **common 集中 cache-notify**:company 改 role/grant 後寫一筆 bump 到 **common 的 `st_cache_notify`**（key `"RolePermissions:{companyId}"`）,既有 poller（輪詢 common 單表）即 evict——**不改 poller、不輪詢 company DB**。
- `SessionInfo` 增 `Roles`（roleid 清單,輕量 string）;`EnterCompany` 從權限快取取 user 的 roles 填入（不查 DB）,`LeaveCompany`/`Logout` 隨 session 清除（與 `CompanyId`/`CustomizeId` 同步）。序列化納入 `st_session`。
- `AuthorizationService.Can(roles, modelId, action)`:對每個 role 從快取取 mask、OR 合併、`HasFlag`。
- 驗收：合成 roles + 權限快取單元測試（層一 AND、多角色 OR 合併、未授予→拒絕、改配置刷快取即生效、判權限不碰 DB）。

### Phase 3 — 方法層 enforcement
- `FormActions` ↔ `PermissionAction` 映射常數。
- `FormBusinessObject` 基類 guard：GetList/GetData→Read、Delete→Delete、Save→逐列 RowState。
- 驗收：越權 action 被擋（403/Forbidden）;Save 混合 RowState 逐列判定;有權正常通過的整合測試。

## 已定案決策

- **per-company 角色（各 company DB）**：role/grant/user_role 存各 company DB,用 `st_` 前綴（= 框架層表,不論 DB;`ft_` 才是業務表）。多租戶下角色必然 per-company。`user_rowid` 跨 DB 邏輯指向 common `st_user`。
- **roles 在 `EnterCompany` 取得**（非 Login）：使用者的角色是 per-company,進公司才確定;**從權限快取取**（不查 DB）、存 `SessionInfo.Roles`,`LeaveCompany`/`Logout` 清除。
- **enforcement 接入點 = `FormBusinessObject` 基類 guard**：方法入口統一檢查,手邊即有 FormSchema。
- **身分存 `SessionInfo.Roles`、權限三表整份快取**：`SessionInfo` 只記 roleid 清單（身分）;`user→role` + `role→grant`（+ `role`）做成 **per-company 權限快取**（`KeyObjectCache`,DB 來源,比照 `CompanyInfoCache`）。`Can` 全程走快取——**user/role/permission 判權限零 DB**,DB 只在載入/改配置時碰。
- **失效 = common 集中 cache-notify（不改 poller）**：poller 仍輪詢 common 單一 `st_cache_notify`。company 的 role/grant 異動後寫一筆 bump 到 common 通知表（`"RolePermissions:{companyId}"`）驅動 evict。bump 與 company 資料變更跨 DB、非嚴格同 transaction——採「先 commit company 資料、再寫 common bump」,最終一致、最壞快取暫時偏舊（可接受）。
- **空 `PermissionModelId` → gate 跳過**：未標 model 的 FormSchema 放行（向後相容、漸進導入）。

## 待確認事項（實作時即可定,無阻塞性前置）

1. **匿名 / 未進公司**：傾向 `Anonymous` 方法跳過 gate;未 `EnterCompany` 的 FormBO 請求本就被既有 `CompanyNotEntered` 擋,不需 line-b 額外處理。
2. **`SessionInfo.Roles` 序列化 + `EnterCompany` 插入點**：實作時確認序列化器涵蓋新欄位、在既有 `EnterCompany` 後段插入取 roles 填 `SessionInfo.Roles`。
