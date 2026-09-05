# 租戶客製化

[English](customization.md) · [← 文件索引](README.zh-TW.md)

> 同一套部署，讓某一家公司得到不同的行為——不同的欄位標題、不同的版面、存檔前多一道檢查
> ——而不必分岔套裝的定義檔或程式碼。

## 一段話講完這個模型

一套部署有一組套裝定義，放在 `DefinePath`。需要不一樣的租戶在 `CustomizePath` 下擁有自己的
資料夾，裡面**只放差異的部分**。執行時框架同時讀兩層，逐次查找決定誰勝出。沒有資料夾的租戶
就解析到套裝層，與這個功能不存在時完全相同——那是預設值，成本為零。

租戶絕不由呼叫端指定。它來自 session：session 進入公司時，`st_company.customize_id` 會被複製到
`SessionInfo.CustomizeId`，伺服端所有消費端一律只從那裡讀。

## 該用哪一種

| 你想改的 | 用 | 檔案位置 |
|---|---|---|
| 欄位標題、表單名稱、訊息、選項文字 | **語系資源** | `{CustomizePath}/{customizeId}/Language/{lang}/{namespace}.Language.xml` |
| 哪些欄位出現、畫面怎麼排 | **FormLayout** | `{CustomizePath}/{customizeId}/FormLayout/{layoutId}.FormLayout.xml` |
| 選單怎麼分組、排序、標題，哪些項目看得到 | **MenuSettings** | `{CustomizePath}/{customizeId}/MenuSettings.xml` |
| 一個程式的整體行為——驗證、流程、AnyCode SQL | **客製 BO** | `{CustomizePath}/{customizeId}/ProgramSettings.xml` |
| 一個程式如何讀寫自己的資料 | **客製 Repository** | `{CustomizePath}/{customizeId}/ProgramSettings.xml` |
| 在既有存檔／刪除流程上多一個步驟 | **業務 plugin** | `{CustomizePath}/{customizeId}/PluginSettings.xml` |

前三種**只改定義檔**——不寫程式、不部署組件。後三種指名型別，因此需要組件放進主機的 `bin`。

**能用輕的就別用重的。** 改個標題是一筆語系資料，不是一個客製 BO；多一道驗證是一個 plugin，
不是一個子類。用重的工具當然也能達成，但那是一個你得跟著每次框架升級維護的類別。

## 怎麼啟用

把 `CustomizePath` 指到 `DefinePath` 旁邊的目錄：

```csharp
var paths = new PathOptions
{
    DefinePath = Path.Combine(deployRoot, "Define"),
    CustomizePath = Path.Combine(deployRoot, "Customize"),
};
```

**`CustomizePath` 留空等於完全關閉覆蓋層**，那是預設值。

接著給公司一個代碼——`st_company.customize_id` 的值。它會變成資料夾名稱，所以必須是合法的目錄
名；含 `..` 或路徑分隔字元的代碼會被框架拒絕。多家公司共用同一份客製時可以共用同一個代碼。

目錄不需要預先建立。租戶對某次查找沒有提供檔案時，就落回套裝層。

## 語系：標題、名稱與選項文字

最常見的客製，而且只改定義檔。疊加是 **per key**：租戶的檔案只放要改的 key，其餘全部——包含
套裝日後才新增的翻譯——都來自套裝層。

namespace 就是表單的 `ProgId`。三種 sub-key 涵蓋一張表單：

| Sub-key | 覆寫的對象 |
|---|---|
| `Schema.DisplayName` | 表單自己的名稱 |
| `Table.{TableName}.DisplayName` | 表單內某個表格的名稱 |
| `Field.{FieldName}.Caption` | 某個欄位的標題 |

要讓租戶 `acme` 把客戶欄位叫作「帳戶」：

```xml
<!-- Customize/acme/Language/zh-TW/Order.Language.xml -->
<LanguageResource Lang="zh-TW" Namespace="Order">
  <Items>
    <LanguageItem Key="Field.customer_id.Caption" Value="帳戶" />
  </Items>
</LanguageResource>
```

整個檔案就這樣。該表單其餘所有標題仍然來自套裝資源。

**選項集是例外：enum 整組取代。** 租戶檔案裡同名的 `LanguageEnum` 會蓋掉套裝那一組，因此必須
列出該選項集應有的**全部** entry——逐 entry 合併會讓順序、以及「沒列到的 entry 是什麼意思」
兩件事都變得曖昧。欄位可以用完整名稱指向別的 namespace 的 enum（`Common.Gender`），這時租戶的
覆寫要放在那個 namespace 的檔案裡。

## FormLayout：什麼出現、排在哪

疊加是**整檔取代**。客製了某個 layout 的租戶就完整擁有它：把套裝的
`{layoutId}.FormLayout.xml` 複製到租戶資料夾再改。`layoutId` 未指定時等同 `ProgId`。

**完整擁有是雙向的**：日後套裝 `FormSchema` 新增的欄位**不會**出現在該租戶的表單上，框架既不
合併也不警告。這是設計意圖而非限制——layout 才是「畫面上有什麼」的權威來源，schema 多了一個
欄位並不等於每個租戶從此都該顯示它。要讓它出現在那個租戶的表單上是一個決定，執行方式就是去改
那份客製 layout 檔。

**標題不在 layout 檔裡**：UI head 選定 layout 之後，會從已在地化的 schema 套用標題，所以即使
客製了 layout，改標題仍然屬於語系資源的事。

> **它怎麼到畫面上。** API 一律供應原始定義，組裝發生在用戶端的 `FormDefinitionLoader`：它取回
> 兩層、有租戶 layout 就用租戶的、兩層都沒有就從 schema 生一份。**不經過 `FormDefinitionLoader`
> 的 UI head 看不到 layout 客製。**

## BO 與 Repository：換掉一個程式的行為

在 `ProgramSettings.xml` 為租戶綁不同的型別。疊加是 **progId 級，其下再分屬性級**——只寫你要
改的那個綁定：

```xml
<!-- Customize/acme/ProgramSettings.xml -->
<ProgramSettings>
  <Items>
    <ProgramItem ProgId="Order" BusinessObject="Acme.Erp.OrderBo, Acme.Erp" />
  </Items>
</ProgramSettings>
```

套裝那筆的 `Repository` 與 `DisplayName` 仍然生效。若要**刻意**讓某個綁定退回框架自己的型別，
請顯式指名該型別，而不是把屬性清空。

寫子類本身是一般開發工作——見開發指引的
[客製化 ProgId 對應的 BO](development-cookbook.zh-TW.md)，以及
[BO 擴充點與交易邊界](development-cookbook.zh-TW.md)（該覆寫哪一段、哪一段在資料庫交易中）。

## 業務 plugin：加一個步驟

當客製屬於**追加**而非取代時——存檔前多一道檢查、存檔後發個通知——綁 plugin 而不是換掉 BO：

```xml
<!-- Customize/acme/PluginSettings.xml -->
<PluginSettings>
  <Items>
    <ProgramPluginItem ProgId="Order">
      <Plugins>
        <PluginItem Type="Acme.Erp.CreditLimitPlugin, Acme.Erp" Stage="BeforeSave" />
        <PluginItem Type="Acme.Erp.OrderSyncPlugin, Acme.Erp"   Stage="AfterSave" />
      </Plugins>
    </ProgramPluginItem>
  </Items>
</PluginSettings>
```

**一筆繫結宣告一個時點，而類別必須恰好覆寫那一個時點。** 因此「存檔前檢查」與「存檔後通知」
是兩個類別，不是一個類別覆寫兩個方法——這也正是設定檔看得出「誰跑在哪個時點」的原因。
建鏈時會把宣告與類別對帳，所以**改變類別覆寫的時點時必須連帶改這份檔案**；
不一致會直接拒絕載入，而不是跑一個設定檔沒寫的時點。

plugin 是唯一「兩層**相加**」的產物：套裝鏈先跑、租戶鏈後跑。因此租戶**無法停用**套裝的
plugin——要拿掉套裝行為，請繼承 BO 覆寫該子方法。

四個時點、每次操作的生命週期、以及「送往其他系統的副作用該寫在哪」，見開發指引的
[業務 plugin](development-cookbook.zh-TW.md)。

## 除了 plugin 之外都是唯讀

客製檔由部署工具產生、執行期讀取；覆蓋層上其餘所有寫入一律拋例外。**`PluginSettings` 是例外**
——部署透過 `SystemBO.GetCustomizePluginSettings` / `SaveCustomizePluginSettings` 維護自己的
plugin 綁定。

兩者皆為 `LocalOnly`：這些綁定決定「哪些程式碼會在存檔與刪除流程裡執行」，因此只有 in-process
可達，由跑在主機上的維護工具呼叫。儲存時會逐一驗證每個綁定型別——必須可載入、繼承
`FormBusinessPlugin`、且至少 override 一個時點——一筆不合格就整份拒存。

檔案式儲存下，寫入落在服務該次呼叫的那台機器，因此多節點部署需要把 `CustomizePath` 放在共享
儲存上，或改用資料庫式儲存（後者天生共享）。

## 不能客製的東西

**`FormSchema` 與 `TableSchema` 永久排除。** 兩者同時驅動資料庫結構與驗證規則，不只驅動 UI；
逐租戶分歧會讓實體 schema 裂開。這是裁決不是缺口——見
[ADR-016](adr/adr-016-multitenant-customization-overlay.md)。

值得先納入規劃的推論：**租戶不能多一個欄位**。它能有的是既有欄位的不同標題、一張藏起該欄位的
表單、一個對該欄位處理方式不同的 BO，或一個負責填它的 plugin。若某租戶真的需要自己的資料，
那就是對所有人的 schema 變更，用不到的地方讓那個欄位空著。

**schema 規則同樣不可客製**，因為它們住在 `FormSchema` 裡。宣告式的預設值、計算欄與驗證因此
適用於每個租戶；逐租戶的邏輯請走 plugin 或客製 BO。

## 接著看哪裡

| 想了解 | 讀 |
|---|---|
| 覆蓋層機制：完整路徑、各型別的疊加粒度、`customizeId` 怎麼解析 | [定義檔總覽](definition-files-overview.zh-TW.md) 第 7 節 |
| 怎麼寫客製 BO、Repository 或 plugin | [端到端開發指引](development-cookbook.zh-TW.md) |
| 為什麼這樣設計 | [ADR-016](adr/adr-016-multitenant-customization-overlay.md) |
| 維護 API 的存取控制 | [API 方法總覽](api-method-reference.zh-TW.md) |
