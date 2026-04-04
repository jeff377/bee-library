# Bee.NET Library — 專案指引

## 專案概述

Bee.NET 是一套模組化的 .NET 企業應用程式框架，以 NuGet 套件形式發布。採用 JSON-RPC 2.0 API 模式，強調安全性、可插拔序列化與跨平台相容性。

- **版本**：3.6.2（`src/Directory.Build.props`）
- **授權**：MIT
- **主要目標框架**：`netstandard2.0` + `net8.0`（核心）、`net10.0`（API / 測試）

## 目錄結構

```
src/         # 核心套件（Bee.Base, Bee.Define, Bee.Api.Core 等）
tests/       # 對應的單元測試專案
samples/     # 示範專案
tools/       # 工具程式
```

## 常用命令

```bash
# 還原相依套件
dotnet restore

# 建置（Release）
dotnet build <project>.csproj --configuration Release --no-restore

# 執行測試（所有）
dotnet test --configuration Release

# 執行特定測試專案
dotnet test tests/<Project>.UnitTests/<Project>.UnitTests.csproj

# 封裝 NuGet
dotnet pack src/<Project>/<Project>.csproj --configuration Release --output ./nupkgs
```

## 架構分層

| 層級 | 專案 |
|------|------|
| 呈現層 | Bee.UI.Core, Bee.UI.WinForms |
| API 層 | Bee.Api.AspNetCore, Bee.Api.AspNet, Bee.Api.Core |
| 商業邏輯層 | Bee.Business |
| 資料存取層 | Bee.Repository, Bee.Repository.Abstractions, Bee.Db |
| 基礎設施 | Bee.Base, Bee.Define, Bee.Cache, Bee.Connect |

## 規則導入

@rules/code-style.md
@rules/testing.md
@rules/security.md
