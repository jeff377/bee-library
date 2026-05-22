# QuickStart.Console

Bee.Api.Client 最小消費端示範。連線到 [`QuickStart.Server`](../QuickStart.Server/README.md)，呼叫 `System.Ping` 與自訂的 `Echo.Echo` BO。

## 跑法

```bash
# 先在另一個 terminal 啟動 QuickStart.Server
cd samples/QuickStart.Console
dotnet run
```

預設連 `http://localhost:5050/api`。要換 endpoint：

```bash
dotnet run -- --endpoint http://other-host:5050/api
```

## 預期輸出

```
→ endpoint: http://localhost:5050/api

• System.Ping
  status: ok

• Echo.Echo (message="hello from QuickStart.Console")
  response : echo: hello from QuickStart.Console
  serverTime: 2026-05-23T13:00:00.0000000Z
```

## 對應到哪些 library 功能

| 程式段落 | library 功能 |
|----------|--------------|
| `ApiClientInfo.ApiKey = "quickstart-demo"` | `Bee.Api.Client.ApiClientInfo` — Remote 模式下每個 request 會帶 `X-Api-Key` |
| `new SystemApiConnector(endpoint, Guid.Empty)` | `Bee.Api.Client.Connectors.SystemApiConnector` — 內部用 `RemoteApiProvider` 走 HTTP |
| `await connector.PingAsync()` | 直接打 `System.Ping`，框架預設將其視為 anonymous，回傳 `status=ok` |
| `new FormApiConnector(endpoint, Guid.Empty, "Echo")` | `Bee.Api.Client.Connectors.FormApiConnector` — 鎖定 progId="Echo"，呼叫 `Echo.<action>` |
| `connector.ExecuteAsync<EchoResponse>("Echo", req, PayloadFormat.Plain)` | 走 `Echo.Echo`；Plain format 因為 BO 標 `[ApiAccessControl(Public, Anonymous)]` 不需要加密 |

## Local vs Remote 模式

這個 console 用 **Remote 模式**（透過 HTTP 連到 Server）。底層 `Bee.Api.Client` 同樣支援 **Local 模式**（in-process 直接呼叫 backend），呼叫端 API 完全相同。差別只在建構連線時：

```csharp
// Remote（本 demo）
var connector = new SystemApiConnector("http://localhost:5050/api", Guid.Empty);

// Local（在同一個 process 內，需要先設定 ApiClientInfo.LocalServiceProvider）
ApiClientInfo.LocalServiceProvider = services.AddBeeFramework(...).BuildServiceProvider();
var connector = new SystemApiConnector(Guid.Empty);
```

Local 模式的完整 composition 由 [`QuickStart.Server/Program.cs`](../QuickStart.Server/Program.cs) 示範（它就是一個 in-process backend）。如果要在 console 內用 Local 模式，把 Server 的 bootstrap 程式碼複製過來、走 `BuildServiceProvider()` 即可。
