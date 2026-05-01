# 計畫：重構 `CacheFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> ⚠️ 執行中發現 `TableSchemaCache` 加 `Get(string tableName)` 1-arg overload 會與 base class `KeyObjectCache<T>.Get(string key)` shadowing(CS0114),改採**不加 overload,讓 caller 顯式傳 `BackendInfo.DatabaseId`** 的方案。這也讓 caller 對 db 來源更明確。

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.ObjectCaching/CacheFunc.cs`(131 行,**13 個 public 方法**)

每個方法都是 1-line facade 呼叫 `CacheContainer.X.Y()`:
```csharp
public static SessionInfo? GetSessionInfo(Guid accessToken)
    => CacheContainer.SessionInfo.Get(accessToken);
public static void SetSessionInfo(SessionInfo sessionInfo)
    => CacheContainer.SessionInfo.Set(sessionInfo);
// ... 13 個都是這個 pattern
```

`CacheContainer`(`internal static class`)已用 `Lazy<T>` 管理 9 個 cache 子類單例(`SystemSettingsCache`、`DatabaseSettingsCache`、`SessionInfoCache` 等,皆 `internal class`)。

> 主計畫進度表寫 14,實際 audit 後 13 個。

> **2026-05-01 補充**:`ViewState` 是舊 .NET Framework Web Forms 概念,本框架已不使用,**整個移除**:
> - 刪除 `src/Bee.ObjectCaching/Runtime/ViewStateCache.cs`(該資料夾僅此一檔,連同空資料夾一起消失)
> - 刪除 `CacheContainer.ViewState` 屬性與相關 `Lazy<ViewStateCache>` 欄位
> - 刪除 `CacheFunc.SaveViewState` / `LoadViewState`(2 個方法,不搬移)
> - 刪除對應 2 個測試(`SaveViewState_ThenLoad_*`、`LoadViewState_MissingKey_*`)
>
> 處理後實際 method 數降為 **11**,cache 子類降為 **8**。

## 設計決策

### Path D:消除 facade,公開 `CacheContainer`

11 個保留方法**全部走 path D**,作法:

1. **`CacheContainer`**:`internal static class` → `public static class`,所有 cache 屬性也改 public(同時刪除 `ViewState` 相關)
2. **8 個 cache 子類**:`internal class` → `public class`(`KeyObjectCache<T>` 繼承不變,`protected override` 實作細節保持隱藏)
3. **新增 `TableSchemaCache.Get(string tableName)`**:overload 內部呼叫既有 2-arg `Get`,以 `BackendInfo.DatabaseId` 作為預設 db
4. **刪除 `ViewStateCache.cs` + `Runtime/` 資料夾**(舊 .NET Framework 遺物)
5. **刪除 `CacheFunc.cs`**

呼叫端改寫(11 個保留 method 對應;ViewState 2 個方法刪除無對應):

| 改前 | 改後 |
|------|------|
| `CacheFunc.GetSystemSettings()` | `CacheContainer.SystemSettings.Get()` |
| `CacheFunc.GetDatabaseSettings()` | `CacheContainer.DatabaseSettings.Get()` |
| `CacheFunc.GetProgramSettings()` | `CacheContainer.ProgramSettings.Get()` |
| `CacheFunc.GetDbSchemaSettings()` | `CacheContainer.DbSchemaSettings.Get()` |
| `CacheFunc.GetTableSchema(db, tbl)` | `CacheContainer.TableSchema.Get(db, tbl)` |
| `CacheFunc.GetTableSchema(tbl)` | `CacheContainer.TableSchema.Get(tbl)`(新 overload)|
| `CacheFunc.GetFormSchema(progId)` | `CacheContainer.FormSchema.Get(progId)` |
| `CacheFunc.GetFormLayout(id)` | `CacheContainer.FormLayout.Get(id)` |
| `CacheFunc.GetSessionInfo(t)` | `CacheContainer.SessionInfo.Get(t)` |
| `CacheFunc.SetSessionInfo(s)` | `CacheContainer.SessionInfo.Set(s)` |
| `CacheFunc.RemoveSessionInfo(t)` | `CacheContainer.SessionInfo.Remove(t)` |
| ~~`CacheFunc.SaveViewState`~~ | **刪除**(ViewState 整個移除)|
| ~~`CacheFunc.LoadViewState`~~ | **刪除**(同上)|

### 為何選 path D 而非 path C

- **path C(rename)**:`CacheFunc` → `Cache` 之類 noun,保留 facade。每個 method 都還是 1-line wrapper
- **path D(消除 facade)**:`CacheContainer.X.Y()` 直接呼叫,移除中間層

選 path D 的理由:
- 13 個方法**全部都是純 wrapper**,facade 沒有任何加值邏輯
- `CacheContainer` 與 cache 子類本來就有完整 API(`Get`/`Set`/`Remove`),公開後即使用即可
- 符合 .NET BCL `MemoryCache.Default` / `IMemoryCache` 的「直接訪問 cache」設計風格
- Cache 子類後續若新增方法,呼叫端立即可用,**不需更新 facade**

### 為何不做 path B(擴充方法)

- 13 個方法首參數是 `Guid`/`string`/`SessionInfo` 等,`Guid`/`string` 過度通用不能擴充;`SessionInfo` 等 domain 型別擴充也不自然(這些是 cache 操作,不是型別自身的行為)

### CA1724 檢查

- `CacheContainer`:不對應任何 BCL namespace 末段,安全
- 9 個 cache 子類(`SessionInfoCache` 等):皆有領域前綴,安全

### 為何 ViewState 完全移除

`ViewState` 是舊 .NET Framework Web Forms 的頁面狀態保存概念。本框架已不使用此模式(現代 .NET 用 SignalR / SPA / 其他狀態管理),`ViewStateCache` + 對應 `CacheFunc` 方法是遺留死碼,**0 個生產端 caller**(只有測試自我引用)。直接整個移除,連同資料夾。

## Method Audit 表

| # | 方法 | Prod | 路徑 | 新位置 |
|---|------|------|------|------|
| 1-11 | 11 個保留方法 | 10+ | **D** | `CacheContainer.X.Y()`(直接訪問)|
| 12-13 | `SaveViewState` / `LoadViewState` | 0 | **A 刪除** | — |

## 影響範圍

**全 repo grep `CacheFunc.` 結果(扣除 `bin/obj`)**:

| 類型 | 檔案 | 出現次數 |
|------|------|---------|
| 產品(類別定義) | `src/Bee.ObjectCaching/CacheFunc.cs` | 1 |
| 產品(`LocalDefineAccess` 各 GetX delegation) | `src/Bee.ObjectCaching/LocalDefineAccess.cs` | 7 |
| 產品(`SessionInfoService` 各方法) | `src/Bee.ObjectCaching/Services/SessionInfoService.cs` | 3 |
| 測試 | `tests/Bee.ObjectCaching.UnitTests/CacheFuncTests.cs` | 約 15(13 個 method 各種呼叫)|
| 測試 | `tests/Bee.ObjectCaching.UnitTests/CacheTests.cs` | 1 |
| 文件/註解 | `tests/Bee.Tests.Shared/GlobalFixture.cs` | 1(註解 cref) |
| 文件 | `docs/plans/plan-funcs-to-net-idiomatic.md` | 1 |

合計 10 處生產端 caller + 16 處測試 caller(都是機械式替換)。

## 執行步驟

### 1. 刪除 `ViewStateCache` + `Runtime/` 資料夾

```bash
git rm src/Bee.ObjectCaching/Runtime/ViewStateCache.cs
# 該資料夾僅此一檔,刪除後資料夾自動消失
```

### 2. 公開 `CacheContainer`(同時移除 ViewState 欄位)

`src/Bee.ObjectCaching/CacheContainer.cs`:
- `internal static class CacheContainer` → `public static class CacheContainer`
- 8 個屬性 `internal static` → `public static`(原 9 個 - ViewState 1 個)
- 移除 `viewState` 欄位與 `ViewState` 屬性

### 3. 公開 8 個 cache 子類

逐一改:
- `src/Bee.ObjectCaching/Database/SessionInfoCache.cs`:`internal class` → `public class`
- `src/Bee.ObjectCaching/Define/{SystemSettings,DatabaseSettings,ProgramSettings,DbSchemaSettings,TableSchema,FormSchema,FormLayout}Cache.cs`:同上(7 個檔案)

`KeyObjectCache<T>` / `ObjectCache` 等 base class 若已 public 不需改;若 internal 也要公開。

### 4. 在 `TableSchemaCache` 加 1-arg overload

```csharp
/// <summary>
/// Gets the table schema for the specified table in the default database.
/// </summary>
/// <param name="tableName">The table name.</param>
public TableSchema? Get(string tableName)
{
    return Get(BackendInfo.DatabaseId, tableName);
}
```

### 5. 更新生產端 caller(10 處)

`src/Bee.ObjectCaching/LocalDefineAccess.cs`(7 處)+ `src/Bee.ObjectCaching/Services/SessionInfoService.cs`(3 處):用 perl 批次替換,對應表如「設計決策」段。

### 6. 刪除 `CacheFunc.cs`

```bash
git rm src/Bee.ObjectCaching/CacheFunc.cs
```

### 7. 拆解測試

`tests/Bee.ObjectCaching.UnitTests/CacheFuncTests.cs` → 改名為 `CacheContainerTests.cs`,內容沿用,呼叫改 `CacheContainer.X.Y()`。**ViewState 相關 2 個測試(`SaveViewState_ThenLoad_*`、`LoadViewState_MissingKey_*`)直接刪除**。

`CacheTests.cs` 與 `GlobalFixture.cs` 內的 `CacheFunc` 引用同步更新。

### 8. 更新主計畫

進度表第 9 列:`📝` → `✅`,完成日填入,方法數 `14` → `11`(13 個 audit 後刪除 ViewState 2 個),處理路徑 `A+D`。

## 驗證

```bash
grep -rn "CacheFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj
dotnet build src/Bee.ObjectCaching/Bee.ObjectCaching.csproj --configuration Release --no-restore
./test.sh tests/Bee.ObjectCaching.UnitTests/Bee.ObjectCaching.UnitTests.csproj
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內的歷史紀錄
- Build 0 warning, 0 error
- Bee.ObjectCaching.UnitTests 數量不變,全綠

## Commit 訊息草稿

```
refactor(caching): eliminate CacheFunc facade, expose CacheContainer, drop ViewState

CacheFunc was a 13-method facade where every method was a 1-line
delegation to CacheContainer.X.Y(). Path D: promote CacheContainer
and the cache subclasses (SessionInfoCache, SystemSettingsCache,
TableSchemaCache, etc.) from internal to public, then update callers
to skip the facade.

This aligns with the BCL pattern (MemoryCache.Default exposes the
cache directly; consumers call Get / Set on it) and removes the
need to keep CacheFunc in sync whenever a cache subclass adds a new
method.

ViewState (legacy .NET Framework Web Forms concept, with zero
production callers) is removed entirely: ViewStateCache.cs and the
now-empty Runtime/ folder go away, along with the corresponding
CacheFunc.SaveViewState / LoadViewState methods and their tests.

TableSchemaCache gains a Get(tableName) overload that defaults the
database id from BackendInfo, replacing the corresponding facade
overload.

CacheFunc.cs is removed entirely. CacheFuncTests renamed to
CacheContainerTests; LocalDefineAccess and SessionInfoService
updated to call CacheContainer directly.

Ninth class executed under the *Func to .NET idiomatic refactor
(see docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

新增一條 idiom(由本次定案):

- **消除純 facade**:當 `*Func` 內所有方法都是 1-line delegation 到某個內部 container/class,**消除 facade、公開 container** 比保留 facade 更乾淨。判斷準則:facade 是否提供額外語意(預設值、組合、轉換)?**完全沒有 → 消除**;有 → 保留 facade(path C 改名)
- 公開 internal class 是合理的 path D 副作用,只要該類本來就設計為消費者可用(有 public Get/Set 等),且實作細節仍藏在 protected/private

沿用既有 idiom:
- 同 `BusinessFunc.GetDatabaseItem → BackendInfo.GetDatabaseItem` 的 path D「搬到 owning class」(本例 owning 是 cache 子類本身)
- 順手把方法名統一為 BCL idiomatic(`Save/Load` → `Set/Get`)是 path D 轉換時合理改名時機

## 風險與回滾

- 變動範圍:10 處生產端 caller + 16 處測試 caller + 9 個 cache 子類可見性變更 + `CacheContainer` 可見性變更
- Public API 大幅擴張(9 個 cache 子類從 internal 變 public),但無外部 NuGet 消費者
- 若失敗單一 `git revert` 即可回滾
