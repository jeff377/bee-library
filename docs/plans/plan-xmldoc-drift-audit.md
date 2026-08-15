# 計畫：XML doc 漂移全 repo 盤點與修正

**狀態：✅ 已完成（2026-08-15）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `IFormCommandBuilder` + 五個 provider 實作的 `<summary>`（adr-024 漏網） | ✅ 已完成（2026-08-15） |
| 2 | 其餘 A 級實質錯誤（客製層型別計數、`CacheBootstrapper`、framework repository 計數） | ✅ 已完成（2026-08-15） |
| 3 | B 級過期敘述（`AddBeeFramework` 對 `UseBeeFramework` 的描述） | ✅ 已完成（2026-08-15） |
| 4 | 閘門：`<c>` 懸空識別字檢查腳本 + `code-style.md` 條文 | ✅ 已完成（2026-08-15） |

## 背景

2026-08-15 校鐵人賽 Day 10 文章時，查證「框架對五家資料庫的 CRUD 抽象」踩到一筆 XML doc 漂移：
`IFormCommandBuilder` 的 `<summary>` 宣稱產生 Select / Insert / Update / Delete 四種語句，
但該介面上只有三個方法。**有人照著 `<summary>` 抄，抄出了一個錯的斷言。**

XML doc 隨 NuGet 套件的 `.xml` 一起發佈、直接出現在消費端 IntelliSense，
依 [rules/public-docs.md](../../.claude/rules/public-docs.md) 它就是公開文件。
`TreatWarningsAsErrors=true` 擋得住「缺 XML doc」，**擋不住「XML doc 說的跟程式碼不一樣」**。

本次因此對 `src/**/*.cs` 的 `///` 做一次全 repo 盤點。

## 盤點方法與涵蓋範圍

掃描對象為 `src/**/*.cs` 的 `///` 註解（排除 `bin/`、`obj/`），共 **26,263 行**、991 個檔案。
不掃 `tests/`、`samples/`、`apps/`、`tools/`（不隨 NuGet 發佈），不掃 in-body `//`。

| # | 掃描類別 | 手法 | 結果 |
|---|---------|------|------|
| 1 | 成員清單與計數 | 數字詞 + 複數名詞的 regex；`A, B, C, and D` 列舉式 regex；逐筆比對實際成員 | **8 筆實質錯誤** |
| 2 | 散文中點名不存在的型別 | 抽出全部 `<c>PascalCase</c>` 共 **371 個**唯一識別字，逐一比對全 solution 原始碼 | **1 筆實質錯誤**（其餘為刻意的歷史指涉 / 外部型別 / 佔位符） |
| 3 | 被重構推翻的行為描述 | `<exception cref>` 逐筆比對實作；`no longer` / `formerly` / `only` / `never` 等斷言關鍵字 | **1 筆過期敘述** |
| 4 | 仍在描述舊架構的段落 | 逐份掃 `docs/adr/` 的「移除 / 退役 / 不再 / 改走」條目，回頭 grep 相關型別的 XML doc | **adr-024 為唯一漏網**（見階段 1） |

## 盤點結果

### A 級：實質錯誤（讀者照做會得到錯的事實）

#### A1–A6 — `IFormCommandBuilder` 家族宣稱四種語句，實際只有三個方法

根因：[adr-024](../adr/adr-024-dataform-save-dataadapter.md) 決策 D3 —— 存檔改走 `DataAdapter` 後，
逐列 `InsertCommandBuilder` / `UpdateCommandBuilder` 已從 `src/Bee.Db/Dml` 移除，
INSERT / UPDATE / DELETE 三句改由 [TableSchemaCommandBuilder.cs](../../src/Bee.Db/Dml/TableSchemaCommandBuilder.cs) 產生。
**程式碼改了、XML doc 沒跟上。**

介面實際成員：`BuildSelect` / `BuildCount` / `BuildDelete`（三個）。
各實作的 `<summary>` 除了沿用四動詞清單，還多錯一層：宣稱「**all four methods** delegate to
the dialect-agnostic cores」。另外六份 `<summary>` **全都漏掉 `BuildCount`**。

| # | 檔案:行號 | XML doc 原文（節錄） | 判定 |
|---|----------|--------------------|------|
| A1 | [IFormCommandBuilder.cs:7](../../src/Bee.Db/Dml/IFormCommandBuilder.cs) | `generates Select, Insert, Update, and Delete statements` | 錯：無 Insert/Update；漏 Count |
| A2 | [SqlFormCommandBuilder.cs:11](../../src/Bee.Db/Providers/SqlServer/SqlFormCommandBuilder.cs) | `generating Select, Insert, Update, and Delete statements` | 同上 |
| A3 | [PgFormCommandBuilder.cs:11](../../src/Bee.Db/Providers/PostgreSql/PgFormCommandBuilder.cs) | `generating Select, Insert, Update, and Delete statements` | 同上 |
| A4 | [SqliteFormCommandBuilder.cs:11-14](../../src/Bee.Db/Providers/Sqlite/SqliteFormCommandBuilder.cs) | `…Select, Insert, Update, and Delete statements…all four methods delegate to…` | 同上 + 方法數錯 |
| A5 | [MySqlFormCommandBuilder.cs:10-16](../../src/Bee.Db/Providers/MySql/MySqlFormCommandBuilder.cs) | `…SELECT, INSERT, UPDATE, and DELETE statements…all four methods delegate to…` | 同上 + 方法數錯 |
| A6 | [OracleFormCommandBuilder.cs:10-16](../../src/Bee.Db/Providers/Oracle/OracleFormCommandBuilder.cs) | `…SELECT, INSERT, UPDATE, and DELETE statements…all four methods delegate to…` | 同上 + 方法數錯 |

**建議修法**：四動詞清單改為 `Select, Count, and Delete`；`all four methods` 改為 `all three methods`。
各實作原有的 dialect 說明（backtick quoting、`:` bind prefix、雙引號 quoting）皆屬正確，保留。

#### A7 — `CustomizeDefineReader` 宣稱四種客製型別，實際五種

- **檔案**：[CustomizeDefineReader.cs:10](../../src/Bee.ObjectCaching/CustomizeDefineReader.cs)
- **原文**：`Default ICustomizeDefineReader: reads the four customizable types from the per-customization-code override containers…`
- **實際**：該類別實作五個 getter —— `GetCustomizeLanguage` / `GetCustomizeProgramSettings` /
  `GetCustomizeMenuSettings` / `GetCustomizePluginSettings` / `GetCustomizeFormLayout`。
  介面 [ICustomizeDefineReader.cs:8](../../src/Bee.Definition/Storage/ICustomizeDefineReader.cs)
  與 [CustomizeOnlyPathOptions.cs:3](../../src/Bee.Definition/CustomizeOnlyPathOptions.cs) 都正確寫「five」。
- **根因**：`PluginSettings` 於 commit `d9c189fa` 加入，此處計數未同步。
- **建議修法**：`four` → `five`。

#### A8 — `CustomizeOnlyStorage` 同一檔內四／五自相矛盾

- **檔案**：[CustomizeOnlyStorage.cs:170](../../src/Bee.Definition/Storage/CustomizeOnlyStorage.cs)
- **原文**：`Only the four customizable types report a signal…`
- **實際**：同檔 `GetChangeSource` 的 switch 有五個 case（ProgramSettings / MenuSettings /
  PluginSettings / FormLayout / Language）。且**同一個檔案**的類別 `<summary>`（:14）與
  `<remarks>`（:18）都寫「five customizable types」、「the five supported getters」。
- **根因**：與 A7 同源。
- **建議修法**：`four` → `five`。

#### A9 — `CacheInfo.Initialize` 指向已不存在的 `CacheBootstrapper`

- **檔案**：[CacheInfo.cs:38](../../src/Bee.ObjectCaching/CacheInfo.cs)
- **原文**：`Called by <c>CacheBootstrapper</c> (registered by <c>AddBeeFramework</c>) after settings are loaded.`
- **實際**：全 solution 無 `CacheBootstrapper` 此型別。唯一的 production 呼叫端是
  [BeeFrameworkServiceCollectionExtensions.cs:77](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs)
  —— `AddBeeFramework` **直接**呼叫 `CacheInfo.Initialize(configuration)`，中間沒有 bootstrapper。
- **根因**：靜態 facade 拆除期間（`db8194ce` 一帶）該型別被移除，散文指涉未清。
  **`<c>` 不受編譯期 cref 解析保護**，這正是它能存活至今的原因（見〈閘門評估〉）。
- **實際修法**：改為 `Called directly by <c>AddBeeFramework</c> after settings are loaded.`

  > **執行時修正了原訂做法**：原本打算換成 `<see cref>` 以納入編譯期把關，實作時發現
  > **相依方向不允許** —— `Bee.ObjectCaching` 只參考 `Bee.Definition` 與
  > `Bee.Repository.Abstractions`，看不到上層的 `Bee.Hosting`，cref 必然解析不到。
  > 因此保留 `<c>`，只拿掉指空的型別名。**這正是「跨組件向上指涉」這個合法 `<c>` 用例的來源**，
  > 已納入下方檢查腳本的說明與 `code-style.md` 條文。

#### A10 — `RepositoryBase` 宣稱九個 framework repository，實際十一個

- **檔案**：[RepositoryBase.cs:8](../../src/Bee.Repository/RepositoryBase.cs)
- **原文**：`…so the nine framework repositories no longer each repeat the same wiring.`
- **實際**：[RepositoryFactory.cs:41-51](../../src/Bee.Repository/Factories/RepositoryFactory.cs)
  的 framework 軸映射表有 **11** 筆：System 九個 + `IAuditLogRepository` / `IAuditLogWriteRepository`。
  稽核軸的兩個 repository（`e6d0e2ff`）晚於 `RepositoryBase`（`3589c4c8`）加入。
- **建議修法**：**刪掉數字**，改為 `so framework repositories no longer each repeat the same wiring`。
  依 [single-source.md](../../.claude/rules/single-source.md)「清點數字 → 不寫（必漂且對讀者無用）」，
  改成 11 只是把下一次漂移往後推。

### B 級：過期敘述（當時為真，現已誤導）

#### B1 — `AddBeeFramework` 宣稱 `UseBeeFramework` 不做任何事，兩份公開 doc 互相矛盾

- **檔案**：[BeeFrameworkServiceCollectionExtensions.cs:41-46](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs)
- **原文**：`app.UseBeeFramework() remains available as an ASP.NET Core integration extension point
  but no longer performs any bootstrap work after Phase 7 removed the transitional
  DbConnectionManager static shim…`
- **實際**：[BeeFrameworkApplicationBuilderExtensions.cs:16-29](../../src/Bee.Api.AspNetCore/BeeFrameworkApplicationBuilderExtensions.cs)
  現在會執行 `WarnWhenApiKeyGateIsNotInForce`，且**它自己的 `<summary>` 寫的是**
  `Activates host-side framework startup checks`。兩份公開 XML doc 對同一個方法的描述已經打架：
  一份說「什麼都不做」、一份說「執行啟動檢查」。
- **判定**：B 級而非 A 級 —— 「bootstrap work」嚴格說仍為真（它不做 bootstrap，只做檢查），
  但消費端在 IntelliSense 讀到的是「這個方法是空的擴充點」，會據此決定不呼叫它，因而漏掉 API key 警示。
- **建議修法**：把「no longer performs any bootstrap work」改為指出它現在承載 host-side 啟動檢查，
  並保留「Phase 7 移除 `DbConnectionManager` 靜態 shim」的歷史說明（那部分正確且有價值）。

### C 級：查過但判定為非漂移（記錄下來，避免下次重掃）

以下是掃描命中、逐筆判讀後**確認無誤**的項目。留檔的目的是讓下一輪盤點不必重走：

| 項目 | 判讀 |
|------|------|
| `<exception cref>` 全數 19 筆「本檔未 throw」疑點 | **全部合法**。多為介面契約（實作端才 throw），或委派給 callee（`ctx.Router.Resolve`、`rowView.Row.GetFieldValue<T>`、`DbTypeConverter.ToFieldDbType`、`ProgramSettingsFormat.EnsureCurrentFormat`、`settings.EnsureValid()`）。 |
| `<c>ItemsForSerialization</c>`、`<c>SafeTypelessFormatter</c>`、`<c>NumberFormatPresets</c>` | **刻意的歷史指涉**（原文即寫 "used to live" / "which is gone" / "the former"）。不是指空。 |
| `<c>IDescriptionSyncCommandBuilder</c>` | **前瞻建議**（"When description persistence is added to other dialects, abstract this via…"），非現況描述。 |
| `<c>TypelessFormatter</c>` | MessagePack 套件的外部型別，非本 repo 型別。 |
| adr-036 / adr-037（MessagePack 標註全移除、`[Union]` 退役、`BEE4003` 退役、contractless 不再是承載機制） | `src/**` XML doc **零殘留**。該批清理已於 `92758cfa` 落地。 |
| adr-020 後記（`Bee.UI.Maui` / `DynamicGrid` 已移除） | 零殘留。`Bee.Web.Blazor.Server` 的 `DynamicGrid` 是**另一個現存型別**，非漏網。 |
| analyzer 診斷編號（`BEE1001`–`BEE4006`） | XML doc 提及的 19 個 ID 與程式碼宣告的 19 個**完全一致**。 |
| 其他數字斷言 | 逐一驗證為真：`DecimalsSource` 四個成員、`PermissionAction` CRUD 四動詞、`DbScope` 三個成員、`CustomizeOnlyPathOptions` 五個 override、`ICacheNotifyReader` 五種 dialect、`DbAccess.Update` 三個 per-row command、`TableSchemaCommandBuilder` 三個 command spec、`SingleFormMode` 三個狀態、`ApiAuthorizationValidator` 點名的兩個方法。 |

## 修正批次

| 階段 | 檔案 | 動作 |
|------|------|------|
| 1 | A1–A6 六個檔案 | 四動詞清單 → `Select, Count, and Delete`；`all four methods` → `all three methods` |
| 2 | A7、A8、A9、A10 四個檔案 | 依上述逐筆建議修法 |
| 3 | B1 一個檔案 | 重寫該句，保留 Phase 7 歷史說明 |
| 4 | 新增檢查腳本 + `code-style.md` 條文 | 見下節 |

每階段結束跑 `dotnet build --configuration Release`（`TreatWarningsAsErrors=true`）。
本次僅動 `///` 內容、不動任何簽章，因此不影響 `PublicAPI.*.txt`。

## 閘門評估：這件事能不能變成可重複的閘門？

依 [single-source.md](../../.claude/rules/single-source.md) 的精神 —— 靠「記得一起改」不會成立
—— 這題必須正面回答。結論分三段，**其中兩段做得到、一段做不到**。

> **執行結果**：以下三項行動皆已落地 —— `check-xmldoc-refs.sh` 已建立並實測，
> `~/.claude/rules/code-style.md` 的兩條條文已寫入。

### 做得到（一）：把 `<c>` 換成 `<see cref>`，即刻納入編譯期把關

**這是本次最有價值的發現。** `src/Directory.Build.props` 同時開了 `GenerateDocumentationFile`
與 `TreatWarningsAsErrors=true`，因此 **`<see cref>` 指向不存在的型別會直接編譯失敗**。

已實測驗證（在 `SysInfo.cs` 注入一個假 cref 後 build，隨即還原）：

```
error CS1574: XML 註解有無法解析的 cref 屬性 'NoSuchTypeXyz'
```

也就是說，A9（`CacheBootstrapper`）**若當初寫成 `<see cref>` 就根本進不了版控**。
散文用反引號 `<c>` 寫型別名，等於主動放棄一道已經存在、且完全免費的閘門。

**行動**：在 `~/.claude/rules/code-style.md` 的 XML doc 段落加一條 ——
「散文中提到**本 solution 內**的型別或成員時一律用 `<see cref>`，不用 `<c>`；
`<c>` 只保留給非型別的字面值（SQL 片段、設定值、外部套件型別、刻意指涉的已移除型別）。」

### 做得到（二）：`<c>` 懸空識別字檢查腳本

留給那些**確實該用 `<c>`** 的場合（外部型別、歷史指涉）一道守門。做法就是本次盤點用的手法：

1. 抽出 `src/**/*.cs` 的 `///` 行中所有 `<c>PascalCase</c>`；
2. 逐一比對是否出現在 solution 任一 `.cs` 的非註解行；
3. 命中不到者報出，扣掉 allowlist。

本次實測：371 個唯一識別字 → 25 個未命中 → 扣掉 SQL 關鍵字（`ALL_TABLES`、`SQL_MODE`…）、
外部型別（`AsyncLocal`、`InternalsVisibleTo`、`FormatterNotRegisteredException`…）與
佔位符（`Cxxx`、`SaveX`、`IXxxRepository`）後，**只剩 6 個需人工判讀、其中 1 個是真漂移**。
訊噪比可接受，且 allowlist 只需維護數十筆。

已實作為 [check-xmldoc-refs.sh](../../check-xmldoc-refs.sh)（比照既有的
[check-public-docs.sh](../../check-public-docs.sh)），而非 analyzer ——
analyzer 拿不到跨專案的全 solution 符號表，腳本拿得到。

**雙向實測**：修完 A9 後跑，乾淨通過（exit 0）；注入一個假的 `<c>GhostBootstrapperXyz</c>`
後跑，正確報出檔案:行號並 exit 1。allowlist 分四類註記（外部型別 / 刻意的歷史指涉 /
前瞻建議的假想型別 / SQL 關鍵字），**新增必須註明歸類**，否則清單會退化成消音器。

### 做不到：「summary 宣稱的方法數 vs 實際公開方法數」的 analyzer

**明確做不到，理由如下，不留「未來可考慮」。**

1. **宣稱藏在自然語言裡，不是結構化欄位。** 本次的六筆是
   `all four methods`、`generates Select, Insert, Update, and Delete statements`、
   `the nine framework repositories`、`reads the four customizable types` —— 四種完全不同的句法。
   要靠 regex 抓，就得窮舉英文的計數表達方式。
2. **就算抓到數字，也對不到「該數什麼」。** `the three per-row commands`（`DbAccess.Update`）
   數的是 Insert/Update/Delete 三種語句、不是該類別的公開方法數；
   `the nine framework repositories` 數的是**另一個檔案**裡的映射表長度。
   數字與被數對象之間沒有任何機械可循的連結。
3. **偽陽性成本高於收益。** 本次掃描中，`the two layers`、`three failure modes`、
   `two distinct escapes` 這類設計散文遠多於真正的成員計數。一個會在每次 build 對這些狂叫的
   analyzer，最終只會被 `#pragma` 或 allowlist 淹沒 —— 那就退化成沒有閘門。
4. **A 級以外的類別根本不可判定。** 「說會 throw X 但現在回傳預設值」「說每次重算但已加快取」
   這種語意漂移，等價於要求機器判斷散文與實作是否同義。沒有任何靜態分析做得到。

**因此對第 1 類（成員清單與計數）的正解不是閘門，而是編輯守則：不寫計數。**
`the nine framework repositories` 的正確修法是刪掉 `nine`（見 A10），
不是把它改成 `eleven`。這條已在 `single-source.md` 有明文，本次把它延伸到 XML doc：

**行動**：在 `~/.claude/rules/code-style.md` 的 XML doc 段落加一條 ——
「XML doc **不寫程式碼構件的清點數字**（方法數、型別數、實作數）。要列就列名稱
（`Select, Count, and Delete`），名稱錯了讀者看得出來，數字錯了看不出來。」

### 閘門評估小結

| 漂移類別 | 可否機械把關 | 手段 |
|---------|------------|------|
| 指向不存在的型別（`<see cref>`） | ✅ 已有 | 編譯期 CS1574 → error（已實測） |
| 指向不存在的型別（散文 `<c>`） | ✅ 可建 | `check-xmldoc-refs.sh` + allowlist；**更好的是改用 `<see cref>` 讓第一列接手** |
| 成員清單與計數 | ❌ 做不到 | 改以編輯守則消除來源：不寫計數，改列名稱 |
| 被重構推翻的行為描述 | ❌ 做不到 | 只能靠 ADR 落地時的人工巡檢（本次第 4 類掃法可重複使用） |

## 相關文件

- [adr-024：DataForm 持久化改走 DataTable 級 DataAdapter](../adr/adr-024-dataform-save-dataadapter.md) —— A1–A6 的根因
- [rules/public-docs.md](../../.claude/rules/public-docs.md) —— XML doc 屬公開文件的依據
- [rules/single-source.md](../../.claude/rules/single-source.md) —— 「清點數字不寫」的通則
