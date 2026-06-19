# Bee.NET Framework

[繁體中文](README.zh-TW.md)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=jeff377_bee-library&metric=alert_status)](https://sonarcloud.io/project/overview?id=jeff377_bee-library)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=jeff377_bee-library&metric=bugs)](https://sonarcloud.io/project/overview?id=jeff377_bee-library)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=jeff377_bee-library&metric=vulnerabilities)](https://sonarcloud.io/project/overview?id=jeff377_bee-library)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=jeff377_bee-library&metric=code_smells)](https://sonarcloud.io/project/overview?id=jeff377_bee-library)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=jeff377_bee-library&metric=coverage)](https://sonarcloud.io/project/overview?id=jeff377_bee-library)

Bee.NET Framework is an **N-Tier + Clean Architecture + MVVM** hybrid designed to accelerate the development of enterprise information systems. It adopts a **Definition-Driven Architecture**, using `FormSchema` as the single source of truth to drive UI layout, database schema, and business validation in a unified way.

> 📌 *N-tier* means the architecture is divided into more than three logical layers. In Bee.NET, the system is separated into at least five layers: presentation, API communication, business logic, data access, and database — each with a clearly defined responsibility.

All packages target **`net10.0`**.

## ✨ Features

- **Definition-Driven Architecture**: `FormSchema` serves as the single source of truth, automatically deriving UI layout (`FormLayout`), database schema (`TableSchema`), and validation rules — define once, sync everywhere.
- **N-Tier + Clean Architecture + MVVM**: Clear separation of presentation, API, business logic (BO), and data access layers, borrowing the best concepts from each pattern for ERP scenarios.
- **Cross-platform compatibility**: All packages target `net10.0` for modern .NET runtime support.
- **Multi-database support**: Built-in dialects for SQL Server, PostgreSQL, SQLite, MySQL, and Oracle; host applications register only what they use.
- **Modular components**: Decoupled libraries for core utilities, data, caching, business logic, and API hosting.
- **Rapid development**: Reusable base classes and FormSchema-driven CRUD reduce repetitive boilerplate.

## 📐 Architecture

For an in-depth look at the layered architecture, data flow, and design decisions behind Bee.NET, see the [Architecture Overview](docs/architecture-overview.md).

For guidelines on API Contract and BO Parameter design (Request/Response vs Args/Result), see the [API/BO Contract Design Principles](docs/api-bo-contract-design.md). The full catalog of public API methods, with each method's `[ApiAccessControl]` settings, lives in the [API Method Reference](docs/api-method-reference.md).

For calling the JSON-RPC API from a JavaScript / TypeScript frontend (React, Vue, Angular, vanilla — no .NET on the client), see the [JSON-RPC Frontend Integration Guide](docs/jsonrpc-frontend-integration.md).

For the full developer documentation index, see [docs/README.md](docs/README.md).

## 📦 Assembly

### Shared (Frontend / Backend)

| Assembly Name | Description |
|---|---|
| **Bee.Base.dll** | Core utilities such as serialization, encryption, and general-purpose helpers. |
| **Bee.Definition.dll** | Defines system-wide structured types including FormSchema, field schemas, and layout configurations. |
| **Bee.Api.Contracts.dll** | Shared data contracts (request/response models) used by both frontend and backend. |
| **Bee.Api.Core.dll** | Encapsulates API support such as model definitions, payload encryption, and serialization pipeline. |

### Backend

| Assembly Name | Description |
|---|---|
| **Bee.Repository.Abstractions.dll** | Interface contracts for the business layer to access the data layer; boundary between Business Object and Repository. |
| **Bee.ObjectCaching.dll** | Runtime caching of FormSchema definitions and derived system data to improve performance. |
| **Bee.Db.dll** | Database abstraction with dynamic SQL command generation and connection binding; ships dialects for SQL Server, PostgreSQL, SQLite, MySQL, and Oracle. |
| **Bee.Repository.dll** | Common repository base classes and FormSchema-driven data access mechanisms. |
| **Bee.Business.dll** | Core business logic (Business Object / BO) implementing use-case workflows. |
| **Bee.Hosting.dll** | Composition root — `AddBeeFramework` extension registering all backend services into any `IServiceCollection` (no ASP.NET Core dependency). Used by ASP.NET Core, WinForms, Console, and Worker Service hosts. |
| **Bee.Api.AspNetCore.dll** | JSON-RPC 2.0 API controller for ASP.NET Core (`UseBeeFramework` middleware + `ApiServiceController`). |

### Frontend

| Assembly Name | Description |
|---|---|
| **Bee.Api.Client.dll** | Connector for local or remote invocation of backend Business Objects (`LocalApiProvider` / `RemoteApiProvider`). |
| **Bee.UI.Core.dll** | Cross-platform UI common layer (`ClientInfo` / `IEndpointStorage` / `IUIViewService` / `VersionInfo`); shared by native UI hosts for client-side connection state and endpoint persistence. |
| **Bee.UI.Avalonia.dll** | Avalonia desktop control library (Windows / macOS / Linux); ships FormSchema-driven controls (`FormView` / `DynamicForm` / `GridControl` plus a field-editor family with `FormScope` ambient binding, all backed by `FormDataObject`) plus a file-backed `FileEndpointStorage`. Single `net10.0` TFM; Avalonia 12.0.0 + DataGrid 12.0.0 as lower bound. |
| **Bee.UI.Maui.dll** | MAUI cross-platform control library (iOS / Android / macOS / Windows); ships FormSchema-driven controls (`DynamicForm` + `FormDataObject`). Default TFM `net10.0`; platform TFMs opt-in via `-p:BeeUiMauiFullPlatforms=true`. |
| **Bee.Web.Blazor.Server.dll** | Razor Class Library (RCL) for Blazor Server hosts; provides DI-scoped connectors and Blazor components (`DynamicForm`, `FormDataObject`). |
| **Bee.Web.Blazor.Wasm.dll** | Razor Class Library (RCL) for Blazor WebAssembly hosts; forced to `RemoteApiProvider`. Must not depend on any backend project. |

### Tooling (dotnet tool)

| Package | Install | Description |
|---|---|---|
| **Bee.Cli** | `dotnet tool install -g Bee.Cli` <br/>Upgrade: `dotnet tool update -g Bee.Cli` | Framework CLI invoked as `dotnet bee`. Currently ships the `defines` subcommand group for materialising / listing the framework default define files (`st_*` TableSchema, framework-shipped FormSchema / FormLayout / Language, SystemSettings / DatabaseSettings templates). Use to bootstrap a new consumer's `DefinePath` from the embedded resources in `Bee.Definition.dll`. |


## 🚀 Quick Start

Want to see Bee.NET running in 30 seconds?

```bash
# Terminal 1 — start the JSON-RPC API host
cd samples/QuickStart.Server
dotnet run

# Terminal 2 — connect and call the Echo BO
cd samples/QuickStart.Console
dotnet run
```

The console will print `System.Ping` status and an echoed message returned from a custom BO. See [`samples/README.md`](samples/README.md) for the full demo list and what each one shows.

## 💡 Sample Projects

All demos live in-repo under [`samples/`](samples/README.md). They're minimal, focused, and evolve alongside the framework. Build them with `dotnet build samples/Bee.Samples.slnx` (kept separate from the main `Bee.Library.slnx`, so the main CI/build stays unaffected).

| Category | Demo | Shows |
|----------|------|-------|
| QuickStart | [`QuickStart.Server`](samples/QuickStart.Server/README.md) + [`QuickStart.Console`](samples/QuickStart.Console/README.md) | Minimal JSON-RPC end-to-end with a custom anonymous BO |
| Blazor Server | [`Blazor.Server.Demo`](samples/Blazor.Server.Demo/README.md) | `BeeLoginPanel` + `FormPage` + Employee CRUD, dispatched in-process via `LocalApiProvider` |
| Blazor Wasm | [`Blazor.Wasm.Demo`](samples/Blazor.Wasm.Demo/README.md) + [`.Host`](samples/Blazor.Wasm.Demo.Host/README.md) | Same Blazor components running in the browser, dispatched over HTTP via `RemoteApiProvider` |
| MAUI | [`Maui.Demo`](samples/Maui.Demo/README.md) | Native mobile-app client (Mac Catalyst / iOS / Android / Windows) rendering the same `FormSchema` |
| Avalonia | [`Avalonia.Demo`](samples/Avalonia.Demo/README.md) | Desktop Avalonia client (Windows / macOS / Linux) rendering the same `FormSchema` |
| Avalonia | [`Avalonia.DemoCenter`](samples/Avalonia.DemoCenter/README.md) | Theme-oriented control demo center (DevExpress-style): nav tree (theme → case) + Demo/Source tabs + theme/FormMode toolbar; covers data binding, read-only/required, FormMode, layout, grid, native-vs-inherited parity (Semi.Avalonia, no backend) |
| Pure JS | [`Web.Js.Demo`](samples/Web.Js.Demo/README.md) | Calling the JSON-RPC API from vanilla JavaScript in a browser — no .NET on the client, no npm |


## 📬 Contact & Follow
You're welcome to follow my technical notes and hands-on experience sharing

[Facebook](https://www.facebook.com/profile.php?id=61574839666569) ｜ [HackMD](https://hackmd.io/@jeff377) ｜ [GitHub](https://github.com/jeff377) ｜ [NuGet](https://www.nuget.org/profiles/jeff377)
