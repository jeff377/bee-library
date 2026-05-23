# Maui.Demo

**English** | [繁體中文](README.zh-TW.md)

A Bee.NET sample: a MAUI client that talks to the JSON-RPC API hosted by `samples/QuickStart.Server`, rendering the shared `Define/FormSchema/Employee.FormSchema.xml`. Maps to [plan-samples-structure.md](../../docs/plans/plan-samples-structure.md) P2 and [plan-maui-integration.md](../../docs/plans/plan-maui-integration.md) Phase 2.

## What it proves

- The same `FormSchema` renders across all three front-ends — Blazor Server, Blazor Wasm, and MAUI (sharing `samples/Define/` with `Blazor.Server.Demo` / `Blazor.Wasm.Demo`)
- When the host passes only `ProgId`, `Bee.UI.Maui.Controls.FormPage` automatically resolves connector / schema / access token through `Bee.UI.Core.ClientInfo` (the Phase 1d fallback behavior)
- A MAUI app over `ConnectType.Remote` walks the same JSON-RPC wire path as Blazor.Wasm

## Prerequisites (macOS)

1. **MAUI workloads**:

   ```
   sudo dotnet workload install maui
   ```

   After installing, `dotnet workload list` should additionally show `maui-ios`, `maui-maccatalyst`, `maui-tizen` (alongside the existing `maui-android`).

2. **Xcode**: Mac Catalyst builds require Xcode (not just the command-line tools). `xcode-select -p` should point at `/Applications/Xcode.app/Contents/Developer`.

3. **Backend**: in another terminal run

   ```
   cd samples/QuickStart.Server
   dotnet run
   ```

   The server listens on `http://localhost:5050` by default. `QuickStart.Server` is already wired through `Bee.Samples.Shared.DemoBackend` with `DemoAuthenticatingSystemBusinessObject`; the first run will auto-create the SQLite file and seed 3 Employee rows.

## Launch the demo

```
cd samples/Maui.Demo
dotnet build -t:Run -c Debug -f net10.0-maccatalyst
```

**Run with `-c Debug`**. On Apple platforms, the Release-mode Mono linker strips the `System.Xml.Serialization` reflection fallback (Sgen), causing `Bee.Api.Client` to fail `FormSchema` deserialization with `XmlSerializeErrorDetails, 2, 2`. Debug doesn't trim, so it just works. To eventually ship a true Release build (App Store / TestFlight), you'd need one of:

- Adding `Microsoft.XmlSerializer.Generator` to pre-emit the Sgen assembly
- Annotating `Bee.Definition` types with `DynamicallyAccessedMembers`
- Adding a `TrimmerRootDescriptor` to keep `System.Xml.Serialization` entry points

The default is Mac Catalyst only (`TargetFrameworks` defaults to `net10.0-maccatalyst`). To build iOS / Android etc., pass `-p:MauiDemoFullPlatforms=true` after installing the corresponding workloads.

## What you'll see

1. **Connect page**: endpoint defaults to `http://localhost:5050/api`. Tapping **Connect** calls `ClientInfo.Initialize(endpoint)`, which runs an HTTP reachability check plus `system.ping` via `ApiConnectValidator`. On success it advances to Login.
2. **Login page**: two entry fields, pre-filled with `demo` / `demo` (from `Bee.Samples.Shared.DemoCredentials`). Tapping **Sign in** calls `SystemApiConnector.LoginAsync`; the token is stored through `ClientInfo.ApplyLoginResult`.
3. **Employee page**: `<FormPage ProgId="Employee" />`. The MAUI side passes neither `Schema` nor `FormConnector`; the Phase 1d fallback pulls them from `ClientInfo`:
   - `ClientInfo.SystemApiConnector.GetDefineAsync<FormSchema>(DefineType.FormSchema, ["Employee"])`
   - `ClientInfo.CreateFormApiConnector("Employee")`
   - `ClientInfo.AccessToken`

   After rendering: the top `DynamicGrid` lists the three seeded rows (Alice / Bob / Carol); selecting a row opens the `DynamicForm` below; New / Save / Delete round-trip through the BO back to the server.

## What this maps to in the library

| Demo behavior | Library component |
|---------------|-------------------|
| Connect → endpoint validation | [src/Bee.UI.Core/ClientInfo.cs](../../src/Bee.UI.Core/ClientInfo.cs) + [src/Bee.Api.Client/ApiConnectValidator.cs](../../src/Bee.Api.Client/ApiConnectValidator.cs) |
| Login → token | [src/Bee.Api.Client/Connectors/SystemApiConnector.cs](../../src/Bee.Api.Client/Connectors/SystemApiConnector.cs) (LoginAsync) |
| Employee form rendering | [src/Bee.UI.Maui/Controls/FormPage.cs](../../src/Bee.UI.Maui/Controls/FormPage.cs) → DynamicForm / DynamicGrid |
| FormSchema fallback | Phase 1d `ResolveSystemConnector` / `ResolveFormConnector` / `ResolveAccessToken` |
| CRUD wire path | FormApiConnector → RemoteApiProvider → QuickStart.Server `ApiController` → DemoBusinessObjectFactory → FormBusinessObject |

## Relationship to the Blazor demos

`QuickStart.Server` is itself a `Bee.Samples.Shared.DemoBackend` host. Alongside `Blazor.Wasm.Demo.Host`, the two are independent hosts that share the same `DemoBackend` code. `Blazor.Server.Demo` uses the in-process Local provider and doesn't need `QuickStart.Server` at all.

`Maui.Demo` is structurally symmetric to `Blazor.Wasm.Demo` (both go `ConnectType.Remote` to a server); the only code difference is the UI family (MAUI ContentPage vs Razor component).

## Deliberately out of scope

- Multiple detail tables / custom widget extensions
- Real deployment (codesign, TestFlight, Microsoft Store) — local-only for the first cut
- Automated sample CI validation — purely manual runs
- A full fix for Apple Release-mode trimming (would require `Microsoft.XmlSerializer.Generator` or `DynamicallyAccessedMembers` annotations) — deferred until the demo actually needs to ship
