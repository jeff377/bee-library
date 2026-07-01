# 設計總覽：ERP 數值處理（NumberKind + 公司位數 + 多幣別 CUKY + 計量單位 UNIT）

**狀態：📐 設計總覽 / 決策紀錄（2026-06-21）** — 本文件不直接執行，保存完整設計、SAP/Odoo 研究與決策理由。**執行拆為 3 個垂直增量 plan**（依序交付）：

| 執行 plan | 範圍 | 相依 | 狀態 |
|-----------|------|------|------|
| [plan-numeric-core.md](plan-numeric-core.md) | **核心（其他類）**：`NumberKind`/`RoundingPolicy`/`NumberKindProfile`、`CompanyNumberFormats`（公司位數）、round-then-sum、交付 bake、`NumericEdit`、基礎 Grid。涵蓋數量/重量（公司層）、Percent、單價/成本（preserve）、匯率（系統固定）、金額（公司預設、單一幣別） | — | ✅ 已完成（2026-07-01） |
| [plan-numeric-multicurrency.md](plan-numeric-multicurrency.md) | **幣別**：`CurrencySettings`(TCURX)、per-field CUKY 綁定、本幣/現金捨入(T001R)/可用幣別、reference-binding 機制、Grid 幣別感知 + 混幣不合計 | core | ✅ 已完成（2026-07-01） |
| [plan-numeric-uom.md](plan-numeric-uom.md) | **計量單位**：`UnitSettings`(T006)、per-field UNIT 綁定（重用 reference-binding）、Grid 單位感知 + 混單位不合計 | multicurrency | ✅ 已完成（2026-07-01） |

> 下方 §1–§3 為**完整設計細節**（三層展開），3 個執行 plan 是其垂直切片並回引本文件對應節次與「背景 / SAP·Odoo 對照 / 核心設計原則」的決策理由。

## 背景

ERP 數值（單價、成本、數量、重量、金額、百分比、匯率）各有不同小數位數、顯示格式與捨入規則，且：
- **小數位數由公司層級自訂**（非貨幣類：每公司業務所需不一致）；
- **金額小數跟貨幣走**（多幣別：JPY=0 / USD=2 / BHD=3，類 SAP TCURX）。

### SAP 對照（設計依據）

研究 SAP ECC/S4 後採納三點核心思路（出處見文末對照），並依本框架簡化：

| SAP 機制 | 我們的對應 | 取捨 |
|---------|-----------|------|
| CURR 欄必綁貨幣鍵 CUKY、小數 runtime 由貨幣解析（TCURX） | **每個金額欄綁自己的貨幣鍵欄（per-field CUKY）**，同列原幣/本幣可不同幣 | 採 SAP 逐欄 CUKY；`FormSchema.CurrencyField` 僅作未指定時的預設 |
| QUAN 欄必綁單位 UNIT、小數由計量單位解析（T006）；重量/數量同一家族 | **數量/重量欄綁單位欄（per-field UNIT）**，位數由 `UnitSettings`；重量＝QUAN 家族成員 | 採 SAP QUAN/UNIT，與 CURR/CUKY 平行；重用同一 per-field 綁定機制 |
| 逐行捨入 → 加總，差異用 header DIFF 吸收 | **round-then-sum**，`total = Σ 已捨入明細` 由構造保證明細加總=總合 | 我們更嚴格；header 層獨立計算（整單折讓等）才需未來的 DIFF 機制 |
| 內部高精度計算、最後依貨幣小數呈現 | 不捨入類保高精度、金額算出才捨到貨幣小數 | 一致 |
| price unit（KPEIN）放大分母避免單價塞小數 | 單價走 **preserve**（高精度不捨入） | 採較簡的 preserve，不引入 price-unit 語意 |
| 匯率 factor（TCURF）處理量級 | 匯率 preserve 高精度（factor 列**未來項**） | 本期不做 factor |
| 捨入單位 T001R（per 公司碼+貨幣，與 TCURX 小數位拆兩層） | **採 SAP 式拆兩層**：幣別小數系統層（`CurrencySettings`）+ 現金捨入單位公司可覆寫（`CompanyCashRounding`） | 小數系統、現金捨入公司，對齊 SAP；最終層套用、產生 DIFF |

> 另對照 **Odoo**：`decimal.precision` 用途類別 ≈ 我們的 `NumberKind`（驗證方向）；`res.currency.rounding` 是單因子（小數+捨入合一）——我們改採 **SAP 式拆兩層**（小數系統 / 現金捨入公司），但沿用其 `float_round(precision_rounding=factor)` 機制做「捨到 0.05 倍數」；底層 float + 容差比較（我們用 `decimal` 規避其陷阱）；`tax_calculation_rounding_method` 預設 `round_per_line` = 我們的 round-then-sum。

### 核心設計原則

1. **兩類數值，捨入策略不同**：
   - **不捨入類（單價、成本、匯率）**：以輸入精度原樣保存，框架**不套任何捨入**；位數僅供顯示格式化（顯示捨入不回寫）。對來源值捨入會把誤差注入下游，禁止。
     - **API 匯入時超過「建議顯示位數」不需處理**——原樣保存正是 preserve 本意；依建議位數捨入反而注入下游誤差（違反本則）。建議位數純顯示，非儲存邊界。
     - **唯一硬邊界是 DB scale 容量上限**（§2.4，`Scale=8`）：傳入小數位超過 scale → 於持久化邊界**靜默捨到 scale（AwayFromZero）**，不報錯、不中斷匯入、不另記 warning。此屬「儲存容量物理截斷」而非「業務/顯示捨入」，且 scale 遠超業務意義，不抵觸本則。
   - **四捨五入類（數量、重量、金額、百分比）**：寫入欄位時 AwayFromZero 捨到該欄位位數。
2. **Round-then-sum（ERP 鐵則）+ 兩層捨入**：四捨五入類的合計 = **已捨入明細值之和**，不全精度重算（明細各捨到幣別自然小數再加總 → 明細加總 = 總合）。**單據最終**再選配**現金捨入單位**（公司可覆寫，SAP T001R 式，如捨 0.05），只套最終應付額、刻意產生捨入差額（記 DIFF 科目）。**幣別小數永遠系統層、現金捨入單位可公司層**，兩者分離。
3. **四個小數位數來源**（SAP 兩條「數值＋參照欄」家族 + 系統 + 公司）：
   - **貨幣（金額 `Amount`）**：綁**幣別欄**（SAP CURR→CUKY），位數由幣別（`CurrencySettings`/TCURX）。優先序：指定 `CurrencyField` → 主檔 `sys_currency` → 公司本幣（見 §1.4）；同列原幣/本幣可不同幣。
   - **計量單位（數量 `Quantity`/重量 `Weight`）**：綁**單位欄**（SAP QUAN→UNIT），位數由單位（`UnitSettings`/T006）；同欄不同列可不同單位（如某列 KG 3 位、某列 PCS 0 位）。無綁單位 → 退公司層 fallback。
   - **系統固定（匯率 `ExchangeRate`）**：單一框架位數（不分公司/貨幣對），對齊 SAP/Odoo——匯率是換算中間因子、無 ISO 法定約束。
   - **公司 × NumberKind（百分比 + 單價/成本 + 數量/重量無單位時 fallback）**：`CompanyInfo.NumberFormats`。單價/成本為不捨入類，公司位數僅供顯示。
4. **公司本幣（預設幣別）**：每公司有一個預設幣別，用途三：(a) **本幣欄（`local_currency`）的預設值**（金額綁該欄取得本幣別）；(b) 外幣**換算本幣**的目標幣別（`本幣金額 = round(原幣金額 × 匯率, 本幣小數)`）；(c) 金額欄無 CUKY 欄時的 fallback。對應 SAP company code local currency（DMBTR）vs document currency（WRBTR）。
5. **位數 ≠ DB 儲存精度**：DB scale 固定高精度作容量上限（如 8，≥ 任一貨幣/公司位數與不捨入類保存精度）。
6. **計算一律 `decimal`，禁用 `double`**；**顯示捨入絕不回寫**綁定值。
7. **解析時序**：公司位數 session 固定 → 交付時 bake；**貨幣位數是單據資料、會變動 → runtime 解析**（UI 改幣別即重算、BO 依單據貨幣捨入）。

### 現況載體

| 概念 | 現況 | 缺口 |
|------|------|------|
| 儲存精度 | `DbField.Precision/Scale`（[DbField.cs:90-113](src/Bee.Definition/Database/DbField.cs)） | 與顯示/計算/貨幣無連動 |
| 顯示格式 | `FormField.DisplayFormat/NumberFormat`（[FormField.cs:131-147](src/Bee.Definition/Forms/FormField.cs)）→ `LayoutColumnFactory`（[LayoutColumnFactory.cs:22-40](src/Bee.Definition/Layouts/LayoutColumnFactory.cs)）→ `GridControl.FormatCell`（[GridControl.cs:1004](src/Bee.UI.Avalonia/Controls/GridControl.cs)） | 每欄手填、非公司/非貨幣感知 |
| 語意 preset | `NumberFormatPresets`（[NumberFormatPresets.cs](src/Bee.Definition/NumberFormatPresets.cs)） | 孤立、0 production caller |
| 公司上下文 | `SessionInfo.CompanyId` → `ICompanyInfoService.Get` → `CompanyInfo`（[CompanyInfoService.cs:43](src/Bee.ObjectCaching/Services/CompanyInfoService.cs)） | 無數值設定欄 |
| 交付時 clone | `SystemBusinessObject.LoadAndLocalizeSchema`（[SystemBusinessObject.cs:456](src/Bee.Business/System/SystemBusinessObject.cs)）：Clone 不 mutate 快取 | 尚無數值格式套用 |
| 貨幣 | （無貨幣定義 / 無金額↔貨幣綁定） | 全新 |
| 計算捨入 | 無 | 無統一入口 |
| 編輯期格式 | 數值欄全走 `TextEdit`，無 `NumericEdit` | 編輯不格式化 |

---

## 階段 1：定義模型

### 目標

建立 `NumberKind` 語意、捨入策略、**貨幣定義**、**單據貨幣綁定**、公司位數模型（縮為非貨幣類），並讓 `NumberKind` 帶到 layout。本階段只建模型、佈線，可獨立 build + test 且既有行為不變。

### 1.1 `NumberKind` enum + 捨入策略 + 位數來源

- 位置：`src/Bee.Definition/NumberKind.cs`。
- 成員與屬性：

  | NumberKind | 格式種類 | 捨入策略 | 位數來源 | 框架預設位數 | 用途 |
  |-----------|:---:|---------|---------|:---:|------|
  | `None` | — | — | — | — | 非語意 / 非數值 |
  | `Quantity` | N | 四捨五入（加總→先捨後加） | **計量單位**（綁單位欄 SAP UNIT；無單位→公司） | 0 | 數量 |
  | `Weight` | N | 四捨五入（加總→先捨後加） | **計量單位**（綁單位欄 SAP UNIT；無單位→公司） | 3 | 重量 |
  | `Amount` | N | **四捨五入（加總→先捨後加）** | **貨幣**（綁貨幣鍵欄 SAP CUKY；原幣→交易貨幣欄、本幣→本幣欄；fallback 公司本幣→2） | 2 | 金額/稅/合計 |
  | `Percent` | P | 四捨五入 | 公司 × Kind | 2 | 百分比 |
  | `UnitPrice` | N | **不捨入（位數僅顯示）** | 公司 × Kind | 4 | 單價 |
  | `Cost` | N | **不捨入（位數僅顯示）** | 公司 × Kind | 4 | 成本 |
  | `ExchangeRate` | N | **不捨入（位數僅顯示）** | **系統固定**（框架預設，不分公司/貨幣） | 5 | 匯率 |

  > 四種位數來源：
  > - **貨幣**（`Amount`）：金額欄綁貨幣鍵欄（SAP CUKY，per-field）；同列原幣/本幣可不同幣，位數依各欄當前幣別（`CurrencySettings`/TCURX）。
  > - **計量單位**（`Quantity`/`Weight`）：綁單位欄（SAP UNIT，per-field）；位數依各欄當前單位（`UnitSettings`/T006）；同欄不同列可不同單位。無綁單位 → 退公司層。
  > - **系統固定**（`ExchangeRate`）：單一框架位數 **5**（對齊 SAP `DEC 9,5` 與銀行牌告），不分公司/貨幣對。
  > - **公司 × Kind**：百分比 + 單價/成本（不捨入，公司位數僅顯示）+ 數量/重量無單位時 fallback。
  > 實際成員與預設值於實作時與使用者再確認。

### 1.2 `NumberKindProfile`（取代 `NumberFormatPresets`）

- 以 `NumberKind` 查框架預設（現況 0 production caller，僅調其測試）：
  ```
  char            GetFormatLetter(NumberKind kind)       // 'N' / 'P'
  int             GetDefaultDecimals(NumberKind kind)     // 框架預設（系統固定類直接用此值）
  RoundingPolicy  GetRoundingPolicy(NumberKind kind)     // Round / Preserve
  DecimalsSource  GetDecimalsSource(NumberKind kind)     // Currency / Unit / SystemFixed / Company
  string          BuildFormatString(NumberKind kind, int decimals)  // ('N',3)=>"N3"；('P',2)=>"P2"
  ```
  - `DecimalsSource` enum：`Currency`（綁幣別欄→`CurrencySettings`，Amount）／`Unit`（綁單位欄→`UnitSettings`，Quantity/Weight；無單位退 Company）／`SystemFixed`（框架預設，ExchangeRate）／`Company`（`CompanyInfo.NumberFormats`，Percent/單價/成本）。
- 格式字串由「種類 + 位數」動態組出，讓貨幣/系統/公司位數皆可帶入。

### 1.3 貨幣定義 `CurrencySettings`（系統層，幣別自然單位）

- 系統層級貨幣參考資料（普世一致、與公司無關，對齊 SAP TCURX 主鍵無 BUKRS）：`CurrencySettings` 為 `KeyCollectionBase` 衍生集合，item `CurrencyItem { string Code; string Numeric; decimal Rounding; string Symbol; string Name }`。
  - **`Code` = ISO 4217 alpha-3**（`USD`/`JPY`/`TWD`，業界標準，SAP/Odoo 皆採）；`Numeric` = ISO 4217 數字碼（`840`/`392`/`901`，選存）。此為部署的幣別主檔（curated，非全 180 種）。
  - **`Rounding` = 幣別自然最小單位**（ISO 4217 minor unit）：USD=`0.01`（2 位）、JPY=`1`（0 位）。**只表達「這個幣別本質的小數位/最小單位」**，不含公司現金捨入政策（那拆到 §1.5）。
  - `GetRounding(code)`：查到回該因子；查無回 fallback `0.01`。
  - `GetDecimals(code)`：由因子反推顯示位數——`0 < r < 1 ? ceil(log10(1/r)) : 0`（`0.01`→2、`1`→0），供 `BuildFormatString` 用。
- **持久化：走既有 `IDefineStorage` 可插拔抽象（仿 `DbCategorySettings` 雙模式）**——序列化一律 XML（`XmlCodec`），後端由部署 `SystemSettings.BackendConfiguration.Components.DefineStorage` 決定：`FileDefineStorage` → `{DefinePath}/CurrencySettings.xml`；`DbDefineStorage` → `st_define.content`（Text，XML 字串，主鍵 `(define_type, customize_id, define_key)`）。**不為 CurrencySettings 自選 DB/檔案**。
  - 不可仿 `SystemSettings`/`DatabaseSettings`（那是 bootstrap 級、純檔案不可放 DB）；CurrencySettings 非 bootstrap，比照 `ProgramSettings`/`DbCategorySettings`。
  - **單一實例 define**（`define_key="*"`）；Defaults scaffold 置 `src/Bee.Definition/Defaults/`（框架預設幣別表）。
  - 要接的點：`DefineType` 加成員（[DefineType.cs](src/Bee.Definition/DefineType.cs)）、`IDefineStorage` 的 `Get/SaveCurrencySettings`（File + Db 各實作）、`ICacheContainer` 加 `CurrencySettingsCache`（仿 [ProgramSettingsCache.cs](src/Bee.ObjectCaching/Define/ProgramSettingsCache.cs)：File→檔案監控、Db→`st_cache_notify`）、`PathOptions.GetCurrencySettingsFilePath()`、`LocalDefineAccess` 的 `Get/SaveCurrencySettings`。
  - 物件三棲（XML 落儲存 + MessagePack 走 wire 給 client，集合需註冊 `CollectionBaseFormatter`，見 `bee-serialization`）。
- **公司不覆寫幣別小數**（SAP TCURX / Odoo currency 皆 system-wide）；但**現金捨入單位可公司覆寫**（SAP T001R 式，見 §1.5）。

### 1.3b 計量單位定義 `UnitSettings`（系統層，SAP T006 式）

> 與 `CurrencySettings` 平行——SAP 的 QUAN/UNIT 家族鏡像 CURR/CUKY；數量/重量綁單位、位數由單位定。

- 系統層級單位參考資料：`UnitSettings` 為 `KeyCollectionBase` 衍生集合，item `UnitItem { string Code; int Decimals; string Dimension; string Name }`。
  - **`Code`** = 單位碼（`KG`/`G`/`TON`/`PCS`/`L`…，慣例 ISO/UN 單位碼或自訂）；**`Decimals`** = 該單位小數位（SAP T006 的 ANDEC/DECAN；簡化為單一 `Decimals`，KG=3、PCS=0…）；**`Dimension`** = 維度分組（weight/length/volume/count，選用，供 UI 分組）。
  - `GetDecimals(code)`：查到回該單位位數；查無回 fallback（框架預設，如 0）。
- **持久化、快取、序列化全比照 §1.3 `CurrencySettings`**：新增 `DefineType.UnitSettings`、`IDefineStorage` 的 `Get/SaveUnitSettings`、`UnitSettingsCache`、`PathOptions.GetUnitSettingsFilePath()`、`LocalDefineAccess` 對應方法；雙模式（檔案/DB st_define）、物件三棲、ship 給 client。Defaults scaffold 置框架預設單位表。
- **系統層、與公司無關**（對齊 SAP T006 client-wide）；位數綁單位、非綁公司。

### 1.4 數值↔參照欄綁定（SAP 雙家族：金額→幣別 CUKY、數量/重量→單位 UNIT）+ 解析優先序

> **金額不是全綁單一單據貨幣**——同一列可有原幣金額與本幣金額，幣別不同（SAP `WRBTR`→`WAERS` / `DMBTR`→`HWAER`）。每個金額欄各綁一個貨幣鍵欄（CUKY）。

- **`FormField.CurrencyField`**（金額欄，選填）：指明「持有**本欄**幣別碼的欄位名」（CUKY 參照）。
- **`FormSchema.CurrencyField`**（主檔，選填）：單據主檔的幣別欄，慣例為**系統欄 `sys_currency`**（sys_ 前綴），存 ISO alpha-3；為下拉，**選項 = 當前公司 `CompanyAllowedCurrencies`**（空則全系統 `CurrencySettings`）。

**金額欄幣別解析優先序（主檔／明細金額欄皆適用）**：
1. 金額欄**有指定** `FormField.CurrencyField` → 以該幣別欄的值為準。
2. **未指定** → 取**單據主檔幣別欄**（`FormSchema.CurrencyField`，`sys_currency`）的值；**明細金額欄也取主檔幣別**（跨表向上讀 master 列的 `sys_currency`）。
3. **主檔無幣別欄** → 取**公司預設幣別**（`CompanyInfo.DefaultCurrency`）。
4. 仍無 → 框架 fallback（2 位）。

- 慣例上**原幣金額不必設 `CurrencyField`**（走規則 2 → 主檔 `sys_currency`）；只有**需綁不同幣別的欄才設**——典型是本幣金額 `home_amount` 設 `CurrencyField=local_currency`（`local_currency` 欄預設值由 `CompanyInfo.DefaultCurrency` 帶入、通常唯讀）。本幣無特例，只是「綁到值＝公司本幣的貨幣欄」。
- 範例（明細列）：`amount`（原幣）**不設** `CurrencyField` → 取主檔 `sys_currency`；`home_amount`（本幣）`CurrencyField=local_currency` → 本幣。兩欄幣別不同、各自解析位數。
- 金額欄小數 runtime 解析：依上述優先序解出幣別碼 → `CurrencySettings.GetDecimals` → 位數。

**數量/重量↔單位綁定（SAP QUAN→UNIT，與上述平行）**：

- **`FormField.UnitField`**（`Quantity`/`Weight` 欄，選填）：指明「持有**本欄**計量單位碼的欄位名」（SAP UNIT 參照）。
- 解析優先序（單位通常 per-列、無「主檔單位」概念）：
  1. 數量/重量欄**有指定** `UnitField` → 該單位欄的值 → `UnitSettings.GetDecimals`。
  2. **未指定** → 退**公司層** `CompanyInfo.GetDecimals(kind)`（`CompanyNumberFormats` else 框架預設）。
- 範例（明細列）：`order_qty` 設 `UnitField=qty_uom`（值 `PCS`→0 位）、`gross_weight` 設 `UnitField=weight_uom`（值 `KG`→3 位）。同一數量欄不同列可不同單位 → 位數不同。
- runtime 解析：解出單位碼 → `UnitSettings.GetDecimals` → 位數（與幣別同一套 per-cell 機制，§3.2c）。

### 1.5 公司層模型：`CompanyInfo`（非貨幣位數 + 本幣 + 現金捨入覆寫 + 可用幣別）

- **`st_company`** 新增四欄，同步 Defaults 與 `tests/Define` 兩份 TableSchema：
  - `number_formats_xml`（`DbType="Text"`，`CompanyNumberFormats` 的 **XML 序列化**——與 `session_user_xml` 同慣例，`XmlCodec`、`_xml` 後綴；先例 [SessionRepository.cs:39,86](src/Bee.Repository/System/SessionRepository.cs)）。
  - `default_currency`（`DbType="String"` Length 譬如 5，公司本幣幣別碼，對應 `CurrencySettings` 的 `Code`）。
  - `cash_rounding_xml`（`DbType="Text"`，`CompanyCashRounding` 的 XML 序列化）。
  - `allowed_currencies_xml`（`DbType="Text"`，`CompanyAllowedCurrencies` 的 XML 序列化）。
- **`CompanyNumberFormats`**：覆寫表用**鍵值集合**（`NumberFormatItem { NumberKind Kind; int Decimals }` + `KeyCollectionBase`，不可用 `Dictionary`——XmlSerializer 無法乾淨序列化且違反 `definition-collection-convention`）。物件三棲。
  - 承載 **Percent + 單價/成本顯示位數 + Quantity/Weight「無綁單位」時的 fallback**；金額走貨幣、匯率走系統固定、數量/重量優先走單位（`UnitSettings`），皆不在此（數量/重量僅 fallback 用）。
- **`CompanyCashRounding`**（SAP T001R 式，**per 公司 × 貨幣**）：鍵值集合，item `CashRoundingItem { string CurrencyCode; decimal Unit }`（如 `CHF`→`0.05`）。**現金捨入單位**，只在**單據最終應付額**套用（§2.2），與幣別自然小數脫鉤；未列之貨幣 → 無額外現金捨入（退幣別自然單位）。物件三棲。
- **`CompanyAllowedCurrencies`**（**公司可用幣別子集**，比 SAP/Odoo 更嚴格的 per-company 白名單）：幣別碼集合（`KeyCollectionBase`，item 持 ISO alpha-3 碼）。**單據貨幣欄的下拉選項只顯示此子集**（空 → 顯示全系統 `CurrencySettings`）。物件三棲。
  - SAP/Odoo 皆無真 per-company 白名單（SAP 全域 TCURC + 靠匯率間接過濾、Odoo 全域 `active` flag）；本框架的 per-company 子集是合理增強，無標準可照抄，故自訂。
- **`CompanyInfo`**（[CompanyInfo.cs](src/Bee.Definition/Identity/CompanyInfo.cs)）新增四屬性：`[Key(4)]` `CompanyNumberFormats`（`int GetDecimals(NumberKind)`：公司覆寫 else `NumberKindProfile.GetDefaultDecimals`）、`[Key(5)]` `string DefaultCurrency`、`[Key(6)]` `CompanyCashRounding`（`decimal GetCashRounding(code, CurrencySettings)`）、`[Key(7)]` `CompanyAllowedCurrencies`（`IReadOnlyList<string> GetAllowedCurrencies(CurrencySettings)`：子集非空回子集 else 全系統碼）。
- **`CompanyRepository.GetById`**（[CompanyRepository.cs](src/Bee.Repository/System/CompanyRepository.cs)）SELECT 增列 `number_formats_xml`、`default_currency`、`cash_rounding_xml`、`allowed_currencies_xml`，分別反序列化/填入；欄空 → 空覆寫表 / 空本幣 / 空現金捨入 / 空白名單（= 全幣別）。
- 快取失效沿用既有 `CompanyInfo` cache-notify。

### 1.6 `FormField` / `LayoutFieldBase` 帶 `NumberKind`

- `FormField` 新增 `NumberKind NumberKind`（`[XmlAttribute]`、`[DefaultValue(None)]`）。
- `LayoutFieldBase` 新增 `NumberKind`；`LayoutColumnFactory.ToField/ToColumn` 一併傳遞（供階段 2/3）。
- **`FormField.Clone()`** 補 `NumberKind`；順帶修既有漏複製的 `ReadOnly`（[FormField.cs:371-391](src/Bee.Definition/Forms/FormField.cs)）。
- `FormField` 另加 `CurrencyField`（金額欄 CUKY 參照，§1.4）、`UnitField`（數量/重量欄 UNIT 參照，§1.4）；`LayoutColumnFactory` 一併傳遞至 `LayoutColumn`（供 §3.2c per-cell）。
- 本階段 `LayoutColumnFactory` 仍照舊複製 explicit `NumberFormat`，不做公司/貨幣/單位解析（留階段 2/3）。

### 1.7 測試

- `NumberKindProfile`：格式種類/預設位數/捨入策略/`GetDecimalsSource`（Currency/Unit/SystemFixed/Company）/`BuildFormatString`。
- `CurrencySettings`：`GetRounding`（命中、fallback 0.01）、`GetDecimals` 由因子反推（0.01→2、1→0）；物件三棲 round-trip。
- `UnitSettings`：`GetDecimals`（KG→3、PCS→0、命中/fallback）；物件三棲 round-trip。
- `CompanyCashRounding`：`GetCashRounding` 公司覆寫（CHF→0.05）else 退幣別自然單位；物件三棲。
- `CompanyAllowedCurrencies`：`GetAllowedCurrencies` 子集非空回子集、空回全系統碼；物件三棲。
- `CompanyInfo`/`CompanyNumberFormats`：`GetDecimals` 覆寫優先/fallback；`DefaultCurrency` 帶入；XML + MessagePack round-trip。
- `CompanyRepository.GetById`：`number_formats_xml`、`default_currency` 解析（含空欄）。
- `FormField`/`LayoutColumnFactory`：`NumberKind` 序列化與傳遞；`Clone` 複製 `NumberKind`、`ReadOnly`。
- XML round-trip：含 `NumberKind`/`CurrencyField`/`UnitField` 的 FormSchema、含 `number_formats_xml` 的 st_company。

### 階段 1 交付標準

`dotnet build` + 相關 UnitTests 全綠；既有「已手填 `NumberFormat`」欄位行為不變（尚未引入解析）。

---

## 階段 2：解析（公司 bake + 貨幣 runtime 計算）

### 目標

非貨幣類於交付時依公司 bake 顯示格式；金額類於 BO 計算依單據貨幣 round-then-sum；貨幣定義 ship 給 client 供 UI runtime 用。全程不 mutate 共用快取。

### 2.1 非貨幣類顯示：交付時 bake（clone）

- 於 `LoadAndLocalizeSchema`（[SystemBusinessObject.cs:456](src/Bee.Business/System/SystemBusinessObject.cs)）clone 後追加「套用數值格式」：對 clone 內每個 `NumberKind != None`、未手填 explicit `NumberFormat` 的欄，依 `GetDecimalsSource` bake：
  - `Company`：`field.NumberFormat = BuildFormatString(kind, company.GetDecimals(kind))`。
  - `SystemFixed`（匯率）：`BuildFormatString(kind, profile.GetDefaultDecimals(kind))`（系統固定，session 無關）。
- **`Currency`（金額）與 `Unit`（有綁 `UnitField` 的數量/重量）不在此 bake**——位數依該欄參照欄（CUKY/UNIT）的當前值、runtime 決定（§2.3 / §3.2）；交付時只標記「reference-bound + 解析後的參照欄名」。數量/重量**無綁單位**時退 `Company`、於此 bake。
- explicit `NumberFormat` 仍優先。命名：擴 `LoadAndLocalizeSchema` 職責或抽 `CompanyNumberFormatApplier`（依 code-style 擇歸屬）。

### 2.2 數值計算：兩層捨入（明細小數 round-then-sum + 最終現金捨入）

> **兩個層級不可混淆**：
> - **明細層**：金額捨到**幣別自然小數**、數量/重量捨到**單位小數**（系統 `CurrencySettings`/`UnitSettings`），round-then-sum，保「明細加總=總合」。
> - **單據最終層**：選配的**現金捨入單位**（公司，`CompanyCashRounding`，如 0.05），只套在最終應付額，刻意產生捨入差額。

- 捨入入口（命名實作時定）：
  ```
  decimal RoundByKind(decimal value, NumberKind kind, string refCode, RoundingContext ctx)  // 明細層
  decimal RoundCash(decimal total, string currencyCode, RoundingContext ctx) // 最終現金捨入層
  // ctx 持有 CompanyInfo + CurrencySettings + UnitSettings；refCode = 該欄參照欄當前值（金額→幣別碼、數量/重量→單位碼）
  ```
  - `RoundByKind`：`Round`+金額類用 `currencySettings.GetRounding(refCode)`（refCode＝CUKY 欄幣別）；`Round`+數量/重量用 `unitSettings.GetDecimals(refCode)`（refCode＝UNIT 欄單位；無單位退 `company.GetDecimals(kind)`）；`Round`+Percent 用 `company.GetDecimals(kind)`；`Preserve`（單價/成本/匯率）原值返回。
  - `RoundCash`：捨到 `company.GetCashRounding(currencyCode, currencySettings)` 因子的最近倍數（公司覆寫 else 幣別自然單位＝無額外捨入）。
- **明細層**：每筆金額先捨到**該欄幣別**自然小數、加總相加已捨入值（原幣、本幣各依自己 CUKY 欄的幣別獨立 round-then-sum）。
- Order 明細樣板（`ft_order_detail`；原幣未設 `CurrencyField` → 取主檔 `sys_currency`；本幣欄 `CurrencyField=local_currency`）：
  1. 原幣金額 `amount(i) = RoundByKind(quantity(i) × unit_price(i), Amount, master.sys_currency, ctx)` — 完整精度單價相乘，每筆先捨到原幣自然小數。
  2. 本幣金額 `home_amount(i) = RoundByKind(amount(i) × rate, Amount, local_currency(i), ctx)` — 已捨入原幣金額 × 匯率（preserve 全精度）、再捨到本幣自然小數。
  3. 表頭 `total = Σ amount(i)`、`home_total = Σ home_amount(i)`。
  - **禁止**：`total = RoundByKind(Σ 全精度, Amount)`（全精度加總再捨 → 明細加總 ≠ 總合）。
- **最終現金捨入（選配）**：若 `company.GetCashRounding(交易貨幣)` 有設（如 0.05），最終應付 `payable = RoundCash(total, 交易貨幣, ctx)`；**捨入差額 `diff = payable − total` 記入捨入差異**（DIFF/rounding 科目，屬帳務邏輯，本計畫定義差額計算、實際入帳由 BO 處理）。無設定 → `payable = total`、`diff = 0`。

### 2.3 貨幣/單位定義 ship 給 client

- 金額/數量顯示位數需於 client runtime 解析（參照欄值會變），故 `CurrencySettings` 與 `UnitSettings` 皆須隨 API 可得（隨既有 define 交付管道送達 client，或 `EnterCompany`/初始載入帶上）。client 端持有 `幣別→decimals` 與 `單位→decimals` 兩張表。

### 2.4 儲存精度固定（DB schema 指引）

- 數值語意欄 DB 用 `Decimal` + **框架統一高 scale**（如 `Scale=8`，≥ 任一貨幣/公司位數與不捨入類保存精度），免 per-company/per-currency ALTER。
- cookbook 記：DB scale 是「容量上限」，與顯示/計算位數無關。
- **API 匯入精度截斷（持久化邊界，preserve 類適用）**：傳入小數位**超過 DB scale** → **靜默捨到 scale（AwayFromZero）**，不報錯、不中斷整批匯入、不另記 warning。
  - 未超過 scale → 一律原樣保存（preserve 本意）；**「超過建議顯示位數」不在此題**——建議位數純顯示、非儲存邊界，超過時不做任何處理（見核心設計原則 1）。
  - 此截斷是「儲存容量物理邊界」非「業務/顯示捨入」，scale=8 遠超單價/匯率業務意義，不抵觸「禁止對來源值捨入」鐵則。
  - 對比四捨五入類（`Amount`/`Quantity`/`Weight`/`Percent`）：匯入超過位數時走 `RoundByKind`（§2.2）依 kind 位數來源捨入——捨入即其政策，與 preserve 相反。
  - **必須由框架顯式 round，不可依賴 DB 隱式轉換**（grounded 自現況碼）：參數綁定層 `DbCommandSpec.CreateCommand`（[DbCommandSpec.cs:106-138](src/Bee.Db/DbCommandSpec.cs)）只設 `Value`/`DbType`/`Size`，`DbParameterSpec` 無 `Precision`/`Scale` 屬性，scale 由 provider 自推；寫入前全 repo 無任何 `decimal.Round`/`Truncate`。多餘小數位丟給 DB 引擎時：① SQL Server/PG/MySQL/Oracle 是**四捨五入非截斷**到 column scale；② **SQLite 完全不強制 scale → 原樣留全精度**。依賴 DB → 跨 provider 不一致（同筆資料 SQLite vs SQL Server 存出不同精度）。
  - **掛載點：Repository 寫入層**（CRUD 由 FormSchema/DbField 驅動，該層握有每欄 `DbField.Scale`）——組 `DbCommandSpec` 前 `decimal.Round(value, dbField.Scale, MidpointRounding.AwayFromZero)`。`DbCommandSpec` 這層拿不到 column scale，不在此做。

### 2.5 文件與測試

- `docs/development-cookbook.md` 增「數值語意、公司位數、多幣別與捨入」一節。
- 測試：
  - 非貨幣顯示（公司 fallback）：Percent 公司 A vs B → 交付 layout 格式不同；數量/重量無 `UnitField` 時退公司位數；explicit override 不被蓋；不 mutate 快取。
  - 數量/重量計算（單位）：`order_qty` 綁 `UnitField=qty_uom`，同欄 PCS（0 位）vs KG（3 位）→ 位數不同；`RoundByKind` 走 `UnitSettings`；同單位 round-then-sum 一致、混單位不加總（§3.2c）。
  - 金額計算（貨幣）：同資料 USD（`0.01`）vs JPY（`1`）→ 明細與合計位數不同；**round-then-sum 不變式** `Σ 已捨入明細 == 表頭合計` 且 ≠ 全精度後捨。
  - 現金捨入（公司覆寫）：公司對 CHF 設 `CompanyCashRounding=0.05` → `RoundCash(total)` 捨到 5 分倍數（12.34→12.35、12.32→12.30），且 `diff = payable − total` 正確；未設 → `payable = total`、`diff = 0`。明細層仍捨 2 位、不受影響。
  - 同列多幣（CUKY）：原幣未設 `CurrencyField` → 取主檔 `sys_currency`、本幣欄綁 `local_currency`，兩金額欄解析出不同位數；`home_amount` 依本幣位數捨入、`Σ home_amount == home_total`；原幣=本幣時兩者一致。
  - 幣別解析優先序：金額欄指定 `CurrencyField` 勝；未指定取主檔 `sys_currency`；主檔無幣別欄退公司本幣；皆無退框架 2 位。
  - Preserve：`RoundByKind` 對單價/成本/匯率原值返回；金額用完整精度單價，與「先把單價捨到顯示位再乘」結果不同。
  - Preserve 匯入精度：傳入小數位 ≤ scale 原樣保存（超過建議顯示位數不被截）；> scale 靜默捨到 scale（AwayFromZero），不拋例外。

### 階段 2 交付標準

`dotnet build` + 測試全綠；非貨幣類進不同公司顯示不同位數；金額計算依單據貨幣 round-then-sum；共用快取零污染。

---

## 階段 3：UI（Avalonia pilot）— NumericEdit + 貨幣 runtime 重算

> 控件先在 `Bee.UI.Avalonia` 定稿（`avalonia-pilot-ui-architecture`），Maui/Blazor 後續比照。

### 目標

數值欄改用 `NumericEdit`：非貨幣類用交付已 bake 的格式；**金額類於 runtime 依單據貨幣值解析，改幣別即重算位數並 reformat**；編輯期格式化、解析、右對齊。

### 3.1 `NumericEdit` 控件

- 位置：`src/Bee.UI.Avalonia/Controls/Editors/NumericEdit.cs`。
- 行為：focus 顯示完整精度、blur 依格式顯示；依 culture parse；partial（`"12."`）暫不轉、保留上一有效值（比照 `GridControl`）；右對齊；顯示捨入不回寫。
- 抽共用格式化：`GridControl.FormatCell` 與 `NumericEdit` 同源。

### 3.2 金額/數量 runtime 參照欄解析（幣別 + 單位同一套）

- **金額欄**（`DecimalsSource.Currency`）位數 = 依 §1.4 優先序解出的幣別欄（指定 `CurrencyField` → 主檔 `sys_currency` → 公司本幣）的**當前值** → client `CurrencySettings` → 位數。
- **數量/重量欄**（`DecimalsSource.Unit`）位數 = 其 `UnitField` 的**當前值** → client `UnitSettings` → 位數（無 `UnitField` → 退公司已 bake 格式）。
- **改參照欄即重算**：監聽解析到的幣別/單位欄變動，只重算「綁到該欄」的數值欄格式並刷新（Grid 與表單皆然）。改 `sys_currency` → 走預設原幣金額重算；改某列 `qty_uom` → 該列數量重算。
- 其餘（Percent / 已 bake 的 Company / SystemFixed）用交付已 bake 的 `NumberFormat`，不需 runtime 解析。

### 3.2b 匯率 UI（三源裡最單純）

- 匯率（`DecimalsSource.SystemFixed`）格式 = 固定 `"N5"`，**交付時即 bake**，UI 不需 runtime 貨幣解析（與金額不同）。
- Preserve 行為：blur 顯示 `N5`（拖尾零保留，如 `31.25000`，FX 慣例）、focus 顯示完整保存精度供編輯；**顯示捨入不回寫**，換算金額時用全精度匯率。
- 多為**唯讀顯示**（匯率來自匯率表/牌告），偶爾可覆寫；右對齊。
- **極端量級匯率**（如 `0.00001234` 在 N5 顯示截斷為 `0.00001`）：值仍全精度正確、僅顯示截斷；常規貨幣對無虞，極端幣別屬未來 factor（TCURF）議題。

### 3.2c Grid 多幣別/多單位格式化（逐格 reference-aware）

> 同一金額欄各列幣別、同一數量欄各列單位都可能不同 → 位數隨列變，**不能用欄級單一格式字串**。金額(幣別)與數量/重量(單位)走**同一套 per-cell 機制**。

- **問題**：金額/數量欄在 Grid 是「一欄多列」。明細 Grid 各列通常共用主檔幣別/同單位（欄內一致）；**清單 Grid 各列自己的參照欄 → 同欄逐列不同位數**（金額 USD 2/JPY 0/BHD 3；數量 PCS 0/KG 3）。
- **解法：`FormatCell` 改為 reference-aware、逐格解析**——`DecimalsSource.Currency`/`Unit` 欄格式不再是欄級固定字串，每格依優先序讀**該列**的參照欄：
  1. 欄帶 metadata `NumberKind` + 參照欄名（`CurrencyField`/`UnitField`，承自 `LayoutColumn`/`FormField`）。
  2. 每格解析參照碼：金額→（`CurrencyField` → 主檔 `sys_currency` → 公司本幣）；數量/重量→（`UnitField` → 無則退公司）。
  3. → `CurrencySettings`/`UnitSettings`.`GetDecimals` → `BuildFormatString` → 格式化該格。
  - `FormatCell`（[GridControl.cs:1004](src/Bee.UI.Avalonia/Controls/GridControl.cs)）由 `(row, fieldName, displayFormat, numberFormat)` 擴為帶 `NumberKind + 參照欄名 + Resolver`（client `CurrencySettings`/`UnitSettings` + 主檔幣別 accessor + 公司本幣）。其餘欄仍用已 bake 固定格式。
  - 一套機制涵蓋兩情境：明細 Grid 每格恰好同位數、清單 Grid 逐列不同。
- **顯示（定案：數值格只顯數字 + 鄰欄獨立幣別/單位欄）= SAP ALV 作法**：數值格只顯格式化數字（依該列位數）、右對齊；**幣別/單位由旁邊獨立欄顯示**（`sys_currency` / `qty_uom`），格內不內嵌碼/符號。混幣/混單位小數點不對齊屬預期，靠參照欄辨識。
  - 對齊 SAP ALV：金額欄 `CurrencyField`、數量欄 `UnitField` ≡ ALV field catalog 的 **`CFIELDNAME`/`QFIELDNAME`**（「currency/quantity unit field referenced」），逐列依參照欄查位數（SAP→TCURX/T006、本框架→`CurrencySettings`/`UnitSettings`）。
- **混參照加總（定案：混則不顯合計）**：跨幣別(USD+JPY)、跨單位(KG+PCS)相加皆無語意，故——
  - **原幣金額欄 / 數量欄**：footer 合計**僅當全欄同一參照（同幣/同單位）時顯示**；混則**不顯合計**（對齊 SAP `DO_SUM` 退路、避開 Odoo issue #79237）。
  - **本幣金額欄**：恆公司本幣、欄內一致 → **永遠可加總**（有意義的跨幣別合計，也是 ERP 同存原幣/本幣的主因）。
- **重算觸發**：列內參照欄改 → 只重算該列數值格 + 重判該欄是否仍同參照（決定合計顯隱）；主檔 `sys_currency` 改 → 重算走預設的列。
- **效能**：`CurrencySettings`/`UnitSettings` client 端字典，逐格 lookup 便宜；明細 Grid 可快取「參照 → format」一次。
- **測試**：清單 Grid 同欄含 USD/JPY/BHD 三列 → 各列位數正確（2/0/3）；明細 Grid 共用主檔幣別 → 欄內一致；改主檔幣別 → 全欄重算。合計：原幣欄全同幣 → 顯合計、混幣 → 不顯；本幣欄恆顯合計。

### 3.3 控件選用

- `ControlType` 新增 `NumericEdit`；`FieldEditorFactory`（[FieldEditorFactory.cs](src/Bee.UI.Avalonia/Controls/Editors/FieldEditorFactory.cs)）對數值型別（`Short/Integer/Long/Decimal/Currency`）`Auto` 解析為 `NumericEdit`。
- culture 沿用既有 `InvariantCulture`。

### 3.4 DemoCenter 數值格式化範例

> 待階段 1–3 框架功能落地後才做（本 plan 先登記為 Phase 3 交付項）。

- 新增 `samples/Avalonia.DemoCenter/Modules/Grids/NumberFormatModule.cs`（繼承 `DemoModuleBase`、`Category => "Grid"`、`Title => "數值格式化"`），結構參考 `Modules/Grids/InCellEditModule.cs`；於 `Modules/DemoModuleRegistry.cs` 註冊一行。
- 展示：Grid 欄涵蓋各 `NumberKind`（數量/單價/成本/重量/金額）不同位數；**下拉切換公司** → 非貨幣類位數變；**下拉切換單據貨幣（USD↔JPY）** → 金額類即時 reformat（0 位↔2 位）；`NumericEdit` 編輯期格式化、右對齊。
- 擴充 `Modules/Views/SampleFormData.cs` 補 `Decimal` 數值欄 + 主檔 `sys_currency` 幣別欄與 seeded 值。
- samples/ 不觸發 CI（`ci-path-filter`），本機 `-c Debug` 目視驗證。

### 3.5 測試

- `NumericEdit`：focus/blur、partial 容錯、format 套用、顯示變動不污染底層值。
- 貨幣 runtime：改 `CurrencyField` 值 → 金額欄格式位數改變。
- `FieldEditorFactory`：數值 → `NumericEdit`。

### 階段 3 交付標準

`dotnet build` + `Bee.UI.Avalonia.UnitTests` 全綠；Avalonia 數值欄編輯期格式化、右對齊；改單據貨幣金額即時重算位數；DemoCenter 範例可跑（切公司 + 切貨幣可見效果，對齊 `avalonia-democenter-alignment-baseline`）。

---

## 不在本計畫範圍（未來項）

- （已納入本期）逐欄貨幣鍵綁定（SAP CUKY per-field）：金額欄各綁 `FormField.CurrencyField`，同列多幣（原幣/本幣）支援。
- **匯率 factor（TCURF）**：量級可讀性處理。
- ~~捨入單位~~：已**納入本期**——SAP T001R 式拆兩層（幣別小數系統 + 現金捨入單位 `CompanyCashRounding` 公司可覆寫），最終應付額套用。
- **header DIFF 捨入差吸收**：當出現 header 層獨立計算（整單折讓、總額級稅）時才需要。
- **price unit（KPEIN）**：本期單價走 preserve。
- （per 計量單位位數已納入本期，見 §1.3b/§1.4）；per 品項位數、多 culture 顯示、DB scale 隨公司/貨幣變動仍不做。
- Maui/Blazor `NumericEdit` 移植（Avalonia pilot 後另案）。

## 風險與相容性

- `FormField`/`LayoutFieldBase` 加 `NumberKind`、`FormSchema` 加 `CurrencyField`，皆 `[DefaultValue]` 空 → 既有 XML 反序列化行為不變。
- `st_company` 加 `number_formats_xml` / `default_currency` / `cash_rounding_xml` / `allowed_currencies_xml`、`CompanyInfo` 加 `[Key(4)]`～`[Key(7)]`：舊資料欄空 → 全用框架預設、本幣空則金額退框架 2 位、現金捨入空則無額外捨入、白名單空則顯示全幣別；MessagePack 尾端加 key 相容。
- 新增 `DefineType.CurrencySettings` / `DefineType.UnitSettings`：無定義 → 幣別 fallback 2 位、單位 fallback 框架預設。
- 重構 `NumberFormatPresets` 為 enum：0 production caller，僅調其測試。
- 解析一律於 per-call clone / runtime 上操作，遵守 `definition-immutability`。

## SAP / Odoo 對照來源（節錄）

**Odoo**
- 貨幣 rounding/decimal_places/round：[res_currency.py – odoo/odoo](https://github.com/odoo/odoo/blob/14.0/odoo/addons/base/models/res_currency.py)
- `float_round`（precision_digits / precision_rounding / rounding_method）：[float_utils.py – odoo/odoo](https://github.com/odoo/odoo/blob/14.0/odoo/tools/float_utils.py)
- 稅務捨入 `round_per_line`（預設）/`round_globally`：[Issue #37896](https://github.com/odoo/odoo/issues/37896)
- Grid 金額內嵌幣別符號（monetary widget）：[monetary_field.js](https://github.com/odoo/odoo/blob/master/addons/web/static/src/views/fields/monetary/monetary_field.js)、monetary sum 痛點 [Issue #79237](https://github.com/odoo/odoo/issues/79237)

**SAP**
- ABAP CURR/QUAN 必綁 CUKY/UNIT：[Currency Fields – ABAP docs](https://help.sap.com/doc/abapdocu_latest_index_htm/latest/en-US/abenddic_currency_field.htm)
- ALV 金額欄參照幣別欄 `CFIELDNAME`（= 本框架 `CurrencyField`）：[LVC_S_FCAT-CFIELDNAME – SAP Datasheet](https://www.sapdatasheet.org/abap/tabl/lvc_s_fcat-cfieldname.html)
- 幣別小數 TCURX（fallback 2、JPY=0）：[JPY Currency Decimal Places – SAP Community](https://community.sap.com/t5/enterprise-resource-planning-q-a/jpy-currency-decimal-places/qaq-p/6660095)、[KBA 2973787](https://userapps.support.sap.com/sap/support/knowledge/en/2973787)
- 數量/重量小數隨單位 T006（ANDEC/DECAN）：[T006 – SAP Datasheet](https://www.sapdatasheet.org/abap/tabl/t006.html)
- 重量是物料屬性、QUAN 綁 GEWEI 單位：[MARA-BRGEW](https://www.sapdatasheet.org/abap/tabl/mara-brgew.html)、[GEWEI 資料元素](https://www.sapdatasheet.org/abap/dtel/gewei.html)；Odoo 重量 `digits='Stock Weight'`：[product_template.py](https://github.com/odoo/odoo/blob/17.0/addons/product/models/product_template.py)
- 匯率 factor TCURF：[TCURF – SAP Datasheet](https://www.sapdatasheet.org/abap/tabl/tcurf.html)、[KBA 3366043](https://userapps.support.sap.com/sap/support/knowledge/en/3366043)
- price unit KPEIN：[KPEIN – SAP Datasheet](https://www.sapdatasheet.org/abap/dtel/kpein.html)
- 逐行捨入/T001R：[Decimal Points in SD Pricing](https://community.sap.com/t5/enterprise-resource-planning-q-a/decimal-points-in-sd-pricing/qaq-p/7427896)、[T001R – TCodeSearch](https://www.tcodesearch.com/sap-tables/T001R)
