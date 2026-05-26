# QuickStart.Server

[English](README.md) | **繁體中文**

最小可運行的 Bee.NET JSON-RPC API host。對外暴露：

- `System.Ping` — 框架內建，anonymous；驗證 host 可達。
- `Echo.Echo` — sample BO，anonymous；回傳的訊息會被加上 `"echo: "` 前綴。

## 跑法

```bash
cd samples/QuickStart.Server
dotnet run
```

啟動後 listen 在 `http://localhost:5050`，JSON-RPC endpoint 為 `POST /api`。

## 預期輸出

第一次啟動會：

1. 從 `samples/Define/` 載入 `SystemSettings.xml` / `DbCategorySettings.xml` / `DatabaseSettings.xml`
2. 從環境變數 `BEE_MASTER_KEY` 取得 master key。demo bootstrap (`DemoBackend.AddBeeBackend`) 在變數未設時會自動注入硬編碼的 demo 值,所以 fresh clone 可零設定直接跑。
3. 在工作目錄產生 `quickstart.db`(已被 `.gitignore`)

console 應顯示 `Now listening on: http://localhost:5050`。

> **Production host 必須覆寫 demo master key。** 硬編碼的 demo 值位於
> `Bee.Samples.Shared.DemoCredentials.DemoMasterKey`,進 git 公開,僅供 demo
> 使用。真實部署必須在 process 啟動「之前」由部署機制(K8s Secret、env file、
> Vault、AWS Secrets Manager…)把 `BEE_MASTER_KEY` 設成真實 secret;bootstrap
> 僅在變數未設時才填值,外部已注入的值會被保留。

## 對應到哪些 library 功能

| 程式段落 | library 功能 |
|----------|--------------|
| `DbProviderRegistry.Register(DatabaseType.SQLite, SqliteFactory.Instance)` | `Bee.Db.Manager.DbProviderRegistry` — ADO.NET provider 切換 |
| `DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory())` | `Bee.Db.Providers.Sqlite` — SQLite dialect（form CRUD / schema 反射 / DDL） |
| `SystemSettingsLoader.Load(paths)` | `Bee.Definition.SystemSettingsLoader` — boot-time 載入 XML |
| `services.AddBeeFramework(...)` | `Bee.Hosting.BeeFrameworkServiceCollectionExtensions` — backend composition root |
| `IFormBoTypeResolver` override | `Bee.Business.IFormBoTypeResolver` — 自訂 progId → BO type 對應 |
| `: ApiServiceController` 空殼 controller | `Bee.Api.AspNetCore.Controllers.ApiServiceController` — `[Route("api")]` JSON-RPC endpoint |
| `[ApiAccessControl(Public, Anonymous)]` | `Bee.Definition.Attributes.ApiAccessControlAttribute` — API 存取控制 |

## 試打看看（不啟動 console demo 的情況下）

```bash
curl -s -X POST http://localhost:5050/api \
  -H 'Content-Type: application/json' \
  -H 'X-Api-Key: quickstart-demo' \
  -d '{
        "jsonrpc": "2.0",
        "id": "1",
        "method": "Echo.Echo",
        "params": { "Value": { "Message": "hello" } }
      }'
```

預期回 JSON-RPC `result.value`，其中 `Response = "echo: hello"`。
