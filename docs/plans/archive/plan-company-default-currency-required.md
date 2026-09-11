# 計畫：公司本幣（`CompanyInfo.DefaultCurrency`）改為必填

**狀態：✅ 已完成（2026-09-10）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 設計裁定 | ✅ 已完成（2026-09-10） |
| 1 | 框架：`NumberFormatResolver` 空本幣拋例外 + 測試 + 種子 + 範例 + 文件 | ✅ 已完成（2026-09-10）：commit `31f832ce`（`[all-db]`） |
| 2 | `apps/Bee.Northwind`（bee-library 內副本）：本幣 USD + 幣別主檔 + 既有 db 回填；Order layout 補 `NumberKind` | ✅ 已完成（2026-09-10）：commit `31f832ce`（本幣）＋ `911689e3`（layout） |
| 3 | 案例 repo `bee-northwind-avalonia` 同步 | ✅ 已完成（2026-09-10）：commit `f3c0f3a`（本幣）＋ `2bcca81`（layout） |
| 4 | 回報 Day 26 那一邊三項事實 | ✅ 已完成（2026-09-10） |

## 背景

使用者裁定：**公司一定要有本幣**。框架目前明確允許空白（XML doc 寫 *Empty means unset … fall back to
the framework default of two decimals*），這次是**破壞性的行為變更**，二進位相容。

## 裁定結果（2026-09-10，使用者確認）

| # | 題目 | 裁定 |
|---|------|------|
| D1 | 本幣從哪裡來 | **框架不補預設、也不在進公司時檢查**。「沒給就 USD」（全球化較通用）只落在本 repo 各**種子 INSERT**；Northwind 同樣填 USD |
| D2 | 空白怎麼處理 | `NumberFormatResolver` 解析金額時，**有公司但本幣空白**就擲 `InvalidOperationException`；**沒有公司上下文**（`Company == null`）照舊退框架預設 |
| D3 | 案例 | 本幣 `USD`，並部署 `Define/CurrencySettings.xml` |
| D4 | 版號 | 4.31.0 minor；commit 標 `!`；PublicAPI 無異動 |

> D1 的背景：框架內沒有任何 `st_company` 寫入者（[`CompanyInfoService.cs:18`](../../../src/Bee.ObjectCaching/Services/CompanyInfoService.cs) 明寫公司主檔由外部維護），
> 所以「寫入時檢查」在框架內沒有落點；binder / repository 層的檢查方案不採用。

## 現況與影響面（已實測，HEAD `325edfbd`）

### 例外會在哪裡浮現

檢查放在 [`NumberFormatResolver.ResolveDecimals`](../../../src/Bee.Definition/NumberFormatResolver.cs) 的 `DecimalsSource.Currency` 分支：
`refCode` 為空、`ctx.Company != null`、`Company.DefaultCurrency` 為空白（`IsNullOrWhiteSpace`）時擲出。
**不論有沒有部署幣別主檔都檢查**——那是公司設定錯誤，與主檔是否存在無關。

| 呼叫路徑 | 會不會觸發 | 依據 |
|---------|-----------|------|
| 伺服端存檔：`FormBusinessObject.BuildRoundingContext` → `FormExpressionCalculator.ApplyComputed` → `RoundByKind` | **會**（金額欄沒綁 `CurrencyField`、主檔也沒 `sys_currency`，或幣別格仍空時） | `Company = ResolveCompanyInfo()`；`ResolveRefCode` 無參照欄回 `null` |
| UI 即時計算：`FormLiveComputation` | **會**，且**不會被 degrade 吞掉** | `FormView.ResolveRoundingContextAsync` 帶 `ClientInfo.Company`；`RunGuarded` 只吞 `ExpressionEvaluationException` / `FormatException` / `InvalidCastException` / `OverflowException` |
| UI 顯示格式：`NumericEdit` / `GridControl` | 不會 | 建 `RoundingContext` 時不帶 `Company`，本幣改走 `DefaultCurrencyCode` 參數 |
| 交付 schema：`NumberFormatApplier.Bake` | 不會 | `Currency` 來源一律 `continue`，不呼叫 resolver |
| `ResolveDecimals(kind, CompanyInfo?)` 等公司多載 | **會**（傳入本幣空白的公司解析 Amount 時） | 內部走 `RoundingContext.ForCompany(company)` |

`FormLiveComputation` **刻意不把 `InvalidOperationException` 加進 degrade 清單**：degrade 是給客戶寫錯運算式用的，
公司設定錯誤應該浮出來而不是靜默停用即時計算。

### 既有測試（逐檔核對過）

| 檔案 | 結論 |
|------|------|
| `NumberFormatResolverTests` | 解析 Amount 時公司都傳 `null` → 不受影響 |
| `NumberFormatResolverCurrencyTests` | 解析 Amount 的公司都有 `JPY`；`:134` 本幣空白的公司只用於 `RoundCash`（不讀本幣）→ 不受影響 |
| `NumberFormatResolverUnitTests` | 只解析 Quantity → 不受影響 |
| `NumberFormatApplierTests`、`FormDefinitionLoaderNumberFormatTests` | `Bake` 跳過 Amount → 不受影響 |
| UI / `FormRuleProcessor` / `FormExpressionCalculator` 測試 | `RoundingContext` 都沒帶 `Company` → 不受影響 |
| `tests/Define/`、`tools/Bee.LoadTests` | **沒有任何 `NumberKind="Amount"` 欄** → 打 DB 的測試不會走到新檢查 |
| `CompanyInfoMessagePackTests.cs:94` | **不改**——測的是序列化 round-trip，`CompanyInfo` 屬性預設仍是空字串 |

### 會壞掉、必須一起改的非測試程式

- [`samples/Avalonia.DemoCenter/Modules/Grids/NumberFormatModule.cs:88`](../../../samples/Avalonia.DemoCenter/Modules/Grids/NumberFormatModule.cs)：
  「公司 B」沒有本幣，而 `NumericColumns` 含 `amount`，切到公司 B 時 `ResolveFormat(Amount, company)` 會拋例外。

### 幣別主檔的部署現況

- 框架只在 [`Defaults/CurrencySettings.xml`](../../../src/Bee.Definition/Defaults/CurrencySettings.xml) 附 scaffold（含 USD、TWD），runtime 不會退回讀它。
- Northwind 的 `DefinePath` 就是原始碼的 `Define/`（`NorthwindBackend.ResolveDefinePath` 往上找 `SystemSettings.xml`）；
  `Defaults.MaterializeTo` 的篩選器**只鋪框架 TableSchema**。兩份 Northwind 都找不到 `CurrencySettings.xml` → **目前沒有主檔**。
- 沒有主檔時 resolver **完全不看公司本幣**，直接退公司位數表 → 框架 2。這是 D3 要部署主檔的理由。

## 階段 1：框架

### 1a. `NumberFormatResolver`

- `ResolveDecimals(NumberKind, RoundingContext, string?)` 的 `Currency` 分支加入檢查，例外訊息：
  `Company '{CompanyId}' has no default currency configured.`（公司代碼是呼叫端自己的上下文，不含表名、欄名等內部細節）。
- XML doc：補 `<exception cref="InvalidOperationException">`；遞補語意改寫為「refCode 空 → 公司本幣；**沒有公司上下文**才退框架預設」。
  公司多載（`ResolveDecimals` / `ResolveFormat` / `RoundByKind` 接 `CompanyInfo?` 者）同步補 `<exception>`。
- `RoundingContext.Company` 的 doc 同步。

### 1b. `CompanyInfo.DefaultCurrency` XML doc

改為「必填；空白屬設定錯誤」。依 `code-style.md`「絕對語氣要指得出執行機制」，這句要以
`<see cref="NumberFormatResolver"/>`（同組件）指出**是誰拒絕它**，並寫明拒絕時點是「解析金額位數時」，不是「載入時」。

### 1c. 測試

**新增**（`NumberFormatResolverCurrencyTests`，純邏輯用 `[Fact]`）：

1. 有公司、本幣空白、`refCode` 空 → `ResolveDecimals(Amount)` 擲 `InvalidOperationException`
2. 同上但本幣為純空白字串 → 一樣擲
3. 同上但**沒有部署主檔**（`CurrencySettings = null`）→ 一樣擲
4. 有公司、本幣空白、但 `refCode` 有值 → 依 `refCode` 解析，不擲
5. 有公司、本幣空白、解析非金額（`Percent`）→ 不擲
6. `RoundByKind(Amount)` 在本幣空白的公司下擲（伺服端存檔走的那條）
7. 沒有公司上下文 → 仍回框架 2（`:54` 已涵蓋，確認保留）

**種子與 INSERT**（D1：沒給就 USD）：

| 位置 | 改動 |
|------|------|
| [`SharedDatabaseState.Seed.cs:136`](../../../tests/Bee.Tests.Shared/SharedDatabaseState.Seed.cs)（C001） | 插入 `USD` |
| 同檔 `:119` 已存在分支 | 補冪等回填 `… SET default_currency = 'USD' WHERE sys_id = 'C001' AND (default_currency = '' OR default_currency IS NULL)`。**這是種子的一部分**：本機持久容器的 C001 已經存在、永遠走這條，不回填的話 `CompanyRepositoryTests:47` 會「本機紅、CI 綠」。Oracle 的 `''` 等於 `NULL`，兩個條件都要 |
| `CompanyRepositoryTests.cs:47` | 斷言改為 `USD` |
| `SystemBusinessObjectLifecycleTests.cs:92`、`SystemBusinessObjectEnterCompanyTests.cs:37/:48`、`TenantCustomizationEndToEndTests.cs:285`、`CompanyRepositoryTests.cs:134`、`UserCompanyRepositoryTests.cs:124` | 插入改給 `USD`。**不是為了讓測試過**（這些測試碰不到金額欄），是讓測試資料符合「公司一定有本幣」 |
| `tools/Bee.LoadTests/Bootstrap/AccountSeeder.cs:58` | 插入 `USD`；**不回填**（LoadTests 定義沒有金額欄） |

### 1d. 範例

`NumberFormatModule.CompanyWithOverrides` 的公司 B 補 `DefaultCurrency = "USD"`。

### 1e. 文件

| 文件 | 改動 |
|------|------|
| [`adr-026`](../../adr/adr-026-numeric-semantics-rounding.md) | 比照 ADR-034 加 `## 修訂紀錄` / `### 2026-09-xx：公司本幣改為必填`：決策、拋例外的時點與理由、「沒有公司上下文仍退框架預設」這條不變；原決策第 68 行「舊資料欄空即全退框架預設」保留為歷史，由修訂段說明已不成立 |
| `docs/development-cookbook.md` / `.zh-TW.md`（631 / 608 行附近） | 遞補鏈旁補一句：公司本幣為必填、空白時解析金額會擲例外；「→ 框架 2」只適用於沒有公司上下文。**兩份同步** |
| `docs/repo-ops/future-work.md:293` | 「預設幣別 vs 本位幣」那段改寫現況：已必填，但換算語意仍未補 |

跑 `./check-public-docs.sh`。CHANGELOG 於發版時由 `/dev-workflow:changelog-draft` 帶出，**必須列「升級須知」**：
既有 `st_company` 列本幣空白者，升級後**含金額計算的單據存檔**（與 UI 即時計算）會擲例外；框架不代選幣別，由部署方自行 `UPDATE`。

### 1f. 驗證與提交

- `dotnet build --configuration Release`（含 `Bee.Samples.slnx` 的 DemoCenter）+ `./test.sh` 全部。
- 種子 SQL 跨 provider（Oracle 的 `IS NULL` 回填）→ **push 前詢問是否 `[all-db]`，建議全跑**。
- commit 帶 pathspec；message 標 `!`，寫明「行為破壞、二進位相容、PublicAPI 無異動」。

## 階段 2：`apps/Bee.Northwind`

- `NorthwindCredentials`：新增 `DefaultCurrency = "USD"` 常數（比照 `CompanyId` / `CustomizeId` 的註解寫法）。
- `NorthwindSchemaSeeder.SeedDemoCompany`（`:258`）：插入改用該常數；`:254` 的 `COUNT(*) > 0` 早退分支前補冪等回填。
  理由：`northwind.db` 是 gitignored 的本機檔，不回填的話，升上 4.31.0 後既有 db 的訂單**存檔會擲例外**。
  同步更新 `<remarks>` 裡「三個 XML 欄給空字串」那段說明。
- 新增 `Define/CurrencySettings.xml`，內容複製自 `src/Bee.Definition/Defaults/CurrencySettings.xml`。
- build 六個 head；Desktop 冒煙：登入 → 進公司 → 開訂單 → 改明細數量 → `amount` 顯示 2 位 → 存檔成功。
  **上次同步時 Desktop 冒煙被工具鏈擋住**；若仍受阻則如實回報，不宣稱已驗證。

## 階段 3：案例 repo 同步

`/Users/jeff/Desktop/repos/bee-northwind-avalonia`。`NorthwindSchemaSeeder.cs`、`NorthwindBackend.cs` 與 bee-library 那份**逐檔相同**（已 diff）。

- 搬移階段 2 的 `NorthwindCredentials`、seeder、`Define/CurrencySettings.xml`。
- **不必等框架發版**：案例引用 NuGet 4.30.0，4.30.0 的 resolver 在本幣 USD + 有主檔時就會走「本幣 → 主檔」這條，資料變更在現行版本即生效；
  框架版號等 4.31.0 發佈後的下一次常規同步再升。
- build → commit（pathspec）→ `git push origin main`（push 前確認）。

## 階段 4：回報 Day 26

框架與案例都完成後，回報以下事實（**不改 `docs/blogs/`**）。預先推導如下，待階段 2 冒煙實測確認：

1. **本幣**：`USD`。
2. **幣別主檔**：有部署，`Define/CurrencySettings.xml`，自框架 `Defaults/` 複製。
3. **`amount` 位數**：**2 位**。解析路徑：`Order.FormSchema.xml` 的 `amount` 是 `NumberKind="Amount"`，沒有 `CurrencyField`，
   主檔也沒有 `sys_currency` → `refCode` 空 → 公司本幣 `USD` → 主檔 `USD` 的 `Rounding=0.01` → 2。
   改動前同樣是 2 位，但路徑是「本幣空白、沒有主檔 → 框架預設」，**本幣沒有參與計算**。

另須一併提醒：框架端的空本幣檢查要到 **4.31.0 發佈**才會出現在 NuGet 套件裡；前三點在案例現行的 4.30.0 上已經成立。

## 執行結果（2026-09-10）

### 範圍對帳

實際改動與階段 1、2 宣告一致：框架 16 檔（src 3、tests 7、tools 1、samples 1、docs 4）＋ Northwind 3 檔
（`NorthwindCredentials.cs`、`NorthwindSchemaSeeder.cs`、`Define/CurrencySettings.xml`）。
案例 repo 同步同樣這 3 檔，逐檔 `cmp` 相同。另有 gitignored 的本機 `northwind.db` 被回填改動（預期行為）。

### 建置與測試

- `Bee.Library.slnx`、`Bee.Samples.slnx`、`Bee.Tools.slnx`：Release 0 警告 0 錯誤。
- `Bee.Northwind.slnx`：以 `DEVELOPER_DIR=/Applications/Xcode-26.5.0.app/…` 建置（主 Xcode 為 26.6，iOS SDK 要求 26.5），0 錯誤；
  67 個警告全為 iOS head 對 MessagePack / SDK 組件的 IL2104 trim 警告，排除後無任何其他警告。
- 案例 repo（NuGet 4.30.0）：同樣帶 `DEVELOPER_DIR`，0 警告 0 錯誤。
- `./test.sh` 全套（四個 DB 容器皆在跑）：17 個測試專案全數通過；唯一略過的
  `Login_WithRsaKeyPair_ReturnsDecryptableSessionKey` 為既有標記，與本次無關。新增測試以 filter 單獨確認 7 筆（含既有遞補測試）通過。

### CI（`31f832ce`，`[all-db]`，run 34489662130）

- 完整模式（四種資料庫 + SonarCloud）通過。
- Sonar 對本次改動報了一筆：`NumberFormatResolver.cs` 的私有 helper 插在兩個 `ResolveDecimals` 多載之間，
  違反「同名多載要相鄰」→ 後續 commit 把 `ResolveCompanyCurrency` 移到類別尾端（零行為變更）。
  其餘 annotation 都在 `tools/Bee.Cli`、`tools/Bee.LoadTests/Bootstrap/SchemaPreparer.cs`，不在本次改動的檔案。
- `911689e3` 沒有觸發 CI：`build-ci.yml` 的 `paths` 只含 `src/`、`tests/` 等，`apps/` 與 `docs/` 的改動不跑，屬預期。

### 執行期驗證（Browser head，取代受阻的 Desktop 冒煙）

- server 啟動時 seeder 回填：本機 `northwind.db` 的 `NORTHWIND` 本幣由 `''` → `USD`（sqlite3 查證）。
- 連線 → 登入 → 自動進公司 → Orders 清單 → 檢視 / 編輯 10248 → Cancel，server log 無錯誤。
- **即時計算的 2 位捨入未能從畫面證實**：canvas 繪製的 grid 以點擊開不出儲存格編輯器。該路徑由 `NumberFormatResolverCurrencyTests` 覆蓋。

### ⚠️ 發現：案例 UI 的金額沒有套任何數值格式（既有狀況，非本次造成）

畫面上明細 `amount` 顯示 `252`、`100`，`unit_price` 顯示 `21`，表頭 `total_amount` 顯示 `352` —— 都是原值，不是 `252.00`。

根因：`Define/FormLayout/Order.FormLayout.xml` 與 `Customize/northwind-demo/FormLayout/Order.FormLayout.xml`
都是手寫的，`LayoutColumn` / `LayoutField` 沒有 `NumberKind`。框架只有在**產生** layout 時
（`FormLayoutGenerator` / `ListLayoutGenerator` / `LookupLayoutGenerator` → `LayoutColumnFactory`）才從 `FormField` 帶入 `NumberKind`，
手寫 layout 在執行期不會被補上，`NumericEdit` / `GridControl` 因此判為 `NumberKind.None` 不格式化。

→ 對 Day 26 第 3 點的影響：resolver 解析出的是 **2 位**（伺服端存檔捨入依 schema 的 `NumberKind`，不看 layout），
但**讀者在案例畫面上看不到 `252.00`**。

### 處置：案例 layout 補 `NumberKind`（使用者裁定，2026-09-10）

兩份 `Order.FormLayout.xml`（`Define/` 與 `Customize/northwind-demo/`，客製 layout 整份取代、不合併，故兩份都改）：

| 欄位 | 補上的屬性 | 為何這樣就夠 |
|------|-----------|-------------|
| 明細 `amount`（`LayoutColumn`） | `NumberKind="Amount"` | grid 顯示文字一律走 `FormatCellForColumn`，不看 `ControlType`；幣別分支由 `CurrencySettings` + `DefaultCurrencyCode` 解析 |
| 表頭 `total_amount`（`LayoutField`） | `NumberKind="Amount"`、`ControlType="NumericEdit"` | `LayoutField.ControlType` 預設 `TextEdit`，`FieldEditorFactory` 只有 `NumericEdit` 會套數值格式 |

**與原本約定範圍的差異：`unit_price` 沒有補。** grid 對非幣別種類（`UnitPrice` 屬公司來源）只回傳 `column.NumberFormat`，
而手寫 layout 沒有 bake 過的格式，單補 `NumberKind` 畫面不會變——補了反而看起來像有作用。

**執行期實證**（Browser head，檢視 10248）：明細 `amount` 顯示 `252.00` / `100.00`，表頭 `total_amount` 顯示 `352.00`。
Server 與案例 repo 的 Server 專案建置皆 0 警告 0 錯誤，server log 無錯誤。

**仍未格式化的地方**（刻意不動，已知）：

- 清單的 Total Amount 欄仍是 `352`——清單 layout 由 schema 產生，而 schema 的 `total_amount` 沒有 `NumberKind`。
- `unit_price` 仍是 `21`（理由見上）。
- 根本解是框架在執行期把 schema 的 `NumberKind` / `NumberFormat` 補進手寫 layout；屬框架另案，本 plan 不處理。
