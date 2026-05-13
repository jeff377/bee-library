# 計畫：BackendInfo 由 Service Locator 改為 DI（主計畫）

**狀態：🚧 進行中**

> 本文件為主計畫（main plan），定義整體目標、原則、階段路線。
> 各階段的細部執行步驟另以 sub-plan 文件描述。
>
> **前置計畫**：[plan-remove-backendinfo-db-globals.md](plan-remove-backendinfo-db-globals.md)
> — 先移除 `BackendInfo.DatabaseType` / `DatabaseId` 並導入 `DbCategoryIds` 常數與 `SessionInfo.CompanyDatabaseId`，
> 完成後 Phase 1 範圍會大幅簡化。

## Sub-plan 進度

| Phase | 主題 | 狀態 | Sub-plan |
|-------|------|------|----------|
| 0 | 前置清理（`SystemSettingsLoader`） | ✅ 已完成（2026-05-12） | [plan-backendinfo-di-phase0-systemsettings-loader.md](plan-backendinfo-di-phase0-systemsettings-loader.md) |
| 1 | Bee.Db 配置注入 | ✅ 已完成（2026-05-12） | [plan-backendinfo-di-phase1-bee-db-config.md](plan-backendinfo-di-phase1-bee-db-config.md) |
| 2 | ObjectCaching 與 DefineAccess（含 DefinePath） | ✅ 已完成（2026-05-12） | [plan-backendinfo-di-phase2-defineaccess-decouple.md](plan-backendinfo-di-phase2-defineaccess-decouple.md) |
| 3 | Business 與 Repository 層注入（含 `IBeeContext`） | ✅ 已完成（2026-05-12） | [plan-backendinfo-di-phase3-business-injection.md](plan-backendinfo-di-phase3-business-injection.md) |
| 4 | Api.Core 與 Api.AspNetCore | ✅ 已完成（2026-05-12） | [plan-backendinfo-di-phase4-api-di.md](plan-backendinfo-di-phase4-api-di.md) |
| 5 | 測試基礎設施重寫 | ✅ 已完成（2026-05-13） | [plan-backendinfo-di-phase5-test-infra.md](plan-backendinfo-di-phase5-test-infra.md) |
| 6 | 移除 BackendInfo 空殼 | 📝 未開始 | — |
| 7 | DbConnectionManager 靜態 facade 移除 + DbAccess ctor DI 化 | 📝 擬定中 | [plan-backendinfo-di-phase7-dbconnectionmanager.md](plan-backendinfo-di-phase7-dbconnectionmanager.md) |

> 狀態圖例：📝 未開始 / 🚧 進行中 / ✅ 已完成
>
> 原 Phase 1（SystemSettings 結構重構）已撤除：XML 結構變動隨各 phase 實際需求逐步推進，不單獨抽出。原 Phase 6（跨平台支援）已撤除：MAUI/Desktop 為 client side，不在 BackendInfo 重構範圍。
>
> Phase 7 於 Phase 5 收尾時新增：原計畫將 `DbConnectionManager` 靜態 facade 移除歸併到 Phase 5，但 scope cascade 至 84+ 測試呼叫點 + 5 src 點，獨立 phase 處理較佳。執行順序：Phase 6 → Phase 7。

## 背景

### 現況

`BackendInfo`（`src/Bee.Definition/BackendInfo.cs`）是全靜態類別，承擔兩種職責：

1. **持有 process-wide 配置值** — `DefinePath`、`DatabaseType`、`DatabaseId`、`MaxDbCommandTimeout`、`LogOptions`、4 個加密金鑰
2. **作為 8 個核心服務的 Service Locator** — `IDefineAccess`、`ISessionInfoService`、`IBusinessObjectFactory`、`IAccessTokenValidator`、`IApiEncryptionKeyProvider`、`ICacheDataSourceProvider`、`IDefineStorage`、`IEnterpriseObjectService`，外加可選的 `ILoginAttemptTracker`

啟動流程透過 `BackendInfo.Initialize(BackendConfiguration)` 一次性完成：讀取設定 → 用反射 `AssemblyLoader.CreateInstance` 建構服務實例 → 寫入靜態屬性。

### 問題

| 面向 | 痛點 |
|------|------|
| **可測試性** | 159 個測試引用 BackendInfo；需要 `GlobalFixture`、`TempDefinePath`、`[Collection("Initialize")]` 等繁瑣機制維持隔離 |
| **依賴顯性** | BO 在程式碼內呼叫 `BackendInfo.SessionInfoService.Get(...)`，建構式看不出依賴，IDE 無法靜態分析 |
| **生命週期** | 全部服務都是 process-wide singleton；Web 場景下 `ISessionInfoService` 應為 per-request scoped，目前以「執行緒上下文 + 靜態服務」混搭 |
| **初始化耦合** | `DefineAccess` 既是 boot-time 配置來源（讀 `SystemSettings.xml`）又是 runtime 服務，職責不清 |
| **反射建構** | `BusinessObjectFactory` 用 `AssemblyLoader.CreateInstance` 建 BO，BO 無法宣告依賴 |

### 為何選擇 DI

- .NET 10 ASP.NET Core 原生支援 `Microsoft.Extensions.DependencyInjection`；本框架的後端宿主程式碼可直接受益
- DI 容器強制把依賴宣告於建構式 → 編譯期可見、IDE 可分析、測試可注入 mock
- Options Pattern 與 `IValidateOptions<T>` 支援啟動期驗證，比 `BackendInfo.Initialize` 內部偷偷拋例外更早失敗
- 生命週期（Singleton / Scoped / Transient）語意明確，per-request scope 邊界清楚

### 範圍邊界

**本計畫聚焦後端**（`Bee.Api.AspNetCore` host 內執行的服務層）。

**不在本計畫範圍內**：

- **遠端模式 client**（MAUI / Desktop 等應用透過 `Bee.Api.Client` 走 JSON-RPC 呼叫後端）—— 不 host BO / Repository / `IDefineAccess`，純粹是 API 消費者
- **`Bee.Api.Client` 近端模式**（in-process 呼叫，例如工具程式直接執行後端邏輯）—— 此模式下確實需要在 client process 內初始化後端服務，但未來再考慮如何套用本計畫的 DI 註冊邏輯

**對近端模式的設計約束**：`AddBeeFramework()` extension 應設計成「host-agnostic 的 `IServiceCollection` 註冊邏輯」，不要綁死 ASP.NET Core 專屬型別（如 `WebApplicationBuilder`）。如此未來近端模式（如 Console / 工具程式）可重用同一份註冊邏輯，只需各自決定 ServiceProvider 的建立方式與 scope 管理策略。

## 目標

1. 移除 `BackendInfo` 類別，所有 Service Locator 呼叫點改為建構式注入或工廠模式
2. `SystemSettings.xml` 的配置仍由 XML 驅動，但反序列化後直接對映 Options 型別
3. 測試基礎設施簡化 — 廢除 `GlobalFixture` 全域初始化、`TempDefinePath` swap、`[Collection("Initialize")]` 串行
4. 整個遷移於 v5.0 完成，無外部消費者故採全 DI 路徑、不留相容過渡層

## 不變條件（Invariants）

執行各階段時必須維持：

- **`SystemSettings.xml` 仍是配置主來源** — 不引入 `appsettings.json` 取代
- **8 個核心服務的可置換性** — 使用者仍可在 XML 中宣告自訂實作型別替換預設
- **加密管線順序** — `序列化 → 壓縮 → 加密` 不變，DI 化不影響密碼學設計
- **每個階段必須可獨立 ship** — 不允許「重構半套既不能 ship 也不能回頭」
- **每個 phase 在單一 PR 內完成該層所有 `BackendInfo` 引用的刪除**（無外部消費者，採全 DI 路徑，不留 `[Obsolete]` 過渡 API、不引入相容 adapter；PR 合進 main 那一刻 build 必須綠）

## 範圍評估摘要

| 維度 | 數量 | 備註 |
|------|------|------|
| `BackendInfo.*` 總引用點 | 217（`src/` 58、`tests/` 159） | — |
| 隨 `BackendInfo` 刪除自然消失 | ~84 | `BackendInfoTests.cs`（52）、`DatabaseSettingsTests.cs`（25）、`BackendInfo.cs` 自我引用（7） |
| **實際需要轉換** | **~133** | src 約 51 + tests 約 82 |
| 跨層 | 6 層（Definition / Base / Db / ObjectCaching / Business / Repository / Api.Core / Api.AspNetCore） | — |
| 服務介面 | 8 個必選 + 1 個可選 | — |
| 配置值欄位 | 5 個純配置 + 4 個加密金鑰 | — |
| 外部消費者 | 0 | 採全 DI 一次到位，不留相容層 |

## 設計原則

### 1. 兩種服務分類

| 類別 | 對應 DI 模式 |
|------|--------------|
| **純配置值**（DatabaseType、加密金鑰、LogOptions） | `IOptions<T>` |
| **可置換服務**（8 個 interface） | `services.AddSingleton<I, Impl>()` 或 `AddScoped` 視 lifetime |

### 2. 生命週期決策

ASP.NET Core 後端宿主：

| 服務 | Lifetime |
|------|---------|
| `IDbAccessFactory`、`IBusinessObjectFactory`、`IDefineAccess`、`IDefineStorage` | Singleton |
| `ICacheDataSourceProvider`、`IEnterpriseObjectService` | Singleton |
| `IFormBoTypeResolver` | Singleton |
| `IBeeContext` | Scoped（per request） |
| `ISessionInfoService` | Scoped（per request） |
| `IAccessTokenValidator` | Scoped |
| `IApiEncryptionKeyProvider` | Scoped |
| `ILoginAttemptTracker` | Singleton（含 cache） |

### 3. 工廠模式適用範圍

DI 容器無法直接管理「runtime 才知道參數的服務」，這類需透過工廠：

- `IDbAccessFactory` — 因 `DbAccess` 依連線 ID 建立
- `IBusinessObjectFactory` — 因 BO 由 `accessToken` / `progId` 等 runtime 值派發（保留現有 `CreateSystemBusinessObject` / `CreateFormBusinessObject(progId)` 介面，內部改用 `IServiceProvider` + `ActivatorUtilities.CreateInstance`）

`IServiceProvider` **只允許**出現在這兩個工廠類別內，其他業務類別禁止注入 — 否則退化回 Service Locator。

### 4. BO 建構式統一與零 DI 註冊

**動機**：ERP 應用會有上千個 `FormBusinessObject` 子類，由 progId XML 表派發；應用開發者寫新 BO 時不應接觸 DI API。

**規則**：

- **共同介面**：所有 BO 仍實作 `IBusinessObject`（`ExecFunc` / `ExecFuncAnonymous`）；BO-to-BO 呼叫只透過此介面 + `ExecFuncArgs`，**禁止**呼叫具名方法
- **兩個 base class，ctor 簽章嚴格固定**：
  - `SystemBusinessObject(IBeeContext ctx, Guid accessToken, bool isLocalCall)`
  - `FormBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall)`
- **`IBeeContext` 聚合 3 個 BO 必用核心服務**（`IDefineAccess` / `ISessionInfoService` / `IBusinessObjectFactory`）加上 `IServiceProvider Services` 逃生口，base class 解包到具名 `protected` 屬性供子類使用
  - `IDbAccessFactory` **不在 IBeeContext** —— DB 存取屬 `Bee.Repository` 層職責，BO 透過 Repository 間接操作
  - **`IServiceProvider Services`**：特殊方法逃生口（rare per-method needs，如 `SystemBO.Login` 需要的 `IApiEncryptionKeyProvider` / `ILoginAttemptTracker`）。明確限定用途、greppable
  - Phase 3 內 `Services` 用 `BackendInfoServiceProvider` 暫時實作（轉發到 BackendInfo.X 靜態查表）；Phase 4 替換為真 DI scope 的 `IServiceProvider`，BO 程式碼不變
- **BO 子類 ctor 必須照搬 base 簽章**（不允許子類追加額外注入參數）—— 任何特化服務都從 `IBeeContext` 取得（含 `Services` 逃生口）
- **BO 不註冊 DI 容器**；factory 透過 `ActivatorUtilities.CreateInstance(sp, boType, accessToken, progId, isLocalCall)` 建構
- **progId → BO Type 對應由 XML 宣告**（`IFormBoTypeResolver` 啟動時讀入），不在 DI 註冊

**結果**：

| 角色 | 註冊 DI? | 工作量 |
|------|---------|--------|
| 框架核心服務（`IBeeContext` 等 8 個） | ✅ | 一次性 |
| BO 子類（上千個） | ❌ | 寫類別 + 加 XML 一行 |

ERP 開發者新增 BO 完全不接觸 DI API。

### 5. 階段獨立可 ship

每個階段完成後：
- 整個 repo build 通過、所有測試綠燈
- 該 phase 對應層的 `BackendInfo` 屬性**已從程式碼中刪除**（不是 `[Obsolete]` 標記）
- `BackendInfo` 隨各 phase 縮減；Phase 6 刪除剩餘空殼類別本身

## 階段路線圖

### Phase 0：前置清理（低風險、可立即執行）
**目標**：把「初始化期讀 SystemSettings.xml」的職責從 `IDefineAccess` 抽離。

- 抽 `SystemSettingsLoader`（純 static、只讀 XML、不依賴任何服務）
- 確認 `LocalDefineAccess` runtime 行為不變
- 不改任何 BackendInfo 屬性訪問點

**獨立價值**：解開現有 boot-time chicken-and-egg 耦合，後續 DI 階段才能順利註冊。

### Phase 1：Bee.Db 配置注入（低風險）
**前置**：[plan-remove-backendinfo-db-globals.md](plan-remove-backendinfo-db-globals.md) 完成。
**目標**：拆解 `DbCommandSpec` → `BackendInfo` 的耦合，讓 Bee.Db 完全脫離 Bee.Definition 的 static 配置。

完成前置後，`BackendInfo` 上 DB 相關只剩 `MaxDbCommandTimeout`（cap 值）。但問題本質不是「單一欄位放哪」，而是 **`DbCommandSpec` setter 內部讀 static**（`src/Bee.Db/DbCommandSpec.cs:101`）——`DbCommandSpec` 是 DTO，用 `new` 建立於數百個呼叫點，沒有 DI 路徑，才被迫 reach 到 BackendInfo。這是 Bee.Db 黏住 BackendInfo 的根因。

**執行步驟**：

1. clamping 邏輯從 `DbCommandSpec.CommandTimeout` setter 搬到 `DbAccess` 執行路徑（`Execute` 系列方法）
2. `DbCommandSpec` 變純 DTO，`CommandTimeout` setter 只存值不夾值
3. `DbAccess` 透過 `IDbAccessFactory` 構造參數拿到 cap（per-app 配置，不需 `IOptions<T>` 包裝單一 int）
4. 刪除 `DbCommandSpecTests` 中兩個操弄 static 的 cap 測試（`CommandTimeout_ExceedsCap_UsesCap` / `CommandTimeout_NoCap_UsesValue`），不寫替代測試
5. 移除 `BackendInfo.MaxDbCommandTimeout` 屬性與 `BackendConfiguration.MaxDbCommandTimeout` 欄位

**對下游的意義**：多 app 場景（APP 30s / Web 60s / 排程 120s）自然支援——各 host 註冊 `IDbAccessFactory` 時傳入該 app 的 cap，不再共享 process-wide static。

**獨立價值**：
- Bee.Db 完全脫離 BackendInfo（DI 改造的第一個乾淨切點）
- 解掉一處「測試修改 production static」違規（與 testing.md 平行安全規則一致）
- 證明「簡單配置不一定走 `IOptions<T>`」——直接構造參數注入即可，為後續階段提供 pattern 參考

### Phase 2：ObjectCaching 與 DefineAccess（高風險，最重要）
**目標**：拆解 `IDefineAccess` 的 boot-time vs runtime 雙重職責。

- 確認 Phase 0 抽出的 `SystemSettingsLoader` 已涵蓋所有 boot-time 用例
- `IDefineAccess` 改為純 runtime 服務，由 DI 注入消費端
- **移除 `BackendInfo.DefinePath`**：引入 `PathOptions`（或同等注入式配置），轉換所有讀取點（`DefinePathInfo`、`MasterKeyProvider`、相關測試 fixture）
- 處理 `DatabaseSettings.Items` 這個 process-wide static 副作用（候選方案：改為 `IDatabaseSettingsProvider` 服務）
- Bee.ObjectCaching 的 6 個檔案改為注入式存取

**獨立價值**：解掉本次遷移最大的設計負債，後續 Business / Repository 層才能順利改造。

**風險**：此階段是設計負債最大的拆解（`DatabaseSettings.Items` static side effect、boot-time vs runtime 雙重職責），實際 `BackendInfo.DefineAccess` 引用 39 處跨多層分布。
**緩解**：sub-plan 細化 PR 切分策略；本階段為單一 PR（不分批），確保 main 上不存在「一半已注入、一半仍讀 BackendInfo.DefineAccess」狀態。

### Phase 3：Business 與 Repository 層注入（中–高風險）
**目標**：BO 與 Repository 透過 `IBeeContext` 取得依賴，廢除對 BackendInfo 的呼叫；BO 維持零 DI 註冊。

**執行步驟**：

1. 設計 `IBeeContext` 介面（成員清單依 Phase 2 完成後的 BO 依賴盤點決定，預期 ≤ 5 個核心服務）
2. `SystemBusinessObject` / `FormBusinessObject` 兩個 base class ctor 改為 `(IBeeContext, Guid accessToken, [string progId,] bool isLocalCall)`，於 base 內解包至 `protected` 屬性
3. 重新設計 `BusinessObjectFactory`：
   - `CreateSystemBusinessObject` 透過 `ActivatorUtilities.CreateInstance(sp, type, accessToken, isLocalCall)`
   - `CreateFormBusinessObject(progId, ...)` 透過 `IFormBoTypeResolver.Resolve(progId)` 取得 Type 後同樣以 `ActivatorUtilities` 建構
4. 設計 `IFormBoTypeResolver` 與對應 XML 結構（progId → 型別全名），啟動時讀入建立快取
5. 改造 4 個 Bee.Business 檔案、1 個 Bee.Repository 檔案
6. 處理 `samples/` 中現有 `new XxxBusinessObject(...)` 呼叫（**破壞性變更**，改走 factory）
7. 介面 `IBusinessObjectFactory` 的回傳型別從 `object` 收緊為 `IBusinessObject`

**獨立價值**：
- BO 依賴透過 base 統一管理；ERP 開發者新增 BO 不接觸 DI API
- BO-to-BO 呼叫透過共同介面 `ExecFunc(args)`，不依賴具體型別
- 測試可建簡單 `TestBeeContext`（屬性 setter 開放）後 `new RequisitionBO(testCtx, token, "Requisition", true)`，不需 `BackendInfo.Initialize`

### Phase 4：Api.Core 與 Api.AspNetCore（中風險）
**目標**：API 層全面 DI 化，建立組裝入口；補上 `IBeeContext.Services` 逃生口完成 BO 注入閉環；清理 Phase 3 預留的所有遺留項。

#### 從 Phase 3 繼承的待辦項（明確）

**屬性移除**：
- [ ] `BackendInfo.DefineAccess`（property + Initialize 內 ResolveDefineAccess wire-up；GlobalFixture / 測試 fixture 改用其他注入機制）
- [ ] `BackendInfo.SessionInfoService`（consumers：`AccessTokenValidator`、`DynamicApiEncryptionKeyProvider`）
- [ ] `BackendInfo.ApiEncryptionKeyProvider`（consumers：`JsonRpcExecutor`、`StaticApiEncryptionKeyProvider`；SystemBO 已用 `Services.GetService<T>`）
- [ ] `BackendInfo.LoginAttemptTracker`（only consumer was SystemBO，已用 `Services.GetService<T>`；屬性可隨 Phase 4 移除）
- [ ] `BackendInfo.AccessTokenValidator`（consumer：`ApiAccessValidator`）
- [ ] `BackendInfo.BusinessObjectFactory`（consumer：`JsonRpcExecutor.CreateBusinessObject`）
- [ ] `BackendInfo.CacheDataSourceProvider` / `BackendInfo.EnterpriseObjectService`（盤點 consumer 後決定移除）

**Bee.Db DML helpers 重構**（Phase 3 推遲）：
- [ ] 5 個 `*FormCommandBuilder` 移除 `(string progId)` ctor，保留 `(FormSchema)` ctor
- [ ] `IDialectFactory.CreateFormCommandBuilder(string)` 介面方法 + 5 個 DialectFactory 實作移除
- [ ] `SelectContextBuilder` ctor 接 `IDefineAccess`；`SelectCommandBuilder` 對應調整、傳遞
- [ ] `TableSchemaBuilder` ctor 接 `IDefineAccess`；`DatabaseRepository` 對應調整
- [ ] 對應測試遷移（含刪除測 `(string progId)` ctor 的測試方法）

**核心交付項**：
- `JsonRpcExecutor` 改為 scoped，建構式注入 `IBusinessObjectFactory`、`IAccessTokenValidator`、`IApiEncryptionKeyProvider`
- `Bee.Api.AspNetCore` 提供 `IServiceCollection.AddBeeFramework(IConfiguration)` extension
- `BusinessObjectFactory` 內部移除反射 `AssemblyLoader`，改用 `ActivatorUtilities`
- ASP.NET Core middleware 處理 scope 建立
- **替換 `IBeeContext.Services` 實作**：Phase 3 留下的 `BackendInfoServiceProvider`（轉發到 BackendInfo.X）換成真 DI scope 的 `IServiceProvider`，BO 程式碼完全不變
- `BackendInfo.Initialize` 流程簡化／逐步移除（待 Phase 5/6 測試 fixture 重寫後完全消失）

**獨立價值**：Web 應用可完全脫離 `BackendInfo`，作為 v5.0 推薦寫法。

**參考實作**：Phase 1-3 commits（`e832802a`、`d8a7cd41`、`ce2a9ece`）；Phase 3 sub-plan「實作時調整」段落記錄了重要的 infra 修正（`AssemblyLoader` 從 byte-load 改為 default load context）。

### Phase 5：測試基礎設施重寫（中風險）— ✅ 已完成
**目標**：移除 `GlobalFixture`、`TempDefinePath`、`[Collection("Initialize")]`。

實際落地：

- `BeeTestFixture` / `SharedDbFixture` 設計完成，每個測試 class 透過 `IClassFixture<T>` 取得獨立 `IServiceProvider`
- 70+ test class 從 `[Collection("Initialize")]` / `BeeTestServices` 遷移至 fixture-based DI
- `TempDefinePath` 廢除 —— 寫檔測試改用 `b.UseTempDefinePath()` fixture builder 或 method-level inline temp dir
- 7 個 cache class（`SystemSettingsCache` 等）改 ctor 注入 `PathOptions`；`LocalDefineAccess` 改 ctor 注入 `ICacheContainer`
- 刪除：`BeeTestServices` / `GlobalFixture` / `BaseTests` / `TempDefinePath` / `DefinePathInfo` / `CacheContainer` 靜態 facade / `ICacheBootstrapper`
- xUnit 平行恢復：本機 wall 1m47s / user 4m49s = ~2.7x parallel speedup（2749 tests）

唯一遺留：`DbConnectionManager` 靜態 facade（5 src 點 + 84 測試 `new DbAccess(id)` 點），獨立到 Phase 7 處理。

**獨立價值**：測試執行加速（無 process-wide lock）、隔離性提升。

### Phase 6：移除 BackendInfo 空殼
**目標**：刪除已縮減至空殼的 `BackendInfo` 與 `BackendConfiguration` 類別。

進入此 phase 時，`BackendInfo` 的所有屬性已在 Phase 1-4 中隨對應層的轉換被逐一刪除，剩下的應該是無成員的空類別或只剩 `Initialize` 之類的入口（也已無實質作用）。

- 刪除 `BackendInfo.cs` 與 `BackendConfiguration.cs`
- 移除任何尚未清掉的 `BackendInfo.Initialize` 呼叫點
- 更新 `docs/architecture-overview.md`、`docs/development-cookbook.md` 反映新模型
- 發 v5.0 release notes

**獨立價值**：技術債清零，新人不再看到 Service Locator 範例。

### Phase 7：DbConnectionManager 靜態 facade 移除 + DbAccess ctor DI 化
**目標**：移除 `DbConnectionManager` 靜態 facade、廢除 `[Collection("DbConnectionState")]`、將 `DbAccess` ctor 改為要求 `IDbConnectionManager`。

Phase 5 收尾時 `DbConnectionManager` 靜態 facade 暫留 —— 5 處 src 消費點 + 84 處測試 `new DbAccess(databaseId)` 呼叫點仍透過靜態取連線資訊。獨立 phase 處理：

- 5 src 點（`DbAccess` / `DbAccessFactory` / `SqliteTableSchemaProvider` / `TableUpgradeOrchestrator` / `TableSchemaBuilder`）改 ctor 注入 `IDbConnectionManager`
- 84 處測試 `new DbAccess(id)` 改走 `IDbAccessFactory.Create(databaseId)`（建議補 `BeeTestFixture.NewDbAccess(id)` 便利屬性）
- `DbConnectionManagerTests` / `DbAccessFactoryTests` 改測 DI instance；`[Collection("DbConnectionState")]` 廢除
- 刪除 `DbConnectionManager.cs` 靜態 facade、`IDbConnectionManagerBootstrapper`、`DbConnectionStateCollection.cs`

完成後：Bee.NET 框架所有「BackendInfo 系」靜態 facade 全部退場；剩餘 process-wide static 僅為 registry-style 一次寫入（`SysInfo` / `CacheInfo.Provider` / `DbProviderRegistry` / `DbDialectRegistry` / `ApiClientInfo.LocalServiceProvider`）。

**獨立價值**：DI 化里程碑收尾；全 repo 0 `[Collection]` 序列化要求；test wall-clock 進一步加速空間。

## 相容性策略

無外部消費者，**採全 DI 路徑、不留相容層**：

- 每個 phase 在單一 PR 內完成該層所有 `BackendInfo` 引用的刪除
- 不使用 `[Obsolete]` 過渡標記、不引入 dual-ctor、不建相容 adapter
- `BackendInfo` 隨各 phase 推進**逐屬性縮減**；Phase 6 刪除剩餘空殼
- 不引入 `BackendInfo.Bind(IServiceProvider)` 或任何 static `IServiceProvider` 持有點（避免 Service Locator 換皮）

## 測試策略

### 過渡期（Phase 1–5）
- 既有測試保留 `GlobalFixture` 機制，逐 phase 遷移
- 新增測試一律使用新 DI fixture（建立範本後跟進）
- CI 上要求「未遷移檔案 + 新 fixture 檔案」**都**綠燈

### Phase 5 完成後（2026-05-13）
- ✅ 廢除 `[Collection("Initialize")]` 串行限制
- ✅ 廢除 `TempDefinePath`（fixture-level `UseTempDefinePath()` 或 method-level inline temp dir 取代）
- ✅ 並行測試恢復至本機 ~2.7x parallel speedup
- 唯一保留的窄序列化 `[Collection("DbConnectionState")]`（2 個 class）待 Phase 7 清理

### 整合測試
每個 phase 完成必須通過：
- `dotnet build --configuration Release`（含 `TreatWarningsAsErrors`）
- `./test.sh`（全套單元測試）
- 至少一個 `samples/` 專案手動執行（API 端點 ping 通）

## 風險與緩解

| 風險 | 緩解方案 |
|------|---------|
| **Phase 2 `DefineAccess` 拆解過大** | 主計畫中標為「最高優先 sub-plan」，先單獨設計再開始 Phase 2 實作 |
| **BO 建構式擴張**（constructor over-injection） | 採用 `IBeeContext` 聚合常用服務 + base class 解包至具名屬性；BO 子類 ctor 簽章嚴格固定（見設計原則 §4） |
| **公開 API 破壞性變更**（samples、外部使用者） | 目前無外部消費者；v5.0 release notes 標注破壞性變更 |
| **單一 PR 內中間 commit build 破裂** | 允許（PR 合進 main 那刻必須綠即可）；rebase / squash 在合 PR 時處理 |
| **測試遷移工作量大**（實際 ~82 處） | 集中於 Phase 5 處理，PR 控制在 < 1500 lines diff |

## 成功標準

- [ ] `grep -r "BackendInfo\." src/ tests/` 結果為 0（除 BackendInfo.cs 本身已刪除）
- [ ] 所有 `samples/` 專案以 `AddBeeFramework` 啟動，不呼叫 `BackendInfo.Initialize`
- [ ] `dotnet test` 不依賴 `GlobalFixture` 或 `[Collection("Initialize")]`
- [ ] CI 通過時間相較 v4.x 不退步（理想：因移除全域初始化鎖而加速）
- [ ] `docs/architecture-overview.md` 中無 `BackendInfo` 字樣（除歷史背景章節）

> Sub-plan 進度追蹤見[本文件頂部「Sub-plan 進度」表格](#sub-plan-進度)。每完成一個 phase 同步更新該表狀態與連結。

## 未決議題（待 sub-plan 進一步討論）

- Phase 2：`DatabaseSettings.Items` static side effect 拆解方案（`IDatabaseSettingsProvider` vs 其他模式）
- Phase 3：`IBeeContext` 應包含哪幾個服務（盤點現有 BO 對 BackendInfo 服務的依賴交集後決定，預期 ≤ 5 個核心服務）
- Phase 3：progId → BO Type 對應 XML 的檔名與結構（命名候選：`FormBusinessObjects.xml` / `BoRouting.xml`）
- Phase 4：JsonRpcExecutor 的 lifetime 抉擇（Scoped vs Transient）
- Phase 5：測試 fixture 是否完全自訂或重用 `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`
