# 計畫：解除 Bee.Definition 對第三方套件的傳遞相依（DynamicExpresso）

**狀態：🚧 進行中（2026-08-10）—— 已採 B 案，交付實作 session**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 相依閘門：把「定義層不得長出外部套件相依」由人眼掃描變成可執行檢查 | 🚧 進行中 |
| 1 | 抽象三型別移入 `Bee.Base`，`Bee.Definition` 拿掉對 `Bee.Expressions` 的 ProjectReference | 📝 擬定中 |
| 2 | 消費端、`PublicAPI` 收尾、ADR 與規則文件 | 📝 擬定中 |

## 一句話

`Bee.Definition` 目前**傳遞相依 `DynamicExpresso.Core`** —— 這與 [adr-036](../adr/adr-036-wire-serialization-externalized.md)
趕走 MessagePack 時所用的判準是同一條，只是當時掃的是「傳輸格式」關鍵字，沒掃到它。
成因不是架構放錯位置，而是**抽象與實作同住一個組件**。

## 現況（實測資料）

### 1. 相依鏈

```
Bee.Definition ──ProjectReference──> Bee.Expressions ──PackageReference──> DynamicExpresso.Core 2.19.3
```

`dotnet list src/Bee.Definition package --include-transitive` 的實際輸出：

| 層級 | 套件 |
|------|------|
| 直接 | `Microsoft.Extensions.Localization.Abstractions` 10.0.8 |
| **傳遞** | **`DynamicExpresso.Core` 2.19.3** |
| 傳遞 | `System.IO.Hashing`（來自 `Bee.Base`，Microsoft 第一方） |

打包驗證：`Bee.Definition.4.19.0.nuspec` 列 `Bee.Expressions` 為相依，
故**任何裝 `Bee.Definition` 的消費者都會被拉進 DynamicExpresso**。

### 2. 全 repo 的第三方套件落點

掃過 17 個 `src/` 專案的直接 `PackageReference`，非 Microsoft 的第三方套件只有三個：

| 套件 | 所在專案 | 是否外洩到定義層 |
|------|---------|----------------|
| `MessagePack` | `Bee.Api.Core` | ❌ 否（adr-036 已處理） |
| `Avalonia` / `Avalonia.Controls.DataGrid` | `Bee.UI.Avalonia` | ❌ 否 |
| **`DynamicExpresso.Core`** | **`Bee.Expressions`** | ✅ **是** |

**唯一的違規點就這一處**，經由一個 ProjectReference，被一個檔案使用。

### 3. 型別 × 消費端矩陣

`Bee.Expressions` 只有四個公開型別。實測各消費端用到哪些：

| 型別 | Bee.Definition | Bee.UI.Avalonia | Bee.Business | Bee.Hosting | 性質 |
|------|:---:|:---:|:---:|:---:|------|
| `IExpressionEvaluator` | ✅ | ✅ | ✅ | ✅ | 抽象 |
| `ExpressionPolicy` | ✅ | — | — | — | 純政策，只依賴 `Bee.Base.Data` |
| `ExpressionEvaluationException` | — | ✅ | — | — | 介面契約的一部分（介面以 `<exception>` 標註） |
| `DynamicExpressoEvaluator` | — | ✅ | — | ✅ | **唯一碰到 DynamicExpresso 的型別** |

`Bee.Definition` 那一格只用兩個型別，且都在 `FormExpressionCalculator.cs` 一個檔案裡
（377 行，evaluator 由**建構子注入**，不自行 new 實作）。

### 4. 抽象面完全乾淨

`IExpressionEvaluator` 的公開表面只有 BCL 型別：

```csharp
object? Evaluate(string expression, IReadOnlyDictionary<string, object?> variables, Type returnType, string timeZoneId = "");
T Evaluate<T>(string expression, IReadOnlyDictionary<string, object?> variables, string timeZoneId = "");
IReadOnlyList<string> GetReferencedVariables(string expression);
```

`ExpressionPolicy` 的兩個方法只吃 `Bee.Base.Data.FieldDbType`。
`grep DynamicExpresso src/Bee.Expressions/*.cs` 除 `DynamicExpressoEvaluator.cs` 外**零命中**。

> 這一點是本計畫成立的關鍵：抽象搬得動，因為它沒有沾到實作的型別。

## 問題出在哪（以及哪部分不是問題）

拆成兩問，答案不同：

**定義層該不該懂運算式？** ✅ **該，維持現狀。**
`FormSchema` 宣告 `ValueExpression` 與驗證規則，求值是定義語意的一部分。
`FormExpressionCalculator` 是 schema-driven 的 `DataRow` 邏輯，**server 存檔前與 client
即時預覽共用同一份實作**，這正是「client 算出的值等於 server 寫入的值」的保證來源
（[adr-028](../adr/adr-028-expression-rule-engine.md)）。把它搬離定義層會斷掉這條線。

**定義層該不該因此帶第三方套件？** ❌ **不該。**
adr-036 的原文判準：

> 判準不是「是不是傳輸格式」，而是**「會不會讓定義層長出外部套件相依」**。

`DynamicExpresso.Core` 正落在這條判準的錯誤那一側。它不只是形式問題：

- **消費面**：只想讀定義的下游（例如純 UI head、定義檔工具）被迫裝一個運算式引擎。
- **行動端**：多一個組件要 trim，且 DynamicExpresso 是反射重度使用者。
- **未來替換**：要換掉 DynamicExpresso，今天得動到定義層的相依宣告。

## 選項與取捨

| 選項 | 內容 | 取捨 |
|------|------|------|
| **A. 維持現狀** | 承認定義層帶運算式引擎 | 零成本，但等於在 adr-036 才立的判準上開一個沒有理由的例外；下次體檢還會再被提出來 |
| **B. 抽象移入 `Bee.Base`** ⭐ | `IExpressionEvaluator` / `ExpressionEvaluationException` / `ExpressionPolicy` → `Bee.Base.Expressions`；`Bee.Expressions` 只留 `DynamicExpressoEvaluator` | 相依鏈立刻乾淨、無新套件；代價是公開型別換 namespace（source-breaking） |
| **C. 新增 `Bee.Expressions.Abstractions` 套件** | 標準的抽象/實作分包 | 語意最「正統」，但為三個型別多發一個 NuGet 套件（框架已 17 個），消費者多一個要認的名字 |
| **D. 定義層自宣告一個極小介面** | `Bee.Definition` 自己定義 evaluator 介面，由組裝層接上 | 不必動 `Bee.Expressions`，但會出現兩個平行介面 + 轉接器，長期比 B 更亂 |
| **E. `FormExpressionCalculator` 搬離定義層** | 移到 `Bee.Business` | ❌ 否決：`Bee.UI.Avalonia` 也用它做即時預覽，搬走等於斷掉 client/server 共用單一實作，而那是 adr-028 的核心保證 |

### 決策：採 B（2026-08-10 定案）

以下三點是選擇理由，實作時不需再議；A / C / D / E 已否決，理由如上表。

1. **相依鏈變成該有的樣子**，且不新增任何套件 ——
   `Bee.Base` 本來就是全框架都依賴的那層，抽象放在那裡不多花任何人一分成本。
2. **搬得動**（見上節第 4 點）：抽象面零 DynamicExpresso 痕跡，是機械式搬移，不是重設計。
3. **與 adr-036 同一手法**：那次把 wire 綁定留在 `Bee.Api.Core`、定義型別不帶標註；
   這次把實作留在 `Bee.Expressions`、抽象下沉到共用層。同一個形狀。

C 只在「日後 `Bee.Base` 也想瘦身」時才更划算，現在不值得多一個套件。

### 落點與命名

`Bee.Base` 現有一級子命名空間：`Attributes` / `Collections` / `Data` / `Exceptions` /
`Security` / `Serialization` / `Tracing`。新增 **`Bee.Base.Expressions`**，三個型別同放。

> `ExpressionEvaluationException` 也放這裡而非 `Bee.Base.Exceptions`：它是
> `IExpressionEvaluator` 契約的一部分（介面上以 `<exception cref>` 標註），
> 與介面同放的內聚性高於「所有例外集中」。`Bee.Base.Exceptions` 現有三個型別
> 都是跨領域通用例外（`ForbiddenException` / `UserMessageException`），性質不同。

## 階段

### 階段 0：把判準變成閘門

**先做這個**，理由與行動端 AOT 那次相同：這次是 grep 關鍵字才漏掉 DynamicExpresso，
下次換個套件名還是會漏。

在 `Bee.Definition.UnitTests` 加一條測試，斷言 `Bee.Definition` 組件的**傳遞相依集合**
落在白名單內。可行做法二選一（實作時決定）：

- 讀 `Bee.Definition.deps.json`（建置產物，含完整傳遞圖），比對白名單；
- 或以 `Assembly.GetReferencedAssemblies()` 遞迴展開。

白名單初值：BCL、`Bee.Base`、`Microsoft.Extensions.Localization.Abstractions`。
**新增任何外部套件相依都必須顯式加白名單**，逼出一次決策而非默默通過。

> 附帶待決：`Microsoft.Extensions.Localization.Abstractions` 嚴格說也是外部套件
> （只被 `Language/BeeStringLocalizer.cs` 一個檔案使用）。它是 Microsoft 第一方、
> 純抽象、隨 .NET 版本走，我**建議留下並列入白名單**，但這代表 adr-036 的判準需要
> 補一句話說清楚「外部套件」的界線是什麼 —— 見階段 2。

### 階段 1：搬移

1. 三個型別移至 `src/Bee.Base/Expressions/`，namespace 改 `Bee.Base.Expressions`。
2. `src/Bee.Definition/Bee.Definition.csproj` 移除 `Bee.Expressions` 的 ProjectReference。
3. `src/Bee.Business/Bee.Business.csproj` 同樣可移除（它只用 `IExpressionEvaluator`）—— 實作時確認。
4. `Bee.UI.Avalonia` / `Bee.Hosting` **保留**對 `Bee.Expressions` 的引用：它們要的是實作，
   位置正確（組裝層才決定用哪個 evaluator）。
5. 消費端補 `using Bee.Base.Expressions;`：production 4 檔、測試 6 檔。

### 階段 2：收尾

- **`PublicAPI` 檔**：`Bee.Expressions/PublicAPI.Shipped.txt` 18 行中的 **12 行**移出
  （只留 `DynamicExpressoEvaluator` 的 5 行 + header），對應條目進 `Bee.Base` 的 Unshipped。
  移除側要用 `*REMOVED*` 標記，發版併檔時的處理見 `rules/releasing.md`。
- **ADR**：修訂 adr-028 或新增一則短 ADR，記「抽象在 `Bee.Base`、實作在 `Bee.Expressions`」
  這條分界；同時把 adr-036 判準中「外部套件」的界線補明確（第一方純抽象 vs 第三方實作）。
- **CHANGELOG**：`IExpressionEvaluator` 等三型別換 namespace 屬 **source-breaking**，須明列。
- `docs/dependency-map.md` 更新 `Bee.Expressions` 那一段的描述。

## 驗收判準

1. `dotnet list src/Bee.Definition/Bee.Definition.csproj package --include-transitive`
   **不再出現 `DynamicExpresso.Core`**。
2. `Bee.Definition` 的 nuspec 相依只剩 `Bee.Base` + `Microsoft.Extensions.Localization.Abstractions`。
3. 階段 0 的相依閘門測試通過，且刻意加一個外部相依時**會失敗**（閘門要驗有效性，不能只驗綠燈）。
4. 全套測試綠、clean Release build 0 warning。
5. 行為零變更：這是純搬移，不動任何求值邏輯。

## 風險

| 風險 | 影響 | 緩解 |
|------|------|------|
| namespace 變更是 source-breaking | 外部消費端要改 `using` | pre-stable 允許，但 CHANGELOG 必須明列；型別名與成員簽章完全不變，改動是機械式的 |
| `ExpressionPolicy` 的歸屬判斷錯誤 | 放錯層，之後又要搬一次 | 它只吃 `Bee.Base.Data.FieldDbType`、零外部相依，放 `Bee.Base` 無爭議；若日後認定它屬定義層語意，再搬進 `Bee.Definition` 也不會引入套件相依 |
| `Bee.Base` 變成什麼都放的抽屜 | 長期內聚下降 | 本次只搬「零外部相依、多層共用」的抽象，符合 `Bee.Base` 既有定位；階段 0 的白名單同時也是對 `Bee.Base` 膨脹的紀錄點 |
| 階段 0 的閘門實作方式選錯 | 檢查不到真實傳遞圖 | 優先用 `deps.json`（建置產物、含完整傳遞圖），`GetReferencedAssemblies` 只反映實際被引用的組件、可能漏掉未使用但已宣告的套件 |
