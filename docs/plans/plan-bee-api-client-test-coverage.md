# Bee.Api.Client 測試覆蓋率提升計畫書

**狀態：🚧 進行中**（階段 1、2 測試已撰寫，待 CI 驗證）

## 1. 背景與目標

`Bee.Api.Client` 是 Bee.NET 框架的 API 客戶端層，提供本機（in-process）／遠端（HTTP）兩種連線模式，封裝 JSON-RPC 2.0 請求／回應的序列化、壓縮、加解密與同步呼叫包裝。下游被三個前端 repo（WinForms / Web / MAUI）共用，是端到端鏈路的關鍵節點。

現況觀察（SonarCloud 2026-04-18 快照）：

- **Coverage：0.0%**（330 lines uncovered，全 src 模組中第三高未覆蓋行數）
- 已有測試專案 `tests/Bee.Api.Client.UnitTests`，但**現有兩個測試檔全部標記為 `[LocalOnlyFact]` / `[LocalOnlyTheory]`**，CI（`CI=true`）下全部 Skipped → SonarCloud 取得 0% 覆蓋率
- 主要功能涉及 HTTP 呼叫、JSON-RPC 執行（`LocalApiServiceProvider` 走 `JsonRpcExecutor`，需後端設定）、加解密金鑰交換等，多半需執行中的 API server 才能完整測試

**目標**：

1. 在不依賴外部 API server 的前提下，補齊 `Bee.Api.Client` 所有可純邏輯測試類別與分支，使 SonarCloud 顯示的 `Bee.Api.Client` Line Coverage **≥ 60%**（扣除必須連 API server 的部分後 **≥ 80%**）。
2. 所有 `public` 類別至少有一個對應測試檔。
3. 不新增任何需連線真實 API server 的測試；既有 `[LocalOnlyFact]` 測試保持原樣，不在本計畫範圍。
4. 通過源碼掃描所列規則（`scanning.md` / `sonarcloud.md`）。

## 2. 現況盤點

`Bee.Api.Client` 共 12 個 `.cs` 檔。圖例：✅ 已覆蓋 / ⚠ 部分覆蓋（多為 LocalOnly）／ ❌ 完全無測試

### 2.1 根目錄

| 類別／檔案 | 狀態 | 現有測試 | 純邏輯可測項目 |
|------|------|----------|--------------|
| `ApiClientContext`（static 屬性） | ❌ | — | 預設值：`ConnectType=Local`、`SupportedConnectTypes=Both`、`Endpoint`/`ApiKey=string.Empty`、`ApiEncryptionKey=Array.Empty<byte>()`；屬性可被覆寫 |
| `ApiConnectValidator` | ⚠ | LocalOnly | `Validate` 空字串拋 `ArgumentException`；非 URL／非本機路徑拋 `InvalidOperationException`；本機路徑但 `SupportedConnectTypes` 不含 `Local` 拋 `InvalidOperationException`；本機路徑不存在拋 `ArgumentException`；本機路徑存在但缺 `SystemSettings.xml` 拋 `FileNotFoundException`；`allowGenerateSettings=true` 在 temp dir 自動建立 `SystemSettings.xml` 與 `DatabaseSettings.xml` |
| `ConnectType`（enum） | n/a | — | enum 不單獨測試 |
| `SupportedConnectTypes`（enum, `[Flags]`） | n/a | — | 同上；`Both = Local \| Remote` 可順便驗證 |
| `SyncExecutor` | ❌ | — | `Run(Func<Task>)`／`Run<T>(Func<Task<T>>)` 正常路徑與回傳值；`null` 委派拋 `ArgumentNullException`；委派內拋例外時 caller 收到（非 `AggregateException` 包裝，因為 `GetAwaiter().GetResult()` 解包） |

### 2.2 ApiServiceProvider

| 類別 | 狀態 | 現有測試 | 純邏輯可測項目 |
|------|------|----------|--------------|
| `IJsonRpcProvider`（介面） | n/a | — | 介面不測 |
| `LocalApiServiceProvider` | ❌ | — | 建構子設定 `AccessToken` 屬性；`Execute`／`ExecuteAsync` 走 `JsonRpcExecutor`，因依賴 `BackendInfo` 已由 `GlobalFixture` 初始化，可測「method 找不到」會回傳含 `Error` 的 `JsonRpcResponse`（而非拋例外） |
| `RemoteApiServiceProvider` | ❌ | — | 建構子：null/空字串 endpoint 拋 `ArgumentException`；`Endpoint`／`AccessToken` 屬性正確設定；`CreateHeaders`（private）需透過反射或重構為 internal 才能直接驗；或改測「`ExecuteAsync` 對不可達 endpoint 拋 `HttpRequestException`」 |

### 2.3 Connectors

| 類別 | 狀態 | 現有測試 | 純邏輯可測項目 |
|------|------|----------|--------------|
| `ApiConnector`（abstract） | ❌ | — | 建構子驗證透過 `SystemApiConnector` 間接測：remote 建構子空 endpoint 拋 `ArgumentException`；`Execute<T>`／`ExecuteAsync<T>` 中 `ValidateArgs` 對 progId/action 空字串拋 `ArgumentException`；`AccessToken`／`Provider` 屬性 |
| `SystemApiConnector` | ⚠ | LocalOnly | 建構子設定屬性；上述 `ApiConnector` 共用驗證；其餘 method（`Login`、`CreateSession`、`GetDefine` 等）仍須 LocalOnly |
| `FormApiConnector` | ❌ | — | local／remote 兩個建構子設定 `ProgId` 屬性；經由 `ApiConnector` 共用驗證 |

### 2.4 DefineAccess

| 類別 | 狀態 | 現有測試 | 純邏輯可測項目 |
|------|------|----------|--------------|
| `RemoteDefineAccess` | ❌ | — | `GetDefine(DefineType)` 對 `TableSchema`／`FormSchema`／`FormLayout` 不傳 keys 或 keys 長度錯誤 → 拋 `ArgumentException`；不支援的 `DefineType` 拋 `NotSupportedException`；其餘需 API server，不在純邏輯範圍 |

## 3. 階段規劃

依「無外部依賴 → 內部依賴 → 介面契約」順序分三階段。每個階段獨立可 commit / push。

### 階段 1：純邏輯與屬性（無外部依賴）

**目標檔案**：5 個 / **預估測試數**：約 30+ 個

1. **`ApiClientContextTests.cs`**
   - `Defaults_AreExpected`：四個 static 屬性的預設值
   - `SupportedConnectTypes_CanBeOverwritten`：覆寫後讀回
   - `ApiEncryptionKey_CanBeReplaced`：傳入 byte[] 後讀回相同陣列
   - **注意**：static 狀態會跨測試共用，每個測試末尾須還原預設值（或集中在 `IDisposable` fixture 中還原）

2. **`SyncExecutorTests.cs`**
   - `Run_NonGeneric_ExecutesSynchronously`：傳入簡單 `Func<Task>` 驗證會執行
   - `Run_Generic_ReturnsResult`：`Run<int>(() => Task.FromResult(42))` 應回傳 42
   - `Run_NullDelegate_ThrowsArgumentNullException`（Theory：兩個 overload）
   - `Run_DelegateThrows_PropagatesException`：`Run(() => throw new InvalidOperationException(...))` 應拋 `InvalidOperationException`（而非 `AggregateException`，因 `GetAwaiter().GetResult()` 解包）
   - `Run_AsyncDelegate_AwaitsCompletion`：`Task.Delay(50)` 後設旗標，驗證旗標已被設定

3. **`ApiConnectValidatorTests.cs`**（補純邏輯，保留現有 LocalOnly）
   - `Validate_EmptyEndpoint_ThrowsArgumentException`
   - `Validate_UnknownFormat_ThrowsInvalidOperationException`：傳入 `"abc"`、`"file://something"` 等
   - `Validate_LocalPath_NotSupported_ThrowsInvalidOperationException`：暫時設 `ApiClientContext.SupportedConnectTypes = Remote`，傳入本機路徑
   - `Validate_LocalPath_NotExists_ThrowsArgumentException`：傳入不存在的路徑
   - `Validate_LocalPath_MissingSystemSettings_ThrowsFileNotFoundException`：建立 temp dir，未放 `SystemSettings.xml`
   - `Validate_LocalPath_AllowGenerateSettings_CreatesFiles`：在 temp dir 開啟 `allowGenerateSettings=true`，驗證 `SystemSettings.xml`／`DatabaseSettings.xml` 被建立
   - **注意**：每個測試末尾還原 `SupportedConnectTypes` 與清理 temp dir；不可使用 `Path.GetTempPath()` 不檢查存在的根目錄即操作（防 path traversal，符合 `scanning.md`）

4. **`RemoteApiServiceProviderTests.cs`**
   - `Constructor_NullOrEmptyEndpoint_ThrowsArgumentException`（Theory：null、""、"   "）
   - `Constructor_ValidArgs_SetsProperties`：驗證 `Endpoint` 與 `AccessToken`
   - `Constructor_DefaultAccessToken_IsAcceptable`：`Guid.Empty`（用於 Login/Ping）

5. **`LocalApiServiceProviderTests.cs`**
   - `Constructor_SetsAccessToken`：屬性正確設定
   - （ExecuteAsync 涉及 `JsonRpcExecutor`／`BackendInfo`，留待階段 2）

### 階段 2：Connector 共用驗證與 RemoteDefineAccess 防護

**目標檔案**：3 個 / **預估測試數**：約 20 個

6. **`SystemApiConnectorTests.cs`**（補純邏輯）
   - `Constructor_RemoteEmptyEndpoint_ThrowsArgumentException`
   - `Constructor_LocalSetsAccessToken`／`Constructor_RemoteSetsAccessToken`：驗證 `AccessToken`、`Provider` 型別正確（`LocalApiServiceProvider`／`RemoteApiServiceProvider`）
   - `ExecuteAsync` 透過 base，間接測 `ValidateArgs`：傳入空 progId（透過 base.ExecuteAsync 不易直接測，可用 `SystemApiConnector.ExecuteAsync(action, value)` 但 progId 由 `SysProgIds.System` 固定 → **改測 action 空字串**：`await Assert.ThrowsAsync<ArgumentException>(() => connector.ExecuteAsync<object>("", ...))`

7. **`FormApiConnectorTests.cs`**
   - `Constructor_LocalSetsProgIdAndProvider`
   - `Constructor_RemoteSetsProgIdAndProvider`
   - `Constructor_RemoteEmptyEndpoint_ThrowsArgumentException`
   - `ExecuteAsync_EmptyAction_ThrowsArgumentException`（透過 base 共用驗證）
   - `ExecuteAsync_NullProgIdConstructor_..`：建構子傳 null progId 是否該防護？目前不防護，依現況記錄

8. **`RemoteDefineAccessTests.cs`**
   - `GetDefine_TableSchema_MissingKeys_ThrowsArgumentException`：keys=null、keys=[]、keys=["onlyOne"]
   - `GetDefine_FormSchema_MissingKeys_ThrowsArgumentException`：keys=null、keys=["a","b"]
   - `GetDefine_FormLayout_MissingKeys_ThrowsArgumentException`：同上
   - `GetDefine_UnsupportedDefineType_ThrowsNotSupportedException`：傳入越界 enum 值（如 `(DefineType)999`）
   - `Constructor_StoresConnector`：可透過 reflection 驗證或改 internal getter；建議僅以後續測試間接驗證
   - **注意**：`GetSystemSettings` / `GetDatabaseSettings` 等 8 個 `SaveXxx`／`GetXxx` 委派方法都會呼叫 `Connector.GetDefine`，需 API server，**留 LocalOnly 或不測**

### 階段 3：以 `LocalApiServiceProvider` 走完整鏈路（評估後決定）

`LocalApiServiceProvider.ExecuteAsync` 透過 `JsonRpcExecutor` 進入 server-side 邏輯，由 `GlobalFixture` 已初始化的 `BackendInfo` 提供環境。**理論上可寫不需 API server 的測試**：呼叫一個必定會回應 `Error`（method not found）的請求，驗證 `JsonRpcResponse.Error.Code` 不為 0。

**評估**：

- ✅ 可貢獻 `LocalApiServiceProvider` 的覆蓋率
- ✅ 可順便覆蓋 `ApiConnector.Execute<T>`／`ExecuteAsync<T>` 的成功與錯誤路徑（Tracer 開／關、`response.Error != null` 拋 `InvalidOperationException`）
- ⚠ 與 `Bee.Api.Core` 的 `JsonRpcExecutorTests` 重疊，注意避免重複測試相同邏輯
- ⚠ 若 `JsonRpcExecutor` 的 method-not-found 行為日後改成拋例外而非回 `Error`，測試需同步調整

**決策**：階段 3 由執行人視階段 1+2 完成後實際覆蓋率決定是否進行。若已達 60% 目標可略過。

## 4. 預期效益

| 階段 | 涵蓋類別 | 預估新增測試數 | 預估覆蓋率提升 |
|------|---------|---------------|---------------|
| 階段 1 | `ApiClientContext`、`SyncExecutor`、`ApiConnectValidator`、`RemoteApiServiceProvider`、`LocalApiServiceProvider`（建構子） | ~30 | 0% → 約 35% |
| 階段 2 | `SystemApiConnector`、`FormApiConnector`、`ApiConnector`（共用驗證）、`RemoteDefineAccess`（防護） | ~20 | 約 35% → 約 60% |
| 階段 3（選擇性） | `LocalApiServiceProvider.ExecuteAsync`、`ApiConnector` 完整鏈路 | ~10 | 約 60% → 約 75% |

**對全 repo 的影響**：階段 1+2 完成後，全 repo 覆蓋率預計從 68.3% 提升至約 71%（330 行未覆蓋中消化約 200 行）。

## 5. 不在範圍內的事項

- 已存在的 `[LocalOnlyFact]` 測試保持原樣，不轉成純邏輯版本（兩種覆蓋面向不同）
- 不新增需要實際 HTTP server 的測試（如測 `RemoteApiServiceProvider.ExecuteAsync` 的成功路徑）
- 不重構 `RemoteApiServiceProvider.CreateHeaders` 為 `internal`／`protected`；若該方法無法以反射測，接受其未覆蓋
- 不調整 `ApiClientContext` 的 static 設計（如改為 instance class）— 雖然有測試隔離難題，但屬另一範疇的設計討論
- `Bee.Api.Client.csproj` 加 `[InternalsVisibleTo]`：階段 1+2 不需要；若階段 3 評估需要才加

## 6. 執行檢核項

每個階段完成後驗證：

- [ ] `dotnet build src/Bee.Api.Client/Bee.Api.Client.csproj --configuration Release --no-restore` 通過
- [ ] `dotnet test tests/Bee.Api.Client.UnitTests/Bee.Api.Client.UnitTests.csproj --settings .runsettings` 通過
- [ ] CI 模式（`CI=true`）下測試數＝新增數（既有 LocalOnly 仍 Skip）
- [ ] 不出現新的 SonarCloud／Roslyn 警告
- [ ] commit message 遵循專案慣例（`test(Bee.Api.Client): ...`）

## 7. 風險與緩解

| 風險 | 緩解 |
|------|------|
| `ApiClientContext` 為 static，測試之間污染狀態 | 每個測試使用 try/finally 還原；或改用 xUnit `IClassFixture` 集中管理快照／回填 |
| `ApiConnectValidator` 寫入 temp dir，CI 可能無寫入權限 | 使用 `Path.GetTempPath()` + 隨機子目錄；以 `try/finally` 確保清除；CI 環境（GitHub Actions runner）允許寫 temp |
| `RemoteDefineAccess` 越界 `DefineType` 測試未來可能因 enum 增加成員而失敗 | 註解標明意圖；若新增成員則更新測試對照表 |
| 階段 3 與 `Bee.Api.Core` 測試重疊 | 階段 3 僅在 client 端做最小化整合驗證（成功 1 + 失敗 1），不重測 server-side 邏輯 |
