# Analyzer 規則

[English](analyzer-rules.md) · [← 文件索引](README.zh-TW.md)

Bee.NET 隨套件提供 Roslyn analyzer，把框架慣例變成建置期診斷。它由 `Bee.Definition` 自動註冊：
引用該套件即生效，不需額外安裝，也不需修改專案設定。

這些規則存在的理由是：大多數框架慣例的違反都很晚才浮現。FormSchema 指向錯誤的資料庫 scope、
參與序列化的型別沒有無參數建構子、BO 方法沒有宣告存取控制——這些都不會讓建置失敗，全都只在執行期
才顯現，而且往往離肇因的定義很遠。診斷把發現的時機提前到建置期，並在訊息中同時指出原因與修法。

## 規則清單

### 定義檔——單檔驗證（BEE1xxx）

| ID | 嚴重度 | 規則 |
|----|--------|------|
| BEE1001 | Error | `FormSchema/@CategoryId` 必須是 `common`、`company` 或 `log` |
| BEE1002 | Error | `DbCategory/@Id` 同樣限於上述三個 scope |
| BEE1003 | Error | `FormField` 與 `DbField` 的 `@DbType` 必須是 `FieldDbType` 的成員 |
| BEE1004 | Error | `ListFields` / `LookupFields` 只能引用已宣告的欄位 |
| BEE1005 | Error | 關聯對應的 `DestinationField` 必須是已宣告欄位 |
| BEE1006 | Warning | 標記 `Type="RelationField"` 的欄位應有對應寫入 |
| BEE1007 | Error | 同一張表不得重複宣告同名欄位 |

### 定義檔——跨檔一致性（BEE2xxx）

| ID | 嚴重度 | 規則 |
|----|--------|------|
| BEE2001 | Warning | FormSchema 的表必須登記在 `DbCategorySettings.xml` 對應 scope 下 |
| BEE2002 | Warning | TableSchema 必須位於 `TableSchema/<categoryId>/<表名>.TableSchema.xml`——資料夾必須等於 CategoryId |
| BEE2003 | Error | `@RelationProgId` 必須是某個 FormSchema 宣告的 ProgId |
| BEE2004 | Error | 關聯對應的 `SourceField` 必須由被引用的 schema 宣告 |
| BEE2005 | Warning | FormSchema 應有對應的 FormLayout |
| BEE2006 | Warning | FormSchema 的持久化欄位必須存在於 TableSchema |
| BEE2007 | Info | 各語系應覆蓋相同的翻譯 key |

### 程式碼慣例（BEE3xxx）

| ID | 嚴重度 | 規則 |
|----|--------|------|
| BEE3001 | Warning | BO 的 public 方法必須被 `[ApiAccessControl]` 涵蓋 |
| BEE3002 | Warning | 定義層集合屬性必須使用框架集合型別——**框架內部規則**，不套用於消費端專案 |
| BEE3003 | Warning | `IExecFuncHandler` 實作的 public 方法必須被 `[ExecFuncAccessControl]` 涵蓋 |

### 序列化與 wire 合約（BEE4xxx）

| ID | 嚴重度 | 規則 |
|----|--------|------|
| BEE4005 | Warning | 框架集合應只公開一個 public `Add` |
| BEE4006 | Error | 參與序列化的型別必須有 public 無參數建構子 |

### Repository 建置閘門（BEE9xxx）

| ID | 嚴重度 | 規則 |
|----|--------|------|
| BEE9001 | Error | `Bee.Base` 與 `Bee.Definition` 只能參考允許清單列出的項目 |
| BEE9002 | Error | `Version`、`AssemblyVersion`、`FileVersion` 必須同步 |

**這兩條是框架內部規則，消費端專案不會觸發。** 它們不是 Roslyn analyzer，而是
`src/Directory.Build.targets` 內的 MSBuild target；列在此處是為了讓編號有一個統一的歸屬。
BEE9001 的存在理由是：加在最底層那兩個組件上的任何東西，都會被框架的每一個消費者繼承
（[ADR-038](adr/adr-038-definition-dependency-boundary.md)）。BEE9002 的存在理由是：
只 bump `Version` 的發版會送出「組件仍宣稱前一版」的套件，而已發布的套件無法回收。

上方表格中有兩處標為框架內部規則——BEE3002 只在框架自身的 `Bee.Definition` 組件內執行，
BEE9xxx 兩條只在本 repository 內執行。列出僅為完整性，消費端專案不會觸發它們。其餘規則都適用於
消費端專案，**BEE4005 也不例外**：你自己繼承 `CollectionBase` / `KeyCollectionBase` 寫出來的集合，
與框架自身的集合受同一條規則檢查。

## 定義檔規則從哪裡讀取

BEE1xxx 與 BEE2xxx 分析的是 XML 而非 C#，這需要 MSBuild 明確地把檔案交給編譯器。套件已代為處理：
`build/Bee.Definition.targets` 會把 `Define\**\*.xml` 加入 `AdditionalFiles`，以專案目錄為根並排除
建置輸出。

若你的定義檔不在 `Define` 底下，改指向其他位置：

```xml
<PropertyGroup>
  <BeeDefinitionFilesGlob>MyDefinitions\**\*.xml</BeeDefinitionFilesGlob>
</PropertyGroup>
```

定義存放於資料庫而非檔案系統時不需任何設定——沒有定義檔可讀時，跨檔規則會靜默，而不會把每張表
都報成缺漏。

## 調整嚴重度

**IMPORTANT：可用的機制取決於診斷報在哪裡。**

| 規則 | 診斷位置 | 使用 |
|------|---------|------|
| BEE1xxx、BEE2xxx | 你的 XML 定義檔 | `.globalconfig` |
| BEE3xxx、BEE4xxx | 你的 C# 原始碼 | `.editorconfig` 或 `.globalconfig` |

`.editorconfig` 是透過「診斷位置所屬的檔案」來解析嚴重度設定。定義檔規則報在以 `AdditionalFiles`
提供的 XML 上，而那不屬於編譯的語法樹，因此**沒有任何 `.editorconfig` section 對它們生效**——
`[*.cs]` 不行，`[*.xml]` 也不行。`.globalconfig` 是編譯層級的設定，因此有效。

在專案檔旁建立 `.globalconfig`：

```ini
is_global = true

# 降級某條規則
dotnet_diagnostic.BEE2001.severity = suggestion

# 完全關閉某條規則
dotnet_diagnostic.BEE2005.severity = none
```

C# 規則則兩種檔案都可以：

```ini
# .editorconfig
[*.cs]
dotnet_diagnostic.BEE4006.severity = warning
```

可用值為 `error`、`warning`、`suggestion`、`silent`、`none`。注意 `dotnet build` 在預設 verbosity
下不會輸出 `suggestion` 與 `silent` 級診斷——它們只在 IDE 顯示。把規則降到那個層級，等於讓它從
建置輸出中消失。

## 整組關閉定義檔規則

```xml
<PropertyGroup>
  <BeeAnalyzeDefinitionFiles>false</BeeAnalyzeDefinitionFiles>
</PropertyGroup>
```

這會讓定義檔完全不被交給編譯器，一次靜默所有 BEE1xxx 與 BEE2xxx 規則。C# 規則不受影響，需依上節
逐條關閉。

## 版本政策

analyzer 規則屬於套件的可觀察行為，新規則可能讓原本成功的建置失敗——在開啟
`TreatWarningsAsErrors` 的專案上尤其明顯。框架因此採以下政策：

- **新規則只在 minor 版本加入，絕不在 patch 版本加入。** 升級 patch 版不會引入你沒見過的診斷。
- **提高既有規則的嚴重度屬於 minor 版本變更**，並會在該版本的變更說明中明確交代。
- **每條規則都可個別調整，定義檔規則可整組關閉**，因此升級絕不會讓你沒有前進的辦法。

嚴重度的分派依據是「規則判錯的機率」而非「違反後果多嚴重」。必然為錯的規則給 error；存在合理例外
的給 warning，待實際誤判率經觀察確認後，才在後續 minor 版本升為 error。
