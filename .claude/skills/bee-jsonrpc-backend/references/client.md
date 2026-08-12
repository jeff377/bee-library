# Client 呼叫（Bee.Api.Client）

前端只需引 `Bee.Api.Client`。基於 `QuickStart.Console`,並已在實際專案的 client 上驗證過。

## 做一個專屬 connector(建議)

client↔server 的正規 seam 是 **`Bee.Api.Client.Connectors.ApiConnector`**(抽象基底,`protected
ExecuteAsync<T>(progId, action, value, format)`)。`FormApiConnector` 就是它的子類(綁 ProgId + 一堆
表單 CRUD 方法)。**自訂 app 別直接用 `FormApiConnector`**(那是給 ERP 表單的)——仿它,繼承 `ApiConnector`
做一個**專屬 connector**,把每個領域 action 包成 typed 方法:

```csharp
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages;   // PayloadFormat

public sealed class XxxApiConnector : ApiConnector
{
    public XxxApiConnector(string endpoint, Guid accessToken) : base(endpoint, accessToken) { }
    // 本機 in-process:base(accessToken) 這個多載

    // 一個 connector 可服務多個 ProgId(base 每次呼叫都帶 progId)。
    public Task<GetLevelsResponse> GetLevelsAsync() =>
        ExecuteAsync<GetLevelsResponse>("Game", "GetLevels", new GetLevelsRequest(), PayloadFormat.Plain);
    // 需登入的 action 用 PayloadFormat.Encrypted(先 System.Login 拿 token 傳給 ctor)。
}

// wire DTO 依屬性名比對,不必引用 server BO 型別。enum 欄用 string(見下)。
public sealed class GetLevelsRequest;
public sealed class GetLevelsResponse { public List<LevelDto> Levels { get; set; } = new(); }
public sealed class LevelDto { public string Name { get; set; } = ""; public string Difficulty { get; set; } = ""; /* ... */ }
```

好處:呼叫端(如你的 `IXxxApi` 實作)只依賴 `XxxApiConnector` 的 typed 方法,不碰 raw action 字串。

## System 呼叫

```csharp
using Bee.Api.Client;
using Bee.Api.Client.Connectors;

ApiClientInfo.ApiKey = "xxx-dev";   // 任意非空即過預設驗證(正式改真 key store)
var sys = new SystemApiConnector(endpoint, Guid.Empty);
await sys.PingAsync();              // System.Ping(匿名)
```
- `endpoint`:`http://<host>:<port>/api`。

## 需登入的呼叫

```csharp
var login = sys.Login("demo", "demo");        // 或用 ClientInfo/ApplyLoginResult 的高階封裝
var token = login.AccessToken;                // Guid
var bo = new FormApiConnector(endpoint, token, "Game");
var r = await bo.ExecuteAsync<TResult>("SomeAuthedAction", args, PayloadFormat.Encrypted);
```

## endpoint（各載具）

| 載具 | endpoint |
|---|---|
| 桌面 / 同機 | `http://localhost:<port>/api` |
| **Android 模擬器** | `http://10.0.2.2:<port>/api`（10.0.2.2 = 主機 loopback） |
| iOS 模擬器 | `http://localhost:<port>/api` |

開發期明文 HTTP：Android 需 `AndroidManifest` 的 `android:usesCleartextTraffic="true"` + `INTERNET` 權限；
iOS 需 `NSAppTransportSecurity` 放行 localhost。

```csharp
private static readonly string Endpoint = OperatingSystem.IsAndroid()
    ? "http://10.0.2.2:5180/api" : "http://localhost:5180/api";
```

## 驗證探針（最可靠）

獨立 console 專案（引 `Bee.Api.Client`），對本機 server 呼 Ping + 你的第一個 action。比 curl 可靠得多
（curl 難手組 ApiPayload 信封）。

```csharp
ApiClientInfo.ApiKey = "xxx-dev";
var sys = new SystemApiConnector("http://localhost:5180/api", Guid.Empty);
await sys.PingAsync();                                    // → ok
var game = new FormApiConnector("http://localhost:5180/api", Guid.Empty, "Game");
var r = await game.ExecuteAsync<GetLevelsResponse>("GetLevels", new GetLevelsRequest(), PayloadFormat.Plain);
Console.WriteLine($"{r.Levels.Count} levels");           // → 期望數量
```

## ⚠️ Wire DTO 反序列化(踩過的坑)

`ExecuteAsync<T>(..., PayloadFormat.Plain)` 的回應由**反射式 System.Text.Json** 反序列化
（`Bee.Api.Core/Conversion/ApiOutputConverter.ConvertResultValue<T>` → `JsonSerializer.Deserialize<T>(json,
new(){PropertyNameCaseInsensitive=true})`）——無 source-gen context。屬性名 **PascalCase**、大小寫不敏感。
（Plain 走 JSON;Encoded/Encrypted 才走 MessagePack,那時才看 `[MessagePackObject]`/`[Key]`。）

### 坑 1(最容易中):enum 欄位——client 反序列化選項**沒註冊 `JsonStringEnumConverter`**
server 端 `JsonCodec` **會**把 enum 序列化成**字串**(`"Master"`),但 client 的 `ConvertResultValue` 用的
`JsonSerializerOptions` **只**設了 `PropertyNameCaseInsensitive`,**沒有** enum string converter。於是 wire DTO
若把該欄位宣告成 `enum` 或 `int`,會丟 `JsonException: The JSON value could not be converted to ...
Path: $.xxx.difficulty`,整個回應反序列化失敗(在 fire-and-forget 的 load 裡會被吞成「空清單/空物件」的假象)。
**這與行動端無關——桌面同樣會中,只是若你的 DTO 剛好沒帶那個 enum 欄位就看不到。**

**修**:wire DTO 的 enum 欄位宣告成 `string`,對應時自己 parse:
```csharp
public string Difficulty { get; set; } = "";   // 收 "Master"
// 對應:
Difficulty = Enum.TryParse<LevelDifficulty>(w.Difficulty, ignoreCase: true, out var d) ? d : default,
```
（`DataTable`/`DataSet` 欄位同理——`ConvertResultValue` 也沒註冊那些 converter。回應 DTO 盡量用扁平、可 set 的基本型別 + string。）

### 坑 2:Release full-trim 會裁掉序列化 metadata
`Bee.Api.Core` **沒有** `ILLink.Descriptors.xml`(只有 `Bee.Definition` 有,見框架 `CHANGELOG` v4.12)。
Release 全連結(`TrimMode=full` / iOS/Android Release)下,linker 把回應 DTO 及其集合元素型別的 setter/ctor
裁掉,反射 STJ 靜默回預設值(桌面 `dotnet publish -p:PublishTrimmed=true -p:TrimMode=full` 可重現;會在
序列化端丟 `JsonSerializer.GetTypeInfo`)。**注意 Debug 的 `AndroidLinkMode=None` 不裁,所以 Debug 的空
結果通常是坑 1,不是這個。**
**修**:行動端 head csproj 把序列化相關 assembly 設為 trim root:
```xml
<ItemGroup>
  <TrimmerRootAssembly Include="Bee.Api.Core" />
  <TrimmerRootAssembly Include="Bee.Base" />
  <TrimmerRootAssembly Include="<你放 wire DTO 的 assembly>" />
  <TrimmerRootAssembly Include="<你放 domain 型別的 assembly>" />
</ItemGroup>
```

### 排查心法
1. 先用桌面 console 探針確認 server 正確。**探針 DTO 要涵蓋所有欄位(尤其 enum)**,否則會像我一樣被「剛好沒帶
   那欄」騙過,誤以為是行動端專屬問題。
2. 在 client 呼叫點 try/catch 把**例外訊息**記出來(logcat / 檔案),`JsonException` 的 `Path` 會直接指出是哪個
   欄位/型別不合——這比猜「trimming/Mono」快得多。
3. 確定是 enum/型別不合 → 改 wire DTO(坑 1);確定是 Release trim → 加 trim root(坑 2)。

> **驗證結論**:server + 桌面 client + **Android 裝置對真 server**(GetLevels)皆已跑通(裝置 hero 顯示 server 端
> 加的 `⋅LIVE` 標記證實)。上面兩坑都是實際踩過並修掉的。
