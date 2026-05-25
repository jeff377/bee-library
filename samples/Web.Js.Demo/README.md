# Web.Js.Demo

[繁體中文](README.zh-TW.md)

Demonstrates calling the Bee.NET JSON-RPC API from pure JavaScript in a browser —
no `npm`, no build step, no framework. The JS frontend uses
`PayloadFormat.Plain` (the wire format opened up by the JSON-RPC frontend
integration plan), so all requests are plain JSON.

Covers all 7 methods downgraded for JS access in the plan:

| Section | Methods |
|---------|---------|
| Login | `System.Login` |
| Ping | `System.Ping` (no auth) |
| Enter Company | `System.EnterCompany` / `System.LeaveCompany` (error path — see UI hint) |
| Employee CRUD | `Employee.GetList` / `GetData` / `GetNewData` / `Save` / `Delete` |
| Logout | `System.Logout` |

## How to run

1. Start the backend (in another terminal):

   ```sh
   cd samples/QuickStart.Server
   dotnet run
   ```

   The server listens on `http://localhost:5050` and has CORS enabled for any
   origin (demo-only — production hosts must restrict origins).

2. Open the demo in a browser. Two options:

   - **Direct file open** — `open samples/Web.Js.Demo/index.html` (Mac) or
     equivalent on Windows / Linux. Modern browsers accept `file://` as an
     origin against an `AllowAnyOrigin` CORS policy.

   - **Static file server** — useful if your browser blocks `file://` for
     `fetch`:

     ```sh
     # Python 3
     cd samples/Web.Js.Demo
     python3 -m http.server 8080
     # or, if installed
     dotnet serve -p 8080
     ```

     Then open `http://localhost:8080/index.html`.

3. Click **Login** (default credentials `demo` / `demo`), then **Ping** —
   each result shows up in the output panel at the bottom.

## Files

| File | Purpose |
|------|---------|
| `index.html` | Minimal UI — Login form, Ping button, result panel. Vanilla CSS, no external dependencies. |
| `bee-api-client.js` | ES module: `rpcCall(method, value)` + `systemApi.login` / `systemApi.ping` + `RpcError` + token state. |
| `app.js` | UI event wiring; depends only on `bee-api-client.js`. |

## Headers used

| Header | Value | Notes |
|--------|-------|-------|
| `Content-Type` | `application/json` | JSON-RPC body |
| `X-Api-Key` | `quickstart-demo` (hard-coded) | Default `ApiAuthorizationValidator` only requires the value to be non-empty. Production hosts must register a stricter validator. |
| `Authorization` | `Bearer <accessToken>` | Sent only after `Login` returns an AccessToken. Required for any method whose `[ApiAccessControl]` is `Authenticated`. |

## Why pure JavaScript

The whole point of opening up the Plain wire format is so JS frontends can talk
to the Bee.NET backend without dragging in `Microsoft.JSInterop`, MessagePack,
or the AES-CBC-HMAC pipeline. Keeping the demo dependency-free demonstrates the
minimum surface a real JS framework integration (React / Vue / Angular) needs to
build on.

If your project already uses a TypeScript toolchain, copy `bee-api-client.js`
into your `src/` and rewrite types as needed — the API surface is small enough
that hand-maintained TypeScript interfaces are cheaper than codegen at this
stage.

## Related

- Plan: [docs/plans/plan-jsonrpc-frontend-integration.md](../../docs/plans/plan-jsonrpc-frontend-integration.md)
- Backend host: [samples/QuickStart.Server](../QuickStart.Server/)
- Demo credentials: [samples/Bee.Samples.Shared/DemoCredentials.cs](../Bee.Samples.Shared/DemoCredentials.cs)
