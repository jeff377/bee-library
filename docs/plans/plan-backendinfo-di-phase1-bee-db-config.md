# 計畫：Phase 1 — Bee.Db 配置注入（拆解 DbCommandSpec ↔ BackendInfo 耦合）

**狀態：✅ 已完成（2026-05-12）**

> 本文件為主計畫 [plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) 的 **Phase 1** sub-plan，獨立可 ship。

## 背景

### 主計畫定位

Phase 1 是 BackendInfo → DI 遷移的第一個實質階段。前置計畫 [plan-remove-backendinfo-db-globals.md](plan-remove-backendinfo-db-globals.md) 已移除 `BackendInfo.DatabaseType` / `DatabaseId` 並導入 `DbCategoryIds`，使 `BackendInfo` 上 DB 相關配置只剩 `MaxDbCommandTimeout` 一個欄位（cap 值）。

但問題本質不是「單一欄位放哪」，而是 **`DbCommandSpec` setter 內部讀 static**。

### 現況的耦合根因

`DbCommandSpec.CommandTimeout` 的 setter 內部讀 `BackendInfo.MaxDbCommandTimeout` 來做 clamp（[src/Bee.Db/DbCommandSpec.cs:96-112](../../src/Bee.Db/DbCommandSpec.cs#L96-L112)）：

```csharp
public int CommandTimeout
{
    get => _commandTimeout;
    set
    {
        int cap = BackendInfo.MaxDbCommandTimeout;   // ← DTO 讀 static
        if (value <= 0)
            _commandTimeout = DefaultTimeout;
        else
            _commandTimeout = (cap > 0 && value > cap) ? cap : value;
    }
}
```

`DbCommandSpec` 是純 DTO，由呼叫端用 `new` 在數百個位置建立，**沒有 DI 路徑**。Setter 要拿到 cap 值的唯一方式就是 reach 全域 static。這是 Bee.Db 黏住 `BackendInfo` 的根因。

### 副作用：測試操弄 static

兩個既有測試（[DbCommandSpecTests.cs:130-162](../../tests/Bee.Db.UnitTests/DbCommandSpecTests.cs#L130-L162)）用 try/finally 操弄 `BackendInfo.MaxDbCommandTimeout` 來驗證 clamp 行為，違反測試規範中的「平行安全」原則（[testing.md](../../.claude/rules/testing.md)）。

## 目標

1. 解掉 `DbCommandSpec` 對 `BackendInfo.MaxDbCommandTimeout` 的依賴
2. 把 clamp 行為從 DTO setter 搬到執行端（`DbAccess`）
3. 移除 `BackendInfo.MaxDbCommandTimeout` 屬性與 `BackendConfiguration.MaxDbCommandTimeout` 欄位
4. 新增 `IDbAccessFactory` 介面，準備好讓後續 phase 透過 DI 注入 cap 配置（Phase 1 本身不啟用 DI 註冊）
5. 刪除兩個操弄 static 的 cap 行為測試（不寫替代測試，依設計討論結論：簡單配置 clamp 不寫專屬測試）

## 非目標（本 phase 不做）

- 不轉換所有 `new DbAccess(databaseId)` 呼叫點（24 處跨 src + tests）—— production 路徑在 Phase 3（Repository）、測試在 Phase 5 處理
- 不引入 DI 容器註冊（`AddBeeFramework()` 在 Phase 4 才設計）
- 不處理 Bee.Db 內其他 BackendInfo 引用：
  - `Dml/SelectContextBuilder.cs:70` (`DefineAccess.GetFormSchema`) — Phase 2
  - `Providers/*/{Db}FormCommandBuilder.cs` (5 個 provider) — Phase 2
  - `Schema/TableSchemaBuilder.cs:54` — Phase 2
  - `Manager/DbConnectionManager.cs:62` — Phase 2
  - `Schema/TableUpgradeOrchestrator.cs:97` — Phase 2
  - `Logging/DbAccessLogger.cs:35` (`LogOptions`) — Phase 3
- 不改 `DbAccess` 公開的執行方法簽章（`Execute` 系列保持原貌）
- 不重寫 24 個 `new DbAccess` 呼叫點（向下相容）

## 設計

### 1. `DbCommandSpec` 變回純 DTO

```csharp
public class DbCommandSpec : CollectionItem
{
    private const int DefaultTimeout = 30;

    /// <summary>
    /// Gets or sets the command execution timeout in seconds.
    /// Stored as-is; clamping (against the per-app cap) is applied by
    /// <see cref="DbAccess"/> at execution time.
    /// </summary>
    public int CommandTimeout { get; set; } = DefaultTimeout;

    // 其餘程式碼不變
}
```

**變更**：
- setter 不再 reach `BackendInfo`，無 clamp 邏輯
- 移除 private `_commandTimeout` 欄位（合併為 auto-property）
- 移除 `using Bee.Definition;`（本檔不再需要）
- `CreateCommand` 內部 `cmd.CommandTimeout = CommandTimeout` 那行**保留**（傳遞原始值），由 `DbAccess` 在後續覆寫

### 2. `DbAccess` 接收 cap，並於執行路徑 clamp

```csharp
public class DbAccess
{
    private const int DefaultTimeout = 30;
    private readonly int _maxCommandTimeout;
    // ... 其他欄位

    /// <summary>
    /// Initializes a new instance of <see cref="DbAccess"/> for the specified database identifier.
    /// </summary>
    /// <param name="databaseId">The database identifier.</param>
    /// <param name="maxCommandTimeout">
    /// Per-app upper bound for <see cref="DbCommand.CommandTimeout"/>; 0 disables the cap.
    /// </param>
    public DbAccess(string databaseId, int maxCommandTimeout = 0)
    {
        // 原有邏輯
        _maxCommandTimeout = maxCommandTimeout;
    }

    public DbAccess(DbConnection externalConnection, DatabaseType databaseType, int maxCommandTimeout = 0)
    {
        // 原有邏輯
        _maxCommandTimeout = maxCommandTimeout;
    }

    /// <summary>
    /// Resolves the effective <see cref="DbCommand.CommandTimeout"/> for a given request:
    /// non-positive → default 30 sec; cap=0 → as-is; otherwise → min(requested, cap).
    /// </summary>
    private int ResolveTimeout(int requested)
    {
        if (requested <= 0) return DefaultTimeout;
        if (_maxCommandTimeout <= 0) return requested;
        return Math.Min(requested, _maxCommandTimeout);
    }
}
```

**變更**：
- 兩個建構式加 optional `maxCommandTimeout` 參數（預設 0 = 不限）—— 既有 24 個 `new DbAccess(...)` 呼叫無需修改
- 新增 private `ResolveTimeout` 計算夾值後的 effective timeout

### 3. 在 9 個 `command.CreateCommand` 後夾值

`DbAccess` 內部目前有 9 處 `using (var cmd = command.CreateCommand(DatabaseType, ...))`。每處後面加一行：

```csharp
using (var cmd = command.CreateCommand(DatabaseType, connection))
{
    cmd.CommandTimeout = ResolveTimeout(command.CommandTimeout);
    // ... 既有邏輯
}
```

`DbCommandSpec.CreateCommand` 內部仍會把 `command.CommandTimeout` 寫進 `cmd.CommandTimeout`（傳原值），但會被 DbAccess 立刻覆寫為 clamped 版本。略有冗餘的重複指派，但保持 `CreateCommand` 簽章不變、影響面最小。

### 4. `IDbAccessFactory` 介面骨架

```csharp
namespace Bee.Db
{
    /// <summary>
    /// Creates <see cref="DbAccess"/> instances bound to the per-app configuration
    /// (such as the <see cref="DbCommand.CommandTimeout"/> cap).
    /// </summary>
    public interface IDbAccessFactory
    {
        /// <summary>
        /// Creates a <see cref="DbAccess"/> for the specified database identifier.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        DbAccess Create(string databaseId);
    }

    /// <summary>
    /// Default <see cref="IDbAccessFactory"/> implementation. Holds the per-app
    /// <see cref="DbCommand.CommandTimeout"/> cap and propagates it to each
    /// <see cref="DbAccess"/> instance.
    /// </summary>
    public sealed class DbAccessFactory : IDbAccessFactory
    {
        private readonly int _maxCommandTimeout;

        public DbAccessFactory(int maxCommandTimeout = 0)
        {
            _maxCommandTimeout = maxCommandTimeout;
        }

        public DbAccess Create(string databaseId)
            => new DbAccess(databaseId, _maxCommandTimeout);
    }
}
```

**用途**：
- 後續 phase 透過 DI 註冊（例如 Phase 4 的 `AddBeeFramework()`）
- 應用程式（host）建立時傳入 per-app cap：APP 30s / Web 60s / 排程 120s
- Phase 1 本身**不註冊到 DI**、**不轉呼叫端**，僅定義型別讓後續可用

不提供 `Create(DbConnection, DatabaseType)` overload（外部連線場景目前只有 `TableUpgradeOrchestrator` 使用，未來 Phase 2 處理）。

### 5. 移除 `BackendInfo.MaxDbCommandTimeout` 與 `BackendConfiguration.MaxDbCommandTimeout`

- `BackendInfo.cs`：移除屬性 + Initialize 中的賦值（`MaxDbCommandTimeout = configuration.MaxDbCommandTimeout;`）
- `BackendConfiguration.cs`：移除欄位
- `tests/Define/SystemSettings.xml`：移除 `<MaxDbCommandTimeout>` 節點（如有）

### 6. 移除既有 CommandTimeout setter 測試區塊

`DbCommandSpec.CommandTimeout` setter 變純後，整個「CommandTimeout setter 測試」區塊 5 個測試方法皆失效：

- `CommandTimeout_Zero_UsesDefault`：原斷言 0 → 30（setter 自動補預設值）；新 setter 直接存 0
- `CommandTimeout_Negative_UsesDefault`：原斷言 -10 → 30；新 setter 直接存 -10
- `CommandTimeout_WithinCap_UsesValue`：原為 round-trip 測試（45 → 45），變成 auto-property 重複驗證，無價值
- `CommandTimeout_ExceedsCap_UsesCap`：操弄 `BackendInfo.MaxDbCommandTimeout` static（編譯錯誤）
- `CommandTimeout_NoCap_UsesValue`：同上

整區塊刪除，**不寫替代測試**——依使用者偏好：簡單配置 clamp 行為不寫專屬 unit test，DbAccess 端的 `ResolveTimeout` 也比照辦理。

## 改動清單

### 修改

| 檔案 | 內容 |
|------|------|
| `src/Bee.Db/DbCommandSpec.cs` | `CommandTimeout` 變純 auto-property；移除 setter clamp 與 `_commandTimeout` 欄位；移除 `using Bee.Definition` |
| `src/Bee.Db/DbAccess.cs` | 兩個建構式加 optional `maxCommandTimeout` 參數；新增 `ResolveTimeout` private method；9 處 `CreateCommand` 後加 `cmd.CommandTimeout = ResolveTimeout(...)` |
| `src/Bee.Definition/BackendInfo.cs` | 移除 `MaxDbCommandTimeout` 屬性與 Initialize 內賦值 |
| `src/Bee.Definition/Settings/SystemSettings/BackendConfiguration.cs` | 移除 `MaxDbCommandTimeout` 欄位 |
| `tests/Define/SystemSettings.xml` | 移除 `<MaxDbCommandTimeout>` 節點（若存在） |
| `tests/Bee.Db.UnitTests/DbCommandSpecTests.cs` | 刪除 `CommandTimeout_ExceedsCap_UsesCap` 與 `CommandTimeout_NoCap_UsesValue` 兩個測試方法 |
| `tests/Bee.Definition.UnitTests/BackendInfoTests.cs` | 移除涉及 `MaxDbCommandTimeout` 的測試斷言（如有） |

### 新增

| 檔案 | 內容 |
|------|------|
| `src/Bee.Db/IDbAccessFactory.cs` | `IDbAccessFactory` 介面 + `DbAccessFactory` 預設實作（兩者放同一檔案） |

### 不需改

- 24 個 `new DbAccess(databaseId)` 呼叫點：因新加的 `maxCommandTimeout` 參數有預設值 0，既有呼叫不需修改（cap=0 = 不限制）
- `DbCommandSpec.CreateCommand`：保留 `cmd.CommandTimeout = CommandTimeout`，由 `DbAccess` 覆寫
- 其他 Bee.Db 內 BackendInfo 引用：屬於 Phase 2/3 範圍

## 影響評估

### 行為變化

| 場景 | Phase 1 前 | Phase 1 後 |
|------|------------|------------|
| `new DbCommandSpec { CommandTimeout = 9999 }` 後讀回 | 被 `BackendInfo.MaxDbCommandTimeout` clamp | 原值 9999（DTO 不做 clamp） |
| `new DbAccess(id).Execute(spec)` 中 cmd.CommandTimeout 實值 | 由 setter 已 clamp 的值 | cap=0（無 factory 介入）→ 原值；cap>0（透過 factory）→ clamp |
| 整個框架的「自動 cap 保護」 | 全域生效（`BackendInfo.MaxDbCommandTimeout`） | 僅透過 `DbAccessFactory` 建立的 DbAccess 生效；直接 `new DbAccess(id)` 不生效 |

**這是合理且預期的行為變化**：原本依賴全域 static 的保護機制，改成由建構式參數明確傳遞。後續 phase 把 production 路徑轉到 factory 後，cap 保護自然恢復。

### 對下游應用程式（如 ERP 後端）的影響

主計畫已明文無外部消費者。本框架 NuGet 套件下游若有人呼叫 `BackendInfo.MaxDbCommandTimeout = 90` 之類設定，會編譯失敗（屬性已刪）。這是預期的破壞性變更，v5.0 release notes 會標注。

## 測試策略

### 不新增測試

依設計討論結論（[memory: feedback_simple_config_no_dedicated_test](../../../.claude/projects/-Users-jeff-Desktop-repos-bee-library/memory/feedback_simple_config_no_dedicated_test.md)），簡單配置的 cap/clamp 行為不寫專屬 unit test。

### 既有測試影響

- `DbCommandSpecTests`：刪除 2 個 cap 測試；其他測試（如 `CommandTimeout_ValidValue_StoresValue`）不受影響
- `BackendInfoTests`：可能涉及 `MaxDbCommandTimeout` 的斷言需移除（待實作時確認）
- Bee.Db 與 Bee.Repository 整合測試：仍透過 `new DbAccess(id)` 建立，cap 預設 0 = 不限，行為與既有測試一致

### 驗證

- `dotnet build --configuration Release`（含 `TreatWarningsAsErrors`）通過
- `./test.sh` 全套測試通過（預期測試總數減少 2）
- `grep -n "BackendInfo\.MaxDbCommandTimeout" src/ tests/` 結果為 0

## 驗收標準

- [ ] `DbCommandSpec.CommandTimeout` 為 auto-property，無 setter clamp 邏輯
- [ ] `DbAccess` 兩個建構式接受 optional `maxCommandTimeout` 參數
- [ ] `DbAccess` 內 9 處 `CreateCommand` 後皆呼叫 `ResolveTimeout` 覆寫 `cmd.CommandTimeout`
- [ ] `IDbAccessFactory` 與 `DbAccessFactory` 存在於 `Bee.Db` 命名空間
- [ ] `BackendInfo.MaxDbCommandTimeout` 屬性與 `BackendConfiguration.MaxDbCommandTimeout` 欄位皆已刪除
- [ ] 兩個操弄 static 的 cap 測試已刪除（不留 `[Obsolete]` 或註解 out 形式）
- [ ] `./test.sh` 全綠
- [ ] GitHub Actions Build CI 通過

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| 「自動 cap 保護」在 Phase 1 後變弱（直接 `new DbAccess` 不生效） | 預期行為；production 路徑在 Phase 3 轉 factory 時恢復；CI / 本機測試環境本就不需要 cap 保護 |
| 某處 production code 依賴 setter 自動 clamp（破壞性） | 本機 `./test.sh` + CI 全綠即可驗證；無外部消費者，影響面僅本 repo |
| `cmd.CommandTimeout` 被 `CreateCommand` 與 DbAccess 各設一次的冗餘 | 行為等價（後者覆寫前者）；犧牲微小性能（一次 int 賦值）換 `CreateCommand` 簽章穩定 |
| `IDbAccessFactory` 沒有立即使用者，可能被誤判為「未使用程式碼」 | XML 文件註解明文：「為 Phase 4 DI 註冊預留」；CA1812 等 analyzer 不會誤報 public interface |

## 提交策略

依使用者規範（[pull-request.md](../../.claude/rules/pull-request.md)）：本機可驗證環境，直接提交 main。

1. 本機跑 `dotnet build --configuration Release` + `./test.sh` 通過
2. 單一 commit 包含所有改動
3. push 後監測 GitHub Actions Build CI

預估 diff 量：< 250 lines
- `DbCommandSpec.cs` 變化：~-15 lines
- `DbAccess.cs` 變化：~+30 lines（兩 ctor 加參數 + ResolveTimeout + 9 處新增 cmd.CommandTimeout 覆寫）
- `IDbAccessFactory.cs` 新增：~40 lines
- `BackendInfo.cs` / `BackendConfiguration.cs` / 測試 XML 刪除：~-15 lines
- `DbCommandSpecTests.cs` 刪除：~-35 lines

## 完成後狀態

- 主計畫頂部「Sub-plan 進度」表 Phase 1 狀態更新為 ✅ 已完成
- `Bee.Db` 對 `BackendInfo` 的耦合從 11 處減為 10 處（移除 `MaxDbCommandTimeout` 那條；剩 `DefineAccess` 系列 9 處 + `LogOptions` 1 處）
- `BackendInfo` 上 DB 相關配置已歸零（前置計畫已處理 `DatabaseType` / `DatabaseId`，本計畫處理 `MaxDbCommandTimeout`）
- `IDbAccessFactory` 介面就緒，待 Phase 4 接入 DI 註冊路徑
