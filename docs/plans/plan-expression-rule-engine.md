# 計畫：自訂運算式與規則引擎（減少 BO 手寫程式碼）

**狀態：🚧 進行中（2026-07-09）— Phase 1（後端）已全數完成；Phase 2（Avalonia 前端即時運算）進行中**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1a | 定義層：`FormField` 運算式屬性 + `FormRule` 規則集合 + 序列化往返 | ✅ 已完成（2026-07-09） |
| 1b | `Bee.Expressions`**可攜共用**求值引擎（DynamicExpresso 封裝 + 快取 + 沙箱 + 型別/null + 相依分析） | ✅ 已完成（2026-07-09） |
| 1c | BO 生命週期整合：Save/Delete 模板方法 + `IFormRuleProcessor`（預設值 / 欄位運算 / 存檔前驗證 / 刪除前檢查，schema 驅動、零 per-form 程式碼） | ✅ 已完成（2026-07-09） |
| 1d | 文件（ADR-028 + expression-rules.md）+ Northwind OrderBO 遷移示範 | ✅ 已完成（2026-07-09） |
| 5a | 抽共用 row-level 計算器（`FormExpressionCalculator`）+ 相依圖 + 前端服務骨架 + 測試 | ✅ 已完成（2026-07-09） |
| 5b | 綁定接線：訂閱 `FieldValueChanged` 重算回寫 + guard + `DefaultValueExpression` | ✅ 已完成（2026-07-09） |
| 5c | （可選）Tier 2：client 取 `CurrencySettings`/`UnitSettings` 逐位對齊 | 📝 待做 |
| 5d | WASM/行動端 AOT 實測 + graceful degrade + Northwind demo-smoke | 📝 待做 |

> **Phase 1（後端）** 涵蓋四類規則：**欄位運算（計算欄）**、**存檔前驗證**、**刪除前檢查**、**欄位預設值運算式**，執行於伺服器端存檔/刪除前。
> **Phase 2（前端即時運算）** 讓使用者在 Avalonia UI 邊打邊看到計算結果；**後端仍為權威**，前端即時運算為 UX 加分、不影響正確性。
> Phase 1 的引擎專案**一開始就設成前後端可攜共用**，Phase 2 不需回頭重構。

## 背景與目標

目前業務邏輯（欄位運算、存檔/刪除前檢查）必須在自訂 BO 以 C# 手寫。痛點：

- 簡單如「金額 = 單價 × 數量」也要開一個 BO override `Save`。
- 客戶無法自訂驗證條件與錯誤訊息（改邏輯就要改程式、重編、重佈）。

目標：以**宣告式運算式**（存在 FormSchema 定義檔）取代大量 BO 樣板碼，並讓客戶可自訂：

- **欄位運算**：`金額 = 單價 * 數量`（存檔前重算、回填、依欄位 Scale 捨入）。
- **存檔前驗證**：條件不通過 → 顯示錯誤訊息、中斷存檔。
- **刪除前檢查**：條件不通過 → 中斷刪除。
- **欄位預設值運算式**：新增資料時以運算式產生預設值。

## 現況探勘結論（動筆前已確認）

- **定義層是綠地**：`FormField`（`src/Bee.Definition/Forms/FormField.cs`）目前只有 `ReadOnly` / `Required` / `DefaultValue`（字面值），**無** expression / formula / validation 載體。`FieldType.VirtualField` enum 值存在但無對應運算式欄位。
- **BO 缺生命週期 hook**：框架**沒有** `BeforeSave` / `ValidateData` / `BeforeDelete` 分離點。現況唯一做法是 override `FormBusinessObject.Save` / `Delete`。本計畫會順帶補上這些 hook。
  - `Save`（`src/Bee.Business/Form/FormBusinessObject.cs:212`）流程：`AuthorizeSave`（權限）→ `EnforceWriteScope`（記錄範圍）→ `repository.Save(dataSet)` → 以 master `sys_rowid` 重讀回傳。**插入點：`AuthorizeSave` 之後、`repository.Save` 之前**。
  - `Delete`（:255）只拿到 `rowId` + `scopeFilter`，**不含 DataSet**；刪除前檢查需先 `repository.GetData(rowId)` 載入該筆再求值。
- **資料載體**：DataSet（1 master + N detail，detail 以 `sys_master_rowid` 關連 master `sys_rowid`）。欄位讀寫已有 `row.GetFieldValue<T>(col)`（`src/Bee.Base/Data/DataRowExtensions.cs`）、原生 `row["field"]`。系統欄常數 `SysFields.RowId` / `MasterRowId`。
- **中斷 + 訊息機制已就緒**：`throw new UserMessageException(msg)`（`src/Bee.Base/Exceptions/UserMessageException.cs`）＝框架指定的「面向使用者業務中斷訊號」，JSON-RPC 以 `JsonRpcErrorCode.UserMessage` 回傳原訊息。驗證不通過即用它中斷。
- **序列化慣例**：定義類別以 `[XmlAttribute]` 持久化、JSON 走 System.Text.Json、MessagePack 走 contractless；集合繼承 `KeyCollectionBase<T>` / 項目繼承 `KeyCollectionItem`，MessagePack 集合須顯式註冊 `CollectionBaseFormatter`（見 `bee-serialization` skill 與 `src/Bee.Api.Core/MessagePack/FormatterResolver.cs`）。

## 引擎選型：DynamicExpresso（已確認）

NuGet 套件 `DynamicExpresso.Core`（MIT）。選它的理由：

- **C# 語法子集直譯器** — `UnitPrice * Quantity`、`Amount > 0`、`string.IsNullOrEmpty(Name)` 直接可寫，貼近開發者與客戶直覺。
- **安全可控** — 預設不曝露任何型別/識別字；未註冊的識別字在 parse 期即報錯。反射、IO、`Type`、`Activator` 全不註冊 → 天然沙箱。
- **可快取** — `Interpreter.Parse(expr, parameters…)` 產出 `Lambda`（編譯後 delegate），parse-once、逐列 `Invoke`。
- **輕量** — 無 Roslyn 啟動與記憶體成本；比 NCalc 更貼近 C# 語法、函式擴充能力更強。

引擎以 `IExpressionEvaluator` 抽象包裝，DynamicExpresso 僅出現在單一實作類別 → 可替換、可測、且未來前端可共用同一抽象。

## 設計

### 1. 定義模型（運算式存哪裡）

**A. 欄位級 — 掛在 `FormField`（新增兩個 `[XmlAttribute]` 屬性）**

| 屬性 | 型別 | 語意 |
|------|------|------|
| `ValueExpression` | `string?` | 計算欄運算式。存檔前重算並回填此欄（通常搭配 `ReadOnly=true`）。首階段為**已儲存欄**（真實 DbField），非虛擬欄。 |
| `DefaultValueExpression` | `string?` | 新增資料時以運算式產生預設值。與既有字面值 `DefaultValue` 互補；兩者並存時運算式優先。 |

**B. 規則級 — 新增 `FormRule` / `FormRuleCollection`，掛在 `FormSchema`**

驗證/檢查不綁單一欄位、需看整列（或整個 DataSet）並產生錯誤訊息，故獨立為規則集合：

```
FormSchema
  └── Rules : FormRuleCollection?          // 新增
        └── FormRule (: KeyCollectionItem)
              ├── RuleId       (string, =Key)
              ├── Trigger      (FormRuleTrigger enum)   // 首階段：BeforeSave / BeforeDelete
              ├── TargetTable  (string?)                // 針對哪個 DataTable；空=master
              ├── When         (string?)                // 適用條件；空=一律套用；false=略過此規則
              ├── Condition    (string)                 // 必須為 true 才通過；false=違規
              ├── Message      (string)                 // 不通過顯示的訊息（可為 LanguageResource key）
              ├── Enabled      (bool, default true)
              └── Order        (int)                    // 多規則求值順序
```

- **兩段式判斷**：
  1. **`When`（適用條件）** — 決定「這條規則現在**該不該檢查**」。
     - 空 / null → **一律套用**（無適用條件）。
     - 求值為 `false` → **略過此規則**（視同通過，不檢查 `Condition`）。
     - 求值為 `true` → 進入第 2 步。
  2. **`Condition`（驗證條件）** — 「**必須成立**的條件」。求值為 `false` → 違規 → `throw UserMessageException(Message)`。
- **範例**：「訂單已核准時，金額必須大於 0」→ `When = Status == "Approved"`、`Condition = Amount > 0`、`Message = "已核准訂單金額必須大於 0"`。狀態非 Approved 的單據則此規則自動略過。
- `When` 與 `Condition` 共用同一求值 context（欄位變數、Session、白名單函式），皆須回傳 `bool`。兩者都放共用 `Bee.Expressions` 走同一沙箱與政策。
- **detail 規則**：`TargetTable` 指向 detail 表時，`When` + `Condition` 對該表**逐列**求值，任一列適用且驗證不通過即中斷（訊息可帶列號，後續強化）。
- `FormRuleTrigger` enum（首階段兩值，後續加 `BeforeInsert` / `BeforeUpdate`）；依 `code-style` 集合語意 enum 末尾加 `s` 的規範，enum 名用單數 `FormRuleTrigger`（單一觸發點，非旗標集合）。
- **命名**：適用性 guard 欄位取名 `When`，對齊 .NET 生態 FluentValidation `.When()` 慣例、對設定者友善。刻意不用 `Precondition`——Design by Contract 中「precondition 不成立＝錯誤」，與本設計「`When` 不成立＝略過（非錯誤）」語意相反，易誤導。`When`（資料條件）與 `Trigger`（存檔/刪除生命週期事件）分工明確、不混淆。
- **為何用結構化 `When` 而非內嵌蘊含式**：`When + Condition` 邏輯上等價於單一 `!When || Condition`，但後者對客戶/顧問設定者易靜默寫反（漏 `!`、`&&`/`||` 混淆、`When && Condition` 反向擋掉非適用單據）。結構化兩格：(1) 降低蘊含邏輯寫錯率；(2) 利於未來設定 UI 逐格輸入與驗證；(3) 可分辨「不適用而略過」與「通過」兩種狀態，供稽核與 Phase 2 前端動態提示。`When` 選填 → 簡單規則零負擔，等同單一 `Condition`。`Condition` 本身仍可用 `?:` / 函式做更細判斷，兩者不衝突。

**序列化**：`FormSchema` 以 **XML 為唯一傳輸序列化路徑**（後端 `XmlCodec.Serialize` → 前端 `XmlCodec.Deserialize<FormSchema>`），不涉 JSON / MessagePack 物件路徑。`FormField` 兩屬性走 `[XmlAttribute]`；`FormRule` 為 `KeyCollectionItem`（無參數 ctor + `[XmlAttribute]` 純量），`FormRuleCollection : KeyCollectionBase<FormRule>` 與 `FormTableCollection` 同構——無需改 `FormatterResolver`。附 XML round-trip 測試。

### 2. 求值引擎（新專案 `src/Bee.Expressions/`，**前後端可攜共用**）

**專案定位**：`net10.0`、**不依賴任何 server-only 型別**（不引用 Bee.Business / Bee.Repository / DB / DataSet-persist 相關）。DynamicExpresso 相依只出現在此專案。`Bee.Business`（後端）與未來各前端（Avalonia 等）**引用同一份引擎與同一份政策** → 前端即時算與後端存檔前重算**逐位一致，不會漂移**。輸入以中性的「變數名→值」對映 + 期望回傳型別表達，故可攜。

- **`IExpressionEvaluator`**（抽象）
  - `object? Evaluate(string expression, IReadOnlyDictionary<string, object?> variables, Type returnType)`
  - 便利多載：`T Evaluate<T>(...)`
- **`DynamicExpressoEvaluator`**（唯一引擎實作）
  - **編譯快取**：以 `(expression text + 參數簽章)` 為 key 快取 `Lambda`；參數簽章由 schema 欄位集合推導 → 同一 FormSchema 的同一運算式全程只 parse 一次。前後端各自持有快取。
  - **沙箱**：只註冊「欄位變數 + 白名單函式（`Math`、字串/日期常用、ERP helper）+ 唯讀 Session context」。反射/IO/型別載入一律不註冊。
- **`ExpressionPolicy`（型別/null，前後端共用同一份，杜絕漂移）**
  - **型別對映**：`FieldDbType → CLR`（沿用既有 `src/Bee.Base/Data/DbTypeConverter.cs`；String→string、Integer→int、Decimal/Currency→decimal、Date/DateTime→DateTime、Boolean→bool、Guid→Guid、Long→long、Short→short）。
  - **null 政策**：空值 → 該型別 CLR 預設（數值 0、字串空字串）。對齊「文字/數值欄偏好 NOT NULL + 預設 0/空」的資料模型（記憶 `tableschema-addcolumn-allownull`），讓 `單價*數量` 遇空值算 0 而非爆 null。
  - **求值產出全精度**：引擎只算出全精度 `decimal`，**本身不做捨入**（保持對 NumberKind/公司無知、乾淨可攜）。捨入在寫回步驟委派既有數值子系統（見下）。
- **捨入：委派既有 `NumberFormatResolver.RoundByKind`（不自建、不用 `DbField.Scale`）**
  - `DbField.Scale` 是建表 DDL 精度，與業務捨入是兩套系統——**不可**拿來當捨入依據。
  - 計算欄全精度結果寫回 DataRow 前，呼叫 `NumberFormatResolver.RoundByKind(raw, field.NumberKind, roundingContext, refCode)`（`src/Bee.Definition/NumberFormatResolver.cs`）。
    - `refCode`：Amount 取該列 `field.CurrencyField` 幣別碼、Quantity/Weight 取 `field.UnitField` 單位碼（同一 DataRow 讀得到）。
    - `roundingContext`（`RoundingContext`）：後端由 `CompanyInfo` 組；前端由已 bake/已載入定義組。
  - 免費繼承：每種 `NumberKind` 不同小數位、公司可調位數、幣別/單位 master 驅動、**round-then-sum 不變式**、`UnitPrice/Cost/ExchangeRate` 用 `Preserve` 保留精度——全部沿用現成。
  - `NumberFormatResolver` 位於 `Bee.Definition`（前端本就載入）→ Phase 2 前端預覽捨入 = 後端存檔捨入，一致性順帶成立。
  - 目前實際 midpoint 模式為單一四捨五入（AwayFromZero）；多捨入模式（銀行家/無條件捨去/進位）屬數值子系統另案，運算式引擎解耦、屆時自動受益（見「延後範圍」）。
- **相依分析（供前端知道「改哪個欄要重算哪個欄」）**
  - 用 DynamicExpresso `Interpreter.DetectIdentifiers(expr)` 取出運算式引用的欄位 → 從 schema 建「來源欄位 → 受影響計算欄」相依圖。
  - 後端 Phase 1 用不到相依圖（存檔前一次全算），但此分析放共用專案、Phase 1 就實作 + 測試，Phase 2 前端直接取用。
- **後端資料橋接**（`Bee.Business` 內，非共用專案）：把 `DataRow` 依 `ExpressionPolicy` 轉成 `variables` 字典交給 evaluator，算出全精度結果後委派 `NumberFormatResolver.RoundByKind` 捨入再寫回 `DataRow`。此橋接才碰 `DataSet` 與 `CompanyInfo`，故留在後端。
- 單元測試：運算正確性、型別對映、null→預設、委派 `RoundByKind` 捨入（各 NumberKind 位數、Preserve、round-then-sum）、快取命中、`DetectIdentifiers` 相依圖、沙箱（未註冊識別字/反射 → parse 期擋下）。

### 3. BO 生命週期整合：Save / Delete 模板方法重構（schema 驅動、零 per-form 程式碼）

把 `FormBusinessObject.Save` / `Delete` 重構為**模板方法**：框架關注點（授權 / 記錄範圍 / 稽核）固定在編排層，中間開三個 `protected virtual` 覆寫點；基底實作依 FormSchema 自動套用規則引擎——一般 CRUD 表單零 BO 程式碼，自訂 BO 覆寫 `Do*` 疊加。

**設計決策（2026-07-09 確認）**
- 框架關注點（`AuthorizeSave` / `EnforceWriteScope` / change-audit 快照與寫入）**留在編排層固定、不可覆寫** → 子類無法因覆寫誤拿掉安全/稽核。
- `Do*` 方法收 **Context 物件**（`SaveContext` / `DeleteContext`，承載 args / DataSet / repository / schema / 結果槽），日後擴充不改簽章、`DoAfter*` 看得到結果。
- `Save` / `Delete` **保持 `public virtual`（附加式、不破壞既有外部覆寫）**；`Do*` 為推薦擴充點。
- **為何拆三段而非 override 整個 `Save`**：三個子方法把業務邏輯的控管點（前置 / 持久化 / 後置）明確標定，框架日後擴增功能時改動落在編排層或特定 `Do*`，不會與子類別「override 整包 `Save`」互相打架；子類也只覆寫需要的那一段、其餘沿用框架實作。

**`Save` 編排層順序（框架固定 = 【】）**
1. 參數檢查
2. 【`AuthorizeSave` + 條件式 `EnforceWriteScope`】
3. `DoBeforeSave(ctx)` — base：規則引擎（見下）
4. 【change-audit 快照】（在計算後、寫入前，`GetChanges` 反映計算結果）
5. `DoSave(ctx)` — base：`repository.Save` → `ctx.RefreshedDataSet` / `AffectedRows`
6. 【change-audit 寫入】
7. `DoAfterSave(ctx)` — base：no-op
8. 回傳 `SaveResult`（取自 ctx）

base `DoBeforeSave` 依序（委派 `IFormRuleProcessor`）：
- **預設值運算式** — 對 `Added` 列，套用有 `DefaultValueExpression` 的欄位（僅在欄位為空時填）。
- **欄位運算** — 對 `Added` + `Modified` 列，重算有 `ValueExpression` 的欄位，全精度求值後委派 `NumberFormatResolver.RoundByKind` 捨入回填；master / detail 各表逐列。
- **存檔前驗證** — 依 `Order` 求值 `Trigger=BeforeSave` 的 `FormRule`（先 `When` 適用性、再 `Condition`）；首個違規 `throw UserMessageException(Message)` 中斷。

**`Delete` 編排層順序**
1. 參數檢查
2. 【`Authorize(Delete)` + 解析 scope filter】
3. 【載入刪前快照一次】（稽核 或 有 `BeforeDelete` 規則時才載，放 `ctx.Snapshot` 共用）
4. `DoBeforeDelete(ctx)` — base：對 `ctx.Snapshot` 求值 `Trigger=BeforeDelete` 規則 → 違規 `throw UserMessageException`
5. `DoDelete(ctx)` — base：`repository.Delete` → `ctx.RowsAffected`
6. 【delete-audit 寫入】
7. `DoAfterDelete(ctx)` — base：no-op

**覆寫慣例**：子類 `override DoBeforeSave(ctx)` 時**先呼叫 `base.DoBeforeSave(ctx)`**（取得規則引擎），再疊自訂邏輯。

**規則處理器**：`IFormRuleProcessor`（Bee.Business 內，注入 `IExpressionEvaluator` + `IDefineAccess` + 捨入所需 company/number context）承載預設值 / 計算 / 驗證。DI 註冊 `IExpressionEvaluator`→`DynamicExpressoEvaluator`（singleton）與 processor。

**示範遷移**：`apps/Bee.Northwind` 的 `OrderBO.Save`（現為 override Save：Validate + ComputeAmounts + AssignOrderNumber + status 規則）遷移到新結構——金額計算改 `ValueExpression`、金額>0 / 狀態轉移改 `FormRule`，剩餘不可宣告化的（`AssignOrderNumber`、查存量狀態）進 `DoBeforeSave` override。作為零/少程式碼的實證（PR4）。

### 4. 沙箱與安全模型

- 運算式來源＝**DefinePath 定義檔（管理者/客戶於設計期撰寫）**，非終端使用者即時輸入 → 威脅模型為「設定錯誤/惡意設定」，風險中低。
- 仍以白名單沙箱防止意外資料存取：只曝露欄位變數 + 白名單函式 + 唯讀 Session；不曝露反射、IO、DB、`Type`、`Activator`。
- 運算式僅為**表達式**（無迴圈/無語句），ReDoS 風險低；如日後開放高風險函式再評估求值 timeout。
- 遵守 `scanning.md`：不以字串拼接組 SQL（本引擎不碰 SQL）、不 catch 基底 `Exception`（parse/eval 失敗捕捉 DynamicExpresso 專屬例外並轉 `UserMessageException` 或設定錯誤例外）。

### 5. i18n

`FormRule.Message` 首階段支援「字面訊息」與「LanguageResource key」兩種：非空且能於現有語系資源解析到 → 用資源值，否則用字面。沿用既有 `FormSchemaLocalizer` 慣例（`bee-scaffold-from-formschema` skill 的 sub-key 規範）。

## 交付切分（PR）

| PR | 階段 | 內容 | 主要檔案 |
|----|------|------|---------|
| PR1 | 1a | 定義層 + XML 序列化往返測試 | `FormField.cs`、新 `FormRule.cs` / `FormRuleCollection.cs` / `FormRuleTrigger.cs`、`FormSchema.cs`（`FormRuleCollection` 與 `FormTableCollection` 同走 `KeyCollectionBase`；FormSchema **以 XML 為唯一傳輸序列化路徑**——後端 `XmlCodec.Serialize` → 前端 `XmlCodec.Deserialize`，不涉 JSON / MessagePack 物件路徑） |
| PR2 | 1b | `Bee.Expressions` **可攜共用**專案（引擎/快取/policy/相依分析/沙箱）+ 單元測試 | 新 `src/Bee.Expressions/*`、slnx 整合、`Directory.Build.props` |
| PR3 | 1c | Save/Delete 模板方法重構（`DoBeforeSave`/`DoSave`/`DoAfterSave` + Delete 對應，Context 物件、框架關注點編排層固定）+ `IFormRuleProcessor`（4 類規則、DataRow 橋接、委派 `RoundByKind`）+ DI + 測試 | `FormBusinessObject.cs`、新 `SaveContext`/`DeleteContext`、`IFormRuleProcessor` / 實作、DI 註冊 |
| PR4 | 1d | 文件 + Northwind 示範 + ADR | `docs/adr/00xx-expression-rule-engine.md`、Northwind FormSchema、README |
| 5a–5d | 2 | Avalonia 前端即時運算（詳見下方「Phase 2 執行級規格」）：加 `Bee.Expressions` 參考 + 相依圖 + 共用 row-level 計算器（5a）；訂閱 `FormDataObject.FieldValueChanged` 重算回寫 + `DefaultValueExpression`（5b）；client 取捨入設定 Tier 2（5c）；AOT 實測 + demo-smoke（5d）| `Bee.UI.Avalonia`（`FormDataObject`/`FormView`/`GridControl`/`NumericEdit`）、`ClientDefineAccess`、共用計算器 |

每個 PR 於本機 `dotnet build` + `dotnet test` 通過後直接推 main（依 `pull-request.md` 桌面環境慣例）。

## Phase 2 前端即時運算（Avalonia）— 執行級規格

> 探勘結論見對話記錄；以下為可直接開工的實作規格。目標：使用者在表單/明細編輯欄位時，計算欄即時反映（邊打邊算），共用後端同一引擎與政策；**後端存檔前重算仍為唯一權威**，前端算不了最壞退回無預覽、不影響正確性。

### 現有架構關鍵點（探勘所得）

| 面向 | 位置 | 說明 |
|------|------|------|
| 資料承載 | `src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs` | 持有單筆 `DataSet`（1 master + N detail），無 ViewModel；欄位直接綁 DataRow |
| **統一變更事件** | `FormDataObject.FieldValueChanged`（L117） | 由 ADO.NET `ColumnChanged` bridge 發出，**master / detail / lookup / 程式寫入全涵蓋** → 即時運算的唯一訂閱入口。EventArgs 帶 `TableName / FieldName / Value / Row` |
| 取 FormField（帶 `ValueExpression`） | `FormDataObject.GetFormField(table, field)`（L347） | 編輯器綁的是 `LayoutField`（不帶運算式），運算式在 schema 層 `FormField` |
| master 回寫 + UI 更新 | `FormDataObject.SetField(...)` → 觸發 `FieldValueChanged` → 各編輯器 `FieldEditorBinder.OnFieldValueChanged`（L290）`RefreshFromSource` | **master 欄位程式回寫會自動刷新顯示**（走 property-changed，非控件事件）|
| detail cell 回寫 | `GridControl.WriteCell`（L1043）→ `row[col]=` | 同觸發 `FieldValueChanged`；**但 realized cell 不追蹤後續 DataRow 寫入 → 明細計算欄回寫後須呼叫 `GridControl.RefreshRows()`（L340）** |
| row-edit 壓制窗口 | `FormDataObject.BeginRowEdit`/`CommitRowEdit`（L236/252） | ADO.NET `BeginEdit` 期間事件被 `_rowsInEdit`（L29）壓住，`CommitRowEdit` 才重播 → dialog 內即時算需考慮此窗口 |
| 捨入設定注入（**缺口**） | `GridControl.CurrencySettings/UnitSettings`（L121/138）、`NumericEdit`（L47/60）有 setter 但 **`FormView` 從未注入**；`ClientDefineAccess` **無** `GetCurrencySettings/GetUnitSettings` | client 端目前拿不到公司/幣別/單位位數 → 見 PR 5a 前置 |
| csproj | `src/Bee.UI.Avalonia/Bee.UI.Avalonia.csproj` | 已引用 `Bee.Definition`；**未引用 `Bee.Expressions`** → 需新增 |

### 設計決策（2026-07-09 確認）

1. **共用重算邏輯落點 → 抽到 `Bee.Definition.Forms`（採用）**：後端 `FormRuleProcessor` 的計算核心（`ApplyDefaults`/`ApplyComputed`/`ValidateRules`/`BuildVariables`/`ResolveRefCode`）是**純函式**，只依賴 `System.Data` + `Bee.Definition`（`FormSchema`/`NumberFormatResolver`/`RoundingContext`）+ `Bee.Expressions`，無任何 server-only 相依。`Bee.Definition.Forms` 已有 `FormRowDefaults`（schema 驅動、操作 `DataRow`、前後端共用）的前例，故新增 `FormExpressionCalculator` 於此；`Bee.Definition` 加 `Bee.Expressions` ProjectReference（`Bee.Expressions` 僅依賴 `Bee.Base`，無循環）。後端 `FormRuleProcessor` 瘦身為 adapter（保留 `IFormRuleProcessor` + DI + `RoundingContext` 組裝 + `UserMessageException` 轉譯），前端即時運算服務委派同一 calculator → 杜絕漂移、免開新專案。
2. **client 捨入 fidelity → Tier 1 先做（採用）**：前端用**框架預設位數**（`NumberKind` 預設：Amount 2、Quantity 0…；`RoundingContext` 三個來源皆 null → `NumberFormatResolver` 自動 fallback 框架預設）捨入預覽，免動 client API、快速可用；與最終值僅在「公司自訂位數」邊緣情境有差，存檔由後端校正。**Tier 2（5c 可選）**：新增 client 取得 `CurrencySettings`/`UnitSettings`（+ 公司位數）→ 注入 `RoundingContext` → 與後端逐位一致。
3. **re-entrancy → 計算中旗標 + 計算欄不當觸發源 + 依附 CommitRowEdit 重播（採用）**：即時運算服務內維護 `_recomputing` 旗標，重算回寫計算欄再觸發的 `FieldValueChanged` 於旗標開啟時忽略；相依圖只由「非計算欄來源」驅動（計算欄自身變動不再引發重算）；`BeginRowEdit` 壓制窗口內事件本被 ADO.NET 壓住，改在 `CommitRowEdit` 重播時一併重算，不特別侵入壓制窗口。

### 相依驅動重算

- 用 `IExpressionEvaluator.GetReferencedVariables`（`DetectIdentifiers`）從 schema 建「來源欄位 → 受影響計算欄」相依圖（每 FormSchema 建一次、快取）。
- `FieldValueChanged(field)` → 查相依圖取受影響計算欄 → 依相依拓樸/宣告順序重算 → `SetField`（master）或 `row[col]=` + `RefreshRows()`（detail）。
- **DefaultValueExpression**：`FormDataObject.NewAsync` 產生新列後，對有 `DefaultValueExpression` 的空欄即時套用（顯示層預設，與後端存檔預設互補）。

### AOT 風險分級

- **Avalonia 桌面** ✅ 無虞。
- **Avalonia WASM / 行動端 AOT**：DynamicExpresso 走 `Expression.Compile()`，AOT 目標需實測（同記憶 `mobile-trim-half-a-solved` / `ios-xmlserializer-ambiguous-add`）。免實機重現法：進入點設 `AppContext.SetSwitch("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false)` 強制 reflection-only 先驗。不行則 **graceful degrade**（該平台不即時算，存檔由後端補）——因後端為權威，正確性不受影響。

### PR 切分

| PR | 範圍 |
|----|------|
| 5a | 前置：`Bee.UI.Avalonia` 加 `Bee.Expressions` 參考；（Tier 1）建立前端即時運算服務骨架 + 相依圖（`DetectIdentifiers`）+ 抽共用 row-level 計算器（決策 1）+ 單元測試（前端算值 = `FormRuleProcessor` 同輸入結果）|
| 5b | 綁定接線：訂閱 `FormDataObject.FieldValueChanged` → 重算 → 回寫（master 自動刷新、detail 補 `RefreshRows()`）+ re-entrancy guard + row-edit 壓制窗口處理；`DefaultValueExpression` 於 `NewAsync` 即時套用 |
| 5c | （Tier 2，可選）client 取得 `CurrencySettings`/`UnitSettings` + 公司位數 → `FormView` 注入 `GridControl`/`NumericEdit` + 即時運算 `RoundingContext` → 前端預覽逐位對齊後端 |
| 5d | WASM / 行動端 AOT 實測（`IsDynamicCodeSupported=false` 桌面重現 + 實機/模擬器）+ graceful degrade；Northwind 明細金額即時預覽端到端驗證（demo-smoke）|

每個 PR 於本機 build + test（含 Avalonia head 建置）通過後推 main。

## 延後範圍（本計畫不做，另案）

- **其他前端即時運算**（Blazor / MAUI）：Phase 2 先做 Avalonia，其餘沿用同一共用引擎後補。
- **虛擬顯示計算欄**（`FieldType.VirtualField`，不落 DB、讀取時算）：需整合讀取路徑。
- **`BeforeInsert` / `BeforeUpdate`** 更細觸發點。
- **跨列 / 明細聚合**（`SUM(detail.Amount)`、detail 讀 master 欄位如 `Master.CurrencyRate`）。
- **多捨入模式**（銀行家捨入 / 無條件捨去 / 無條件進位）：屬數值子系統擴充（`RoundingPolicy` → `RoundingMode`、`NumberKindProfile`），與運算式引擎解耦；本計畫計算欄委派單一四捨五入，屆時新增模式後自動受益。
- 求值 timeout / 高風險函式白名單擴充。

## 已定案決策（2026-07-09 全數確認）

1. **運算式引擎**：DynamicExpresso（`DynamicExpresso.Core`）。
2. **執行位置**：後端為權威（Phase 1）；Avalonia 前端即時預覽（Phase 2）；引擎專案前後端可攜共用。
3. **規則適用性 guard 命名**：`When`（對齊 FluentValidation `.When()`；不用 `Precondition`）。
4. **規則判斷語意**：`When` 空 → 一律套用、`false` → 略過整條規則；`Condition` 為 true 即通過、`false` 即違規。
5. **計算欄捨入**：委派既有 `NumberFormatResolver.RoundByKind`（依 `NumberKind`），繼承公司/幣別/單位可調位數與 round-then-sum；不自建、不用 `DbField.Scale`。多捨入模式屬數值子系統另案。
6. **null 求值政策**：`DBNull → 型別預設值（0/空字串）`。若某些欄位語意上「null ≠ 0」需另設旗標，列後續。
7. **新專案命名**：`Bee.Expressions`（求值引擎屬基礎設施層）。
