# 計畫：Phase 7 — DbConnectionManager 靜態 facade 移除 + DbAccess ctor DI 化

**狀態：✅ 已完成（2026-05-13）**

> 本文件為主計畫 [plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) 的 **Phase 7** sub-plan，獨立可 ship。在 Phase 6（`BackendInfo` / `BackendConfiguration` 空殼類別刪除）完成後執行。

## 背景

Phase 5 結束時 `[Collection("Initialize")]` / `GlobalFixture` / `BeeTestServices` / `TempDefinePath` / `DefinePathInfo` / `CacheContainer` 靜態 facade 全部移除，cache 與 IDefineAccess 改由 DI ctor 注入。但 `DbConnectionManager` 靜態 facade（`src/Bee.Db/Manager/DbConnectionManager.cs`）暫留，原因：

- 5 處 src 消費點仍呼叫 `DbConnectionManager.GetConnectionInfo / CreateConnection` 靜態方法
- 84 處測試 `new DbAccess(databaseId)` 呼叫點透過 `DbAccess` ctor 間接依賴靜態
- 改造需 cascade ctor 注入，scope 與 Phase 5 PR 5.4i 相當

Phase 5 PR 5.4i 為此引入 `[Collection("DbConnectionState")]` 序列化 `DbConnectionManagerTests` / `DbAccessFactoryTests`，是 Phase 5 唯一保留的「Initialize 系列以外」序列化點。

## 現況盤點（Phase 5 結束後）

### Production code 消費 DbConnectionManager 靜態

| 檔案 | 行 | 呼叫 |
|------|----|------|
| `src/Bee.Db/DbAccess.cs` | 39 | `GetConnectionInfo(databaseId)` — ctor 內 |
| `src/Bee.Db/Providers/Sqlite/SqliteTableSchemaProvider.cs` | 102 | `GetConnectionInfo(DatabaseId)` |
| `src/Bee.Db/Schema/TableUpgradeOrchestrator.cs` | 42, 96 | `GetConnectionInfo(databaseId)` × 2 |
| `src/Bee.Db/Schema/TableUpgradeOrchestrator.cs` | 100 | `CreateConnection(databaseId)` |
| `src/Bee.Db/Schema/TableSchemaBuilder.cs` | 34 | `GetConnectionInfo(databaseId)` |

### Production code 連帶要改的 ctor

`DbAccess(string databaseId, int maxCommandTimeout = 0)` ctor 內 line 39 呼叫靜態取連線資訊。改造後須改為接收 `IDbConnectionManager`（或 `DbConnectionInfo`）。

### Test 消費點

| 模式 | 數量 | 主要分布 |
|------|------|---------|
| `new DbAccess(databaseId)` | ~84 | 大量分布在 `Bee.Db.UnitTests` / `Bee.Repository.UnitTests` / `Bee.Business.UnitTests` |
| `DbConnectionManager.CreateConnection(...)` 直接呼叫 | ~10 | `DbAccessTests` / `DbAccessStringMethodTests` / `DbAccessTransactionTests` / `DbConnectionTests` |
| `DbConnectionManager.GetConnectionInfo / Remove / Clear / Contains / Count` | ~15 | 主要在 `Bee.Db.UnitTests/Manager/DbConnectionManagerTests`（測試對象就是 manager 本身） |
| `tests/Bee.Tests.Shared/SharedDatabaseState.cs:215` | 1 | bootstrap path 自己呼叫 static 建連線 |
| `[Collection("DbConnectionState")]` | 2 class | `DbConnectionManagerTests` / `DbAccessFactoryTests` |

### 已存在的基建（Phase 5 PR 5.3b）

- `IDbConnectionManager` 介面（`src/Bee.Db/Manager/IDbConnectionManager.cs`）
- `DbConnectionManagerService` 實作（DI 注入 `IDatabaseSettingsProvider`）
- `BeeFrameworkServiceCollectionExtensions.AddBeeFramework` 已註冊 `IDbConnectionManager` 為 Singleton
- `IDbConnectionManagerBootstrapper` / `DbConnectionManagerBootstrapper`（過渡 wire-up，把 DI singleton 寫入靜態 facade）

## 目標

1. 刪除 `DbConnectionManager` 靜態 facade，全 src consumer 改 ctor 注入 `IDbConnectionManager`
2. `DbAccess(string databaseId)` ctor 改為要求 `IDbConnectionManager`；測試 / 生產 caller 統一走 `IDbAccessFactory.Create(databaseId)`
3. 刪除 `IDbConnectionManagerBootstrapper` / `DbConnectionManagerBootstrapper`、`AddBeeFramework` 內對應註冊、`UseBeeFramework` 內 resolve
4. 廢除 `[Collection("DbConnectionState")]` + 對應 `CollectionDefinition` marker
5. Phase 5 plan 文件「DbConnectionManager 暫留」備註可標 ✅ 結案

## 非目標（本 phase 不做）

- 不改 `DbAccess(DbConnection externalConn, DatabaseType, ...)` ctor —— 外部連線注入路徑無 manager 依賴，保留不動
- 不重構 `IDbConnectionManager` 介面本身（API 沿用 PR 5.3b 設計）
- 不順手改 `DbProviderRegistry` / `DbDialectRegistry` 靜態 registry（兩者由 `SharedDatabaseState.EnsureRegistered` 一次寫入；不影響並行）

## 設計

### 1. DbAccess ctor 改造

**Before**：
```csharp
public DbAccess(string databaseId, int maxCommandTimeout = 0)
{
    var connInfo = DbConnectionManager.GetConnectionInfo(databaseId); // static
    DatabaseType = connInfo.DatabaseType;
    ...
}
```

**After**：
```csharp
public DbAccess(string databaseId, IDbConnectionManager connectionManager, int maxCommandTimeout = 0)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
    ArgumentNullException.ThrowIfNull(connectionManager);
    var connInfo = connectionManager.GetConnectionInfo(databaseId);
    DatabaseType = connInfo.DatabaseType;
    ...
}
```

外部連線 ctor `DbAccess(DbConnection, DatabaseType, int)` 不變。

### 2. DbAccessFactory 補注入

```csharp
public sealed class DbAccessFactory : IDbAccessFactory
{
    private readonly IDbConnectionManager _connectionManager;
    private readonly int _maxCommandTimeout;

    public DbAccessFactory(IDbConnectionManager connectionManager, int maxCommandTimeout = 0)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _maxCommandTimeout = maxCommandTimeout;
    }

    public DbAccess Create(string databaseId)
        => new DbAccess(databaseId, _connectionManager, _maxCommandTimeout);
}
```

`AddBeeFramework` 註冊 factory 時注入 manager（factory 已是 DI singleton）。

### 3. 5 src 消費點 ctor 注入

| 類別 | 改造 |
|------|------|
| `SqliteTableSchemaProvider` | 加 `IDbConnectionManager` ctor 參數；callers 由 `SqliteDialectFactory.CreateTableSchemaProvider` 注入 |
| `TableUpgradeOrchestrator` | 加 `IDbConnectionManager` ctor 參數；通常由 `SystemBusinessObject.UpgradeTableSchema` 透過 DI 取得 |
| `TableSchemaBuilder` | 加 `IDbConnectionManager` ctor 參數 |

各 caller 連帶調整：`SqliteDialectFactory` 等 dialect factory class（已是 DI-resolved instance）注入 manager 並傳給其建立的 schema provider；`TableUpgradeOrchestrator` / `TableSchemaBuilder` 的 caller（如 `SharedDatabaseState.EnsureSchema`、`SystemExecFuncHandler.UpgradeTableSchema`）改 ctor 注入 manager。

### 4. 測試遷移

**機械遷移：** 84 處 `new DbAccess(databaseId)` 改為 `_fx.GetRequiredService<IDbAccessFactory>().Create(databaseId)`。

可在 `BeeTestFixture` 加便利屬性減少冗長：
```csharp
public DbAccess NewDbAccess(string databaseId)
    => GetRequiredService<IDbAccessFactory>().Create(databaseId);
```

如此 84 處遷移即 `new DbAccess(id)` → `_fx.NewDbAccess(id)`。

**`DbConnectionManager.CreateConnection` 直接呼叫者**（~10 處）改為 `_fx.GetRequiredService<IDbConnectionManager>().CreateConnection(...)`。

**`DbConnectionManagerTests`** —— 測試對象本就是 manager，從測 static 改測 DI instance：

```csharp
public class DbConnectionManagerTests : IClassFixture<SharedDbFixture>
{
    private readonly IDbConnectionManager _manager;
    public DbConnectionManagerTests(SharedDbFixture fx)
    {
        _manager = fx.GetRequiredService<IDbConnectionManager>();
    }

    [Fact]
    public void GetConnectionInfo_ReplacesAllPlaceholders()
    {
        ...
        var info = _manager.GetConnectionInfo(id);
        ...
    }
}
```

`Clear` / `Remove` 等破壞性操作仍是 race 風險 —— 但 race 對象是 fixture instance 的 manager（非 process-wide），單 fixture instance 內測試序列即可，xUnit 自然 class-scoped 序列化滿足條件。`[Collection("DbConnectionState")]` 可移除。

**`DbAccessFactoryTests`** 同理：改用 fixture 注入 factory，跳脫 static facade。

### 5. SharedDatabaseState

`SharedDatabaseState.cs:215` 的 `DbConnectionManager.CreateConnection(databaseId)`：

```csharp
private static void VerifyConnection(string databaseId)
{
    using var conn = DbConnectionManager.CreateConnection(databaseId); // static
    conn.Open();
    ...
}
```

改為傳入 `IDbConnectionManager`：

```csharp
private static void VerifyConnection(string databaseId, IDbConnectionManager mgr)
{
    using var conn = mgr.CreateConnection(databaseId);
    conn.Open();
    ...
}
```

`EnsureSchemaAndSeed` / `EnsureDatabase` 等呼叫鏈順著補 manager 參數；caller `BeeTestFixtureBuilder.BuildServiceProvider` / `TestProcessBootstrap` 從 DI provider 取出 manager 傳入。

### 6. 刪除

- `src/Bee.Db/Manager/DbConnectionManager.cs`（靜態 facade）
- `src/Bee.Api.AspNetCore/Bootstrapping/IDbConnectionManagerBootstrapper.cs`
- `tests/Bee.Db.UnitTests/Manager/DbConnectionStateCollection.cs`

### 7. 簡化

- `BeeFrameworkServiceCollectionExtensions.AddBeeFramework` 移除 `IDbConnectionManagerBootstrapper` 註冊
- `BeeFrameworkApplicationBuilderExtensions.UseBeeFramework` 改為純粹 no-op（或整個刪除？視 API 相容性決定，可能保留空殼便於後續擴充）
- `TestProcessBootstrap.InitializeOnce` 移除 `provider.GetRequiredService<IDbConnectionManagerBootstrapper>()` 那行

## 執行步驟（建議切分）

### 7A：production code DI 化

- `DbAccess` / `DbAccessFactory` ctor 簽章改造
- 5 src 消費點注入 `IDbConnectionManager`
- `AddBeeFramework` 註冊與 dialect factory chain 補注入
- src build 綠

### 7B：test code 機械遷移

- Python script：`new DbAccess(id)` → `_fx.NewDbAccess(id)`（84 處）
- 補 `BeeTestFixture.NewDbAccess` 便利屬性
- 直接呼叫 `DbConnectionManager.X` 改 fixture 取 manager（~10 處）
- `DbConnectionManagerTests` / `DbAccessFactoryTests` 改測 DI instance
- 純測試 build 綠

### 7C：清理

- 刪除靜態 facade / bootstrapper / DbConnectionStateCollection
- `[Collection("DbConnectionState")]` 從 2 class 移除
- `SharedDatabaseState` 改傳入 manager
- AddBeeFramework / UseBeeFramework / TestProcessBootstrap 同步簡化

### 7D：驗證 + 文件

- 5x stress test 全綠
- `.claude/rules/testing.md` §「目前仍存在的窄序列化」段落更新（移除 DbConnectionState 條目）
- 更新 Phase 5 plan 內「DbConnectionManager 暫留」備註
- 量測：本機 test wall-clock 是否進一步加速（DbConnectionState 移除後 2 class 可並行）

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| **84 處 `new DbAccess(id)` 改造遺漏** | Python script 處理後 grep 確認 0 命中；compile error 自然回饋 |
| **`SqliteTableSchemaProvider` 由 `SqliteDialectFactory.CreateTableSchemaProvider(databaseId)` 建立 —— factory 是否能取得 manager？** | dialect factory 已是 DI-resolved instance；補 `IDbConnectionManager` ctor 參數即可。注意 `IDbDialectFactory` 介面方法簽章不要動，manager 由實作 ctor 注入 |
| **DI 容器中 factory ctor 簽章變動破壞 AddBeeFramework 註冊** | 改 `services.AddSingleton<IDbAccessFactory>(sp => new DbAccessFactory(sp.GetRequiredService<IDbConnectionManager>(), maxCommandTimeout))` |
| **`DbConnectionManagerTests.Clear_EmptiesAllCachedEntries`** —— 在 DI instance 上測試 Clear，但 fixture-scoped IDbConnectionManager 是 singleton；同 fixture 內測試序列 OK；不同 fixture instance 各自的 manager 自然隔離。**race 已自然消除** | 直接刪 `[Collection("DbConnectionState")]`；無需替代 collection |
| **xUnit 平行恢復後 1 class（每個 fixture instance 自己的 IDbConnectionManager）內的測試仍序列；但 `DbAccessFactoryTests` 與 `DbConnectionManagerTests` 分屬不同 class，彼此並行** | 兩 class 各自的 IDbConnectionManager 來自各自 fixture instance（不同 cache），不會撞 process-wide 狀態。Phase 5 PR 5.4i 的 race 根因是「兩個 class 透過 static 共用 manager」；DI 化後天然消除 |

## 完成標準

| 項目 | 標準 |
|------|------|
| `grep -rn "DbConnectionManager\." src tests --include="*.cs"` | 僅剩 `IDbConnectionManager` interface 提及 + `DbConnectionManagerService` 實作；無靜態 facade 引用 |
| `grep -rn "\[Collection(\"DbConnectionState\")\]"` | 0 命中 |
| `grep -rn "new DbAccess(" tests --include="*.cs"` | 0 命中（全走 factory）；或極少數明確說明為何不走 factory |
| `dotnet test` | 全綠、5x stress 全綠 |
| `.claude/rules/testing.md` | 「目前仍存在的窄序列化」段落移除 DbConnectionState 條目 |
| Phase 5 plan 文件 | 「DbConnectionManager 暫留」備註標記為已收尾、引用本 Phase 7 plan |

## 後續銜接

Phase 7 完成後，Bee.NET 框架的 DI 化里程碑：

- 所有 process-wide static facade（`BackendInfo` / `RepositoryInfo` / `DefinePathInfo` / `CacheContainer` / `DbConnectionManager`）已全部移除
- 唯一保留的 process-wide static 為 `SysInfo`（仍由 `TestProcessBootstrap.InitializeOnce` 一次寫入）、`CacheInfo.Provider`（cache backend，per-host 設定一次）、`DbProviderRegistry` / `DbDialectRegistry`（ADO.NET provider/dialect 註冊表，per-host 一次）、`Bee.Api.Client.ApiClientInfo.LocalServiceProvider`（待 Bee.Api.Client 重構處理）
- `BeeTestFixture` 不再依賴任何 process-wide bootstrap 步驟（除了 `SysInfo.Initialize` 與三個 registry 註冊）
- 全 repo 0 處 `[Collection("...")]` 序列化要求
- xUnit 平行完全恢復

## 未決議題（待實作展開時決定）

- 是否同步整理 `IDbDialectFactory` 介面與其實作 family 的 ctor 注入？（多數 factory 已是 DI-resolved，補 `IDbConnectionManager` 不複雜）
- `BeeFrameworkApplicationBuilderExtensions.UseBeeFramework` 在無 bootstrapper 後是否完全保留空殼？保留可供未來擴充（middleware 註冊等），刪除則更乾淨。傾向保留（API 相容性）
- 是否需要 `BeeTestFixture.NewDbAccess(id)` 便利屬性？傾向**需要** —— 84 處遷移寫起來短、可讀性高
