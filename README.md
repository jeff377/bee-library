# Bee.NET Framework

Bee.NET Framework is an **n-tier software architecture** designed to accelerate the development of enterprise information systems. Built on **.NET Standard 2.0**, it features high modularity and cross-platform compatibility across .NET Framework, .NET Core, .NET 5+, and beyond.

> 📌 *N-tier* means the architecture is divided into more than three logical layers. In Bee.NET, the system is separated into at least five layers, including presentation, API communication, business logic, and data access.

[繁體中文](README.zh-TW.md)

## ✨ Features

- **N-tier architecture**: Clear separation of presentation, API, business logic, and data access layers for better modularity and maintainability.
- **Cross-platform compatibility**: Powered by .NET Standard 2.0 for broad .NET runtime support.
- **Modular components**: Decoupled libraries for core utilities, data, caching, business logic, and API hosting.
- **Rapid development**: Provides common reusable components to simplify system integration.

## 📦 Assembly

| Assembly Name              | Scope         | Target Framework       | Description |
|----------------------------|---------------|------------------------|-------------|
| **Bee.Base.dll**           | Frontend / Backend | netstandard2.0; net10.0  | Core utilities such as serialization, encryption, and general-purpose helpers. |
| **Bee.Define.dll**         | Frontend / Backend | netstandard2.0; net10.0  | Defines system-wide structured types for config, schema, and layout. |
| **Bee.Api.Contracts.dll**   | Frontend / Backend | netstandard2.0; net10.0  | Shared data contracts between frontend and backend. |
| **Bee.Repository.Abstractions.dll**   | Backend        | netstandard2.0; net10.0  | Defines the interface contracts for the business layer to access the data layer, serving as the boundary between the business logic layer and the data access layer. |
| **Bee.Cache.dll**          | Backend        | netstandard2.0; net10.0  | Runtime caching of definitions and related system data to improve performance. |
| **Bee.Db.dll**             | Backend        | netstandard2.0; net10.0  | Database abstraction with dynamic command generation and connection binding. |
| **Bee.Repository.dll**      | Backend        | netstandard2.0; net10.0  | Provides common repository base classes and data access mechanisms. |
| **Bee.Business.dll**       | Backend        | netstandard2.0; net10.0  | Implements core business logic and application-level workflows. |
| **Bee.Api.Core.dll**       | Frontend / Backend | netstandard2.0; net10.0 | Encapsulates API support such as model definitions, encryption, and serialization. |
| **Bee.Api.AspNetCore.dll** | Backend       | net10.0                 | Provides a JSON-RPC 2.0 API controller for ASP.NET Core, serving as a unified endpoint to handle backend method calls. |
| **Bee.Api.AspNet.dll**     | Backend       | net48     | Provides a JSON-RPC 2.0 API HttpModule for ASP.NET, enabling unified POST endpoint for backend logic. |
| **Bee.Connect.dll**        | Frontend       | netstandard2.0; net10.0  | Connector for local or remote invocation of backend logic. |
| **Bee.UI.Core.dll**        | Frontend       | netstandard2.0; net10.0  | Manages client-server connection settings and states. |
| **Bee.UI.WinForms.dll**    | Frontend       | net10.0                | UI components and layout management for WinForms. |


## 💡 Sample Project

Refer to [jsonrpc-sample](https://github.com/jeff377/jsonrpc-sample), which includes examples of JSON-RPC server and client implementations, and demonstrates how to use the Connector for both local and remote connections.


## 📬 Contact & Follow
You're welcome to follow my technical notes and hands-on experience sharing

[Facebook](https://www.facebook.com/profile.php?id=61574839666569) ｜ [HackMD](https://hackmd.io/@jeff377) ｜ [GitHub](https://github.com/jeff377) ｜ [NuGet](https://www.nuget.org/profiles/jeff377)
