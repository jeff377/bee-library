# Bee.NET — Samples

**English** | [繁體中文](README.zh-TW.md)

A collection of minimal, runnable Bee.NET demos. Each demo has a single focus and consumes the libraries via `ProjectReference` directly from `src/` (no NuGet round-trip) — changes to library code are immediately reflected.

> Solution: [`samples/Bee.Samples.slnx`](Bee.Samples.slnx) (kept separate from the main `Bee.Library.slnx` so it never weighs down CI or main-solution build time).

## See Bee running in 30 seconds

```bash
# Terminal 1 — start the JSON-RPC API host
cd samples/QuickStart.Server
dotnet run                          # listens on http://localhost:5050

# Terminal 2 — connect and invoke the Echo BO
cd samples/QuickStart.Console
dotnet run
```

You should see `response : echo: hello from QuickStart.Console` in Terminal 2.

To watch Blazor components render a `FormSchema` and drive a Login + Employee CRUD flow:

```bash
# Option A — Blazor Server (in-process LocalApiProvider, no HTTP round-trip)
cd samples/Blazor.Server.Demo
dotnet run                          # → http://localhost:5055

# Option B — Blazor Wasm (same components, but in-browser + HTTP /api)
cd samples/Blazor.Wasm.Demo.Host
dotnet run                          # → http://localhost:5070
```

Both sign in with **`demo / demo`** and render the same `Employee` FormSchema.

## Where should I start?

| I want to learn… | Look at |
|------------------|---------|
| How to spin up a Bee backend, register a custom BO, and expose the JSON-RPC API | [`QuickStart.Server`](QuickStart.Server/README.md) |
| How to call Bee from a third-party client with `Bee.Api.Client` (Remote mode) | [`QuickStart.Console`](QuickStart.Console/README.md) |
| How to use `Bee.Web.Blazor.Server` components (Local dispatch — best perf) | [`Blazor.Server.Demo`](Blazor.Server.Demo/README.md) |
| How to use `Bee.Web.Blazor.Wasm` components (.NET running in the browser, HTTP only) | [`Blazor.Wasm.Demo`](Blazor.Wasm.Demo/README.md) + [`.Host`](Blazor.Wasm.Demo.Host/README.md) |
| How the same `FormSchema` renders inside a native mobile app (Mac Catalyst / iOS / Android) | [`Maui.Demo`](Maui.Demo/README.md) |
| How the same `FormSchema` renders inside a desktop Avalonia app (Windows / macOS / Linux) | [`Avalonia.Demo`](Avalonia.Demo/README.md) |
| Style parity of the field editors / `GridControl` against native Avalonia controls, plus both grid editing modes | [`Avalonia.Editors.Gallery`](Avalonia.Editors.Gallery/README.md) |
| How to call Bee from pure JavaScript (no .NET on the client, Plain wire format) | [`Web.Js.Demo`](Web.Js.Demo/README.md) |
| Login, AccessToken, and encrypted-payload fallback resolution on the client | `Blazor.Server.Demo` or `Maui.Demo` (either) |

## Demo catalog

| Project | Role | Default port | Launch | Library focus |
|---------|------|--------------|--------|---------------|
| [`QuickStart.Server`](QuickStart.Server/README.md) | API host | `5050` | `dotnet run` | Bee.Api.AspNetCore + Bee.Hosting + Bee.Business + Bee.Db |
| [`QuickStart.Console`](QuickStart.Console/README.md) | API client | — | `dotnet run` | Bee.Api.Client |
| [`Blazor.Server.Demo`](Blazor.Server.Demo/README.md) | Full-stack Blazor Server | `5055` | `dotnet run` | Bee.Web.Blazor.Server + Bee.Samples.Shared |
| [`Blazor.Wasm.Demo`](Blazor.Wasm.Demo/README.md) | In-browser Wasm components | — | (launched via `.Host`) | Bee.Web.Blazor.Wasm |
| [`Blazor.Wasm.Demo.Host`](Blazor.Wasm.Demo.Host/README.md) | Wasm static files + API host | `5070` | `dotnet run` | Bee.Api.AspNetCore + Bee.Web.Blazor.Wasm |
| [`Maui.Demo`](Maui.Demo/README.md) | Native mobile-app client | — (talks to 5050) | `dotnet build -t:Run -c Debug -f net10.0-maccatalyst` | Bee.UI.Maui + Bee.Api.Client |
| [`Avalonia.Demo`](Avalonia.Demo/README.md) | Desktop Avalonia client | — (talks to 5050) | `dotnet run -c Debug` | Bee.UI.Avalonia + Bee.Api.Client |
| [`Avalonia.Editors.Gallery`](Avalonia.Editors.Gallery/README.md) | Desktop Avalonia control gallery | — (no backend) | `dotnet run -c Debug` | Bee.UI.Avalonia |
| [`Web.Js.Demo`](Web.Js.Demo/README.md) | Pure-JS browser client | — (talks to 5050) | `open index.html` | (no .NET — vanilla HTML/JS) |
| [`Bee.Samples.Shared`](Bee.Samples.Shared/) | Shared backend wiring | — | (consumed by other demos) | Bee.Business + Bee.Db + Bee.Hosting + Bee.Api.Client |

### Inter-demo dependencies

```
QuickStart.Console ──HTTP──▶ QuickStart.Server
                              (also Maui.Demo's default backend)

Maui.Demo          ──HTTP──▶ QuickStart.Server  ← must be started first
Avalonia.Demo      ──HTTP──▶ QuickStart.Server  ← must be started first
Web.Js.Demo        ──HTTP──▶ QuickStart.Server  ← must be started first (CORS enabled)

Blazor.Wasm.Demo   ◀──static files── Blazor.Wasm.Demo.Host
                                  (host bundles the Bee backend and /api endpoint)

Blazor.Server.Demo                ← no separate server; front-end and back-end share the process
```

## Shared credentials

The Blazor / MAUI demos all sign in with `demo / demo`:

| Field | Value |
|-------|-------|
| User ID | `demo` |
| Password | `demo` |
| Display name | `Demo User` |

These are matched in [`DemoAuthenticatingSystemBusinessObject`](Bee.Samples.Shared/DemoAuthenticatingSystemBusinessObject.cs) with a hard-coded comparison — no `st_user` lookup, so **no system tables need to be seeded**.

`QuickStart.Server`'s `Echo.Echo` BO is annotated `[ApiAccessControl(Public, Anonymous)]`, so `QuickStart.Console` **needs no login**.

## Shared Define directory

[`samples/Define/`](Define/) is the shared definition directory used by every demo. Each host locates it by walking up from `AppContext.BaseDirectory` looking for `Define/SystemSettings.xml` (see [`DemoBackend.ResolveDefinePath`](Bee.Samples.Shared/DemoBackend.cs)), guaranteeing that "one set of definitions drives multiple front-ends".

```
Define/
├── SystemSettings.xml                       # System settings (IsDebugMode=true; MasterKeySource=Environment)
├── DbCategorySettings.xml                   # One "common" category
├── DatabaseSettings.xml                     # SQLite local DB (quickstart.db)
├── FormSchema/
│   └── Employee.FormSchema.xml              # Master-detail demo (employee + employee phones)
└── TableSchema/
    └── common/
        ├── ft_employee.TableSchema.xml
        └── ft_employee_phone.TableSchema.xml
```

## Master key

`SystemSettings.xml` ships with `MasterKeySource.Type = Environment` and `Value = BEE_MASTER_KEY`, so each demo host reads the encryption master key from the environment. [`DemoBackend.AddBeeBackend`](Bee.Samples.Shared/DemoBackend.cs) injects a fixed demo value (`DemoCredentials.DemoMasterKey`) when `BEE_MASTER_KEY` is unset, so a fresh clone runs with zero setup and `quickstart.db` rows encrypted on one run keep decrypting on the next.

> **Production hosts must override the demo master key.** The demo constant is committed to source and intended only for demos. Set `BEE_MASTER_KEY` from a deployment-managed secret (K8s Secret, env file, Vault, AWS Secrets Manager, …) **before** the process starts — the bootstrap only fills the variable when it is unset, so any externally injected value is preserved.

## Files generated on first run

The files below are **not** in git — they are runtime artifacts. A fresh clone will create them on the first `dotnet run`:

| File | Created by | Contents | gitignore rule |
|------|------------|----------|----------------|
| `samples/<Host>/quickstart.db` | [`DemoSchemaSeeder`](Bee.Samples.Shared/DemoSchemaSeeder.cs) | SQLite with `ft_employee` + `ft_employee_phone` and 3 demo rows (Alice / Bob / Carol) | `/samples/**/*.db` |

> The three hosts (`QuickStart.Server` / `Blazor.Server.Demo` / `Blazor.Wasm.Demo.Host`) **each get their own `quickstart.db`** and don't interfere with each other. Re-running the same host reuses existing data (both schema creation and seeding are idempotent).

To reset demo data: delete `samples/<Host>/quickstart.db` and re-run. To rotate the demo master key: change `DemoCredentials.DemoMasterKey` (or set `BEE_MASTER_KEY` to a different value externally) **and** delete every `quickstart.db` — existing rows are encrypted with the old key and would yield decryption failures otherwise.

## Local vs Remote dispatch

`Bee.Api.Client` exposes a **uniform API surface** to callers; only the underlying provider differs:

| Mode | Path | Used by | Sample demo |
|------|------|---------|-------------|
| **Local** | client → `LocalApiProvider` → `JsonRpcExecutor` → BO (same process) | Blazor Server, in-process tooling, BO-to-BO calls | `Blazor.Server.Demo` |
| **Remote** | client → `RemoteApiProvider` → HTTP POST → `ApiServiceController` → `JsonRpcExecutor` → BO | Blazor Wasm, Console, MAUI, cross-machine | `QuickStart.Console`, `Blazor.Wasm.Demo`, `Maui.Demo` |

Switching modes is a one-liner in `AddBeeBlazor` / `ApiClientInfo`:

```csharp
// Local
builder.Services.AddBeeBlazor(o => o.UseLocalProvider());

// Remote
builder.Services.AddBeeBlazor(o => o.UseRemoteProvider("http://host:5070/api"));
```

## Build all samples

```bash
dotnet build samples/Bee.Samples.slnx
```

> Neither `./test.sh` nor the main `Bee.Library.slnx` touch the samples directory; the samples are always "try-when-you-want" rather than CI-validated.

## FAQ

**Q: Port 5050 / 5055 / 5070 is already in use — what now?**
Edit `samples/<Host>/Properties/launchSettings.json` and change `applicationUrl`. Don't forget to update anything that points at that host: the `--endpoint` flag for `QuickStart.Console`, the endpoint field in `Maui.Demo`, and `MauiProgram.DefaultEndpoint`.

**Q: I'm getting `Could not locate 'Define/SystemSettings.xml' walking up from ...`**
Run `dotnet run` from inside the bee-library checkout. Don't copy the built binaries outside the repo — `DemoBackend` walks upward from `AppContext.BaseDirectory` looking for `Define/`, and that walk fails outside the repo.

**Q: Why does the MAUI demo have to run in Debug?**
On Apple platforms the Release-mode Mono linker strips the `System.Xml.Serialization` reflection fallback, breaking `FormSchema` deserialization. See [`Maui.Demo/README.md`](Maui.Demo/README.md) for details.

**Q: Can I run all three hosts at the same time without conflicts?**
Yes. The three hosts listen on different ports (5050 / 5055 / 5070), each has its own `quickstart.db`, and they share `samples/Define/` read-only. Running all three plus the Console and MAUI demos in parallel is fully supported.

**Q: I edited code under `src/` — how do I see it in the demos?**
Just re-run. `ProjectReference` rebuilds automatically. No `dotnet pack` and no cache flushing required.

## Deliberately out of scope

- Realistic ERP scenarios (sales orders, purchase orders, etc.) — left for a future standalone demo repo
- SQL Server / PostgreSQL / Oracle / MySQL — SQLite is enough for demonstration
- Full auth/authz flows (OAuth, JWT, an actual `st_user` table) — short-circuited with hard-coded `demo/demo`
- Deployment scripts (Docker / k8s / TestFlight / Microsoft Store)
- CI validation for samples — manual runs only, except that `Maui.Demo/.smoke.yaml` can be smoke-tested by the `demo-smoke` skill
