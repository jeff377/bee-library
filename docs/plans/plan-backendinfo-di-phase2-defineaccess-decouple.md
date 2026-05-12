# 計畫：Phase 2 — ObjectCaching 與 DefineAccess 解耦（含 DefinePath / DatabaseSettings）

**狀態：✅ 已完成（2026-05-12）**

> 本文件為主計畫 [plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) 的 **Phase 2** sub-plan，獨立可 ship。
>
> 主計畫標示此 phase 為**最關鍵、最高風險**。本 sub-plan 採縮減範圍策略（選項 B）以維持單一 PR 可消化；`BackendInfo.DefineAccess` 靜態屬性本身的移除留待 Phase 3（BO/Repository 轉注入時自然移除）。

## 背景

### 主計畫定位

Phase 2 的目標是把 `Bee.Definition` 層的基礎配置與 `Bee.ObjectCaching` 層改為注入式，為後續 Phase 3（BO/Repository 注入）鋪路。

### 範圍取捨

主計畫第 192-204 行原本 Phase 2 包含「`IDefineAccess` 改為純 runtime 服務，由 DI 注入消費端」這個寬泛敘述。若嚴格解讀，會牽涉 39 處 `BackendInfo.DefineAccess` 引用跨 BO / Repository / Bee.Db DML 多層，PR 規模 40+ 檔案，超出單一 PR 可消化範圍。

實際採用的範圍策略（與使用者確認）：

| 動作 | 在 Phase 2 範圍 | 留待後續 |
|------|----------------|---------|
| 移除 `BackendInfo.DefinePath` + 引入 `PathOptions` | ✅ | — |
| 移除 `BackendInfo.GetDatabaseItem` + `ValidateDatabaseSettings` + 引入 `IDatabaseSettingsProvider` | ✅ | — |
| 移除 `BackendInfo.DefineStorage`，所有 OC 內部 storage 引用改注入 | ✅ | — |
| ObjectCaching 7 個 cache 類別 + `CacheContainer` + `CacheInfo` + `LocalDefineAccess` 改注入 | ✅ | — |
| **保留** `BackendInfo.DefineAccess` 靜態屬性 | — | Phase 3（隨 BO/Repository 轉注入移除）|
| Bee.Db DML helpers (`*FormCommandBuilder`、`TableSchemaBuilder`、`SelectContextBuilder`) 內 `BackendInfo.DefineAccess.X` 引用 | — | Phase 3（這些 helper 由 BO/Repo 呼叫，隨 caller 改注入時自然處理）|
| BO / Repository 內 `BackendInfo.DefineAccess.X` 引用 | — | Phase 3 |

### 設計策略

對於 `BackendInfo.DefinePath` 與 `DbConnectionManager` 等 static API 的替換，採 **C1 策略**：把 static state 從 `BackendInfo` 拆出，搬到該領域專屬的 static 類別（`DefinePathInfo` 自持 `PathOptions`、`DbConnectionManager` 自持 `IDatabaseSettingsProvider`），透過 `Initialize` 方法注入。

- 仍是 process-wide static，**但已從 `BackendInfo` 解耦**
- Phase 4 引入 DI 容器後，這些 static Initialize 改為 DI 註冊路徑（`IOptions<PathOptions>`、scoped provider）—— 自然演進
- Phase 2 不引入 instance-based API（避免改動所有 caller）

### `IDefineAccess` 實作的 wire-up 範圍

`BackendInfo.Initialize` 內處理的 `IDefineAccess` 實作只考慮 backend 場景：預設 `LocalDefineAccess`，或 XML 配置指定的後端自訂實作。`Bee.Api.Client.RemoteDefineAccess`（前端透過 JSON-RPC 取定義）不在 backend host 範圍，本 phase wire-up 不考慮。

## 目標

1. 移除 `BackendInfo.DefinePath` 屬性，引入 `PathOptions` POCO，由 `DefinePathInfo` 持有並提供
2. 移除 `BackendInfo.GetDatabaseItem` 方法、`BackendInfo.ValidateDatabaseSettings` 方法
3. 引入 `IDatabaseSettingsProvider` 介面 + 預設實作，由 `DbConnectionManager` 與 `TableUpgradeOrchestrator` 使用
4. 移除 `BackendInfo.DefineStorage` 屬性
5. `ObjectCaching` 6 個 storage-backed cache 類別接受 `IDefineStorage` 注入；`LocalDefineAccess` 同樣注入 `IDefineStorage`
6. `CacheContainer` 改為可注入式（接受 `IDefineStorage` 後建立 cache 實例）
7. `CacheInfo` 取消 static ctor 內讀 `BackendInfo.DefineAccess` 的依賴
8. `BackendInfo.Initialize` 流程重組：明確建立 `IDefineStorage` → `IDefineAccess`，並 wire up `DefinePathInfo`、`DbConnectionManager`、`CacheContainer`、`CacheInfo`

## 非目標（本 phase 不做）

- 不移除 `BackendInfo.DefineAccess` 靜態屬性（Phase 3 處理）
- 不改 `Bee.Db` 內 `BackendInfo.DefineAccess.GetFormSchema` / `GetTableSchema` 等 9 處引用（Phase 3 處理）
- 不改 BO / Repository 內 `BackendInfo.DefineAccess` 引用（Phase 3）
- 不引入 DI 容器（Phase 4）
- 不把 `DefinePathInfo` / `DbConnectionManager` 改為 instance-based API（保持靜態 API surface 不變）
- 不解決 `DatabaseSettings.Items` 集合本身的「process-wide 可變」性質——這是 test fixture 的 concern，與 production 設計分開處理

## 設計

### 1. `PathOptions` POCO

新增 `src/Bee.Definition/PathOptions.cs`：

```csharp
namespace Bee.Definition
{
    /// <summary>
    /// Path-related configuration. Currently holds <see cref="DefinePath"/>;
    /// may grow in later phases.
    /// </summary>
    public class PathOptions
    {
        /// <summary>
        /// Root directory for definition data files
        /// (SystemSettings.xml, DatabaseSettings.xml, FormSchema/, TableSchema/ 等).
        /// </summary>
        public string DefinePath { get; init; } = string.Empty;
    }
}
```

### 2. `DefinePathInfo` 重構

`src/Bee.Definition/DefinePathInfo.cs`：

```csharp
public static class DefinePathInfo
{
    private static PathOptions _options = new();

    /// <summary>
    /// Current path options snapshot. Exposed primarily for test helpers
    /// (e.g. <c>TempDefinePath</c>) that need to save and restore state.
    /// </summary>
    public static PathOptions CurrentOptions => _options;

    /// <summary>
    /// Installs the path options. Typically called once at host startup;
    /// test helpers may call this transiently to swap and restore paths.
    /// </summary>
    public static void Initialize(PathOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    // 既有 static API 不變，內部改讀 _options.DefinePath
    public static string GetSystemSettingsFilePath() => GetDefinePath("SystemSettings.xml");
    // ... 其他 helpers 同樣，內部用 _options.DefinePath
}
```

**呼叫端不變**：`DefinePathInfo.GetSystemSettingsFilePath()` 等所有呼叫處（src + tests）API 完全相同。

### 3. `MasterKeyProvider` 重構

`src/Bee.Definition/Security/MasterKeyProvider.cs:64-67` 目前讀 `BackendInfo.DefinePath` 解析相對路徑。改為：

```csharp
if (!Path.IsPathRooted(filePath))
    filePath = Path.Combine(DefinePathInfo.CurrentOptions.DefinePath, filePath);
```

### 4. `IDatabaseSettingsProvider`

新增 `src/Bee.Definition/IDatabaseSettingsProvider.cs`：

```csharp
namespace Bee.Definition
{
    /// <summary>
    /// Provides access to the current <see cref="DatabaseSettings"/> snapshot
    /// (with <see cref="DatabaseSettings.Items"/> populated). Replaces the
    /// former <c>BackendInfo.GetDatabaseItem</c> / <c>ValidateDatabaseSettings</c>
    /// static helpers.
    /// </summary>
    public interface IDatabaseSettingsProvider
    {
        /// <summary>Returns the current database settings.</summary>
        DatabaseSettings Get();

        /// <summary>Looks up the database item for the given identifier.</summary>
        /// <exception cref="KeyNotFoundException">When no matching item exists.</exception>
        DatabaseItem GetItem(string databaseId);

        /// <summary>
        /// Validates that the settings contain the framework-required
        /// <c>common</c> database item; throws if missing.
        /// </summary>
        void ValidateRequired();
    }

    /// <summary>
    /// Default <see cref="IDatabaseSettingsProvider"/> backed by an
    /// <see cref="IDefineAccess"/> instance.
    /// </summary>
    public sealed class DefineAccessDatabaseSettingsProvider : IDatabaseSettingsProvider
    {
        private readonly IDefineAccess _defineAccess;
        public DefineAccessDatabaseSettingsProvider(IDefineAccess defineAccess)
            => _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));

        public DatabaseSettings Get() => _defineAccess.GetDatabaseSettings();

        public DatabaseItem GetItem(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentNullException(nameof(databaseId));
            var settings = Get();
            if (!settings.Items!.Contains(databaseId))
                throw new KeyNotFoundException($"DatabaseItem '{databaseId}' not found.");
            return settings.Items[databaseId];
        }

        public void ValidateRequired()
        {
            var settings = Get();
            if (settings.Items == null || !settings.Items.Contains(Bee.Definition.Database.DbCategoryIds.Common))
                throw new InvalidOperationException(
                    $"DatabaseSettings must contain a DatabaseItem with Id='{Bee.Definition.Database.DbCategoryIds.Common}'.");
        }
    }
}
```

### 5. `DbConnectionManager` 接受 provider

`src/Bee.Db/Manager/DbConnectionManager.cs` 改為：

```csharp
public static class DbConnectionManager
{
    private static IDatabaseSettingsProvider? _provider;

    /// <summary>
    /// Installs the database settings provider. Must be called at host startup
    /// before any <see cref="GetConnectionInfo"/> invocation.
    /// </summary>
    public static void Initialize(IDatabaseSettingsProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Clear();   // invalidate cache when re-initialized
    }

    private static DbConnectionInfo CreateConnectionInfo(string databaseId)
    {
        if (_provider == null)
            throw new InvalidOperationException(
                "DbConnectionManager has not been initialized. Call DbConnectionManager.Initialize(provider) at startup.");

        var settings = _provider.Get();
        // ... 其餘邏輯不變
    }
}
```

### 6. `TableUpgradeOrchestrator` 同樣注入

`src/Bee.Db/Schema/TableUpgradeOrchestrator.cs:97` 改為呼叫 `IDatabaseSettingsProvider`（透過 ctor 注入或方法參數，待實作時定）。

最簡方案：把所需的 `DatabaseType` 改由呼叫端傳入，避免 `TableUpgradeOrchestrator` 自己查表。實作時確認。

### 7. ObjectCaching 注入策略

每個讀取 `BackendInfo.DefineStorage` 的 cache 類別加 ctor 參數：

```csharp
public class DatabaseSettingsCache : ObjectCache<DatabaseSettings>
{
    private readonly IDefineStorage _storage;
    public DatabaseSettingsCache(IDefineStorage storage)
        => _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    protected override DatabaseSettings? CreateInstance()
    {
        if (_storage is FileDefineStorage)
        {
            // 既有路徑：直接讀檔
            // ...
        }
        return _storage.GetDatabaseSettings();
    }
}
```

同樣處理：`DbCategorySettingsCache`、`FormSchemaCache`、`FormLayoutCache`、`TableSchemaCache`、`ProgramSettingsCache`。

`SystemSettingsCache` 不讀 storage（直接讀 XML 檔），不需要 ctor 參數。`SessionInfoCache` 不讀 storage（保持原樣）。

### 8. `CacheContainer` 重構

從靜態 Lazy fields 改為靜態 `Initialize(IDefineStorage)`：

```csharp
public static class CacheContainer
{
    private static IDefineStorage? _storage;
    private static SystemSettingsCache? _systemSettings;
    private static DatabaseSettingsCache? _databaseSettings;
    // ... 其他 cache fields

    public static void Initialize(IDefineStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _systemSettings = new SystemSettingsCache();
        _databaseSettings = new DatabaseSettingsCache(_storage);
        _programSettings = new ProgramSettingsCache(_storage);
        _dbCategorySettings = new DbCategorySettingsCache(_storage);
        _tableSchema = new TableSchemaCache(_storage);
        _formSchema = new FormSchemaCache(_storage);
        _formLayout = new FormLayoutCache(_storage);
        _sessionInfo = new SessionInfoCache();
    }

    public static SystemSettingsCache SystemSettings
        => _systemSettings ?? throw new InvalidOperationException("CacheContainer not initialized.");
    // ... 其他 getter 同樣 throw if null
}
```

### 9. `LocalDefineAccess` 接受 `IDefineStorage`

`src/Bee.ObjectCaching/LocalDefineAccess.cs` 改為：

```csharp
public class LocalDefineAccess : IDefineAccess
{
    private readonly IDefineStorage _storage;

    public LocalDefineAccess(IDefineStorage storage)
        => _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    public void SaveSystemSettings(SystemSettings settings)
    {
        string filePath = DefinePathInfo.GetSystemSettingsFilePath();
        XmlCodec.SerializeToFile(settings, filePath);
        var cache = new SystemSettingsCache();
        cache.Remove();
    }

    public void SaveDbCategorySettings(DbCategorySettings settings)
    {
        _storage.SaveDbCategorySettings(settings);   // 取代 BackendInfo.DefineStorage
        // ...
    }
    // ... 其餘 BackendInfo.DefineStorage 引用全部改為 _storage
}
```

### 10. `CacheInfo` 取消 `BackendInfo.DefineAccess` 依賴

`src/Bee.ObjectCaching/CacheInfo.cs` static ctor 內目前讀 `BackendInfo.DefineAccess.GetSystemSettings()`。改為 explicit Initialize：

```csharp
public static class CacheInfo
{
    public static ICacheProvider Provider { get; set; } = new MemoryCacheProvider();

    /// <summary>
    /// Initializes the cache provider from configuration. Caller controls when
    /// this is invoked (typically host startup, after settings are loaded).
    /// </summary>
    public static void Initialize(BackendConfiguration configuration)
    {
        var components = configuration.Components;
        Provider = CreateOrDefault<ICacheProvider>(
            components.CacheProvider, BackendDefaultTypes.CacheProvider)!;
    }

    private static T? CreateOrDefault<T>(string configured, string fallback) where T : class
    {
        var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
        return AssemblyLoader.CreateInstance(typeName) as T;
    }
}
```

移除 static ctor。Provider 預設仍是 `MemoryCacheProvider`，呼叫 `Initialize` 後才可能切換。

### 11. `BackendInfo` 變更

`src/Bee.Definition/BackendInfo.cs`：

- 移除 `DefinePath` 屬性
- 移除 `DefineStorage` 屬性
- 移除 `GetDatabaseItem(string)` 方法
- 移除 `ValidateDatabaseSettings()` 方法
- 保留 `DefineAccess` 屬性（Phase 3 處理）
- `Initialize(BackendConfiguration)` 內部 wire up 順序調整：

```csharp
public static void Initialize(BackendConfiguration configuration, bool autoCreateMasterKey)
{
    LogOptions = configuration.LogOptions;

    if (!SysInfo.IsSingleFile)
    {
        InitializeComponents(configuration);   // 建立服務（內部處理 storage → access）

        // wire OC 與 DbConnectionManager
        var dbProvider = new DefineAccessDatabaseSettingsProvider(DefineAccess);
        DbConnectionManager.Initialize(dbProvider);
        dbProvider.ValidateRequired();
    }

    InitializeSecurityKeys(configuration, autoCreateMasterKey);
}

private static void InitializeComponents(BackendConfiguration configuration)
{
    var components = configuration.Components;

    // 1. 建立 IDefineStorage（無依賴；只在 Initialize 流程內持有，不對外暴露）
    var storage = CreateOrDefault<IDefineStorage>(
        components.DefineStorage, BackendDefaultTypes.DefineStorage);

    // 2. 建立 IDefineAccess（可能吃 storage，也可能不吃）
    DefineAccess = ResolveDefineAccess(components.DefineAccess, storage);

    // 3. 注入 storage 到 OC 層
    CacheContainer.Initialize(storage);
    CacheInfo.Initialize(configuration);

    // 4. 其餘服務（無相互依賴）
    ApiEncryptionKeyProvider = CreateOrDefault<IApiEncryptionKeyProvider>(
        components.ApiEncryptionKeyProvider, BackendDefaultTypes.ApiEncryptionKeyProvider);
    AccessTokenValidator = CreateOrDefault<IAccessTokenValidator>(
        components.AccessTokenValidator, BackendDefaultTypes.AccessTokenValidator);
    // ... 其餘服務照舊
}

/// <summary>
/// Resolves the configured <see cref="IDefineAccess"/> implementation. Two ctor
/// shapes are supported — <c>(IDefineStorage)</c> and parameterless — covering the
/// dominant patterns without locking implementations to a specific dependency shape.
/// </summary>
private static IDefineAccess ResolveDefineAccess(string? typeName, IDefineStorage storage)
{
    if (string.IsNullOrWhiteSpace(typeName))
        return new LocalDefineAccess(storage);

    var type = Type.GetType(typeName)
        ?? throw new InvalidOperationException($"IDefineAccess type '{typeName}' not found.");

    var ctorWithStorage = type.GetConstructor(new[] { typeof(IDefineStorage) });
    if (ctorWithStorage != null)
        return (IDefineAccess)ctorWithStorage.Invoke(new object[] { storage });

    return (IDefineAccess?)Activator.CreateInstance(type)
        ?? throw new InvalidOperationException($"Failed to construct IDefineAccess: {typeName}");
}
```

**重點**：
- `IDefineStorage` 物件只在 `InitializeComponents` 區域變數內存活；不再有 `BackendInfo.DefineStorage` 公開靜態屬性
- `ResolveDefineAccess` 用反射偵測 ctor 簽章，不需特例字串比對

### 12. `BackendInfo.Initialize` 內 `DefinePathInfo.Initialize` 呼叫

外部 caller（`BackendInfo.Initialize` 之前）需要先 wire `DefinePathInfo`。修改 GlobalFixture：

```csharp
// 原：BackendInfo.DefinePath = Path.Combine(repoRoot, "tests", "Define");
DefinePathInfo.Initialize(new PathOptions
{
    DefinePath = Path.Combine(repoRoot, "tests", "Define")
});
```

`TempDefinePath` 改為 swap PathOptions（見後）。

## 改動清單

### 新增

| 檔案 | 內容 |
|------|------|
| `src/Bee.Definition/PathOptions.cs` | `PathOptions` POCO |
| `src/Bee.Definition/IDatabaseSettingsProvider.cs` | 介面 + `DefineAccessDatabaseSettingsProvider` 預設實作 |

### 修改

| 檔案 | 修改 |
|------|------|
| `src/Bee.Definition/DefinePathInfo.cs` | 內部持有 `PathOptions`，新增 `CurrentOptions` getter + `Initialize`；既有 static API 不變 |
| `src/Bee.Definition/Security/MasterKeyProvider.cs` | 兩處 `BackendInfo.DefinePath` 改為 `DefinePathInfo.CurrentOptions.DefinePath` |
| `src/Bee.Definition/BackendInfo.cs` | 移除 `DefinePath`、`DefineStorage`、`GetDatabaseItem`、`ValidateDatabaseSettings`；`Initialize` 重組 wire-up 順序，內部建立 storage + access 並注入到 OC + DbConnectionManager |
| `src/Bee.Db/Manager/DbConnectionManager.cs` | 改用 `IDatabaseSettingsProvider`；新增 `Initialize` 方法 |
| `src/Bee.Db/Schema/TableUpgradeOrchestrator.cs` | 移除 `BackendInfo.GetDatabaseItem` 呼叫，改為其他途徑取得 `DatabaseType`（實作時定） |
| `src/Bee.ObjectCaching/CacheContainer.cs` | 從 static Lazy 改為 `Initialize(IDefineStorage)` 方法；getter throw if 未初始化 |
| `src/Bee.ObjectCaching/CacheInfo.cs` | 移除 static ctor；新增 `Initialize(BackendConfiguration)` 方法 |
| `src/Bee.ObjectCaching/LocalDefineAccess.cs` | ctor 接受 `IDefineStorage`；所有 `BackendInfo.DefineStorage` 改為 `_storage` |
| `src/Bee.ObjectCaching/Define/DatabaseSettingsCache.cs` | ctor 接受 `IDefineStorage` |
| `src/Bee.ObjectCaching/Define/DbCategorySettingsCache.cs` | 同上 |
| `src/Bee.ObjectCaching/Define/FormSchemaCache.cs` | 同上 |
| `src/Bee.ObjectCaching/Define/FormLayoutCache.cs` | 同上 |
| `src/Bee.ObjectCaching/Define/TableSchemaCache.cs` | 同上 |
| `src/Bee.ObjectCaching/Define/ProgramSettingsCache.cs` | 同上 |
| `tests/Bee.Tests.Shared/GlobalFixture.cs` | 啟動序列改為先 `DefinePathInfo.Initialize`，後 `BackendInfo.Initialize`（內部會 wire 其他） |
| `tests/Bee.Tests.Shared/TempDefinePath.cs` | swap/restore `DefinePathInfo.CurrentOptions` 而非 `BackendInfo.DefinePath` |
| `tests/Bee.Definition.UnitTests/DefinePathInfoTests.cs` | `WithDefinePath` helper 改用 `DefinePathInfo.Initialize` |
| `tests/Bee.Definition.UnitTests/Storage/FileDefineStorageTests.cs` | 同樣的 swap pattern 改用 `DefinePathInfo` |
| `tests/Bee.Definition.UnitTests/BackendInfoTests.cs` | 移除涉及 `DefinePath` / `DefineStorage` / `GetDatabaseItem` / `ValidateDatabaseSettings` 的測試（其他保留） |
| `tests/Bee.Definition.UnitTests/Settings/DatabaseSettingsTests.cs` | 若有使用 `BackendInfo.GetDatabaseItem` 等改為新 API |
| `tests/Bee.Tests.Shared/TestSessionFactory.cs` | 若引用 `BackendInfo.DefineStorage` 改新路徑 |

預估改動 18-22 個檔案。

### 不需改

- 24 個 `new DbAccess(databaseId)` 呼叫點
- Bee.Db 內 5 個 `*FormCommandBuilder` + `TableSchemaBuilder` + `SelectContextBuilder`（Phase 3）
- BO / Repository 內 `BackendInfo.DefineAccess` 引用（Phase 3）
- 其他 `BackendInfo.DefineAccess.X` 外部呼叫（保留）

## 測試策略

### 既有測試影響

- `DefinePathInfoTests` 與 `FileDefineStorageTests`：try/finally swap `BackendInfo.DefinePath` 的 helper 改為 swap `DefinePathInfo` 的 `_options`。行為等價，斷言不變
- `BackendInfoTests`：涉及 `MaxDbCommandTimeout`（Phase 1 已處理）、`DefinePath`、`DefineStorage`、`GetDatabaseItem`、`ValidateDatabaseSettings` 的測試需移除（這些 API 不存在了）
- `CacheTests` 等 OC 測試：可能需要在 `[Collection("Initialize")]` 內已 setup 好 storage 注入路徑；`GlobalFixture` 改動會自動處理
- `SystemSettingsCacheTests`（PR #56 新增）：行為應不變

### 新增測試

依使用者偏好（簡單配置 / 注入 wire-up 不寫專屬測試），本 phase 不新增 unit test，僅靠既有測試套件覆蓋 wire-up 正確性。

### 整合驗證

- `dotnet build --configuration Release` 通過
- `./test.sh` 全套測試通過
- `grep -n "BackendInfo\.\(DefinePath\|DefineStorage\|GetDatabaseItem\|ValidateDatabaseSettings\)" src/ tests/` 結果為 0
- 至少手動驗證 `tests/Define/` fixture 在新 wire-up 下能正確被 SystemSettingsCache + DatabaseSettingsCache 讀取

## 驗收標準

- [ ] `BackendInfo.DefinePath` / `DefineStorage` / `GetDatabaseItem` / `ValidateDatabaseSettings` 皆已刪除
- [ ] `BackendInfo.DefineAccess` 仍存在（Phase 3 處理）
- [ ] `PathOptions` 與 `IDatabaseSettingsProvider` 介面存在於 `Bee.Definition`
- [ ] `DefinePathInfo` static API 完全相容（既有所有呼叫端不需修改）
- [ ] `DbConnectionManager` 透過 `IDatabaseSettingsProvider` 取得 settings；新增 `Initialize` 方法
- [ ] `LocalDefineAccess` 與 6 個 cache 類別接受 `IDefineStorage` 注入；內部無 `BackendInfo.*` 引用
- [ ] `CacheContainer` 與 `CacheInfo` 改用 explicit Initialize 模式
- [ ] `GlobalFixture` 與 `TempDefinePath` 已遷移到新 API
- [ ] `./test.sh` 全綠
- [ ] GitHub Actions Build CI 通過

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| `LocalDefineAccess` 從反射建構改為顯式建構，影響 `BackendDefaultTypes.DefineAccess` 替換路徑 | XML 宣告自訂 `IDefineAccess` 實作型別仍可運作（顯式 `Activator.CreateInstance` 傳入 storage）；測試一個自訂實作確認 |
| `CacheContainer.Initialize` 未呼叫時 getter throw —— 啟動序列若搞錯順序，會在第一次 cache 存取時爆炸 | `BackendInfo.Initialize` 內部明文 wire 順序；`GlobalFixture` 同步調整；錯誤訊息明確指出 Initialize 未呼叫 |
| `CacheInfo` 取消 static ctor 後，第一次存取 `Provider` 仍為 default（`MemoryCacheProvider`），可能與 XML 宣告不同 | `BackendInfo.Initialize` 內呼叫 `CacheInfo.Initialize(configuration)` 解決；Phase 2 期間 `BackendInfo.Initialize` 是 entry，保證順序 |
| `DbConnectionManager.Initialize` 未呼叫時 getter throw —— 啟動序列必須先 Initialize | 同 CacheContainer：`BackendInfo.Initialize` 內統一 wire；訊息明確 |
| `IDatabaseSettingsProvider.GetItem` 替代 `BackendInfo.GetDatabaseItem`，但 `TableUpgradeOrchestrator` 之前用 `GetDatabaseItem().DatabaseType` —— 直接呼叫端改用 provider | 確認所有 `BackendInfo.GetDatabaseItem` 呼叫端皆已轉換（grep 驗證） |
| 多個測試操弄 `BackendInfo.DefineStorage` static —— 改為 `LocalDefineAccess` 注入後，這類測試需重寫 | 列出所有受影響測試，逐一改造；改造成本應 < 半天 |
| Phase 2 PR 規模 18-22 檔案，review 負擔大 | 預估值已符合主計畫第 270 行 < 500 lines diff 的上限（多數變更為小修改）；commit message 詳述變更分組 |

## 提交策略

依使用者規範（[pull-request.md](../../.claude/rules/pull-request.md)）：本機可驗證環境，直接提交 main。

1. 本機跑 `dotnet build --configuration Release` + `./test.sh` 通過
2. 單一 commit 包含所有改動
3. push 後監測 GitHub Actions Build CI

預估 diff 量：< 600 lines（檔案多但多數為小改）。

## 完成後狀態

- 主計畫頂部「Sub-plan 進度」表 Phase 2 狀態更新為 ✅ 已完成
- `BackendInfo` 屬性剩：`LogWriter` / `LogOptions` / 4 個加密金鑰 / `ApiEncryptionKeyProvider` / `AccessTokenValidator` / `BusinessObjectFactory` / `CacheDataSourceProvider` / `DefineAccess` / `SessionInfoService` / `EnterpriseObjectService` / `LoginAttemptTracker`（10 個服務 + 4 個金鑰 + log）
- Bee.Definition 層的 path / DB settings static state 已從 `BackendInfo` 拆出
- `ObjectCaching` 內部 wire-up 改為注入式，無 `BackendInfo.*` 引用
- Phase 3 動工時的前提就緒：`IDefineAccess` 已是「無 static 依賴」實作，可由 DI 注入到 BO/Repo
