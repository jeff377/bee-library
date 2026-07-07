# 計畫（項 1）：登入記錄（st_log_login）

**狀態：✅ 已完成（2026-07-05）**

> 六軸之①「登入記錄」。母計畫見 [plan-audit-trail.md](plan-audit-trail.md)；依賴已完成的 [項 0 基礎設施](plan-audit-0-foundation.md)。
> 本項是第一個實際使用 `IAuditLogWriter` 的軸，同時端到端驗證「entry → 落 log DB」整條管線。

---

## 1. 範圍

**本項要做（IN）**
- `st_log_login` 表（TableSchema/log/ + DbCategorySettings log 分類註冊；Defaults 與 tests/Define 兩處）。
- `LoginAuditEntry` + `LoginEvent` 列舉（`Bee.Definition.Logging`）。
- 在 `SystemBusinessObject.Login` / `Logout` 掛 `IAuditLogWriter.Write`（登入成功 / 失敗 / 鎖定 / 登出）。
- 單元測試（entry 欄位）+ `[DbFact]` 整合測試（寫入 st_log_login 後讀回驗證）。

**本項不做（OUT）**
- `client_ip` 的來源貫穿（BO 無 `HttpContext`；見 §7 L5，另案處理）。
- 其他軸（異動 / 執行 / 檢視）。

---

## 2. 資料表 `st_log_login`

| 欄位 | DbType | Null | 說明 |
|------|--------|------|------|
| `sys_no` | AutoIncrement | — | 流水號（PK、寫入順序，兼 hash-chain 排序鍵） |
| `sys_rowid` | Guid | — | 唯一識別（見 §7 L4） |
| `log_time` | DateTime | — | 事件時刻（UTC） |
| `user_id` | String | ✔ | 登入帳號（`args.UserId`；失敗時仍有） |
| `user_name` | String | ✔ | 顯示名稱（成功時 `AuthenticateUser` 回傳；失敗 null）——去正規化，免 join `st_user` |
| `company_id` | String | ✔ | 登入為 pre-company → null |
| `company_name` | String | ✔ | 登入為 pre-company → null |
| `access_token` | Guid | ✔ | 成功時為新 session token；失敗/鎖定為 null |
| `trace_id` | Guid | ✔ | 登入不與子操作關聯 → null（見 §7 L5） |
| `client_ip` | String | ✔ | 目前 null（見 §7 L5） |
| `source` | String | ✔ | `"System.Login"` / `"System.Logout"` |
| `event` | Integer | — | `LoginEvent` 列舉（見 §4） |
| `fail_reason` | String | ✔ | 失敗/鎖定原因（**不含明文密碼**，見 security.md） |

- 索引：`pk_{0}`(sys_no)、`rx_{0}`(sys_rowid unique)。無 `access_token` unique（日誌可重複）。
- `DbName="log"`；註冊到 `DbCategorySettings` 的 `<DbCategory Id="log">`（目前 `<Tables />` 為空）。
- 共通欄位順序對齊 `AuditEntry.GetColumns()`（項 0 已定），append-only。

---

## 3. 掛勾點（`SystemBusinessObject`，`Bee.Business`）

`Login`（[SystemBusinessObject.cs:72-126](../../src/Bee.Business/System/SystemBusinessObject.cs)）三個出口：

| 出口 | 位置 | event | 資料 |
|------|------|-------|------|
| 鎖定 | line 80-81 throw 前 | `LockedOut` | user_id=args.UserId, fail_reason="帳號鎖定" |
| 認證失敗 | line 86-87 throw 前 | `LoginFailed` | user_id=args.UserId, fail_reason="帳密錯誤" |
| 成功 | line 106 建立 session 後 | `LoginSucceeded` | user_id=args.UserId, user_name=userName, access_token=sessionInfo.AccessToken |

`Logout`（line 221-236）：`SessionInfoService.Remove` 前寫一筆 `Logout`（user_id 來自 sessionInfo，若存在；access_token=當前 token）。

**取得 writer**：沿用既有 escape-hatch 模式 `Services.GetService<IAuditLogWriter>()`（項 0 已保證恆有註冊，停用時為 `NullAuditLogWriter`），與 `Login` 內取 `ILoginAttemptTracker` 同法，**不改 ctor**。

**掛在 base `Login`/`Logout`**：app 子類通常只覆寫 `AuthenticateUser`（line 265），不覆寫 `Login`，故 base 內埋點即覆蓋所有 app（見 §7 L6 假設）。寫入在 throw **之前**。

---

## 4. `LoginAuditEntry` + `LoginEvent`（`Bee.Definition.Logging`）

```csharp
public enum LoginEvent { LoginSucceeded, LoginFailed, LockedOut, Logout }

public sealed class LoginAuditEntry : AuditEntry
{
    public override string TableName => "st_log_login";
    public LoginEvent Event { get; init; }
    public string? FailReason { get; init; }

    protected override void AddColumns(IList<AuditColumn> columns)
    {
        columns.Add(new AuditColumn("event", (int)Event));
        columns.Add(new AuditColumn("fail_reason", FailReason));
    }
}
```

`event` 列舉自帶結果（成功/失敗/鎖定/登出），**不另設 result 欄**（見 §7 L1）。

---

## 5. 註冊與 schema 建立

1. `src/Bee.Definition/Defaults/TableSchema/log/st_log_login.TableSchema.xml`（新資料夾 `log/`）。
2. `src/Bee.Definition/Defaults/DbCategorySettings.xml` 的 `<DbCategory Id="log">` 加 `<TableItem TableName="st_log_login" DisplayName="Login log" />`。
3. `tests/Define/TableSchema/log/st_log_login.TableSchema.xml` + `tests/Define/DbCategorySettings.xml` 同步（讓 `SharedDbFixture` 建 log DB schema，供 `[DbFact]` 用）。

---

## 6. 測試

- **單元**（Bee.Definition.UnitTests）：`LoginAuditEntry.GetColumns()` 含 `event`/`fail_reason` + 共通欄；`TableName == "st_log_login"`。
- **整合 `[DbFact]`**（SQL Server / PostgreSQL）：透過 `IAuditLogWriter`（同步模式）寫一筆 `LoginSucceeded` 到 log DB，再 `SELECT` 讀回驗證欄位。這是**首個端到端驗證**（entry→INSERT→log DB→讀回）。
- 需 `AuditLogOptions.Enabled=true` + `UseBackgroundWriter=false`（同步，測試好斷言）的 fixture 設定。

---

## 7. 決策紀錄（已定案 2026-07-05）

- **L1 事件模型 → `event` 列舉自帶結果 + `fail_reason`**，不設獨立 `result` 欄（對齊 res.users.log / SAL message-id）。
- **L2 涵蓋事件 → 成功 / 失敗 / 鎖定 / 登出 全記**（四出口皆埋點）。
- **L3 per-category 開關 → BO 檢查 `AuditLogOptions.LoginEnabled`**（全域 `Enabled` 之下再細粒度 gating）。
- **L4 主鍵 → `AuditEntry` base 加 `sys_rowid`（Guid）**：每筆日誌帶 Guid rowid，符框架慣例並利 item 4 hash-chain。屬對已完成項 0 的小幅擴充（`AuditEntry.GetColumns()` 於共通欄最前面加 `sys_rowid = Guid.NewGuid()`）。
- **L5 client_ip → 登入先存 null**（BO 無 `HttpContext`）；IP 貫穿列為跨切面另案。
- **L6 hook 位置 → base `SystemBusinessObject.Login`/`Logout`**（假設 app 只覆寫 `AuthenticateUser`，不覆寫 `Login`）。
- **L7 日誌獨立性 → 共通欄去正規化，不 join**：log DB 與 common/company 實體分離、跨 DB join 不可行，故 `AuditEntry` base 共通欄**移除 `user_rowid`**、**新增 `user_name` + `company_name`**（去正規化顯示值）。login 寫入時 `user_name` 取自 `AuthenticateUser` 的 `userName`（成功時）。連同 L4 的 `sys_rowid`，一併為對已完成項 0 `AuditEntry` 的擴充。詳見母計畫 [§4.2](plan-audit-trail.md)。

---

## 8. 影響 / 新增檔案（預估）

| 動作 | 檔案 |
|------|------|
| 新增 | `src/Bee.Definition/Logging/LoginAuditEntry.cs`（+ `LoginEvent`） |
| 修改 | `src/Bee.Definition/Logging/AuditEntry.cs`（L4 若採 sys_rowid） |
| 新增 | `src/Bee.Definition/Defaults/TableSchema/log/st_log_login.TableSchema.xml` |
| 修改 | `src/Bee.Definition/Defaults/DbCategorySettings.xml` |
| 新增/修改 | `tests/Define/TableSchema/log/st_log_login.TableSchema.xml`、`tests/Define/DbCategorySettings.xml` |
| 修改 | `src/Bee.Business/System/SystemBusinessObject.cs`（Login/Logout 埋點） |
| 測試 | `tests/Bee.Definition.UnitTests/Logging/`（entry）、`tests/Bee.Business.UnitTests/`（`[DbFact]` 端到端） |

---

## 9. 相依與後續
- 前置：項 0（已完成）。
- 完成後接：**項 2 異動記錄**（DiffGram + outbox，較複雜）。
