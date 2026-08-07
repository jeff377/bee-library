# 權限與授權指南

[English](permission-authorization.md) · [← 文件索引](README.zh-TW.md)

Bee.NET 的權限分為**三個維度**，套用於**兩個把關點**——**後端**是權威安全邊界；**前端**把 UI 元素降級為 UX 輔助（永不作為安全邊界）：

| 維度 | 問題 | 後端 | 前端 |
|------|------|------|------|
| **動作權限（Action）** | 能不能對該 model 做這個*動作*？ | ✅ 權威 gate — `Can(model, action)` | ✅ 工具列命令／按鈕狀態 |
| **列權限（Record / row）** | 能對*哪些列*做？ | ✅ 權威 scope 過濾 + 寫入 re-query | — |
| **欄權限（Field / column）** | 能不能*檢視*／*編輯*這個敏感欄？ | — *（後端未遮罩）* | ✅ 隱藏（無 `Read`）／唯讀（無 `Update`） |

**動作維度在*兩個*把關點都套用**：後端在方法層 enforce（真正的邊界），前端則把它反映成命令／按鈕狀態，讓使用者不會被提供他做不到的動作。**列權限僅在後端**。**欄權限僅在前端**——是 UX 輔助、非資料邊界（見[第 10 節](#10-在-host-app-啟用-capabilityopt-in)警語）。

兩個後端維度皆於請求時完全走記憶體快照（DB 只在載入快取、登入、`EnterCompany`、改配置時碰）。授權與 `ApiAccessControlAttribute`（管加密等級與是否需登入）**正交**。設計理由見 [ADR-019](adr/adr-019-permission-authorization-model.md)。

三個維度全部**以公司為範圍**。屬於整個部署、不屬於任何公司的資產 —— API 金鑰，以及部署端日後新增的其他項目 —— 由另一條平行判定管轄，見**第三部分**。

---

# 第一部分 — 後端 enforcement（動作 + 列）

動作與列兩個維度是權威把關。兩者皆完全走記憶體快照，且與表單解耦。

## 1. 定義權限模型

**權限模型**是業務實體（如 `PurchaseOrder`），刻意與表單 `progId` 區別。所有 model 集中於單一 registry（`PermissionModels`，`DefineType.PermissionModels`）。每個 model 逐 action 宣告預設的 record-scope 策略：

```xml
<PermissionModels>
  <PermissionModel ModelId="PurchaseOrder" DisplayName="採購單">
    <Rules>
      <PermissionRule Action="Read"   Scope="DeptAndSub" />
      <PermissionRule Action="Update" Scope="Own" />
      <PermissionRule Action="Delete" Scope="Own" />
      <PermissionRule Action="Create" Scope="All" />
      <!-- Print / Export 省略 Scope → 繼承 model 的 Read scope -->
    </Rules>
  </PermissionModel>
</PermissionModels>
```

- `ModelId` 為業務實體 PascalCase。**一個 model 可被多個表單消費**（`PO001` 建立、`PO002` 查詢、`PO009` 報表都引用 `PurchaseOrder`）——授一次三功能一起生效。
- model 不綁資料表也不綁欄。scope 策略保持純語意，具體欄位由 FormSchema 提供（見下節）。

## 2. 表單綁定 model

`FormSchema` 宣告它消費哪個 model，並標記**主表**哪個欄是擁有者 / 部門：

```xml
<FormSchema ProgId="PO001" PermissionModelId="PurchaseOrder" ...>
  <Tables>
    <FormTable TableName="PO001" ...>
      <Fields>
        <FormField FieldName="buyer_rowid" Caption="採購員" ScopeRole="Owner" />
        <FormField FieldName="dept_rowid"  Caption="部門"   ScopeRole="Dept" />
        <!-- ... 其他欄位 ... -->
      </Fields>
    </FormTable>
  </Tables>
</FormSchema>
```

規則：

- `ScopeRole` **僅限主表**。明細表標 `ScopeRole` 會被 `PermissionBindingValidator` 檢出（見[定義驗證](#定義驗證由宿主呼叫)）——record scope 由主檔決定、明細隨之。
- **可標記多個 `Owner` / `Dept` 欄**——每個標記欄各貢獻一個 OR 分支。例如調職單同時標**調出部門**（`from_dept`）與**調入部門**（`to_dept`），讓*兩邊*部門主管都看得到這筆。
- 空的 `PermissionModelId` → 表單**不套權限**，後端兩層皆跳過（漸進導入／向後相容）。

## 3. 授權角色

角色、授權、指派存於各**company 資料庫**（`st_` 框架表，公司內配置）：

| 表 | 欄位 | 意義 |
|----|------|------|
| `st_role` | `sys_id`, `sys_name` | 角色（指派的打包單位） |
| `st_role_grant` | `role_id`, `model_id`, `action`, `scope` | 角色對某 model 某 action 的授權 + 其 record scope |
| `st_user_role` | `user_id`, `role_id` | user ↔ role 指派（`user_id` 即 `st_user.sys_id`） |

`st_role_grant` 是 **per-action**：有列即為層一授權；其 `scope`（`ScopeStrategy`）驅動層二。這正是「某角色看得到整個部門、卻只能改自己的」之所以可能。

```sql
-- 「採購員」可看部門及子部門的採購單，但只能改／刪自己的：
INSERT INTO st_role_grant (role_id, model_id, action, scope) VALUES
  ('Buyer', 'PurchaseOrder', 2 /*Read*/,   4 /*DeptAndSub*/),
  ('Buyer', 'PurchaseOrder', 4 /*Update*/, 2 /*Own*/),
  ('Buyer', 'PurchaseOrder', 8 /*Delete*/, 2 /*Own*/);
-- scope = ScopeStrategy：Inherit=0, All=1, Own=2, Dept=3, DeptAndSub=4
-- action = PermissionAction（flags）：Create=1, Read=2, Update=4, Delete=8, Print=16, Export=32
```

`scope = Inherit (0)` → 取 model 該 action 的預設（第 1 節）。

## 4. 連結使用者與員工（部門 scope 需要）

部門／擁有者 scope 需解析**當前使用者 → 所屬部門**。使用者（`st_user`，common DB）透過 `st_employee.user_rowid` 連到員工（`st_employee`，company DB）：

```
st_user.sys_rowid  ──(st_employee.user_rowid)──▶  st_employee  ──(dept_rowid)──▶  st_department
```

`EnterCompany` 時框架一次性解析 `user → employee → 部門`，把 `UserRowId`、`EmployeeRowId`、`DeptRowId` 快照進 session，之後 scope 過濾零 DB。未綁員工的使用者其 employee/部門為空——`Own` 仍會比對 `UserRowId`，`Dept`/`DeptAndSub` 則不匹配任何列（fail-closed）。

## 5. enforcement 行為

### 層一 — 動作 gate

`FormBusinessObject` 執行前先判 `(model, action)`：

- `GetList` / `GetData` → `Read`
- `Save` → 逐列依 `RowState`：`Added`→`Create`、`Modified`→`Update`、`Deleted`→`Delete`
- `Delete` → `Delete`

多角色 **OR 聯集**（能力累加）。未過 → 拋 `ForbiddenException`。

### 層二 — record scope

**讀取**（`GetList`、`GetData`）把 scope filter `AND` 進查詢。越範圍列被過濾掉；越範圍的單列查詢回 `null`（與「查無」不可區分，呼叫端無法探測看不到的列）。

**寫入**（`Update`、`Delete`）由對 DB 的**權威 re-query** 把關——`WHERE sys_rowid = id AND <scope>`——而**非**評估送來的 payload。偽造的 DataSet 無法 relabel 繞過。

- `Save` 只在主表列為「既存記錄存檔」（非 `Added`）時 re-check。只改表身、主表 `Unchanged` 仍算 `Update`。
- `Delete(rowId)` 越範圍 → 回 0、不 cascade。
- **`Create` 不套 scope**——新列無既存範圍可違反，由動作授權管。
- scope **僅主表**：主檔過了，整筆（含明細）一併放行。

### scope 策略

| 策略 | 讀取 filter（與寫入 re-query 同源） |
|------|----------------------------------|
| `All` | 不限制 |
| `Own` | `owner 欄 IN {UserRowId, EmployeeRowId}` |
| `Dept` | `dept 欄 = DeptRowId` **OR** Own |
| `DeptAndSub` | `dept 欄 IN（部門 + 所有後代）` **OR** Own |
| `Inherit` | model 該 action 的預設（再退 Read scope，再退 `All`） |

- `Dept` / `DeptAndSub` **隱含 `Own`**——使用者永遠看得到自己擁有的列。
- **多個 scope 欄**：主表標多個 `Owner` / `Dept` 欄時，同策略對所有標記欄取 **OR 聯集**——如調職單 `from_dept`、`to_dept` 各 `IN` 子樹,任一部門主管都看得到該筆。
- **多角色合併**：任一角色對該 action 授 `All` → 不過濾；否則各策略 **OR 聯集**。
- `Own` 的 owner 欄可能存 user rowid 或 employee rowid（如*登打者* vs *請假人員*）；`IN {UserRowId, EmployeeRowId}` 同時涵蓋兩者，且使用者未必對應員工。

---

# 第二部分 — 前端 capability（動作命令 + 欄權限）

前端以 per-model 的 **capability 快照**降級 UI 元素，讓使用者不會被提供無權的命令、或看到無權的敏感資料。它呈現三維度中的**兩個**：**動作維度**化為工具列命令／按鈕狀態、**欄權限維度**化為敏感欄的隱藏／鎖定。這是**純 UX**——後端（第一部分）仍是權威邊界。

## 6. 標記敏感欄位

欄權限維度是 **opt-in**：只標需要控管的欄。絕大多數欄位不標記，照 layout 正常呈現。

```xml
<FormField FieldName="unit_cost" Caption="單價" SensitiveCategory="Cost" />
```

`SensitiveCategory`（預設 `None` = 不控管）是**具名、有限的分類**——`Amount`、`Cost`、`PersonalData`——平行於 `ScopeRole`。設計者挑分類而非自創 id，因此分類集合封閉、可被檢查。它適用於**任何欄位**，主表或明細 Grid 欄皆可。

## 7. Well-known 分類 model

每個非 `None` 分類**依慣例**對應一個 permission model，其 id 等於分類名（`Cost` → `"Cost"` model）。這些就是同一份 `PermissionModels` registry 裡的一般 model——像其他 model 一樣宣告與授權。若標了分類卻無對應 model，`PermissionBindingValidator` 會回報錯誤（見[定義驗證](#定義驗證由宿主呼叫)）。

```sql
-- Viewer 看得到成本但不能改（只授 Read）；Editor 可改（授 Read + Update）。
-- 編輯也需要 Read——只授 Update 而無 Read 會讓該欄被隱藏。
INSERT INTO st_role_grant (role_id, model_id, action, scope) VALUES
  ('CostViewer', 'Cost', 2 /*Read*/,   1 /*All*/),
  ('CostEditor', 'Cost', 2 /*Read*/,   1 /*All*/),
  ('CostEditor', 'Cost', 4 /*Update*/, 1 /*All*/);
```

分類 gate 是**全公司一致、且與表單自身 model 正交**：看不看得到 `Cost` 欄取決於 `Cost.Read`，*獨立於* `PurchaseOrder.Read`。使用者可能有權讀採購單、卻仍被隱藏成本欄。這貼合 ERP 實務——成本／金額／個資的可見性是**資料分類**問題，應跨所有表單一致、授權一次。

## 8. capability 快照怎麼到前端

`EnterCompany` 時，後端對 session 的角色算出 per-model action mask（`CompanyRolePermissions.GetAllowedByModel`），附在 `EnterCompanyResponse.Capabilities`——一個 `Dictionary<modelId, PermissionAction>`——搭 `EnterCompany` 這班既有往返，**零額外請求**。只有使用者持有 grant 的 model 會出現在 map 中。

## 9. 前端如何降級

`ClientInfo.Capabilities` 快取此快照（nullable），`Bee.UI.Core.Permissions.ElementCapabilityResolver`（純函式、UI-agnostic）讀它：

- **`null` → capability 未啟用 → 什麼都不降級。** 從未進公司、或不用權限的 app，呈現與過去完全相同。
- **非 null → 已啟用。** map 中缺某 model 即代表對它*無權*。

兩種元素消費 resolver：

- **命令按鈕**（工具列）。每顆按鈕建立時自帶所需 `PermissionAction`（`New`→`Create`、`Save`→`Create|Update`、`Delete`→`Delete`、`View`→`Read`）；resolver 的 `Can(...)` 以表單 `PermissionModelId` 判定，採 **any-of** 語意（`Save` 只要使用者有 `Create` 或 `Update` 其一即顯示）。無權按鈕被隱藏。這是**動作維度在前端的 UX 投影**。
- **敏感欄位**。`ResolveField(...)` 讀欄位的 `SensitiveCategory`、查分類 model，依**兩個各自獨立的子 gate** 降級——`Read` 管*可見性*、`Update` 管*可編輯性*——所以一個欄位可以「看得到但不能改」。主表欄與明細 Grid 欄一體適用。

  | `<分類>.Read` | `<分類>.Update` | 結果 |
  |:---:|:---:|---|
  | ✗ | （皆可） | **隱藏**——不渲染該欄 |
  | ✓ | ✗ | **顯示、唯讀**——*例如看得到成本、但不能改* |
  | ✓ | ✓ | **顯示、可編輯** |

  隱藏優先於唯讀——連看都不能看的欄，就不必談能不能改。

> **明細 Grid 的動作（新增／編輯／刪除列）不套 capability。** 明細與主檔同屬一個 aggregate，其列能否編輯跟隨表單編輯模式——而進入編輯模式所需的權限，早已被工具列命令擋掉。只有 Grid 內的敏感*欄位*會降級。

降級不會動到快取定義：前端把它套在**每視圖新生成**的 layout 上，只**收窄**可見性／可編輯性。

## 10. 在 host app 啟用 capability（opt-in）

capability **未接線前一律 inert**，既有 app 不受影響。要啟用：

1. 在 `PermissionModels` 宣告 well-known 分類 model（`Amount` / `Cost` / `PersonalData`）並授權（第 7 節）。
2. `SystemApiConnector.EnterCompanyAsync` 之後，把回應交給 client 快取：
   ```csharp
   var response = await ClientInfo.SystemApiConnector.EnterCompanyAsync(companyId);
   ClientInfo.ApplyEnterCompanyResult(response);   // 快取 capability 快照
   ClientInfo.ResetDefineCache();                  // （既有）清掉舊租戶的定義快取
   ```
3. `LeaveCompany` 時清除：`ClientInfo.ClearCompanyContext();`。

> **警語 — 欄權限是 UX，不是資料邊界。** `GetList` / `GetData` 仍會回傳敏感欄的值；前端只是把它隱藏／鎖定。繞過標準 UI 的 client 仍可能從 API 取得原始值。請把欄權限視為*呈現層*。任何**絕不能離開伺服器**的資料，應放在**動作**或**列**邊界（第一部分）或自成一個 permission model——而非僅靠 `SensitiveCategory`。伺服器端欄位遮罩屬另案（見非目標）。

---

# 第三部分 — 部署層管理（公司模型之外）

以上全部以公司為範圍：角色、授權、部門樹都放在**各公司自己的資料庫**，session 未進入公司時 `IAuthorizationService.Can` 一律回 `false`。

但有一類資產屬於**整個部署**、不屬於任何公司 —— API 金鑰識別的是呼叫的*應用程式*，不是租戶。用公司層權限去守它，等於讓一家租戶的管理員替所有租戶做決定。因此另立一條平行的判定。

| | 公司層授權 | 部署層授權 |
|---|---|---|
| 介面 | `IAuthorizationService.Can(token, modelId, action)` | `IDeploymentAuthorizationService.Can(token, action)` |
| 問題 | 這位使用者能不能**在公司 C 內**做 X？ | 這位使用者能不能對**整個部署**做 X？ |
| 身分來源 | 各公司資料庫的 `st_role` / `st_role_grant` / `st_user_role` | common 資料庫的 `st_user.deployment_admin` |
| 需要公司脈絡 | 需要 —— 沒進公司就無權 | 不需要 —— 依定義本來就沒有 |

> **兩者互不授予。** 部署層管理員**不會因此取得任何公司的資料權限**：讀寫某公司的資料仍走第一部分的動作與列邊界，那兩道閘查的是該公司的角色，對這個旗標一無所知。反過來，公司管理員在部署層也什麼都不是。兩條判定不互相 fallback。

## 11. 部署層授權涵蓋哪些作業

`DeploymentAction` 刻意維持極小，目前只有一個成員：

| 動作 | 受管的作業 |
|------|-----------|
| `ManageApiKey` | `SystemBO.CreateApiKey` —— 發放 API 金鑰 |

`CreateApiKey` **只對遠端呼叫**做這道判定。行程內呼叫免管理員 —— 這正是讓全新部署在還沒有任何管理員時，仍能在主機上鑄出第一把金鑰的原因。

判定**每次都查資料庫**，刻意與公司層不對稱（後者從快取回答、完全不碰 DB）。理由：部署層作業低頻，而**撤銷管理員必須即時生效**，任何形式的快取都會帶來延遲。

## 12. 指派管理員

`SystemBO.SetDeploymentAdmin(userId, isDeploymentAdmin)` 是 `st_user.deployment_admin` 的**唯一**寫入路徑，且為 `LocalOnly`：第一位管理員在主機上指派，之後就能由部署端自建的管理介面接手。

「唯一寫入路徑」是 runtime 強制的，不是靠約定：`ProtectedFields` 列有該欄，FormSchema 驅動的寫入路徑（`DataFormRepository.Save`）會把它從所有 INSERT / UPDATE 剔除，**即使表單宣告了它**。少了這道，部署端只要自建一張 `st_user` 維護表單，就等於給了一般使用者自我提權的路。讀取不受影響 —— 表單可以顯示該欄，只是存不進去。

框架不出貨任何 `st_user` 資料列，因此「全新部署要不要預先標一位管理員」由部署端的 seeder 決定；無論如何欄位預設都是「非管理員」。

## 13. 稽核留痕

部署層作業記在變更軸（`st_log_change`），`prog_id` 為 `System`，`source` 標明作業（`System.SetDeploymentAdmin`、`System.CreateApiKey`），操作者一如其他稽核列去正規化寫進 `user_id` / `user_name`。`changes_xml` 帶前後值，因此追蹤得出是**授予**還是**撤銷** —— 兩者同為 `Update`，沒有前後值就分不出來。

兩個值得知道的性質：

- **一律標為敏感**（`is_sensitive`），把日誌篩到只剩重點時仍看得到它們。
- **不受 `AuditLogOptions.ChangeEnabled` 影響。** 那個開關是給「業務資料歷程量太大」用的；指派管理員既不日常、量也不大。關掉它這些留痕照樣寫入 —— 只有整個關掉 `AuditLogOptions.Enabled` 才會停。

API 金鑰的稽核列記錄金鑰的 id、名稱、類型、聯絡人與到期時間，**絕不**記錄祕密段或其雜湊：日誌資料庫是獨立的儲存體，讀者群通常更廣。

## 14. 既有部署的升級

1. **欄位會自動加上。** `st_user.deployment_admin` 由框架的 schema 升級機制補上（`ALTER … ADD`），既有列取預設值、*不是*管理員。不需手動 DDL。
2. **除非你覆寫過該表的定義。** 框架 runtime 只從你的 `DefinePath` 讀取表定義。若部署端自帶 `st_user.TableSchema.xml`，必須自行補上該欄 —— 框架內嵌的預設檔不會被參考。（見[框架保留名稱 §3](framework-reserved-names.zh-TW.md#3-消費者命名守則)。）
3. **在主機上指派第一位管理員**，以行程內呼叫 `SetDeploymentAdmin`。在那之前沒有任何遠端呼叫者鑄得出 API 金鑰，而本機呼叫的行為與升級前完全相同。

---

## 定義驗證（由宿主呼叫）

`PermissionBindingValidator.Validate(schemas, models)` 驗證表單與權限 registry 之間的綁定：
每個 `FormSchema.PermissionModelId` 都指向存在的 model、`ScopeRole` 只標在主表、每個非 `None`
的 `SensitiveCategory` 都有對應的 well-known model。每個違規回傳一則訊息，定義正確時回傳空清單。

**框架不會替你呼叫它。** 沒有自動的載入期掃描——無效的綁定不會讓應用啟動失敗，而是延後成
「權限判定靜默不作用」（空的 `PermissionModelId` 代表*不套權限*，所以 model id 打錯會退化成
「無 enforcement」而非錯誤）。請自行在「失敗成本低」的位置呼叫：宿主啟動時、部署冒煙測試、
或在 CI 對 `DefinePath` 下的定義跑一次。

```csharp
var errors = PermissionBindingValidator.Validate(allFormSchemas, permissionModels);
if (errors.Count > 0)
    throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
```

## 快取與失效

- 角色／授權／user-role 載入 per-company `CompanyRolePermissions` 快取；部門樹載入 per-company `DepartmentTree` 快取。兩者皆 DB 來源，由 common cache-notify poller 失效。
- `SessionInfo` 持有請求時快照（`Roles`、`UserRowId`、`EmployeeRowId`、`DeptRowId`），`EnterCompany` 填入、`LeaveCompany` / `Logout` 清除。
- 前端 capability 快照（`ClientInfo.Capabilities`）同為某時間點：`EnterCompany` 填入、`LeaveCompany` / 換 token 時清除。授權變更後需重新 `EnterCompany` 才會刷新。
- 快照為某時間點：改配置對「走快取的判定」即時反映（`Can` 現查快取）；已進公司 session 的 role/employee/部門快照於下次 `EnterCompany` 更新。

## 傳輸與憑證強化（正式環境）

- **強制 HTTPS。** 登入請求以 `PayloadFormat.Encoded`（序列化 + 壓縮 + Base64，**非加密**）承載密碼；RSA 握手只保護 server 回傳的 session key。因此傳輸機密性完全仰賴 TLS。正式環境所有端點務必以 HTTPS 提供（並啟用 HSTS），切勿以純 HTTP 暴露 JSON-RPC 端點。
- **覆寫 API 金鑰驗證器。** 預設 `ApiAuthorizationValidator` 只檢查 `X-Api-Key` 標頭非空、不驗其值 —— 真正的認證走 Bearer access token。若將 API 金鑰當作存取閘門，請覆寫 `ApiServiceOptions.AuthorizationValidator`，以常數時間比對對照設定的金鑰集。預設驗證器仍在使用時，`UseBeeFramework` 會記錄啟動警告。

## 非目標

- **宣告式自訂命令模型** — 標準工具列命令目前於程式碼標記（第 9 節）；把 Print / Export / Approve 做成*資料驅動*的 `FormLayout` element 尚未模型化。未來加入時，自訂命令會自帶 opt-in 的 `PermissionAction`。
- **後端欄位遮罩** — 欄權限維度是前端 UX。伺服器端對敏感欄的遮罩（讓其值永不離開伺服器）尚未實作；今日對硬性資料機密請改用動作／列邊界。
