# Blazor.Wasm.Demo.Host

**English** | [繁體中文](README.zh-TW.md)

An ASP.NET Core host that serves **both** (1) the Bee backend JSON-RPC `/api` endpoint and (2) the `Blazor.Wasm.Demo` client's static files (`_framework/blazor.webassembly.js` + WASM dlls) — all from a single process.

When the browser hits `/`:

1. The host returns `index.html` (which contains `<script src="_framework/blazor.webassembly.js">`)
2. The WASM runtime downloads, then executes `Blazor.Wasm.Demo`'s `Program.cs`
3. `AddBeeBlazor(UseRemoteProvider($"{BaseAddress}api"))` points the endpoint at itself (same origin)
4. `BeeLoginPanel` → POST `/api` → Bee `JsonRpcExecutor` → the in-process `DemoAuthenticatingSystemBusinessObject`
5. After login, `FormPage` → POST `/api` → `FormBusinessObject` → SQLite

## How to run

```bash
cd samples/Blazor.Wasm.Demo.Host
dotnet run
# Browser opens http://localhost:5060 automatically
```

On first run:

1. Auto-generates `samples/Define/Master.key` if missing
2. Creates `samples/Blazor.Wasm.Demo.Host/quickstart.db` (SQLite) with `ft_employee` + `ft_employee_phone`
3. Seeds 3 demo employees

## What this maps to in the library

| Demo behavior | Library component |
|---------------|-------------------|
| Bee backend in-process registration | `AddBeeFramework` (Bee.Hosting) |
| JSON-RPC `/api` endpoint | `ApiServiceController` (Bee.Api.AspNetCore) |
| Serving WASM static files | ASP.NET Core `UseBlazorFrameworkFiles` |
| Login customization | `DemoAuthenticatingSystemBusinessObject` + `DemoBusinessObjectFactory` (Bee.Samples.Shared) |
| Employee CRUD | `FormBusinessObject` + `FormSchema` + `FormRepositoryFactory` |

> Compared to `QuickStart.Server`: that one only ships an anonymous Echo BO. This host adds WASM static-file serving and the Employee schema seed on top. The two hosts can run side-by-side on ports 5050 and 5060 without interfering with each other.
