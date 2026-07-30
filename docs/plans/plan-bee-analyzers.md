# 計畫：Bee.Analyzers — 把框架慣例變成編譯期錯誤

**狀態：🚧 進行中（2026-07-30）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 基礎設施：analyzer 專案 + `AdditionalFiles` 管線 + 3 條規則走通端到端 | ✅ 已完成（2026-07-30） |
| 2 | XML 定義檔規則（BEE1xxx 單檔、BEE2xxx 跨檔一致性）完整化 | 📝 待做 |
| 3 | 序列化與 wire 合約規則（BEE4xxx） | 📝 待做 |
| 4 | C# 程式碼慣例規則（BEE3xxx） | 📝 待做 |
| 5 | 發佈整合、消費端零設定驗證、對外文件 | 📝 待做 |

## 背景

框架以 NuGet 套件形式發布，外部開發者**只引用套件、不持有 repo source**。這個前提決定了
「讓 AI coding 工具快速理解如何在此框架實作功能」的可行手段：

| AI 在消費端能看到 | 來源 | 需開發者做什麼 |
|---|---|---|
| ✅ analyzer 診斷訊息 | 隨 NuGet 自動生效 | **什麼都不用做** |
| ✅ `Bee.*.xml`（XML doc） | 套件必帶（已開 `GenerateDocumentationFile`） | 無 |
| ✅ 套件 README | `PackageReadmeFile` 已設 | 無，但 AI 不會主動翻 |
| ❌ `docs/`、`docs/adr/`、`.claude/rules/` | 只存在於本 repo | 拿不到 |

`docs/` 下 30+ 份雙語公開文件與 `.claude/skills/` 的 11 個 skill，外部開發者的 AI 全都讀不到。

### 為何 analyzer 優先於補文件

AI coding 的迴圈是 **write → build → fix**。文件只影響第一次 write；編譯錯誤影響**每一次** fix。
且 analyzer 是唯一「主動找上 AI」而非等 AI 來讀的管道，並隨套件版本必然對齊——不像散佈到消費端
的慣例文件會隨版本升級而漂移（漂移後比沒有更危險）。

其餘手段（XML doc `<remarks>` 強化、`dotnet new` template 帶 AGENTS.md、MCP server、
Claude Code plugin skill 包）不衝突，屬後續獨立 plan；本 plan 只做 analyzer。

## 關鍵設計決策

### D1：主戰場是 XML 定義檔，不是 C# 語法樹

框架的核心工作流是**改定義檔**而非寫程式碼——一張可用的 CRUD 表單 = 4 處純定義修改，
不寫 UI / CRUD 程式碼。因此若 analyzer 只分析 C# 語法樹，會錯過大部分實際錯誤面。

Roslyn analyzer 可透過 `AdditionalFiles` 讀取非 C# 檔案。本 repo **已有此機制的先例**：
`src/Directory.Build.props` 用 `<AdditionalFiles Include="PublicAPI.Shipped.txt" />` 供
`Microsoft.CodeAnalysis.PublicApiAnalyzers` 讀取。

消費端定義檔結構（依 `tests/Define/` 實測）：

```
Define/
├── DatabaseSettings.xml
├── DbCategorySettings.xml          # DbCategory Id="common|company|log" + TableItem 清單
├── SystemSettings.xml
├── FormSchema/    <ProgId>.FormSchema.xml
├── FormLayout/    <ProgId>.FormLayout.xml
├── TableSchema/   <categoryId>/<dbTableName>.TableSchema.xml
└── Language/      <culture>/<ProgId>.Language.xml
```

### D2：內嵌於 `Bee.Definition` 套件，不發佈獨立 NuGet 套件

analyzer 專案位於 `src/Bee.Analyzers/`（不獨立打包），透過 `Bee.Definition.csproj` 的 pack
設定把產出 dll 放進 nupkg 的 `analyzers/dotnet/cs/` 路徑。

理由：
- **零額外安裝**——開發者 `dotnet add package Bee.Definition` 即自動生效，符合本 plan 的核心目標
- 定義檔驗證的語意屬於 `Bee.Definition` 的職責範圍
- 規避「新增 src 套件時 CI/發佈 workflow 的 pack 清單非 glob」的漏發佈風險（`build-ci.yml:202-217`
  與 `nuget-publish.yml:62-79` 皆為逐行列舉）

> 待確認：序列化規則（BEE4xxx）針對的型別多定義於 `Bee.Api.Core` / `Bee.Api.Contracts`，
> 但 analyzer 只需在**消費端**生效即可，仍隨 `Bee.Definition` 散佈（消費端必然間接引用它）。
> 若實作後發現需要 `Bee.Api.Core` 的型別符號才能判定，改以 `Compilation.GetTypeByMetadataName`
> 按名稱解析，避免 analyzer 專案產生對 `Bee.Api.Core` 的編譯期依賴。

### D3：severity 依「誤判可能性」分級，不依「後果嚴重性」

兩個前提決定這個分級軸：

1. **`info` 對 AI 等於不存在**——`dotnet build` 的預設 verbosity 不顯示 `info` 級診斷（只有 IDE 顯示）。
   AI coding 的迴圈是跑 build 讀輸出，因此 `info` 規則對本 plan 的核心目標毫無作用。
   **除純建議類外一律不使用 `info`。**
2. **既有專案升版不該被誤判擋下**——消費端若也開 `TreatWarningsAsErrors=true`，warning 亦等於 error。
   因此決定 severity 的關鍵不是「違反後果多嚴重」，而是「這條規則判錯的機率有多高」。

首版分級：

| 確定性 | 規則 | 首版 severity |
|--------|------|--------------|
| **高**——違反必然是錯，誤判空間極小 | BEE1001–1005、BEE1007（非法列舉值、重複欄名、單檔內欄位參照）<br>BEE4001–4006、BEE4008（序列化沉默失敗與 wire 合約） | `error` |
| **中**——極可能是錯但有合理例外 | BEE1006、BEE2001–2006（跨檔一致性）<br>BEE3001–3002、BEE4007、BEE4009 | `warning` |
| **純建議** | BEE2007（缺翻譯）、BEE3003（cache mutate，僅能偵測明顯樣式） | `info` |

升級路徑：`warning` 級規則上線後觀察實際誤判率，確認低誤判後再逐條升為 `error`。

> `info` 級的兩條規則**已知對 AI 無效**，保留它們僅為 IDE 提示價值；不指望它們達成本 plan 的目標。

全部可由消費端 `.editorconfig` 逐條調整，並提供整組關閉的 MSBuild property。

### D4：診斷訊息寫給 AI 讀，不是傳統簡短診斷

訊息需自帶「錯在哪 + 為什麼 + 怎麼改」，讓 AI 無需查文件即可自我修正：

```
BEE2001: FormSchema 'Product' declares CategoryId 'common', but its DbTableName 'ft_product'
         is not registered under the 'common' category in DbCategorySettings.xml
         (it is registered under 'company'). CategoryId selects the database scope;
         business tables must use 'company'.
         Fix: change the FormSchema CategoryId to 'company'.
```

### D6：診斷訊息一律英文；說明文件雙語

診斷訊息直接出現在外部開發者的 build output 與 IDE 錯誤清單，讀者與 XML doc 相同（NuGet 套件
使用者），因此**一律英文**——與 `rules/public-docs.md`、`rules/code-style.md` 對公開 API surface
的既有要求一致。

適用範圍：

| 產出 | 語言 |
|------|------|
| `DiagnosticDescriptor` 的 `title` / `messageFormat` / `description` | 英文 |
| analyzer 原始碼的 in-body 註解 | 英文（公開 repo 慣例） |
| `docs/` 下的規則說明文件 | 雙語（`xxx.md` + `xxx.zh-TW.md`） |
| 消費端 README 段落 | 英文（`README.md`）+ 中文（`README.zh-TW.md`） |
| 本 plan 與 `docs/plans/` | 繁體中文（非公開文件） |

訊息撰寫要求（英文條件下仍須滿足 D4）：完整句子、可直接指出修正動作；程式碼識別字與定義檔
屬性名用反引號或單引號包圍，避免 SonarCloud S125 誤判。

### D5：analyzer 的視野是「一次編譯」——同一規則在框架內外能力不同

這是規則設計上最容易誤判可行性的一點。編譯消費端專案時，analyzer 看得到開發者自己的語法樹，
但框架內部 `private static readonly` 欄位的初始化運算式只存在於 IL，**analyzer 拿不到**。

以 BEE4001（formatter 未註冊）為例：

| 執行位置 | 能做到 |
|---|---|
| 框架自身建置（編譯 `Bee.Api.Core`） | 完整差集比對：所有 `MessagePackCollectionBase<>` 子類 ↔ `MessagePackCodec` 註冊清單，漏的直接 `error` |
| 消費端建置 | 僅能偵測「定義了 `MessagePackCollectionBase<>` 子類」，無從得知是否已註冊 |

因此 BEE4001 拆為兩個版本，框架內版本先做（見階段 3）；消費端版本有前置依賴（見下）。

## 規則清單

### A 類：XML 定義檔單檔驗證（BEE1xxx）

| ID | 規則 | Severity |
|----|------|----------|
| BEE1001 | `FormSchema/@CategoryId` 僅接受 `common` / `company` / `log` | error |
| BEE1002 | `DbCategory/@Id` 僅接受上述三值 | error |
| BEE1003 | `FormField/@DbType` 必須為合法 `DbType` 列舉值 | error |
| BEE1004 | `ListFields` / `LookupFields` 列出的欄位必須是本 schema 已宣告的 `FormField/@FieldName` | error |
| BEE1005 | `FieldMapping/@DestinationField` 必須是本 schema 已宣告欄位 | error |
| BEE1006 | 標記 `Type="RelationField"` 的欄位應為某 `FieldMapping` 的 `DestinationField` | warning |
| BEE1007 | 同一 `FormTable` 內 `FieldName` 不得重複 | error |

### B 類：XML 定義檔跨檔一致性（BEE2xxx）— 文件永遠做不到

| ID | 規則 | Severity |
|----|------|----------|
| BEE2001 | `FormSchema/@DbTableName` 必須登記在 `DbCategorySettings.xml` 中**對應 CategoryId** 的 `<TableItem>` 下 | warning |
| BEE2002 | `FormSchema/@DbTableName` 必須有對應的 `TableSchema/<categoryId>/<dbTableName>.TableSchema.xml`（資料夾必須等於 CategoryId） | warning |
| BEE2003 | `FormField/@RelationProgId` 指向的 ProgId 必須存在對應 `FormSchema` 檔 | error |
| BEE2004 | `FieldMapping/@SourceField` 必須是被引用 ProgId 之 FormSchema 的已宣告欄位 | error |
| BEE2005 | FormSchema 的每個 ProgId 應有對應 `FormLayout` 檔 | warning |
| BEE2006 | FormSchema 欄位與對應 TableSchema 的實體欄位應一致（缺漏 / 型別不符） | warning |
| BEE2007 | `Language/<culture>/` 各語系檔的 sub-key 覆蓋應一致（缺翻譯） | info |

> BEE2001 / BEE2002 直接對應已知的高頻踩雷：`CategoryId` 是 DB scope 選擇器、業務表必須
> `company`、TableSchema 資料夾必須等於 CategoryId。這些目前**只有 runtime 才會炸**。

### C 類：C# 程式碼慣例（BEE3xxx）

| ID | 規則 | Severity |
|----|------|----------|
| BEE3001 | 繼承 `FormBusinessObject` / `SystemBusinessObject` 的公開 API 方法需標 `[ApiAccessControl]` | warning |
| BEE3002 | 定義型別的集合屬性不得使用裸 `List<T>` / `Collection<T>`，須繼承 `KeyCollectionBase` / `CollectionBase` | warning |
| BEE3003 | `IDefineAccess.GetX` 取得的 cache 物件不得 mutate（需 `Clone()`） | info（僅能偵測明顯樣式） |

### D 類：序列化與 wire 合約（BEE4xxx）— 沉默失敗的重災區

API 合約需同時通過 **XML（持久化）+ JSON + MessagePack（wire）** 三棲序列化。這類錯誤的共同特徵是
**桌面測不出來**：不是完全不報錯（資料默默消失），就是只在特定路徑（wire round-trip、行動端
reflection-only）才爆炸。

| ID | 規則 | Severity |
|----|------|----------|
| BEE4001 | 繼承 `MessagePackCollectionBase<>` 的具體型別未註冊 `CollectionBaseFormatter` → **序列化為空集合且無任何錯誤** | error |
| BEE4002 | `[JsonPropertyName("x")]` 與 `[MessagePackObject(keyAsPropertyName: true)]` 併用 → JSON 與 MessagePack 的欄位名不一致，兩個 wire 格式對不上 | error |
| BEE4003 | `[Union]` 多型型別不得使用 `keyAsPropertyName: true`，必須維持整數 `[Key]` | error |
| BEE4004 | **僅整數 `[Key]` 型別**：public 建構子的參數順序必須跟隨 `[Key]` **數值**順序（見下方實測） | error |
| BEE4005 | 繼承 `KeyCollectionBase<T>` / `MessagePackKeyCollectionBase<T>` 的型別只能有一個 public instance `Add`（多載在 reflection-only 路徑擲 `AmbiguousMatchException`） | error |
| BEE4006 | 三棲型別必須有無參數建構子（XML 反射路徑與行動端 reflection-only 皆需要） | error |
| BEE4007 | 衍生 / 計算屬性的 ignore 標籤不一致——`[IgnoreMember]`、`[JsonIgnore]`、`[XmlIgnore]` 只標其中一兩個 | warning |
| BEE4008 | `[MessagePackObject]` 型別的 public 屬性缺 `[Key]` 且未開 `keyAsPropertyName` | error |
| BEE4009 | wire 合約破壞語意化：公開屬性改名在 `keyAsPropertyName: true` 下即破壞 wire 相容性 | warning |

**規則依據與備註**：

- **BEE4001** 的約束已由框架自己在原始碼寫下：`MessagePackCodec.cs:39-42`（「every
  `MessagePackCollectionBase<>` collection must be registered above ... An unregistered collection
  serializes as empty with no error」）與 `FormatterResolver.cs:14-34`（該 resolver 看似 fallback
  但實際 unreachable）。註冊清單為 `MessagePackCodec.cs:29-36` 的逐行列舉，漏一行無任何機制會紅
  ——與 CI pack 清單同一種病。
- **BEE4003** 對應 `FilterNode`（`[Union(0, typeof(FilterCondition))]`）的既有約束：`[Union]` 多型
  與 name-based key 互斥。
- **BEE4004 / BEE4005 / BEE4006** 對應的失敗完全不在桌面顯現：ctor 順序錯會讓 wire round-trip
  對調欄位而 XML / JSON 測試全綠；後兩者只在行動端 reflection-only 路徑擲例外。
- **BEE4004 的範圍已由實測（MessagePack 3.1.7）定案**，與原先假設不同，詳見階段 1 的實測結論。
- **BEE4009** 價值有限，因為公開屬性改名**已會**被既有的 `PublicApiAnalyzers`（RS0016）攔下。
  本規則只補兩塊：把 RS0016 的訊息語意化為「這會破壞 wire 相容性」，以及補 `[Union]` 整數 Key
  變更的偵測（RS0016 管不到 attribute 參數）。排在最後，可視情況不做。

### BEE4001 消費端版本的前置依賴（不在本 plan 範圍）

`MessagePackCodec` 是 `internal static`、`Options` 為 `private static readonly`，formatter 清單
硬編碼於框架內部。**外部開發者定義自己的 `MessagePackCollectionBase<>` 子類時，沒有任何公開管道
可註冊 formatter** ——他會拿到空集合且無法修正。

在補上公開註冊擴充點之前，BEE4001 的消費端版本只能「報告一個無解的問題」，那比不報更糟。因此：

- 階段 3 只做 **BEE4001 框架內版本**（防止維護者自己漏註冊，立即有價值）
- 消費端版本**待公開註冊點補齊後**再啟用；該擴充點是功能缺口而非慣例把關，**建議另開 plan**
- 其餘 BEE4002–BEE4009 不受此限，兩端皆可生效

## 技術約束與風險

| 項目 | 內容 | 對策 |
|------|------|------|
| **TFM 硬約束** | Roslyn analyzer 必須 target `netstandard2.0`，與 repo 全 `net10.0` 慣例衝突 | `src/Bee.Analyzers/` 單獨設 TFM，並在專案檔註明原因 |
| **Roslyn 版本對齊** | `Microsoft.CodeAnalysis.CSharp` 版本需 ≤ 消費端 SDK 內建版本，過高會載入失敗 | 選保守版本線，並在多 SDK 版本下實測 |
| **AdditionalFiles 注入** | 消費端需把 `Define/**/*.xml` 加為 `AdditionalFiles` | 由套件 `build/Bee.Definition.targets` 以 glob 自動注入，提供 property 可 opt-out |
| **一次編譯的視野** | 見 D5——跨 assembly 的內部狀態拿不到 | 規則按執行位置拆版本；跨型別判定用 `GetTypeByMetadataName` 按名稱解析 |
| **無法引用框架型別** | analyzer 為 `netstandard2.0`，不能引用 `Bee.Definition`（`net10.0`），故 `FieldDbType` 等合法值清單必須硬編碼於 analyzer 內，會與框架漂移 | 在 `tests/Bee.Analyzers.UnitTests`（`net10.0`，可同時引用兩者）加同步斷言測試：analyzer 內的清單必須等於實際列舉的所有值，列舉新增值時測試立刻紅 |
| **定義檔根目錄判定** | 消費端的定義檔目錄名不保證是 `Define/`（`PathOptions.DefinePath` 可設） | 不硬編目錄名，由 `AdditionalFiles` 中 `*.FormSchema.xml` 的實際路徑往上推導根目錄 |
| **定義存放於 DB** | `DbDefineStorage` 讓定義可存於 `st_define` 而非檔案系統，此時檔案根本不存在 | 偵測不到定義檔時**整組靜默**（不是降 severity）——DB 模式下任何跨檔規則都會全數誤判 |
| **編譯效能** | 每次編譯 parse 全部定義檔；IDE 中每次打字都會重跑 | incremental + 內容快取；設定義檔數量的效能基準測試；`EnableConcurrentExecution` |
| **誤判風險** | 跨檔規則在「定義檔存放於 DB 而非檔案系統」時會誤判 | 這類場景降為 `info`，偵測不到定義檔根目錄時整組靜默 |
| **消費端 TreatWarningsAsErrors** | warning 會變 error 阻斷建置 | 依 D3 保守分級；README 明示如何逐條調整 |

## 待議項目

### 已定案

| 項目 | 決定 |
|------|------|
| 首版 severity 策略 | 依**誤判可能性**分批上線（見 D3）。高確定性規則直接 `error`，跨檔與慣例類首版 `warning` 觀察誤判率後再升。不用 `info`（純建議類除外，且已知對 AI 無效） |
| 診斷訊息語言 | 一律英文；`docs/` 說明文件雙語（見 D6） |
| `Bee.Analyzers` 納入 `Bee.Library.slnx` | **納入**，避免重演 `tools/` / `samples/` / `apps/` 不在 slnx 內導致改動無把關的問題。動工第一步先實測 `netstandard2.0` 與全 `net10.0` 方案並存是否乾淨（`TreatWarningsAsErrors`、analyzer 套件版本） |

連帶需在階段 5 處理：analyzer 新增規則在 semver 下的定位，以及 CHANGELOG 如何交代新規則對既有專案的影響。

### 尚未評估

- BEE4009 是否值得做——RS0016 已覆蓋公開屬性改名，本規則只補訊息語意化與 `[Union]` 整數 Key
- 診斷 ID 前綴 `BEE` 的第三方撞名風險（風險低但未實證）

## 驗證方式

1. **單元測試**：`tests/Bee.Analyzers.UnitTests`，用 `Microsoft.CodeAnalysis.Testing` 對每條規則
   建立 positive / negative 案例（含「不該觸發」的案例，防誤判回歸）。
2. **端到端消費端驗證**：建一個**只引用 NuGet 套件**（非 ProjectReference）的臨時專案，
   確認 analyzer 自動生效——這是本 plan 成功的唯一硬指標，ProjectReference 會掩蓋打包路徑錯誤。
3. **AI 可讀性驗證**：對每條規則，實測 AI 只憑診斷訊息（不給任何文件）能否正確修正。
   訊息若無法讓 AI 自我修正，則改寫訊息而非補文件。
4. **序列化規則的真實性驗證**：BEE4xxx 每條規則都要有一個「故意違反 → 三棲 round-trip 測試確實
   失敗」的對照案例，證明該規則擋的是真實失敗而非想像的失敗。
5. 既有回歸：`./test.sh` 全綠，`build-ci.yml` strict build 階段不新增警告。

## 範圍界定（本 plan 不做）

- 不做 `AGENTS.md` 散佈、`dotnet new` template、MCP server、Claude Code plugin skill 包——各自獨立 plan
- 不做 MessagePack formatter 的**公開註冊擴充點**（BEE4001 消費端版本的前置，屬功能缺口，建議另開 plan）
- 不做 source generator（僅診斷，不生成程式碼）
- 不做 `CodeFixProvider`——成本高於 analyzer 且對 AI 幫助有限（AI 讀懂訊息即可自行修正）
- 不改任何既有公開 API 表面
- 階段 1 不追求規則數量，只求端到端管線走通並驗證 AI 可讀性

## 階段細節

### 階段 1：基礎設施 + 3 條規則走通端到端 ✅

**產出**

| 項目 | 位置 |
|------|------|
| analyzer 專案（`netstandard2.0`，已納入 slnx） | `src/Bee.Analyzers/` |
| 測試專案（28 測試全綠） | `tests/Bee.Analyzers.UnitTests/` |
| BEE1001 — XML 單檔管線 | `Definitions/FormSchemaCategoryIdAnalyzer.cs` |
| BEE2001 — XML 跨檔管線 | `Definitions/FormSchemaTableRegistrationAnalyzer.cs` |
| BEE4004 — C# 語意管線 | `Serialization/MessagePackConstructorOrderAnalyzer.cs` |
| 共用基礎設施（XML `Location` 計算、安全 XML 載入、定義檔命名、scope 清單） | `Definitions/XmlAttributeLocator.cs`、`DefinitionDocumentLoader.cs`、`DefinitionFileNames.cs`、`DbCategoryScopes.cs` |
| 打包與零設定接線 | `Bee.Definition.csproj` 的 `_BeePackAnalyzer` target、`src/Bee.Definition/build/Bee.Definition.targets` |
| 規則發佈追蹤（RS2008，analyzer 版的 PublicAPI baseline） | `src/Bee.Analyzers/AnalyzerReleases.{Shipped,Unshipped}.md` |

**端到端驗證結果**（只 `dotnet add package Bee.Definition`、零額外設定的臨時專案）

```
Define/FormSchema/Product.FormSchema.xml(2,52): error BEE1001: FormSchema 'Product' declares
  CategoryId 'business', which is not an accepted database scope. Accepted values are 'common',
  'company', 'log'. Business tables must use 'company'; 'common' is reserved for shared framework
  tables.

Define/FormSchema/Product.FormSchema.xml(4,36): warning BEE2001: FormSchema 'Product' declares
  CategoryId 'common', but its table 'ft_product' is not registered under that scope in
  DbCategorySettings.xml. It is registered under 'company'. Fix: either change the FormSchema
  CategoryId to 'company', or move the table registration to 'common' in DbCategorySettings.xml.
```

`-p:BeeAnalyzeDefinitionFiles=false` 可整組關閉（實測 0 警告）。

**實測結論：BEE4004 的範圍與原假設不同**

以 MessagePack 3.1.7 實測「ctor 參數順序 vs Key 順序」的實際後果：

| 型別形狀 | ctor 參數順序與 key 順序不符時 |
|---------|--------------------------|
| 整數 `[Key]`，**有**無參數 ctor | ❌ 靜默對調欄位 |
| 整數 `[Key]`，無無參數 ctor | ❌ 靜默對調欄位 |
| 整數 `[Key]`，參數名完全不對應成員 | ❌ 靜默對調欄位 |
| `keyAsPropertyName: true`（name-based） | ✅ 正確（依名稱比對） |
| 整數 `[Key]`，ctor 跟隨 **Key 數值**順序但不同於宣告順序 | ✅ 正確 |

修正三點原假設：
1. **無參數 ctor 不會被優先選用** —— 原以為它能兜住，實測不能；MessagePack 選參數最多的 ctor
2. **參數名正確也無效** —— MessagePack 按 key 順序**依位置**填入，不做名稱比對
3. **name-based 型別不受影響** —— 故規則範圍排除 `keyAsPropertyName`，實際只涵蓋 `[Union]` 多型家族
   （它們被迫使用整數 Key，見 BEE4003），判定基準為 **Key 數值順序**而非宣告順序

順帶檢查既有的 `FilterCondition`：ctor `(fieldName, @operator, value, secondValue)` 對齊 Key 100–103，
**無現存缺陷**；此規則的價值是防止未來改壞。

**其他實作發現**

- **定義檔規則必然是 compilation-end 診斷（RS1037）**：`AdditionalFiles` 只能從 compilation action 取得，
  故 descriptor 必須帶 `WellKnownDiagnosticTags.CompilationEnd`。副作用是 **IDE 即時分析預設不顯示**
  這些診斷（需開 full solution analysis）；build output 不受影響，故不影響本 plan 針對 AI 工作流的目標。
- **`EnforceExtendedAnalyzerRules` 會擋下數個常見寫法**：`messageFormat` 須為單句無句號或多句帶句號
  （RS1032）；每條規則須登記於 `AnalyzerReleases.Unshipped.md`（RS2008）。
- **測試 harness 自行實作，未採用 `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`**：該套件於測試期
  從 NuGet 解析參考組件（依賴網路），且其 markup 語法的價值集中在 code fix 測試（本 plan 不做）。
  自建 harness 對 `AdditionalFiles` 也有完整控制。
- **語意分析測試必須明確傳入 anchor 型別**：僅靠 `AppDomain` 已載入組件不可靠——組件載入是惰性的，
  單獨執行某個測試時 `MessagePack` 可能尚未載入，導致測試素材編譯出 error type、規則靜默不觸發，
  **徵狀與「規則有 bug」完全相同**。`AnalyzerRunner.GetCompilationDiagnostics` 即為區分兩者而加。

### 階段 2：XML 定義檔規則完整化

補齊 BEE1002–1007、BEE2002–2007。需先建立定義檔根目錄的解析與快取層（跨檔規則都依賴它）。

### 階段 3：序列化與 wire 合約規則

補齊 BEE4001（**僅框架內版本**）、BEE4002、BEE4003、BEE4005–BEE4008。BEE4009 視情況。

此階段對框架自身立即有價值：BEE4001 框架內版本防的是維護者漏註冊 formatter，
而該風險目前只靠 `MessagePackCodec.cs` 的一段註解在防守。

### 階段 4：C# 程式碼慣例

補齊 BEE3001–3003。BEE3003 僅能偵測明顯樣式，接受覆蓋不完整。

### 階段 5：發佈整合與對外文件

- 確認 `build-ci.yml` / `nuget-publish.yml` 對 analyzer 產出的處理正確（見 D2，內嵌不需新增 pack 行，
  但需確認 `--no-build` 流程下 analyzer dll 已就位）
- 實作整組關閉的 MSBuild property，並驗證 `.editorconfig` 逐條調整 severity 確實生效
- 定調 analyzer 新增規則在 semver 下的定位，並在 CHANGELOG（雙語）明確交代新規則對既有專案的影響
  與關閉方式——首版 `error` 級規則可能讓既有專案升版後建置失敗
- 消費端 README 段落（雙語）：規則總表、如何逐條調整 severity、如何整組關閉
- 於 `docs/` 新增雙語公開文件說明 analyzer 規則（外部開發者可讀）
- 評估是否新增 ADR 記錄 D1 / D2 / D3 / D5 四項決策
