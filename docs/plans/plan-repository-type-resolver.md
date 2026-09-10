# 計畫：抽出 `IRepositoryTypeResolver`，處理 `RepositoryFactory` 建構子的 S107

**狀態：✅ 已完成（2026-09-10）**

## 背景

2026-09-10 清 SonarCloud 的最後一筆 open issue：

- **`csharpsquid:S107`**（Constructor has 8 parameters, which is greater than the 7 authorized）
  [`src/Bee.Repository/Factories/RepositoryFactory.cs:66`](../../src/Bee.Repository/Factories/RepositoryFactory.cs)

2026-08-06 的 `/sonar-fix` 已把它記進
[`docs/.sonar-fix-state/skip.json`](../.sonar-fix-state/skip.json)，理由是「public API 簽章、
收斂成 options 物件是破壞性變更」。那個判斷**只回答了「能不能自動修」**，沒回答「該不該修」，
本 plan 補上後者。

建構子現況（8 參數，後 3 個 optional）：

```csharp
public RepositoryFactory(
    IServiceProvider services,
    IDefineAccess defineAccess,
    IDbAccessFactory dbAccessFactory,
    IDbConnectionManager connectionManager,
    IRepositoryDatabaseRouter router,
    ICacheNotifyService? cacheNotify = null,
    ICustomizeDefineReader? customizeReader = null,       // ← 2026-08-04 76c87cdd 加入
    ISessionInfoService? sessionInfoService = null)       // ← 同上
```

## 結論先講

1. **有真實接縫，不是為了壓數字硬拆。** 第 7、8 個參數連同 `ResolveFormRepositoryType` /
   `FindProgramItem` / `GetCustomizeId` 三個成員，是「progId → repository 型別（含租戶客製
   overlay）」這件事——也就是 BO 軸 [`IBoTypeResolver`](../../src/Bee.Business/IBoTypeResolver.cs)
   在 repository 軸的**對應物，被 inline 在工廠裡**。抽出去之後工廠剩 7 個參數。
2. **但任何能真正關掉 S107 的做法都是破壞性變更**，已實測（見下）：加多載關不掉。
   已由使用者決定接受，見文末「已決定」。
3. **不採用「把參數塞進 options 物件」**，即使那也能讓數字降到 7 以下——那只是搬走分數。

## 已實測的前提

### 加多載關不掉 S107，也不會改變正式環境走的路徑

`RepositoryFactory` 由 `CreateConfigurableService` 以 `ActivatorUtilities.CreateInstance(sp, type)`
建構（[`BeeFrameworkServiceCollectionExtensions.Factories.cs`](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.Factories.cs)）。
以 `Microsoft.Extensions.DependencyInjection` 10.0.0 實測兩個建構子並存時會挑哪支：

| 情境 | `ActivatorUtilities` / 容器挑中 |
|------|-------------------------------|
| 舊 8 參數（3 optional）＋ 新 6 參數 | **舊 8** |
| 舊 8 參數 ＋ 新 5 參數 | **舊 8**（容器 `AddSingleton<T>` 亦同） |
| 舊 8 `[Obsolete]` ＋ 新 5 `[ActivatorUtilitiesConstructor]` | 新 5 |
| 只有新建構子 | 新 |

兩者都挑**可滿足的最長建構子**。所以：

- 單純加多載 → 正式環境**繼續走舊的 8 參數**，新多載形同裝飾，而 S107 本來就掛在舊那支上。
- `[Obsolete]` + `[ActivatorUtilitiesConstructor]` 能讓 DI 改走新的，但舊的 8 參數還在 → **S107 照報**。

**結論：關閉 S107 ＝ 舊建構子必須消失 ＝ 原始碼與二進位層級的破壞性變更。** 沒有第三條路。

### 影響面盤點

| 項目 | 現況 |
|------|------|
| 直接呼叫建構子者 | 只有測試：`RepositoryFactoryGuardTests`（4 筆 guard + `CreateFactory`）、`ProgramItemRepositoryBindingTests.CreateFactory`、`RepositoryFactoryTests.CreateFactory`。**src / samples / apps / tools 零呼叫** |
| 正式環境 | 全走 DI 反射建構，新建構子的參數只要都已註冊就自動適配 |
| repo 內子類別 | 無（`: RepositoryFactory` 零命中）|
| `ResolveFormRepositoryType`（shipped `protected`）| repo 內零使用者 |
| 以設定檔替換工廠型別（`BackendComponents.RepositoryFactory`）| repo 內無 |
| 外部消費者 | 不可知。`RepositoryFactory` 非 sealed、有 `protected virtual CreateFormRepositoryCore`，**是刻意開放繼承的型別** |
| 版本政策 | pre-stable，minor 允許破壞性變更（`CHANGELOG` 4.x 多版有「破壞性變更」一節）|

## 為什麼說它是真實接縫

依 `.claude/rules/sonarcloud.md` 對 S3776 的判讀方式（同樣適用 S107）：**問得出第二件事叫什麼名字，
才抽出來**。

### 候選一（不採用）：前 5 個參數 → 已存在的 `RepositoryContext`

建構子的確把 `defineAccess` / `dbAccessFactory` / `connectionManager` / `router` / `cacheNotify`
原封不動組成 [`RepositoryContext`](../../src/Bee.Repository/RepositoryContext.cs)，
名字現成。但：

- 那是**參數打包**，不是職責分離——工廠照樣要這些東西，只是換個袋子裝。
- `IRepositoryContext` 不在 DI 裡（它的 doc 明寫「Built once by `RepositoryFactory` from its own
  injected services」），改吃 context 就得把它註冊進容器，等於把組裝責任搬到 Hosting。
- 這正是上一輪結論說「不算數」的那種做法。

### 候選二（採用）：租戶客製 overlay 的型別解析 → `IRepositoryTypeResolver`

工廠目前做兩件事：

| 職責 | 成員 | 需要的相依 |
|------|------|-----------|
| **建構** repository 實例 | `CreateFormRepository` / `CreateFormRepositoryCore` / `Create<T>` / `s_frameworkTypes` | context 那 5 個 + `services` |
| **解析** progId 綁定到哪個型別 | `ResolveFormRepositoryType` / `FindProgramItem` / `GetCustomizeId` / `UnloadableRepository` | `defineAccess` + **`customizeReader` + `sessionInfoService`** |

後者帶著自己的領域包袱：`ProgramSettings.xml` 缺檔容忍、租戶客製 overlay、
「customizeId 只能取自 session」的跨租戶防護、fail-fast 失敗語意（ADR-034 決策的落點）。
而且**它在 BO 軸已經是獨立型別**：

| | BO 軸 | Repository 軸（現況） |
|---|------|-------------------|
| 解析介面 | `IBoTypeResolver` | — |
| 預設實作 | `ProgramSettingsBoTypeResolver`（278 行） | inline 在 `RepositoryFactory`（275 行中約 110 行）|
| overlay | `CustomizeOverlay.FindProgramItem` | 同一支 |
| 缺檔容忍 | `catch (FileNotFoundException)` | 同，註解還寫著「Same tolerance as the business-object resolver」|
| 工廠建構子 | `BusinessObjectFactory` 吃 `IBoTypeResolver` | 吃 `customizeReader` + `sessionInfoService` |

名字現成、對照物現成。**判定：是接縫。**

### 除了 S107，還換到什麼

1. **host 能換掉解析而不必繼承工廠。** 現在想改「progId 綁定到哪個 repository」只能繼承
   `RepositoryFactory` 覆寫 `CreateFormRepositoryCore`，連同建構一起接手。之後註冊一個
   `IRepositoryTypeResolver` 即可，與 BO 軸對稱。
2. **解析邏輯可單獨測。** `ProgramItemRepositoryBindingTests` 的 9 筆測試只驗型別解析，
   卻得為了建工廠準備 `StubDbAccessFactory` / `StubConnectionManager` / `StubRouter` 三個永遠
   不會被呼叫的 stub。
3. **工廠只剩「建構」一件事**，約縮 110 行。

### 買不到的（誠實說明）

- **兩軸的 overlay 程式碼仍是兩份。** `ProgramSettingsBoTypeResolver` 在 `Bee.Business`、
  新 resolver 在 `Bee.Repository`，而 `Bee.Repository` 不參考 `Bee.Business`（方向相反）。
  真正去重得在 `Bee.Definition` 開一個「讀 `ProgramItem`（含 overlay）」的共用抽象——那是另一個
  範圍的決定，**本 plan 不做**。本 plan 只讓 repository 軸在**結構上**與 BO 軸對齊。
- **外部子類別的破壞無法避免**，只能靠 CHANGELOG 講清楚。

## 設計

### 新型別（皆在 `src/Bee.Repository/Factories/`）

**`IRepositoryTypeResolver`**（public）

```csharp
public interface IRepositoryTypeResolver
{
    Type Resolve(Guid accessToken, string progId);
}
```

- 放 `Bee.Repository` 而非 `Bee.Repository.Abstractions`：沿用 `IRepositoryContext` 的既有理由——
  實作者必然要衍生 `DataFormRepository`，本來就參考 `Bee.Repository`。
- **收 `accessToken`，不收 `customizeId`——刻意與 `IBoTypeResolver.Resolve(customizeId, progId)` 不同。**
  工廠現有註解寫明「customizeId 只能取自 session，採信呼叫端給的值就是跨租戶讀取」。
  讓 resolver 自己從 session 讀，這條防護就由**介面簽章本身**執行——根本沒有傳 customizeId 的地方。
  （BO 軸讓工廠讀 session 再傳進去，在「工廠是唯一呼叫端」下同樣安全，本 plan 不動它。）
- 這也是工廠能降到 7 參數的原因：`sessionInfoService` 隨解析一起搬走。

**`ProgramSettingsRepositoryTypeResolver`**（public sealed）

- 建構子 `(IDefineAccess defineAccess, ICustomizeDefineReader? customizeReader = null, ISessionInfoService? sessionInfoService = null)`
  —— null 語意與今天工廠的對應參數完全相同（停用 overlay、只看基底註冊表）。
- 內容：原封搬 `ResolveFormRepositoryType` / `FindProgramItem` / `GetCustomizeId` /
  `UnloadableRepository`，連同其 XML doc（fail-fast 理由、無 type cache 的理由、per-property overlay 的理由）。
- **不加 type cache**：原實作刻意不快取（doc 寫明理由：定義重載零失效機制、昂貴的一半已由
  `AssemblyLoader` 快取）。搬家不改行為。

### `RepositoryFactory` 新建構子（7 參數）

```csharp
public RepositoryFactory(
    IServiceProvider services,
    IDefineAccess defineAccess,
    IDbAccessFactory dbAccessFactory,
    IDbConnectionManager connectionManager,
    IRepositoryDatabaseRouter router,
    IRepositoryTypeResolver typeResolver,
    ICacheNotifyService? cacheNotify = null)
```

**`typeResolver` 必填，不給 optional 預設值。** 考慮過的替代：

| 做法 | 取捨 |
|------|------|
| `IRepositoryTypeResolver? typeResolver = null`，null 時內建 `new ProgramSettingsRepositoryTypeResolver(defineAccess)` | 只傳 5~6 個引數的呼叫端**原始碼相容**。但若 Hosting 漏註冊 resolver，DI 會靜默走 null 分支 → **租戶客製的 Repository 綁定整批失效、沒有任何訊號**。這正是 ADR-034 花一整節否決的「靜默退回」|
| **必填（採用）** | 漏註冊時 DI 建構失敗、啟動即紅。代價是所有直接呼叫端都要改——而那些呼叫端全在測試裡 |

> ⚠️ 必填的「響亮」目前**打了折扣**：`CreateConfigurableService` 會吞掉 `ActivatorUtilities` 的
> `InvalidOperationException`，改擲「Failed to construct IRepositoryFactory」而**丟掉原始訊息**
> （原本會指名解析不到的參數型別）。仍然會紅、只是訊息沒指到點上。已另開獨立任務處理，
> 不在本 plan 範圍。本 plan 的防線是 `BeeFrameworkServiceResolutionTests` 既有的
> `GetRequiredService<IRepositoryFactory>()` 斷言——漏註冊時它必紅。

### `ResolveFormRepositoryType`（shipped `protected`）的去留

**建議同版移除**，理由：

- 保留它就是一行轉呼叫 `_typeResolver.Resolve(...)` 的純 facade（`code-style.md`：不保留）。
- 它存在的用途是讓覆寫 `CreateFormRepositoryCore` 的子類別能重用解析；抽出後子類別改注入
  `IRepositoryTypeResolver` 即可。
- 既然建構子已經是破壞性變更，同一版一次破完，好過分兩版各破一次。

`CreateFormRepositoryCore`（`protected virtual`）與 `Context`（`protected`）**保留**——那是建構的擴充點，不屬解析。

### Hosting 註冊

比照 `IBoTypeResolver`（`BeeFrameworkServiceCollectionExtensions.cs` 約 L240）明寫依賴，
不走反射：

```csharp
services.AddSingleton<IRepositoryTypeResolver>(sp =>
    new ProgramSettingsRepositoryTypeResolver(
        sp.GetRequiredService<IDefineAccess>(),
        sp.GetRequiredService<ICustomizeDefineReader>(),
        sp.GetRequiredService<ISessionInfoService>()));
```

用 `GetRequiredService` 而非 `GetService`：正式環境 overlay 所需的兩個服務今天都已註冊
（L103、L195），寫成 required 讓「有人拿掉其中一個註冊」時啟動就紅，而不是 overlay 靜默關閉。

## 實作範圍

| 檔案 | 改動 |
|------|------|
| `src/Bee.Repository/Factories/IRepositoryTypeResolver.cs` | 新增 |
| `src/Bee.Repository/Factories/ProgramSettingsRepositoryTypeResolver.cs` | 新增（自工廠搬入） |
| `src/Bee.Repository/Factories/RepositoryFactory.cs` | 建構子 8 → 7、移除解析四成員與 `ResolveFormRepositoryType`、`CreateFormRepositoryCore` 改呼叫 `_typeResolver` |
| `src/Bee.Repository/PublicAPI.Unshipped.txt` | 新增兩型別成員；`*REMOVED*` 舊建構子與 `ResolveFormRepositoryType`（沿用 `HttpUtilities` 那次的慣例） |
| `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` | 註冊 `IRepositoryTypeResolver` |
| `src/Bee.Repository/README.md` / `README.zh-TW.md` | `Factories/` 目錄說明補上 resolver（雙語同步） |
| `tests/Bee.Repository.UnitTests/ProgramSettingsRepositoryTypeResolverTests.cs` | 新增（自 binding 測試搬入）|
| `tests/Bee.Repository.UnitTests/ProgramItemRepositoryBindingTests.cs` | 見下，改寫或刪除 |
| `tests/Bee.Repository.UnitTests/RepositoryFactoryGuardTests.cs` | helper 與 4 筆 guard 改用新建構子；補一筆 `null typeResolver` guard |
| `tests/Bee.Repository.UnitTests/RepositoryFactoryTests.cs` | `CreateFactory` 改用新建構子 |
| `docs/.sonar-fix-state/skip.json` | 移除 S107 那筆（它的 skip 理由不再成立）|
| 本 plan | 狀態列 |

**不動**：`ProgramSettingsBoTypeResolver` / `IBoTypeResolver` / `BusinessObjectFactory`（BO 軸）、
`RepositoryContext` / `IRepositoryContext`、`CreateConfigurableService`（另開任務）。

## 測試調整

`ProgramItemRepositoryBindingTests` 現有 9 筆，**全部是在驗型別解析**（第一筆另帶一個
「建出的實例 ProgId 正確」的斷言）：

| 現有測試 | 去向 |
|---------|------|
| `BoundRepository_ReturnsRegisteredType` | 解析部分 → resolver 測試；**實例建構部分留在工廠**，改用 stub resolver 驗「工廠照 resolver 給的型別建出實例、帶上 progId」|
| `EmptyRepository_FallsBackToDefault` | → resolver |
| `ProgIdNotRegistered_FallsBackToDefault` | → resolver |
| `NoRegistryFile_FallsBackToDefault` | → resolver |
| `UnloadableType_ThrowsNamingBoth` | → resolver |
| `NotDerivedFromDataFormRepository_ThrowsNamingBoth` | → resolver |
| `CustomizationDeclaresProgId_ReplacesBaseBinding` | → resolver |
| `SessionWithoutCustomizeId_UsesBaseBinding` | → resolver |
| `CustomizationSilentOnProgId_FallsBackToBase` | → resolver |

搬到 resolver 後三個工廠用 stub（`StubDbAccessFactory` / `StubConnectionManager` / `StubRouter`）
在該檔消失。原檔若只剩那一筆工廠建構測試，併入 `RepositoryFactoryGuardTests` 並刪除原檔；
動筆時依實際剩餘內容決定，**不為了保留檔名而留空殼**。

**斷言內容逐字搬、不改寫**——這 9 筆是 ADR-034 fail-fast 語意的閘門，搬家時改斷言等於換掉閘門。

新增：

- `RepositoryFactory_NullTypeResolver_ThrowsArgumentNullException`（guard）
- 工廠確實**委派**給注入的 resolver（stub resolver 回自訂型別 → 工廠建出該型別）——這是新接縫本身的閘門。

### 變異測試（沿用上一個 plan 的做法）

新接縫至少驗兩個變異各有一筆測試變紅：

| 變異 | 應變紅 |
|------|--------|
| 工廠忽略 `_typeResolver`、寫死回 `DataFormRepository` | 委派測試 |
| resolver 的 `GetCustomizeId` 固定回空字串（overlay 失效）| `CustomizationDeclaresProgId_ReplacesBaseBinding` |

後者特別要驗：**overlay 失效是本次重構最可能出的無聲回歸**，而它不會讓任何「預設路徑」的測試紅。

## 驗證

1. `dotnet build --configuration Release`（strict）。
2. `./test.sh`（全部；`Bee.Repository` 動到 → 依 `testing.md` 建議完整模式）。
3. 上表兩個變異測試。
4. 本機加 `SonarAnalyzer.CSharp` 確認 S107 消失、兩個新檔零警告，然後移除暫時參考
   （見 [`docs/repo-ops/gotchas/test-ci-release.md`](../repo-ops/gotchas/test-ci-release.md)）。
5. `./check-public-docs.sh`（README 有改）。
6. commit type 用 `refactor(repository)!:`，讓 `/dev-workflow:changelog-draft` 歸入「破壞性變更」；
   message 寫明相容性判定。push 前依 `testing.md` 問 CI 模式——觸及 `src/Bee.Repository/**`，**建議 `[all-db]`**。

## 驗收標準

- [ ] `RepositoryFactory` 只有一個 public 建構子，7 參數。
- [ ] 原 9 筆 binding 測試的斷言**逐字**存在於 resolver 測試（或工廠測試）中，全綠。
- [ ] 兩個變異各有對應測試變紅。
- [ ] `BeeFrameworkServiceResolutionTests` 全綠（DI 能建出工廠，resolver 已註冊）。
- [ ] 完整模式 CI 綠；SonarCloud open issue 歸零。
- [ ] `skip.json` 的 S107 條目已移除。

## 已決定（2026-09-10，使用者確認）

| # | 問題 | 決定 | 理由 |
|---|------|------|------|
| 1 | 接不接受破壞性變更 | **接受**，照本 plan 抽出 resolver | 接縫是真的、有 BO 軸對照；repo 內直接呼叫端全是測試；pre-stable 政策允許 |
| 2 | 舊 8 參數建構子 | **同版移除** | 保留 `[Obsolete]` 一版的話，那一版 S107 照報、需另標 Won't Fix；且依實測 DI 仍走舊建構子，除非另加 `[ActivatorUtilitiesConstructor]`——多一層機關卻換不到什麼 |
| 3 | `ResolveFormRepositoryType`（shipped `protected`）| **同版移除** | 抽出後只剩一行轉呼叫（純 facade）；與建構子一次破完 |

**被否決的方向**：不做、在 SonarCloud UI 標 Won't Fix。否決理由同上表第 1 列。

## 執行結果（2026-09-10）

三項決定照「已決定」表執行：抽出 `IRepositoryTypeResolver` / `ProgramSettingsRepositoryTypeResolver`，
`RepositoryFactory` 建構子 8 → 7，舊建構子與 `ResolveFormRepositoryType` 同版移除
（`PublicAPI.Unshipped.txt` 以 `*REMOVED*` 申報）。

### 範圍對帳

宣告的檔案全數異動，另有兩處偏離，皆在執行中當場說明：

| 偏離 | 內容 | 理由 |
|------|------|------|
| **多動一個未宣告檔** | `tests/Bee.Hosting.UnitTests/BeeFrameworkServiceResolutionTests.cs` | `AddBeeFramework_RepositoryFactory_ReceivesCustomizationDependencies` 以**反射讀工廠私有欄位名**驗客製相依有接上，事前以建構子呼叫與方法名 grep 都掃不到，全測試跑下去才紅。**改指向 resolver，不是刪除**：它守的「選用參數漏填 → 租戶客製靜默停用」風險原封跟著搬到 resolver 的建構子，改為驗「工廠持有的就是容器註冊的 resolver，且其兩個相依皆非 null」。下表 M5 證明它仍抓得到那個回歸 |
| **多一道工廠端契約檢查 + 1 筆測試** | `CreateFormRepositoryCore` 檢查 resolver 回傳型別衍生自 `DataFormRepository` | resolver 可替換之後，原本只由內建 resolver 執行的契約對自訂 resolver 就沒有東西在擋；介面 doc 宣稱的契約需要一個執行它的機制 |

> **平行路徑檢查的漏網型態**：反射字串（`GetField("_customizeReader")`）不會出現在
> 「型別名 / 方法名」的 grep 裡。搬移私有成員時，要另外以**欄位名字串**掃一次 `tests/`。

測試斷言的形狀有一處必要調整：9 筆解析測試的受測對象從工廠變成 resolver，型別斷言由
`Assert.IsType<T>(instance)` 改為 `Assert.Equal(typeof(T), type)`；**例外訊息的斷言原封搬移**。
「依解析結果建出實例並帶上 progId」那一半改由工廠端的 stub resolver 測試承擔。

### 變異測試

| # | 變異 | 變紅的測試 |
|---|------|-----------|
| M1 | 工廠忽略 resolver、寫死 `DataFormRepository` | `ResolverBindsCustomType_BuildsThatTypeWithProgId`、`ForwardsAccessTokenAndProgIdToResolver`、`ResolverReturnsNonRepositoryType_…` |
| M2 | resolver 的 `GetCustomizeId` 恆回空字串（overlay 失效）| `Resolve_CustomizationDeclaresProgId_ReplacesBaseBinding` |
| M3 | 工廠傳 `Guid.Empty` 而非呼叫端 token 給 resolver | `CreateFormRepository_ForwardsAccessTokenAndProgIdToResolver` |
| M4 | 拿掉工廠端契約檢查 | `CreateFormRepository_ResolverReturnsNonRepositoryType_ThrowsNamingProgIdAndType` |
| M5 | Hosting 註冊 resolver 時漏傳兩個客製相依 | `AddBeeFramework_RepositoryFactory_ReceivesCustomizationDependencies` |

M2、M3、M5 是本次重構**最可能出的無聲回歸**——三者都讓租戶的 Repository 覆寫失效而每個請求照樣成功，
預設路徑的測試全都看不出來。三者各有專屬測試擋下。還原後三個原始碼檔與變異前逐位元組相同。

### 驗證

- clean Release build（`Bee.Library.slnx`，`--no-incremental`）：0 警告 0 錯誤。
- `./test.sh`：首輪 17 個測試專案中 `Bee.Hosting.UnitTests` 紅 1 筆（即上表的反射測試），修正後
  於還原的原始碼上重跑 `Bee.Repository.UnitTests` 224 筆、`Bee.Hosting.UnitTests` 92 筆全綠；
  其餘專案首輪即全綠。
- `./check-public-docs.sh`：僅規則檔已列明的已知誤報。
- 本機加 `SonarAnalyzer.CSharp` 建置 `Bee.Repository`：**S107 消失**，三個改動 / 新增檔零 Sonar 警告；
  暫時參考已移除。
- `docs/.sonar-fix-state/skip.json` 的 S107 條目已移除。

### 範圍外

盤點時發現 `CreateConfigurableService` 吞掉 `ActivatorUtilities` 的原始例外（使「必填參數漏註冊」的
訊息指不到原因），已另開獨立任務，由另一個 session 處理，不含在本次 commit。
