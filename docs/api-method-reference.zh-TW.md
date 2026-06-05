# API 方法參考

[English](api-method-reference.md)

本文件為**單頁總覽**：列出所有透過 `JsonRpcExecutor` 對外公開的 BO 方法，
依 BO 軸分組。每列標註該方法的 wire-level [合約介面](api-bo-contract-design.zh-TW.md)、
BO 層 Args / Result 型別、`[ApiAccessControl]` 設定，與一行用途說明。

> **真相來源。** 本參考由 `BoApiSurfaceTests`（位於
> `tests/Bee.Business.UnitTests/`）與 BO 源碼即時對照。新增或修改方法時必須
> 同步更新本文件與測試 baseline，否則 build 會失敗。

## 欄位說明

| 欄位 | 意義 |
|------|------|
| **Method** | JSON-RPC `method` 欄位 — `progId.action`。對應 `SystemActions` / `FormActions` 常數。 |
| **Protection** | `[ApiAccessControl]` 第一參數：`Public` / `Encoded` / `Encrypted`。詳見[安全規範](../.claude/rules/security.md)。 |
| **Auth** | `[ApiAccessControl]` 第二參數：`Anonymous` / `Authenticated`。 |
| **用途** | 一行摘要；完整說明見對應 BO 方法的 XML doc。 |

### 命名慣例（Contract / Args / Result 可由 action 推導）

下方表列的每一個 `<Action>`，對應的合約 / BO 型別都依固定 pattern 推導：

- **Wire 合約**：`Bee.Api.Contracts.I<Action>Request` / `I<Action>Response`
- **Wire DTO**：`Bee.Api.Core.Messages.<Axis>.<Action>Request` / `<Action>Response`
- **BO Args / Result**：`Bee.Business.<Axis>.<Action>Args` / `<Action>Result`

例如 `GetLanguage` → `IGetLanguageRequest` / `IGetLanguageResponse` /
`GetLanguageArgs` / `GetLanguageResult`。IDE「跳至符號」即可從 action 名
直達任一型別，無需在表格內重複列出。

## 軸：Base（`BusinessObject`）

定義於基底類別，所有 BO 軸繼承使用。

| Method | Protection | Auth | 用途 |
|--------|------------|------|------|
| `ExecFunc` | Public | Authenticated | 通用 dispatch 機制，依名稱呼叫 host 定義的自訂方法。 |
| `ExecFuncAnonymous` | Public | Anonymous | 同 `ExecFunc` 但開放未登入呼叫（如註冊流程）。 |

## 軸：System（`SystemBusinessObject`）

單例系統層級 BO，wire 上以 `System.<action>` 派發。

| Method | Protection | Auth | 用途 |
|--------|------------|------|------|
| `Ping` | Public | Anonymous | Liveness 探針;回傳 server timestamp。 |
| `GetCommonConfiguration` | Public | Anonymous | 回傳 `CommonConfiguration`（payload options、debug flag、預設語系等）。 |
| `Login` | Public | Anonymous | 使用者驗證；回傳 access token 與動態 API 加密金鑰。 |
| `CreateSession` | Public | Anonymous | 發行匿名 session token（無使用者身分）。 |
| `EnterCompany` | Public | Authenticated | 將 session 切換至指定 company（多租戶範圍）。 |
| `LeaveCompany` | Public | Authenticated | 清除 company context，session 維持登入。 |
| `Logout` | Public | Authenticated | 銷毀目前 session（同時清除 company context）。 |
| `GetDefine` | Public | Authenticated | XML envelope 取定義資料（通用 — .NET client FormSchema / FormLayout / LanguageResource 都走此方法）。 |
| `SaveDefine` | Public | Authenticated | XML envelope 持久化定義資料；同時失效對應 cache slot。 |
| `GetFormSchema` | Public | Authenticated | **JS-only。** 以 typed JSON tree 回傳 `FormSchema`（依 session `Culture` 自動本地化）。 |
| `GetFormLayout` | Public | Authenticated | **JS-only。** 回傳 `FormLayout`（由自動本地化的 FormSchema 動態 generate）。 |
| `GetDepartmentTree` | Public | Authenticated | 以 typed 物件（JSON / MessagePack）回傳當前公司的部門樹（per-company 組織階層）；未進公司時為 `null`。 |
| `GetLanguage` | Public | Authenticated | **JS-only。** 取單一 `(Lang, Namespace)` 配對的 `LanguageResource`。 |
| `CheckPackageUpdate` | Encoded | Anonymous | 回報是否有 client 端套件升級可用。 |
| `GetPackage` | Encoded | Anonymous | 串流回傳 client 端升級套件 binary。 |

> **JS-only 方法。** `GetFormSchema` / `GetFormLayout` / `GetLanguage` 內部走
> `KeyCollectionBase`，在 MessagePack（Encoded / Encrypted wire format）下
> 會掉 collection 元素。設計上為 JS / TypeScript 端透過 Plain JSON wire 取用，
> .NET client 請改用 `GetDefine` 配對應 `DefineType`。

## 軸：Form（`FormBusinessObject`）

per-program BO 實體，wire 上以 `<progId>.<action>` 派發（例如 `Employee.GetList`、`Order.Save`）。

| Method | Protection | Auth | 用途 |
|--------|------------|------|------|
| `GetList` | Public | Authenticated | Master table 列表查詢；支援 `Filter` / `Sort` / `Paging`（呼叫端務必分頁）。 |
| `GetNewData` | Public | Authenticated | 回傳空白 `DataSet` 骨架（含 FormSchema 預設值 + server 派發的 `sys_rowid`）。 |
| `GetData` | Public | Authenticated | 依 `RowId` 載入單筆主檔列（與其所有子表列）。 |
| `Save` | Public | Authenticated | 將 `DataSet` 持久化，依每列 `RowState` dispatch INSERT / UPDATE / DELETE。 |
| `Delete` | Public | Authenticated | 依 `RowId` 直接刪除單筆主檔列。 |

## 參考

- [API 合約 & BO 參數設計](api-bo-contract-design.zh-TW.md) — Contract / Args / Result 分層設計原理
- [安全規範](../.claude/rules/security.md) — `ApiAccessControl` 語意與 payload pipeline
- [bee-add-bo-method skill](../.claude/skills/bee-add-bo-method/SKILL.md) — 新增方法 step-by-step（含更新本參考的步驟）
