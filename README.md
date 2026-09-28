# Bee.NET Framework

[繁體中文](README.zh-TW.md)

> [!IMPORTANT]
> **This repository is no longer maintained.** Bee.NET continues as
> [Polhem](https://github.com/polhem-dev/polhem), and every `Bee.*` package is renamed to `Polhem.*`. See
> [Migrating from Bee.NET](https://github.com/polhem-dev/polhem#migrating-from-beenet) for what changed.
> `Bee.*` 4.33.0 is the last release; all versions are deprecated on NuGet.
>
> **本 repo 已停止維護。** Bee.NET 由 [Polhem](https://github.com/polhem-dev/polhem) 接續，所有 `Bee.*` 套件改名為 `Polhem.*`，
> 變更內容見[從 Bee.NET 遷移](https://github.com/polhem-dev/polhem/blob/main/README.zh-TW.md#從-beenet-遷移)。`Bee.*` 最後一版為 4.33.0，NuGet 上的所有版本都已標為 deprecated。

| Bee.NET package | Replacement |
|-----------------|-------------|
| `Bee.Base` | [`Polhem.Base`](https://www.nuget.org/packages/Polhem.Base) |
| `Bee.Expressions` | [`Polhem.Expressions`](https://www.nuget.org/packages/Polhem.Expressions) |
| `Bee.Definition` | [`Polhem.Definition`](https://www.nuget.org/packages/Polhem.Definition) |
| `Bee.ObjectCaching` | [`Polhem.ObjectCaching`](https://www.nuget.org/packages/Polhem.ObjectCaching) |
| `Bee.Db` | [`Polhem.Db`](https://www.nuget.org/packages/Polhem.Db) |
| `Bee.Api.Contracts` | [`Polhem.Api.Contracts`](https://www.nuget.org/packages/Polhem.Api.Contracts) |
| `Bee.Business` | [`Polhem.Business`](https://www.nuget.org/packages/Polhem.Business) |
| `Bee.Api.Core` | [`Polhem.Api.Core`](https://www.nuget.org/packages/Polhem.Api.Core) |
| `Bee.Api.Client` | [`Polhem.Api.Client`](https://www.nuget.org/packages/Polhem.Api.Client) |
| `Bee.UI.Core` | [`Polhem.UI.Core`](https://www.nuget.org/packages/Polhem.UI.Core) |
| `Bee.UI.Avalonia` | [`Polhem.UI.Avalonia`](https://www.nuget.org/packages/Polhem.UI.Avalonia) |
| `Bee.Repository.Abstractions` | [`Polhem.Repository.Abstractions`](https://www.nuget.org/packages/Polhem.Repository.Abstractions) |
| `Bee.Repository` | [`Polhem.Repository`](https://www.nuget.org/packages/Polhem.Repository) |
| `Bee.Hosting` | [`Polhem.Hosting`](https://www.nuget.org/packages/Polhem.Hosting) |
| `Bee.Api.AspNetCore` | [`Polhem.Api.AspNetCore`](https://www.nuget.org/packages/Polhem.Api.AspNetCore) |
| `Bee.Web.Blazor.Server` | [`Polhem.Web.Blazor.Server`](https://www.nuget.org/packages/Polhem.Web.Blazor.Server) |
| `Bee.Cli` | [`Polhem.Cli`](https://www.nuget.org/packages/Polhem.Cli) |

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
- **N-Tier + Clean Architecture + MVVM**: Clear separation of presentation, API, business logic (BO), and data access layers, borrowing the best concepts from each pattern for enterprise information systems.
- **Cross-platform compatibility**: All packages target `net10.0` for modern .NET runtime support.
- **Multi-database support**: Built-in dialects for SQL Server, PostgreSQL, SQLite, MySQL, and Oracle; host applications register only what they use.
- **Modular components**: Decoupled libraries for core utilities, data, caching, business logic, and API hosting.
- **Rapid development**: Reusable base classes and FormSchema-driven CRUD reduce repetitive boilerplate.
- **Conventions enforced at build time**: Roslyn analyzers ship with the packages and register automatically, turning framework conventions — database scope selection, cross-file definition consistency, wire contract shape — into build diagnostics that name both the cause and the fix. See [Analyzer Rules](docs/en/analyzer-rules.md).

## 📐 Architecture

For an in-depth look at the layered architecture, data flow, and design decisions behind Bee.NET, see the [Architecture Overview](docs/en/architecture-overview.md).

For guidelines on API Contract and BO Parameter design (Request/Response vs Args/Result), see the [API/BO Contract Design Principles](docs/en/api-bo-contract-design.md). The full catalog of public API methods, with each method's `[ApiAccessControl]` settings, lives in the [API Method Reference](docs/en/api-method-reference.md).

For calling the JSON-RPC API from a JavaScript / TypeScript frontend (React, Vue, Angular, vanilla — no .NET on the client), see the [JSON-RPC Frontend Integration Guide](docs/en/jsonrpc-frontend-integration.md).

For the full developer documentation index, see [docs/en/README.md](docs/en/README.md).

## 📦 Assembly

### Shared (Frontend / Backend)

| Assembly Name | Description |
|---|---|
| **Bee.Base.dll** | Core utilities such as serialization, encryption, and general-purpose helpers. |
| **Bee.Definition.dll** | Defines system-wide structured types including FormSchema, field schemas, and layout configurations. |
| **Bee.Expressions.dll** | Portable, sandboxed expression evaluator (DynamicExpresso-backed) for computed fields and validation rules; shared by backend save and Avalonia client live preview so both sides compute identically. |
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
| **Bee.UI.Avalonia.dll** | Avalonia desktop control library (Windows / macOS / Linux); ships FormSchema-driven controls (`FormView` / `ListView` / `GridControl` plus a field-editor family with `FormScope` ambient binding, all backed by `FormDataObject`) plus a file-backed `FileEndpointStorage`. Single `net10.0` TFM; Avalonia 12.0.0 + DataGrid 12.0.0 as lower bound. |
| **Bee.Web.Blazor.Server.dll** | Razor Class Library (RCL) for Blazor Server hosts; provides DI-scoped connectors and Blazor components (`DynamicForm`, `FormDataObject`). |

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

Ready to build your own? [Getting Started](docs/en/getting-started.md) walks through the same thing from an empty folder — packages, `DefinePath`, DI wiring, your first business object, and calling it from a client.

## 🐝 Featured demo — Bee.Northwind

[`apps/Bee.Northwind`](apps/Bee.Northwind/README.md) is the flagship demo: the classic Northwind inventory case built almost entirely from definitions (eight forms, master-detail orders with lookups, exactly one hand-written business object — everything else is XML). The same shared `Bee.Northwind.UI` runs on **four Avalonia heads** — Desktop, Browser (WASM), iOS, and Android — against one JSON-RPC server.

The same Order form rendered by each head — same definitions, same controls, only the platform shell differs:

| Desktop | Browser (WASM) |
|---|---|
| <img src="https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-desktop-order-detail.png" alt="Desktop — order detail" width="420"> | <img src="https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-browser-order-detail.png" alt="Browser — order detail" width="420"> |

| iOS | Android |
|---|---|
| <img src="https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-ios-order-detail.png" alt="iOS — order detail" width="200"> | <img src="https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-android-order-detail.png" alt="Android — order detail" width="200"> |

More screens, the form catalog, and how to run it: [`apps/Bee.Northwind/README.md`](apps/Bee.Northwind/README.md).

## 💡 Sample Projects

All demos live in-repo under [`samples/`](samples/README.md). They're minimal, focused, and evolve alongside the framework. Build them with `dotnet build samples/Bee.Samples.slnx` (kept separate from the main `Bee.Library.slnx`, so the main CI/build stays unaffected).

| Category | Demo | Shows |
|----------|------|-------|
| QuickStart | [`QuickStart.Server`](samples/QuickStart.Server/README.md) + [`QuickStart.Console`](samples/QuickStart.Console/README.md) | Minimal JSON-RPC end-to-end with a custom anonymous BO |
| Blazor Server | [`Blazor.Server.Demo`](samples/Blazor.Server.Demo/README.md) | `BeeLoginPanel` + `FormPage` + Employee CRUD, dispatched in-process via `LocalApiProvider` |
| Avalonia | [`Avalonia.DemoCenter`](samples/Avalonia.DemoCenter/README.md) | Theme-oriented control demo center (DevExpress-style): nav tree (theme → case) + Demo/Source tabs + theme/FormMode toolbar; covers data binding, read-only/required, FormMode, layout, grid, native-vs-inherited parity (Semi.Avalonia, no backend) |
| Pure JS | [`Web.Js.Demo`](samples/Web.Js.Demo/README.md) | Calling the JSON-RPC API from vanilla JavaScript in a browser — no .NET on the client, no npm |


## 📬 Contact & Follow
You're welcome to follow my technical notes and hands-on experience sharing

[Facebook](https://www.facebook.com/profile.php?id=61574839666569) ｜ [HackMD](https://hackmd.io/@jeff377) ｜ [GitHub](https://github.com/jeff377) ｜ [NuGet](https://www.nuget.org/profiles/jeff377)
