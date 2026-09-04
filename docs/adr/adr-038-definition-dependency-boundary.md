# ADR-038：定義層相依邊界——運算式抽象下沉至 `Bee.Base`，判準以閘門固化

## 狀態

**已採納（Accepted，2026-08-11）** —— 決策已執行。

本 ADR 延續 [ADR-036](adr-036-wire-serialization-externalized.md) 的判準並補上其界線定義，
同時修訂 [ADR-028](adr-028-expression-rule-engine.md) 中「抽象與實作同住 `Bee.Expressions`」
的組件配置（求值語意與 client/server 共用單一實作的結論不變）。

## 背景

ADR-036 把 MessagePack 趕出定義層時立下的判準是：

> 判準不是「是不是傳輸格式」，而是**「會不會讓定義層長出外部套件相依」**。

該判準當時是以人眼 grep「傳輸格式」相關關鍵字落實的，因此漏掉了另一條相依鏈：

```
Bee.Definition ──ProjectReference──> Bee.Expressions ──PackageReference──> DynamicExpresso.Core
```

`Bee.Definition.4.19.0.nuspec` 因而把 `Bee.Expressions` 列為相依，
**任何安裝 `Bee.Definition` 的消費者都會被拉進一個運算式引擎**——包含只想讀定義的
純 UI head 與定義檔工具。掃過 `src/` 全部 17 個專案後，這是唯一一處違規。

成因不是架構放錯位置，而是**抽象與實作同住一個組件**：`Bee.Expressions` 的四個公開型別中，
只有 `DynamicExpressoEvaluator` 真正碰到 DynamicExpresso，其餘三個的公開表面只有 BCL 型別
與 `Bee.Base.Data.FieldDbType`。

## 決策

### 一、抽象下沉至 `Bee.Base`，實作留在 `Bee.Expressions`

| 型別 | 新位置 | 理由 |
|------|--------|------|
| `IExpressionEvaluator` | `Bee.Base.Expressions` | 抽象，公開表面零外部型別 |
| `ExpressionEvaluationException` | `Bee.Base.Expressions` | 介面契約的一部分（介面以 `<exception cref>` 標註） |
| `ExpressionPolicy` | `Bee.Base.Expressions` | 純政策，只依賴 `Bee.Base.Data.FieldDbType` |
| `DynamicExpressoEvaluator` | 維持 `Bee.Expressions` | **唯一**碰到 DynamicExpresso 的型別 |

`ExpressionEvaluationException` 放 `Bee.Base.Expressions` 而非 `Bee.Base.Exceptions`：
它與介面的內聚性高於「所有例外集中」，而 `Bee.Base.Exceptions` 現有成員
（`ForbiddenException` / `UserMessageException`）都是跨領域通用例外，性質不同。

相依鏈隨之改變：

| 專案 | 對 `Bee.Expressions` 的引用 | 理由 |
|------|---------------------------|------|
| `Bee.Definition` | ❌ 移除 | 只用抽象（`FormExpressionCalculator` 由建構子注入 evaluator） |
| `Bee.Business` | ❌ 移除 | 同上（`FormRuleProcessor`） |
| `Bee.Hosting` | ✅ 保留 | 組裝層，DI 註冊時要指定具體 evaluator |
| `Bee.UI.Avalonia` | ✅ 保留 | client 端自建 evaluator 做即時預覽 |

**`FormExpressionCalculator` 不搬離定義層**：`FormSchema` 宣告 `ValueExpression` 與驗證規則，
求值是定義語意的一部分；且 server 存檔前與 client 即時預覽共用同一份實作，正是
ADR-028「client 算出的值等於 server 寫入的值」的保證來源。搬走會斷掉這條線。

### 二、「外部套件」的界線

ADR-036 的判準只說「外部套件相依」，未定義界線。本 ADR 補上：

| 類別 | 例 | 定義層可否相依 |
|------|-----|--------------|
| BCL / 平台詞彙 | `System.Xml.Serialization`、`[JsonIgnore]` | ✅ 可 |
| **Microsoft 第一方、純抽象、隨 .NET 版本走** | `Microsoft.Extensions.Localization.Abstractions` | ✅ 可 |
| 第三方套件 | `DynamicExpresso.Core`、`MessagePack` | ❌ 不可 |
| 第一方**實作**套件 | 帶具體實作、獨立版本節奏者 | ❌ 不可（比照第三方） |

判別法：**這個相依有沒有替消費者做出技術選擇？** 純抽象套件沒有——它不帶實作、
不鎖定引擎，且版本隨 .NET 走而非隨供應商走。`Microsoft.Extensions.Localization.Abstractions`
（`Language/BeeStringLocalizer.cs` 使用）因此留在定義層。

### 三、判準改由閘門執行

判準只寫在 ADR 裡就會像這次一樣被漏掉，故以兩道互補的閘門執行：

| 閘門 | 位置 | 涵蓋範圍 |
|------|------|---------|
| **建置期鎖** | `src/Directory.Build.targets`（診斷碼 `BEE9001`） | 受鎖組件**直接**宣告的 `PackageReference` / `ProjectReference`（清單見該檔的 `BeeEnforceDependencyBoundary` 條件，此處不複寫） |
| **傳遞閉包測試** | `tests/Bee.Definition.UnitTests/DefinitionDependencyGateTests.cs` | `Bee.Definition` 的**整個傳遞相依閉包** |

兩道都需要，因為各自看不到對方那一半：建置期鎖看不到「經由某個 `ProjectReference`
間接帶進來的套件」——DynamicExpresso 正是這樣進來的；閉包測試則要跑測試才會知道，
而寫 csproj 的那一刻不會。

建置期鎖只檢查**會流到消費者**的參考：`PrivateAssets="all"` 的套件（SourceLink、analyzer）
與 `ReferenceOutputAssembly="false"` 的專案參考（`Bee.Analyzers` 的建置排序）不算，
因為它們不會出現在 nuspec。

閉包測試讀的是測試組件自己的 `.deps.json`，從 `Bee.Definition` 這個節點做 BFS。
選 `deps.json` 而非 `Assembly.GetReferencedAssemblies()`，是因為後者只反映「實際被 IL 引用」
的組件——宣告了卻尚未使用的套件相依會漏掉，而那正是本閘門要攔的東西。

**要放行一個新相依，三處都要改**（`BeeAllowedDependency`、測試白名單、本 ADR 的理由），
**逼出一次決策而非默默通過**。

## 理由

### 為何不用「新增 `Bee.Expressions.Abstractions` 套件」

抽象／實作分包是更「正統」的做法，但要為三個型別多發一個 NuGet 套件（框架已有 17 個），
消費者也多一個要認的名字。`Bee.Base` 本來就是全框架都依賴的那層，抽象放在那裡
**不多花任何人一分成本**，也不新增任何套件。分包只在日後 `Bee.Base` 也想瘦身時才更划算。

### 為何不讓定義層自宣告一個極小介面

那樣不必動 `Bee.Expressions`，但會出現兩個平行介面加一個轉接器，長期比下沉更亂。

### 搬得動，因為抽象面是乾淨的

`grep DynamicExpresso src/Bee.Expressions/*.cs` 除 `DynamicExpressoEvaluator.cs` 外零命中。
這是機械式搬移，不是重設計——求值邏輯一行未改。

## 後果

### 破壞性變更（source-breaking）

三個公開型別換命名空間，且三個公開建構子的參數型別隨之改變：

| 成員 | 變更 |
|------|------|
| `Bee.Expressions.IExpressionEvaluator` | → `Bee.Base.Expressions.IExpressionEvaluator` |
| `Bee.Expressions.ExpressionEvaluationException` | → `Bee.Base.Expressions.ExpressionEvaluationException` |
| `Bee.Expressions.ExpressionPolicy` | → `Bee.Base.Expressions.ExpressionPolicy` |
| `FormExpressionCalculator(IExpressionEvaluator)` | 參數型別換 namespace |
| `FormRuleProcessor(IExpressionEvaluator)` | 參數型別換 namespace |
| `FormLiveComputation(FormSchema, RoundingContext?, IExpressionEvaluator?)` | 參數型別換 namespace |

型別名與成員簽章本身不變，外部消費端的改動是機械式的（改 `using`）。
框架處於 pre-stable（v4.x），此類變更允許但必須在 CHANGELOG 明列。

### 行為零變更

純搬移，求值邏輯、捨入政策、時區處理一律未動。

### `RS0026` 的例外

`IExpressionEvaluator` 的兩個 `Evaluate` 多載都帶選擇性參數 `timeZoneId`，
在 `Bee.Base` 屬「新增 API」，故觸發
`RS0026: Do not add multiple overloads with optional parameters`。
兩處以 `[SuppressMessage]` 標註並附理由：該多載對**不是**新 API，兩者一直是同進同出的一組，
不存在「呼叫端被靜默改綁到另一個多載」的風險——而那正是 RS0026 要防的事。

### 驗證

| 項目 | 結果 |
|------|------|
| `dotnet list src/Bee.Definition package --include-transitive` | 不再出現 `DynamicExpresso.Core` |
| `Bee.Definition.nuspec` 相依 | 只剩 `Bee.Base` + `Microsoft.Extensions.Localization.Abstractions` |
| 傳遞閉包測試 | 搬移前紅（抓到 `Bee.Expressions, DynamicExpresso.Core`）、搬移後綠；刻意注入 `MessagePack` 後再次轉紅（連其傳遞相依 `MessagePack.Annotations` / `Microsoft.NET.StringTools` 一併列出） |
| 建置期鎖 | 對 `Bee.Base` / `Bee.Definition` 各注入一次 `MessagePack`，兩者皆以 `BEE9001` 中止建置；還原後 0 warning / 0 error |
| clean Release build | 0 warning / 0 error |
| 全套單元測試 | 16 個測試專案全綠 |
