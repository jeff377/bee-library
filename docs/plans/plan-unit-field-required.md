# 計畫：數量／重量欄必須綁定 `UnitField`

**狀態：✅ 已完成（2026-09-11）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 設計裁定（D1–D6） | ✅ 已完成（2026-09-11） |
| 1 | 框架：解析器、計算器、`Bake` + 測試 + 範例 + 文件 | ✅ 已完成（2026-09-11） |
| 2 | DefineEditor 設計期檢查（`FormSchemaValidator`） | ✅ 已完成（2026-09-11） |

## 背景

起因是 [uom-decimals-prior-art.md](../repo-ops/uom-decimals-prior-art.md) 的層級比較：
SAP 與 Odoo 都沒有「公司層的單位位數」，Bee.NET 卻在數量欄沒綁單位時退到公司位數。

### 兩條遞補鏈退到的東西不一樣

| | 第一步 | 第二步 | 第三步 | 位數最後由誰決定 |
|---|---|---|---|---|
| 金額 | 綁定的 `CurrencyField` | 表頭 `sys_currency` | 公司本幣（必填） | 系統層幣別主檔 |
| 數量／重量 | 綁定的 `UnitField` | **公司位數** | 框架預設 | 綁了看單位主檔；**沒綁由公司決定** |

幣別那條每一步拿到的都是**一個幣別代碼**，公司只決定「用哪個幣別」、從不直接決定位數。
單位那條的第二步拿到的是**一個位數**。

## 裁定結果（2026-09-11，使用者逐題確認）

**原則：標成 `Quantity`／`Weight` 的欄位就必須綁 `UnitField`；不需要綁單位的欄位用一般數值（不標 `NumberKind`）。**
這與 SAP 相同（ABAP Dictionary 要求 `QUAN` 型別指定單位參照欄，不帶單位的數值用 `DEC`）。
執行期行為**對齊框架的多幣別作法**，只在幣別有、單位沒有對應物的地方分開處理 ——
使用者的判準是「**公司會有本幣當預設幣別，但不會有預設單位**」。

| # | 題目 | 裁定 | 對照幣別 |
|---|------|------|---------|
| D1 | 沒綁 `UnitField` 在哪裡擋 | **計算時**擲 `InvalidOperationException`（伺服端存檔與前端即時計算共用 `FormExpressionCalculator`）；顯示路徑與 `Bake` 不擲；另由 DefineEditor 在設計期報 Error | 幣別的設定錯誤（公司沒本幣）同樣是計算時擲、顯示不擲。幣別沒有設計期檢查，單位多這一層是為了抓「只輸入、不計算」的欄位 |
| D2 | 綁了、該列單位代碼空 | `RoundByKind` **不捨入**，原值返回 | 幣別會退到表頭幣別 → 公司本幣；單位沒有可退的對象，拿不到位數就不丟資訊 |
| D3 | 代碼不在單位主檔 | 退**框架預設**（Quantity 0／Weight 3）並捨入，不報錯 | 幣別退 0.01（2 位）並捨入，同一個形狀 |
| D4 | 單位主檔沒部署 | 退**框架預設**並捨入，**不經公司** | 幣別會先查公司位數表；單位不經公司，理由同上 |
| D5 | 公司層的 `Quantity`／`Weight` 覆寫 | 忽略（由 D4 推導） | — |
| D6 | 單位隱含在語意裡的純計數 | 用一般數值，不標 `NumberKind`；不新增固定單位碼屬性 | — |

**由裁定推導（非另行裁定）：**

- `Bake` 對所有 `Quantity`／`Weight` 欄一律不 bake，與金額一律不 bake 一致。沒綁的欄因此顯示原值。
- 判斷順序：代碼空（D2）先於主檔是否部署（D4）。
- 公司多載 `RoundByKind(value, Quantity|Weight, company)` 沒有單位代碼，依 D2 原值返回；
  `ResolveDecimals`／`ResolveFormat` 的公司多載回框架預設（只影響顯示格式，它們必須回整數）。

## 現況與影響面（已實測，HEAD `efea60bb`）

### 解析路徑：現況 → 改後

| 情況 | 伺服端捨入（`FormExpressionCalculator` → `RoundByKind`） | 前端交付（`FormDefinitionLoader` → `Bake`） | 前端顯示（`GridControl`／`NumericEdit`，程式不動） |
|------|------|------|------|
| **A** 綁、代碼有值、查得到 | 單位位數（不變） | 不 bake（不變） | 單位位數（不變） |
| **B** 綁、該列代碼空 | 公司位數 → 框架預設 ⇒ **不捨入** | 不 bake（不變） | 欄級 `NumberFormat`，實際為空 → 原值（不變） |
| **C** 綁、代碼不在主檔 | 0 位 ⇒ **框架預設** | 不 bake（不變） | 0 位 ⇒ **框架預設格式**（解析器改了，顯示跟著變） |
| **D** 綁、主檔沒部署 | 公司位數 → 框架預設 ⇒ **框架預設** | 不 bake（不變） | 欄級 `NumberFormat` → 原值（不變） |
| **E** 沒綁 | 公司位數 → 框架預設 ⇒ **計算欄擲例外** | bake 公司位數 ⇒ **不 bake** | baked 格式 ⇒ **原值** |

依據：[`NumberFormatResolver.ResolveDecimals`](../../src/Bee.Definition/NumberFormatResolver.cs) 的
`DecimalsSource.Unit` 分支、[`FormExpressionCalculator.ResolveRefCode`](../../src/Bee.Definition/Forms/FormExpressionCalculator.cs)
（只讀**同一列**的變數）、[`NumberFormatApplier.Bake`](../../src/Bee.Definition/Forms/NumberFormatApplier.cs)、
[`UnitSettings.GetDecimals`](../../src/Bee.Definition/Settings/UnitSettings/UnitSettings.cs)、
[`GridControl.ResolveCellNumberFormat`](../../src/Bee.UI.Avalonia/Controls/GridControl.Cells.cs)（單位分支在代碼空時不解析）。

### 幣別側的對照依據

| 情況 | 金額現況 | 位置 |
|------|---------|------|
| 綁了、格子空 | 退表頭 `sys_currency` → 公司本幣 | `FormExpressionCalculator.ResolveRefCode`、`NumberFormatResolver.ResolveDecimals` |
| 代碼不在主檔 | `CurrencySettings.FallbackRounding`（0.01）→ 2 位 | `CurrencySettings.GetRounding` |
| 主檔沒部署 | 伺服端退公司位數表 → 框架 2；前端顯示用欄級格式（金額不 bake → 原值） | `NumberFormatResolver.ResolveDecimals`、`GridControl.ResolveCellNumberFormat` |
| 公司沒本幣 | 計算時擲；顯示不擲（UI 的 `RoundingContext` 不帶 `Company`，改傳 `DefaultCurrencyCode`） | `NumberFormatResolver.ResolveCompanyCurrency`、`FormView.Build` |
| 載入期／設計期檢查 | 無：`FormSchemaCache.CreateInstance` 讀到即回傳；`FormSchemaValidator` 不看 `CurrencyField` | [`FormSchemaCache`](../../src/Bee.ObjectCaching/Define/FormSchemaCache.cs)、[`FormSchemaValidator`](../../tools/DefineEditor/Services/FormSchemaValidator.cs) |

### `Bake` 只在前端

`NumberFormatApplier.Bake` 全 `src/` 唯一的呼叫端是
[`FormDefinitionLoader.GetLocalizedSchemaAsync`](../../src/Bee.Api.Client/Definitions/FormDefinitionLoader.cs)，
註解寫明伺服端照原樣供應定義。ADR-026 D5 與 cookbook〈Display format is baked at delivery〉寫的
「`SystemBusinessObject.LoadAndLocalizeSchema` 於伺服端 bake」**已過時**，修 ADR 時一併更正。

### 必綁檢查為何放計算器、不放解析器

B 與 E 傳進 `ResolveDecimals` 的 `refCode` 都是空的，解析器分不出「沒綁」與「綁了但格子空」。
看得到 `FormField` 的只有計算器、`Bake` 與 DefineEditor；依 D1 取計算器與 DefineEditor。

### 目前的使用者

- **XML 定義檔**（全 repo，不含 `docs/blogs/`）：沒有任何 `NumberKind="Quantity"` 或 `"Weight"`。
  Northwind 的 `quantity` 是 `Integer`、未標 `NumberKind` → **`apps/` 與案例 repo 不受影響**。
- **非測試程式**：
  - [`NumberFormatModule`](../../samples/Avalonia.DemoCenter/Modules/Grids/NumberFormatModule.cs)：
    `quantity`、`gross_weight` 未綁單位（重量的單位寫在標題「重量(kg)」裡），公司 B 設了 `Quantity` 覆寫。**要改。**
  - [`MultiUnitModule`](../../samples/Avalonia.DemoCenter/Modules/Grids/MultiUnitModule.cs)：已綁 `qty_uom`，不受影響。

### 既有測試（逐檔核對過）

| 檔案／測試 | 改後 |
|------|------|
| `NumberFormatResolverUnitTests.ResolveDecimals_NoUnit_FallsBackToCompany`（公司設 `Quantity` 2、`refCode` null，預期 2） | **改**：預期框架 0（公司不參與）；另補 `RoundByKind` 原值返回 |
| `NumberFormatResolverUnitTests.ResolveDecimals_NoUnitMaster_FrameworkDefault`（無公司、無主檔、`KG`） | 不變（D4） |
| `NumberFormatResolverUnitTests.ResolveDecimals_UnknownUnit_ReturnsUnitFallback`（`Weight`、`XXX`，預期 0） | **改**：預期 3（D3），`DisplayName` 同步 |
| `NumberFormatResolverUnitTests` 其餘（依單位解析、round-then-sum、同欄不同單位） | 不變 |
| `UnitSettingsTests.GetDecimals_Miss_ReturnsFallback` | 不變：`UnitSettings.GetDecimals` 公開語意保留，解析器改用 `Find` |
| `NumberFormatApplierTests.Bake_QuantityWithoutUnitField_BakedFromCompany` | **改**：不 bake |
| `NumberFormatApplierTests.Bake_QuantityWithUnitField_NotBaked` | 不變 |
| `FormExpressionCalculatorTests:24`、`FormRuleProcessorTests:27`、`FormLiveComputationTests:23`／`:145` | 不變：未綁的 `qty`／`quantity` 是**輸入欄**（計算欄是 `amount`），D1 只檢查計算欄 |
| `FormExpressionCalculatorCoverageTests:241` | 不變：綁 `unit`、列上 `KG`、無主檔 → 框架 3，`qty * 2` = 6 |
| `GridControlUnitTests`、`NumericEditTests` 的單位測試 | 不變：UI 程式不動 |

## 階段 1：框架

### 1a. `NumberFormatResolver`

- `DecimalsSource.Unit` 分支改為（拿掉公司那一步）：
  1. `refCode` 空 → 框架預設（D2 的「不捨入」由 `RoundByKind` 處理，見下）
  2. 沒有主檔 → 框架預設（D4）
  3. `UnitSettings.Find(code)` 查不到 → 框架預設（D3）
  4. 查得到 → 該單位位數
- `RoundByKind`：`DecimalsSource.Unit` 且 `refCode` 空 → 原值返回（D2）。放在 `RoundByKind` 而不是 `ResolveDecimals`，因為後者必須回整數。
- XML doc：class summary（「falls back to the company decimals」）、分支註解、`RoundByKind` 兩個多載補
  「數量／重量沒有單位代碼時原值返回」；`ResolveDecimals`／`ResolveFormat` 的公司多載註明數量／重量回框架預設。

### 1b. 必綁檢查（D1）

- `FormExpressionCalculator.ApplyComputed`：結果要捨入的 `Quantity`／`Weight` 計算欄若 `UnitField` 空 →
  `InvalidOperationException`，訊息只帶 schema 內的名稱：`Field '{table}.{field}' is a {kind} field but has no UnitField.`
- `FormLiveComputation` 的 degrade 清單不含 `InvalidOperationException`（本幣必填時刻意如此），即時計算會把錯誤浮出來。
- `GridControl`／`NumericEdit` 與 `Bake` 不擲。

### 1c. `NumberFormatApplier.Bake`

`DecimalsSource.Unit` 一律 `continue`；刪掉「未綁單位退公司並 bake」的分支、行內註解與 remarks 對應段落。

### 1d. XML doc 同步

| 位置 | 改動 |
|------|------|
| `FormField.UnitField` | 「Empty falls back to the company decimals」→ 必填；以 `<see cref>` 指出由 `FormExpressionCalculator` 在計算時拒絕 |
| `RoundingContext.UnitSettings` | 沒有主檔時退**框架預設**，不是公司位數 |
| `CompanyInfo.NumberFormats`、`CompanyNumberFormats` | 拿掉「Quantity/Weight fallback when no unit is bound」 |
| `UnitSettings.GetDecimals` | 註明它是單純查表；`NumberFormatResolver` 對查不到的代碼改用該 kind 的框架預設 |
| `UnitItem.Decimals` | 順手修正：`ANDEC` 是**捨入**位數、顯示位數是 `DECAN`，而且這個值顯示與捨入共用 |

### 1e. 測試

**修改**：上方〈既有測試〉表標「改」的三支。

**新增**（純邏輯，`[Fact]`）：

1. `Quantity` 計算欄未綁 `UnitField` → `ApplyComputedRow` 擲 `InvalidOperationException`
2. 同上，`Weight`
3. 未綁的 `Quantity` **輸入欄**（非計算）→ `ApplyComputedRow` 不擲
4. 綁了、該列代碼空 → `RoundByKind` 原值返回（`Quantity` 與 `Weight`），**即使公司設了覆寫**
5. 綁了、代碼不在主檔 → `Weight` 回 3、`Quantity` 回 0，並依此捨入
6. 無主檔、公司設了 `Quantity` 覆寫 → 仍回框架預設（公司不參與的回歸護欄）
7. `Bake`：未綁的 `Quantity`／`Weight` 都不 bake
8. 公司多載 `RoundByKind(value, Quantity, company)` → 原值返回

### 1f. 範例

`NumberFormatModule`：

- `quantity`、`gross_weight` 各補一個單位欄（PCS、KG）並綁上，Grid 帶 `UnitSettings`（主檔建法比照 `MultiUnitModule`）；標題拿掉「(kg)」。
- 公司 B 拿掉 `Quantity` 覆寫；說明文字改為「數量／重量跟單位走、不隨公司變」。

### 1g. 文件

| 文件 | 改動 |
|------|------|
| [ADR-026](../adr/adr-026-numeric-semantics-rounding.md) | `## 修訂紀錄` 新增 `### 2026-09-xx：數量／重量必須綁定計量單位`：原則、D1–D5、與幣別對照的理由（公司有本幣、沒有預設單位）。原 D1 表格「無則退公司」保留為歷史，由修訂段說明已不成立。**連帶更正**：D5 寫的伺服端 bake 已移到前端 `FormDefinitionLoader` |
| `docs/development-cookbook.md` / `.zh-TW.md` | 英文 614、623、643–645 行附近，中文 591、620–622 行與〈交付時 bake〉那段：遞補鏈改寫、刪掉 614／591 那句過期說明、bake 位置更正。**兩份同步** |

跑 `./check-public-docs.sh`。CHANGELOG 於發版時由 `/dev-workflow:changelog-draft` 帶出，**必須列「升級須知」**：

1. 未綁 `UnitField` 的 `Quantity`／`Weight` **計算欄**：伺服端存檔與前端即時計算會擲例外。
2. 公司 `number_formats_xml` 裡的 `Quantity`／`Weight` 覆寫不再生效。
3. 綁了單位但該列單位代碼空：計算欄不再捨入（原本捨到公司位數或框架預設）。
4. 單位代碼不在主檔：由 0 位改為框架預設（`Weight` 為 3）。
5. 未綁單位的 `Quantity`／`Weight` 欄交付時不再 bake 公司位數格式，改顯示原值。

### 1h. 驗證與提交

- `dotnet build --configuration Release`（`Bee.Library.slnx`、含 DemoCenter 的 `Bee.Samples.slnx`、`Bee.Tools.slnx`）＋ `./test.sh` 全部。
- 沒有 SQL 或 provider 異動，精簡模式 CI 即可；push 前照規則詢問是否 `[all-db]`。
- commit 帶 pathspec；message 標 `!`，寫明「行為破壞、二進位相容、PublicAPI 無異動」（沒有簽章變更）。

## 階段 1 執行結果（2026-09-11）

### 範圍對帳

實際改動與 1a–1g 宣告一致，多出一檔：

- `src/Bee.Api.Client/Definitions/FormDefinitionLoader.cs`：呼叫 `Bake` 那段註解寫「綁了幣別或單位的欄位才不 bake」，
  改後所有數量／重量欄都不 bake，註解同步。只改註解。

另有兩處在宣告檔案內、但 1a–1g 沒有逐條列出：

- `FormExpressionCalculator.ApplyFieldExpressions`／`ApplyComputedRow` 補 `<exception cref="InvalidOperationException">`。
- cookbook〈Two rules that are easy to get wrong〉的 round-then-sum 那句原本教「以公司多載 `RoundByKind(value, kind, company)` 捨明細」，
  改後該多載對數量／重量原值返回，改為「金額與數量／重量用帶參照代碼的多載」；〈Units of measure〉那段一併更正
  `AmountColumnSummary` 的描述（Grid 沒有內建頁尾，由宿主接上）。

### 平行路徑

- 其他 head：全 `src/`、`tools/`、`apps/` 只有 `Bee.Db` 的 `DbDefineStorage` 與 `Bee.Api.Core` 的 wire 契約碰到 `UnitSettings`，
  都只負責存取與序列化，沒有自行解析位數。
- `UnitSettings.GetDecimals` 改完後在 `src/` 已無呼叫端；屬框架公開 API，保留。
- 其餘提到 `UnitSettings` 的公開文件（`caching`、`definition-files-overview`、`development-constraints`、
  `framework-capabilities`、`terminology`，雙語）沒有「退公司位數」的描述，不需改。

### 建置與測試

- `Bee.Library.slnx`、`samples/Bee.Samples.slnx`、`tools/Bee.Tools.slnx`：Release 0 警告 0 錯誤。
- `./test.sh` 全套（四個 DB 容器皆在跑）：17 個測試專案全數通過；唯一略過的
  `Login_WithRsaKeyPair_ReturnsDecryptableSessionKey` 為既有標記，與本次無關。
- 新增與改寫的解析器、`Bake`、計算器測試以 filter 單獨確認實際執行並通過。
- `./check-public-docs.sh`：只有規則已列的已知誤報。

### 相容性與未驗證項

- PublicAPI 無異動（沒有簽章變更）；行為破壞、二進位相容。
- DemoCenter 的 `NumberFormatModule` 只確認建置通過，**沒有啟動畫面驗證**。

## 階段 2：DefineEditor 設計期檢查

[`FormSchemaValidator.ValidateFields`](../../tools/DefineEditor/Services/FormSchemaValidator.cs) 比照
`ValidateRelation` 的寫法，新增一個檢查：

- `NumberKind` 為 `Quantity`／`Weight` 且 `UnitField` 空 → `Error`。
- `UnitField` 指名的欄不在**同一張** `FormTable` → `Error`。這是現行實作的限制（`ResolveRefCode` 只讀同一列的變數），不是設計原則。

測試位置比照既有 validator 測試（動工前先確認 DefineEditor 的測試專案）。

## 階段 2 執行結果（2026-09-11）

### 與上方原文不同之處

- **測試位置**：原文寫「比照既有 validator 測試」，但 DefineEditor 原本沒有任何測試專案。經使用者同意新建
  `tests/Bee.DefineEditor.UnitTests`（比照 `Bee.Cli.UnitTests`）並登錄到 `Bee.Library.slnx`，順帶讓 DefineEditor 進入 CI 建置
  （改動前所有 workflow 都不建置它）。
- **欄名比對用 Ordinal**：`FormExpressionCalculator.ResolveRefCode` 以宣告欄名、`StringComparer.Ordinal` 查單位欄，
  大小寫不同會被當成空單位代碼。因此 `UnitField` 是否存在的檢查用 Ordinal，不沿用同檔其餘檢查的 `OrdinalIgnoreCase`。

### 整合時踩到的問題

新測試專案第一次以 `Bee.Library.slnx` 建置時，DefineEditor 報 `CS1061`：`AppBuilder` 沒有 `WithDeveloperTools`。

- **根因**：MSBuild 建 solution 時，對不在 solution 內的 `ProjectReference` 預設拿掉 `Configuration`，被參考的專案退回
  Debug 編譯；restore 則以 Release 評估，DefineEditor 只在 Debug 帶入的 `AvaloniaUI.DiagnosticsSupport` 沒有編譯資產，
  `#if DEBUG` 裡的呼叫就編不過。
- **證據**：失敗那次寫入的是 `tools/DefineEditor/obj/Debug`，同時段 `obj/Release` 沒有更新。
- **修法**：新測試專案設 `ShouldUnsetParentConfigurationAndPlatform=false`（csproj 內附註解）。修正後以 CI 同一條
  `dotnet build Bee.Library.slnx --configuration Release --no-incremental` 建置成功，DefineEditor 改寫 `obj/Release`。

**連帶發現（未處理）**：

- `Bee.Cli.UnitTests`、`Bee.LoadTests.UnitTests` 帶進來的 `Bee.Cli`、`Bee.LoadTests` 在 library solution 裡同樣以 Debug 編譯
  （`./test.sh` 以 Release 建置時，更新的是兩者的 `obj/Debug`）。它們沒有依組態切換的套件，所以沒出錯。
  `docs/repo-ops/gotchas/test-ci-release.md` 記錄的「SonarCloud 沒分析到 `tools/**/*.cs`，機制不明」或許與此有關，**未查證**。
- `build-ci.yml` 的 `paths` 不含 `tools/**`：只改 DefineEditor 的 commit 仍不觸發 CI。

### 範圍對帳

與宣告一致：`tools/DefineEditor/Services/FormSchemaValidator.cs`、`tests/Bee.DefineEditor.UnitTests/`（csproj 與測試檔）、
`Bee.Library.slnx`，以及本 plan 與 `docs/plans/README.md`。`FormSchemaValidator` 的 class summary 原本寫「three classes」
這個清點數字並提到 plan，一併改為列出檢查項目名稱。

### 平行路徑

DefineEditor 與 `Bee.Cli` 內沒有其他檢查 `FormSchema` 的地方（另一個 `DatabaseSettingsValidator` 不碰欄位）。

### 建置與測試

- `Bee.Library.slnx`（`--no-incremental`，同 CI）與 `tools/Bee.Tools.slnx`：Release 0 警告 0 錯誤。
- `./test.sh` 全套：18 個測試專案全數通過（含新增的 `Bee.DefineEditor.UnitTests` 10 筆）；唯一略過的
  `Login_WithRsaKeyPair_ReturnsDecryptableSessionKey` 為既有標記。
- 新測試逐筆確認實際執行。「不報錯」與「報錯」兩類測試共用同一個篩選條件，後者抓得到錯誤，排除篩選條件本身失效而空轉。

## 不在範圍

- **手填數量不捨、不擋**（prior-art 第 1 點）：使用者 2026-09-11 決定先記錄。
- **單位換算／基準單位**（prior-art 第 5 點）。
- **幣別側的同形狀問題**：`CurrencyField` 參照是否存在沒有設計期檢查。
- **選單位後不一定重算**：相依圖只看運算式引用的欄，單位欄通常不在裡面。D2 改為不捨入後，這個風險從「捨錯位數」變成「選完單位後仍是未捨入的值」，存檔前的伺服端計算會以當時的單位重算。
- [plan-rounding-mode.md](plan-rounding-mode.md) 階段 2（數量／重量的方向政策）：同樣會改 `NumberFormatResolver`。
  兩份都要做的話，**先落本 plan**（它改的是遞補鏈），方向政策再疊上去。

## 與鐵人賽的關係

不碰 `docs/blogs/`。正文有沒有寫到「數量沒綁單位時退公司位數」**沒有查**；落地前向審稿那個 session 確認。
