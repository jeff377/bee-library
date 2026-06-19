# 計畫：欄位標題色彩統一標示唯讀（棕）與必填（藍）— 主檔 + 明細

**狀態：✅ 已完成（2026-06-17）**

> **決策結果**：A1 顯式 `Required` 屬性（鏡像 ReadOnly）；唯讀（棕 `#A0522D`）優先於必填（藍 `#2563EB`）；色彩集中於共用 `FieldCaptionStyle`，主檔（FormView）與明細（GridControl）共用。顏色經使用者目視確認。

## 目標

對**主檔欄位標題**與**明細欄表頭**統一以「標題文字顏色」標示欄位狀態：

- **唯讀** → 棕色（沿用 GridControl 已採的 `#A0522D`）
- **必填** → 藍色（新增，色值待定）

讓使用者掃一眼標題即知哪些欄位唯讀、哪些必填，主檔與明細規則一致。

## 現況（探查結論）

| 項目 | 位置 | 現況 |
|------|------|------|
| 主檔欄位標題 | `FormView.BuildFieldCell()`（`src/Bee.UI.Avalonia/Views/FormView.cs:~419`） | 純 `TextBlock { Text = field.Caption }`，無上色 |
| 明細欄表頭 | `GridControl.BuildColumnHeader()`（`src/Bee.UI.Avalonia/Controls/GridControl.cs`） | 唯讀欄已上棕色，**必填未做** |
| 唯讀屬性 | `LayoutFieldBase.ReadOnly`（`LayoutField` / `LayoutColumn` 共同繼承） | ✅ 已有；generator 經 `LayoutColumnFactory` 從 `FormField.ReadOnly` 傳遞 |
| **必填屬性** | — | ❌ **定義層不存在**。`FormField` / `LayoutFieldBase` 皆無 Required/NotNull/AllowNull |
| DB 層必填 | `DbField.AllowNull`（`AllowNull=false` 即必填，預設 false） | 存在於 TableSchema，未向上傳到 form/layout |
| 標題建構共用 | — | 主檔與明細**各自獨立**，無共用邏輯 |

> 註：執行期 `FormDataObject` 的 ADO.NET `DataColumn.AllowDBNull` 也反映 NotNull，是另一個潛在的 render-time 必填來源。

## 待決策（架構面，需使用者拍板）

### 決策 A：「必填」來源（最關鍵，決定改動範圍）

| 選項 | 做法 | 取捨 |
|------|------|------|
| **A1 顯式屬性** | 新增 `FormField.Required` + `LayoutFieldBase.Required`（鏡像 ReadOnly），`LayoutColumnFactory.ToField/ToColumn` 傳遞 | 最清楚、與 ReadOnly 對稱、可序列化；但既有 demo 定義要補標 `Required` |
| **A2 由 schema NotNull 推導** | 由 TableSchema `DbField.AllowNull=false` 自動推導必填，不新增定義屬性 | 零補標、單一真相（schema 已定 NotNull）；但需在 generate/render 時取得對應 schema 的 nullable，要確認 plumbing（generator/FormView 拿不拿得到 TableSchema 或 DataColumn.AllowDBNull） |
| **A3 兩者並用** | `LayoutFieldBase.Required` 為 UI 契約，generator 從 schema NotNull 自動帶入，author 可顯式覆寫 | 最完整（自動 + 可覆寫）；最多工 |

### 決策 B：唯讀 vs 必填同時成立時的優先序

唯讀欄不可輸入，「必填」對它無意義。建議**唯讀（棕）優先**於必填（藍）。待確認。

### 決策 C：必填的藍色色值

提一個起始藍（如 `#1E6FD9` 一類），與棕 `#A0522D` 對比清楚、Light/Dark 皆可讀；上線後依目視微調（同底線深度的迭代方式）。

## 實作大綱（待決策 A 定案後細化）

1. **（若 A1/A3）定義層加 `Required`**：`LayoutFieldBase.Required`（`LayoutField`/`LayoutColumn` 自動繼承）+ `FormField.Required`，序列化比照 `ReadOnly`（`[XmlAttribute]` + `[DefaultValue(false)]`）。
2. **generator 傳遞**：`LayoutColumnFactory.ToField/ToColumn` 加 `Required = field.Required`（A1）或從 schema NotNull 帶入（A2/A3）。
3. **共用標題上色 helper**：抽一個依 `(ReadOnly, Required)` 回傳標題 Foreground 的共用邏輯（放 `Bee.UI.Avalonia`），主檔與明細共用，避免兩套色規。
4. **FormView 主檔標題**：`BuildFieldCell` 的 caption TextBlock 套用上色 helper。
5. **GridControl 表頭**：`BuildColumnHeader` 擴充必填藍（現有唯讀棕保留），改用共用 helper。
6. **測試**：FormView 標題色 + GridControl 表頭色，依 ReadOnly/Required 組合驗證；色值用具名常數便於調。
7. **Gallery 目視**：標一個必填欄、一個唯讀欄對照。

## 風險

- **定義層改動**（決策 A1/A3）會動到 `Bee.Definition`（序列化、generator），屬 API surface 變更，需審慎。
- **A2 推導的 plumbing**：要確認 FormView/generator 在該時點能取得 schema 的 nullable，否則退回 A1。
- 色彩可及性：純靠顏色辨識對色盲不友善（與 GridControl 唯讀棕同樣的既有取捨）；本案沿用使用者選定的顏色方案。
