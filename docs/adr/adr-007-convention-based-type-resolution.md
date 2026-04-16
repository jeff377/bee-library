# ADR-007：以命名慣例自動推導 API 型別

## 狀態

已採納（2026-04-16）

## 背景

框架採用 API/BO 兩層分離的型別設計（見 [API 合約與 BO 參數設計原則](../api-bo-contract-design.md)）：

- **BO 層**：`{Action}Args` / `{Action}Result`（純 POCO）
- **API 層**：`{Action}Request` / `{Action}Response`（含 MessagePack 序列化屬性）

`JsonRpcExecutor` 執行 BO 方法後，必須把 BO 回傳的 `{Action}Result` 轉成對應的 API `{Action}Response`，用戶端才能正確反序列化。

### 原本的做法

原先透過 `ApiContractRegistry.Register<TContract, TApi>()` 靜態註冊 Contract 介面 → API 型別的對應：

```csharp
// 必須在應用程式啟動時針對每一個 API 方法手動呼叫
ApiContractRegistry.Register<ILoginResponse, LoginResponse>();
ApiContractRegistry.Register<IPingResponse, PingResponse>();
// ...
```

### 問題

1. **容易遺漏**：新增 API 方法時如忘記註冊，BO 回傳值會直接上拋 `InvalidCastException`，錯誤訊息不直接指向根因。
2. **新增步驟繁瑣**：每個新 Action 都需要額外改動註冊碼，違反「命名慣例即契約」的設計哲學。
3. **實際使用狀況**：檢視 repo 內原始碼與啟動流程，並無任何地方呼叫 `ApiContractRegistry.Register`，顯示手動註冊機制在實務上難以維護。

## 決策

改用**反射 + 命名慣例**自動推導 API 回應型別，由 `ApiOutputConverter` 統一處理：

```
BO 回傳：{Action}Result   ──反射搜尋 Bee.Api.Core 組件──▶   API 回應：{Action}Response
```

### 實作要點

- 新增 [`ApiOutputConverter`](../../src/Bee.Api.Core/ApiOutputConverter.cs)，於 `JsonRpcExecutor.ExecuteAsyncCore` 完成 BO 呼叫後立即進行型別轉換
- 反射結果以 `ConcurrentDictionary<Type, Type>` 快取，每個 BO 型別只掃描一次
- 透過 `typeof(void)` 作為 sentinel 表示「找不到對應型別」（因 `ConcurrentDictionary` 不接受 null 值）
- 找不到對應型別時回傳原值，不中斷流程（向後相容）

### 命名慣例（強制規範）

自動推導依賴以下命名慣例，違反者將無法自動轉換：

| 層級 | 輸入 | 輸出 |
|------|------|------|
| BO（`Bee.Business`） | `{Action}Args` | `{Action}Result` |
| API（`Bee.Api.Core`） | `{Action}Request` | `{Action}Response` |
| Contract（`Bee.Api.Contracts`） | `I{Action}Request` | `I{Action}Response` |

例：`PingResult` → `PingResponse`、`LoginResult` → `LoginResponse`。

## 取捨

### 優點

- **零樣板**：新增 API 方法不需動到啟動碼，只要遵守命名即可自動對應
- **錯誤更早浮現**：命名不一致會在第一個測試就明顯地失敗，不會在註冊缺漏時悄悄運行
- **程式碼集中**：型別轉換邏輯集中在 `ApiOutputConverter`，與 `ApiInputConverter`（輸入端）對稱

### 代價

- **首次呼叫有反射成本**：需要 `Assembly.GetTypes()` 掃描一次；透過快取消除後續影響
- **命名偏離無法自動處理**：違反 `{Action}Result` / `{Action}Response` 的型別需要個別處理（目前無例外）
- **跨組件搜尋限制**：目前僅搜尋 `Bee.Api.Core` 組件，若未來 API 型別分散到多組件需擴充搜尋範圍

## 影響

### 程式碼

- **新增**：`src/Bee.Api.Core/ApiOutputConverter.cs`
- **修改**：`src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`（新增 1 行呼叫）
- **修改**：`src/Bee.Api.Core/ApiInputConverter.cs`（補強 `JsonElement` 反序列化路徑）
- **保留**：`ApiContractRegistry`（供 Encoded/Encrypted 格式的 MessagePack 序列化轉換使用）

### 文件

- 本 ADR
- 更新 [API 合約與 BO 參數設計原則](../api-bo-contract-design.md) 移除手動註冊步驟
- 更新 [端到端開發指引](../development-cookbook.md) 說明 `ApiOutputConverter` 的角色
- 更新 [開發限制與反模式](../development-constraints.md) 的 API 契約段落

### 對開發人員的意義

- 新增 API 方法時，只要遵守命名慣例，框架會自動完成型別轉換
- 命名偏離慣例會導致 BO 回傳值直接流到用戶端（可能造成型別錯誤），應在程式碼審查時嚴格檢查
