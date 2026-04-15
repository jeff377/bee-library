# 計畫：API 合約序列化完整測試

## 目標

補齊 `Bee.Api.Core` 所有 API 合約的序列化測試，確保 MessagePack（二進位傳輸）和 JSON（JSON-RPC 外層協議）兩種序列化路徑都能正確 round-trip。

## 現況分析

### MessagePack 序列化 — 已有測試

| 合約 | 測試位置 |
|------|---------|
| PingRequest / PingResponse | `MessagePackTests.cs` |
| CreateSessionRequest / CreateSessionResponse | `MessagePackTests.cs` |
| GetDefineRequest / GetDefineResponse | `MessagePackTests.cs` |
| ExecFuncRequest / ExecFuncResponse | `MessagePackTests.cs` |
| LoginRequest / LoginResponse | `ApiRequestResponseTests.cs` |
| CheckPackageUpdateRequest / Response | `MessagePackContractsTests.cs` |
| GetPackageRequest / Response | `MessagePackContractsTests.cs` |

### MessagePack 序列化 — 缺少測試

| 合約 | 說明 |
|------|------|
| GetCommonConfigurationRequest | 無額外 Key 屬性（僅繼承 Parameters） |
| GetCommonConfigurationResponse | Key(100) CommonConfiguration |
| SaveDefineRequest | Key(100) DefineType, Key(101) Xml, Key(102) Keys |
| SaveDefineResponse | 無額外 Key 屬性（僅繼承 Parameters） |

### JSON 序列化 (Newtonsoft.Json) — 完全無測試

| 合約 | 說明 |
|------|------|
| JsonRpcRequest | 外層請求封裝，含 jsonrpc、method、params、id |
| JsonRpcResponse | 外層回應封裝，含 jsonrpc、method、result、error、id |
| JsonRpcError | 錯誤物件，含 code、message、data |
| ApiPayload (JsonRpcParams / JsonRpcResult) | 承載 Payload 的容器，含 format、value、type |

## 實作計畫

### 任務 1：補齊 MessagePack 缺少的合約測試

**檔案**：`tests/Bee.Api.Core.UnitTests/MessagePackTests.cs`（新增測試方法）

新增以下測試：

1. **`GetCommonConfiguration_Serialize`** — 測試 GetCommonConfigurationRequest（僅 Parameters）和 GetCommonConfigurationResponse（CommonConfiguration 屬性）的 round-trip
2. **`SaveDefine_Serialize`** — 測試 SaveDefineRequest（DefineType + Xml + Keys）和 SaveDefineResponse（僅 Parameters）的 round-trip

沿用現有 `TestFunc.TestMessagePackSerialization<T>()` 模式。

### 任務 2：新增 JSON-RPC 序列化測試

**檔案**：新增 `tests/Bee.Api.Core.UnitTests/JsonRpcSerializationTests.cs`

使用 `Newtonsoft.Json.JsonConvert` 測試 JSON round-trip，涵蓋：

1. **`JsonRpcRequest_Serialize_PreservesAllProperties`**
   - 驗證 jsonrpc、method、id 正確序列化
   - 驗證 params（JsonRpcParams）以 `format`、`value`、`type` 鍵序列化
   - 驗證 `[JsonIgnore]` 的 SerializeState 不會出現在 JSON 中

2. **`JsonRpcResponse_Success_Serialize`**
   - 驗證成功回應：result 存在、error 為 null
   - 驗證 jsonrpc、method、id 正確 round-trip

3. **`JsonRpcResponse_Error_Serialize`**
   - 驗證錯誤回應：error 存在含 code/message/data、result 為 null
   - 驗證 JsonRpcErrorCode 數值正確（如 -32700、-32600）

4. **`JsonRpcError_Serialize_PreservesAllProperties`**
   - code、message、data 三個欄位 round-trip

5. **`ApiPayload_Serialize_PreservesFormatAndTypeName`**
   - 使用 JsonRpcParams / JsonRpcResult 測試 format enum 值、value、type 屬性
   - 驗證 PayloadFormat enum 序列化為數值（非字串）

6. **`JsonRpcRequest_FromJsonString_Deserialize`**
   - 模擬前端送出的 JSON 字串，驗證能正確反序列化為 JsonRpcRequest
   - 這是最關鍵的測試：確保前端 JSON 格式與後端 C# 模型相容

7. **`JsonRpcResponse_ToJsonString_MatchesExpectedFormat`**
   - 將 JsonRpcResponse 序列化為 JSON 字串
   - 驗證 JSON key 名稱符合 JSON-RPC 2.0 規範（小寫 `jsonrpc`、`method`、`result`、`error`、`id`）

### 任務 3：驗證

- 執行 `dotnet test tests/Bee.Api.Core.UnitTests/` 確認所有新舊測試通過

## 不在範圍內

- API Payload 加密/解密管線（已有 `AesPayloadEncryptorTests.cs` 和 `ApiPayloadConverterTests.cs` 覆蓋）
- DataTable/DataSet 自訂 Formatter（已有完整測試）
- SafeTypelessFormatter 型別白名單（已有完整測試）
