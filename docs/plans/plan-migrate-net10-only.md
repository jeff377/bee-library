# 全面改採 net10.0 單一目標框架計畫

## 背景

根據 [ADR-006](../adr/adr-006-dual-target-framework.md) 的評估結論，所有前端消費者（WinForms、Web、APP）皆已是 net10.0+，netstandard2.0 已無消費者。本計畫將全面移除 netstandard2.0 目標，簡化程式碼與維護成本。

---

## 影響範圍

### 1. 專案檔（10 個 csproj）

將 `<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>` 改為 `<TargetFramework>net10.0</TargetFramework>`：

| 專案 | 檔案 |
|------|------|
| Bee.Base | `src/Bee.Base/Bee.Base.csproj` |
| Bee.Definition | `src/Bee.Definition/Bee.Definition.csproj` |
| Bee.Business | `src/Bee.Business/Bee.Business.csproj` |
| Bee.Repository.Abstractions | `src/Bee.Repository.Abstractions/Bee.Repository.Abstractions.csproj` |
| Bee.Repository | `src/Bee.Repository/Bee.Repository.csproj` |
| Bee.Db | `src/Bee.Db/Bee.Db.csproj` |
| Bee.ObjectCaching | `src/Bee.ObjectCaching/Bee.ObjectCaching.csproj` |
| Bee.Api.Core | `src/Bee.Api.Core/Bee.Api.Core.csproj` |
| Bee.Api.Contracts | `src/Bee.Api.Contracts/Bee.Api.Contracts.csproj` |
| Bee.Api.Client | `src/Bee.Api.Client/Bee.Api.Client.csproj` |

> 注意：`Bee.Api.AspNetCore` 已是 `net10.0` 單一目標，不需變更。

### 2. 條件編譯移除（1 個檔案）

| 檔案 | 說明 |
|------|------|
| `src/Bee.Base/Security/PasswordHasher.cs` (行 79-91) | `PBKDF2SHA256` 方法中的 `#if NETSTANDARD2_0` 區塊，移除 netstandard2.0 的 SHA-1 降級路徑，僅保留 net10.0 的 `Rfc2898DeriveBytes.Pbkdf2` SHA-256 實作 |

### 3. 文件更新

| 檔案 | 變更 |
|------|------|
| `docs/adr/adr-006-dual-target-framework.md` | 狀態改為「已採納」，記錄遷移完成 |
| `.claude/rules/code-style.md` | 移除「目標 netstandard2.0 + net10.0 的核心套件不使用僅限新版 API」規則 |
| `.claude/CLAUDE.md` | 主要目標框架描述更新為 `net10.0` |
| `src/Bee.Base/README.md` | 目標框架段落更新（移除 netstandard2.0） |
| `src/Bee.Base/README.zh-TW.md` | 同步更新繁體中文版 |

---

## 執行步驟

### 步驟一：修改專案檔

將 10 個 csproj 的 `<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>` 改為 `<TargetFramework>net10.0</TargetFramework>`。

### 步驟二：移除條件編譯

清理 `PasswordHasher.cs` 中的 `#if NETSTANDARD2_0` 區塊：
- 移除 netstandard2.0 分支（SHA-1 降級路徑）
- 移除 `#pragma warning disable/restore`
- 僅保留 `Rfc2898DeriveBytes.Pbkdf2(..., HashAlgorithmName.SHA256, ...)` 呼叫

### 步驟三：建置驗證

```bash
dotnet build --configuration Release
```

確認所有專案編譯通過，無警告（`TreatWarningsAsErrors` 開啟）。

### 步驟四：執行測試

```bash
dotnet test --configuration Release
```

確認所有單元測試通過。

### 步驟五：更新文件

依「影響範圍 §3」所列更新各文件。

---

## 風險評估

| 風險 | 等級 | 緩解措施 |
|------|------|----------|
| 遺漏的 netstandard2.0 消費者 | 低 | ADR-006 已確認三個前端皆為 net10.0+ |
| PasswordHasher 行為變更 | 無 | netstandard2.0 分支僅為降級相容，實際部署環境皆使用 net10.0 路徑 |
| NuGet 套件相容性 | 低 | 下一次版本發佈（minor 版號提升）時才會影響消費者，消費者有明確的版本升級行為 |
