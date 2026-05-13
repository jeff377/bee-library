# 計畫：Phase 5 — 測試基礎設施重寫（per-class ServiceProvider、廢除 Initialize collection / TempDefinePath、並行恢復）

**狀態：🚧 進行中（架構基建 PR 5.1–5.4d 完成，剩 bulk migration + 清理）**

> 本文件為主計畫 [plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) 的 **Phase 5** sub-plan，獨立可 ship。

## 實作進度

| PR | 主題 | Commit | 備註 |
|----|------|--------|------|
| 5.1 | BackendInfo 屬性清空（Logging 死碼 / DatabaseSettingsCryptor / SecurityKeys） | `5037c128` | ✅ |
| 5.1b | SysInfo.IsDebugMode 推下 / 廢除 `[Collection("SysInfo")]` | `188f9fe1` | ✅ |
| 5.2 | DefinePathInfo → PathOptions 注入（static 改為 shim） | `de28bc2e` | ✅ 偏離：static facade 暫留至 5.7 |
| 5.3a | RepositoryInfo 徹底刪除 + 4 consumers ctor/Services 注入 | `e6e0c09f` | ✅ |
| 5.3b | IDbConnectionManager interface + DbConnectionManagerService | `067f9d59` | ✅ 偏離：static facade 暫留至 5.7 |
| 5.3c | ICacheContainer interface + CacheContainerService | `d5a4f873` | ✅ 同上 |
| 5.4a | BeeTestFixture / BeeTestFixtureBuilder 骨幹 | `40f2fcda` | ✅ |
| 5.4b | SharedDatabaseState 抽出 + UseSharedDatabases() | `6f1a90c5` | ✅ |
| 5.4c | SessionInfoService 改 ctor 注入 ICacheContainer | `f761c393` | ✅ |
| 5.4d | Cache key prefix 隔離（opt-in），達成真 per-fixture 資料隔離 | `9bfac037` + `93ee78a9` | ✅ fix follow-up：CacheContainerService 預設不帶 prefix，BeeTestFixture 才透過 service replacement 加上 |
| 5.4e | `Bee.Api.AspNetCore.UnitTests` 遷移至 `IClassFixture<BeeTestFixture>`（3 class / 11 tests） | `52038841` | ✅ 新增 `TestSessionFactory.CreateAccessToken(BeeTestFixture, ...)` overload；刪除 `GlobalCollection.cs` |
| 5.4f | `Bee.Definition.UnitTests` 廢除 `[Collection("Initialize")]`（4 class）+ 4 個 TempDefinePath sites | `3cfd05c4` | ✅ `DefinePathInfoTests` 重寫為 `PathOptionsFilePathTests`（測 PathOptions instance）；`SystemSettingsLoaderTests`/`MasterKeyProviderTests` 改用 local temp-dir helper；`FormSchemaTests`/`FileDefineStorageTests` 純脫 Collection；刪除 `GlobalCollection.cs` |
| 5.4g | `Bee.ObjectCaching.UnitTests` 部分遷移：14/16 TempDefinePath sites + 2 class 脫 Collection | `fd10f117` | ✅ `LocalDefineAccessSaveTests` 全脫 Collection + 14 個 TempDefinePath → local TempDir；`SessionInfoServiceTests` 脫 Collection；`CacheTests`/`LocalDefineAccessTests` 改 `IClassFixture<BeeTestFixture>` hybrid（仍保 Collection 等 5.7 cache 注入 PathOptions）；修復 `MemoryCacheProviderTests` 中 PhysicalFileProvider watcher 在 `Path.GetTempPath()` 根目錄被平行測試誤觸發的 race（改用獨立子資料夾）；2 個 cache file-load 測試（`DatabaseSettingsCacheTests`/`SystemSettingsCacheTests`）暫留 TempDefinePath + Collection，待 5.7 |
| 5.4h | `Bee.Api.Core.UnitTests` 全脫 Collection + `Bee.Api.Client.UnitTests` 全脫 Collection | `2145cd2b` | ✅ 新增 `SharedDbFixture : BeeTestFixture` 助手（`UseSharedDatabases()`）；Api.Core 4 class 全遷移（2 純邏輯 drop Collection、2 改 `IClassFixture<BeeTestFixture>`，含 `JsonRpcExecutorTests` 走 `TestSessionFactory.CreateAccessToken(_fx)`）；Api.Client 3 class 全遷移（`SystemApiConnectorTests` → `IClassFixture<SharedDbFixture>`、`RemoteDefineAccessTests` → `IClassFixture<BeeTestFixture>`、`ApiConnectValidatorTests` drop Collection）；刪除兩專案 `GlobalCollection.cs` |
| 5.4i | `Bee.Db.UnitTests` + `Bee.Repository.UnitTests` 全脫 `[Collection("Initialize")]`（49 class，Python script + 4 outlier manual）| `5b1cc99d` | ✅ 全部改 `IClassFixture<SharedDbFixture>`；4 個 outlier 手動處理（DbDialectRegistryTests 2 兄弟 class、SqlTableSchemaProvider/OracleTableSchemaProvider 2 兄弟 class、DbProviderRegistryTests 嵌套類別）；新增 `[Collection("DbConnectionState")]` 序列化 `DbConnectionManagerTests`/`DbAccessFactoryTests`（共享 process-wide `DbConnectionManager` 快取，`Clear()` 等破壞性操作會 race，待 5.7 DI 化後脫除）；刪除兩專案 `GlobalCollection.cs`；本機 5x stress test 全綠 |
| 5.4j | `Bee.Business.UnitTests` 全脫 `[Collection("Initialize")]`（11 class，Python script） | （本 PR） | ✅ 全部改 `IClassFixture<SharedDbFixture>`；`SystemBusinessObjectTests` 既有空 ctor 刪除（xUnit 限一個 public ctor）；既有 `BeeTestServices.GetRequiredService<T>()` / `TestBeeContext.Create()` 保留待 PR 5.5；刪除 `GlobalCollection.cs`；本機 5x stress test 全綠（108/109 + 1 skip） |

## 偏離原計畫紀要

實作過程中採用了幾個原計畫沒寫的折衷：

1. **三個 static class 保留為 shim**：原計畫想在各 PR 內直接刪除 `DefinePathInfo` / `CacheContainer` / `DbConnectionManager` 靜態類別，但 5+ 個 src consumer + 66+ 個測試呼叫點需同步遷移；硬刪會與後續 PR 5.4 fixture 重寫衝突，因此改採「保留 thin shim、PR 5.7 集中清理」。原則上維持向後相容，consumer 邊遷移、shim 邊縮窄。
2. **Cache prefix 改 opt-in 而非預設**：PR 5.4d 起初讓 `CacheContainerService` 每 instance 自動帶 `cc_{Guid}` prefix，結果破壞 `GlobalFixture` 的 bootstrap-then-DI handoff（bootstrap 寫的 DatabaseItem key 與 DI singleton prefix 不同 → KeyNotFoundException）。fix 後改為「預設空 prefix、`BeeTestFixtureBuilder` 才用 `services.Replace` 注入帶 prefix 的實例」。
3. **BeeTestFixture 與 GlobalFixture 雙軌共存**：BeeTestFixture ctor 內 `_ = new GlobalFixture();` 觸發既有 once-init lock 共享 process-wide static 初始化。原計畫想完全替換，實際採漸進式雙軌（避免 fixture / shim 同步刪改）。

## 剩餘工作

機械型 bulk migration，依測試專案分批；每個 PR 內單一 test project 完整遷移，build 綠：

### PR 5.4e–5.4j（按專案遷移，依複雜度排序）

每個 PR 約 5–15 個 test class，內容相似：

- `[Collection("Initialize")]` / `BaseTests` 繼承 → `IClassFixture<BeeTestFixture>` + ctor 注入 `_fx`
- `BeeTestServices.GetRequiredService<T>()` → `_fx.GetRequiredService<T>()`
- `TestBeeContext.Create()` → `TestBeeContext.Create(_fx)`（需新增 `TestBeeContext.Create(BeeTestFixture)` overload）
- `TestSessionFactory.CreateAccessToken(...)` → 新增 `(BeeTestFixture, ...)` overload
- `TempDefinePath` 使用點 → fixture subclass `WritableDefineFixture : BeeTestFixture` 配 `UseTempDefinePath()`，或單測試局部 `new BeeTestFixture(b => b.UseTempDefinePath())`

建議順序（由簡至難）：

| PR | 範圍 | 估約 |
|----|------|------|
| 5.4e | `Bee.Api.AspNetCore.UnitTests` (1 class, 11 tests) | 入門 PoC |
| 5.4f | `Bee.Definition.UnitTests` 內 BeeTestFixture-using 部分（剩 ~7 class 帶 `[Collection("Initialize")]`） | small |
| 5.4g | `Bee.ObjectCaching.UnitTests`（含 TempDefinePath 16 sites + LocalDefineAccess 寫檔測試） | 中 |
| 5.4h | `Bee.Api.Core.UnitTests` + `Bee.Api.Client.UnitTests`（後者含 `ApiClientInfo.LocalServiceProvider` 限制） | 中 |
| 5.4i | `Bee.Repository.UnitTests` + `Bee.Db.UnitTests`（含 `[DbFact]` 整合 + 66 個 `new DbAccess(id)` 呼叫點） | 大 |
| 5.4j | `Bee.Business.UnitTests`（最多 BO test，含 `TestBeeContext.CreateWithOverrides` 使用） | 大 |

### PR 5.5–5.7 清理

只能在所有 test project 遷移完成後執行：

- **PR 5.5**：刪除 `BeeTestServices` static class / `TestBeeContext.Create()` 無參數 overload / `TestSessionFactory.CreateAccessToken(...)` 無 fixture overload（grep 確認 0 caller 後）
- **PR 5.6**：刪除 `TempDefinePath` / `BaseTests` / `GlobalCollection` / `GlobalFixture` / `DbGlobalFixture`；移除 `BeeTestFixture` ctor 內 `_ = new GlobalFixture();` 觸發（須改為直接呼叫 `SharedDatabaseState.EnsureRegistered`）
- **PR 5.7**：刪除 `DefinePathInfo` / `CacheContainer` 靜態 facade / `DbConnectionManager` 靜態 facade / `DbConnectionManagerBootstrapper` / `CacheBootstrapper`；`AddBeeFramework` 簡化（不再需要 bootstrappers 把 DI singleton 寫入 static）

### PR 5.8 文件 + CI

- `docs/architecture-overview.md` 反映新測試 fixture 模型
- `.claude/rules/testing.md` 更新「全域狀態與平行安全」段落
- `MEMORY.md` 更新 `feedback_simple_config_no_dedicated_test` 等記憶
- `xunit.runner.json` 確認 `parallelizeTestCollections: true`
- CI 時間量測

## 背景

### 主計畫定位

Phase 4 已把後端宿主轉入 DI 容器，`BackendInfoServiceProvider` 退場，BO 透過真 `IServiceProvider` 取得服務。但測試端仍維持 v4.x 設計：

- `GlobalFixture` 在 process scope 內 build **一個** `IServiceProvider`，暴露為 `BeeTestServices.Provider` 靜態取用點
- 所有測試類別掛 `[Collection("Initialize")]`（透過 `BaseTests` 或直接標記），強制 xUnit 在同一 collection 內串行
- 寫檔測試（`SaveDefine` 系列、`MasterKeyProvider` 預設檔測試）一律用 `TempDefinePath` 暫時切換 `DefinePathInfo` 的 process-wide static
- 修改 `BackendInfo.ConfigEncryptionKey` / `BackendInfo.LogOptions` 的測試以 `try/finally` 還原；修改 `SysInfo.IsDebugMode` 的測試另立 `[Collection("SysInfo")]` 串行

結果：

- **CI 時間因 collection 串行被放大**：159 個測試引用點集中在 `Initialize` collection，無法跨類別並行
- **隱性平行陷阱**：未來新增測試類別若忘了 `[Collection("Initialize")]`，會與其他 collection 並行存取 process-wide static，產生隨機失敗（測試規範 `testing.md` §「全域狀態與平行安全」已記錄過 `SysInfo` 的踩雷案例）
- **Phase 4 留下的轉接層仍在**：`BeeTestServices` static holder、`ApiClientInfo.LocalServiceProvider` static holder —— 主計畫 §「相容性策略」明確禁止「任何 static `IServiceProvider` 持有點」，這兩處本來就標記為 transitional

### 為何「per-class ServiceProvider」是正確終點

xUnit 預設 collection-level parallel：**不同 test class 自然平行**，同一 class 內串行。如果每個 test class 自己持有一個 `IServiceProvider`，所有依賴都從這個 provider 解析，那麼：

- 兩個並行 test class 各自的 `ISessionInfoService` / `IDefineAccess` / `ICacheDataSourceProvider` 是不同實例 → 不會 cross-contaminate
- `TempDefinePath` 不再需要：fixture 直接構造一個 `IDefineAccess` 指向 per-class 暫存目錄；測試結束 fixture dispose 一次清掉
- `[Collection("Initialize")]` 不再需要：xUnit 預設 collection-per-class 已足夠隔離
- `[Collection("SysInfo")]` 不再需要：`SysInfo.IsDebugMode` 推下到 `ApiPayloadOptionsFactory` ctor 參數後，測試在 fixture 構造階段直接指定

### 現況盤點（Phase 4 結束後）

| 類別 | 引用點 | Phase 5 處理 |
|------|--------|--------------|
| `BackendInfo.LogOptions`（src × 1、tests × 1） | `DbAccessLogger`、`DbAccessLoggerTests` | 注入 `LogOptions` 或 `IOptions<LogOptions>`；刪除 `BackendInfo.LogOptions` 屬性 |
| `BackendInfo.ConfigEncryptionKey`（src × 2、tests × ~14） | `DatabaseSettings.{Before/After}Serialize`、`DatabaseSettingsTests` | 注入機制（見設計 §2.2）；刪除屬性 |
| `BackendInfo.ApiEncryptionKey` / `CookieEncryptionKey` / `DatabaseEncryptionKey`（src × 4） | `BeeFrameworkServiceCollectionExtensions.InitializeSecurityKeys` 內部寫入；`StaticApiEncryptionKeyProvider` 讀 `ApiEncryptionKey`；其他三個目前 prod 0 caller | 全部改為 DI Options 或 ctor 注入；刪除四個屬性 |
| `BackendInfo.LogWriter`（src × 0、tests × 0） | 無消費者 | 直接刪除 |
| `DefinePathInfo._options` static（src × 17、tests × ~25 含 TempDefinePath） | `FileDefineStorage`、`MasterKeyProvider`、ObjectCaching 6 個 cache、`LocalDefineAccess`、`DbConnectionManager` 透過 IDatabaseSettingsProvider 間接 | 改為 `PathOptions` 注入式 service；callers 改 ctor 注入；`DefinePathInfo` static facade 撤掉 |
| `CacheContainer` 9 個 static 屬性（src 多處） | ObjectCaching / Business / Repository 直接 grep `CacheContainer.*` | 改為 `ICacheContainer` 注入式介面（內部仍可保留快取實作） |
| `DbConnectionManager` static（src × 6） | `Bee.Db` 內部 + `DbConnectionManagerBootstrapper` | 改為 `IDbConnectionManager` 注入式介面，scoped or singleton |
| `RepositoryInfo` static（src × 4） | `Business`、`Repository`、bootstrappers | 改為 ctor 注入兩個 factory；刪除 `RepositoryInfo` 類別 |
| `BeeTestServices.Provider` static | tests × 36 | 換為 `BeeTestFixture.Provider` per-class 注入；刪除靜態 |
| `ApiClientInfo.LocalServiceProvider` static | `Bee.Api.Client.LocalApiProvider` × 1（src） | Phase 5 不處理（屬 Bee.Api.Client 近端模式重構，主計畫 §「範圍邊界」） |
| `SysInfo.IsDebugMode` static | `ApiPayloadOptionsFactory`、3 個測試類掛 `[Collection("SysInfo")]` | 推下到 `ApiPayloadOptionsFactory` ctor 參數；廢除 SysInfo collection |

### 規模評估

- **新增類別 / 介面**：6（`ICacheContainer`、`IDbConnectionManager`、`IPathOptionsProvider` 或同等、`BeeTestFixture`、`BeeTestScope`、若需要的 logging options 介面）
- **改造的 src 檔案**：~30（DefinePathInfo 17 callers + CacheContainer 消費端 + DbConnectionManager 消費端 + ApiPayloadOptionsFactory + DbAccessLogger + DatabaseSettings）
- **改造的 tests**：~95 個測試類別（含 `[Collection("Initialize")]` 30 個 + TempDefinePath 16 個 + SysInfo 3 個 + BeeTestServices.Provider 36 個 + DbFact/DbTheory 約 30 個）
- **PR 切分**：本 sub-plan 一次性 phase（按主計畫 §「相容性策略」），但建議 6 個分階段 PR 在 phase 內依序合入（單一 phase 整體在 main 上保持 build 綠）。詳見 §10「PR 切分」

## 目標

1. 後端宿主與測試 fixture 共用同一份 DI 註冊邏輯，**所有 framework 服務的 lifecycle 都在 `IServiceProvider` 內**
2. 測試 fixture 改為「per-class」模式：每個測試類別自帶一個 `BeeTestFixture`，內含一個 `IServiceProvider` 與必要的 per-class 暫存目錄／資料庫資料
3. 廢除 `[Collection("Initialize")]`、`BaseTests`、`GlobalCollection`、`TempDefinePath`、`[Collection("SysInfo")]`、`BeeTestServices` static、`ApiClientInfo.LocalServiceProvider`（後者僅在 Bee.Api.Client 近端模式範圍內可以接受暫留 — 詳見 §「非目標」）
4. xUnit collection-level parallel 完全恢復；CI 預期相較 v4.x 不退步、理想加速
5. 主計畫 §「成功標準」對 Phase 5 對應的兩條完成：
   - [x] `dotnet test` 不依賴 `GlobalFixture` 或 `[Collection("Initialize")]`
   - [x] `grep -r "BackendInfo\." src/ tests/` 結果僅剩 `BackendInfo.cs` 本身

## 非目標（本 phase 不做）

主計畫 Phase 6 處理：

- **不刪除 `BackendInfo.cs` 類別本身**：本 phase 把剩餘 6 個屬性（`LogOptions`、`LogWriter`、4 個加密金鑰）都清空後，`BackendInfo` 變為空殼類別，Phase 6 一次性刪除。如此 Phase 5 PR 內不需處理 Bee.Definition assembly 公開型別移除的細節
- **不引入 `WebApplicationFactory<TStartup>`**：本框架沒有 `Startup` / `Program` 入口；測試直接以 `IServiceCollection` + `AddBeeFramework` 構造 provider，不需 ASP.NET host model
- **不重寫 `Bee.Api.Client` 近端模式**：`ApiClientInfo.LocalServiceProvider` 是 client 端拿後端 provider 的靜態 holder，主計畫 §「範圍邊界」已說明 client 重構不在本計畫範圍。Phase 5 把 holder 從「process-wide」改為「per-test 寫入」即可（fixture 構造時設值、dispose 時還原）—— 短期治標，Bee.Api.Client 重構時治本
- **不引入 `xUnit.v3`**：v3 的 collection / fixture 模型對 per-class 並行更友善，但升級成本大，與本 phase 解耦
- **不改動 `[DbFact]` / `[DbTheory]` / `[LocalOnlyFact]` 機制**：環境變數判斷邏輯不變，僅 fixture 內 wiring 改造

## 設計

### 1. 整體架構

#### 1.1 從「process-wide static + bootstrap」到「DI-resolved scope」

Phase 4 的 ASP.NET host 啟動流程是：

```
AddBeeFramework
  → 註冊 IDefineStorage / IDefineAccess / ... 9 個 service
  → 註冊 3 個 bootstrapper（ICacheBootstrapper / IDbConnectionManagerBootstrapper / IRepositoryInfoBootstrapper）
BuildServiceProvider
UseBeeFramework
  → eager-resolve 3 個 bootstrapper，把 CacheContainer / DbConnectionManager / RepositoryInfo
    三個 process-wide static 寫入；之後消費端透過 static accessor 取用
```

問題是 bootstrapper 把資料**寫到** static 而不是**包進** service 實例 —— 等於把 DI 容器當「初始化觸發器」用，runtime 還是 Service Locator。

Phase 5 目標：

```
AddBeeFramework
  → 註冊 IDefineStorage / IDefineAccess / ICacheContainer / IDbConnectionManager / ISystemRepositoryFactory / IFormRepositoryFactory / ... 全部 DI service
BuildServiceProvider  ← 構造好後 provider 就是 single source of truth
（消費端透過 ctor 注入；沒有 bootstrapper、沒有 static holder）
```

消費端不再讀 `CacheContainer.X` / `DbConnectionManager.X` / `RepositoryInfo.X`，改為 ctor 注入 `ICacheContainer` / `IDbConnectionManager` / 兩個 factory。

#### 1.2 Singleton vs Scoped lifecycle 決策

| 介面 | Lifetime | 理由 |
|------|---------|------|
| `IDefineStorage` / `IDefineAccess` | Singleton | 檔案存取無 per-request state |
| `ICacheContainer` / `IDbConnectionManager` | Singleton | 內含快取資料，per-class fixture 建立一個 |
| `IDatabaseSettingsProvider` | Singleton | 委派到 `IDefineAccess` |
| `ISystemRepositoryFactory` / `IFormRepositoryFactory` | Singleton | 工廠本身無狀態 |
| `IBusinessObjectFactory` / `IFormBoTypeResolver` | Singleton | 同 Phase 4 |
| `ISessionInfoService` | Singleton（per-fixture） | 仍在 `BackendComponents` 配置；per-class fixture 自然隔離 |
| `IAccessTokenValidator` / `IApiEncryptionKeyProvider` | Singleton | 同 Phase 4 |
| `JsonRpcExecutor` | Transient | 同 Phase 4 |
| `IPathOptionsProvider`（新） | Singleton | 構造時注入單一 `PathOptions` 實例 |
| `IDebugModeProvider`（如需，見 §3） | Singleton | 同 |

> 主計畫 §3 對 lifetime 列了「`ISessionInfoService` 應為 per-request scoped」等 ASP.NET 場景假設。實際 Phase 4 全 Singleton（因為 `BusinessObjectFactory` 為 Singleton 持有 root provider，無法 resolve Scoped）。Phase 5 維持此判斷 —— Scoped 的真實需求要等到有 per-request 並發測試才會出現，那是 Phase 6 之後的話題。

### 2. 移除剩餘 `BackendInfo` 屬性

#### 2.1 `BackendInfo.LogOptions` → `LogOptions` 構造參數注入

`DbAccessLogger` 是純 static class，目前讀 `BackendInfo.LogOptions.DbAccess`。改造方案兩種：

**方案 A**（推薦）：`DbAccessLogger` 改為 instance class，透過 ctor 注入 `LogOptions`；`DbAccess` 內部持有 logger instance。
- 優點：類別職責清楚、無 static state、單元測試直接 new
- 缺點：`DbAccess` ctor 多一個參數；現有大量 `new DbAccess(databaseId)` 呼叫點要連帶調整 —— 但 prod 程式碼這些 call site 多數已透過 `IDbAccessFactory`（Phase 1 引入），factory 注入 logger 一次即可

**方案 B**：保留 static `DbAccessLogger`，但 `LogStart` / `LogEnd` 多收一個 `LogOptions` 參數。
- 優點：改動最小
- 缺點：每個 call site 都要傳 `LogOptions`；本質還是 plumbing

採用方案 A。`DbAccessLogger` 變 instance：

```csharp
public sealed class DbAccessLogger
{
    private readonly DbAccessLogOptions? _opts;
    public DbAccessLogger(LogOptions logOptions)
    {
        _opts = logOptions?.DbAccess;
    }
    public DbLogContext LogStart(DbCommandSpec cmd, string dbId = "") { ... }
    public void LogEnd(DbLogContext ctx, int affectedRows = -1) { ... }
    public void LogError(DbLogContext ctx, Exception ex) { ... }
}
```

`IDbAccessFactory` 多收一個 `LogOptions` 構造參數；factory 在 `Create()` 時把 logger instance 傳給 `DbAccess`。`AddBeeFramework` 註冊時把 `configuration.LogOptions` 透過 `IOptions<LogOptions>` 注入 factory，或直接以 ctor 構造參數方式傳遞（與 Phase 1 `MaxDbCommandTimeout` 相同 pattern）。

刪除 `BackendInfo.LogOptions` 與 `BackendInfo.LogWriter`（後者 prod 0 caller）。

#### 2.2 `BackendInfo.ConfigEncryptionKey` → DI 注入 + 介面

`DatabaseSettings` 在 XML serialize/deserialize hook 內讀 `BackendInfo.ConfigEncryptionKey` 解密 `Servers` / `Items` 的 `Password` 欄位。問題：`DatabaseSettings` 是 DTO 由 XML 反序列化建立，沒有 ctor 注入路徑。

設計選項：

**方案 A**（推薦）：將「密碼加解密」職責從 `DatabaseSettings` DTO 移出，改由 `IDefineAccess` / `IDatabaseSettingsProvider` 的讀寫路徑負責。
- DTO 純資料容器，無 hook
- `LocalDefineAccess.GetDatabaseSettings()` 在 cache miss 後對 `Servers/Items.Password` 解密；`SaveDatabaseSettings` 在寫檔前加密
- 密鑰透過 ctor 注入 `IDefineAccess`（key 由 `IOptions<SecurityKeyOptions>` 或 `byte[]` ctor 參數提供）
- 優點：DTO 無副作用、加解密職責歸屬清楚、密鑰流經 DI

**方案 B**：保留 hook，引入 ambient context（如 `AsyncLocal<byte[]>`）在 serialize 期間設值。
- 優點：對既有 hook 介面侵入最小
- 缺點：仍是 service locator 形式、`AsyncLocal` 在並行測試下行為微妙

採用方案 A。實作步驟：

1. 把 `DatabaseSettings.BeforeSerialize` / `AfterDeserialize` 內的加解密邏輯抽到 `DatabaseSettingsCryptor` 工具類（接 `byte[] combinedKey` 參數）
2. `DatabaseSettings` 的 hook 改為 no-op（或移除 `IObjectSerializeProcess` 實作）
3. `LocalDefineAccess` 改為持有 `byte[] _configEncryptionKey`；`GetDatabaseSettings` / `SaveDatabaseSettings` 內呼叫 `DatabaseSettingsCryptor.Decrypt(settings, _configEncryptionKey)` / `Encrypt(...)`
4. `LocalDefineAccess` ctor 新增 `(IDefineStorage storage, byte[] configEncryptionKey)` 重載（保留無參數 / 單參數版本以便 `LocalDefineAccessTests` 等不需密鑰的測試）
5. `AddBeeFramework` 構造 `IDefineAccess` 時把 `configuration.SecurityKeySettings` 解密後的 `byte[] configEncryptionKey` 透過 ctor 傳入

刪除 `BackendInfo.ConfigEncryptionKey`。

#### 2.3 `BackendInfo.ApiEncryptionKey` / `CookieEncryptionKey` / `DatabaseEncryptionKey`

- `ApiEncryptionKey` 唯一消費者 `StaticApiEncryptionKeyProvider`：`BeeFrameworkServiceCollectionExtensions.CreateApiEncryptionKeyProvider` 內 `new StaticApiEncryptionKeyProvider(BackendInfo.ApiEncryptionKey)`。改為在同一處直接從 `configuration.SecurityKeySettings.ApiEncryptionKey` 解密得到 byte[] 後傳入 ctor —— 不必經過 `BackendInfo`
- `CookieEncryptionKey` / `DatabaseEncryptionKey`：prod 0 caller，目前 `InitializeSecurityKeys` 寫入但無人讀。直接刪除這兩個屬性 + `InitializeSecurityKeys` 內對應賦值

#### 2.4 `BackendInfo` 屬性最終狀態

Phase 5 結束時 `BackendInfo.cs` 變為空 static class（無屬性、無方法）。**保留 class 本身**等 Phase 6 與 `BackendConfiguration` 一起刪除，避免 Phase 5 PR diff 過大。

### 3. `SysInfo.IsDebugMode` 推下

`ApiPayloadOptionsFactory.CreateEncryptor("none" / "")` 讀 `SysInfo.IsDebugMode` 決定是否允許 `NoEncryptionEncryptor`。改造：

- `ApiPayloadOptionsFactory` 改為 instance class，ctor 注入 `bool isDebugMode`（或 `IDebugModeProvider`）
- `ApiServiceOptions` / `JsonRpcExecutor` 一併調整建構鏈
- `AddBeeFramework` 內以 `configuration.CommonConfiguration.IsDebugMode` 注入

廢除 `[Collection("SysInfo")]` 與 `SysInfoCollection`；3 個 `[Collection("SysInfo")]` 測試類別改為直接在 fixture 構造時指定 `IsDebugMode = true/false`。

> 此項是測試規範 `testing.md` §「全域狀態與平行安全」明確列出的「長期：應改為接受 isDebugMode 參數」遷移目標，本 phase 落地。

### 4. `DefinePathInfo` 移除 static

#### 4.1 `IPathOptionsProvider` 介面

```csharp
namespace Bee.Definition
{
    public interface IPathOptionsProvider
    {
        PathOptions Options { get; }
    }
    internal sealed class PathOptionsProvider(PathOptions options) : IPathOptionsProvider { ... }
}
```

`PathOptions` 加入便利方法（直接掛在 type 上而非另立 `DefinePathInfo`）：

```csharp
public class PathOptions
{
    public string DefinePath { get; init; } = "";
    public string GetSystemSettingsFilePath() => Path.Combine(DefinePath, "SystemSettings.xml");
    // ... 7 個 GetXxxFilePath 方法
}
```

`DefinePathInfo` static class **刪除**。

#### 4.2 17 個 callers 改造

| 檔案 | 處理 |
|------|------|
| `FileDefineStorage`（6 處） | ctor 注入 `IPathOptionsProvider` 或 `PathOptions` |
| `MasterKeyProvider`（1 處） | 改為 instance class 或 method 接 `PathOptions` 參數 |
| `LocalDefineAccess`（3 處） | ctor 注入 `PathOptions` |
| `Bee.ObjectCaching` 6 個 cache（`SystemSettingsCache` / `DatabaseSettingsCache` / `ProgramSettingsCache` / `DbCategorySettingsCache` / `TableSchemaCache` / `FormSchemaCache` / `FormLayoutCache`） | ctor 注入 `PathOptions`；`ChangeMonitorFilePaths` 從注入路徑算出 |
| `SystemSettingsLoader.Load()` 無參數版本 | 改為要求 `PathOptions` 參數；caller（含 GlobalFixture/BeeTestFixture）顯式傳入 |

#### 4.3 `TempDefinePath` 廢除

不再需要 — fixture 構造時直接給定 `PathOptions { DefinePath = "/tmp/bee-{guid}" }`，dispose 時清理目錄。改寫範例見 §7「測試範例」。

### 5. `CacheContainer` / `DbConnectionManager` / `RepositoryInfo` 改 DI

#### 5.1 `ICacheContainer`

抽 9 個 cache 屬性為介面：

```csharp
public interface ICacheContainer
{
    SystemSettingsCache SystemSettings { get; }
    DatabaseSettingsCache DatabaseSettings { get; }
    // ... 9 個
}
```

`CacheContainer` 改為 instance class，ctor 注入 `IDefineStorage` 與 `PathOptions`；DI 註冊為 Singleton。所有 `CacheContainer.X` 呼叫點改為 ctor 注入 `ICacheContainer`。

刪除 `CacheContainer.Initialize` static 方法、`ICacheBootstrapper` / `CacheBootstrapper`。

#### 5.2 `IDbConnectionManager`

抽 `DbConnectionManager` 為介面（已有 `GetConnectionInfo`、`CreateConnection`、`Remove`、`Clear`、`Contains`、`Count`）：

```csharp
public interface IDbConnectionManager
{
    DbConnection CreateConnection(string databaseId);
    DbConnectionInfo GetConnectionInfo(string databaseId);
    bool Remove(string databaseId);
    void Clear();
    bool Contains(string databaseId);
    int Count { get; }
}
```

實作 class 改 instance，ctor 注入 `IDatabaseSettingsProvider`；DI 註冊 Singleton。

刪除 `DbConnectionManager` static class、`IDbConnectionManagerBootstrapper`。

#### 5.3 `RepositoryInfo`

只有兩個 static 屬性 `SystemFactory` / `FormFactory`。改造：

- 兩個消費端（`Business`、`Repository`）改為 ctor 注入 `ISystemRepositoryFactory` / `IFormRepositoryFactory`
- `RepositoryInfo` 類別刪除
- `IRepositoryInfoBootstrapper` 刪除

### 6. `BeeTestFixture` 設計

#### 6.1 API surface

```csharp
namespace Bee.Tests.Shared
{
    public sealed class BeeTestFixture : IDisposable
    {
        public IServiceProvider Provider { get; }
        public string DefinePath => _pathOptions.DefinePath;
        public PathOptions PathOptions => _pathOptions;

        public BeeTestFixture(Action<BeeTestFixtureBuilder>? configure = null) { ... }

        public T GetRequiredService<T>() where T : notnull
            => Provider.GetRequiredService<T>();
        public T? GetService<T>() where T : class
            => Provider.GetService<T>();

        public void Dispose() { ... }   // 清 temp dir、dispose provider
    }

    public sealed class BeeTestFixtureBuilder
    {
        // 預設指向 tests/Define（共享 fixture 資料，read-only）
        public BeeTestFixtureBuilder UseSharedDefineFixture();
        // 切到 per-class temp dir，並可選複製 tests/Define 內容
        public BeeTestFixtureBuilder UseTempDefinePath(bool copyFixture = false);
        // 開 debug 模式
        public BeeTestFixtureBuilder UseDebugMode();
        // 注入特定 master key 來源（CI 走 env var、本機走預設）
        public BeeTestFixtureBuilder UseMasterKeySource(MasterKeySource source);
        // service override（取代 TestOverrideServiceProvider）
        public BeeTestFixtureBuilder ReplaceService<TService>(TService instance);
        public BeeTestFixtureBuilder ReplaceService<TService, TImplementation>();
        // 對 BackendConfiguration 做最後微調
        public BeeTestFixtureBuilder ConfigureBackend(Action<BackendConfiguration> configure);
    }
}
```

實作要點：

- 構造時呼叫 `SystemSettingsLoader.Load(pathOptions)` 取得 settings；按 builder 覆寫
- `services.AddBeeFramework(settings.BackendConfiguration, autoCreateMasterKey: true)`
- `BuildServiceProvider`，不再 eager-resolve 任何 bootstrapper（§5 改造後沒有 bootstrapper）
- Dispose 時 `Provider.Dispose()`（觸發所有 Singleton 的 IDisposable）+ 清 temp dir
- **無 process-wide static**：fixture 內所有狀態都在 `Provider` 內

#### 6.2 共享資料庫的處理

`DbGlobalFixture` 目前在 process 內建 schema + 寫 seed 一次。Phase 5 後三種選項：

**選項 A**：每個測試類別自建 schema + seed（per-class isolated DB？）
- 否決：本機 / CI 都不允許每個 test class 建一個 DB；schema build 也太貴

**選項 B**：保留「process 內第一次跑時 build schema + seed」，但移到 lazy / idempotent 模式
- 採用：用 `Lazy<DbGlobalState>` + double-checked-lock 維持「process 一次性」語意
- 各 `BeeTestFixture` 構造時透過 `BeeTestFixtureBuilder.UseSharedDatabases()` 觸發 lazy；DB 連線資訊（含 `BEE_TEST_CONNSTR_*` env var）讀入 fixture 內的 `IDefineAccess` 的 `DatabaseSettings.Items`
- Schema build / seed 本來就是冪等（`TableSchemaBuilder.Execute` 回傳 created/upgraded，seed 用 `WHERE sys_id = '001'` 檢查），multiple fixture 並行進入 lazy 受 lock 保護

**選項 C**：把 schema/seed 拉到 test runner 外（global setup script）
- 否決：本機開發體驗差；現有 `./test.sh` 就有 container 啟動邏輯，不必再加一層

採用選項 B。實作上：

```csharp
internal static class SharedDatabaseState
{
    private static readonly object _lock = new();
    private static bool _initialized;
    public static IReadOnlyList<DatabaseItem> EnsureRegistered() { ... }
    // 第一次呼叫時：對每個 BEE_TEST_CONNSTR_* env var 設定的 DB，verify connection → ensure schema → ensure seed
    // 之後呼叫直接回傳 cached list
}
```

`BeeTestFixture` 構造時若 builder 開了 `UseSharedDatabases()`，就把 `SharedDatabaseState.EnsureRegistered()` 的 items 灌入 fixture 內的 `IDatabaseSettingsProvider`。

#### 6.3 fixture 在 xUnit 中的綁定方式

xUnit 標準寫法 —— class fixture：

```csharp
public class MyTests : IClassFixture<BeeTestFixture>
{
    private readonly BeeTestFixture _fx;
    public MyTests(BeeTestFixture fx) { _fx = fx; }

    [Fact]
    public void Foo() { ... _fx.GetRequiredService<IDefineAccess>() ... }
}
```

`IClassFixture<T>` 由 xUnit 為每個 test class 建立一個 `BeeTestFixture` 實例，所有 `[Fact]` 共享、跑完後 dispose。預設 `BeeTestFixture` 構造（無 builder 客製）走 shared fixture 模式（指向 `tests/Define`，無 temp）—— 適合純邏輯／序列化測試。

需要 per-class 隔離的測試（如 `LocalDefineAccessSaveTests`、`MasterKeyProviderTests` 寫檔場景）改用以下其中一種：

**Pattern A**：自定 fixture 子類

```csharp
public sealed class WritableDefineFixture : BeeTestFixture
{
    public WritableDefineFixture() : base(b => b.UseTempDefinePath(copyFixture: true)) {}
}

public class LocalDefineAccessSaveTests : IClassFixture<WritableDefineFixture> { ... }
```

**Pattern B**：每 test 自建 fixture（不用 `IClassFixture`）

```csharp
public class FooTests
{
    [Fact]
    public void Bar()
    {
        using var fx = new BeeTestFixture(b => b.UseTempDefinePath());
        // ...
    }
}
```

> Pattern B 適合 `MasterKeyProviderTests` 內單一測試需要極端隔離（如「DefinePath 下無 Master.key」）；Pattern A 適合多個寫檔測試共用同一 temp dir。

#### 6.4 `BeeTestServices` static 移除

替換策略：所有現有 `BeeTestServices.GetRequiredService<T>()` 改為 `_fixture.GetRequiredService<T>()`（透過 ctor 注入的 fixture instance）。

`TestSessionFactory` 改為 instance method：

```csharp
public static class TestSessionFactory
{
    public static Guid CreateAccessToken(BeeTestFixture fx, string userId = "test", TimeSpan? expiresIn = null) { ... }
}
```

`TestBeeContext.Create()` 改接 `BeeTestFixture` 參數。

`TestOverrideServiceProvider` 保留（仍有 per-test ad-hoc 需求，如 `ApiAspNetCoreTests` 補一個 `IHostEnvironment`），但常見「指定一個 override」場景改由 `BeeTestFixtureBuilder.ReplaceService<T>(...)` 在 fixture 構造階段處理。

### 7. 測試範例

#### 7.1 替換 `[Collection("Initialize")]` + `BeeTestServices.Provider`

**Before**：

```csharp
[Collection("Initialize")]
public class SystemBusinessObjectLoginTests
{
    [Fact]
    public void Login_Authenticated_ReturnsValidSessionToken()
    {
        var sessionService = BeeTestServices.GetRequiredService<ISessionInfoService>();
        var bo = new TestableSystemBusinessObject(Guid.Empty, _ => (true, "User One"));
        // ...
    }
}
```

**After**：

```csharp
public class SystemBusinessObjectLoginTests : IClassFixture<BeeTestFixture>
{
    private readonly BeeTestFixture _fx;
    public SystemBusinessObjectLoginTests(BeeTestFixture fx) { _fx = fx; }

    [Fact]
    public void Login_Authenticated_ReturnsValidSessionToken()
    {
        var sessionService = _fx.GetRequiredService<ISessionInfoService>();
        var bo = new TestableSystemBusinessObject(_fx.GetRequiredService<IBeeContext>(), Guid.Empty, _ => (true, "User One"));
        // ...
    }
}
```

#### 7.2 替換 `TempDefinePath`

**Before**：

```csharp
[Collection("Initialize")]
public class LocalDefineAccessSaveTests
{
    [Fact]
    public void SaveSystemSettings_WritesXml()
    {
        using var temp = new TempDefinePath();
        var access = new LocalDefineAccess(new FileDefineStorage());
        access.SaveSystemSettings(new SystemSettings());
        Assert.True(File.Exists(DefinePathInfo.GetSystemSettingsFilePath()));
    }
}
```

**After**：

```csharp
public sealed class WritableDefineFixture : BeeTestFixture
{
    public WritableDefineFixture() : base(b => b.UseTempDefinePath(copyFixture: true)) {}
}

public class LocalDefineAccessSaveTests : IClassFixture<WritableDefineFixture>
{
    private readonly WritableDefineFixture _fx;
    public LocalDefineAccessSaveTests(WritableDefineFixture fx) { _fx = fx; }

    [Fact]
    public void SaveSystemSettings_WritesXml()
    {
        var access = _fx.GetRequiredService<IDefineAccess>();
        access.SaveSystemSettings(new SystemSettings());
        Assert.True(File.Exists(_fx.PathOptions.GetSystemSettingsFilePath()));
    }
}
```

#### 7.3 替換 `[Collection("SysInfo")]` + `try/finally SysInfo.IsDebugMode`

**Before**：

```csharp
[Collection("SysInfo")]
public class ApiPayloadOptionsFactoryTests
{
    [Fact]
    public void CreateEncryptor_None_DebugMode_ReturnsNoEncryptionEncryptor()
    {
        var original = SysInfo.IsDebugMode;
        try
        {
            SysInfo.IsDebugMode = true;
            var encryptor = ApiPayloadOptionsFactory.CreateEncryptor("none");
            Assert.IsType<NoEncryptionEncryptor>(encryptor);
        }
        finally { SysInfo.IsDebugMode = original; }
    }
}
```

**After**：

```csharp
public class ApiPayloadOptionsFactoryTests
{
    [Fact]
    public void CreateEncryptor_None_DebugMode_ReturnsNoEncryptionEncryptor()
    {
        var factory = new ApiPayloadOptionsFactory(isDebugMode: true);
        var encryptor = factory.CreateEncryptor("none");
        Assert.IsType<NoEncryptionEncryptor>(encryptor);
    }
}
```

#### 7.4 `DbFact` 整合測試

`[DbFact(DatabaseType.SQLServer)]` 行為不變（仍依 `BEE_TEST_CONNSTR_SQLSERVER` 判斷 skip）。fixture 改用：

```csharp
public sealed class SharedDbFixture : BeeTestFixture
{
    public SharedDbFixture() : base(b => b.UseSharedDatabases()) {}
}

public class DbAccessTests : IClassFixture<SharedDbFixture>
{
    private readonly SharedDbFixture _fx;
    public DbAccessTests(SharedDbFixture fx) { _fx = fx; }

    [DbFact(DatabaseType.SQLServer)]
    public void ExecuteDataTable_VariousParameterFormats_ReturnsDataTable()
    {
        var factory = _fx.GetRequiredService<IDbAccessFactory>();
        var dbAccess = factory.Create("common_sqlserver");
        // ...
    }
}
```

`UseSharedDatabases()` 觸發 `SharedDatabaseState.EnsureRegistered()` 取得所有有 env var 的 DB items，灌入 fixture 的 `IDefineAccess.DatabaseSettings.Items`。

### 8. `Bee.Api.Client.LocalServiceProvider` 處理

主計畫 §「範圍邊界」明確 Phase 5 不重寫 client，但 `GlobalFixture` 內目前有：

```csharp
Bee.Api.Client.ApiClientInfo.LocalServiceProvider = provider;
```

替代方式：

- `BeeTestFixture` 構造時把 provider 寫入 `ApiClientInfo.LocalServiceProvider`，dispose 時還原
- **約束**：使用 `ApiClientInfo.LocalServiceProvider` 的測試（`Bee.Api.Client.UnitTests` 中走近端模式的 RemoteDefineAccess 等）必須加 `[Collection("LocalApiClient")]` 串行 —— 這個 static holder 本身的並行安全要等 Bee.Api.Client 重構才能解
- 此 collection 是 Phase 5 唯一保留的明確 collection，且 scope 縮到只有 client 近端模式測試（< 5 個 class）

文件化在 `tests/Bee.Api.Client.UnitTests/` 加 README 段落說明此限制。

## 執行步驟

各 PR 內部都包含「改 src + 改對應測試」，PR 合進 main 那刻 build 必須綠（主計畫 §「不變條件」）。

### PR 5.1：拆 `BackendInfo` 配置型欄位

範圍：§2 + §3 全部

- `BackendInfo.LogOptions` / `LogWriter` / 4 個加密金鑰 → DI Options / ctor 參數
- `DbAccessLogger` 改 instance、`DbAccess` / `IDbAccessFactory` ctor 串接
- `DatabaseSettings` 抽 `DatabaseSettingsCryptor`、`LocalDefineAccess` 持金鑰
- `ApiPayloadOptionsFactory` 改 instance、`SysInfo.IsDebugMode` ctor 注入
- `BackendInfo` 縮為空 static class

PR diff 估計：~600 lines

### PR 5.2：拆 `DefinePathInfo`

範圍：§4 全部

- 新增 `IPathOptionsProvider` + `PathOptions` 便利方法
- 17 個 src callers 改注入
- `LocalDefineAccess` / `MasterKeyProvider` / 6 個 cache / `FileDefineStorage` 全部 ctor 改造
- 對應測試（17 個 src callers 中有對應測試的，多數會自然落到 §6 fixture 改造）

PR diff 估計：~800 lines

### PR 5.3：拆 `CacheContainer` / `DbConnectionManager` / `RepositoryInfo`

範圍：§5 全部

- 三個 static class 改為 DI service
- 三個 bootstrapper 刪除
- `AddBeeFramework` / `UseBeeFramework` 對應簡化
- 所有消費端改 ctor 注入

PR diff 估計：~700 lines

### PR 5.4：`BeeTestFixture` 設計與骨幹

範圍：§6 全部

- 新增 `BeeTestFixture` / `BeeTestFixtureBuilder` / `SharedDatabaseState`
- `BeeTestServices` static 刪除、`GlobalFixture` / `DbGlobalFixture` / `GlobalCollection` / `BaseTests` / `TempDefinePath` 刪除
- `TestSessionFactory` / `TestBeeContext` ctor 改造

> 此 PR 在 src 完成 §1–§5 後才能進行 —— `BeeTestFixture` 構造邏輯依賴新的 DI 註冊路徑。

PR diff 估計：~500 lines（純新增 + 刪除舊 helper）

### PR 5.5–5.7：測試類別遷移

依 unit-test 專案分批；每個 PR 內單一 test project 全部測試改完，build 綠：

- **PR 5.5**：`Bee.Base.UnitTests` + `Bee.Definition.UnitTests` + `Bee.ObjectCaching.UnitTests`（含 `LocalDefineAccessSaveTests` 等寫檔測試）
- **PR 5.6**：`Bee.Db.UnitTests` + `Bee.Repository.UnitTests`（含 `[DbFact]` 整合測試 fixture 串接）
- **PR 5.7**：`Bee.Business.UnitTests` + `Bee.Api.Core.UnitTests` + `Bee.Api.AspNetCore.UnitTests` + `Bee.Api.Client.UnitTests`（含 `[Collection("LocalApiClient")]` 套用）

每個 PR 完成驗證項：
- `dotnet test tests/<Project>` 通過
- 該專案內 `[Collection("Initialize")]` / `TempDefinePath` / `BeeTestServices` 引用清零
- `./test.sh` 全套通過

### PR 5.8：CI 並行恢復 + 清理

- 移除 `xunit.runner.json` 等若有 `parallelizeAssembly: false` / `parallelizeTestCollections: false` 配置
- CI workflow（`build-ci.yml`）增加 collection-level parallel 監控指標
- `docs/architecture-overview.md` / `docs/development-cookbook.md` / `.claude/rules/testing.md` 同步更新「測試 fixture」段落
- 更新 `MEMORY.md` 與相關記憶（feedback_simple_config_no_dedicated_test 等）

PR diff 估計：~200 lines

## 驗證策略

| 驗證項 | 工具 / 命令 | 通過條件 |
|--------|-------------|---------|
| build 綠 | `dotnet build --configuration Release` | 0 warning（`TreatWarningsAsErrors=true`） |
| 全套單元測試 | `./test.sh` | 0 failed、0 errored；skipped 只能是 `[DbFact]` env var 缺、`[LocalOnlyFact]` 在 CI |
| `BackendInfo.` grep | `grep -rn "BackendInfo\." src/ tests/ --include='*.cs'` | 僅 `BackendInfo.cs` 本身（無屬性，僅 class 宣告） |
| `[Collection("Initialize")]` grep | `grep -rn "Initialize" tests/ --include='*.cs'` | 0 hits |
| `TempDefinePath` grep | `grep -rn "TempDefinePath" tests/ --include='*.cs'` | 0 hits |
| `BeeTestServices` grep | `grep -rn "BeeTestServices" tests/ --include='*.cs'` | 0 hits |
| `DefinePathInfo` grep | `grep -rn "DefinePathInfo" src/ tests/ --include='*.cs'` | 0 hits |
| 並行測試壓力 | `./test.sh` 連跑 5 次 | 全綠（驗證消除 race 而非「偶爾過」） |
| CI 時間 | `build-ci.yml` 對 main 的 wall clock | 不退步（理想：縮短 ≥ 15%） |

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| **`CacheContainer` 解耦觸及 ObjectCaching 內部 cache 介面** —— 9 個 cache class 既有實作以 internal field 持 `IDefineStorage`，改為 DI 後要小心 ChangeMonitor 監控 file path 從 static `DefinePathInfo` 變來源 | 先在 PR 5.2 完成 `PathOptions` 注入，再到 PR 5.3 解耦 `CacheContainer`；保留 cache 內部對 file path 的監聽路徑，僅換取得方式 |
| **`DatabaseSettings` 加解密職責搬家可能影響既有 XML 檔案的 round-trip 行為** | 抽出 `DatabaseSettingsCryptor` 後留 18+ 個 `DatabaseSettingsTests` 既有用例驗證；新增 1-2 個 fixture 測試 round-trip：寫加密 → 讀解密 → 比對原值 |
| **`DbGlobalFixture` 改為 lazy-init `SharedDatabaseState` 後 race 可能性** | `Lazy<T>` + `LazyThreadSafetyMode.ExecutionAndPublication` 保證 init body 只執行一次；既有 `[DbFact]` schema/seed 已是冪等 |
| **xUnit `IClassFixture<T>` 構造成本** —— 每個 test class 都 build provider 可能拖慢執行 | 純邏輯類測試使用 shared `BeeTestFixture`（無 temp dir、無 DB），構造輕；DB 測試用 `SharedDbFixture` 重用 lazy DB state |
| **`[Collection("LocalApiClient")]` 串行 Bee.Api.Client 近端模式測試** | 範圍受限（< 5 class）；在測試 README 與 fixture comment 明確標示「Bee.Api.Client 重構前的暫留」 |
| **單一 phase 內 8 個 PR 合入過程中 main 上半改半未改** | 主計畫 §「相容性策略」要求 PR 合進 main 那刻 build 綠 → 每個 PR 內必須完整移除該範圍的舊 API 並改完所有 callers；不允許 PR 5.2 合入後留下「`DefinePathInfo` 還在但 callers 已改完」這種半套狀態 |

## 完成標準

| 項目 | 標準 |
|------|------|
| 主計畫 §「成功標準」第 1 條 | `grep -r "BackendInfo\." src/ tests/` 結果僅 `BackendInfo.cs` 本身（屬性已清零；類別 Phase 6 移除） |
| 主計畫 §「成功標準」第 3 條 | `dotnet test` 不依賴 `GlobalFixture` 或 `[Collection("Initialize")]` |
| 主計畫 §「成功標準」第 4 條 | CI 時間相較 v4.x 不退步 |
| 主計畫 §「成功標準」第 5 條 | `docs/architecture-overview.md` 無 `GlobalFixture` / `BeeTestServices` 字樣 |
| 平行測試恢復 | xUnit 預設 `parallelizeTestCollections: true`、無人為串行 collection（除 `LocalApiClient` 外） |
| TempDefinePath 廢除 | 類別不存在；寫檔測試一律走 `BeeTestFixtureBuilder.UseTempDefinePath()` |

## 後續銜接 Phase 6

Phase 5 完成時 `BackendInfo.cs` 與 `BackendConfiguration.cs` 已成空殼（無屬性、無方法、或僅剩無用入口），可在 Phase 6 內直接刪除：

- 刪除 `src/Bee.Definition/BackendInfo.cs`
- 刪除 `src/Bee.Definition/BackendConfiguration.cs`（若已空殼）
- `ApiClientInfo.LocalServiceProvider` 留待 Bee.Api.Client 重構處理（不在 BackendInfo 主計畫範圍）
- 更新文件：`development-cookbook.md` 移除 `BackendInfo.Initialize` 提及；`architecture-overview.md` 反映 DI 模型

## 未決議題（待實作展開時決定）

- 是否引入 `Microsoft.Extensions.Options` package 換 raw byte[] / int / `LogOptions` 構造參數注入？傾向**不引入**：Phase 1 已建立「簡單配置不一定走 `IOptions<T>`」pattern；保留 ctor 參數簡潔
- `WritableDefineFixture` 應放共享處（`Bee.Tests.Shared`）還是各 test project 自定？傾向放共享處 —— 多個 project 都有寫檔測試
- `SharedDatabaseState` 是否需要 dispose hook 釋放 SQLite keep-alive 連線？目前 `GlobalFixture._sqliteKeepAlive` 故意不釋放（process exit 時 OS 回收）；移到 lazy 後可繼續同策略
- `IDebugModeProvider` 介面是否值得單獨建？或直接 ctor 參數 `bool isDebugMode`？傾向後者 —— 單一布林無服務化價值
