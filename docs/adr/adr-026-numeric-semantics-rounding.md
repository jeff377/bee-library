# ADR-026：數值語意、公司/貨幣/單位位數與 round-then-sum

## 狀態

已採納（2026-07-01）

## 背景

ERP 數值（單價、成本、數量、重量、金額、百分比、匯率）各有不同的小數位數、顯示格式與捨入規則，且位數來源不一致：非貨幣類每公司可自訂、金額跟貨幣走（JPY=0 / USD=2 / BHD=3）、數量/重量跟計量單位走（KG=3 / PCS=0）、匯率是系統固定的中間換算因子。實作前的現況把這些需求散落在互不連動的載體：

| 概念 | 現況載體 | 缺口 |
|------|---------|------|
| 儲存精度 | `DbField.Precision/Scale` | 與顯示/計算/貨幣無連動 |
| 顯示格式 | `FormField.NumberFormat` → `LayoutColumnFactory` → `GridControl.FormatCell` | 每欄手填、非公司/貨幣/單位感知 |
| 語意 preset | `NumberFormatPresets` | 孤立、0 production caller |
| 計算捨入 | 無 | 無統一入口 |

沒有統一的數值語意，導致三個 ERP 級別的正確性問題無處著力：**合計 ≠ 明細加總**（浮動精度或全精度加總後才捨）、**對來源值誤捨**（把單價/匯率捨到顯示位數注入下游誤差）、**跨 provider 儲存精度不一致**（多餘小數丟給 DB 引擎，SQLite 不強制 scale、其餘 provider 四捨五入，同筆資料存出不同精度）。

此設計借鏡 SAP ECC/S4（CURR/CUKY、QUAN/UNIT、TCURX、T006、T001R、逐行捨入）與 Odoo（`res_currency` decimal_places、`float_round`、`round_per_line`），並依本框架 FormSchema-driven 架構簡化。它橫跨定義層（`Bee.Definition`）、商業邏輯層（`Bee.Business`）、資料存取層（`Bee.Repository`）與 UI 層（`Bee.UI.Avalonia`），是框架對外 API surface 的結構性契約，故立此 ADR。完整設計與 SAP/Odoo 出處見已封存的設計總覽（`plan-numeric-formatting` 系列）；本 ADR 收斂「為何如此」與拒絕的替代方案，供 cookbook 引用。

## 考慮過的選項

以下逐一列出關鍵決策點上「看似合理但被否決」的替代方案（採納方案見下節）。

1. **全精度加總後一次捨入**（`total = round(Σ 全精度明細)`）：直覺、少一次逐筆捨入。**否決**——ERP 憑證要求「表頭合計逐字等於明細欄加總」，全精度重算會讓 `total ≠ Σ 已顯示明細`，對不上帳。SAP SD pricing、Odoo `round_per_line` 皆採逐行捨入正是此故。

2. **對單價/成本/匯率依「建議顯示位數」捨入後儲存**：讓資料乾淨、位數一致。**否決**——這三類是計算來源，捨到顯示位數會把誤差注入所有下游計算（金額 = 數量 × 單價）。顯示位數純為呈現，非儲存邊界；來源值必須以輸入精度原樣保存。

3. **金額顯示格式於交付時一併 bake**（與百分比/匯率同路徑，交付時就把 `NumberFormat` 寫死）：一致、client 端零解析。**否決**——金額位數取決於**單據貨幣**，而貨幣是**會變動的單據資料**（使用者在 UI 改幣別、明細列各自不同幣）。交付時的公司 session 固定，但貨幣不固定；把 `N2` 寫死會在切幣別時失準。金額/單位位數必須 runtime 解析。

4. **per-company / per-currency 調整 DB 欄位 scale**（讓儲存精度貼齊業務位數）：省儲存空間、schema 語意精確。**否決**——會導致每加一間公司或一種貨幣就要 `ALTER TABLE`，運維不可行。DB scale 應是與顯示/計算正交的單一高容量上限。

5. **公司覆寫表用 `Dictionary<NumberKind,int>`**：語意直觀。**否決**——`XmlSerializer` 無法乾淨序列化 `Dictionary`，且違反 `definition-collection-convention`（定義層集合一律走 `KeyCollectionBase`）。改用鍵值集合。

6. **沿用 `NumberFormatPresets` 靜態表**：不動既有碼。**否決**——它是孤立、非語意驅動、0 production caller 的格式字串表，無法承載捨入策略與位數來源。重構為 `NumberKind` enum + `NumberKindProfile`。

## 決策

採「以 `NumberKind` 語意欄為中樞，位數來源四分、捨入兩層、儲存精度正交」的整體設計。六項核心決策：

- **D1 — `NumberKind` 語意欄驅動三件事**：`FormField`（傳遞至 `LayoutFieldBase`）帶 `NumberKind`，決定 (a) 顯示格式種類（`N`/`P`）、(b) 是否於寫入時捨入（`Round` vs `Preserve`）、(c) 位數來源。成員與框架預設是簽核契約：

  | `NumberKind` | 捨入策略 | 位數來源 | 框架預設 |
  |-------------|---------|---------|:-------:|
  | `Quantity` / `Weight` | `Round` | 計量單位（綁 `UnitField`；無則退公司） | 0 / 3 |
  | `Amount` | `Round` | 貨幣（綁 `CurrencyField`；無則退主檔/公司） | 2 |
  | `Percent` | `Round` | 公司 × Kind | 2 |
  | `UnitPrice` / `Cost` | `Preserve` | 公司（僅顯示） | 4 |
  | `ExchangeRate` | `Preserve` | 系統固定 | 5 |

- **D2 — round-then-sum（ERP 鐵則）**：`Round` 類的合計 = **已捨入明細值之和**，絕不全精度重算。每筆明細先以 `RoundByKind` 捨到其位數再加總，由構造保證 `Σ 明細 == 合計`。原幣/本幣各依自己的貨幣鍵欄獨立 round-then-sum。

- **D3 — 兩層捨入分離**：明細層捨到**幣別自然小數 / 單位小數**（系統層 `CurrencySettings` / `UnitSettings`）；單據最終層再選配套用**現金捨入單位**（公司可覆寫，SAP T001R 式，如 CHF→0.05），只作用於最終應付額、刻意產生捨入差額（記 DIFF 科目）。幣別小數永遠系統層、現金捨入單位可公司層，兩者不混淆。

- **D4 — `Preserve` 永不回寫捨入值**：`UnitPrice` / `Cost` / `ExchangeRate` 以輸入精度原樣保存，位數僅供顯示（顯示捨入不回寫綁定值）。`RoundByKind` 對這些 kind 原值返回。唯一硬邊界是 DB scale 容量上限（見 D6）。

- **D5 — 非貨幣類交付時 bake、貨幣/單位 runtime 解析**：
  - 公司位數（`Percent`、單價/成本）與系統固定（匯率）於 `SystemBusinessObject.LoadAndLocalizeSchema` 的 **per-call clone** 上 bake（`NumberFormatApplier.Bake` 寫入 `FormField.NumberFormat`）；作者手填的 `NumberFormat` 永遠優先；快取 schema 絕不 mutate。
  - `Amount`（跟貨幣）與綁了 `UnitField` 的 `Quantity`/`Weight`（跟單位）**不 bake**——交付時只標記參照欄名，位數依該欄當前值 runtime 解析（UI 改幣別/單位即重算；BO 依單據貨幣/單位捨入）。採 SAP per-field CUKY/UNIT：金額欄綁 `FormField.CurrencyField`（未指定退主檔 `sys_currency` → 公司 `DefaultCurrency` → 框架 2）、數量/重量欄綁 `FormField.UnitField`。

- **D6 — DB scale 是容量天花板，與顯示/計算正交**：數值欄用 `Decimal` + 框架統一高 scale（如 8），無 per-company/per-currency `ALTER`。顯示位數（`NumberFormat`）與計算位數（`RoundByKind`）與 DB scale 無關。API 匯入超過 scale 時於 Repository 寫入層**顯式** `decimal.Round(value, DbField.Scale, AwayFromZero)`（不可依賴 DB 隱式轉換——跨 provider 不一致）；此為儲存容量物理截斷，非業務捨入，且 scale 遠超業務意義，不抵觸 D4。

## 影響

- **正確性**：合計恆等於明細加總（D2）；來源值零誤差傳播（D4）；跨 provider 儲存精度一致（D6）。
- **多租戶/多幣別**：同一 schema 交付給兩間公司可帶不同格式（`Percent` P2 vs P4）；同一單據原幣/本幣、同一欄不同列可不同幣別/單位，位數各自 runtime 解析。
- **相容性（既有資料不需遷移）**：`FormField`/`LayoutFieldBase` 加 `NumberKind`、`FormSchema` 加 `CurrencyField`，皆 `[DefaultValue]` 空 → 既有 XML 反序列化不變；`st_company` 新增四欄（`number_formats_xml`/`default_currency`/`cash_rounding_xml`/`allowed_currencies_xml`）與 `CompanyInfo` `[Key(4)]`~`[Key(7)]`，舊資料欄空即全退框架預設；MessagePack 尾端加 key 相容。
- **新定義型別**：`DefineType.CurrencySettings`（TCURX 式，系統層 ISO 4217 curated 表）與 `DefineType.UnitSettings`（T006 式），走既有 `IDefineStorage` 雙模式（檔案/`st_define`）+ 三棲序列化 + 隨 `GetDefine` ship 給 client；無定義則各自 fallback。
- **後續規範（新增數值欄時）**：宣告語意欄一律設 `NumberKind`；金額欄視需要綁 `CurrencyField`（原幣可省，走主檔 `sys_currency`）、數量/重量欄綁 `UnitField`；BO 計算一律 `decimal` 且走 `RoundByKind` round-then-sum，禁止全精度加總後才捨、禁止對 `Preserve` 類捨入。
- **未做（未來項）**：匯率 factor（TCURF）、price unit（KPEIN）、header DIFF 捨入差吸收、Maui/Blazor `NumericEdit` 移植。

## 參考

- cookbook：`docs/development-cookbook.md` §Numeric Semantics, Company Decimals, and Rounding（how-to 與 API 入口）
- 相關 ADR：[ADR-005](adr-005-formschema-driven.md)（FormSchema 驅動）、[ADR-012](adr-012-session-company-context.md)（session 公司上下文）、[ADR-017](adr-017-db-cache-invalidation.md)（cache 失效）
- 記憶：`erp-round-then-sum`、`db-param-scale-not-enforced`
- SAP：ABAP CURR/QUAN 必綁 CUKY/UNIT、ALV `CFIELDNAME`、幣別小數 TCURX、單位小數 T006（ANDEC/DECAN）、逐行捨入/現金捨入 T001R
- Odoo：`res_currency`（decimal_places/rounding）、`float_round`、稅務 `round_per_line`（預設）vs `round_globally`
