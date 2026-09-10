# SonarCloud 規則指引

SonarCloud 掃過本專案後常出現的規則。撰寫新程式碼時主動遵守。

> 安全 SAST 規則（SQL 注入、XXE、路徑安全、資源釋放、例外處理基本原則）見 `scanning.md`；
> 命名與格式見 `code-style.md`。
>
> **已由 `.editorconfig` 硬性化者不列入**（build 即失敗）：S1118／S3442→CA1052、
> S2325→CA1822、S2933→IDE0044、S4487→IDE0051/0052、S927→CA1725、S6580→CA1305。
>
> **不納入**：CA 系列（編譯器已把關）。

## 規則一覽

| 規則 | 原則 |
|------|------|
| **S3925** | 名稱含 `Exception` 的類別必須繼承 `System.Exception` |
| **S2094** | 不應存在空 class；移除或改為 interface |
| **S3260** | 不被繼承的 `private` nested class 應標 `sealed` |
| **S2344** | `enum` 不應明確指定 `int` 為 underlying type（預設即是） |
| **S2342** | 集合／旗標語意的 enum 名稱末尾加 `s`（如 `TraceLayers`） |
| **S101** | 類別名 Pascal case；連續大寫縮寫僅首字大寫（`Utf8StringWriter` 非 `UTF8StringWriter`） |
| **S1006** | override／實作方法必須保留與基底相同的 default 參數值 |
| **S4144** | 多個方法實作完全相同時應合併，或一個呼叫另一個 |
| **S1066** | 可合併的巢狀 `if` 合併為單一 `if` + `&&` |
| **S127** | `for` loop 不應在 body 內修改停止條件變數 |
| **S4023** | 用 pattern matching（`is MyType t`）取代 `is` + cast |
| **S1116** | 移除多餘的空 statement（`;`） |
| **S3604** | 建構子已明確賦值的欄位，不應再有 inline initializer |
| **S3963** | 可 inline 初始化的靜態欄位，不放 static constructor |
| **S3877** | static constructor 不應 throw（會讓整個 type 不可用） |
| **S2743** | generic type 的 `static` field 不跨 closed constructed types 共享，需確認是否有意 |
| **S6562** | `new DateTime(...)` 須明確指定 `DateTimeKind` |
| **S3267** | 用 `.Where()` 取代 `foreach` + `if` 的過濾 |
| **S3878** | `params` 呼叫端不需明確建 array，直接傳元素 |
| **S112** | 不 throw `ApplicationException`；用自訂例外或 `InvalidOperationException` |
| **S1133** | `[Obsolete]` 且確定無呼叫者的程式碼應移除 |
| **S3885** | 用 `Assembly.Load`（依 `AssemblyName`）而非 `Assembly.LoadFrom`（依路徑，load context 會不一致） |
| **S2701** | `Assert.True`／`Assert.False` 第一參數不應為字面值 |
| **S3776** | 認知複雜度上限 15；超標多半是一個方法做了兩件事，見下 |

## S3776 — 認知複雜度（2026-09-10 起納入）

門檻 15。計分重點是**巢狀加成**：迴圈裡的一個 `if` 算 2 分、再內一層算 3 分，
而扁平的 `switch` 不論幾個 case 都只算 1。所以分數高幾乎一律指向「巢狀太深」，
不是「分支太多」。

**這條之前列在「不納入」，2026-09-10 移除該豁免。** 當時的理由是「屬情境判斷」——
那句話是對的，但它被當成了不看的藉口。實際把四個超標方法逐一處理後，**四個都有真實的接縫**：

| 方法 | 分數 | 真正的問題 |
|------|------|-----------|
| `WireValueJsonConverter.ResolveCode` | 22 | 22 個連續 `if (type == typeof(X)) return`，本來就是一張查表 |
| `FormBusinessObject.EnforceWriteScope` | 20 | 一個迴圈同時做「蒐集主檔 rowid」與「檢查越範圍」，兩者對「哪些列算數」的看法不同 |
| `FormBusinessObject.EnforceDetailOwnership` | 18 | 三層 `foreach`，中間一層是完整的「檢查一張明細表」 |
| `BeeFrameworkServiceCollectionExtensions.AddBeeFramework` | 18 | 317 行的方法裡夾著一段自成一格的稽核寫入器註冊 |

**判讀方式**：把它當成「這個方法是不是在做兩件事」的探針，不是當成要壓低的數字。
問得出第二件事叫什麼名字就抽出來；問不出來（例如一連串各自獨立、無法合併的業務規則）
就到 SonarCloud UI 標 *Won't Fix* 並寫明理由，**不要為了壓分數而硬拆出沒有名字的方法**——
那只是把分數搬走，讀的人還是得跳過去。

**本機重現**（CI 只在完整模式跑 Sonar）：S3776 在 NuGet 版 analyzer 預設未啟用，
要暫時加 `PackageReference` 並在 `.editorconfig` 明確開啟，細節見
`docs/repo-ops/gotchas/test-ci-release.md`。

## S6444 — Regex 一律傳 timeout（ReDoS 防護）

`Regex.IsMatch`／`Regex.Replace`／`new Regex(...)` **一律**傳 `TimeSpan.FromSeconds(1)`，
即使 pattern 是編譯期常數或已 `Regex.Escape()`。

```csharp
Regex.IsMatch(input, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
```

## S7636 / S7637 — GitHub Actions

secrets 不在 `run:` 直接展開（會進 log），改以 step-level `env:` 注入後引用；
第三方 actions pin 完整 commit SHA，版本 tag 以行尾註解保留可讀性。

```yaml
- name: Push to NuGet
  env:
    NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
  run: dotnet nuget push ... --api-key "$env:NUGET_API_KEY"

- uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5  # v4.3.1
```

取 SHA：`gh api repos/<org>/<name>/git/ref/tags/<tag> --jq .object.sha`

## S125 — 死碼判定（誤報率高，不可無腦刪）

真的是被註解掉的程式碼才移除；需保留歷史用 `git log`。
**此規則對英文 WHY 註解誤判率高** —— 啟發式 parser 一遇英文識別字 + 行尾 `;` 就 hit。

處理流程：`/sonar-fix` 已把 S125 排除在自動修正外（一律進 `humanReview`）→ 人工判讀該位置 →
合法 WHY 註解就在 SonarCloud UI 標 *False Positive*，真為死碼才手動刪。
**不為了通過掃描而刪註解。** 正向寫法（完整英文句子、`.` 結尾、避免行尾 `;`）見 `code-style.md`。
