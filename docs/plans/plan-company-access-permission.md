# 計畫：公司存取權限（user-company 對照）— 後端

**狀態：✅ 已完成（2026-05-16）**

## 背景

`SystemBusinessObject.EnterCompany` 目前未實作權限驗證，任何登入過的 user 都能進入任何 company。`SessionInfo.CompanyId` 之後串到 `RepositoryDatabaseRouter` 用於決定 company-category 的 DatabaseId，缺少這層把關等於完全沒有多公司隔離。

[SystemBusinessObject.cs:148](../../src/Bee.Business/System/SystemBusinessObject.cs#L148) 已留下 `TODO(plan-company-access-permission)` 接點，這份 plan 即對應該接點。

並列觀察：[CompanyInfoService.Get](../../src/Bee.ObjectCaching/Services/CompanyInfoService.cs#L25) 文件寫了「cache miss 時 fallback 到 database」但實作只查 cache —— 因為過去沒有 `st_company` 表可查。本 plan 同時補上這層 fallback。

## 範圍

**本次涵蓋**：
- 新增 `st_company`、`st_user_company` 兩張實體表（common 庫）
- 對應 `TableSchema/*.xml` 與 `DbCategorySettings.xml` 更新
- 後端 Repository / Service / BO 程式碼
- 測試（含 5 DB `[DbFact]` 覆蓋與 fixture seed）

**本次不做**（未來另案）：
- `FormSchema/Company.FormSchema.xml`、`FormSchema/UserCompany.FormSchema.xml`
- FormBO-driven CRUD 維護畫面（Insert/Update/Delete via DataSet）
- 角色/權限模型（role-based）—— 本次以 `enabled` 旗標暫代
- API 層的 `CompanyAdmin` / `UserCompanyAdmin` BO 與 wire/contract

## 設計決策

### D1：對照表 FK 用 rowid，不用 business id

```
st_user_company.user_rowid    → st_user.sys_rowid
st_user_company.company_rowid → st_company.sys_rowid
```

**Why**：使用者 `sys_id`（business key，如 "001"）允許後續變更；用 rowid 做 FK，sys_id 異動不破壞對照關係。

### D2：Repository 拆兩支

- `ICompanyRepository`：讀 `st_company`（給 `CompanyInfoService` 做 cache fallback 用）
- `IUserCompanyRepository`：查 `st_user_company` 對照 + `HasAccess(userId, companyId)`

**Why**：st_company 之後會有獨立維護畫面（ProgId=Company），對照表是純後端機制；職責不同分開更清楚。仿 [ISessionRepository](../../src/Bee.Repository.Abstractions/System/ISessionRepository.cs) 模式經 `ISystemRepositoryFactory` 建立。

### D3：user_rowid 解析採即時 JOIN，不擴張 SessionInfo

EnterCompany 權限驗證以一個 SQL JOIN 完成：
```sql
SELECT 1 FROM st_user_company uc
INNER JOIN st_user u    ON u.sys_rowid = uc.user_rowid
INNER JOIN st_company c ON c.sys_rowid = uc.company_rowid
WHERE u.sys_id = {0} AND c.sys_id = {1} AND c.enabled = 1
```

**Why**：不擴張 `SessionInfo`（保持 cache invalidation 面最小）；查詢只在 EnterCompany 觸發，不在熱路徑；rowid 在 DB 內部使用，外部 API 仍以 business id 溝通。

### D4：失敗訊息統一不洩漏細節（D8 merged error surface）

下列三種情況一律拋同一例外：
```csharp
throw new InvalidOperationException("Company access denied.");
```

- company 不存在
- company 存在但 `enabled=false`
- user-company 對照不存在

**Why**：避免攻擊者透過錯誤訊息差異枚舉 company 清單；既有 [SystemBusinessObject.cs:146](../../src/Bee.Business/System/SystemBusinessObject.cs#L146) 已採此模式，延續一致。

## DB Schema

### `st_company`（common 庫）

| 欄位 | DbType | 說明 |
|------|--------|------|
| `sys_no` | AutoIncrement | 流水號（PK） |
| `sys_rowid` | Guid | rowid（unique） |
| `sys_id` | String(20) | company business key（unique） |
| `sys_name` | String(50) | 公司名稱 |
| `company_database_id` | String(50) | 對應 `DatabaseSettings` 條目，company 庫的 DatabaseId |
| `enabled` | Boolean | 啟用旗標（DB default `true`） |
| `sys_insert_time` | DateTime | audit |

索引：`pk_st_company`（sys_no, PK）、`rx_st_company`（sys_rowid, unique）、`ux_st_company`（sys_id, unique）

### `st_user_company`（common 庫）

| 欄位 | DbType | 說明 |
|------|--------|------|
| `sys_no` | AutoIncrement | 流水號（PK） |
| `sys_rowid` | Guid | rowid（unique） |
| `user_rowid` | Guid | FK → `st_user.sys_rowid` |
| `company_rowid` | Guid | FK → `st_company.sys_rowid` |
| `sys_insert_time` | DateTime | audit |

索引：`pk_st_user_company`（sys_no, PK）、`rx_st_user_company`（sys_rowid, unique）、`ux_st_user_company`（user_rowid + company_rowid, unique 複合）

> FK 不在 TableSchema 階段宣告（既有表均無 FK 宣告慣例），由應用層保證一致性；對照表的 `ux_` 複合 unique 防重複授權。

## 影響的程式

### 新增

| 路徑 | 內容 |
|------|------|
| `tests/Define/TableSchema/common/st_company.TableSchema.xml` | st_company schema |
| `tests/Define/TableSchema/common/st_user_company.TableSchema.xml` | st_user_company schema |
| `src/Bee.Repository.Abstractions/System/ICompanyRepository.cs` | `CompanyInfo? GetById(string companyId)` |
| `src/Bee.Repository.Abstractions/System/IUserCompanyRepository.cs` | `bool HasAccess(string userId, string companyId)`、`void Grant(Guid userRowId, Guid companyRowId)`、`void Revoke(Guid userRowId, Guid companyRowId)` |
| `src/Bee.Repository/System/CompanyRepository.cs` | 仿 [SessionRepository](../../src/Bee.Repository/System/SessionRepository.cs)，原生 SQL |
| `src/Bee.Repository/System/UserCompanyRepository.cs` | 同上 |

### 修改

| 路徑 | 改動 |
|------|------|
| `tests/Define/DbCategorySettings.xml` | common 區塊加 2 個 `<TableItem>` |
| `src/Bee.Db/Providers/PostgreSql/PgSchemaSyntax.cs` | `GetDefaultExpression` 加 Boolean case：`"1"` → `TRUE`、`"0"` → `FALSE`；內建 `"0"` 保持 |
| `src/Bee.Db/Providers/PostgreSql/PgTableSchemaProvider.cs` | `ParseDBDefaultValue` 加 boolean dataType case：DB 回 `"true"`/`"false"` 規範化為 `"1"`/`"0"` |
| `src/Bee.Repository.Abstractions/Factories/ISystemRepositoryFactory.cs` | 加 `CreateCompanyRepository()`、`CreateUserCompanyRepository()` |
| `src/Bee.Repository/Factories/SystemRepositoryFactory.cs` | 對應實作 |
| `src/Bee.ObjectCaching/Services/CompanyInfoService.cs` | ctor 注入 `ICompanyRepository`；`Get` 於 cache miss 時呼叫 `GetById` 並回填 cache |
| `src/Bee.Business/System/SystemBusinessObject.cs` `EnterCompany` | 移除 TODO，加入 `IUserCompanyRepository.HasAccess` 驗證；失敗統一拋 `InvalidOperationException("Company access denied.")` |
| `tests/Bee.Tests.Shared/SharedDatabaseState.cs` | `EnsureSeedData` 擴展：建 1 筆 seed company（sys_id="C001"、enabled=true）+ 建 seed user '001' ↔ seed company 對照 |
| `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` | 若 `CompanyInfoService` ctor 變動，DI 註冊端對應調整 |

### 不動

- `SessionInfo`（D3 決策）
- `Bee.Db` 各方言 builder（純加表，無新 DbType / 新 dialect 需求）
- `RepositoryDatabaseRouter`（已注入 `ICompanyInfoService`，本案讓 `CompanyInfoService` 自身能解析即可）

## 測試計畫

### Repository（5 DB `[DbFact]`）

`tests/Bee.Repository.UnitTests/System/CompanyRepositoryTests.cs`：
- `GetById_ExistingCompany_ReturnsCompanyInfo`
- `GetById_NonExistent_ReturnsNull`
- `GetById_Disabled_ReturnsCompanyInfo`（enabled flag 不過濾在 Repository 層，只在 BO 層）

`tests/Bee.Repository.UnitTests/System/UserCompanyRepositoryTests.cs`：
- `HasAccess_GrantedAndEnabled_ReturnsTrue`
- `HasAccess_NotGranted_ReturnsFalse`
- `HasAccess_GrantedButCompanyDisabled_ReturnsFalse`
- `HasAccess_NonExistentCompany_ReturnsFalse`
- `Grant_SameUserCompanyTwice_ThrowsOrIsNoOp`（依 `ux_` 複合 unique 行為決定，先傾向「拋例外」讓上層處理冪等）

### Service（per-class fixture）

`tests/Bee.ObjectCaching.UnitTests/CompanyInfoServiceTests.cs`：
- `Get_CacheHit_ReturnsCachedValue`（不打 DB）
- `Get_CacheMiss_LoadsFromRepositoryAndFillsCache`（注入 fake repo）
- `Get_CacheMissAndNotInDb_ReturnsNull`

### BO（per-class fixture + 5 DB）

`tests/Bee.Business.UnitTests/System/EnterCompanyPermissionTests.cs`：
- `EnterCompany_UserHasAccess_Succeeds_SetsSessionCompanyId`
- `EnterCompany_UserNoAccess_ThrowsCompanyAccessDenied`
- `EnterCompany_CompanyDisabled_ThrowsCompanyAccessDenied`
- `EnterCompany_CompanyNotExist_ThrowsCompanyAccessDenied`（三個失敗情境驗證錯誤訊息一致）

## 任務拆解（建議實作順序）

1. **PG 方言修訂**：`PgSchemaSyntax.GetDefaultExpression` Boolean case + `PgTableSchemaProvider.ParseDBDefaultValue` boolean case；補單元測試確認 input `"1"` → `TRUE`、DB 回 `"true"` → 規範化 `"1"`
2. **新增 TableSchema XML**（st_company / st_user_company）+ DbCategorySettings.xml 更新；本機跑 `./test.sh tests/Bee.Db.UnitTests/...` 確認 schema 在 5 DB 都能建出（boolean default 是關鍵驗證點）
3. **SharedDatabaseState seed**：建 seed company + seed 對照
4. **`ICompanyRepository` + `CompanyRepository`** + 單元測試
5. **`IUserCompanyRepository` + `UserCompanyRepository`** + 單元測試
6. **`ISystemRepositoryFactory` 擴充** + factory 實作 + DI wiring
7. **`CompanyInfoService` 注入 `ICompanyRepository`、補 DB fallback** + 服務測試
8. **`SystemBusinessObject.EnterCompany` 接入權限驗證** + BO 測試
9. **5 DB 完整跑一輪**確認沒有 dialect 漏洞

## 相關文件 / 記憶

- `docs/architecture-overview.md` — multi-tenancy 章節
- `project_multi_tenancy_model` 記憶 — 多公司共用模型
- `project_three_db_categories` 記憶 — common / company / log 三類
- `feedback_bo_must_use_repository` — BO 不直接存取 DB
- 既有 plan：`plan-bo-repo-db-routing`（DbScope / Router）

## 已確認決議（plan 草案討論結果）

- **R1**：Repository 拆兩支（D2 採選 A）
- **R2**：user_rowid 採即時 JOIN，不擴張 SessionInfo（D3 採選 A）
- **R3**：`IUserCompanyRepository` 只公開 `HasAccess`；`Grant/Revoke` 寫進實作但宣告為 `internal`（或不放上介面）。測試 seed 走 `SharedDatabaseState` 原生 SQL，沿用 st_user seed 風格。未來做 CompanyAdmin BO 時再公開
- **R4**：`enabled` 欄位 DB default `true`，TableSchema XML 內以 `DefaultValue="1"` 宣告。各方言落地：
  - SQL Server / SQLite / MySQL / Oracle：既有 `DEFAULT 1` 拼接已可用（型別 `BIT` / `BOOLEAN`(SQLite) / `TINYINT(1)` / `NUMBER(1)` 均接受 `1` literal）
  - **PostgreSQL 例外**：`BOOLEAN` 欄位不接受 `1`/`0` 整數 literal，必須是 `TRUE`/`FALSE`。本 plan 一併修訂 `PgSchemaSyntax.GetDefaultExpression`（Boolean case：`"1"` → `TRUE`、`"0"` → `FALSE`）與 `PgTableSchemaProvider.ParseDBDefaultValue`（boolean dataType：DB 回 `"true"`/`"false"` 規範化為 `"1"`/`"0"`），保持「規範形式 = `"1"`/`"0"`、PG 拼接層翻譯」
  - 規範形式統一：跨方言用戶層 `DbField.DefaultValue` 一律寫 `"1"` 或 `"0"`，由各方言 SchemaSyntax 負責翻譯到 native literal
- **R5**：失敗訊息延用既有字串 `"Company access denied."`（D4）
- **R6**：本次 schema 維持精簡欄位集（不加 `display_order` / `note` / `sys_update_time`），日後需要時走 `ITableAlterCommandBuilder` schema diff rebuild

## 未來再做（範圍外）

- 生產環境 `st_company` bootstrap 機制（CLI / 部署腳本 / 框架啟動時建 default company）
- `Company` 維護畫面（FormSchema + FormBO CRUD）
- `UserCompany` 維護畫面或對照表 admin BO（屆時把 `Grant/Revoke` 公開）
- role-based 權限模型
