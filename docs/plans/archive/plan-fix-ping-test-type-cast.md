# 修正計畫：Ping 測試 InvalidCastException

## 問題描述

`JsonRpcExecutorTests.Ping_ValidRequest_ReturnsOkStatus` 測試失敗，錯誤為：

```
System.InvalidCastException: Unable to cast object of type
'Bee.Business.System.PingResult' to type 'Bee.Api.Core.System.PingResponse'
```

同一根因影響共 6 個測試（A 類 + B 類），本計畫一併修正。

## 根因

`JsonRpcExecutor.ExecuteAsyncCore` 執行 BO 方法後，回傳值（如 `PingResult`）直接放入 `response.Result.Value`，沒有轉換為 API Response 型別（如 `PingResponse`）。

## 命名規則

所有 System Action 的型別命名遵循一致的慣例，可由 Action 名稱推導：

| Action | BO Args (輸入) | BO Result (輸出) | API Request | API Response |
|--------|---------------|-----------------|-------------|--------------|
| `Ping` | `PingArgs` | `PingResult` | `PingRequest` | `PingResponse` |
| `Login` | `LoginArgs` | `LoginResult` | `LoginRequest` | `LoginResponse` |
| ... | `{Action}Args` | `{Action}Result` | `{Action}Request` | `{Action}Response` |

輸入端已有 `ApiInputConverter` 處理 `{Action}Request → {Action}Args` 轉換。
輸出端缺少對應機制——本次修正補上。

---

## 修正方案：反射自動推導

利用命名慣例 `{Action}Result → {Action}Response`，在 executor 中透過反射自動找到對應的 API Response 型別並轉換，不需要手動註冊映射。

### 步驟 1：新增 `ApiOutputConverter`

**檔案**：`src/Bee.Api.Core/ApiOutputConverter.cs`（新增）

與 `ApiInputConverter` 對稱，負責將 BO Result 轉換為 API Response：

```csharp
/// <summary>
/// Converts BO result objects to API response types by naming convention.
/// When the Executor receives a BO result (e.g., PingResult), this converter
/// resolves the corresponding API response type (e.g., PingResponse) via reflection
/// and copies matching properties.
/// </summary>
public static class ApiOutputConverter
{
    // Cache: BO Result Type → API Response Type (null if no match)
    private static readonly ConcurrentDictionary<Type, Type> _cache = new();

    /// <summary>
    /// Converts a BO result to the corresponding API response type.
    /// Returns the original value if no matching API response type is found.
    /// </summary>
    public static object Convert(object boResult)
    {
        if (boResult == null) return null;

        var boType = boResult.GetType();
        var responseType = _cache.GetOrAdd(boType, ResolveResponseType);

        if (responseType == null) return boResult;

        return ApiInputConverter.Convert(boResult, responseType);
    }

    /// <summary>
    /// Resolves the API response type from a BO result type using naming convention.
    /// e.g., PingResult → PingResponse (in Bee.Api.Core assembly).
    /// </summary>
    private static Type ResolveResponseType(Type boType)
    {
        const string resultSuffix = "Result";
        const string responseSuffix = "Response";

        if (!boType.Name.EndsWith(resultSuffix))
            return null;

        var responseName = boType.Name[..^resultSuffix.Length] + responseSuffix;

        // Search in the Bee.Api.Core assembly (where API response types live)
        var apiCoreAssembly = typeof(ApiOutputConverter).Assembly;
        return apiCoreAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == responseName && !t.IsAbstract);
    }
}
```

**設計要點**：
- 使用 `ConcurrentDictionary` 快取映射結果，每個 BO 型別只做一次反射掃描
- 複用既有 `ApiInputConverter.Convert` 做屬性複製
- 找不到對應型別時回傳原值（向後相容）

### 步驟 2：在 `JsonRpcExecutor.ExecuteAsyncCore` 呼叫轉換

**檔案**：`src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`

在第 76–79 行之間，取得 BO 回傳值後、放入 response 前，加入轉換：

```csharp
// 現有程式碼
var value = await ExecuteMethodAsync(progId, action, request.Params.Value, format);

// ★ 新增：將 BO Result 轉換為 API Response（依命名慣例自動推導）
value = ApiOutputConverter.Convert(value);

// 現有程式碼
response.Result = new JsonRpcResult { Value = value };
```

### 步驟 3：驗證測試

執行受影響的測試：

```bash
# 單一測試
dotnet test tests/Bee.Api.Core.UnitTests/ --configuration Release --settings .runsettings --filter "Ping_ValidRequest_ReturnsOkStatus"

# 全部測試
dotnet test --configuration Release --settings .runsettings
```

---

## 文件更新

### 步驟 4：新增 ADR

**檔案**：`docs/adr/adr-007-convention-based-type-resolution.md`（新增）

記錄「以命名慣例自動推導 API 型別」的架構決策：

- **背景**：原設計需手動呼叫 `ApiContractRegistry.Register`，實務上容易遺漏且新增 Action 時步驟繁瑣
- **決策**：改用反射 + 命名慣例自動推導，`{Action}Result` → `{Action}Response`
- **命名規則**：`{Action}Args`、`{Action}Result`（BO 層）/ `{Action}Request`、`{Action}Response`（API 層）為強制命名慣例
- **取捨**：首次呼叫有反射成本，但透過快取消除後續影響；偏離命名慣例的型別將無法自動轉換
- **影響**：新增 Action 不再需要手動註冊，`ApiContractRegistry` 仍保留供 Encoded/Encrypted 格式使用

### 步驟 5：更新公開文件

以下文件中提及「手動註冊 `ApiContractRegistry`」的段落需同步更新，說明 Plain 格式已改為自動推導：

| 檔案 | 需修改的段落 |
|------|-------------|
| `docs/api-bo-contract-design.md` | §Response Mapping（移除手動 Register 範例）、§Steps to Add a New API Method 第 4 步 |
| `docs/api-bo-contract-design.zh-TW.md` | 同上（雙語同步） |
| `docs/development-cookbook.md` | §關鍵元件（加入 `ApiOutputConverter` 說明，更新流程圖） |
| `docs/development-constraints.md` | §API 契約註冊（更新為自動推導，保留命名慣例為強制規則） |

**修改重點**：
- 「新增 API Method」步驟中移除「註冊 `ApiContractRegistry`」
- 新增命名慣例為**強制規範**的說明：不符合 `{Action}Result` / `{Action}Response` 命名的型別將無法自動轉換
- 流程圖中 `ApiContractRegistry 型別對應` 改為 `ApiOutputConverter 型別對應（命名慣例自動推導）`

---

## 影響範圍總覽

| 檔案 | 變更類型 |
|------|----------|
| `src/Bee.Api.Core/ApiOutputConverter.cs` | **新增** — BO Result → API Response 反射轉換器 |
| `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` | 新增 1 行 `ApiOutputConverter.Convert` 呼叫 |
| `docs/adr/adr-007-convention-based-type-resolution.md` | **新增** — 架構決策紀錄 |
| `docs/api-bo-contract-design.md` | 更新 Response Mapping 及新增方法步驟 |
| `docs/api-bo-contract-design.zh-TW.md` | 同上（雙語同步） |
| `docs/development-cookbook.md` | 更新關鍵元件及流程圖 |
| `docs/development-constraints.md` | 更新契約註冊規則 |

## 不在此次範圍

- **C 類測試**（#7 Login 帳號不存在、#8 localhost 未啟動）：屬於測試環境議題
- **`ApiContractRegistry`**：既有的 Encoded/Encrypted 格式轉換路徑維持不動
