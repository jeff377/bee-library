# Bee.NET Framework

Bee.NET Framework is an **n-tier software architecture** designed to accelerate the development of enterprise information systems. Built on **.NET Standard 2.0**, it features high modularity and cross-platform compatibility across .NET Framework, .NET Core, .NET 5+, and beyond.

> 📌 *N-tier* means the architecture is divided into more than three logical layers. In Bee.NET, the system is separated into at least five layers, including presentation, API communication, business logic, and data access.

## ✨ Features

- **N-tier architecture**: Clear separation of presentation, API, business logic, and data access layers for better modularity and maintainability.
- **Cross-platform compatibility**: Powered by .NET Standard 2.0 for broad .NET runtime support.
- **Modular components**: Decoupled libraries for core utilities, data, caching, business logic, and API hosting.
- **Rapid development**: Provides common reusable components to simplify system integration.

## 📦 Assembly

| Assembly Name              | Scope         | Target Framework       | Description |
|----------------------------|---------------|------------------------|-------------|
| **Bee.Base.dll**           | Frontend / Backend | netstandard2.0; net8.0  | Core utilities such as serialization, encryption, and general-purpose helpers. |
| **Bee.Define.dll**         | Frontend / Backend | netstandard2.0; net8.0  | Defines system-wide structured types for config, schema, and layout. |
| **Bee.Contracts.dll**       | Frontend / Backend | netstandard2.0; net8.0  | Shared data contracts between frontend and backend. |
| **Bee.Repository.Abstractions.dll**   | Backend        | netstandard2.0; net8.0  | Defines the interface contracts for the business layer to access the data layer, serving as the boundary between the business logic layer and the data access layer. |
| **Bee.Cache.dll**          | Backend        | netstandard2.0; net8.0  | Runtime caching of definitions and related system data to improve performance. |
| **Bee.Db.dll**             | Backend        | netstandard2.0; net8.0  | Database abstraction with dynamic command generation and connection binding. |
| **Bee.Repository.dll**      | Backend        | netstandard2.0; net8.0  | Provides common repository base classes and data access mechanisms. |
| **Bee.Business.dll**       | Backend        | netstandard2.0; net8.0  | Implements core business logic and application-level workflows. |
| **Bee.Api.Core.dll**       | Frontend / Backend | netstandard2.0; net8.0 | Encapsulates API support such as model definitions, encryption, and serialization. |
| **Bee.Api.AspNetCore.dll** | Backend       | net8.0                 | Provides a JSON-RPC 2.0 API controller for ASP.NET Core, serving as a unified endpoint to handle backend method calls. |
| **Bee.Api.AspNet.dll**     | Backend       | net48     | Provides a JSON-RPC 2.0 API HttpModule for ASP.NET, enabling unified POST endpoint for backend logic. |
| **Bee.Connect.dll**        | Frontend       | netstandard2.0; net8.0  | Connector for local or remote invocation of backend logic. |
| **Bee.UI.Core.dll**        | Frontend       | netstandard2.0; net8.0  | Manages client-server connection settings and states. |
| **Bee.UI.WinForms.dll**    | Frontend       | net8.0                | UI components and layout management for WinForms. |


## 💡 Sample Project

Refer to [jsonrpc-sample](https://github.com/jeff377/jsonrpc-sample), which includes examples of JSON-RPC server and client implementations, and demonstrates how to use the Connector for both local and remote connections.


## 📬 Contact & Follow
You're welcome to follow my technical notes and hands-on experience sharing

[Facebook](https://www.facebook.com/profile.php?id=61574839666569) ｜ [HackMD](https://hackmd.io/@jeff377) ｜ [GitHub](https://github.com/jeff377) ｜ [NuGet](https://www.nuget.org/profiles/jeff377)

---

# Bee.NET Framework（繁體中文）

Bee.NET Framework 是一套 **多層式（N-Tier）應用架構**，旨在加速企業資訊系統的開發。此架構建構於 **.NET Standard 2.0** 之上，具備高度模組化與跨平台相容性，支援 .NET Framework、.NET Core、.NET 5+ 等環境。

> 📌 *N-Tier* 指超過三層的邏輯分層架構，在 Bee.NET 中實際拆分為五層（例如表現層、API 呼叫層、業務邏輯層、資料存取層），更能因應複雜企業需求。

## ✨ 特色

- **多層式架構（N-Tier）**：支援表現層、API 層、邏輯層與資料層分離，強化可維護性與擴充性。
- **跨平台支援**：核心採用 .NET Standard 2.0，可執行於多種 .NET 平台。
- **模組化組件**：根據職責切分為多個元件，靈活組合、降低耦合。
- **開發加速器**：快速建立與整合企業常見功能模組。

## 📦 組件說明

| 組件名稱                   | 適用範圍       | 目標框架               | 說明 |
|----------------------------|----------------|------------------------|------|
| **Bee.Base.dll**            | 前端 / 後端    | netstandard2.0; net8.0  | 提供基礎函式與工具（序列化、加密等），作為共通基礎模組。 |
| **Bee.Define.dll**         | 前端 / 後端    | netstandard2.0; net8.0  | 定義系統結構化資料，如設定、資料表結構、表單配置。 |
| **Bee.Contracts.dll**   | 前端 / 後端    | netstandard2.0; net8.0  | 前後端共用的資料契約。 |
| **Bee.Repository.Abstractions.dll** | 後端           | netstandard2.0; net8.0  | 定義業務層存取資料層的介面契約，作為業務邏輯層與資料存取層之間的邊界。 |
| **Bee.Cache.dll**          | 後端           | netstandard2.0; net8.0  | 執行階段快取模組，快取定義資料與衍生資料以提升效能。 |
| **Bee.Db.dll**               | 後端           | netstandard2.0; net8.0  | 封裝資料庫操作邏輯，支援 SQL 命令組合與動態連線綁定。 |
| **Bee.Repository**      | 後端           | netstandard2.0; net8.0  | 提供共用的 Repository 基底類別與資料存取機制。 |
| **Bee.Business.dll**      | 後端           | netstandard2.0; net8.0  | 實作應用層業務邏輯，處理表單流程與業務規則。 |
| **Bee.Api.Core.dll**      | 前端 / 後端    | netstandard2.0; net8.0  | 提供 API 核心支援，包含資料模型、加解密、序列化等功能。 |
| **Bee.Api.AspNetCore.dll** | 後端           | net8.0                 | 提供 ASP.NET Core 的 JSON-RPC 2.0 API 控制器，作為統一入口處理後端方法呼叫。 |
| **Bee.Api.AspNet.dll** | 後端           | net48     | 提供 ASP.NET 的 JSON-RPC 2.0 API HttpModule，作為統一的 POST 入口處理後端方法呼叫。 |
| **Bee.Connect.dll**       | 前端           | netstandard2.0; net8.0  | 提供連接器機制，支援近端與遠端呼叫後端邏輯元件。 |
| **Bee.UI.Core.dll**        | 前端           | netstandard2.0; net8.0  | 管理用戶端與伺服端連線的設定與狀態。 |
| **Bee.UI.WinForms.dll**    | 前端           | net8.0                 | WinForms 使用者介面元件與排版管理。|

## 💡 範例程式

請參考 [jsonrpc-sample](https://github.com/jeff377/jsonrpc-sample)，其中包含 JSON-RPC 的 Server 與 Client 實作範例，並說明如何透過 Connector 進行近端與遠端連線。

## 📬 聯絡與關注
歡迎追蹤我的技術筆記與實戰經驗分享

[Facebook](https://www.facebook.com/profile.php?id=61574839666569) ｜ [HackMD](https://hackmd.io/@jeff377) ｜ [GitHub](https://github.com/jeff377) ｜ [NuGet](https://www.nuget.org/profiles/jeff377)