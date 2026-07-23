---
name: bee-add-bo-method
description: bee-library 新增對外公開的 BO 方法（FormBusinessObject / SystemBusinessObject）跨 contract / wire / BO / Repository / Client 共 7~8 個檔案的完整流程，含 3 條硬性規則、層別樣板、兩層 round-trip 測試與最終 checklist。當使用者要「新增 BO 方法」、「為 progId.action 加 API」、「FormBO/SystemBO 加方法」、「實作 GetList / Insert / Update / Delete」之類需求時使用。
---

# bee-library 新增 BO 方法

bee-library 為 JSON-RPC 2.0 + BO 雙軌架構，新增一個對外公開的 BO 方法
（如 `FormBusinessObject.GetList`、`SystemBusinessObject.GetDefine`）會跨越
4 層共 **7~8 個檔案**，命名、檔位置、`[Key]` 編號、DI 註冊方式都有固定慣例。
本 skill 把這條路徑寫死，避免每次摸索。

> 樣板對照（讀程式碼時對著看）：
> - System 軸：`SystemBusinessObject.GetDefine`
> - Form 軸：`FormBusinessObject.GetList`

## 硬性規則（不可違反）

這 3 條是專案架構底線，違反就是設計錯誤、不是「另一種寫法」。

### 規則 1：BO 嚴禁直接存取 `Bee.Db`

- `Bee.Business.csproj` **不**依賴 `Bee.Db`（dependency-map.md 顯示
  `Business --> Contracts / Definition / RepoAbs`，沒有 Db）
- FormSchema-driven 的 SELECT / INSERT / UPDATE / DELETE **必須**透過
  `IDataFormRepository`（Repository 抽象）執行
- BO 是「薄殼」：拆解 args、呼叫 Repository、組裝 Result；
  `FormSchema.MasterTable` / `ListFields` fallback 等邏輯放在 Repository，**不**在 BO 重複
- 看到 PR 把 `Bee.Db` ProjectReference 加進 `Bee.Business.csproj` → 設計錯誤

理由：解耦業務邏輯與資料存取，未來換 ORM / 加 sharding / 加快取 / 單元測試替換
都需要這層抽象。development-cookbook.md 寫的「FormBusinessObject →
IFormCommandBuilder → DbAccess」是**邏輯層次**的描述（BO 觸發、Repository 執行），
不是 ProjectReference 路徑。

### 規則 2：ProgId 即 master table 名稱，不傳 `TableName`

- 框架不變式：`FormSchema.MasterTable.TableName == ProgId`
  （見 `FormSchema.MasterTable` 取 `Tables[ProgId]`）
- BO 方法的 args / wire DTO / Repository 簽名**不**帶 `TableName` 屬性
- Repository 內以 `_schema.ProgId` 直接當 tableName 傳給 `SelectCommandBuilder.Build(...)`；
  不需 `MasterTable?.TableName` fallback
- 詳列子表查詢（detail / aux table）屬另一條方法（如 `GetDetailList`），
  不要為它而在 master 方法上多攜一個 `TableName` 欄位

理由：簡化合約面積，避免為支援罕見子表查詢而讓常見 master 查詢多攜一個欄位。

### 規則 3：Action 常數類別依 BO 軸分割

| BO 軸 | 常數類別 | 範例 |
|-------|---------|------|
| `SystemBusinessObject` | `Bee.Definition.SystemActions` | `Ping` / `Login` / `GetDefine` |
| `FormBusinessObject` | `Bee.Definition.FormActions` | `GetList` |
| 未來新軸 | `Bee.Definition.<Axis>Actions` | — |

- `SystemActions` 名稱已綁定 `SystemBusinessObject`；把 FormBO 的 action 塞進去
  會混淆「哪類 BO 處理這個 action」、IntelliSense 也會雜
- 為了「省一個檔案」把單一 action 塞進其他軸的類別 → 設計錯誤
- Client connector 引用對應軸的常數類別（`FormApiConnector` 用 `FormActions`，
  不用 `SystemActions`）
- **唯一例外**：`ExecFunc` / `ExecFuncAnonymous` / `ExecFuncLocal` 是 base `BusinessObject`
  的方法、不分軸，引用 `SystemActions.ExecFunc` 是歷史合理寫法

## 整體流程（4 層、7~8 檔）

| # | 層 | 檔案 | 慣例 |
|---|----|------|------|
| 1 | Contract | `src/Bee.Api.Contracts/I<Action>Request.cs` | 純介面，無 attribute |
| 2 | Contract | `src/Bee.Api.Contracts/I<Action>Response.cs` | 純介面，無 attribute |
| 3 | Wire DTO | `src/Bee.Api.Core/Messages/<Axis>/<Action>Request.cs` | `[MessagePackObject]` + `[Key(n)]`，繼承 `ApiRequest` |
| 4 | Wire DTO | `src/Bee.Api.Core/Messages/<Axis>/<Action>Response.cs` | `[MessagePackObject]` + `[Key(n)]`，繼承 `ApiResponse` |
| 5 | Action 常數 | `src/Bee.Definition/<Axis>Actions.cs` | `public const string <Action> = "<Action>"` |
| 6 | BO Args | `src/Bee.Business/<Axis>/<Action>Args.cs` | POCO，繼承 `BusinessArgs`，實作 `I<Action>Request` |
| 7 | BO Result | `src/Bee.Business/<Axis>/<Action>Result.cs` | POCO，繼承 `BusinessResult`，實作 `I<Action>Response` |
| 8 | BO method | `src/Bee.Business/<Axis>/<Axis>BusinessObject.cs` 新增方法 | `[ApiAccessControl(...)]`，回傳 `<Action>Result` |
| 8b | BO 介面宣告 | `src/Bee.Business/<Axis>/I<Axis>BusinessObject.cs` 加方法簽名 | 給其他 BO 經 `IBusinessObjectFactory` 取用 |
| (9) | Repository（FormSchema-driven CRUD 才需要） | `IDataFormRepository.cs` + `DataFormRepository.cs` + `FormRepositoryFactory.cs` | 走規則 1 |
| (10) | Client | `src/Bee.Api.Client/Connectors/<Axis>ApiConnector.cs` | `<Action>Async` + 同步 wrapper |

`<Axis>` = `System` 或 `Form`（也可未來新軸）。

## P0 探勘清單（read-only，不改碼）

新增方法前先回答這 3 題：

1. **BO 需要哪些服務**（DefineAccess / Repository / 其他）
   - `BusinessObject._ctx` 已暴露：`DefineAccess`、`SessionInfoService`、
     `BoFactory`、`Services`（IServiceProvider escape hatch）
   - 其他服務（如 `IFormRepositoryFactory`）走 `Services.GetRequiredService<T>()`；
     不要為單一方法擴 `IBeeContext` 公開簽章
   - 違反規則 1 的「直接呼 `IFormCommandBuilder` / `DbAccess`」**不要**做

2. **參數型別跨 wire 序列化能力**
   - 基本型別、`string`、`DataTable` 都有 MessagePack 支援
   - 多型 union（如 `FilterNode` → `FilterCondition` / `FilterGroup`）必須在
     抽象基類加 `[MessagePackObject]` + `[Union(n, typeof(T))]`
   - Collection 透過 `Bee.Api.Core/MessagePack/MessagePackCodec.cs:26` 的
     `CollectionBaseFormatter<,>` 處理 —— **必須**走 `MessagePackCodec` /
     `MessagePackPayloadSerializer` 公開 API，不可直接用
     `MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options)`
     （會吃掉 collection 元素且無錯誤）

3. **是否需要 `ApiContractRegistry.Register<>()`**
   - **預設不需要**。`Bee.Api.Core/Conversion/ApiOutputConverter.cs:74-86`
     的命名慣例反射 `XxxResult` → `XxxResponse`（限 `Bee.Api.Core` assembly）
     自動完成 BO Result → Wire Response 的 deep copy
   - `SysInfo.AllowedTypeNamespaces`（`Bee.Base/SysInfo.cs:49`）預設已含
     `Bee.Api.Core` / `Bee.Business` / `Bee.Definition`
   - 例外：BO Result 命名違反 `XxxResult` 慣例（少見）才需要顯式註冊

## 各層撰寫指引

### Layer 1: Contract 介面（`Bee.Api.Contracts`）

```csharp
namespace Bee.Api.Contracts
{
    /// <summary>Contract interface for the <Action> request.</summary>
    public interface I<Action>Request
    {
        // 只讀屬性 + 完整 XML doc
        SomeProperty { get; }
    }
}
```

- 介面**不**標 `[MessagePackObject]`、**不**寫實作
- `Bee.Api.Contracts.csproj` 已 reference `Bee.Definition`；可直接用
  `FilterNode` / `SortFieldCollection` / `DefineType` 等定義型別
- XML doc 用**英文**（公開 NuGet repo 慣例）

### Layer 2: Wire DTO（`Bee.Api.Core/Messages/<Axis>/`）

```csharp
[MessagePackObject]
public class <Action>Request : ApiRequest, I<Action>Request
{
    [Key(100)] public string Foo { get; set; } = string.Empty;
    [Key(101)] public Bar? Bar { get; set; }
}
```

- `Key` 從 **100** 起。0–99 留給 `ApiRequest`/`ApiResponse` base 欄位
- 命名與 contract 介面對齊：`<Action>Request` ↔ `<Action>Response`
- **預留未來欄位的 Key 編號要在 XML doc 註明**
- 屬性必須是 `{ get; set; }`（MessagePack 需要 setter）；介面是 read-only
- 資料夾與命名空間必須一致：`Messages/Form/` ↔ `namespace Bee.Api.Core.Messages.Form`
  （IDE0130，違反 strict build 失敗）

### Layer 3: Action 常數（`Bee.Definition/<Axis>Actions.cs`）

```csharp
public static class <Axis>Actions
{
    /// <summary><動作說明></summary>
    public const string <Action> = "<Action>";
}
```

- 一個 axis 一個檔案（規則 3）
- 與 JSON-RPC `method` field 對齊：`"<progId>.<Action>"`

### Layer 4: BO Args / Result（`Bee.Business/<Axis>/`）

```csharp
public class <Action>Args : BusinessArgs, I<Action>Request
{
    public string Foo { get; set; } = string.Empty;
    // 與 Wire DTO 同欄位，但不標 [Key]
}

public class <Action>Result : BusinessResult, I<Action>Response
{
    public DataTable? Table { get; set; }
}
```

- BO POCO 不標 MessagePack attribute（命名慣例 `XxxResult` → `XxxResponse`
  在 `ApiOutputConverter` 反射時就由 wire 端的 `[MessagePackObject]` 接手）
- 欄位名要與 Wire DTO 對齊（`ApiInputConverter.Convert` 用反射 copy 屬性，
  名稱不對等於該欄位送丟）

### Layer 5: BO method（走 Repository，遵守規則 1）

```csharp
/// <summary><英文方法說明></summary>
/// <remarks>
/// （視需要說明硬性使用前提，如 GetList 要求呼叫端提供 Filter 限縮結果）
/// </remarks>
[ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
public virtual <Action>Result <Action>(<Action>Args args)
{
    ArgumentNullException.ThrowIfNull(args);

    var factory = Services.GetRequiredService<IFormRepositoryFactory>();
    var repository = factory.CreateDataFormRepository(ProgId);
    var result = repository.<DoWork>(args.X, args.Y, ...);

    return new <Action>Result { /* 從 repository 結果組裝 */ };
}
```

- `virtual` 開放 host 端 override（多租戶、特化邏輯）
- `[ApiAccessControl]` 三選一：`Public/Encoded/Encrypted` × `Anonymous/Authenticated`
- BO 不重複實作 fallback 邏輯（fallback 屬於 Repository）

### Layer 8b: BO 介面宣告（`I<Axis>BusinessObject.cs`）

BO 公開方法有兩種使用情境：

1. **API 呼叫**：透過 `JsonRpcExecutor` 反射 `progId.action` 派發，**只需 BO 實體有 `public` method + `[ApiAccessControl]`**，不需在介面宣告
2. **跨 BO 呼叫**：另一個 BO 透過 `IBusinessObjectFactory.CreateXxxBusinessObject(...)` 取得實體後直接呼叫 method，這時呼叫端拿到的是 `I<Axis>BusinessObject` 型別 → **method 必須在介面上**才能呼叫

**慣例：給 API 用的方法，預設一併開放給其他 BO 用** —— 在 `I<Axis>BusinessObject` 加上對應簽名。

```csharp
// src/Bee.Business/Form/IFormBusinessObject.cs
public interface IFormBusinessObject : IBusinessObject
{
    /// <summary><英文方法說明></summary>
    /// <param name="args">The input arguments.</param>
    <Action>Result <Action>(<Action>Args args);
}
```

- 介面方法**不**標 `[ApiAccessControl]`（attribute 由 BO 實體承擔）
- 簽名與 BO 實作完全一致（C# 用 signature 自動 match）
- 既存 `ISystemBusinessObject` 的 `CreateSession` / `GetDefine` / `SaveDefine` 是樣板
- 例外：純 API 用、不適合給其他 BO 直接呼叫的（如 `Ping` 健康檢查、`Login`）可不放介面 —— 屬人工判斷

#### 跨 BO 呼叫的實際寫法

`IBusinessObjectFactory` 在 `Bee.Definition`（下層），`I<Axis>BusinessObject` 在 `Bee.Business`（上層）。為避免 `Bee.Definition → Bee.Business` 反向相依，factory method 一律宣告回傳 `object`。

**呼叫端在 `Bee.Business` 或更高層時**，用 `BusinessObjectFactoryExtensions`（位於 `Bee.Business`）提供的 typed wrapper 直接拿介面、不必自己 cast：

```csharp
// 在另一個 BO method 內 — 推薦寫法
var formBo = _ctx.BoFactory.CreateFormBO(AccessToken, "Employee");
var listResult = formBo.GetList(new GetListArgs { /* ... */ });

var systemBo = _ctx.BoFactory.CreateSystemBO(AccessToken);
var defineResult = systemBo.GetDefine(new GetDefineArgs { /* ... */ });
```

每個 axis 對應一個固定介面（`IFormBusinessObject` / `ISystemBusinessObject`），實體之間以 `progId` 在 runtime 區分；沒有「特化 BO 介面」的設計，因此不需要泛型多載。

**呼叫端在不能引用 `Bee.Business` 的專案時**（例如 `Bee.Api.Core` 內部派發路徑），仍直接用 `IBusinessObjectFactory.CreateXxxBusinessObject(...)` 拿 `object` 後 cast：

```csharp
var rawBo = factory.CreateFormBusinessObject(token, progId);
// 此情境下通常不會 cast 到 IFormBusinessObject（會引入 Bee.Business 依賴），
// 而是經由 reflection / dynamic dispatch 操作。
```

- 設計理由詳見 `Bee.Definition/IBusinessObjectFactory.cs` 的 XML doc
- cast 失敗會丟 `InvalidCastException` —— 確保 host 端註冊正確的 BO 型別
- 不要為了「免 cast」把 `IBusinessObjectFactory` 搬到 `Bee.Business`，會把整個依賴拓撲翻轉
- 不要用 default interface method 在 `IBusinessObjectFactory` 上加泛型 method（如 `T CreateForm<T>()`）—— constraint 無法引用 `IFormBusinessObject`，型別安全僅靠 `where T : class`；呼叫端要明寫 `<IFormBusinessObject>` 反而易錯（`FormBO` 一定實作 `IFormBusinessObject`，這個對應該由 helper 寫死、不是讓 caller 重新指定）

### Layer 9（FormSchema-driven CRUD 才需）: Repository 抽象擴充

1. **`Bee.Repository.Abstractions/Form/IDataFormRepository.cs`** 加方法簽名
   - 參數型別限 `Bee.Definition.*`（RepoAbs 只依賴 Definition）
   - **不**接受 `Bee.Business.*` 型別（會反向依賴）
2. **`Bee.Repository/Form/DataFormRepository.cs`** 實作（這層可用 `Bee.Db`）
   - ctor 接 `FormSchema schema`, `IDefineAccess defineAccess`,
     `IDbAccessFactory dbAccessFactory`, `IDbConnectionManager connectionManager`,
     `string databaseId`
   - 取 dialect：`DbDialectRegistry.Get(connInfo.DatabaseType)
     .CreateFormCommandBuilder(_schema, _defineAccess)`
   - tableName 直接傳 `_schema.ProgId`（規則 2）
   - 執行：`_dbAccessFactory.Create(_databaseId).Execute(spec)`
3. **`Bee.Repository/Factories/FormRepositoryFactory.cs`** 已注入所需服務
   - `CreateDataFormRepository(progId)` 解析 schema、決定
     `databaseId = schema.CategoryId`（多租戶 host 端覆寫）
4. **`Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs`** 的
   `IFormRepositoryFactory` 註冊**必須**用 `CreateConfigurableService`
   （DI-aware），不是 `CreateOrDefault`（parameterless）。Factory ctor 改簽
   名時若忘記同步換 → runtime InvalidOperationException

### Layer 10: Client connector（`Bee.Api.Client/Connectors/<Axis>ApiConnector.cs`）

```csharp
public async Task<<Action>Response> <Action>Async(
    <param1> p1 = default!, ..., <paramN>? pN = null)
{
    var request = new <Action>Request { /* 屬性 = parameter */ };
    return await ExecuteAsync<<Action>Response>(<Axis>Actions.<Action>, request)
        .ConfigureAwait(false);
}

public <Action>Response <Action>(
    <param1> p1 = default!, ..., <paramN>? pN = null)
{
    return SyncExecutor.Run(() => <Action>Async(p1, ..., pN));
}
```

- 一定有 async + 同步兩個 overload
- `ExecuteAsync<TResponse>` 預設 `PayloadFormat.Encrypted`；除非 BO method 標
  `ApiProtectionLevel.Encoded` 才在這層 override
- 例外傳遞：JsonRpcExecutor 把 BO 拋的 `ArgumentException /
  InvalidOperationException / NotSupportedException / FormatException /
  JsonRpcException` 視為 user-facing 轉成 RpcError 帶原訊息；其他系統例外
  （含 NRE、IO）統一收斂為 `"Internal server error"`

## 測試（三層覆蓋）

| 層 | 路徑 / 樣板 | 驗證重點 | 不驗證 |
|----|------------|---------|--------|
| Wire-level | `tests/Bee.Api.Core.UnitTests/<Axis>/<Action>MessagePackTests.cs` | `[Key]` 編號 / union / collection / DataTable 序列化 | BO 行為、SQL |
| Executor dispatch | `tests/Bee.Api.Core.UnitTests/<Axis>/<Action>JsonRpcRoundTripTests.cs` | `progId.action` 反射派發、Input/Output Converter | 實際 SQL 結果 |
| BO+DB integration | `tests/Bee.Business.UnitTests/<Axis>/<Axis>BusinessObject<Action>Tests.cs` | BO → Repository → SQL → 真實資料正確性 | wire format |

三層加起來才是「end-to-end 工作」。**不要**寫單支「executor + 真實 DB」的整合測試
—— 過度耦合、重複覆蓋既有層的職責。

### Wire-level 測試樣板

```csharp
public class <Action>MessagePackTests
{
    [Fact]
    public void <Action>Request_RoundTrip_PreservesUnion()
    {
        var request = new <Action>Request { /* fully populated */ };
        var bytes = MessagePackCodec.Serialize(request);
        var restored = MessagePackCodec.Deserialize<<Action>Request>(bytes);
        // Assert union / collection / DataTable round-trip
    }

    [Fact]
    public void <Action>Request_DefaultValues_RoundTrip() { /* 確認 null collection 不 NRE */ }
}
```

### Executor dispatch 測試樣板（stub Repository）

```csharp
public class <Action>JsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
{
    [Fact]
    public void <Action>_ThroughJsonRpc_DispatchesAndReturnsTable()
    {
        var stubRepo = new StubDataFormRepository(/* fixed DataTable */);
        var stubFactory = new StubFormRepositoryFactory(stubRepo);
        var overrideServices = new TestOverrideServiceProvider(
            _fx.Provider,
            (typeof(IFormRepositoryFactory), stubFactory));

        var boFactory = new BusinessObjectFactory(
            overrideServices,
            _fx.GetRequiredService<IDefineAccess>(),
            _fx.GetRequiredService<ISessionInfoService>(),
            _fx.GetRequiredService<IFormBoTypeResolver>());

        var executor = new JsonRpcExecutor(
            boFactory,
            _fx.GetRequiredService<IAccessTokenValidator>(),
            _fx.GetRequiredService<IApiEncryptionKeyProvider>())
        {
            AccessToken = TestSessionFactory.CreateAccessToken(_fx),
            IsLocalCall = true,
        };

        var request = new JsonRpcRequest
        {
            Method = $"<ProgId>.{<Axis>Actions.<Action>}",
            Params = new JsonRpcParams { Value = new <Action>Request { /* ... */ } },
            Id = Guid.NewGuid().ToString(),
        };

        var response = executor.Execute(request);
        Assert.Null(response.Error);
        var result = Assert.IsType<<Action>Response>(response.Result!.Value);
        // ... assertions on result + stub.LastXxx
    }
}
```

### BO + DB integration 測試樣板

```csharp
public class <Axis>BusinessObject<Action>Tests : IClassFixture<SharedDbFixture>
{
    [DbFact(DatabaseType.SQLite)]
    public void <Action>_Sqlite_<情境>()
    {
        // 用 TestDbConventions.GetDatabaseId(dbType, categoryId) 取測試 databaseId
        // 構造 DataFormRepository 並用 TestOverrideServiceProvider 注入到 BO Context
        // 種子 InsertCommandBuilder 種測試資料，try/finally 清理
        // 呼叫 bo.<Action>(args)，斷言回傳 DataTable
    }
}
```

- 測試環境的 databaseId 是 `{categoryId}_{dbtype}`（如 `company_sqlite`），
  與 prod 的 `CategoryId` 直接當 databaseId 不一致 —— 必須在測試中明確傳
  測試 databaseId（不依賴 production Factory 的解析）
- 種子/清理沿用 `EmployeeBuildSelectIntegrationTests` 的 try/finally + GUID
  RowId pattern；用 `InsertCommandBuilder` / `DeleteCommandBuilder` 跨 dialect
  種資料

## 容易踩的坑

撰寫過程中容易遇到、CI 必擋：

1. **`Bee.Business` 加 `Bee.Db` ProjectReference = 違反規則 1**：表示在 BO 裡
   直接呼叫 Db；改走 Repository 抽象
2. **資料夾與命名空間不一致（IDE0130）**：`Messages/Form/` 必須對應
   `namespace Bee.Api.Core.Messages.Form`
3. **Key 編號重複**：同一 DTO 內 `[Key(n)]` 不可重複，含 base class；100 起算
4. **BO Result 名稱違反 `XxxResult` 慣例**：`ApiOutputConverter` 反射 miss，
   要嘛改名要嘛在 `Bee.Hosting.AddBeeFramework` 補
   `ApiContractRegistry.Register<I, T>()`
5. **多型 union 沒標 `[Union]`**：MessagePack 反序列化時 collection 元素全變
   null；必須在抽象基類加 `[Union(0, typeof(ConcreteA))]` 等
6. **直接用 `MessagePackSerializer.Serialize(..., ContractlessStandardResolver.Options)`
   繞過框架**：collection 元素掉光、無錯誤拋出。一律走 `MessagePackCodec` /
   `MessagePackPayloadSerializer`
7. **`ApiInputConverter` 用屬性名比對**：BO Args 屬性名與 Wire DTO 不對齊 →
   該欄位送丟，無編譯錯誤
8. **`SysInfo.AllowedTypeNamespaces` 白名單外的型別**：跨 wire 反序列化會被
   `SafeTypelessFormatter` 擋下；用 `Bee.Api.Core` / `Bee.Business` /
   `Bee.Definition` 內的型別最安全
9. **`FormRepositoryFactory` ctor 簽名改 → DI 註冊要同步換**：
   `CreateOrDefault` (parameterless) → `CreateConfigurableService` (DI-aware)
10. **既有 trivial POCO 屬性測試在 ctor 簽名變寬時直接刪**：當測試只是
    `new T(progId).ProgId == progId` 這種 setter 檢查，重構後改維護成本高、
    無實質覆蓋；整合測試承接覆蓋率
11. **`[DbFact]` 只看環境變數，不檢查 DB 可達性**：`.runsettings` 設了
    `BEE_TEST_CONNSTR_SQLSERVER` 但 SQL Server container 沒跑 → 測試會失敗
    而非 skip。SQLite in-memory 沒這問題（無外部依賴）
12. **process-wide static 在測試類別間 race**：`SysInfo.TraceListener` 一旦被
    某測試類別指向 capture writer，所有並行測試類別的 Tracer 事件都會寫進
    那個 writer；其後設用 `ConcurrentQueue<T>` 等執行緒安全容器，不要用
    `List<T>`
13. **`CollectionBaseFormatter` 對 null collection 行為**：曾有 bug 導致
    `SortFieldCollection? = null` 序列化 NRE，2026-05-14 已修正為 nil 進 nil
    出。新增 nullable collection 欄位前確認此修正仍在
14. **`FormApiConnector` ExecFunc 系列引用 `SystemActions`** — 歷史合理寫法
    （`ExecFunc` 是 base BO 方法、不分軸），這是規則 3 的**唯一**例外。
    新增 FormBO 專屬 method 一律用 `FormActions.<Action>`

## 完整 checklist（合併所有層）

新增一個 `<Action>` 方法時，依序完成下面所有步驟。

**P0 探勘（read-only）**：
- [ ] 確認 BO 取所需服務的路徑（Repository 抽象 / `Services.GetRequiredService<T>()`）
- [ ] 序列化能力：基本型別 / collection / union 全部走過
- [ ] `ApiContractRegistry` 不需顯式註冊（除非 BO Result 命名違反 `XxxResult` 慣例）

**P1 合約層**（單 PR / 單 commit）：
- [ ] `src/Bee.Api.Contracts/I<Action>Request.cs`
- [ ] `src/Bee.Api.Contracts/I<Action>Response.cs`
- [ ] `src/Bee.Api.Core/Messages/<Axis>/<Action>Request.cs`
- [ ] `src/Bee.Api.Core/Messages/<Axis>/<Action>Response.cs`
- [ ] `src/Bee.Definition/<Axis>Actions.cs` 加 `public const string <Action>`
- [ ] Release build 0w/0e

**P2 BO + Repository**（單 PR / 單 commit）：
- [ ] `src/Bee.Business/<Axis>/<Action>Args.cs`
- [ ] `src/Bee.Business/<Axis>/<Action>Result.cs`
- [ ] `src/Bee.Business/<Axis>/<Axis>BusinessObject.cs` 加 method（走 Repository）
- [ ] `src/Bee.Business/<Axis>/I<Axis>BusinessObject.cs` 加方法簽名（除非該 method 是純 API 用、不適合給其他 BO 直接呼叫）
- [ ] `src/Bee.Repository.Abstractions/Form/IDataFormRepository.cs` 加抽象方法
- [ ] `src/Bee.Repository/Form/DataFormRepository.cs` 實作
- [ ] `src/Bee.Repository/Factories/FormRepositoryFactory.cs` 注入新依賴（如有）
- [ ] `src/Bee.Hosting/...` DI 註冊若 ctor 簽名變更 → 改為 `CreateConfigurableService`
- [ ] BO 整合測試：`tests/Bee.Business.UnitTests/<Axis>/<Axis>BusinessObject<Action>Tests.cs`
- [ ] Release build 0w/0e + SQLite 測試通過

**P3 Client + wire 測試**（單 PR / 單 commit）：
- [ ] `src/Bee.Api.Client/Connectors/<Axis>ApiConnector.cs` 加 `<Action>Async` + 同步包裝
- [ ] Wire-level round-trip 測試
- [ ] Executor dispatch round-trip 測試
- [ ] Release build 0w/0e + 測試通過

**P4 Surface 同步**（與 P2 / P3 任一同 commit）：
- [ ] 更新 `docs/api-method-reference.md` + `docs/api-method-reference.zh-TW.md` 對應軸的表格新增 / 修改該方法
- [ ] 更新 `tests/Bee.Business.UnitTests/BoApiSurfaceTests.cs` 的 `ExpectedSurface` baseline（新增、移除、或 `[ApiAccessControl]` 改動都要動）
- [ ] BoApiSurfaceTests 通過（驗證 baseline 與實際反射結果同步）

**Commit + push**：
- [ ] 桌面環境直接 commit 到 main，push 後等 CI 跑完並回報
- [ ] 失敗時依 `rules/pull-request.md` 流程處理（明確可修則直接修復、commit、push）

## 參考檔案（讀程式碼對著看）

| 用途 | 檔案 |
|------|------|
| Contract 介面樣板 | `src/Bee.Api.Contracts/IGetDefineRequest.cs` / `IGetDefineResponse.cs` |
| Wire DTO 樣板 | `src/Bee.Api.Core/Messages/System/GetDefineRequest.cs` / `GetDefineResponse.cs` |
| Action 常數樣板 | `src/Bee.Definition/SystemActions.cs` / `FormActions.cs` |
| BO Args/Result 樣板 | `src/Bee.Business/System/GetDefineArgs.cs` / `GetDefineResult.cs` |
| BO 方法樣板 | `src/Bee.Business/System/SystemBusinessObject.cs`（`GetDefine`） / `src/Bee.Business/Form/FormBusinessObject.cs`（`GetList`） |
| Repository 抽象 / 實作 | `src/Bee.Repository.Abstractions/Form/IDataFormRepository.cs` / `src/Bee.Repository/Form/DataFormRepository.cs` |
| Repository Factory | `src/Bee.Repository/Factories/FormRepositoryFactory.cs` |
| Client connector | `src/Bee.Api.Client/Connectors/SystemApiConnector.cs` / `FormApiConnector.cs` |
| JSON-RPC dispatch | `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` |
| 命名慣例反射 | `src/Bee.Api.Core/Conversion/ApiOutputConverter.cs` |
| BO 整合測試樣板 | `tests/Bee.Business.UnitTests/Form/FormBusinessObjectGetListTests.cs` |
| Wire round-trip 樣板 | `tests/Bee.Api.Core.UnitTests/Form/GetListMessagePackTests.cs` |
| Executor dispatch 樣板 | `tests/Bee.Api.Core.UnitTests/Form/GetListJsonRpcRoundTripTests.cs` |
| API method 單頁總覽 | `docs/api-method-reference.md`（每加新方法須同步） |
| Surface audit 測試 | `tests/Bee.Business.UnitTests/BoApiSurfaceTests.cs` |
