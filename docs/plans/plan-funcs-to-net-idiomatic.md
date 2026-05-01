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
| 2 | `BusinessFunc` | 3 | D | 📝 | — |
| 3 | `DefineFunc` | 3 | B | 📝 | — |
| 4 | `DateTimeFunc` | 5 | B | 📝 | — |
| 5 | `HttpFunc` | 5 | B | 📝 | — |
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
