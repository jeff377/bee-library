# 計畫：重構 `BusinessFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Business/BusinessFunc.cs`(74 行,2 個 public 方法)

```csharp
namespace Bee.Business;

public static class BusinessFunc
{
    public static DatabaseItem GetDatabaseItem(string databaseId);
    public static void InvokeExecFunc(
        IExecFuncHandler execFunc,
        ApiAccessRequirement currentRequirement,
        ExecFuncArgs args,
        ExecFuncResult result);
}
```

> 主計畫進度表寫 3 個方法,實際 audit 後僅 2 個 public method,清點誤計。

## Method Audit 表

| # | 方法簽章 | 處理路徑 | 新位置/名稱 | 替代方案備註 |
|---|---------|--------|------------|------------|
| 1 | `GetDatabaseItem(string databaseId)` | D | `BackendInfo.GetDatabaseItem` (`Bee.Definition`) | 主計畫已指名為 path D 範例 |
| 2 | `InvokeExecFunc(IExecFuncHandler, ...)` | B | `ExecFuncHandlerExtensions.InvokeExecFunc` (`Bee.Business`) | 加 `this` 修飾,呼叫端變 `handler.InvokeExecFunc(...)` |

兩個方法都搬走後,`BusinessFunc.cs` 整個檔案刪除。

### `GetDatabaseItem` — path D 細節

**現行邏輯**:
1. 驗證 `databaseId` 非空
2. 從 `BackendInfo.DefineAccess.GetDatabaseSettings()` 取 settings
3. 確認 `settings.Items` 內含此 ID
4. 回傳對應 `DatabaseItem`

**搬遷理由**:
- 生產端**零 caller**(grep 確認:只剩測試引用),`DbConnectionManager.CreateConnectionInfo` 已自有 inline 版本(用 `InvalidOperationException` 而非 `KeyNotFoundException`,不直接共用)
- 即使如此,主計畫已指定 `BusinessFunc.GetDatabaseItem` → `BackendInfo.GetDatabaseItem` 為 path D 範例,保留為公開 API 供將來使用,並讓 `BackendInfo` 變成查詢入口

**新方法位置**:`BackendInfo` 類別,加在現有 `Initialize*` 方法之後。
**例外行為保留**:`ArgumentNullException` / `KeyNotFoundException` 不變,測試斷言不需改。

### `InvokeExecFunc` — path B 細節

**現行呼叫**(4 處生產端 + 9 處測試端):
```csharp
BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Authenticated, args, result);
```

**轉為擴充方法後**:
```csharp
handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result);
```

**為何選 path B 而非 path C(`ExecFunc.Invoke` 之類的靜態類)**:
- 第一參數 `IExecFuncHandler` 是有意義的主體,`handler.InvokeExecFunc(...)` 讀起來像「handler 執行 ExecFunc」,語意自然
- 若用 `ExecFunc` 作為靜態類名,會與 `BusinessObject.ExecFunc()` 方法在 IDE 自動補全時混淆
- 用 `ExecFuncDispatcher` / `ExecFuncInvoker` 等替代名稱會增加陌生 type,擴充方法 + IDE 提示則零學習成本

**新類別**:`public static class ExecFuncHandlerExtensions`,放 `src/Bee.Business/ExecFuncHandlerExtensions.cs`。
**Namespace**:沿用 `Bee.Business`(與 `IExecFuncHandler` 同),呼叫端不需多 `using`。
**方法名**:保留 `InvokeExecFunc`(對 caller 來說只是把第一參數轉為 `this`)。

## 影響範圍

**全 repo grep `BusinessFunc` 結果(扣除 `bin/obj`)**:

| 類型 | 檔案 | 出現次數 |
|------|------|---------|
| 產品(類別定義) | `src/Bee.Business/BusinessFunc.cs` | 1 |
| 產品(InvokeExecFunc caller) | `src/Bee.Business/Form/FormBusinessObject.cs` | 2 |
| 產品(InvokeExecFunc caller) | `src/Bee.Business/System/SystemBusinessObject.cs` | 2 |
| 測試(GetDatabaseItem,3 處呼叫 + 1 cref) | `tests/Bee.Business.UnitTests/BusinessFuncTests.cs` | 4 |
| 測試(InvokeExecFunc,9 處呼叫 + 1 cref) | `tests/Bee.Business.UnitTests/BusinessFuncTests.cs` | 10 |
| 文件 | `docs/plans/plan-funcs-to-net-idiomatic.md` | 1(主計畫表中提及) |

合計 4 處生產端 caller + 13 處測試 caller(含 cref)。

## 執行步驟

### 1. 新增 `BackendInfo.GetDatabaseItem`

`src/Bee.Definition/BackendInfo.cs`:在 `InitializeSecurityKeys` 之前(或所有屬性之後、`Initialize` 系列之前)加入:

```csharp
/// <summary>
/// Gets the database item for the specified database identifier.
/// </summary>
/// <param name="databaseId">The database identifier.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="databaseId"/> is null or empty.</exception>
/// <exception cref="KeyNotFoundException">Thrown when no matching database item exists.</exception>
public static DatabaseItem GetDatabaseItem(string databaseId)
{
    if (string.IsNullOrWhiteSpace(databaseId))
        throw new ArgumentNullException(nameof(databaseId));

    var settings = DefineAccess.GetDatabaseSettings();
    if (!settings.Items!.Contains(databaseId))
        throw new KeyNotFoundException($"{nameof(databaseId)} '{databaseId}' not found.");

    return settings.Items[databaseId];
}
```

註:原方法用 `StrFunc.IsEmpty(databaseId)`,搬到 `BackendInfo` 後改用 BCL `string.IsNullOrWhiteSpace`(已是後續 path A 的方向,順手換掉避免冗餘相依)。

### 2. 新增 `ExecFuncHandlerExtensions.InvokeExecFunc`

`src/Bee.Business/ExecFuncHandlerExtensions.cs`(新檔):

```csharp
using System.Runtime.ExceptionServices;
using Bee.Base;
using Bee.Business.Attributes;
using Bee.Definition.Security;

namespace Bee.Business;

/// <summary>
/// Extension methods for <see cref="IExecFuncHandler"/>.
/// </summary>
public static class ExecFuncHandlerExtensions
{
    /// <summary>
    /// Invokes an ExecFunc method by reflection.
    /// </summary>
    /// <param name="handler">The handler that implements the method identified by FuncID.</param>
    /// <param name="currentRequirement">The access requirement of the current call.</param>
    /// <param name="args">The input arguments.</param>
    /// <param name="result">The output result.</param>
    public static void InvokeExecFunc(
        this IExecFuncHandler handler,
        ApiAccessRequirement currentRequirement,
        ExecFuncArgs args,
        ExecFuncResult result)
    {
        // (邏輯完全沿用原 BusinessFunc.InvokeExecFunc,把 execFunc 改名為 handler)
    }
}
```

### 3. 更新生產端 caller

`src/Bee.Business/Form/FormBusinessObject.cs`(2 處):
```csharp
// 改前
BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Authenticated, args, result);
// 改後
handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result);
```

`src/Bee.Business/System/SystemBusinessObject.cs`(2 處):同上。

### 4. 刪除 `BusinessFunc.cs`

兩個方法都搬走後,`src/Bee.Business/BusinessFunc.cs` 整個刪除。

### 5. 拆解測試

`tests/Bee.Business.UnitTests/BusinessFuncTests.cs` 拆成兩處:

#### 5a. `GetDatabaseItem` 測試 → 搬到 `Bee.Definition.UnitTests`

新增到 `tests/Bee.Definition.UnitTests/BackendInfoTests.cs`(已存在,加 3 個 method)。

由於 `FakeDefineAccess` 是 `internal sealed` 在 `Bee.Business.UnitTests.Fakes`,跨 test project 無法存取。在 `BackendInfoTests.cs` 內加一個極簡 private nested class:

```csharp
private sealed class FakeDefineAccessMinimal : IDefineAccess
{
    public DatabaseSettings Settings { get; } = new DatabaseSettings();
    public DatabaseSettings GetDatabaseSettings() => Settings;
    // 其餘成員 throw NotImplementedException
}
```

並維持 `[Collection("Initialize")]` 串行化(`BackendInfoTests` 已有此 attribute),避免與其他 mutate `BackendInfo.DefineAccess` 的測試 race。

3 個測試:
- `GetDatabaseItem_EmptyId_ThrowsArgumentNullException`
- `GetDatabaseItem_NotFound_ThrowsKeyNotFoundException`
- `GetDatabaseItem_Found_ReturnsItem`

#### 5b. `InvokeExecFunc` 測試 → 改名 + 換呼叫語法

`tests/Bee.Business.UnitTests/BusinessFuncTests.cs` → `tests/Bee.Business.UnitTests/ExecFuncHandlerExtensionsTests.cs`
- 類別:`BusinessFuncTests` → `ExecFuncHandlerExtensionsTests`
- 9 處 `BusinessFunc.InvokeExecFunc(handler, ...)` → `handler.InvokeExecFunc(...)`
- XML cref `<see cref="BusinessFunc"/>` 等改指向新類別
- 移除 GetDatabaseItem 相關 3 個測試(已搬走)
- 保留 `[Collection("Initialize")]`(因為仍有測試會 mutate `BackendInfo.DefineAccess`,例如 `FakeDefineAccess` 的注入)。檢查確認 InvokeExecFunc 9 個測試是否真的需要這個 collection:它們不 mutate BackendInfo,移除 attribute 應安全 —— 但風險小、保留也 OK。**保留**以避免回歸。

### 6. 更新主計畫

進度表第 2 列:`📝` → `✅`,完成日填入,方法數 `3` → `2`。

## 驗證

```bash
# 確認沒有遺漏的 BusinessFunc 引用
grep -rn "BusinessFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj

# Build 全部受影響專案
dotnet build src/Bee.Definition/Bee.Definition.csproj --configuration Release --no-restore
dotnet build src/Bee.Business/Bee.Business.csproj --configuration Release --no-restore

# Test
./test.sh tests/Bee.Business.UnitTests/Bee.Business.UnitTests.csproj
./test.sh tests/Bee.Definition.UnitTests/Bee.Definition.UnitTests.csproj
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內的歷史紀錄文字
- Build 0 warning, 0 error
- 兩個測試專案全綠(BackendInfoTests 從原本 3 個 test 變 6 個;ExecFuncHandlerExtensionsTests 9 個 test 全通過)

## Commit 訊息草稿

```
refactor(business): split BusinessFunc into BackendInfo and IExecFuncHandler extensions

GetDatabaseItem moves to BackendInfo (path D — domain integration into
the place that already owns DefineAccess and security keys).
InvokeExecFunc becomes ExecFuncHandlerExtensions.InvokeExecFunc, an
extension method on IExecFuncHandler (path B — first parameter is now
`this`, callers read `handler.InvokeExecFunc(...)`).

BusinessFunc.cs is deleted; tests are split between BackendInfoTests
(GetDatabaseItem, 3 cases) and ExecFuncHandlerExtensionsTests (rest).

Second class executed under the *Func → .NET idiomatic refactor (see
docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

本次定案的 idiom,後續同類型方法沿用:

- **Path D**:當靜態方法本質屬於某 domain object(已負責同類資料),搬到該 object 作為 static method,而非另立 utility 類
- **Path B(domain interface)**:遇到第一參數是 domain interface(非 BCL 型別)且能讀成「subject 動作」的方法,轉擴充方法,類別命名 `<Interface 主體>Extensions`(例:`IExecFuncHandler` → `ExecFuncHandlerExtensions`)
- 跨 test project 共用 fake 的策略:**就地建立 minimal nested fake**,避免移動到 `Bee.Tests.Shared` 引發 visibility 變動(若同一 fake 被多處使用才考慮共享)

## 風險與回滾

- 變動範圍:4 處生產端 caller + 13 處測試 caller + 拆 2 個測試檔
- 無外部 NuGet 消費者,可接受 public API breaking
- 若失敗單一 `git revert` 即可回滾
