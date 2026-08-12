# 未使用型別盤點與清理（2026-08-12）

**狀態：✅ 已完成（2026-08-12）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | Roslyn 符號級全掃 + 逐項蒐證盤點 | ✅ 已完成（2026-08-12） |
| 1 | 接上 `NumberFormatApplier`（功能缺口，非清理） | ✅ 已完成（2026-08-12） |
| 2 | 已宣告未實作的對外契約：兩個 JSON-RPC 錯誤碼 | ✅ 已完成（2026-08-12） |
| 3 | 低風險清理：冗餘多載、私有結構未用欄位、過期註解 | ✅ 已完成（2026-08-12） |
| 4 | 五個無文件記載的公開工具型別去留 | ✅ 已完成（2026-08-12） |
| 5 | `Bee.Api.Contracts` 契約介面的定位 | ✅ 已完成（2026-08-12） |

---

## 階段 0：掃描方法與盤點結論

### 方法

以 Roslyn 建一次性分析工具，`MSBuildWorkspace` 載入 repo 全部 **47 個專案**、**1913 個文件**，
逐節點取語意模型的 `GetSymbolInfo`，建立「符號 → 參考檔案集合」索引，涵蓋 `src/` 的
**995 個型別 / 8038 個成員**。非文字比對。

交叉比對的反射註冊面：[`BackendDefaultTypes`](../../src/Bee.Definition/BackendDefaultTypes.cs)
的 9 個型別名字串、[`ExecFuncHandlerExtensions`](../../src/Bee.Business/ExecFuncHandlerExtensions.cs)
的 `handler.GetType().GetMethod(args.FuncId)`、
[`JsonRpcExecutor`](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs) 依 action 名反射派送 BO 方法、
`[DiagnosticAnalyzer]` 屬性載入、`DbDialectRegistry.Register` 的 host 端註冊。

### 四個必須避開的偽陽性陷阱

**皆為本次實測踩到並修正**，日後重跑務必沿用，否則結論會相反：

1. **`SymbolFinder.FindReferencesAsync` 對擴充方法宿主類永遠回 0** —— 呼叫端 `x.Foo()` 綁定的是
   方法符號、不是宿主類符號。若只做型別級查詢，`DataTableExtensions`、
   `BeeServiceProviderExtensions` 等會全被誤判為死碼（後者實際有 14 個檔案在用）。
   **正解**：型別存活＝型別本身**或其任一成員**有外部參考。
2. **宣告節點內的型別節點不可跳過** —— `public DbCommandSpecCollection Commands { get; set; }`
   的型別名是 `PropertyDeclarationSyntax` 的子節點。初版為略過「宣告名」而跳過
   `Parent is MemberDeclarationSyntax` 的 `SimpleNameSyntax`，連帶濾掉屬性 / 欄位 / 回傳型別，
   於是 `DbCommandResultCollection` 等三個型別被誤報。**C# 的宣告識別字是 `SyntaxToken` 不是節點，
   本來就不需要跳過。**
3. **屬性存取不會參考到 accessor 符號** —— `x.Foo` 綁定 `IPropertySymbol`，不是 `get_Foo`。
   若把 accessor 當一般方法統計，會產生 1800 筆假死方法。
4. **「零外部參考」不等於死碼** —— 需再分「同檔還有沒有其他型別」。12 個零外部參考的型別
   全部是 private/internal 的同檔輔助型別，在自己檔案內被使用。

### 掃描結論

**沒有任何完全死掉的型別。未使用的 private/internal 方法 0 個、欄位 0 個、屬性 0 個**
—— `IDE0051`/`IDE0052` 搭配 `TreatWarningsAsErrors=true` 已把這層守住，不需要額外閘門。

### 與前輪體檢的關係（重要）

`archive/plan-framework-review-2026-07-28.md` 的 **P2-4** 已做過一次死碼清除，
且**範圍經使用者逐項裁決、非全表照刪**。已裁決事項不在本 plan 重議：

| 前輪裁決 | 內容 |
|---------|------|
| 「未消費的抽象」**保留** | `IDefineField`、`IElementCapabilityResolver` —— 理由是「要不要留擴充點的**判斷題**，不是死碼」。**這兩個正好落在本次『介面無消費端』清單內，不重提。** |
| 「刻意擴充點」**保留** | `CheckPackageUpdate` / `GetPackage` 全棧 —— base 擲 `NotSupportedException` 供子類覆寫，已列入公開 API 參考文件 |
| `DateTimeExtensions` 部分刪除 | `IsEmpty` 已刪；`GetYearMonth` **知情保留**（該輪還拿它當 `PublicApiAnalyzers` 生效的示範案例） |

`archive/plan-framework-review-2026-08-11.md` 的 **D-6 / D-8 / X-6** 也涵蓋同一片地，
其中 `ApiErrorInfo`、`MessagePackContract` 已刪除完畢（本次掃描已確認不存在）。
**X-6 與本 plan 的 `BusinessObjectFactoryExtensions` 是同一根因**，見階段 4。

> 前輪用的是 grep，本輪用 Roslyn 符號解析。前輪自己記錄過一次 grep 漏判
> （`TreeNodeIgnoreAttribute` 被誤刪、build 失敗才還原）。本 plan 只列**前輪工具查不到、
> 或前輪未裁決**的項目。

---

## 階段 1：接上 `NumberFormatApplier`

**這不是清理，是功能缺口。** 優先於其餘階段。

### 盤點結果

[`NumberFormatApplier`](../../src/Bee.Definition/Forms/NumberFormatApplier.cs) 負責把公司層數值
顯示格式 bake 到 `FormSchema` 的數值欄位。現況是**它從不執行**：

- 呼叫端由 commit `ec94d0aa`（2026-08-01，`feat(api)!: 定義類 API 一律供應原始定義 + XML 信封`）
  移除，`LoadAndLocalizeSchema` 與 `TryGetCompanyInfo` 一併刪除。
- **移除是刻意的**：該 commit message 明寫「客製與生成則移到**需求端**」，
  [`SystemBusinessObject.Define.cs`](../../src/Bee.Business/System/SystemBusinessObject.Define.cs)
  的 `GetFormSchema` `<remarks>` 也改寫成「呼叫端自行套用」。
- **缺口在需求端沒補上**：client pipeline
  [`FormDefinitionLoader.GetLocalizedSchemaAsync`](../../src/Bee.Api.Client/Definitions/FormDefinitionLoader.cs)
  已接上 `FormSchemaLocalizer`，`GetRuntimeLayoutAsync` 已接上 `CustomizeOverlay`，
  **唯獨沒有接 `NumberFormatApplier`**。
- 其 XML doc 仍指向已不存在的 `SystemBusinessObject.LoadAndLocalizeSchema`（全 repo 零命中）。

### 已確認的接線條件

- 資料來源齊備：`CompanyInfo.GetDecimals(NumberKind)` 存在
  （[CompanyInfo.cs:72](../../src/Bee.Definition/Identity/CompanyInfo.cs)），
  `ClientInfo.Company` 在 `EnterCompany` 後持有 `CompanyInfo`
  （[ClientInfo.cs:202](../../src/Bee.UI.Core/ClientInfo.cs)）。
- **相依方向是單向的**：`Bee.UI.Core` → `Bee.Api.Client`，所以 `FormDefinitionLoader`
  **拿不到** `ClientInfo.Company`。公司資訊必須由參數或 ctor 注入
  （`CompanyInfo?` 參數，或 ctor 收 `Func<CompanyInfo?>`），不能反向相依。
- **兩條路徑都要 bake**：`GetLocalizedSchemaAsync` 在 `lang` 為空時於
  [第 62–63 行](../../src/Bee.Api.Client/Definitions/FormDefinitionLoader.cs)
  早退回 `raw.Clone()`，該路徑同樣需要 bake。
- `Bake` 就地 mutate，依 `rules/definition.md` 的 cache 不可變規則**必須傳 clone**；
  兩條路徑都已 `Clone()`，可直接沿用。用 `HasNumericField` 先判斷以省下不必要的 clone
  （該方法就是為此存在）。

### 步驟

1. 確認 baking 落點：client pipeline（與 localizer / overlay 同層，本 plan 建議）或改回 server 端。
2. 為 `FormDefinitionLoader` 加公司資訊注入點，在兩條路徑上呼叫 `Bake`。
3. 由各 head 傳入 `ClientInfo.Company`。
4. 修正 `NumberFormatApplier` 的 XML doc（移除已不存在的 `LoadAndLocalizeSchema` 交叉引用）。
5. 補端到端測試：公司 A 與公司 B 位數不同 → 同一 ProgId 取得不同格式。

### 執行結果（2026-08-12）

落點選在 client pipeline。`FormDefinitionLoader.GetLocalizedSchemaAsync` 現在**兩條路徑都 bake**
（原本空語系會早退），公司來源是新增的 `CompanyAccessor` init 屬性。

- **注入形式改為 `init` 屬性而非 ctor 多載**：先做的 ctor 多載被 **RS0027** 擋下——
  `PublicApiAnalyzers` 要求「帶 optional 參數的多載必須是參數最多的那個」，而既有 ctor 的
  `defaultLang = ""` 與新的三參數多載相衝。把既有 ctor 改成三參數全 optional 可以過 analyzer，
  但那會讓已出貨的兩參數簽章消失（二進位破壞性，`releasing.md` 明文禁止）。
  `init` 屬性兩邊都避開，且與 `RoundingContext` 的既有風格一致。
- **`Func<CompanyInfo?>` 而非 `CompanyInfo` 值**：進入的公司會在 session 存續期間變動
  （`EnterCompany` / `LeaveCompany`），建構時快照會讓 loader 一直 bake 前一租戶的位數。
  已有回歸測試釘住這點。
- **兩個 head 同時受益**：`FormView.DefinitionLoader`（Avalonia）與 `FormPage.DefinitionLoader`
  （Blazor）都經同一個 loader，不需各自改。
- 傳導鏈已確認：`FormField.NumberFormat` / `CurrencyField` → `LayoutColumnFactory` →
  `LayoutColumn` → `GridControl` / `NumericEdit`。UI 只對 Currency / Unit-bound 類做逐列解析，
  其餘直接讀「交付下來的」格式字串——正是 `Bake` 負責填的那一半。
- **`NumberFormatApplier.HasNumericField` 仍為 0 caller**：它存在的目的是「省下不必要的 clone」，
  但快取不可變規則要求無論如何都得 clone，這個接線點用不到它。不為了給它一個呼叫端而扭曲程式碼。

---

## 階段 2：已宣告未實作的對外契約

[`JsonRpcErrorCode`](../../src/Bee.Api.Core/JsonRpc/JsonRpcErrorCode.cs) 的
`CompanyNotEntered = -32002` 與 `CompanyAccessDenied = -32003` **從未被拋出**。

這不是「多宣告一個列舉值」而已 —— 兩者的 XML doc 都寫明了 HTTP 對映
（409 Conflict / 403 Forbidden），`CompanyAccessDenied` 還記載了「兩種失敗合併為單一錯誤碼以防
匿名列舉公司 id」的安全設計。**這是已發布的對外契約，但實作走的是另一條路**：

- [`EnterCompany`](../../src/Bee.Business/System/SystemBusinessObject.Session.cs) 失敗時擲
  `InvalidOperationException("Company access denied.")`。
- `JsonRpcExecutor.IsUserFacingException`
  （[:312](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs)）的 BCL 白名單**含
  `InvalidOperationException`**。
- 因此 `MapException`（[:353](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs)）對映成
  `UserMessage = -32099`，而非設計的 `-32003`。

對照組：`PermissionDenied = -32004` 有專屬的
[`ForbiddenException`](../../src/Bee.Base/Exceptions/ForbiddenException.cs) 與 `MapException`
分支，走得通。**公司層那兩個碼缺的就是這條專屬例外 + 對映分支。**

### 步驟

擇一，不要放著：

- **實作**：比照 `ForbiddenException` 建立對應例外型別，`EnterCompany` 改擲之，
  `MapException` 加分支；client 端 [`ApiConnector`](../../src/Bee.Api.Client/Connectors/ApiConnector.cs)
  已對 `PermissionDenied` 有處理，同步補上。
- **刪除**：兩個碼移出列舉（破壞性，需走 `PublicAPI` 流程），並移除 XML doc 內的 HTTP 對映描述。

> 保留現狀的成本：client 端會為永不出現的錯誤碼寫處理分支，而真正的公司拒絕會以
> 「一般業務訊息」的形式抵達，前端無法據以做 403 的統一處理。

### 執行結果（2026-08-12）：`CompanyAccessDenied` 已實作

**盤點時的判斷需要更正**：原以為實作這兩個碼等於「發明政策」。實際上
[`adr-012`](../adr/adr-012-session-company-context.md) 的行為矩陣與新增錯誤碼表、以及公開文件
[`jsonrpc-frontend-integration`](../jsonrpc-frontend-integration.md) 的錯誤碼表，
**都早已把這兩個碼寫成對外契約**（-32002 → HTTP 409、-32003 → HTTP 403）。
政策是既有決策，缺的只是實作。

已完成：新增 `Bee.Base.Exceptions.CompanyAccessDeniedException`（合併三種失敗原因、
以相同訊息避免公司 id 列舉），`EnterCompany` 改擲之，`JsonRpcExecutor.MapException` 加分支
（**必須排在 BCL 白名單之前**——`InvalidOperationException` 在白名單內，是先前錯誤地落到
-32099 的原因），`ApiConnector` 於 client 端重建例外。三個既有 `EnterCompany` 測試的斷言型別
一併更新。

### 執行結果（2026-08-12）：`CompanyNotEntered` 已實作

**盤點階段對這一項的判斷是錯的，必須更正。** 當時說「adr-012 只寫『company 類 BO 方法』而未列舉，
要實作就得逐一裁定整個 BO 表面」——那個裁定**框架早就做完了**，而且守衛已經寫在程式碼裡：

```csharp
// RepositoryDatabaseRouter.Resolve，本次修改前
if (string.IsNullOrEmpty(session.CompanyId))
    throw new InvalidOperationException("CompanyNotEntered");
```

判準**不是 per-BO-method，是 per-`DbScope`**：任何 `DbScope.Company` 的 repository 存取都要求
公司情境，而 scope 由 `FormSchema.CategoryId == "company"` 推導（`RepositoryFactory`）。
**單一收斂點，不需要每個業務方法各自守。** 先前只看到 `FormBusinessObject` 回傳 `null` 的那個
分支，沒往下追到 router。

真正的缺陷是**錯誤碼的名字被當成例外訊息字串寫死**，且測試把該字串釘住
（`Assert.Equal("CompanyNotEntered", ex.Message)`）。後果：

1. `InvalidOperationException` 在 `IsUserFacingException` 的 BCL 白名單內
2. → `MapException` 對映成 `UserMessage = -32099`
3. → **使用者沒進公司就開表單，畫面會跳出一個寫著 `CompanyNotEntered` 的訊息框**，
   而前端無從處理——-32099 的文件語意就是「原文顯示給使用者」

已完成（形狀與 `CompanyAccessDenied` 相同）：新增
`Bee.Base.Exceptions.CompanyNotEnteredException`，`RepositoryDatabaseRouter` 改擲之並換上正常的
英文訊息，`MapException` 加分支，`ApiConnector` 於 client 端重建例外，
`IRepositoryDatabaseRouter` 的 `<exception>` 文件把「未進公司」與「CompanyInfo cache miss」
拆成兩條。釘住字串的測試改為斷言**型別**，並反向斷言訊息**不含**錯誤碼名稱。

> **這個錯法值得記住**：需要一個錯誤碼卻沒有對應例外型別時，把碼名塞進訊息字串看起來像
> 「先擋著」，實際上是把協定狀態降級成業務訊息，而且測試釘住字串之後，這個降級就再也不會
> 被視為缺陷。

---

## 階段 3：低風險清理

| 項目 | 位置 | 盤點結果 |
|------|------|---------|
| 冗餘多載 `NormalizeDateTimeMode(DataSet)` | [DataTableExtensions.cs:218](../../src/Bee.Base/Data/DataTableExtensions.cs) | 同名 `DataTable` 版本有 11 處使用，`DataSet` 版本零參考 |
| 冗餘多載 `WireContract.For<T>(Func<T>)` | [WireContract.cs:20](../../src/Bee.Api.Core/MessagePack/WireContract.cs) | 另一多載在用，此多載零參考 |
| `SecurityKeys` 未消費欄位 | [BeeFrameworkServiceCollectionExtensions.Factories.cs:163](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.Factories.cs) | private `record struct` 的 `CookieEncryptionKey` / `DatabaseEncryptionKey` 已解密但無消費端。確認是否為預留欄位；否則刪除（純內部，零公開表面變更） |
| **過期註解** | [SystemApiConnector.cs:220-232](../../src/Bee.Api.Client/Connectors/SystemApiConnector.cs) | 註解寫「`GetFormSchema` / `GetFormLayout` / `GetLanguage` 是 **JS-only（Plain JSON wire format）**，.NET client 不提供」。但 `ec94d0aa` 已把這三個入口改為**一律 XML 信封**，commit message 明寫「改成 XML 後同一組 API 兩端都能用」。**理由已不成立，註解卻還在。** |

前兩項為 public，刪除需走 `PublicAPI.Unshipped.txt` 的 `*REMOVED*` 流程（見 `releasing.md` 第 3 節）。

### 執行結果（2026-08-12）

四項全部完成。兩點值得記下：

- `WireContract.For<T>(Func<T>)` 是 `internal`，**不需要** `PublicAPI` 條目；只有
  `NormalizeDateTimeMode(DataSet)` 進了 `Bee.Base` 的 `*REMOVED*` 清單。
- `SecurityKeys` 移除兩個欄位是零行為變更（解出來的值本來就沒有去處），**但確實少了一項
  副作用**：那兩把金鑰不再於啟動時被解密，因此格式損毀不會再 fail fast。這對「沒有任何消費端
  的金鑰」而言不算損失，反而更誠實——但**上游問題仍在**：`SecurityKeySettings.CookieEncryptionKey`
  與 `DatabaseEncryptionKey` 是已出貨的設定合約，全 repo 零消費端，部署者設定了會以為有效。
  與 `archive/plan-framework-review-2026-08-11.md` 的 **D-7**（`AuditLogOptions.ExecEnabled`）
  同型，應併案處理。

---

## 階段 4：無文件記載的公開工具型別

盤點時列為候選的 public 型別——**只有測試在使用**，`src`/`samples`/`apps`/`tools` 全數零參考，
**且不在任何公開文件內、也未經前輪裁決**。下表為裁決後仍保留者：

| 型別 | 位置 | 備註 |
|------|------|------|
| `DataTableComparer` | [DataTableComparer.cs](../../src/Bee.Base/Data/DataTableComparer.cs) | |
| `DataRowViewExtensions` | [DataRowViewExtensions.cs](../../src/Bee.Base/Data/DataRowViewExtensions.cs) | 純委派給 `DataRowExtensions` |
| `Dictionary<T>` | [Dictionary.cs](../../src/Bee.Base/Collections/Dictionary.cs) | 見下方註記 |

判準用 `code-style.md`「0-caller 框架公開 API 保留、**純 BCL wrapper 且 0 caller 才直接刪**」。

### 裁決與執行結果（2026-08-12，逐項由使用者裁決）

| 型別 | 裁決 | 結果 |
|------|------|------|
| `MemberPath` | **刪除** | 型別與專屬測試一併刪除，`*REMOVED*` 已入 `PublicAPI.Unshipped.txt` |
| `ConnectionTestResult` | **刪除** | 同上；`AssemblyLoader` 的測試改用 `TreeNodeAttribute` 當樣本型別（見下） |
| `DataTableComparer` | **保留為公開 API + 補文件** | 已列入 `Bee.Base/README` 雙語版。原訂搬進 `Bee.Tests.Shared`，執行時發現代價過高（見下） |
| `DataRowViewExtensions` | **保留 + 補文件** | 已列入 `Bee.Base/README` 雙語版 |
| `Dictionary<T>` | **保留原名** | 零變動。撞名問題與 wire 白名單無關——白名單靠 `SysInfo.IsTypeNameAllowed` 的**命名空間**規則放行，不是逐型別列舉 |
| `BusinessObjectFactoryExtensions` | **保留為 host-facing API + 修正措辭** | 見下 |

#### `DataTableComparer` 為何沒搬進 `Bee.Tests.Shared`

原判斷「它的真實使用者是測試，搬過去最誠實」在執行時被一個事實推翻：
**`Bee.Base.UnitTests` 目前只參考 `Bee.Base` 一個專案**，而 `Bee.Tests.Shared` 會帶進
`Bee.Api.Client` + `Bee.Hosting` + `Bee.UI.Core`。為了一個 75 行的比對工具，把相依圖最底層的
測試專案變成相依整個框架，代價明顯大於收益。改判為刻意的公開工具並補文件。

#### 刪除 `ConnectionTestResult` 的連帶成本（原本沒算到）

`AssemblyLoaderTests` / `AssemblyLoaderExtraTests` 拿它當 `GetType` / `CreateInstance` 的樣本型別，
而它是**根命名空間 `Bee.Base` 底下唯一具備無參數 ctor 的 public 型別**——測試會挑它不是巧合。
替換為 `Bee.Base.Attributes.TreeNodeAttribute` 時踩到兩個坑，兩個都值得記住：

1. **命名空間推斷測試必須用根命名空間型別**：`AssemblyLoader.GetType(fullTypeName)` 以
   「去掉最後一段當組件名」推斷，`Bee.Base.Attributes.X` 會去找不存在的 `Bee.Base.Attributes.dll`。
   該測試改用 `Bee.Base.SysInfo`（只需型別存在，不需具現化）。
2. **`AssemblyLoader.CreateInstance` 有多載解析陷阱**：`CreateInstance(aqn, "ok", true)` 的第二個
   引數是 `string`，C# 會綁到 `CreateInstance(assemblyName, typeName, params object[])`——AQN 被當成
   組件名，擲 `FileLoadException`。**原測試從沒踩到，只因它的第一個 ctor 引數剛好是 `bool`。**
   現改為顯式傳 `object[]`，並在測試留下 `WARNING:` 註解。這是框架公開 API 的真實地雷，
   任何第一個 ctor 引數為 `string` 的呼叫端都會中。

#### `BusinessObjectFactoryExtensions`：盤點時的說法需要收回

盤點階段把它報成「規範與程式碼漂移」，**講重了**。逐一查證後：

- `IFormBusinessObject` 的 `<remarks>` 已明寫「the framework itself makes no BO-to-BO call through
  this seam today, so its only current callers are tests」——準確。
- `ILogBusinessObject` 的 `<remarks>` 已寫「`CreateLogBO` … has no callers inside the framework
  so far」——準確。前輪體檢 **X-6** 指出的那句事實錯誤（「there is no CreateLogBO factory
  extension」）**先前已修掉**，本次無事可做。
- `rules/definition.md` 的 `_ctx.BoFactory.CreateXxxBO(...)` 是**判準的假設語氣**（「會不會有人
  這樣呼叫」），不是在宣稱框架內部這樣用。

真正缺的只有一句「呼叫端是誰」。已在 `rules/definition.md` 補上：框架內部沒有 BO-to-BO 場景
（`JsonRpcExecutor` 依 progId 派送、不知道是哪條軸，`RepositoryFactory` 同理），**零 caller 是
預期的、不是死碼**，並註明本次盤點曾誤列為候選，避免下輪體檢再重來一次。

#### 追加裁決：移除 `ILogBusinessObject`（2026-08-12）

使用者於本階段裁定：**軸介面只有「會被別的 BO 呼叫」的軸才需要**，而稽核記錄查詢本質上不是
BO 之間會做的事——它是 client / API 面的需求。因此移除 `ILogBusinessObject` 與 `CreateLogBO`
（後者的回傳型別即前者，必然一併移除），`IBusinessObject` / `IFormBusinessObject` /
`ISystemBusinessObject` 三個全數保留。

`LogBusinessObject` 的 9 個稽核查詢方法照樣經 `JsonRpcExecutor` 對外開放，完全不受影響——
移除的只是一個沒有消費端的公開介面表面。判準已寫進 `rules/definition.md`：
**新增一條軸時先問「這條軸的方法，另一個 BO 會想呼叫嗎？」不會就別開介面。**

### `Dictionary<T>` 另有一項

[`Dictionary.cs:7`](../../src/Bee.Base/Collections/Dictionary.cs) 的
`public class Dictionary<T> : Dictionary<string, T>`（OrdinalIgnoreCase key）**違反 `code-style.md`
自己的命名規範**：撞 `System.Collections.Generic.Dictionary`，消費端同時 `using` 兩個 namespace
時需要 alias。

**但它不是純粹的孤兒**：`WireTypeWhitelist` 的 XML doc 拿它當「允許的外層泛型」範例，
4 個測試（`WireTypeWhitelistTests` / `ApiPayloadConverterTests`）以它為白名單邊界案例的樣本。
刪除須連帶更新這些測試與文件；改名則只需同步字串。**建議改名而非刪除**
（如 `OrdinalIgnoreCaseDictionary<T>`），撞名問題實際存在而使用場景可能仍有效。

### 建議保留（有明文定位，非遺留）

| 型別 | 保留理由 |
|------|---------|
| [`FileHashValidator`](../../src/Bee.Base/Security/FileHashValidator.cs) | `security.md` 明文「使用 `FileHashValidator` 驗證檔案完整性，不自行實作雜湊比對邏輯」 |
| [`PermissionBindingValidator`](../../src/Bee.Definition/Settings/Permission/PermissionBindingValidator.cs) | 自身 `<remarks>` 明寫「框架不自己呼叫，由 host 在啟動 / 部署冒煙測試 / CI 呼叫」 |
| [`TraceDispatcher`](../../src/Bee.Base/Tracing/TraceDispatcher.cs) | `ITraceListener` 的預設實作，由 host 決定是否啟用 |
| [`VersionInfo`](../../src/Bee.UI.Core/VersionInfo.cs) | 見於根 `README`、`docs/dependency-map.md`、`adr-013` |
| [`BeeStringLocalizer`](../../src/Bee.Definition/Language/BeeStringLocalizer.cs) | 見於 `docs/terminology.md`、`adr-038` |
| `IPValidator` / `CollectionExtensions` / `DateTimeExtensions` | 均列於 `src/Bee.Base/README.md` 雙語版；`DateTimeExtensions` 另經前輪知情保留 |

### `BusinessObjectFactoryExtensions`：規範與程式碼漂移

`rules/definition.md` 把 `_ctx.BoFactory.CreateXxxBO(...)` 寫成 BO-to-BO 解耦的**標準呼叫法**，
`IFormBusinessObject` / `ILogBusinessObject` 的 XML doc 也稱其為「intended entry point」——
但 `src/` 內**沒有任何一處這樣呼叫**，實際都直接走 `IBusinessObjectFactory.CreateBusinessObject`。

這與 `archive/plan-framework-review-2026-08-11.md` 的 **X-6**（`ILogBusinessObject` 的
`<remarks>` 寫「there is no `CreateLogBO` factory extension」但它存在且已發布）是同一根因：
**這三個擴充方法的定位從未落實，文件各說各話。**

處置擇一：把 `src/` 既有呼叫點改用擴充方法；或刪除擴充方法並同步修正
`rules/definition.md` 與兩個介面的 XML doc。**與 X-6 一併處理，不要分兩次改同一段文件。**

---

## 階段 5：`Bee.Api.Contracts` 契約介面的定位

[`ApiContractRegistry`](../../src/Bee.Api.Core/Registry/ApiContractRegistry.cs) 的
`Register<TContract,TApi>()` **production 零呼叫**（其自身 `<remarks>` 已載明），
因此 `s_mappings` 恆為空、`ConvertForSerialization`（由
[`ApiPayloadConverter.cs:34`](../../src/Bee.Api.Core/JsonRpc/ApiPayloadConverter.cs) 呼叫）恆為 no-op。

連帶地，`Bee.Api.Contracts` 的 **56 個 `I*Request` / `I*Response` 介面沒有任何執行期消費端**——
掃描顯示其參考全部落在 base list（`class PingArgs : BusinessArgs, IPingRequest`）。

**它們目前的實際作用是編譯期形狀閘門**：強制 `Bee.Business` 的 `XxxArgs` / `XxxResult` 與
`Bee.Api.Core` 的 `XxxRequest` / `XxxResponse` 保持同一組成員。這個作用有價值，但與
「BO 回傳純 POCO、由 registry 轉成 API 型別」的原始設計意圖已經脫節。

### 步驟

1. 決定 `ApiContractRegistry` 去留：真要支援「BO 回傳純 POCO」→ 補上註冊與測試；
   不支援 → 刪除 registry 與 `ApiPayloadConverter` 的呼叫點。
2. 若刪除 registry，**契約介面本身仍應保留**（形狀閘門有效），但要把這個定位寫進
   [`Bee.Api.Contracts/README.md`](../../src/Bee.Api.Contracts/README.md) 雙語版，
   避免後人再次以為它有執行期用途。
3. 決策若屬長效架構判斷，補一份 ADR。

### 裁決與執行結果（2026-08-12）

**盤點階段對這批介面的描述需要大幅修正。** 說它們「沒有執行期消費端、只剩編譯期形狀閘門」
是錯的——追查 API↔BO 雙向轉換後，實際情況是：

**1. 契約介面承載著一個靜默反射複製的正確性，每次 API 呼叫、雙向都在用。**

API 型別與 BO 型別的互轉由 [`ApiInputConverter.Convert`](../../src/Bee.Api.Core/Conversion/ApiInputConverter.cs)
承擔（名字叫 Input，實際雙向都是它）：

| 方向 | 呼叫端 | 轉換 |
|------|--------|------|
| 入站 | `JsonRpcExecutor.InvokeMethodAsync` | `LoginRequest` → `LoginArgs`（依 BO 方法的參數型別） |
| 出站 | `ApiOutputConverter.Convert` | `LoginResult` → `LoginResponse`（依 `Result`→`Response` 命名慣例） |

它**以反射逐一比對屬性名稱複製**，名稱對不上就靜默跳過——不擲例外、不警告，呼叫看起來成功
但欄位是空的。**契約介面是唯一在編譯期擋下這件事的機制**：兩邊都實作 `ILoginRequest`，
編譯器就逼它們帶同一組成員。

**2. 有 9 個介面是 `DateTimeWireGuard` 的多型判別依據**，用來辨識帶 `DataSet` / 裸 `DateTime`
的 payload 並強制 ADR-032 的 wire 不變式。這是不折不扣的執行期用途。

#### 已執行

| 項目 | 裁決 | 結果 |
|------|------|------|
| `ApiContractRegistry` | **刪除** | 型別、`ApiPayloadConverter` 的呼叫點、專屬測試、`PublicAPI` 條目全數移除 |
| 契約介面家族 | **保留 + 補 BO 側配對閘門** | 新增 `BusinessContractPairingTests`；`GetDepartmentTreeArgs` 補上 `IGetDepartmentTreeRequest` |
| 定位文件 | **寫進 README 雙語** | `Bee.Api.Contracts/README.md` / `.zh-TW.md` 新增〈這些介面為什麼存在〉一節 |

**registry 為何是刪除而非保留擴充點**：它宣稱的用途是「BO 回傳純 POCO → 轉成 API 型別」，
而**這件事 `ApiOutputConverter` 已經在做**（同樣是屬性名稱複製）。它是同一件事的第二份實作，
掛在兩次轉換都完成之後的位置上，`payload.Value` 到那裡時早已是 API 型別。
不是保留的擴充點，是被取代的重複實作——而且讓每一個 payload 白跑一次
`GetType().GetInterfaces()` 迴圈。

**BO 側閘門的實測**：69 個 `BusinessArgs` / `BusinessResult` 中只有 `GetDepartmentTreeArgs`
一個漏網（其契約介面早已存在）。補上後閘門即為綠。wire 側的 `ApiContractPairingTests` 是
2026-07-28 體檢發現 `GetDepartmentTreeRequest` 漏介面後補的——**同一個對稱性在兩側各漏過一次，
兩側現在都有閘門了。**

---

## 附錄：已確認為誤報、不需處理

留下這份清單，避免下次重跑掃描時再查一次。

| 類別 | 型別 | 存活機制 |
|------|------|---------|
| Analyzer | `Bee.Analyzers` 的 16 個 | `[DiagnosticAnalyzer]` 屬性，編譯器載入 |
| 反射註冊 | `AccessTokenValidator`、`CacheDataSourceProvider`、`FileDefineStorage`、`CacheDefineAccess`、`SessionInfoService`、`CompanyInfoService` | `BackendDefaultTypes` 型別名字串 |
| DI 註冊 | 36 個實作型別（`BusinessObjectFactory`、`ScopeResolver`、`LanguageService`、`RolePermissionService`…） | 唯一 prod 參考來自 `BeeFrameworkServiceCollectionExtensions`，消費端走介面。**這是正常的 DI 形狀，不是死碼** |
| Host 註冊 | `MySqlDialectFactory`、`OracleDialectFactory`、`PgDialectFactory` | host 自行 `DbDialectRegistry.Register`（samples/apps 只註冊 SQLite） |
| 反射派送 | `SystemExecFuncHandler.Hello` / `UpgradeTableSchema` / `TestConnection`、`FormExecFuncHandler.Hello` | `GetMethod(args.FuncId)` |
| 序列化慣例 | `DepartmentNode.ShouldSerializeChildren` | `XmlSerializer` 的 `ShouldSerialize*` 慣例 |
| 語言慣例 | `PayloadSwap.operator ==` / `!=` | 覆寫 `Equals` 的配套 |
| Client 公開 API | `ClientDefineAccess` 的 9 個 `Save*Async`、`SystemApiConnector` 的 ApiKey / Plugin 系列 | 框架對外 API surface，0 caller 保留 |
| 刻意擴充點 | `CheckPackageUpdate` / `GetPackage` 全棧（含 4 個 wire 型別） | 前輪已裁決保留 |
| 常數家族完整性 | `SysFields.ValidDate` / `InvalidDate` / `InsertUserRowId` / `UpdateUserRowId` / `UpdateTime`、`ApiHeaders.ContentType`、`PropertyCategories.Behavior` / `Action`、`TraceCategories.General` | 公開常數家族，缺項比留著更糟 |
| 列舉完整性 | `TraceLayers.All`（flags 聚合）、`DbAccessAnomalyLogLevel.Error`（等級序列中段）、`JsonRpcErrorCode.InvalidParams`（JSON-RPC 2.0 標準碼） | |
