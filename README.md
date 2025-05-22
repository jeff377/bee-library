
# Bee.NET Framework

Bee.NET Framework is a three-tier software architecture designed to accelerate the development of enterprise information systems. Built on **.NET Standard 2.0**, it features high modularity and cross-platform compatibility across .NET Framework, .NET Core, .NET 5+, and beyond.

## ✨ Features

- **Three-tier architecture**: Clear separation of presentation, business logic, and data access layers.
- **Cross-platform compatibility**: Powered by .NET Standard 2.0 for broad .NET runtime support.
- **Modular components**: Decoupled libraries for core utilities, data, caching, business logic, and API hosting.
- **Rapid development**: Provides common reusable components to simplify system integration.

## 📦 Assembly

| Assembly Name              | Scope         | Target Framework       | Description |
|----------------------------|---------------|------------------------|-------------|
| **Bee.Base.dll**           | Frontend / Backend | .NET Standard 2.0  | Core utilities such as serialization, encryption, and general-purpose helpers. |
| **Bee.Define.dll**         | Frontend / Backend | .NET Standard 2.0  | Defines system-wide structured types for config, schema, and layout. |
| **Bee.Cache.dll**          | Backend        | .NET Standard 2.0      | Runtime caching of definitions and related system data to improve performance. |
| **Bee.Db.dll**             | Backend        | .NET Standard 2.0      | Database abstraction with dynamic command generation and connection binding. |
| **Bee.Business.dll**       | Backend        | .NET Standard 2.0      | Implements core business logic and application-level workflows. |
| **Bee.Api.Core.dll**       | Frontend / Backend        | .NET Standard 2.0      | Encapsulates API support such as model definitions, encryption, and serialization. |
| **Bee.Api.AspNetCore.dll** | Backend       | .NET 8             | Provides a JSON-RPC 2.0 API controller for ASP.NET Core, serving as a unified endpoint to handle backend method calls via JSON-RPC protocol. |
| **Bee.Api.AspNet.dll**     | Backend        | .NET Framework 4.8     | Provides a JSON-RPC 2.0 API HttpModule for ASP.NET, enabling a unified POST endpoint to invoke backend methods via the JSON-RPC protocol. |
| **Bee.Connect.dll**        | Frontend       | .NET Standard 2.0      | Connector for local or remote invocation of backend logic. |
| **Bee.UI.Core.dll**        | Frontend       | .NET Standard 2.0      | Manages client-server connection settings and states. |
| **Bee.UI.WinForms.dll**        | Frontend       | .NET 8      | UI components and layout management for WinForms. |

---

# Bee.NET Framework（繁體中文）

Bee.NET Framework 是一套三層式應用架構，旨在加速企業資訊系統的開發。此架構建構於 **.NET Standard 2.0** 之上，具備高度模組化與跨平台相容性，支援 .NET Framework、.NET Core、.NET 5+ 等環境。

## ✨ 特色

- **三層式架構**：支援表現層、邏輯層與資料層分離，強化可維護性與擴充性。
- **跨平台支援**：核心採用 .NET Standard 2.0，可執行於多種 .NET 平台。
- **模組化組件**：根據職責切分為多個元件，靈活組合、降低耦合。
- **開發加速器**：快速建立與整合企業常見功能模組。

## 📦 組件說明

| 組件名稱                   | 適用範圍       | 目標框架               | 說明 |
|----------------------------|----------------|------------------------|------|
| **Bee.Base.dll**           | 前端 / 後端    | .NET Standard 2.0      | 提供基礎函式與工具（序列化、加密等），作為共通基礎模組。 |
| **Bee.Define.dll**         | 前端 / 後端    | .NET Standard 2.0      | 定義系統結構化資料，如設定、資料表結構、表單配置。 |
| **Bee.Cache.dll**          | 後端           | .NET Standard 2.0      | 執行階段快取模組，快取定義資料與衍生資料以提升效能。 |
| **Bee.Db.dll**             | 後端           | .NET Standard 2.0      | 封裝資料庫操作邏輯，支援 SQL 命令組合與動態連線綁定。 |
| **Bee.Business.dll**       | 後端           | .NET Standard 2.0      | 實作應用層業務邏輯，處理表單流程與業務規則。 |
| **Bee.Api.Core.dll**       | 前端 / 後端           | .NET Standard 2.0      | 提供 API 核心支援，包含資料模型、加解密、序列化等功能。 |
| **Bee.Api.AspNetCore.dll** | 後端       | .NET 8                 | 提供 ASP.NET Core 的 JSON-RPC 2.0 API 控制器，作為統一入口處理後端方法呼叫。 |
| **Bee.Api.AspNet.dll**     | 後端           | .NET Framework 4.8     | 提供 ASP.NET 的 JSON-RPC 2.0 API HttpModule，作為統一的 POST 入口處理後端方法呼叫。 |
| **Bee.Connect.dll**        | 前端           | .NET Standard 2.0      | 提供連接器機制，支援近端與遠端呼叫後端邏輯元件。 |
| **Bee.UI.Core.dll**        | 前端       | .NET Standard 2.0      | 管理用戶端與伺服端連線的設定與狀態。 |
| **Bee.UI.WinForms.dll**        | 前端       | .NET 8      | WinForms 使用者介面元件與排版管理。|

