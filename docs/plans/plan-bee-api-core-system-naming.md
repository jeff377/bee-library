# 計畫 E：Bee.Api.Core/System 命名空間語意精準化

**狀態：📝 擬定中**

## 背景

`src/Bee.Api.Core/System/` 目前收納 **16 份檔案**，全部是系統級 API 的 Request/Response DTO（Login、Ping、CreateSession、GetDefine、SaveDefine、CheckPackageUpdate、GetPackage、GetCommonConfiguration 各一對）。

namespace 為 `Bee.Api.Core.System`。

## 問題分析

### 問題 1：`System` 字面與 BCL `System.*` 重疊

`System` 是 .NET BCL 的根命名空間。當開發者輸入 `System.` 觸發 IntelliSense 時，會看到 `Bee.Api.Core.System.LoginRequest` 等型別出現，造成噪音與認知負擔。

### 問題 2：`System` 字面語意模糊

`System` 字面有多種解讀：
- 系統級操作（OS、底層基礎設施）
- 系統內建（與 user-defined 對立）
- 系統 API（與 module-specific API 對立 — 本專案的實際意圖）

讀者無法從命名空間直接判斷其本質是「DTO」還是「邏輯」。

### 問題 3：與 `Bee.Api.Contracts` 的關係不清

`Bee.Api.Contracts` 提供契約介面（如 `ILoginRequest`、`ILoginResponse`），而 `Bee.Api.Core.System` 提供具體實作型別（如 `LoginRequest`、`LoginResponse`，含 `[MessagePackObject]` / `[Serializable]` 標註）。兩者是「介面 / 具體訊息」的對應關係，但命名空間風格不一致：
- `Contracts` 直接表達「這裡放契約」
- `System` 沒有直接表達「這裡放具體訊息」

## 目標

1. 命名空間直接表達「**API 訊息實作**」職能
2. 避免與 BCL `System.*` 字面衝突
3. 與 plan B 提案的 `Bee.Api.Core.Messages` 風格一致
4. 不異動套件版本號（沿用 4.0.x）

### 不涵蓋

- 行為變更
- DTO 結構變動
- 與 `Bee.Api.Contracts` 之間的契約結構調整

## 三種選項

### 選項 1：與 plan B 整併入 `Bee.Api.Core.Messages`

將 `System/` 整批搬入 `Messages/`，所有 16 份系統 API 訊息與 plan B 提案的 7 份基底訊息共處 `Bee.Api.Core.Messages`。

**結構**：
```
Bee.Api.Core/Messages/
  ApiMessageBase.cs / ApiRequest.cs / ApiResponse.cs (來自 plan B)
  ApiHeaders.cs / PayloadFormat.cs (來自 plan B)
  ExecFuncRequest.cs / ExecFuncResponse.cs (來自 plan B)
  LoginRequest.cs / LoginResponse.cs / Ping*.cs / CreateSession*.cs ...（16 份系統訊息）
```

- **優點**：所有 API 訊息類型集中
- **缺點**：合併後 23 份檔案在同一資料夾，較雜；基底（`ApiMessageBase`）與具體訊息（`LoginRequest`）混置

### 選項 2：與 plan B 並存，建立 `Bee.Api.Core.Messages.System` 子命名空間

保留 plan B 的 `Bee.Api.Core.Messages`（放基底與通用訊息），系統訊息歸 `Bee.Api.Core.Messages.System`。

**結構**：
```
Bee.Api.Core/Messages/
  ApiMessageBase.cs / ApiRequest.cs / ApiResponse.cs (基底)
  ApiHeaders.cs / PayloadFormat.cs (通用)
  ExecFuncRequest.cs / ExecFuncResponse.cs (通用)
  System/
    LoginRequest.cs / LoginResponse.cs / ...（16 份系統訊息）
```

- **優點**：基底與具體訊息分層；System 字面留在第二級命名空間，避開 BCL 衝突的觀感較輕
- **缺點**：仍叫 `System`，問題 2 未完全解決

### 選項 3：改名 `System` → `SystemApi` 或 `SystemMessages`

放在 `Bee.Api.Core.SystemApi`（或 `Bee.Api.Core.Messages.SystemApi` 若採選項 2 結構）。

**優點**：完全避開 `System` 字面衝突
**缺點**：稍冗長；需要說服自己 `SystemApi` 比 `System` 好

### 選項 4：完全不動

接受目前 `Bee.Api.Core.System` 的命名。

**理由**：BCL 衝突的實際影響很小（IDE 通常依完整路徑解析）；既有外部使用者已習慣。

## 推薦方案

**選項 2（與 plan B 並存，建立 `Bee.Api.Core.Messages.System`）**。

理由：
1. 與 plan B 的 `Bee.Api.Core.Messages` 形成自然層次：`Messages` 放基底與通用、`Messages.System` 放具體系統訊息
2. 16 份系統訊息獨立成子命名空間，將來新增其他類別 API 訊息（例如表單操作）可直接加 `Messages.Forms`
3. 不必選 `SystemApi` 這類冗長名（選項 3）
4. `System` 字面在第二級而非頂層，與 BCL 衝突的觀感較輕
5. 結構與 plan B 的 `Messages` 內容自然搭配

不推薦**選項 1**：合併後 23 份雜放降低可讀性。

不推薦**選項 4**：不動就無法統一 plan B 的 `Messages` 命名風格。

不推薦**選項 3** 改名（`SystemApi` / `SystemMessages`）：較冗長且不對稱（plan B 的 `Messages` 已經暗示 messages，再叫 `SystemMessages` 重複）。

## 搬移清單（若採選項 2）

> 前置條件：plan B 已執行完畢（`Bee.Api.Core.Messages` 子命名空間已建立）。

新增 `src/Bee.Api.Core/Messages/System/` 資料夾，將既有 `System/` 整批搬入：

| 來源 | 目標 |
|------|------|
| `src/Bee.Api.Core/System/LoginRequest.cs` | `src/Bee.Api.Core/Messages/System/LoginRequest.cs` |
| `src/Bee.Api.Core/System/LoginResponse.cs` | `src/Bee.Api.Core/Messages/System/LoginResponse.cs` |
| `src/Bee.Api.Core/System/PingRequest.cs` | `src/Bee.Api.Core/Messages/System/PingRequest.cs` |
| `src/Bee.Api.Core/System/PingResponse.cs` | `src/Bee.Api.Core/Messages/System/PingResponse.cs` |
| `src/Bee.Api.Core/System/CreateSessionRequest.cs` | `src/Bee.Api.Core/Messages/System/CreateSessionRequest.cs` |
| `src/Bee.Api.Core/System/CreateSessionResponse.cs` | `src/Bee.Api.Core/Messages/System/CreateSessionResponse.cs` |
| `src/Bee.Api.Core/System/GetDefineRequest.cs` | `src/Bee.Api.Core/Messages/System/GetDefineRequest.cs` |
| `src/Bee.Api.Core/System/GetDefineResponse.cs` | `src/Bee.Api.Core/Messages/System/GetDefineResponse.cs` |
| `src/Bee.Api.Core/System/SaveDefineRequest.cs` | `src/Bee.Api.Core/Messages/System/SaveDefineRequest.cs` |
| `src/Bee.Api.Core/System/SaveDefineResponse.cs` | `src/Bee.Api.Core/Messages/System/SaveDefineResponse.cs` |
| `src/Bee.Api.Core/System/CheckPackageUpdateRequest.cs` | `src/Bee.Api.Core/Messages/System/CheckPackageUpdateRequest.cs` |
| `src/Bee.Api.Core/System/CheckPackageUpdateResponse.cs` | `src/Bee.Api.Core/Messages/System/CheckPackageUpdateResponse.cs` |
| `src/Bee.Api.Core/System/GetPackageRequest.cs` | `src/Bee.Api.Core/Messages/System/GetPackageRequest.cs` |
| `src/Bee.Api.Core/System/GetPackageResponse.cs` | `src/Bee.Api.Core/Messages/System/GetPackageResponse.cs` |
| `src/Bee.Api.Core/System/GetCommonConfigurationRequest.cs` | `src/Bee.Api.Core/Messages/System/GetCommonConfigurationRequest.cs` |
| `src/Bee.Api.Core/System/GetCommonConfigurationResponse.cs` | `src/Bee.Api.Core/Messages/System/GetCommonConfigurationResponse.cs` |

每份檔案內 `namespace Bee.Api.Core.System` → `namespace Bee.Api.Core.Messages.System`。

## 影響面

- 移動檔案：16 份
- 跨 package 預估更新 `using` 檔案數：30–80 份（待 grep 確認 `using Bee.Api.Core.System;`）

### 對外 API breaking change 對應表

| 舊 namespace | 新 namespace |
|-------------|-------------|
| `Bee.Api.Core.System` | `Bee.Api.Core.Messages.System` |

不升版號，release notes 列出對應。

## 執行步驟

1. **前置依賴：plan B 已完成**
2. **搬遷**
   1. 建立 `src/Bee.Api.Core/Messages/System/` 資料夾
   2. `git mv src/Bee.Api.Core/System/*.cs src/Bee.Api.Core/Messages/System/`
   3. 全 repo `sed` 更新 namespace 與 using
3. **驗證**
   1. `dotnet build --configuration Release` 全綠
   2. `./test.sh` 全綠
4. **文件同步**
   1. 更新 `src/Bee.Api.Core/README.md` 與 `README.zh-TW.md` Directory Structure（與 plan B 一併）

## 風險與權衡

| 風險 | 緩解 |
|------|------|
| 命名空間從兩段（`Bee.Api.Core.System`）變三段（`Bee.Api.Core.Messages.System`），比較長 | 可由 `using` 縮短；命名空間清晰度勝過字面長度 |
| 若不執行 plan B，本 plan 無法獨立採用選項 2 | 接受此前置依賴；若選擇單獨做本 plan，請改採選項 3（改名為 `SystemApi`） |

### 不採用的替代方案

1. **選項 1（合併）**：23 份檔案雜放降低可讀性
2. **選項 3（改名 `SystemApi`）**：較冗長；plan B 已用 `Messages` 表達訊息語意，再加 `Api` / `Messages` 前綴重複
3. **選項 4（不動）**：未統一風格，BCL 衝突問題持續

## 驗收標準

- [ ] `dotnet build --configuration Release` 全綠且無 warning
- [ ] `./test.sh` 全綠
- [ ] `src/Bee.Api.Core/System/` 已不存在
- [ ] `src/Bee.Api.Core/Messages/System/` 含 16 份系統訊息檔案
- [ ] 全 repo 不再出現 `Bee.Api.Core.System` namespace
- [ ] 公開型別名稱完全不變
- [ ] 套件版本號未變動（仍為 4.0.x）
- [ ] CI 通過

## 待決事項

執行前請確認：
1. 採用哪個選項？（推薦選項 2）
2. 是否與 plan B 合併執行？（強烈建議合併，因為兩者本來就是同一個 `Bee.Api.Core` 重整的兩面）
