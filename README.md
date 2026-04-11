# Bee.NET Framework

[繁體中文](README.zh-TW.md)

Bee.NET Framework is an **N-Tier + Clean Architecture + MVVM** hybrid designed to accelerate the development of enterprise information systems. It adopts a **Definition-Driven Architecture**, using `FormSchema` as the single source of truth to drive UI layout, database schema, and business validation in a unified way.

> 📌 *N-tier* means the architecture is divided into more than three logical layers. In Bee.NET, the system is separated into at least five layers: presentation, API communication, business logic, data access, and database — each with a clearly defined responsibility.

Core packages target **`netstandard2.0; net10.0`** for broad .NET runtime compatibility (.NET Framework, .NET Core, .NET 5+, and beyond). The API hosting package (`Bee.Api.AspNetCore`) targets **`net10.0`**.

## ✨ Features

- **Definition-Driven Architecture**: `FormSchema` serves as the single source of truth, automatically deriving UI layout (`FormLayout`), database schema (`TableSchema`), and validation rules — define once, sync everywhere.
- **N-Tier + Clean Architecture + MVVM**: Clear separation of presentation, API, business logic (BO), and data access layers, borrowing the best concepts from each pattern for ERP scenarios.
- **Cross-platform compatibility**: Core packages target `netstandard2.0; net10.0` for broad .NET runtime support.
- **Modular components**: Decoupled libraries for core utilities, data, caching, business logic, and API hosting.
- **Rapid development**: Reusable base classes and FormSchema-driven CRUD reduce repetitive boilerplate.

## 📐 Architecture

For an in-depth look at the layered architecture, data flow, and design decisions behind Bee.NET, see the [Architecture Overview](docs/architecture-overview.md).

For guidelines on API Contract and BO Parameter design (Request/Response vs Args/Result), see the [API/BO Contract Design Principles](docs/api-bo-contract-design.md).

## 📦 Assembly

### Shared (Frontend / Backend)

| Assembly Name | Description |
|---|---|
| **Bee.Core.dll** | Core utilities such as serialization, encryption, and general-purpose helpers. |
| **Bee.Definition.dll** | Defines system-wide structured types including FormSchema, field schemas, and layout configurations. |
| **Bee.Api.Contracts.dll** | Shared data contracts (request/response models) used by both frontend and backend. |
| **Bee.Api.Core.dll** | Encapsulates API support such as model definitions, payload encryption, and serialization pipeline. |

### Backend

| Assembly Name | Description |
|---|---|
| **Bee.Repository.Abstractions.dll** | Interface contracts for the business layer to access the data layer; boundary between Business Object and Repository. |
| **Bee.ObjectCaching.dll** | Runtime caching of FormSchema definitions and derived system data to improve performance. |
| **Bee.Db.dll** | Database abstraction with dynamic SQL command generation and connection binding. |
| **Bee.Repository.dll** | Common repository base classes and FormSchema-driven data access mechanisms. |
| **Bee.Business.dll** | Core business logic (Business Object / BO) implementing use-case workflows. |
| **Bee.Api.AspNetCore.dll** | JSON-RPC 2.0 API controller for ASP.NET Core; unified endpoint for backend method dispatch. |

### Frontend

| Assembly Name | Description |
|---|---|
| **Bee.Api.Client.dll** | Connector for local or remote invocation of backend Business Objects. |


## 💡 Sample Project

Refer to [bee-jsonrpc-sample](https://github.com/jeff377/bee-jsonrpc-sample), which includes examples of JSON-RPC server and client implementations, and demonstrates how to use the Connector for both local and remote connections.


## 📬 Contact & Follow
You're welcome to follow my technical notes and hands-on experience sharing

[Facebook](https://www.facebook.com/profile.php?id=61574839666569) ｜ [HackMD](https://hackmd.io/@jeff377) ｜ [GitHub](https://github.com/jeff377) ｜ [NuGet](https://www.nuget.org/profiles/jeff377)
