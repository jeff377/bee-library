# 計畫：重構 `DateTimeFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Base/DateTimeFunc.cs`(50 行,**4 個 public 方法**)

```csharp
namespace Bee.Base;

public static class DateTimeFunc
{
    public static bool IsEmpty(DateTime dateValue);   // < 1753-01-01
    public static bool IsDate(object value);          // DateTime 或可剖析字串
    public static string Format(DateTime, string);    // ToString(format, InvariantCulture) 包裝
    public static DateTime GetYearMonth(DateTime);    // 月初日期
}
```

> 主計畫進度表寫 5 個方法,實際 audit 後 4 個。又是清點誤計。

## Method Audit 表

| # | 方法簽章 | Prod caller | 處理路徑 | 新位置/名稱 | 備註 |
|---|---------|------------|--------|------------|------|
| 1 | `IsEmpty(DateTime)` | 0 | **B** | `DateTimeExtensions.IsEmpty(this DateTime)` | 框架自有「SQL min date」語意,值得保留為擴充 |
| 2 | `IsDate(object)` | 1(`BaseFunc.CDateTime`) | **A 刪除 + inline** | `BaseFunc.CDateTime` 內 inline 用 BCL `is` + `DateTime.TryParse` | 單一 caller,helper 加值僅是 dispatch on object;直接 BCL 化更清楚 |
| 3 | `Format(DateTime, string)` | 0 | **A 刪除** | — | 純 `dateValue.ToString(format, InvariantCulture)` 包裝,無加值 |
| 4 | `GetYearMonth(DateTime)` | 0 | **B** | `DateTimeExtensions.GetYearMonth(this DateTime)` | 框架自有「月初」helper,屬於可預期的 framework API |

4 個方法處理完後,`DateTimeFunc.cs` 整個刪除。

### 1. `IsEmpty` — path B 細節

**現行**:
```csharp
DateTimeFunc.IsEmpty(d)  // d < 1753-01-01
```

**轉擴充方法後**:
```csharp
d.IsEmpty()
```

**保留 `IsEmpty()` 名稱理由**:
- 雖然 `IsEmpty` 對 `DateTime` 字面上有點誤導(DateTime 並無「空」概念),但「`< 1753-01-01` 視為空值」是本框架(對應 SQL Server 的 `datetime` 下限)既有約定,改名會擴大影響面
- 替代名稱 `IsBeforeSqlMin()` 更精確但較囉嗦,且暴露「為什麼是 1753」的實作細節到 API 名稱
- 沿用 `IsEmpty()` 對既存呼叫(雖然只有測試)語意一致

### 2. `IsDate` — path A 刪除 + inline 細節

**現行 `BaseFunc.CDateTime`**:
```csharp
if (DateTimeFunc.IsDate(value)) { return Convert.ToDateTime(value, CultureInfo.InvariantCulture); }
```

**Inline 後**:
```csharp
if (value is DateTime dt) { return dt; }
if (DateTime.TryParse(BaseFunc.CStr(value), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) { return parsed; }
```

**為何走 path A 而非 B**:
- `IsDate(object)` 第一參數是 `object`,擴充 `object` 會污染所有 IntelliSense
- 拆成 `string.IsValidDate()` 雖可行,但 caller 仍需處理 `DateTime` 分支,沒比直接 BCL 簡潔
- 直接用 `is DateTime` + `DateTime.TryParse` 是 .NET idiomatic 寫法,讀者一看就懂,不需查 helper

**測試覆蓋**:`BaseFunc.CDateTime` 已有 `CDateTime_VariousInputs_ReturnsExpectedResult` 測試,涵蓋 `DateTime` 實例、可剖析字串(`"2015-03-12"`、`"20150312"`、ROC `"1040312"`)、null、DBNull、空字串等,inline 後仍受保護。`DateTimeFuncTests` 中 2 個 `IsDate_*` 測試直接刪除(`IsDate(null)` / `IsDate("abc 2026")` 等 false 情境本來就由 `CDateTime` 走 fallback 路徑覆蓋)。

### 3. `Format(DateTime, string)` — path A 刪除細節

**現行**:
```csharp
public static string Format(DateTime dateValue, string format)
    => dateValue.ToString(format, CultureInfo.InvariantCulture);
```

**為何刪除**:
- 0 prod caller(只有測試自己呼叫)
- 純 BCL 包裝,無加值
- 「強制 InvariantCulture」這個約定如果重要,應該由呼叫端在格式化日期時自行明示(`.ToString(format, CultureInfo.InvariantCulture)`),而非藏在 helper 裡;隱式預設反而會讓不熟此 helper 的人忘記文化相關 bug

`DateTimeFuncTests` 中 1 個 `Format_*` 測試直接刪除。

### 4. `GetYearMonth(DateTime)` — path B 細節

**現行**:
```csharp
DateTimeFunc.GetYearMonth(d)  // 回傳 new DateTime(d.Year, d.Month, 1, 0, 0, 0, Unspecified)
```

**轉擴充方法後**:
```csharp
d.GetYearMonth()
```

**保留 `GetYearMonth()` 名稱理由**:
- 沿用既有命名,不重新發明
- 替代名稱 `ToMonthStart()` 雖然更貼近 BCL 風格(`To*`),但「YearMonth」是本框架已建立的詞彙(可能在月份報表、會計期等情境出現)
- 雖然 0 prod caller,但屬於明確、有意義的 framework API(月初截斷),保留作為擴充方法

## 影響範圍

**全 repo grep `DateTimeFunc` 結果(扣除 `bin/obj`)**:

| 類型 | 檔案 | 出現次數 |
|------|------|---------|
| 產品(類別定義) | `src/Bee.Base/DateTimeFunc.cs` | 1 |
| 產品(`IsDate` caller) | `src/Bee.Base/BaseFunc.cs:369` | 1 |
| 測試 | `tests/Bee.Base.UnitTests/DateTimeFuncTests.cs` | 8(`IsEmpty` x 3、`IsDate` x 4、`Format` x 2、`GetYearMonth` x 1) |
| 文件 | `docs/plans/plan-funcs-to-net-idiomatic.md` | 1 |

合計 1 處生產端 caller(將被 inline 消除)+ 8 處測試 caller。

## 執行步驟

### 1. 新增 `DateTimeExtensions.cs`

`src/Bee.Base/DateTimeExtensions.cs`(新檔):

```csharp
namespace Bee.Base
{
    /// <summary>
    /// Extension methods for <see cref="DateTime"/>.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Determines whether the specified date/time value is empty (before SQL Server's minimum date 1753-01-01).
        /// </summary>
        /// <param name="dateValue">The date/time value to check.</param>
        public static bool IsEmpty(this DateTime dateValue)
        {
            // SQL Server's datetime minimum is 1753-01-01; values earlier are treated as empty
            return dateValue < new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Gets the first day of the year and month of the specified date.
        /// </summary>
        /// <param name="dateValue">The date value.</param>
        public static DateTime GetYearMonth(this DateTime dateValue)
        {
            return new DateTime(dateValue.Year, dateValue.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        }
    }
}
```

### 2. Inline `IsDate` 到 `BaseFunc.CDateTime`

`src/Bee.Base/BaseFunc.cs`:把 line 369 的 `DateTimeFunc.IsDate(value)` 路徑改寫為 BCL 版本(見 §2 細節)。

`using System.Globalization;` 已存在,`DateTime.TryParse` 用得上。

### 3. 刪除 `DateTimeFunc.cs`

4 個方法都搬完/刪除後,整個檔案 `git rm`。

### 4. 拆解測試

`tests/Bee.Base.UnitTests/DateTimeFuncTests.cs` 拆成兩處:

#### 4a. `IsEmpty` / `GetYearMonth` 測試 → 新檔 `DateTimeExtensionsTests.cs`

- 建新檔 `tests/Bee.Base.UnitTests/DateTimeExtensionsTests.cs`
- 搬 3 個 `IsEmpty_*` test methods(共 4 個 InlineData)+ 1 個 `GetYearMonth_*` test
- 改名:`IsEmpty_*` / `GetYearMonth_*` 命名保留,呼叫改 `date.IsEmpty()` / `input.GetYearMonth()`

#### 4b. `IsDate_*` / `Format_*` 測試 → 直接刪除

- 2 個 `IsDate_*` tests(`IsDate_AcceptsDateTimeAndParseableString`、`IsDate_InvalidString_ReturnsFalse`):刪除。Happy path 由 `BaseFuncTests.CDateTime_VariousInputs_ReturnsExpectedResult` 涵蓋;false 情境由 `CDateTime` 的 fallback `try { ... } catch { return defaultValue; }` 路徑邏輯覆蓋。
- 1 個 `Format_*` test:隨方法刪除而刪除。

#### 4c. 刪除原 `DateTimeFuncTests.cs`

### 5. 更新主計畫

進度表第 4 列:`📝` → `✅`,完成日填入,方法數 `5` → `4`。

## 驗證

```bash
# 確認沒有遺漏的 DateTimeFunc 引用
grep -rn "DateTimeFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj

# Build
dotnet build src/Bee.Base/Bee.Base.csproj --configuration Release --no-restore

# Test
./test.sh tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內的歷史紀錄
- Build 0 warning, 0 error
- 測試:Bee.Base.UnitTests **少 3 個 test method**(`IsDate_*` x 2 + `Format_*` x 1),其餘全綠;`CDateTime_VariousInputs_ReturnsExpectedResult` 仍通過

## Commit 訊息草稿

```
refactor(base): split DateTimeFunc — extensions on DateTime, inline IsDate, drop Format

IsEmpty and GetYearMonth become DateTimeExtensions methods (path B —
extensions on DateTime, callers read d.IsEmpty() / d.GetYearMonth()).

IsDate is deleted; its single production caller (BaseFunc.CDateTime)
inlines `is DateTime` + DateTime.TryParse — the BCL idiom is no
longer worth wrapping (path A). The two IsDate tests are removed
since BaseFuncTests.CDateTime_VariousInputs already covers the
behavior end-to-end.

Format is deleted (path A — pure ToString(format, InvariantCulture)
wrapper with zero production callers; callers can use BCL directly
and explicit culture is clearer than hidden default).

DateTimeFunc.cs is removed entirely. Tests split:
IsEmpty/GetYearMonth move to DateTimeExtensionsTests; IsDate/Format
tests are deleted.

Fourth class executed under the *Func → .NET idiomatic refactor (see
docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

本次定下的 idiom,後續沿用:

- **Path A 「刪除 + inline」**:當 `*Func` 方法只有 1-2 個 prod callers 且 body 是 BCL 1-line 包裝,直接 inline 到 caller,刪除原 helper —— 比保留薄殼更清楚
- **`object` 不擴充**:第一參數為 `object` 的方法不轉擴充方法(會污染所有型別 IntelliSense),改走 path A inline 或 path C 的 noun-form static utility
- **預設值/隱式約定的判斷**:當 helper 唯一加值是「強制某個預設」(本例 `InvariantCulture`),如果該預設應該被呼叫端意識到,不該藏在 helper 裡;直接用 BCL 並把預設明示給呼叫端,而非透過 helper 隱含預設

## 風險與回滾

- 變動範圍:1 處生產端 caller(`BaseFunc.CDateTime`)+ 測試重組 + 2 個 helper 刪除
- Public API breaking:`IsDate` 與 `Format` 直接刪除,無外部 NuGet 消費者
- 若失敗單一 `git revert` 即可回滾
