# 計畫 B：Bee.Api.Core 根層命名空間整理

**狀態：📝 擬定中**

## 背景

`Bee.Api.Core` 根層目前堆積 **11 份檔案**，混合四種職能（訊息／轉換／註冊／配置），與已存在的子命名空間（`Authorization` / `JsonRpc` / `MessagePack` / `System` / `Transformer` / `Validator`）並存，但根層卻未做進一步分類。問題形式類比剛重整過的 `Bee.Db` 根層（`TableSchemaCommandBuilder` / `JoinType` 散落根層）。

根層現狀：

| 類別 | 職能 |
|------|------|
| `ApiHeaders` | 訊息相關常數（HTTP header key） |
| `ApiMessageBase` | 訊息基底抽象類別 |
| `ApiRequest` | 抽象 request |
| `ApiResponse` | 抽象 response |
| `ExecFuncRequest` | 具體 request（執行 function） |
| `ExecFuncResponse` | 具體 response（執行 function） |
| `PayloadFormat` | payload 格式 enum |
| `ApiInputConverter` | API 型別 → BO 參數型別轉換 |
| `ApiOutputConverter` | BO 回傳 → API 型別轉換 |
| `ApiContractRegistry` | Contract 介面 → API 型別註冊 |
| `ApiServiceOptions` | 啟動配置（authorizer / transformer / serializer 選項） |

## 目標

1. 根層只保留**真正不屬於任何子命名空間**的橫切型別（如 `ApiServiceOptions`）
2. 訊息／轉換／註冊三種職能各歸明確子命名空間
3. 所有公開型別**名稱不變**，僅 namespace 改變
4. 不異動套件版本號（沿用 4.0.x）

### 不涵蓋

- 行為變更（純命名空間調整）
- 公開型別改名 / 拆分 / 合併
- 既有子命名空間（`Authorization` / `JsonRpc` / `MessagePack` / `System` / `Transformer` / `Validator`）內檔案調整
- `Bee.Api.Core/System/` 命名爭議（屬另一份 plan E 的範圍）
- 跨 package 邏輯異動，僅同步更新 `using`

## 設計原則

依「**訊息形態 / 行為轉換 / 註冊配置 三層分離**」歸類根層內容：

| 層次 | 命名空間 | 內容 |
|------|----------|------|
| 訊息 | `Bee.Api.Core.Messages`（新增） | API 訊息抽象類別、具體 Request/Response、訊息 header / payload 格式 |
| 轉換 | `Bee.Api.Core.Conversion`（新增） | API 型別 ↔ BO 型別之間的轉換器 |
| 註冊 | `Bee.Api.Core.Registry`（新增） | Contract → API 型別等對應關係的註冊中心 |
| 啟動配置 | `Bee.Api.Core` 根層 | `ApiServiceOptions` 留根層，作為使用者配置入口 |

## 搬移清單

### A. 訊息相關 → `Bee.Api.Core.Messages`（7 份）

新增 `src/Bee.Api.Core/Messages/` 資料夾：

| 來源 | 目標 | 性質 |
|------|------|------|
| `src/Bee.Api.Core/ApiMessageBase.cs` | `Messages/ApiMessageBase.cs` | 抽象基底 |
| `src/Bee.Api.Core/ApiRequest.cs` | `Messages/ApiRequest.cs` | 抽象 request |
| `src/Bee.Api.Core/ApiResponse.cs` | `Messages/ApiResponse.cs` | 抽象 response |
| `src/Bee.Api.Core/ExecFuncRequest.cs` | `Messages/ExecFuncRequest.cs` | 具體 request |
| `src/Bee.Api.Core/ExecFuncResponse.cs` | `Messages/ExecFuncResponse.cs` | 具體 response |
| `src/Bee.Api.Core/ApiHeaders.cs` | `Messages/ApiHeaders.cs` | header key 常數 |
| `src/Bee.Api.Core/PayloadFormat.cs` | `Messages/PayloadFormat.cs` | payload 格式 enum |

### B. 轉換相關 → `Bee.Api.Core.Conversion`（2 份）

新增 `src/Bee.Api.Core/Conversion/` 資料夾：

| 來源 | 目標 |
|------|------|
| `src/Bee.Api.Core/ApiInputConverter.cs` | `Conversion/ApiInputConverter.cs` |
| `src/Bee.Api.Core/ApiOutputConverter.cs` | `Conversion/ApiOutputConverter.cs` |

> 與既有 `Transformer/` 子命名空間有什麼不同？`Transformer/` 是 payload 層級的 serializer / compressor / encryptor（運作在 byte[] 層次）；`Conversion/` 則是 .NET 物件模型層的型別轉換（API 型別 ↔ BO 型別）。兩者抽象層次不同，建議保持分開。

### C. 註冊中心 → `Bee.Api.Core.Registry`（1 份）

新增 `src/Bee.Api.Core/Registry/` 資料夾：

| 來源 | 目標 |
|------|------|
| `src/Bee.Api.Core/ApiContractRegistry.cs` | `Registry/ApiContractRegistry.cs` |

> 為什麼開新命名空間給單一檔案？預期未來可能有其他註冊中心（如 transformer registry、custom validator registry），預留分類空間；同時讓「註冊」職能在 namespace 層級就能識別。

### D. 留根層（1 份）

| 檔案 | 留根層理由 |
|------|----------|
| `ApiServiceOptions.cs` | 使用者啟動時配置整個 API service 的入口；放根層讓 `using Bee.Api.Core;` 即可看到 |

### E. 不動的部分

既有子命名空間全部不動：

- `Bee.Api.Core.Authorization` — 授權
- `Bee.Api.Core.JsonRpc` — JSON-RPC 訊息協定
- `Bee.Api.Core.MessagePack` — MessagePack 序列化
- `Bee.Api.Core.System` — 系統級 API 訊息（屬另一份 plan E 範圍）
- `Bee.Api.Core.Transformer` — payload 轉換管線
- `Bee.Api.Core.Validator` — API 存取驗證

## 影響面

- 移動檔案：**10 份**（A=7、B=2、C=1）
- 留根層：**1 份**（`ApiServiceOptions`）
- 跨 package 預估更新 `using` 檔案數：約 30–60 份（待全 repo grep 確認）

### 對外 API breaking change 對應表

| 舊 namespace | 新 namespace | 影響型別 |
|-------------|-------------|---------|
| `Bee.Api.Core` | `Bee.Api.Core.Messages` | `ApiMessageBase`、`ApiRequest`、`ApiResponse`、`ExecFuncRequest`、`ExecFuncResponse`、`ApiHeaders`、`PayloadFormat` |
| `Bee.Api.Core` | `Bee.Api.Core.Conversion` | `ApiInputConverter`、`ApiOutputConverter` |
| `Bee.Api.Core` | `Bee.Api.Core.Registry` | `ApiContractRegistry` |

不升版號（沿用 4.0.x），release notes 列出對應表。

## 執行步驟

1. **A 階段：訊息類搬入 `Messages/`**
   1. 建立 `src/Bee.Api.Core/Messages/` 資料夾
   2. `git mv` 7 份檔案
   3. 改 `namespace Bee.Api.Core` → `namespace Bee.Api.Core.Messages`
   4. 全 repo `grep "ApiRequest\|ApiResponse\|ApiMessageBase\|ExecFuncRequest\|ExecFuncResponse\|ApiHeaders\|PayloadFormat"` 補 `using`
2. **B 階段：轉換類搬入 `Conversion/`**
   1. 建立 `src/Bee.Api.Core/Conversion/` 資料夾
   2. `git mv` 2 份檔案
   3. 改 namespace + 補 using
3. **C 階段：註冊類搬入 `Registry/`**
   1. 建立 `src/Bee.Api.Core/Registry/` 資料夾
   2. `git mv` 1 份檔案
   3. 改 namespace + 補 using
4. **驗證**
   1. `dotnet build --configuration Release` 全綠
   2. `./test.sh` 全綠
5. **文件同步**
   1. 更新 `src/Bee.Api.Core/README.md` 與 `README.zh-TW.md` Directory Structure
   2. 視需要補 ADR 或在 ADR-008 補述

## 風險與權衡

| 風險 | 緩解 |
|------|------|
| `Conversion/` 與既有 `Transformer/` 名稱讀者可能混淆 | README 明確標註：Transformer 處理 byte[] 層級 payload 管線，Conversion 處理 .NET 物件模型型別轉換 |
| `Registry/` 只放 1 份檔案，似乎過度切割 | 接受此「單檔命名空間」，預留未來擴充；類比 .NET 中許多只有少數類別的命名空間（如 `System.Buffers`）|
| 訊息類 7 份檔案集中後子目錄變大 | 7 份還在合理範圍；若未來成長可再切（如 Messages.Abstractions / Messages.System） |

### 不採用的替代方案

1. **將訊息類繼續放根層** — 與 plan A（Bee.Definition）採取的「根層只留橫切型別」原則矛盾
2. **將 `ApiInputConverter` / `ApiOutputConverter` 併入 `Transformer/`** — 兩者抽象層次不同，併入會降低 Transformer 的職能聚焦度
3. **將 `ApiContractRegistry` 留根層** — 它本質是註冊中心，應與 `ApiServiceOptions`（使用者配置入口）區分

## 驗收標準

- [ ] `dotnet build --configuration Release` 全綠且無 warning
- [ ] `./test.sh` 全綠
- [ ] `src/Bee.Api.Core/` 根層只剩 `ApiServiceOptions.cs`
- [ ] `Bee.Api.Core.Messages` 命名空間含原 7 份訊息類
- [ ] `Bee.Api.Core.Conversion` 命名空間含原 2 份轉換類
- [ ] `Bee.Api.Core.Registry` 命名空間含 `ApiContractRegistry`
- [ ] 公開型別名稱完全不變
- [ ] `src/Bee.Api.Core/README.md` Directory Structure 與實際對齊
- [ ] 套件版本號未變動（仍為 4.0.x）
- [ ] CI 通過
