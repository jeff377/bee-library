# 計畫（項 4）：異常記錄（st_log_anomaly）

**狀態：✅ 已完成（2026-07-08）**

> **已知限制**：DB 異常偵測掛在 `DbAccess.Execute`（Query/Scalar/NonQuery 主路徑）；`ExecuteBatch` / `UpdateDataTables`（交易批次）本輪未含，之後有需要再補。逾時判定為 heuristic（例外訊息含 "timeout" 或 elapsed ≈ command timeout）。log DB 自身的 `DbAccess` 不做異常偵測以避免遞迴。

> 原「系統/錯誤（軸⑤）」重新定義為 **異常記錄**——針對 **API 與 DB** 的異常做處理並持久化。母計畫見 [plan-audit-trail.md](plan-audit-trail.md)；依賴已完成的 [項 0](plan-audit-0-foundation.md)。
> **取代先前模糊的「執行記錄全記」**：全記量體大又與登入/異動重複；異常記錄只記「有問題的」，量體天然低、定位清楚（維運 + 效能調校 + bug 追蹤）。

---

## 1. 異常三分類（＋ DB 量體類）

| 類型 | 認定 | 用途 | 是 code bug？ |
|------|------|------|--------------|
| **Error 錯誤** | 捕捉到例外 | 需修 bug | ✅ |
| **Timeout 逾時** | timeout 例外（DB command timeout / API 逾時） | **獨立出來**做效能/容量參考 | ❌（infra/效能訊號） |
| **Slow 過久** | 完成但 elapsed > 設定警界值 | 效能警示參考 | ❌ |
| **LargeAffected / LargeResult**（DB） | 影響列數 / 回傳列數 > 門檻 | 大量更新/查詢警示 | ❌ |

**逾時獨立**是關鍵：逾時不是邏輯或程式碼錯誤，混進 Error 會誤導 bug 排查；獨立後可聚合分析「哪些操作常逾時」做效能優化。

---

## 2. 既有基礎（已設計、未實作）

框架**已有** `DbAccessAnomalyLogOptions`（`LogOptions.DbAccess`），含門檻 `ExecutionTimeThreshold`（慢）/ `AffectedRowThreshold` / `ResultRowThreshold` + `Level`（Error/Warning）——**但全 src 無消費者**（定義了沒實作）。本項即把它**實作出來 + 擴到 API 層 + 持久化到 log DB**。

---

## 3. 資料表（**API / DB 分開**——視角不同、記的資訊不同）

### 3.1 `st_log_anomaly_api`（API 動作異常，有 session context）

| 欄位 | DbType | Null | 說明 |
|------|--------|------|------|
| `sys_no` / `sys_rowid` | AutoIncrement / Guid | — | 鍵 |
| `log_time` | DateTime | — | 事件時刻（UTC） |
| who/company/access_token/client_ip | String/Guid | ✔ | 沿用 `AuditEntry` 共通欄（去正規化；API 有 session） |
| `method` | String | — | `"ProgId.Action"`——**哪個動作**出異常 |
| `anomaly_kind` | Integer | — | `Error` / `Timeout` / `Slow` |
| `elapsed_ms` / `threshold_ms` | Integer | ✔ | 耗時 / Slow 門檻 |
| `error_type` / `error_message` | String | ✔ | 例外型別 / **已消毒**訊息（無堆疊/路徑） |

### 3.2 `st_log_anomaly_db`（DB 指令異常，技術視角、無 session）

| 欄位 | DbType | Null | 說明 |
|------|--------|------|------|
| `sys_no` / `sys_rowid` | AutoIncrement / Guid | — | 鍵 |
| `log_time` | DateTime | — | 事件時刻（UTC） |
| `database_id` | String | — | **哪個 DatabaseID** 出異常 |
| `command` | Text | — | SQL **模板**（`CommandText`，`{0}` 佔位、**不含參數值**，見 §5） |
| `anomaly_kind` | Integer | — | `Error` / `Timeout` / `Slow` / `LargeAffected` / `LargeResult` |
| `elapsed_ms` / `threshold_ms` | Integer | ✔ | 耗時 / 門檻 |
| `affected_rows` / `result_rows` | Integer | ✔ | 大量列數類用 |
| `error_type` / `error_message` | String | ✔ | 例外型別 / **已消毒**訊息 |

- **DB 表無 who/company**：`DbAccess` 無 session context，本來就取不到——精簡且準確，關鍵是 `database_id` + `command`。
- 量體天然低（只記異常），**無需量體閘門**。索引：`pk`/`rx` + `anomaly_kind`（+ DB 表 `database_id`）助分析。

---

## 4. 掛勾（兩個單一收斂點）

**API —— `JsonRpcExecutor.ExecuteAsyncCore`**（[line 71](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs)）
- 已有 `Tracer.Start`（`ctx` 帶 `Stopwatch` → elapsed）+ try/catch(Exception)。
- `catch` 區塊：分類 **Timeout**（例外為 timeout 型）vs **Error**（其他）→ 寫異常記錄。
- 成功但 `elapsed > ApiSlowThreshold` → **Slow**。

**DB —— `DbAccess.Execute`**（[line 128](../../src/Bee.Db/DbAccess.cs)）
- 包 `Stopwatch` + try/catch 於 `Execute` 收斂點：**Timeout**（`DbException` 逾時）/ **Error**（其他 `DbException`）；完成後 `elapsed > ExecutionTimeThreshold` → **Slow**；影響/回傳列數 > 門檻 → LargeAffected/LargeResult（N2）。
- 沿用既有 `DbAccessAnomalyLogOptions` 門檻。

**兩個 entry**（`Bee.Definition.Logging`，走現成 `IAuditLogWriter`，best-effort）：
- `ApiAnomalyEntry`：subclass `AuditEntry`，用共通欄（who/company/token/ip）+ `method`/`kind`/`timing`/`error`。
- `DbAnomalyEntry`：**精簡**——不帶 who/company（DbAccess 無 session）。需讓 `AuditEntry` 的共通欄可被覆寫（把共通欄抽成 `protected virtual AddCommonColumns`；`DbAnomalyEntry` override 為只留 `sys_rowid`/`log_time`，再 `AddColumns` 加 `database_id`/`command`/…）。此為對已完成項 0 `AuditEntry` 的小幅、向後相容擴充（現有 Login/Change/Access entry 行為不變）。

---

## 5. 安全（硬性）

- **不記完整 SQL 與參數值**（scanning.md）：`source` 存 `CommandText` **模板**（`{0}` 佔位），參數值一律不入庫。
- **不記堆疊/內部路徑**（security.md）：`error_message` 消毒後才寫；`error_type` 存型別名即可。

---

## 6. 決策紀錄（已定案 2026-07-08）

- **N1 → 取代「執行記錄全記」**：異常記錄取代原 exec-log 全記；audit 面由登入/異動/檢視涵蓋，`Tracer` 不作稽核來源。
- **N2 → Error / Timeout / Slow + DB LargeAffected / LargeResult**：五類。前三對齊你的定義（跨 API+DB）；後兩為 DB 專屬（沿用既有 `AffectedRowThreshold` / `ResultRowThreshold`）。
- **N3 → DbAccessFactory 持有 writer + 分置 options**：
  - DB 寫入管道：`DbAccessFactory`（已 DI）注入 `IAuditLogWriter`，`Create()` 時傳入 `DbAccess`；`DbAccess` 偵測到異常直接寫（best-effort）。
  - options：擴充 `AuditLogOptions`（加 `AnomalyEnabled` + API `SlowThresholdMs`）；DB 門檻沿用既有 `LogOptions.DbAccess`（`DbAccessAnomalyLogOptions`，本項首度實作其消費）。
- **N4 → API / DB 兩表**（修正原單表決定）：`st_log_anomaly_api`（動作 + who）與 `st_log_anomaly_db`（databaseId + command，無 who）。兩者視角不同、記的資訊不同，分表才乾淨（`DbAccess` 無 session，DB 表本就無 who）。表名 anomaly 在前成群。
- **N5 → API + DB 一起做**。

---

## 7. 相依與後續
- 前置：項 0（已完成）。
- 完成後：稽核軌跡六軸收斂完成（登入/異動/檢視/異常 + 安全併入異動；系統觀測歸 ILogger）。收尾補 [framework-reserved-names.md](../framework-reserved-names.md) §1 log 分類。
