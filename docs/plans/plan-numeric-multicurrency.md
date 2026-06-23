# 計畫：多幣別數值（CurrencySettings + CUKY 綁定 + 現金捨入 + 本幣）

**狀態：📝 待做（2026-06-21）**

> 執行增量 2/3，相依 [plan-numeric-core.md](plan-numeric-core.md)。設計理由、SAP/Odoo 對照見 [plan-numeric-formatting.md](plan-numeric-formatting.md)（設計總覽 §1.3、§1.4、§1.5、§2.1–2.3、§3.2、§3.2b、§3.2c）。
> 本 plan 引入「**參照欄綁定**」通用機制（金額→幣別），計量單位 plan 將重用之。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 定義：`CurrencySettings`(define)、`FormSchema.CurrencyField` + `FormField.CurrencyField`、`CompanyInfo` 本幣/現金捨入/可用幣別 | 📝 待做 |
| 2 | 邏輯：金額依各 CUKY 欄 round-then-sum、兩層捨入（小數 + 現金捨入）、定義 ship client | 📝 待做 |
| 3 | UI：金額 runtime 幣別解析、Grid per-cell 幣別感知 + 混幣不合計、DemoCenter 多幣別 | 📝 待做 |

## 階段 1：定義（總覽 §1.3、§1.4、§1.5）

- **`CurrencySettings`**（系統層 define，類 SAP TCURX）：`CurrencyItem{Code(ISO 4217 alpha-3); Numeric; Rounding(幣別自然最小單位 0.01/1); Symbol; Name}`；`GetRounding`/`GetDecimals`（因子反推）。**持久化走 `IDefineStorage` 雙模式**（仿 `DbCategorySettings`：`DefineType.CurrencySettings`、`Get/SaveCurrencySettings`、`CurrencySettingsCache`、`PathOptions` 路徑、`LocalDefineAccess`、Defaults scaffold）。物件三棲。
- **`NumberKindProfile.GetDecimalsSource`** 擴出 `Currency`（Amount）。
- **CUKY 綁定**：`FormField.CurrencyField`（金額欄選填）、`FormSchema.CurrencyField`（主檔預設幣別欄，慣例 `sys_currency`）。**幣別解析優先序**：指定 `CurrencyField` → 主檔 `sys_currency` → 公司本幣 → 框架 2 位（明細跨表向上讀主檔）。
- **`CompanyInfo`** 加 `[Key(5)]` `DefaultCurrency`、`[Key(6)]` `CompanyCashRounding`（SAP T001R 式 per 公司×貨幣）、`[Key(7)]` `CompanyAllowedCurrencies`（per-company 可用幣別子集）；`st_company` 加 `default_currency`/`cash_rounding_xml`/`allowed_currencies_xml`；`CompanyRepository.GetById` 增列。
- 單據貨幣欄下拉選項 = `CompanyAllowedCurrencies`（空→全系統）。
- **測試**：`CurrencySettings` 命中/fallback/三棲；`CompanyCashRounding`/`CompanyAllowedCurrencies`；解析優先序；含 `CurrencyField` 的 FormSchema round-trip。

## 階段 2：邏輯（總覽 §2.1、§2.2、§2.3）

- 交付 bake：`Currency` 金額**不 bake**（runtime 決定），交付僅標記參照欄名。
- **捨入入口擴充** `RoundByKind(value, kind, refCode, ctx)`（ctx 加 `CurrencySettings`）：`Round`+金額用 `currencySettings.GetRounding(refCode)`（refCode = CUKY 欄幣別）。新增 `RoundCash(total, currencyCode, ctx)`：捨到 `company.GetCashRounding` 因子。
- **兩層捨入**：明細層金額捨幣別自然小數 + round-then-sum（交易/本幣各依自己貨幣）；最終層選配現金捨入、`diff = payable − total` 記 DIFF。
- **本幣金額**：`home_amount = RoundByKind(amount × rate, Amount, local_currency, ctx)`，`local_currency` 欄預設 `CompanyInfo.DefaultCurrency`。
- `CurrencySettings` ship 給 client。
- **測試**：USD vs JPY 位數不同 + round-then-sum 不變式；現金捨入 0.05 + diff；同列原幣/本幣不同幣；Preserve。

## 階段 3：UI（Avalonia，總覽 §3.2、§3.2c）

- **金額 runtime 幣別解析**：金額欄位數依解析出的幣別欄當前值 → client `CurrencySettings`；改幣別即重算綁到該欄的金額欄。
- **Grid per-cell 幣別感知**：`FormatCell` 由欄級字串擴為帶 `NumberKind + CurrencyField + Resolver`，逐格解析幣別（= SAP ALV `CFIELDNAME`）。**顯示**：金額格只顯數字 + 鄰欄獨立幣別欄。**混幣合計**：原幣欄全同幣才顯合計、混幣不顯；本幣欄恆可加總。
- DemoCenter：切單據貨幣（USD↔JPY）→ 金額即時 reformat。
- **測試**：清單同欄 USD/JPY/BHD 各位數正確；改主檔幣別全欄重算；混幣不顯/同幣顯/本幣恆顯合計。
- **交付標準**：build + 測試綠；進不同公司/改單據貨幣顯示正確；多幣別 DemoCenter 可跑。

## 相容性

- `FormSchema.CurrencyField`/`FormField.CurrencyField`、`st_company` 三新欄、`CompanyInfo` `[Key(5)]`~`[Key(7)]`：舊資料空 → 退公司本幣/框架 2 位、無現金捨入、白名單空＝全幣別。
- 新增 `DefineType.CurrencySettings`：無定義 → fallback 2 位。
