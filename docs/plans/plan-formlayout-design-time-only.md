# 計畫：FormLayout 收回設計階段，移除執行階段自動推導

**狀態：🚧 進行中（2026-08-20）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 框架路徑：移除 `FormSchema.GetFormLayout`、generator 轉設計階段公開 API、三個執行階段呼叫點改為讀定義檔／報錯；同步補齊 `samples/Define` 落檔與全部呼叫端（測試、樣本） | ✅ 已完成（2026-08-20） |
| 2 | 公開文件雙語同步 + `docs/terminology*`、`docs/api-method-reference*`、BEE2005 訊息 + `.claude/skills` 三支 | ✅ 已完成（2026-08-20） |
| 3 | `tools/DefineEditor` 新增「由 FormSchema 產生 FormLayout」入口 + `Smoke.cs` | ✅ 已完成（2026-08-20） |
| 4 | 端到端實測（Northwind 桌面 head、案例 repo `bee-northwind-avalonia`）與 PublicAPI／發版註記收尾 | 🚧 進行中（2026-08-20）：自動化驗證全綠、外部 repo 曝險已評估；三處 UI 實跑與發版待使用者 |

## 背景

設計定位已改為：**`FormLayout` 在設計階段產生**（由定義編輯器／工具產出並存成定義檔），
**執行階段不會由 `FormSchema` 自動推導**。這是 `53025c34`（「FormLayout 是畫面的權威來源」）
同一條線的延續——既然 layout 是「畫面上有什麼」的權威、且 base schema 新增欄位不會自動出現在
已客製的租戶畫面上，那麼「沒有 layout 檔就在執行期臨時生一份」就是同一條規則的破口：
它讓「權威來源」在缺檔時退化成 schema 的即時投影，而那份投影沒有人審過、也沒有存在任何地方。

`src/Bee.Analyzers` 的 **BEE2005（FormSchema 應有對應的 FormLayout）** 已經是這個定位的先聲，
只是目前它警告的東西在執行期會被默默補上，因此沒有實質後果。

## 現況（HEAD `87875e1d` 實測）

### 三個執行階段呼叫點

| # | 位置 | 現況 |
|---|------|------|
| 1 | [FormSchema.cs:241](../../src/Bee.Definition/Forms/FormSchema.cs) | `public FormLayout GetFormLayout(string layoutId = "default") => FormLayoutGenerator.Generate(this, layoutId);` |
| 2 | [FormDefinitionLoader.cs:117](../../src/Bee.Api.Client/Definitions/FormDefinitionLoader.cs) | `CustomizeOverlay.PickFormLayout` 回 `null` 時 `return localizedSchema.GetFormLayout(effectiveLayoutId);` |
| 3 | [FormView.Commands.cs:70](../../src/Bee.UI.Avalonia/Views/FormView.Commands.cs)、[FormPage.razor.cs:90](../../src/Bee.Web.Blazor.Server/Components/FormPage.razor.cs) | 無 loader 分支：`DefinitionLoader is null ? Schema.GetFormLayout() : ...` |

伺服端**不**推導：`SystemBusinessObject.GetFormLayout` 走 `DefineAccess.FindFormLayout`，
缺檔回空字串，並在 remarks 明寫「An empty result is a normal answer meaning "no layout definition
— generate one"」。推導一律發生在 client／UI head 這一側。

### 缺檔的現行語意鏈

```
FileDefineStorage.GetFormLayout  → null（remarks：missing 是正常情境，框架會從 FormSchema 產生）
FormLayoutCache.CreateInstance   → null
CacheDefineAccess.GetFormLayout(layoutId)              → 缺檔即 throw InvalidOperationException
CacheDefineAccess.FindFormLayout(customizeId, layoutId)→ 缺檔回 null（客製層與 base 層都沒有）
```

亦即「缺檔即錯」在 `CacheDefineAccess.GetFormLayout(layoutId)` **已經成立**；
真正把 `null` 當常態的只有 `FindFormLayout` 這條 runtime 路徑，以及它的兩個消費者。

### 定義檔落檔現況

| Define 目錄 | FormSchema | FormLayout | 缺口 |
|-------------|-----------|-----------|------|
| `src/Bee.Definition/Defaults/` | Department, Employee | 兩份齊全 | — |
| `apps/Bee.Northwind/Define/` | 8 份 | 8 份齊全 | — |
| `tests/Define/` | 10 份 | 9 份 | **PermGateForm 無 layout** |
| `samples/Define/` | Department, Employee, Project | **完全沒有 FormLayout 目錄** | **三份全缺** |
| `~/Desktop/repos/bee-northwind-avalonia/Define/` | — | 8 份齊全 | —（自首次 commit 即在） |

`samples/Blazor.Server.Demo` 的 `<FormPage ProgId="Employee" />` **目前完全靠執行期推導**，
移除後會直接壞掉——這是本次唯一「不補檔就一定壞」的既有樣本。

### `GetFormLayout` 以外的兩支（確認保留，不在本次範圍）

`FormSchema.GetListLayout()` → `ListLayoutGenerator`、`GetLookupLayout()` → `LookupLayoutGenerator`
**維持原樣**。理由：清單欄位集（`FormSchema.ListFields`）與 lookup 欄位集（`LookupFields`）
本來就宣告在 `FormSchema` 上，`DefineType` 沒有對應的定義檔型別，也沒有任何落檔形式——
它們是 schema 的投影，不是獨立定義。與「單筆表單版面」是兩件事。

## 已定案（2026-08-20 使用者拍板，四項全採建議案）

### D1 — 缺檔時的錯誤落在哪一層

**✅ 採用（A）：storage 層維持 `FormLayout?`，把「缺檔即錯」放在 runtime 組裝層。**

- `IDefineStorage.GetFormLayout` 是「檔案在不在」的事實層，且同一個介面成員的另一個實作
  `CustomizeOnlyStorage.GetFormLayout` **必須**能回 `null`——租戶沒客製是常態。
  一個成員不可能同時對 base 層是「缺檔即錯」、對客製層是「缺檔正常」。
- 「缺檔即錯」在 `CacheDefineAccess.GetFormLayout(layoutId)` 已經成立，不必再往下推一層。
- 實際要改的是**誰把 `null` 當錯**：`FormDefinitionLoader.GetRuntimeLayoutAsync` 從「產生一份」
  改為擲 `InvalidOperationException`，訊息指出缺哪個 `layoutId`、該落在哪個相對路徑。
- `FileDefineStorage.GetFormLayout` 的 remarks **只改敘述不改型別**：現行那句
  「the framework generates a layout from the `FormSchema` when no file exists」在本計畫後為假，
  改為敘明 `null` 的判讀權在呼叫端（runtime 視為設定錯誤、客製層視為未覆寫）。

**已排除（B）：`IDefineStorage.GetFormLayout` 改為非可空並直接擲例外。**
代價是 `FindFormLayout`／客製層失去「沒有」這個答案，且與 `GetLanguage`（缺檔回 `null`）
的同族語意分岔。已排除。

### D2 — `FormLayoutGenerator` 的去向

**✅ 採用（A）：留在 `Bee.Definition`，轉 `public`，並移除 `FormSchema.GetFormLayout()` 實例方法。**

移除實例方法才是關鍵動作——`Schema.GetFormLayout()` 一行就能叫到，正是「執行階段順手會叫到的
形狀」；改成必須顯式 `using Bee.Definition.Layouts;` 再寫 `FormLayoutGenerator.Generate(schema, layoutId)`，
意圖就藏不住了。型別名維持 `FormLayoutGenerator`（與 `ListLayoutGenerator` /
`LookupLayoutGenerator` 同族），XML doc 的 `<remarks>` 明寫「設計階段用；執行階段一律讀定義檔」。

為何不移到 `tools/` 側：

1. `tools/DefineEditor` **沒有測試專案**（只有自帶的 `Smoke.cs`），現有
   `tests/Bee.Definition.UnitTests/Layouts/FormLayoutGeneratorTests.cs` 與
   `FormLayoutGeneratorExtraTests.cs` 會無處可去。
2. `.claude/skills/bee-scaffold-from-formschema` 的產檔手法（throw-away xUnit fact 直接呼叫
   framework public generator）依賴它是框架公開 API。
3. 外部框架使用者若自建定義工具（本 repo 的 DefineEditor 不是唯一可能的產生端）同樣需要它。

**可選加碼**：移到 `Bee.Definition.Layouts.Design` 命名空間（資料夾 `Layouts/Design/`），
讓 IntelliSense 上就分得開。代價是與 `ListLayoutGenerator` 同族的兩支分居兩個命名空間。
**已決定不做** —— 型別維持在 `Bee.Definition.Layouts`，與同族兩支並列。

**已排除（B）：整支搬進 `tools/DefineEditor`，框架完全不帶產生器。**
最徹底，但測試無處安放、scaffold skill 失效、外部工具無法產生。已排除。

### D3 — UI head 無 loader 分支換成什麼

兩個 head 的無 loader 分支目前是「純本機」路線：schema 由 `ClientInfo.DefineAccess.GetFormSchemaAsync`
（Avalonia）或 `system.GetDefineAsync<FormSchema>`（Blazor）取得，layout 則憑空產生。

**✅ 採用：同一條路徑再取一次 layout，並為 `FormView` 補一個公開 `Layout` 屬性。**

- 無 loader 但有後端 → `ClientInfo.DefineAccess.GetFormLayoutAsync(layoutId)`（已是 shipped 公開 API，
  且經 `GetDefine` 走伺服端 `CacheDefineAccess.GetFormLayout(layoutId)`，缺檔時本來就會擲例外、
  訊息明確）。Blazor 端對稱地多打一次 `GetDefineAsync<FormLayout>`。
- 無 loader 且**沒有後端**（`FormView.Schema` 是公開屬性，host 可直接塞一份 schema，
  `FormView.Resolve.cs` 的 doc 明寫這是合法用法）→ 目前這條路完全靠推導活著，移除後就沒有出口。
  故補一個對稱的公開屬性 `FormView.Layout`：host 塞了就照用，兩者都沒有才報錯。
  `samples/Avalonia.DemoCenter` 三個模組（程式碼建 schema、無後端）正是這個情境。

## 階段 1 — 框架路徑與呼叫端

### 1.1 定義層

- `src/Bee.Definition/Layouts/FormLayoutGenerator.cs`：`internal` → `public`，補設計階段定位的
  `<remarks>`。
- `src/Bee.Definition/Forms/FormSchema.cs`：**移除** `GetFormLayout(string layoutId = "default")`。
- `src/Bee.Definition/Storage/FileDefineStorage.cs`：改寫 `GetFormLayout` 的 remarks（型別不動）。
- `src/Bee.Definition/Storage/IDefineAccess.cs`：`FindFormLayout` 的 remarks 現行寫著
  「the runtime layout path calls this and generates a layout from the `FormSchema` when the result
  is `null`」，改為敘明 runtime 把 `null` 視為設定錯誤。
- `src/Bee.ObjectCaching/CacheDefineAccess.Schemas.cs`：`GetFormLayout(string)` 與
  `FindFormLayout` 的 remarks 同步（現行兩處都以「generates from the FormSchema instead」解釋
  為何要有可空的那一支）。

### 1.2 執行階段組裝層

- `src/Bee.Api.Client/Definitions/FormDefinitionLoader.cs`
  - `GetRuntimeLayoutAsync`：`definition is null` 分支改擲 `InvalidOperationException`。
  - 類別頂部 `<remarks>` 的第 4 點「generate from the schema when neither exists」與
    `GetRuntimeLayoutAsync` 的 summary（「else one generated from the schema」）同步改寫。

### 1.3 UI head

- `src/Bee.UI.Avalonia/Views/FormView.cs` / `FormView.Commands.cs` / `FormView.Resolve.cs`：
  新增公開 `Layout` 屬性；`ResolveLayoutAsync` 依 D3 的三段順序解析；
  `FormView.cs:22` 與 `FormView.Resolve.cs` 中提到「layout is generated from it」的 doc 同步。
- `src/Bee.Web.Blazor.Server/Components/FormPage.razor.cs`：無 loader 分支改抓
  `GetDefineAsync<FormLayout>`，註解同步。
- `src/Bee.UI.Avalonia/Permissions/LayoutCapabilityApplier.cs:11` 的 doc 提到
  `FormSchema.GetFormLayout`，同步。

> `FormView.cs:22` 用的是 `<see cref="FormSchema.GetFormLayout"/>`——成員移除後**編譯期就會
> CS1574 失敗**，不會漏。這正是 `code-style.md` 要求散文用 `<see cref>` 的理由。

### 1.4 定義檔落檔

- **`samples/Define/FormLayout/` 補 Department / Employee / Project 三份**（必要，否則
  `samples/Blazor.Server.Demo` 壞）。產法用 `.claude/skills/bee-scaffold-from-formschema`
  的 throw-away xUnit fact，直接序列化 generator 原貌。
- `tests/Define/FormLayout/PermGateForm.FormLayout.xml`：**先判定再補**——確認
  `PermGateForm` 是否有任何測試走版面路徑。純權限 fixture 就不補，但要確認 BEE2005 是否
  對 `tests/Define` 生效（見階段 2）。

### 1.5 呼叫端機械改寫

- `samples/Avalonia.DemoCenter/Modules/`：`AutoFormLayoutModule`、`MasterDetailModule`、
  `MultiColumnLayoutModule` 三個模組改呼叫 `FormLayoutGenerator.Generate(...)` 並設到
  `FormView.Layout`。`AutoFormLayoutModule` 的標題與敘述（現為「FormLayout 自動產生」／
  「免手繪版面」）改為敘明這是**設計階段**產生器的示範。
- 測試約 20+ 處（`Bee.UI.Avalonia.UnitTests`、`Bee.Web.Blazor.Server.UnitTests`、
  `Bee.Definition.UnitTests`）改為 `FormLayoutGenerator.Generate(schema, "...")`。
  測試 fixture 呼叫設計階段 generator 是正當用法，不需改成讀檔。
- `tests/Bee.Definition.UnitTests/FormSchemaTests.cs:141` 的 `[DisplayName]`
  「GetFormLayout 對稱於 GetListLayout…」敘述已不成立，改寫。

### 1.6 PublicAPI

- `src/Bee.Definition/PublicAPI.Shipped.txt:384` 移除
  `Bee.Definition.Forms.FormSchema.GetFormLayout(string! layoutId = "default") -> ...`，
  於 `PublicAPI.Unshipped.txt` 以 `*REMOVED*` 申報。
- 新增 `Bee.Definition.Layouts.FormLayoutGenerator` 與 `Generate` 的公開項目。
- **這是二進位破壞性變更**（移除公開方法 → 消費端 `MissingMethodException`），
  屬 minor 版以上，須在 commit message 明寫判定（見 `rules/commit-verification.md` 第 2 條）。
- `src/Bee.UI.Avalonia/PublicAPI.Unshipped.txt` 新增 `FormView.Layout`。

## 階段 1 執行中的修正（2026-08-20 實測）

三點與計畫的「現況」描述不同，記錄於此以免後續階段照錯的前提推導：

1. **base 層缺 layout 檔時，client 端不是拿到 `null`，而是當場收到伺服端的例外。**
   計畫依 `SystemBusinessObject.GetFormLayout`（缺檔回空字串）推得「空 XML → `default!` → `null`」，
   但 `ClientDefineAccess.GetFormLayoutAsync` 走的是 `GetDefine`／`DefineType.FormLayout` 這條，
   對應伺服端 `CacheDefineAccess.GetFormLayout(layoutId)` —— **缺檔即 throw**，傳到 client 是
   `UserMessageException: FormLayout 'X' not found.`。
   因此 `FormDefinitionLoader` 新加的 `InvalidOperationException` 是**第二道防線**（涵蓋伺服端回
   空 payload 的情形），不是主要路徑。行為結論不變（缺檔即失敗、不推導），但
   **測試不可斷言例外型別**，改為驗「擲例外且訊息含 layoutId」。

2. **`FormView` 無 loader 分支必須 `Clone()`。** `ClientDefineAccess` 逐實例快取定義，
   而 `LayoutCapabilityApplier.Apply` 是 in-place mutate ——
   直接把快取實例交給它會違反「cache 內定義 init 後不可異動」。loader 那條本來就 clone。

3. **BEE2005 未對 `tests/Define` 生效**（計畫階段 1.4 待確認的事項）：
   `PermGateForm` 無 layout 檔而 clean Release build 零警告。該 fixture 只驗 BO 權限 gate、
   攔在進 repository 之前，不走版面路徑，故**判定不補**。

## 階段 2 — 文件與 skill 同步

### 公開文件（雙語必須同步）

| 檔案 | 要改什麼 |
|------|---------|
| `docs/definition-files-overview.md` / `.zh-TW.md` | §1 FormLayout 那一列的「Owns」補上「設計階段產出的定義檔」；§7 客製層敘述維持不變（整檔取代不受影響），但要確認沒有殘留「缺檔會產生」的推論 |
| `docs/architecture-overview.md` / `.zh-TW.md` | §「UI 推導來源」、§5「FormLayout 與 TableSchema 預設由 FormSchema 推導產生」、NoCode 那一列的「FormSchema → FormLayout + TableSchema 自動產生」——三處都要把「推導」限定在**設計階段**，不改「FormLayout 是 FormSchema 的 UI 投影」這個仍然為真的敘述 |
| `docs/terminology.md` / `.zh-TW.md` | `FormLayoutGenerator` 那一列「Automatically generates a FormLayout from a FormSchema」→ 加上設計階段限定 |
| `docs/api-method-reference.md` / `.zh-TW.md` | `GetFormLayout` 那一列現寫「Returns a `FormLayout` (generated from auto-localized FormSchema)」，**這句在 HEAD 就已經是錯的**（該方法回傳的是原樣儲存的 base 定義），順手更正 |
| `docs/development-cookbook.md` / `.zh-TW.md` | 圖中的「→ 衍生 FormLayout（UI 維度）」加設計階段限定 |
| `docs/analyzer-rules.md` / `.zh-TW.md` | BEE2005 的說明從「應有」升級為「執行階段必需」 |

### analyzer

- `src/Bee.Analyzers/Definitions/SidecarDefinitionAnalyzer.cs` 的 BEE2005 訊息改寫
  （缺 layout 現在是執行期會失敗，不只是建議）。
- **嚴重度維持 Warning**（已定案）：升 Error 會讓既有 app repo 立刻建置失敗，代價不成比例。

### `.claude/` skill（會直接誤導未來 session，必須同步）

- `.claude/skills/bee-add-form/SKILL.md:8` —— 「FormLayout 由框架從 FormSchema 自動產生
  （`FormSchema.GetFormLayout` → `FormLayoutGenerator`），**不必寫**」→ 全句反轉，
  並把 checklist 從 4 處改為 5 處（含 FormLayout 落檔）。同檔 71 行、
  `.claude/skills/bee-app-scaffold/SKILL.md:126` 的「免寫 FormLayout」同理。
- `.claude/skills/bee-scaffold-from-formschema/SKILL.md` —— 27 行的「僅在要客製版面時……
  不落檔也能跑」反轉為必要；61–84 行樣板的 `schema.GetFormLayout("{ProgId}")` 改為
  `FormLayoutGenerator.Generate(schema, "{ProgId}")`；階段 3 落地後再補一句指向 DefineEditor。

> `docs/plans/archive/` 下的舊 plan **不改**——那是當時的紀錄，不是現行行為。

> **階段 2 執行中補上的兩處（計畫未列）**：
> `SystemBusinessObject.GetFormLayout` 的 remarks 原寫「缺檔時 caller 應由 schema 產生一份」，
> 與新定位相反（計畫只把它當「伺服端不推導」的證據引用，沒列入待改）；
> `samples/Blazor.Server.Demo/README` 雙語的技術對照表點名 `FormSchema.GetFormLayout(layoutId)`，
> 該成員已不存在。

## 階段 3 — DefineEditor 產生入口

`tools/DefineEditor` 已有 `ViewModels/FormLayoutDocumentViewModel.cs`（可開啟／編輯／存檔），
缺的是「從 FormSchema 產生一份」的入口。

- 在 FormSchema 節點（`DefinePathScanner` 的 `DefineType.FormSchema` 節點）加 context menu
  命令「產生 FormLayout」，或掛在 `FormSchemaDocumentViewModel` 上。
- 行為：讀該 `.FormSchema.xml` → `FormLayoutGenerator.Generate(schema, progId)` →
  寫入 `{DefinePath}/FormLayout/{progId}.FormLayout.xml` → 開啟該文件。
- **既有檔案必須先確認覆寫**——重新產生會丟掉人工調整過的版面，這是本功能唯一的破壞性動作。
- `tools/DefineEditor/Smoke.cs` 補一則（現有 477–496 行已有 FormLayout 文件的 smoke 樣板可參照）。

## 階段 4 — 實測與收尾

1. `dotnet build --configuration Release`（`TreatWarningsAsErrors=true`，任何警告即失敗）。
2. `./test.sh`（全部）。
3. `./check-public-docs.sh`。
4. `samples/Blazor.Server.Demo` 實跑，確認補檔後 `<FormPage ProgId="Employee" />` 正常。
5. `samples/Avalonia.DemoCenter` 三個 layout 模組實跑。
6. `apps/Bee.Northwind` 桌面 head 實跑（含 `Customize/` 的 Order 整檔覆寫仍生效）。
7. **案例 repo `~/Desktop/repos/bee-northwind-avalonia` 實測**——八份版面檔齊全，
   行為上不應受影響，但要確認 demo 仍跑得起來（這是本計畫唯一的外部消費者證據）。
8. CHANGELOG 草稿走 `/dev-workflow:changelog-draft`；破壞性變更判定寫進 commit message。

## 階段 4 進度（2026-08-20）

已完成（自動化）：

| # | 項目 | 結果 |
|---|------|------|
| 1 | 四個 solution clean Release build（`Bee.Library` / `samples` / `tools` / Northwind Desktop） | 0 警告 0 錯誤 |
| 2 | `./test.sh` 全套 | 5680 通過 / 1 略過 / 0 失敗 |
| 3 | `./check-public-docs.sh` | 輸出與本計畫動工前一致（只剩既有已知誤報） |
| 7 | 案例 repo `bee-northwind-avalonia` 曝險評估 | **零曝險**：8 份 FormSchema 對 8 份 FormLayout 齊全，且全 repo 無 `GetFormLayout` 呼叫；它固定在 `PackageReference 4.22.0`，**升版前不受影響** |
| — | `tools/DefineEditor --smoke` | 全 14 項綠（含新增的 `formlayout-gen`） |

待使用者執行：

- **4 / 5 / 6 的 UI 實跑**（`Blazor.Server.Demo`、`Avalonia.DemoCenter` 三個 layout 模組、
  Northwind 桌面 head 含 `Customize/` 的 Order 覆寫）—— 依
  `src/Bee.UI.Avalonia/CLAUDE.md` 的偏好「改動編譯通過即可交付，由使用者自行啟動測試」。
- **8 發版收尾**（CHANGELOG 草稿、版號、PublicAPI.Unshipped → Shipped）—— 屬發版流程，
  版號與時機是使用者決策。破壞性變更判定已寫進階段 1 的 commit message。

> **第 7 項的計畫假設已修正**：計畫把案例 repo 實測列為「本計畫唯一的外部消費者證據」，
> 但它消費的是已發佈套件，現在跑只驗得到 4.22.0 的行為。真正該驗的時點是**升到本版之後**。

## 風險與不在範圍

- **破壞性**：任何**依賴執行期推導**的外部消費者（沒有落 FormLayout 檔的 app）升版後會在
  開表單時擲例外。這是本計畫的意圖，但必須在 CHANGELOG 明列「缺檔即失敗」與補檔方法。
- **不在範圍**：`GetListLayout()` / `GetLookupLayout()`（見上）、客製層的整檔取代語意、
  `FormLayoutCaptionApplier`（layout 只描述結構、文字取自 schema 的分工不變）。
- **順序**：階段 3（DefineEditor 入口）理論上該在階段 1 之前，才有「正規產生方式」。
  但階段 1 需要的三份 sample 落檔可用 scaffold skill 的 throw-away fact 產出，
  故不阻塞；若使用者偏好，階段 3 可提前。
