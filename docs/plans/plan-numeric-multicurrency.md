# 計畫：多幣別數值（CurrencySettings + CUKY 綁定 + 現金捨入 + 本幣）

**狀態：📝 待做（2026-06-21）**

> 執行增量 2/3，相依 [plan-numeric-core.md](plan-numeric-core.md)。設計理由、SAP/Odoo 對照見 [plan-numeric-formatting.md](plan-numeric-formatting.md)（設計總覽 §1.3、§1.4、§1.5、§2.1–2.3、§3.2、§3.2b、§3.2c）。
> 本 plan 引入「**參照欄綁定**」通用機制（金額→幣別），計量單位 plan 將重用之。

## 接手指引（core 已完成，2026-07-01；務必沿用，勿重推導）

> **開場先讀**：本 plan + [plan-numeric-core.md](plan-numeric-core.md) 的三個「階段落地 note」+ 設計總覽對應節次。core 已 commit、CI 綠。以下是 core 已定、multicurrency 必須沿用的既成事實：

- **集合上 wire 一律 `MessagePackCollectionBase<T>`，不是 `KeyCollectionBase`**：`CompanyInfo` 走 `IEnterCompanyResponse.Company` MessagePack，而 `FormatterResolver` 只認 `MessagePackCollectionBase<>`。`CurrencySettings`/`CompanyCashRounding`/`CompanyAllowedCurrencies` 皆須此基底 + 在 `MessagePackCodec.Options` **顯式註冊** `CollectionBaseFormatter<TColl, TItem>`（否則沉默出空集合）。item 繼承 `MessagePackCollectionItem`、`[Key(100+)]`；鍵語意用線性 `FindX` 取代。core 的 `CompanyNumberFormats`/`NumberFormatItem` 是現成範本。
- **`DecimalsSource` 已是終版四值，`Currency` 已就位**：core 的 `NumberFormatResolver.ResolveDecimals` 對 `Currency`/`Unit` 目前**暫退 `company.GetDecimals`**。multicurrency 只需把「退 company」換成「依 refCode（CUKY 欄當前幣別）查 `CurrencySettings.GetDecimals`」——**在 `NumberFormatResolver`/`NumberFormatApplier` 內擴充，勿另起入口**。`RoundByKind` 亦在此加 `refCode` 參數。
- **`CompanyInfo` MessagePack 編號已保留**：core 只用了 `[Key(4)] NumberFormats`（型別 `CompanyNumberFormats`，屬性名 `NumberFormats` 非 `CompanyNumberFormats`）。`[Key(5)] DefaultCurrency` / `[Key(6)] CompanyCashRounding` / `[Key(7)] CompanyAllowedCurrencies` 依 §1.5 編號接續（尾端加 key 相容）。
- **新增 `_xml`（Text）欄一律 NOT NULL、補齊所有 hand-written INSERT 帶 `''`**：`default_currency`（String）走 NOT NULL 預設空字串；`cash_rounding_xml`/`allowed_currencies_xml`（Text）NOT NULL——MySQL TEXT 不能 DEFAULT，故 `SharedDatabaseState` seed 與各測試 helper 的 st_company INSERT 都要把新欄列進去（見 [database-dialect-differences.md](../database-dialect-differences.md) §3.2）。**別為了過 MySQL 改 nullable**。core 已把 st_company 的 hand-written INSERT 補齊 `number_formats_xml`，照同一批位置加即可。
- **`IDefineStorage` 雙模式新 define 的接點清單**（`CurrencySettings`）：`DefineType` 加成員、`IDefineStorage` File+Db 各實作 `Get/SaveCurrencySettings`、`ICacheContainer` 加 `CurrencySettingsCache`（仿 `ProgramSettingsCache`：File→檔案監控、Db→`st_cache_notify`）、`PathOptions.GetCurrencySettingsFilePath()`、`LocalDefineAccess` 對應方法、`Defaults/` scaffold。可參 `bee-add-cache-object` skill。
- **交付 bake 分工已定**：`NumberFormatApplier.Bake` 對 `Company`/`SystemFixed` 於交付時 bake；`Currency`/`Unit` **不 bake**（runtime 決定），只標記參照欄名。multicurrency 的 UI runtime 解析走 §3.2。
- **Grid 逐格格式化**：core 的 `CellValueFormatter` + `GridControl.FormatCell` 目前吃欄級固定 `NumberFormat`。multicurrency 需把 `FormatCell` 擴為帶 `NumberKind + CurrencyField + Resolver` 的逐格解析（§3.2c），`NumericEdit` 亦需 runtime 幣別重算。
- **驗證環境雷（core 已踩）**：本機持久 DB 容器以 `ALTER ADD` 加欄會強制 nullable，**遮蔽** CI fresh-CREATE 的 NOT NULL 行為；要驗 MySQL NOT NULL 需 `docker exec mysql8 ... ALTER TABLE common.st_company MODIFY <col> LONGTEXT NOT NULL` 重現。桌面環境走「直接改 main + 本機 build/test 通過再 push」，CI 仍會複驗。

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
