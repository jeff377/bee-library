# 序列化與運算式引擎規範（骨幹）

> wire formatter 的註冊程序、三個誤判點、`object` 封套、AOT 實測與回歸閘門
> → `src/Bee.Api.Core/CLAUDE.md`（觸及該專案時自動載入）。
> 行動端 trim / AOT 的型別形狀要件 → `rules/apple-mobile-trim.md`。
> 踩雷脈絡 → `docs/repo-ops/gotchas/serialization-and-expressions.md`。

## wire body codec 逐請求協商（adr-044）

body codec **不是部署設定**，由每個請求在 payload 信封的 `codec` 欄位宣告，伺服端**以同一個
codec 回應**；**未宣告即 MessagePack** —— 那是相容性常數而非挑出來的預設值（所有早於協商機制的
客戶端都不宣告且都送 MessagePack）。可用名稱與對應實作見 `PayloadCodecNames` 與
`ApiPayloadOptionsFactory.CreateSerializer`，**本檔不複寫那份清單**。

`PayloadFormat`（Plain/Encoded/Encrypted）是**加密/壓縮維度**，與 codec 正交：`Plain` 的 body
一律是信封那一份 System.Text.Json，只有 `Encoded` / `Encrypted` 的 body 才由 codec 決定怎麼拼寫。

因此 **.NET client（含 iOS / Android / WASM head）不宣告時兩端仍都跑 MessagePack**，
`ApiConnector.PayloadCodec` 是那一端唯一的改法。「行動端走 JSON、MessagePack 只在桌面/伺服器間」
的假設仍然不成立。

> ⚠️ **`ApiPayloadOptions.Serializer` 已於 4.27.0 移除**（破壞性變更）；殘留在既有
> `SystemSettings.xml` 的 `<Serializer>` 元素會被**忽略**而不是被採用。
> 「框架沒有 JSON body serializer」「`CreateSerializer` 只有一個 case」是 **4.26.0 以前**的結論，
> **別再照它推導**。

## 定義層不得引入傳輸格式套件（adr-036）

`src/Bee.Definition` **不得**有 `MessagePack`（或任何傳輸格式套件）的 `PackageReference`。
判準是「會不會讓定義層長出外部套件相依」：`[XmlIgnore]` / `[JsonIgnore]` 是 BCL 詞彙、
可用；MessagePack 標註不可。全 repo 的 MessagePack 相依只在 **`Bee.Api.Core`**。

wire 綁定由 `src/Bee.Api.Core/MessagePack/` 的**手寫 formatter** 承擔，定義型別不帶標註。

## 新增 wire 型別必須顯式註冊 formatter

`ContractlessStandardResolver` **沒有 reflection fallback**，只是桌面端的便利退路 ——
它靠 `Reflection.Emit`，而 .NET for iOS 對每個建置設 `DynamicCodeSupport=false`，
那裡未註冊的型別會擲 `FormatterNotRegisteredException`（不是變慢）。

新增 `Bee.Api.Core.Messages.*`、其遞移可達的定義層型別或集合時，**必須**到
`src/Bee.Api.Core/MessagePack/WireContracts.*.cs` 註冊。完整程序與三個容易誤判的點見
`src/Bee.Api.Core/CLAUDE.md`；漏補會被 `WireContractDriftTests` 擋下。

> 「MessagePack 在 AOT 可用」是被 2026-08-10 實測推翻的舊結論，**別照它推導** ——
> 推翻的過程與那次讓 iOS wire 整條壞掉的紀錄留在 `src/Bee.Api.Core/CLAUDE.md`。

## AOT：DynamicExpresso 無需特殊處理（此條不變）

DynamicExpresso 的 `Expression.Compile()` 在 `IsDynamicCodeSupported=false` 時自動退回**直譯器**。

**行動端不需為 AOT 停用即時運算。** `FormLiveComputation.IsDegraded` 的 degrade 機制是為
「客戶撰寫的運算式語法/識別字錯誤」防護，**與 AOT 無關**。

## 運算式變數表兩條硬性要求

跨 `Bee.Base`（`ExpressionPolicy`）與各 UI head（`FormLiveComputation`）兩處，故留常駐。

1. **變數 key 一律用 `FormField.FieldName`（schema 宣告的大小寫）**，不要用
   `DataColumn.ColumnName` —— **DynamicExpresso 識別字區分大小寫**，而運算式寫的是宣告欄名；
   拿 `DataColumn.ColumnName` 當 key 等於把運算式綁死在「記憶體 DataSet 當下用哪種大小寫存欄名」上。
   `DataRow` 索引與 `Fields.Contains` 本就大小寫無關，寫回不受影響。

   > **`AddColumn` 現在存小寫，不是大寫**（`fieldName.ToLowerInvariant()`，
   > `src/Bee.Base/Data/DataTableExtensions.cs`）。歷史上它存大寫，用大寫當 key 會直接
   > `UnknownIdentifierException`；ADR-029 已把儲存大小寫遷移為小寫，**與宣告欄名恰好一致**。
   > 這**不代表本條失效**——結論本來就是「與儲存大小寫解耦」，不是「避開大寫」；
   > 恰好一致只是讓違反此條的寫法暫時看不出症狀。

   **回歸測試務必用「與宣告欄名大小寫不同」的欄名建 DataTable**（現行實作下即大寫）。
   用跟宣告欄名一模一樣的小寫測，兩種寫法都會過，等於沒測到解耦。
2. **`ExpressionPolicy.CoerceValue` 不能只靠 `Convert.ChangeType`** —— `Guid` / `byte[]` 非
   `IConvertible`。client 端從 SQLite 讀回的 GUID 欄是 **String 型**，且可能是**空字串**。
   規則：`Guid` → 空/空白回 `Guid.Empty`、否則 `Guid.Parse`；`byte[]` → 空字串回空陣列、
   否則 `FromBase64String`。對齊「null/DBNull → 型別預設值」政策。
