# Bee.Web.Blazor.Wasm

> Bee.NET 的 Blazor WebAssembly 元件庫 —— FormSchema 驅動 UI 元件，於瀏覽器的 .NET WebAssembly runtime 內執行。

[English](README.md)

## 架構定位

- **層級**：Web 前端（Razor Class Library）
- **Hosting 模式**：Blazor WebAssembly —— 元件於瀏覽器的 .NET WASM runtime 執行，對後端的呼叫透過 HTTP 傳輸。
- **Provider 配對**：搭配 `Bee.Api.Client` 中的 `RemoteApiProvider`。
- **上游相依**：`Bee.Api.Client`
- **下游消費**：Blazor WASM 宿主應用程式。

## 目標框架

- `net10.0` —— 建置或執行需 `wasm-tools` workload。

## 狀態

僅有專案骨架。UI 元件（DynamicForm / DynamicGrid / FormPage / FormDataObject）尚未實作。完整設計請見 [docs/plans/plan-blazor-web-integration.md](../../docs/plans/plan-blazor-web-integration.md)。

## 相依約束

**嚴禁相依任何後端組件**（Repository / Business / Hosting 等）—— 瀏覽器執行環境無法載入伺服端組件。此約束由相依鏈強制：`Bee.Api.Client → Bee.Api.Core → Bee.Api.Contracts/Definition` 全為純資料/協定層。

## 授權

MIT
