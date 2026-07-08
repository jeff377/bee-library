# 計畫（項 3）：檢視記錄（st_log_access）

**狀態：✅ 已完成（2026-07-07）**

> 六軸之②「檢視記錄」。母計畫見 [plan-audit-trail.md](plan-audit-trail.md)；依賴已完成的 [項 0](plan-audit-0-foundation.md)。
> **量體是本項的核心約束**——讀取遠多於寫入，故設計為 **opt-in + 敏感度驅動 + 只掛檢視單筆**。

---

## 1. 量體控制（核心設計）

檢視記錄若全記必爆（每次 `GetData`/`GetList` 都寫）。三層閘門把量體壓到「只記真正該記的」：

1. **opt-in**：`AuditLogOptions.AccessEnabled` 預設 **false**（項 0 已定）。不開就零成本。
2. **只掛檢視單筆（`GetData`）**：GDPR 型需求「誰看了某筆記錄」＝**明細檢視**。清單查詢（`GetList`）量體大，**不掛**。GetData 屬明細檢視、量體有界（使用者一次開一筆）。
3. **觸發範圍可再收斂**：哪些表單的檢視要記，見 **A1**（所有表單 / 只含敏感欄 / per-form opt-in）——進一步壓低量體。

對齊 SAP RAL（只針對指定敏感欄 + channel）與 Odoo auditlog（read 模式昂貴、per-rule 才開）。

對齊 SAP RAL（只針對指定敏感欄 + channel）與 Odoo auditlog（read 模式昂貴、per-rule 才開）。

---

## 2. 資料表 `st_log_access`

| 欄位 | DbType | Null | 說明 |
|------|--------|------|------|
| `sys_no` | AutoIncrement | — | 流水號 PK |
| `sys_rowid` | Guid | — | 唯一識別 |
| `log_time` | DateTime | — | 事件時刻（UTC） |
| `user_id` / `user_name` | String | ✔ | 觸發者（去正規化） |
| `company_id` / `company_name` | String | ✔ | 租戶（去正規化） |
| `access_token` | Guid | ✔ | session |
| `trace_id` | Guid | ✔ | 關聯 id |
| `client_ip` / `source` | String | ✔ | 來源；`source="ProgId.GetData"` |
| `prog_id` | String | — | 業務物件 |
| `row_key` | String | ✔ | 被檢視記錄的 `sys_rowid` |

- **記錄粒度 = per-record（不記欄位）**：一次 `GetData`＝檢視一整筆＝一列 log（誰、看了哪筆）。GetData 本就載入整筆完整資料，欄位級無必要。
- 共通欄沿用 `AuditEntry`。索引：`pk_{0}`、`rx_{0}`、`ix_..._row_key`（查「某記錄被誰看過」）。

---

## 3. `AccessAuditEntry`（`Bee.Definition.Logging`）

```csharp
public sealed class AccessAuditEntry : AuditEntry
{
    public override string TableName => "st_log_access";
    public string ProgId { get; init; } = "";
    public string? RowKey { get; init; }

    protected override void AddColumns(IList<AuditColumn> columns)
    {
        columns.Add(new("prog_id", ProgId));
        columns.Add(new("row_key", RowKey));
    }
}
```

---

## 4. 掛勾（`FormBusinessObject.GetData`，[line 190](../../src/Bee.Business/Form/FormBusinessObject.cs)）

```csharp
var dataSet = repository.GetData(args.RowId, ResolveScopeFilter(PermissionAction.Read));
if (dataSet != null && AccessAuditEnabled())     // 全記（未來加 per-form 規則，見 §6）
    WriteAccessAudit(args.RowId, ProgId + ".GetData");
return new GetDataResult { DataSet = dataSet };
```

- `AccessAuditEnabled()`：`AuditLogOptions is { Enabled: true, AccessEnabled: true }`（同項 1/2 escape-hatch）。
- `WriteAccessAudit`：記 who（session 去正規化）+ prog_id + row_key（=args.RowId）→ `IAuditLogWriter.Write`（best-effort，同項 2）。**不記欄位**。

---

## 5. 註冊與測試

- `TableSchema/log/st_log_access.TableSchema.xml`（Defaults + tests）；`DbCategorySettings` log 加 `st_log_access`；`DefaultsTests` 數量 26→27、TableSchema 13→14。
- 單元：`AccessAuditEntry` 欄位；`GetSensitiveFields` 對含/不含敏感欄的 schema 回傳正確。
- `[DbFact]`（SQL Server/PostgreSQL）：寫一筆 `st_log_access` → 讀回。

---

## 6. 決策紀錄與待定案

**已定（2026-07-07）**
- **掛勾範圍 → 只掛 `GetData`**（明細檢視），`GetList` 清單不掛（量體）。
- **記錄粒度 → per-record，不記欄位**：一次檢視一列（who + prog_id + row_key）。GetData 本就載入整筆，欄位級無必要。

- **A1 觸發範圍 → 所有表單**（與項 2 異動一致）：`AccessEnabled` 開啟後，每次 `GetData` 都記一筆。GetData 屬明細檢視、量體有界；量體再壓低靠未來的 per-form 規則。

**未來增強（涵蓋項 2 異動 + 項 3 檢視，另案）**
- **per-form 稽核規則（admin 執行期）**：一份「哪些 progId 要做異動/檢視記錄」的設定（per-form、per-操作），對齊 **Odoo `auditlog.rule`**——管理員挑要稽核的表單，不改程式或定義檔。SAP 對照：Change Documents 開發期旗標 / RAL 管理員設定。屆時 `ShouldAuditChange` / `ShouldAuditView` 改讀此規則；預設維持現行「全記」以相容。

---

## 7. 相依與後續
- 前置：項 0（已完成）。
- 完成後接：項 4 執行記錄（暫緩，範圍待決定）。軸⑤系統/錯誤已移出稽核範圍（observability，走 ILogger）。
