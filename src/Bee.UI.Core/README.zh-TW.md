# Bee.UI.Core

> `Bee.UI.*` 前端家族（Avalonia / MAUI / Blazor）共享的用戶端基礎層：連線狀態、API 連接器、endpoint 持久化，以及用戶端權限能力解析。

[English](README.md)

## 架構定位

- **層級**：UI 層（共享用戶端基礎）
- **下游**（依賴此專案者）：`Bee.UI.Avalonia`、`Bee.UI.Maui` 與 Blazor 前端
- **上游**（此專案依賴）：`Bee.Api.Client`

## 目標框架

- `net10.0` -- 使用現代執行階段 API 與效能改進

## 概觀

`Bee.UI.Core` 是所有 `Bee.UI.*` 前端共用、與 UI 框架無關的基礎層。它持有 per-process 的用戶端連線
狀態（`ClientInfo`）、抽象化服務 endpoint 的持久化位置（`IEndpointStorage`），並將 server 下發的
權限快照轉換為每個 UI 元素的能力決策（`ElementCapabilityResolver`）。它不含任何 UI 框架型別，因此
Avalonia、MAUI、Blazor 可共用同一套連線與權限邏輯，各自以自己的方式渲染。

## 主要型別

### 連線狀態

- `ClientInfo` -- 用戶端連線的靜態單例。持有 `AccessToken`（per-process token 模型：重設 token 會
  清掉快取的 `SystemApiConnector`、`ClientDefineAccess` 與能力快照）、延遲建立 `SystemApiConnector`
  與 `ClientDefineAccess`、透過 `CreateFormApiConnector(progId)` 產生表單層級連接器、經
  `InitializeAsync` / `SetEndpointAsync` 解析 endpoint（本機 vs. 遠端），並套用登入 / EnterCompany
  結果（`ApplyLoginResult`、`ApplyEnterCompanyResult`、`ClearCompanyContext`）。`ResetDefineCache`
  在切換租戶後丟棄快取的定義資料。

### Endpoint 持久化

- `IEndpointStorage` -- 服務 endpoint 的持久化契約（`LoadEndpoint` / `SetEndpoint` / `SaveEndpoint`）。
- `EndpointStorage` -- 預設實作，backing 為 `ClientInfo.ClientSettings`（`{ExeName}.Settings.xml`）。
  當預設檔案位置不適用時，前端會將 `ClientInfo.EndpointStorage` 指派為平台專屬實作 —— 例如
  `FileEndpointStorage`（於 `Bee.UI.Avalonia`）或 sandbox-friendly 的 `MauiPreferenceEndpointStorage`
  （於 `Bee.UI.Maui`）。

### 主機服務

- `IUIViewService` -- 由主機 UI 框架提供的檢視服務（例如 `ShowApiConnectAsync`，在 endpoint 缺失或
  無法連線時彈出連線設定）。

### 權限能力解析

- `IElementCapabilityResolver` / `ElementCapabilityResolver` -- 與 UI 無關的純解析器，將 per-model
  權限快照（通常為 `ClientInfo.Capabilities`）轉換為元素層級決策：`Can(schema, action, capabilities)`
  判斷命令、`ResolveField(schema, fieldName, tableName, capabilities)` 判斷敏感欄位。快照為 `null`
  代表停用強制，所有元素維持完整能力。
- `FieldCapability` -- 單一欄位解析後的能力（`Visible` / `ReadOnly`），由消費端 UI 與欄位的 layout
  狀態合併。`FieldCapability.Allowed` 為不受限的預設值。

> 用戶端能力解析**僅為 UX 降級**。後端始終是權威的安全邊界。

## 設計慣例

- **per-process token 模型** -- `ClientInfo` 是持有整個行程單一 access token 的靜態單例；變更 token
  會使快取的連接器、定義存取器與能力快照失效。
- **與 UI 框架無關** -- 不滲入任何 UI 框架型別，因此同一套連線與權限邏輯服務所有 `Bee.UI.*` 前端。
- **可插拔的 endpoint 儲存** -- 主機以平台適用的 `IEndpointStorage` 實作覆寫 `ClientInfo.EndpointStorage`。
- **async-friendly 初始化** -- `InitializeAsync` / `SetEndpointAsync` 驗證 endpoint 並初始化連接器
  時不阻塞，因此在單執行緒執行環境（browser WASM）上安全。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.UI.Core/
  ClientInfo.cs          # 用戶端連線狀態與連接器工廠
  IEndpointStorage.cs    # Endpoint 持久化契約
  EndpointStorage.cs     # 預設的 ClientSettings-backed 實作
  IUIViewService.cs      # 主機提供的檢視服務
  VersionInfo.cs         # 套件版本中繼資料
  Permissions/           # ElementCapabilityResolver、FieldCapability、
                         # IElementCapabilityResolver
```
