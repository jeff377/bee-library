# ADR-028：自訂運算式與規則引擎（減少 BO 手寫程式碼）

## 狀態

已採納（2026-07-09）

## 背景

業務邏輯中大量「欄位運算」與「存檔/刪除前檢查」原本必須在自訂 BO 以 C# 手寫：連「金額 = 單價 × 數量」這種一行公式也要開一個 BO 覆寫 `Save`。痛點有二——樣板碼多、且**客戶無法自訂**（改一條驗證條件就要改程式、重編、重佈）。

目標是讓這類邏輯改以**宣告式運算式**存在 `FormSchema` 定義檔，客戶於設計期即可自訂：

- **欄位運算**：計算欄（`金額 = 單價 * 數量`），存檔前重算回填。
- **存檔前驗證 / 刪除前檢查**：條件不通過顯示訊息、中斷動作。
- **欄位預設值運算式**：新增資料時以運算式產生預設值。

此能力橫跨定義層（`Bee.Definition`）、新求值引擎（`Bee.Expressions`）與商業邏輯層（`Bee.Business`），是框架對外 API surface 的結構性契約，故立此 ADR。完整設計與階段見 `docs/plans/plan-expression-rule-engine.md`；使用者指引見 `docs/expression-rules.md`。

## 考慮過的選項

1. **求值引擎自研 mini parser**：完全可控、零外部相依。**否決**——重造輪子、維運成本高。改採 **DynamicExpresso**（`DynamicExpresso.Core`，MIT）：C# 語法子集直譯器、預設不曝露任何型別（未註冊識別字於 parse 期即報錯，天然沙箱）、可 parse-once 編譯成 delegate 快取。比 Roslyn Scripting 輕、比 NCalc 更貼近 C# 語法。

2. **前端為權威、或前後端各自實作運算**：欄位運算在 UI 即時算完直接送存。**否決**——資料完整性不能託付前端（可被竄改/算錯）。採**後端為唯一權威**：存檔前後端一定依 schema 重算並覆蓋前端送來的計算欄值；前端即時運算是純 UX 預覽。

3. **捨入在運算式引擎內自建（依 `DbField.Scale`）**：就近處理。**否決**——`DbField.Scale` 是建表 DDL 精度，與業務捨入是兩套系統（見 ADR-026）。計算欄數值結果**委派既有 `NumberFormatResolver.RoundByKind`**（依 `NumberKind`），免費繼承公司/幣別/單位可調位數與 round-then-sum；引擎本身只算全精度、對 NumberKind 無知，保持可攜。

4. **子類別覆寫整個 `Save`（現況）**：沿用既有擴充方式。**否決**——「override 整包 `Save`」讓框架日後擴增功能與子類覆寫互相打架，且每個自訂點都要重抄授權/稽核樣板。改把 `Save`/`Delete` 重構為**模板方法**（見下）。

5. **`FormRule` 適用性用內嵌蘊含式（`!When || Condition`）**：少一個欄位。**否決**——蘊含式對客戶/顧問設定者易靜默寫反。改用結構化的選填 `When`（適用條件）+ `Condition`（驗證條件）兩段式，並取名 `When`（對齊 .NET FluentValidation `.When()`，避開 Design-by-Contract 中「precondition 不成立＝錯誤」的語意落差）。

## 決策

- **定義層**：`FormField` 新增 `ValueExpression`（計算欄）、`DefaultValueExpression`（預設值運算式）；`FormSchema` 新增 `FormRule` 集合（`When` / `Condition` / `Message` / `Trigger`＝`BeforeSave`｜`BeforeDelete` / `TargetTable` / `Enabled` / `Order`）。`FormSchema` 以 **XML 為唯一傳輸序列化路徑**（後端 `XmlCodec.Serialize` → 前端 `XmlCodec.Deserialize`）。

- **求值引擎（`Bee.Expressions`，可攜共用）**：只依賴 `Bee.Base` + DynamicExpresso，無 server-only 相依，供後端 BO 與未來前端共用同一引擎與同一 `ExpressionPolicy`（型別對映、`DBNull`→型別預設 0/空），確保前端預覽值 = 後端存檔值。沙箱只曝露欄位變數 + 白名單函式（`Today`/`Now`/`IsNullOrEmpty`）+ `Guid`。

- **BO 生命週期（模板方法）**：`FormBusinessObject.Save`/`Delete` 重構為編排層——授權（`AuthorizeSave`）、記錄範圍（`EnforceWriteScope`）、稽核**固定不可覆寫**；中間開 `DoBeforeSave`/`DoSave`/`DoAfterSave`（及 Delete 對應）三個 `protected virtual` 覆寫點。基底 `DoBeforeSave` 依 schema 自動套用預設值 → 計算欄（委派 `RoundByKind` 捨入）→ `BeforeSave` 驗證；一般 CRUD 表單**零 BO 程式碼**。子類覆寫 `Do*` 時先呼叫 `base.Do*` 再疊自訂。

- **後端為權威**：前端即時運算（Phase 2，首要 Avalonia）為 UX 加分；存檔以後端重算為準，前端算不了（AOT 邊界）最壞退回無預覽、正確性不受影響。

## 影響

- **正面**：客戶可純靠定義做欄位運算與存檔/刪除前驗證，不寫 BO；框架擴增功能落在編排層或特定 `Do*`，不與子類覆寫打架；捨入沿用單一數值子系統（round-then-sum 一致）。

- **示範**：`apps/Bee.Northwind` 的 `OrderBO` 由「override `Save`」遷移為「override `DoBeforeSave`」——明細金額改 `ValueExpression`、客戶/產品/數量必填改 `FormRule`；僅「至少一筆明細」「表頭合計（跨列 SUM）」「狀態轉移（需查存量狀態）」「單號產生（需 DB 序列）」留在 `DoBeforeSave`，清楚標定宣告式的當前邊界。

- **邊界（另案，見 plan 延後範圍）**：跨列/明細聚合（`SUM(detail)`）、虛擬顯示計算欄、`BeforeInsert`/`BeforeUpdate` 更細觸發、多捨入模式（銀行家/無條件捨去/進位，屬數值子系統擴充）、求值 timeout 與 Session 變數曝露。

- **相依**：新增第三方套件 `DynamicExpresso.Core`（MIT）。引擎經 `Expression.Compile()`，行動端/WASM AOT 目標的即時運算需另行實測（同 ADR-025 的 trim/AOT 脈絡）——但因後端為權威，此風險僅影響前端預覽、不影響資料正確性。
