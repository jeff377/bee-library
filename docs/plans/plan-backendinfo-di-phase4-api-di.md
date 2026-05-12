# 計畫：Phase 4 — Api.Core / Api.AspNetCore DI 化與 BackendInfo 服務屬性清空

**狀態：✅ 已完成（2026-05-12）**

> 實作時調整：
> - 所有框架服務 lifetime 統一為 Singleton（plan 原訂 Scoped，但 BO factory 為 Singleton 持有 root IServiceProvider，Scoped 服務無法透過它解析；deferred 至 Phase 5/6 再依需求拆 lifetime）。
> - `Bee.Definition.BeeServiceProviderExtensions` 改為 `internal` + 透過 `InternalsVisibleTo("Bee.Business")` 暴露，避免與 MEDI 的 `GetRequiredService<T>` 擴充方法 ambiguity。
> - `Bee.Api.Client.LocalApiProvider` 透過 `ApiClientInfo.LocalServiceProvider` 靜態 holder 取 in-process IServiceProvider（Bee.Api.Client 近端模式不在 Phase 4 重構範圍，主計畫 §「範圍邊界」已說明）。
> - `BackendComponents` 無 `LoginAttemptTracker` 欄位，因此 `ILoginAttemptTracker` 不由 AddBeeFramework 註冊；應用程式 / 測試需要時自行 `services.AddSingleton<ILoginAttemptTracker, ...>()` 或透過 `TestOverrideServiceProvider` 注入。
> - 主計畫 §10.4「修改 samples」與 §10.5「更新文件」未執行 —— repo 內目前無 samples 目錄；文件部分（README / development-cookbook）保留至有實際 sample 專案時再同步。

> 本文件為主計畫 [plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) 的 **Phase 4** sub-plan，獨立可 ship。

## 背景

### 主計畫定位

Phase 4 接續 Phase 3：BO 與 Repository 已用 `IBeeContext` 注入但 BO base 內 `Services` 仍由 `BackendInfoServiceProvider`（讀 `BackendInfo.X` 靜態）轉發。本 phase 把整個後端宿主轉入真正的 `Microsoft.Extensions.DependencyInjection`：

- 引入 DI 容器，由 ASP.NET Core 在 Startup 期間註冊所有框架服務
- `BackendInfoServiceProvider` 退場，換成 request scope 的 `IServiceProvider`，BO 程式碼不變
- 一次清空 Phase 3 留下的所有 `BackendInfo.X` 服務型屬性（共 7 個）
- 完成 Phase 3 推遲的 `Bee.Db` DML helpers 重構（5 個 `*FormCommandBuilder` + `IDialectFactory.CreateFormCommandBuilder` + `SelectContextBuilder` / `TableSchemaBuilder` 注入式改造）
- 移除 `BusinessObjectFactory` 內反射建構 BO 的路徑，改用 `ActivatorUtilities.CreateInstance`

### 為什麼此 phase 是「組裝關鍵」

主計畫 §3 規定 `IServiceProvider` 只能出現在 `IDbAccessFactory` / `IBusinessObjectFactory` 兩個 factory 內，且 §「相容性策略」明確禁止「任何 static `IServiceProvider` 持有點」。Phase 4 是把這條約束落地的唯一機會：
- 製作真 DI 容器但不在 production 程式碼留下任何 `BeeRoot.Services` 之類的靜態 holder
- 同時保證 BO base class（共用 `Services` 逃生口）的呼叫方式完全不變 —— 因為 ctor 收到的 `IServiceProvider` 本來就由 factory 注入

### 主計畫對應的設計決議

- 主計畫 §3「工廠模式適用範圍」：`IServiceProvider` 僅允許出現在兩個 factory 內
- 主計畫 §4「BO 建構式統一與零 DI 註冊」：BO 子類 ctor 簽章已於 Phase 3 固定，Phase 4 不再變動
- 主計畫 §「相容性策略」：「不引入 `BackendInfo.Bind(IServiceProvider)` 或任何 static `IServiceProvider` 持有點」

### Phase 3 殘留範圍

實際盤點 `grep -rn "BackendInfo\." src/` 並排除 `BackendInfo.cs` 本身後，Phase 4 需要處理：

| 類別 | 引用點 | 處理方式 |
|------|--------|---------|
| `BackendInfo.DefineAccess`（10 處） | `Bee.Db` × 7（5 個 FormCommandBuilder + SelectContextBuilder + TableSchemaBuilder）、`Bee.Definition` × 2（comments/loaders）、其他 doc 註解 | 7 個 production 引用透過注入式 ctor 移除；comment 註解依需要更新 |
| `BackendInfo.SessionInfoService`（2 處） | `AccessTokenValidator`、`DynamicApiEncryptionKeyProvider` | 透過 ctor 注入 `ISessionInfoService` |
| `BackendInfo.ApiEncryptionKey`（1 處 + 1 處註解） | `StaticApiEncryptionKeyProvider` | 透過 ctor 注入金鑰 byte[] |
| `BackendInfo.ApiEncryptionKeyProvider`（1 處） | `JsonRpcExecutor` | 透過 ctor 注入 |
| `BackendInfo.AccessTokenValidator`（1 處） | `ApiAccessValidator.IsTokenValid` | `ApiAccessValidator.ValidateAccess` 多收一個 `IAccessTokenValidator` 參數 |
| `BackendInfo.BusinessObjectFactory`（2 處） | `JsonRpcExecutor.CreateBusinessObject` | 透過 ctor 注入 |
| `BackendInfo.LoginAttemptTracker` / `EnterpriseObjectService` / `CacheDataSourceProvider`（5 處） | `BackendInfoServiceProvider`（Phase 3 過渡） | 整個 `BackendInfoServiceProvider` 類別刪除 |
| `BackendInfo.LogOptions`（2 處） | `DbAccessLogger` | **保留**（Phase 4 不動 LogOptions；Phase 5/6 統一移至 `IOptions<T>`） |
| `BackendInfo.ConfigEncryptionKey` / `ApiEncryptionKey` / 等加密金鑰（4 個） | `DatabaseSettings` × 2、tests | **保留**（純配置型 byte[]，Phase 5/6 移至 `IOptions<T>`） |

7 個服務屬性會在本 phase 從 `BackendInfo` 移除；4 個加密金鑰 + `LogOptions` + `LogWriter` 保留至 Phase 5/6。

## 目標

1. 在 `Bee.Api.AspNetCore` 加入 `IServiceCollection.AddBeeFramework(BackendConfiguration)` extension，作為 host-agnostic 的框架註冊入口（接 `IServiceCollection`，不綁 `WebApplicationBuilder`）
2. 改造 `BusinessObjectFactory`：移除靜態 `Initialize`、`BackendInfoServiceProvider` 與反射 `Activator.CreateInstance` 路徑，改為 ctor 注入 `IServiceProvider` + `IDefineAccess` + `ISessionInfoService` + `IFormBoTypeResolver`；BO 建構走 `ActivatorUtilities.CreateInstance`
3. 改造 `JsonRpcExecutor`：ctor 注入 `IBusinessObjectFactory`、`IAccessTokenValidator`、`IApiEncryptionKeyProvider`；per-request 的 `accessToken` / `isLocalCall` 透過 ctor 末位參數注入（搭配 `ActivatorUtilities`）
4. 改造 `ApiAccessValidator`：`ValidateAccess` 多收 `IAccessTokenValidator` 參數，移除對 `BackendInfo.AccessTokenValidator` 的讀取
5. 改造 `AccessTokenValidator` / `DynamicApiEncryptionKeyProvider`：ctor 注入 `ISessionInfoService`，移除 `BackendInfo.SessionInfoService` 讀取
6. 改造 `StaticApiEncryptionKeyProvider`：ctor 注入 API 加密金鑰 byte[]（暫透過 factory delegate；Phase 5/6 接 `IOptions<T>`）
7. `ApiServiceController.HandleRequestAsync` 改為透過 `ActivatorUtilities.CreateInstance<JsonRpcExecutor>` 從 `HttpContext.RequestServices` 建構 executor
8. 完成 Phase 3 推遲的 Bee.Db DML helpers 改造（5 個 `*FormCommandBuilder` 移除 `(string progId)` ctor、`IDialectFactory.CreateFormCommandBuilder(string)` 介面方法 + 5 個實作刪除、`SelectContextBuilder` / `SelectCommandBuilder` / `TableSchemaBuilder` 接 `IDefineAccess`、`DatabaseRepository` 對應調整）
9. 刪除 `BackendInfo` 的 7 個服務屬性：`DefineAccess`、`SessionInfoService`、`ApiEncryptionKeyProvider`、`AccessTokenValidator`、`BusinessObjectFactory`、`CacheDataSourceProvider`、`EnterpriseObjectService`、`LoginAttemptTracker`
10. `BackendInfo.Initialize` 改造：內部以 `IServiceCollection` + `AddBeeFramework` 建構 `IServiceProvider` 並回傳；不再把任何服務寫入 `BackendInfo` 靜態屬性
11. `GlobalFixture` 改為持有 `IServiceProvider`；測試引用 `BackendInfo.X` 服務的點全部改為從 fixture 解析（透過新增 `BeeTestServices` shared helper）

## 非目標（本 phase 不做）

主計畫 Phase 5/6 處理範圍：

- 不改 `BackendInfo` 上的純配置欄位：`ApiEncryptionKey` / `CookieEncryptionKey` / `ConfigEncryptionKey` / `DatabaseEncryptionKey` / `LogOptions` / `LogWriter` 保留
- 不引入 `IOptions<T>` 配置 pattern（Phase 5/6 一次到位）
- 不引入 `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`
- 不重寫測試集合機制：`[Collection("Initialize")]` / `GlobalFixture` 仍存在，僅內部換成持有 `IServiceProvider` 的形式；per-class ServiceProvider 等 fixture 重寫由 Phase 5 處理
- 不撤掉 `BackendInfo.Initialize` 入口（Phase 5/6 隨測試 fixture 重寫一併移除；Phase 4 內保留為 thin shim）
- 不重新設計 `DatabaseRepository` / `ISystemRepositoryFactory` 等介面 —— 本 phase 只解決 `TableSchemaBuilder(databaseId, IDefineAccess)` 的參數調整，Repository 透過注入的 IDefineAccess 傳遞
- 不引入新的 progId → BO Type XML 對應檔（Phase 3 留下 `DefaultFormBoTypeResolver`，本 phase 不擴充）

## 設計

### 1. 套件相依與 AddBeeFramework 入口

#### 1.1 套件相依新增

| 專案 | 新增 package / project ref |
|------|---------------------------|
| `Bee.Api.AspNetCore` | `Microsoft.Extensions.DependencyInjection.Abstractions`（已隨 `Microsoft.AspNetCore.App` framework ref 提供，無需 PackageReference）<br>新增 `<ProjectReference Include="..\Bee.Business\Bee.Business.csproj" />`<br>新增 `<ProjectReference Include="..\Bee.Repository\Bee.Repository.csproj" />`<br>新增 `<ProjectReference Include="..\Bee.Db\Bee.Db.csproj" />`<br>新增 `<ProjectReference Include="..\Bee.ObjectCaching\Bee.ObjectCaching.csproj" />` |
| `Bee.Definition` | **不**加 DI 相依（保持 host-agnostic） |
| `Bee.Business` | **不**加 DI 相依（`IServiceProvider` 已是 BCL 型別） |

`Microsoft.Extensions.DependencyInjection.Abstractions` 透過 ASP.NET shared framework 提供（`Microsoft.AspNetCore.App` 已隱含包含 MEDI），不需顯式 PackageReference；非 ASP.NET host 若未來要重用 `AddBeeFramework`，可以再抽出至獨立的 `Bee.Hosting` 套件（不在本 phase 範圍）。

#### 1.2 入口 API

```csharp
namespace Bee.Api.AspNetCore
{
    public static class BeeFrameworkServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Bee.NET framework services in the DI container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The backend configuration (from SystemSettings.xml).</param>
        /// <param name="autoCreateMasterKey">Whether to auto-create the master key if missing.</param>
        public static IServiceCollection AddBeeFramework(
            this IServiceCollection services,
            BackendConfiguration configuration,
            bool autoCreateMasterKey = false)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            // 1. Decrypt security keys (stays on BackendInfo for now — Phase 5/6 moves to IOptions)
            InitializeSecurityKeys(configuration, autoCreateMasterKey);
            BackendInfo.LogOptions = configuration.LogOptions;

            // 2. IDefineStorage / IDefineAccess (singletons)
            services.AddSingleton<IDefineStorage>(_ => CreateOrDefault<IDefineStorage>(
                configuration.Components.DefineStorage, BackendDefaultTypes.DefineStorage));
            services.AddSingleton<IDefineAccess>(sp => ResolveDefineAccess(
                configuration.Components.DefineAccess, sp.GetRequiredService<IDefineStorage>()));

            // 3. ObjectCaching wire-up (runs once at first IDefineAccess resolution)
            services.AddSingleton<ICacheBootstrapper>(sp =>
                new CacheBootstrapper(sp.GetRequiredService<IDefineStorage>(), configuration));

            // 4. Bee.Db DbConnectionManager bootstrap (singleton; ensures DbConnectionManager.Initialize once)
            services.AddSingleton<IDatabaseSettingsProvider>(sp =>
                new DefineAccessDatabaseSettingsProvider(sp.GetRequiredService<IDefineAccess>()));
            services.AddSingleton<IDbConnectionManagerBootstrapper>(sp =>
                new DbConnectionManagerBootstrapper(sp.GetRequiredService<IDatabaseSettingsProvider>()));

            // 5. Core services (configurable via BackendConfiguration.Components)
            services.AddSingleton<IApiEncryptionKeyProvider>(_ => CreateOrDefault<IApiEncryptionKeyProvider>(
                configuration.Components.ApiEncryptionKeyProvider, BackendDefaultTypes.ApiEncryptionKeyProvider));
            services.AddScoped<IAccessTokenValidator>(_ => CreateOrDefault<IAccessTokenValidator>(
                configuration.Components.AccessTokenValidator, BackendDefaultTypes.AccessTokenValidator));
            services.AddScoped<ISessionInfoService>(_ => CreateOrDefault<ISessionInfoService>(
                configuration.Components.SessionInfoService, BackendDefaultTypes.SessionInfoService));
            services.AddSingleton<ICacheDataSourceProvider>(_ => CreateOrDefault<ICacheDataSourceProvider>(
                configuration.Components.CacheDataSourceProvider, BackendDefaultTypes.CacheDataSourceProvider));
            services.AddSingleton<IEnterpriseObjectService>(_ => CreateOrDefault<IEnterpriseObjectService>(
                configuration.Components.EnterpriseObjectService, BackendDefaultTypes.EnterpriseObjectService));

            // 6. Login attempt tracker — optional, only registered if configured
            if (!string.IsNullOrWhiteSpace(configuration.Components.LoginAttemptTracker))
            {
                services.AddSingleton<ILoginAttemptTracker>(_ => CreateOrDefault<ILoginAttemptTracker>(
                    configuration.Components.LoginAttemptTracker, configuration.Components.LoginAttemptTracker));
            }

            // 7. BO factory: ctor injection of IDefineAccess / ISessionInfoService / IServiceProvider / IFormBoTypeResolver
            services.AddSingleton<IFormBoTypeResolver, DefaultFormBoTypeResolver>();
            services.AddSingleton<IBusinessObjectFactory, BusinessObjectFactory>();

            // 8. Repository factories
            services.AddSingleton<ISystemRepositoryFactory>(_ => CreateOrDefault<ISystemRepositoryFactory>(
                configuration.Components.SystemRepositoryFactory, BackendDefaultTypes.SystemRepositoryFactory));
            services.AddSingleton<IFormRepositoryFactory>(_ => CreateOrDefault<IFormRepositoryFactory>(
                configuration.Components.FormRepositoryFactory, BackendDefaultTypes.FormRepositoryFactory));

            // 9. JsonRpcExecutor — transient (per request); IBusinessObjectFactory etc. resolved via ctor
            services.AddTransient<JsonRpcExecutor>();

            return services;
        }
    }
}
```

#### 1.3 兩個 bootstrapper 的職責

`Bee.ObjectCaching.CacheContainer.Initialize(storage)` 與 `Bee.Db.Manager.DbConnectionManager.Initialize(provider)` 都是 process-wide static 初始化點（Phase 2/3 留下的 wire-up 模式）。Phase 4 不重寫這兩個 static singleton；改以 DI 註冊一個 marker 介面 `ICacheBootstrapper` / `IDbConnectionManagerBootstrapper`，在 host 啟動時透過 `app.Services.GetRequiredService<...>()` 觸發一次 ctor，ctor 內部呼叫對應的 `Initialize`：

```csharp
internal sealed class CacheBootstrapper : ICacheBootstrapper
{
    public CacheBootstrapper(IDefineStorage storage, BackendConfiguration configuration)
    {
        CacheContainer.Initialize(storage);
        CacheInfo.Initialize(configuration);
    }
}

internal sealed class DbConnectionManagerBootstrapper : IDbConnectionManagerBootstrapper
{
    public DbConnectionManagerBootstrapper(IDatabaseSettingsProvider provider)
    {
        DbConnectionManager.Initialize(provider);
    }
}
```

Host 端在 `Program.cs` / `Startup.cs` 完成 `AddBeeFramework` 並 build 後，必須做一次 eager resolve：

```csharp
app.Services.GetRequiredService<ICacheBootstrapper>();
app.Services.GetRequiredService<IDbConnectionManagerBootstrapper>();
```

提供一個 `app.UseBeeFramework()` extension 包裝 eager resolve，避免每個 host 重寫。

> 為什麼不用 `IHostedService` / `IStartupFilter`？兩者依賴 ASP.NET Generic Host 的執行模型，而 `AddBeeFramework` 要保持 host-agnostic。透過顯式 marker resolve，無論 host 是 ASP.NET、Console，還是測試 fixture 都能呼叫。

### 2. `BusinessObjectFactory` 重寫

#### 2.1 新 ctor

```csharp
public class BusinessObjectFactory : IBusinessObjectFactory
{
    private readonly IServiceProvider _services;
    private readonly IDefineAccess _defineAccess;
    private readonly ISessionInfoService _sessionInfoService;
    private readonly IFormBoTypeResolver _resolver;

    public BusinessObjectFactory(
        IServiceProvider services,
        IDefineAccess defineAccess,
        ISessionInfoService sessionInfoService,
        IFormBoTypeResolver resolver)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public IBusinessObject CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true)
    {
        var ctx = BuildContext();
        return ActivatorUtilities.CreateInstance<SystemBusinessObject>(_services, ctx, accessToken, isLocalCall);
    }

    public IBusinessObject CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true)
    {
        var type = _resolver.Resolve(progId);
        var ctx = BuildContext();
        return (IBusinessObject)ActivatorUtilities.CreateInstance(_services, type, ctx, accessToken, progId, isLocalCall);
    }

    private IBeeContext BuildContext() => new BeeContext
    {
        DefineAccess = _defineAccess,
        SessionInfoService = _sessionInfoService,
        BoFactory = this,
        Services = _services,
    };
}
```

#### 2.2 刪除的 API

- `static void Initialize(IDefineAccess, ISessionInfoService)` — 不再需要（Phase 3 transitional）
- `static readonly IServiceProvider _services = new BackendInfoServiceProvider()` 欄位 — 不再需要
- `BackendInfoServiceProvider` 類別 — 整個刪除

#### 2.3 介面收緊

`IBusinessObjectFactory` 回傳型別於 Phase 3 已收緊為 `IBusinessObject`；Phase 4 無變動。

### 3. `JsonRpcExecutor` 重寫

#### 3.1 ctor

```csharp
public class JsonRpcExecutor
{
    private readonly IBusinessObjectFactory _boFactory;
    private readonly IAccessTokenValidator _tokenValidator;
    private readonly IApiEncryptionKeyProvider _keyProvider;

    public JsonRpcExecutor(
        IBusinessObjectFactory boFactory,
        IAccessTokenValidator tokenValidator,
        IApiEncryptionKeyProvider keyProvider)
    {
        _boFactory = boFactory ?? throw new ArgumentNullException(nameof(boFactory));
        _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    public Guid AccessToken { get; set; }
    public bool IsLocalCall { get; set; }

    public async Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
    {
        // ... 既有邏輯，但所有 BackendInfo.X 改成 _xxx ctor field
        ApiAccessValidator.ValidateAccess(method, new ApiCallContext(AccessToken, IsLocalCall, format), _tokenValidator);
        byte[]? apiEncryptionKey = format == PayloadFormat.Encrypted ? _keyProvider.GetKey(AccessToken) : null;
        // ...
    }

    private object CreateBusinessObject(Guid accessToken, string progId)
    {
        if (progId == SysProgIds.System)
            return _boFactory.CreateSystemBusinessObject(accessToken, IsLocalCall);
        return _boFactory.CreateFormBusinessObject(accessToken, progId, IsLocalCall);
    }
}
```

#### 3.2 ctor 簽章選擇：為何不在 ctor 帶 `accessToken` / `isLocalCall`

- runtime 值（accessToken / isLocalCall）每次請求變動，不可放在 singleton ctor
- DI 容器無法在 service registration 時知道這些值
- 因此把 services 放 ctor、runtime 值放 property setter（在 controller 內 `executor.AccessToken = ...` 設定後呼叫 `ExecuteAsync`）

替代方案（為什麼不採用）：
- `IJsonRpcExecutorFactory.Create(accessToken, isLocalCall)`：多一層工廠，沒帶來價值
- `ExecuteAsync(request, accessToken, isLocalCall)`：簽章改動會影響 contract 介面，需另開單

採 ctor service + setter runtime 值。

#### 3.3 Controller 端建構

```csharp
protected virtual async Task<IActionResult> HandleRequestAsync(Guid accessToken, JsonRpcRequest request)
{
    var executor = HttpContext.RequestServices.GetRequiredService<JsonRpcExecutor>();
    executor.AccessToken = accessToken;
    executor.IsLocalCall = false;  // ApiServiceController 永遠是遠端呼叫
    var result = await executor.ExecuteAsync(request);
    // ...
}
```

DI 將 `JsonRpcExecutor` 註冊為 transient（一次請求一個新實例），自動從 request scope 解析 `IBusinessObjectFactory` / `IAccessTokenValidator` / `IApiEncryptionKeyProvider`。

### 4. `ApiAccessValidator` 改造

仍是 `static class`，但 `ValidateAccess` 多收 `IAccessTokenValidator` 參數：

```csharp
public static class ApiAccessValidator
{
    public static void ValidateAccess(MethodInfo method, ApiCallContext context, IAccessTokenValidator tokenValidator)
    {
        // ... 既有邏輯
        if (attr.AccessRequirement == ApiAccessRequirement.Authenticated && !IsTokenValid(context.AccessToken, tokenValidator))
            throw new UnauthorizedAccessException("AccessToken is required or invalid.");
        // ...
    }

    private static bool IsTokenValid(Guid accessToken, IAccessTokenValidator provider)
    {
        if (accessToken == Guid.Empty) return false;
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        return provider.Validate(accessToken);
    }
}
```

呼叫端從 `JsonRpcExecutor` 帶入 `_tokenValidator`；tests 直接 new `FakeTokenProvider` 後傳入。

### 5. Bee.Business 服務改造

#### 5.1 `AccessTokenValidator`

```csharp
public class AccessTokenValidator : IAccessTokenValidator
{
    private readonly ISessionInfoService _sessionInfoService;

    public AccessTokenValidator(ISessionInfoService sessionInfoService)
    {
        _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
    }

    public bool Validate(Guid accessToken)
    {
        if (ValueUtilities.IsEmpty(accessToken))
            throw new UnauthorizedAccessException("Access token is required.");
        var sessionInfo = _sessionInfoService.Get(accessToken);
        // ... 既有邏輯
    }
}
```

#### 5.2 `DynamicApiEncryptionKeyProvider`

同樣 ctor 注入 `ISessionInfoService`。

#### 5.3 `StaticApiEncryptionKeyProvider`

ctor 接受 `byte[]` 金鑰：

```csharp
public class StaticApiEncryptionKeyProvider : IApiEncryptionKeyProvider
{
    private readonly byte[] _apiEncryptionKey;

    public StaticApiEncryptionKeyProvider(byte[] apiEncryptionKey)
    {
        _apiEncryptionKey = apiEncryptionKey ?? throw new ArgumentNullException(nameof(apiEncryptionKey));
        if (apiEncryptionKey.Length == 0)
            throw new ArgumentException("ApiEncryptionKey cannot be empty.", nameof(apiEncryptionKey));
    }

    public byte[] GetKey(Guid accessToken) => _apiEncryptionKey;
    public byte[] GenerateKeyForLogin() => _apiEncryptionKey;
}
```

DI 註冊時透過 factory 從 `BackendInfo.ApiEncryptionKey`（仍在 Phase 4 內保留）取得：

```csharp
// In AddBeeFramework, only if StaticApiEncryptionKeyProvider is the configured impl:
// AssemblyLoader.CreateInstance(...) 流程仍透過 BackendDefaultTypes 處理，但 ctor 需要參數 ——
// 改為在 DI registration 時手動構造：
services.AddSingleton<IApiEncryptionKeyProvider>(_ =>
{
    var typeName = string.IsNullOrWhiteSpace(configuration.Components.ApiEncryptionKeyProvider)
        ? BackendDefaultTypes.ApiEncryptionKeyProvider
        : configuration.Components.ApiEncryptionKeyProvider;
    var type = AssemblyLoader.GetType(typeName)!;
    // Static provider needs the API key; Dynamic provider needs ISessionInfoService.
    // For Static: pass BackendInfo.ApiEncryptionKey via ctor.
    // For Dynamic: resolve via ActivatorUtilities to inject ISessionInfoService.
    if (type == typeof(StaticApiEncryptionKeyProvider))
        return new StaticApiEncryptionKeyProvider(BackendInfo.ApiEncryptionKey);
    return (IApiEncryptionKeyProvider)ActivatorUtilities.CreateInstance(_, type)!;
});
```

> **設計權衡**：`StaticApiEncryptionKeyProvider` 與 `DynamicApiEncryptionKeyProvider` ctor 簽章不同，無法用統一 `ActivatorUtilities.CreateInstance` 解析。本 phase 容忍 factory 內 if-else 分派；Phase 5/6 將 API key 移至 `IOptions<ApiEncryptionKeyOptions>`，兩個 provider ctor 統一為 `(IOptions<ApiEncryptionKeyOptions>, ISessionInfoService)` 並消除 if-else。

### 6. Bee.Db DML helpers 重構（Phase 3 推遲）

#### 6.1 5 個 `*FormCommandBuilder` 移除 `(string progId)` ctor

```csharp
// Before
public PgFormCommandBuilder(string progID)
{
    FormSchema = BackendInfo.DefineAccess.GetFormSchema(progID);
    // ...
}
public PgFormCommandBuilder(FormSchema formDefine) { ... }

// After — 只剩 (FormSchema) ctor
public PgFormCommandBuilder(FormSchema formDefine) { ... }
```

#### 6.2 `IDialectFactory.CreateFormCommandBuilder(string)` 移除

介面方法刪除 + 5 個 `*DialectFactory` 對應實作刪除。production code 無此呼叫，僅測試使用。

#### 6.3 `SelectContextBuilder` ctor 接 `IDefineAccess`

```csharp
public SelectContextBuilder(FormTable formTable, HashSet<string> usedFieldNames, IDefineAccess defineAccess)
{
    _formTable = formTable;
    _usedFieldNames = usedFieldNames;
    _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
}
```

`AddTableJoin` 內 `BackendInfo.DefineAccess.GetFormSchema(...)` → `_defineAccess.GetFormSchema(...)`。

#### 6.4 `SelectCommandBuilder` 對應調整

`SelectCommandBuilder` ctor 多收 `IDefineAccess`，在 `GetSelectContext` 內傳給 `SelectContextBuilder`：

```csharp
public SelectCommandBuilder(FormSchema formDefine, DatabaseType databaseType, IDefineAccess defineAccess) { ... }
```

5 個 `*FormCommandBuilder` 內 `new SelectCommandBuilder(FormSchema, databaseType)` 改為 `new SelectCommandBuilder(FormSchema, databaseType, defineAccess)` —— 但 `*FormCommandBuilder` ctor 也要多收 `IDefineAccess`。

**權衡**：`*FormCommandBuilder` 與 `SelectCommandBuilder` 的 ctor 都需要 `IDefineAccess`。呼叫鏈：

```
Repository (有 IDefineAccess) → DialectFactory.CreateFormCommandBuilder(FormSchema)
                              → new PgFormCommandBuilder(FormSchema)   ← 此處需要 IDefineAccess
                              → builder.BuildSelect(...) → new SelectCommandBuilder(formSchema, dbType, defineAccess)
```

Phase 4 內最小改動：

| 類別 | 改造 |
|------|------|
| `*FormCommandBuilder`（5 個） | ctor 簽章 = `(FormSchema, IDefineAccess)`；內部把 `IDefineAccess` 傳給 `SelectCommandBuilder` |
| `IDialectFactory.CreateFormCommandBuilder(FormSchema)` | 介面方法簽章不變（已是 `FormSchema`）；但 `IDialectFactory` 介面也要接 `IDefineAccess` —— 由 caller 在 `IDialectFactory` 建構時注入，介面方法本身保持單參 |
| `SelectCommandBuilder` | ctor = `(FormSchema, DatabaseType, IDefineAccess)` |
| `SelectContextBuilder` | ctor = `(FormTable, HashSet<string>, IDefineAccess)` |

實際上 `IDialectFactory` 是 stateless 的（每個 `*DialectFactory` 都沒欄位），要在 `CreateFormCommandBuilder` 時拿到 `IDefineAccess` 有兩種模式：

- **A. `IDialectFactory.CreateFormCommandBuilder(FormSchema, IDefineAccess)`**：介面方法加參數
- **B. `IDialectFactory` ctor 接 `IDefineAccess`，每個 `*DialectFactory` 變 stateful**

選 **A**：介面方法多收 `IDefineAccess`，每個 `*DialectFactory` 實作把它傳給 `new XxxFormCommandBuilder(formSchema, defineAccess)`。改動更小、更顯式。

#### 6.5 `TableSchemaBuilder` ctor 接 `IDefineAccess`

```csharp
public TableSchemaBuilder(string databaseId, IDefineAccess defineAccess)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
    DatabaseId = databaseId;
    _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
    // ...
}
```

`CreateComparer` 內 `BackendInfo.DefineAccess.GetTableSchema(...)` → `_defineAccess.GetTableSchema(...)`。

#### 6.6 `DatabaseRepository` 對應調整

```csharp
internal class DatabaseRepository : IDatabaseRepository
{
    private readonly IDefineAccess _defineAccess;

    public DatabaseRepository(IDefineAccess defineAccess)
    {
        _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
    }

    public bool UpgradeTableSchema(string databaseId, string categoryId, string tableName)
    {
        // ...
        var builder = new TableSchemaBuilder(databaseId, _defineAccess);
        return builder.Execute(categoryId, tableName);
    }
}
```

`SystemRepositoryFactory.CreateDatabaseRepository(...)` 需要拿到 `IDefineAccess`。`ISystemRepositoryFactory` 介面方法不變（避免 breaking change），改成 `SystemRepositoryFactory` ctor 接 `IDefineAccess`、`CreateDatabaseRepository` 內傳遞。

`SystemRepositoryFactory` 透過 `AddBeeFramework` 註冊為 DI service，ctor 自動注入 `IDefineAccess`。

### 7. `BackendInfo` 屬性清理

#### 7.1 移除的屬性

| 屬性 | 來源 |
|------|------|
| `DefineAccess` | Phase 3 推遲 |
| `SessionInfoService` | Phase 3 推遲 |
| `ApiEncryptionKeyProvider` | Phase 3 推遲 |
| `AccessTokenValidator` | Phase 3 推遲 |
| `BusinessObjectFactory` | Phase 3 推遲 |
| `CacheDataSourceProvider` | Phase 3 推遲 |
| `EnterpriseObjectService` | Phase 3 推遲 |
| `LoginAttemptTracker` | Phase 3 推遲 |

#### 7.2 保留的成員

| 成員 | 型別 | 保留原因 |
|------|------|---------|
| `LogWriter` | `ILogWriter` | Phase 5/6 移至 `IOptions<T>` |
| `LogOptions` | `LogOptions` | 同上 |
| `ApiEncryptionKey` | `byte[]` | Phase 5/6 移至 `IOptions<T>` |
| `CookieEncryptionKey` | `byte[]` | 同上 |
| `ConfigEncryptionKey` | `byte[]` | 同上 |
| `DatabaseEncryptionKey` | `byte[]` | 同上 |
| `Initialize(BackendConfiguration, bool)` / `Initialize(BackendConfiguration)` | `void` → `IServiceProvider` | 改造為「建構 SP 並回傳」（見 §7.3） |

#### 7.3 `Initialize` 改造

```csharp
public static IServiceProvider Initialize(BackendConfiguration configuration, bool autoCreateMasterKey)
{
    if (!SysInfo.IsSingleFile)
    {
        var services = new ServiceCollection();
        services.AddBeeFramework(configuration, autoCreateMasterKey);
        var provider = services.BuildServiceProvider();

        // Eager-resolve bootstrap markers to fire static wire-up.
        provider.GetRequiredService<ICacheBootstrapper>();
        provider.GetRequiredService<IDbConnectionManagerBootstrapper>();

        ValidateDatabaseSettings(provider);
        return provider;
    }

    InitializeSecurityKeys(configuration, autoCreateMasterKey);
    return EmptyServiceProvider.Instance;  // single-file mode skips full bootstrap
}

public static IServiceProvider Initialize(BackendConfiguration configuration)
    => Initialize(configuration, false);
```

**Breaking change**：回傳型別從 `void` → `IServiceProvider`。呼叫端必須接收 SP。

- production：未來 ASP.NET host 不會直接呼叫 `BackendInfo.Initialize`，而是 `services.AddBeeFramework(configuration)` + `BuildServiceProvider()`，所以這個 breaking 不影響 production
- tests：`GlobalFixture` 接收 SP 並存為 fixture-scope field（取代既有「寫入 BackendInfo 靜態」模式）
- samples：少數示範專案目前呼叫 `BackendInfo.Initialize(...)` —— Phase 4 內一併改成 `AddBeeFramework`

`Bee.Definition` 是否要因此引入 `Microsoft.Extensions.DependencyInjection.Abstractions` 相依？是。理由：`Initialize` 回傳 `IServiceProvider` 是 BCL 型別不需要 MEDI；但 `AddBeeFramework` 內部呼叫需 `ServiceCollection` 與 `BuildServiceProvider`。

**設計權衡**：

| 選項 | 優點 | 缺點 |
|------|------|------|
| A. `BackendInfo.Initialize` 留在 `Bee.Definition` 並引入 MEDI | 對外 API 不變 | `Bee.Definition` 多一個套件相依 |
| B. `BackendInfo.Initialize` 改為 thin wrapper 呼叫 `Bee.Api.AspNetCore.BeeFrameworkServiceCollectionExtensions.AddBeeFramework`，並透過反射呼叫 | `Bee.Definition` 不引入 MEDI | 反射呼叫，複雜 |
| C. 完全移除 `BackendInfo.Initialize`，所有呼叫端改為直接 `AddBeeFramework` | 最乾淨 | breaking change 影響 GlobalFixture / samples / 任何手動 host |

選 **C**：完全移除 `BackendInfo.Initialize`，呼叫端（GlobalFixture / samples / 任何手動 host）改為直接 `services.AddBeeFramework(...).BuildServiceProvider()`。`BackendInfo` 縮減為「只剩加密金鑰 + LogOptions/LogWriter」的純配置 holder，待 Phase 5/6 移至 `IOptions<T>` 後整個刪除。

> 主計畫 §「相容性策略」明確規範「不留相容過渡層、不使用 `[Obsolete]` 過渡標記」。移除 `BackendInfo.Initialize` 符合精神。

### 8. ASP.NET Core 整合

#### 8.1 Host 端 wiring

```csharp
// Program.cs (sample / 使用者專案)
var settings = SystemSettingsLoader.Load();
SysInfo.Initialize(settings.CommonConfiguration);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddBeeFramework(settings.BackendConfiguration, autoCreateMasterKey: false);

var app = builder.Build();
app.UseBeeFramework();   // eager-resolve bootstrappers (CacheContainer + DbConnectionManager init)
app.MapControllers();
app.Run();
```

`UseBeeFramework`（新增）：

```csharp
public static class BeeFrameworkApplicationBuilderExtensions
{
    public static IApplicationBuilder UseBeeFramework(this IApplicationBuilder app)
    {
        app.ApplicationServices.GetRequiredService<ICacheBootstrapper>();
        app.ApplicationServices.GetRequiredService<IDbConnectionManagerBootstrapper>();
        return app;
    }
}
```

#### 8.2 `ApiServiceController` 改造

```csharp
protected virtual async Task<IActionResult> HandleRequestAsync(Guid accessToken, JsonRpcRequest request)
{
    try
    {
        var executor = HttpContext.RequestServices.GetRequiredService<JsonRpcExecutor>();
        executor.AccessToken = accessToken;
        executor.IsLocalCall = false;
        var result = await executor.ExecuteAsync(request);
        // ...
    }
    catch (Exception ex) { /* ... */ }
}
```

### 9. 測試基礎設施 shim

#### 9.1 `Bee.Tests.Shared.BeeTestServices`（新增）

```csharp
namespace Bee.Tests.Shared
{
    /// <summary>
    /// Process-wide DI container for tests; built once by <see cref="GlobalFixture"/>
    /// and reused across all test classes. Phase 4 transitional helper — replaced
    /// by per-class ServiceProvider in Phase 5.
    /// </summary>
    public static class BeeTestServices
    {
        private static IServiceProvider? _provider;

        internal static void Initialize(IServiceProvider provider)
        {
            _provider = provider;
        }

        public static IServiceProvider Provider =>
            _provider ?? throw new InvalidOperationException(
                "BeeTestServices not initialized. Ensure GlobalFixture has run.");

        public static T GetRequiredService<T>() where T : notnull
            => Provider.GetRequiredService<T>();
    }
}
```

#### 9.2 `GlobalFixture` 改造

```csharp
private static void InitializeOnce()
{
    var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
    DefinePathInfo.Initialize(new PathOptions { DefinePath = Path.Combine(repoRoot, "tests", "Define") });

    // Bootstrap：暫時建立一個 DefineAccess 給 RegisterXxx() 與 EnsureFallbackCommonDatabaseItem 使用。
    var bootstrapStorage = new FileDefineStorage();
    CacheContainer.Initialize(bootstrapStorage);
    var bootstrapAccess = new LocalDefineAccess(bootstrapStorage);
    RegisterSqlServer(bootstrapAccess);
    RegisterPostgreSql(bootstrapAccess);
    RegisterSqlite(bootstrapAccess);
    RegisterMySql(bootstrapAccess);
    RegisterOracle(bootstrapAccess);
    EnsureFallbackCommonDatabaseItem(bootstrapAccess);

    var settings = SystemSettingsLoader.Load();
    settings.BackendConfiguration.Components.BusinessObjectFactory = BackendDefaultTypes.BusinessObjectFactory;
    if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
    {
        settings.BackendConfiguration.SecurityKeySettings.MasterKeySource = new MasterKeySource
        {
            Type = MasterKeySourceType.Environment,
            Value = "BEE_TEST_FIXTURE_MASTER_KEY"
        };
    }
    SysInfo.Initialize(settings.CommonConfiguration);

    // 用 AddBeeFramework 建 DI 容器，取代既有 BackendInfo.Initialize。
    var services = new ServiceCollection();
    services.AddBeeFramework(settings.BackendConfiguration, autoCreateMasterKey: true);
    var provider = services.BuildServiceProvider();
    provider.GetRequiredService<ICacheBootstrapper>();
    provider.GetRequiredService<IDbConnectionManagerBootstrapper>();
    BeeTestServices.Initialize(provider);
}
```

> `RegisterXxx(bootstrapAccess)` 簽章調整：原本透過 `BackendInfo.DefineAccess.GetDatabaseSettings()` 取得，改為傳入 `bootstrapAccess`。

#### 9.3 測試引用點遷移範例

```csharp
// Before
BackendInfo.SessionInfoService.Set(sessionInfo);

// After
BeeTestServices.GetRequiredService<ISessionInfoService>().Set(sessionInfo);
```

```csharp
// Before
var settings = BackendInfo.DefineAccess.GetSystemSettings();

// After
var settings = BeeTestServices.GetRequiredService<IDefineAccess>().GetSystemSettings();
```

預期遷移檔案數約 15 個（不含註解類變更）。詳見 §10「改動清單」。

#### 9.4 `TestBeeContext` / `TestSessionFactory` 改造

`TestBeeContext.Create()` 從 `BeeTestServices.Provider` 解析所有成員：

```csharp
public static IBeeContext Create()
{
    var sp = BeeTestServices.Provider;
    return new BeeContext
    {
        DefineAccess = sp.GetRequiredService<IDefineAccess>(),
        SessionInfoService = sp.GetRequiredService<ISessionInfoService>(),
        BoFactory = sp.GetRequiredService<IBusinessObjectFactory>(),
        Services = sp,
    };
}
```

`TestServiceProvider` private 類別整個刪除（不需要再轉發到 BackendInfo）。

#### 9.5 替換式測試（swap pattern）的處理

部分測試用「臨時替換 BackendInfo.X，跑完還原」的模式（典型如 `SystemBusinessObjectLoginTests` 與 `ApiAccessValidatorTests`）。本 phase 透過兩種替代模式：

- **Pattern A：直接建構待測元件**（首選）
  ```csharp
  // Before
  BackendInfo.LoginAttemptTracker = tracker;
  var bo = (SystemBusinessObject)BackendInfo.BusinessObjectFactory.CreateSystemBusinessObject(token);
  ...
  BackendInfo.LoginAttemptTracker = original;

  // After
  var ctx = new BeeContext
  {
      DefineAccess = BeeTestServices.GetRequiredService<IDefineAccess>(),
      SessionInfoService = BeeTestServices.GetRequiredService<ISessionInfoService>(),
      BoFactory = BeeTestServices.GetRequiredService<IBusinessObjectFactory>(),
      Services = new TestOverrideServiceProvider(BeeTestServices.Provider, (typeof(ILoginAttemptTracker), tracker)),
  };
  var bo = new SystemBusinessObject(ctx, token, isLocalCall: true);
  ```
  `TestOverrideServiceProvider`（新增於 `Bee.Tests.Shared`）：在原 `IServiceProvider` 之上疊一層 override map，僅覆蓋指定服務。

- **Pattern B：直接傳參數**（適用 `ApiAccessValidatorTests`）
  ```csharp
  // Before
  BackendInfo.AccessTokenValidator = new FakeTokenProvider { Result = false };
  ApiAccessValidator.ValidateAccess(method, context);

  // After
  var fake = new FakeTokenProvider { Result = false };
  ApiAccessValidator.ValidateAccess(method, context, fake);
  ```

Pattern A/B 都不需要修改 production 靜態狀態，自然支援平行測試。

### 10. 改動清單

#### 10.1 新增

| 檔案 | 內容 |
|------|------|
| `src/Bee.Api.AspNetCore/BeeFrameworkServiceCollectionExtensions.cs` | `AddBeeFramework` 入口 |
| `src/Bee.Api.AspNetCore/BeeFrameworkApplicationBuilderExtensions.cs` | `UseBeeFramework` 入口（eager-resolve bootstrappers） |
| `src/Bee.Api.AspNetCore/Bootstrapping/ICacheBootstrapper.cs` + 預設實作 | OC 初始化 marker |
| `src/Bee.Api.AspNetCore/Bootstrapping/IDbConnectionManagerBootstrapper.cs` + 預設實作 | DbConnectionManager 初始化 marker |
| `tests/Bee.Tests.Shared/BeeTestServices.cs` | process-wide DI holder for tests |
| `tests/Bee.Tests.Shared/TestOverrideServiceProvider.cs` | per-test 替換特定 service |

#### 10.2 修改 src（~22 個）

| 檔案 | 修改 |
|------|------|
| `src/Bee.Api.AspNetCore/Bee.Api.AspNetCore.csproj` | 加 4 個 project ref（Business / Repository / Db / OC） |
| `src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs` | `HandleRequestAsync` 改用 DI 解析 `JsonRpcExecutor` |
| `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` | ctor 注入 3 個 services；移除 `BackendInfo.*` 讀取 |
| `src/Bee.Api.Core/Validator/ApiAccessValidator.cs` | `ValidateAccess` 多收 `IAccessTokenValidator`；移除 `IsTokenValid` 內 `BackendInfo` 讀取 |
| `src/Bee.Business/BusinessObjectFactory.cs` | 移除 static `Initialize` + `_services` 欄位；ctor 注入 4 個 deps；用 `ActivatorUtilities.CreateInstance` |
| `src/Bee.Business/BackendInfoServiceProvider.cs` | **刪除** |
| `src/Bee.Business/Validator/AccessTokenValidator.cs` | ctor 注入 `ISessionInfoService` |
| `src/Bee.Business/Providers/DynamicApiEncryptionKeyProvider.cs` | ctor 注入 `ISessionInfoService` |
| `src/Bee.Business/Providers/StaticApiEncryptionKeyProvider.cs` | ctor 接 `byte[] apiEncryptionKey` |
| `src/Bee.Db/Dml/SelectContextBuilder.cs` | ctor 加 `IDefineAccess` |
| `src/Bee.Db/Dml/SelectCommandBuilder.cs` | ctor 加 `IDefineAccess`；傳給 `SelectContextBuilder` |
| `src/Bee.Db/Schema/TableSchemaBuilder.cs` | ctor 加 `IDefineAccess` |
| `src/Bee.Db/Providers/IDialectFactory.cs` | `CreateFormCommandBuilder` 介面方法：移除 `(string)` 多載；保留 `(FormSchema)` 多載 + 加 `IDefineAccess` 參數 |
| `src/Bee.Db/Providers/PostgreSql/PgFormCommandBuilder.cs` | 移除 `(string progId)` ctor；`(FormSchema)` ctor 加 `IDefineAccess` |
| `src/Bee.Db/Providers/Oracle/OracleFormCommandBuilder.cs` | 同上 |
| `src/Bee.Db/Providers/Sqlite/SqliteFormCommandBuilder.cs` | 同上 |
| `src/Bee.Db/Providers/MySql/MySqlFormCommandBuilder.cs` | 同上 |
| `src/Bee.Db/Providers/SqlServer/SqlFormCommandBuilder.cs` | 同上 |
| `src/Bee.Db/Providers/PostgreSql/PgDialectFactory.cs` | 移除 `(string)` 多載實作；`(FormSchema, IDefineAccess)` 多載新增 |
| `src/Bee.Db/Providers/Oracle/OracleDialectFactory.cs` | 同上 |
| `src/Bee.Db/Providers/Sqlite/SqliteDialectFactory.cs` | 同上 |
| `src/Bee.Db/Providers/MySql/MySqlDialectFactory.cs` | 同上 |
| `src/Bee.Db/Providers/SqlServer/SqlDialectFactory.cs` | 同上 |
| `src/Bee.Repository/System/DatabaseRepository.cs` | ctor 注入 `IDefineAccess`；傳給 `TableSchemaBuilder` |
| `src/Bee.Repository/System/SystemRepositoryFactory.cs`（或對應檔） | ctor 注入 `IDefineAccess`；傳給 `DatabaseRepository` |
| `src/Bee.Definition/BackendInfo.cs` | 刪除 7 個服務屬性 + `Initialize` 系列方法 + 對應 helper（`InitializeComponents` / `ResolveDefineAccess` / `InvokeStaticMethod` / `ValidateComponents` / `ValidateDatabaseSettings` 等多數搬到 `AddBeeFramework`） |

#### 10.3 修改 tests（~15 個）

| 檔案 | 修改 |
|------|------|
| `tests/Bee.Tests.Shared/GlobalFixture.cs` | 重寫 `InitializeOnce`：改用 `AddBeeFramework`；移除 `BackendInfo.DefineAccess = ...`；`RegisterXxx` 系列接 `bootstrapAccess` 參數 |
| `tests/Bee.Tests.Shared/TestBeeContext.cs` | 從 `BeeTestServices.Provider` 解析；刪除內部 `TestServiceProvider` private 類別 |
| `tests/Bee.Tests.Shared/TestSessionFactory.cs` | `BackendInfo.SessionInfoService.Set` → `BeeTestServices.GetRequiredService<ISessionInfoService>().Set` |
| `tests/Bee.Api.Core.UnitTests/ApiAccessValidatorTests.cs` | 改用 pattern B（傳 fake validator 參數），刪除 `BackendInfo.AccessTokenValidator` 替換 |
| `tests/Bee.Business.UnitTests/AccessTokenValidatorTests.cs` | 直接 `new AccessTokenValidator(BeeTestServices.GetRequiredService<ISessionInfoService>())` 並 `Set` session 後驗證 |
| `tests/Bee.Business.UnitTests/DynamicApiEncryptionKeyProviderTests.cs` | 同上 |
| `tests/Bee.Business.UnitTests/StaticApiEncryptionKeyProviderTests.cs` | 改測 ctor 接 byte[] 的行為；移除 `BackendInfo.ApiEncryptionKey = null!` 測試（Phase 5/6 替換為 `IOptions` validation 測試） |
| `tests/Bee.Business.UnitTests/SystemBusinessObjectLoginTests.cs` | 用 pattern A（`TestOverrideServiceProvider` 注入 fake tracker） |
| `tests/Bee.Business.UnitTests/SystemBusinessObjectExtraTests.cs` | `BackendInfo.DefineAccess.GetDatabaseSettings()` → `BeeTestServices.GetRequiredService<IDefineAccess>().GetDatabaseSettings()` |
| `tests/Bee.Business.UnitTests/SystemBusinessObjectDefineTests.cs` | 註解更新；無實質代碼變動（測試本身透過 BO 而非 BackendInfo） |
| `tests/Bee.ObjectCaching.UnitTests/CacheTests.cs` | 同 SystemBusinessObjectExtraTests |
| `tests/Bee.ObjectCaching.UnitTests/SystemSettingsCacheTests.cs` | 註解更新 |
| `tests/Bee.Db.UnitTests/Manager/DbConnectionManagerTests.cs` | `BackendInfo.DefineAccess.GetDatabaseSettings()` → `BeeTestServices.GetRequiredService<IDefineAccess>().GetDatabaseSettings()` |
| `tests/Bee.Db.UnitTests/DbAccessTests.cs` | 同上 |
| `tests/Bee.Db.UnitTests/DbAccessIsolationLevelTests.cs` | 同上 |
| `tests/Bee.Db.UnitTests/TableSchemaBuilderTests.cs` | `new TableSchemaBuilder("common_sqlserver")` → `new TableSchemaBuilder("common_sqlserver", BeeTestServices.GetRequiredService<IDefineAccess>())` |
| `tests/Bee.Db.UnitTests/SelectCommandBuilderTests.cs` | `new SelectCommandBuilder(schema, DatabaseType.X)` → `new SelectCommandBuilder(schema, DatabaseType.X, BeeTestServices.GetRequiredService<IDefineAccess>())` |
| `tests/Bee.Db.UnitTests/FormCommandBuilderIudIntegrationTests.cs` | 同上 |
| `tests/Bee.Db.UnitTests/PgDialectFactoryTests.cs` / `SqliteDialectFactoryTests.cs` / `MySqlDialectFactoryTests.cs` / `OracleDialectFactoryTests.cs` / `DbDialectRegistryTests.cs` | 對應已刪除 `CreateFormCommandBuilder(string)` 介面方法的測試**刪除**；保留 `(FormSchema, IDefineAccess)` 多載測試 |
| `tests/Bee.Db.UnitTests/SqliteFormCommandBuilderTests.cs` / `OracleFormCommandBuilderValidProgIdTests.cs` | 對應 `(string progId)` ctor 測試**刪除**；保留 `(FormSchema)` ctor 測試（加 `IDefineAccess` 參數） |

#### 10.4 修改 samples

| 檔案 | 修改 |
|------|------|
| `samples/**/Program.cs`（凡呼叫 `BackendInfo.Initialize`） | 改成 `services.AddBeeFramework(settings.BackendConfiguration)` + `app.UseBeeFramework()` |

> 實作前 grep 確認 samples 範圍；若無 sample 直接呼叫 `BackendInfo.Initialize` 則 skip。

#### 10.5 更新文件

| 檔案 | 內容 |
|------|------|
| `src/Bee.Api.AspNetCore/README.md` + `.zh-TW.md` | 新增「使用 `AddBeeFramework` 啟動框架」章節 |
| `docs/development-cookbook.md`（含可能的 `.zh-TW.md`） | 初始化流程章節改為 DI 範例 |

## 驗收標準

- [ ] `Bee.Api.AspNetCore.BeeFrameworkServiceCollectionExtensions.AddBeeFramework` extension 存在
- [ ] `Bee.Api.AspNetCore.BeeFrameworkApplicationBuilderExtensions.UseBeeFramework` extension 存在
- [ ] `BusinessObjectFactory` ctor = `(IServiceProvider, IDefineAccess, ISessionInfoService, IFormBoTypeResolver)`
- [ ] `BusinessObjectFactory` 不再有 static `Initialize` 方法
- [ ] `BackendInfoServiceProvider` 類別已刪除
- [ ] `JsonRpcExecutor` ctor 注入 3 個 services；無 `BackendInfo.*` 引用
- [ ] `ApiAccessValidator.ValidateAccess` 簽章多收 `IAccessTokenValidator`
- [ ] `AccessTokenValidator` / `DynamicApiEncryptionKeyProvider` ctor 注入 `ISessionInfoService`
- [ ] `StaticApiEncryptionKeyProvider` ctor 接 `byte[] apiEncryptionKey`
- [ ] `BackendInfo` 不再有 `DefineAccess` / `SessionInfoService` / `ApiEncryptionKeyProvider` / `AccessTokenValidator` / `BusinessObjectFactory` / `CacheDataSourceProvider` / `EnterpriseObjectService` / `LoginAttemptTracker` 屬性
- [ ] `BackendInfo.Initialize` 系列方法已刪除（含 `InitializeComponents` / `ResolveDefineAccess` 等 helper）
- [ ] 5 個 `*FormCommandBuilder` 的 `(string progId)` ctor 已刪除；`(FormSchema, IDefineAccess)` ctor 為唯一形式
- [ ] `IDialectFactory.CreateFormCommandBuilder(string)` 介面方法已刪除；`CreateFormCommandBuilder(FormSchema, IDefineAccess)` 為唯一形式
- [ ] `SelectContextBuilder` / `SelectCommandBuilder` / `TableSchemaBuilder` ctor 接 `IDefineAccess`
- [ ] `BeeTestServices` 與 `TestOverrideServiceProvider` 存在於 `Bee.Tests.Shared`
- [ ] `./test.sh` 全綠（含 SQL Server + PostgreSQL + SQLite 三組整合測試）
- [ ] `dotnet build --configuration Release` 通過（`TreatWarningsAsErrors=true`）
- [ ] GitHub Actions Build CI 通過
- [ ] `grep -n "BackendInfo\.DefineAccess\|BackendInfo\.SessionInfoService\|BackendInfo\.ApiEncryptionKeyProvider\|BackendInfo\.AccessTokenValidator\|BackendInfo\.BusinessObjectFactory\|BackendInfo\.CacheDataSourceProvider\|BackendInfo\.EnterpriseObjectService\|BackendInfo\.LoginAttemptTracker" src/ tests/` 結果為 0
- [ ] `BackendInfo` 剩餘成員僅有：4 個加密金鑰 + `LogOptions` + `LogWriter`（Phase 5/6 處理）

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| `AddBeeFramework` 內 if-else 分派 `StaticApiEncryptionKeyProvider` vs `DynamicApiEncryptionKeyProvider` ctor 不對稱 | 接受暫時 if-else，明確註解「Phase 5/6 統一 IOptions 後可移除」；驗收時不視為設計問題 |
| GlobalFixture bootstrap 流程仍需臨時 `LocalDefineAccess`（讓 `RegisterXxx` 拿到 `IDatabaseSettings`） | 維持既有「bootstrap access → AddBeeFramework 內覆寫」模式，但 bootstrap access 只活在 method scope，不再寫入 BackendInfo |
| `BackendInfo.Initialize` 移除影響範圍 | grep 全 repo 確認所有呼叫點都已遷移；samples 與 GlobalFixture 是僅有呼叫者 |
| `Microsoft.Extensions.DependencyInjection` 對 `Bee.Api.AspNetCore` 新增 4 個 project ref 可能引起套件相依擴張 | ASP.NET host 套件本來就要依賴 Business / Db / Repository / OC（否則無法在 production 跑），這只是把隱含相依顯式化 |
| `*FormCommandBuilder` ctor 簽章 + `IDialectFactory.CreateFormCommandBuilder` 簽章雙重變動 | 主計畫 §「相容性策略」允許「PR 合進 main 那刻必須綠」中間 commit 破裂；單一 PR 內一次處理 |
| `TableSchemaBuilder` 接 `IDefineAccess` 後，`DatabaseRepository` 的構造路徑改變 | `SystemRepositoryFactory` 透過 DI 注入 `IDefineAccess`，每個 Repository 建構時都有 access；改動局部 |
| Test 遷移工作量 ~15 個檔案 | 每個檔案的變動是機械式的（grep / replace 同樣 pattern），可批次處理 |
| `BackendInfo.ApiEncryptionKey` 仍是 static 但 `StaticApiEncryptionKeyProvider` 改 ctor 注入，AddBeeFramework 內手動讀 `BackendInfo.ApiEncryptionKey` 傳入 | 接受此 transitional 模式；Phase 5/6 一併移除 |
| 平行測試引發的 race（`BeeTestServices.Initialize` 多次寫入） | 同 Phase 0-3 既有的 `_initialized` flag + `lock` 保護 |
| 多個 `[Collection("Initialize")]` 共享同一個 `IServiceProvider`，scoped service（`ISessionInfoService`）跨測試共享狀態 | 改用 `provider.CreateScope()`：tests 內部自己建 scope 隔離；或 Phase 5 per-class fixture 處理 |

> **Scoped service 跨測試共享風險**特別重要：`ISessionInfoService` / `IAccessTokenValidator` 註冊為 Scoped，process-wide ServiceProvider 上每次 `GetRequiredService<ISessionInfoService>()` 會用 root scope（共享）。tests 可能因此互相污染。Phase 4 內權衡：
> - 選 1：把 `ISessionInfoService` 註冊為 Singleton（測試內共享一個實例，與既有「BackendInfo.SessionInfoService static」行為等價）
> - 選 2：tests 內手動 `using var scope = BeeTestServices.Provider.CreateScope();`
> - 選 3：Phase 5 per-class fixture 處理
>
> 採選 **1**：在 production 中 `ISessionInfoService` 是 Scoped（per request），但 test fixture 內把它降階成 Singleton（透過 GlobalFixture build 容器時用 `services.Replace(ServiceDescriptor.Singleton<ISessionInfoService>(...))` 覆蓋）。production 行為不變，test 行為與 Phase 3 一致。

## 提交策略

依使用者規範（[pull-request.md](../../.claude/rules/pull-request.md)）：本機可驗證，直接提交 main。

1. 本機跑 `dotnet build --configuration Release` + `./test.sh` 通過（含 SQL Server / PostgreSQL container）
2. 預估 diff：800–1500 lines（src ~1000、tests ~500、新增 helper ~200）；檔案數約 40 個
3. 單一 commit 包含所有改動；commit message 含主計畫 Phase 4 reference
4. push 後監測 GitHub Actions Build CI

### 子任務拆分建議（單一 commit 內按順序執行，方便檢查）

1. 加套件 project ref + 建立 `Bootstrapping` 資料夾與兩個 marker 介面
2. 重寫 `BusinessObjectFactory`（ctor 注入版）+ 刪除 `BackendInfoServiceProvider`
3. 改造 `AccessTokenValidator` / `DynamicApiEncryptionKeyProvider` / `StaticApiEncryptionKeyProvider`
4. 改造 `JsonRpcExecutor` + `ApiAccessValidator`
5. 改造 `Bee.Db` 5 個 `*FormCommandBuilder` + 5 個 `*DialectFactory` + `IDialectFactory` + `SelectContextBuilder` / `SelectCommandBuilder` / `TableSchemaBuilder`
6. 改造 `DatabaseRepository` + `SystemRepositoryFactory`
7. 撰寫 `AddBeeFramework` / `UseBeeFramework` extension（含兩個 bootstrapper）
8. 改造 `ApiServiceController.HandleRequestAsync`
9. 刪除 `BackendInfo` 7 個服務屬性 + 所有 Initialize 系列方法
10. 撰寫 `BeeTestServices` + `TestOverrideServiceProvider` shared helper
11. 改造 `GlobalFixture` + `TestBeeContext` + `TestSessionFactory`
12. 遷移其餘 13 個 test 檔案
13. 更新 samples（若有）+ README + development-cookbook
14. 本機 build + ./test.sh 三輪驗證（無 DB、SQL Server only、SQL Server + PostgreSQL）
15. push & 監測 CI

## 完成後狀態

- 主計畫頂部「Sub-plan 進度」表 Phase 4 狀態更新為 ✅ 已完成
- `BackendInfo` 縮減為僅含 4 個加密金鑰 + `LogOptions` + `LogWriter`，等待 Phase 5/6 拆解
- ASP.NET host 透過 `services.AddBeeFramework(...)` + `app.UseBeeFramework()` 啟動，不再呼叫 `BackendInfo.Initialize`
- BO 內 `Services` 逃生口背後是真 DI scope，可解析任何已註冊服務
- 測試透過 `BeeTestServices.Provider` 解析服務，不再依賴 `BackendInfo` 靜態屬性
- Phase 5 動工的基礎就緒：per-class fixture / `IOptions<T>` 配置遷移 / 加密金鑰移出 BackendInfo

## 未決議題（實作時細化）

- `ApiServiceController` 的 `IsLocalCall` 永遠為 `false` —— 是否需要 host-level configuration（例如 `[ApiAccessControl(LocalOnly)]` 的 in-process 呼叫場景）？本 phase 假設遠端呼叫；近端 in-process 模式由 Phase 5/6 或主計畫 §「範圍邊界」未來處理
- `Bee.Definition` 是否需要把 `IBeeContext` 從 namespace 重新整理？目前在 `Bee.Definition` 根 namespace。Phase 4 不動
- `BackendDefaultTypes.LoginAttemptTracker` 預設為何？若為 null 則跳過註冊；非 null 但無法解析則拋例外 —— 需查既有 `BackendDefaultTypes.cs` 是否有 `LoginAttemptTracker` 欄位
- `ApiEncryptionKey` 為空時，`StaticApiEncryptionKeyProvider` ctor 拋例外可能造成「未配置加密金鑰但需要 API key provider 解析」的早失敗。是否要在 `AddBeeFramework` 內加 conditional registration？實作時依 `ApiEncryptionKey.Length` 決定
