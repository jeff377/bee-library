# Business Object + demo 登入

改 `Xxx` 為專案名。基於 `QuickStart.Server` / `Bee.Samples.Shared`,並已在實際專案的 server 上驗證過。

## args / result（純 POCO）

放 `Xxx.Server.Contracts`（列入 `AllowedTypeNamespaces`）。繼承 `BusinessArgs` / `BusinessResult`，**免** MessagePack 標記。

```csharp
using Bee.Business;

namespace Xxx.Server.Contracts;

public sealed class GetLevelsArgs : BusinessArgs { }          // 空 args 也要有型別

public sealed class GetLevelsResult : BusinessResult
{
    public List<Level> Levels { get; set; } = [];            // 用可 set 的屬性
}
```

## 自訂 BO —— 選對基底

**兩種基底,依用途選**：

| 基底 | 何時用 | 帶來什麼 |
|---|---|---|
| **`BusinessObject`**(`Bee.Business`) | **自訂 RPC BO**——你只想暴露自己的 action | 最小基底,無 CRUD;乾淨 |
| `FormBusinessObject`(`Bee.Business.Form`) | **ERP 定義驅動表單**——要框架內建的 `GetList`/`GetData`/`Save`/`Delete` 對某張表 CRUD | 一整套 FormSchema 驅動的 CRUD action |

大多數「app 自己的業務端點」該用 **`BusinessObject`**;`FormBusinessObject` 是給制式資料表單的。

```csharp
using Bee.Business;                 // BusinessObject
using Bee.Definition;
using Bee.Definition.Attributes;    // ApiAccessControlAttribute
using Bee.Definition.Security;      // ApiProtectionLevel / ApiAccessRequirement
using Xxx.Server.Contracts;

namespace Xxx.Server.BusinessObjects;

public sealed class GameBO : BusinessObject
{
    // 4-arg ctor 必須對齊 factory 的 Activator.CreateInstance(type, ctx, token, progId, isLocalCall)。
    // BusinessObject 基底只收 (ctx, token, isLocalCall) → progId 丟掉即可(基底用不到)。
    public GameBO(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
        : base(ctx, accessToken, isLocalCall) { }

    // 每個 action 必標 [ApiAccessControl],否則被拒。單一 args 進、單一 result 出。
    [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
    public GetLevelsResult GetLevels(GetLevelsArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return new GetLevelsResult { Levels = /* seed 或 DB */ };
    }
}
```

> **注意 ctor 差異**:`BusinessObject` 基底是 3-arg `(ctx, token, isLocalCall)`,但 factory 的 `CreateFormBusinessObject`
> 路徑(所有非 System/AuditLog 的 progId 都走這)用 **4-arg** `Activator.CreateInstance`。所以你的 BO 仍要宣告 4-arg
> ctor(收下 progId 再丟給 3-arg base)。`FormBusinessObject` 則本身就是 4-arg。`IFormBoTypeResolver` 回傳任何有對應
> ctor 的 `Type` 都行,不限 `FormBusinessObject`。

**`ApiProtectionLevel`**：`Public`(明文可) / `Encoded`(序列化+壓縮) / `Encrypted`(再加密) / `LocalOnly`(僅本機)。
**`ApiAccessRequirement`**：`Anonymous`(免 token) / `Authenticated`(需登入)。
protection 對 client 送的 `PayloadFormat`：Plain 需 Public；Encrypted 允許非 LocalOnly。

## progId → BO：resolver（程式，AOT 友善）

```csharp
using Bee.Business;
using Bee.Business.Form;

namespace Xxx.Server.BusinessObjects;

public sealed class XxxFormBoTypeResolver : IFormBoTypeResolver
{
    public Type Resolve(string progId) => progId switch
    {
        "Game" => typeof(GameBO),
        _ => typeof(FormBusinessObject),   // 未知 progId → 框架預設定義驅動 CRUD
    };
}
```
（或走宣告式 `ProgramSettings.xml` 的 `BusinessObject=`，見 define-config.md。）

## demo 登入三件套

框架 `SystemBusinessObject.AuthenticateUser` 預設回 false → `System.Login` 一定要 override 才會過。
demo 硬編一組帳密、免 seed `st_user`。

### Credentials

```csharp
namespace Xxx.Server.Auth;

public static class XxxCredentials
{
    public const string UserId = "demo";
    public const string Password = "demo";
    public const string DisplayName = "Demo User";
}
```

### 認證 System BO

```csharp
using Bee.Business.System;
using Bee.Definition;

namespace Xxx.Server.Auth;

public sealed class XxxAuthenticatingSystemBusinessObject : SystemBusinessObject
{
    public XxxAuthenticatingSystemBusinessObject(IBeeContext ctx, Guid accessToken, bool isLocalCall = true)
        : base(ctx, accessToken, isLocalCall) { }

    protected override bool AuthenticateUser(LoginArgs args, out string userName)
    {
        if (args is { UserId: XxxCredentials.UserId, Password: XxxCredentials.Password })
        {
            userName = XxxCredentials.DisplayName;
            return true;
        }
        userName = string.Empty;
        return false;
    }
}
```

### 工廠

```csharp
using Bee.Business;
using Bee.Business.AuditLog;   // LogBusinessObject
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Storage;
using Xxx.Server.Auth;

namespace Xxx.Server.BusinessObjects;

public sealed class XxxBusinessObjectFactory : IBusinessObjectFactory
{
    private readonly IServiceProvider _services;
    private readonly IDefineAccess _defineAccess;
    private readonly ISessionInfoService _sessionInfoService;
    private readonly ILanguageService _languageService;
    private readonly IFormBoTypeResolver _resolver;

    public XxxBusinessObjectFactory(
        IServiceProvider services, IDefineAccess defineAccess,
        ISessionInfoService sessionInfoService, ILanguageService languageService,
        IFormBoTypeResolver resolver)
    {
        _services = services; _defineAccess = defineAccess;
        _sessionInfoService = sessionInfoService; _languageService = languageService;
        _resolver = resolver;
    }

    public object CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true)
        => new XxxAuthenticatingSystemBusinessObject(BuildContext(), accessToken, isLocalCall);

    public object CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true)
        => Activator.CreateInstance(_resolver.Resolve(progId), BuildContext(), accessToken, progId, isLocalCall)!;

    // 4.14.0 起 IBusinessObjectFactory 有此成員；委派框架預設。編譯錯誤會提示你缺哪個成員。
    public object CreateLogBusinessObject(Guid accessToken, bool isLocalCall = true)
        => new LogBusinessObject(BuildContext(), accessToken, isLocalCall);

    private BeeContext BuildContext() => new()
    {
        DefineAccess = _defineAccess,
        SessionInfoService = _sessionInfoService,
        LanguageService = _languageService,
        BoFactory = this,
        Services = _services,
    };
}
```

在 `AddBeeFramework` **之後** `AddSingleton<IFormBoTypeResolver, ...>()` + `AddSingleton<IBusinessObjectFactory, ...>()`（見 backend-bootstrap.md）。

## System 方法（框架內建，client 直接可用）

- `System.Ping`（匿名）→ 健康檢查
- `System.Login`（匿名）→ 帳密換 `AccessToken`(Guid) + 每 session 加密金鑰
- 之後呼叫帶 `Authorization: Bearer <token>`；auth-exempt：Ping / Login / GetApiPayloadOptions
