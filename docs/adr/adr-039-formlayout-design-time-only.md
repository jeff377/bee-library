# ADR-039：`FormLayout` 收回設計階段，執行階段不再由 `FormSchema` 推導

## 狀態

**已採納（Accepted，2026-08-20）** —— 決策已執行，隨 4.23.0 發佈。

[ADR-016](adr-016-multitenant-customization-overlay.md) 立下客製層的**整檔取代**語意，
「`FormLayout` 是畫面的權威來源」則是該語意的推論，明文寫在
[定義檔全景](../definition-files-overview.zh-TW.md)（commit `53025c34`）。
本 ADR 補齊該推論在**缺檔情境**下的行為；ADR-016 的雙層唯讀疊加語意不變。

## 背景

`FormLayout` 是 `FormSchema` 在 UI 維度的投影，描述表單的視覺配置。框架早已確立
**「`FormLayout` 是畫面上有什麼的權威來源」**（[定義檔全景](../definition-files-overview.zh-TW.md)）：
客製層採整檔取代，因此 base schema 新增欄位**不會**自動出現在已客製的租戶畫面上——
租戶看到什麼，由租戶那份版面檔說了算。

但這條規則有一個破口：**版面檔缺席時，執行階段會由 `FormSchema` 臨時推導一份**。
入口是 `FormSchema.GetFormLayout(string)`，一行呼叫 `FormLayoutGenerator`，被三條執行階段
路徑使用（`FormDefinitionLoader` 的缺檔分支，以及 Avalonia / Blazor 兩個 UI head 的無 loader 分支）。

這使得「權威來源」在缺檔時退化成 schema 的即時投影，而**那份投影沒有人審過、也沒有存在
任何地方**——恰恰是「權威來源」這個概念要排除的東西。同一個部署可能今天推導出 A 版面、
明天因為 schema 加了一個欄位就推導出 B 版面，中間沒有任何一次人為決定。

`src/Bee.Analyzers` 的 **BEE2005（FormSchema 應有對應的 FormLayout）** 已經是這個定位的先聲，
只是它警告的東西在執行期會被默默補上，所以那道警告背後沒有任何後果。

## 決策

**`FormLayout` 一律在設計階段產出並存成定義檔；執行階段原樣讀取，缺檔屬設定錯誤。**

### 一、缺檔的錯誤落在 runtime 組裝層，不下推到 storage

`IDefineStorage.GetFormLayout` 維持 `FormLayout?`（可空），不改為「缺檔即擲」。

理由是同一個介面成員有兩個語意相反的實作：`CustomizeOnlyStorage.GetFormLayout` **必須**能回
`null`——租戶沒有客製是常態。一個成員不可能同時對 base 層是「缺檔即錯」、對客製層是
「缺檔正常」。storage 層只回答「檔案在不在」，**如何判讀 `null` 屬於呼叫端**。

改的是誰把 `null` 當錯：`FormDefinitionLoader.GetRuntimeLayoutAsync` 由「產生一份」改為擲
`InvalidOperationException`，訊息指出缺的 `layoutId` 與該檔案應落的相對路徑。

> **實作時的修正**：base 層缺檔時，實際上多半是**伺服端**先擲例外——
> `ClientDefineAccess.GetFormLayoutAsync` 走 `GetDefine` / `DefineType.FormLayout`，
> 對應伺服端 `CacheDefineAccess.GetFormLayout(layoutId)`，而該多載本來就對缺檔擲例外
> （「缺檔即錯」在那一層**早已成立**）。loader 的 guard 涵蓋的是剩下那種情形：伺服端回空 payload。
> 因此**測試不應斷言例外型別**，只驗「擲例外且訊息含 layoutId」。

### 二、產生器留在 `Bee.Definition` 並轉為 `public`，同時移除 `FormSchema.GetFormLayout`

`FormLayoutGenerator` 由 `internal` 改 `public`，`<remarks>` 明寫設計階段定位。

**移除實例方法才是關鍵動作。** `Schema.GetFormLayout()` 一行就能叫到，正是「執行階段順手會叫到
的形狀」；改成必須顯式 `using Bee.Definition.Layouts;` 再寫
`FormLayoutGenerator.Generate(schema, layoutId)`，意圖就藏不住了。

不搬到 `tools/` 側的三個理由：

1. `tools/DefineEditor` **沒有測試專案**（只有自帶的 `Smoke.cs`），既有的產生器測試會無處安放。
2. 本 repo 的 scaffolding 流程以「框架公開 API」的形式呼叫它。
3. 外部框架使用者若自建定義工具，`DefineEditor` 不是唯一可能的產生端。

### 三、UI head 的無 loader 分支改為三段解析，並補 `FormView.Layout`

`FormView`（Avalonia）新增可覆寫的 `ResolveLayoutAsync`，依序解析：

1. host 設定的 `FormView.Layout`
2. `DefinitionLoader` 組裝出的執行階段版面
3. 經 `ClientInfo.DefineAccess` 取得的 base 定義
4. 都沒有 → 擲 `InvalidOperationException`

第 1 段所需的公開屬性 `FormView.Layout` 是本決策的必要配套：`FormView.Schema` 是公開屬性，
host **可以**直接塞一份 schema 而背後沒有任何後端（`samples/Avalonia.DemoCenter` 的版面模組
正是如此）。移除推導後這條路沒有出口，故補一個對稱的 `Layout`。

Blazor 的 `FormPage` 對稱地改讀 `GetDefineAsync<FormLayout>`。

> 第 3 段**必須 `Clone()`**：`ClientDefineAccess` 逐實例快取定義，而
> `LayoutCapabilityApplier.Apply` 是就地 mutate，直接把快取實例交給它會違反
> 「cache 內定義 init 後不可異動」。loader 那條路徑本來就 clone。

### 四、`GetListLayout()` / `GetLookupLayout()` 不在範圍內

兩者維持原樣。清單欄位集（`FormSchema.ListFields`）與 lookup 欄位集（`LookupFields`）
**本來就宣告在 `FormSchema` 上**，`DefineType` 沒有對應型別，也沒有任何落檔形式——
它們是 schema 的投影，不是獨立定義。與「單筆表單版面」是兩件事。

### 五、BEE2005 升級敘述，嚴重度維持 Warning

訊息由「應有」改為「缺檔在執行期會失敗」。**不升為 Error**：那會讓既有 app repo 立刻建置失敗，
代價與收益不成比例；缺檔的實際後果已由執行期擲例外承擔。

## 理由

**為什麼不保留推導當作「方便的預設」。** 因為它與「權威來源」不相容，而不是因為它不方便。
一份沒有人審過、不存在於任何地方的版面，其內容會隨 schema 漂移而無聲改變——使用者看到的畫面
因此取決於「最後一次有人改 schema 是什麼時候」，而不是「最後一次有人決定畫面長怎樣」。
這正是「版面是權威來源」這條規則要排除的情形，只是先前沒有把缺檔這條路一併收掉。

**為什麼是破壞性變更而非漸進廢棄。** `[Obsolete]` 標註無法阻止推導繼續發生，而推導繼續發生
就等於規則繼續有破口。BEE2005 已存在，升版前可先建置取得完整的缺檔清單，遷移路徑明確
（用 `DefineEditor` 產生一份、審過、存檔），因此直接移除的成本可控。

**為什麼產生器要留在框架而非只留在工具。** 「版面在設計階段產生」是規則，「用哪個工具產生」
不是。把產生器留在框架公開 API，外部使用者才能自建產生端，規則本身不綁定 `DefineEditor`。

## 後果

**正面**：

- 「`FormLayout` 是畫面權威來源」不再有例外情形；版面內容一律是某次人為決定的結果。
- 缺檔從靜默補上變成**建置期警告（BEE2005）＋執行期明確例外**，兩道都指出該補哪個檔案。
- 產生器成為公開 API，外部定義工具可用同一份實作。

**負面 / 成本**：

- **二進位破壞性變更**：移除 public `FormSchema.GetFormLayout`，消費端會得到
  `MissingMethodException`。已於 `PublicAPI.Unshipped.txt` 以 `*REMOVED*` 申報。
- **任何依賴執行期推導的部署升版後會在開表單時失敗**——這是本決策的意圖，
  但必須在 CHANGELOG 明列補檔方法。
- 新增一張表單的定義工作由 4 處變 5 處（多一份 FormLayout 落檔）。

**配套**（隨本決策一併落地）：

- `tools/DefineEditor` 的 FormSchema 節點新增「產生 FormLayout」命令，寫入
  `{DefinePath}/FormLayout/{ProgId}.FormLayout.xml`。**既有檔案覆寫前先確認**——
  重新產生會丟掉人工調整過的版面，是該功能唯一的破壞性動作。
- `samples/Define/` 補上三份先前完全依賴推導的版面檔。
