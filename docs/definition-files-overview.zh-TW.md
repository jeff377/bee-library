# 定義檔全景

[English](definition-files-overview.md) · [← 文件索引](README.zh-TW.md)

> 所有定義檔的全景圖：各自管什麼、彼此怎麼串、改了哪個會影響哪一層。本頁是導引層 —— 每一項都連向深入說明它的文件。

Bee.NET 是定義驅動的：`DefinePath` 下的 XML 不是外掛在應用上的組態，它**本身就是**應用的結構。框架讀它來建 SQL、繪 UI、執行權限與在地化文字。

---

## 1. 全部定義類型

共 11 種定義類型，以 `DefineType` 列舉，全部透過 `IDefineAccess` 取得。其中 7 種是 `DefinePath` 根目錄下的單一檔案，4 種帶 key、放在子資料夾。

| 定義 | `DefinePath` 下的路徑 | 管什麼 | 深入閱讀 |
|------|---------------------|--------|---------|
| **FormSchema** | `FormSchema/{progId}.FormSchema.xml` | 定義中樞：欄位、型別、關聯、主從結構、計算欄與規則 | [架構總覽](architecture-overview.zh-TW.md) |
| **TableSchema** | `TableSchema/{categoryId}/{tableName}.TableSchema.xml` | 實體資料表：欄位、型別、長度、可空性、索引 | [Schema 升級](database-schema-upgrade.zh-TW.md) |
| **FormLayout** | `FormLayout/{layoutId}.FormLayout.xml` | 表單在畫面上如何排版 | [架構總覽](architecture-overview.zh-TW.md) |
| **Language** | `Language/{lang}/{namespace}.Language.xml` | 在地化標題與列舉項目，每個 namespace × 語言一檔 | — |
| **SystemSettings** | `SystemSettings.xml` | 行程層級設定：主金鑰來源、payload 選項、debug 模式 | [端到端開發指引](development-cookbook.zh-TW.md) |
| **DatabaseSettings** | `DatabaseSettings.xml` | 實體資料庫與其連線字串 | [資料庫設定指引](database-settings-guide.zh-TW.md) |
| **DbCategorySettings** | `DbCategorySettings.xml` | 各資料表屬於哪個邏輯分類、該分類由哪個資料庫承載 | [資料庫設定指引](database-settings-guide.zh-TW.md) |
| **ProgramSettings** | `ProgramSettings.xml` | 程式清單：progId → 自訂商業物件綁定，以及導覽選單 | — |
| **PermissionModels** | `PermissionModels.xml` | 權限模型 registry：模型、動作與 record scope 策略 | [權限與授權](permission-authorization.zh-TW.md) |
| **CurrencySettings** | `CurrencySettings.xml` | 幣別主檔：各幣別小數位與自然最小單位 | [端到端開發指引](development-cookbook.zh-TW.md) |
| **UnitSettings** | `UnitSettings.xml` | 計量單位主檔：各單位顯示小數位 | [端到端開發指引](development-cookbook.zh-TW.md) |

## 2. FormSchema 是中樞

一份 `FormSchema` 同時驅動三個層。這是框架中最重要的一組關係：

```text
                    ┌──────────────────┐
                    │   FormSchema     │  欄位 · 型別 · 關聯
                    │   {progId}       │  主從結構 · 規則
                    └────────┬─────────┘
             ┌───────────────┼───────────────┐
             ▼               ▼               ▼
      ┌────────────┐  ┌────────────┐  ┌──────────────┐
      │ FormLayout │  │ TableSchema│  │ 規則 /       │
      │  （UI）    │  │（資料庫）  │  │ 運算式       │
      └────────────┘  └────────────┘  └──────────────┘
        長什麼樣        存在哪裡         什麼才合法
```

- **對資料庫**：框架在執行期依 FormSchema 產生 SQL —— 沒有 ORM、沒有產生的 entity 類別。見 [FormMap](formmap.zh-TW.md)。
- **對 UI**：`FormLayout` 排列 FormSchema 宣告的欄位；控件直接讀欄位的 metadata（最大長度、清單項目、唯讀、關聯 → lookup）。
- **對驗證**：計算欄與 `FormRule` 就寫在 FormSchema 內。見 [運算式與規則](expression-rules.zh-TW.md)。

實務結果是：**一般 CRUD 不需要任何程式碼**。一份 FormSchema、對應的 TableSchema、一筆 `DbCategorySettings` 登錄與一個 `ProgramSettings` 項目，就是一張能用的表單。

## 3. 啟動三件組

三個設定檔在 host 啟動時依固定順序讀入，且後者相依於前者：

```text
SystemSettings.xml          ──▶ SysInfo.Initialize + ApiServiceOptions.Initialize
   （主金鑰、payload）            （行程層級狀態）
        │
        ▼
DatabaseSettings.xml        ──▶ 實體資料庫 + 連線字串
   （以 id 被參照）               （用主金鑰解密）
        │
        ▼
DbCategorySettings.xml      ──▶ 資料表 → 分類 → 資料庫的解析
   （common / company / log）
```

`SystemSettings` 必須最先載入，因為它指名的主金鑰正是用來解密 `DatabaseSettings` 內連線字串的東西。完整順序見[端到端開發指引 § 框架初始化順序](development-cookbook.zh-TW.md)；違反順序會壞在哪裡見[開發限制與反模式](development-constraints.zh-TW.md)。

### CategoryId 是 scope 選擇器，不是自由字串

`CategoryId` 只認三個值，選錯是最常見的設定錯誤：

| 分類 | 意義 |
|------|------|
| `common` | 跨公司共享的框架表（session、快取通知、使用者） |
| `company` | 各公司獨立資料 —— **所有業務表都屬於這裡**，應用的組織表亦然 |
| `log` | 日誌與稽核表 |

表前綴（`st_` / `ft_`）表示這張表**歸誰所有**，分類表示**資料落在哪裡**。兩者是**正交**的軸。見[資料庫設定指引](database-settings-guide.zh-TW.md)與[框架保留命名](framework-reserved-names.zh-TW.md)。

## 4. ProgramSettings 身兼二職

`ProgramSettings.xml` 既是路由表也是選單來源：

```xml
<ProgramCategory Id="transactions" DisplayName="交易">
  <Items>
    <ProgramItem ProgId="Customer" DisplayName="客戶" />
    <ProgramItem ProgId="Order" DisplayName="訂單"
                 BusinessObject="MyApp.Server.BusinessObjects.OrderBO, MyApp.Server" />
  </Items>
</ProgramCategory>
```

- **`BusinessObject` 留空** → 該 progId 解析到框架預設的 `FormBusinessObject`，即純定義驅動的 CRUD。
- **`BusinessObject` 有值** → 由該型別承接此 progId，用於宣告式表達不了的情況（跨列聚合、資料庫查詢）。
- **Categories 與 Items** 同時也是 shell 建構導覽選單的來源。

因此在運行中的應用加一張表單，是四處 XML 修改、零程式碼。

## 5. 改了 X 要同步改什麼

| 你改了 | 還要一併更新 |
|--------|------------|
| 在 **FormSchema** 加欄位 | 對應 **TableSchema** 的欄位，然後執行 [schema 升級](database-schema-upgrade.zh-TW.md)；要顯示就加進 **FormLayout**；標題加進 **Language** |
| 新增**一張表單** | **FormSchema** + **TableSchema** + **DbCategorySettings** 的資料表登錄 + **ProgramSettings** 的一個 `ProgramItem` |
| 新增**一張資料表** | 它的 **TableSchema** 必須放在與 `DbCategorySettings` 分類相符的 `TableSchema/{categoryId}/` 資料夾 —— 資料夾名**就是**分類 |
| 新增**一個資料庫** | 先加 **DatabaseSettings** 項目，再於 **DbCategorySettings** 把分類指過去 |
| 改**幣別或單位精度** | **CurrencySettings** / **UnitSettings**；欄位層級的捨入依 `NumberKind`，不是原始欄位型別 |
| 新增**受權限控管的動作** | **PermissionModels**，接著是相關的 `FormField.ScopeRole` —— 見[權限與授權](permission-authorization.zh-TW.md) |

## 6. `DefinePath` 與 `Defaults/` scaffold

兩件很容易混淆的事：

- **`DefinePath`** 是執行期實際讀取的位置，也是執行期定義的**唯一**來源。
- **`Defaults/`** 內嵌於 `Bee.Definition.dll`，是**開新專案的 scaffold 來源**。`dotnet bee defines materialize` 會把它複製進你的 `DefinePath`，一次性動作。

> **不存在 fallback。** 若某份定義在 `DefinePath` 缺漏，框架**不會**回退去讀 `Defaults/`。要在專案中使用某個框架系統表，把它的定義展開進你的 `DefinePath` 再往上擴充 —— 並保留框架的標準欄位，權限與組織功能依賴它們。

### 定義資料在 init 後不可異動

透過 `IDefineAccess.GetX(...)` 取得的一切都是**行程層級的快取共用實例**，每個 session 拿到同一個 reference。在 runtime 上直接 mutate 會跨 session 洩漏。要改請先 clone；要持久化請走 `IDefineAccess.SaveX(...)`，它會寫入 storage 並使該快取失效。

完整規則見[開發限制與反模式 § 定義資料 init 後不可異動](development-constraints.zh-TW.md)。

### 儲存體可抽換

上述檔案佈局是預設實作（`FileDefineStorage`）。定義也可存放於資料庫 —— 見 [ADR-018](adr/adr-018-db-define-storage.md)。兩種情況下 `IDefineAccess` 都是同一套介面，變的只是背後的儲存體。

## 7. `CustomizePath` 與租戶客製覆蓋層

`DefinePath` 放的是所有租戶共用的 base 定義。`CustomizePath` 是可選的第二個根目錄，讓單一公司在**不分叉 base** 的前提下覆蓋其中一部分 —— 設計背景見 [ADR-016](adr/adr-016-multitenant-customization-overlay.md)。

### 怎麼打開

由 host 自行算出路徑，與 `DefinePath` 一起傳給 `AddBeeFramework`。框架**沒有組態綁定機制**，`PathOptions` 一向由 host 建構，`CustomizePath` 走的是同一條路：

```csharp
var paths = new PathOptions
{
    DefinePath = definePath,
    CustomizePath = Path.Combine(deployRoot, "Customize"),
};
builder.Services.AddBeeFramework(settings.BackendConfiguration, paths);
```

**`CustomizePath` 留空即整層關閉** —— 所有消費端一律走 base，行為與這個功能不存在時逐位元相同。這是預設值。接線示範見 `samples/Bee.Samples.Shared/DemoBackend.cs`。

### 檔案佈局

```
{CustomizePath}/{customizeId}/ProgramSettings.xml
{CustomizePath}/{customizeId}/FormLayout/{layoutId}.FormLayout.xml
{CustomizePath}/{customizeId}/Language/{lang}/{namespace}.Language.xml
```

目錄不必存在。某次查找若該租戶沒有對應檔案，就回退 base 層。

### 只有三種型別，三種粒度

| 型別 | 覆蓋粒度 |
|------|---------|
| **LanguageResource** | 文字（`LanguageItem`）是 **key 級**。客製檔只放要改的 key，其餘全部來自 base —— 因此 base 日後新增的翻譯會自動傳播。**`LanguageEnum` 是例外：整組取代。** 客製檔有同名 enum 就整組換掉 base 的，因此客製檔必須列出該選項集要有的**全部** entry |
| **ProgramSettings** | **progId 級**。同一個 progId 的客製項目勝過 base 項目 |
| **FormLayout** | **整檔級**。客製 layout 整份取代該 `layoutId` 的 base layout |

**粒度不同是刻意的**，分界線在於：這份東西是**一袋彼此獨立的值**，還是**一個組合起來才成立的整體**。

文字 key 彼此獨立——「這個標題我們叫法不同」不影響其餘任何一個 key，所以逐 key 疊加既省成本又直覺。但 layout 是**一整個版面**：區塊、排列順序、欄寬與巢狀只有整體看才有意義，局部疊加會冒出無從直覺回答的問題（「這個區塊搬走了，底下的欄位跟著走嗎？」）。**enum 屬於後者而非前者**：它是一組**有順序的選項集**，逐 entry 合併會讓順序、以及「客製檔沒列到的 entry 是什麼意思」兩件事都變得曖昧。

所以 layout 與 enum 一樣：客製了就完整擁有那一份，沒客製的租戶則原封不動拿到 base 版本。

**完整擁有**是雙向的：日後 base `FormSchema` 新增的欄位，**不會**出現在已客製該 layout 的租戶畫面上，框架既不合併也不對這個差異提出警告。這是設計意圖而非限制 —— **layout 才是「畫面上有什麼」的權威來源**，schema 多了一個欄位並不等於每個租戶的表單從此都該顯示它。要讓新欄位出現在那個租戶的表單上是一個決定，而這個決定的執行方式就是去改那份客製 layout 檔。

> **FormSchema 與 TableSchema 永久排除。** 兩者同時驅動資料庫結構與驗證規則，不只驅動 UI；逐租戶分歧會讓實體 schema 裂開。這是裁決不是缺口 —— 見 ADR-016。

> 客製層**唯讀**。客製檔由部署工具產生，覆蓋層上所有 `SaveXxx` 一律拋例外。

### `customizeId` 從哪來

`CompanyInfo.CustomizeId`（欄位 `st_company.customize_id`）在 session 進入公司時被複製到 `SessionInfo.CustomizeId`，離開公司 / 登出時清除。伺服端消費者一律只從 `SessionInfo` 讀，不從別處讀。

兩個必須納入規劃的推論：

- **`EnterCompany` 之前沒有任何客製。** 登入畫面、公司選單、以及到那之前的所有訊息都走 base，因為此時還沒有 `CustomizeId`。
- **`SessionInfo.CustomizeId` 是快照不是即時值。** 它在進公司當下複製，與角色、employee context 的策略一致。事後改 `st_company.customize_id` 不會影響既有 session，要下次 `EnterCompany` 才會拿到新值。

> **安全界線：** 伺服端**永不**採信 client 傳來的 `customizeId` 作為查找依據 —— 那等於讓呼叫端自選要讀哪一家租戶的客製檔。client 從 `EnterCompany` 拿到的那份 `CustomizeId` 只供 client 自己的 UI 在地化使用；伺服端一律讀 `SessionInfo.CustomizeId`。

---

## 接下來讀什麼

| 你想 | 讀 |
|------|-----|
| 看這些拼圖如何組成架構 | [架構總覽](architecture-overview.zh-TW.md) |
| 走完整條「定義 → API」流程 | [端到端開發指引](development-cookbook.zh-TW.md) |
| 理解 FormSchema 如何產生 SQL | [FormMap](formmap.zh-TW.md) |
| 以宣告方式做欄位運算與驗證 | [運算式與規則](expression-rules.zh-TW.md) |
| 知道哪些命名歸框架所有 | [框架保留命名](framework-reserved-names.zh-TW.md) |
| 設定資料庫與分類 | [資料庫設定指引](database-settings-guide.zh-TW.md) |
