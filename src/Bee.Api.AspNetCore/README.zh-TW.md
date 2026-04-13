# Bee.Api.AspNetCore

> 提供統一 JSON-RPC 2.0 API 端點的 ASP.NET Core 控制器程式庫。

[English](README.md)

## 架構定位

- **層級**：API 層（裝載）
- **上游**（依賴此專案）：應用程式（使用者繼承控制器）
- **下游**（此專案依賴）：`Bee.Api.Core`

## 目標框架

- `net10.0` -- ASP.NET Core 裝載需要現代執行階段

## 主要功能

### 單一 POST 端點

- 公開單一 `POST /api` 路由，標記 `[ApiController]` 與 `[Produces("application/json")]`。
- 處理前驗證 `Content-Type: application/json`，其他媒體類型回傳 `415 Unsupported Media Type`。

### 完整非同步請求管線

- `PostAsync` 協調完整生命週期：讀取請求、驗證授權、執行處理。
- 每個階段皆為 `virtual` 方法，子類別可獨立覆寫。

### JSON-RPC 請求解析

- `ReadRequestAsync` 讀取原始內容，反序列化為 `JsonRpcRequest`，並驗證 `Method` 欄位。
- 針對空內容、缺少方法及格式錯誤的 JSON 回傳結構化的 `JsonRpcException` 錯誤。

### 授權驗證

- `ValidateAuthorization` 擷取 `X-Api-Key` 與 `Authorization` 標頭，委派至已設定的 `ApiServiceOptions.AuthorizationValidator`。
- 驗證失敗時回傳 `401 Unauthorized` 與 JSON-RPC 錯誤。

### 請求執行

- `HandleRequestAsync` 以已驗證的存取權杖建立 `JsonRpcExecutor` 並以 `application/json` 回傳結果。
- 未處理的例外在開發環境回傳詳細訊息，正式環境回傳空訊息，回傳 `500 Internal Server Error`。

### 結構化錯誤回應

- `CreateErrorResponse` 產生一致的 `JsonRpcResponse`，包含錯誤碼、訊息及選用的資料酬載，對應至適當的 HTTP 狀態碼。

## 主要公開 API

| 類別 / 成員 | 用途 |
|-------------|------|
| `ApiServiceController` | 抽象基底控制器；繼承後註冊於 ASP.NET Core 應用程式 |
| `PostAsync` | 所有 JSON-RPC 請求的進入點（`POST /api`） |
| `ReadRequestAsync` | 解析並驗證 JSON-RPC 請求內容 |
| `ValidateAuthorization` | 透過 `ApiAuthorizationValidator` 檢查 API 金鑰與 Bearer 權杖 |
| `HandleRequestAsync` | 將請求分派至 `JsonRpcExecutor` |
| `CreateErrorResponse` | 建立標準化的 JSON-RPC 錯誤回應 |
| `IsDevelopment` | 指示主機環境是否為開發環境 |

## 設計慣例

- **樣板方法模式** -- `PostAsync` 定義管線骨架；`ReadRequestAsync`、`ValidateAuthorization`、`HandleRequestAsync` 與 `CreateErrorResponse` 皆為 `virtual`，可選擇性覆寫。
- **開發與正式環境錯誤訊息區分** -- 例外詳細資訊僅在 `IsDevelopment` 為 `true` 時公開，防止正式環境資訊洩漏。
- **不直接依賴 DI 容器** -- 服務從 `HttpContext.RequestServices` 解析，控制器可在任何 ASP.NET Core 主機中運作，無需額外設定。
- **外部相依**：`Microsoft.AspNetCore.Mvc.Core`（v2.3.0）。

## 目錄結構

```
Bee.Api.AspNetCore/
  Controllers/
    ApiServiceController.cs    # 抽象基底控制器（約 177 行）
```
