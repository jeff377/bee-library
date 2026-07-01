# 計畫：數值處理核心（NumberKind + 公司位數 + round-then-sum）

**狀態：🚧 進行中（2026-07-01）**

> 執行增量 1/3，最先做。設計理由、SAP/Odoo 對照見 [plan-numeric-formatting.md](plan-numeric-formatting.md)（設計總覽）。
> 本 plan 不含幣別/單位的「參照欄綁定」——金額暫走公司預設位數（單一幣別）、數量/重量走公司位數；多幣別見 [plan-numeric-multicurrency.md](plan-numeric-multicurrency.md)、計量單位見 [plan-numeric-uom.md](plan-numeric-uom.md)。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 定義模型：`NumberKind`/`RoundingPolicy`/`NumberKindProfile`、`CompanyNumberFormats`、`FormField`/`LayoutFieldBase` 帶 `NumberKind` | ✅ 已完成（2026-07-01） |
| 2 | 邏輯：交付時依公司 bake 顯示格式；BO round-then-sum（捨到公司/框架位數） | ✅ 已完成（2026-07-01） |
| 3 | UI（Avalonia）：`NumericEdit` + 基礎 Grid 格式化 + DemoCenter 範例 | 📝 待做 |

> **階段 2 落地note（2026-07-01）**：`NumberFormatResolver`（Bee.Definition）= `ResolveDecimals`/`ResolveFormat`/`RoundByKind`（Round=AwayFromZero、Preserve 原值、SystemFixed 忽略公司覆寫、Company/Currency/Unit 退 company/框架）。`NumberFormatApplier.Bake(FormSchema, CompanyInfo?)`（Bee.Definition.Forms）在 `SystemBusinessObject.LoadAndLocalizeSchema` 的 clone 上 bake `FormField.NumberFormat`（explicit 優先、None 略過、不 mutate 快取；`HasNumericField` 保留 anonymous 非數值 schema 免 clone 的最佳化）。cookbook 增「Numeric Semantics, Company Decimals, and Rounding」節。Definition 778 測試全綠；GetFormSchema/GetFormLayout round-trip 無回歸（EnterCompany 失敗為既有無-docker 環境性）。

> **階段 1 落地note（2026-07-01）**：`CompanyInfo` 屬性命名為 `NumberFormats`（型別 `CompanyNumberFormats`），非總覽字面的 `CompanyNumberFormats`（避免 `company.CompanyNumberFormats` stutter）；對外 API 是 `company.GetDecimals(NumberKind)`。MessagePack 須在 `MessagePackCodec.Options` 顯式註冊 `CollectionBaseFormatter<CompanyNumberFormats, NumberFormatItem>`（ContractlessStandardResolver 排在動態 `FormatterResolver` 前，否則集合沉默出空——已驗證並修）。build + Definition(759)/Api.Core 非 DB 測試全綠。CompanyRepository：`GetById_Enabled_*`（5 DB）加 `Assert.Empty(NumberFormats)` 覆蓋「空欄→空表反序列化」；**不另做 per-dialect 填值 round-trip DbFact**——此 fixture 的 repo 固定走 `DbCategoryIds.Common`（bound SQL Server），per-dialect insert 讀不到（見 SharedDatabaseState 說明），填值反序列化改由 `CompanyInfoTests` 的 XML round-trip 單元測試覆蓋。

## NumberKind 契約表（已拍板 2026-07-01，定案不再改）

| 成員 | 格式字母 | `RoundingPolicy` | `DecimalsSource`（終版） | 框架預設位數 | 用途 |
|------|:---:|:---:|:---:|:---:|------|
| `None` | — | — | — | — | 非語意/非數值 |
| `Quantity` | N | `Round` | `Unit`（無單位→`Company`） | 0 | 數量 |
| `Weight` | N | `Round` | `Unit`（無單位→`Company`） | 3 | 重量 |
| `Amount` | N | `Round` | `Currency`（無幣別→`Company`本幣→2） | 2 | 金額/稅/合計 |
| `Percent` | P | `Round` | `Company` | 2 | 百分比 |
| `UnitPrice` | N | `Preserve` | `Company`（位數僅顯示） | 4 | 單價 |
| `Cost` | N | `Preserve` | `Company`（位數僅顯示） | 4 | 成本 |
| `ExchangeRate` | N | `Preserve` | `SystemFixed` | 5 | 匯率 |

- `DecimalsSource` enum 從 core 即定**終版四值**（`Currency`/`Unit`/`SystemFixed`/`Company`）、`GetDecimalsSource()` 一次寫對永不改。
- **core 增量無 `CurrencySettings`/`UnitSettings`**：core 的 bake/round 邏輯對 `Currency`/`Unit` 兩源**暫退 `company.GetDecimals(kind)`**（用上表框架預設位數）；multicurrency/uom 增量才把退路換成真正的幣別/單位解析。**表不變，只有解析邏輯逐增量變厚**。
- `CompanyInfo` MessagePack key 不預留空屬性：core 只加 `[Key(4)] CompanyNumberFormats`，`[Key(5)]`~`[Key(7)]`（DefaultCurrency/CashRounding/AllowedCurrencies）編號已由總覽 §1.5 釘死、留 multicurrency 增量時才加（尾端加 key 相容）。

## 範圍與不做

- **做**：語意型別框架、公司層位數、捨入策略（Round/Preserve）、round-then-sum 鐵則、交付 bake、編輯控件。
- **涵蓋的 NumberKind**：`Quantity`/`Weight`（公司層）、`Percent`、`UnitPrice`/`Cost`（preserve）、`ExchangeRate`（系統固定）、`Amount`（公司預設位數、**單一幣別**）。
- **不做（留後續）**：`CurrencySettings`/`UnitSettings`、CUKY/UNIT 參照欄綁定、`DecimalsSource.Currency`/`Unit`、現金捨入、本幣、Grid 逐格 reference-aware。本 plan 的 `DecimalsSource` 只有 `Company` / `SystemFixed`。

## 階段 1：定義模型（對應總覽 §1.1、§1.2、§1.5、§1.6）

- `src/Bee.Definition/NumberKind.cs`：enum（`None`/`Quantity`/`Weight`/`Amount`/`Percent`/`UnitPrice`/`Cost`/`ExchangeRate`）。
- `NumberKindProfile`（取代孤立的 `NumberFormatPresets`，0 production caller、僅調其測試）：`GetFormatLetter`/`GetDefaultDecimals`/`GetRoundingPolicy`/`GetDecimalsSource`（本期僅回 `Company`/`SystemFixed`）/`BuildFormatString`。`RoundingPolicy` enum：`Round`/`Preserve`。
- `CompanyInfo` 加 `[Key(4)]` `CompanyNumberFormats`（集合 `NumberFormatItem{NumberKind Kind; int Decimals}`，**不可用 Dictionary**）、`int GetDecimals(NumberKind)`（覆寫 else 框架預設）。
  - **修正（2026-07-01 實作）**：基底用 **`MessagePackCollectionBase<NumberFormatItem>`** 而非總覽 §1.5 寫的 `KeyCollectionBase`——`CompanyInfo` 經 `IEnterCompanyResponse.Company` 走 MessagePack wire，而 `FormatterResolver`（src/Bee.Api.Core/MessagePack/FormatterResolver.cs:43-54）只認 `MessagePackCollectionBase<>`；`KeyCollectionBase` 會 fall 到 StandardResolver 無法乾淨序列化。`NumberFormatItem : MessagePackCollectionItem`（`[Key(100)]Kind`/`[Key(101)]Decimals`，仿 `SortField`）。鍵語意由 `FindDecimals(kind)` 線性查取代（覆寫表極小）。同樣修正套用到後續 `CurrencySettings`/`UnitSettings`（皆 ship client）。`st_company` 加 `number_formats_xml`（Text，`XmlCodec`，同 `session_user_xml` 慣例）；`CompanyRepository.GetById` SELECT 增列、反序列化。物件三棲。
- `FormField` 加 `NumberKind`（`[XmlAttribute]`、`[DefaultValue(None)]`）；`LayoutFieldBase` 加 `NumberKind`；`LayoutColumnFactory.ToField/ToColumn` 傳遞。`FormField.Clone()` 補 `NumberKind` + 修漏複製的 `ReadOnly`。
- **測試**：`NumberKindProfile` 各成員、`CompanyNumberFormats` 覆寫/fallback + 三棲 round-trip、`CompanyRepository` 解析、`FormField` 序列化/Clone、含 `NumberKind` 的 FormSchema XML round-trip。
- **交付標準**：build + UnitTests 綠；既有手填 `NumberFormat` 行為不變。

## 階段 2：邏輯（對應總覽 §2.1、§2.2、§2.4）

- **顯示 bake**：`SystemBusinessObject.LoadAndLocalizeSchema` clone 後套用——`Company`：`field.NumberFormat = BuildFormatString(kind, company.GetDecimals(kind))`；`SystemFixed`（匯率）：用框架預設位數。explicit `NumberFormat` 優先。不 mutate 快取（遵守 `definition-immutability`）。
- **捨入入口** `RoundByKind(value, kind, ctx)`：`Round` 用 `company.GetDecimals(kind)` 四捨五入（AwayFromZero）；`Preserve`（單價/成本/匯率）原值返回。
- **round-then-sum 鐵則**：明細各捨到位數再加總，`total = Σ 已捨入明細`；禁止全精度加總後才捨。
- DB 數值欄 `Decimal` 高 scale（如 8）作容量上限。cookbook 補「數值語意與捨入」節。
- **測試**：捨入邊界、round-then-sum 不變式（多筆 `10.333` → `Σ 已捨入 == 合計` 且 ≠ 全精度後捨）、Preserve 原值返回、bake 不污染快取。

## 階段 3：UI（Avalonia，對應總覽 §3.1、§3.3、§3.4）

- `NumericEdit`（`src/Bee.UI.Avalonia/Controls/Editors/NumericEdit.cs`）：focus 顯全精度、blur 依格式、culture parse、partial 容錯、右對齊、顯示捨入不回寫。與 `GridControl.FormatCell` 共用格式化。
- `ControlType` 加 `NumericEdit`；`FieldEditorFactory` 對數值型別 `Auto` → `NumericEdit`。
- 基礎 Grid：`FormatCell` 套交付 bake 的欄級 `NumberFormat`（**本期固定欄級格式，不逐格**）。
- DemoCenter：`Modules/Grids/NumberFormatModule.cs` + 註冊；展示各 NumberKind 不同位數、切換公司 → 位數變。
- **測試**：`NumericEdit` focus/blur/partial/不回寫；`FieldEditorFactory` 對應。
- **交付標準**：build + `Bee.UI.Avalonia.UnitTests` 綠；DemoCenter 可跑（切公司見效果）。

## 相容性

- `FormField`/`LayoutFieldBase` 加 `NumberKind`（`[DefaultValue(None)]`）、`st_company` 加 `number_formats_xml`、`CompanyInfo` 加 `[Key(4)]`：舊資料/XML 行為不變。
- 重構 `NumberFormatPresets` → `NumberKindProfile`：0 production caller。
