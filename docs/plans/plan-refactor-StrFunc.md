# 計畫：重構 `StrFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Base/StrFunc.cs`(567 行,**39 個 public method**)

## 設計哲學:框架封裝預設值

**核心原則(由本次重訂定案,2026-05-01)**:

Bee.NET 是 ERP 框架。**ERP 業務情境字串比較預設不區大小寫**,呼叫端不該每次都想 `StringComparison`、`CultureInfo` 等技術細節。框架的價值正是 **集中管理這些 default**:

- 字串比較 / 包含 / 前後綴匹配 → **預設 IgnoreCase**
- 字串格式化 → **預設 InvariantCulture**(避免 locale-dependent decimal/date format 問題)
- 呼叫端只在「需要不同行為」時才傳第二個參數

這意味著大部分方法**不能 inline 至 BCL**(因為 BCL 強制呼叫端傳 `StringComparison`),必須留在框架 helper。

## Method 分類(全 39 個)

### Group A:保留至 `StringUtilities`(16 個,封裝 framework default)

純靜態類別,所有方法封裝 `StringComparison.CurrentCultureIgnoreCase` 或 `CultureInfo.InvariantCulture`,呼叫端不需傳。

| 原方法 | 新位置 | 框架 default 內容 |
|--------|--------|-----------------|
| `IsEmpty(string?, bool isTrim=true)` | `StringUtilities.IsEmpty` | trim + null/whitespace |
| `IsEmpty(object)` | `StringUtilities.IsEmpty(object)` | object→string + null check |
| `IsNotEmpty(string?, bool)` | `StringUtilities.IsNotEmpty` | 同上反向 |
| `IsNotEmpty(object)` | `StringUtilities.IsNotEmpty(object)` | 同上反向 |
| `Format(string, params object[])` | `StringUtilities.Format` | **InvariantCulture** |
| `IsEquals(s1, s2, ...)` | `StringUtilities.IsEquals(s1, s2, bool ignoreCase=true)` | **預設 IgnoreCase** |
| `IsEqualsOr(s, params)` | `StringUtilities.IsEqualsOr` | 多重比對,IgnoreCase |
| `Replace(s, search, repl, ...)` | `StringUtilities.Replace(... ignoreCase=true)` | regex + IgnoreCase + timeout |
| `Split(s, delim)` | `StringUtilities.Split` | null/empty → 空陣列 |
| `Contains(s, sub)` | `StringUtilities.Contains(s, sub, ignoreCase=true)` | **預設 IgnoreCase** |
| `LeftWith(s, value)` | `StringUtilities.StartsWith(s, prefix, ignoreCase=true)` | **預設 IgnoreCase**(改名對齊 BCL)|
| `RightWith(s, value)` | `StringUtilities.EndsWith(s, suffix, ignoreCase=true)` | **預設 IgnoreCase**(改名對齊 BCL)|
| `Pos(s, sub)` | `StringUtilities.IndexOf(s, sub, ignoreCase=true)` | **預設 IgnoreCase**(改名對齊 BCL)|
| `PosRev(s, sub)` | `StringUtilities.LastIndexOf(s, sub, ignoreCase=true)` | **預設 IgnoreCase**(改名對齊 BCL)|
| `Trim(string)` | `StringUtilities.Trim` | 額外剝 ZWSP/BOM |
| `GetNextId(value, base/baseValues)` x 2 | `StringUtilities.GetNextId` | 自訂 base 序號遞增 |

**改名理由**:
- `LeftWith`/`RightWith` → `StartsWith`/`EndsWith`:對齊 BCL 命名,呼叫端讀起來更熟悉(`StringUtilities.StartsWith(s, prefix)` ≈ `s.StartsWith(prefix)` 但不需 StringComparison)
- `Pos`/`PosRev` → `IndexOf`/`LastIndexOf`:同樣對齊 BCL

### Group B:`StringExtensions`(5 個 string 擴充方法,無 BCL 衝突)

| 原方法 | 擴充方法形式 | 框架封裝 |
|--------|-------------|---------|
| `SplitLeft(s, delim, out l, out r)` | `s.SplitLeft(delim, out l, out r)` | out 參數左分割,IgnoreCase 找 delim |
| `SplitRight(s, delim, out l, out r)` | `s.SplitRight(delim, out l, out r)` | 同上,從右邊找 |
| `LeftCut(s, prefix)` | `s.LeftCut(prefix)` | 條件 prefix 移除,IgnoreCase 比對 |
| `RightCut(s, suffix)` | `s.RightCut(suffix)` | 條件 suffix 移除,IgnoreCase 比對 |
| `LeftRightCut(s, prefix, suffix)` | `s.LeftRightCut(prefix, suffix)` | 同時 prefix+suffix |

**為何這 5 個適合擴充**:
- 都無 BCL 同名衝突
- 第一參數 string 後面接「對 string 做什麼」,讀起來自然(`s.LeftCut("x")`)

### Group C:Inline 至 callers(7 個方法,BCL 已夠簡潔且無 default 需封裝)

| 原方法 | BCL 替換 | Caller 量 |
|--------|----------|----------|
| `ToUpper(s)` | `s?.ToUpper() ?? string.Empty` | 4 |
| `ToLower(s)` | `s?.ToLower() ?? string.Empty` | 4 |
| `Length(s)` | `s?.Length ?? 0` | 2 |
| `Substring(s, idx)` | `s.Substring(...)` 加邊界保護 | 1 |
| `Substring(s, idx, len)` | 同上 | 1 |
| `Left(s, n)` | `s.Substring(0, n)` 加邊界保護 | 2 |
| `Append(StringBuilder, s, delim)` | `if(sb.Length>0) sb.Append(delim); sb.Append(s);` | 0 直接(`Merge(sb)` 1 個轉這格式)|

合計約 14 處 caller 需 inline 改寫。這些方法 BCL 都有等價(無 culture/case 需封裝),inline 後可讀性不降。

### Group D:刪除(6 個,0 caller + 無框架價值)

| 方法 | 原因 |
|------|------|
| `SplitNewLine(s)` | 0 caller |
| `Like(s, pattern, options)` | 0 caller,VB-style wildcard |
| `Dup(int, char)` | 0 caller,只內部給 `PadLeft` 用 |
| `PadLeft(s, len, ch)` | 0 caller(`Dup` 也刪)|
| `Right(s, len)` | 0 caller |
| `Merge(s1, s2, delim)` (string overload) | 0 caller |

## 影響範圍

- **Caller**:284 處 prod + 大量測試
- **大部分機械式替換**:`StrFunc.X` → `StringUtilities.X`(16 個方法)
- **少量擴充式改寫**:`StrFunc.SplitLeft(s, ...)` → `s.SplitLeft(...)`(5 個方法)
- **少量 inline**:約 14 處 caller(7 個方法)

## CA1724 檢查

- `StringUtilities` 不對應任何 BCL namespace,安全
- `StringExtensions` 不對應任何 BCL namespace,安全

## 執行步驟

### 1. 新建 `src/Bee.Base/StringUtilities.cs`

包含 Group A 16 個方法。所有比較類方法(`IsEquals`/`Contains`/`StartsWith`/`EndsWith`/`IndexOf`/`LastIndexOf`/`Replace`)以 `bool ignoreCase = true` 為預設參數。

### 2. 新建 `src/Bee.Base/StringExtensions.cs`

包含 Group B 5 個 `this string` 擴充方法。

### 3. 機械式 rename(Group A)

```bash
perl -i -pe 's/StrFunc\.IsEmpty\(/StringUtilities.IsEmpty(/g; s/StrFunc\.IsNotEmpty\(/StringUtilities.IsNotEmpty(/g; s/StrFunc\.Format\(/StringUtilities.Format(/g; s/StrFunc\.IsEquals\(/StringUtilities.IsEquals(/g; s/StrFunc\.IsEqualsOr\(/StringUtilities.IsEqualsOr(/g; s/StrFunc\.Replace\(/StringUtilities.Replace(/g; s/StrFunc\.Split\(/StringUtilities.Split(/g; s/StrFunc\.Contains\(/StringUtilities.Contains(/g; s/StrFunc\.LeftWith\(/StringUtilities.StartsWith(/g; s/StrFunc\.RightWith\(/StringUtilities.EndsWith(/g; s/StrFunc\.Pos\(/StringUtilities.IndexOf(/g; s/StrFunc\.PosRev\(/StringUtilities.LastIndexOf(/g; s/StrFunc\.Trim\(/StringUtilities.Trim(/g; s/StrFunc\.GetNextId\(/StringUtilities.GetNextId(/g'
```

### 4. 擴充方法轉換(Group B)

```bash
perl -i -pe 's/StrFunc\.SplitLeft\(([^,()]+),\s*/$1.SplitLeft(/g; s/StrFunc\.SplitRight\(([^,()]+),\s*/$1.SplitRight(/g; s/StrFunc\.LeftCut\(([^,()]+),\s*/$1.LeftCut(/g; s/StrFunc\.RightCut\(([^,()]+),\s*/$1.RightCut(/g; s/StrFunc\.LeftRightCut\(([^,()]+),\s*/$1.LeftRightCut(/g'
```

### 5. Group C inline(手動 14 處)

逐一處理 BaseFunc / SqlTableSchemaProvider 等檔案,把 `StrFunc.ToUpper` 等改 BCL。

### 6. 刪除 `StrFunc.cs`

```bash
git rm src/Bee.Base/StrFunc.cs
```

### 7. 拆解測試

`StrFuncTests.cs` → `StringUtilitiesTests.cs`,刪除 Group D 6 個方法的測試,Group B/C 對應更新。

### 8. 更新主計畫

進度表第 11 列 ✅,方法數 39,處理路徑 `A+B+C+D` 全用上。

## 驗證

```bash
grep -rn "StrFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj
# 應只剩 docs/plans/ 內歷史紀錄
dotnet build  # 全 src project 0 警告 0 錯誤
./test.sh tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
```

## Commit 訊息草稿

```
refactor(base): split StrFunc — encapsulate framework defaults in StringUtilities + StringExtensions

StrFunc had 39 methods. The refactor preserves the framework's
"caller doesn't think about culture / case" philosophy: most methods
move to StringUtilities (16 methods) keeping their IgnoreCase /
InvariantCulture defaults so the call site stays concise. Five
prefix / suffix / split helpers move to StringExtensions as `this
string` extension methods (no BCL name conflict). Seven trivial BCL
wrappers (ToUpper / ToLower / Length / Substring / Left, plus the
Append StringBuilder helper) inline at the ~14 callers. Six methods
with zero callers are deleted (SplitNewLine / Like / Dup / PadLeft /
Right / Merge string overload).

A few methods are also renamed at the same time to align with BCL
vocabulary: LeftWith → StartsWith, RightWith → EndsWith,
Pos → IndexOf, PosRev → LastIndexOf. Callers still bind to the
framework helper (case-insensitive default), but the names match
what .NET developers already know.

Why not push more methods to BCL inline? Because the framework's
job is to encapsulate ERP defaults — string comparison should default
to case-insensitive, format strings should default to
InvariantCulture, and callers should not have to remember those at
every site. Inlining IsEquals / IsEmpty / Format / Contains etc. to
BCL would force ~140 call sites to add StringComparison /
CultureInfo arguments, which defeats the point of having a framework
helper.

Eleventh class executed under the *Func to .NET idiomatic refactor
(see docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

新增一條 idiom(由本次 StrFunc 大規模重構定案):

- **框架 helper 封裝 default 的原則**:當 framework 有「ERP 業務情境的明確 default」(case-insensitive 比較、InvariantCulture 格式化、null/empty 統一處理),應**封裝在 helper 內**,而非要求呼叫端每次傳。判斷準則:
  - BCL 提供的方法是否需要 culture / comparison 參數?
  - Framework 對該參數有明確 default(如「比較永遠 IgnoreCase」)?
  - 兩者皆是 → 保留為 helper(`StringUtilities.IsEquals(s1, s2)` 不需第三參數)
  - 否則才考慮 inline 至 BCL
- **BCL idiomatic 命名同時對齊**:helper 改名為 BCL 慣用詞(`StartsWith`/`EndsWith`/`IndexOf`)即使行為不同(default IgnoreCase),讓 .NET 老手讀起來熟悉

## 風險與回滾

- 變動範圍極大(284+ caller),但**大部分機械式 sed 替換**(語意保留)
- 真正手動 inline 約 14 處,可控
- Public API breaking:類名 + 6 個方法刪除 + 4 個改名,無外部 NuGet 消費者
- 失敗單一 `git revert` 即可回滾
