# 計畫：BackendInfo 由 Service Locator 改為 DI（主計畫）

**狀態：📝 擬定中**

> 本文件為主計畫（main plan），定義整體目標、原則、階段路線。
> 各階段的細部執行步驟另以 sub-plan 文件描述，列於文末。
>
> **前置計畫**：[plan-remove-backendinfo-db-globals.md](plan-remove-backendinfo-db-globals.md)
> — 先移除 `BackendInfo.DatabaseType` / `DatabaseId` 並導入 `DbCategoryIds` 常數與 `SessionInfo.CompanyDatabaseId`，
> 完成後 Phase 2 範圍會大幅簡化。

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
| **多平台適配** | 即將支援 MAUI / Desktop；Service Locator 在跨平台情境下生命週期語意難以表達 |
| **初始化耦合** | `DefineAccess` 既是 boot-time 配置來源（讀 `SystemSettings.xml`）又是 runtime 服務，職責不清 |
| **反射建構** | `BusinessObjectFactory` 用 `AssemblyLoader.CreateInstance` 建 BO，BO 無法宣告依賴 |

### 為何選擇 DI

- .NET 10 三種應用模型（ASP.NET Core / MAUI / Desktop with Generic Host）皆原生支援 `Microsoft.Extensions.DependencyInjection`，註冊邏輯可跨平台共用
- DI 容器強制把依賴宣告於建構式 → 編譯期可見、IDE 可分析、測試可注入 mock
- Options Pattern 與 `IValidateOptions<T>` 支援啟動期驗證，比 `BackendInfo.Initialize` 內部偷偷拋例外更早失敗
- 生命週期（Singleton / Scoped / Transient）語意明確，scope 邊界各平台可一致實作

## 目標

1. 移除 `BackendInfo` 類別，所有 Service Locator 呼叫點改為建構式注入或工廠模式
2. `SystemSettings.xml` 的配置仍由 XML 驅動，但反序列化後直接對映 Options 型別
3. 框架同時支援 Web / MAUI / Desktop，共用同一份服務註冊邏輯
4. 測試基礎設施簡化 — 廢除 `GlobalFixture` 全域初始化、`TempDefinePath` swap、`[Collection("Initialize")]` 串行
5. 公開 API surface（`samples/` 與 BO 建構式）的破壞性變更控制在 v5.0 單一版本內完成

## 不變條件（Invariants）

執行各階段時必須維持：

- **`SystemSettings.xml` 仍是配置主來源** — 不引入 `appsettings.json` 取代（MAUI 等平台讀檔行為一致性）
- **8 個核心服務的可置換性** — 使用者仍可在 XML 中宣告自訂實作型別替換預設
- **加密管線順序** — `序列化 → 壓縮 → 加密` 不變，DI 化不影響密碼學設計
- **每個階段必須可獨立 ship** — 不允許「重構半套既不能 ship 也不能回頭」
- **跨 commit 期間 build 必須通過** — 不能用 long-lived branch

## 範圍評估摘要

| 維度 | 數量 |
|------|------|
| `BackendInfo.*` 總引用點 | 217（`src/` 58、`tests/` 159） |
| 跨層 | 6 層（Definition / Base / Db / ObjectCaching / Business / Repository / Api.Core / Api.AspNetCore） |
| 服務介面 | 8 個必選 + 1 個可選 |
| 配置值欄位 | 5 個純配置 + 4 個加密金鑰 |
| 測試 fixture 需重寫 | `GlobalFixture`、`TempDefinePath`、`BackendInfoTests` 及 159 個呼叫點 |

## 設計原則

### 1. 兩種服務分類

| 類別 | 對應 DI 模式 |
|------|--------------|
| **純配置值**（DatabaseType、加密金鑰、LogOptions） | `IOptions<T>` |
| **可置換服務**（8 個 interface） | `services.AddSingleton<I, Impl>()` 或 `AddScoped` 視 lifetime |

### 2. 生命週期決策

| 服務 | Web | MAUI / Desktop | 備註 |
|------|-----|----------------|------|
| `IDbAccessFactory`、`IBusinessObjectFactory`、`IDefineAccess`、`IDefineStorage` | Singleton | Singleton | 一致 |
| `ICacheDataSourceProvider`、`IEnterpriseObjectService` | Singleton | Singleton | 一致 |
| `ISessionInfoService` | Scoped（per request） | Singleton（per app） | **平台差異點** |
| `IAccessTokenValidator` | Scoped | Singleton 或不註冊 | 平台差異點 |
| `IApiEncryptionKeyProvider` | Scoped | Singleton | 平台差異點 |
| `ILoginAttemptTracker` | Singleton（含 cache） | Singleton 或不註冊 | 一致 |

跨平台差異透過「平台專屬的 `Add<Platform>Bee()` extension」處理，核心 `AddBeeFramework()` 不涉及平台分支。

### 3. 工廠模式適用範圍

DI 容器無法直接管理「runtime 才知道參數的服務」，這類需透過工廠：

- `IDbAccessFactory` — 因 `DbAccess` 依連線 ID 建立
- `IBusinessObjectFactory` — 因 BO 名稱由 JSON-RPC 請求決定（保留現有介面，但內部改用 `IServiceProvider` + `ActivatorUtilities.CreateInstance`）

`IServiceProvider` **只允許**出現在這兩個工廠類別內，其他業務類別禁止注入 — 否則退化回 Service Locator。

### 4. SystemSettings 結構與 Options 一致化

現有 `BackendConfiguration` 內含 `Components`（型別名稱）、`SecurityKeySettings`（金鑰）、配置欄位散落於頂層。重構為：

```
SystemSettings
├── Database         : DbOptions
├── Encryption       : EncryptionOptions
├── Log              : LogOptions
└── Services         : ServiceRegistry
```

讓 `SystemSettings.Database` 物件**本身就是** `DbOptions`（可直接 `Options.Create(settings.Database)` 註冊），消除「XML 模型 ↔ Options」雙型別維護成本。

### 5. 階段獨立可 ship

每個階段完成後：
- 整個 repo build 通過、所有測試綠燈
- 不留 half-migrated 狀態（不允許「一半類別已注入、一半仍讀 BackendInfo」混搭超過一個 PR 的時間）
- `BackendInfo` 在最終階段才刪除；中間階段以 `[Obsolete]` 標記過渡 API

## 階段路線圖

### Phase 0：前置清理（低風險、可立即執行）
**目標**：把「初始化期讀 SystemSettings.xml」的職責從 `IDefineAccess` 抽離。

- 抽 `SystemSettingsLoader`（純 static、只讀 XML、不依賴任何服務）
- 確認 `LocalDefineAccess` runtime 行為不變
- 不改任何 BackendInfo 屬性訪問點

**獨立價值**：解開現有 boot-time chicken-and-egg 耦合，後續 DI 階段才能順利註冊。

### Phase 1：SystemSettings 結構重構（低風險）
**目標**：讓 SystemSettings 結構與未來的 Options 類別 1:1 對應。

- 設計新 `DbOptions`、`EncryptionOptions`、`LogOptions`、`ServiceRegistry` 類別
- 重構 `BackendConfiguration` 或新建 `SystemSettings` 容器以組合上述 Options
- 撰寫 XML schema migration tool（一次性轉換既有 `SystemSettings.xml`）
- `BackendInfo.Initialize` 仍是 Service Locator，但內部改吃新結構

**獨立價值**：即使不繼續 DI 化，新結構也比扁平屬性更可讀；提供未來 DI 註冊的「Options-shaped」介面。

### Phase 2：Bee.Db 配置注入（低風險）
**前置**：[plan-remove-backendinfo-db-globals.md](plan-remove-backendinfo-db-globals.md) 完成。
**目標**：底層配置（完成前置後僅剩 `MaxDbCommandTimeout`）改為注入式。

- 評估是否仍需 `DbOptions` 包裝（僅一個欄位，候選方案：保留 static / `IOptions<DbOptions>` / 直接注入 `int`）
- 設計 `IDbAccessFactory`，內部接收選定的配置形式
- `DbAccess` 提供雙建構式：DI 版 + 過渡版（從 BackendInfo 取得，標 `[Obsolete]`）
- 跨層調用點仍可呼叫無參數建構式，向下相容

**獨立價值**：證明 DI 模式在最簡單的純配置場景可行，建立後續階段的 pattern 基準。

### Phase 3：ObjectCaching 與 DefineAccess（高風險，最重要）
**目標**：拆解 `IDefineAccess` 的 boot-time vs runtime 雙重職責。

- 確認 Phase 0 抽出的 `SystemSettingsLoader` 已涵蓋所有 boot-time 用例
- `IDefineAccess` 改為純 runtime 服務，由 DI 注入消費端
- 處理 `DatabaseSettings.Items` 這個 process-wide static 副作用（候選方案：改為 `IDatabaseSettingsProvider` 服務）
- Bee.ObjectCaching 的 6 個檔案改為注入式存取

**獨立價值**：解掉本次遷移最大的設計負債，後續 Business / Repository 層才能順利改造。

**風險**：此階段牽涉 266 次 `BackendInfo.DefineAccess.*` 呼叫，需仔細設計過渡期共存策略。
**緩解**：sub-plan 中再細化 — 可能需要 feature flag 或臨時的 adapter pattern。

### Phase 4：Business 與 Repository 層注入（中–高風險）
**目標**：BO 與 Repository 建構式注入依賴，廢除對 BackendInfo 的呼叫。

- 重新設計 `IBusinessObjectFactory.Create()` 使用 `IServiceProvider` + `ActivatorUtilities.CreateInstance`
- BO 基底類別建構式加入常用依賴（`ISessionInfoService`、`IDbAccessFactory` 等）
- 處理 `samples/` 中現有 `new XxxBusinessObject(...)` 呼叫的相容性（**破壞性變更**）
- 4 個 Bee.Business 檔案、1 個 Bee.Repository 檔案改造

**獨立價值**：BO 依賴顯性化，測試可直接 `new UserBO(mockSession, mockDb)`，不需 `BackendInfo.Initialize`。

### Phase 5：Api.Core 與 Api.AspNetCore（中風險）
**目標**：API 層全面 DI 化，建立組裝入口。

- `JsonRpcExecutor` 改為 scoped，建構式注入 `IBusinessObjectFactory`、`IAccessTokenValidator`
- `Bee.Api.AspNetCore` 提供 `IServiceCollection.AddBeeFramework(IConfiguration)` extension
- `BusinessObjectFactory` 內部完全移除反射 `AssemblyLoader`，改用 `ActivatorUtilities`
- ASP.NET Core middleware 處理 scope 建立

**獨立價值**：Web 應用可完全脫離 `BackendInfo`，作為 v5.0 推薦寫法。

### Phase 6：跨平台支援（MAUI / Desktop）
**目標**：提供平台專屬註冊 extension，驗證跨平台可行性。

- 新增 `Bee.Maui`（或 `Bee.Hosting.Maui`）專案，提供 `MauiAppBuilder.UseBeeFramework()`
- 新增 `Bee.Desktop`（或重用 Generic Host），提供 `IHostBuilder.UseBeeFramework()`
- 設計平台專屬 `ISessionInfoService` 實作（Web: HttpContext-backed、MAUI/Desktop: in-memory singleton）
- 建立 sample 專案各一份

**獨立價值**：證明 DI 改造後框架可跨三介面使用。

### Phase 7：測試基礎設施重寫（中風險）
**目標**：移除 `GlobalFixture`、`TempDefinePath`、`[Collection("Initialize")]`。

- 設計 `BeeTestFixture` — 每個測試 class 建獨立 `ServiceProvider`（取代 process-wide init）
- 重寫 159 個測試引用點為「從 fixture 取 scope」
- `TempDefinePath` 廢除，改為「fixture 內注入 in-memory `IDefineAccess`」
- 並行測試的 static state 競爭問題自然解消

**獨立價值**：測試執行加速（無 process-wide lock）、隔離性提升。

### Phase 8：移除 BackendInfo
**目標**：所有過渡 API 清除，`BackendInfo` 類別刪除。

- 刪除 `BackendInfo.cs` 與 `BackendConfiguration.cs`
- 刪除所有 `[Obsolete]` 標記的過渡建構式
- 更新 `docs/architecture-overview.md`、`docs/development-cookbook.md` 反映新模型
- 發 v5.0 release notes

**獨立價值**：技術債清零，新人不再看到 Service Locator 範例。

## 跨平台支援考量

組裝入口的階層：

```
Bee.Core (AddBeeFramework)
   ├── Bee.Api.AspNetCore.AddBeeWeb
   ├── Bee.Maui.UseBeeMobile
   └── Bee.Desktop.UseBeeDesktop
```

- `AddBeeFramework` 負責「跨平台一致的部分」（純配置、Singleton 服務、工廠）
- 平台 extension 補上「平台專屬部分」（`ISessionInfoService` 實作、scope 行為）

各平台 sample 專案於 Phase 6 建立，作為合約驗證點。

## 相容性策略

| 期間 | BackendInfo 狀態 | 對 user code 衝擊 |
|------|-----------------|-------------------|
| Phase 0–2 | 完整保留，無 `[Obsolete]` | 零衝擊（內部重整） |
| Phase 3–5 | 過渡建構式 / adapter 出現，標 `[Obsolete]` 但仍可運作 | 編譯警告但不破壞 |
| Phase 6–7 | 過渡 API 全標 `[Obsolete]`，新平台 sample 全用 DI | 警告 + 文件引導遷移 |
| Phase 8（v5.0） | 刪除 | 破壞性，release notes 標注 |

過渡期間「BackendInfo 與 DI 並存」的關鍵約束：**BackendInfo 內部實作改為從 DI container 解析**（而非自己反射建構），確保兩種寫法行為等價。

## 測試策略

### 過渡期（Phase 1–7）
- 既有測試保留 `GlobalFixture` 機制，逐 phase 遷移
- 新增測試一律使用新 DI fixture（建立範本後跟進）
- CI 上要求「未遷移檔案 + 新 fixture 檔案」**都**綠燈

### 完成後（Phase 8）
- 廢除 `[Collection("Initialize")]` 串行限制
- 廢除 `TempDefinePath`（檔案隔離由 `IDefineAccess` mock 處理）
- 並行測試完全恢復，CI 時間預期縮短

### 整合測試
每個 phase 完成必須通過：
- `dotnet build --configuration Release`（含 `TreatWarningsAsErrors`）
- `./test.sh`（全套單元測試）
- 至少一個 `samples/` 專案手動執行（API 端點 ping 通）

## 風險與緩解

| 風險 | 緩解方案 |
|------|---------|
| **Phase 3 `DefineAccess` 拆解過大** | 主計畫中標為「最高優先 sub-plan」，先單獨設計再開始 Phase 3 實作 |
| **BO 建構式擴張**（constructor over-injection） | 設計 `IBeeContext` 聚合常用服務作為單一注入點，避免每個 BO 注入 5+ 服務 |
| **公開 API 破壞性變更**（samples、外部使用者） | v5.0 release notes 提供完整 migration guide；保留 v4.x 維護分支 1 個 release 週期 |
| **過渡期 `BackendInfo` 與 DI 行為分歧** | BackendInfo 內部改為從 DI 解析（單一 source of truth） |
| **MAUI 平台特殊性**（檔案系統、resource 載入） | Phase 6 建立 MAUI sample 時實機驗證；ARM Mac 與 Windows 各跑一次 |
| **測試遷移工作量大**（159 處） | 逐 phase 遷移（不跨 phase 批改），每 phase PR 控制在 < 500 lines diff |

## 成功標準

- [ ] `grep -r "BackendInfo\." src/ tests/` 結果為 0（除 BackendInfo.cs 本身已刪除）
- [ ] 所有 `samples/` 專案以 `AddBeeFramework` 啟動，不呼叫 `BackendInfo.Initialize`
- [ ] `Bee.Api.AspNetCore`、`Bee.Maui`、`Bee.Desktop` 各有 hello-world sample 並通過手動驗證
- [ ] `dotnet test` 不依賴 `GlobalFixture` 或 `[Collection("Initialize")]`
- [ ] CI 通過時間相較 v4.x 不退步（理想：因移除全域初始化鎖而加速）
- [ ] `docs/architecture-overview.md` 中無 `BackendInfo` 字樣（除歷史背景章節）

## 後續 Sub-plans 清單

各 phase 在動工前都會寫一份 sub-plan，存於 `docs/plans/`，由主計畫連結追蹤：

- [ ] `plan-backendinfo-di-phase0-systemsettings-loader.md` — Phase 0
- [ ] `plan-backendinfo-di-phase1-options-restructure.md` — Phase 1
- [ ] `plan-backendinfo-di-phase2-bee-db-options.md` — Phase 2
- [ ] `plan-backendinfo-di-phase3-defineaccess-decouple.md` — Phase 3（**最關鍵，需先深度設計**）
- [ ] `plan-backendinfo-di-phase4-business-injection.md` — Phase 4
- [ ] `plan-backendinfo-di-phase5-api-layer-di.md` — Phase 5
- [ ] `plan-backendinfo-di-phase6-multiplatform.md` — Phase 6
- [ ] `plan-backendinfo-di-phase7-test-infra.md` — Phase 7
- [ ] `plan-backendinfo-di-phase8-remove-backendinfo.md` — Phase 8

每個 sub-plan 完成後在本文件對應 checkbox 打勾，方便追蹤主計畫進度。

## 未決議題（待 sub-plan 進一步討論）

- Phase 3：`DatabaseSettings.Items` static side effect 拆解方案（`IDatabaseSettingsProvider` vs 其他模式）
- Phase 4：BO 是否引入 `IBeeContext` 聚合介面，避免建構式爆炸
- Phase 5：JsonRpcExecutor 的 lifetime 抉擇（Scoped vs Transient）
- Phase 6：MAUI 平台 `SystemSettings.xml` 載入方式（嵌入式 resource vs `FileSystem.AppDataDirectory`）
- Phase 7：測試 fixture 是否完全自訂或重用 `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`
