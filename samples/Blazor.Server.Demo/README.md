# Blazor.Server.Demo

Blazor Server 宿主，示範如何把 `Bee.Web.Blazor.Server` 元件庫掛到 ASP.NET Core，並透過 **in-process LocalApiProvider** 直連 Bee 後端（同一個程序內、無 HTTP round-trip）。

## 跑起來

```bash
cd samples/Blazor.Server.Demo
dotnet run
# 瀏覽器自動開 http://localhost:5055
```

第一次執行時：

1. 自動建立 `samples/Define/Master.key`（如果不存在）
2. `samples/Blazor.Server.Demo/quickstart.db`（SQLite）自動建立 `ft_employee` + `ft_employee_phone`
3. 寫入 3 筆 demo 員工（Alice / Bob / Carol）

## 預期畫面

1. 進入首頁先看到 **Sign in** 區塊，預填提示 `demo / demo`
2. 按下 Sign in（送 `SystemApiConnector.LoginAsync`，由 `DemoAuthenticatingSystemBusinessObject` 接住）
3. 登入成功後顯示 `<FormPage ProgId="Employee" />`：
   - 上方 toolbar：`New` / `Save` / `Delete`
   - 中段：員工列表（`DynamicGrid` 依 `FormSchema.ListFields` 動態渲染）
   - 下段：選中 row 後出現的編輯表單（`DynamicForm`）
4. 點 `New` → 改欄位 → `Save`，新員工會出現在列表

## 對應到 library

| Demo 行為 | Library 元件 |
|----------|--------------|
| Login 表單 | `BeeLoginPanel`（Phase 1d） |
| AccessToken cascading | `BeeAccessTokenProvider`（Phase 1d） |
| 員工列表渲染 | `DynamicGrid` + `FormSchema.ListLayout` |
| 員工編輯表單 | `DynamicForm` + `FormSchema.FormLayout` |
| 列表 + 表單整合 | `FormPage` |
| CRUD 走 Bee | `FormDataObject.LoadAsync / SaveAsync / NewAsync / DeleteAsync` |
| Local 模式 in-process 派遣 | `BeeApiConnectorFactory.UseLocalProvider` |
| In-process JSON-RPC | `LocalApiProvider` → `JsonRpcExecutor` → `FormBusinessObject` |

## 簡化措施（與 production 不同）

- **`DemoAuthenticatingSystemBusinessObject`** 用寫死的 `demo/demo` 比對，不查 `st_user` 表 → 因此這版 demo 不需要 system tables（st_user / st_session / st_company / st_user_company）
- **單一 process 共享 `ApiClientInfo.LocalServiceProvider` 與 `ApiClientInfo.ApiEncryptionKey`**：多使用者同時 Login 會踩到對方的金鑰；demo 一次只一位使用者 OK，production 需走每連線的方案
- SQLite 為單一檔（`quickstart.db`），跟 `QuickStart.Server` 一樣
