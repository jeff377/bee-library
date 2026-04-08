# Bee.NET Framework（繁體中文）

Bee.NET Framework 是一套採用 **N-Tier + Clean Architecture + MVVM** 混合模式的企業資訊系統開發框架，以**定義導向架構（Definition-Driven Architecture）**為核心，以 `FormSchema` 作為系統唯一定義來源（Single Source of Truth），統一驅動 UI 配置、資料表結構與業務驗證規則。

> 📌 *N-Tier* 指超過三層的邏輯分層架構，在 Bee.NET 中實際拆分為五層（表現層、API 呼叫層、業務邏輯層、資料存取層、資料庫層），各層職責明確分離，更能因應複雜企業需求。

核心套件目標框架為 **`netstandard2.0; net10.0`**，支援 .NET Framework、.NET Core、.NET 5+ 等多種執行環境。API 託管套件（`Bee.Api.AspNetCore`）目標框架為 **`net10.0`**。

[English](README.md)

## ✨ 特色

- **定義導向架構（Definition-Driven Architecture）**：以 `FormSchema` 作為系統唯一定義來源，自動推導 UI 配置（`FormLayout`）、資料表結構（`TableSchema`）與驗證規則，定義一處即全層同步。
- **N-Tier + Clean Architecture + MVVM**：支援表現層、API 層、業務邏輯層（BO）與資料存取層的清晰分離，針對 ERP 場景從各模式取用最適合的概念。
- **跨平台支援**：核心套件採用 `netstandard2.0; net10.0`，可執行於多種 .NET 平台。
- **模組化組件**：根據職責切分為多個元件，靈活組合、降低耦合。
- **開發加速器**：透過可重用基底類別與 FormSchema 驅動的 CRUD，大幅減少重複程式碼。

## 📦 組件說明

### 共用（前端 / 後端）

| 組件名稱 | 說明 |
|---|---|
| **Bee.Core.dll** | 提供基礎函式與工具（序列化、加密等），作為共通基礎模組。 |
| **Bee.Definition.dll** | 定義系統結構化資料，包含 FormSchema、欄位結構描述與版面配置。 |
| **Bee.Api.Contracts.dll** | 前後端共用的資料契約（請求 / 回應模型）。 |
| **Bee.Api.Core.dll** | 提供 API 核心支援，包含資料模型、Payload 加解密與序列化管線。 |

### 後端

| 組件名稱 | 說明 |
|---|---|
| **Bee.Repository.Abstractions.dll** | 定義業務層存取資料層的介面契約，作為 Business Object 與 Repository 之間的邊界。 |
| **Bee.ObjectCaching.dll** | 執行階段快取 FormSchema 定義資料與衍生資料，提升系統效能。 |
| **Bee.Db.dll** | 封裝資料庫操作邏輯，支援動態 SQL 命令產生與連線綁定。 |
| **Bee.Repository.dll** | 提供共用的 Repository 基底類別與 FormSchema 驅動的資料存取機制。 |
| **Bee.Business.dll** | 實作業務邏輯核心（Business Object / BO），負責 Use Case 工作流程。 |
| **Bee.Api.AspNetCore.dll** | 提供 ASP.NET Core 的 JSON-RPC 2.0 API 控制器，作為統一入口處理後端方法呼叫。 |

### 前端

| 組件名稱 | 說明 |
|---|---|
| **Bee.Api.Client.dll** | 提供連接器機制，支援近端與遠端呼叫後端 Business Object。 |

## 💡 範例程式

請參考 [bee-jsonrpc-sample](https://github.com/jeff377/bee-jsonrpc-sample)，其中包含 JSON-RPC 的 Server 與 Client 實作範例，並說明如何透過 Connector 進行近端與遠端連線。

## 📬 聯絡與關注
歡迎追蹤我的技術筆記與實戰經驗分享

[Facebook](https://www.facebook.com/profile.php?id=61574839666569) ｜ [HackMD](https://hackmd.io/@jeff377) ｜ [GitHub](https://github.com/jeff377) ｜ [NuGet](https://www.nuget.org/profiles/jeff377)
