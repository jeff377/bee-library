# 相依邊界規範（`Bee.Base` / `Bee.Definition`）

## `Bee.Base` 與 `Bee.Definition` 是最底層的兩個組件

**除非必要，不得再為這兩個專案加入任何套件參考（`PackageReference`）。**

它們是全框架相依圖的底部：`Bee.Definition` 的直接下游遍及 contracts、資料存取、快取、
商業邏輯、API 與 UI 各層，`Bee.Base` 則是**所有**專案的相依。**加在這兩層的任何套件，
會沿相依鏈傳染給每一個消費者**——包含只想讀定義的純 UI head 與定義檔工具。

同理，**下游專案的 `ProjectReference` 也算**：`Bee.Definition` 曾透過
`ProjectReference → Bee.Expressions` 傳遞相依 `DynamicExpresso.Core`（adr-038 已解），
外顯症狀是 nuspec 多一筆相依，但 grep「套件名」永遠掃不到。

## 「必要」的界線

| 類別 | 例 | 可否 |
|------|-----|------|
| BCL / 平台詞彙 | `System.Xml.Serialization`、`[JsonIgnore]` | ✅ 可 |
| Microsoft 第一方、**純抽象**、隨 .NET 版本走 | `Microsoft.Extensions.Localization.Abstractions` | ✅ 可 |
| 第三方套件 | `DynamicExpresso.Core`、`MessagePack`、`Newtonsoft.Json` | ❌ 不可 |
| 第一方**實作**套件（帶具體實作、獨立版本節奏） | — | ❌ 不可，比照第三方 |

判別法：**這個相依有沒有替消費者做出技術選擇？** 純抽象套件沒有——它不帶實作、不鎖定引擎。
完整判準與理由見 [adr-038](../../docs/adr/adr-038-definition-dependency-boundary.md)
與 [adr-036](../../docs/adr/adr-036-wire-serialization-externalized.md)。

## 想加東西時的正解

抽象與實作分居兩層，**抽象下沉、實作留在上層**：

- 抽象面若只有 BCL 型別 → 放 `Bee.Base`（如 `Bee.Base.Expressions.IExpressionEvaluator`）。
- 帶第三方套件的實作 → 留在專屬組件（如 `Bee.Expressions.DynamicExpressoEvaluator`），
  由**組裝層**（`Bee.Hosting`、各 UI head）決定用哪個實作。

`Bee.Base` 也不是抽屜：只搬「零外部相依、多層共用」的抽象進去。

## 兩道閘門（互補，不是重複）

| 閘門 | 位置 | 擋什麼 | 何時紅 |
|------|------|-------|--------|
| **建置期鎖** | `src/Directory.Build.targets`（`BEE9001`） | 受管專案的 `PackageReference`，以及**整個專案參考閉包**（見下） | 寫下去的當下，`dotnet build` 即失敗 |
| **傳遞閉包測試** | `tests/Bee.Definition.UnitTests/DefinitionDependencyGateTests.cs` | `Bee.Definition` 的**整個傳遞相依閉包** | 跑測試時 |

**受管專案**：`Bee.Base`、`Bee.Definition`、以及 **`Bee.Api.Contracts`**（2026-08-11 納入——
它位於每一個 UI head 的傳遞閉包內，ADR-038 的論證對它幾乎同等成立）。

**兩道都需要**：建置期鎖看不到「經由某個 `ProjectReference` 間接帶進來的**套件**」——
DynamicExpresso 正是這樣進來的；閉包測試則要跑測試才會知道。

> **修正（2026-08-11 實測）：建置期鎖看得到傳遞的「專案參考」，只是看不到傳遞的「套件」。**
> .NET SDK 的 `IncludeTransitiveProjectReferences` 會在 Build 之前把傳遞專案參考併進
> `@(ProjectReference)`。證據：`Bee.Api.Contracts` 的 csproj 只寫了 `Bee.Definition`，
> 一啟用閘門就以 `Bee.Base` 報 BEE9001。因此**allowlist 要列出整個專案參考閉包**，
> 不是只列 csproj 寫下的那幾條。`Bee.Definition` 的 allowlist 早就有 `Bee.Base`，
> 所以這個行為先前一直沒被看見。

建置期鎖只檢查**會流到消費者**的參考：`PrivateAssets="all"` 的套件（SourceLink、analyzer）
與 `ReferenceOutputAssembly="false"` 的專案參考（`Bee.Analyzers` 的建置排序）不算，
因為它們不會出現在 nuspec。

### 要放行時（三處都要改，缺一不可）

1. `src/Directory.Build.targets` 的 `BeeAllowedDependency`
2. `DefinitionDependencyGateTests.cs` 的 `s_allowedDependencies`
3. [adr-038](../../docs/adr/adr-038-definition-dependency-boundary.md) 記下理由

**這個麻煩是刻意的**——目的是逼出一次決策，而非默默通過。
先問一次上一節的「正解」：能不能只把抽象放這裡、實作留在別的組件？
