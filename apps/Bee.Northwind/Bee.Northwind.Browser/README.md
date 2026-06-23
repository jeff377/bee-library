# Bee.Northwind.Browser

The **web (WASM) head** of the [Bee.Northwind](../README.md) demo. It compiles the shared
`Bee.Northwind.UI` Avalonia application to WebAssembly with the **Avalonia Browser** backend
and runs it in a browser — the same `App`, view models and views as
[`Bee.Northwind.Desktop`](../Bee.Northwind.Desktop), just a different platform head
(`.UseBrowser()` instead of `.UseDesktop()`). It is a thin JSON-RPC client; the backend is the
unchanged [`Bee.Northwind.Server`](../Bee.Northwind.Server).

## Prerequisites

The .NET 10 SDK plus the **WebAssembly tools** workload (one-time):

```bash
sudo dotnet workload install wasm-tools
```

## Running (development)

Two terminals from the repository root:

```bash
# 1. Backend (JSON-RPC on http://localhost:5100). Dev-only CORS lets the WASM
#    dev server call it cross-origin.
dotnet run --project apps/Bee.Northwind/Bee.Northwind.Server

# 2. Web client dev server (Avalonia WASM on http://localhost:5200)
dotnet run --project apps/Bee.Northwind/Bee.Northwind.Browser
```

Open <http://localhost:5200/>, then **Connect** (endpoint pre-filled with
`http://localhost:5100/api`) → **Sign in** with `demo` / `demo`.

Because the dev server (`5200`) and the API (`5100`) are different origins, the server enables
a development-only CORS policy (`BeeDevWasm`, gated by `IsDevelopment()`) that allows any
`localhost` origin. A production deployment should serve the published WASM **same-origin** from
the API host and drop that policy.

## WASM-specific wiring

Single-threaded `browser-wasm` forces a few choices that the desktop head does not need; each is
commented at its source:

| Concern | Why | Where |
|---------|-----|-------|
| Endpoint persistence via `localStorage` | the browser sandbox cannot write files (`FileEndpointStorage` is desktop-only) | `Storage/BrowserLocalStorageEndpointStorage.cs` |
| `JsonSerializerIsReflectionEnabledByDefault=true` | browser-wasm disables System.Text.Json reflection by default; Bee's `JsonCodec` is reflection-based | `Bee.Northwind.Browser.csproj` |
| Async connect / define load | sync-over-async (`SyncExecutor`) throws *"Cannot wait on monitors"* on the single thread — use `ClientInfo.InitializeAsync` / `connector.GetDefineAsync` | `ClientInfo`, `FormsViewModel` |
| Overlay dialogs instead of `Window` | there are no native windows — lookup / row-edit dialogs render on the `OverlayLayer` | `Bee.UI.Avalonia` `OverlayDialogHost` |

## Release / publish

```bash
dotnet publish apps/Bee.Northwind/Bee.Northwind.Browser -c Release -o <out>
```

The project sets `<PublishTrimmed>false</PublishTrimmed>`: Bee (de)serializes definition and
message types by reflection throughout (System.Text.Json, XmlSerializer, MessagePack,
`TypeDescriptor`, `Assembly.GetType`), none of it source-generated, so IL trimming both fails
analysis (`IL2026` under `TreatWarningsAsErrors`) and would strip metadata the reflection paths
need at runtime. Disabling trimming trades bundle size (~16 MB gzip) for correctness. Making the
bundle trim-safe needs source-generated serializers — a framework-wide effort tracked separately.

The published `wwwroot/` is a static bundle; serve it from any static host (or, for a same-origin
deployment, from `Bee.Northwind.Server`).
