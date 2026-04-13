# ADR-006：雙目標框架策略（netstandard2.0 + net10.0）

## 狀態

已採納

## 背景

框架需要決定 NuGet 套件的目標框架。選項包括：

1. **僅 netstandard2.0**：最大相容性，但無法使用新版 API
2. **僅 net10.0**：最新功能與效能，但限制使用者環境
3. **雙目標（netstandard2.0 + net10.0）**：兩者兼顧，但增加維護成本

## 決策

核心套件（Bee.Base、Bee.Definition、Bee.Api.Core、Bee.Api.Client 等）採用 `netstandard2.0` + `net10.0` 雙目標框架。API 託管套件（Bee.Api.AspNetCore）僅目標 `net10.0`。

## 理由

- **向後相容**：使用者可能仍在 .NET Framework 4.7.2+ 或較舊的 .NET Core 環境中執行 WinForms / Console 應用程式。netstandard2.0 確保這些環境可以使用核心套件。
- **新版最佳化**：net10.0 目標允許使用 `Span<T>`、新的加密 API、效能改進等現代 .NET 功能，透過條件編譯（`#if NETSTANDARD2_0`）在不同框架提供最佳實作。
- **API 託管例外**：`Bee.Api.AspNetCore` 依賴 ASP.NET Core，天然只能在 .NET（非 .NET Framework）上執行，因此僅目標 net10.0 是合理的。
- **NuGet 自動選擇**：使用者安裝 NuGet 套件時，NuGet 會自動選擇最匹配的目標框架，不需要使用者手動處理。

## 取捨

- **維護成本增加**：條件編譯區塊（`#if`）增加程式碼複雜度，且需要在兩個框架上分別測試。
- **API 限制**：netstandard2.0 目標中不能使用僅限 .NET 5+ 的 API（如 `System.Half`、新的 `Span` 重載等）。
- **建置時間增加**：每次建置需要編譯兩個目標框架。

## 影響

- `src/Directory.Build.props`：定義共用的目標框架設定
- 各專案 `.csproj` 中 `<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>`
- `Bee.Api.AspNetCore.csproj` 例外：`<TargetFramework>net10.0</TargetFramework>`
- `Bee.Base/Security/` 中的加密實作使用 `#if NETSTANDARD2_0` 條件編譯
- `.claude/rules/code-style.md` 規定「目標 netstandard2.0 + net10.0 的核心套件不使用僅限新版 API」
