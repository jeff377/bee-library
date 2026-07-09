# 運算式與規則（欄位運算與存檔/刪除前驗證）

用**宣告式運算式**在 `FormSchema` 定義檔裡做欄位運算與驗證，取代手寫 BO 程式碼。客戶/顧問於設計期即可自訂，不需改程式、重編、重佈。

設計背景與決策見 [ADR-028](adr/adr-028-expression-rule-engine.md)。

## 三種能力

| 能力 | 載體 | 時機 |
|------|------|------|
| 計算欄 | `FormField.ValueExpression` | 存檔前對新增/異動列重算回填 |
| 欄位預設值 | `FormField.DefaultValueExpression` | 新增列時，欄位為空才填 |
| 驗證 / 前置檢查 | `FormSchema` 下的 `FormRule` | `BeforeSave` / `BeforeDelete` |

> **後端為權威**：存檔時後端一定依定義重算計算欄並覆蓋前端送來的值；驗證也在後端執行。前端即時運算（規劃中）只是 UX 預覽，正確性不依賴前端。

## 運算式語法

- **變數 = 欄位名**：直接寫欄位名即可，如 `unit_price * qty`。同列所有欄位都可用。
- **運算子**：C# 語法子集（`+ - * /`、`> >= < <= == !=`、`&& || !`、三元 `? :`、字串 `==`）。
- **可用函式/型別**（沙箱白名單）：`Math`（`Math.Round`、`Math.Abs`…）、`Today()`、`Now()`、`IsNullOrEmpty(s)`、`IsNullOrWhiteSpace(s)`、`Guid`（如 `customer_rowid != Guid.Empty`）。
- **禁用**：反射、IO、任意型別載入——未在白名單的識別字會在解析期直接報錯（設定錯誤）。
- **空值**：空欄（`DBNull`）以型別預設值代入（數值 0、字串空字串、`Guid.Empty`…），所以 `unit_price * qty` 遇空值算 0 而不會出錯。

## 計算欄：`ValueExpression`

```xml
<FormField FieldName="amount" Caption="Amount" DbType="Currency"
           NumberKind="Amount" ReadOnly="true"
           ValueExpression="quantity * unit_price * (1 - discount)" />
```

- 存檔前對 `Added` / `Modified` 列重算（`Unchanged` 列不動，避免誤標為已異動）。
- **捨入**：數值結果依欄位 `NumberKind` 捨入（`Amount`→2 位、`Quantity`→0、`UnitPrice`→保留精度…，公司/幣別/單位可調；見 [ADR-026](adr/adr-026-numeric-semantics-rounding.md)）。故明細先各自捨入、加總才不會對不上帳（round-then-sum）。
- 計算欄通常搭配 `ReadOnly="true"`。
- 同列多個計算欄可相依：**依宣告順序**求值，後面的看得到前面剛算好的值。

## 欄位預設值：`DefaultValueExpression`

```xml
<FormField FieldName="order_date" Caption="Order Date" DbType="Date"
           DefaultValueExpression="Today()" />
```

- 新增列時求值；**只在欄位為空時填**，不覆寫已有值。
- 與字面值 `DefaultValue` 併存時，運算式優先。

## 驗證與前置檢查：`FormRule`

```xml
<Rules>
  <FormRule RuleId="customer_required"
            Condition="customer_rowid != Guid.Empty"
            Message="Please select a customer." />
  <FormRule RuleId="quantity_positive" TargetTable="OrderDetail"
            Condition="quantity &gt; 0"
            Message="Quantity must be greater than zero." />
  <FormRule RuleId="approved_amount"
            When="status == &quot;Approved&quot;"
            Condition="total_amount &gt; 0"
            Message="An approved order must have a positive total." />
</Rules>
```

| 屬性 | 說明 |
|------|------|
| `Condition` | **必須成立**的條件（回傳 bool）；為 `false` 即違規，中斷動作並顯示 `Message` |
| `When` | 選填的**適用條件**；空＝一律套用，`false`＝略過整條規則（視同通過），`true` 才檢查 `Condition` |
| `Message` | 不通過時顯示給使用者的訊息 |
| `Trigger` | `BeforeSave`（預設）或 `BeforeDelete` |
| `TargetTable` | 空＝主檔；填明細表名＝對該表**逐列**檢查 |
| `Order` | 同一 trigger 內的求值順序（小的先） |
| `Enabled` | 是否啟用（預設 true） |

> **兩段式判斷**：`When` 決定「這條規則現在該不該檢查」、`Condition` 是「必須成立的驗證」。例：「訂單已核准時，總額必須 > 0」→ `When = status == "Approved"`、`Condition = total_amount > 0`。狀態非 Approved 的單據自動略過。
>
> XML 裡 `>` 要寫 `&gt;`、字串引號要寫 `&quot;`。

## 何時仍需寫 BO（當前邊界）

Phase 1 的運算式是**逐列（per-row）**模型。以下情境還不能純宣告，需在自訂 BO 覆寫 `DoBeforeSave` / `DoBeforeDelete`：

- **跨列聚合**：如「表頭合計 = 明細金額加總」「至少一筆明細」——需跨列運算。
- **需查資料庫**：如「狀態轉移須比對資料庫既存狀態」「自動單號取序列」。

`apps/Bee.Northwind` 的 `OrderBO` 是實例：明細金額與必填檢查已宣告化，只有上述聚合/DB 相依留在 `DoBeforeSave`。

### 自訂 BO 覆寫慣例

```csharp
protected override void DoBeforeSave(SaveContext context)
{
    base.DoBeforeSave(context);   // 先跑規則引擎（預設值 / 計算欄 / BeforeSave 驗證）
    // 再疊上宣告式表達不了的邏輯（聚合、DB 查詢…）
}
```

`Save` / `Delete` 已重構為模板方法：授權、記錄範圍、稽核由框架編排層固定處理，你只覆寫 `DoBeforeSave` / `DoSave` / `DoAfterSave`（及 Delete 對應）需要的那一段。
