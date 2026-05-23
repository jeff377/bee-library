# Blazor.Wasm.Demo.Host

ASP.NET Core host：同一個程序內**同時**提供 (1) Bee 後端 JSON-RPC `/api` endpoint 與 (2) `Blazor.Wasm.Demo` 客戶端的靜態檔（`_framework/blazor.webassembly.js` + WASM dlls）。

瀏覽器一進 `/`：

1. Host 回 `index.html`（內含 `<script src="_framework/blazor.webassembly.js">`）
2. WASM runtime 下載完成後執行 `Blazor.Wasm.Demo` 的 `Program.cs`
3. `AddBeeBlazor(UseRemoteProvider($"{BaseAddress}api"))` 把 endpoint 設為自己（同源）
4. `BeeLoginPanel` → POST `/api` → Bee `JsonRpcExecutor` → 同程序的 `DemoAuthenticatingSystemBusinessObject`
5. Login 成功後 `FormPage` → POST `/api` → `FormBusinessObject` → SQLite

## 跑起來

```bash
cd samples/Blazor.Wasm.Demo.Host
dotnet run
# 瀏覽器自動開 http://localhost:5060
```

第一次執行：

1. 自動建立 `samples/Define/Master.key`（如果不存在）
2. `samples/Blazor.Wasm.Demo.Host/quickstart.db`（SQLite）自動建立 `ft_employee` + `ft_employee_phone`
3. 寫入 3 筆 demo 員工

## 對應到 library

| Demo 行為 | Library 元件 |
|----------|--------------|
| Bee 後端 in-process 註冊 | `AddBeeFramework`（Bee.Hosting） |
| JSON-RPC `/api` endpoint | `ApiServiceController`（Bee.Api.AspNetCore） |
| 提供 Wasm 靜態檔 | ASP.NET Core `UseBlazorFrameworkFiles` |
| Login 客製 | `DemoAuthenticatingSystemBusinessObject` + `DemoBusinessObjectFactory`（Bee.Samples.Shared） |
| Employee CRUD | `FormBusinessObject` + `FormSchema` + `FormRepositoryFactory` |

> 與 `QuickStart.Server` 的差別：`QuickStart.Server` 只示範 Anonymous Echo BO；此 host 多了 Wasm 靜態檔提供 + 員工 schema seed。兩個 host 可分別在 5050 / 5060 同時跑，互不干擾。
