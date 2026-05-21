# 計畫：依 code-style 規範重構全部程式碼

**狀態：✅ 已完成（2026-05-06）**

## 背景

bee-ui-core 目前共 5 個原始碼檔案 + 1 個 csproj，與使用者層 `~/.claude/rules/code-style.md` 規範存在多項落差，需逐項對齊。

### 檢查清單對照

| 規範項目 | 現況 | 需處理？ |
|---|---|---|
| 資料夾與命名空間一致性（IDE0130） | `Common/` 與 `Interface/` 子資料夾，但全部 `namespace Bee.UI.Core` | ✅ |
| 介面就近放置（不集中至 `Interface/`） | 介面集中在 `Interface/` | ✅ |
| XML 文件註解使用英文 | 全部用繁體中文 | ✅ |
| 預設不寫註解、寫 WHY 不寫 WHAT | `ClientInfo.cs` 多處冗餘 in-body 註解 | ✅ |
| Nullable Reference Types 啟用 | csproj 未啟用 | ✅ |
| Implicit Usings 啟用 | csproj 未啟用 | ✅ |
| `TreatWarningsAsErrors` / `Deterministic` | `src/Directory.Build.props` 已啟用 | — |
| 私有欄位 `_camelCase` | `ClientInfo.cs` 已遵守 | — |
| 縮排（4 空白）/ 行尾（LF）/ UTF-8 | 抽樣檢視符合 | — |

## 範圍

**程式碼檔案（5 個）：**
- `src/Bee.UI.Core/Common/ClientInfo.cs`
- `src/Bee.UI.Core/Common/EndpointStorage.cs`
- `src/Bee.UI.Core/Common/VersionInfo.cs`
- `src/Bee.UI.Core/Interface/IEndpointStorage.cs`
- `src/Bee.UI.Core/Interface/IUIViewService.cs`

**設定檔（1 個）：**
- `src/Bee.UI.Core/Bee.UI.Core.csproj`

## 重構項目

### 1. 檔案結構扁平化

合併 `Common/` 與 `Interface/` 為單一根目錄，所有檔案直接放在 `src/Bee.UI.Core/` 下。

**理由：**
- 全部 5 個檔案都已是 `namespace Bee.UI.Core`，扁平化後立即符合 IDE0130
- code-style.md 明文反對 `Interface/` 集中放置（「介面可依所屬功能就近放置，不強制集中」）
- 規模太小，建子命名空間（如 `Bee.UI.Core.Endpoint`）反而 over-engineering
- 未來真正出現 sub-domain 時再分（屆時資料夾與命名空間同步建立）

**結果：**
```
src/Bee.UI.Core/
├── ClientInfo.cs
├── EndpointStorage.cs
├── IEndpointStorage.cs
├── IUIViewService.cs
└── VersionInfo.cs
```

> **注意**：搬檔以 `git mv` 進行以保留歷史。

### 2. csproj 啟用語言特性

`src/Bee.UI.Core/Bee.UI.Core.csproj` 加入：

```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<LangVersion>latest</LangVersion>
```

> `TreatWarningsAsErrors` / `Deterministic` 已由 `src/Directory.Build.props` 提供，不重複。

### 3. 啟用 Nullable 後的程式碼調整

預期會出現的 nullable warning：

| 檔案 | 位置 | 處理方式 |
|---|---|---|
| `ClientInfo.cs` | 多處 `static field = null` 初始化 | 移除 `= null`（為 default value）並改宣告為 `?` 型別 |
| `ClientInfo.cs` | `Arguments`, `UIViewService`, `UserInfo` 等 nullable 屬性 | 標 `?` |
| `VersionInfo.cs` | `InformationalVersion` 回傳可能為 null | 已用 `?.` 與 `??`，補 `?` 標註即可 |
| 其他 | 視 build 後 warning 補齊 | — |

### 4. 啟用 Implicit Usings 後的 using 清理

移除以下隱含 using：
- `using System;`
- `using System.Collections.Generic;`
- `using System.IO;`
- `using System.Reflection;`（不在 implicit set，保留）
- `using System.Diagnostics;`（不在 implicit set，保留）

> .NET SDK 預設的 implicit usings 包含 `System`、`System.Collections.Generic`、`System.IO`、`System.Linq`、`System.Net.Http`、`System.Threading`、`System.Threading.Tasks` 等。

### 5. XML 文件註解中譯英

依 code-style.md：「XML 文件使用**英文**撰寫」。需翻譯所有 `/// <summary>` 與 `<param>` 描述（共約 35 處）。

範例對照：
```csharp
// 改前
/// <summary>
/// 用戶端資訊。
/// </summary>

// 改後
/// <summary>
/// Provides client-side connection state and access to API connectors.
/// </summary>
```

> 翻譯時保留原意，不擴寫；對於外部使用者真正需要知道的前置條件、警告，移到 `<remarks>`。

### 6. 移除冗餘 in-body 註解

依 code-style.md：「預設不寫註解 — 命名清楚的識別符已能說明 WHAT」。

主要清理對象 `ClientInfo.cs`：

| 行（原） | 註解內容 | 處置 |
|---|---|---|
| 84 | `// 重設 AccessToken 時，需重置 SystemConnector 及 DefineAccess` | **保留**（解釋 setter 副作用，加 `// NOTE:` 前綴） |
| 59 | `// 取得執行檔名稱（不含副檔名）` | 移除（程式碼已自明） |
| 172、180 | `// 設定近端連線相關屬性` / `// 設定遠端連線相關屬性` | 移除 |
| 184 | `// 設定存取權杖令牌為空，因為連線方式變更後需要重新登入` | **保留**（解釋 WHY，加 `// NOTE:` 前綴） |
| 194、201 | `// 判斷服務端點位置為本地路徑或網址，傳回對應的連線方式` | 簡化或移除 |
| 199、203 | `// 設置連線方式` / `// 儲存服務端點` | 移除 |
| 223、225、227、229、231 | `// 取得目前設置服務端點` 等 | 移除 |
| 236 | `// 若連線初始化失敗，傳回 false，要求用戶重新設定` | 移除（catch all 已隱含） |
| 249、252、255 | `// 取得命令列引數` 等 | 移除 |
| 257、259、263 | `// 初始化連線設置` 等 | 移除 |
| 277 | `// 近端連線時，模擬伺服端的初始化` | 移除（與方法名重複） |
| 298 | `// TODO: 未來如有其他登入後需設定的屬性...` | **保留**（明確標 TODO） |
| 312 | `//RepositoryInfo.Initialize(settings.BackendConfiguration);` | **移除**（S125：絕不註解掉舊程式碼） |

`EndpointStorage.cs`、`VersionInfo.cs`、介面檔不需此項處理（無冗餘 in-body 註解）。

### 7. 雜項清理

- `EndpointStorage.cs` 第 37–38 行多餘空白行 → 移除
- `IEndpointStorage.cs` 第 25 行多餘空白行 → 移除
- 其他 trailing whitespace、檔尾無 newline 等小問題

## 公開 API 影響

**有變更：**
- 命名空間維持 `Bee.UI.Core`（不變）→ **公開 API 完全相容**
- 檔案路徑改變（影響 source link，但 NuGet 套件不受影響）

**無變更：**
- 所有 `public` / `private` member 簽名不動
- `ClientInfo.ApplyLoginResult(LoginResponse)` 維持
- 所有 enum / struct / interface 不動

## 驗證方式

1. **編譯驗證**：`dotnet build src/Bee.UI.Core/Bee.UI.Core.csproj --configuration Release` 0 警告 0 錯誤
2. **NuGet pack 驗證**：`dotnet pack ...` 成功產生 `.nupkg` 與 `.snupkg`
3. **公開 API diff**：透過 `nuget` 對比新舊 `.nupkg` 內 assembly 的 public surface（visual 即可）
4. **git history 保留**：搬檔用 `git mv`，`git log --follow <file>` 仍能追到歷史

## 風險

- **Nullable 啟用後的 cascading warning**：`ClientInfo` 多處 static state，啟用後可能需追加多個 `?` / `!`。預期可在一次 build-fix loop 內收斂
- **XML 註解翻譯走味**：英文不到位的 summary 比繁中模糊更糟。會以「能描述同樣意圖的最短英文」為準，必要時請使用者複審 1–2 處關鍵 API（如 `ClientInfo.Initialize` overloads）

## 執行順序

1. csproj 加入 `Nullable` / `ImplicitUsings` / `LangVersion`
2. 搬檔扁平化（`git mv`）
3. 翻譯 XML 註解
4. 清理 in-body 註解
5. 修正 nullable warning 至 0 警告 0 錯誤
6. 移除 implicit using 已涵蓋的 using
7. `dotnet build` + `dotnet pack` 驗證
8. 文件頂部標記完成狀態

## 預估產出

- 5 個原始碼檔案搬移 + 修改
- 1 個 csproj 修改
- 0 個新檔案
- 1 個計畫文件（本檔，完成後自我標記）
