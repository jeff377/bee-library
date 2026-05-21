# Bee.Web.Blazor.Server

> Bee.NET 的 Blazor Server 元件庫 —— FormSchema 驅動 UI 元件，於 ASP.NET Core 宿主程序內執行。

[English](README.md)

## 架構定位

- **層級**：Web 前端（Razor Class Library）
- **Hosting 模式**：Blazor Server —— 元件邏輯在 ASP.NET Core 伺服端執行，瀏覽器透過 SignalR 接收 DOM diff。
- **Provider 配對**：搭配 `Bee.Api.Client` 中的 `LocalApiProvider`（進程內呼叫，無 HTTP round-trip）。
- **上游相依**：`Bee.Api.Client`
- **下游消費**：ASP.NET Core 宿主應用程式。

## 目標框架

- `net10.0`

## 狀態

僅有專案骨架。UI 元件（DynamicForm / DynamicGrid / FormPage / FormDataObject）尚未實作。完整設計請見 [docs/plans/plan-blazor-web-integration.md](../../docs/plans/plan-blazor-web-integration.md)。

## 相依約束

僅相依 `Bee.Api.Client`。宿主應用程式負責透過 `AddBeeFramework` 註冊後端服務，並選擇 `IApiProvider` 實作。

## 授權

MIT
