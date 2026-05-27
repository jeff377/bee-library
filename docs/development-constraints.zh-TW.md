# 開發限制與反模式

[English](development-constraints.md)

> 本文件列出框架的設計限制與禁止事項，供 AI Coding 工具參考，避免產生違反框架慣例的程式碼。
> 安全相關規範請參閱 [安全規範](../.claude/rules/security.md)。

## 初始化順序限制

框架透過標準的 `IServiceCollection` DI 容器註冊；框架服務以 ctor 注入解析，不再使用靜態入口點。Host 啟動必須依以下四步進行：

1. `var paths = new PathOptions { DefinePath = "..." }` — 指向定義檔目錄
2. `var settings = SystemSettingsLoader.Load(paths)` — 讀取 `SystemSettings.xml`（boot-time only；runtime 快取存取走 DI 注入的 `IDefineAccess`）
3. `SysInfo.Initialize(settings.CommonConfiguration)` — process-wide debug flag / payload options
4. `services.AddBeeFramework(settings.BackendConfiguration, paths)` — 註冊框架服務（擴充方法來自 `Bee.Hosting`）
5. `services.BuildServiceProvider()` 後 `app.UseBeeFramework()`（僅 ASP.NET Core 宿主；非 web 宿主則把產出的 `IServiceProvider` 設給 `ApiClientInfo.LocalServiceProvider` 啟用近端模式）

完整參考見 [development-cookbook.zh-TW.md § Framework Initialization Order](development-cookbook.zh-TW.md#framework-initialization-order)。

### 違反後果

- 在 `AddBeeFramework` 之前解析框架服務 → DI 容器拋 `InvalidOperationException`（服務未註冊）
- `SystemSettingsLoader.Load` 指向不存在的 `SystemSettings.xml` → 拋 `FileNotFoundException`
- 直接 `new DbAccess(databaseId)` 而未提供 `IDbConnectionManager` 參數 → 編譯錯誤（框架要求所有 ctor 都帶 `IDbConnectionManager`）；改透過 DI 注入的 `IDbAccessFactory.Create(databaseId)` 取得 `DbAccess` 實例

### 參考範例

`tests/Bee.Tests.Shared/TestProcessBootstrap.cs` 展示測試 process 的正確初始化順序。

## 定義資料初始化後不可異動

框架初始化完成後，**所有伺服端 cache 內的定義資料一律為唯讀，執行期間不可
被異動**。每個 session 透過 `IDefineAccess` / `ICacheContainer` 共用同一份
in-memory 實例，cache 為 process-wide；對單一 session 做的調整會洩漏到其他
所有 session，並行的 mutation 會競態。

### 適用範圍

任何透過 `IDefineAccess.GetX(...)` 取得的物件（由同名 `ICacheContainer` slot
back-up）：

- `FormSchema`、`FormLayout`、`TableSchema`
- `SystemSettings`、`DatabaseSettings`、`ProgramSettings`、`DbCategorySettings`
- `LanguageResource`
- `SessionInfo` 是刻意保留的例外 —— 它本來就是 per-session 實體、非共用
  定義資料，cache key 即 access token

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
- **持久化變更**走 `IDefineAccess.SaveX(...)`：
  1. 寫入後端 storage
  2. invalidate cache slot，下一次 `GetX` 從 storage rebuild
- **需要 deep copy？** 用該類型的 `Clone()` 方法（已提供於 `FormSchema` /
  `FormTable` / `FormField` / `TableSchema` / `DatabaseSettings` 等）。
  **不可**用 `XmlCodec` round-trip 替代 —— 它會在來源 mutate state。

### 為什麼這條重要

Bee.NET 設計用於多租戶 ASP.NET Core / Blazor Server host：單一 process 同時
服務眾多 session，每個 session 可能有不同語系與租戶 context。Cache 是
singleton；序列化生命週期 hook 讓即使是「讀取性質」的 `XmlCodec.Serialize`
對共用狀態也非冪等。**「定義資料 init 後不可異動」**這條 invariant 是讓
所有 session 能安全共用 cache 實例、無需協調的單一基礎規則。

## 跨層禁止事項

| 禁止行為 | 原因 | 正確做法 |
|----------|------|----------|
| API 層直接引用 Repository 層 | 違反分層架構 | 透過 Business Object 間接存取 |
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

其他所有例外在正式環境一律遮蔽為 `"Internal server error"`（對映到 `JsonRpcErrorCode.InternalError` `-32000`），避免洩漏內部細節。

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

`SafeTypelessFormatter` 和 `SafeMessagePackSerializerOptions` 實施型別白名單機制：

- 僅已註冊的型別可被反序列化
- 未註冊的型別會拋出 `MessagePackSerializationException`
- 新增 API 型別時必須同步註冊到 `ApiContractRegistry`

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
