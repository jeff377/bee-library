# Bee.Web.Blazor.Server

> Blazor Server component library for Bee.NET — FormSchema-driven UI components running in the ASP.NET Core host process.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Web Frontend (Razor Class Library)
- **Hosting model**: Blazor Server — component logic executes on the ASP.NET Core server; the browser receives DOM diffs via SignalR.
- **Provider binding**: pairs with `LocalApiProvider` from `Bee.Api.Client` (in-process call, no HTTP round-trip).
- **Position in the dependency graph**: see [Project Dependency Map](../../docs/dependency-map.md). Not enumerated here — the csproj files are the authority, and a prose copy in every package README drifts with nothing to catch it. These did: `Bee.Hosting` was missing as a dependent from four of them for months after it was extracted.
- Consumed by ASP.NET Core host applications.

## Target Framework

- `net10.0`

## Status

CRUD UI shipped:

- `FormDataObject` derives an in-memory `DataSet` (master row + detail tables) from `FormSchema` and exposes `GetField` / `SetField` for two-way binding.
- `DynamicForm` renders the master section(s) of a `FormLayout`, dispatching each field to the input element appropriate to its `ControlType` (text / date / month / checkbox / textarea / dropdown).
- Round-trip server methods (`LoadAsync` / `SaveAsync` / `DeleteAsync` / `NewAsync`) are fully implemented, calling the backend BO through the API connector.
- `DynamicGrid` (list view) and `FormPage` (list + master-detail wired via a shared `FormDataObject`) are implemented.

## Dependency Constraints

Depends only on `Bee.Api.Client`. The host application is responsible for registering backend services via `AddBeeFramework` and choosing the `IJsonRpcProvider` implementation.

## License

MIT
