# ADR-003：使用靜態 Service Locator 而非依賴注入

## 狀態

已採納

## 背景

框架需要一個機制讓各層存取共用的 Provider（如 BusinessObjectProvider、RepositoryProvider、DefineAccess 等）。常見選項：

1. **依賴注入（DI）**：透過建構子或屬性注入，由 DI 容器管理生命週期
2. **靜態 Service Locator**：透過靜態類別提供全域存取點

## 決策

採用靜態 Service Locator 模式，透過 `BackendInfo`、`RepositoryInfo`、`CacheFunc`、`ApiServiceOptions` 等靜態類別提供全域存取。

## 理由

- **跨宿主環境**：框架需同時支援 ASP.NET Core（有內建 DI）、WinForms、Console App、Blazor 等多種宿主環境。靜態存取不依賴特定 DI 容器，所有環境一致。
- **歷史相容性**：框架早期目標為 netstandard2.0，無法依賴 `Microsoft.Extensions.DependencyInjection`。雖然現已改採 net10.0，但此模式已為既有慣例。
- **簡化初始化**：應用程式啟動時只需按順序設定靜態屬性，不需要建構複雜的 ServiceCollection 註冊流程。
- **確定性初始化**：靜態建構子確保 Provider 在首次存取時初始化，避免 DI 容器解析順序不明確的問題。
- **既有慣例**：框架從 .NET Framework 時代延續此模式，WinForms 等非 DI 環境的使用者已習慣此 API。

## 取捨

- **測試困難**：靜態狀態難以在測試間隔離，需要額外的 reset 機制。
- **隱含相依**：呼叫端的相依關係不在建構子中明確宣告，閱讀程式碼時不容易看出。
- **初始化順序敏感**：必須嚴格遵守初始化順序（見 `docs/development-constraints.md`），違反會在執行時期才發現。
- **不符合現代 .NET 慣例**：新的 .NET 專案普遍採用 DI。

## 影響

- `BackendInfo`（Bee.Definition）：Provider 與安全金鑰的全域入口
- `RepositoryInfo`（Bee.Repository.Abstractions）：Repository Provider 的全域入口
- `CacheFunc`（Bee.ObjectCaching）：快取操作的全域 Facade
- `ApiServiceOptions`（Bee.Api.Core）：API 序列化/壓縮/加密元件的全域配置
- `ApiClientInfo`（Bee.Api.Client）：用戶端連線配置的全域入口
- 初始化順序記錄於 `docs/development-constraints.md` 和 `docs/development-cookbook.md`
