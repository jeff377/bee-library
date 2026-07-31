# 計畫：docs 根目錄文件重編排

**狀態：✅ 已完成（2026-07-31）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 索引重編排（旅程分層 + 主題交叉索引）與既有文件一致性修補 | ✅ 已完成（2026-07-31） |
| 2 | `expression-rules` 雙語化並列入索引 | ✅ 已完成（2026-07-31） |
| 3 | 新增 `getting-started`（雙語） | ✅ 已完成（2026-07-31） |
| 4 | 新增 `definition-files-overview`（雙語） | ✅ 已完成（2026-07-31） |
| 5 | 驗收：連結檢查、雙語同步、公開文件規範掃描 | ✅ 已完成（2026-07-31） |

## 執行結果與最終範圍

- **定義類型數與擬定時的估計不符**：計畫寫「八種定義檔」，實際 `DefineType` 有 **11 種**
  （多出 `PermissionModels`、`CurrencySettings`、`UnitSettings`）。文件依實際情況撰寫。
- **宣告範圍外的追加**（3 檔）：驗收時發現三處**既有**斷連結，均在公開文件、且因原始碼搬移造成，
  順手修正並在此記錄 —— `docs/adr/adr-007-*.md`（`ApiOutputConverter.cs` 已移入 `Conversion/`）、
  `samples/Avalonia.Demo/README.md` 與 `.zh-TW.md`（`FormView.cs` 已移入 `Views/`）。
- **未處理、留待後續**：`docs/adr/README.md` 是中文內容但用英文主檔名，且無 `.zh-TW.md` 對版
  —— 與 `expression-rules` 同類型的雙語破例。因計畫非目標明列「不動 `adr/` 內部結構」，本次未處理。

## 背景

`docs/` 根目錄現有 19 份公開文件（雙語共 39 檔、約 10,000 行）。**內容品質不是問題，問題在編排與入口。**
盤點結果：

### 問題 1 — 索引分類軸混用兩種標準

`docs/README.md` 現行四類「入門 / 開發指引 / 資料庫 / 設計概念」中，「資料庫」是**主題軸**，
其餘三類是**文件類型軸**。兩軸混用導致歸類不穩：

| 文件 | 現在放在 | 實際性質 |
|---|---|---|
| `api-method-reference` | 設計概念 | 純查表 reference |
| `terminology` | 入門 | 詞彙表 reference |
| `dependency-map` | 入門 | 16 專案相依，進階向，新手第一天用不到 |
| `temporal-types` / `datetime-timezone` | 設計概念 | 寫欄位時每天查的參考 |
| `analyzer-rules` | 開發指引 | 建置期診斷清單 reference |

### 問題 2 — 沒有閱讀路徑，也沒有份量標示

索引只有分類表格，未指出「先讀哪幾份就能動手」。`development-cookbook`（633 行）與
`datetime-timezone`（111 行）在表格裡看起來份量相同。

### 問題 3 — 缺 Getting Started

`docs/` 沒有「從零到跑起來」。此事現散在三處，且都不是為外部開發者寫的：

- repo 根 `README.md` 的 Quick Start —— 跑**既有** sample，不是建自己的專案
- `development-cookbook` 的 Framework Initialization Order —— 既有 host 的接線細節，非教學
- `.claude/skills` 的 `bee-jsonrpc-backend` —— agent 專用，外部開發者看不到

結論：**裝完 NuGet 之後要做什麼，沒有單一入口。**

### 問題 4 — 缺「定義檔全景」

FormSchema / FormLayout / TableSchema / LanguageResource / SystemSettings / DatabaseSettings /
DbCategorySettings / ProgramSettings 是框架最核心的東西，卻散在四份文件：
`architecture-overview`（前三者的概念）、`database-settings-guide`（兩個設定檔）、
`framework-reserved-names`（檔案清單）、`development-cookbook`（FormSchema-Driven 段）。
**沒有一份講「有哪些定義檔、各管什麼、彼此怎麼串」。**

### 問題 5 — `expression-rules.md` 是孤兒且違反雙語硬規則

未列入索引、無 `.zh-TW.md` 對版、無語言切換連結、英文檔名裝中文內容。但被
`CHANGELOG.md` / `CHANGELOG.zh-TW.md`（公開）、`datetime-timezone`、
`database-naming-conventions`、`../adr/adr-028-expression-rule-engine.md` 共 6 處引用 —— 不是死檔。
[plan-framework-review-2026-07-28.md](archive/plan-framework-review-2026-07-28.md) 已標記過，未修。

### 問題 6 — 三處一致性瑕疵

- **19 份文件無一有回索引麵包屑**（`../docs/adr/README.md` 反而有）。從搜尋引擎直落單篇者沒有回索引的路。
- **語言切換連結位置不一**：`analyzer-rules`、`database-dialect-differences`、`permission-authorization`
  放在 `# 標題`**之前**，其餘 16 份放之後。GitHub 上前者會讓語言連結成為頁面第一行、標題被壓下。
- **`database-schema-upgrade` 有三個名字**：檔名 `database-schema-upgrade`、
  H1 `Table Schema Upgrade Guide`、索引「資料庫 Schema 升級」。

## 目標與非目標

**目標**

1. 索引改為**依讀者旅程分層**，任何一份文件的歸屬有單一判準（讀者處於哪個階段）。
2. 保留主題找法：加一張**主題交叉索引**小表，兩種找法並存。
3. 補上兩個內容缺口（getting-started、definition-files-overview）。
4. 消除雙語 / 麵包屑 / 命名的一致性破例。

**非目標**

- **不改任何既有檔名**。`docs/*.md` 已被 CHANGELOG、ADR、repo 根 README、外部文章引用，
  改名會斷外部連結，收益不足以抵銷。名稱不一致改以統一 H1 與索引措辭解決。
- **不合併既有文件**。內容重疊（如 cookbook 與 architecture-overview）屬另一議題，本計畫不處理。
- 不動 `adr/`、`changelogs/`、`repo-ops/`、`plans/` 的內部結構。

## 階段 1 — 索引重編排與一致性修補

### 1.1 新索引結構

`docs/README.md` / `docs/README.zh-TW.md` 改為五層，每份文件標**類型**與**份量**：

| 層 | 意義 | 收錄 |
|---|---|---|
| **開始使用** | 讀完能動手 | `getting-started`【新】、`architecture-overview`、`definition-files-overview`【新】 |
| **核心概念** | 為何這樣設計 | `formmap`、`api-bo-contract-design`、`dependency-map` |
| **開發指引** | 做某件事的流程 | `development-cookbook`、`expression-rules`、`permission-authorization`、`jsonrpc-frontend-integration`、`database-settings-guide`、`database-schema-upgrade` |
| **查詢參考** | 隨手查 | `terminology`、`api-method-reference`、`framework-reserved-names`、`database-naming-conventions`、`database-dialect-differences`、`temporal-types`、`datetime-timezone`、`analyzer-rules`、`development-constraints` |
| **深入閱讀** | 決策脈絡與版本 | `adr/`、`changelogs/`（附 `plans/`、`repo-ops/` 的性質說明） |

每列格式：`| 文件 | 類型 | 篇幅 | 說明 |`，類型取
`教學 / 概念 / 指引 / 參考`，篇幅取 `短（< 150 行）/ 中（150–350）/ 長（> 350）`。

索引開頭加一段**四行閱讀路徑**（不做成表格，避免與下方分層重複）：

> 第一次接觸 → 讀「開始使用」三份即可動手；
> 要理解取捨 → 加讀「核心概念」；
> 動手中卡住 → 查「開發指引」對應那份；
> 寫欄位 / 命名 / 查 API → 直接翻「查詢參考」。

### 1.2 主題交叉索引

在五層之後加一張小表，讓「我要找資料庫的東西」這種找法仍成立：

| 主題 | 相關文件 |
|---|---|
| 資料庫 | `database-naming-conventions`、`framework-reserved-names`、`database-settings-guide`、`database-schema-upgrade`、`database-dialect-differences`、`formmap` |
| 定義層 | `definition-files-overview`、`architecture-overview`、`expression-rules`、`framework-reserved-names` |
| API 與前端 | `api-bo-contract-design`、`api-method-reference`、`jsonrpc-frontend-integration`、`permission-authorization` |
| 型別與時間 | `temporal-types`、`datetime-timezone` |
| 品質與規範 | `analyzer-rules`、`development-constraints`、`database-naming-conventions` |

同一份文件出現在多主題是預期行為（交叉索引的本意）。

### 1.3 一致性修補

| 修補 | 範圍 | 做法 |
|---|---|---|
| 麵包屑 | 19 份 × 雙語 = 38 檔 | 語言切換連結**同一行**補回索引連結：`[繁體中文](xxx.zh-TW.md) · [← 文件索引](README.md)` |
| 語言連結位置 | `analyzer-rules`、`database-dialect-differences`、`permission-authorization` × 雙語 = 6 檔 | 一律移到 `# 標題` 之後、正文之前 |
| 名稱三不一致 | `database-schema-upgrade` × 雙語 | H1 統一為 `Database Schema Upgrade` /「資料庫 Schema 升級」，與索引措辭一致；**檔名不動** |

## 階段 2 — `expression-rules` 雙語化

1. 現有中文內容搬至 `docs/expression-rules.zh-TW.md`。
2. `docs/expression-rules.md` 改寫為英文主檔（內容對等，非逐字直譯；程式碼區塊與識別符不動）。
3. 兩份補語言切換 + 麵包屑（沿用階段 1 格式）。
4. 列入索引「開發指引」層。

**既有 6 處引用皆指向 `expression-rules.md`，英文主檔沿用同檔名 → 連結不需改。**
另檢查 `CHANGELOG.zh-TW.md` 第 38 行的中文引用宜改指 `.zh-TW.md` 版。

## 階段 3 — 新增 `getting-started`（雙語）

定位：**建自己的第一個 Bee.NET 後端**，與 repo 根 README 的「跑既有 sample」互補、不重複。

大綱（約 200 行 × 雙語）：

1. 前置需求（.NET 10 SDK、一個支援的資料庫）
2. 建專案並安裝套件（`Bee.Api.AspNetCore` / `Bee.Hosting` 的選擇依據）
3. `dotnet tool install -g Bee.Cli` → `dotnet bee defines materialize --path ./Define`
4. 設定 `SystemSettings`（`MasterKeySource`）與 `DatabaseSettings`（連線字串）
5. `AddBeeFramework` 接線（**引用** `development-cookbook` 的啟動流程圖，不重抄）
6. 定義第一張表單（**引用** `definition-files-overview`，此處只給最小可跑的 FormSchema + TableSchema）
7. 寫第一個 BO 並以 `Bee.Api.Client` 呼叫
8. 「接下來讀什麼」——指回索引五層

**寫作紀律：每一步凡已有深入文件者只給最小可跑範例 + 連結，不複製內容**，避免產生第二個
會漂移的事實來源。

repo 根 `README.md` / `README.zh-TW.md` 的「Quick Start」段落末尾加一句指向本文件。

## 階段 4 — 新增 `definition-files-overview`（雙語）

定位：全部定義檔的**全景圖**，回答「有哪些、各管什麼、彼此怎麼串、改哪個影響哪層」。

大綱（約 250 行 × 雙語）：

1. 一張總表：檔案 / 所在層 / 職責 / 誰讀它 / 對應深入文件
2. 三個定義中樞的關係圖：FormSchema →（驅動）FormLayout / TableSchema / 驗證規則
3. 執行期設定三件組：SystemSettings / DatabaseSettings / DbCategorySettings 的載入順序與相依
4. ProgramSettings（BO 綁定 + 選單來源）與 LanguageResource（i18n）
5. 「改了 X 要同步改什麼」對照表
6. 檔案落地位置：`DefinePath` 與 `Defaults/` scaffold 的關係（**明確寫出 runtime 不 fallback 到 `Defaults/`**）

**本文件以彙整現有內容為主**，取材自 `architecture-overview`、`database-settings-guide`、
`framework-reserved-names`、`development-cookbook`；凡深入細節一律連過去，本文件只負責「全景 + 導引」。

## 階段 5 — 驗收

1. **索引完整性**：`docs/*.md`（排除 `README*`）每份都在索引中恰好出現一次（主題交叉索引不計）。
2. **雙語成對**：`docs/` 根目錄每個 `xxx.md` 都有 `xxx.zh-TW.md`，反之亦然。
3. **連結不爛**：全 repo markdown 相對連結解析檢查（含 repo 根 README、CHANGELOG、ADR 指進 `docs/` 的連結）。
4. **公開文件不得引用 plan**：跑 `../../.claude/rules/public-docs.md` 落地檢查節的四段 grep，
   確認新增文件未引用本 plan 或任何 plan。
5. **麵包屑**：38 檔皆含回索引連結。

## 風險與取捨

| 項目 | 判斷 |
|---|---|
| 分層後「資料庫」群被打散 | 以主題交叉索引補回，兩種找法並存；代價僅索引多一張小表 |
| 新增兩份文件會與既有內容漂移 | 以「只給最小範例 + 連結」的寫作紀律壓低；兩份新文件定位皆為**導引**而非事實來源 |
| 不改檔名 → 名稱語意仍不完全對齊 | 外部連結穩定性優先；以統一 H1 與索引措辭緩解 |
| 階段 3、4 是新寫作，工作量最大 | 階段 1、2 可獨立交付先行落地，不必等 3、4 |

## 執行順序

階段 1 → 2 可立即開始且互不相依；階段 3、4 依賴階段 1 的新索引結構（新文件要掛進正確的層）。
階段 5 於 3、4 完成後執行一次即可。
