# 計畫：將 Bee.Core 更名為 Bee.Base

## 背景

`Bee.Core` 套件名稱已被 NuGet.org 上的其他人佔用，無法以此名稱發佈。
由於本套件原名即為 `Bee.Base`（3.x 版本），且內容為整個框架的底層基礎設施，
決定完整更名回 `Bee.Base`，包含資料夾、專案檔、命名空間一併調整。

## 影響範圍統計

| 類型 | 數量 |
|------|------|
| `namespace Bee.Core` 宣告（src/Bee.Core/） | 74 處 |
| `using Bee.Core` 參考（src/） | 339 處 |
| `using Bee.Core` 參考（tests/） | 27 處 |
| ProjectReference 指向 Bee.Core（csproj） | 4 個 |
| slnx 路徑項目 | 2 筆 |
| GitHub Actions yml | 2 個檔案 |
| 文件（docs） | 1 個檔案 |

---

## 執行步驟

### 步驟 1：重命名資料夾與專案檔

| 原路徑 | 新路徑 |
|--------|--------|
| `src/Bee.Core/` | `src/Bee.Base/` |
| `src/Bee.Base/Bee.Core.csproj` | `src/Bee.Base/Bee.Base.csproj` |
| `tests/Bee.Core.UnitTests/` | `tests/Bee.Base.UnitTests/` |
| `tests/Bee.Base.UnitTests/Bee.Core.UnitTests.csproj` | `tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj` |

### 步驟 2：更新 Bee.Base.csproj 內容

- `<Description>` 更新為 Bee.Base 說明
- `<PackageTags>` 將 `bee.core` 改為 `bee.base`

### 步驟 3：更新 ProjectReference（4 個 csproj）

| 檔案 | 變更 |
|------|------|
| `src/Bee.Definition/Bee.Definition.csproj` | ProjectReference 路徑改為 `../Bee.Base/Bee.Base.csproj` |
| `tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj` | ProjectReference 路徑改為 `../../src/Bee.Base/Bee.Base.csproj` |
| `tests/Bee.Business.UnitTests/Bee.Business.UnitTests.csproj` | ProjectReference 路徑改為 `../../src/Bee.Base/Bee.Base.csproj` |
| `tests/Bee.Definition.UnitTests/Bee.Definition.UnitTests.csproj` | ProjectReference 路徑改為 `../../src/Bee.Base/Bee.Base.csproj` |

### 步驟 4：全域替換命名空間與 using（共 440 處）

以下替換全域執行：

| 替換前 | 替換後 |
|--------|--------|
| `namespace Bee.Core` | `namespace Bee.Base` |
| `using Bee.Core` | `using Bee.Base` |
| `Bee.Core.` （型別完整名稱，如 XML 文件中） | `Bee.Base.` |

範圍：`src/Bee.Base/**/*.cs`、`src/**/*.cs`、`tests/**/*.cs`

### 步驟 5：更新 Bee.Library.slnx

```xml
<!-- 舊 -->
<Project Path="src/Bee.Core/Bee.Core.csproj" />
<Project Path="tests/Bee.Core.UnitTests/Bee.Core.UnitTests.csproj" />

<!-- 新 -->
<Project Path="src/Bee.Base/Bee.Base.csproj" />
<Project Path="tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj" />
```

### 步驟 6：更新 GitHub Actions yml

**build-ci.yml** 與 **nuget-publish.yml**：
- build 步驟：`src/Bee.Core/Bee.Core.csproj` → `src/Bee.Base/Bee.Base.csproj`
- pack 步驟：同上
- nuget-publish.yml release body：`- Bee.Core` → `- Bee.Base`

### 步驟 7：更新 docs/terminology.md

將文件中 `Bee.Core` 相關說明更新為 `Bee.Base`。

---

## 驗證項目

- [ ] `dotnet build Bee.Library.slnx` 成功，0 Warning、0 Error
- [ ] `dotnet restore Bee.Library.slnx` 成功
- [ ] Bee.Core.UnitTests 更名後測試專案可正常建置
- [ ] commit & push 後 build-ci.yml 通過

---

## 注意事項

- 步驟 4 全域替換前，先確認無其他套件的命名空間也含有 `Bee.Core`（避免誤替換）
- `Bee.Core` 在 NuGet 的已發佈版本（他人的）不受影響
- 已推送至 NuGet 的 5 個套件（`Bee.Api.*`、`Bee.Business`）均不依賴 `Bee.Core` 的 NuGet 套件 ID，僅使用 ProjectReference，不受影響
