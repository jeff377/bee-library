# 計畫：移除 BackendInfo 資料庫全域設定，導入 Category-based 路由

**狀態：📝 擬定中**

> 本計畫為 [BackendInfo DI 遷移主計畫](plan-backendinfo-to-di-migration.md) 的**前置作業**。
> 完成後 DI Phase 2（Bee.Db 配置注入）的範圍會大幅簡化。

## 背景

### 設計缺失

`BackendInfo` 目前持有三個全域資料庫設定：

```csharp
public static DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;
public static string DatabaseId { get; set; } = string.Empty;
public static int MaxDbCommandTimeout { get; set; } = 60;
```

這個設計來自「單一預設資料庫」的早期假設，與目前 `DatabaseSettings + DbCategorySettings` 多連線模型（詳見 [database-settings-guide.zh-TW.md](../database-settings-guide.zh-TW.md)）已矛盾：

- **`DatabaseType` 屬於 DatabaseItem**：每筆 `DatabaseItem` 已自帶 `DatabaseType`，可能 common 在 SQL Server、log 在 PostgreSQL，全域單一型別不再成立
- **`DatabaseId` 不是固定字串**：多租戶部署下 company 變 `company001`、`company002`...；按年封存下 log 變 `log2025`、`log2026`...，「全域預設 DB」概念失效
- **`MaxDbCommandTimeout` 是真正 process-level 設定**：本計畫**不移除**，僅 DatabaseType / DatabaseId 在範圍內

### 缺失的功能

目前框架無法表達「使用者登入後屬於哪個 company 資料庫」：

- 多租戶部署下，登入 company001 的使用者應只能存取 `company001` DatabaseItem
- 現況 `SessionInfo` 缺欄位記錄此資訊，業務層只能自行 hack（如約定 UserId 前綴）
- 應該由框架明確支援：`SessionInfo.CompanyDatabaseId`

### 與 DI 主計畫的關係

DI 主計畫 Phase 2 原本要把 `DatabaseType / DatabaseId / MaxDbCommandTimeout` 三者打包成 `DbOptions` 注入。完成本計畫後：

- `DbOptions` 只剩 `MaxDbCommandTimeout`
- 是否還需要 `IOptions<DbOptions>` 包裝可重新評估（可能直接保留 static 即可）
- DI Phase 2 的「機械替換」工作量更小、設計面更乾淨

**順序意義**：本計畫先 ship，DI 主計畫從 Phase 0 啟動時面對的 `BackendInfo` 已較簡潔。

## 目標

1. 新增 `DbCategoryIds` 常數類別，集中 `common` / `company` / `log` 三個邏輯分類字串
2. 移除 `BackendInfo.DatabaseType` 與 `BackendInfo.DatabaseId` 兩個屬性
3. 各 callsite 改為從「context 已知的 DatabaseItem」取得 DatabaseType
4. `SessionRepository` 改為明確使用 `DbCategoryIds.Common`，不再讀全域預設
5. `SessionInfo` 新增 `CompanyDatabaseId` 欄位，支援多租戶 company 路由
6. 啟動驗證新增 `common` DatabaseItem 唯一性與 Id 命名檢查
7. 不破壞 `MaxDbCommandTimeout` 既有行為（保留為 BackendInfo 屬性，DI plan 再處理）

## 範圍評估

### 受影響檔案

| 檔案 | 改動類型 |
|------|---------|
| `src/Bee.Definition/Database/DbCategoryIds.cs`（**新增**） | 常數類別 |
| `src/Bee.Definition/BackendInfo.cs` | 移除 2 屬性 + 啟動驗證強化 |
| `src/Bee.Definition/BackendConfiguration.cs`（或對應檔） | 移除 2 對應欄位 |
| `src/Bee.Definition/Identity/SessionInfo.cs` | 新增 `CompanyDatabaseId` 屬性 |
| `src/Bee.Db/DbAccess.cs` | 改為從 `DatabaseSettings` 查 DatabaseType |
| `src/Bee.Db/Dml/TableSchemaCommandBuilder.cs` | 移除 1-arg ctor 或要求顯式傳 DatabaseType |
| `src/Bee.Definition/Database/DbTableIndex.cs` | 比對方法接收 DatabaseType 參數 |
| `src/Bee.Repository/System/SessionRepository.cs` | 4 處 `BackendInfo.DatabaseId` 改 `DbCategoryIds.Common` |

### 測試影響

| 檔案 | 改動 |
|------|------|
| `tests/Bee.Db.UnitTests/TableSchemaCommandBuilderTests.cs` | 對應修正 |
| `tests/Bee.ObjectCaching.UnitTests/CacheContainerTests.cs` | 2 處改使用 `DbCategoryIds.Common` |
| `tests/Bee.Tests.Shared/GlobalFixture.cs` | 4 處註解更新（移除 `BackendInfo.DatabaseId` 引用） |
| `tests/Bee.Definition.UnitTests/SessionInfoTests.cs` | 新增 `CompanyDatabaseId` 序列化測試 |
| `tests/Bee.Repository.UnitTests/SessionRepositoryTests.cs`（若有） | CompanyDatabaseId round-trip 測試 |

### 範圍量化

| 項目 | 數量 |
|------|------|
| `BackendInfo.DatabaseType` callsite | 3 src + 1 test |
| `BackendInfo.DatabaseId` callsite | 4 src + 2 test |
| 新增 class | 1（`DbCategoryIds`） |
| 新增 SessionInfo 欄位 | 1（`CompanyDatabaseId`） |
| 預估 PR 大小 | < 300 lines diff |

## 設計

### 1. `DbCategoryIds` 常數類別

```csharp
namespace Bee.Definition.Database;

/// <summary>
/// Built-in database category identifiers used by the framework.
/// </summary>
/// <remarks>
/// These are <b>CategoryId</b> values (logical classification), not <b>DatabaseId</b> values
/// (physical connections). In multi-tenant or time-archived deployments, the actual
/// <see cref="DatabaseItem.Id"/> may differ (e.g., <c>company001</c>, <c>log2025</c>),
/// but the <see cref="DatabaseItem.CategoryId"/> remains one of these constants.
/// </remarks>
public static class DbCategoryIds
{
    /// <summary>Common database — shared system tables (e.g., st_user, st_session).</summary>
    public const string Common = "common";

    /// <summary>Company database — business data, isolated per company.</summary>
    public const string Company = "company";

    /// <summary>Log database — audit / operation logs.</summary>
    public const string Log = "log";
}
```

**故意只放 CategoryId 不放 DatabaseId**：DatabaseId 在動態切分部署（多租戶、按年）下是運算結果，無法靜態化。

### 2. `BackendInfo` 修改

**移除**：

```csharp
public static DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;  // ← 刪除
public static string DatabaseId { get; set; } = string.Empty;                    // ← 刪除
```

**保留**：

```csharp
public static int MaxDbCommandTimeout { get; set; } = 60;  // 真 process-level 設定
```

**`BackendConfiguration` 對應欄位同步移除**。

### 3. `SessionInfo` 新增欄位

```csharp
public class SessionInfo : IKeyObject, IUserInfo
{
    // ... 既有欄位 ...

    /// <summary>
    /// Gets or sets the company database identifier for this session.
    /// Set during login based on the user's company affiliation; used by business
    /// objects to route company-scoped data access to the correct DatabaseItem.
    /// </summary>
    /// <remarks>
    /// In single-company deployments, this typically equals <see cref="DbCategoryIds.Company"/>.
    /// In multi-tenant deployments, this is the specific DatabaseItem.Id (e.g., "company001").
    /// </remarks>
    public string CompanyDatabaseId { get; set; } = string.Empty;
}
```

**序列化**：`SessionInfo` 透過 XML 序列化儲存於 `st_session.session_user_xml`（Text 欄位）— **不需 DB schema migration**。
**新登入**：登入流程設定此欄位（具體實作待業務層；本計畫不規範如何決定 company DatabaseId，僅提供欄位）。
**既有 session**：反序列化舊資料時欄位為空字串，業務層應有 fallback 或要求重新登入。

### 4. 各 callsite 處理

#### `DbAccess.cs:46`

```csharp
// 現況
DatabaseType = BackendInfo.DatabaseType;

// 改為
DatabaseType = BackendInfo.GetDatabaseItem(databaseId).DatabaseType;
```

DbAccess 建構時已收 `databaseId`，從 `DatabaseSettings` 查對應 DatabaseItem 的型別。

#### `TableSchemaCommandBuilder.cs:32`

```csharp
// 現況：1-arg ctor delegating
public TableSchemaCommandBuilder(TableSchema tableSchema)
    : this(BackendInfo.DatabaseType, tableSchema) { ... }

// 改為：移除 1-arg ctor，強制呼叫端提供 DatabaseType
// 所有呼叫點補上 DatabaseType（從上下文已知的 DatabaseItem 取得）
```

#### `DbTableIndex.cs:114`

```csharp
// 現況：在 Equals/比對方法內部讀 BackendInfo.DatabaseType
if (BackendInfo.DatabaseType == DatabaseType.SQLServer && ...) { ... }

// 改為：方法簽章加入 DatabaseType 參數
public bool EqualsForDatabase(DbTableIndex source, DatabaseType databaseType)
{
    if (databaseType == DatabaseType.SQLServer && ...) { ... }
}
```

呼叫端在已知 target DatabaseItem 的場景下提供 type。

#### `SessionRepository.cs`（4 處）

```csharp
// 現況
var dbAccess = new DbAccess(BackendInfo.DatabaseId);

// 改為
var dbAccess = new DbAccess(DbCategoryIds.Common);
```

依賴慣例「`CategoryId=common` 的 DatabaseItem.Id 也命名為 `common`」— 此慣例由新啟動驗證保證。

### 5. 啟動驗證強化

現有 P1 `ValidateComponents()` 之外，新增 `ValidateDatabaseSettings()`：

```csharp
private static void ValidateDatabaseSettings()
{
    if (SysInfo.IsSingleFile) return;

    var settings = DefineAccess.GetDatabaseSettings();
    var commonItems = settings.Items!.Where(i => i.CategoryId == DbCategoryIds.Common).ToArray();

    if (commonItems.Length == 0)
        throw new InvalidOperationException(
            $"DatabaseSettings must contain exactly one DatabaseItem with CategoryId='{DbCategoryIds.Common}'.");

    if (commonItems.Length > 1)
        throw new InvalidOperationException(
            $"DatabaseSettings contains {commonItems.Length} DatabaseItems with CategoryId='{DbCategoryIds.Common}'; expected exactly 1.");

    if (commonItems[0].Id != DbCategoryIds.Common)
        throw new InvalidOperationException(
            $"DatabaseItem with CategoryId='{DbCategoryIds.Common}' must also have Id='{DbCategoryIds.Common}' (convention required for framework session table routing).");
}
```

在 `BackendInfo.Initialize()` 末尾呼叫，與 `ValidateComponents()` 並列為啟動期安全網。

**設計取捨**：此驗證是「Fail-Fast 換取簡潔慣例」。若未來需放寬「common.Id 必為 common」這個約束，可改為 `DatabaseSettings.Items` 提供 `GetSingleByCategoryId(string)` helper，再由 `SessionRepository` 走查表路徑 — 但目前慣例已夠用，不過度設計。

## 執行階段（單一 PR 內順序）

1. 新增 `DbCategoryIds.cs` 常數類別
2. `SessionInfo` 新增 `CompanyDatabaseId` 屬性 + 對應測試
3. `BackendInfo` 新增 `ValidateDatabaseSettings()`（先加邏輯，後續 callsite 替換才依賴此驗證）
4. 替換 4 處 `BackendInfo.DatabaseId` → `DbCategoryIds.Common`
5. 替換 3 處 `BackendInfo.DatabaseType` → 各 callsite 處理（DatabaseItem 查表 / 方法參數化）
6. 移除 `BackendInfo.DatabaseType` 與 `DatabaseId` 屬性
7. 移除 `BackendConfiguration` 對應欄位
8. 更新測試（含 `GlobalFixture.cs` 註解）
9. 更新 [database-settings-guide.zh-TW.md](../database-settings-guide.zh-TW.md) 與英文版（如有提到全域 DB 預設處）
10. 跑完整測試（`./test.sh` 全綠）+ build with `TreatWarningsAsErrors`

## 測試策略

### 新增測試

- `DbCategoryIdsTests`（基本字串值驗證）
- `SessionInfoTests`：`CompanyDatabaseId` 序列化 round-trip
- `BackendInfoTests`：`ValidateDatabaseSettings` 三種失敗情境（無 common、多筆 common、common.Id 不匹配）

### 既有測試調整

- `CacheContainerTests` 改用 `DbCategoryIds.Common`
- `TableSchemaCommandBuilderTests` 適應新 ctor 簽章
- `GlobalFixture.cs` 註解中提到 `BackendInfo.DatabaseId` 之處改寫

### Fixture 兼容性

`tests/Define/DatabaseSettings.xml` 已遵守「common 唯一 + Id 為 `common`」慣例（見 `database-settings-guide.zh-TW.md` §6.2 範例）。執行新驗證應通過，不需改動 fixture XML。

## 相容性與 Migration

### 對外 API

`BackendInfo.DatabaseType` / `DatabaseId` 是 public static。移除為 **breaking change**，但：

- 框架使用者若有讀這兩個屬性 → compile error，明確失敗
- 框架使用者若有寫這兩個屬性 → compile error，明確失敗
- 不需 `[Obsolete]` 過渡期：值本身已不適用（看新部署範本即知），保留會給錯誤訊號

**Release notes 必須列出**：建議讀取者改用 `BackendInfo.GetDatabaseItem(id).DatabaseType`；寫入者通常是 boot-time 配置，改用 `DatabaseSettings.xml` 設定。

### Session XML 向下相容

舊 `st_session.session_user_xml` 內容無 `<CompanyDatabaseId/>` 節點：
- XML 反序列化容忍缺欄位 → `CompanyDatabaseId = ""`（預設值）
- 業務層讀取空字串時應有 fallback 或強制使用者重新登入
- **無需 migration script**，舊 session 自然 expire 後即清除

### 配置檔

`SystemSettings.xml`（即 `BackendConfiguration` 序列化來源）中對應 `DatabaseType` / `DatabaseId` 欄位 → migration 時移除。
若使用者既有檔案保留這些節點，反序列化會忽略（XmlSerializer 預設行為），但建議乾淨化以避免混淆。

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| `DbAccess` 改為查 `DatabaseSettings` 取 DatabaseType 多一次查表 | DatabaseSettings 已快取（`CacheContainer.DatabaseSettings` 20 秒 sliding），查找 O(1)；效能影響可忽略 |
| 某些測試 fixture 未遵守「common 唯一」慣例 | 啟動驗證在 fixture init 時暴露，立即修正 fixture |
| `TableSchemaCommandBuilder` 移除 1-arg ctor 是 breaking change | grep 確認 src/ tests/ samples/ 中所有呼叫點都已改為 2-arg；外部使用者透過 release notes 引導 |
| 舊 session 反序列化遇到新欄位 → 預期空字串，但若業務層未處理可能 NRE | 業務層 fallback 規格在 release notes 明列；建議部署前先 invalidate 舊 session |
| `BackendConfiguration` 欄位移除可能破壞既有 `SystemSettings.xml` 反序列化 | XmlSerializer 對未知節點預設忽略，不破壞；但需測試確認 |

## 成功標準

- [ ] `grep -rn "BackendInfo\.\(DatabaseType\|DatabaseId\)" src/ tests/` 結果為 0
- [ ] `DbCategoryIds` 常數類別建立，三個常數定義正確
- [ ] `SessionInfo.CompanyDatabaseId` 屬性存在且通過序列化 round-trip 測試
- [ ] 啟動驗證能正確攔截三種違反慣例的 `DatabaseSettings` 配置
- [ ] `./test.sh` 全綠（含 SQL Server / PostgreSQL 路徑）
- [ ] `dotnet build --configuration Release` 在 `TreatWarningsAsErrors` 下通過
- [ ] `database-settings-guide` 雙語版同步更新
- [ ] DI 主計畫 Phase 2 章節已加 cross-reference 標註此計畫為前置條件

## 不納入範圍

- **如何決定 `SessionInfo.CompanyDatabaseId` 的值**：登入流程的業務邏輯（如「依使用者 → 公司 → DatabaseId」對照規則）由業務層自訂，本計畫只提供欄位
- **log DatabaseId 動態推導 helper**：按年份取 `log2025` 等的 helper（如 `LogDatabaseRouter`）值得做但與本計畫主軸無關，另立小計畫
- **MaxDbCommandTimeout 處理**：留待 DI 主計畫 Phase 2 一併評估（保留 static / 改 Options / 改 const 三種方案）
- **公開 API obsolete 過渡期**：本計畫直接 breaking，依 v5.0 release notes 引導；不引入 `[Obsolete]` 中間階段
- **多 common DatabaseItem 支援**：明確排除 — 框架慣例維持「common 唯一」，未來若有需要再評估

## Log 路由與未來計畫

### 為何 SessionInfo 只加 CompanyDatabaseId，不加 LogDatabaseId

Company 路由與 log 路由本質不同：

| 路由 | 維度 | 變動頻率 | 何時可知 |
|------|------|----------|----------|
| **Company** | 租戶 | 登入後整個 session 不變 | 登入時 |
| **Business Log** | 租戶 × 時間 × log 類型 | 跨年、跨類型會變 | 寫入當下 |

把 LogDatabaseId 也塞進 SessionInfo 會有幾個問題：

1. 登入時設定的值跨年後 stale
2. 多軸切分（tenant × year × kind）無法用單一字串表達
3. Pre-session 場景（登入失敗、系統事件）沒有 session 也需要寫 log
4. 未來改路由策略時，session 內已存的字串解釋規則難以變更

**正確抽象**：log 路由是「寫入當下根據多 input 計算」的函式，不是 session 屬性。

### 兩種 log 的概念分野

| 類型 | 對象 | 用途 | 儲存 |
|------|------|------|------|
| **Diagnostic log**（現有 `ILogWriter`） | 開發者 | 設計階段監看、anomaly 追蹤、debug | Console / File |
| **Business log**（登入/執行/瀏覽/異動） | 營運稽核 | 法規遵循、營運追蹤、跨年查詢 | Log DB（依路由分流） |

現有 `ILogWriter`（`NullLogWriter` / `ConsoleLogWriter`）屬於前者，**不會被擴充為後者**。Business log 該有獨立抽象（如 `IAuditLogWriter` + `ILogDatabaseRouter`），與 `ILogWriter` 並存且互不引用。

### 未來計畫草圖（不在本 plan 範圍）

```csharp
public interface ILogDatabaseRouter
{
    string Resolve(LogRoutingContext ctx);
}

public sealed class LogRoutingContext
{
    public SessionInfo? Session { get; init; }   // null for pre-session events
    public DateTime WhenUtc { get; init; }
    public LogKind Kind { get; init; }
}

public enum LogKind { Login, Execution, Browse, DataChange, SystemError }
```

不同部署註冊不同實作：`SingleLogDatabaseRouter` / `YearlyLogDatabaseRouter` / `TenantYearlyLogDatabaseRouter` 等。

`SessionInfo.CompanyDatabaseId` 是 router 的 input 之一，但 router 還會看時間、類型、是否有 session — 因此 router 是 DI 注入的服務（在 DI 主計畫 Phase 4 BO 注入後，BO 可直接 `IAuditLogWriter` + `ILogDatabaseRouter`）。

### 預期時序

1. **本計畫**（DB cleanup）：完成 CompanyDatabaseId、不動 log 路由
2. **DI 主計畫**：完成 BO 注入、`ILogDatabaseRouter` 預留為將注入的服務
3. **後續獨立計畫** `plan-log-database-routing.md`（未撰寫）：設計並實作 router 策略集、`IAuditLogWriter`、log table schema

本計畫範圍刻意限縮，避免 log 路由設計尚未成熟時就在 `SessionInfo` 內放會 stale 的欄位。

## 後續關聯計畫

完成本計畫後，[plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) Phase 2 範圍重新評估：

- `DbOptions` 結構可能不需要（只剩 `MaxDbCommandTimeout`）
- 直接於 Phase 2 sub-plan 中決定「保留 static / 改 IOptions / 改 const」

未來另立計畫：
- `plan-log-database-routing.md`（待撰寫）：business log DB 路由設計
