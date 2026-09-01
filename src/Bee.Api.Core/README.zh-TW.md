# Bee.Api.Core

> 核心 API 框架，處理 JSON-RPC 執行、Payload 加密管線、授權驗證與型別對應。

[English](README.md)

## 架構定位

- **層級**：API 層（核心引擎）
- **下游**（依賴此專案）：`Bee.Api.AspNetCore`、`Bee.Api.Client`
- **上游**（此專案依賴）：`Bee.Api.Contracts`、`Bee.Definition`

## 目標框架

- `net10.0` -- 存取現代執行階段 API 與效能改進

## 主要功能

### JSON-RPC 執行

- `JsonRpcExecutor` -- 解析 `ProgId.Action` 方法識別碼，透過反射建立商業物件並叫用目標方法。
- `JsonRpcRequest` / `JsonRpcResponse` / `JsonRpcError` -- 標準 JSON-RPC 2.0 訊息型別。
- `ApiPayload` / `ApiPayloadConverter` -- JSON-RPC 傳輸的 Payload 封裝與轉換。
- 例外清理 -- 正式環境中隱藏內部錯誤細節，避免資訊洩漏。

### Payload 安全管線

- `ApiPayloadTransformer` -- 協調序列化 -> 壓縮 -> 加密管線（入站則執行反向流程）。
- `IApiPayloadSerializer` / `MessagePackPayloadSerializer` -- 可插拔的 MessagePack 序列化。
- `IApiPayloadCompressor` / `GzipPayloadCompressor` -- 可插拔的 Gzip 壓縮。
- `IApiPayloadEncryptor` / `AesPayloadEncryptor` -- 可插拔的 AES-CBC-HMAC 加密。
- `NoEncryptionEncryptor` -- 僅供測試使用的旁路加密器。
- `ApiPayloadOptionsFactory` -- 依據保護等級建立管線選項。

### 重放防護（選用，預設關閉）

- `ApiPayloadFrame` -- 加密封套內的時間戳與序號，前置於 payload body。
- `ReplayWindow` / `IReplayWindowStore` -- per-session 的序號滑動視窗（64 格位圖，零資料庫往返）。
- `ApiServiceOptions.RequireWireFrame` -- 總開關，**用戶端與伺服器必須設成同一個值**。
- `ApiReplayProtection` -- `ApiAccessControlAttribute` 的第三維度，逐方法宣告是否檢查序號。

僅涵蓋 `Encoded` 與 `Encrypted`。`Plain` 沒有封套，任何防重放欄位都能被改寫，因此不帶 frame；
`Encoded` 帶 frame 但無 HMAC，只擋無腦原樣重送。完整邊界、啟用順序與已知限制見
[ADR-042](../../docs/adr/adr-042-api-replay-protection.md)。

### 授權與存取控制

- `IApiAuthorizationValidator` / `ApiAuthorizationValidator` -- 驗證傳入請求的授權上下文。
- `ApiAuthorizationContext` / `ApiAuthorizationResult` -- 授權輸入與結果型別。
- `ApiAccessValidator` -- 透過 `ApiAccessControlAttribute` 實施方法層級保護。
- `ApiCallContext` -- 擷取每次呼叫的中繼資料（Token、保護等級、呼叫者身分）。

### 型別對應與契約註冊

- `ApiContractRegistry` -- 將契約介面對應至具體的 API 請求/回應型別。
- `ApiInputConverter` -- 將原始 JSON-RPC 參數轉換為強型別請求物件。
- `ApiHeaders` -- API 通訊的標準標頭常數。
- `PayloadFormat` -- 定義保護等級的列舉（`Plain`、`Encoded`、`Encrypted`）。

### MessagePack 基礎設施

> 這些型別為 `internal`。此處記載是因為它們定義了 wire 行為，但它們**不屬於**套件的公開表面
> ——要走同一條管線請用 public 的 `MessagePackPayloadSerializer`。

- `SafeMessagePackSerializerOptions` -- 反序列化型別白名單，防止不受信任型別攻擊。
- `MessagePackCodec` -- MessagePack 序列化的編解碼器。
- `WireContracts` -- 全部 wire 型別的顯式 formatter 註冊。contractless resolver 只是桌面端的
  便利退路、不是承載機制：.NET for iOS 關閉動態碼，未註冊的型別在那裡直接失敗
  （見 [ADR-037](../../docs/adr/adr-037-wire-explicit-registration.md)）。
- `WireValueFormatter` -- `object` 型別成員（條件值、參數值、資料表儲存格）的判別式封套。

### 內建系統操作

- 內建 `Login`、`Ping`、`CreateSession`、`GetDefine`、`SaveDefine`、`ExecFunc` 等系統層級操作的請求/回應型別。

## 主要公開 API

| 類別 / 介面 | 用途 |
|-------------|------|
| `JsonRpcExecutor` | 解析 `ProgId.Action`、建立 BO、叫用方法 |
| `ApiServiceOptions` | 可插拔元件的靜態 DI 註冊 |
| `ApiPayloadTransformer` | 序列化 -> 壓縮 -> 加密管線 |
| `ApiAccessValidator` | 透過 `ApiAccessControlAttribute` 實施方法層級保護 |
| `ApiContractRegistry` | 將契約介面對應至 API 型別 |
| `PayloadFormat` | 保護等級列舉（`Plain`、`Encoded`、`Encrypted`） |
| `ApiAuthorizationValidator` | 請求授權驗證 |
| `ApiCallContext` | 每次呼叫的中繼資料（Token、保護等級、身分） |
| `ApiPayloadOptionsFactory` | 依據保護等級建立管線選項 |

## 設計慣例

- **策略模式** -- 序列化器、壓縮器與加密器透過介面（`IApiPayloadSerializer`、`IApiPayloadCompressor`、`IApiPayloadEncryptor`）注入，各階段可獨立替換。
- **嚴格管線順序** -- Payload 轉換器在出站時強制執行序列化 -> 壓縮 -> 加密，入站時則為解密 -> 解壓縮 -> 反序列化，順序不可變更。
- **型別白名單** -- `SafeMessagePackSerializerOptions` 將可反序列化的型別限制為明確的允許清單，防止反序列化攻擊。
- **反射式分派** -- `JsonRpcExecutor` 以名稱解析並叫用商業物件方法，將傳輸層與具體 BO 型別解耦。
- **例外清理** -- 非開發環境中，內部例外細節會從回應中移除，避免資訊洩漏。
- **三層保護等級** -- `Public`（無需驗證）、`Encoded`（Token + Base64）、`Encrypted`（Token + 完整加密），透過 `ApiAccessControlAttribute` 提供分級安全機制。
- **啟用可為 Null 參考型別**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Api.Core/
  Authorization/    IApiAuthorizationValidator、ApiAuthorizationValidator、
                    ApiAuthorizationContext、ApiAuthorizationResult
  Conversion/       ApiInputConverter、ApiOutputConverter
                    （.NET 物件模型轉換：API 型別 ↔ BO 型別）
  JsonRpc/          JsonRpcExecutor、JsonRpcRequest、JsonRpcResponse、JsonRpcError、
                    JsonRpcException、ApiPayload、ApiPayloadConverter
  Messages/         ApiMessageBase、ApiRequest、ApiResponse、
                    ExecFuncRequest、ExecFuncResponse、
                    ApiHeaders、PayloadFormat、ApiErrorInfo、ApiCallContext
    System/         內建系統級請求/回應型別
                    （Login、Ping、CreateSession、GetDefine、SaveDefine、
                    GetPackage、CheckPackageUpdate、GetCommonConfiguration）
  MessagePack/      SafeMessagePackSerializerOptions、MessagePackCodec、
                    WireContracts（顯式註冊）、WireValueFormatter、
                    ADO.NET 型別自訂格式器
  Registry/         ApiContractRegistry（Contract → API 型別註冊中心）
  Transformers/     IApiPayloadTransformer、ApiPayloadTransformer、
                    IApiPayloadSerializer、MessagePackPayloadSerializer、
                    IApiPayloadCompressor、GzipPayloadCompressor、
                    IApiPayloadEncryptor、AesPayloadEncryptor、
                    NoEncryptionEncryptor、ApiPayloadOptionsFactory
                    （byte 層級 payload 管線；與 Conversion 的 .NET 物件層級
                    型別轉換抽象層次不同）
  Validator/        ApiAccessValidator
  （根目錄）         ApiServiceOptions（使用者啟動配置入口）
```

命名空間佈局遵循 [ADR-008](../../docs/adr/adr-008-bee-db-namespace-layout.md) 的設計原則：
契約依職能歸類（`Messages` 放訊息型別、`Conversion` 放型別轉換、`Registry` 放註冊中心等）；
根層只保留跨切面基礎設施（此處僅 `ApiServiceOptions`）。
