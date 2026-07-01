# 計畫：多幣別數值（CurrencySettings + CUKY 綁定 + 現金捨入 + 本幣）

**狀態：✅ 已完成（2026-07-01）**

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
| 1 | 定義：`CurrencySettings`(define)、`FormSchema.CurrencyField` + `FormField.CurrencyField`、`CompanyInfo` 本幣/現金捨入/可用幣別 | ✅ 已完成（2026-07-01） |
| 2 | 邏輯：金額依各 CUKY 欄 round-then-sum、兩層捨入（小數 + 現金捨入）、定義 ship client | ✅ 已完成（2026-07-01） |
| 3 | UI：金額 runtime 幣別解析、Grid per-cell 幣別感知 + 混幣不合計、DemoCenter 多幣別 | ✅ 已完成（2026-07-01） |

> **階段 3 落地 note（2026-07-01）**：`GridControl` 加 per-instance `CurrencySettings` + `DefaultCurrencyCode` 屬性（null＝關閉幣別感知、退欄級 baked 格式，向後相容）；新增 `FormatCellForColumn`（金額欄逐格：讀 `row.Row[column.CurrencyField]`＝該列 CUKY 值 → `NumberFormatResolver.ResolveFormat(Amount, ctx, code)`；空退 `DefaultCurrencyCode`），把三個顯示呼叫點（list-mode 單欄、interactive read-only、swap-cell ShowDisplay）改走它；`FormatCell` 靜態版保留給 composite/bool/lookup。改幣別＝host 更新值 + 既有 `RefreshRows()`。`NumericEdit` 同加 `CurrencySettings`+`DefaultCurrencyCode`：Amount 欄 runtime 由 `Binder.TargetRow[CurrencyField]`（detail）或 `DefaultCurrencyCode`（master，host 驅動）解析，null＝維持原 baked 行為。**混幣合計走 helper 非 GridControl footer**（依 scope 決策，現有 grid 無 footer 基礎設施）：新增 `AmountColumnSummary.TryComputeTotal`（全同幣回合計、混幣回 null；本幣欄欄內同幣＝恆合計）。DemoCenter 加 `MultiCurrencyModule`（原幣欄綁 sys_currency、本幣欄綁 local_currency、單據貨幣 USD↔JPY 切換即 reformat、手提 footer 用 helper）並註冊。測試（Avalonia +12）：per-cell USD/JPY/BHD 位數、列幣別空退預設、無 CurrencySettings 退 baked、幣別勝 baked；NumericEdit 依幣別；AmountColumnSummary 混幣/同幣/本幣/大小寫。全 15 test 專案綠（Avalonia 314、Definition 831）。samples 不觸發 CI，`-c Debug` 目視。

## ✅ 三階段全數完成

多幣別增量 2/3 完成。`CurrencySettings`(define)、per-field CUKY、兩層捨入、本幣、Grid per-cell 幣別感知、混幣合計 helper、DemoCenter、cookbook「Multi-currency」節皆落地。下一增量：計量單位（[plan-numeric-uom.md](plan-numeric-uom.md)），重用本增量的 reference-binding 機制（`CurrencyField` → `UnitField`）。

> **階段 2 落地 note（2026-07-01）**：resolver 走「**擴既有入口、不另起**」——`RoundingContext{Company, CurrencySettings}`（uom 增量再加 UnitSettings），`NumberFormatResolver` 加 currency-aware overload `ResolveDecimals/ResolveFormat/RoundByKind(kind, ctx, refCode)`：Currency 源以 refCode（CUKY 欄幣別）查 `CurrencySettings.GetDecimals`，refCode 空退公司 `DefaultCurrency`、再退框架 2；新增 `RoundCash(total, currencyCode, ctx)`（捨到 `company.GetCashRounding` 因子最近倍數、AwayFromZero，caller 記 `diff=payable−total`）。core 的 company-only overload `(kind, company)` 保留為 back-compat（委派 `RoundingContext.ForCompany`），既有呼叫零改。`NumberFormatApplier.Bake` 改：**Currency 源（Amount）不 bake NumberFormat**（runtime 由 UI 依幣別解析），改把 `schema.CurrencyField`（主檔 sys_currency）stamp 到無 `CurrencyField` 的金額欄（＝「解析後的參照欄名」，供 §3.2c UI）；Company/SystemFixed 仍 bake。**ship client 零新碼**：`CurrencySettings` 未被 `SystemBusinessObject.GetDefine` gating（僅擋 SystemSettings/DatabaseSettings），client 走既有 `GetDefineAsync<CurrencySettings>(DefineType.CurrencySettings)`（XML 傳）即得。**修正 stage 1 遺漏**：`GetCurrencySettings` 改 optional（File→File.Exists 檢查回 null、Db→ReadOptional），對齊 compat「無定義→fallback 2 位」。無既有 order BO，故階段 2＝擴 resolver+Bake+cookbook+測試（20 個新測；含 USD/JPY/BHD 位數、round-then-sum 不變式、現金捨入 0.05+diff、本幣、Preserve、fixture 全鏈 GetCurrencySettings）。cookbook 增「Multi-currency」節。全 15 test 專案綠（Definition 831）。

> **階段 1 落地 note（2026-07-01）**：CurrencySettings 採**集合衍生**（`CurrencySettings : MessagePackCollectionBase<CurrencyItem>`，`[XmlRoot("CurrencySettings")]` 讓 XML/DB 存檔根節點可讀；`CurrencyItem : MessagePackCollectionItem` `[Key(100+)]` Code/Numeric/Rounding/Symbol/Name 五欄；`GetRounding`/`GetDecimals`（`DecimalsFromRounding` decimal-safe 迴圈反推）/`Find`）。IDefineStorage 雙模式 12 接點仿 `DbCategorySettings`（`DefineType.CurrencySettings`+`DefineTypeExtensions`、File/Db storage、`PathOptions.GetCurrencySettingsFilePath`、`CurrencySettingsCache`、`ICacheContainer`+`CacheContainerService`、`CacheDefineAccess` 分派、`Defaults/CurrencySettings.xml` curated 10 幣別含 BHD 3 位）；**`IDefineAccess.Get/SaveCurrencySettings` 用 default 介面方法**（仿 `PermissionModels` 委派 `GetDefine`/`SaveDefine`，省 ~10 個 stub churn），`CacheDefineAccess` 覆寫走快取路徑。`CompanyInfo` 尾端加 `[Key(5)]DefaultCurrency`/`[Key(6)]CashRounding`(`CompanyCashRounding`)/`[Key(7)]AllowedCurrencies`(`CompanyAllowedCurrencies`)+`GetCashRounding`/`GetAllowedCurrencies`；三個新集合各在 `MessagePackCodec.Options` 註冊 `CollectionBaseFormatter`。`st_company` 加 `default_currency`(String NOT NULL)/`cash_rounding_xml`/`allowed_currencies_xml`(Text NOT NULL)，同步 Defaults+tests/Define TableSchema、`CompanyRepository.GetById` SELECT+反序列化、SharedDatabaseState+4 個測試 helper INSERT 補齊帶 `''`。`FormField.CurrencyField`+`FormSchema.CurrencyField`（僅 CurrencyField，UnitField 留 uom 增量）+`LayoutFieldBase.CurrencyField`+`LayoutColumnFactory` 傳遞+`FormField.Clone`。全 15 test 專案綠（Definition 811、Api.Core 312）。**環境雷**：加 NOT NULL 欄觸發持久容器 st_company **table rebuild**，暴露 core `number_formats_xml` 本機 ALTER-ADD 遺留的 nullable-drift（rebuild 重申 NOT NULL 撞既有 NULL 列）→ `SharedDatabaseState` setup skip → EnterCompany 測試連帶失敗；解法：`DROP TABLE st_company`（4 容器）讓 fixture fresh 重建，CI fresh-CREATE 無此問題（見 [[tableschema-addcolumn-allownull]]）。

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
