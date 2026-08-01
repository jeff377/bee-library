# Plan：Layout 客製化（討論稿）

> 狀態：📝 擬定中（L1–L5 已定案；**L6 待裁決後即可動工**）· 2026-08-01
> 範圍：**FormLayout 的租戶客製**——版面重排、欄位隱藏、區塊調整。
> 前置：[客製化共同前置](plan-customization-foundation.md)（缺口 A、B 已於 F1／F2 補完）
> 相關：[業務邏輯客製](plan-customization-business.md)｜[語系客製](plan-customization-language.md)｜[ADR-016](../adr/adr-016-multitenant-customization-overlay.md)

---

## 0. 一句話結論

**三類客製中結構問題最深的一類。** 不只是「沒接線」——
**沒有任何真實路徑會讀到 FormLayout 定義檔**：API 從 FormSchema 即時生成，
而兩個 UI head 連問都沒問過，直接在本地自行生成。
所以客製 FormLayout 檔在真實執行路徑上**永遠不會被讀到**，
**base 的手工 layout 檔同樣是死的**。

補接線之前，得先決定 FormLayout 的取得策略——這比原本以為的動得更深，
會動到 UI head。

---

## 1. 現況

> 以下各點於 2026-08-01 重新驗證，皆仍成立；並補上原稿漏記的第 4 條與 §1.5。

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

### 1.2 四重證據：這條 overlay 從未在正式路徑被觸發

1. **`GetDefine` 繞過**：`CacheDefineAccess.cs:112` 呼叫的是**單參數** `GetFormLayout(keys![0])`。
   → 依決策 L4，這**是對的、不需要改**（見 §2 決策 L4）。
2. **API 不讀定義檔** ★：`SystemBusinessObject.GetFormLayout`（`SystemBusinessObject.Define.cs:126-140`）
   走的是 `schema.GetFormLayout(layoutId)` → `FormLayoutGenerator.Generate`
   （`FormSchema.cs:243`）——**從 FormSchema 即時生成**，完全不碰 FormLayout 定義檔。
3. **UI head 連問都沒問** ★★（原稿漏記，2026-08-01 補）：
   兩個 head 都是拿到 `FormSchema` 後在**本地**生成 layout，從不向 server 要：
   - `src/Bee.Web.Blazor.Server/Components/FormPage.razor.cs:72` — `_schema.GetFormLayout()`
   - `src/Bee.UI.Avalonia/Views/FormView.cs:403` — `Schema.GetFormLayout()`

   且 `ClientDefineAccess.GetFormLayoutAsync`（`src/Bee.Api.Client/ClientDefineAccess.cs:225`）
   **全 repo 零呼叫端**。
4. **僅測試呼叫**：全 repo 呼叫雙參數 `GetFormLayout(customizeId, layoutId)` 的只有 3 個測試檔。

> **第 3 條是關鍵**：原稿的階段 L1 只寫「改 `SystemBusinessObject.GetFormLayout` +
> `CacheDefineAccess.GetDefine`」，但**兩條都修好，Avalonia 與 Blazor 使用者仍看不到客製 layout**
> ——它們從來沒問過。UI head 必須一起改，這是本案真正的工程量所在。

### 1.3 表達能力

`FormLayout`（`../../src/Bee.Definition/Layouts/FormLayout.cs`）只有
`LayoutId / ProgId / Caption / ColumnCount / Sections / Details`——
**沒有 patch / merge / delta 的表達能力**。（依決策 L2 也不需要。）

### 1.4 測試

`tests/Bee.ObjectCaching.UnitTests/CacheDefineAccessFormLayoutCustomizeTests.cs`（4 測試）：
整檔擇一、空 id 短路。**皆為手動傳 customizeId 的元件級測試。**

### 1.5 現存的 layout 定義檔全部是死的（2026-08-01 補）

| 位置 | 檔數 | 狀態 |
|------|------|------|
| `apps/Bee.Northwind/Define/FormLayout/` | 8 | 手工維護，**從未被讀取** |
| `tests/Define/FormLayout/` | 9 | 測試 fixture，僅 storage 層測試直接讀 |
| `src/Bee.Definition/Defaults/FormLayout/` | 2 | scaffold 來源 |

> 這是 §1.2 的直接後果，也是 L1 決策的實質代價來源：一旦改成「定義檔優先」，
> Northwind 那 8 個檔會**同時甦醒**。動工前必須確認它們與現行 FormSchema 一致，
> 否則會以「修好客製」之名改壞現況。

### 1.6 caption 被烘進 layout（2026-08-01 補，牽動 L5）

`LayoutColumnFactory.ToField/ToColumn`（`:19,37`）把 `FormField.Caption` **複製**進
`LayoutFieldBase.Caption`。現行 API 路徑是「**先在地化 schema、再生成 layout**」
（`SystemBusinessObject.Define.cs:134`），所以生成出來的 layout 帶的是**已在地化**的文字。

**改讀定義檔後這條就斷了**：layout 檔裡的 caption 是作者當初寫死的靜態文字，
不會經過語系層。這是 §2 決策 L5 的由來。

---

## 2. 設計決策

### 決策 L1：FormLayout 的來源策略 — ✅ 已定案（2026-08-01）：採 L1-a，定義檔優先

- **選項 L1-a（採用）**：**定義檔優先，缺檔才即時生成**
  ```
  查 cust 檔 → 查 base 檔 → 都沒有 → FormLayoutGenerator.Generate()
  ```
  優點：客製檔與 base 手工調整都生效；符合「衍生後可微調」的架構描述；語意直覺。
  代價：Northwind 現存 8 個 layout 檔會甦醒（見 §1.5）；且要留意「檔案存在但過期」
  （FormSchema 加了欄位，舊 layout 檔沒有）。

- **選項 L1-b**：維持即時生成，客製改為「生成後套 patch」。**已由決策 L2 排除。**
- **選項 L1-c**：只在有 customizeId 時查檔。治標——base 的手工 layout 仍然失效。

**「layout 檔過期」的處置**：採 **(iii) 偵測不符時記錄警告**，不自動 merge
（自動 merge 等於走向 L1-b，已排除）。定義維護流程負責重新產生。

### 決策 L2：整檔取代 vs 部分覆寫 — ✅ 已定案（2026-08-01）：採 L2-a，整檔取代

現況即整檔擇一，**不改**。

> **定案理由**（使用者 2026-08-01）：Language 採 per-key 疊加、FormLayout 採整檔取代，
> 兩者刻意不同——**FormLayout 是整個版面，很難用局部疊加方式，也不直覺**。
> 判別線是「一袋彼此獨立的值」vs「組合起來才成立的整體」；layout 與 `LanguageEnum`
> 同屬後者。這條分界已寫進公開文件
> [`definition-files-overview`](../definition-files-overview.md) §7（雙語）。
>
> 已知取捨（ADR-016 未列）：base 日後新增欄位／改版**不會傳播**到客製版，長期分歧。
> 由 L1 的「過期偵測警告」承接這個風險，不另做 patch 機制。

**連帶排除**：選項 L1-b、原階段 L4（patch 機制）。

### 決策 L3：客製 Layout 的欄位集邊界 — ✅ 已確認（2026-08-01）：維持 ADR-016 邊界

Layout 客製**只能重排／隱藏既有欄位，欄位集仍由共用 FormSchema 鎖定**。
「這個租戶多一個欄位」**不在本案範圍**——會撞到 ADR-016「FormSchema 永久不可客製」
（加欄位同時牽動 DB schema 與驗證規則），需另一套自訂欄位／擴充欄位機制，屬另案且需新 ADR。

### 決策 L4：`GetDefine` 與 `GetFormLayout` 的職責分界 — ✅ 已定案（2026-08-01）

> 本決策為 2026-08-01 新增，原稿未涵蓋；它同時解掉了
> [共同前置](plan-customization-foundation.md) §2.A 表格第五列掛著的「簽章無 customizeId，需另議」。

| API 方法 | 語意 | 客製疊加？ |
|---------|------|----------|
| `GetDefine(DefineType.FormLayout, ...)` | **未經任何處理的原始定義檔** | **否** |
| `GetFormLayout(args)` | **運行階段所需要的 FormLayout** | **是** |

推論：

- `CacheDefineAccess.GetDefine` 走單參數 `GetFormLayout(layoutId)` 是**正確的**，
  §1.2 第 1 條不是缺口，**不需修改**。定義編輯器一類的工具要的正是原始檔。
- 生成、客製疊加、在地化等「運行階段加工」全部歸 `GetFormLayout`。
- **UI head 應改呼叫 `GetFormLayout` API**，不再本地生成。

### 決策 L5：layout 定義檔的 caption 在地化 — ✅ 已定案（2026-08-01）：採 L5-a

背景見 §1.6：改讀定義檔後，layout 檔裡的 caption 是靜態文字，不再經過語系層。
現行「即時生成」路徑則是先在地化 schema 再生成，caption 一定是在地化過的。

| 選項 | 作法 | 取捨 |
|------|------|------|
| **L5-a（採用）** | **layout 檔只定義結構，caption 一律由在地化後的 FormSchema 覆寫** | 語系與版面職責分離：改文字去語系檔（已有 per-key 客製機制）、改版面去 layout 檔。與現行行為一致（caption 永遠是翻過的）。代價：layout 檔作者不能在 layout 裡決定文字 |
| **L5-b** | layout 檔的 caption 原樣使用，不經語系層 | 最單純；但同一份 layout 檔在多語系部署下只會有一種語言，且行為不一致（同一張表單「有 layout 檔就不翻譯、沒有就翻譯」） |
| **L5-c** | layout 檔的 caption 視為語系 key，經 `FormSchemaLocalizer` 同一套 sub-key 規範查找 | 表達力最好；但需為 layout 另定一套 sub-key 規範，工程量與概念負擔最大 |

**落地方式**：讀到 layout 檔後，逐 `LayoutField` / `LayoutColumn` 以
`FormField.Caption`（已在地化）回填 `LayoutFieldBase.Caption`；section / grid 的 caption
同理取自 `FormTable.DisplayName`，`FormLayout.Caption` 取自 `FormSchema.DisplayName`。
schema 裡找不到對應欄位時保留 layout 檔原值（layout 檔可能落後於 schema，見 L1 的過期偵測）。

### 決策 L6：缺檔怎麼探測 — ⏳ **待裁決（新的擋動工項，2026-08-01 實作時發現）**

L1-a 的「缺檔才生成」需要一個**不靠例外**判斷定義檔是否存在的方式，而現況做不到：

| 後端 | 缺件時的行為 |
|------|------------|
| `FileDefineStorage.GetFormLayout`（`:163`） | 丟 `FileNotFoundException` |
| `DbDefineStorage.GetFormLayout`（`:175` → `ReadRequired:227-233`） | 丟 `InvalidOperationException` |
| `CustomizeOnlyStorage.GetFormLayout`（`:41`） | 回 `null` |

**兩個 base 後端丟不同型別的例外，第三個回 null** ——而 `IDefineStorage` 的簽章
本來就宣告 `FormLayout? GetFormLayout(string layoutId)`（**nullable**），
只有 `CustomizeOnlyStorage` 履行了這個契約。

| 選項 | 作法 | 取捨 |
|------|------|------|
| **L6-a（建議）** | 讓 base storage **履行介面已宣告的 nullable 契約**：`FileDefineStorage` / `DbDefineStorage` 缺件回 `null`。`CacheDefineAccess` 另加 `FormLayout? FindFormLayout(customizeId, layoutId)` 供「缺檔才生成」用；既有 `GetFormLayout(layoutId)` 維持缺件丟例外，呼叫端契約不變 | 語意最正；但要改 1 個既有測試斷言（`FileDefineStorageTests:122` 目前斷言丟 `FileNotFoundException`），且 `DbDefineStorage` 要區分「沒這筆」與「反序列化失敗」 |
| **L6-b** | 不動 storage，在 `SystemBusinessObject.GetFormLayout` 內同時 catch `FileNotFoundException` 與 `InvalidOperationException` | 零 API 異動；但 DB 後端的**反序列化失敗會被吞成「當作沒檔 → 改用生成」**，真錯誤靜默消失。且違反 `rules/scanning.md` 對寬泛 catch 的精神 |
| **L6-c** | 新增 `IDefineStorage.Exists(DefineType, key)`，先探測再讀 | 語意清楚；但每個 `IDefineStorage` 實作者（含 host 自訂）都要補一個成員，公開介面擴張 |

> 這條沒定不能動工——它決定「讀檔」這個動作本身怎麼寫。

---

## 3. 建議階段

> 依 L1／L4 重寫；原稿的階段 L1 低估了工程量（漏了 UI head，見 §1.2 第 3 條），
> 原階段 L4（patch）已由決策 L2 排除。

| 階段 | 範圍 | 前置 | 狀態 |
|------|------|------|------|
| L0 | 決策定案 | — | 🚧 L1–L5 ✅ 已定案；**L6 待裁決** |
| L1 | `SystemBusinessObject.GetFormLayout` 改為「cust 檔 → base 檔 → 生成」，讀到檔則以在地化 schema 回填 caption（L5-a）。`GetDefine` 不動 | foundation F1、L6 | 📝 待做 |
| L2 | 接上 `SessionInfo.CustomizeId`（比照語系 G1 的作法），客製 layout 在 API 路徑生效 | foundation F2（已完成） | 📝 待做 |
| L3 | **UI head 改為向 server 取 layout**（Avalonia `FormView`、Blazor `FormPage`），不再本地生成。需保留 Avalonia 端的 `LayoutCapabilityApplier` 權限降級 | L1、L2 | 📝 待做 |
| L4 | 過期偵測：FormSchema 欄位集與 layout 檔不符時記錄警告（決策 L1） | L1 | 📝 待做 |
| L5 | 端到端測試：帶 CustomizeId 的 session → API → 拿到客製 layout | foundation F3 | 📝 待做 |

**動工前的前置檢查**（來自 §1.5）：確認 `apps/Bee.Northwind/Define/FormLayout/` 那 8 個檔
與現行 FormSchema 一致。它們甦醒後就是 Northwind 的實際版面，內容過期會直接改壞 demo。

> 回歸防護：**未設 CustomizeId 時，layout 取得結果與現況逐位元一致**——
> 但注意 L1-a **刻意改變 base 行為**（開始讀檔），所以「逐位元一致」只在
> **無 layout 檔**的部署成立；有檔的部署（Northwind、tests）行為必然改變，
> 這正是本案要修的落差，需個別確認而非以回歸測試一概而論。

### 範圍外（明確不做）

- **list layout**：`FormSchema.GetListLayout()` 同樣由兩個 head 本地生成
  （`FormPage.razor.cs:73`、`ListView.Commands.cs:23`），且**沒有對應的定義檔型別**。
  本案只處理 FormLayout；form 走 server、list 仍本地生成的不對稱是已知且刻意的。
- **patch / delta 機制**：決策 L2 排除。
- **客製加欄位**：決策 L3 排除，需另案 + 新 ADR。

---

## 4. 給 review 的提問

1. ~~**實務上 Layout 客製多是哪種？**（小幅調整 vs 整版重做）→ 決定 L2 要不要早點做 patch。~~
   ✅ 2026-08-01 定案：**整檔取代**（L2-a），不做 patch。理由見 §2 決策 L2。
2. ~~**同意「定義檔優先、缺檔才生成」(L1-a) 嗎？**~~
   ✅ 2026-08-01 定案：**同意**（L1-a）。
3. ~~**客製 layout 需不需要「加欄位」？**~~
   ✅ 2026-08-01 確認：**不需要**，維持 ADR-016 邊界（L3）。
4. ~~**（新）layout 檔的 caption 怎麼在地化？**~~
   ✅ 2026-08-01 定案：**layout 檔只定義結構，caption 由在地化後的 schema 回填**（L5-a）。
5. **（新）缺檔怎麼探測？** 兩個 base storage 對「沒有這份定義」丟不同型別的例外，
   而介面簽章本來就宣告 nullable。→ 決策 L6，**目前唯一擋動工項**。

### 動工前另外兩個小約定（實作時自行採用，如有異議請提）

- **layoutId 的預設值**：`GetFormLayoutArgs.LayoutId` 留空時，現行程式碼填 `"default"`，
  但**全 repo 沒有任何 `default.FormLayout.xml`**——現存檔案一律是
  `{ProgId}.FormLayout.xml` 且檔內 `LayoutId == ProgId`。因此留空時改以 **ProgId 當 layoutId**，
  否則現存檔案永遠查不到。（`"default"` 目前只是生成器的佔位字串。）
- **回填找不到對應欄位時**：保留 layout 檔原值，不清空、不丟例外——交由 L1 的過期偵測記錄警告。
