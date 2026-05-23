# QuickStart.Console

**English** | [繁體中文](README.zh-TW.md)

The minimal consumer demo for `Bee.Api.Client`. Connects to [`QuickStart.Server`](../QuickStart.Server/README.md) and invokes both the built-in `System.Ping` and the custom `Echo.Echo` BO.

## How to run

```bash
# Start QuickStart.Server in another terminal first
cd samples/QuickStart.Console
dotnet run
```

The default endpoint is `http://localhost:5050/api`. To override:

```bash
dotnet run -- --endpoint http://other-host:5050/api
```

## What to expect

```
→ endpoint: http://localhost:5050/api

• System.Ping
  status: ok

• Echo.Echo (message="hello from QuickStart.Console")
  response : echo: hello from QuickStart.Console
  serverTime: 2026-05-23T13:00:00.0000000Z
```

## What this maps to in the library

| Code | Library feature |
|------|-----------------|
| `ApiClientInfo.ApiKey = "quickstart-demo"` | `Bee.Api.Client.ApiClientInfo` — in Remote mode, every request carries this as `X-Api-Key` |
| `new SystemApiConnector(endpoint, Guid.Empty)` | `Bee.Api.Client.Connectors.SystemApiConnector` — internally uses `RemoteApiProvider` over HTTP |
| `await connector.PingAsync()` | Invokes `System.Ping`; treated as anonymous by the framework and returns `status=ok` |
| `new FormApiConnector(endpoint, Guid.Empty, "Echo")` | `Bee.Api.Client.Connectors.FormApiConnector` — bound to progId "Echo", invokes `Echo.<action>` |
| `connector.ExecuteAsync<EchoResponse>("Echo", req, PayloadFormat.Plain)` | Calls `Echo.Echo`; `PayloadFormat.Plain` is sufficient because the BO is annotated `[ApiAccessControl(Public, Anonymous)]` |

## Local vs Remote modes

This console uses **Remote mode** (HTTP to the server). `Bee.Api.Client` also supports **Local mode** (in-process dispatch to the backend) with an identical caller-side API — only the connector construction differs:

```csharp
// Remote (this demo)
var connector = new SystemApiConnector("http://localhost:5050/api", Guid.Empty);

// Local (same process; requires ApiClientInfo.LocalServiceProvider to be set)
ApiClientInfo.LocalServiceProvider = services.AddBeeFramework(...).BuildServiceProvider();
var connector = new SystemApiConnector(Guid.Empty);
```

[`QuickStart.Server/Program.cs`](../QuickStart.Server/Program.cs) is itself a complete in-process backend wiring; to use Local mode from a console, copy its bootstrap code and call `BuildServiceProvider()`.
