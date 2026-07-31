# 快速上手

[English](getting-started.md) · [← 文件索引](README.zh-TW.md)

> 從一個空資料夾建出第一個 Bee.NET 後端：安裝套件、備妥 `DefinePath`、接好 DI 容器、發布 JSON-RPC 端點、加一個商業物件，再由用戶端呼叫。

本文帶你建**自己的專案**。若你只想先看框架跑起來、還不想動手寫，repo 的 [`samples/`](../samples/README.zh-TW.md) 有可直接執行的範例 —— `QuickStart.Server` + `QuickStart.Console` 正是本頁對應的那組。

每個步驟都連向深入說明該主題的文件。本頁只給最小可跑的內容，不重複那些文件已寫的東西。

---

## 前置需求

- **.NET 10 SDK**
- **一個資料庫**。SQL Server、PostgreSQL、MySQL、Oracle、SQLite 皆可。SQLite 不需架設伺服器，以下即以它示範。

## 1. 建專案並安裝套件

```bash
dotnet new web -o MyApp.Server
cd MyApp.Server
dotnet add package Bee.Api.AspNetCore
dotnet add package Bee.Db
```

**該選哪個 host 套件？** `Bee.Api.AspNetCore` 會遞移帶入組合根 `Bee.Hosting`。若你的 host 不是 ASP.NET Core —— WinForms、WPF、Console、Worker Service —— 改為直接參考 `Bee.Hosting`，並略過步驟 4 的 `UseBeeFramework` 呼叫。

## 2. 備妥 `DefinePath`

框架從一個 XML 定義檔目錄（`DefinePath`）啟動。框架自身的最小必要集 —— `st_*` TableSchema、`SystemSettings.xml`、`DatabaseSettings.xml`、`DbCategorySettings.xml`，以及框架隨附的 Department / Employee 表單 —— 以嵌入資源形式放在 `Bee.Definition.dll`。首次執行前把它們展開一次：

```bash
dotnet tool install -g Bee.Cli
dotnet bee defines materialize --path ./Define
```

預設 skip-existing，重跑不會覆蓋你自己的修改。同一動作也可用程式呼叫 `Bee.Definition.Defaults.MaterializeTo(...)`。

接著編輯 `./Define` 下兩個檔案：

- **`SystemSettings.xml`** —— 設定 `MasterKeySource`。預設值 `Environment` 會從 `BEE_MASTER_KEY` 讀取金鑰。
- **`DatabaseSettings.xml`** —— 填入連線字串。

→ 每個定義檔各管什麼：[定義檔全景](definition-files-overview.zh-TW.md)。完整檔案清單與使用端擴充規則：[框架保留命名](framework-reserved-names.zh-TW.md)。

## 3. 註冊資料庫方言

框架不強迫每個 host 都拉進所有 ADO.NET driver，因此你用哪個方言就明確註冊哪個：

```csharp
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Providers.Sqlite;
using Microsoft.Data.Sqlite;

DbProviderRegistry.Register(DatabaseType.SQLite, new SqliteProviderFactory(SqliteFactory.Instance));
DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());
```

依需要把 `Sqlite` 換成 `SqlServer`、`PostgreSql`、`MySql` 或 `Oracle`。

## 4. 接線 DI 容器

```csharp
using Bee.Api.AspNetCore;
using Bee.Api.Core;
using Bee.Base;
using Bee.Definition;
using Bee.Hosting;

var builder = WebApplication.CreateBuilder(args);

var paths = new PathOptions { DefinePath = "./Define" };
var settings = SystemSettingsLoader.Load(paths);

SysInfo.Initialize(settings.CommonConfiguration);
ApiServiceOptions.Initialize(
    settings.CommonConfiguration.ApiPayloadOptions,
    settings.CommonConfiguration.IsDebugMode);

builder.Services.AddBeeFramework(
    settings.BackendConfiguration,
    paths,
    autoCreateMasterKey: true);

builder.Services.AddControllers();

var app = builder.Build();
app.UseBeeFramework();
app.MapControllers();
app.Run();
```

**順序是硬性的。** `SystemSettingsLoader.Load` 必須早於 `SysInfo.Initialize`，後者必須早於 `AddBeeFramework`。`UseBeeFramework` 不註冊任何 middleware 或端點 —— 它只做啟動檢查。

→ 啟動流程圖與 `AddBeeFramework` 註冊了什麼：[端到端開發指引 § 框架初始化順序](development-cookbook.zh-TW.md)。順序背後的限制：[開發限制與反模式 § 初始化順序限制](development-constraints.zh-TW.md)。

## 5. 發布 JSON-RPC 端點

`ApiServiceController` 已宣告 `[Route("api")]` 與 POST handler，因此一個空的子類別就是整個端點：

```csharp
using Bee.Api.AspNetCore.Controllers;

namespace MyApp.Server.Controllers;

public class ApiController : ApiServiceController
{
}
```

`POST /api` 現在已能接受 JSON-RPC 2.0 請求。

## 6. 寫第一個商業物件

商業物件以 **progId** 定位。`"System"` 以外的 progId 一律走 form business object 派發，故繼承 `FormBusinessObject` 並比照其建構子簽章：

```csharp
using Bee.Business;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;

namespace MyApp.Server.BusinessObjects;

public class EchoArgs : BusinessArgs
{
    public string Message { get; set; } = string.Empty;
}

public class EchoResult : BusinessResult
{
    public string Response { get; set; } = string.Empty;
}

public class EchoBusinessObject : FormBusinessObject
{
    public EchoBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
        : base(ctx, accessToken, progId, isLocalCall)
    {
    }

    [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
    public virtual EchoResult Echo(EchoArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return new EchoResult { Response = $"echo: {args.Message}" };
    }
}
```

`[ApiAccessControl]` 決定該方法是否對外可達、以及其保護等級。`Public` + `Anonymous` 不需 access token 也不需加密握手 —— 適合當第一次呼叫，**不適合**用在真實資料上。

以 `IFormBoTypeResolver` 把 progId 對應到型別，其餘 progId 一律回退給框架原本的解析：

```csharp
public sealed class MyFormBoTypeResolver : IFormBoTypeResolver
{
    public Type Resolve(string progId) => progId switch
    {
        "Echo" => typeof(EchoBusinessObject),
        _ => typeof(FormBusinessObject),
    };
}
```

註冊時機必須在 `AddBeeFramework` **之後** —— 最後註冊者勝出：

```csharp
builder.Services.AddSingleton<IFormBoTypeResolver, MyFormBoTypeResolver>();
```

→ `Args` / `Result` 的命名規則與契約三層分離：[API ↔ BO 契約設計](api-bo-contract-design.zh-TW.md)。哪些方法該放介面：[開發限制與反模式](development-constraints.zh-TW.md)。

## 7. 由用戶端呼叫

.NET 端使用 `Bee.Api.Client`：

```csharp
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages;

ApiClientInfo.ApiKey = "my-demo-key";

var connector = new FormApiConnector("http://localhost:5050/api", Guid.Empty, "Echo");
var result = await connector.ExecuteAsync<EchoResponse>(
    "Echo",
    new EchoRequest { Message = "hello" },
    PayloadFormat.Plain);
```

用戶端的 request / response DTO 請與伺服端的 `Args` / `Result` 分開宣告 —— 這才是第三方整合者看到契約的樣子，也能讓 wire 形狀誠實反映實際約定。

`PayloadFormat.Plain` 對應上面宣告的 `Public` + `Anonymous`。任何受保護的方法都需先 `Login`，由它發出 access token 與 RSA 握手。

→ 前端無 .NET、以 JavaScript / TypeScript 呼叫：[JSON-RPC 前端整合指引](jsonrpc-frontend-integration.zh-TW.md)。所有對外方法與其存取控制：[API 方法參考](api-method-reference.zh-TW.md)。

## 8. 改用「定義」取代寫程式

上面的 Echo 物件是刻意手寫的 —— 它只是「證明管線通了」的最小單位。**一般 CRUD 完全不需要商業物件**：宣告一份 `FormSchema` 加上對應的 `TableSchema`，框架就會從定義產生 SQL、清單與存檔路徑。

這才是框架真正的重點，起點在此 → [定義檔全景](definition-files-overview.zh-TW.md)，接著 [架構總覽](architecture-overview.zh-TW.md)。

---

## 接下來讀什麼

| 你想 | 讀 |
|------|-----|
| 先理解設計再往下走 | [架構總覽](architecture-overview.zh-TW.md) |
| 知道每個定義檔在管什麼 | [定義檔全景](definition-files-overview.zh-TW.md) |
| 走完整條「定義 → API」流程 | [端到端開發指引](development-cookbook.zh-TW.md) |
| 不寫程式就完成欄位運算與驗證 | [運算式與規則](expression-rules.zh-TW.md) |
| 加上認證與權限 | [權限與授權指南](permission-authorization.zh-TW.md) |
| 把定義變更推送到線上資料庫 | [資料庫 Schema 升級](database-schema-upgrade.zh-TW.md) |

上述內容的完整可執行版本在 [`samples/QuickStart.Server`](../samples/QuickStart.Server/README.zh-TW.md) 與 [`samples/QuickStart.Console`](../samples/QuickStart.Console/README.zh-TW.md)。若想看幾乎全以定義建成的完整應用，見 [`apps/Bee.Northwind`](../apps/Bee.Northwind/README.zh-TW.md)。
