# Bee.Web.Blazor.Server

> Blazor Server component library for Bee.NET — FormSchema-driven UI components running in the ASP.NET Core host process.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Web Frontend (Razor Class Library)
- **Hosting model**: Blazor Server — component logic executes on the ASP.NET Core server; the browser receives DOM diffs via SignalR.
- **Provider binding**: pairs with `LocalApiProvider` from `Bee.Api.Client` (in-process call, no HTTP round-trip).
- **Upstream**: `Bee.Api.Client`
- **Downstream**: ASP.NET Core host applications.

## Target Framework

- `net10.0`

## Status

Project skeleton only. UI components (DynamicForm / DynamicGrid / FormPage / FormDataObject) are not yet implemented. See [docs/plans/plan-blazor-web-integration.md](../../docs/plans/plan-blazor-web-integration.md) for the full design.

## Dependency Constraints

Depends only on `Bee.Api.Client`. The host application is responsible for registering backend services via `AddBeeFramework` and choosing the `IApiProvider` implementation.

## License

MIT
