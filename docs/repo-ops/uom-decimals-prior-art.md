# 計量單位小數位的外部佐證（prior art）

**這不是踩雷誌**，所以不放在 `gotchas/`。它是一份**設計決策的外部佐證**：查 SAP 與 Odoo
怎麼處理計量單位（unit of measure）的小數位與捨入，用來檢驗 bee-library 現行設計哪些是
刻意、哪些是缺口。

決定本身與拒絕過的替代方案在
[ADR-026](../adr/adr-026-numeric-semantics-rounding.md) —— **本檔不複寫那些**，只放外部對照與判讀。
Bee.NET 這一側的現況以原始碼為準，本檔提到的型別與成員名稱就是查證入口。

> **這是 2026-09-11 查到的狀態，不是持續維護的比較表。** 兩家都在演進（Odoo 19 就把
> 逐單位的捨入設定整個拿掉了），下次要引用前先確認還成立。查證邊界寫在文末附錄，一併讀。

## 一句話結論

- **「位數放在系統層、逐單位設定」三者一致**，層級本身不需要改。
- **單位存位數**（而不是捨入單位）這一點 Bee.NET 跟 SAP 一樣；Odoo 16–18 存捨入單位，
  **19 起連逐單位都不設了**，只剩一個全系統位數。
- 真正的差異有兩個：**手填數量不捨、也不擋**（兩家都有機制），以及
  **沒綁單位的數量欄會退到公司層位數**（兩家都沒有這個層級）。

## 五個問題的對照

| 問題 | SAP（ECC／S/4HANA） | Odoo | Bee.NET |
|---|---|---|---|
| **1. 位數存哪** | `T006`（client 層）兩欄：`ANDEC` 捨入位數、`DECAN` 顯示位數，都是整數型別。`QUAN` 欄本身的 DB 小數位與單位位數無關 | 16–18：每個單位的 `uom.uom.rounding`（捨入單位，預設 0.01）＋全域 `decimal.precision`「Product Unit of Measure」。saas-18.1：`rounding` 改為由全域位數算出。saas-19.2 起：欄位移除 | `UnitItem.Decimals`（整數，系統層 define `UnitSettings`）。DB scale 與它正交（ADR-026 D6） |
| **2. 捨入單位與位數分開？** | 兩欄都是位數，沒有捨入單位。「捨到倍數」在物料／工廠層（`MARC-BSTRF`、捨入參數檔 `RDPRF`），性質是業務規則 | 16–18 分開：`rounding` 管換算、全域位數管 ORM 寫入，兩者之間只有畫面 onchange 警告。19 起合一 | 一個數字兩用：顯示（`NumberFormatResolver.ResolveFormat`）與捨入（`RoundByKind`）。沒有捨入單位 |
| **3. 誰決定、覆寫到哪層** | client 層。公司代碼、物料、單據列的位數覆寫機制，官方來源查不到 | 全資料庫一份，`decimal.precision` 沒有公司欄位。16–18 可逐單位；產品、單據列都不行 | 系統層。欄位綁了單位就不看公司；沒綁時退公司位數（見〈單位設定的層級〉） |
| **4. 何時捨、怎麼捨** | 顯示依 `DECAN`。換算用 `UNIT_CONVERSION_SIMPLE` 的 `ROUND_SIGN`（`+` 上、`-` 下、`X` 商業、空白不捨，預設空白），依據 `T006` 哪一欄**未確認**。輸入沒有框架層捨入，由應用層拒絕（例：採購 `ME 678`） | 有 `digits` 的 Float 欄在進 cache／寫 DB 時 `float_round`，預設 HALF-UP（遠離零）。換算 `_compute_quantity` 預設 `UP`，捨到目標單位；`stock.move.product_qty` 改用 HALF-UP。17.0 完成數量不合單位精度時擲 `UserError` | 只捨計算欄（`FormExpressionCalculator.ApplyComputed` → `RoundByKind`，`AwayFromZero`）。`NumericEdit` 寫回完整精度，只在顯示時格式化。沒有單位換算 |
| **5. 明細單位不同時的合計** | ALV 以 `QFIELDNAME` 綁單位欄，合計 "displayed separately by unit"（分單位小計）。交貨單抬頭 `LIKP-BTGEW`＋`GEWEI`，換算成抬頭單位的規則在 ECC 查不到 | 銷售／採購明細數量欄沒有合計；`stock.move` 清單有 `sum` 且不換算（讀 XML 推論，未實跑）；`sale.report` 在 SQL 先換成產品基準單位再加總；出貨重量用系統單一重量單位 | 框架 Grid 沒有頁尾合計。`AmountColumnSummary.TryComputeTotal` 對混單位回 `null`（不換算），目前只有 DemoCenter 的 `MultiUnitModule` 手接 |

### Odoo 的版本差異（影響上表結論的部分）

| 項目 | 16.0 / 17.0 / 18.0 | saas-18.1 起 | saas-19.2 起（master 相同） |
|---|---|---|---|
| `uom.category` | 存在 | 移除，改為 `relative_factor` ＋ `relative_uom_id` 樹狀結構 | — |
| `uom.uom.rounding` | stored，逐單位設定，`CHECK (rounding>0)` | compute，值為 `10**-precision_get('Product Unit')`，逐單位覆寫消失 | 欄位移除 |
| 全域精度名稱 | 「Product Unit of Measure」（product 模組） | 「Product Unit」（搬到 uom 模組） | 同左 |
| `_compute_quantity` 捨到哪 | 目標單位的 `rounding` | 同左（但已等於全域位數） | 直接用全域位數 |
| `float_round` 捨入法 | 16.0 只有 `UP`／`DOWN`／`HALF-UP`；17.0 加 `HALF-EVEN`／`HALF-DOWN`；18.0 未知方法改擲 `ValueError` | — | — |
| 單據列單位欄名 | `product_uom` | — | 19.0 為 `product_uom_id` |

`_compute_quantity` 的預設 `rounding_method='UP'` 在所有查過的版本都沒變。

## 對 bee-library 的意義

### 1. 手填數量不捨、也不擋 —— 缺口（未決）

- ADR-026 D1 寫 `NumberKind` 決定「(b) 是否於寫入時捨入」，`Quantity`／`Weight` 屬 `Round`；
  `.claude/rules/database.md` 也寫四捨五入類「寫入時 `AwayFromZero` 捨到該欄位位數」。
- 實作上 `RoundByKind` 的呼叫端只有 `FormExpressionCalculator`；`NumericEdit` 寫回完整精度，
  `FormBusinessObject` 存檔也不捨。
- 後果：單位 PCS（0 位）手填 1.5，畫面顯示 2、資料庫存 1.5；以它為來源的合計會跟畫面上的明細
  對不起來 —— 正是 D2 round-then-sum 要防的事。**這條不限單位，金額與百分比同樣成立。**
- 兩家都有機制：Odoo 在 ORM 寫入時捨到全域位數；SAP 在應用層拒絕超位數輸入。
- **2026-09-11 決定先記錄、不處理。** 待決的方向有三：存檔前捨 `Round` 類、超位數時擋輸入、
  或把 ADR 的「寫入時」改成「計算後」對齊現行實作。

### 2. 單位代碼查不到時退 0 位、不報錯 —— 缺口候選

- `UnitSettings.GetDecimals` 查不到回 `FallbackDecimals`（0）。`FormField.UnitField` 的 XML doc
  只說明空值退公司位數，沒提查不到的情形。重量欄綁到打錯的 `KGS`，1.234 會被捨成 1。
- ADR-026 的 2026-09-10 修訂把公司本幣改為必填，理由是「退路換到的是靜默的錯誤位數」——
  這裡是同一個形狀。
- Odoo 的單位欄是指向 `uom.uom` 的 Many2one，無效代碼進不去。SAP 單位 domain `MEINS` 的
  值表是 `T006`（見附錄查證邊界）。
- 幣別那一側（查不到退 0.01）同形狀，不在本次範圍。

### 3. 單位存位數、幣別存捨入單位 —— 對齊 SAP，合理，但理由沒寫

- 業界沒有「單位必須存捨入單位」的慣例：SAP 存位數，Odoo 19 連逐單位都放棄。
  「捨到 0.5 箱」這類需求在 SAP 是物料層的業務規則（`BSTRF`／`RDPRF`），不是單位的位數。
- ADR-026 與引入時的 commit `eb10bc0c` 只寫「位數直存」，沒寫為什麼跟幣別不同。
  算有意識但沒留理由；要補的話一句話進 ADR-026 即可。

### 4. `ANDEC`／`DECAN` 合成一欄 —— 簡化可接受，但 XML doc 寫錯

SAP 分捨入位數與顯示位數；Bee.NET 一個 `Decimals` 兩用；Odoo 19 也合一。
問題在 `UnitItem.Decimals` 的 XML doc 寫「display decimal places (SAP T006 `ANDEC`)」：
`ANDEC` 是**捨入**位數、顯示位數是 `DECAN`，而且這個值實際上也用在捨入。

### 5. 沒有單位換算 —— 範圍外，但 ADR 沒記

SAP 有 `T006` 的 SI 換算欄位（`ZAEHL`／`NENNR`／`EXP10`／`ADDKO`）與物料層 `MARM`；
Odoo 有 `factor`（19 起 `relative_factor`）。Bee.NET 的 `UnitItem.Dimension` 只做 UI 分組，
因此混單位合計只能隱藏，做不到 Odoo 報表那樣先換成基準單位再加。
ADR-026「未做」清單列了 TCURF、KPEIN、DIFF，沒列單位換算。

### 6. 混單位合計 —— 方向一致，cookbook 說過頭

「不跨單位相加」與 SAP 一致（SAP 分單位小計，Bee.NET 整個不顯示）。但 cookbook
〈Units of measure〉讀起來像 Grid 內建了這個 gate，實際上框架 Grid 沒有頁尾，只有 sample 手接。

### 7. 只有列層單位、沒有表單層 —— 刻意，已記錄

commit `eb10bc0c` 與 cookbook 都寫明 per-row。SAP 的 `UNIT` 參照、Odoo 的單位欄也都在列上，不衝突。

## 單位設定的層級：三者比較

| 層級 | SAP | Odoo | Bee.NET | 判讀 |
|---|---|---|---|---|
| **系統／租戶**（SAP client、Odoo 資料庫、Bee 部署） | `T006`：逐單位位數、SI 換算、維度 | 全域精度：所有數量欄的寫入位數；19 起是唯一的位數來源 | `UnitSettings`：逐單位位數 | 三者都放這層，**不是問題** |
| **公司** | 無（官方來源查不到覆寫機制） | 無（`decimal.precision` 沒有公司欄位） | **有**：`CompanyInfo.NumberFormats` 的 `Quantity`／`Weight` 項，只在欄位沒綁單位時生效 | **兩家都沒有的層級**，見下 |
| **單位** | `ANDEC`（捨入位數）＋`DECAN`（顯示位數） | 16–18：`rounding`（捨入單位）；19 起無 | `Decimals`（位數，顯示與捨入共用） | 與 SAP 同；比 Odoo 19 細 |
| **物料／產品** | 基準單位＋替代單位換算（`MARM`）；工廠層倍數捨入（`MARC-BSTRF`／`RDPRF`），不是位數 | 產品選基準單位（`uom_id`）；沒有位數欄 | 無。框架沒有產品主檔，列上的單位值由應用帶入 | 位數三者都不放這層；換算與倍數在兩家落在這層，Bee.NET **沒有接縫** |
| **單據列** | 列帶單位；`QUAN` 欄必須參照單位欄；超位數由應用層擋（`ME 678`） | 列選單位（Many2one）；位數跟系統；17.0 庫存移動不合精度擲例外 | 列上 `UnitField` 的值決定位數；手填值不捨、不擋 | 綁定形狀一致；差在**沒有強制**（上方第 1 點） |
| **數量欄沒綁單位時** | 不存在：ABAP Dictionary 要求每個 `QUAN` 欄指定參照的單位欄 | 照用系統位數（寫入位數本來就不跟單位走） | 退公司位數 → 框架預設，交付時由 `NumberFormatApplier.Bake` 烤進 `NumberFormat` | **Bee.NET 獨有**，與「公司」那列是同一件事 |
| **單位代碼無效時** | 單位 domain `MEINS` 的值表是 `T006`；各欄是否硬擋未逐欄查 | 進不去（外鍵） | 退 0 位、不報錯 | 上方第 2 點 |

**判讀：層級本身沒放錯，有問題的是公司層那條退路。**

- **位數放系統層、逐單位設定**，Bee.NET 與 SAP 相同、比 Odoo 19 細。不需要往公司層或產品層搬 ——
  兩家都沒有公司層的單位位數，也都不把位數放在產品上。
- **公司層退路是兩家都沒有的東西。** 同一個「數量」語意，位數來源會因 schema 作者有沒有設
  `UnitField` 而在系統層與公司層之間跳；同一張沒綁單位的表單交付給兩間公司，數量可以顯示成不同位數，
  綁了單位的欄卻不會。SAP 從 ABAP Dictionary 層面就不允許沒有單位參照的數量欄；Odoo 的寫入位數
  從來不跟單位走，也就不存在「沒綁時退到哪」的問題。
- ADR-026 D1 的表格有寫「無則退公司」，但沒寫理由。對照同一份 ADR 2026-09-10 修訂的推理，
  這條退路換到的也是「沒有人為這個欄選過的位數」。**可能的方向（未決）**：
  - 要求 `Quantity`／`Weight` 必綁 `UnitField`，沒綁時報錯（SAP 的做法）；
  - 沒綁時改走系統層預設、不經公司（Odoo 的做法）；
  - 維持現狀，但把理由補進 ADR-026。

  **2026-09-11 使用者傾向第一項**（理由：綁定 `UnitField` 與多幣別作法一致），已另擬
  [plan-unit-field-required.md](../plans/plan-unit-field-required.md) 評估。
- **物料層的空缺是範圍問題，不是層級放錯**：框架沒有產品主檔，換算與倍數捨入若要做，接縫會落在
  應用層。ADR-026「未做」清單目前沒提到它（上方第 5 點）。

---

## 附錄：查證邊界

> **別把這份當定論。**
>
> **親自重抓核對過的**：Odoo 17.0／19.0／master 的 `addons/uom/models/uom_uom.py`
> （`rounding` 欄位、約束、`_compute_quantity`）、17.0 `odoo/tools/float_utils.py` 的
> HALF-UP 定義、17.0／19.0 `sale_order_line.py` 的單位欄型別；SAP 官方的 ABAP Dictionary
> 數量欄頁與 ALV 欄位目錄頁；sapdatasheet 上 `ANDEC`／`DECAN` 的描述與 `MEINS` 的值表。
> 其餘 Odoo 行號與 SAP 出處由調查代理讀取，未逐一重抓。
>
> **SAP 的來源品質參差**：`ANDEC` 的定義只找得到資料字典鏡像站（sapdatasheet，第三方），
> help.sap.com 沒有直接定義；community.sap.com 一律回 403，只看得到搜尋摘要。
>
> **值表不等於硬檢查**：`MEINS` 的值表是 `T006`，但 SAP 的值表只是建議外鍵的來源，
> 各欄位實際有沒有檢查要看該欄的外鍵定義，這點沒有逐欄查。
>
> **仍未查的**：`UNIT_CONVERSION_SIMPLE` 依據的是 `ANDEC` 還是 `DECAN`；S/4HANA
> `I_UnitOfMeasure` 的小數位欄位名；ECC 抬頭重量換算規則；S/4HANA 是否改變
> `ANDEC`／`DECAN` 語意；Odoo 前端輸入時是否先捨；`stock.move` 清單跨單位直接相加
> 是讀 XML 推的，沒實際跑。

## 附錄：出處

**SAP**

- ABAP Dictionary 數量欄（官方）：https://help.sap.com/doc/abapdocu_753_index_htm/7.53/en-US/abenddic_quantity_field.htm
- `WRITE … UNIT`（官方）：https://help.sap.com/doc/abapdocu_751_index_htm/7.51/en-us/abapwrite_to_options.htm
- ALV 數量／幣別欄（官方）：https://help.sap.com/saphelp_nw73/helpdata/en/4e/bd13c61041389ee10000000a421937/content.htm
- 單位換算 function group SCV0（官方）：https://help.sap.com/doc/saphelp_nw73ehp1/7.31.19/en-US/48/dfb0aaab14280fe10000000a42189c/content.htm
- `T006`（第三方）：https://www.sapdatasheet.org/abap/tabl/t006.html
- `ANDEC`／`DECAN`（第三方）：https://www.sapdatasheet.org/abap/dtel/andec.html 、https://www.sapdatasheet.org/abap/dtel/decan.html
- `MEINS` domain（第三方）：https://www.sapdatasheet.org/abap/doma/meins.html
- `UNIT_CONVERSION_SIMPLE` 參數（第三方）：https://www.sapdatasheet.org/abap/func/unit_conversion_simple.html
- `BSTRF`／`RDPRF`（第三方）：https://www.sapdatasheet.org/abap/dtel/bstrf.html 、https://www.sapdatasheet.org/abap/tabl/marc-rdprf.html
- 訊息 `ME 678`（第三方）：https://www.sapdatasheet.org/abap/msag/me-678.html

**Odoo**（master 行號為 2026-09-11 抓取當下）

- 17.0 `uom_uom.py`（`rounding` L66、約束 L81、`_compute_quantity` L211–239）：https://github.com/odoo/odoo/blob/17.0/addons/uom/models/uom_uom.py#L66
- 19.0 `uom_uom.py`（`_compute_rounding` L62–67）：https://github.com/odoo/odoo/blob/19.0/addons/uom/models/uom_uom.py#L62-L67
- master `uom_uom.py`（`_compute_quantity` L139–167）：https://github.com/odoo/odoo/blob/master/addons/uom/models/uom_uom.py#L139-L167
- 17.0 `product_data.xml`（全域精度 L35–38）：https://github.com/odoo/odoo/blob/17.0/addons/product/data/product_data.xml#L35-L38
- 17.0 `product/models/uom_uom.py`（onchange 警告 L10–21）：https://github.com/odoo/odoo/blob/17.0/addons/product/models/uom_uom.py#L10-L21
- 17.0 `decimal_precision.py`（無公司欄位 L21–34）：https://github.com/odoo/odoo/blob/17.0/odoo/addons/base/models/decimal_precision.py#L21-L34
- 17.0 `fields.py`（Float 寫入捨入 L1531–1555）：https://github.com/odoo/odoo/blob/17.0/odoo/fields.py#L1531-L1555
- 17.0 `float_utils.py`（`float_round` L35–111）：https://github.com/odoo/odoo/blob/17.0/odoo/tools/float_utils.py#L35-L111
- 17.0 `stock_move.py`（`product_qty` L279–282、`_set_quantity` L388–403）：https://github.com/odoo/odoo/blob/17.0/addons/stock/models/stock_move.py#L279-L282
- 17.0 `stock_move_views.xml`（`sum` L28、L47–48）：https://github.com/odoo/odoo/blob/17.0/addons/stock/views/stock_move_views.xml#L28
- 17.0 `sale_report.py`（換算後加總 L94–98、L176–177）：https://github.com/odoo/odoo/blob/17.0/addons/sale/report/sale_report.py#L94-L98
- 17.0 `stock_delivery/models/stock_move.py`（重量 L33–38）：https://github.com/odoo/odoo/blob/17.0/addons/stock_delivery/models/stock_move.py#L33-L38
- 17.0／19.0 `sale_order_line.py`（單位欄 17.0 L120、19.0 L132）：https://github.com/odoo/odoo/blob/17.0/addons/sale/models/sale_order_line.py#L120
- 17.0 Purchase UoM 文件（rounding 與 Decimal Accuracy 說明）：https://www.odoo.com/documentation/17.0/applications/inventory_and_mrp/purchase/products/uom.html
