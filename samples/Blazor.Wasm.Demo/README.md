# Blazor.Wasm.Demo

**English** | [繁體中文](README.zh-TW.md)

The Blazor WebAssembly client. It shares the same `FormSchema` and the same `BeeLoginPanel` / `FormPage` components as [`Blazor.Server.Demo`](../Blazor.Server.Demo/README.md); the only differences are where it runs:

- Component logic runs in the **browser** (.NET WASM runtime)
- The backend call goes through **`RemoteApiProvider`** (HTTP); the endpoint defaults to `${BaseAddress}api`
- Must be served by [`Blazor.Wasm.Demo.Host`](../Blazor.Wasm.Demo.Host/README.md) — the host serves both the WASM static files and the `/api` JSON-RPC endpoint

## How to run

```bash
cd samples/Blazor.Wasm.Demo.Host
dotnet run
# Browser opens http://localhost:5070 automatically
```

Don't `dotnet run` this Wasm project directly — there's no server inside it.

## What you'll see

Identical to Blazor.Server.Demo: sign in with `demo / demo`, then render `FormPage ProgId="Employee"`.

## Differences from the Server version

| Aspect | Blazor.Server.Demo | Blazor.Wasm.Demo |
|--------|--------------------|------------------|
| Component execution location | ASP.NET Core server | User's browser |
| BO dispatch | `LocalApiProvider` (in-process) | `RemoteApiProvider` (HTTP /api) |
| `AddBeeBlazor` option | `UseLocalProvider()` | `UseRemoteProvider(endpoint)` |
| Component library | `Bee.Web.Blazor.Server` | `Bee.Web.Blazor.Wasm` |
| Component source differences | None (DynamicForm / FormPage signatures match) | None |

**Element-for-element parity**: every interaction after login returns the same `DataSet` and behaves the same way on both sides — that's the whole point of "one FormSchema, many front-ends".
