# Blazor.Wasm.Demo

[English](README.md) | **繁體中文**

Blazor WebAssembly client，與 [`Blazor.Server.Demo`](../Blazor.Server.Demo/README.zh-TW.md) 共用同一份 `FormSchema` 與相同的 `BeeLoginPanel` / `FormPage` 元件，差別只在執行環境：

- 元件邏輯跑在**瀏覽器**（.NET WASM runtime）
- 連 backend 改走 **`RemoteApiProvider`**（HTTP），endpoint 預設為 `${BaseAddress}api`
- 必須搭配 [`Blazor.Wasm.Demo.Host`](../Blazor.Wasm.Demo.Host/README.zh-TW.md) 才能跑（host 同時提供 Wasm 靜態檔與 `/api` JSON-RPC endpoint）

## 跑起來

```bash
cd samples/Blazor.Wasm.Demo.Host
dotnet run
# 瀏覽器自動開 http://localhost:5060
```

不要直接 `dotnet run` 這個 Wasm 專案——它沒有 server。

## 預期畫面

跟 Blazor.Server.Demo 一模一樣：登入 `demo / demo` → `FormPage ProgId="Employee"`。

## 與 Server 版本的差異

| 面向 | Blazor.Server.Demo | Blazor.Wasm.Demo |
|------|--------------------|------------------|
| 元件執行位置 | ASP.NET Core server | 使用者瀏覽器 |
| BO 派遣方式 | `LocalApiProvider`（in-process） | `RemoteApiProvider`（HTTP /api） |
| `AddBeeBlazor` 選項 | `UseLocalProvider()` | `UseRemoteProvider(endpoint)` |
| 元件庫 | `Bee.Web.Blazor.Server` | `Bee.Web.Blazor.Wasm` |
| 元件源碼差異 | 無（DynamicForm / FormPage 等簽名一致） | 無 |

**element-for-element 一致**：Login 後在頁面上能做的所有操作，兩邊回應的 DataSet 與行為都相同——這即是「同一份 FormSchema 渲染多前端」的展示。
