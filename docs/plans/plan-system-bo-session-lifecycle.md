# 計畫：SystemBO Session 生命週期方法（Login / EnterCompany / LeaveCompany / Logout）

**狀態：✅ 已完成（2026-05-15）**

> **前置 plan**：[plan-keyobjectcache-negative-cache.md](plan-keyobjectcache-negative-cache.md)
> 本 plan 的 P2（`CompanyInfoCache` 建立）依賴 `KeyObjectCache<T>` 負向快取支援。
> 前置 plan 完成後才進入本 plan 的 P2。

## 背景

目前 `SystemBusinessObject` 已有 `Login` 方法，登入成功後建立 `SessionInfo` 並快取至 `ISessionInfoService`。但缺少：

1. **登入公司**的概念——使用者帳密驗證通過後，必須再選定一家公司才能執行業務操作（FormBO / ReportBO 等需要 company context 的 BO）
2. **公司切換**——多公司權限的使用者需要在 session 期間切換公司
3. **登出方法**——主動銷毀 session 的 server-side API

本計畫補齊 SystemBO 的 session 生命週期：

```
Login(account, password)        ←→  Logout()
EnterCompany(companyId)         ←→  LeaveCompany()
```

四個方法湊成兩組對稱對，職責切乾淨：

```
合法路徑：
  Login → EnterCompany → [業務呼叫] → LeaveCompany → EnterCompany(其他)... → Logout
  Login → EnterCompany → Logout（隱含 LeaveCompany）
  Login → Logout（從未進入公司也能直接登出）

非法路徑（應回專屬錯誤碼）：
  未 Login → EnterCompany / LeaveCompany / 任何業務呼叫
  未 EnterCompany → company 類 BO 呼叫
```

## 目標

- 新增 `EnterCompany` / `LeaveCompany` / `Logout` 三個 SystemBO 方法
- 建立 `CompanyInfo` 領域型別，承載公司中繼資料（CompanyDatabaseId、LogDatabaseId 等）
- 擴充 `SessionInfo` 加上 `CompanyId` 欄位
- 建立 `ICompanyInfoService` + cache 層，類比 `ISessionInfoService` 模式
- 定義「未進入公司」狀態下呼叫 company 類 BO 的錯誤行為（攔截 + 錯誤碼）
- 補齊 `SystemActions` 常數、wire 層 DTO、契約介面、round-trip 測試

## 非目標（明確排除）

- **不**在本計畫實作 Repository 自動注入 `sys_company_rowid` 的攔截機制——那是 Repository 層的獨立工作，列為後續計畫
- **不**處理 `CompanyInfo` 從哪裡讀（共用 DB 的 `ft_company` 表？fixture XML？）——本計畫先定義介面，提供 stub 實作；實際資料來源由後續 plan 接手
- **不**處理「使用者-公司權限對映表」的資料模型——**本計畫不做公司權限驗證**，`EnterCompany` 只做「公司是否存在於 CompanyInfo cache」的存在性檢查。完整權限驗證（含 user-company schema、角色、可見性規則）另起獨立 plan 深度討論

## 已確認的設計決議

### D1：CompanyInfo 位置

`src/Bee.Definition/Identity/CompanyInfo.cs`，命名空間 `Bee.Definition.Identity`，與 `SessionInfo.cs` 同層級（identity 領域內聚）。

### D2：CompanyInfo 初版欄位（4 個）

```csharp
namespace Bee.Definition.Identity;

public class CompanyInfo : IKeyObject
{
    public string CompanyId { get; set; }          // 公司代碼（key）
    public string CompanyName { get; set; }
    public string CompanyDatabaseId { get; set; }  // 對映 DatabaseSettings 的 DatabaseId（company 類 DB）
    public string LogDatabaseId { get; set; }      // 對映 DatabaseSettings 的 DatabaseId（log 類 DB）
    public string Key => CompanyId;
}
```

IsActive / Culture / TimeZone / 有效期間等欄位後續按需擴充，不在本計畫範圍。

### D3：CompanyInfo 快取策略——獨立 company-level

新增 `ICompanyInfoService` + `CompanyInfoCache`，類比 `ISessionInfoService` 模式，keyed by CompanyId，跨 session 共享。

`SessionInfo.CompanyId` 只存 ID（型別 `string?`，見 D6），需要時呼叫 `ICompanyInfoService.Get(companyId)` 取得完整資訊；公司資料變更時 `Remove(companyId)` 即時對所有 session 生效。

### D4：EnterCompany 採直接覆寫模型

- 切換公司 = 呼叫 `EnterCompany(newId)` 一步完成（**不需**先 Leave）
- `SessionInfo.CompanyId` 已有舊值且與新值不同 → 直接覆寫，不報錯
- `SessionInfo.CompanyId` 已有舊值且與新值相同 → idempotent，不報錯
- 每次呼叫仍須做存在性檢查（依 D5，本計畫**不做**權限驗證）

### D5：本計畫**不做**公司權限驗證

`EnterCompany` 只做「companyId 是否能在 `ICompanyInfoService` 取到」的存在性檢查，不驗證該 user 是否有權進入該 company。

理由：權限模型（user-company 對映 schema、角色、可見性規則）需要獨立深度討論，不應卡住本 session lifecycle 計畫。

擴展點規劃：未來權限 plan 接手時，建議在 `EnterCompany` 內加入權限驗證步驟（介於存在性檢查與寫入 SessionInfo 之間），而非攔截器模式——讓權限失敗能用同一條錯誤路徑（D7 的 `CompanyAccessDenied`）回應。

本計畫實作時在 `EnterCompany` 的權限驗證點留下明確 TODO 註解，引用後續 plan。

### D6：`SessionInfo.CompanyId` 型別為 `string?`（nullable）

- `null` = 未進入公司
- 非 null = 已進入該 ID 對應的公司
- 不用空字串作為「未進入」訊號（避免與「公司 ID = ''」的誤解空間）
- 與 `SessionInfo` 既有非 nullable string 欄位（UserId / UserName / Culture / TimeZone）風格不同，但專案已全域啟用 nullable reference types，加 `?` annotation 不是問題

### D7：Logout 隱含 LeaveCompany

`Logout` 內部先做 `LeaveCompany` 的清理（清 `SessionInfo.CompanyId`），再銷毀整個 session。呼叫端只需呼叫 `Logout` 一次，不必先 Leave 再 Logout。

### D8：錯誤碼定義

| 情境 | 錯誤碼 | HTTP status | 備註 |
|------|-------|------------|------|
| 未 Login 呼叫任何需驗證方法 | `Unauthorized`（既有） | 401 | 既有機制不動 |
| 已 Login 但未 EnterCompany，呼叫 company 類 BO | `CompanyNotEntered`（新增） | 409 Conflict | session 狀態錯誤，非權限問題 |
| EnterCompany 時 companyId 不存在 **或** user 對該 company 無權限 | `CompanyAccessDenied`（新增） | 403 | **合併**兩種狀態：防止 user enumeration（attacker 無法靠錯誤碼差異探測哪些 companyId 存在） |
| LeaveCompany / Logout 對未進入狀態呼叫 | idempotent，回 success | 200 | 不報錯 |

「無權限」與「不存在」共用 `CompanyAccessDenied`——本計畫雖然不做權限驗證，但錯誤碼定義已預留位置，未來權限 plan 接手時直接套用，不需要再改錯誤碼結構。

新錯誤碼加在何處：實作時確認專案既有 error code 集中定義位置（可能在 `Bee.Api.Core/Errors/` 或 `Bee.Definition/`），新增到同處。

## 方法簽名與 wire 結構

### 1. Login（既有，本計畫不改）

```csharp
public LoginResult Login(LoginArgs args);
```

僅在 `ISystemBusinessObject` 介面**補上 Login 簽名**（survey 顯示介面目前沒有明確宣告 Login，依賴抽象基底）。

### 2. EnterCompany（新增）

```csharp
[ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
public EnterCompanyResult EnterCompany(EnterCompanyArgs args);
```

**EnterCompanyArgs**：
```csharp
public class EnterCompanyArgs : IEnterCompanyRequest
{
    public string CompanyId { get; set; }
}
```

**EnterCompanyResult**：
```csharp
public class EnterCompanyResult : IEnterCompanyResponse
{
    public CompanyInfo Company { get; set; }  // 回傳完整 CompanyInfo（前端可用來顯示）
}
```

**行為**：
1. 從 `ISessionInfoService` 取目前 session（依 `ApiContext.AccessToken`）；找不到 → `Unauthorized`
2. 從 `ICompanyInfoService` 取 `CompanyInfo`（存在性檢查）；找不到 → `CompanyAccessDenied`（依 D8，與權限不足共用同一錯誤碼）
3. **權限驗證點（後續 plan 接手）**：本計畫此處留 TODO，未來填入 user-company 權限驗證；驗證失敗同樣回 `CompanyAccessDenied`
4. 寫入 `session.CompanyId = args.CompanyId`，呼叫 `ISessionInfoService.Set(session)` 更新 cache
5. 回傳 `EnterCompanyResult { Company = companyInfo }`

### 3. LeaveCompany（新增）

```csharp
[ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
public LeaveCompanyResult LeaveCompany(LeaveCompanyArgs args);
```

**LeaveCompanyArgs**：空 args（保留結構以便未來擴充，例如「軟離開保留草稿」）

**LeaveCompanyResult**：空 result 或回傳前一個 CompanyId

**行為**：
1. 取 session
2. 若 `session.CompanyId` 非空，清空（`null`）並更新 cache
3. idempotent：本來就空也回 success

### 4. Logout（新增）

```csharp
[ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
public LogoutResult Logout(LogoutArgs args);
```

**LogoutArgs / LogoutResult**：均為空。

**行為**：
1. 取 session
2. （內部）若 `session.CompanyId` 非空，先清掉
3. `ISessionInfoService.Remove(session.AccessToken)` 銷毀整個 session
4. idempotent：找不到 session 也回 success（避免攻擊者用回傳值探測）

## 影響檔案清單

依 `bee-add-bo-method` skill 的 7~8 檔案流程，三支新方法各需動到：

本計畫**不再**包含 `ICompanyAccessValidator`——權限驗證另起 plan，本計畫只在 `EnterCompany` 內留擴展點 TODO（見 D5）。

### 共用變更（一次性）

| 檔案 | 變更 |
|------|------|
| `src/Bee.Definition/Identity/SessionInfo.cs` | 加 `CompanyId` 欄位（`string?`，見 D6） |
| `src/Bee.Definition/Identity/CompanyInfo.cs` | **新增**：`CompanyInfo` 類別（見 D2 結構） |
| `src/Bee.Definition/SystemActions.cs` | 加 `EnterCompany` / `LeaveCompany` / `Logout` 常數 |
| `src/Bee.ObjectCaching/Services/CompanyInfoService.cs` | **新增**：`ICompanyInfoService` 介面 + 實作 |
| `src/Bee.ObjectCaching/Database/CompanyInfoCache.cs` | **新增**：cache 結構 |
| `src/Bee.ObjectCaching/Abstractions/ICacheContainer.cs` | 加 `CompanyInfo` 屬性 |
| `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` | 註冊 `ICompanyInfoService` |
| Error code 集中定義位置（實作時定位） | 加 `CompanyNotEntered`、`CompanyAccessDenied` 兩個錯誤碼 |

### 每支新方法各自需要

`EnterCompany` 為例：

| 層 | 檔案 |
|----|------|
| Contract | `src/Bee.Api.Contracts/System/IEnterCompanyRequest.cs`、`IEnterCompanyResponse.cs` |
| Args/Result（wire DTO） | `src/Bee.Business/System/EnterCompanyArgs.cs`、`EnterCompanyResult.cs` |
| BO 實作 | `src/Bee.Business/System/SystemBusinessObject.cs` 加 method |
| BO 介面 | `src/Bee.Business/System/ISystemBusinessObject.cs` 加簽名 |
| Registry 註冊 | `src/Bee.Api.Core/Registry/ApiContractRegistry.cs`（若需要） |
| Client | `src/Bee.Api.Client/SystemApiConnector.cs` 加 client wrapper |
| 測試 | `tests/Bee.Business.UnitTests/System/SystemBusinessObjectTests.cs` round-trip 測試 |

`LeaveCompany` / `Logout` 同模式，加總約 **24~28 個檔案異動**。

## Repository 自動注入 sys_company_rowid（後續計畫，僅佔位）

**本計畫不實作**，但設計上需確認以下接點：

- `SessionInfo.CompanyId` 必須能被 Repository 層讀取（透過 `ApiContext` 或 DI 注入的 `ISessionContext`）
- FormSchema 應有方法判斷某張表是否屬於 company 類（依其 CategoryId）
- 自動注入發生在 `IDataFormRepository` 之上而非 `DbAccess` 之下，因為要讀 SessionInfo

詳細設計另起 `plan-repository-company-isolation.md`。

## 測試策略

**單元測試**：

- `EnterCompany_NoLogin_ThrowsUnauthorized`
- `EnterCompany_ValidCompany_UpdatesSessionCompanyId`
- `EnterCompany_SwitchToAnotherCompany_OverwritesPreviousCompanyId`
- `EnterCompany_SameCompany_IdempotentSuccess`
- `EnterCompany_NonExistentCompany_ThrowsCompanyAccessDenied`（依 D8 合併語意：不存在共用此錯誤碼）
- `LeaveCompany_NotEntered_IdempotentSuccess`
- `LeaveCompany_Entered_ClearsCompanyId`
- `Logout_WithCompany_ClearsCompanyAndSession`
- `Logout_NoLogin_IdempotentSuccess`

> 權限驗證相關測試（`NoAccess_ThrowsCompanyAccessDenied` 等）由後續權限 plan 補上。

**Round-trip 整合測試**（兩層）：

1. **BO 層 round-trip**：直接呼叫 BO method，驗 args/result 序列化
2. **Client → Server round-trip**：透過 `SystemApiConnector` 觸發完整 JSON-RPC，驗 wire payload

**全流程整合測試**：
```
Login → EnterCompany(A) → 業務呼叫 → EnterCompany(B) → LeaveCompany → EnterCompany(A) → Logout
```

## 階段拆分

| Phase | 內容 | 狀態 |
|-------|------|------|
| **P1** | 共用基礎：`SessionInfo.CompanyId` 欄位、`CompanyInfo` 類別、`SystemActions` 常數、錯誤碼定義 | ✅ 已完成（2026-05-15） |
| **P2** | CompanyInfo 快取層：`ICompanyInfoService` + 註冊 + `ICacheContainer.CompanyInfo` | ✅ 已完成（2026-05-15） |
| **P3** | `EnterCompany` 方法（含 contract / wire / BO / client / 測試）；權限驗證點留 TODO | ✅ 已完成（2026-05-15） |
| **P4** | `LeaveCompany` 方法（同上） | ✅ 已完成（2026-05-15） |
| **P5** | `Logout` 方法（同上） | ✅ 已完成（2026-05-15） |
| **P6** | 全流程整合測試（Login → EnterCompany(A) → EnterCompany(B) → LeaveCompany → EnterCompany(A) → Logout） | ✅ 已完成（2026-05-15） |

每 phase 可獨立 commit、獨立 build & test 通過。

## 後續延伸 plan（範圍外，僅佔位）

- **`plan-company-access-permission.md`**——使用者-公司權限資料模型、`EnterCompany` 權限驗證實作
- **`plan-repository-company-isolation.md`**——Repository 自動注入 `sys_company_rowid` 過濾與寫入機制

兩者均依賴本 plan 落地後的 `SessionInfo.CompanyId` 與 `CompanyInfo` 結構。
