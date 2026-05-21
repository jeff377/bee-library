# 計畫:清理 Connector 既有同步方法

**狀態:✅ 已完成(2026-05-22)**

## 1. 背景

本計畫是 [plan-bo-crud-methods.md](plan-bo-crud-methods.md) 的姐妹計畫。
`plan-bo-crud-methods` 已確立「新加 connector method 預設 async-only」為今後慣例(該計畫 §1.3),
本計畫負責**清理既有的同步 connector 方法**(`SystemApiConnector` × 10、`FormApiConnector` × 2),
讓整個 `Bee.Api.Client` 收斂為 async-only 對外介面。

兩 plan 互無實作順序依賴:
- `plan-bo-crud-methods` 只**新增** async-only 方法 → 可獨立 ship
- 本計畫只**移除**既有同步方法 + 內部改寫 → 可獨立 ship
- 任一順序執行皆可

### 為何要清理

`SyncExecutor.Run` 用 `Task.Run(...).GetAwaiter().GetResult()` 雖然避開了 SynchronizationContext 死鎖,但代價是「當前 thread 阻塞 + thread pool 借一個 thread 跑 async work」**雙倍 thread 佔用**。
這在 Blazor Server / ASP.NET Core 等 thread-pool-bound 場景特別痛 —— 每次同步 connector 呼叫等於少一個並發 SignalR 連線。

[plan-bo-crud-methods.md](plan-bo-crud-methods.md) 已對「為何 async-only」做完整論述,本計畫不重複。

---

## 2. 設計 baseline:`IDefineAccess` 維持 sync(不 async 化)

本計畫採用「**`IDefineAccess` 介面不動,把 sync-over-async 集中在 `RemoteDefineAccess` 一個檔內**」的方案,
而**非**對全 repo 進行 async 化。

### 對比:全 async 化 vs IDefineAccess 維持 sync

| 比較項 | 全 async 化整條 chain | IDefineAccess 維持 sync(本方案) |
|--------|---------------------|---------------------------------|
| `IDefineAccess` 介面 | 改為 async(15 個 method 都要加 Async 版) | 不動 |
| `LocalDefineAccess` / `RemoteDefineAccess` | 全部 method 改 async | 只動 `RemoteDefineAccess` 兩處 |
| `Bee.Business` 所有 BO | 全部 `GetDefine` 呼叫點改 await | 不動 |
| `Bee.Repository` 整鏈 | 全部 `IDefineAccess.*` 呼叫點改 await | 不動 |
| `Bee.Db` 的 `SelectContextBuilder` / `SelectCommandBuilder` / `IFormCommandBuilder` | 介面與實作改 async | 不動 |
| 全部測試 | 改 async,大量改寫 | 只動 connector 同步測試 |
| `bee-ui-core` 桌面端 | 大量呼叫點改 async,跨 repo 大協調 | 1 檔 2 行 |
| 變動檔案數估算 | ~80+ | ~5 |
| 性能 | BO/Repository 真 async | `RemoteDefineAccess` 仍 sync,但其內建快取吸收 — `SyncExecutor.Run` 只在 cache miss 觸發 |

### 為何 `IDefineAccess` 維持 sync 是合理的

- **`LocalDefineAccess` 本來就是本地檔案 IO**,async 化沒有實質好處
- **`RemoteDefineAccess` 內建快取**([src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs:73-83](../../src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs#L73-L83)):
  - 熱路徑命中快取,不打 HTTP,sync 即可
  - 只有 cache miss 才觸發底層 HTTP,本計畫透過 `SyncExecutor.Run` 包覆,sync-over-async 開銷可忽略
- **`IDefineAccess` 是 Definition 取得介面**,語意上偏「靜態查表」,async 化只增加噪音

### 已確認:`RemoteDefineAccess` 內呼叫 connector 同步版只有 **2 處**

完整盤點 [src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs](../../src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs):

| 行號 | 呼叫 | 用途 |
|------|------|------|
| **L79** | `this.Connector.GetDefine<T>(defineType, keys)` | private generic `GetDefine<T>`,被所有 typed Getter(`GetSystemSettings` / `GetFormSchema` / `GetFormLayout` / `GetTableSchema` / ...)透過 dispatch 表共用 |
| **L136** | `this.Connector.SaveDefine(defineType, defineObject, keys)` | public `SaveDefine`,被所有 typed Setter(`SaveSystemSettings` 等)共用 |

兩處改 `SyncExecutor.Run(() => Connector.XxxAsync(...))` 即可斷開對 connector 同步版的依賴。
不需逐 method 改寫,因為所有 typed 方法都經過這兩個 funnel。

---

## 3. 範圍

### 3.1 動的部分(本 repo,~5 檔)

| 檔案 | 異動 |
|------|------|
| `src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs` | L79 與 L136 兩處改用 `SyncExecutor.Run(() => Connector.XxxAsync(...))` |
| `src/Bee.Api.Client/Connectors/SystemApiConnector.cs` | 移除 10 個同步方法 |
| `src/Bee.Api.Client/Connectors/FormApiConnector.cs` | 移除 2 個同步方法 |
| `src/Bee.Api.Client/ApiConnectValidator.cs` | L128 `connector.Ping()` 改 `SyncExecutor.Run(() => connector.PingAsync())` |
| `tests/Bee.Api.Client.UnitTests/SystemApiConnectorTests.cs` | L40 `CreateSession` 與 L103 `Ping` 改 async 測試 |

> `SyncExecutor` 本身**保留**(`RemoteDefineAccess` 與 `ApiConnectValidator` 內部仍需用),不刪除。

### 3.2 動的部分(跨 repo `bee-ui-core`,1 檔 2 行)

| 檔案 | 異動 |
|------|------|
| `bee-ui-core/src/Bee.UI.Core/ClientInfo.cs` | L165 與 L185 兩處 `SystemApiConnector.Initialize()` 改 async |

ClientInfo 是 static class,改寫策略:
- 選項 A:`Initialize` 全套改 async,連帶呼叫鏈往上走
- 選項 B:用 `SyncExecutor.Run(() => sc.InitializeAsync())` 局部包覆,UI thread 介面不變

選項 B 的副作用最小(桌面 WinForms / WPF 流程不需大改),建議採用。
精確策略由本計畫實作階段或 bee-ui-core 端決定,本計畫只標出「**bee-library v? 發布後 bee-ui-core 需同步更新**」的協調點。

### 3.3 不動的部分

明確不在本計畫範圍:

- `IDefineAccess` 介面(不 async 化)
- `LocalDefineAccess`(本來就是本地檔案 IO)
- `RemoteDefineAccess` 的其他 method(只動兩個 funnel)
- 所有 `Bee.Business` 的 BO
- 所有 `Bee.Repository` 的 Repository
- `Bee.Db` 的 `SelectContextBuilder` / `SelectCommandBuilder` / `IFormCommandBuilder`
- `Bee.Api.Client/Connectors/ApiConnector.cs` base class 的 `protected ExecuteAsync<T>`(本來就是 async)
- `SyncExecutor`(保留,改為集中於 2 個內部消費點)
- 新增 CRUD 方法(由 `plan-bo-crud-methods.md` 處理)

---

## 4. 同步方法完整清單

### 4.1 `SystemApiConnector`(10 個移除)

| 方法 | 對應的 async 版本 |
|------|------------------|
| `ExecFunc(ExecFuncRequest)` | `ExecFuncAsync` |
| `Ping()` | `PingAsync` |
| `Initialize()` | `InitializeAsync` |
| `CreateSession(string, int, bool)` | `CreateSessionAsync` |
| `Login(string, string)` | `LoginAsync` |
| `GetDefine<T>(DefineType, string[]?)` | `GetDefineAsync<T>` |
| `SaveDefine(DefineType, object, string[]?)` | `SaveDefineAsync` |
| `EnterCompany(string)` | `EnterCompanyAsync` |
| `LeaveCompany()` | `LeaveCompanyAsync` |
| `Logout()` | `LogoutAsync` |

> 全部既有同步方法都已有 1:1 對應的 async 版本(已在 `SystemApiConnector.cs` 確認),刪除後呼叫端只需把 `connector.X(...)` 改為 `await connector.XAsync(...)`。

### 4.2 `FormApiConnector`(2 個移除)

| 方法 | 對應的 async 版本 |
|------|------------------|
| `ExecFunc(ExecFuncRequest)` | `ExecFuncAsync` |
| `GetList(...)` | `GetListAsync` |

### 4.3 `ApiConnector` base class

無 public 同步方法(`ExecuteAsync<T>` 是 protected,本來就 async),不需動。

---

## 5. 真實 callers 完整清單

### 5.1 本 repo prod code(3 處,全部會在本計畫內改寫)

| 位置 | 呼叫 | 改寫方式 |
|------|------|---------|
| `src/Bee.Api.Client/ApiConnectValidator.cs:128` | `connector.Ping()` | 改 `SyncExecutor.Run(() => connector.PingAsync())` |
| `src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs:79` | `Connector.GetDefine<T>(...)` | 改 `SyncExecutor.Run(() => Connector.GetDefineAsync<T>(...))` |
| `src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs:136` | `Connector.SaveDefine(...)` | 改 `SyncExecutor.Run(() => Connector.SaveDefineAsync(...))` |

### 5.2 本 repo 測試(2 處,本計畫一併改 async)

| 位置 | 呼叫 |
|------|------|
| `tests/Bee.Api.Client.UnitTests/SystemApiConnectorTests.cs:40` | `connector.CreateSession(...)` |
| `tests/Bee.Api.Client.UnitTests/SystemApiConnectorTests.cs:103` | `connector.Ping()`(包在 `Record.Exception` 內) |

xUnit 完全支援 `async Task` test method 與 `Record.ExceptionAsync`,改寫成本低。

### 5.3 `bee-ui-core` 跨 repo(2 處,本計畫實作 PR 同步協調)

| 位置 | 呼叫 |
|------|------|
| `bee-ui-core/src/Bee.UI.Core/ClientInfo.cs:165` | `SystemApiConnector.Initialize()`(實際是 instance method) |
| `bee-ui-core/src/Bee.UI.Core/ClientInfo.cs:185` | `SystemApiConnector.Initialize()` |

> 已 grep 過 `bee-ui-core/src/` 內所有 `.cs`,確認**只有這 2 處**呼叫 connector 同步版。沒有其他隱性依賴。

### 5.4 BO 層的同名方法不在影響範圍

`grep .GetList(` / `.ExecFunc(` 等會 match 到大量 BO 層 method(如 `bo.ExecFunc(new ExecFuncArgs(...))`),這些是 **BO 上的同步 method**,不是 connector 同步 method。BO 層由 `plan-bo-crud-methods` §1.3 確認「BO 端維持純同步」,本計畫一併不動。

---

## 6. `RemoteDefineAccess` 改寫策略

### 6.1 改寫 pattern

```csharp
// 改寫前(L79)
defineObject = this.Connector.GetDefine<T>(defineType, keys);

// 改寫後
defineObject = SyncExecutor.Run(() => this.Connector.GetDefineAsync<T>(defineType, keys));
```

```csharp
// 改寫前(L136)
this.Connector.SaveDefine(defineType, defineObject, keys);

// 改寫後
SyncExecutor.Run(() => this.Connector.SaveDefineAsync(defineType, defineObject, keys));
```

### 6.2 快取行為驗證

- 改寫前後,`GetDefine<T>` 的快取邏輯(L75-L82)結構不變,只置換內部 connector 呼叫
- `SyncExecutor.Run` 只在快取 miss(L77 `if (!this.List.TryGetValue(cacheKey, out object? defineObject))` 為 true)時觸發
- 熱路徑(快取命中)完全不經 `SyncExecutor.Run`,性能與改寫前相同

### 6.3 例外處理對齊

`SyncExecutor.Run` 內部使用 `Task.Run(...).GetAwaiter().GetResult()`,其例外行為:
- **既有 connector 同步版**:已經透過 `SyncExecutor.Run` 包裝,例外行為與本計畫改寫後**完全相同**
- 既有同步版本如 `SystemApiConnector.SaveDefine`(L267-L272)實作就是 `SyncExecutor.Run(() => SaveDefineAsync(...))`,呼叫端 try/catch 的例外與直接呼叫 async 經 `GetAwaiter().GetResult()` 結果一致

→ **無例外語意變化**,呼叫端 try/catch 不需調整。

### 6.4 `IDefineAccess.GetDefine(DefineType, string[]?)` 與 typed Getter 的關係

[RemoteDefineAccess.cs:90-114](../../src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs#L90-L114) 的 public `GetDefine` 是 dispatch 表,switch 後呼叫 typed Getter(`GetSystemSettings` 等),最終都 funnel 到 private generic `GetDefine<T>`(L73-L83)→ 命中 connector L79。

```
public GetDefine(DefineType, keys)
  ↓ switch
public GetSystemSettings() / GetFormSchema() / ...
  ↓
private GetDefine<T>(DefineType, keys)
  ↓ cache miss
this.Connector.GetDefine<T>(...)   ← L79,本計畫改寫點
```

所以**改 L79 一處就能讓所有 typed Getter 同時切換到 async connector**,不需逐 method 改寫。
`SaveDefine` 同理:所有 typed Setter(`SaveSystemSettings` 等)都 funnel 到 L136 的 public `SaveDefine`。

---

## 7. `SyncExecutor` 的去留

### 7.1 保留,但收斂消費點

本計畫後 `SyncExecutor.Run` 的消費點:
1. `RemoteDefineAccess.GetDefine<T>` private(L79 改寫後)
2. `RemoteDefineAccess.SaveDefine` public(L136 改寫後)
3. `ApiConnectValidator.Ping` 呼叫(L128 改寫後)
4. `ApiConnectValidator` 既有 L124 `HttpUtilities.IsEndpointReachableAsync` 包覆(不動)
5. `SyncExecutor` 自身的單元測試 `tests/Bee.Api.Client.UnitTests/SyncExecutorTests.cs`(保留)

### 7.2 不打 `[Obsolete]`,但加 XML doc 警告

`SyncExecutor` 仍是 public class(在 `Bee.Api.Client` 內)。打 `[Obsolete]` 會讓上述 5 個消費點 build fail(`TreatWarningsAsErrors=true`)。

替代方案:在 `SyncExecutor` 的 XML doc remarks 加註:

```xml
/// <remarks>
/// Intended only for bridging synchronous interfaces (e.g., <c>IDefineAccess</c>) over
/// asynchronous connector calls. New code should use the connector's <c>*Async</c>
/// methods directly. Do not introduce new <c>SyncExecutor.Run</c> call sites without
/// architectural review.
/// </remarks>
```

> 改 `internal` 也是選項,但會破壞既有 NuGet 套件 public API。若 v5 確定有破壞性更新,可再考慮改 `internal`,本計畫先以 doc 標示意圖。

---

## 8. 階段策略:一刀切(推薦)

### 8.1 推薦:一刀切(策略 B)

由於本 repo prod callers 真的很少(3 處 prod + 2 處測試),且全部會在本計畫內一起改寫,
建議**單一 PR 完成 bee-library 端所有變動**:

1. 改寫 `RemoteDefineAccess` L79 / L136
2. 改寫 `ApiConnectValidator` L128
3. 改寫 `SystemApiConnectorTests` L40 / L103
4. 移除 `SystemApiConnector` 10 個同步方法
5. 移除 `FormApiConnector` 2 個同步方法
6. `SyncExecutor` XML doc 加註

PR 通過後發布新版 NuGet 套件,接著:
7. 同步更新 `bee-ui-core/src/Bee.UI.Core/ClientInfo.cs` 兩處(獨立 PR,bee-ui-core repo)

### 8.2 不採用:漸進 `[Obsolete]`(策略 A)

`TreatWarningsAsErrors=true` 下,加 `[Obsolete]` 立刻 build fail。
要走漸進策略,反而必須**先改完所有 callers 才能加 `[Obsolete]`** —— 順序與直覺相反,
且改完 callers 後就可以直接刪除,加 `[Obsolete]` 多此一舉。

> 結論:本計畫不採用 `[Obsolete]` 過渡,直接走一刀切。

### 8.3 版本號

建議將本計畫納入下一個 **minor version**(例如 `v4.2.0`):
- 本計畫是 breaking change(刪除 public method),嚴格意義應該是 major bump
- 但實際 callers 只有內部 + `bee-ui-core` 1 檔 2 行,影響面極小
- minor bump + CHANGELOG 明確標示 breaking 即可
- 若選擇 v5.0.0 major bump,可一併納入其他清理(`SyncExecutor` 改 `internal`、`IDefineAccess` 命名整理等)

版本號選擇由實作階段決定,本計畫只建議「**獨立發版,CHANGELOG 標示 breaking change**」。

---

## 9. 跨 repo 協調

### 9.1 `bee-ui-core` 同步更新

實作時序:
1. **bee-library**:單一 PR 合併 → 發布新版 NuGet 套件
2. **bee-ui-core**:升 `Bee.Api.Client` NuGet 版本 → build 立即 fail(`SystemApiConnector.Initialize()` 同步呼叫已不存在)→ 改 `ClientInfo.cs:165` / `:185` 兩處 → 合併
3. 兩 repo 都通過後,doc 同步更新

### 9.2 `bee-ui-core` 端改寫建議

`ClientInfo.Initialize` 是 static 流程,涉及 UI thread。建議:

```csharp
// 改寫前
SystemApiConnector.Initialize();

// 改寫後(維持 ClientInfo public API 為同步)
SyncExecutor.Run(() => SystemApiConnector.InitializeAsync());
```

`SyncExecutor` 是 public,bee-ui-core 可直接使用。
若 bee-ui-core 端想趁機 async 化 `ClientInfo`,可另外開 plan,**不卡本計畫**。

### 9.3 其他下游 consumer

本 NuGet 套件可能有未知的外部 consumer。處理方式:
- CHANGELOG 明確標示 breaking change + migration guide(同步方法 → `*Async`)
- README 加註「v4.2.0 起 connector 改為 async-only」
- migration guide 內提供 `SyncExecutor.Run(() => XxxAsync(...))` 作為臨時 workaround,讓桌面端有過渡期

---

## 10. 測試策略

### 10.1 既有測試遷移

`tests/Bee.Api.Client.UnitTests/SystemApiConnectorTests.cs` 改寫範例:

```csharp
// 改寫前
[DisplayName("CreateSession 應回傳新 AccessToken")]
public void CreateSession_ReturnsNewToken()
{
    var connector = ...;
    Guid newToken = connector.CreateSession(userId, expiresIn, oneTime);
    Assert.NotEqual(Guid.Empty, newToken);
}

// 改寫後
[DisplayName("CreateSessionAsync 應回傳新 AccessToken")]
public async Task CreateSessionAsync_ReturnsNewToken()
{
    var connector = ...;
    Guid newToken = await connector.CreateSessionAsync(userId, expiresIn, oneTime);
    Assert.NotEqual(Guid.Empty, newToken);
}
```

`Ping` 測試:`Record.Exception` 改 `Record.ExceptionAsync`,搭配 `async Task` 測試簽名。

### 10.2 `RemoteDefineAccess` 行為一致性測試

新增至 `tests/Bee.Api.Client.UnitTests/`(或對應 fixture 內):

| 測試名 | 驗證點 |
|--------|--------|
| `RemoteDefineAccess_GetSystemSettings_ReturnsValueOnFirstCall` | 第一次呼叫(cache miss)觸發底層 async,回傳值正確 |
| `RemoteDefineAccess_GetSystemSettings_UsesCacheOnSecondCall` | 第二次呼叫不再進入 `SyncExecutor.Run`(命中快取),回傳同一物件 |
| `RemoteDefineAccess_SaveSystemSettings_Succeeds` | SaveDefine 經 `SyncExecutor.Run` 包覆後行為正常,無例外語意改變 |
| `RemoteDefineAccess_GetDefine_PropagatesConnectorException` | 底層 connector async 拋出例外時,sync 呼叫端收到的例外型別與既有行為一致 |

### 10.3 `SyncExecutor` 自身測試

`tests/Bee.Api.Client.UnitTests/SyncExecutorTests.cs` **保留不動** — `SyncExecutor` 仍是 public class,測試覆蓋率不該下降。

### 10.4 不需新增的測試

- BO / Repository / Db chain 測試:本計畫不動這些層,既有測試應全部繼續 pass
- `LocalDefineAccess` 測試:本計畫不動,既有測試應全部繼續 pass

---

## 11. 風險與緩解

### 11.1 例外語意改變

**風險**:`SyncExecutor.Run` 內 `Task.Run().GetAwaiter().GetResult()` 與 connector 同步版的例外行為若不一致,呼叫端 try/catch 可能漏接。

**緩解**:
- 既有 connector 同步版本身就是 `SyncExecutor.Run` 包裝(`SystemApiConnector.cs:62, 113, 137, 168, 206, 239, 269...`),例外路徑與本計畫改寫後**完全相同**
- §10.2 第 4 條測試明確驗證這點

### 11.2 `bee-ui-core` 升級延誤

**風險**:bee-library 發版後,bee-ui-core 升級延誤,期間使用者拿到不相容的兩版。

**緩解**:
- 同一 PR cycle 內把 bee-ui-core 的對應更新一起做完
- bee-library 發版 commit message 內明確標示「需同步升級 bee-ui-core 至 v?」
- CHANGELOG 明示 minimum compatible bee-ui-core version

### 11.3 未知外部 consumer

**風險**:有非預期的下游使用者依賴同步方法,升級後 build fail。

**緩解**:
- CHANGELOG 提供 migration guide(每個被移除方法 → 對應的 async 版本 + `SyncExecutor.Run` workaround)
- 此為 breaking change 的自然代價;沒有 deprecation 過渡是因為 `TreatWarningsAsErrors` 機制讓過渡無意義

### 11.4 性能 regression

**風險**:`RemoteDefineAccess` 內加 `SyncExecutor.Run` 包覆,cache miss 時多一次 thread pool 切換。

**緩解**:
- 既有同步版底下就是 `SyncExecutor.Run`,改寫後**完全相同**,無 regression
- 命中快取路徑不變

---

## 12. 不在本計畫範圍

- **`IDefineAccess` async 化**:明確排除,理由見 §2 對比表
- **`Bee.Business` / `Bee.Repository` / `Bee.Db` chain async 化**:不動,理由見 §2 與 §3.3
- **`SyncExecutor` 改 `internal`**:本計畫只加 XML doc 警告;改 `internal` 留給 v5 major 計畫(若有)
- **新增 CRUD 方法**:由 `plan-bo-crud-methods.md` 處理
- **BO 端同步方法清理**:`FormBusinessObject` / `SystemBusinessObject` 等的同步 `ExecFunc` / `GetList` 等不動,理由 `plan-bo-crud-methods.md` §1.3 已說明(`JsonRpcExecutor.InvokeMethodAsync` 自動處理 sync/async,BO 端是否 async 對 wire 行為無影響)
- **`HttpUtilities.IsEndpointReachableAsync` 的 sync 包裝(`ApiConnectValidator:124`)**:這是 `SyncExecutor` 的合理使用,不動
- **CHANGELOG 撰寫**:由 `/changelog-draft` skill 在發版時處理,不在本計畫範圍

---

## 13. Checklist

實作收尾時逐項勾選:

- [ ] `RemoteDefineAccess.cs` L79 改用 `SyncExecutor.Run(() => Connector.GetDefineAsync<T>(...))`
- [ ] `RemoteDefineAccess.cs` L136 改用 `SyncExecutor.Run(() => Connector.SaveDefineAsync(...))`
- [ ] `ApiConnectValidator.cs` L128 改用 `SyncExecutor.Run(() => connector.PingAsync())`
- [ ] `SystemApiConnectorTests.cs` L40 改 `async Task` + `CreateSessionAsync`
- [ ] `SystemApiConnectorTests.cs` L103 改 `async Task` + `Record.ExceptionAsync(() => connector.PingAsync())`
- [ ] `SystemApiConnector.cs` 移除 10 個同步方法(`ExecFunc` / `Ping` / `Initialize` / `CreateSession` / `Login` / `GetDefine<T>` / `SaveDefine` / `EnterCompany` / `LeaveCompany` / `Logout`)
- [ ] `FormApiConnector.cs` 移除 2 個同步方法(`ExecFunc` / `GetList`)
- [ ] `SyncExecutor.cs` 加 XML doc remarks(§7.2)
- [ ] 新增 §10.2 的 4 條 `RemoteDefineAccess` 行為一致性測試
- [ ] `dotnet build --configuration Release` 通過,確認無 callers 遺漏(`TreatWarningsAsErrors=true` 會強制顯現)
- [ ] `./test.sh` 全部 pass
- [ ] bee-library PR 合併、發布新版 NuGet 套件(版本號於實作階段決定,§8.3)
- [ ] `bee-ui-core/src/Bee.UI.Core/ClientInfo.cs:165` / `:185` 兩處改 `SyncExecutor.Run(() => SystemApiConnector.InitializeAsync())`(獨立 PR,bee-ui-core repo)
- [ ] bee-library CHANGELOG 標示 breaking change + 提供 migration guide(每個被移除方法 → 對應 async 版本 + workaround)
- [ ] 計畫文件頂部狀態列改為 `**狀態:✅ 已完成(YYYY-MM-DD)**`
