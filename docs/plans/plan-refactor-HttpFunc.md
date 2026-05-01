# 計畫：重構 `HttpFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> ⚠️ 執行中發現原計畫提議的類名 `Http` 觸發 Roslyn analyzer **CA1724**(類名與 BCL `System.Net.Http` namespace 名稱衝突),改為 **`HttpUtilities`** 並執行落地。下方 audit 表已更新為實際結果。

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Base/HttpFunc.cs`(148 行,4 個 public + 1 個 private)

```csharp
namespace Bee.Base;

public static class HttpFunc
{
    private static readonly ConcurrentDictionary<string, HttpClient> _clientMap = ...;
    private static HttpClient GetOrCreateClient(string fullUrl);  // private pool helper

    public static bool IsUrl(string input);
    public static Task<bool> IsEndpointReachableAsync(string endpoint, TimeSpan? timeout = null);
    public static Task<string> PostAsync(string endpoint, string body, NameValueCollection? headers = null);
    public static Task<string> GetAsync(string endpoint, NameValueCollection? headers = null);
}
```

> 主計畫進度表寫 5 個方法,實際 audit 後 4 個 public(第 5 個是 private `GetOrCreateClient`)。

## Method Audit 表

| # | 方法簽章 | 處理路徑 | 新位置/名稱 | 備註 |
|---|---------|--------|------------|------|
| 1 | `IsUrl(string)` | C(整體類別 rename) | `HttpUtilities.IsUrl(string)` | 第一參數 string 不擴充 |
| 2 | `IsEndpointReachableAsync(string, TimeSpan?)` | C | `HttpUtilities.IsEndpointReachableAsync(...)` | 同上 |
| 3 | `PostAsync(string, string, NameValueCollection?)` | C | `HttpUtilities.PostAsync(...)` | 同上 |
| 4 | `GetAsync(string, NameValueCollection?)` | C | `HttpUtilities.GetAsync(...)` | 同上 |

整個 class 改名為 `Http`,4 個 public method 名稱與簽章**完全不變**;private `GetOrCreateClient` 與 `_clientMap` 也保留不動。`HttpFunc.cs` 改名為 `HttpUtilities.cs`。

### 為何整體用 path C 而非把 `IsUrl` 拆出去

考慮過拆分:
- `IsUrl` → 獨立 `Url.IsHttpUrl(string)` 類
- `IsEndpointReachableAsync` / `Get` / `Post` → `Http` 或 `HttpRequester`

但拆分有兩個問題:
1. **多一個 type 增加學習成本**:`IsUrl` 只有 1 處生產端 caller(`ApiConnectValidator.cs:30`)且語意上是「是否為 HTTP URL」,放在 `Http` 類聚合度更高
2. **沒有實質好處**:獨立 `Url` 類目前只能容納這 1 個方法,符合「不為假設的未來設計」原則,等真有更多 URL 工具方法再拆

### 為何 path C 不選 path B(`string` 擴充方法)

- 4 個 method 第一參數都是 `string`(URL),擴充 `string` 會污染所有字串的 IntelliSense
- 同 `byte[]` 不擴充 `Gzip`、`object` 不擴充 `IsDate` 的原則一致

### 為何選 `HttpUtilities` 作為類名(2026-05-01 修正)

**原計畫**:`Http`(短而對齊 `Gzip` idiom)

**執行時發現**:Roslyn analyzer **CA1724** 觸發 —— 類名與 BCL **namespace** 同名(末段)就會警告;不限於 type vs type 衝突。`Http` 對應 `System.Net.Http` namespace 末段,本專案 `TreatWarningsAsErrors=true` 把警告變成編譯錯誤。

**子計畫原本判斷有誤**:寫到「`System.Net.Http` 內無 type 叫 `Http`,故 `Bee.Base.Http` 不衝突」,但這只看 type vs type,沒考慮 type vs namespace。

**最終選擇**:`HttpUtilities`
- `Utilities` 是 .NET 公認的「靜態工具類」後綴(對齊 `RuntimeHelpers`、`StringExtensions`、`Convert` 等多種 helper 命名風格)
- 避開 `System.Net.Http` namespace 名稱衝突
- 避開 `System.Web.HttpUtility`(單數)的 type 同名 ambiguous 問題
- 雖較冗長(`HttpUtilities.IsUrl(...)` vs `Http.IsUrl(...)`),但 IDE 自動補全會省掉打字成本

## 影響範圍

**全 repo grep `HttpFunc` 結果(扣除 `bin/obj`)**:

| 類型 | 檔案 | 出現次數 |
|------|------|---------|
| 產品(類別定義) | `src/Bee.Base/HttpFunc.cs` | 1 |
| 產品(`IsUrl` caller) | `src/Bee.Api.Client/ApiConnectValidator.cs:30` | 1 |
| 產品(`IsEndpointReachableAsync` caller) | `src/Bee.Api.Client/ApiConnectValidator.cs:124` | 1 |
| 產品(`PostAsync` caller) | `src/Bee.Api.Client/Providers/RemoteApiProvider.cs:50` | 1 |
| 測試 | `tests/Bee.Base.UnitTests/HttpFuncTests.cs` | 8(`IsUrl` x 1、`GetAsync` x 2、`PostAsync` x 1、`IsEndpointReachableAsync` x 4) |
| 文件 | `docs/plans/plan-funcs-to-net-idiomatic.md` | 1 |

合計 3 處生產端 caller + 8 處測試 caller。

## 執行步驟

### 1. Rename `HttpFunc.cs` → `HttpUtilities.cs`

```bash
git mv src/Bee.Base/HttpFunc.cs src/Bee.Base/HttpUtilities.cs
```

內部:
- `public static class HttpFunc` → `public static class HttpUtilities`
- XML doc summary 文字 `"Utility library for HTTP operations."` 保留(僅類名變)
- 其餘內容(private `_clientMap`、`GetOrCreateClient`、4 個 public method)完全不動

### 2. 更新生產端 caller

`src/Bee.Api.Client/ApiConnectValidator.cs`:
- Line 30: `HttpFunc.IsUrl(endpoint)` → `HttpUtilities.IsUrl(endpoint)`
- Line 124: `HttpFunc.IsEndpointReachableAsync(endpoint)` → `HttpUtilities.IsEndpointReachableAsync(endpoint)`

`src/Bee.Api.Client/Providers/RemoteApiProvider.cs`:
- Line 50: `HttpFunc.PostAsync(...)` → `HttpUtilities.PostAsync(...)`

### 3. 改測試檔名與類名

```bash
git mv tests/Bee.Base.UnitTests/HttpFuncTests.cs tests/Bee.Base.UnitTests/HttpUtilitiesTests.cs
```

內部:
- `public class HttpFuncTests` → `public class HttpUtilitiesTests`
- 8 處 `HttpFunc.X` → `HttpUtilities.X`(全檔 sed 替換即可)
- 兩個 nested helper class(`LoopbackHttpServer`、`StallingServer`)完全不動

### 4. 更新主計畫

進度表第 5 列:`📝` → `✅`,完成日填入,方法數 `5` → `4`(public),處理路徑記為 `C`。

## 驗證

```bash
# 確認沒有遺漏的 HttpFunc 引用
grep -rn "HttpFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj

# Build
dotnet build src/Bee.Base/Bee.Base.csproj --configuration Release --no-restore
dotnet build src/Bee.Api.Client/Bee.Api.Client.csproj --configuration Release --no-restore

# Test
./test.sh tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
./test.sh tests/Bee.Api.Client.UnitTests/Bee.Api.Client.UnitTests.csproj
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內的歷史紀錄
- Build 0 warning, 0 error
- 測試:Bee.Base.UnitTests 全綠(`HttpTests` 8 個 cases 通過);`Bee.Api.Client.UnitTests` 全綠

## Commit 訊息草稿

```
refactor(base): rename HttpFunc to Http

Align with .NET BCL static utility naming (Path, Convert, Encoding,
File). Public methods unchanged; namespace Bee.Base preserved.

All four public methods (IsUrl, IsEndpointReachableAsync, PostAsync,
GetAsync) take a URL string as first parameter — extending string is
ruled out (would pollute IntelliSense for all strings), so the whole
class stays together under a noun-form static class. The internal
HttpClient pool (GetOrCreateClient + _clientMap) is preserved.

Tests renamed: HttpFuncTests → HttpTests; private LoopbackHttpServer
and StallingServer helpers unchanged.

Fifth class executed under the *Func → .NET idiomatic refactor (see
docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

新增一條(由本次 CA1724 教訓得出):

- **Path C 命名衝突檢查**:選靜態類名時必須避開**所有** BCL namespace 末段名稱(不只是 type 名稱)。Roslyn analyzer **CA1724** 對 namespace-vs-type 同名也會警告,在 `TreatWarningsAsErrors=true` 下會編譯失敗。常見地雷:`Http`(`System.Net.Http`)、`Json`(`System.Text.Json`)、`Xml`(`System.Xml`)、`Linq`(`System.Linq`)、`Threading`(`System.Threading`)等。短名 `Gzip` 安全是因為 BCL 沒有 `*.Gzip` namespace。**選名前先 grep BCL namespace 清單。**

沿用既有 idiom:

- 沿用 `GzipFunc → Gzip` 的 path C 命名(去 `Func` 後綴 → noun-form 靜態類)
- 不擴充 `string`(過度通用型別,跟 `byte[]` 不擴充 `Gzip`、`object` 不擴充 `IsDate` 一致)
- 不為假設的未來設計(不預先把 `IsUrl` 拆到獨立 `Url` 類,只有 1 個方法不值得多 type)

## 風險與回滾

- 變動範圍:類別名 + 3 處生產端 caller + 測試檔/類名(內部 helper 不動)
- Public API breaking:類別名變更,但無外部 NuGet 消費者
- 若失敗單一 `git revert` 即可回滾
