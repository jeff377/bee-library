# Bee.Business 測試覆蓋率提升計畫書

**狀態：✅ 已完成（2026-04-18）**

## 完成摘要

- **新增測試檔**：13 個 `.cs` 測試檔 + 2 個 Fakes（`FakeExecFuncHandler`、`TestableBusinessObject`/`BareBusinessObject`）
- **測試數量**：`Bee.Business.UnitTests` 由 14 tests 提升至 **83 tests 通過**（另 2 個 `[LocalOnlyFact]` 於 CI 略過）
- **覆蓋範圍**：
  - P0（純邏輯）：`BusinessFunc`、`ExecFuncAccessControlAttribute`、`ExecFuncArgs/Result`、`BusinessObject`、`BusinessObjectProvider`、`FormBusinessObject`、`SystemBusinessObject` 純邏輯路徑
  - P1（Fixture 支援）：`SystemBusinessObject` 的 `GetDefine`/`GetCommonConfiguration`、`DynamicApiEncryptionKeyProvider`、`StaticApiEncryptionKeyProvider`、`AccessTokenValidationProvider`
  - P2（DTO round-trip）：Bee.Business.System 下所有 `Args`/`Result`、`BusinessArgs`/`BusinessResult.Parameters` lazy 初始化
- **全案驗證**：`CI=true dotnet test --configuration Release --settings .runsettings` 無回歸，解決方案所有測試通過。

## 1. 背景與目標

`Bee.Business` 是 Bee.NET 框架的商業邏輯層，提供：
- 自訂方法執行框架（`ExecFunc` / `ExecFuncAnonymous`）
- 系統級操作（登入、Session、Define 存取、套件更新等）
- 暴力破解防護（`LoginAttemptTracker`）
- API 加密金鑰策略（靜態 / 動態）
- AccessToken 驗證

此層被 `Bee.Api.Core`（下游）依賴呼叫，是源碼掃描（SonarCloud / SAST）關注的安全敏感區。

**現況盤點**（`tests/Bee.Business.UnitTests/`）：

| 檔案 | 涵蓋內容 |
|------|----------|
| `LoginAttemptTrackerTests.cs` | ✅ 已完整覆蓋（10 tests） |
| `SystemBusinessObjectTests.cs` | ⚠ 僅 2 個 `[LocalOnlyFact]`，CI 模式下完全被略過 |
| `GlobalCollection.cs` / `GlobalFixture`（shared） | 已初始化 `BackendInfo`、`DefineAccess`、`DbProviderManager` |

Baseline：`dotnet test` 顯示 14 tests（含 Tests.Shared 共用），CI 模式下僅 12 通過（2 個 LocalOnly 略過），Bee.Business 自身測試在 SonarCloud 上覆蓋率極低。

**目標**：

1. 在不依賴外部資料庫／API 服務的前提下，補齊 `Bee.Business` 所有可純邏輯測試的類別，使 SonarCloud 顯示的 Line Coverage 顯著提升（預期 ≥ 70%；扣除必須連 DB 的部分後 ≥ 85%）。
2. 所有 `public` 類別至少有一個對應測試檔。
3. 不新增任何需連接真實資料庫的測試；既有 `[LocalOnlyFact]` 保持原樣。
4. 通過源碼掃描所列規則（`scanning.md` / `sonarcloud.md`）。

## 2. 現況盤點

以「資料夾分組」列出 `Bee.Business` 所有公開類別與目前測試狀態。

圖例：✅ 已完整覆蓋 / ⚠ 部分覆蓋（多為 LocalOnly）／ ❌ 完全無測試

### 2.1 根目錄

| 類別 | 狀態 | 純邏輯？ | 備註 |
|------|------|----------|------|
| `BusinessArgs`（abstract） | ❌ | ✅ | `Parameters` lazy init、setter/getter 可透過子類測 |
| `BusinessResult`（abstract） | ❌ | ✅ | 同上 |
| `ExecFuncArgs` | ❌ | ✅ | 兩個建構子、`FuncId` 預設值 |
| `ExecFuncResult` | ❌ | ✅ | 繼承 `BusinessResult` 的空類別 |
| `BusinessFunc`（static） | ❌ | ⚠ | `InvokeExecFunc` 純邏輯可測（透過測試用 handler）；`GetDatabaseItem` 需 `BackendInfo.DefineAccess`（Fixture 已初始化可測） |
| `IBusinessObject` / `IFormBusinessObject` / `ISystemBusinessObject` | n/a | — | 介面 |

### 2.2 Attributes

| 類別 | 狀態 | 純邏輯？ | 備註 |
|------|------|----------|------|
| `ExecFuncAccessControlAttribute` | ❌ | ✅ | 預設建構子、自訂 `AccessRequirement` 傳入、屬性 getter |

### 2.3 BusinessObjects

| 類別 | 狀態 | 純邏輯？ | 備註 |
|------|------|----------|------|
| `BusinessObject`（abstract） | ❌ | ✅ | 建構子、`AccessToken`／`IsLocalCall` 屬性、`ExecFunc`／`ExecFuncAnonymous` 委派至 `DoExecFunc*`（用測試子類即可覆蓋） |
| `FormBusinessObject` | ❌ | ✅ | 建構子、`ProgId` 屬性、`DoExecFunc*` 透過 `FormExecFuncHandler.Hello` 可覆蓋 |
| `SystemBusinessObject` | ⚠ | 混合 | `Ping` 純邏輯；`GetCommonConfiguration`、`GetDefine`、`SaveDefine` 使用 `BackendInfo.DefineAccess`（Fixture 可）；`Login`／`CreateSession` 需 Session Repository（LocalOnly）；`CheckPackageUpdate`／`GetPackage` 僅拋 `NotSupportedException`（純邏輯可測） |
| `BusinessObjectProvider` | ❌ | ✅ | `CreateSystemBusinessObject`、`CreateFormBusinessObject` 回傳正確型別 |
| `FormExecFuncHandler`（internal） | ❌ | ✅ | 建構子、`AccessToken`、`static Hello` 填入預期字串。internal 類別透過 `FormBusinessObject.DoExecFunc` 間接覆蓋，或透過 `InternalsVisibleTo` 直接測 |
| `SystemExecFuncHandler`（internal） | ❌ | ⚠ | `Hello` 純邏輯（帶 `[ExecFuncAccessControl(Anonymous)]`）；`UpgradeTableSchema` / `TestConnection` 需 RepositoryInfo（LocalOnly） |
| `IExecFuncHandler` | n/a | — | Marker 介面 |

### 2.4 Provider

| 類別 | 狀態 | 純邏輯？ | 備註 |
|------|------|----------|------|
| `LoginAttemptTracker` | ✅ | ✅ | 已完整涵蓋（10 tests） |
| `StaticApiEncryptionKeyProvider` | ❌ | ⚠ | 依賴 `BackendInfo.ApiEncryptionKey`（Fixture 會經 `autoCreateMasterKey: true` 自動設定）；`GenerateKeyForLogin` 走 `GetKey(Guid.Empty)`；未初始化時拋 `InvalidOperationException` 的分支可透過暫存還原測 |
| `DynamicApiEncryptionKeyProvider` | ❌ | ⚠ | `GetKey(Guid.Empty)` 拋 `UnauthorizedAccessException`；`GetKey` 查不到 session 拋例外；`GenerateKeyForLogin` 委派 `AesCbcHmacKeyGenerator`（可驗長度） |
| `CacheDataSourceProvider` | ❌ | ❌ | `GetSessionUser` 依賴 `RepositoryInfo.SystemProvider.SessionRepository`（LocalOnly） |

### 2.5 Validator

| 類別 | 狀態 | 純邏輯？ | 備註 |
|------|------|----------|------|
| `AccessTokenValidationProvider` | ❌ | ⚠ | `Guid.Empty` 拋例外分支可純邏輯測；其他路徑需 `BackendInfo.SessionInfoService`（Fixture 已初始化記憶體快取，可直接 Set 後測） |

### 2.6 System（Args/Result 純 DTO）

全部為 POCO，僅含屬性 getter/setter 與預設值，純邏輯可測：

| 類別 | 備註 |
|------|------|
| `CreateSessionArgs` / `CreateSessionResult` | UserID、ExpiresIn、OneTime／AccessToken、ExpiredAt |
| `LoginArgs` / `LoginResult` | UserId、Password、ClientPublicKey／AccessToken、ExpiredAt、ApiEncryptionKey、UserId、UserName |
| `PingArgs` / `PingResult` | ClientName、TraceId／Status="ok"、ServerTime=UtcNow、Version、TraceId |
| `GetDefineArgs` / `GetDefineResult` | DefineType、Keys／Xml |
| `SaveDefineArgs` / `SaveDefineResult` | DefineType、Xml、Keys／（空） |
| `GetCommonConfigurationArgs` / `GetCommonConfigurationResult` | （空）／CommonConfiguration |
| `CheckPackageUpdateArgs` / `CheckPackageUpdateResult` | Queries（List）／Updates（List） |
| `GetPackageArgs` / `GetPackageResult` | AppId、ComponentId、Version、Platform、Channel、FileId／FileName、Content、FileSize、Sha256、PackageUrl |

## 3. 測試缺口優先級

### P0（必補，純邏輯且 SonarCloud 覆蓋率影響最大）

| 項目 | 理由 |
|------|------|
| `BusinessFuncTests`（`InvokeExecFunc`） | 反射派發 + 權限驗證，是安全敏感路徑；多條分支（method 找不到 / Anonymous 呼叫 Authenticated / 正常呼叫 / 例外展開） |
| `ExecFuncAccessControlAttributeTests` | 預設與自訂建構子 |
| `ExecFuncArgsTests` / `ExecFuncResultTests` | 兩個建構子、`FuncId` 預設 / 設定 / 來回讀寫；`ExecFuncResult` 繼承基底 |
| `BusinessObjectTests`（用測試用子類） | 建構子、屬性、`ExecFunc` 委派至 `DoExecFunc`、`ExecFuncAnonymous` 委派至 `DoExecFuncAnonymous` |
| `BusinessObjectProviderTests` | 兩個工廠方法回傳正確型別、`isLocalCall` 預設 true、`progId` 寫入 |
| `SystemBusinessObjectPureLogicTests` | `Ping`（`TraceId` 回傳正確、`ServerTime` 為 UTC）、`CheckPackageUpdate` / `GetPackage` 基底版拋 `NotSupportedException`、`CreateSession` `ExpiresIn` 越界拋 `ArgumentOutOfRangeException`（上下界）、`GetDefine` / `SaveDefine` 非 LocalCall 時對 `SystemSettings`／`DatabaseSettings` 拋 `NotSupportedException` |

### P1（應補，需依賴 Fixture 初始化）

| 項目 | 理由 |
|------|------|
| `FormBusinessObjectTests` | 建構子、`ProgId` 屬性；`DoExecFunc` / `DoExecFuncAnonymous` 呼叫 `FormExecFuncHandler.Hello`（驗證 `result.Parameters["Hello"]`） |
| `SystemBusinessObjectDefineTests` | `GetCommonConfiguration`、`GetDefine`（有 LocalCall 情境）、`SaveDefine` round-trip 透過 Fixture 的 `LocalDefineAccess` |
| `DynamicApiEncryptionKeyProviderTests` | `Guid.Empty` → `UnauthorizedAccessException`、找不到 session → `UnauthorizedAccessException`、有 session → 回傳 key、`GenerateKeyForLogin` 回傳 64 bytes |
| `StaticApiEncryptionKeyProviderTests` | Fixture 已設定 `BackendInfo.ApiEncryptionKey`；`GetKey` / `GenerateKeyForLogin` 皆回傳該 key；以備份還原方式測未初始化分支拋例外 |
| `AccessTokenValidationProviderTests` | `Guid.Empty` 拋例外；以 `BackendInfo.SessionInfoService.Set(...)` 設定 session 後驗證正常、過期、token 不一致各分支 |

### P2（選補）

| 項目 | 理由 |
|------|------|
| `BusinessArgsTests` / `BusinessResultTests` | `Parameters` lazy init、setter 可重新指派（透過 `ExecFuncArgs`／`ExecFuncResult` 間接測） |
| DTO 屬性測試（System/*Args、*Result） | 屬性預設值（如 `PingResult.Status = "ok"`、`GetPackageArgs.Platform = "Win-x64"`）、setter/getter round-trip；數量多但每個只需 1~2 行 |
| `SystemExecFuncHandlerTests`（`Hello` 方法） | 填入 `result.Parameters["Hello"]`（需 `InternalsVisibleTo`，或透過 `SystemBusinessObject.ExecFuncAnonymous` 呼叫 `Hello` 間接覆蓋） |
| `FormExecFuncHandlerTests`（直接測 `Hello`） | 同上，需 `InternalsVisibleTo` 或經 `FormBusinessObject` 間接覆蓋 |

**不納入範圍**：

- `SystemBusinessObject.Login`、`CreateSession` — 需 Session Repository / 真實 DB（保留既有 LocalOnly 測試）
- `SystemExecFuncHandler.UpgradeTableSchema`、`TestConnection` — 需 RepositoryInfo
- `CacheDataSourceProvider` — 需 RepositoryInfo

## 4. 實施階段

採三階段，每階段結束後執行 `dotnet test --configuration Release --settings .runsettings` 並觀察覆蓋率。

### 階段 1：P0 核心（預估 1~1.5 日）

1. **`BusinessFuncTests`** — 於測試命名空間內建立 `FakeExecFuncHandler : IExecFuncHandler`，內含：
   - `public void Anonymous(ExecFuncArgs, ExecFuncResult)` 帶 `[ExecFuncAccessControl(Anonymous)]`
   - `public void Authenticated(ExecFuncArgs, ExecFuncResult)` 帶 `[ExecFuncAccessControl(Authenticated)]`
   - `public void Default(...)` 未帶 attribute（預設 Authenticated）
   - `public void Throws(...)` 刻意拋自訂例外
   - 驗證：找不到方法拋 `MissingMethodException`、Anonymous 呼叫 Authenticated 拋 `UnauthorizedAccessException`、Authenticated 呼叫 Authenticated 正常執行並填入 result、Anonymous 呼叫 Anonymous 正常、預設（無 attribute）時行為同 Authenticated、被叫方法拋例外時保留原始堆疊（以 `Assert.Throws<MyException>` 確認型別非 `TargetInvocationException`）
2. **`ExecFuncAccessControlAttributeTests`**
   - 預設建構子 → `AccessRequirement == Authenticated`
   - 傳入 `Anonymous`／`Authenticated` 各一條
3. **`ExecFuncArgsTests` / `ExecFuncResultTests`**
   - `ExecFuncArgs()` → `FuncId == string.Empty`
   - `ExecFuncArgs("Foo")` → `FuncId == "Foo"`
   - `Parameters` lazy 初始化（首次存取不為 null）、寫入後可讀回
   - `ExecFuncResult` 為 `BusinessResult` 的子類，`Parameters` 同樣 lazy 初始化
4. **`BusinessObjectTests`** — 建立測試用子類 `TestableBusinessObject : BusinessObject`，override `DoExecFunc`／`DoExecFuncAnonymous` 填入 marker 字串
   - 建構子：`AccessToken`、`IsLocalCall` 正確寫入；`isLocalCall` 預設 true
   - `ExecFunc` 回傳新 `ExecFuncResult` 並執行 override；`ExecFuncAnonymous` 同理
   - 未 override 時 `ExecFunc` 仍回傳空 `ExecFuncResult`（不拋例外）
5. **`BusinessObjectProviderTests`**
   - `CreateSystemBusinessObject(token)` 回傳 `SystemBusinessObject` 且 `AccessToken`、`IsLocalCall` 寫入正確
   - `CreateFormBusinessObject(token, progId)` 回傳 `FormBusinessObject` 且 `ProgId` 正確
   - `isLocalCall=false` 傳入有效
6. **`SystemBusinessObjectPureLogicTests`**
   - `Ping` 回 `Status=="ok"`、`TraceId` 等於傳入、`ServerTime` 接近 `DateTime.UtcNow`（±5 秒）
   - `CheckPackageUpdate` → `NotSupportedException`
   - `GetPackage` → `NotSupportedException`
   - `CreateSession` `ExpiresIn = 0` / `-1` / `86401` 皆拋 `ArgumentOutOfRangeException`
   - `GetDefine(DefineType=SystemSettings, isLocalCall=false)` → `NotSupportedException`
   - `GetDefine(DefineType=DatabaseSettings, isLocalCall=false)` → `NotSupportedException`
   - `SaveDefine` 對應兩條 `NotSupportedException`
   - **注意**：這些測試不碰 Repository，`isLocalCall` 非 LocalCall 時進入 guard 立刻拋例外，不會觸發 DefineAccess

### 階段 2：P1 Fixture-backed（預估 1 日）

1. **`FormBusinessObjectTests`**
   - `ProgId` 正確寫入
   - `ExecFunc(new ExecFuncArgs("Hello"))` → `result.Parameters["Hello"] == "Hello form-level BusinessObject"`
   - `ExecFuncAnonymous` 同理
   - 呼叫不存在方法 → `MissingMethodException`
2. **`SystemBusinessObjectDefineTests`**（`[Collection("Initialize")]`）
   - `GetCommonConfiguration` 回傳非空 XML（透過 Fixture 的 `LocalDefineAccess`）
   - `GetDefine(DefineType.DatabaseSettings, isLocalCall=true)` 回傳包含 `DatabaseSettings` 的 XML
   - `SaveDefine` + `GetDefine` round-trip 驗證寫入
3. **`DynamicApiEncryptionKeyProviderTests`**（`[Collection("Initialize")]`）
   - `GetKey(Guid.Empty)` → `UnauthorizedAccessException`
   - `GetKey(unknown)` → `UnauthorizedAccessException`
   - 先 `BackendInfo.SessionInfoService.Set(...)` 後 `GetKey(token)` 回傳該 session 的 `ApiEncryptionKey`
   - `GenerateKeyForLogin()` 回傳 64 bytes
4. **`StaticApiEncryptionKeyProviderTests`**（`[Collection("Initialize")]`）
   - `GetKey(Guid.Empty)` 回傳 `BackendInfo.ApiEncryptionKey`
   - `GenerateKeyForLogin()` 與 `GetKey` 同
   - 暫存 `BackendInfo.ApiEncryptionKey`、設為 `null`、測試 `GetKey` 拋 `InvalidOperationException`、還原（用 `try/finally` 避免污染其他測試）
5. **`AccessTokenValidationProviderTests`**（`[Collection("Initialize")]`）
   - `ValidateAccessToken(Guid.Empty)` → `UnauthorizedAccessException`
   - `ValidateAccessToken(unknown)` → `UnauthorizedAccessException`（查不到 session）
   - 建立過期的 `SessionInfo` 後呼叫 → `UnauthorizedAccessException`（`ExpiredAt < DateTime.UtcNow`）
   - 建立有效的 `SessionInfo` 後呼叫 → `true`

### 階段 3：P2 收斂與 DTO 覆蓋（預估 0.5 日）

1. **`SystemDtosTests`**（集中於一個檔案，使用 `[Theory]` 驗證所有 Args/Result 的預設值與屬性 round-trip）
   - `PingResult.Status == "ok"`
   - `GetPackageArgs.ComponentId == "Main"`、`Platform == "Win-x64"`、`Channel == "Stable"`、`AppId/Version/FileId == ""`
   - `CheckPackageUpdateArgs.Queries` 預設為空 List
   - 每個類別做 setter/getter round-trip（用 `Assert.Equal`）
2. **`BusinessArgsResultTests`**（透過 `ExecFuncArgs` / `ExecFuncResult` 子類）
   - `Parameters` 存取前為 null（用反射驗證 `_parameters` 欄位為 null）可略；只驗證首次存取非 null 即可
   - `Parameters` setter 可重新指派為新 `ParameterCollection`
3. **可選：internal handler 直接測試**
   - 若採用 `InternalsVisibleTo("Bee.Business.UnitTests")`（需修改 `Bee.Business.csproj`），可直接測 `FormExecFuncHandler.Hello`、`SystemExecFuncHandler.Hello`；否則維持階段 2 的間接覆蓋即可
   - **預設不修改 csproj**，僅做間接覆蓋，避免擴張專案對外可見介面

## 5. 風險與注意事項

1. **全域狀態污染**：測試修改 `BackendInfo.ApiEncryptionKey` / `SessionInfoService` 時必須以 `try/finally` 還原，避免影響後續測試。
2. **`SystemBusinessObject.Login`／`CreateSession` 不納入**：仍依 `SessionRepository` 連 DB。既有 `SystemBusinessObjectTests.cs` 的兩個 `[LocalOnlyFact]` 不動。
3. **`[ExecFuncAccessControl]` 反射解析**：在 `BusinessFuncTests` 中必須確認 attribute inheritance 為 true（檔案定義 `Inherited = true`），測試覆蓋衍生類別場景（選項）。
4. **`System.Exception` catch-all**：`BusinessFunc.InvokeExecFunc` 使用 `catch (Exception ex)` 後重新拋出，符合 `scanning.md` 的「重新拋出保留堆疊」要求；測試需驗證 `TargetInvocationException` 被 unwrap（`BaseFunc.UnwrapException`）。
5. **Nullable 警告**：新測試檔啟用 Nullable，`null!` 用於故意傳入 null 的測試情境。
6. **避免重複 DisplayName**：沿用 `<方法名稱>_<情境>_<預期結果>` 命名規則（如 `InvokeExecFunc_AnonymousCallAuthenticated_ThrowsUnauthorized`）。
7. **不引入 mock 框架**：全以測試用 Fake 類別（`TestableBusinessObject`、`FakeExecFuncHandler`）代替，與既有 `Bee.Db` 測試策略一致。
8. **覆蓋率回報**：CI 有 `coverlet` 收集，階段結束後於 PR 上觀察 SonarCloud 報告，必要時再補分支。

## 6. 交付清單

| 類型 | 檔案 | 預估測試數 |
|------|------|------------|
| 新增 | `BusinessFuncTests.cs`（含 `FakeExecFuncHandler`） | ~8 |
| 新增 | `ExecFuncAccessControlAttributeTests.cs` | 3 |
| 新增 | `ExecFuncArgsTests.cs` | 4 |
| 新增 | `ExecFuncResultTests.cs` | 2 |
| 新增 | `BusinessObjectTests.cs`（含 `TestableBusinessObject`） | ~6 |
| 新增 | `BusinessObjectProviderTests.cs` | 4 |
| 新增 | `SystemBusinessObjectPureLogicTests.cs` | ~10 |
| 新增 | `FormBusinessObjectTests.cs` | 4 |
| 新增 | `SystemBusinessObjectDefineTests.cs` | 3 |
| 新增 | `DynamicApiEncryptionKeyProviderTests.cs` | 4 |
| 新增 | `StaticApiEncryptionKeyProviderTests.cs` | 3 |
| 新增 | `AccessTokenValidationProviderTests.cs` | 4 |
| 新增 | `SystemDtosTests.cs`（`[Theory]` 驅動） | ~10 |
| 新增 | `BusinessArgsResultTests.cs`（經子類） | 3 |

**預估總新增**：約 **68 個測試**，Bee.Business.UnitTests 通過數由現行 12 → ~80（CI 模式）。

## 7. 完成標準

1. 所有新增測試在 `CI=true` 模式下通過（純邏輯不需外部資源）。
2. 依賴 Fixture 的測試在本地與 CI 皆通過（透過 `[Collection("Initialize")]`）。
3. SonarCloud `Bee.Business` Line Coverage 顯著上升（目標 ≥ 70%）。
4. 無新增 `[LocalOnlyFact]`／`[LocalOnlyTheory]`。
5. `dotnet build` 無新警告；符合 `scanning.md` / `sonarcloud.md` 規則。
6. 完成後在本計畫文件頂部補上 `**狀態：✅ 已完成（YYYY-MM-DD）**` 與完成摘要。
