# 計畫（項 0）：日誌基礎設施

**狀態：✅ 已完成（2026-07-05）**

> 六軸日誌的**前置工作項**。母計畫與研究藍本見 [plan-audit-trail.md](plan-audit-trail.md)。
> 本項定案後才動工，完成後其餘工作項（執行/登入/異動/檢視/系統）皆依賴之。

---

## 1. 範圍

**本項要做（IN）**

- `IAuditLogWriter` 抽象 + 預設實作（業務程式碼寫日誌的唯一入口）。
- **事件型日誌的非同步寫入通道**：exec / login / access 這類「不在業務交易內」的日誌，經記憶體 channel → 背景批次 INSERT → log DB。
- `AuditEntry` 基底型別 + 共通欄位模型（見 [plan-audit-trail.md](plan-audit-trail.md) §4.2）。
- 背景寫入服務（`BackgroundService`，對齊 `CacheNotifyPoller` 先例）。
- `AuditLogOptions` 設定（總開關 + 各類別開關 + 批次/間隔），對齊 `CacheNotifyOptions` 放置慣例。
- `trace_id` 貫穿機制（一次 API 呼叫的關聯 id）。
- DI 註冊（`AddBeeFramework` 依 `Enabled` 掛載）。
- 無 host 環境（純 Local sample / 單元測試）的降級路徑。

**本項不做（OUT，交給後續工作項）**

- 具體日誌表 schema 與掛勾（各軸自己的工作項）。
- **transactional outbox 機制**：只有異動記錄（項 2）需要業務交易內落地，故 outbox worker 隨項 2 一起做（見 §6 F1）。本項只把「事件型非同步寫入」打通。

---

## 2. 架構

### 2.1 兩條寫入路徑（本項只做第一條）

```
事件型（exec/login/access）───► IAuditLogWriter.Write(entry)
                                     │ 入記憶體 channel（非阻塞）
                                     ▼
                          AuditLogWriterService（BackgroundService）
                                     │ 批次、依目標表分組
                                     ▼
                          IDbAccessFactory → DbAccess(DbCategoryIds.Log) → INSERT

異動型（change，屬項 3）──────► 業務交易內寫 outbox 列（同業務 DB，強一致）
                                     ▼ 背景 flush worker（項 3 引入）
                          搬到 log DB 後標記/刪除
```

事件型日誌**不在業務交易內**（exec 無自身 DB 寫入、login 寫 common），故直接走非同步 channel 即可；不需要 outbox 的交易保證。異動型才需要 outbox（項 3）。

### 2.2 落表路由

事件型固定落 `DbCategoryIds.Log`（`"log"`），**不需 access token**——`RepositoryDatabaseRouter.Resolve(DbScope.Log, _)` 對 Common/Log 是固定 databaseId（[`src/Bee.Repository/RepositoryDatabaseRouter.cs`](../../src/Bee.Repository/RepositoryDatabaseRouter.cs) 註解已預留此情境：「pre-EnterCompany 方法可寫 audit log 而不需公司情境」）。寫入端用 `IDbAccessFactory` 取得 `DbAccess`，不 `new DbAccess(...)`。

### 2.3 介面與型別（草案）

```csharp
// Bee.Definition.Logging（或 Bee.Base）——抽象與 entry 型別
public interface IAuditLogWriter
{
    // 非阻塞入列；實作決定滿載/失敗策略（見 F2）
    void Write(AuditEntry entry);
}

// 共通欄位基底（對應 plan-audit-trail §4.2）
public abstract class AuditEntry
{
    public DateTime LogTimeUtc { get; init; }      // 事件時刻（UTC）
    public Guid? UserRowId { get; init; }
    public string? UserId { get; init; }
    public string? CompanyId { get; init; }
    public Guid? AccessToken { get; init; }
    public Guid? TraceId { get; init; }
    public string? ClientIp { get; init; }
    public string? Source { get; init; }           // "ProgId.Action" / channel
    // 子類：ExecAuditEntry / LoginAuditEntry / AccessAuditEntry / ChangeAuditEntry
    // 每個子類知道自己的目標表名與欄位對映（INSERT 由 writer 依型別分派）
}
```

### 2.4 背景寫入服務

`AuditLogWriterService : BackgroundService`（`Bee.Hosting`），骨架對齊 [`CacheNotifyPoller`](../../src/Bee.Hosting/CacheNotify/CacheNotifyPoller.cs)：
- 建構子注入 `IDbAccessFactory` + `AuditLogOptions` + `ILogger`。
- `ExecuteAsync`：`await foreach` 讀 channel（或 `PeriodicTimer` + drain），累到 batch size / flush 間隔就分組 INSERT。
- 單次 flush 失敗只 log 並續跑（韌性迴圈，對齊 `SafePoll` 精神），失敗批次依 F2 策略處理（重試/落檔）。
- `AddBeeFramework` 在 `AuditLogOptions.Enabled` 為真時註冊。

### 2.5 trace_id 貫穿

於 `JsonRpcExecutor` 進入點產生 `trace_id`（或重用 `TraceContext`），透過 ambient context（`AsyncLocal` / 傳入 `SessionInfo`）向下傳遞，使同一次呼叫產生的 exec/change/access entry 共用同一 `trace_id`。本項建立傳遞管道；各軸於自己的工作項填值。

---

## 3. 影響 / 新增檔案（預估）

| 動作 | 檔案 | 說明 |
|------|------|------|
| 新增 | `src/Bee.Definition/Logging/IAuditLogWriter.cs`、`AuditEntry.cs`（+ 子類） | 抽象與 entry 型別 |
| 新增 | `src/Bee.Definition/Settings/SystemSettings/AuditLogOptions.cs` | 設定（對齊 `CacheNotifyOptions` 位置） |
| 新增 | `src/Bee.Hosting/Audit/AuditLogWriterService.cs` | 背景寫入服務 |
| 新增 | 預設實作（channel writer）+ `NullAuditLogWriter`（無 host / 關閉時） | 對齊 `NullLogWriter` 慣例 |
| 修改 | `AddBeeFramework` DI 註冊處 | 依 `Enabled` 掛載 |
| 測試 | `tests/...` | writer 入列/批次/降級/關閉 的單元測試 |

> 抽象放 `Bee.Definition.Logging`（與 `DbAccessAnomalyLogOptions` 同區）待 F 決策確認；背景服務放 `Bee.Hosting`（與 `CacheNotifyPoller` 同）。

---

## 4. 驗收（本項完成的定義）

- `IAuditLogWriter.Write` 可被呼叫、非阻塞；`Enabled=false` 或無 host 時走降級/Null 實作不報錯。
- 背景服務能把入列的 `AuditEntry`（用一個臨時測試子類）批次寫入 log DB（`[DbFact]` 對 SQL Server / PostgreSQL 驗證）。
- 韌性：log DB 暫時不可用時不崩、依 F2 策略不遺失（或明確記錄降級）。
- `trace_id` 能從進入點傳遞到 writer。
- 零回歸：`Enabled=false` 為預設，未啟用時對現有流程零影響。

---

## 5. 決策紀錄（已定案 2026-07-05）

- **F1 outbox worker 歸屬 → 併入項 2 異動**。foundation 保持薄，只打通事件型非同步；outbox（change 專屬）隨項 2。
- **F2 不可漏記等級 → bounded channel + 滿時退化為同步 INSERT**。平時非同步批次；channel 滿時該筆改同步直寫（不丟）。疊加持久性：log DB 不可用時 → 重試 N 次 → 落地本機檔 fallback，避免遺失。
- **F3 無 host 環境 → 可設定，預設同步直寫**。有 host（AspNetCore/Console）走背景服務；無 host（純 Local sample）預設同步直寫（不漏記）；單元測試用 `NullAuditLogWriter`。
- **F6 抽象放置層 → `Bee.Definition.Logging`**（`IAuditLogWriter` / `AuditEntry` 與既有 `DbAccessAnomalyLogOptions` 等 logging 型別同區）。

**採建議、未另議（低風險，實作時如需可回頭調整）**

- **F4 entry 型別 → `AuditEntry` 基底 + 型別化子類**（`ExecAuditEntry` / `LoginAuditEntry` / …）；INSERT 由 writer 依型別分派。
- **F5 共通欄位 → 採 [plan-audit-trail.md](plan-audit-trail.md) §4.2**；`client_ip` 於 Local call 無 `HttpContext` 時存 null（可接受）。

---

## 6. 相依與後續

- 前置：無。
- 阻擋：項 1–5 全部依賴本項完成。
- 完成後接：**項 1 登入記錄**（第一個實際使用本基礎設施、端到端驗證寫入管線的軸）。
