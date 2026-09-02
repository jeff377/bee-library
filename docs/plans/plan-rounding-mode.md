# 計畫：捨入政策可設定化

**狀態：📝 擬定中（2026-09-01）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 決策：是否執行（見「決策點」一節），僅補文件即可結案 | 📝 待做 |
| 1 | 金額／百分比的 midpoint 政策可設定 | 📝 待做 |
| 2 | 數量／重量的方向政策可設定 | 📝 待做 |
| 3 | 現金捨入方向可設定（`CashRoundingItem.Rule`） | 📝 待做 |
| 4 | 文件收斂（ADR-043、cookbook 雙語、`rules/database.md`） | 📝 待做 |

## 背景

問題起於一個提問：「多幣別的小數位數進位，ERP 是否應採偶捨奇入（banker's rounding）？」

### 現況盤點

全 `src/` 只有兩處 `Math.Round`，都在 [NumberFormatResolver.cs](../../src/Bee.Definition/NumberFormatResolver.cs)：

| 位置 | 用途 | 現行模式 |
|------|------|---------|
| `RoundByKind`（第 110 行） | 明細層捨到幣別／單位位數 | `MidpointRounding.AwayFromZero` 硬編 |
| `RoundCash`（第 147 行） | 單據最終應付額對齊現金捨入單位（T001R 式） | `AwayFromZero` 硬編，且方向只有 nearest |

production 呼叫點僅 [FormExpressionCalculator.cs:336](../../src/Bee.Definition/Forms/FormExpressionCalculator.cs:336)（計算欄）一處。
行為由 `RoundByKind(12.5m, Amount, ctx, "JPY") == 13` 等測試鎖住
（[NumberFormatResolverCurrencyTests.cs:72](../../tests/Bee.Definition.UnitTests/NumberFormatResolverCurrencyTests.cs:72)、
[NumberFormatResolverTests.cs:93](../../tests/Bee.Definition.UnitTests/NumberFormatResolverTests.cs:93)）。

### 範圍界定：與「加總」無關

捨入政策的作用點**只有明細那一次**，不涉及合計：

- round-then-sum 之下 `Σ round(xᵢ)` 已落在該位數的格上，對合計再捨是 no-op。
- [AmountColumnSummary.TryComputeTotal](../../src/Bee.UI.Avalonia/Controls/AmountColumnSummary.cs:20)
  是純 `sum += value`（XML doc 明寫 *does not round*），且混幣別回 `null` 不給合計 ——
  單一欄的合計恆為單一幣別。
- 唯一在合計之後動手的是 `RoundCash`，但它**不是**在修正加總誤差，
  而是刻意製造差額（應付額對齊 CHF 0.05，差額記 DIFF 科目）。語意不同，勿並列。

因此本 plan 的標的是「每一筆明細計算欄捨入時的政策」，與多幣別加總無關。

### 提問前提需要修正

「ERP 一律採偶捨奇入」不成立，**不應照它改預設**：

- **SAP**：商業捨入為 half-up；TCURX 位數與 T001R 現金捨入皆非 half-even。
- **Odoo**：`float_round` 預設 `HALF-UP`；`HALF-EVEN` 是後補的**選項**，限特定稅務場景。
- 偶捨奇入的實際場合是「統計匯總去偏差」與**少數法域的稅額規定**，屬個案要求而非 ERP 通則。

另注意 .NET `Math.Round` 預設本來就是 `ToEven`，此處是**顯式**寫 `AwayFromZero`，
且 `.claude/rules/database.md` 有明文條款要求 —— 是有意識的決策，不是疏漏。

### 真正的缺口：捨入政策無接縫，且金額與數量要的不是同一種政策

位數與現金捨入單位都做到可設定，唯獨**捨入政策**硬編。
而按 `NumberKind` 拆開會看到，`Round` 類的兩個軸要的政策形狀不同：

| 軸 | 捨入的本質 | 真正的爭議點 | 現況 |
|----|-----------|------------|------|
| `Amount` / `Percent`（幣別、公司） | 會計與稅務 | **midpoint 政策**（half-up vs half-even）—— 法規對「恰好半格」的規定 | ❌ 硬編 half-up |
| `Quantity` / `Weight`（計量單位） | 物理計量 | **方向政策**（無條件捨去／進位）—— 領料不可超領、包裝進位到整箱 | ❌ 硬編 half-up |
| 現金捨入 | 付款實務 | **方向政策**（nearest／up／down）—— SAP T001R | ❌ 只有 nearest |

關鍵判斷：**數量幾乎不存在 half-even 需求**，用四捨五入處理「0.5 箱」在業務上本就是錯的。
反之金額極少需要無條件捨去。因此用單一 `RoundingMode` 蓋住兩者，會給數量一個它不需要的
選項，同時給不出它真正需要的方向控制 —— 這是本 plan 相對初稿最重要的修正。

[ADR-026](../adr/adr-026-numeric-semantics-rounding.md) 的 D2 只規範 round-then-sum 的**順序**，
從未提及捨入政策的選擇理由 —— 這是**未記錄的隱含決策**，無論本 plan 是否執行都該補。

## 決策點（階段 0）

| 選項 | 內容 | 適用時機 |
|------|------|---------|
| **A：只補文件** | 僅執行階段 4 的 ADR 部分，記錄「為何選 half-up」，程式碼零變更 | 目前無多法域／無方向捨入需求 |
| **B：階段 1** | 金額／百分比的 midpoint 政策可設定 | 已知有 half-even 法域需求 |
| **C：階段 2** | 數量／重量的方向政策可設定 | 有領料／包裝類的方向需求（比 B 更常見於實際 ERP） |
| **D：全做** | 三個階段一次補完 | 三者同動 `NumberFormatResolver`，分批做等於改三次 |

**建議 A**，理由：三個階段的設定歸屬層各不相同（幣別層／欄位層／幣別覆寫層），
需求未出現時先設計三個維度等於在猜；每個維度都是一份**永久**的公開 API 表面
與跨層同步成本（PublicAPI、wire 契約、DB schema、雙語文件）。
缺口已記錄於此，需求出現時再依對應階段執行。

**若只做一項，建議 C 而非 B** —— 方向捨入（領料不可超領）在實際 ERP 的出現頻率
高於 half-even 法域要求，而且現行 half-up 對它是**業務上錯誤**的行為，
不像 half-up 對金額只是「非某些法域偏好」。

## 階段 1：金額／百分比的 midpoint 政策

### 設計

midpoint 政策是**法人所在法域的會計政策**，不隨貨幣而變（同一間公司對 USD 與 JPY
用同一套規則），因此掛公司層。

1. `src/Bee.Definition/MidpointPolicy.cs` —— 新 enum：`HalfAwayFromZero`（預設）/ `HalfToEven`。

   **不直接沿用 BCL `MidpointRounding`**：它含 `ToZero` / `ToNegativeInfinity` /
   `ToPositiveInfinity` 三個非 midpoint 語意的成員，暴露出去等於承諾框架支援未驗證的組合。

2. [CompanyInfo.cs](../../src/Bee.Definition/Identity/CompanyInfo.cs) 新增
   `MidpointPolicy` 屬性（`[XmlAttribute]` + `[DefaultValue]`）。

3. `RoundByKind` 對 `Amount` / `Percent` 從 `ctx.Company?.MidpointPolicy` 解析。
   **簽章不變** —— `RoundingContext` 已攜帶 `Company`。

### 影響面

| 層 | 動作 |
|----|------|
| 定義層 | 新 enum + `CompanyInfo` 一個屬性 + `RoundByKind` 分支 |
| PublicAPI | `src/Bee.Definition/PublicAPI.Unshipped.txt` 加 enum 與屬性行 |
| wire | [WireContracts.Definition.cs:20](../../src/Bee.Api.Core/MessagePack/WireContracts.Definition.cs:20) 的 `CompanyInfo` 契約尾端加一個 `.Member`（name-based key，加 member 相容，見 [ADR-030](../adr/adr-030-messagepack-name-based-keys.md)） |
| 資料庫 | `st_company` 加 `midpoint_policy`（`AllowNull=false`）；[CompanyRepository.cs:46](../../src/Bee.Repository/System/CompanyRepository.cs:46) 一帶讀取。**加欄 checklist 見 `rules/database.md`：所有 INSERT（含 seed 與測試 helper）都要給值** |
| 測試 | 既有 `12.5 → 13` 兩筆在預設下仍過（回歸護欄）；新增 `HalfToEven` 情境（`12.5 → 12`、`13.5 → 14`）與 XML／MessagePack round-trip |

## 階段 2：數量／重量的方向政策

### 設計

**歸屬層與階段 1 不同，這是本階段的設計重點。**
方向需求通常是**逐欄**的（同一張單上，領料數要捨去、包裝數要進位），
既非公司政策也非計量單位屬性 —— 因此掛 `FormField`：

1. `src/Bee.Definition/RoundingDirection.cs` —— 新 enum：
   `Nearest`（預設，= 現行行為）/ `Up`（無條件進位）/ `Down`（無條件捨去）。
2. `FormField` 新增 `RoundingDirection` 屬性（`[XmlAttribute]` + `[DefaultValue(Nearest)]`），
   並比照 `NumberKind` 傳遞至 `LayoutFieldBase`。
3. `RoundByKind` 需要取得該欄的方向 —— **這會改變簽章**，是三個階段中成本最高的一項：
   現行 `RoundByKind(value, kind, ctx, refCode)` 只吃 kind 不吃 field。
   兩條路徑待評估：(a) 加一個吃 `FormField` 的多載；
   (b) 由 [FormExpressionCalculator](../../src/Bee.Definition/Forms/FormExpressionCalculator.cs:336) 呼叫端
   在 `RoundByKind` 之後再套方向。**傾向 (b)** —— 不動既有簽章，
   且方向是欄位層政策、與「位數解析」職責分離。

   ⚠️ 對既有 public 建構子／方法加 optional 參數是**二進位破壞性變更**（RS0027，
   見 `docs/repo-ops/gotchas/`），不可用「加預設參數」規避多載。

### 影響面

無資料庫 schema 變更（`FormField` 走定義檔 XML）。
定義層 + PublicAPI + wire 的 `FormField` 契約 + 測試。

## 階段 3：現金捨入方向

掛既有的逐幣別覆寫項，與 `Unit` 同層（方向隨幣別而變：CHF 0.05 就近、某些稅制無條件捨去）：

1. `src/Bee.Definition/CashRoundingRule.cs` —— 新 enum：`Nearest`（預設）/ `Up` / `Down`。
2. [CashRoundingItem.cs](../../src/Bee.Definition/CashRoundingItem.cs) 新增 `Rule` 屬性。
3. `RoundCash` 依 rule 改用 `Math.Ceiling` / `Math.Floor` / `Math.Round(..., AwayFromZero)`。

`CurrencyItem`（系統層自然單位）**不加** —— 無公司覆寫時 unit 即自然最小單位，
捨到自身倍數是 no-op，方向沒有意義。

走既有 `cash_rounding_xml` 欄位，**零資料庫 schema 變更**，三個階段中成本最低。

## 階段 4：文件收斂

1. **新增 ADR-043**：記錄「為何預設 half-up」（SAP／Odoo 實務、ERP 商業捨入慣例）、
   「為何金額與數量的捨入政策不共用同一個設定」、以及「為何開放設定而不換預設」。
   **選項 A 只做這一項**，此時 ADR 收斂為記錄既有決策
   （`adr-043-commercial-rounding-default.md`）。
2. `docs/development-cookbook.md` / `.zh-TW.md` §Numeric Semantics 補設定入口（雙語同步）。
3. `.claude/rules/database.md` 的「四捨五入類 → `AwayFromZero`」條文改為
   「預設 `AwayFromZero`，可由公司政策／欄位方向覆寫」。

**不修改 ADR-026** —— 它是已採納的決策紀錄，設定化屬後續增修，另立 ADR 並互相引用。

## 順帶回報：ADR-026 D6 的既有落差

D6 寫「API 匯入超過 scale 時於 Repository 寫入層顯式
`decimal.Round(value, DbField.Scale, AwayFromZero)`」，但 `src/Bee.Repository/` **查無此實作**
（全 repo 唯二的 `Math.Round` 都在 `NumberFormatResolver`）。
這是既有落差，不在本 plan 範圍內 —— 記於此以免下次重複發現。

## 驗證

- `./test.sh tests/Bee.Definition.UnitTests/Bee.Definition.UnitTests.csproj`
- 階段 1 動到 `st_company`，需跑完整模式 CI（`[all-db]`）—— 加欄涉及四種 provider 的 DDL。
- 階段 2、3 無 schema 變更，精簡模式即可。
