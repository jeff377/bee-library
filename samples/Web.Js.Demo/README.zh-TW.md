# Web.Js.Demo

[English](README.md)

示範如何用純 JavaScript（瀏覽器原生）呼叫 Bee.NET 的 JSON-RPC API ——
不需要 `npm`、不需要 build、不需要任何框架。JS 前端走
`PayloadFormat.Plain`（由 JSON-RPC 前端整合計畫開放的 wire format），
所有請求都是純 JSON。

涵蓋本 plan 開放給 JS 的 7 個方法：

| 區塊 | 對應方法 |
|------|---------|
| Login | `System.Login` |
| Ping | `System.Ping`（不需 auth） |
| Enter Company | `System.EnterCompany` / `System.LeaveCompany`（錯誤路徑示範 — 見 UI hint） |
| Employee CRUD | `Employee.GetList` / `GetData` / `GetNewData` / `Save` / `Delete` |
| Logout | `System.Logout` |

## 怎麼跑

1. 啟動後端（另開一個 terminal）：

   ```sh
   cd samples/QuickStart.Server
   dotnet run
   ```

   Server 監聽 `http://localhost:5050`，已開 CORS 允許任何 origin
   （demo-only — production host 必須明確限制 origin）。

2. 用瀏覽器開啟 demo，兩種方式：

   - **直接開檔** — `open samples/Web.Js.Demo/index.html`（Mac），
     Windows / Linux 用對應工具。現代瀏覽器在 `AllowAnyOrigin` CORS 政策下
     接受 `file://` origin。

   - **靜態檔案 server** — 如果瀏覽器擋 `file://` 的 `fetch`：

     ```sh
     # Python 3
     cd samples/Web.Js.Demo
     python3 -m http.server 8080
     # 或裝了 dotnet serve
     dotnet serve -p 8080
     ```

     然後開 `http://localhost:8080/index.html`。

3. 點 **Login**（預設帳密 `demo` / `demo`），再點 **Ping** ——
   結果會出現在下方的輸出區。

## 檔案

| 檔案 | 用途 |
|------|------|
| `index.html` | 最小 UI — Login 表單、Ping 按鈕、結果輸出區。Vanilla CSS，無外部依賴。 |
| `bee-api-client.js` | ES module：`rpcCall(method, value)` + `systemApi.login` / `systemApi.ping` + `RpcError` + token 狀態管理。 |
| `app.js` | UI 事件綁定；只依賴 `bee-api-client.js`。 |
| `.smoke.yaml` | `demo-smoke` skill 的設定檔 — 啟動兩個 prerequisite server 並驗證頁面載入後出現預期的區塊文字。檔案開頭註解說明瀏覽器 tier 限制（無法點擊 → 只能 load-level smoke）。 |

## 使用的 Headers

| Header | 值 | 說明 |
|--------|-----|------|
| `Content-Type` | `application/json` | JSON-RPC body |
| `X-Api-Key` | `quickstart-demo`（寫死於 client） | 預設的 `ApiAuthorizationValidator` 只要求非空字串，不檢查實際值。Production host 必須註冊更嚴格的 validator。 |
| `Authorization` | `Bearer <accessToken>` | 只在 `Login` 取得 AccessToken 後才送。所有 `[ApiAccessControl]` 為 `Authenticated` 的方法都需要。 |

## 為什麼純 JavaScript

開放 Plain wire format 的目的就是讓 JS 前端能直接和 Bee.NET 後端通訊，
不必引入 `Microsoft.JSInterop`、MessagePack、或 AES-CBC-HMAC 加密管線。
保持 demo 零依賴，可以清楚示範真實 JS 框架整合（React / Vue / Angular）
需要的最小 surface。

如果你的專案已經用 TypeScript toolchain，直接把 `bee-api-client.js`
複製到 `src/` 改寫成 TS interface 就好 —— API surface 還很小，
手寫 TypeScript interface 比 codegen 划算。

## 相關文件

- 計畫：[docs/plans/plan-jsonrpc-frontend-integration.md](../../docs/plans/plan-jsonrpc-frontend-integration.md)
- 後端 host：[samples/QuickStart.Server](../QuickStart.Server/)
- Demo 帳密：[samples/Bee.Samples.Shared/DemoCredentials.cs](../Bee.Samples.Shared/DemoCredentials.cs)
