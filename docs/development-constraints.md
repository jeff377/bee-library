# 開發限制與反模式

> 本文件列出框架的設計限制與禁止事項，供 AI Coding 工具參考，避免產生違反框架慣例的程式碼。
> 安全相關規範請參閱 [安全規範](../.claude/rules/security.md)。

## 初始化順序限制

框架使用多個靜態入口點，必須依照以下順序初始化：

1. `BackendInfo.DefinePath` — 設定定義檔路徑
2. `BackendInfo.DefineAccess` — 設定定義存取介面（`LocalDefineAccess` 或 `RemoteDefineAccess`）
3. `SysInfo.Initialize(settings.CommonConfiguration)` — 系統資訊初始化
4. `BackendInfo.Initialize(settings.BackendConfiguration)` — 後端元件與安全金鑰初始化
5. `RepositoryInfo` — 首次存取時由靜態建構子自動初始化（依賴步驟 2）
6. `CacheFunc` — 首次存取時透過 `Lazy<T>` 自動初始化
7. `ApiServiceOptions.Initialize()` — API 服務元件初始化

### 違反後果

- 在步驟 2 之前存取 `RepositoryInfo` → 拋出 `InvalidOperationException`
- 在步驟 4 之前使用加密功能 → 金鑰為空，加密失敗
- 在步驟 7 之前處理 API 請求 → 序列化/壓縮/加密元件為 null

### 參考範例

`tests/Bee.Tests.Shared/GlobalFixture.cs` 展示了正確的初始化順序。

## 跨層禁止事項

| 禁止行為 | 原因 | 正確做法 |
|----------|------|----------|
| API 層直接引用 Repository 層 | 違反分層架構 | 透過 Business Object 間接存取 |
| Business Object 直接建立 `DbConnection` | 繞過連線管理與日誌 | 使用 `DbAccess` 類別 |
| Client 端存取 `RepositoryInfo` | 僅限 Server 端使用 | 透過 `ApiConnector` 呼叫 API |
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

`JsonRpcExecutor` 僅將以下例外類型原樣回傳給 Client：

- `UnauthorizedAccessException`
- `ArgumentException`（含 `ArgumentNullException`、`ArgumentOutOfRangeException`）
- `InvalidOperationException`
- `NotSupportedException`
- `FormatException`
- `JsonRpcException`

其他所有例外在正式環境一律轉為 `"Internal server error"`，開發環境（`IsDevelopment`）會回傳完整錯誤訊息。

### 設計意圖

- 防止內部實作細節洩漏給 Client
- 如需回傳特定錯誤訊息，使用上述類型或自訂 `JsonRpcException`

## FormSchema 設計限制

- FormSchema 在執行時期為**唯讀**，不可動態新增欄位
- `SqlFormCommandBuilder.BuildInsertCommand()` 基底實作拋出 `NotSupportedException`，子類別必須覆寫
- TableSchema 手動調整的部分（精度、索引、預設值）在 FormSchema 更新時會被保留
- `FormTable.DbTableName` 必須包含 schema 前綴（如 `dbo.Employee`）

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
