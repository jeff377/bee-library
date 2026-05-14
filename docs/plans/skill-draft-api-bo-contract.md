# Skill 草稿：bee-library 新增 BO 方法 + API 合約流程

**狀態：📝 草稿（隨 plan-formbo-getlist.md P1–P3 增補；P3 完成後整理為正式 skill）**

## 目的

bee-library 為 JSON-RPC 2.0 API + BO 雙軌架構，新增一個對外開放的 BO 方法（例如
`FormBusinessObject.GetList`、`SystemBusinessObject.GetDefine`）會跨越 4 層共
**7~8 個檔案**，命名與檔位置都有固定慣例。本文件把這條路徑記下，避免每次新增
方法時重新摸索。

> 樣板對照：`SystemBusinessObject.GetDefine`（System 軸）、
> `FormBusinessObject.GetList`（Form 軸，本 plan 正在新增）。

## 硬性規則（不可違反）

這兩條是專案架構底線，違反就是設計錯誤、不是「另一種寫法」：

### 規則 1：BO 嚴禁直接存取 `Bee.Db`

- `Bee.Business.csproj` **不**依賴 `Bee.Db`（[dependency-map.md:46](../dependency-map.md:46)
  `Business --> Contracts / Definition / RepoAbs`，沒有 Db）
- FormSchema-driven 的 SELECT / INSERT / UPDATE / DELETE **必須**透過
  `IDataFormRepository`（Repository 抽象）執行
- BO 是「薄殼」：拆解 args、呼叫 Repository、組裝 Result。`FormSchema.MasterTable`
  / `ListFields` fallback 等邏輯放在 Repository，**不**在 BO 重複
- 看到 PR 把 `Bee.Db` ProjectReference 加進 `Bee.Business.csproj` → 設計錯誤

理由：解耦業務邏輯與資料存取，未來換 ORM / 加 sharding / 加快取 / 單元測試
替換都需要這層抽象。[development-cookbook.md:248-258](../development-cookbook.md:248)
的「FormBusinessObject → IFormCommandBuilder → DbAccess」是**邏輯層次**的
描述（BO 觸發、Repository 執行），不是 ProjectReference 路徑。

### 規則 2：ProgId 即 master table 名稱，不傳 `TableName`

- 框架不變式：`FormSchema.MasterTable.TableName == ProgId`
- BO 方法的 args / wire DTO / Repository 簽名**不**帶 `TableName` 屬性 ——
  ProgId 已經夠唯一決定目標 master table
- Repository 內以 `_schema.ProgId` 直接當 tableName 傳給 `SelectCommandBuilder
  .Build(...)`；不需 `MasterTable?.TableName` fallback
- 詳列子表查詢（detail / aux table）屬於另一條方法（如未來的
  `GetDetailList`），不要把它擠進 GetList 的簽名

理由：簡化合約面積，避免「為支援罕見子表查詢而讓常見 master 查詢多攜一個欄位」
的設計稅。

### 規則 3：Action 常數類別依 BO 軸分割

| BO 軸 | 常數類別 | 範例 |
|-------|---------|------|
| `SystemBusinessObject` | `Bee.Definition.SystemActions` | `Ping` / `Login` / `GetDefine` |
| `FormBusinessObject` | `Bee.Definition.FormActions` | `GetList` |
| 未來新軸 | `Bee.Definition.<Axis>Actions` | — |

- `SystemActions` 名稱已綁定 `SystemBusinessObject` 的語意；把 FormBO 的 action
  塞進去會混淆「哪類 BO 處理這個 action」、IntelliSense 也會雜
- 為了「省一個檔案」把單一 action 塞進其他軸的類別 → 設計錯誤
- Client connector 引用對應軸的常數類別（`FormApiConnector` 用 `FormActions`，
  不用 `SystemActions`）

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
| (9) | Client | `src/Bee.Api.Client/Connectors/<Axis>ApiConnector.cs` | `<Action>Async` + 同步 wrapper |

`<Axis>` = `System` 或 `Form`（也可未來新軸）。

## P0 探勘清單

新增方法前先回答這 3 題（read-only，不改碼）：

1. **BO 需要哪些服務**（DefineAccess / IDbAccessFactory / FormCommandBuilder / Repository...）
   - `BusinessObject._ctx`（[BusinessObject.cs:14](../../src/Bee.Business/BusinessObject.cs:14)）
     已暴露：`DefineAccess`、`SessionInfoService`、`BoFactory`、`Services`
     （IServiceProvider escape hatch）
   - 其他服務（如 `IDbAccessFactory`）走 `Services.GetRequiredService<T>()`；
     不要為單一方法擴 `IBeeContext` 公開簽章
   - `IFormCommandBuilder` 例外：靜態 `DbDialectRegistry.Get(databaseType)
     .CreateFormCommandBuilder(schema, defineAccess)` —— 不走 DI
2. **參數型別跨 wire 序列化能力**
   - 基本型別、`string`、`DataTable` 都有 MessagePack 支援
   - 多型 union（如 `FilterNode` → `FilterCondition` / `FilterGroup`）必須在
     抽象基類加 `[MessagePackObject]` + `[Union(n, typeof(T))]`
   - Collection 透過 [MessagePackCodec.cs:26](../../src/Bee.Api.Core/MessagePack/MessagePackCodec.cs:26)
     的 `CollectionBaseFormatter<,>` 處理 —— **必須**走 `MessagePackCodec` /
     `MessagePackPayloadSerializer` 公開 API，不可直接用 `MessagePackSerializer
     .Serialize(obj, ContractlessStandardResolver.Options)`（會吃掉 collection
     元素且無錯誤）
3. **是否需要 ApiContractRegistry.Register<>()**
   - **預設不需要**。[ApiOutputConverter.cs:74](../../src/Bee.Api.Core/Conversion/ApiOutputConverter.cs:74)
     有命名慣例反射：`XxxResult` → `XxxResponse`（限 `Bee.Api.Core` assembly）
     自動完成 BO Result → Wire Response 的 deep copy
   - `SysInfo.AllowedTypeNamespaces`（[SysInfo.cs:49](../../src/Bee.Base/SysInfo.cs:49)）
     預設已含 `Bee.Api.Core` / `Bee.Business` / `Bee.Definition`
   - 例外：BO Result 命名違反慣例（不以 `Result` 結尾）才需要顯式註冊

## 各層撰寫指引

### Layer 1: Contract 介面（`Bee.Api.Contracts`）

```csharp
namespace Bee.Api.Contracts
{
    public interface I<Action>Request
    {
        // 只讀屬性 + 完整 XML doc。值型別與 nullable 引用型別都標清楚
        SomeProperty { get; }
    }
}
```

- 介面**不**標 `[MessagePackObject]`、**不**寫實作
- `Bee.Api.Contracts.csproj` 已 reference `Bee.Definition`；可直接用
  `FilterNode` / `SortFieldCollection` / `DefineType` 等定義型別

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
- **預留未來欄位的 Key 編號要在 XML doc 註明**（如 plan-formbo-getlist 在
  Request 預留 `Key(103)`、Response 預留 `Key(101)` 給分頁）
- 屬性必須是 `{ get; set; }`（MessagePack 需要 setter）；介面是 read-only

### Layer 3: Action 常數（`Bee.Definition/<Axis>Actions.cs`）

```csharp
public static class <Axis>Actions
{
    public const string <Action> = "<Action>";
}
```

- 與 JSON-RPC `method` field 對齊：`"<progId>.<Action>"`
- 一個 axis 對應一個檔案（`SystemActions`、`FormActions`...）

### Layer 4: BO Args/Result（`Bee.Business/<Axis>/`）

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

### Layer 5: BO method

```csharp
[ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
public virtual <Action>Result <Action>(<Action>Args args)
{
    ArgumentNullException.ThrowIfNull(args);
    // ... 業務邏輯
    return new <Action>Result { ... };
}
```

- `virtual` 開放 host 端 override（多租戶、特化邏輯）
- `[ApiAccessControl]` 三選一：`Public/Encoded/Encrypted` × `Anonymous/Authenticated`
  （見 `.claude/rules/security.md`）
- XML `<remarks>` 寫硬性使用前提（呼叫端必填條件、效能限制）

### Layer 6: Client connector（`Bee.Api.Client/Connectors/`）

```csharp
public async Task<<Action>Response> <Action>Async(...)
{
    var request = new <Action>Request { ... };
    return await ExecuteAsync<<Action>Response>(<Axis>Actions.<Action>, request)
        .ConfigureAwait(false);
}

public <Action>Response <Action>(...) => SyncExecutor.Run(() => <Action>Async(...));
```

- 一定有 async + 同步兩個 overload
- `ExecuteAsync<TResponse>` 預設 `PayloadFormat.Encrypted`；除非 BO method 標
  `ApiProtectionLevel.Encoded` 才在這層 override

## 測試清單

| 測試類 | 路徑 | 內容 |
|--------|------|------|
| BO POCO | `tests/Bee.Business.UnitTests/<Axis>/<Action>ArgsTests.cs` | 純屬性 getter/setter |
| BO method | `tests/Bee.Business.UnitTests/<Axis>/<Axis>BusinessObject<Action>Tests.cs` | `IClassFixture<BeeTestFixture>` 或 `SharedDbFixture`（依需要 DB） |
| API round-trip | `tests/Bee.Api.Core.UnitTests/<Axis>/<Action>JsonRpcRoundTripTests.cs` | 走 `JsonRpcExecutor`，模擬 wire format |

## 容易踩的坑

1. **資料夾與命名空間一致性（IDE0130）**：`Messages/Form/` 必須對應
   `namespace Bee.Api.Core.Messages.Form`，違反會在 strict build 失敗
2. **Key 編號重複**：同一 DTO 內 `[Key(n)]` 不可重複，含 base class；100 起算
   是慣例
3. **BO Result 名稱違反 `XxxResult` 慣例**：`ApiOutputConverter` 反射就 miss，
   要嘛改名要嘛在 `Bee.Hosting.AddBeeFramework` 補
   `ApiContractRegistry.Register<I, T>()`
4. **多型 union 沒標 `[Union]`**：MessagePack 反序列化時 collection 元素全變
   null；必須在抽象基類加 `[Union(0, typeof(ConcreteA))]` 等
5. **直接用 `MessagePackSerializer.Serialize(...,
   ContractlessStandardResolver.Options)` 繞過框架**：collection 元素掉光、
   無錯誤拋出。一律走 `MessagePackCodec` / `MessagePackPayloadSerializer`
6. **`ApiInputConverter` 用屬性名比對**：BO Args 屬性名與 Wire DTO 不對齊 →
   該欄位送丟，無編譯錯誤
7. **`SysInfo.AllowedTypeNamespaces` 白名單外的型別**：跨 wire 反序列化會被
   `SafeTypelessFormatter` 擋下；用 `Bee.Api.Core` / `Bee.Business` /
   `Bee.Definition` 內的型別最安全

## 落地驗證

每階段完成後：

```bash
dotnet build --configuration Release --nologo  # 0 warning / 0 error
```

`TreatWarningsAsErrors=true` 會把 nullable、IDE0130、CA 系列 analyzer 警告變
編譯失敗。

完整測試：

```bash
./test.sh                            # 全套，含 [DbFact] 容器
./test.sh tests/<Project>.UnitTests/<Project>.UnitTests.csproj  # 限定專案
```

## P2 落地觀察（2026-05-14）

> 對應「硬性規則 1」（見前段）：BO **不**直接呼叫 `Bee.Db`，走 Repository 抽象。
> 本段記錄具體落地時的 code shape 與步驟。

### 第 6 步 BO method 的固定樣板（取代 plan 原本「BO 直接呼叫 IFormCommandBuilder」）

```csharp
[ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
public virtual <Action>Result <Action>(<Action>Args args)
{
    ArgumentNullException.ThrowIfNull(args);

    var factory = Services.GetRequiredService<IFormRepositoryFactory>();
    var repository = factory.CreateDataFormRepository(ProgId);
    var result = repository.<DoWork>(args.X, args.Y, ...);

    return new <Action>Result { ... };
}
```

關鍵點：
- BO 是「薄殼」：把 args 拆解、call Repository、把回傳組成 Result
- `Services.GetRequiredService<T>()` 走 `BusinessObject._ctx.Services` escape
  hatch；`InternalsVisibleTo("Bee.Business")` 已從 `Bee.Definition` 開放
- **FormSchema 載入、欄位 fallback（`ListFields` / `MasterTable`）等邏輯放
  Repository**，不在 BO 重複實作；BO 不需要 `DefineAccess.GetFormSchema(ProgId)`

### Repository 抽象擴充流程（新 FormSchema-driven 方法時）

1. `Bee.Repository.Abstractions/Form/IDataFormRepository.cs` 加方法簽名
   - 參數型別限 `Bee.Definition.*`（RepoAbs 只依賴 Definition）
   - 不接受 `Bee.Business.*` 型別（會反向依賴）
2. `Bee.Repository/Form/DataFormRepository.cs` 實作
   - ctor 注入：`FormSchema schema`, `IDefineAccess defineAccess`,
     `IDbAccessFactory dbAccessFactory`, `IDbConnectionManager connectionManager`,
     `string databaseId`
   - 取 dialect：`DbDialectRegistry.Get(connInfo.DatabaseType).CreateFormCommandBuilder(schema, defineAccess)`
   - 執行：`_dbAccessFactory.Create(databaseId).Execute(spec)`
3. `Bee.Repository/Factories/FormRepositoryFactory.cs` 已注入所需服務
   - `CreateDataFormRepository(progId)` 解析 schema、決定 `databaseId =
     schema.CategoryId`（多租戶 host 端覆寫）
4. `Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` 的
   `IFormRepositoryFactory` 註冊 **必須**用 `CreateConfigurableService`
   （DI-aware），不是 `CreateOrDefault`（parameterless）

### BO 整合測試的 fixture 與多 DB 路由

- 用 `IClassFixture<SharedDbFixture>`（自動跑 `EnsureSchemaAndSeed`）
- `[DbFact(DatabaseType.X)]` 自動依 `BEE_TEST_CONNSTR_X` 環境變數決定 skip
- **測試環境的 databaseId 不等於 schema.CategoryId**：測試多 DB 並存採
  `{categoryId}_{dbtype}` 命名（如 `company_sqlite`），與 prod 的 `CategoryId`
  直接當 databaseId 不一致
- 解法：測試構造 `DataFormRepository` 時傳入測試 databaseId（透過
  `TestDbConventions.GetDatabaseId(dbType, categoryId)`），並用 stub
  `IFormRepositoryFactory` 在 `TestBeeContext.CreateWithOverrides(...)` 中注入
- 種子/清理沿用 `EmployeeBuildSelectIntegrationTests` 的 try/finally + GUID
  RowId pattern；用 `InsertCommandBuilder` / `DeleteCommandBuilder` 跨 dialect
  種資料

### 容易踩的坑（P2 新增）

8. **`Bee.Business` 加 `Bee.Db` ProjectReference = 違反硬性規則 1**：表示你在
   BO 裡直接呼叫 Db；改走 Repository 抽象（見前段「硬性規則 1」）
9. **`FormRepositoryFactory` 改 ctor 簽名 → DI 註冊要同步換**：
   `CreateOrDefault` (parameterless) → `CreateConfigurableService` (DI-aware)，
   否則 runtime 會吃到 InvalidOperationException
10. **既有 trivial POCO 屬性測試在 ctor 簽名變寬時直接刪**：當測試只是
    `new T(progId).ProgId == progId` 這種屬性 setter 檢查，重構後改維護成本
    高、無實質覆蓋；按使用者偏好直接刪除，整合測試承接覆蓋率
11. **`[DbFact]` 只看環境變數設定，不檢查 DB 可達性**：`.runsettings` 設了
    `BEE_TEST_CONNSTR_SQLSERVER` 但 SQL Server container 沒跑 → 測試會失敗而非
    skip。SQLite in-memory 沒這問題（無外部依賴）

## P3 落地觀察（2026-05-14）

### Client connector 第 9 步固定樣板

```csharp
public async Task<<Action>Response> <Action>Async(
    <param1> p1 = default!,
    ...,
    <paramN>? pN = null)
{
    var request = new <Action>Request { /* 屬性 = parameter */ };
    return await ExecuteAsync<<Action>Response>(<Axis>Actions.<Action>, request)
        .ConfigureAwait(false);
}

public <Action>Response <Action>(
    <param1> p1 = default!,
    ...,
    <paramN>? pN = null)
{
    return SyncExecutor.Run(() =>
        <Action>Async(p1, ..., pN)
    );
}
```

- 一定有 async + 同步兩個 overload，公開 API 對齊
- `ExecuteAsync<TResponse>` 預設 `PayloadFormat.Encrypted`；除非 BO method 標
  `ApiProtectionLevel.Encoded` 才在這層 override
- 參數採 optional default，呼叫端可只填要用的
- 例外傳遞：JsonRpcExecutor 把 BO 拋出的
  `ArgumentException / InvalidOperationException / NotSupportedException /
  FormatException / JsonRpcException` 視為「user-facing」轉成 RpcError 帶原訊息；
  其他系統例外（含 NRE、IO）統一收斂為 `"Internal server error"`
  （見 [JsonRpcExecutor.cs:190-198](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:190)）

### API round-trip 測試的最小 setup

兩層測試覆蓋：

**A. Wire-level serialization round-trip**（無 DB、無 executor）
- 直接呼叫 `MessagePackCodec.Serialize<T>(value)` → `Deserialize<T>(bytes)`
- 驗證 `[Key]` 屬性 + 多型 union（`FilterNode` 子型別）+ collection
  （`SortFieldCollection`、`FilterNodeCollection`）+ 自訂 formatter
  （`DataTableFormatter`）保留語意
- 樣板：
  [GetListMessagePackTests.cs](../../tests/Bee.Api.Core.UnitTests/Form/GetListMessagePackTests.cs)

**B. JsonRpcExecutor dispatch round-trip**（無 DB，用 stub Repository）
- 用 `BeeTestFixture`（不接 DB）+ `TestOverrideServiceProvider` 套住
  `IFormRepositoryFactory` → 自建 `BusinessObjectFactory` 注入 override 後的
  `IServiceProvider` → 自建 `JsonRpcExecutor`
- 一個 stub `IDataFormRepository` 回固定 DataTable 並記錄收到的 args，
  可同時驗證 dispatch 與 `ApiInputConverter` 屬性對映
- 樣板：[GetListJsonRpcRoundTripTests.cs](../../tests/Bee.Api.Core.UnitTests/Form/GetListJsonRpcRoundTripTests.cs)

兩層測試的職責切分：
| 層 | 驗證重點 | 不驗證 |
|----|---------|--------|
| A wire-level | `[Key]` 編號 / union / collection / DataTable 序列化 | BO 行為、SQL |
| B executor | `progId.action` 反射派發、Input/Output Converter 路徑 | 實際 SQL 結果 |
| P2 BO+DB | BO → Repository → SQL → 真實資料正確性 | wire format |

三層加起來才是「end-to-end 工作」。**不要**寫單支「executor + 真實 DB」的
整合測試 —— 過度耦合、重複覆蓋既有層的職責。

### 容易踩的坑（P3 新增）

12. **`CollectionBaseFormatter` 沒處理 null** — 第一個用 nullable collection
    欄位（如 `SortFieldCollection? SortFields`）的型別會在序列化時 NRE。
    [CollectionBaseFormatter.cs](../../src/Bee.Api.Core/MessagePack/CollectionBaseFormatter.cs)
    已於 P3 修正為 nil 進 nil 出（[Serialize 寫 WriteNil、Deserialize TryReadNil]），
    新增 nullable collection 欄位前確認此修正已落地
13. **`FormApiConnector` 的 ExecFunc 系列引用 `SystemActions`** — 歷史遺
    （ExecFunc 是 base BO 方法、不分軸），這是**唯一**例外。新增 FormBO 專屬
    method 一律用 `FormActions.<Action>`（規則 3）
14. **`SyncExecutor.Run` 解包例外** — async 內拋的 `ArgumentNullException` 等
    在同步包裝會以原型別重新拋出，但 await 的 stacktrace 會合併兩層，測試
    `Assert.Throws<T>` 仍然成立

## 完整 checklist（合併所有層）

新增一個 FormSchema-driven BO 方法（以 `<Action>` 代表方法名、`<Axis>`=
`Form`）時，依序完成下面所有步驟。每步驟對應一個 Edit / Write 動作，
不確定時回頭看上方對應 Layer 段。

**P0 探勘（read-only）**：
- [ ] 確認 BO 取所需服務的路徑（Repository 抽象 / `Services.GetRequiredService<T>()`）
- [ ] 序列化能力：基本型別 / collection / union 全部走過
- [ ] `ApiContractRegistry` 不需顯式註冊（除非 BO Result 命名違反 `XxxResult` 慣例）

**P1 合約層**（單 PR）：
- [ ] `src/Bee.Api.Contracts/I<Action>Request.cs`
- [ ] `src/Bee.Api.Contracts/I<Action>Response.cs`
- [ ] `src/Bee.Api.Core/Messages/<Axis>/<Action>Request.cs`
- [ ] `src/Bee.Api.Core/Messages/<Axis>/<Action>Response.cs`
- [ ] `src/Bee.Definition/<Axis>Actions.cs` 加 `public const string <Action>`
- [ ] Release build 0w/0e

**P2 BO + Repository**（單 PR）：
- [ ] `src/Bee.Business/<Axis>/<Action>Args.cs`
- [ ] `src/Bee.Business/<Axis>/<Action>Result.cs`
- [ ] `src/Bee.Business/<Axis>/<Axis>BusinessObject.cs` 加 method（走 Repository）
- [ ] `src/Bee.Repository.Abstractions/<Axis>/I<X>Repository.cs` 加抽象方法
- [ ] `src/Bee.Repository/<Axis>/<X>Repository.cs` 實作（用 IFormCommandBuilder + DbAccess）
- [ ] `src/Bee.Repository/Factories/<Axis>RepositoryFactory.cs` 注入新增 ctor 依賴（如新增）
- [ ] `src/Bee.Hosting/...` DI 註冊若 ctor 簽名變更 → 由 `CreateOrDefault` 改為
      `CreateConfigurableService`
- [ ] BO 整合測試：`tests/Bee.Business.UnitTests/<Axis>/<Axis>BusinessObject<Action>Tests.cs`
- [ ] Release build 0w/0e + SQLite 測試通過

**P3 Client + wire 測試**（單 PR）：
- [ ] `src/Bee.Api.Client/Connectors/<Axis>ApiConnector.cs` 加 `<Action>Async` + 同步包裝
- [ ] Wire-level round-trip 測試：
      `tests/Bee.Api.Core.UnitTests/<Axis>/<Action>MessagePackTests.cs`
- [ ] Executor dispatch round-trip：
      `tests/Bee.Api.Core.UnitTests/<Axis>/<Action>JsonRpcRoundTripTests.cs`
- [ ] Release build 0w/0e + 測試通過

## 參考檔案

| 用途 | 檔案 |
|------|------|
| Contract 介面樣板 | [IGetDefineRequest.cs](../../src/Bee.Api.Contracts/IGetDefineRequest.cs) / [IGetDefineResponse.cs](../../src/Bee.Api.Contracts/IGetDefineResponse.cs) |
| Wire DTO 樣板 | [GetDefineRequest.cs](../../src/Bee.Api.Core/Messages/System/GetDefineRequest.cs) / [GetDefineResponse.cs](../../src/Bee.Api.Core/Messages/System/GetDefineResponse.cs) |
| Action 常數樣板 | [SystemActions.cs](../../src/Bee.Definition/SystemActions.cs) |
| BO Args/Result 樣板 | [GetDefineArgs.cs](../../src/Bee.Business/System/GetDefineArgs.cs) / [GetDefineResult.cs](../../src/Bee.Business/System/GetDefineResult.cs) |
| BO 方法樣板 | [SystemBusinessObject.cs:191](../../src/Bee.Business/System/SystemBusinessObject.cs:191)（`GetDefine`） |
| Client connector 樣板 | [SystemApiConnector.cs:217](../../src/Bee.Api.Client/Connectors/SystemApiConnector.cs:217)（`GetDefineAsync` / `GetDefine`） |
| JSON-RPC dispatch | [JsonRpcExecutor.cs:144](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:144) |
| 命名慣例反射 | [ApiOutputConverter.cs:74](../../src/Bee.Api.Core/Conversion/ApiOutputConverter.cs:74) |
