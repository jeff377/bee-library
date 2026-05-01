# 計畫：`*Func` 系列全面 .NET 慣例化重構（主計畫）

**狀態：🚧 進行中**

## 動機

Bee.NET 定位為要推廣的 ERP 開源框架,對外採用率與第一印象很重要。
現行 12 個 `*Func` 靜態工具類別是內部慣例,但不符合 .NET 主流命名,
對新使用者形成 onboarding 阻力:

- 與 BCL `System.Func<T>` delegate 撞名,容易引起誤解
- 沒有 `*Extensions` 帶來的 IDE 提示親和性(`s.IsEmpty()` 跳出來 vs
  `StrFunc.IsEmpty(s)` 找不到)
- 部分功能 BCL 已內建(.NET 8/9/10 後生態更豐富),wrapper 顯得冗餘

趁全面採用 .NET 10 的時機,把工具類別對齊 .NET idiomatic 命名。

## 範圍

12 個 `*Func` 類別 + 跨數百個 caller。

| 類別 | 行數 | 方法 | 專案 |
|------|------|-----|------|
| `BaseFunc` | 770 | 46 | Bee.Base |
| `StrFunc` | 567 | 40 | Bee.Base |
| `FileFunc` | 257 | 22 | Bee.Base |
| `SerializeFunc` | 245 | 10 | Bee.Base/Serialization |
| `DbFunc` | 148 | 7 | Bee.Db |
| `HttpFunc` | 147 | 5 | Bee.Base |
| `CacheFunc` | 130 | 14 | Bee.ObjectCaching |
| `DefineFunc` | 130 | 3 | Bee.Definition |
| `DataSetFunc` | 111 | 8 | Bee.Base/Data |
| `BusinessFunc` | 74 | 3 | Bee.Business |
| `GzipFunc` | 62 | 3 | Bee.Base/Serialization |
| `DateTimeFunc` | 49 | 5 | Bee.Base |

## 重構原則:4 種處理路徑

對每個方法逐一判斷處理路徑:

| 路徑 | 條件 | 範例 |
|------|------|------|
| **A. 直接改用 BCL** | 已有等價功能 | `BaseFunc.NewGuid()` → `Guid.NewGuid()` |
| **B. 轉擴充方法** | 屬於某 BCL 型別的擴增 | `DateTimeFunc.IsDate(d)` → `d.IsValidDate()` |
| **C. 純靜態工具(改名)** | 與型別無關,純功能集合 | `GzipFunc` → `GzipExtensions` 或 `Compression.Gzip` |
| **D. 移到領域類別** | 該方法屬於某物件職責 | `BusinessFunc.GetDatabaseItem` → `BackendInfo.GetDatabaseItem` |

### 判斷準則

#### A 優先(BCL 替代)
- .NET 10 BCL 已有 100% 等價功能 → 直接刪
- 範例:`NewGuid` → `Guid.NewGuid()`、`EnsureNotNullOrWhiteSpace` →
  `ArgumentException.ThrowIfNullOrWhiteSpace`(.NET 8+)、
  `RndInt` → `Random.Shared.Next()`

#### B(擴充方法)── 主力路徑
- 第一參數是 BCL 型別(`string`、`DateTime`、`DataTable` 等)
- 加 `this` 修飾讓 IDE 提示自然
- 命名對應 BCL 風格,動詞優先

#### C(改名)
- 跟某 BCL 型別不直接相關,但是純功能集合
- 命名:`*Extensions`(若仍有 `this` 擴充)、`*Utilities`、或純功能名

#### D(領域整合)
- 方法本質屬於某 domain object(BO、Repository、Manager 等)
- 搬到該物件的 instance 方法或對應 static method

## 命名空間策略

擴充方法放在跟原型別相同的命名空間下,例如:
- `string` 擴充 → `Bee.Base`(目前 `StrFunc` 所在)
- `DateTime` 擴充 → `Bee.Base`
- `DataTable`/`DataSet` 擴充 → `Bee.Base.Data`(對齊 BCL `System.Data`)

**不另立** `Bee.Extensions` 命名空間 —— 過度切割反而提高 `using` 負擔,
讓使用者要記哪個型別在哪個命名空間。

## 執行策略 P3:類別 by 類別

從小到大、從簡單到複雜,每次處理一個類別,跑完整流程:

1. 建立子 plan(audit 該類別所有方法)
2. 為每個方法決定 A/B/C/D 處理路徑
3. 確認子 plan → 重構執行
4. Build + test 驗證
5. Commit + push + CI 監測
6. 子 plan 標記完成 → 封存
7. 主 plan 進度表更新

### 執行順序

從小到大的理由:
- 簡單類別練手,找出 sed 替換 idiom、確認 build/test 流程
- 後面類別會依賴前面的決策(尤其是擴充方法的命名 idiom 一致性)
- `BaseFunc` 最後處理,因其他類別可能吸收 `BaseFunc` 部分方法

| # | 類別 | 方法數 | 主 path | 狀態 | 完成日 |
|---|------|-------|--------|------|-------|
| 1 | `GzipFunc` | 2 | C | ✅ | 2026-05-01 |
| 2 | `BusinessFunc` | 2 | B+D | ✅ | 2026-05-01 |
| 3 | `DefineFunc` | 5 | B+C+D | ✅ | 2026-05-01 |
| 4 | `DateTimeFunc` | 4 | A+B | ✅ | 2026-05-01 |
| 5 | `HttpFunc` | 4 | C | ✅ | 2026-05-01 |
| 6 | `DbFunc` | 7 | B/D | 📝 | — |
| 7 | `DataSetFunc` | 8 | B | 📝 | — |
| 8 | `SerializeFunc` | 10 | C | 📝 | — |
| 9 | `CacheFunc` | 14 | B/D | 📝 | — |
| 10 | `FileFunc` | 22 | A/B | 📝 | — |
| 11 | `StrFunc` | 40 | A/B | 📝 | — |
| 12 | `BaseFunc` | 46 | 全部 | 📝 | — |

> 狀態圖示:`📝` 待開始 / `🚧` 進行中 / `✅` 已完成 / `⏸` 暫停

## 子 plan 規範

### 命名

每個類別的子 plan 命名為 `plan-refactor-<XxxFunc>.md`,例如
`plan-refactor-GzipFunc.md`。完成後封存到 `docs/archive/`。

### 標準結構

每份子 plan 應包含:

```markdown
# 計畫：重構 `XxxFunc` 為 .NET idiomatic

**狀態：📝 擬定中**

## 目前內容
(列出該類別所有 public method 的簽章)

## Method Audit 表

| # | 方法簽章 | 處理路徑 | 新位置/名稱 | 替代方案備註 |
|---|---------|--------|------------|------------|
| 1 | `Foo(string s)` | B | `StringExtensions.Foo` | — |
| 2 | `Bar()` | A | `Guid.NewGuid()` BCL 替代 | 直接刪 |
| ... |

## 影響範圍
(grep caller 數量與檔案分布)

## 執行步驟
(具體 sed/edit 操作清單)

## 驗證
(build + test 命令)

## Commit 訊息草稿
```

## 跨類別決策紀錄

執行過程中浮現的共通決策記在此處,後續類別遵循同樣 pattern:

### Path C(改名)— 由 `GzipFunc → Gzip`(2026-05-01)定案

- **去掉 `Func` 後綴**,以名詞作為靜態 utility 類名,對齊 BCL `Path`、
  `Convert`、`Encoding`、`Convert` 等慣例
- 不另立 `*Extensions` 命名 —— 沒有 `this` 擴充就不用 `*Extensions`
- 不擴充 `byte[]`、`object` 等過度通用型別,以免污染 IntelliSense
- Namespace 維持原樣(本例 `Bee.Base.Serialization`),不另開新層級

### Path D + B 拆分 — 由 `BusinessFunc`(2026-05-01)定案

當原 `*Func` 類別內方法屬性各異,**逐一判斷後可整個刪除類別,讓每個方法找到自然歸屬**:

- **path D(domain integration)**:方法本質屬於某 domain object 既有職責,直接搬該 object 作 static method,不另立 utility(例:`BusinessFunc.GetDatabaseItem` → `BackendInfo.GetDatabaseItem`)
- **path B(domain interface 擴充)**:第一參數為 domain interface 且能讀成「subject 動作」者,轉擴充方法,類別命名 `<Interface 主體>Extensions`(例:`BusinessFunc.InvokeExecFunc` → `ExecFuncHandlerExtensions.InvokeExecFunc`)
- **不保留** grab-bag 共用靜態類:即使預期未來有更多 BO 共用方法,也應依方法本身屬性判斷歸屬,而非預先建立空殼類
- 跨 test project 共用 fake 的策略:**就地建立 minimal nested fake**,避免移動到 `Bee.Tests.Shared` 引發 visibility 變動(僅當同一 fake 被多處重複使用時才共享)

### Path B + C + D 三路拆分 — 由 `DefineFunc`(2026-05-01)定案

進一步擴充上述原則:

- **Enum 擴充方法**:domain enum 的轉換/查詢方法用 `<EnumName>Extensions.ToXxx(this EnumName)` 命名(例:`DefineFunc.GetDefineType` → `DefineTypeExtensions.ToClrType`),對齊 path B + .NET `To*` 慣例
- **Path C 命名延伸**:當方法承載「框架級命名約定」的查表(例:`"Amount" → "N2"`),改名到能呈現「這是預設組」語意的類別 —— 例 `NumberFormatPresets`(而非 `NumberFormatNames` 之類過弱的名稱);類名已含領域字眼時,方法名不重複(`ToFormatString` 而非 `ToNumberFormatString`)
- **Path D 進一步**:當原 `*Func` 方法是某 domain object 上 instance method 的「外包裝實作」(例:`FormSchema.GetListLayout` → `DefineFunc.GetListLayout` 又繞回來),應**直接內聯**回該 instance method,連帶私有 helper 一起搬入,徹底消除 wrapper 環呼叫
- **不為假設的未來設計**:即便已預期某 API 未來會擴充(例:`NumberFormatPresets` 未來可能加 enum overload),**現在只搬最低限度的內容**,不預先抽常數、enum、overload。等真的需要時再加

### Path A 「刪除 + inline」 — 由 `DateTimeFunc`(2026-05-01)定案

當 `*Func` 方法只有 1-2 個 prod callers 且 body 是 BCL 1-line 包裝,直接 inline 到 caller、刪除原 helper。比保留薄殼更清楚:

- **`object` 不擴充**:第一參數為 `object` 的方法不轉擴充方法(會污染所有型別 IntelliSense),改走 path A inline 或 path C 的 noun-form static utility(例:`DateTimeFunc.IsDate(object)` → inline 至 `BaseFunc.CDateTime` 用 `is DateTime` + `DateTime.TryParse`)
- **預設值/隱式約定的判斷**:當 helper 唯一加值是「強制某預設」(例:`InvariantCulture`),如果該預設應該被呼叫端意識到,不該藏在 helper 裡 —— 直接用 BCL 並把預設明示給呼叫端,而非透過 helper 隱含預設(本例 `DateTimeFunc.Format` 直接刪除)
- **Inline 後的測試覆蓋**:刪除 helper 前確認 caller 已有完整測試覆蓋 inlined 邏輯;若有,helper 自身的測試可一併刪除,避免重複測試

### Path C 命名衝突檢查 — 由 `HttpFunc → HttpUtilities`(2026-05-01)補強

選靜態類名時必須避開**所有** BCL namespace 末段名稱(不只是 type 名稱):

- Roslyn analyzer **CA1724** 對 namespace-vs-type 同名也會警告;在 `TreatWarningsAsErrors=true` 下會編譯失敗
- 常見地雷:`Http`(`System.Net.Http`)、`Json`(`System.Text.Json`)、`Xml`(`System.Xml`)、`Linq`(`System.Linq`)、`Threading`(`System.Threading`)、`Diagnostics` 等
- 短名 `Gzip` 安全是因為 BCL 沒有 `*.Gzip` namespace
- **選名前先 grep BCL namespace 清單**,或建立後本地跑一次 `dotnet build` 確認 CA1724 不觸發
- 命名衝突解法依優先序:換複數(`Http` → `HttpUtilities`)、加領域前綴、加 `*Helpers` 後綴(避免 `*Helper` 單數,在 .NET 已過時且仍可能與舊 type 撞名)

預期會碰到的決策點:
- 多個類別都有 `string` 擴充方法時,集中到同一個 `StringExtensions` 還是
  按主題分開?
- `FieldDbType` 等領域型別的擴充方法放哪個命名空間?
- 與既有 `IObjectCache`、`IDbAccess` 等介面整合的程度?

## 風險與回滾

- 範圍大,但 P3 每次只動一個類別,任一階段卡住可暫停或回滾單一 commit
- Public API breaking —— 已確認無外部 NuGet 消費者,可接受
- 預期需多次 commit,主 plan 持續更新到全部完成

## 時程

無嚴格時程要求,可隨時中斷。每個類別完成後評估是否繼續。

## 完成標準

- 12 個 `*Func` 類別全部處理完畢(消滅或改名)
- 全專案無 `*Func` 命名(除非有特殊保留理由)
- `code-style.md` 命名規則更新,反映新的 .NET idiomatic 命名慣例
- 所有 caller 已遷移,build + test 通過

## 後續(完成後)

- 更新 `.claude/rules/code-style.md`:
  - 移除任何 `*Func` 慣例描述
  - 新增 `*Extensions`、領域物件 method 等 .NET idiomatic 慣例說明
- 評估其他歷史命名(如 `Manager/` 資料夾)是否也需要對齊
