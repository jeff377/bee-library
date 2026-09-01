# 開發限制與反模式

[English](development-constraints.md) · [← 文件索引](README.zh-TW.md)

> 本文件列出框架的設計限制與禁止事項，供 AI Coding 工具參考，避免產生違反框架慣例的程式碼。
> 安全相關規範請參閱 [安全規範](../.claude/rules/security.md)。

## 初始化順序限制

框架透過標準的 `IServiceCollection` DI 容器註冊；框架服務以 ctor 注入解析，不再使用靜態入口點。Host 啟動必須依以下五步進行：

1. `var paths = new PathOptions { DefinePath = "..." }` — 指向定義檔目錄
2. `var settings = SystemSettingsLoader.Load(paths)` — 讀取 `SystemSettings.xml`（boot-time only；runtime 快取存取走 DI 注入的 `IDefineAccess`）
3. `SysInfo.Initialize(settings.CommonConfiguration)` — process-wide debug flag / payload options
4. `services.AddBeeFramework(settings.BackendConfiguration, paths)` — 註冊框架服務（擴充方法來自 `Bee.Hosting`）
5. `services.BuildServiceProvider()` 後 `app.UseBeeFramework()`（僅 ASP.NET Core 宿主；非 web 宿主則把產出的 `IServiceProvider` 設給 `ApiClientInfo.LocalServiceProvider` 啟用近端模式）

完整參考見 [development-cookbook.zh-TW.md § 框架初始化順序](development-cookbook.zh-TW.md#框架初始化順序)。

### 違反後果

- 在 `AddBeeFramework` 之前解析框架服務 → DI 容器拋 `InvalidOperationException`（服務未註冊）
- `SystemSettingsLoader.Load` 指向不存在的 `SystemSettings.xml` → 拋 `FileNotFoundException`
- 以資料庫 id 建立 `DbAccess` 卻未提供 `IDbConnectionManager` 參數 → 編譯錯誤；改透過 DI 注入的 `IDbAccessFactory.Create(databaseId)` 取得實例。（另有一個 ctor 接已開啟的 `DbConnection` 加其 `DatabaseType`，不需連線管理員——供自行持有連線的呼叫端使用，例如 cache-notify poller）

### 參考範例

`tests/Bee.Tests.Shared/TestProcessBootstrap.cs` 展示測試 process 的正確初始化順序。

## 快取資料初始化後不可異動

框架初始化完成後，**所有伺服端 cache 內的物件一律為唯讀，執行期間不可被
異動**。每個 session 從 process-wide 的 `ICacheContainer` 拿到的是同一份
in-memory 實例；對單一 session 做的調整會洩漏到其他所有 session，並行的
mutation 會競態。

這條規則的成立理由是「cache 為共用」，與資料從哪裡載入無關。因此下面兩類
都適用 —— 從定義檔載入的定義資料，以及從資料庫載入的快照。

### 適用範圍：定義檔快取

任何透過 `IDefineAccess.GetX(...)` 取得的物件（由同名 `ICacheContainer` slot
back-up）：

- `FormSchema`、`FormLayout`、`TableSchema`
- `SystemSettings`、`DatabaseSettings`、`ProgramSettings`、`DbCategorySettings`
- `MenuSettings`、`PluginSettings`、`PermissionModels`、`CurrencySettings`、
  `UnitSettings`
- `LanguageResource`

### 適用範圍：資料庫相依快取

這類經 `ICacheDataSourceProvider` 載入（而非 `IDefineAccess`），失效走共用的
cache-notify 表（而非 `SaveX` 呼叫）—— 但它們同樣存在於 process-wide 的
`ICacheContainer`、同樣被所有 session 共用，所以同一條禁令一體適用。取用管道
是 `ICacheContainer` slot 或包裝它的服務（`ICompanyInfoService`、
`IRolePermissionService`、`IDepartmentTreeService`、`IAuditRuleService`、
`IApiKeyValidator`）：

| 快取型別 | `ICacheContainer` slot | cache key |
|---------|------------------------|-----------|
| `CompanyInfo` | `CompanyInfo` | 公司 id |
| `CompanyRolePermissions` | `CompanyRolePermissions` | 公司 id |
| `DepartmentTree` | `DepartmentTree` | 公司 id |
| `CompanyAuditRules` | `CompanyAuditRules` | 公司 id |
| `ApiKeyInfo` | `ApiKey` | 金鑰 `sys_id` |
| `ApiKeyGateState` | `ApiKeyGate` | `ApiKeyGateState.CacheKey` |

其中 `CompanyAuditRules` 與 `CompanyRolePermissions` 由結構本身保證 —— 不公開
任何 setter，索引在建構子內一次建好。另外四個因為同時要跨 wire 而帶有 public
setter，這條規則在它們身上編譯器管不到；請把 cache 交給你的實例一律視為凍結。

### 唯一的例外

`SessionInfo` 是刻意保留的例外 —— 它本來就是 per-session 實體、非共用資料，
cache key 即 access token。

### 禁止樣式

| 樣式 | 為何不可 |
|------|---------|
| `cachedSchema.Caption = "..."` | mutate 共用實例 → 跨 session 洩漏 / race |
| `XmlCodec.Serialize(cachedInstance)` 當作免費的 deep-clone | `IObjectSerialize` 生命週期會在來源物件上翻動 `SerializeState` → thread race + 在高並行下 `IsSerializeEmpty` 行為錯亂 |
| 把 per-session 狀態塞進 cached 物件的 `Tag` / 擴充屬性 | `Tag` 也是 process-shared |
| 用 `MasterTable` / collection setter 在 cached 實例上 swap 子節點 | 同樣 race 面 |

### 正確作法

- **需要 per-session 視圖（如本地化 schema）？** 先 clone、再 mutate 副本：
  ```csharp
  var customised = cachedSchema.Clone();
  FormSchemaLocalizer.Localize(customised, sessionLang);
  return customised;
  ```
- **定義資料的持久化變更**走 `IDefineAccess.SaveX(...)`：
  1. 寫入後端 storage
  2. invalidate cache slot，下一次 `GetX` 從 storage rebuild
- **資料庫相依資料的持久化變更**走所屬 repository 加上一筆 cache-notify 記錄，
  由 poller 在每個 process 失效該 slot。這類**沒有** `SaveX`；只寫了資料列卻
  漏掉 notify 記錄，會讓所有 process 繼續拿舊快照。
- **需要 deep copy？** 用該類型的 `Clone()` 方法（已提供於 `FormSchema` /
  `FormTable` / `FormField` / `TableSchema` / `DatabaseSettings` 等）。
  **不可**用 `XmlCodec` round-trip 替代 —— 它會在來源 mutate state。
  **資料庫相依的那幾個型別沒有 `Clone()`** —— 它們是拿來讀的快照，不是拿來
  客製的。需要 per-session 變體時，把要用的值複製進自己的物件，不要為此補一個
  `Clone()` 再去 mutate。

### 為什麼這條重要

Bee.NET 設計用於多租戶 ASP.NET Core / Blazor Server host：單一 process 同時
服務眾多 session，每個 session 可能有不同語系與租戶 context。Cache 是
singleton；序列化生命週期 hook 讓即使是「讀取性質」的 `XmlCodec.Serialize`
對共用狀態也非冪等。**「快取資料載入後不可異動」**這條 invariant 是讓
所有 session 能安全共用 cache 實例、無需協調的單一基礎規則。

資料庫相依快取只會把賭注放大、不會縮小：它們持有的是授權狀態。被 mutate 的
`CompanyRolePermissions` 或 `DepartmentTree` 不只是讓某個 session 看到錯的
標題 —— 它會在其他 session 上放行或擋掉存取。

## 跨層禁止事項

| 禁止行為 | 原因 | 正確做法 |
|----------|------|----------|
| API 層直接引用 Repository 層（指 `Bee.Api.Core`、`Bee.Api.AspNetCore`；**不含**組合根 `Bee.Hosting`，接線各層本就是它的職責） | 違反分層架構 | 透過 Business Object 間接存取 |
| Business Object 直接建立 `DbConnection` | 繞過連線管理與日誌 | 使用 `DbAccess` 類別 |
| BO 引用 `Bee.Db`（`Bee.Business.csproj` 無 `Bee.Db` 的 `ProjectReference`） | BO 是業務邏輯的薄殼，資料存取屬於 Repository | FormSchema-driven CRUD → `IDataFormRepository`；自訂查詢 → 自訂 bo repo 配合 `IDbAccessFactory` |
| BO 寫死 `databaseId` 字串或直接讀 `SessionInfo.CompanyId` / `CompanyInfo` | 將 BO 與路由實作耦合；部署設定變更時會壞 | 使用 `BusinessObject.ResolveDatabaseId(DbScope)`（自訂 bo repo）或 `CreateDataFormRepository(progId)`（FormSchema CRUD）；helper 內部委派給 `IRepositoryDatabaseRouter`，這是單一真相來源 |
| Client 端從 DI 容器解析 Repository 服務 | 僅限 Server 端使用 | 透過 `ApiConnector` 呼叫 API |
| 跳過 Payload Pipeline 順序 | 破壞加解密一致性 | 維持 Serialize → Compress → Encrypt |
| 在 BO 中直接回傳 API 型別 | BO 不應依賴 API 序列化格式 | 回傳 BO 型別，由 `ApiOutputConverter` 依命名慣例自動對應 |

## ExecFunc 開發限制

### 方法簽章規則

ExecFunc handler 方法必須遵守以下規則：

- **必須** 是 `public` 方法（反射呼叫需要）
- **必須** 非泛型（`GetMethod()` 不支援泛型解析）
- **固定簽章**：`void MethodName(ExecFuncArgs args, ExecFuncResult result)`
- **FuncId 對應方法名稱**，大小寫敏感
- 未標記 `[ExecFuncAccessControl]` 的方法預設需要 `Authenticated`

### 存取控制宣告

```csharp
// 匿名存取
[ExecFuncAccessControl(ApiAccessRequirement.Anonymous)]
public void PublicMethod(ExecFuncArgs args, ExecFuncResult result) { }

// 需要登入（預設行為，可省略 Attribute）
[ExecFuncAccessControl(ApiAccessRequirement.Authenticated)]
public void SecureMethod(ExecFuncArgs args, ExecFuncResult result) { }
```

## 例外處理規則

### Client 可見的例外類型

`JsonRpcExecutor` 僅將以下例外類型原樣回傳給 Client，並對映到 `JsonRpcErrorCode.UserMessage`（`-32099`）：

- `UserMessageException`（**預設選項**）
- `UnauthorizedAccessException`
- `ArgumentException`（含 `ArgumentNullException`、`ArgumentOutOfRangeException`）
- `InvalidOperationException`
- `NotSupportedException`
- `FormatException`
- `JsonRpcException`

`ForbiddenException` 同樣原文回傳，但使用不同的錯誤碼：`JsonRpcErrorCode.PermissionDenied`（`-32004`）。用戶端若假設「所有 user-facing 失敗都是 `-32099`」，會誤判權限拒絕。

其他所有例外在正式環境一律遮蔽為 `"Internal server error"`（對映到 `JsonRpcErrorCode.InternalError` `-32000`），避免洩漏內部細節。

`CommonConfiguration.IsDebugMode` 啟用時改為透傳原始訊息，錯誤碼仍為 `-32000`。這類例外由 executor 自行處理、不會往上拋到 transport，訊息一旦被換掉，開發者就沒有任何線索可循。兩種模式都不含堆疊追蹤。

### 使用時機

| 例外型別 | 使用情境 |
|----------|----------|
| `UserMessageException` | **預設選項**：任何要顯示給使用者看的訊息（業務規則違反、驗證失敗、流程中斷） |
| `ArgumentException` | API contract 違反 —— 呼叫端傳錯參數（null、格式錯、超出範圍）。**注意**：白名單暫保留，新程式碼請優先用 `UserMessageException` |
| `InvalidOperationException` | 物件狀態錯誤、操作時機不對。**注意**：白名單暫保留，新程式碼請優先用 `UserMessageException` |
| `UnauthorizedAccessException` | 認證／授權失敗 |
| `NotSupportedException` | 功能未實作或不支援當前情境 |
| `FormatException` | 字串／資料格式無法解析 |
| `JsonRpcException` | API 框架自身的協定錯誤（HTTP status / JSON-RPC error code） |

### Client 端的對應行為

`ApiConnector.FinalizeResponse` 依 `JsonRpcError.Code` 重建例外：

- `code == UserMessage` → 拋出 `UserMessageException(message)`，訊息純淨無前綴，可直接顯示給使用者
- 其他 code → 拋出 `InvalidOperationException($"API error: {code} - {message}")`，保留協定層除錯資訊

Client 端建議的 catch 順序：

```csharp
try
{
    var result = await connector.SomeAction(args);
}
catch (UserMessageException ex)
{
    // 業務訊息：直接顯示給使用者
    ShowMessage(ex.Message);
}
catch (Exception ex)
{
    // 系統錯誤：記錄 log、顯示通用錯誤頁
    LogError(ex);
}
```

### 演進方向

長期目標是讓 `UserMessageException` 成為 user-facing 訊息的**唯一**通道，把 BCL 例外回歸 BCL 本意（呼叫錯誤、狀態錯誤、程式 bug）。白名單目前保留 BCL 例外是為了**漸進過渡**：

- **新程式碼**：一律用 `UserMessageException` 拋送使用者訊息
- **舊程式碼**：遇到時順手把 `InvalidOperationException("xxx")`／`ArgumentException("xxx")` 改成 `UserMessageException("xxx")`
- **白名單縮減**：當某個 BCL 例外在 prod BO 已 0 處 user-facing 使用時，由獨立 plan 評估從白名單移除
- **終態**：白名單只剩 `UserMessageException` 與 `JsonRpcException`

### 擴充方式

- 需要更多屬性（如 `Code`、`Details`、結構化資料）：直接在 `UserMessageException` 加 nullable 屬性（向後相容）
- 需要分類錯誤（如 `NotFoundException` 對應 HTTP 404）：繼承 `UserMessageException` 拆子類別
- i18n 載點：`JsonRpcError.Data` 欄位已預留，整體機制由獨立 plan 設計

### 設計意圖

- 防止內部實作細節洩漏給 Client
- 為業務訊息建立獨立通道，與「真程式錯誤」在型別上明確區隔，方便未來 logging／監控分流

## FormSchema 設計限制

- FormSchema 在執行時期為**唯讀**，不可動態新增欄位
- `IFormCommandBuilder`（位於 `Bee.Db.Dml`）為 CRUD 命令建構契約，5 DB providers 各自實作（`SqlFormCommandBuilder` / `PgFormCommandBuilder` / `MySqlFormCommandBuilder` / `OracleFormCommandBuilder` / `SqliteFormCommandBuilder`），無共同基底類別
- TableSchema 手動調整的部分（精度、索引、預設值）在 FormSchema 更新時會被保留
- `FormTable.DbTableName`：可選欄位；若為空，使用 `FormTable.TableName` 作為實體表名。命名應遵循 [`資料庫命名規範`](database-naming-conventions.md)（lowercase + snake_case）

## 型別安全限制

### MessagePack 型別白名單

`WireTypeWhitelist` 和 `SafeMessagePackSerializerOptions` 實施型別白名單機制：

- 僅白名單內的型別可被反序列化
- 白名單外的型別在型別被解析之前即遭拒絕
- 白名單涵蓋常見基礎型別，以及 `SysInfo.AllowedTypeNamespaces` 所列的命名空間

### wire 型別必須註冊 formatter

凡走 MessagePack wire 的型別一律顯式註冊——contractless resolver 只是桌面端的便利退路，
不是承載機制：.NET for iOS 關閉動態碼，未註冊的型別在那裡直接失敗。新增訊息合約、
新增其可達的定義層型別、或引入新的封閉泛型具現（`List<T>`、`Dictionary<K,V>`、`T?`、列舉）
時都必須補上註冊。漂移測試會走同一條型別閉包，漏補即建置失敗。
詳見 [ADR-037](adr/adr-037-wire-explicit-registration.md)。

### API 契約命名慣例（強制）

API Request/Response 與 BO Args/Result 型別必須遵守命名慣例，`ApiOutputConverter` 才能自動將 BO 回傳值對應到 API 型別（詳見 [ADR-007](adr/adr-007-convention-based-type-resolution.md)）：

| 層級 | 輸入 | 輸出 |
|------|------|------|
| BO（`Bee.Business`） | `{Action}Args` | `{Action}Result` |
| API（`Bee.Api.Core`） | `{Action}Request` | `{Action}Response` |
| Contract（`Bee.Api.Contracts`） | `I{Action}Request` | `I{Action}Response` |

- 偏離命名慣例的型別將無法自動轉換，BO 回傳值會直接流至用戶端造成型別錯誤
- `ApiContractRegistry` 仍用於 Encoded / Encrypted 格式的 MessagePack Typeless 序列化白名單，但**不再需要手動呼叫 `Register`** 來建立回應映射

## 帳號安全限制

- `LoginAttemptTracker` 預設規則：連續 5 次登入失敗後鎖定帳號 15 分鐘
- 鎖定期間所有登入嘗試直接拒絕，不檢查密碼
- 成功登入會重置失敗計數器

## Session 持久化限制

登入時會寫入重建種子至 `st_session`，`SessionInfoCache` 在快取失效時據以重建。由此衍生三項限制：

- **種子不是快照。** 它只存無法再推導的值——token、使用者、到期、公司；角色、客製代碼、
  record scope 一律於每次重建重算。**不要把可推導的狀態加進 `SessionUser`**：存進去的值就不再
  跟隨來源變動，登入後被撤銷的權限會殘留在該份副本裡。
- **重建需搭配「能取回 session 金鑰」的 provider。** `DerivedApiEncryptionKeyProvider`（預設）與
  `StaticApiEncryptionKeyProvider` 可以；`DynamicApiEncryptionKeyProvider` 不行，因為它的金鑰
  只存在於 session 內。使用 dynamic provider 時，session 一律不重建——快取失效即請使用者重新登入，
  勝過給出一個「看似有效但每個加密呼叫都失敗」的 session。
- **自訂登入流程必須走框架的建構路徑。** 只建構 `SessionInfo` 並呼叫 `SessionInfoService.Set`
  的程式碼，會產生一個背後沒有列的 session：行程一重啟就消失，在其他節點上根本不存在。

## API 重放防護限制

啟用 `ApiServiceOptions.RequireWireFrame` 後，Encoded 與 Encrypted 的請求會在加密封套內夾帶
一段 wire frame（時間戳 + 序號）。設計背景見 [ADR-042](adr/adr-042-api-replay-protection.md)。
由此衍生四項限制：

- **兩端必須設成同一個值。** frame 的有無是部署層級的事實，不由封包自述——伺服器若「偵測」
  frame 在不在，攻擊者只要把 frame 拿掉就能關閉防護。因此兩端設定不一致必然失敗，這是刻意的。
  啟用順序：**兩端先升套件，再同時開啟兩端開關**。
- **Plain 路徑不受保護。** 明文沒有攻擊者無法偽造的綁定，任何防重放欄位他都能改寫（改成當下
  時間、改成更大的序號），那就是一個全新的合法請求。`ApiProtectionLevel.Public` 的方法
  （含 `Save` / `Delete` / `ExecFunc`）仍允許以 Plain 呼叫，該路徑不帶 frame、不受檢查。
  `Encoded` 帶 frame 但無 HMAC，只擋無腦原樣重送。
- **逾時重送會失敗，而非重試成功。** 序號解的是「拒絕重放」，冪等鍵解的是「安全重試」，
  兩者不可互相取代。框架本身沒有自動重試，但應用層自己包的重試迴圈、以及使用者手動
  「重新送出」都會踩到；需要安全重試的場景請自行實作冪等鍵。
- **匿名呼叫不做序號檢查。** 序號是 per session 的，登入前沒有 session 可計數。
  `ExecFuncAnonymous` 若有副作用，其冪等由應用層自負。

## 資料庫 Schema 限制

框架的 schema 定義（`TableSchema`）與升級機制（`TableUpgradeOrchestrator`）**刻意不支援**下列資料庫層元素：

- **Foreign Key 約束**
- **Trigger**
- **View**

### 設計原則

Referential integrity、business rules 與衍生資料由**程式端（Business Object 層）**處理，schema 定義僅描述資料表結構（欄位、索引、主鍵）。

### 設計理由

- 資料庫層相依會讓跨 provider 支援與 schema 升級成本爆炸
- 實務 ERP 場景下，BO 層已能完整表達業務規則，不需下推至 DB
- 升級流程（新增／刪除欄位、改型別）不必處理 FK 暫存／trigger 重建／view 刷新等級聯議題

### 若真的需要 FK / Trigger / View

不透過框架，改由專案自訂的 migration 腳本手動維護。升級管線不會產生對應 DDL，也不保證相容。
