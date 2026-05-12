# 計畫：Phase 3 — Business 與 Repository 層 IBeeContext 注入

**狀態：✅ 已完成（2026-05-12）**

> 實作時調整：保留 `BackendInfo.DefineAccess` 靜態屬性（test migration 量過大，Phase 4 處理）；保留 5 個 `*FormCommandBuilder` 的 `(string progId)` ctor、`IDialectFactory.CreateFormCommandBuilder(string)` 介面方法、`SelectContextBuilder` / `TableSchemaBuilder` 內 `BackendInfo.DefineAccess` 引用（Phase 4 處理）。Phase 3 聚焦 BO + Repository + factory + IBeeContext + ResolveDefineAccess wire-up。額外修正 `AssemblyLoader` 載入機制（從 `Assembly.Load(byte[])` 改為 default load context，避免 split type identity）。

> 本文件為主計畫 [plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) 的 **Phase 3** sub-plan，獨立可 ship。

## 背景

### 主計畫定位

Phase 3 是 BackendInfo → DI 遷移計畫的 BO + Repository 注入階段。前置 Phase 2 已把 `BackendInfo.DefineStorage` 與 `BackendInfo.DefinePath` 等基礎設施屬性拆解；本 phase 進一步把 BO（`SystemBusinessObject` / `FormBusinessObject`）與 Repository 對 `BackendInfo.X` 的依賴改為透過 `IBeeContext` 注入。

### 為什麼此 phase 是「設計關鍵」

Phase 3 確立 BO 的依賴注入慣例 —— 後續 ERP 應用所有 BO 子類都遵循此模式。三個關鍵設計決議：

1. **`IBeeContext` 最小化（3 個成員）**：依用戶確認的依賴盤點，BO 通用必需的服務僅 3 個
2. **`IDbAccessFactory` 不在 IBeeContext**：DB 存取屬 `Bee.Repository` 層職責，BO 透過 Repository 間接操作
3. **BO 子類嚴格統一 ctor**（主計畫設計原則 §4）：所有 FormBusinessObject 子類 ctor 簽章固定，由 factory + ActivatorUtilities 統一建構

### 主計畫對應的設計決議

- 主計畫 §4：「BO 建構式統一與零 DI 註冊」明文 `IBeeContext` 聚合通用服務 + `Services` 逃生口（Phase 4 加入）
- 主計畫 Phase 4：補上 `IBeeContext.Services` 處理 Phase 3 殘留的 BackendInfo.X 讀取（特殊方法用）

## 目標

1. 定義 `IBeeContext` 介面（3 成員）+ `BeeContext` 預設實作
2. 定義 `IFormBoTypeResolver` 介面 + 預設實作（progId → BO Type 映射；本 phase 提供 fallback 到 `FormBusinessObject` 的最小實作）
3. 改造 `BusinessObject` base + `SystemBusinessObject` + `FormBusinessObject` 的 ctor 簽章與 BackendInfo.X 引用
4. 重寫 `BusinessObjectFactory` 使用 `IBeeContext` 與 `IFormBoTypeResolver`，介面回傳型別由 `object` 收緊為 `IBusinessObject`
5. 改造 `RepositoryInfo` 啟動流程（從 static ctor 改為 explicit `Initialize`）
6. 清理 `Bee.Db` DML helpers 對 `BackendInfo.DefineAccess` 的 7 處引用
7. 刪除 5 個 `*FormCommandBuilder` 的 `(string progId)` ctor 與 `IDialectFactory.CreateFormCommandBuilder(string)` 介面方法（production 無用，僅測試使用）
8. **移除 `BackendInfo.DefineAccess` 屬性**

## 非目標（本 phase 不做）

- 不移除以下 `BackendInfo.X` 屬性（Phase 4 處理）：
  - `BackendInfo.SessionInfoService`（仍被 `AccessTokenValidator` / `DynamicApiEncryptionKeyProvider` 使用；Phase 3 內 BO 已切到 ctx，但這 2 個 Bee.Business 內非 BO 類仍直接讀 static）
  - `BackendInfo.ApiEncryptionKeyProvider`（`SystemBO.Login` 已切到 `Services.GetRequiredService<T>()`，但 `JsonRpcExecutor`、`StaticApiEncryptionKeyProvider` 仍讀 static）
  - `BackendInfo.LoginAttemptTracker`（`SystemBO.Login` 已切到 `Services.GetService<T>()`，但 static 仍需保留供 `BackendInfoServiceProvider` 內部查表）
  - `BackendInfo.AccessTokenValidator`（`ApiAccessValidator` 仍用）
  - `BackendInfo.BusinessObjectFactory`（`JsonRpcExecutor` 仍用）
  - `BackendInfo.CacheDataSourceProvider` / `BackendInfo.EnterpriseObjectService`（其他層使用）
  - 加密金鑰、`LogWriter` / `LogOptions`
- 不引入 DI 容器（Phase 4）
- 不在 Bee.Repository 改 `new DbAccess(databaseId)` 為注入式（Phase 1 已備好 `IDbAccessFactory`，Phase 4 之後再接 DI）

**重要說明**：`IBeeContext.Services` 在 Phase 3 已存在介面 + 預設 `BackendInfoServiceProvider` 實作。SystemBO.Login 等特殊方法 **已可使用** `Services.GetService<T>()` 模式（無 BackendInfo.X 直接讀）。Phase 4 僅需把 `BackendInfoServiceProvider` 換成真 DI scope 的 `IServiceProvider`，BO 程式碼**完全不變**。

## 設計

### 1. `IBeeContext` 介面（Bee.Definition）

```csharp
namespace Bee.Definition
{
    /// <summary>
    /// Per-call context handed to business objects at construction time.
    /// Aggregates the cross-cutting services that virtually every BO method
    /// touches, plus an <see cref="IServiceProvider"/> escape hatch for rare
    /// per-method needs.
    /// </summary>
    public interface IBeeContext
    {
        /// <summary>The definition data access service.</summary>
        IDefineAccess DefineAccess { get; }

        /// <summary>The session-info access service.</summary>
        ISessionInfoService SessionInfoService { get; }

        /// <summary>Factory for building business objects (used for BO-to-BO calls).</summary>
        IBusinessObjectFactory BoFactory { get; }

        /// <summary>
        /// Escape hatch for resolving services not in the typed core members.
        /// Use sparingly — reserved for rare per-method needs (e.g. login-only
        /// helpers used by <see cref="SystemBusinessObject"/>.Login).
        /// Phase 3 wires this to a <c>BackendInfo</c>-backed provider; Phase 4
        /// swaps the impl for the real DI scope.
        /// </summary>
        IServiceProvider Services { get; }
    }

    /// <summary>
    /// Default <see cref="IBeeContext"/> implementation; a plain POCO assembled
    /// by <c>BusinessObjectFactory</c> at BO construction time.
    /// </summary>
    public sealed class BeeContext : IBeeContext
    {
        public required IDefineAccess DefineAccess { get; init; }
        public required ISessionInfoService SessionInfoService { get; init; }
        public required IBusinessObjectFactory BoFactory { get; init; }
        public required IServiceProvider Services { get; init; }
    }

    /// <summary>
    /// Minimal generic extensions on <see cref="IServiceProvider"/>; defined here
    /// to avoid taking a hard dependency on <c>Microsoft.Extensions.DependencyInjection.Abstractions</c>
    /// (Phase 4 may switch to that package).
    /// </summary>
    public static class BeeServiceProviderExtensions
    {
        public static T? GetService<T>(this IServiceProvider sp) where T : class
            => sp.GetService(typeof(T)) as T;

        public static T GetRequiredService<T>(this IServiceProvider sp) where T : class
            => sp.GetService<T>() ?? throw new InvalidOperationException(
                $"Required service of type {typeof(T)} not found in IBeeContext.Services.");
    }
}
```

#### Phase 3 內的 `BackendInfoServiceProvider`

`BusinessObjectFactory.BuildContext` 內使用一個 internal `IServiceProvider` 實作，把 lookup 轉發到 `BackendInfo.X` 靜態：

```csharp
namespace Bee.Business
{
    /// <summary>
    /// Phase 3 transitional <see cref="IServiceProvider"/> implementation that
    /// resolves a fixed set of services from <c>BackendInfo</c> statics. Phase 4
    /// replaces this with the real DI <c>IServiceProvider</c> from the request scope.
    /// </summary>
    internal sealed class BackendInfoServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IApiEncryptionKeyProvider)) return BackendInfo.ApiEncryptionKeyProvider;
            if (serviceType == typeof(ILoginAttemptTracker))      return BackendInfo.LoginAttemptTracker;
            if (serviceType == typeof(IAccessTokenValidator))     return BackendInfo.AccessTokenValidator;
            if (serviceType == typeof(IEnterpriseObjectService))  return BackendInfo.EnterpriseObjectService;
            if (serviceType == typeof(ICacheDataSourceProvider))  return BackendInfo.CacheDataSourceProvider;
            return null;
        }
    }
}
```

### 2. `IFormBoTypeResolver`（Bee.Business 內）

```csharp
namespace Bee.Business
{
    /// <summary>
    /// Resolves the concrete <see cref="FormBusinessObject"/>-derived type for a given progId.
    /// </summary>
    public interface IFormBoTypeResolver
    {
        /// <summary>
        /// Returns the concrete BO type for the given progId. Implementations may consult
        /// an XML mapping file or fall back to the framework's default <see cref="FormBusinessObject"/>.
        /// </summary>
        Type Resolve(string progId);
    }

    /// <summary>
    /// Default resolver — always returns <see cref="FormBusinessObject"/>. ERP applications can
    /// install a custom resolver that consults an XML mapping (per main plan future direction).
    /// </summary>
    public sealed class DefaultFormBoTypeResolver : IFormBoTypeResolver
    {
        public Type Resolve(string progId) => typeof(Form.FormBusinessObject);
    }
}
```

Phase 3 不引入 XML 映射檔（主計畫未決議題之一）；提供 `DefaultFormBoTypeResolver` 作為框架預設，ERP 應用可在後續 phase / 應用層替換。

### 3. `BusinessObject` base 與兩個子類

#### `BusinessObject`（Bee.Business 內）

```csharp
public abstract class BusinessObject : IBusinessObject
{
    private readonly IBeeContext _ctx;

    protected BusinessObject(IBeeContext ctx, Guid accessToken, bool isLocalCall = true)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        AccessToken = accessToken;
        IsLocalCall = isLocalCall;
    }

    public Guid AccessToken { get; }
    public bool IsLocalCall { get; }

    // 由 base 解包到 protected 屬性，子類用法 = 具名屬性（不需 ctx.X.Y）
    protected IDefineAccess DefineAccess => _ctx.DefineAccess;
    protected ISessionInfoService SessionInfoService => _ctx.SessionInfoService;
    protected IBusinessObjectFactory BoFactory => _ctx.BoFactory;
    protected IServiceProvider Services => _ctx.Services;   // escape hatch (use sparingly)

    // ExecFunc / ExecFuncAnonymous 簽章不變
    // ...
}
```

#### `SystemBusinessObject`

```csharp
public class SystemBusinessObject : BusinessObject, ISystemBusinessObject
{
    public SystemBusinessObject(IBeeContext ctx, Guid accessToken, bool isLocalCall = true)
        : base(ctx, accessToken, isLocalCall) { }

    public virtual GetCommonConfigurationResult GetCommonConfiguration(...)
    {
        var settings = DefineAccess.GetSystemSettings();   // ← ctx.DefineAccess（透過 base 屬性）
        // ...
    }

    public virtual LoginResult Login(LoginArgs args)
    {
        var tracker = Services.GetService<ILoginAttemptTracker>();   // ← 逃生口（Phase 3 BackendInfo-backed；Phase 4 swap 真 DI）
        // ... 既有邏輯
        byte[] encryptionKey = Services.GetRequiredService<IApiEncryptionKeyProvider>()
            .GenerateKeyForLogin();
        // ...
        SessionInfoService.Set(sessionInfo);   // ← ctx.SessionInfoService（typed core member）
        // ...
    }

    private GetDefineResult GetDefineCore(GetDefineArgs args)
    {
        // var access = BackendInfo.DefineAccess;  ← 刪除
        object value = DefineAccess.GetDefine(args.DefineType, args.Keys);   // ← ctx
    }

    private SaveDefineResult SaveDefineCore(SaveDefineArgs args)
    {
        // ...
        DefineAccess.SaveDefine(args.DefineType, defineObject, args.Keys);   // ← ctx
    }
}
```

**Phase 3 內 SystemBO 仍有 2 處 `BackendInfo.X` 讀取**（`LoginAttemptTracker`、`ApiEncryptionKeyProvider`），這是預期的 Phase 4 處理範圍。

#### `FormBusinessObject`

```csharp
public class FormBusinessObject : BusinessObject, IFormBusinessObject
{
    public FormBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
        : base(ctx, accessToken, isLocalCall)
    {
        ProgId = progId;
    }

    public string ProgId { get; }

    // DoExecFunc / DoExecFuncAnonymous body 不變
}
```

### 4. `BusinessObjectFactory` 重寫

```csharp
public class BusinessObjectFactory : IBusinessObjectFactory
{
    private readonly IFormBoTypeResolver _resolver;

    public BusinessObjectFactory() : this(new DefaultFormBoTypeResolver()) { }

    public BusinessObjectFactory(IFormBoTypeResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public IBusinessObject CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true)
    {
        var ctx = BuildContext();
        return new SystemBusinessObject(ctx, accessToken, isLocalCall);
    }

    public IBusinessObject CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true)
    {
        var type = _resolver.Resolve(progId);
        var ctx = BuildContext();
        return (IBusinessObject)Activator.CreateInstance(type, ctx, accessToken, progId, isLocalCall)!;
    }

    /// <summary>
    /// Builds an <see cref="IBeeContext"/> from the current <see cref="BackendInfo"/> statics.
    /// Phase 4 replaces this with DI-scoped construction.
    /// </summary>
    private IBeeContext BuildContext() => new BeeContext
    {
        DefineAccess = BackendInfo.DefineAccess,        // ← Phase 3 結束時 DefineAccess 屬性已移除
        SessionInfoService = BackendInfo.SessionInfoService,
        BoFactory = this,
    };
}
```

**矛盾點解決**：BuildContext 仍讀 `BackendInfo.DefineAccess`，但本 phase 要移除這個屬性。解法：BackendInfo 內保留一個 internal `_defineAccess` 欄位或 `BackendInfo.Initialize` 直接持有 IDefineAccess 並透過某種方式給 factory。

**簡化解法**：`BusinessObjectFactory` 改為 Initialize 時注入 IDefineAccess + ISessionInfoService（同 Phase 2 對 OC / DbConnectionManager 的處理模式），用反射呼叫 + Phase 3 內加 `Initialize(IDefineAccess defineAccess, ISessionInfoService sessionInfoService)`。

```csharp
public class BusinessObjectFactory : IBusinessObjectFactory
{
    private static IDefineAccess? _defineAccess;
    private static ISessionInfoService? _sessionInfoService;

    /// <summary>
    /// Installs the cross-cutting services used to build per-BO contexts.
    /// Called by BackendInfo.Initialize via reflection.
    /// </summary>
    public static void Initialize(IDefineAccess defineAccess, ISessionInfoService sessionInfoService)
    {
        _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
    }

    private readonly IFormBoTypeResolver _resolver;

    public BusinessObjectFactory() : this(new DefaultFormBoTypeResolver()) { }
    public BusinessObjectFactory(IFormBoTypeResolver resolver) { _resolver = resolver; }

    private static readonly IServiceProvider _services = new BackendInfoServiceProvider();

    private IBeeContext BuildContext()
    {
        if (_defineAccess == null || _sessionInfoService == null)
            throw new InvalidOperationException("BusinessObjectFactory.Initialize must be called first.");
        return new BeeContext
        {
            DefineAccess = _defineAccess,
            SessionInfoService = _sessionInfoService,
            BoFactory = this,
            Services = _services,   // Phase 3: BackendInfo-backed; Phase 4: replaced with real DI scope
        };
    }

    // CreateSystemBusinessObject / CreateFormBusinessObject 如前
}
```

`BackendInfo.Initialize` 透過反射 wire（同 Phase 2 處理 `CacheContainer.Initialize` 的模式）：

```csharp
InvokeStaticMethod("Bee.Business.BusinessObjectFactory, Bee.Business",
    "Initialize", new object[] { DefineAccess, SessionInfoService });
```

### 5. `IBusinessObjectFactory` 介面收緊

```csharp
public interface IBusinessObjectFactory
{
    IBusinessObject CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true);   // object → IBusinessObject
    IBusinessObject CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true);
}
```

`JsonRpcExecutor.CreateBusinessObject` 回傳型別保持 `object`（呼叫端只需要呼叫 `IBusinessObject.ExecFunc/ExecFuncAnonymous`），不影響。

### 6. `RepositoryInfo` 啟動流程

從 static ctor 改為顯式 `Initialize`：

```csharp
public static class RepositoryInfo
{
    public static ISystemRepositoryFactory? SystemFactory { get; set; }
    public static IFormRepositoryFactory? FormFactory { get; set; }

    /// <summary>
    /// Installs the repository factories from the given configuration.
    /// Called by BackendInfo.Initialize via reflection.
    /// </summary>
    public static void Initialize(BackendConfiguration configuration)
    {
        var components = configuration.Components;
        SystemFactory = CreateOrDefault<ISystemRepositoryFactory>(
            components.SystemRepositoryFactory, BackendDefaultTypes.SystemRepositoryFactory);
        FormFactory = CreateOrDefault<IFormRepositoryFactory>(
            components.FormRepositoryFactory, BackendDefaultTypes.FormRepositoryFactory);
    }

    private static T? CreateOrDefault<T>(string configured, string fallback) where T : class
        => AssemblyLoader.CreateInstance(string.IsNullOrWhiteSpace(configured) ? fallback : configured) as T;
}
```

移除 `BackendInfo.DefineAccess` 引用（原本透過它讀 SystemSettings，現在 caller 直接給 BackendConfiguration）。

### 7. `Bee.Db` DML helpers cleanup

#### `*FormCommandBuilder.cs`（5 個 provider）

**刪除** `(string progId)` ctor，保留 `(FormSchema)` ctor：

```csharp
// Before
public PgFormCommandBuilder(string progID)
{
    FormSchema = BackendInfo.DefineAccess.GetFormSchema(progID);
    // ...
}
public PgFormCommandBuilder(FormSchema formDefine) { ... }

// After
public PgFormCommandBuilder(FormSchema formDefine) { ... }   // 只剩這個
```

#### `IDialectFactory.cs` + 5 個 `*DialectFactory.cs`

**刪除** `CreateFormCommandBuilder(string progId)` 介面方法與所有實作。production code 無此呼叫，僅測試使用。

#### `SelectContextBuilder.cs`（Bee.Db.Dml）

接受 `IDefineAccess` 作為 ctor 參數，由呼叫端（`SelectCommandBuilder`）傳入：

```csharp
public class SelectContextBuilder
{
    private readonly IDefineAccess _defineAccess;

    public SelectContextBuilder(FormTable formTable, HashSet<string> usedFieldNames, IDefineAccess defineAccess)
    {
        // ...
        _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
    }

    private void AddTableJoin(...)
    {
        var srcFormDefine = _defineAccess.GetFormSchema(foreignKeyField.RelationProgId);   // ← ctx 傳入
        // ...
    }
}
```

#### `SelectCommandBuilder.cs`

接受 `IDefineAccess`、傳給 `SelectContextBuilder`。呼叫端（最終是 Repository / BO）負責取得 IDefineAccess。

#### `TableSchemaBuilder.cs`（Bee.Db.Schema）

同樣接受 `IDefineAccess`：

```csharp
public class TableSchemaBuilder
{
    private readonly IDefineAccess _defineAccess;

    public TableSchemaBuilder(string databaseId, IDefineAccess defineAccess)
    {
        DatabaseId = databaseId;
        _defineAccess = defineAccess;
        // ...
    }

    private TableSchemaComparer CreateComparer(string categoryId, string tableName)
    {
        // ...
        var defineTable = _defineAccess.GetTableSchema(categoryId, tableName);   // ← ctx 傳入
    }
}
```

#### `DatabaseRepository.cs`（Bee.Repository）

呼叫 `TableSchemaBuilder` 時傳入 `IDefineAccess`。`DatabaseRepository` 本身可從 `BackendInfo.DefineAccess`（即將移除）或從注入取得。

**簡化**：Phase 3 內讓 `DatabaseRepository` 透過 `RepositoryInfo.DefineAccess` 取得（暫時加 static 屬性，Phase 4 改 DI）—— 或更簡：Repository 從 `BackendInfo.DefineAccess` 讀（但這個 phase 結束時就要移除）。

**最簡解**：`DatabaseRepository` 在實例化時接受 `IDefineAccess`，呼叫端（用 RepositoryInfo.SystemFactory.CreateXxx() 的 SystemBO 等）有 IBeeContext.DefineAccess 可傳入。但這擴張了 Repository factory 介面...

實作時取決於 Repository factory 介面，詳細在實作時定。可能需要：

```csharp
// 簡化：DatabaseRepository 接受 IDefineAccess
// Factory.CreateDatabaseRepository(IDefineAccess defineAccess) — 但介面要改
```

或 `BackendInfo.Initialize` 中把 IDefineAccess 注入到 `Bee.Repository` 的某個靜態接收點（同 Phase 2 對 OC 的模式）。實作時細化。

### 8. `BackendInfo.cs` 變更

- **刪除** `public static IDefineAccess DefineAccess { get; set; }` 屬性
- `Initialize` 流程中新增：
  - 反射呼叫 `Bee.Business.BusinessObjectFactory.Initialize(defineAccess, sessionInfoService)`
  - 反射呼叫 `Bee.Repository.Abstractions.RepositoryInfo.Initialize(configuration)`
- 既有的 `DefineAccess` 反射建構流程移除（改由 factory 內部 wiring）

**注意**：Phase 2 留下的 `BackendInfo.Initialize` wire-up 已涵蓋 `CacheContainer` / `CacheInfo` / `DbConnectionManager`。Phase 3 額外加 `BusinessObjectFactory` 與 `RepositoryInfo` 的 wire-up（用同樣的反射 helper）。

但是 `DefineAccess` 還是要存在於 wire-up 流程中（Cache、DbConnectionManager 都需要）。解法：
- Phase 3 內 `BackendInfo.cs` 改為 internal 持有 `IDefineAccess` 不對外暴露，僅作為 wire-up 鏈中的中間值
- Initialize 流程：
  1. 建立 `IDefineStorage`（Phase 2 已有）
  2. 建立 `IDefineAccess`（Phase 2 已有 ResolveDefineAccess）—— 改存 local var，不再寫入 public static
  3. wire CacheContainer / CacheInfo / DbConnectionManager（傳 storage / defineAccess）
  4. wire BusinessObjectFactory / RepositoryInfo（傳 defineAccess / sessionInfoService / configuration）

```csharp
private static void InitializeComponents(BackendConfiguration configuration)
{
    var Components = configuration.Components;

    var storage = CreateOrDefault<IDefineStorage>(Components.DefineStorage, BackendDefaultTypes.DefineStorage);
    var defineAccess = ResolveDefineAccess(Components.DefineAccess, storage);
    // NOTE: defineAccess 不再寫入 public static —— 只做為 wire-up 鏈傳遞

    InvokeStaticMethod("Bee.ObjectCaching.CacheContainer, Bee.ObjectCaching", "Initialize", new object[] { storage });
    InvokeStaticMethod("Bee.ObjectCaching.CacheInfo, Bee.ObjectCaching", "Initialize", new object[] { configuration });

    var dbProvider = new DefineAccessDatabaseSettingsProvider(defineAccess);
    InvokeStaticMethod("Bee.Db.Manager.DbConnectionManager, Bee.Db", "Initialize", new object[] { dbProvider });

    // ─── Phase 3 新增 ───
    SessionInfoService = CreateOrDefault<ISessionInfoService>(
        Components.SessionInfoService, BackendDefaultTypes.SessionInfoService);

    InvokeStaticMethod("Bee.Business.BusinessObjectFactory, Bee.Business",
        "Initialize", new object[] { defineAccess, SessionInfoService });
    InvokeStaticMethod("Bee.Repository.Abstractions.RepositoryInfo, Bee.Repository.Abstractions",
        "Initialize", new object[] { configuration });

    // ─── 其餘 services（無相互依賴）───
    ApiEncryptionKeyProvider = CreateOrDefault<IApiEncryptionKeyProvider>(...);
    // ...
}
```

### 9. 測試影響

| 檔案 | 處理方式 |
|------|---------|
| BO 相關測試（`SystemBusinessObjectTests` 等）| 建構 BO 時透過 mock/test factory 取得 IBeeContext；或在測試初始化階段啟動 BackendInfo.Initialize（fixture 已有） |
| FormCommandBuilder 測試中使用 `(string progId)` ctor 的部分 | 改用 `(FormSchema)` ctor 並預先建立 FormSchema；無法簡化的測試直接刪除 |
| `DbDialectRegistryTests.CreateFormCommandBuilder_UnknownProgId_ThrowsFileNotFoundException` | 刪除（介面方法移除） |
| `SqliteDialectFactoryTests` / `PgDialectFactoryTests` 的 `CreateFormCommandBuilder` 測試 | 刪除 |
| `SqliteFormCommandBuilderTests` `(string)` ctor 測試 | 刪除（保留 `(FormSchema)` ctor 測試） |
| BackendInfoTests | 移除 `DefineAccess` 屬性相關斷言 |
| GlobalFixture | 不需改（CacheContainer wire-up + BackendInfo.Initialize 仍照同樣順序） |

## 改動清單

### 新增

| 檔案 | 內容 |
|------|------|
| `src/Bee.Definition/IBeeContext.cs` | 介面定義 |
| `src/Bee.Definition/BeeContext.cs` | 預設實作（POCO） |
| `src/Bee.Business/IFormBoTypeResolver.cs` | 介面定義 + `DefaultFormBoTypeResolver` 預設實作 |

### 修改 src（約 18 個）

| 檔案 | 修改 |
|------|------|
| `src/Bee.Business/BusinessObject.cs` | ctor 新增 IBeeContext；protected 屬性解包 |
| `src/Bee.Business/System/SystemBusinessObject.cs` | ctor 改 IBeeContext；GetSystemSettings / GetDefineCore / SaveDefineCore 改用 DefineAccess 屬性；Login 內 SessionInfoService.Set 改用屬性（LoginAttemptTracker + ApiEncryptionKeyProvider 保持 BackendInfo 讀取，Phase 4 處理） |
| `src/Bee.Business/Form/FormBusinessObject.cs` | ctor 改 IBeeContext |
| `src/Bee.Business/BusinessObjectFactory.cs` | 重寫；新增 static `Initialize`；BuildContext + ActivatorUtilities/Activator.CreateInstance |
| `src/Bee.Definition/IBusinessObjectFactory.cs` | 回傳型別 `object` → `IBusinessObject` |
| `src/Bee.Repository.Abstractions/RepositoryInfo.cs` | static ctor 改為顯式 Initialize |
| `src/Bee.Definition/BackendInfo.cs` | 移除 `DefineAccess` 屬性；Initialize 流程新增 BusinessObjectFactory + RepositoryInfo wire-up；SessionInfoService 改為先建立後 wire 給 BusinessObjectFactory |
| `src/Bee.Db/Dml/SelectContextBuilder.cs` | ctor 加 IDefineAccess 參數 |
| `src/Bee.Db/Dml/SelectCommandBuilder.cs` | 接收 IDefineAccess 並傳遞給 SelectContextBuilder |
| `src/Bee.Db/Schema/TableSchemaBuilder.cs` | ctor 加 IDefineAccess 參數 |
| `src/Bee.Repository/System/DatabaseRepository.cs` | 取得 IDefineAccess 並傳給 TableSchemaBuilder（具體機制實作時定） |
| `src/Bee.Db/Providers/PostgreSql/PgFormCommandBuilder.cs` | 移除 `(string progId)` ctor |
| `src/Bee.Db/Providers/Oracle/OracleFormCommandBuilder.cs` | 同上 |
| `src/Bee.Db/Providers/Sqlite/SqliteFormCommandBuilder.cs` | 同上 |
| `src/Bee.Db/Providers/MySql/MySqlFormCommandBuilder.cs` | 同上 |
| `src/Bee.Db/Providers/SqlServer/SqlFormCommandBuilder.cs` | 同上 |
| `src/Bee.Db/Providers/IDialectFactory.cs` | 移除 `CreateFormCommandBuilder(string)` 方法 |
| `src/Bee.Db/Providers/PostgreSql/PgDialectFactory.cs` | 移除實作 |
| `src/Bee.Db/Providers/Oracle/OracleDialectFactory.cs` | 同上 |
| `src/Bee.Db/Providers/Sqlite/SqliteDialectFactory.cs` | 同上 |
| `src/Bee.Db/Providers/MySql/MySqlDialectFactory.cs` | 同上 |
| `src/Bee.Db/Providers/SqlServer/SqlDialectFactory.cs` | 同上 |

### 修改 tests（約 8-10 個）

| 檔案 | 修改 |
|------|------|
| `tests/Bee.Db.UnitTests/DbDialectRegistryTests.cs` | 刪除 `CreateFormCommandBuilder_UnknownProgId_ThrowsFileNotFoundException` |
| `tests/Bee.Db.UnitTests/SqliteDialectFactoryTests.cs` | 刪除 progId 相關測試 |
| `tests/Bee.Db.UnitTests/PgDialectFactoryTests.cs` | 同上 |
| `tests/Bee.Db.UnitTests/SqliteFormCommandBuilderTests.cs` | 移除 `(string progId)` ctor 測試 |
| `tests/Bee.Db.UnitTests/OracleFormCommandBuilderValidProgIdTests.cs` | 刪除（整檔；只測 progId ctor） |
| `tests/Bee.Definition.UnitTests/BackendInfoTests.cs` | 移除 DefineAccess setter/getter 相關斷言 |
| `tests/Bee.Business.UnitTests/SystemBusinessObjectTests.cs` 等 | BO 建構改透過 factory；如需直接 new，建立 test BeeContext |

## 驗收標準

- [ ] `IBeeContext` 介面（含 `Services` 逃生口）與 `BeeContext` 預設實作存在於 `Bee.Definition`
- [ ] `BeeServiceProviderExtensions` 提供 `GetService<T>()` / `GetRequiredService<T>()` 泛型擴充
- [ ] `BackendInfoServiceProvider` 在 Bee.Business 內提供 Phase 3 暫時實作（Phase 4 替換）
- [ ] `IFormBoTypeResolver` + `DefaultFormBoTypeResolver` 存在於 `Bee.Business`
- [ ] `BusinessObject` base + `SystemBusinessObject` + `FormBusinessObject` ctor 簽章符合主計畫 §4 規範
- [ ] `BusinessObjectFactory.Initialize` 由 `BackendInfo.Initialize` 透過反射呼叫
- [ ] `IBusinessObjectFactory` 回傳型別為 `IBusinessObject`
- [ ] `RepositoryInfo` 使用顯式 Initialize（無 static ctor 對 BackendInfo.DefineAccess 的依賴）
- [ ] `BackendInfo.DefineAccess` 屬性已刪除
- [ ] 5 個 `*FormCommandBuilder` 的 `(string progId)` ctor 已刪除
- [ ] `IDialectFactory.CreateFormCommandBuilder(string)` 已刪除（含 5 個實作）
- [ ] `SelectContextBuilder` / `SelectCommandBuilder` / `TableSchemaBuilder` 接受 `IDefineAccess` 參數
- [ ] `./test.sh` 全綠
- [ ] GitHub Actions Build CI 通過
- [ ] `grep -n "BackendInfo\.DefineAccess" src/ tests/` 結果為 0

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| BO ctor 簽章改變影響所有測試的 `new SystemBusinessObject(token, ...)` 直接呼叫 | grep 全 repo 找出所有直接 new BO 的測試，改透過 `BackendInfo.BusinessObjectFactory.CreateSystemBusinessObject(...)` 或 mock context；測試 fixture (`GlobalFixture`) 已啟動 BackendInfo.Initialize，factory 可用 |
| `BusinessObjectFactory.Initialize` 必須在 `BackendInfo.Initialize` 流程中順序正確 | 與 Phase 2 的 reflection wire-up 同模式；錯誤訊息明確（getter throw if not initialized） |
| `DatabaseRepository` 取得 `IDefineAccess` 的具體機制不明 | 實作時若 Repository factory 介面要改動，影響面可能擴大；備選：暫時透過某個 internal helper 取得（與 Phase 4 DI 化的橋接）。實作時定 |
| `RepositoryInfo.SystemFactory!` 的 nullable warning 因為 static ctor 移除而變多 | 用 `null!` 顯式宣告或調整為 throw if not initialized 模式 |
| Bee.Db DML helpers 接受 IDefineAccess 後，原本可用「parameterless ctor」建構的點變多參 | grep 全 repo 確認所有 new SelectContextBuilder / TableSchemaBuilder 呼叫點都已調整 |
| BackendInfo.DefineAccess 既然不對外暴露，但 Phase 3 內部 wire-up 仍需要它 | InitializeComponents 內把 `defineAccess` 存 local var，僅作為 wire-up 鏈中間值；不對外公開（屬性已刪） |

## 提交策略

依使用者規範（[pull-request.md](../../.claude/rules/pull-request.md)）：本機可驗證，直接提交 main。

1. 本機跑 `dotnet build --configuration Release` + `./test.sh` 通過
2. 單一 commit 包含所有改動
3. push 後監測 GitHub Actions Build CI

預估 diff 量：800-1200 lines（含測試遷移）。檔案數約 30 個。

## 完成後狀態

- 主計畫頂部「Sub-plan 進度」表 Phase 3 狀態更新為 ✅ 已完成
- `BackendInfo.DefineAccess` 屬性刪除
- BO 開始遵循 `IBeeContext` 注入模式；後續 ERP 應用所有 BO 子類照此寫
- Phase 4 動工的基礎就緒：剩餘的 Phase 3 殘留 BackendInfo.X reads（5-6 個屬性）由 Phase 4 統一處理 + DI 容器導入
