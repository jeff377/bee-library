# 計畫 C：Bee.Base/ApiException 分層污染處理

**狀態：✅ 已完成（2026-04-28）— 採選項 3（搬到 Bee.Api.Core.Messages 並改名 ApiErrorInfo）**

## 背景

`Bee.Base/ApiException.cs`（namespace `Bee.Base`）有兩個問題：

### 問題 1：分層污染

`Bee.Base` 是相依性最底層（其他所有 Bee.* 都引用它）。底層出現「Api」字樣的型別，違反「底層不該知道上層概念」的分層原則。

### 問題 2：名稱與本質不符

實際上 `ApiException` **不是**例外類別 — 它不繼承 `Exception`，而是一個 DTO（實作 `IObjectSerializeBase`），用來在 JSON-RPC 邊界承載錯誤資訊。檔案中已有 `[SuppressMessage("S2166")]` 註記，明確承認此命名問題：

> ApiException is a serializable DTO carrying API error info across the JSON-RPC boundary, not a thrown exception. Renaming would break the published 4.x public API surface.

亦即過去刻意為了避免 breaking change 而保留了不恰當的命名。

### 使用統計

全 repo 引用 `ApiException`：**僅 2 個檔案**（`src/Bee.Base/ApiException.cs` 自身 + `tests/Bee.Base.UnitTests/MiscTypesTests.cs`）。亦即 internal 使用幾乎為零；風險來自**外部 NuGet consumer** 是否有使用。

## 目標

1. 解決命名與本質不符的問題（DTO 卻取名 Exception）
2. 解決分層污染（Api 字樣不該出現在 Base 層）
3. 不異動套件版本號（沿用 4.0.x）

### 不涵蓋

- 行為變更（純命名空間 / 類別名調整）
- 新增功能或修改 DTO 結構

## 三種選項分析

### 選項 1：搬到 `Bee.Api.Core`，名稱不改

- **動作**：將 `ApiException` 移至 `src/Bee.Api.Core/Messages/ApiException.cs`（namespace `Bee.Api.Core.Messages`，與 plan B 一致）
- **解決**：分層污染 ✓
- **未解決**：名稱與本質不符（仍叫 Exception 但其實是 DTO）✗
- **breaking change**：是（namespace 改變）

### 選項 2：留 `Bee.Base`，但改名為 `ApiErrorInfo` / `ApiErrorPayload`

- **動作**：類別改名（保留同 namespace）
- **解決**：名稱與本質不符 ✓
- **未解決**：分層污染（仍在 Bee.Base）✗
- **breaking change**：是（型別名變）

### 選項 3：搬到 `Bee.Api.Core` 並改名（推薦）

- **動作**：移至 `src/Bee.Api.Core/Messages/ApiErrorInfo.cs`（namespace `Bee.Api.Core.Messages`）+ 類別名改為 `ApiErrorInfo`
- **解決**：分層污染 ✓ + 名稱與本質不符 ✓
- **breaking change**：是（namespace + 名稱都變）
- **理由**：既然要做 breaking change，一次性把兩個問題都解掉，避免日後再做一次

### 選項 4：維持現狀

- **解決**：什麼都不解決
- **breaking change**：無
- **理由**：若擔心外部 consumer 影響，繼續忍受目前命名

## 推薦方案

**選項 3**（搬到 `Bee.Api.Core.Messages` 並改名為 `ApiErrorInfo`）。

理由：
1. 內部使用近乎為零（只有 1 個測試檔），影響面極小
2. `[SuppressMessage]` 註記已承認名稱問題；既然要修，一次到位
3. 與 plan B 同步：本變更可併入 plan B 一起做（plan B 同樣建立 `Bee.Api.Core.Messages`）
4. 4.x 已陸續累積多個命名空間 breaking change（plan A、B、剛完成的 Bee.Db 重整），release notes 統一說明即可

若使用者偏好較保守，**選項 1**（只搬不改名）也可接受，仍解決分層污染。

## 搬移清單（若採選項 3）

| 來源 | 目標 |
|------|------|
| `src/Bee.Base/ApiException.cs`（class `ApiException`） | `src/Bee.Api.Core/Messages/ApiErrorInfo.cs`（class `ApiErrorInfo`） |

更新內容：
- namespace `Bee.Base` → `Bee.Api.Core.Messages`
- class name `ApiException` → `ApiErrorInfo`
- 移除 `[SuppressMessage("S2166")]`（不再需要）
- 移除原檔案

更新引用方：
- `tests/Bee.Base.UnitTests/MiscTypesTests.cs` — 改 `using` 與類別名

## 影響面

- 移動／重命名檔案：1 份（+1 份測試引用更新）
- 對外 NuGet consumer：是 breaking change（命名空間 + 型別名雙改）

### 對外 API breaking change 對應表

| 舊 | 新 |
|----|-----|
| `Bee.Base.ApiException` | `Bee.Api.Core.Messages.ApiErrorInfo` |

不升版號，release notes 列出對應表並建議使用者全文取代 `using Bee.Base;` 中對 `ApiException` 的引用。

## 執行步驟

1. **前置依賴：plan B 已完成**（建立 `Bee.Api.Core.Messages` 子命名空間）
2. **搬遷與重命名**
   1. 建立 `src/Bee.Api.Core/Messages/ApiErrorInfo.cs`，copy 原檔內容
   2. 改 namespace 與 class name
   3. 移除 `[SuppressMessage("S2166")]`（DTO 本就不該被視為 Exception）
   4. 刪除 `src/Bee.Base/ApiException.cs`
3. **更新引用方**
   1. `tests/Bee.Base.UnitTests/MiscTypesTests.cs`：`using` 改為 `Bee.Api.Core.Messages`，型別名改 `ApiErrorInfo`
   2. 全 repo grep `ApiException\b` 確認無遺漏（NuGet consumer 不在此範圍）
4. **驗證**
   1. `dotnet build --configuration Release` 全綠
   2. `./test.sh` 全綠
5. **文件同步**
   1. 更新 `src/Bee.Base/README.md` 與 `README.zh-TW.md`：移除 `ApiException` 的提及
   2. 更新 `src/Bee.Api.Core/README.md` 與 `README.zh-TW.md`：列入 `ApiErrorInfo`
   3. ADR-008 補述：「型別名應與本質一致；DTO 不該命名為 *Exception」

## 風險與權衡

| 風險 | 緩解 |
|------|------|
| 外部 NuGet consumer 可能仍使用 `Bee.Base.ApiException` | release notes 明列雙改對應表；4.x 已經陸續做了多個命名空間調整，使用者已習慣這類更新 |
| 改名後丟失「這是用於錯誤回傳的 DTO」的語意（Exception 字樣本身有提示作用） | 新名 `ApiErrorInfo` 直接表達 DTO 本質，讀者不會誤以為是可拋出的例外 |
| `[SuppressMessage]` 註記消失後若有人重新加入類似命名，又會踩 S2166 | 移除舊類別本身就移除了 attribute，不需擔心 |

### 不採用的替代方案

1. **選項 4（維持現狀）**：兩個問題都不解，違反此次 review 初衷
2. **選項 2（只改名不搬）**：不解分層污染，且 `Bee.Base` 出現 `ApiErrorInfo` 字樣依然奇怪（Api 字樣留底層）

## 驗收標準

- [ ] `dotnet build --configuration Release` 全綠且無 warning
- [ ] `./test.sh` 全綠
- [ ] `src/Bee.Base/` 不再含 `ApiException.cs`
- [ ] `src/Bee.Api.Core/Messages/ApiErrorInfo.cs` 存在且為 `Bee.Api.Core.Messages` namespace
- [ ] 全 repo 不再出現 `Bee.Base.ApiException` 或 `class ApiException`
- [ ] `src/Bee.Base/README*.md` 與 `src/Bee.Api.Core/README*.md` 更新完成
- [ ] 套件版本號未變動（仍為 4.0.x）
- [ ] CI 通過

## 待決事項

執行前請確認：
1. 採用哪個選項？（推薦選項 3）
2. 是否與 plan B 合併執行？（推薦合併，因為 plan B 已建立 `Bee.Api.Core.Messages` 子命名空間）
