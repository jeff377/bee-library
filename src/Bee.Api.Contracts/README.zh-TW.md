# Bee.Api.Contracts

> API 層與商業邏輯層之間的契約介面庫，定義所有 Request/Response 介面。

[English](README.md)

## 架構定位

- **層級**：API 層（契約）
- **下游**（依賴此專案）：`Bee.Api.Core`、`Bee.Business`
- **上游**（此專案依賴）：`Bee.Definition`

## 目標框架

- `netstandard2.0` -- 廣泛相容 .NET Framework 4.6.1+ 與 .NET Core 2.0+
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

- `ICheckPackageUpdateRequest` / `ICheckPackageUpdateResponse` -- 查詢可用的套件更新
- `IGetPackageRequest` / `IGetPackageResponse` -- 下載套件內容
- `PackageUpdateQuery` -- 更新檢查的查詢參數
- `PackageUpdateInfo` -- 更新中繼資料（版本、大小、SHA-256、交付模式），以 MessagePack 序列化
- `PackageDelivery` -- 列舉，定義交付模式（`Url` 或 `Api`）

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
| `ICheckPackageUpdateRequest` / `ICheckPackageUpdateResponse` | 套件更新檢查契約 |
| `IGetPackageRequest` / `IGetPackageResponse` | 套件下載契約 |
| `PackageUpdateQuery` | 更新檢查查詢參數 |
| `PackageUpdateInfo` | 套件更新中繼資料（MessagePack） |
| `PackageDelivery` | 交付模式列舉（`Url` / `Api`） |

## 設計慣例

- **純介面定義** -- 每個 API 操作以 `IXxxRequest` / `IXxxResponse` 配對定義，本專案不含任何實作邏輯。
- **MessagePack 序列化** -- 資料類別如 `PackageUpdateInfo` 使用 `[MessagePackObject]` 與 `[Key(n)]` 屬性進行高效能二進位序列化。
- **RSA 安全機制** -- 登入契約包含 `ClientPublicKey`（用戶端產生）與 `ApiEncryptionKey`（伺服器產生），用於安全金鑰交換。
- **穩定列舉值** -- `PackageDelivery` 成員具有明確的整數值，不可變更既有值以維護序列化相容性。
- **啟用可為 Null 參考型別**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Api.Contracts/
  ILoginRequest.cs / ILoginResponse.cs
  ICreateSessionRequest.cs / ICreateSessionResponse.cs
  IPingRequest.cs / IPingResponse.cs
  IGetDefineRequest.cs / IGetDefineResponse.cs
  ISaveDefineRequest.cs / ISaveDefineResponse.cs
  IExecFuncRequest.cs / IExecFuncResponse.cs
  IGetCommonConfigurationRequest.cs / IGetCommonConfigurationResponse.cs
  ICheckPackageUpdateRequest.cs / ICheckPackageUpdateResponse.cs
  IGetPackageRequest.cs / IGetPackageResponse.cs
  PackageUpdateQuery.cs
  PackageUpdateInfo.cs
  PackageDelivery.cs
```
