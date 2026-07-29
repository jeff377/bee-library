# 計畫：Database 快取改為經 ICacheDataSourceProvider 自資料庫載入

**狀態：✅ 已完成（2026-07-29）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `ICacheDataSourceProvider` 擴充三個取數方法 + `CacheDataSourceProvider` 實作 | ✅ 已完成（2026-07-29） |
| 2 | 三個快取實作 `CreateInstance` read-through（`Func<T>` 延遲解析）+ 三個 service 收斂 | ✅ 已完成（2026-07-29） |

> `SessionInfoCache` 的 read-through 原列為本 plan 的階段 3 / 4，因範圍實為 `st_session`
> 持久化（種子寫入、金鑰導出、`CreateSession` 重構、過期清理）而非快取載入，已抽離為
> [plan-session-persistence.md](plan-session-persistence.md)。

## 執行結果

建置 0 警告 0 錯誤；全測試 4154 通過、1 略過（既有 `[Fact(Skip)]` 佔位，與本次無關）。

與原設計的差異：

- **快取的資料來源建構式改為 `internal`**。原訂新增 public 多載，但 `RS0026` / `RS0027`
  不允許「兩個 public 多載都帶選擇性參數」，且要求帶選擇性參數者必須是參數最多的多載。
  既有 `XxxCache(string cachePrefix = "")` 已 shipped 不宜改簽章，故新建構式改 `internal`
  ——`CacheContainerService` 同組件內可用，**三個快取的公開表面因此完全未變動**。
- **`CacheDataSourceProvider` 注入個別 repository**（`ISessionRepository` / `ICompanyRepository` /
  `IRolePermissionRepository` / `IDepartmentRepository`）而非 `ISystemRepositoryFactory`。
  連帶補上 `ISessionRepository` 的個別 DI 註冊（原本只能經 factory 取得）。
- **D1 維持原建議**：兩個 snapshot 方法各自多讀一次 `st_company`，未走 `CompanyInfoCache`。
- `Bee.ObjectCaching` 對 `Bee.Repository.Abstractions` 的專案參考**保留**——
  `EmployeeContextResolver` 仍在使用。

**已完成的後續重構（2026-07-29，同日）**：原本每新增一個系統 repository 要登記三處
——工廠方法、DI 個別註冊、消費端 ctor 參數——`CacheDataSourceProvider` 的建構式因此會
隨 DB-sourced 快取增加而持續變寬。改為比照 BO 層既有慣例收斂：

- 判準來自 `IFormRepositoryFactory` / `IBusinessObjectFactory`：**身分是資料（ProgId），
  不是型別；DI 只放工廠，不放實例**。一千個 ERP 功能共用同一個 `DataFormRepository`
  類別，`FormBusinessObject` 只持有工廠、每次操作才 `CreateDataFormRepository(ProgId)`，
  DI 註冊數不隨功能數增長。
- `CacheDataSourceProvider`、`EmployeeContextResolver` 改為持有 `ISystemRepositoryFactory`，
  各方法內按需 `CreateXxxRepository()`；`EnterCompany` 的 `IUserCompanyRepository` 同改。
- `BeeFrameworkServiceCollectionExtensions` 中 7 行個別 repository 註冊全數移除，
  只留 `ISystemRepositoryFactory`。新增系統 repository 從此只需動工廠介面一處。
- `CacheDataSourceProvider` 的建構式因此還原為 `PublicAPI.Shipped.txt` 中原本的簽章，
  該類別的 breaking change 消失；`EmployeeContextResolver` 建構式則為新的 breaking change
  （已登錄 `PublicAPI.Unshipped.txt`）。
- 副作用：外部 app 若原本 `sp.GetRequiredService<ICompanyRepository>()` 需改從工廠取得。
  已確認 `apps/` `samples/` `tools/` 完全未引用個別 repository。

**尚未處理**：`ICacheDataSourceProvider` 新增三個方法屬 breaking change（已登錄
`PublicAPI.Unshipped.txt`），發版時需在 CHANGELOG 標示。

## 背景

`src/Bee.ObjectCaching/Database/` 下四個快取的 `CreateInstance` 全部回傳 `null`：

| 快取 | 現況 |
|------|------|
| `CompanyInfoCache` | 註解標 "not yet implemented"；由 `CompanyInfoService` 讀 `st_company` 後 `Set()` 回填 |
| `CompanyRolePermissionsCache` | 註解明示 "no self-loading"；由 `RolePermissionService` 回填 |
| `DepartmentTreeCache` | 同上，由 `DepartmentTreeService` 回填 |
| `SessionInfoCache` | 僅由 Login 經 `Set()` 填入；覆寫 `GetNegativePolicy` 關閉負快取（不在本 plan 範圍，見 [plan-session-persistence.md](plan-session-persistence.md)） |

Define 側的快取早已是「注入介面 + `CreateInstance` 自行載入」的形狀，例如
[TableSchemaCache.cs](../../src/Bee.ObjectCaching/Define/TableSchemaCache.cs) 的 `CreateInstance`
直接呼叫注入的 `IDefineStorage`。Database 側缺的是**讀取邏輯放錯層**：三個 service 各自手刻了
一份 read-through（[CompanyInfoService.cs](../../src/Bee.ObjectCaching/Services/CompanyInfoService.cs)、
[RolePermissionService.cs](../../src/Bee.ObjectCaching/Services/RolePermissionService.cs)、
[DepartmentTreeService.cs](../../src/Bee.ObjectCaching/Services/DepartmentTreeService.cs)），
base class 的 `CreateInstance` 反而掛空檔。

**接縫已經存在。** [ICacheDataSourceProvider.cs](../../src/Bee.Definition/ICacheDataSourceProvider.cs)
定義於 `Bee.Definition`，由 [CacheDataSourceProvider.cs](../../src/Bee.Business/Providers/CacheDataSourceProvider.cs)
實作，並以 `CreateConfigurableService` 註冊（app 可抽換）。介面目前只有 `GetSessionUser`，
且**全 repo 無呼叫端**——是先行預留、尚未接上的接縫。本計畫即把它接上。

## 目標

1. `CreateInstance` 成為唯一載入入口，資料來源一律經 `ICacheDataSourceProvider` 取得。
2. 快取語意回歸 base class：miss → `CreateInstance` → 依 `GetPolicy` 寫入；負快取與
   cache-notify 失效由 base 統一處理，service 不再重複實作。
3. 三個 service 保留（分層邊界，見下），`Get` 收斂為單行委派。
4. 公開 API 不破壞：既有建構式與呼叫端零改動；未提供 provider 時行為與今日逐字一致。

## 關鍵限制

### C1. DI 循環相依 —— 必須延遲解析

`CacheContainerService` 的建構式**不能**直接要求 `ICacheDataSourceProvider`：

```
ICacheContainer → ICacheDataSourceProvider → ISystemRepositoryFactory → IDefineAccess → ICacheContainer
```

環的閉合點在 [BeeFrameworkServiceCollectionExtensions.cs:101](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs)
——`IDefineAccess` 註冊時明確解析 `ICacheContainer`（`CacheDefineAccess` 以它做讀寫失效）。
現有三個 service 之所以扛住載入邏輯，正是因為它們註冊在較後段、於自身建構時才解析相依，天然避開環。

**解法**：快取持有 `Func<ICacheDataSourceProvider>`，第一次 cache miss 才呼叫。
此時 `ICacheContainer` singleton 早已建構完成，解析不再回頭觸發自身建構。

> 本 repo 已有同型前例：同檔 `IDbAccessFactory` 註冊處以 lazy writer resolver 打斷
> `IDbAccessFactory → IAuditLogWriter → AuditLogDbSink → IDbAccessFactory`。

### C2. 介面方法一律回傳 domain 型別，不得回傳 repository 介面

`ICacheDataSourceProvider` 位於 `Bee.Definition`，而 `Bee.Repository.Abstractions`
**反向相依** `Bee.Definition`（`ICompanyRepository.GetById` 回傳 `CompanyInfo`）。
若介面方法回傳 `ICompanyRepository` 之類，`Bee.Definition` 就得引用 `Bee.Repository.Abstractions`
→ **專案循環參考，編譯不過**。

回傳 `CompanyInfo`（`Bee.Definition.Identity`）、`CompanyRolePermissions`（同）、
`DepartmentTree`（`Bee.Definition.Organization`）則完全無此問題，且
`Bee.ObjectCaching` 已引用 `Bee.Definition`，零新增專案參考。

**附帶效益**：companyId → `CompanyDatabaseId` 的解析收進 provider 內部（它持有
`ISystemRepositoryFactory`），快取端不需要再取得 `ICompanyInfoService` 或 sibling 快取，
`CreateInstance` 因此可以是一行。

### C3. 公開 API 表面

`Bee.ObjectCaching` 有 `PublicAPI.Shipped.txt`，`CacheContainerService` 兩個建構式均已列入；
`new CacheContainerService(...)` 有 13 處呼叫端（含 `CacheDefineAccess`、`CacheContainerProvider`
與 11 處測試）。故一律**新增多載**、不改既有簽章。
`ICacheDataSourceProvider` 為公開介面，新增方法屬 breaking change（外部自訂實作會編譯失敗），
需登錄 `PublicAPI.Unshipped.txt` 並於 CHANGELOG 標示。

## 設計

### 階段 1：擴充 `ICacheDataSourceProvider`

```csharp
public interface ICacheDataSourceProvider
{
    SessionUser? GetSessionUser(Guid accessToken);          // 既有，本輪不動

    CompanyInfo? GetCompanyInfo(string companyId);
    CompanyRolePermissions? GetCompanyRolePermissions(string companyId);
    DepartmentTree? GetDepartmentTree(string companyId);
}
```

`CacheDataSourceProvider` 實作（維持既有 `ISystemRepositoryFactory` 相依，每次呼叫才建 repository）：

```csharp
public CompanyInfo? GetCompanyInfo(string companyId)
    => _systemFactory.CreateCompanyRepository().GetById(companyId);

public DepartmentTree? GetDepartmentTree(string companyId)
{
    var company = GetCompanyInfo(companyId);
    if (company == null) { return null; }

    var rows = _systemFactory.CreateDepartmentRepository().GetDepartments(company.CompanyDatabaseId);
    return new DepartmentTree(companyId, rows);
}

public CompanyRolePermissions? GetCompanyRolePermissions(string companyId)
{
    var company = GetCompanyInfo(companyId);
    if (company == null) { return null; }

    var repo = _systemFactory.CreateRolePermissionRepository();
    var databaseId = company.CompanyDatabaseId;
    return new CompanyRolePermissions(companyId, repo.GetRoleGrants(databaseId), repo.GetUserRoles(databaseId));
}
```

> **待確認 D1**：上面兩個 snapshot 方法各自多讀一次 `st_company`（未經 `CompanyInfoCache`）。
> 替代做法是讓 provider 改注入 `ICompanyInfoService`（走快取）——在 C1 的延遲解析下不會成環。
> 但 snapshot miss 本身罕見（per company、20 分鐘 sliding），多一次讀取影響有限，
> 且直接用 repository 可讓 provider 不相依快取服務、職責更單純。**建議先用上方寫法**，
> 實測有需要再換。

### 階段 2：三個快取 read-through + service 收斂

各快取新增一個帶 provider 的建構式多載（既有建構式保留並傳 `null`）：

```csharp
public class CompanyInfoCache : KeyObjectCache<CompanyInfo>
{
    private readonly Func<ICacheDataSourceProvider>? _dataSource;

    public CompanyInfoCache(string cachePrefix = "") : this(null, cachePrefix) { }

    /// <param name="dataSource">
    /// Lazy accessor for the cache data source.
    /// WARNING: this must stay a factory. Resolving the provider while `CacheContainerService`
    /// is under construction closes the dependency cycle `ICacheContainer` →
    /// `ICacheDataSourceProvider` → `ISystemRepositoryFactory` → `IDefineAccess` →
    /// `ICacheContainer`. Deferring to the first cache miss breaks it, because the container
    /// singleton is fully constructed by then.
    /// </param>
    public CompanyInfoCache(Func<ICacheDataSourceProvider>? dataSource, string cachePrefix = "")
        : base(cachePrefix)
    {
        _dataSource = dataSource;
    }

    protected override CompanyInfo? CreateInstance(string key)
        => _dataSource?.Invoke().GetCompanyInfo(key);
}
```

`CompanyRolePermissionsCache` / `DepartmentTreeCache` 同形狀，各自呼叫對應方法。

`CacheContainerService` 加一個多載，既有兩個建構式委派並傳 `null`（＝維持今日行為，呼叫端零改動）：

```csharp
public CacheContainerService(IDefineStorage storage, PathOptions paths, string cachePrefix)
    : this(storage, paths, cachePrefix, dataSource: null) { }

public CacheContainerService(IDefineStorage storage, PathOptions paths, string cachePrefix,
    Func<ICacheDataSourceProvider>? dataSource)
{
    // ...
    CompanyInfo = new CompanyInfoCache(dataSource, CachePrefix);
    CompanyRolePermissions = new CompanyRolePermissionsCache(dataSource, CachePrefix);
    DepartmentTree = new DepartmentTreeCache(dataSource, CachePrefix);
    SessionInfo = new SessionInfoCache(CachePrefix);   // 不在本 plan 範圍
}
```

DI 註冊改傳延遲工廠：

```csharp
services.AddSingleton<ICacheContainer>(sp =>
    new CacheContainerService(
        sp.GetRequiredService<IDefineStorage>(),
        sp.GetRequiredService<PathOptions>(),
        string.Empty,
        sp.GetRequiredService<ICacheDataSourceProvider>));   // method group → Func<T>，不即時解析
```

三個 service 的 `Get` 收斂為單行委派，並移除已無用的 repository 建構式參數：

```csharp
public CompanyInfo? Get(string companyId) => _cache.CompanyInfo.Get(companyId);
public CompanyRolePermissions? Get(string companyId) => _cache.CompanyRolePermissions.Get(companyId);
public DepartmentTree? Get(string companyId) => _cache.DepartmentTree.Get(companyId);
```

三個 service 建構式簽章改變（少一個參數），需同步 `BeeFrameworkServiceCollectionExtensions`
的三處註冊與相關測試。

cache-notify 失效鏈不變：base 的 `BuildPolicy` 仍套 `ChangeNotifyKey = CacheGroup + ":" + key`，
即現行 `"CompanyInfo:{companyId}"` / `"DepartmentTree:{companyId}"` /
`"CompanyRolePermissions:{companyId}"`，寫入端合約零改動。

**保留 service 層的理由**：`ICompanyInfoService` / `IRolePermissionService` / `IDepartmentTreeService`
定義在 `Bee.Definition`，是讓 `Bee.Business`、`Bee.Repository` 等上層不必相依 `Bee.ObjectCaching`
的分層邊界（`Bee.Business` 目前確實未引用 `Bee.ObjectCaching`），不屬 code-style 要消除的純 facade；
且三者都還有 `Set` / `Remove`。呼叫端（`BeeContext`、`AccessTokenValidator`、`ScopeResolver`、
`RepositoryDatabaseRouter`）零改動。

**附帶修掉一處浪費**：今日 `CompanyInfoService.Get` 的 miss 路徑會先讓 base 寫入一個負快取
marker（`CreateInstance` 回 null → 5 分鐘 absolute），隨即被 service 的 `Set()` 覆寫。
改為 read-through 後直接寫入正快取，不再有這次多餘寫入。

**收尾檢查**：三個 service 移除 repository 相依後，確認 `Bee.ObjectCaching` 是否仍需
`Bee.Repository.Abstractions` 專案參考；若已無使用則一併移除。

## 測試

| 範圍 | 測試 |
|------|------|
| provider | 三個新方法各自：company 不存在 → `null`；正常路徑組出結果；驗證傳給 repository 的 `databaseId` 等於 `CompanyDatabaseId` |
| 快取 | 有／無 `Func<ICacheDataSourceProvider>` 兩路徑；miss 觸發 provider 恰一次；第二次 `Get` 命中快取不再觸發；provider 回 `null` 時走負快取 |
| DI | `AddBeeFramework` 後解析 `ICacheContainer` 與 `ICacheDataSourceProvider` 均成功（C1 是否成立的唯一硬證據） |

既有測試須確認不回歸：`CompanyInfoServiceTests`、`Services/DepartmentTreeServiceTests`、
`BeeFrameworkServiceResolutionTests`、`DbDefineCacheInvalidationTests`（跨節點失效）。

`PublicAPI.Unshipped.txt` 需登錄：`ICacheDataSourceProvider` 三個新方法、三個快取的新建構式多載、
`CacheContainerService` 新多載。

## 風險

| 風險 | 因應 |
|------|------|
| DI 循環相依（C1） | 一律 `Func<T>` 延遲解析；以實際 build DI container 的測試把關 |
| `ICacheDataSourceProvider` 新增方法為 breaking change | 登錄 `PublicAPI.Unshipped.txt`；CHANGELOG 標示；評估是否提供 default implementation 降低衝擊 |
| 公開 API 破壞（快取／容器） | 只新增多載，既有簽章與 13 處呼叫端不動 |
| service 建構式簽章變更 | 同步 DI 三處註冊與相關測試；介面本身不變，上層呼叫端零影響 |
| 未提供 provider 的部署行為改變 | `dataSource` 為 `null` 時 `CreateInstance` 維持回 `null`，與今日逐字一致 |

## 執行順序

階段 1 → 2 連續執行。兩者對外可觀察行為相同（僅載入位置換層），屬純內部重構。
兩階段已於 2026-07-29 完成並上線（CI 綠）。

後續的 `SessionInfoCache` read-through 見 [plan-session-persistence.md](plan-session-persistence.md)。
