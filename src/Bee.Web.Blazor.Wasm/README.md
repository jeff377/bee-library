# Bee.Web.Blazor.Wasm

> Blazor WebAssembly component library for Bee.NET — FormSchema-driven UI components running in the browser's .NET WebAssembly runtime.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Web Frontend (Razor Class Library)
- **Hosting model**: Blazor WebAssembly — components execute in the browser's .NET WASM runtime; backend calls go over HTTP.
- **Provider binding**: pairs with `RemoteApiProvider` from `Bee.Api.Client`.
- **Upstream**: `Bee.Api.Client`
- **Downstream**: Blazor WASM host applications.

## Target Framework

- `net10.0` — building or hosting requires the `wasm-tools` workload.

## Status

CRUD UI shipped:

- `FormDataObject` derives an in-memory `DataSet` (master row + detail tables) from `FormSchema` and exposes `GetField` / `SetField` for two-way binding.
- `DynamicForm` renders the master section(s) of a `FormLayout`, dispatching each field to the input element appropriate to its `ControlType` (text / date / month / checkbox / textarea / dropdown).
- Round-trip server methods (`LoadAsync` / `SaveAsync` / `DeleteAsync` / `NewAsync`) are fully implemented, calling the backend BO through the API connector.
- `DynamicGrid` (list view) and `FormPage` (list + master-detail wired via a shared `FormDataObject`) are implemented.

See [docs/plans/plan-blazor-web-integration.md](../../docs/plans/plan-blazor-web-integration.md) for the full design.

## Dependency Constraints

**Must not depend on any backend project** (Repository / Business / Hosting, etc.) — the browser runtime cannot load server-only assemblies. The constraint is enforced by the dependency chain: `Bee.Api.Client → Bee.Api.Core → Bee.Api.Contracts/Definition` are all pure data/protocol layers.

## License

MIT
