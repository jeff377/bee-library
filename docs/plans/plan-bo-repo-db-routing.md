# 計畫：bo repo 三類資料庫存取路由

**狀態：✅ 已完成（2026-05-15）**

> **前置 plan**：[plan-system-bo-session-lifecycle.md](plan-system-bo-session-lifecycle.md)（已完成）
> 本 plan 依賴前置 plan 提供的 `SessionInfo.CompanyId`、`CompanyInfo`、`ICompanyInfoService`。

## 用語

- **bo repo**：BO 層消費的 Repository（`IDataFormRepository`、`ISessionRepository`、自訂 Report / Job repo 等的統稱）。本文一律稱「bo repo」以與 git repository 區隔。
- **`schema.CategoryId`**：schema **屬性**，標示某 FormSchema / TableSchema 在邏輯上歸屬哪一類用途。是字串，序列化到 XML、用於檔案路徑（如 `TableSchema/<categoryId>/`），值固定為 `"common"` / `"company"` / `"log"`（見 `DbCategoryIds`）。
- **`DbScope` enum**：bo repo 的**存取意圖**，型別安全列舉，固定三值：`Common` / `Company` / `Log`。代表「這個 bo repo 想存取哪一類 DB」，與 `schema.CategoryId` **概念上完全脫勾**——雖然目前值對應一致，但前者是 schema 屬性（XML 配置），後者是執行時意圖（程式碼決策）。
- **databaseId**：**物理**連線識別字，對映 `DatabaseSettings.Items` 內一筆 connection。

三層關係：

```
schema.CategoryId (string, XML 屬性)
    ↓ FormRepositoryFactory 內部轉換（字串 → enum）
DbScope (enum, 程式碼意圖)
    ↓ IRepositoryDatabaseRouter 解析（依 session 或固定規則）
databaseId (string, 物理連線)
```

**這是框架的基本機制，與是否多租戶無關**——多公司模式只是 `CompanyInfo` 內 `CompanyDatabaseId` / `LogDatabaseId` 設不同值，路由邏輯本身不變。

## 背景

Bee.NET 把 DB 邏輯上分成三類，每類映射規則固定：

| category | databaseId 取得方式 |
|---|---|
| `common` | 固定值 `"common"`（所有 session 共用同一物理 DB） |
| `company` | 依當前 session 的 `CompanyInfo.CompanyDatabaseId`（由 EnterCompany 寫入 SessionInfo.CompanyId 解析得來） |
| `log` | 依當前 session 的 `CompanyInfo.LogDatabaseId` |

這個映射規則**單租戶 / 多租戶都一樣**——差別只在 `CompanyInfo` 內 `CompanyDatabaseId` / `LogDatabaseId` 的具體值（單租戶通常設成 `"company"` / `"log"` 字串，多租戶可能多家公司指向同一字串或各自獨立 databaseId）。framework 內部不需區分這兩種部署模式。

### 目前的 gap

`FormRepositoryFactory.CreateDataFormRepository(progId)` 內：

```csharp
var databaseId = schema.CategoryId;  // 直接用 categoryId 當 databaseId
```

這個簡化只在「`CompanyInfo.CompanyDatabaseId == "company"` 且 `LogDatabaseId == "log"`」的 default 部署下意外正確；任何想拆 log 出獨立 DB、或設定不同 CompanyDatabaseId 的部署都會路由錯。**正規路由（透過 CompanyInfo）目前尚未實作**。

自訂 bo repo（Report、Job、ExecFunc AnyCode 等）情況更糟——每個 BO 方法都要自己寫「取 session → 取 CompanyInfo → 挑 CompanyDatabaseId / LogDatabaseId」的樣板碼，沒有共享邏輯。

兩個具體問題：

1. **沒有單一路由邏輯**：category → databaseId 的映射若散落，未來改動（譬如新增 cache、新增 fallback、加 telemetry）會多點漏改
2. **BO 樣板碼噪音**：每個操作 company / log DB 的 BO method 都重複「session 檢查 + 公司解析」

## 目標

建立 bo repo 三類 DB 的**單一路由**：

1. **單一解析邏輯**：`category → databaseId` 只實作一次，所有 bo repo 共用
2. **FormRepositoryFactory 內建路由**：FormSchema-driven CRUD 呼叫端不需操心 category 或 databaseId
3. **BO base helper**：自訂 bo repo 的 BO 呼叫端能用 one-liner 取 databaseId
4. **錯誤行為一致**：session 不存在、未進公司、CompanyInfo cache miss 各對映清楚的錯誤路徑

## 非目標（明確排除）

- **不**做 `sys_company_rowid` 自動注入（SELECT/UPDATE/DELETE 過濾、INSERT 自動填）—— 後續獨立 plan
- **不**動 `ISystemRepositoryFactory` 及 `ISessionRepository` 等系統 bo repo —— 它們永遠用 `common`，目前的硬編碼即正確；本 plan 不破壞
- **不**動 `IReportFormRepository` —— 目前 ctor 不取 databaseId，後續若需 report 走 company / log DB 再加
- **不**處理 ExecFunc / AnyCode 的呼叫路徑變更 —— 自訂 bo repo 模式可直接套用，但 ExecFunc handler 內部如何用 BO helper 屬另一個議題

## 設計

### D1：新增 `DbScope` enum 表達 bo repo 存取意圖

```csharp
// src/Bee.Definition/DbScope.cs
namespace Bee.Definition
{
    /// <summary>
    /// Identifies which of the three logical databases a bo repo intends to access.
    /// Independent from <c>schema.CategoryId</c> — that string is a schema attribute,
    /// while this enum is the runtime access intent used by the routing layer.
    /// </summary>
    public enum DbScope
    {
        /// <summary>Shared cross-company database (e.g. st_user, st_session).</summary>
        Common,
        /// <summary>Per-session company database, resolved via <c>CompanyInfo.CompanyDatabaseId</c>.</summary>
        Company,
        /// <summary>Per-session log database, resolved via <c>CompanyInfo.LogDatabaseId</c>.</summary>
        Log,
    }
}
```

放在 `Bee.Definition` 與 `DbCategoryIds` 同層級：兩者都是「framework 的三類 DB 概念」的表現，差別在於前者給程式碼用、後者給 XML 配置用。

### D2：抽出 `IRepositoryDatabaseRouter` 服務

單一解析邏輯封裝為 DI service，輸入 `DbScope` enum：

```csharp
// src/Bee.Repository.Abstractions/IRepositoryDatabaseRouter.cs
public interface IRepositoryDatabaseRouter
{
    /// <summary>
    /// Resolves the physical databaseId for the given access intent and session.
    /// </summary>
    /// <param name="access">The bo repo's access intent (Common / Company / Log).</param>
    /// <param name="accessToken">The current request's access token.</param>
    /// <exception cref="UnauthorizedAccessException">Session not found or has expired.</exception>
    /// <exception cref="InvalidOperationException">CompanyNotEntered, or CompanyInfo cache miss.</exception>
    string Resolve(DbScope access, Guid accessToken);
}
```

實作：

```csharp
// src/Bee.Repository/RepositoryDatabaseRouter.cs
public sealed class RepositoryDatabaseRouter : IRepositoryDatabaseRouter
{
    private readonly ISessionInfoService _sessionService;
    private readonly ICompanyInfoService _companyService;

    public RepositoryDatabaseRouter(
        ISessionInfoService sessionService,
        ICompanyInfoService companyService)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
    }

    public string Resolve(DbScope scope, Guid accessToken)
    {
        // Common / Log 跨公司共用，固定 databaseId、不需 accessToken
        switch (scope)
        {
            case DbScope.Common: return DbCategoryIds.Common;
            case DbScope.Log:    return DbCategoryIds.Log;
        }

        // Company 才需 session + CompanyInfo
        if (scope != DbScope.Company)
            throw new InvalidOperationException($"Unsupported DbScope value: {scope}.");

        var session = _sessionService.Get(accessToken)
            ?? throw new UnauthorizedAccessException("Session not found or has expired.");
        if (string.IsNullOrEmpty(session.CompanyId))
            throw new InvalidOperationException("CompanyNotEntered");

        var company = _companyService.Get(session.CompanyId)
            ?? throw new InvalidOperationException(
                "Company information unavailable; please re-enter the company.");

        return company.CompanyDatabaseId;
    }
}
```

`default` 分支保留，是 enum 演進時的防禦（若有人加新 enum 值忘記在 router 處理）。

放在 `Bee.Repository`（不是 `Bee.Business`）以便 Factory 與其他 Repository 都可消費，且不違反專案的層級規則（Repository 層可引用 Definition 的 SessionInfoService / CompanyInfoService）。

### D3：`IFormRepositoryFactory.CreateDataFormRepository` 加 `accessToken` 參數，內部轉換 `schema.CategoryId` → `DbScope`

Factory 是「字串 → enum」的轉換點：

```csharp
// 介面變更
public interface IFormRepositoryFactory
{
    IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken);
    IReportFormRepository CreateReportFormRepository(string progId);  // 不動
}

// 實作
public class FormRepositoryFactory : IFormRepositoryFactory
{
    private readonly IDefineAccess _defineAccess;
    private readonly IDbAccessFactory _dbAccessFactory;
    private readonly IDbConnectionManager _connectionManager;
    private readonly IRepositoryDatabaseRouter _router;  // 新增

    public IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(progId);
        var schema = _defineAccess.GetFormSchema(progId);
        var access = ParseCategoryId(schema.CategoryId);  // string → enum
        var databaseId = _router.Resolve(access, accessToken);
        return new DataFormRepository(progId, schema, _defineAccess,
            _dbAccessFactory, _connectionManager, databaseId);
    }

    private static DbScope ParseCategoryId(string categoryId)
        => categoryId switch
        {
            DbCategoryIds.Common  => DbScope.Common,
            DbCategoryIds.Company => DbScope.Company,
            DbCategoryIds.Log     => DbScope.Log,
            _ => throw new InvalidOperationException(
                $"Unknown schema.CategoryId '{categoryId}'.")
        };
}
```

`ParseCategoryId` 設為 `private static`，是 Factory 的內部實作細節；若未來 Factory 以外場景也需要這個轉換（譬如反序列化 XML 後做檢查），再抽出公開 utility。

**Why 不從 ambient 取 accessToken**：factory 是 singleton，accessToken 是 per-call；顯式參數最安全、不需引入 AsyncLocal 等隱式狀態。

### D4：BO base 加兩個 helper

```csharp
// src/Bee.Business/BusinessObject.cs
public abstract class BusinessObject
{
    // ... 既有 SessionInfoService / Services / etc.

    /// <summary>
    /// Resolves the databaseId for the specified access intent, using the current
    /// session's company context for Company / Log access.
    /// </summary>
    protected string ResolveDatabaseId(DbScope access)
        => Services.GetRequiredService<IRepositoryDatabaseRouter>()
                   .Resolve(access, AccessToken);

    /// <summary>
    /// Convenience wrapper around <see cref="IFormRepositoryFactory.CreateDataFormRepository"/>
    /// that auto-passes the current <see cref="AccessToken"/>.
    /// </summary>
    protected IDataFormRepository CreateDataFormRepository(string progId)
        => Services.GetRequiredService<IFormRepositoryFactory>()
                   .CreateDataFormRepository(progId, AccessToken);
}
```

呼叫端從：

```csharp
// 舊：FormSchema-driven CRUD
var factory = Services.GetRequiredService<IFormRepositoryFactory>();
var repository = factory.CreateDataFormRepository(ProgId);

// 舊：自訂 bo repo（如果有人寫過）
var session = SessionInfoService.Get(AccessToken) ?? throw ...;
var company = Services.GetRequiredService<ICompanyInfoService>().Get(session.CompanyId) ?? throw ...;
var dbId = company.CompanyDatabaseId;
var repo = new MyCustomRepo(Services.GetRequiredService<IDbAccessFactory>(), dbId);
```

簡化為：

```csharp
// 新：FormSchema-driven CRUD
var repository = CreateDataFormRepository(ProgId);  // one-liner

// 新：自訂 bo repo —— 型別安全，無 magic string
var dbId = ResolveDatabaseId(DbScope.Company);
var repo = new MyCustomRepo(Services.GetRequiredService<IDbAccessFactory>(), dbId);
```

### D5：錯誤對應與 `JsonRpcExecutor` 整合

| 情境 | 例外型別 | JSON-RPC 錯誤碼 | HTTP |
|------|---------|----------------|------|
| session 不存在 / 已過期 | `UnauthorizedAccessException` | 既有 `Unauthorized` (-32001) | 401 |
| 進入 `company` / `log` category 但 `session.CompanyId == null` | `InvalidOperationException("CompanyNotEntered")` | 既有 `CompanyNotEntered` (-32002) | 409 |
| `CompanyInfo` cache miss | `InvalidOperationException(訊息不含 CompanyId)` | 既有 `InternalError` (-32000) | 500 |
| 未知 categoryId | `InvalidOperationException` | `InternalError` | 500 |

`JsonRpcExecutor.IsUserFacingException` 已涵蓋 `UnauthorizedAccessException` / `InvalidOperationException`，會帶原訊息回 client；本 plan 不需要動 executor 邏輯。

### D6：系統 bo repo（如 `ISessionRepository`）不變

- `ISystemRepositoryFactory.CreateSessionRepository()` 等系統 bo repo 永遠用 `common` DB，現況硬編碼 `"common"` 字串即正確
- 本 plan **不**改動，保持簡潔
- 若未來需要某個系統 bo repo 走 company DB（不太可能），可同樣套用 `IRepositoryDatabaseRouter`

### D7：DI 註冊

```csharp
// src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs
services.AddSingleton<IRepositoryDatabaseRouter, RepositoryDatabaseRouter>();
// FormRepositoryFactory 既有註冊：CreateConfigurableService 已會自動 inject 新 router 依賴
```

`IRepositoryDatabaseRouter` 是純功能 service，無 host 端覆寫需求 → 直接 `AddSingleton`，不走 `CreateConfigurableService` 路徑。

## 影響檔案清單

### 介面 / 主邏輯（新增 3 檔、改 3 檔）

| 檔案 | 變更 |
|------|------|
| `src/Bee.Definition/DbScope.cs` | **新增**：enum 表達 bo repo 存取意圖（Common / Company / Log） |
| `src/Bee.Repository.Abstractions/IRepositoryDatabaseRouter.cs` | **新增**：介面，輸入 `DbScope` enum 與 `accessToken` |
| `src/Bee.Repository/RepositoryDatabaseRouter.cs` | **新增**：實作 |
| `src/Bee.Repository.Abstractions/Factories/IFormRepositoryFactory.cs` | `CreateDataFormRepository` 加 `Guid accessToken` 參數 |
| `src/Bee.Repository/Factories/FormRepositoryFactory.cs` | ctor inject router、內部 `string → DbScope` 轉換並呼叫 router |
| `src/Bee.Business/BusinessObject.cs` | 加 `ResolveDatabaseId(DbScope)` + `CreateDataFormRepository(progId)` 兩個 protected helper |

### DI 註冊（改 1 檔）

| 檔案 | 變更 |
|------|------|
| `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` | 加 `services.AddSingleton<IRepositoryDatabaseRouter, RepositoryDatabaseRouter>()` |

### 既有 caller 遷移（改 1+ 檔）

| 檔案 | 變更 |
|------|------|
| `src/Bee.Business/Form/FormBusinessObject.cs` | `GetList` 內 `factory.CreateDataFormRepository(progId)` 改用 BO helper |
| 任何其他既有 `CreateDataFormRepository(` caller | P0 探勘掃過全部 |

### 測試（新增 1~2 檔、改 1~2 檔）

| 檔案 | 變更 |
|------|------|
| `tests/Bee.Repository.UnitTests/RepositoryDatabaseRouterTests.cs` | **新增**：6 條解析路徑 + 錯誤路徑 |
| `tests/Bee.Repository.UnitTests/Factories/FormRepositoryFactoryTests.cs` | 加 / 更新測試確認 factory 走 router |
| 既有 `FormBusinessObjectGetListTests` | caller 簽名變更同步調整 |

## 階段拆分

| Phase | 內容 | 狀態 |
|-------|------|------|
| **P0** | 探勘：grep 找所有 `CreateDataFormRepository(` 與 `IDbAccessFactory.Create(` 的 caller | ✅ 已完成（2026-05-15） |
| **P1** | 新增 `DbScope` enum + `IRepositoryDatabaseRouter` 介面 + 實作 + DI 註冊 + 單元測試 | ✅ 已完成（2026-05-15） |
| **P2** | `IFormRepositoryFactory` 介面變更（加 `accessToken`）+ `FormRepositoryFactory` 內部 string → enum 轉換並呼叫 router | ✅ 已完成（2026-05-15） |
| **P3** | `BusinessObject` 加 helper（`ResolveDatabaseId(DbScope)` + `CreateDataFormRepository(progId)`） | ✅ 已完成（2026-05-15） |
| **P4** | 遷移既有 caller（`FormBusinessObject.GetList` 等），更新測試簽名 | ✅ 已完成（2026-05-15） |
| **P5** | 整合測試驗證 router 在實際 BO + DB 路徑下行為正確（既有 `FormBusinessObjectGetListTests` 走完整路徑通過 → 隱性驗證；多 CompanyInfo 場景由 `RepositoryDatabaseRouterTests` 涵蓋） | ✅ 已完成（2026-05-15） |

## 測試策略

### Router 單元測試（`RepositoryDatabaseRouterTests`）

不需 DB，stub `ISessionInfoService` + `ICompanyInfoService` 即可：

- `Resolve_Common_ReturnsCommon`（固定，不需 session）
- `Resolve_Log_ReturnsLog`（固定，不需 session；驗證 pre-session 場景如 Login 寫 log 可用）
- `Resolve_CommonAndLogWithEmptyAccessToken_ReturnsFixedDatabaseId`（驗證 Guid.Empty 也能拿到 common/log）
- `Resolve_CompanyWithSession_ReturnsCompanyDatabaseId`
- `Resolve_CompanyNoSession_ThrowsUnauthorized`
- `Resolve_CompanySessionWithoutCompanyId_ThrowsCompanyNotEntered`
- `Resolve_CompanyInfoCacheMiss_ThrowsInternalError`
- `Resolve_TwoCompaniesWithSameCompanyDatabaseId_BothReturnSameDatabaseId`（CompanyInfo 設定彈性驗證：多公司指向同 databaseId 時路由仍正確）

### Factory 單元測試（`FormRepositoryFactoryTests`）

確認 factory 的「string → enum 轉換」與「委派 router」兩部分：

- `CreateDataFormRepository_CommonCategoryId_ParsesToCommonDbScope`
- `CreateDataFormRepository_CompanyCategoryId_ParsesToCompanyDbScope`
- `CreateDataFormRepository_LogCategoryId_ParsesToLogDbScope`
- `CreateDataFormRepository_UnknownCategoryId_ThrowsInvalidOperationException`
- `CreateDataFormRepository_RouterThrows_PropagatesException`

### 整合測試

既有 `FormBusinessObjectGetListTests` 用 `SharedDbFixture` + `common_sqlite`（CategoryId 為 `common`）。改 caller 簽名後驗證：

- 既有 `[DbFact]` 測試仍通過
- 新增測試：`GetList_CompanyCategoryWithExplicitCompanyDatabaseId_RoutesToCorrectDb`（確認 schema.CategoryId=`company` 時，最終 DB 由 `CompanyInfo.CompanyDatabaseId` 決定而非 categoryId 字串）

## 已確認的設計決議

### D8：`DbScope` enum 命名與位置
- 名稱：**`DbScope`**（短、語意清楚、無命名衝突）
- 位置：`src/Bee.Definition/DbScope.cs`，與 `DbCategoryIds` 同層級

### D9：`DbScope` → databaseId 解析規則
- `DbScope.Common` → 固定 databaseId = `"common"`，**不需 accessToken**
- `DbScope.Log` → 固定 databaseId = `"log"`，**不需 accessToken**
- `DbScope.Company` → 透過 accessToken → `SessionInfo.CompanyId` → `CompanyInfo.CompanyDatabaseId`

語意對稱：只有 `Company` 需要 session + EnterCompany 後才能用；`Common` 與 `Log` pre-session 即可存取。這支援 Login / CreateSession / Logout 等需要寫 audit log 但未進公司的方法。

封存 log DB（databaseId 例：`log_2025` / `log_2024` 年份後綴）不由 `DbScope.Log` 路由——若 admin 工具需要查封存，走自訂 bo repo 顯式指定 databaseId。

連帶決定：移除 `CompanyInfo.LogDatabaseId` 欄位（dead field；多公司 log 隔離可由後續 plan-repository-company-isolation 的 `sys_company_rowid` 列級分區處理）。

### D10：`CreateDataFormRepository` 簽名一次到位
- **不保留**無 accessToken 的 overload
- 既有 caller 同步更新；本 plan 的 P4 階段負責遷移

### D11：CompanyInfo cache miss 錯誤訊息不洩漏 CompanyId
- RpcError 給呼叫端的訊息：通用「Company information unavailable; please re-enter the company.」
- Server 端 log 完整包含 accessToken / CompanyId（debug 用）
- 與 plan-system-bo-session-lifecycle D8 防 user enumeration 一致

### D12：Router 介面命名 `IRepositoryDatabaseRouter`
- 中性、對外部 NuGet 使用者友善
- 「Repository」表達消費情境、「Database」表達被路由對象、「Router」表達角色
- 路徑 `src/Bee.Repository.Abstractions/IRepositoryDatabaseRouter.cs`

---

設計全部確認。進入 P0 探勘。
