# 計畫：業務 plugin 設定檔標記時點（重啟 ADR-035 決策三）

**狀態：✅ 已完成（2026-09-05）—— 三階段全數落地，掛在 4.29.0（版號檔尚未 bump、tag 未推）**

## 背景

維護者的要求原話：

> 我認為在設定檔中要標記時點，這樣比較明確。直接看 xml 就知道有那些時間有外掛。

這**不是補一個漏掉的功能**，而是重啟
[adr-035](../adr/adr-035-business-logic-plugin.md) 的**決策三：設定檔只列型別，時點由類別自己
override**。該節明文否決了「設定檔明寫『時點 × 型別』」，並且承認被否決的方案
「設定檔可讀性與儲存時的驗證精確度都比較好」。

ADR 否決的唯一理由是：那會逼一個業務需求拆成兩個類別，失去 **per-operation 一個實例**
（同一次 `Save` 的所有時點共用同一實例，狀態靠 instance field 傳遞）。ADR 自己把爭點範圍定死：

> 這是本方案相對於「時點 × 型別」的**唯一實質優勢**。若改成每個時點各建一次，本方案就只剩
> 「設定檔少打幾行」，不值得為此放棄對方的可讀性。

## 裁定（2026-09-05）

**一筆繫結一個時點。一個 plugin 類別只掛單一時點。**

理由是**責任單一**：兩個時點的作用本質不同（`BeforeSave` 是存檔前的檢查／調整，
`AfterSave` 是存檔後的副作用），不可能做相同的動作。把兩件性質不同的事塞進同一個類別，
是為了「共用一個 instance field」而犧牲類別的單一職責 —— 那個交換不划算。
加上設定檔要能一眼看出時點，兩者指向同一個結論。

> **一則供 ADR 引用的事實澄清**：四個時點的簽章是
> `BeforeSave(SaveContext)` / `AfterSave(SaveContext)` /
> `BeforeDelete(DeleteContext)` / `AfterDelete(DeleteContext)`。
> **參數型別只在 Save 管線與 Delete 管線之間不同，管線內部相同**。
> 所以「一類別一時點」不是被型別簽章**強制**的，是責任單一的**設計裁示** ——
> ADR 修訂時應寫成後者，寫成前者會被 `SaveContext` 相同這件事直接反駁掉。

### 這個裁定放棄了什麼

ADR 稱為「唯一實質優勢」的 per-operation 跨時點狀態共享。
「`BeforeSave` 檢查 → `AfterSave` 後續動作」必須寫成兩個類別，兩者之間**沒有共享狀態的地方**，
`AfterSave` 需要的資料要重算或重讀。ADR 說這在 ERP 客製裡是常態 —— 這個代價是明知而選的。

ADR 當初把這件事看成「一個需求被迫拆成兩個類別」（損失）；本裁定把它看成
「兩件不同的事本來就該是兩個類別」（正確化）。差別不在事實，在於**那個 instance field
值不值得用單一職責去換** —— ADR 判值得，現在判不值得。

### 這個裁定換到了什麼

1. **XML 直接看得出時點**（本案的目的）。
2. **`Type` 仍是唯一 key。** 一類別一時點 ⇒ 同一型別在一個 program 內永遠只出現一次 ⇒
   不需要複合鍵，`Plugins_DuplicateType_Throws` 不必修改。
   （早先評估認為「一筆一時點」要改成 `(Type, Stage)` 複合鍵，那是在允許同型別跨時點的前提下才成立的。）
3. **列舉更簡單**：單一時點的純列舉，不需要 `[Flags]`，也就沒有「空白分隔 vs 逗號分隔」的問題。
4. **執行期可簡化**：見「`EnsureInstances` 應改為按需建構」。

## 現況查證（2026-09-05 逐檔複驗）

### 設定檔一筆繫結只有一個屬性

[`PluginItem.cs`](../../src/Bee.Definition/Settings/PluginSettings/PluginItem.cs) 只有一個
`[XmlAttribute] public string Type`，而它就是 `base.Key`；基底 `KeyCollectionItem` 的每個成員都是
`[XmlIgnore, JsonIgnore]`。實際序列化輸出：

```xml
<PluginSettings>
  <Items>
    <ProgramPluginItem ProgId="Order">
      <Plugins>
        <PluginItem Type="Acme.Plugins.CreditLimitCheck, Acme.Plugins" />
      </Plugins>
    </ProgramPluginItem>
  </Items>
</PluginSettings>
```

### 時點的唯一來源是執行期反射

[`FormPluginChain.OverriddenStages`](../../src/Bee.Business/Form/FormPluginChain.cs) 以
`method.DeclaringType != typeof(FormBusinessPlugin)` 判四個方法各自有沒有被覆寫。
`FormPluginStage` 這個型別住在 **`Bee.Business`**，在 `Bee.Definition` 裡零命中。

### 反射結果有一個 IO 決策直接吃它

[`FormBusinessObject.Write.cs:214`](../../src/Bee.Business/Form/FormBusinessObject.Write.cs)：
`pluginNeedsSnapshot` 用 `Chain.HasStage(BeforeDelete/AfterDelete)` 決定刪除前要不要多讀一次資料。
時點資訊錯了不只是「跑錯 plugin」，而是「plugin 拿到 `null` snapshot」。

### ⚠️ ADR 的可讀性補償措施**從未落地**

ADR-035 對「從 XML 看不出時點」的回答是：

> 解法不是改設定檔結構，而是由載入時的反射（比對 `MethodInfo.DeclaringType`）算出各時點的執行
> 清單……**供維護工具顯示執行順序**。

實際上：`FormPluginChain.TypesForStage` 的**生產端呼叫者為零**（只有
`tests/Bee.Business.UnitTests/FormPluginRunnerTests.cs` 用到），且 `tools/`、`apps/`、`samples/`
全樹查無任何 `PluginSettings` 消費者。**那個維護工具不存在，也沒人在做。**

可讀性代價一直是淨損失 —— 這是本次改變決策的直接原因之一，必須寫進 ADR 修訂。

### 相容性負擔：無（維護者裁定 2026-09-05）

- 全 repo 查無任何 `PluginSettings.xml`（`src/Bee.Definition/Defaults/`、
  `apps/Bee.Northwind/Define/` 皆無），**沒有自家存量**。
- 格式雖自 v4.17.0（2026-08-06）公開，但**維護者裁定 plugin 目前無實際消費者，不理會舊格式**。
  因此本案不做任何相容處理、不留過渡路徑、不寫遷移指引 ——
  當成一個尚未被使用的功能來改，而不是一次遷移。

### wire 形狀不受影響（已確認）

`PluginSettings` 以 **XML 字串**過 wire：`WireContracts.System.cs` 只註冊了
`GetCustomizePluginSettingsResponse.Xml` / `SaveCustomizePluginSettingsRequest.Xml` 兩個字串成員，
`PluginItem` 本身從未進入 MessagePack 註冊表。因此只要不動訊息本身，
`wire-contracts/` 與 `wire-fixtures/` 就沒有 diff，
[`bee-connector-js`](https://github.com/jeff377/bee-connector-js) 的 CI 不會變紅。

## 目標形狀

```xml
<PluginSettings>
  <Items>
    <ProgramPluginItem ProgId="Order">
      <Plugins>
        <PluginItem Type="Acme.Plugins.CreditLimitCheck, Acme.Plugins" Stage="BeforeSave" />
        <PluginItem Type="Acme.Plugins.OrderSync, Acme.Plugins"        Stage="AfterSave" />
      </Plugins>
    </ProgramPluginItem>
  </Items>
</PluginSettings>
```

## 宣告與反射：精確對帳

**規則：類別實際覆寫的時點必須「恰好一個」，且等於 `Stage` 宣告的那一個。不符即拒。**

- 相等成立時，「照宣告跑」與「照反射跑」是同一件事 → **執行語意零變更**。
- 不符一律拋 → 不存在「覆寫了卻沒宣告 → 靜默不跑」，那正是 `ValidatePluginType` 現在擋
  「掛了等於沒掛」的同一種病。
- 覆寫 **0 個**（現行已擋）與覆寫 **≥2 個**（新增的擋法）都是設定錯誤。

兩道閘門，互補：

| 閘門 | 位置 | 擋什麼 |
|------|------|--------|
| 儲存時 | `SystemBusinessObject.ValidatePluginType` | 維護 API 存進來的客製檔，編輯者**當場**知道 |
| 解析時 | `PluginSettingsResolver.BuildChain` | **手寫檔**（套裝層與外部使用者永遠不經維護 API） |

解析時這道是必要的：套裝層 `{DefinePath}/PluginSettings.xml` 沒有維護 API。而 resolver 現行政策
本來就是「每個失敗一律拋」，所以這不是新政策，是同一條政策多蓋一種錯。

反射沒有退場，但**降為驗證器**：chain 以 `(customizeId, progId)` 快取，反射本來就只算一次，
對帳是零額外成本。

> 這確實讓時點資訊存在兩處（類別覆寫了什麼、XML 宣告了什麼）。`single-source.md` 允許這個例外，
> 但要求「**必須明列該例外，並納入某種檢查**」—— 檢查就是上面兩道閘門，不一致的檔案根本載不起來。

### 誠實的代價：改 plugin 類別要連帶改 XML

現況下替 plugin 換一個覆寫的時點，重新部署組件就自動生效。之後同一件事會在下次解析時**拋例外**，
直到 XML 跟上。這是大聲失敗而非靜默，且付代價的人就是編輯那份 XML 的人 ——
但它是新增的耦合，**必須寫進公開文件**。

## 型別要放哪：`Bee.Business.Form.FormPluginStage` 應遷入定義層並改名

`PluginItem` 在 `Bee.Definition`，`FormPluginStage` 在 `Bee.Business`，
而相依方向是 **Business → Definition**。所以宣告用的時點型別必須存在於 `Bee.Definition`。

**建議：在 `Bee.Definition` 新增 `PluginStage`，並移除 `Bee.Business.Form.FormPluginStage`，
Business 端改用定義層那一個。**

方向是允許的（Business 可以參考 Definition），所以這是**單一列舉**，不需要兩個列舉互相對帳、
也不需要映射鎖定測試。三條路的取捨：

1. **原名搬到 `Bee.Definition`** —— ❌ 不可行。二進位相容要靠 `[TypeForwardedTo]`，
   而它要求型別**完整名稱不變**，等於要把 `Bee.Business.Form` 命名空間塞進 `Bee.Definition` 組件，
   違反資料夾／命名空間一致性。
2. **兩個列舉並存 + 映射測試** —— 可行但多餘：既然相依方向允許，沒有理由留兩份。
3. **新增 `Bee.Definition.Settings.PluginStage`，移除 `Bee.Business.Form.FormPluginStage`** ——
   ✅ 建議。單一來源、零漂移。

> **不可沿用 `FormPluginStage` 這個名字**：同名不同 namespace 的兩個列舉，在同時 `using` 兩邊的
> 消費端會 `CS0104`（`code-style.md` 明列的命名衝突樣態）。
> `PluginStage` 不與任何 BCL namespace 末段同名（CA1724 通過），貼進 UI 專案也不撞 Avalonia / WPF 型別。
>
> 移除 `Bee.Business.Form.FormPluginStage` 是 public API 移除 → 破壞性變更，
> 但本案本來就是 `!`，且下游幾乎只會覆寫方法、不會引用這個列舉。

## `EnsureInstances` 應改為按需建構

[`FormPluginRunner.EnsureInstances`](../../src/Bee.Business/Form/FormPluginRunner.cs)
目前**一建就建整條鏈**，包含只掛 Delete 時點的 plugin ——
在舊設計下這是必要的（同實例要跨時點被找到），新設計下**純屬浪費**：
一個實例只會被一個時點呼叫，Save 操作沒有理由建構 Delete-only 的 plugin。

改為只建構該時點需要的實例。連帶：

- `FormPluginRunner` 的類別註解（「**This is where the one-instance-per-operation guarantee lives**」
  與跨時點傳遞 instance field 的整段）**會全部變成錯的**，必須改寫。
- 既有的「per-operation 一實例」測試要改寫為新語意的斷言。

## `Stage` 缺漏的偵測：列舉需要 `None = 0`

**不是為了相容舊檔**（已裁定不理會），而是為了**手寫新檔時打錯字**：
XML 少一個 `[XmlAttribute]` 不會有任何錯誤，屬性會靜靜拿到型別預設值。

若 `PluginStage` 的第一個成員就是 `BeforeSave`（值 0），「沒寫 `Stage`」與「寫了
`Stage="BeforeSave"`」在反序列化後**完全無法區分**。因此：

```
PluginStage { None = 0, BeforeSave = 1, AfterSave = 2, BeforeDelete = 3, AfterDelete = 4 }
```

`None` 只作為「未宣告」的哨兵，兩道閘門一律拒，永遠不會抵達 dispatch。

> 值得記的一點：**即使沒有 `None`，精確對帳也不會讓錯誤靜默** —— 缺 `Stage` 要嘛
> 恰好等於類別實際覆寫的時點（行為正確），要嘛對不上而拋。
> 加 `None` 換到的純粹是**診斷品質**：能說「你沒宣告 `Stage`」，
> 而不是莫名其妙的「你宣告了 `BeforeSave` 但類別覆寫的是 `AfterSave`」。

### 兩則錯誤訊息

> `Program 'Order' declares plugin 'Acme.Plugins.CreditLimitCheck, Acme.Plugins' with no Stage.`
> `The type overrides BeforeSave — declare Stage="BeforeSave".`

> `Program 'Order' declares plugin 'Acme.Plugins.OrderFlow, Acme.Plugins', which overrides`
> `BeforeSave and AfterSave. A plugin binds to exactly one stage — split it into one class per stage.`

反射既然已經算出實際覆寫的時點，訊息就順手把正確答案寫出來 —— 與 `BEE9001` 同一種作風。

## ADR 要怎麼處理

**建議：改寫 adr-035 決策三，不新開 ADR。**

判別法：這次改的是**同一個決策的答案**，不是在既有決策之上疊一個新決策。ADR-035 其餘五個決策
（掛載而非取代、四個掛載點封閉、兩層相加、失敗處理不對稱、與規則引擎分界）全部不受影響 ——
新開一份會讓讀者必須同時讀兩份才知道決策三現在是什麼。

具體改法：

1. 標題與內容改為「**設定檔明寫時點，一個 plugin 一個時點**」。
2. **誠實寫出放棄了什麼**：per-operation 跨時點狀態共享（ADR 自稱的「唯一實質優勢」）
   被主動放棄，而 ADR 說是常態的「檢查 + 後續動作」現在必須是兩個類別。
   同時記下事實澄清：參數不一致只發生在跨管線，這是設計裁示而非型別強制。
3. **刪掉「可讀性代價以工具中和」那一節**，並記一句：該補償措施從未實作
   （`TypesForStage` 零生產端呼叫），是本次改變決策的直接原因之一。
4. 「因此 per-operation 生命週期是這個選擇的成立條件」一節需重寫 ——
   實例仍是 per-operation 建構，但**不再承載跨時點的保證**。
5. 狀態列改為 `**已採納（Accepted，2026-08-06）；決策三於 <落地日> 修訂**`。

> ADR 是公開文件，**不得連結本 plan**（`rules/public-docs.md`）。背景要留就直接寫進 ADR 本體。

## 分階段變更清單

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 定義層：`PluginStage` 列舉、`PluginItem.Stage`、查詢簽章換成帶時點的繫結、`CustomizeOverlay` 跟著換；XML round-trip 測試 | ✅ 已完成（2026-09-05） |
| 2 | Business 層：移除 `FormPluginStage` 改用定義層列舉、兩道對帳閘門、`EnsureInstances` 改按需建構、`pluginNeedsSnapshot` 改吃宣告 | ✅ 已完成（2026-09-05） |
| 3 | 文件：ADR-035 決策三改寫、雙語公開文件、CHANGELOG（破壞性變更） | ✅ 已完成（2026-09-05） |

> 為什麼不把階段 1 單獨發版：階段 1 做完的中間狀態是「XML 多一個沒人讀的屬性」，
> 下游收不到任何好處，只是多一次 commit。三階段應在同一版落地。

### 階段 1 —— `src/Bee.Definition`

- 新增 `Settings/PluginSettings/PluginStage.cs`：純列舉（**非 `[Flags]`**），
  `None = 0` 起頭（理由見上節），其後 `BeforeSave / AfterSave / BeforeDelete / AfterDelete`。
- [`PluginItem.cs`](../../src/Bee.Definition/Settings/PluginSettings/PluginItem.cs)
  加 `[XmlAttribute] public PluginStage Stage { get; set; }`。
  既有 `PluginItem(string type)` **直接改成** `PluginItem(string type, PluginStage stage)`，
  不留多載 —— 既然無消費者就沒有二進位相容負擔，而留著舊那支等於留一個
  「建得出沒有時點的 `PluginItem`」的入口。
  **公開的無參數建構子必須保留**（`XmlSerializer` 的 reflection-only 路徑要用）。
- `PluginItemCollectionExtensions.Add` 同理**直接換簽章**，不留舊多載。
  （擴充方法不受「集合只能有一個 public instance `Add`」的限制 —— 那條只管 instance 方法。）
- [`PluginSettings.GetPluginTypes(progId)`](../../src/Bee.Definition/Settings/PluginSettings/PluginSettings.cs)
  目前回 `IReadOnlyList<string>`，資訊量不足 → 換成回傳型別與時點的成對結果。
  引入 `readonly record struct PluginBinding(string Type, PluginStage Stage)`
  （**不要**直接回 `PluginItem`：那是 cache 內的實例，交出去等於開放 mutate，
  違反 `rules/definition.md` 的 cache 不可異動）。
  依 `code-style.md`「消除純 facade」，舊的 `GetPluginTypes` **移除**而非留成 1-line wrapper。
- [`CustomizeOverlay.GetPluginTypes`](../../src/Bee.Definition/Customization/CustomizeOverlay.cs)
  同步換型別；兩層相加語意、base 在前、無移除語意**皆不變**。
- `PublicAPI.Unshipped.txt`：新增成員入列；被移除／換簽章的成員從 `Shipped` 移除。
  **commit message 仍須說明二進位相容性判定** —— 判定結論是「不相容但可接受，因為無消費者」，
  而不是「不必判」。analyzer 擋得住未申報，擋不住已申報但不相容（`rules/commit-verification.md`）。
- 測試 `tests/Bee.Definition.UnitTests/Settings/PluginSettingsTests.cs`：
  補 `Stage` 的 XML round-trip；`Plugins_DuplicateType_Throws` 應**維持通過且不必修改**
  —— 那是「key 沒被動到」的回歸證據。

### 階段 2 —— `src/Bee.Business`

- **移除** `Form/FormPluginStage.cs`，全檔改用 `Bee.Definition.Settings.PluginStage`。
- [`FormPluginChain`](../../src/Bee.Business/Form/FormPluginChain.cs)：
  `Entry` 由 `Stages[]` 改為單一 `Stage`；`Create` 改為同時收型別與宣告時點，於建構時對帳
  （覆寫數 ≠ 1 或 ≠ 宣告值即拋，訊息列出實際覆寫了哪些）。
  `HasStage` 化簡為相等比較。`TypesForStage` 保留（0 caller，但屬框架公開 API，
  且不是 BCL wrapper —— `code-style.md` 的保留條件）。
- [`FormPluginRunner`](../../src/Bee.Business/Form/FormPluginRunner.cs)：
  `EnsureInstances` 改按需建構（見上節）；類別註解整段改寫。
- [`PluginSettingsResolver.BuildChain`](../../src/Bee.Business/Form/PluginSettingsResolver.cs)：
  把宣告一路帶到 `FormPluginChain.Create`；缺 `Stage` 的舊格式拋出自我遷移訊息。
- [`SystemBusinessObject.ValidatePluginType`](../../src/Bee.Business/System/SystemBusinessObject.Plugin.cs)：
  現行的「至少覆寫一個時點」升級為「恰好覆寫一個，且等於宣告」；`ValidatePluginBindings` 傳入宣告。
- [`FormBusinessObject.Write.cs:214`](../../src/Bee.Business/Form/FormBusinessObject.Write.cs)
  `pluginNeedsSnapshot`：介面不變（仍問 `Chain.HasStage`），語意來源改為宣告。
  對帳成立時值等同今日，**該處無行為變更**。
- 測試：宣告與覆寫不符（多、少、錯）皆拋；
  **既有的 per-operation 一實例測試要改寫**為新語意 —— 跨時點共享已不是承諾，
  該測試若原封保留就是一個「宣稱有保證但實際沒有」的來源。

### 階段 3 —— 文件

- [adr-035](../adr/adr-035-business-logic-plugin.md) 決策三改寫（見上節）。
- 公開文件雙語同步（`rules/public-docs.md`）：
  `docs/customization.md` / `.zh-TW.md`（XML 範例在 143 / 129 行附近）、
  `docs/development-cookbook.md` / `.zh-TW.md`（470 / 452 行附近）、
  `docs/framework-capabilities.md` / `.zh-TW.md`。
  **必須寫進去的新事實**：一個 plugin 一個時點；改類別的覆寫要連帶改 XML。
- `docs/changelogs/<版號>.md` / `.zh-TW.md`：列為破壞性變更。
  **不附遷移指引**（無消費者），但要如實寫出形狀改了、以及一個 plugin 只能掛一個時點。
- XML doc 同步 —— 以下三處的現行敘述**會直接變成錯的**：
  - `PluginItem` remarks：「names the type and nothing else」
  - `FormBusinessPlugin` remarks：「Override only the stages the customization needs」、
    「state computed in `BeforeSave` can be read in `AfterSave` through an instance field」
  - `FormPluginRunner` remarks：「This is where the one-instance-per-operation guarantee lives」

### 不需要動的部分（已查證）

- **wire 契約**：`wire-contracts/` 與 `wire-fixtures/` 無 diff，`bee-connector-js` 的 CI 不受影響。
- **MessagePack 註冊**：`PluginItem` 從未進 `WireContracts.*`，不需新增 formatter。
- **既有定義檔**：repo 內無任何 `PluginSettings.xml`。
- **工具**：`tools/` / `apps/` / `samples/` 無 `PluginSettings` 消費者。
- **collection key 與重複偵測**：`Type` 仍是唯一 key，不需複合鍵。

## 決策紀錄（2026-09-05 全數定案，可動工）

| # | 題目 | 裁定 |
|---|------|------|
| 1 | 繫結粒度 | **一筆繫結一個時點**（`Stage="BeforeSave"`）。理由是責任單一：兩個時點作用本質不同，不該塞同一個類別 |
| 2 | 舊格式（缺 `Stage`）相容 | **不理會**。plugin 目前無實際消費者，不做相容處理、不留過渡路徑、不寫遷移指引 |
| 3 | `FormPluginStage` | **下移改名**：`Bee.Definition.Settings.PluginStage` 單一列舉，移除 `Bee.Business.Form.FormPluginStage` |
| 4 | ADR | **改寫 adr-035 決策三**，不新開 ADR |

上表是本 plan 的設計封印 —— 實作階段照做，不再重新討論這四題。

## 一個時間上的旁註

2026 iThome 鐵人賽 Day 21（2026-09-06 發佈，發佈後不可修改）整個第二節的節標題就是
「設定檔只列型別，一次操作一個實例」，描述的正是現行設計 —— 而本變更會同時翻掉那個標題的兩半。
這不構成不做的理由，只是決策時要知道那篇會成為**一份「當時的設計」紀錄**。
文章本身在另一個 private repo，本案不觸碰它。

## 落地紀錄（2026-09-05）

三階段同一次 commit 落地，掛在 **4.29.0**（`Version.props` 未改、tag 未推——版號由維護者決定）。

### 與 plan 的差異

- 查詢方法定名為 **`GetPluginBindings`**（`PluginSettings` 與 `CustomizeOverlay` 各一），
  回傳 `IReadOnlyList<PluginBinding>`。
- 另外新增 **`Bee.Business.Form.FormPluginBinding`**（`readonly record struct(Type, PluginStage)`）
  —— plan 只說 `Create` 要「同時收型別與宣告時點」，沒定形狀。用具名型別而非 `ValueTuple`：
  repo 的公開 API 沒有 ValueTuple 先例，且 `Create` 的可讀性靠它。
- **`FormPluginChain.Create` 多收 `progId`**：plan 指定的兩則錯誤訊息都含 progId，而 chain 本來
  就是 per-progId 的。
- **`ValidatePluginType` 改為呼叫 `Create` 再把 `InvalidOperationException` 包成
  `UserMessageException`**（plan 未寫細節）。好處是兩道閘門共用同一份對帳邏輯與訊息，
  不會各自漂移——「這個方法接受的定義，resolver 一定載得起來」。

### 未驗證項目的查證結果

1. **`PluginStage` / `PluginBinding` 撞名** —— 無。repo 內無同名 namespace，非 BCL namespace 末段，
   不撞 Avalonia / WPF 型別。`TreatWarningsAsErrors` 下 CA1724 未報，等於由 analyzer 確認過。
2. **`XmlSerializer` 對缺少 enum 型 `XmlAttribute` 的行為** —— **確實落到列舉的 0 值**。
   已釘成永久測試 `Deserialize_MissingStageAttribute_YieldsNone`（`PluginSettingsTests`），
   `None = 0` 的設計因此有機制守著。
3. **`ValidatePluginType` 的呼叫端** —— 見上「與 plan 的差異」。
4. **`ActivatorUtilities` 注入行為** —— 按需建構後不變。新增
   `Run_ConstructsWithInjectedDependencies` 釘住「三個定位參數之外的相依仍由容器解析」。
5. **per-operation 一實例測試** —— 已改寫，未原封保留。單元層換成
   `Run_ConstructsOnlyThePluginsOfTheStageBeingRun`（按需建構）與
   `Run_DifferentOperations_DoNotShareInstances`（不跨呼叫共用）；整合層那個
   `Save_BothStages_RunInOrderOnOneInstance` 改寫為
   `Save_TwoStages_RunInPipelineOrderAsSeparateInstances`，斷言兩個時點依管線順序執行、
   且是**兩個實例**。三個原本覆寫兩個時點的測試 plugin（`TracingPlugin` /
   `SnapshotProbePlugin` / `RuleOffSnapshotProbePlugin`）拆成各自單一時點的類別，
   共用記錄改放靜態容器。

### 驗證

- `dotnet build Bee.Library.slnx -c Release`：0 error 0 warning。
- `tools/` / `samples/` / `apps/Bee.Northwind`（iOS head 需 `DEVELOPER_DIR` 指到側裝的對應
  Xcode）三個 solution 皆建置成功。
- 全部 16 個測試專案綠燈。
- `./check-public-docs.sh` 與 `./check-xmldoc-refs.sh` 皆通過。
