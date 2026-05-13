# 計畫：Phase 6 — 移除 BackendInfo 空殼類別 + 殘留引用清理

**狀態：✅ 已完成（2026-05-13）**

> 本文件為主計畫 [plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) 的 **Phase 6** sub-plan，獨立可 ship。在 Phase 5（測試基礎設施重寫）完成後執行；Phase 7（DbConnectionManager 靜態 facade 移除）為下一階段獨立工作。

## 背景

Phase 5 完成後（commit `0d1076d3`，2026-05-13），`BackendInfo.cs` 已縮減為**無成員的靜態類別**，所有過往承載的服務（`IDefineAccess` / `ISessionInfoService` / 各加密金鑰 / `LogOptions` / `LogWriter`）已透過 DI ctor 注入。剩下的工作：

1. 刪除 `BackendInfo.cs` 本身
2. 清掃 src / tests / docs 內**過時的 BackendInfo 文字引用**（docstring `<see cref>`、註解、文件範例）

Phase 7（`DbConnectionManager` 靜態 facade 移除）獨立計畫；Phase 6 不碰任何 DbConnectionManager 相關程式碼。

## 與主計畫敘述的差異（重要）

主計畫 §「Phase 6」原描述為：

> 刪除 `BackendInfo.cs` 與 `BackendConfiguration.cs`

執行前盤點發現 **`BackendConfiguration.cs` 並非空殼**，仍承載 4 個 XML 反序列化欄位：

| 欄位 | Production 消費點 | 是否仍 active |
|------|------------------|---------------|
| `SecurityKeySettings` | `BeeFrameworkServiceCollectionExtensions.AddBeeFramework` → `DecryptSecurityKeys` | ✅ 是 |
| `Components` (`BackendComponents`) | `AddBeeFramework`（讀取每個 service 的 type name）、`CacheInfo.Initialize` | ✅ 是 |
| `LogOptions` | 0 caller（Phase 5 已改 DI ctor 注入，欄位殘留但無讀取） | ⚠️ 死碼但 XML fixture 仍宣告節點 |
| `ApiKey` | 0 caller（純 XML round-trip 序列化/setter 測試） | ⚠️ 死碼且 XML fixture 無節點 |

`BackendConfiguration` 仍是 `SystemSettings.xml` 的 `<BackendConfiguration>` 節點反序列化目標，由 `AddBeeFramework(BackendConfiguration, PathOptions)` 拿來抽取設定。**它有實質角色，不該硬刪**。把它解構為多個 `IOptions<T>`（`IOptions<SecurityKeySettings>` + `IOptions<BackendComponents>` + ...）是另一個量級的工程（要改 `AddBeeFramework` 簽章 + 對應 host / test bootstrap），不在 Phase 6 範圍。

故 Phase 6 採**方案 A（保留 BackendConfiguration）**，僅刪除 `BackendInfo.cs` 空殼 + 殘留文字引用 + **順手清掉 `BackendConfiguration.ApiKey` 死欄位**（見下方 §「BackendConfiguration.ApiKey 死欄位清理」）。執行完畢時同步**修訂主計畫 §「Phase 6」描述**對齊現實。

`BackendConfiguration.LogOptions` 是另一個 0 caller 死欄位，但因 `tests/Define/SystemSettings.xml` 有 `<LogOptions>` 節點且外部部署 XML 可能也有，本 phase **不動**，留待將來重新接 DI 時一併處理。

## 現況盤點

### `BackendInfo.cs` 本身

```csharp
// src/Bee.Definition/BackendInfo.cs（13 行）
public static class BackendInfo { }
```

完全空殼，刪除後唯一可能影響的：
- 任何外部編譯依賴 `BackendInfo` 符號（無外部消費者，符合主計畫不變條件）
- IDE 跳轉到 `BackendInfo` 的引用點

### 殘留 `BackendInfo` 文字引用（active src/tests/docs）

| 類型 | 檔案 | 行 | 形態 | 處置 |
|------|------|----|------|------|
| src docstring | `src/Bee.Api.Client/ApiClientInfo.cs` | 5 | `<see cref="Bee.Definition.BackendInfo"/>` | 改寫為純文字（不指向已刪型別）或移除 |
| src 註解 | `src/Bee.Business/System/SystemBusinessObject.cs` | 70-71 | `// Phase 3 backs this with BackendInfo statics; Phase 4 swaps for real DI scope.` | 改寫為現況描述（不再提及 BackendInfo） |
| src docstring | `src/Bee.Definition/IDatabaseSettingsProvider.cs` | 10 | `<c>BackendInfo.GetDatabaseItem</c> / <c>BackendInfo.ValidateDatabaseSettings</c>` | 改寫為「集中管理 `DatabaseSettings.Items` 與查找邏輯」（去除過時的 Service Locator 對照） |
| test 註解 | `tests/Bee.Business.UnitTests/StaticApiEncryptionKeyProviderTests.cs` | 8 | `不再操弄 BackendInfo 靜態` | 改寫為「直接構造，不再走任何 process-wide static」 |
| doc | `docs/development-cookbook.md` | 247 | `從 BackendInfo.DefineAccess 取得 FormSchema` | 改為 `IDefineAccess`（DI ctor 注入） |
| doc | `docs/development-cookbook.zh-TW.md` | 246 | 同上 | 同步 |
| doc | `docs/development-constraints.md` | 8-28（「Initialization Order Constraints」整節） | 描述 7 步靜態初始化 | 整節改寫為 DI 模型（`SystemSettingsLoader.Load` → `SysInfo.Initialize` → `AddBeeFramework`） |
| doc | `docs/development-constraints.zh-TW.md` | 同上 | 同上 | 同步 |
| doc | `docs/database-settings-guide.md` | 109, 208, 213, 268-277, 300, 307, 382, 397, 411（13 處） | 大量 `BackendInfo.GetDatabaseItem` / `BackendInfo.DefineAccess.X` / `BackendInfo.ConfigEncryptionKey` / `BackendInfo.DefinePath` 範例 | 全面 sweep 為 DI 等價：`IDatabaseSettingsProvider.GetItem` / `IDefineAccess.X` / DI-injected `byte[] configEncryptionKey` / `PathOptions.DefinePath` |
| doc | `docs/database-settings-guide.zh-TW.md` | 同上 | 同上 | 同步 |

### 不處理的引用（明確排除）

| 類型 | 範例 | 排除原因 |
|------|------|----------|
| ADR-003 | `docs/adr/adr-003-static-service-locator.md` 全篇 | ADR 是歷史決策紀錄；標 "Superseded by ADR-XXX" 比改內容好。本 phase 不開新 ADR，僅在頂部加 superseded 註記指向主計畫 |
| ADR-010 | `docs/adr/adr-010-logical-database-category.md` 2 處 BackendInfo.GetDatabaseItem 範例 | 同上；ADR 凍結，可在頂部加註「BackendInfo 已於 v5.0 移除，等價呼叫見 `IDatabaseSettingsProvider`」 |
| `docs/archive/plan-*.md` | 4 個歷史 plan | 已封存，不更動 |
| `docs/plans/plan-backendinfo-*.md` | 主計畫 + 各 phase sub-plan | 歷史紀錄，保留原描述；主計畫 §「Phase 6」描述同步修訂（見下方 §「主計畫修訂」） |
| `docs/.sonar-fix-state/skip.json` | sonar-fix state 檔 | 工具產物，不手改 |

## 目標

1. 刪除 `src/Bee.Definition/BackendInfo.cs`
2. 清掃 4 處 src/tests 內過時 `BackendInfo` 文字引用（docstring / 註解）
3. 同步更新 4 份 active 文件（`development-cookbook` / `development-constraints` / `database-settings-guide`，含對應 `.zh-TW.md`）
4. 為 ADR-003 / ADR-010 加上 superseded / 等價對照頂部註記（不改 ADR 主體）
5. 主計畫 sub-plan 進度表標 Phase 6 ✅；§「Phase 6」描述同步修訂對齊現實
6. CI 與本機 build + test 通過

## 非目標（本 phase 不做）

- **不刪 `BackendConfiguration.cs`** —— 它是 `SystemSettings.xml` 反序列化的 root，仍由 `AddBeeFramework` active 消費（見 §「與主計畫敘述的差異」）
- **不解構 `BackendConfiguration` 為 `IOptions<T>`** —— 那會改 `AddBeeFramework` 簽章與所有 host / test bootstrap，超出 Phase 6 範圍，必要時另開 sub-plan
- **不碰 `DbConnectionManager` 靜態 facade** —— 是 Phase 7 範圍
- **不開新 ADR**（如「採用 DI 取代 Service Locator」）—— 主計畫已是設計權威文，新增 ADR 是重複；ADR-003/010 加 superseded 註記即可
- **不發 v5.0 release notes** —— 那是 Phase 7 結案後的整體里程碑

## BackendConfiguration.ApiKey 死欄位清理

`BackendConfiguration.ApiKey` 確認為孤兒欄位 —— production 0 caller，且設計上預期承擔的「server 端 API Key 比對」邏輯根本沒實作（[`ApiAuthorizationValidator.cs:44`](../../src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs#L44) 只檢查 client header 「非空」）。與 3 個 active 同名 `ApiKey`（`ApiHeaders.ApiKey` / `ApiClientInfo.ApiKey` / `ApiAuthorizationContext.ApiKey`）並存徒增語意混淆。納入本 phase 一併清除。

**改動**：

| 檔案 | 改動 |
|------|------|
| `src/Bee.Definition/Settings/SystemSettings/BackendConfiguration.cs:25-29` | 移除 `ApiKey` 屬性 + 對應 `[Category("API")] / [Description] / [DefaultValue("")]` attributes |
| `tests/Bee.ObjectCaching.UnitTests/LocalDefineAccessSaveTests.cs:35, 41` | 改用其他 active 欄位作為「可識別字串」驗證寫檔內容（候選：`SecurityKeySettings.MasterKeySource.Value = "saved_id"`） |
| `tests/Bee.Definition.UnitTests/DefinitionSerializationTests.cs:103` | 直接刪該行（XML round-trip 主體未變），或改設其他欄位 |
| `tests/Bee.Definition.UnitTests/DtoSerializationTests.cs:324, 333` | 改用其他 active 欄位 + 對應 assertion |

**未來副議題（不在本 phase）**：server 端 API Key 比對邏輯目前缺失，client 隨便傳非空字串都過。要不要補比對邏輯獨立議題；若補，會走 DI 注入（`IOptions<ApiAuthorizationOptions>` 等），不會回頭使用 `BackendConfiguration.ApiKey`。

## 不順手做的死欄位（明確排除）

`BackendConfiguration.LogOptions` 亦為 0 caller，但本 phase **不刪**，原因：

- `tests/Define/SystemSettings.xml` 第 14-21 行有 `<LogOptions>` 節點，刪欄位要同步刪 XML 節點
- 外部部署環境的 `SystemSettings.xml` 可能也有 `<LogOptions>` 節點。`XmlCodec` 對未知節點的容錯行為未驗證，潛在破壞性變更風險
- `LogOptions` 是日誌設定的設計入口，未來重新接 DI 機會比 `ApiKey` 高
- 省下的程式碼負擔極小（單欄位 + DbAccessAnomalyLogOptions 一個子型別），不值得承擔相容風險

留待將來真要把 LogOptions 接回 DI 時一起處理。

## 設計

### 1. `src/Bee.Definition/BackendInfo.cs` 刪除

直接 `git rm`，無 transitional shim、無 `[Obsolete]` 標記（檔案內無成員，標 `[Obsolete]` 也無意義）。

### 2. 文字引用 sweep 規則

| 模式 | 改寫範例 |
|------|----------|
| `<see cref="Bee.Definition.BackendInfo"/>` | 拆解為純文字描述（如「the backend service registration entry point，see `BeeFrameworkServiceCollectionExtensions.AddBeeFramework`」），或直接移除（如 ApiClientInfo docstring 的「Counterpart of BackendInfo」可以改為「provides client-side runtime info paralleling backend's `AddBeeFramework` registration」） |
| `BackendInfo.DefineAccess.X` | `IDefineAccess.X`（DI ctor 注入） |
| `BackendInfo.GetDatabaseItem(databaseId)` | `IDatabaseSettingsProvider.GetItem(databaseId)`（DI ctor 注入） |
| `BackendInfo.ConfigEncryptionKey` | DI-injected `byte[] configEncryptionKey` ctor 參數（如 `LocalDefineAccess(IDefineStorage, PathOptions, ICacheContainer, byte[] configEncryptionKey)`） |
| `BackendInfo.DefinePath` | `PathOptions.DefinePath`（DI ctor 注入） |
| `BackendInfo.Initialize(settings.BackendConfiguration)` | `services.AddBeeFramework(settings.BackendConfiguration, paths)` |

### 3. `docs/development-constraints.md` 重寫範圍

「Initialization Order Constraints」整節（含對應 zh-TW 版）刪除舊 7 步初始化清單，改為簡短描述 DI 初始化流程：

```text
1. SystemSettingsLoader.Load(paths) → SystemSettings DTO
2. SysInfo.Initialize(settings.CommonConfiguration) → process-wide debug flag / payload options
3. services.AddBeeFramework(settings.BackendConfiguration, paths) → 註冊 framework 服務
4. services.BuildServiceProvider() + app.UseBeeFramework()（ASP.NET 環境）
```

並引用 `development-cookbook.md` § "Framework Initialization Order" 作為權威來源（避免兩份文件重複描述）。

### 4. `docs/database-settings-guide.md` sweep

13 處引用全面改寫為 DI 等價。範例：

**Before**：
```csharp
DatabaseSettings dbSettings = BackendInfo.DefineAccess.GetDatabaseSettings();
DatabaseItem item = BackendInfo.GetDatabaseItem("company_main");
```

**After**：
```csharp
// 透過 DI ctor 注入 IDefineAccess / IDatabaseSettingsProvider
public class MyService(IDefineAccess defineAccess, IDatabaseSettingsProvider dbSettings)
{
    public void Demo()
    {
        DatabaseSettings settings = defineAccess.GetDatabaseSettings();
        DatabaseItem item = dbSettings.GetItem("company_main");
    }
}
```

`BackendInfo.ConfigEncryptionKey` / `BackendInfo.DefinePath` 在文件中的範例改為「DI-injected ctor 參數」描述，並 link `BeeFrameworkServiceCollectionExtensions.AddBeeFramework`。

### 5. ADR 頂部 superseded 註記

**ADR-003**（Static Service Locator）：在標題下加：

```markdown
> **狀態**：Superseded（v5.0，2026-05）—— 框架已全面改為 DI ctor 注入。本 ADR 保留作為歷史決策紀錄，遷移細節見 [plan-backendinfo-to-di-migration.md](../plans/plan-backendinfo-to-di-migration.md)。
```

**ADR-010**（Logical Database Category）：在標題下加：

```markdown
> **註記**：本文中 `BackendInfo.GetDatabaseItem(databaseId)` 範例在 v5.0 後等價為 `IDatabaseSettingsProvider.GetItem(databaseId)`（DI ctor 注入）。設計理念不變。
```

不改 ADR 主體段落。

### 6. 主計畫修訂

`docs/plans/plan-backendinfo-to-di-migration.md`：

- §「Sub-plan 進度」表格的 Phase 6 列加 sub-plan 連結（`[plan-backendinfo-di-phase6-backendinfo-deletion.md](plan-backendinfo-di-phase6-backendinfo-deletion.md)`）、狀態改為 ✅ 已完成（執行落地日期）
- §「Phase 6：移除 BackendInfo 空殼」段落改寫，澄清 `BackendConfiguration` 保留作為 XML 反序列化 root；範圍聚焦 `BackendInfo.cs` 刪除 + 文字引用 sweep
- §「成功標準」第 1 條（`grep -r "BackendInfo\." src/ tests/` 結果為 0）保留 —— Phase 6 完成後仍為 0
- §「成功標準」最後一條（`docs/architecture-overview.md` 中無 `BackendInfo` 字樣）擴大為「`docs/` active 文件中無 `BackendInfo` 字樣（除 `docs/plans/` 歷史紀錄與 `docs/adr/` 加註記引用）」

## 範圍邊界（明確列示）

### 在範圍內
- `src/Bee.Definition/BackendInfo.cs` 刪除
- `BackendConfiguration.ApiKey` 屬性刪除 + 3 個測試調整（見 §「BackendConfiguration.ApiKey 死欄位清理」）
- 4 處 src/tests 文字引用 sweep
- 4 份 active 文件（cookbook、constraints、database-settings-guide，含 zh-TW）內 BackendInfo 範例 sweep
- ADR-003 / ADR-010 加頂部 superseded / 等價對照註記
- 主計畫 sub-plan 進度表更新與 §「Phase 6」描述修訂

### 範圍外（明確不做）
- `BackendConfiguration.cs` 刪除（仍是 XML 反序列化 root）
- `BackendConfiguration` 解構為 `IOptions<T>`
- `BackendConfiguration.LogOptions` 死欄位（見 §「不順手做的死欄位」）
- `BackendComponents` / `BackendDefaultTypes` 任何改動
- 補 server 端 API Key 比對邏輯（獨立議題，見 §「BackendConfiguration.ApiKey 死欄位清理」）
- `DbConnectionManager` 靜態 facade（Phase 7）
- `SysInfo` / `ApiClientInfo` / `CacheInfo.Provider` 等其他 registry-style 靜態（Phase 7 後仍保留，符合主計畫設計）
- v5.0 release notes 撰寫（待 Phase 7 完成後）

## 執行步驟

### Step 1：刪除 `BackendInfo.cs`
```bash
git rm src/Bee.Definition/BackendInfo.cs
dotnet build --configuration Release
```
預期 build 通過（無 production 引用）。若觸發任何編譯錯誤，回到 §「現況盤點」表補登 missing 引用。

### Step 2a：刪除 `BackendConfiguration.ApiKey`

1. 移除 [`src/Bee.Definition/Settings/SystemSettings/BackendConfiguration.cs:25-29`](../../src/Bee.Definition/Settings/SystemSettings/BackendConfiguration.cs#L25) 的 `ApiKey` 屬性 + 對應 `[Category("API")] / [Description] / [DefaultValue("")]` attributes
2. 改寫 3 處測試（改用其他 active 欄位作為「可識別字串」）：
   - `tests/Bee.ObjectCaching.UnitTests/LocalDefineAccessSaveTests.cs:35, 41`
   - `tests/Bee.Definition.UnitTests/DefinitionSerializationTests.cs:103`
   - `tests/Bee.Definition.UnitTests/DtoSerializationTests.cs:324, 333`
3. `dotnet build` 通過後 commit。

### Step 2b：sweep src/tests 文字引用（4 處）

| 檔案 | 改動 |
|------|------|
| `src/Bee.Api.Client/ApiClientInfo.cs:5` | docstring `Counterpart of <see cref="Bee.Definition.BackendInfo"/>` → 純文字描述「provides client-side runtime info; backend registration entry point is `AddBeeFramework`」 |
| `src/Bee.Business/System/SystemBusinessObject.cs:70-71` | 「Phase 3 backs this with BackendInfo statics; Phase 4 swaps for real DI scope.」→ 「Resolved through `Services` (DI scope) — `ILoginAttemptTracker` is an optional service apps register if they need brute-force protection.」 |
| `src/Bee.Definition/IDatabaseSettingsProvider.cs:7-12` | docstring 改為「Provides access to the current `DatabaseSettings` snapshot (with `DatabaseSettings.Items` populated). Resolved through DI ctor injection.」（移除 BackendInfo 對照） |
| `tests/Bee.Business.UnitTests/StaticApiEncryptionKeyProviderTests.cs:8` | 註解改為「Phase 4 之後 provider 透過 ctor 注入 byte[] 金鑰；測試也跟著用直接構造。」（移除 BackendInfo 字眼） |

`dotnet build` 通過後 commit。

### Step 3：sweep active docs

**3a. `docs/development-cookbook.md` + `.zh-TW.md`**：第 247 / 246 行 `BackendInfo.DefineAccess` → `IDefineAccess`。

**3b. `docs/development-constraints.md` + `.zh-TW.md`**：重寫「Initialization Order Constraints」整節（8-28 行），改為簡短的 DI 4 步流程 + link cookbook。

**3c. `docs/database-settings-guide.md` + `.zh-TW.md`**：sweep 13 處引用為 DI 等價（見 §「設計 §4」）。

### Step 4：ADR 加註記

- `docs/adr/adr-003-static-service-locator.md`：標題下加 superseded 註記
- `docs/adr/adr-010-logical-database-category.md`：標題下加等價對照註記

### Step 5：修訂主計畫

- §「Sub-plan 進度」表 Phase 6 列：狀態 ✅ + sub-plan 連結
- §「Phase 6」段落改寫對齊現實
- §「成功標準」更新（見 §「設計 §6」）

### Step 6：本機 build + test 驗證

```bash
dotnet build --configuration Release
./test.sh
```

兩者通過即可 commit + push。

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| **`docs/database-settings-guide.md` sweep 漏改** | 完成後 `grep -n "BackendInfo" docs/database-settings-guide.md docs/database-settings-guide.zh-TW.md` 應為 0；同步檢查 zh-TW 對應行號是否一致 |
| **ADR 註記改動觸發 ADR 變更管理流程** | 僅加頂部 1-2 行 superseded 註記，不改 body；commit message 標明「ADR-003/010: add superseded note, body unchanged」 |
| **外部使用者 XML 含 `<BackendConfiguration><ApiKey>` 節點** | 刪除欄位後 `XmlCodec.DeserializeFromFile` 應 ignore 未知節點（驗證；現行 fixture XML 無此節點，外部部署環境通常也未配置 `<ApiKey>` 因為它從未被讀取過）；若實測會 throw，補 unit test 確認容錯行為再決定處置 |
| **ApiKey 刪除後 3 個測試 XML round-trip 覆蓋率下降** | 改用其他現有 string 欄位（如 `SecurityKeySettings.MasterKeySource.Value`）保留 round-trip 驗證強度 |

## 成功標準

- [ ] `src/Bee.Definition/BackendInfo.cs` 已刪除
- [ ] `BackendConfiguration.ApiKey` 屬性已刪除（`grep -rn "BackendConfiguration\.ApiKey\|BackendConfiguration\..ApiKey" src tests` 為 0）
- [ ] `grep -rn "BackendInfo" src tests --include="*.cs"` 結果為 0
- [ ] `grep -rn "BackendInfo" docs --include="*.md"` 結果限縮於 `docs/plans/`（歷史紀錄）+ `docs/adr/` 加註記引用 + `docs/archive/`（封存）
- [ ] `dotnet build --configuration Release` 通過（`TreatWarningsAsErrors=true`）
- [ ] `./test.sh` 全套單元測試綠燈
- [ ] 主計畫 §「Sub-plan 進度」表 Phase 6 標 ✅ + sub-plan 連結
- [ ] 主計畫 §「Phase 6」描述對齊現實（不再聲稱刪除 `BackendConfiguration.cs`）

## 後續

Phase 6 完成後 → Phase 7（`DbConnectionManager` 靜態 facade 移除 + `DbAccess` ctor DI 化），見 [plan-backendinfo-di-phase7-dbconnectionmanager.md](plan-backendinfo-di-phase7-dbconnectionmanager.md)。

Phase 6 + Phase 7 都完成後，主計畫 §「目標」第 1 條「移除 BackendInfo 類別」與 §「成功標準」全條件達成；v5.0 release notes 可一併撰寫。
