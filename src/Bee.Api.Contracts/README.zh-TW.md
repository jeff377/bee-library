# Bee.Api.Contracts

> API 層與商業邏輯層之間的契約介面庫，定義所有 Request/Response 介面。

[English](README.md)

## 架構定位

- **層級**：API 層（契約）
- **在相依圖中的位置**：見[專案相依性全景圖](../../docs/dependency-map.zh-TW.md)。**此處不逐一列出** —— 權威來源是 csproj，而散落在每份套件 README 的散文拷貝會漂且無人察覺。它們確實漂了：`Bee.Hosting` 抽出後，有四份 README 的下游數個月都沒把它補上。

## 目標框架

- `net10.0` -- 存取現代執行階段 API 與效能改進

## 主要功能

### 驗證契約

- `ILoginRequest` / `ILoginResponse` -- RSA 金鑰交換登入流程（用戶端傳送 `ClientPublicKey`，伺服器回傳 `ApiEncryptionKey`）
- `ICreateSessionRequest` / `ICreateSessionResponse` -- 驗證成功後建立 Session

### 健康檢查

- `IPingRequest` / `IPingResponse` -- 輕量級 API 健康/連線檢查

### 定義 CRUD

- `IGetDefineRequest` / `IGetDefineResponse` -- 擷取 FormSchema 驅動的定義資料
- `ISaveDefineRequest` / `ISaveDefineResponse` -- 儲存定義資料變更

### 自訂函式執行

- `IExecFuncRequest` / `IExecFuncResponse` -- 呼叫伺服器端自訂函式（AnyCode 模式）

### 組態

- `IGetCommonConfigurationRequest` / `IGetCommonConfigurationResponse` -- 擷取共用應用程式組態

### 套件管理

- `PackageUpdateQuery` -- 更新檢查的查詢參數
- `PackageUpdateInfo` -- 更新中繼資料（版本、大小、SHA-256、交付模式），以 MessagePack 序列化

## 主要公開 API

| 介面 / 類別 | 用途 |
|-------------|------|
| `ILoginRequest` / `ILoginResponse` | RSA 金鑰交換登入契約 |
| `ICreateSessionRequest` / `ICreateSessionResponse` | Session 建立契約 |
| `IPingRequest` / `IPingResponse` | 健康檢查契約 |
| `IGetDefineRequest` / `IGetDefineResponse` | 定義擷取契約 |
| `ISaveDefineRequest` / `ISaveDefineResponse` | 定義儲存契約 |
| `IExecFuncRequest` / `IExecFuncResponse` | 自訂函式執行契約 |
| `IGetCommonConfigurationRequest` / `IGetCommonConfigurationResponse` | 組態擷取契約 |
| `PackageUpdateQuery` | 更新檢查查詢參數 |

## 設計慣例

- **軸分命名空間** -- 介面依 `System` / `Form` / `AuditLog` 子命名空間分組，對映 `Bee.Business.*` 與 `Bee.Api.Core.Messages.*` 兩層，使「合約介面、訊息實作、商業物件」三者共用同一軸。跨 BO 的泛用 `IExecFunc*` 派發契約保留在根命名空間 `Bee.Api.Contracts`（鏡像 `Bee.Api.Core.Messages` 根層的 `ExecFunc*` 實作）。
- **純介面定義** -- 每個 API 操作以 `IXxxRequest` / `IXxxResponse` 配對定義，本專案不含任何實作邏輯。
- **不帶序列化標註** -- 資料類別如 `PackageUpdateInfo` 是帶 public 可讀寫屬性的普通型別。它們與 wire 的綁定以手寫 formatter 的形式住在 `Bee.Api.Core`，因此本套件不相依任何傳輸格式（[ADR-036](../../docs/adr/adr-036-wire-serialization-externalized.md)）。
- **RSA 安全機制** -- 登入契約包含 `ClientPublicKey`（用戶端產生）與 `ApiEncryptionKey`（伺服器產生），用於安全金鑰交換。
- **啟用可為 Null 參考型別**（`<Nullable>enable</Nullable>`）。

## 這些介面為什麼存在

它們不是裝飾性的標記——有兩個機制在執行期與編譯期依賴它們。

**1. 讓一個靜默的反射複製保證完整。** 每一次 API 呼叫都經 `ApiInputConverter.Convert` 雙向轉換，
而它是**以屬性名稱比對**來複製的：入站由 `JsonRpcExecutor` 呼叫，把 wire 訊息轉成 BO 參數；
出站由 `ApiOutputConverter` 呼叫，把 BO 結果轉成 wire 回應。名稱對不上就靜默跳過——不擲例外、
不警告，呼叫看起來成功但該欄位是空的。正因為 `LoginRequest` 與 `LoginArgs` 都實作
`ILoginRequest`，編譯器才會逼兩邊帶同一組成員，複製也就不可能只複製一半。

**2. 作為 wire 不變式的判別依據。** `DateTimeWireGuard` 以回應契約
（`IGetListResponse`、`ISaveResponse`、`ILogListResponse` 等）做 pattern matching，
辨識出帶有 `DataSet` 或裸 `DateTime` 的 payload，並對其強制 ADR-032 的 wire 不變式。

配對的兩側都由測試把關，而非靠人工 review：`ApiContractPairingTests`（位於
`Bee.Api.Core.UnitTests`）確認每個 `ApiRequest` / `ApiResponse` 子型別都實作對應契約；
`BusinessContractPairingTests`（位於 `Bee.Business.UnitTests`）對每個 `BusinessArgs` /
`BusinessResult` 做同樣的事。兩道閘門都是因為該側曾經漏過而且沒人發現才補上的。

> 這裡沒有「契約 → 實作」的執行期註冊表。曾經有一個（`ApiContractRegistry`），
> 為的是「BO 回傳純 POCO」這個從未發生的情境；它已被移除，該做的轉換
> `ApiOutputConverter` 本來就在做。

## 目錄結構

介面依軸分子資料夾（資料夾＝子命名空間）；跨 BO 的 `IExecFunc*` 配對保留在根層。

```
Bee.Api.Contracts/
  IExecFuncRequest.cs / IExecFuncResponse.cs          # 根層 — 跨 BO 泛用派發
  System/                                             # namespace Bee.Api.Contracts.System
    ILoginRequest.cs / ILoginResponse.cs
    ICreateSessionRequest.cs / ICreateSessionResponse.cs
    IPingRequest.cs / IPingResponse.cs
    IEnterCompany* / ILeaveCompany* / IGetLanguage*
    IGetDefine* / ISaveDefine* / IGetFormSchema* / IGetFormLayout* / IGetDepartmentTreeResponse
    IGetCommonConfiguration*
  Form/                                               # namespace Bee.Api.Contracts.Form
    IGetList* / IGetData* / IGetNewData* / ISave* / IDelete* / IGetLookup*
  AuditLog/                                           # namespace Bee.Api.Contracts.AuditLog
    IGetChangeLog* / IGetChangeDetail* / IGetAccessLog* / IGetLoginLog*
    IGetApiAnomaly* / IGetDbAnomaly* / IGetTopApiMethodsRequest
    ILogListResponse.cs / ILogAggregateResponse.cs / RecordFieldChange.cs
```
