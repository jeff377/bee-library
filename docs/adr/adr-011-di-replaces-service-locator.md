# ADR-011：採用 DI 取代靜態 Service Locator

## 狀態

已採納（2026-05-13）

Supersedes [ADR-003](adr-003-static-service-locator.md)。

## 背景

ADR-003（採用靜態 Service Locator）的前提已不再適用：

- 框架已從 netstandard2.0 改為 **net10.0**，可直接使用 `Microsoft.Extensions.DependencyInjection`，不再需要避開 DI 容器以維持跨 target 相容性
- 靜態 facade 累積的測試隔離成本超過原本「簡化初始化」的收益 —— 主計畫盤點顯示 BackendInfo 系靜態類別被 **159 個測試**引用，需要 `GlobalFixture` / `TempDefinePath` / `[Collection("Initialize")]` 等多重機制維持隔離，仍頻繁出現 process-wide static race
- BO 隱含相依（呼叫端的依賴關係不在建構子中明確宣告）、初始化順序敏感（違反會在執行時期才發現）、不符現代 .NET 慣例

## 決策

全面採用建構式注入（ctor injection）；以 `IServiceCollection.AddBeeFramework(BackendConfiguration, PathOptions)` 為框架服務註冊入口，由 `Bee.Api.AspNetCore` 套件提供。

設計範圍與不變條件詳見主計畫 [plan-backendinfo-to-di-migration.md](../plans/plan-backendinfo-to-di-migration.md) §「不變條件」與「設計原則」。

## 理由

- **可見的依賴宣告**：所有服務依賴出現在建構式參數列；編譯期可見、IDE 可靜態分析、code review 容易
- **可測試性**：測試直接 `new BO(testCtx, ...)` 注入 mock，無需 process-wide reset 機制；測試 fixture 改為 per-class `IServiceProvider`，xUnit 平行恢復
- **Lifetime 語意明確**：Singleton / Scoped / Transient 對應到 DI 容器，per-request scope 邊界清楚（過去以「執行緒上下文 + 靜態服務」混搭）
- **Options Pattern 啟動期驗證**：`IValidateOptions<T>` 比 `BackendInfo.Initialize` 內部偷偷拋例外更早失敗
- **服務可置換性保留**：`SystemSettings.xml` 內 `BackendComponents` 仍宣告每個替換介面的具體型別名稱；`AddBeeFramework` 讀取後將設定的型別註冊到 DI 容器

## 取捨

- **近端模式（`Bee.Api.Client` in-process）需處理 ServiceProvider 注入點**：在 client process 內呼叫後端邏輯時，目前以 `ApiClientInfo.LocalServiceProvider` 過渡保留靜態 holder。待後續 `Bee.Api.Client` 重構時再決定如何套用本 ADR 的 DI 註冊邏輯
- **BO 子類仍走零 DI 註冊**：ERP 應用會有上千個 `FormBusinessObject` 子類，由 progId XML 表派發；應用開發者寫新 BO 時不應接觸 DI API。改採 `IBeeContext` 聚合 BO 必用核心服務 + `ActivatorUtilities.CreateInstance(sp, boType, accessToken, progId, isLocalCall)` 由 factory 建構（見主計畫 §「設計原則 §4」）
- **遷移為 v5.0 破壞性變更**：採全 DI 路徑、不留 `[Obsolete]` 過渡層、不引入 dual-ctor 或相容 adapter。每個 phase 在單一 PR 內完成該層所有靜態 facade 引用的刪除

## 影響

### 移除的靜態 facade（全 Bee.NET 範圍）

| 類別 | 原所在套件 | 角色 |
|------|-----------|------|
| `BackendInfo` | Bee.Definition | 8 個服務 + 加密金鑰 + 配置值的全域入口 |
| `RepositoryInfo` | Bee.Repository.Abstractions | Repository Provider 全域入口 |
| `CacheFunc` / `CacheContainer` | Bee.ObjectCaching | 快取操作 facade、cache singleton |
| `DefinePathInfo` | Bee.Definition | 定義檔路徑全域入口 |
| `DbConnectionManager` | Bee.Db | 資料庫連線資訊靜態 facade |
| `ApiServiceOptions` | Bee.Api.Core | API 序列化/壓縮/加密元件全域配置 |

### 保留的 process-wide static（registry-style 一次寫入，不影響並行）

- `SysInfo` —— process-wide debug flag / payload options（一次寫入）
- `CacheInfo.Provider` —— cache backend（per-host 設定一次）
- `DbProviderRegistry` —— ADO.NET `DbProviderFactory` 註冊表
- `DbDialectRegistry` —— framework `IDialectFactory` 註冊表
- `ApiClientInfo.LocalServiceProvider` —— `Bee.Api.Client` 近端模式過渡 holder（待後續 ADR 處理）

### 後端宿主啟動流程

```text
1. paths = new PathOptions { DefinePath = "..." }
2. settings = SystemSettingsLoader.Load(paths)
3. SysInfo.Initialize(settings.CommonConfiguration)
4. services.AddBeeFramework(settings.BackendConfiguration, paths)
5. provider = services.BuildServiceProvider()
6. app.UseBeeFramework()   // ASP.NET only — 目前為 no-op，保留作未來 middleware 註冊點
```

完整參考見 [docs/development-cookbook.md § Framework Initialization Order](../development-cookbook.md#framework-initialization-order)。

### 測試基礎設施

- `[Collection("Initialize")]` / `GlobalFixture` / `BeeTestServices` / `TempDefinePath` 全數移除
- 取代為 `IClassFixture<BeeTestFixture>`（per-class `IServiceProvider`）+ `SharedDbFixture`（process-wide shared DB schema/seed）
- xUnit 平行恢復：本機 wall-clock ~2.7x parallel speedup（2749 tests）

## 實作參考

| 文件 | 內容 |
|------|------|
| [plan-backendinfo-to-di-migration.md](../plans/plan-backendinfo-to-di-migration.md) | 主計畫：整體目標、不變條件、7 個 phase 路線圖、設計原則 |
| Phase 0–7 sub-plans（同目錄） | 各層改造的執行細節與盤點 |
| [docs/development-cookbook.md](../development-cookbook.md) | DI 化後的初始化流程與請求管線 |
| [docs/development-constraints.md](../development-constraints.md) | 初始化順序限制（DI 模型） |
