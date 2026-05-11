# 計畫：移除 BackendInfo 資料庫全域設定，導入 Category-based 路由

**狀態：✅ 已完成（2026-05-11）**

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
5. 啟動驗證新增 `Id='common'` DatabaseItem 存在性檢查
6. 不破壞 `MaxDbCommandTimeout` 既有行為（保留為 BackendInfo 屬性，DI plan 再處理）

> **不在本計畫範圍**：`SessionInfo` 新增 company / log 路由欄位刻意延後 — 待 `CompanyInfo` 與 log routing 設計成熟後另立計畫處理（詳見 §不納入範圍 與 §後續關聯計畫）。

## 範圍評估

### 受影響檔案

| 檔案 | 改動類型 |
|------|---------|
| `src/Bee.Definition/Database/DbCategoryIds.cs`（**新增**） | 常數類別 |
| `src/Bee.Definition/BackendInfo.cs` | 移除 2 屬性 + 啟動驗證強化 |
| `src/Bee.Definition/BackendConfiguration.cs`（或對應檔） | 移除 2 對應欄位 |
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

### 範圍量化

| 項目 | 數量 |
|------|------|
| `BackendInfo.DatabaseType` callsite | 3 src + 1 test |
| `BackendInfo.DatabaseId` callsite | 4 src + 2 test |
| 新增 class | 1（`DbCategoryIds`） |
| 預估 PR 大小 | < 250 lines diff |

## 設計

### 1. `DbCategoryIds` 常數類別

```csharp
namespace Bee.Definition.Database;

/// <summary>
/// Built-in database category identifiers used by the framework.
/// </summary>
/// <remarks>
/// These constants serve two roles:
/// <list type="bullet">
/// <item><b>CategoryId</b> values for logical classification on <see cref="DatabaseItem.CategoryId"/>
/// and <see cref="FormSchema.CategoryId"/>.</item>
/// <item><b>DatabaseId</b> conventions for framework system routing (e.g.,
/// <c>SessionRepository</c> uses <see cref="Common"/> as the literal <see cref="DatabaseItem.Id"/>
/// for the shared system database).</item>
/// </list>
/// In multi-tenant or time-archived deployments, the physical <see cref="DatabaseItem.Id"/>
/// may diverge from the CategoryId (e.g., <c>company001</c>, <c>log2025</c>), but the
/// <see cref="DatabaseItem.CategoryId"/> remains one of these constants — and for the
/// <see cref="Common"/> category, the framework requires Id == CategoryId == "common"
/// (enforced at startup).
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

**為何不另立 `WellKnownDatabaseIds`**：framework 慣例下「common 類別的物理連線 Id 也叫 common」，CategoryId 與 DatabaseId 在此處名稱重合。獨立兩組常數會分裂同一概念，反而誤導；單一常數類加註釋說明雙角色更清楚。

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

### 3. 各 callsite 處理

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

此處 `DbCategoryIds.Common` 字面值作為 `DatabaseItem.Id`（即 framework system DB 連線 ID）使用，由啟動驗證保證 `Id='common'` 的 DatabaseItem 必存在。

### 4. 啟動驗證強化

現有 P1 `ValidateComponents()` 之外，新增 `ValidateDatabaseSettings()`：

```csharp
private static void ValidateDatabaseSettings()
{
    if (SysInfo.IsSingleFile) return;

    var settings = DefineAccess.GetDatabaseSettings();
    if (settings.Items == null || !settings.Items.Contains(DbCategoryIds.Common))
        throw new InvalidOperationException(
            $"DatabaseSettings must contain a DatabaseItem with Id='{DbCategoryIds.Common}'.");
}
```

在 `BackendInfo.Initialize()` 末尾呼叫,與 `ValidateComponents()` 並列為啟動期安全網。

**設計取捨**:
- `DatabaseItemCollection` 繼承自 `KeyCollectionBase<DatabaseItem>`,以 `Id` 為唯一 key,本身已保證不重複,因此不需要額外的 Id 重複性檢查
- Framework runtime 路由(如 `SessionRepository`)實際依據是 `DatabaseItem.Id`,而非 `CategoryId`;`CategoryId` 屬於 UI / schema 歸屬用的邏輯分類
- 因此只需驗證「`Id='common'` 的 DatabaseItem 存在」即可,單一 `Contains` 檢查同時涵蓋「唯一」與「命名慣例」兩個面向

## 執行階段（單一 PR 內順序）

1. 新增 `DbCategoryIds.cs` 常數類別
2. `BackendInfo` 新增 `ValidateDatabaseSettings()`（先加邏輯，後續 callsite 替換才依賴此驗證）
3. 替換 4 處 `BackendInfo.DatabaseId` → `DbCategoryIds.Common`
4. 替換 3 處 `BackendInfo.DatabaseType` → 各 callsite 處理（DatabaseItem 查表 / 方法參數化）
5. 移除 `BackendInfo.DatabaseType` 與 `DatabaseId` 屬性
6. 移除 `BackendConfiguration` 對應欄位
7. 更新測試（含 `GlobalFixture.cs` 註解）
8. 更新 [database-settings-guide.zh-TW.md](../database-settings-guide.zh-TW.md) 與英文版（如有提到全域 DB 預設處）
9. 跑完整測試（`./test.sh` 全綠）+ build with `TreatWarningsAsErrors`

## 測試策略

### 新增測試

- `DbCategoryIdsTests`（基本字串值驗證）
- `BackendInfoTests`：`ValidateDatabaseSettings` 失敗情境（`DatabaseSettings` 中不存在 `Id='common'` 的 DatabaseItem）

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

### 配置檔

`SystemSettings.xml`（即 `BackendConfiguration` 序列化來源）中對應 `DatabaseType` / `DatabaseId` 欄位 → migration 時移除。
若使用者既有檔案保留這些節點，反序列化會忽略（XmlSerializer 預設行為），但建議乾淨化以避免混淆。

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| `DbAccess` 改為查 `DatabaseSettings` 取 DatabaseType 多一次查表 | DatabaseSettings 已快取（`CacheContainer.DatabaseSettings` 20 秒 sliding），查找 O(1)；效能影響可忽略 |
| 某些測試 fixture 未包含 `Id='common'` 的 DatabaseItem | 啟動驗證在 fixture init 時暴露，立即修正 fixture |
| `TableSchemaCommandBuilder` 移除 1-arg ctor 是 breaking change | grep 確認 src/ tests/ samples/ 中所有呼叫點都已改為 2-arg；外部使用者透過 release notes 引導 |
| `BackendConfiguration` 欄位移除可能破壞既有 `SystemSettings.xml` 反序列化 | XmlSerializer 對未知節點預設忽略，不破壞；但需測試確認 |

## 成功標準

- [ ] `grep -rn "BackendInfo\.\(DatabaseType\|DatabaseId\)" src/ tests/` 結果為 0
- [ ] `DbCategoryIds` 常數類別建立，三個常數定義正確
- [ ] 啟動驗證能正確攔截缺少 `Id='common'` DatabaseItem 的 `DatabaseSettings` 配置
- [ ] `./test.sh` 全綠（含 SQL Server / PostgreSQL 路徑）
- [ ] `dotnet build --configuration Release` 在 `TreatWarningsAsErrors` 下通過
- [ ] `database-settings-guide` 雙語版同步更新
- [ ] DI 主計畫 Phase 2 章節已加 cross-reference 標註此計畫為前置條件

## 不納入範圍

- **`SessionInfo` 多租戶 company 路由欄位**：未來預期走「`SessionInfo.CompanyId` → `CompanyInfo.DatabaseId` 查表」路徑，但 `CompanyInfo` 尚未設計；先 ship 空字串欄位等於擺設，因此延後至 `CompanyInfo` 相關計畫一併處理
- **Log 路由（含 `SessionInfo.LogDatabaseId` / `ILogDatabaseRouter`）**：log 路由本質是「寫入當下根據多 input 計算」的函式（詳見 §Log 路由設計考量），與本計畫主軸無關，另立計畫
- **MaxDbCommandTimeout 處理**：留待 DI 主計畫 Phase 2 一併評估（保留 static / 改 Options / 改 const 三種方案）
- **公開 API obsolete 過渡期**：本計畫直接 breaking，依 v5.0 release notes 引導；不引入 `[Obsolete]` 中間階段
- **多 common DatabaseItem 支援**：明確排除 — 框架慣例維持「common 唯一」，未來若有需要再評估

## Company / Log 路由設計考量（供後續計畫參考）

本計畫只完成「移除 BackendInfo 全域 DB 設定」，company / log 路由刻意延後。以下整理留待後續計畫參考的設計脈絡。

### Company 路由：建議走 `CompanyId → CompanyInfo` lookup，不直接存 `DatabaseId`

`SessionInfo` 未來需要記錄「使用者登入的公司」以支援多租戶部署。直觀做法是直接加 `CompanyDatabaseId` 欄位，但更乾淨的設計是：

```
SessionInfo.CompanyId (業務維度) → CompanyInfo.DatabaseId (部署細節)
```

**理由**：
- `CompanyId` 是業務主鍵，公司搬資料庫不影響其 ID；session 不會 stale
- 一個 Company 未來若拆讀寫庫、加 archive 庫，單一 `DatabaseId` 字串無法表達
- 部署細節集中於 `CompanyInfo`（DB 表或設定檔），session 只記業務識別

### Log 路由：是函式，不是 session 屬性

Company 路由與 log 路由本質不同：

| 路由 | 維度 | 變動頻率 | 何時可知 |
|------|------|----------|----------|
| **Company** | 租戶 | 登入後整個 session 不變 | 登入時 |
| **Business Log** | 租戶 × 時間 × log 類型 | 跨年、跨類型會變 | 寫入當下 |

把 `LogDatabaseId` 塞進 `SessionInfo` 會有幾個問題：

1. 登入時設定的值跨年後 stale
2. 多軸切分（tenant × year × kind）無法用單一字串表達
3. Pre-session 場景（登入失敗、系統事件）沒有 session 也需要寫 log
4. 未來改路由策略時，session 內已存的字串解釋規則難以變更

**正確抽象**：log 路由是「寫入當下根據多 input 計算」的函式（如 `ILogDatabaseRouter.Resolve(ctx)`），不是 session 屬性。

### 兩種 log 的概念分野

| 類型 | 對象 | 用途 | 儲存 |
|------|------|------|------|
| **Diagnostic log**（現有 `ILogWriter`） | 開發者 | 設計階段監看、anomaly 追蹤、debug | Console / File |
| **Business log**（登入/執行/瀏覽/異動） | 營運稽核 | 法規遵循、營運追蹤、跨年查詢 | Log DB（依路由分流） |

現有 `ILogWriter`（`NullLogWriter` / `ConsoleLogWriter`）屬於前者，**不會被擴充為後者**。Business log 該有獨立抽象（如 `IAuditLogWriter` + `ILogDatabaseRouter`），與 `ILogWriter` 並存且互不引用。

## 後續關聯計畫

完成本計畫後，[plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) Phase 2 範圍重新評估：

- `DbOptions` 結構可能不需要（只剩 `MaxDbCommandTimeout`）
- 直接於 Phase 2 sub-plan 中決定「保留 static / 改 IOptions / 改 const」

未來另立計畫：
- **`plan-company-database-routing.md`（待撰寫）**：設計 `CompanyInfo`（儲存形式：DB 表 vs 設定檔）、`SessionInfo.CompanyId` 欄位、登入流程 hook、BO 端如何取得 company DatabaseId
- **`plan-log-database-routing.md`（待撰寫）**：設計 `ILogDatabaseRouter` 策略集、`IAuditLogWriter`、log table schema
