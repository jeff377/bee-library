# 計畫：多計量單位數值（UnitSettings + UNIT 綁定）

**狀態：📝 待做（2026-06-21）**

> 執行增量 3/3，相依 [plan-numeric-multicurrency.md](plan-numeric-multicurrency.md)（重用其「參照欄綁定 / Grid per-cell」機制）。設計理由、SAP/Odoo 對照見 [plan-numeric-formatting.md](plan-numeric-formatting.md)（設計總覽 §1.3b、§1.4、§2.2、§3.2、§3.2c）。
> SAP 把數量/重量當 **QUAN + UNIT** 家族，鏡像金額的 **CURR + CUKY**——本 plan 即把幣別那套搬到計量單位，是薄疊加。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 定義：`UnitSettings`(define)、`FormField.UnitField`、`DecimalsSource.Unit` | 📝 待做 |
| 2 | 邏輯：數量/重量依各 UNIT 欄 round-then-sum、`UnitSettings` ship client | 📝 待做 |
| 3 | UI：數量/重量 runtime 單位解析、Grid per-cell 擴及單位 + 混單位不合計、DemoCenter | 📝 待做 |

## 階段 1：定義（總覽 §1.3b、§1.4）

- **`UnitSettings`**（系統層 define，類 SAP T006，與 `CurrencySettings` 平行）：`UnitItem{Code(KG/G/PCS/L…); Decimals(KG=3/PCS=0); Dimension(weight/length/volume/count，選用); Name}`；`GetDecimals(code)`（命中/fallback）。**持久化、快取、序列化全比照 `CurrencySettings`**：`DefineType.UnitSettings`、`Get/SaveUnitSettings`、`UnitSettingsCache`、`PathOptions.GetUnitSettingsFilePath()`、`LocalDefineAccess`、Defaults scaffold。物件三棲。
- **`NumberKindProfile.GetDecimalsSource`**：`Quantity`/`Weight` 改回 `Unit`（無綁單位時 fallback `Company`）。
- **UNIT 綁定**：`FormField.UnitField`（數量/重量欄選填，SAP UNIT 參照）。解析優先序（單位通常 per-列、無主檔單位概念）：指定 `UnitField` → 該單位欄值 → `UnitSettings.GetDecimals`；未指定 → 退 `company.GetDecimals(kind)`。
- **測試**：`UnitSettings` 命中（KG→3、PCS→0）/fallback/三棲；含 `UnitField` 的 FormSchema round-trip。

## 階段 2：邏輯（總覽 §2.1、§2.2、§2.3）

- 交付 bake：有綁 `UnitField` 的數量/重量**不 bake**（runtime）；無綁 → 退 `Company`、於交付 bake。
- **捨入入口**：`RoundByKind(value, kind, refCode, ctx)`（ctx 加 `UnitSettings`）——`Round`+數量/重量用 `unitSettings.GetDecimals(refCode)`（refCode = UNIT 欄單位；無單位退 `company.GetDecimals(kind)`）。同單位 round-then-sum。
- `UnitSettings` ship 給 client。
- **測試**：同欄 PCS（0 位）vs KG（3 位）位數不同；走 `UnitSettings`；同單位 round-then-sum、混單位不加總。

## 階段 3：UI（Avalonia，總覽 §3.2、§3.2c）

- **數量/重量 runtime 單位解析**：位數依 `UnitField` 當前值 → client `UnitSettings`；改單位即重算該列。
- **Grid per-cell 擴及單位**：沿用幣別 plan 的 reference-aware `FormatCell`，加單位 resolver（`UnitField` ≡ SAP ALV `QFIELDNAME`）。**顯示**：數量格只顯數字 + 鄰欄獨立單位欄。**混單位合計**：同單位才顯合計、混單位不顯。
- DemoCenter：數量欄不同列不同單位（PCS/KG）→ 位數不同。
- **測試**：清單同欄 PCS/KG 位數正確；改單位該列重算；混單位不顯/同單位顯合計。
- **交付標準**：build + 測試綠；多單位數量/重量顯示正確；DemoCenter 可跑。

## 相容性

- `FormField.UnitField`、`DecimalsSource.Unit`：舊資料/無綁單位 → 退公司位數（同核心 plan 行為）。
- 新增 `DefineType.UnitSettings`：無定義 → fallback 框架預設位數。
