# Plan：Layout 客製化（討論稿）

> 狀態：📝 擬定中（討論用，尚未動工）· 2026-07-25
> 範圍：**FormLayout 的租戶客製**——版面重排、欄位隱藏、區塊調整。
> 前置：[客製化共同前置](plan-customization-foundation.md)（缺口 A、B 未補則本案無法生效）
> 相關：[業務邏輯客製](plan-customization-business.md)｜[語系客製](plan-customization-language.md)｜[ADR-016](../adr/adr-016-multitenant-customization-overlay.md)

---

## 0. 一句話結論

**三類客製中結構問題最深的一類。** 不只是「沒接線」——
**正式 API 路徑根本不讀 FormLayout 定義檔**，而是從 FormSchema 即時生成，
所以客製 FormLayout 檔在真實執行路徑上**永遠不會被讀到**。
補接線之前，得先決定 FormLayout 的取得策略。

---

## 1. 現況

### 1.1 已實作的 overlay（但沒有真實呼叫端）

`CacheDefineAccess.cs:387-399`：
```csharp
public FormLayout GetFormLayout(string customizeId, string layoutId)
{
    if (!string.IsNullOrEmpty(customizeId) && _customizeReader is not null)
    {
        var custom = _customizeReader.GetCustomizeFormLayout(customizeId, layoutId);
        if (custom is not null)
            return custom;   // 整檔勝出，無合併
    }
    return GetFormLayout(layoutId);
}
```
**整檔取代，無任何差異疊加。**

### 1.2 三重證據：這條 overlay 從未在正式路徑被觸發

1. **`GetDefine` 繞過**：`CacheDefineAccess.cs:110-112` 呼叫的是**單參數** `GetFormLayout(keys![0])`。
   這是 .NET client 取得 FormLayout 的唯一路徑（`ClientDefineAccess.cs:220`）。
2. **API 根本不讀定義檔** ★：`SystemBusinessObject.GetFormLayout`（`SystemBusinessObject.Define.cs:108-125`）
   走的是 `schema.GetFormLayout(layoutId)` → `FormLayoutGenerator.Generate(this, layoutId)`
   （`FormSchema.cs:243-244`）——**從 FormSchema 即時生成**，完全不碰 FormLayout 定義檔。
3. **僅測試呼叫**：全 repo 呼叫雙參數 `GetFormLayout(customizeId, layoutId)` 的只有 4 個測試檔的 5 行。

> **這代表不只客製失效——連 base 的手工調整 FormLayout 檔在 API 路徑上也不生效。**
> 與 `architecture-overview` 描述的「FormLayout 由 FormSchema 衍生後可微調」有落差。

### 1.3 表達能力

`FormLayout`（`src/Bee.Definition/Layouts/FormLayout.cs`）只有
`LayoutId / ProgId / Caption / ColumnCount / Sections / Details`——
**沒有 patch / merge / delta 的表達能力**。

### 1.4 測試

`tests/Bee.ObjectCaching.UnitTests/CacheDefineAccessFormLayoutCustomizeTests.cs`（4 測試）：
整檔擇一、空 id 短路。**皆為手動傳 customizeId 的元件級測試。**

---

## 2. 設計決策

### 決策 L1：FormLayout 的來源策略 ★核心

現況是「一律從 FormSchema 即時生成」。要讓客製（乃至 base 手工調整）生效，必須改變這點。

- **選項 L1-a（建議）**：**定義檔優先，缺檔才即時生成**
  ```
  查 cust 檔 → 查 base 檔 → 都沒有 → FormLayoutGenerator.Generate()
  ```
  優點：客製檔與 base 手工調整都生效；符合「衍生後可微調」的架構描述；語意直覺。
  缺點：需改 `SystemBusinessObject.GetFormLayout` 與 `CacheDefineAccess.GetDefine` 兩條路徑；
  要留意「檔案存在但過期」的情境（FormSchema 加了欄位，舊 layout 檔沒有）。

- **選項 L1-b**：**維持即時生成，客製改為「生成後套 patch」**
  優點：base layout 永遠與 FormSchema 同步，不會過期。缺點：需先有 L2 的 patch 能力，工程量大。

- **選項 L1-c**：**只在有 customizeId 時查檔**
  優點：改動最小。缺點：治標——base 的手工 layout 仍然失效，等於承認 1.2 的落差。

> 待討論：L1-a 的「layout 檔過期」問題怎麼處理？
> 選項：(i) 不處理，由定義維護流程負責重新產生；(ii) 生成後與檔案做 merge（等於走向 L1-b）；
> (iii) 偵測 FormSchema 欄位集與 layout 不符時記錄警告。

### 決策 L2：整檔取代 vs 部分覆寫

現況整檔擇一。**整檔複製的維護風險**：base 日後新增欄位/改版**不會傳播**到客製版，長期分歧。
（ADR-016 未把這點列為已知取捨。）

- **選項 L2-a**：**先沿用整檔取代**，部分覆寫列為第二階段。
  優點：零新機制，先讓客製跑起來、累積實際案例再決定 patch 語法。
  缺點：早期客製案例會以整檔形式沉澱，日後轉 patch 需遷移。

- **選項 L2-b**：**直接做 delta/patch**——以指令式覆寫表達
  （如「隱藏欄位 X」「移動 Y 到 section Z」「改 section 標題」）。
  優點：表達力好、維護最省、base 改版自動傳播。
  缺點：需設計新的 patch schema 與套用順序語意；`FormLayout` 目前無此表達能力。

> **建議取決於你的實務型態**（見 §4 提問 1）：
> 若客製多為「小幅調整」→ L2-b 的長期價值高，值得早做；
> 若多為「整版重做」→ L2-a 已足夠，patch 反而是過度設計。

### 決策 L3：客製 Layout 的欄位集邊界

ADR-016 定調：Layout 客製「**只能重排 / 隱藏既有欄位，欄位集仍由共用 FormSchema 鎖定**」
（因為 FormSchema 不可客製，見 foundation plan §3）。

> **待確認**：這個邊界對你們實務夠嗎？
> 若客製需求包含「這個租戶要多一個欄位」，那就**超出 Layout 客製範圍**，
> 會撞到 ADR-016 的永久排除項，需要另一套機制（自訂欄位／擴充欄位），屬「更細的客製化」議題。

---

## 3. 建議階段

| 階段 | 範圍 | 前置 |
|------|------|------|
| L0 | 決策定案（L1 來源策略、L2 疊加粒度、L3 邊界確認） | — |
| L1 | 依 L1 決策調整 FormLayout 取得路徑（`SystemBusinessObject.GetFormLayout` + `CacheDefineAccess.GetDefine`） | foundation F1（`CustomizePath` 配置） |
| L2 | 接上 customizeId，客製 layout 生效 | foundation F2（消費端接線） |
| L3 | 端到端測試：帶 CustomizeId 的 session → API → 拿到客製 layout | foundation F4 |
| L4（選配） | 部分覆寫 / patch 機制（若選 L2-b） | 需新 ADR |

> 回歸防護：**未設 CustomizeId 時，layout 取得結果與現況逐位元一致**。
> 特別注意 L1-a 會改變 base 行為（開始讀檔），需確認既有 base layout 檔內容正確、不會反而改壞現況。

---

## 4. 給 review 的提問

1. **實務上 Layout 客製多是哪種？**
   (a) 小幅調整（隱藏幾個欄位、換位置、改區塊標題）
   (b) 整版重做（版面結構完全不同）
   → 決定 L2 要不要早點做 patch。
2. **同意「定義檔優先、缺檔才生成」(L1-a) 嗎？** 這會改變現有所有 layout 取得路徑的行為
   （含 base——目前 base 手工 layout 檔其實也沒生效）。
3. **客製 layout 需不需要「加欄位」？** 若需要，會超出 ADR-016 邊界，需另案處理。
