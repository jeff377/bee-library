# 計畫：Northwind 案例改走框架的 `EnterCompany`（維持單一公司）

**狀態：🚧 進行中（2026-08-28）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 後端：seed `st_company` / `st_user_company`、建三張角色表、刪除兩個捷徑類別 | ✅ 已完成（2026-08-28） |
| 2 | 前端：登入成功後自動 `EnterCompanyAsync`（不做公司選單 UI） | ✅ 已完成（2026-08-28） |
| 3 | 文件與鏡像：雙語 README、`bee-northwind-avalonia` 同步 | 🚧 進行中 |

> **執行裁定**：使用者於 2026-08-28 裁定「直接改程式」，並選擇**不延後**到 Day 13 發佈之後
> —— 第六節原建議的「Day 13 照常發佈、之後才動 code」因此不適用，Day 13 的案例段改由
> 原 session 修訂。第六節保留原判讀作為紀錄，處置一欄已依裁定更新。

---

## 一、結論先講

**原建議：改，但排在 2026-08-29 之後動手。實際裁定：改，且不延後**（見頂部裁定註記）。

改的理由不是「案例應該走正式路徑」這種美學或教學考量 —— 而是**捷徑已經在產生錯誤行為，
而且其中一項應用層根本補不完**。下面兩項都是本次盤點實測出來的，不是推論。

原本建議延後的理由是文章：鐵人賽 Day 13 於 08-29 發佈，其「回到 Northwind」段落
以「案例沒有走進公司那一步」為論點，發佈當日之後線上版改不動。使用者選擇改內文而非延後改 code。

---

## 二、捷徑目前造成的兩個錯誤行為（實測）

### 錯誤 1：per-form 稽核規則在案例裡是死的

昨天才接上的 `st_audit_rule` 三筆規則（`Order` / `Customer` 開檢視記錄、`Category`
關異動記錄）**完全不會生效**，每張表單一律退回 `SystemSettings.xml` 的部署層預設。

鏈路（全部可在原始碼直接對照）：

```
FormBusinessObject.ResolveAuditRule()
  → IAuditRuleService.Get(companyId)
  → CompanyAuditRulesCache.CreateInstance
  → CacheDataSourceProvider.GetCompanyAuditRules(companyId)
  → GetCompanyInfo(companyId)
  → ICompanyRepository.GetById  ← 讀 st_company，不是 ICompanyInfoService
  → SELECT ... FROM st_company WHERE sys_id='NORTHWIND' AND enabled=1
```

**關鍵在倒數第二步**：[`CacheDataSourceProvider.GetCompanyAuditRules`](../../src/Bee.Business/Providers/CacheDataSourceProvider.cs)
解析公司走的是 `ICompanyRepository`（資料表），**不是**案例替換掉的 `ICompanyInfoService`。
而案例的 `st_company` 是空的 —— 實測 `apps/Bee.Northwind/Bee.Northwind.Server/northwind.db`：

| 資料表 | 列數 |
|--------|------|
| `st_company` | **0** |
| `st_user_company` | **0** |
| `st_user` | 1 |
| `st_audit_rule` | 3 |

`GetById` 回 `null` → `GetCompanyAuditRules` 回 `null` → `?.Find(ProgId)` 短路 → 規則從未被讀到。

**同一個 `GetCompanyInfo` 也擋住 `GetCompanyRolePermissions` 與 `GetDepartmentTree`** ——
只是那兩個在案例裡本來就沒有資料，看不出來。

> ⚠️ 連帶影響：雙語 README 昨天新增的那一段（「`Order` 與 `Customer` 把檢視記錄打開…
> `Category` 把異動記錄關掉」）描述的是**不會發生的行為**。無論本計畫改不改，
> 那段都得處理 —— 這是改的最強理由，因為它已經讓對外文件說了不實的話。
> 同時 [plan-per-form-audit-rule.md](plan-per-form-audit-rule.md) 標著「✅ 已完成」，
> 而它在唯一一個實際部署上跑不起來。

### 錯誤 2：session 重建會掉公司，而應用層補不了

[`NorthwindSystemBusinessObject.Login`](../../apps/Bee.Northwind/Bee.Northwind.Server/NorthwindSystemBusinessObject.cs)
只寫**快取**：

```csharp
session.CompanyId = company.CompanyId;
session.CustomizeId = company.CustomizeId;
SessionInfoService.Set(session);      // ← 只有這一行
```

框架的 `EnterCompany` 在同一位置寫**兩處**：

```csharp
SessionRepository.UpdateSession(CreateSeed(sessionInfo));   // ← 種子（st_session）
SessionInfoService.Set(sessionInfo);                        // ← 快取
```

且 `EnterCompany` 的註解寫明了為什麼：*「Seed before cache: the company is the one snapshotted
value that cannot be derived, so a rebuild that missed it would silently drop the user back to
"no company".」*

案例正是那個 rebuild 掉公司的情形。快取被逐出或伺服器重啟之後，
[`CacheDataSourceProvider.GetSessionInfo`](../../src/Bee.Business/Providers/CacheDataSourceProvider.cs)
從 `st_session.session_user_xml` 讀回的種子 `CompanyId` 是 `null`，
`if (StringUtilities.IsNotEmpty(seed.CompanyId))` 不成立 → 跳過 `Bind` →
回來一個沒有公司的 session → 下一次任何 `CategoryId="company"` 表單在
[`RepositoryDatabaseRouter`](../../src/Bee.Repository/RepositoryDatabaseRouter.cs)
擲 `CompanyNotEnteredException`。徵狀：**demo 重啟後，前端還握著 token，
但每一張表單都開不起來，直到重新登入。**

**而這一項應用層修不掉**：`SystemBusinessObject` 的 `CreateSeed` 是 `private static`、
`SessionRepository` 是 `private`，子類別拿不到任何一個。

> 這一點推翻了那個類別自己的註解 ——「stamping `SessionInfo.CompanyId` directly is the
> minimal equivalent」並不成立，它漏了種子，而框架沒有留給應用補上的接縫。
> 捷徑不是「夠用的簡化」，是**做不完的簡化**。

### 附帶（不算錯誤，但同類）

`ClientInfo.Company` 永遠是 `null`（從未呼叫 `ApplyEnterCompanyResult`），
所以前端的公司層小數位覆寫與本幣退回框架預設。案例沒有設定這些值，**目前無感**。

---

## 三、若要改：最小改動清單

### ⛔ 先解一個會擋住登入的地雷

**案例的資料庫沒有 `st_role` / `st_role_grant` / `st_user_role`。**
`NorthwindBackend` 只 materialize `TableSchema/common/` 與 `TableSchema/log/` 兩個 prefix
（`NorthwindSchemaSeeder.FrameworkTableSchemaPrefixes`），框架隨附的
`TableSchema/company/st_role*.xml` 從未被複製過。實測 `northwind.db` 裡一張都沒有。

而 [`RolePermissionRepository`](../../src/Bee.Repository/System/RolePermissionRepository.cs)
的 `GetRoleGrants` / `GetUserRoles` **沒有 schema 探測保護**（`AuditRuleRepository.GetRules`
有，會回 `[]`），直接 `SELECT ... FROM st_role_grant`。

**所以：只 seed `st_company` 而不建那三張表，會把「靜默失效」升級成「登入後進不了公司」。**
兩件事必須一起做。

### 階段 1：後端（`apps/Bee.Northwind/Bee.Northwind.Server`）

| # | 動作 | 檔案 |
|---|------|------|
| 1 | 三張角色表的 `TableSchema` 落到案例 Define（比照 `st_department` / `st_employee` 的既有做法，company 類本來就由案例自帶） | `Define/TableSchema/company/st_role{,_grant}.TableSchema.xml`、`st_user_role.TableSchema.xml` |
| 2 | `DbCategorySettings.xml` 的 `company` 類註冊上述三張 | `Define/DbCategorySettings.xml` |
| 3 | seeder 加一筆 `st_company`（common 類，與 `SeedDemoUser` 同一個 `DbAccess`） | `NorthwindSchemaSeeder.cs` |
| 4 | seeder 加一筆 `st_user_company`（`user_rowid` / `company_rowid` 都要先查回 `sys_rowid`） | 同上 |
| 5 | **刪除** `NorthwindCompanyInfoService.cs`，並移除 `NorthwindBackend` 裡的 `AddSingleton<ICompanyInfoService, …>` | `NorthwindCompanyInfoService.cs`、`NorthwindBackend.cs` |
| 6 | **刪除** `NorthwindSystemBusinessObject.cs`，並移除 `ProgramSettings.xml` 的 `System` 綁定 | `NorthwindSystemBusinessObject.cs`、`Define/ProgramSettings.xml` |
| 7 | `NorthwindCredentials` 保留（seed 值來源仍需要），只是 `CompanyName` / `CompanyDatabaseId` / `CustomizeId` 的消費端從服務改成 seeder | `NorthwindCredentials.cs` |

`st_company` 該 seed 的值：

| 欄位 | 值 | 備註 |
|------|-----|------|
| `sys_id` | `NORTHWIND` | `NorthwindCredentials.CompanyId` |
| `sys_name` | `Northwind Traders` | |
| `company_database_id` | `company` | |
| `customize_id` | `northwind-demo` | **客製層靠這一欄，漏了整層靜默關閉** |
| `enabled` | `true` | `CompanyRepository` / `UserCompanyRepository` 都帶 `enabled` 過濾 |
| `number_formats_xml` / `cash_rounding_xml` / `allowed_currencies_xml` | `''` | ⚠️ `DbType="Text"`，依 `rules/database.md` 每個手寫 INSERT 必須顯式帶空字串（MySQL 的 TEXT 不能有 DEFAULT） |
| `default_currency` | `''` | |

### 階段 2：前端（一個檔、約四行）

[`LoginViewModel.LoginAsync`](../../apps/Bee.Northwind/Bee.Northwind.UI/ViewModels/LoginViewModel.cs)
在 `ClientInfo.ApplyLoginResult(response)` 之後補：

```csharp
var company = await connector.EnterCompanyAsync(NorthwindCredentials.CompanyId).ConfigureAwait(true);
ClientInfo.ApplyEnterCompanyResult(company);
```

- `SystemApiConnector.EnterCompanyAsync` 與 `ClientInfo.ApplyEnterCompanyResult` **client 端早就備妥**，不需框架改動。
- `Bee.Northwind.UI` 由 Desktop / Browser / Android / iOS **四個 head 共用**，改一處四邊都到。
- **不做公司選單 UI** —— 單一公司下那是多餘的，而且會讓 demo 的登入流程多一個要解釋的畫面。

### 階段 3：文件與鏡像

- 雙語 README：修正稽核規則那一段（現在描述的行為不會發生）；`NorthwindSystemBusinessObject`
  相關敘述整段移除。
- `bee-northwind-avalonia`（下游鏡像）：**在本 repo 三個階段都驗完之後才同步**，一次推完整組。
  現況兩邊的 `NorthwindSystemBusinessObject.cs` / `NorthwindCompanyInfoService.cs` /
  `NorthwindCredentials.cs` 完全相同，同步時是純刪除 + seeder 換版。

---

## 四、確認不會壞的部分（已逐項查證）

| 疑慮 | 查證結果 |
|------|---------|
| 角色為空會不會讓表單開不起來 | **不會。** 伺服端 [`FormBusinessObject.Permission`](../../src/Bee.Business/Form/FormBusinessObject.Permission.cs) 在 `PermissionModelId` 為空時整段 no-op；前端 [`ElementCapabilityResolver.Can`](../../src/Bee.UI.Core/Permissions/ElementCapabilityResolver.cs) 同樣 `modelId` 為空即回 `true`。案例八張表單一張都沒宣告 `PermissionModelId`（新增的 `AuditRule` 也刻意拿掉了）。 |
| `Capabilities` 從 `null` 變成空字典會不會讓 UI 全降級 | **不會**，理由同上（空 `modelId` 先短路）。 |
| 欄位軸（`SensitiveCategory`）會不會被降級 | **不會。** 案例 Define 底下 `SensitiveCategory` 出現 **0 次**。 |
| `CustomizeId` 會不會斷 | **不會**，但**必須 seed `customize_id`**。改動後由 `SessionCompanyBinder` 從 `CompanyInfo` 抄，語意與現在完全相同，只是來源從硬編碼換成資料表。 |
| `CompanyInfo` 其他欄位（數值格式、幣別）會不會變 | **不會。** `CompanyInfo` 的屬性預設與 `CompanyRepository` 從空欄位解出來的值一致（空集合 / 空字串）。 |
| 員工脈絡解析會不會出事 | **不會。** `st_employee` 存在（5 列），但沒有任何一列的 `user_rowid` 指向 `demo`，所以 `EmployeeContextResolver` 回 `EmployeeContext(userRowId, Empty, Empty)`。比現在還好一點 —— `UserRowId` 從 `Guid.Empty` 變成真值。 |

> 順帶：把 `demo` 使用者連到 `E001` 就能讓 record-scope 變成可示範的，
> **但那是另一件事**，不納入本計畫。

---

### 實作時多發現的一件事（第三節清單未預見）

**`ClientInfo.ApplyLoginResult` 會丟棄快取的 connector**，因為那一個是用登入前的空 token 建的
（`AccessToken` 的 setter 內 `_systemConnector = null`）。所以 `EnterCompanyAsync`
**必須重新讀 `ClientInfo.SystemApiConnector`**，不能沿用 `LoginAsync` 那一行的區域變數 ——
沿用會得到 `AccessToken is required or invalid`，讀起來像 session 出問題而不是變數過期。

第一版就是這樣寫的，由第五節的探針抓出來。已在
[`LoginViewModel`](../../apps/Bee.Northwind/Bee.Northwind.UI/ViewModels/LoginViewModel.cs)
留下 WARNING 註解。

---

## 五、驗證方式

**全部已執行，結果如下（2026-08-28）。**

| # | 驗證 | 結果 |
|---|------|------|
| 1 | `dotnet build Bee.Library.slnx -c Release` | ✅ 0 警告 0 錯誤 |
| 2 | `dotnet build apps/Bee.Northwind/Bee.Northwind.slnx -c Release`（四個 head） | ✅ 0 錯誤。⚠️ iOS head 需 `DEVELOPER_DIR=/Applications/Xcode-26.5.0.app/Contents/Developer`（`rules/apple-mobile-trim.md` 的既有雷，與本次改動無關）；56 個警告全是 NuGet / SDK 組件的既有 IL2104 |
| 3 | `./test.sh` 全套 | ✅ 全綠（1 筆既有 RSA 環境 skip） |
| 4 | `./check-public-docs.sh` | ✅ 僅既有的兩處性質說明與已知誤報 |
| 5 | 刪掉 `northwind.db` 重跑 seeder | ✅ `st_company` 1 列（`customize_id=northwind-demo`、`enabled=1`）、`st_user_company` 1 列（join 得回 `demo` ↔ `NORTHWIND`）、三張角色表建出 |
| 6 | 登入 + `EnterCompany` | ✅ 回 `NORTHWIND` / `customize=northwind-demo`；`ClientInfo.Company` 由 `null` 變成有值；`capabilities` 為空字典且不影響任何表單 |
| 7 | 客製層仍生效 | ✅ `GetCustomizeFormLayoutAsync("Order")` 回 **5** 個欄位（套裝版 8 個） |
| 8 | **稽核規則開始生效** | ✅ 讀兩次 `Order` → `st_log_access` 出現 2 列（`AccessEnabled=false` 但 `Order` 的 `access_mode=On`）；讀 `Category` → **無**列（`Inherit` → 沿用部署預設關閉）。改動前這張表恆為 0 |
| 9 | **session 重建不再掉公司** | ✅ `st_session` 種子內含 `<CompanyId>NORTHWIND</CompanyId>`；重啟 server 後同一個 token 仍能開公司分類表單（`before restart : OK (5 rows)` / `after restart : OK (5 rows)`） |

第 6～9 項以一支丟棄式 console client（`Bee.UI.Core` 的真實用戶端堆疊）跑完，驗完即刪。

> ⚠️ **CI 建議帶 `[all-db]`** —— 動到 `DbCategorySettings` 與三張新表的建置。尚未執行，
> 依慣例由使用者於 push 前裁定。

---

## 六、對三篇文章的處置建議

| 篇 | 發佈日 | 現況 | 建議 |
|----|--------|------|------|
| **Day 13** | 2026-08-29 | 已定稿、已過發佈前審稿。「回到 Northwind」整段（約 1,054 字）建立在「案例沒有走進公司那一步」上 | ⚠️ **使用者裁定改內文**（推翻本表原建議的「照常發佈」），由**原 session** 於發佈前修訂。本 session 不動 `docs/blogs/`。核心教學（兩階段生命週期、界線是相依關係算出來的）不受影響，要改的是案例現況那一段 —— 而它現在有更強的素材：案例不但走了進公司那一步，捷徑留下的兩個無聲錯誤也成了「為什麼那一步不能省」的實證 |
| **Day 29** | 2026-09-14 | 已校稿完成、未發佈。對帳表把 `NorthwindSystemBusinessObject` 列在「應用程式碼」欄，且是應用外殼七個 `.cs` 之一 | **要改。** 該檔會被刪除，七個變六個。⚠️ 注意 `day-notes.md` 記著 Day 29「一個檔案數都沒寫、改為逐支列出在做什麼」，所以是**改清單不是改數字** |
| **Day 30** | 2026-09-15 | 已定稿、未發佈 | **不必改。** 全篇對 `EnterCompany` / 案例登入零引用（已 grep 確認） |
| **Day 19** | 2026-09-04 | 未發佈 | **不必改，而且會變更準確。** §133「客製化代碼是進公司那一段填進 session 的六個值之一」與 §176「案例的公司資料填了客製化代碼 `northwind-demo`」在改動後才字面成立 —— 現在案例根本沒有「公司資料」這一列 |
| **Day 25** | 2026-09-10 | 🚧 校稿中 | ⚠️ **無論本計畫改不改都要修。** §79 寫「表單這一側要標得出敏感，需要一份逐表單的稽核規則，**而那份規則還不存在**」—— 該機制已於 2026-08-26～28 三個 commit 落地，這句已成錯誤斷言 |

> **Day 19 的「五個第二個」收束線不受影響。** 那條線講的是「第二家公司 / 第二個行程 /
> 第二個同時存檔的人…只在第二個之後才存在」。本計畫**維持單一公司**，沒有製造第二家，
> 那句話仍然成立。`northwind-case-assessment.md` 第 5 項判「不補」的對象是**加第二家公司**，
> 與本計畫是不同的題目 —— 別把那個結論套過來。

---

## 七、不改的代價（若裁定不改）

必須至少處理下面兩件，否則是把已知錯誤留著：

1. 雙語 README 昨天新增的稽核規則段落要**改寫成「規則表可維護、但本 demo 不讀取」**，
   或整段移除 —— 現在的寫法對外描述了不存在的行為。
2. `NorthwindSystemBusinessObject` 的類別註解要修正 —— 「minimal equivalent」不成立，
   應寫明它漏了 session 種子、重啟後會掉公司，且應用層補不了。

換句話說，**「不改」不是零成本**，它要花的力氣是把捷徑的界線重新誠實地寫一遍。

---

## 相關

- [plan-per-form-audit-rule.md](plan-per-form-audit-rule.md) —— 稽核規則本體（標記已完成，
  但如第二節所述，它在唯一一個實際部署上沒有生效）
- [adr-041](../adr/adr-041-per-form-audit-rule.md) —— per-form 稽核規則的決策紀錄
