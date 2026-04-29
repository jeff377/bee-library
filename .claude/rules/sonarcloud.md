# SonarCloud 規則指引

本規範收錄 SonarCloud 掃描過本專案後常出現的規則，依主題分類。撰寫新程式碼時主動遵守，避免重複觸發 code smell。

> 安全相關 SAST 規則（SQL 注入、XXE、路徑安全、資源釋放、例外處理基本原則）見 `scanning.md`。
> 一般命名與格式見 `code-style.md`。
>
> 以下規則已由 `.editorconfig` 硬性化（build time 即失敗），不再列於本文件：
> - **S1118 / S3442** → CA1052（static holder type）
> - **S2325** → CA1822（方法應為 static）
> - **S2933** → IDE0044（readonly 欄位）
> - **S4487** → IDE0051 / IDE0052（未使用 private 成員）
> - **S927** → CA1725（override 參數名一致）
> - **S6580** → CA1305（`IFormatProvider` 文化相依 API）

---

## 1. 類別與型別設計

| 規則 | 原則 |
|------|------|
| **S3925** | 名稱含 `Exception` 的類別必須繼承 `System.Exception`（或相容基底） |
| **S2094** | 不應存在空 class；移除或改為 interface |
| **S3260** | 不被繼承的 `private` nested class 應標為 `sealed` |
| **S2344** | `enum` 不應明確指定 `int` 作為 underlying type（預設即為 int） |
| **S2342** | 表達「集合／旗標」語意的 enum 名稱末尾應加 `s`（如 `TraceLayers`） |
| **S101** | 類別名採 Pascal case；連續大寫縮寫僅首字大寫（`Utf8StringWriter` 而非 `UTF8StringWriter`） |

## 2. 介面與 override 一致性

| 規則 | 原則 |
|------|------|
| **S1006** | override／實作方法必須保留與基底／介面相同的 default 參數值 |
| **S4144** | 多個方法實作完全相同時應合併，或改以一個呼叫另一個 |

## 3. 控制流與語法

| 規則 | 原則 |
|------|------|
| **S1066** | 可合併的巢狀 `if` 應合併為單一 `if` 搭配 `&&` |
| **S127** | `for` loop 不應在 body 中修改停止條件變數（如 `i`） |
| **S4023** | 使用 pattern matching 取代 `is`+cast 的舊寫法 |
| **S1116** | 移除空 statement（多餘的 `;`） |

```csharp
// ✅ S4023: pattern matching
if (obj is MyType t) { t.DoWork(); }

// ❌ S4023: type-check + cast
if (obj is MyType) { ((MyType)obj).DoWork(); }
```

## 4. 欄位與初始化

| 規則 | 原則 |
|------|------|
| **S3604** | 欄位若於建構子中被明確賦值，不應再有 inline initializer（如 `= null`、`= string.Empty`） |
| **S3963** | 可 inline 初始化的靜態欄位，不應放入 static constructor |
| **S3877** | static constructor 不應 throw（異常會導致整個 type 不可用） |
| **S2743** | generic type 中的 `static` field 不會跨 close constructed types 共享；需確認是否為有意行為 |

## 5. DateTime

| 規則 | 原則 |
|------|------|
| **S6562** | 建立 `DateTime` 時須明確指定 `DateTimeKind`（`Utc`／`Local`／`Unspecified`） |

```csharp
// ✅
var dt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

// ❌
var dt = new DateTime(2026, 1, 1, 0, 0, 0);
```

## 6. 集合與 LINQ

| 規則 | 原則 |
|------|------|
| **S3267** | 可用 `.Where()` 取代 `foreach` + `if` 的過濾模式 |

```csharp
// ✅
foreach (var x in list.Where(x => x.IsActive)) { ... }

// ❌
foreach (var x in list) { if (x.IsActive) { ... } }
```

## 7. 字串與陣列

| 規則 | 原則 |
|------|------|
| **S3878** | `params` 參數呼叫時不需明確建立 array；直接傳入元素即可 |

```csharp
// ✅
string.Join(", ", "a", "b", "c");

// ❌
string.Join(", ", new[] { "a", "b", "c" });
```

## 8. 例外處理

| 規則 | 原則 |
|------|------|
| **S112** | 不應 throw `System.ApplicationException`；改用自訂例外或 `InvalidOperationException` |

> 其他例外規則（`catch (Exception)`、空 catch、`throw ex;`）見 `scanning.md`。

## 9. 死碼與已廢棄程式碼

| 規則 | 原則 |
|------|------|
| **S125** | 移除被註解掉的程式碼；需保留歷史就用 git log（正向寫法見 `code-style.md` §註解規範） |
| **S1133** | 標記為 `[Obsolete]` 且確定無呼叫者的程式碼應移除 |

## 10. Reflection 與 Assembly

| 規則 | 原則 |
|------|------|
| **S3885** | 優先使用 `Assembly.Load`（依 `AssemblyName`）而非 `Assembly.LoadFrom`（依路徑），後者會導致 load context 不一致 |

## 11. 測試

| 規則 | 原則 |
|------|------|
| **S2701** | `Assert.True`／`Assert.False` 的第一參數不應為字面值（如 `Assert.True(true)`），應為被測表達式 |

## 12. Regex（ReDoS 防護）

| 規則 | 原則 |
|------|------|
| **S6444** | `Regex.IsMatch`／`Regex.Replace`／`new Regex(...)` 一律傳入 `TimeSpan.FromSeconds(1)` timeout，即使 pattern 為編譯期常數或已用 `Regex.Escape()` 轉義 |

```csharp
// ✅ 正確：明確指定 timeout
Regex.IsMatch(input, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
Regex.Replace(s, Regex.Escape(search), replacement, options, TimeSpan.FromSeconds(1));
new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));

// ❌ 禁止：未傳 timeout
Regex.IsMatch(input, pattern);
new Regex(pattern, RegexOptions.Compiled);
```

## 13. GitHub Actions 工作流

| 規則 | 原則 |
|------|------|
| **S7636** | secrets 不直接在 `run:` block 展開；改以 step-level `env:` 注入後引用環境變數，避免遭 log 洩漏 |
| **S7637** | 第三方 actions 一律 pin 至完整 commit SHA；版本 tag 以行尾註解保留可讀性 |

```yaml
# ✅ 正確：secrets 用 env:，action pin commit SHA
- name: Push to NuGet
  env:
    NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
  run: dotnet nuget push ... --api-key "$env:NUGET_API_KEY"

- uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5  # v4.3.1

# ❌ 禁止：secrets 直接在 run 展開、action 只 pin 主版本
- name: Push to NuGet
  run: dotnet nuget push ... --api-key ${{ secrets.NUGET_API_KEY }}

- uses: actions/checkout@v4
```

取得 SHA 的方法：`gh api repos/<org>/<name>/git/ref/tags/<tag> --jq .object.sha`

## 14. 序列化

| 規則 | 原則 |
|------|------|
| **S5766** | `[Serializable]` marker 類別之建構子若**未**實作 `(SerializationInfo, StreamingContext)`、屬性皆為原始型別、不經 `BinaryFormatter` 反序列化，則非實際反序列化入口，於 Sonar UI 標記 **Safe** 並說明理由即可，不必改程式 |

---

## 不納入之規則

- **Cognitive Complexity（S3776）** — 屬重構判斷題，需依情境評估；不納入為硬性規則。
- **CA 系列（Roslyn Analyzer）** — 由編譯器警告把關（本專案 `TreatWarningsAsErrors=true`），不重複列入。
- **一次性專案特定修正** — 如某欄位改名、某類別拆分，非通用原則。
