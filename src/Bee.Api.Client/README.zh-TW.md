# Bee.Api.Client

> API 用戶端連接器，提供統一介面以進行本機（行程內）與遠端（網路）商業邏輯呼叫。

[English](README.md)

## 架構定位

- **層級**：前端 / 用戶端
- **下游**（依賴此專案）：應用程式（WinForms、Blazor 等）
- **上游**（此專案依賴）：`Bee.Api.Core`

## 目標框架

- `net10.0` -- 存取現代執行階段 API 與效能改進

## 主要功能

### 本機 / 遠端策略

- `IJsonRpcProvider` 抽象化傳輸層；`LocalApiProvider` 透過 `JsonRpcExecutor` 在行程內呼叫商業邏輯，`RemoteApiProvider` 則向遠端端點發送 HTTP POST 請求。
- 建構時透過連接器的雙建構函式模式選擇啟用的策略。

### 系統層級連接器

- `SystemApiConnector` 公開系統操作：`Login`（RSA 金鑰交換驗證）、`Ping`（健康檢查）、`CreateSession`（一次性或限時權杖）、`Initialize`（環境初始化）、`GetDefine` / `SaveDefine`（定義 CRUD）以及 `ExecFunc`（自訂函式執行）。
- 每個非同步方法皆有對應的同步版本，由 `SyncExecutor` 驅動。

### 表單層級連接器

- `FormApiConnector` 綁定至特定 `ProgId`，公開表單層級的商業物件呼叫（`ExecFunc`、`ExecFuncAnonymous`、`ExecFuncLocal`）。
- 繼承 `ApiConnector` 的完整酬載管線（編碼、壓縮、加密）。

### 連線驗證

- `ApiConnectValidator` 從端點字串判斷 `ConnectType`（本機或遠端），驗證目標，並可選擇性地為本機連線自動產生缺少的設定檔。
- 遠端驗證會執行 `Ping` 以確認連線後才回傳。

### 快取定義存取

- `RemoteDefineAccess` 透過 API 實作 `IDefineAccess`，快取已擷取的定義（SystemSettings、DatabaseSettings、FormSchema、FormLayout 等），避免重複的網路呼叫。

### 應用程式上下文

- `ApiClientInfo` 持有靜態執行階段組態：`ConnectType`、`Endpoint`、`ApiKey`、`ApiEncryptionKey` 與 `SupportedConnectTypes`。

### 非同步轉同步橋接

- `SyncExecutor` 封裝 `Task.Run` + `GetAwaiter().GetResult()`，可安全地從同步上下文（建構函式、WinForms 事件處理常式）呼叫非同步方法，避免死結。

## 主要公開 API

| 類別 / 介面 | 用途 |
|-------------|------|
| `ApiClientInfo` | 靜態執行階段組態（連線類型、端點、金鑰） |
| `ApiConnector` | 抽象基底連接器，含酬載管線與追蹤 |
| `SystemApiConnector` | 系統層級操作（Login、Ping、CreateSession、Initialize、Define CRUD、ExecFunc） |
| `FormApiConnector` | 表單層級商業物件呼叫，綁定至特定 ProgId |
| `IJsonRpcProvider` | JSON-RPC 傳輸策略介面 |
| `LocalApiProvider` | 行程內提供者，透過 `JsonRpcExecutor` |
| `RemoteApiProvider` | HTTP 提供者，附帶 API 金鑰與 Bearer 權杖標頭 |
| `RemoteDefineAccess` | 透過 API 實作 `IDefineAccess`，含快取機制 |
| `ApiConnectValidator` | 驗證端點並判斷連線類型 |
| `ConnectType` | 列舉：`Local`、`Remote` |
| `SupportedConnectTypes` | 旗標列舉：`Local`、`Remote`、`Both` |
| `SyncExecutor` | 非同步轉同步橋接，供非非同步呼叫者使用 |

## 設計慣例

- **策略模式** -- `IJsonRpcProvider` 搭配 `LocalApiProvider` 與 `RemoteApiProvider` 實作；連接器在建構時選擇策略。
- **樣板方法** -- `ApiConnector` 定義 `ExecuteAsync<T>` 的固定步驟（建立請求、轉換酬載、呼叫提供者、還原回應）；子類別提供領域專屬方法。
- **雙建構函式模式** -- 每個連接器提供兩個建構函式：`(Guid accessToken)` 用於本機、`(string endpoint, Guid accessToken)` 用於遠端，對應兩種提供者類型。
- **酬載格式協商** -- 請求預設為 `PayloadFormat.Encrypted`；管線在未設定加密金鑰時自動降級為 `Encoded`，本機提供者於非偵錯模式下降級為 `Plain`。
- **SyncExecutor 支援舊式呼叫者** -- 每個非同步方法皆有使用 `SyncExecutor` 的同步包裝，使 WinForms 建構函式與同步 API 可無死結使用。

## 目錄結構

```
Bee.Api.Client/
  ApiClientInfo.cs              # 靜態執行階段組態
  ApiConnectValidator.cs           # 端點驗證與 ConnectType 偵測
  ConnectType.cs                   # Local / Remote 列舉
  SupportedConnectTypes.cs         # 支援的連線類型旗標列舉
  SyncExecutor.cs                  # 非同步轉同步橋接
  Connectors/
    ApiConnector.cs                # 抽象基底連接器
    SystemApiConnector.cs          # 系統層級操作
    FormApiConnector.cs            # 表單層級商業物件呼叫
  Providers/
    IJsonRpcProvider.cs            # 傳輸策略介面
    LocalApiProvider.cs     # 行程內提供者
    RemoteApiProvider.cs    # HTTP 提供者
  DefineAccess/
    RemoteDefineAccess.cs          # 透過 API 實作快取式 IDefineAccess
```
