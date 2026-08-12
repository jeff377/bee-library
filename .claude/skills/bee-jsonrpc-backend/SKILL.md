---
name: bee-jsonrpc-backend
description: >
  用 Bee.NET 框架套件從零建立一個 JSON-RPC 後端 server(ASP.NET Core, POST /api)+ 前端 client 呼叫,
  含 bootstrap、Define/ XML 設定、自訂 Business Object、demo 登入、session/API key、以及避雷。
  當使用者要「用 Bee 框架建後端」「Bee JSON-RPC server」「AddBeeFramework 怎麼設」「Bee 的
  BusinessObject / ProgramSettings / Define 設定」「Bee.Api.Client 怎麼呼叫 server」「Bee server 登入 /
  session / X-Api-Key」「把現有專案的後端接上 Bee」之類需求時使用,即使沒明確講出全部關鍵字也要主動
  觸發。**只負責 server 建置 + client 呼叫的通用骨架與設定;不負責特定 app 的業務邏輯設計。**
---

# 用 Bee.NET 建 JSON-RPC 後端

Bee 是一套**定義驅動(XML)+ 反射分派**的 JSON-RPC 後端框架。整個 server 幾乎沒有 C#:框架的 API
executor、action 字串解析、MessagePack 序列化、`[ApiAccessControl]` 驗證、CRUD 基底方法全在 `Bee.*`
NuGet 套件裡。host 專案只是薄殼:一個 csproj、`Program.cs`、bootstrap 類、一個空 controller、幾個
自訂 BO,加上 `Define/` 設定樹。

**理解這點能省下大量摸索**:大多數行為由 `Define/*.xml` 觸發,不是由你寫的程式。

## 權威參考(照抄的來源)

`bee-library/samples`(與框架同版,**最佳藍本**):
- `samples/QuickStart.Server/` — 最小 server(Program.cs、空 controller、`EchoBusinessObject`、`QuickStartFormBoTypeResolver`)
- `samples/QuickStart.Console/` — 最小 client(`Bee.Api.Client` 直接呼叫)
- `samples/Bee.Samples.Shared/` — `DemoBackend`(`AddBeeBackend`/`UseBeeBackend`)、`DemoBusinessObjectFactory`、`DemoAuthenticatingSystemBusinessObject`、`DemoCredentials`

完整 app（含 seeder、company scope、`ProgramSettings` 宣告式綁定）：`apps/Bee.Northwind/`。

> 動工前務必打開 `QuickStart.Server` + `DemoBackend.cs` 對照,版本細節(介面成員、tag 名)會隨版本變。

## 不適用（改走別支）

- 要 **DB scope（common/company/log）、company context、seeder** →
  **`bee-app-scaffold`**（它建立在本 skill 的樣板之上，只補這三塊）
- 要 **ERP 定義驅動的表單 CRUD**（`FormBusinessObject` 的 `GetList`/`Save`/`Delete`）→
  同上；本 skill 的 BO 軸是最小的 `BusinessObject`
- 在已接好的 app 上**加一張表單** → **`bee-add-form`**

## 請求流程(先建立心智模型)

```
client ──POST /api──▶ ApiServiceController
   body: {jsonrpc:"2.0", method:"Game.GetLevels", params:{format,value,type}, id}
   headers: X-Api-Key(必填), Authorization: Bearer <token>(非匿名方法才需要)
        │
        ▼
  驗 X-Api-Key + token → 以第一個「.」切 method = (ProgId, Action)
        │  ProgId=="System" → CreateSystemBusinessObject
        │  其餘            → CreateFormBusinessObject → IFormBoTypeResolver.Resolve(progId)
        ▼
  Activator.CreateInstance(boType, ctx, token, progId, isLocalCall)
  GetMethod(Action) → 檢查 [ApiAccessControl] → 反射叫用:單一 args 進、單一 result 出
        │
        ▼
  result 經 ApiPayload(MessagePack→gzip→aes,或 Plain=JSON)包回,client 反序列化為 TResult
```

**單一 args / 單一 result** 是每個 action 的鐵則:`TResult Action(TArgs args)`(或 `async Task<TResult>`)。

## 建置步驟

依序做;每步的完整程式碼在 `references/`。

### 1. 專案骨架

Web 專案(`Microsoft.NET.Sdk.Web`, `net10.0`),加套件(集中式版本管理放 `Directory.Packages.props`):

```xml
<PackageReference Include="Bee.Api.AspNetCore" />  <!-- controller + API pipeline -->
<PackageReference Include="Bee.Business" />         <!-- BO 基底 -->
<PackageReference Include="Bee.Db" />               <!-- 資料存取 / dialect -->
<PackageReference Include="Bee.Hosting" />          <!-- AddBeeFramework -->
<PackageReference Include="Microsoft.Data.Sqlite" />
<!-- 釘 3.x 非漏洞版;Microsoft.Data.Sqlite 會拉進漏洞的 2.1.x -->
<PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" />
```

### 2. `Program.cs` + backend bootstrap

`Program.cs` 極簡;真正的 wiring 封裝在一個 `XxxBackend` 靜態類(照抄 `DemoBackend.cs`)。**兩段初始化都要**:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.AddXxxBackend();          // 見下
builder.Services.AddControllers();
var app = builder.Build();
app.UseXxxBackend();              // seeder + ApiClientInfo.LocalServiceProvider(如需)
app.MapControllers();
app.Run();
```

`AddXxxBackend` 的核心順序(缺一不可):
```csharp
var paths = new PathOptions { DefinePath = ResolveDefinePath() };   // walk-up 找 Define/SystemSettings.xml
DbProviderRegistry.Register(DatabaseType.SQLite, new SqliteProviderFactory(SqliteFactory.Instance));
DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());
var settings = SystemSettingsLoader.Load(paths);
SysInfo.Initialize(settings.CommonConfiguration);
ApiServiceOptions.Initialize(settings.CommonConfiguration.ApiPayloadOptions,   // ← 易漏!設 codec
                             settings.CommonConfiguration.IsDebugMode);
builder.Services.AddBeeFramework(settings.BackendConfiguration, paths, autoCreateMasterKey: true);
// 在 AddBeeFramework 之後註冊(last-wins),換掉工廠 / resolver:
builder.Services.AddSingleton<IFormBoTypeResolver, XxxFormBoTypeResolver>();
builder.Services.AddSingleton<IBusinessObjectFactory, XxxBusinessObjectFactory>();
```

完整樣板(含 `ResolveDefinePath`、st_cache_notify materialize、master key fallback)見 `references/backend-bootstrap.md`。

### 3. 空 controller

```csharp
public class ApiController : Bee.Api.AspNetCore.Controllers.ApiServiceController { }
```
基底已宣告 `[Route("api")]` + POST handler,發布 `POST /api`,不需自己寫。

### 4. `Define/` 設定樹

五個 XML(`Program.cs` 靠 walk-up 從 `AppContext.BaseDirectory` 往上找到含 `SystemSettings.xml` 的資料夾)。
每個檔的角色與**最小內容**見 `references/define-config.md`。重點:
- `SystemSettings.xml` — payload codec(messagepack/gzip/aes-cbc-hmac)、master key 來源、**`<AllowedTypeNamespaces>`**(你的 args/result 命名空間要列進去)。
- `DatabaseSettings.xml` — `common`(框架強制)+ `company`,指向 SQLite 檔。
- `DbCategorySettings.xml`、`ProgramSettings.xml`、`TableSchema/`(含框架 `st_cache_notify`)。

### 5. demo 登入(免 seed st_user 快速起步)

框架 `SystemBusinessObject.AuthenticateUser` 預設回 false,所以 `System.Login` 一定要 override 才會過。三件套(照抄 Demo):
- `XxxCredentials`(硬編 demo user)
- `XxxAuthenticatingSystemBusinessObject : SystemBusinessObject`(override `AuthenticateUser(LoginArgs, out userName)`)
- `XxxBusinessObjectFactory : IBusinessObjectFactory`(`CreateSystemBusinessObject` 回上面那個)+ `XxxFormBoTypeResolver`

完整程式碼見 `references/business-object.md`。

### 6. 自訂 Business Object

自訂 RPC BO 繼承**最小的 `BusinessObject`**(`Bee.Business`),**不是** `FormBusinessObject`(那是 ERP
定義驅動表單 BO,帶 GetList/Save/Delete;一般 app 不需要)。詳見 `references/business-object.md`。

```csharp
public sealed class GameBO : BusinessObject
{
    // 4-arg ctor(factory 用 Activator.CreateInstance(type, ctx, token, progId, isLocalCall));
    // BusinessObject 基底只收 3-arg → progId 丟掉。
    public GameBO(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
        : base(ctx, accessToken, isLocalCall) { }

    [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
    public GetLevelsResult GetLevels(GetLevelsArgs args) => new() { /* ... */ };
}
```
- args/result 是**純 POCO**,繼承 `BusinessArgs`/`BusinessResult`(免 MessagePack 標記),放進 `AllowedTypeNamespaces`。
- **每個 action 必須標 `[ApiAccessControl]`**——無 attr 的方法會被框架直接拒絕(見避雷)。
- progId→BO 綁定二選一:resolver 的 `switch`(程式,AOT 友善;回傳任何有對應 ctor 的 `Type` 皆可),或 `ProgramSettings.xml` 的 `BusinessObject="Ns.GameBO, Asm"`(宣告式)。

### 7. client 呼叫

前端只需引 `Bee.Api.Client`。**別直接用 `FormApiConnector`**(ERP 表單專用)——仿它,繼承 `ApiConnector`
做一個**專屬 connector**,把每個領域 action 包成 typed 方法。詳見 `references/client.md`:

```csharp
public sealed class XxxApiConnector : ApiConnector
{
    public XxxApiConnector(string endpoint, Guid accessToken) : base(endpoint, accessToken) { }
    public Task<GetLevelsResponse> GetLevelsAsync() =>
        ExecuteAsync<GetLevelsResponse>("Game", "GetLevels", new GetLevelsRequest(), PayloadFormat.Plain);
}
// 呼叫端:
ApiClientInfo.ApiKey = "xxx-dev";                       // 任意非空即過預設驗證
var r = await new XxxApiConnector(endpoint, Guid.Empty).GetLevelsAsync();
```
- endpoint:桌面 `http://localhost:<port>/api`;**Android 模擬器 `10.0.2.2`**、iOS 模擬器 `localhost`;開發期需放行 cleartext。
- **wire DTO 依屬性名比對**,不必引用 server 的 BO 型別(enum 欄用 `string`,見避雷)。

## 避雷(這些會讓你卡很久)

- **`ApiServiceOptions.Initialize` 易漏**:它設 codec(序列化/壓縮/加密),`AddBeeFramework` **不**做這件事。漏了 → payload 行為錯誤。
- **每個 action 必標 `[ApiAccessControl]`**:框架對「無 attr(且 base/宣告類也無)」的方法一律拒絕(`UnauthorizedAccessException`)。這不是可選的。
- **`X-Api-Key` 永遠必填**,連 auth-exempt 方法(`System.Ping`/`System.Login`/`System.GetApiPayloadOptions`)也要;只是這三個免 `Authorization: Bearer`。
- **`AllowedTypeNamespaces` 漏列**:Encoded/Encrypted 走 typeless MessagePack,型別命名空間沒列進 `SystemSettings.xml` 會反序列化失敗。
- **factory / resolver 要在 `AddBeeFramework` 之後**用 `AddSingleton` 註冊(last-wins);之前註冊會被框架預設覆蓋。
- **Define 靠 walk-up 定位**:預設不 copy-to-output,要從 checkout 內跑(`dotnet run`),或自行把 Define 設 `CopyToOutputDirectory`。
- **master key**:dev 可 `autoCreateMasterKey: true` + 環境變數 `BEE_MASTER_KEY`(固定值讓加密列跨執行可解);正式環境必須由部署機制注入真 key。
- **介面成員隨版本增加**:如 4.14.0 的 `IBusinessObjectFactory` 多了 `CreateLogBusinessObject`——自訂 factory 要補上(委派框架 `LogBusinessObject`)。編譯錯誤會直接告訴你缺哪個。
- **wire DTO 反序列化**:client 對 Plain 回應用反射 STJ,且**沒註冊 `JsonStringEnumConverter`**——DTO 的 enum 欄位要宣告成 `string` 自己 parse,否則丟 `JsonException`(桌面/行動端都會中,見 `references/client.md`)。Release full-trim 另需 `TrimmerRootAssembly`。

## 驗證

1. `dotnet run --project <server>`(記住 dev port)。
2. 寫一個小 console(`Bee.Api.Client`,見 `references/client.md` 末的探針)呼 `System.Ping` + 你的第一個 action,確認真 JSON-RPC 往返成功——**這是最快、最可靠的驗證**,勝過 curl(curl 難組 ApiPayload 信封)。
3. 再接真前端 / 行動端。

## references/

- `backend-bootstrap.md` — `Program.cs` + `XxxBackend` 完整樣板(walk-up、materialize、master key、factory 註冊)。
- `define-config.md` — 五個 Define XML 的角色 + 可貼用最小內容。
- `business-object.md` — BO / args / result / demo 登入三件套 / resolver 完整程式碼。
- `client.md` — `Bee.Api.Client` 呼叫式、endpoint、探針、行動端反序列化注意事項。
