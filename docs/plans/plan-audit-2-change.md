# 計畫（項 2）：異動記錄（st_log_change）

**狀態：✅ 已完成（2026-07-05）**

> 六軸之③「異動記錄」（含軸⑥安全，以 `is_sensitive` 旗標區分）。母計畫見 [plan-audit-trail.md](plan-audit-trail.md)；依賴已完成的 [項 0](plan-audit-0-foundation.md)。
> 採 **DataSet DiffGram 單表**（母計畫 §6.1 方案 D，你先前定案）。

---

## 1. 範圍

**IN**：`st_log_change`（單表 + `changes_xml`）、`ChangeAuditEntry`、在 `FormBusinessObject.Save`/`Delete` 擷取 DiffGram 並寫入、註冊 schema、單元 + `[DbFact]` 測試。

**OUT**：欄位級 EAV 可查詢模式（`st_log_change_field`，選配，之後有需要再開）；報表/查詢 UI。

---

## 2. 關鍵發現（決定架構）

- `FormBusinessObject.Save(SaveArgs args)`（[FormBusinessObject.cs:205](../../src/Bee.Business/Form/FormBusinessObject.cs)）**同時具備**：
  - `args.DataSet` —— 可在 `repository.Save`（line 220）**之前** `GetChanges()` 擷取 before/after。
  - session context（`AccessToken` → `SessionInfo`：who / company）—— 與 `SystemBusinessObject` 同基底。
- `DataFormRepository`（[DataFormRepository.cs:23](../../src/Bee.Repository/Form/DataFormRepository.cs)）**無 session context**（只有 ProgId/schema/databaseId），且其 `Save` 內 `UpdateDataTables` 自開交易、`AcceptChanges` 後 RowState 即被沖掉。

**結論**：BO 層是唯一同時握有「變更資料 + who/company」的地方 → **擷取與寫入都在 `FormBusinessObject.Save`**（`repository.Save` 前擷取、後寫入）。這也讓最簡寫法可**直接重用項 0 的 `IAuditLogWriter`**。

---

## 3. 寫入路徑（**需重新定案**，C1）

先前 D2 定「transactional outbox」，但落到實作，兩案的複雜度差距很大：

| 方案 | 作法 | 一致性 | 複雜度 |
|------|------|--------|--------|
| **A. best-effort 非同步（重評後建議）** | BO 於 `repository.Save` 前 `GetChanges()`；commit 後呼叫 `IAuditLogWriter.Write(changeEntry)`（走項 0 背景 channel → log DB） | 業務 commit 後才寫 log；程序在極小空窗當機會漏該筆 | **低**：純加在 `FormBusinessObject.Save`，零 repository 改動、零新基礎設施 |
| B. transactional outbox（原 D2） | outbox 表放 company DB、於 `repository.Save` 交易內寫入 → 背景 worker flush 到 log DB | 強一致（business commit ⇔ log 落地） | **高**：outbox 表 per company DB + `UpdateDataTables` 需擴充成同交易寫、`IDataFormRepository.Save` 簽章需傳 audit metadata、**多租戶要跨所有 company DB 輪詢 flush** |

**建議改採 A**：理由——
1. 項 0 的 writer 已存在，exec/access 兩軸本就非同步；change 用同一條路徑一致。
2. B 的多租戶 flush（worker 需列舉並輪詢每個 company DB 的 outbox）是實質負擔，且 outbox 表汙染每個業務庫。
3. best-effort 的漏失窗口極小（commit 後到 async enqueue 之間），且項 0 已有「channel 滿退化同步 + log DB 掛落檔」的韌性；可再對 change 這種高價值日誌**強制同步寫**（不入 channel、直接 sink）進一步縮小窗口。
4. 若日後稽核合規要求「零漏失」，再升級為 B —— entry 模型與 `st_log_change` schema 不變，屬 additive。

> 本 plan 後續章節以 **A** 撰寫；若你堅持 B，§5/§6 需改寫（outbox 表 + worker + 簽章）。

---

## 4. 資料表 `st_log_change`（單表）

| 欄位 | DbType | Null | 說明 |
|------|--------|------|------|
| `sys_no` | AutoIncrement | — | 流水號 PK |
| `sys_rowid` | Guid | — | 唯一識別 |
| `log_time` | DateTime | — | 事件時刻（UTC） |
| `user_id` / `user_name` | String | ✔ | 觸發者（去正規化，日誌獨立） |
| `company_id` / `company_name` | String | ✔ | 租戶（去正規化） |
| `access_token` | Guid | ✔ | session |
| `trace_id` | Guid | ✔ | 關聯 id |
| `client_ip` / `source` | String | ✔ | 來源；`source="ProgId.Save"` |
| `prog_id` | String | — | 業務物件 |
| `table_name` | String | — | 主表名 |
| `row_key` | String | ✔ | 主檔 `sys_rowid`（可查「某單所有異動」） |
| `change_kind` | Integer | — | 由主檔 RowState 導出：Insert / Update / Delete |
| `is_sensitive` | Boolean | — | 軸⑥安全旗標（見 C2） |
| `changes_xml` | Text/CLOB | — | `GetChanges()` 的 **DiffGram** XML（master+detail、多列、多欄、新舊值） |

- 共通欄沿用 `AuditEntry`（項 1 已含 `sys_rowid`/去正規化欄）。
- 索引：`pk_{0}`(sys_no)、`rx_{0}`(sys_rowid)、建議另加 `row_key` / `prog_id` 一般索引助查詢。

---

## 5. `ChangeAuditEntry`（`Bee.Definition.Logging`）

```csharp
public enum ChangeKind { Insert = 1, Update = 2, Delete = 3 }

public sealed class ChangeAuditEntry : AuditEntry
{
    public override string TableName => "st_log_change";
    public string ProgId { get; init; } = "";
    public string ChangeTableName { get; init; } = "";  // 對映欄位 table_name
    public string? RowKey { get; init; }
    public ChangeKind ChangeKind { get; init; }
    public bool IsSensitive { get; init; }
    public string ChangesXml { get; init; } = "";

    protected override void AddColumns(IList<AuditColumn> columns)
    {
        columns.Add(new("prog_id", ProgId));
        columns.Add(new("table_name", ChangeTableName));
        columns.Add(new("row_key", RowKey));
        columns.Add(new("change_kind", (int)ChangeKind));
        columns.Add(new("is_sensitive", IsSensitive));
        columns.Add(new("changes_xml", ChangesXml));
    }
}
```

---

## 6. 掛勾（`FormBusinessObject`）

**Save**（[FormBusinessObject.cs:205](../../src/Bee.Business/Form/FormBusinessObject.cs)）：
```csharp
using var changes = args.DataSet.GetChanges();          // 擷取在 repository.Save 之前
var (refreshed, affected) = repository.Save(args.DataSet);
WriteChangeAudit(changes, ProgId, ...);                  // commit 後、best-effort
```
`WriteChangeAudit`：`ChangeEnabled` gating → 序列化 `changes.WriteXml(sw, XmlWriteMode.DiffGram)`（**鐵則：必用 DiffGram，普通 WriteXml 丟舊值**）→ 從 session 補 who/company → 主檔 RowState 導 `change_kind`、主檔 sys_rowid 為 `row_key` → `IAuditLogWriter.Write`。

**Delete**（line 234）：稽核開啟時**刪除前先 `GetData(rowId)` 載入 master+明細**（snapshot）→ 刪除 → 把 snapshot 所有列標記為 Deleted → `GetChanges()` 產生含**完整 before-image** 的 DiffGram（被刪記錄每欄舊值）→ 寫入。格式與 Save 一致。只在稽核開啟時多一次讀；載不到才退回只記 key。

**取 writer/options**：`Services.GetService<IAuditLogWriter>()` / `AuditLogOptions`（同項 1 escape-hatch）。

---

## 7. 註冊與測試

- `TableSchema/log/st_log_change.TableSchema.xml`（Defaults + tests）；`DbCategorySettings` log 分類加 `st_log_change`（Defaults + tests）；`DefaultsTests` 數量 25→26、TableSchema 12→13。
- 單元：`ChangeAuditEntry` 欄位；DiffGram 序列化能還原新舊值（建立含 Modified 列的 DataSet → GetChanges → WriteXml(DiffGram) → ReadXml → 驗證 Original/Current）。
- `[DbFact]`（SQL Server/PostgreSQL）：寫一筆 `st_log_change`（含 changes_xml）→ 讀回。

---

## 8. 決策紀錄（已定案 2026-07-05）

- **C1 寫入路徑 → best-effort 非同步（推翻 D2 outbox）**：BO 於 `repository.Save` 前 `GetChanges()`、commit 後呼叫項 0 `IAuditLogWriter`。零 repository 改動、零新基礎設施。對 change 這類高價值日誌可強制同步寫（不入 channel、直接 sink）以縮小漏失窗口。日後若需零漏失再升級 outbox（entry/schema 不變、additive）。
- **C2 is_sensitive → 預設 false + 日後掛勾**：本輪欄位保留但固定 false；真正要區分敏感表單時再接來源（FormSchema 旗標或清單）。
- **C3 Delete → 記錄完整 before-image（採建議補強）**：`FormBusinessObject.Delete` 於刪除前 `GetData(rowId)` 載入 master+明細，標記所有列為 Deleted 後序列化 DiffGram，`changes_xml` 含被刪記錄每欄舊值（與 Save 同格式）。僅在稽核開啟時多一次讀；載不到才退回只記 key。
- **C4 change_kind → 由主檔 RowState 導出**：Added→Insert / Deleted→Delete / 其餘→Update。
- **C5 空變更 → 不寫**：`GetChanges()` 為 null（無異動）時略過，對齊 Save no-op。

---

## 9. 相依與後續
- 前置：項 0（已完成）、項 1（已完成，`AuditEntry`/schema 慣例）。
- 完成後接：**項 3 執行記錄**（掛 `JsonRpcExecutor` 單一收斂點）。
