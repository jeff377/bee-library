# 計畫：部署層管理員（不綁公司的營運權限）

**狀態：✅ 已完成（階段 1–3 全數落地）· 2026-08-03**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 身分來源與判定接縫：`IDeploymentAuthorizationService` ＋ 首位管理員的產生路徑 | ✅ 已完成（2026-08-01） |
| 2 | 套用至 API Key：`CreateApiKey` 等改由部署層權限把關，遠端管理成立 | ✅ 已完成（2026-08-03） |
| 3 | 稽核、文件（雙語）與既有部署的升級指引 | ✅ 已完成（2026-08-03） |

> 直接解鎖 [plan-api-key-store.md](plan-api-key-store.md) 的階段 3——該階段的管理表單需要一條
> 「遠端可用、但不是誰登入都行」的授權路徑，而框架現有的權限模型給不出來。

## 背景：公司層權限守不住部署層資產

框架的授權判定寫死在公司範圍內
（[AuthorizationService.cs](../../src/Bee.ObjectCaching/Services/AuthorizationService.cs)）：

```csharp
var session = _sessionInfoService.Get(accessToken);
if (session == null || string.IsNullOrEmpty(session.CompanyId) || session.Roles.Count == 0)
{
    return false;                                   // 沒進公司 → 一律無權
}
var snapshot = _rolePermissionService.Get(session.CompanyId);
```

角色資料（`st_role` / `st_role_grant` / `st_user_role`）也存在**各公司的資料庫**裡。
整套模型的前提是「權限屬於某家公司」。

但有一類資產不屬於任何公司：

| 資產 | 為何不屬於公司 |
|------|--------------|
| API Key | 依 API Key plan 的 D9 刻意不綁公司——它識別的是**應用程式**，不是使用者也不是租戶 |
| 公司本身 | 建立 / 停用試用公司、指派資料庫，是租賃營運行為 |
| 資料庫連線設定 | 部署層設定 |
| 跨公司稽核查詢 | 依定義跨越公司邊界 |

用公司層權限去守這些，語意直接錯掉：**A 公司的管理員會取得鑄造整個部署通用金鑰的能力**。
API Key plan 階段 1 因此把 `CreateApiKey` 定為 `LocalOnly`，並留下「階段 3 需要一條權限把關的
路徑，屆時才處理」——本 plan 就是那條路徑。

租賃方向（見 [plan-row-level-tenancy.md](plan-row-level-tenancy.md)）會讓這個面持續長大：
建立試用公司、監看用量、處理畢業，全都是不綁公司的營運操作。與其為 API Key 開一個特例，
不如把「部署層管理員」這個概念一次立起來。

## 已確認的既有條件

| 位置 | 現況 | 對本 plan 的意義 |
|------|------|----------------|
| `IAuthorizationService.Can(accessToken, modelId, action)` | 無 `CompanyId` 即回 `false` | 不改它；另立一條平行判定，避免把公司層語意攪混 |
| `RepositoryDatabaseRouter.Resolve` | `DbScope.Common` / `Log` 不需要公司即可解析 | 部署層操作只碰 common / log 庫，**天然不需要進公司**，router 已支援 |
| `st_user` | 位於 common 庫，無任何管理員欄位 | 身分來源（D1） |
| `st_user` **沒有出貨的 FormSchema** | 框架只出貨 TableSchema | 自我提權的防線現況只是「框架碰巧沒開這扇門」，不是機制（見 D6） |
| [UserRepository.cs](../../src/Bee.Repository/System/UserRepository.cs) | 目前**純讀**（`GetRowIdBySysId` / `GetLocale` / `GetName`） | 旗標的讀寫是它的第一組寫入方法 |
| `CreateApiKey` | `LocalOnly` | 階段 2 改為部署層權限把關後，遠端管理才成立 |

## 設計定案

四項原「決策待議」於 2026-08-01 定案，另補三項實作時必然撞上、原 plan 未涵蓋的決策（D5–D7）。

### D1：管理員身分放在 `st_user` 的旗標欄（定案）

`st_user` 增設 `deployment_admin`（Boolean）。common 庫天然不綁公司、立刻可用，
粒度是全有全無。

落選方案與理由：

| 選項 | 不採用的理由 |
|------|------------|
| common 庫另建一組 RBAC（`st_deployment_role` 等三張表） | 為目前唯一的消費者（API Key）造一整套，成本明顯過頭 |
| 設定檔列舉管理員帳號 | 不進 DB、無管理介面；「營運行為不該是部署設定」正是 API Key plan 否決定義檔的理由 |
| 沿用 `PermissionModels` 加部署層 model | 同一套 model id 會有兩種 scope 語意，日後難解釋 |

**判定一律走新接縫** [IDeploymentAuthorizationService](../../src/Bee.Definition/Identity/IDeploymentAuthorizationService.cs)
（`Bee.Definition/Identity/`），呼叫端只問「這個 token 能不能做這件事」。
今天以旗標實作，日後若真需要細粒度，換掉實作即可、呼叫端不動——**先立接縫、後補粒度**。

### D2：介面帶動作參數，實作先全有全無（定案）

簽章為 `Can(Guid accessToken, DeploymentAction action)`，
[DeploymentAction](../../src/Bee.Definition/Identity/DeploymentAction.cs) 目前只有 `ManageApiKey`。
實作階段一律以旗標回答，不看 action。簽章多一個參數的成本，遠低於日後回頭改所有呼叫端。

### D3：首位管理員由本機指派產生（定案）

- **框架提供的路徑只有一條**：`SystemBO.SetDeploymentAdmin` 為 `LocalOnly`，第一位在主機上指派，
  之後遠端接手。
- **seed 由部署端決定**：框架的 `Defaults/` 只出貨 TableSchema，不出貨 `st_user` 資料列，
  因此「新部署預先標一位管理員」是各部署 seeder 的事，框架只保證欄位預設為非管理員。
- **設定檔 bootstrap 帳號（原 D3-C）否決**——它等於一個永久後門，與 D1 否決設定檔路徑同一理由。
  移入「不在範圍」。

### D4：本 plan 只解 API Key（定案）

公司管理的形狀要等列級租戶隔離落地才看得清楚，現在一起做會兩邊互相等待。
接縫立起來後，公司管理只是多一個 `DeploymentAction`。

### D5：判定路徑每次查 DB，刻意不快取（新增，定案）

`AuthorizationService` 的 class summary 明寫 **"both from cache — zero DB on the check path"**；
本條路徑**刻意不對稱**：`Can()` → session → `IUserRepository.IsDeploymentAdmin(userId)` 直接查 DB。

理由：部署層操作（鑄金鑰、指派管理員）低頻，省下的那次查詢不值得引入一組快取與其一致性問題；
更重要的是**撤銷管理員必須即時生效**，而快取方案都會帶一段延遲。

落選方案：

| 選項 | 不採用的理由 |
|------|------------|
| Cache 物件 + cache-notify（比照 `ApiKeyCache`） | 為單一 bool 造一整組快取（`ICacheContainer` 三處同步、兩個 CacheNotify stub），成本與收益不成比例 |
| 登入時寫進 `SessionInfo` | 零 DB 零新元件，但**撤銷要等既有 session 過期**——提權旗標不該吃這個延遲 |

> 這條要寫進 `IDeploymentAuthorizationService` 實作的 XML doc，否則日後 review 會被當成
> 「忘了加快取」的不一致。

### D6：旗標的唯一寫入口是 `SetDeploymentAdmin`（新增，定案）

原 plan 的風險表只寫「不得經一般使用者維護表單寫入」這個**原則**，沒有機制。
現況能成立純粹是框架沒出貨 `st_user` 的 FormSchema——部署端自建一張含該欄的維護表單，防線就沒了。

定案為 **runtime 硬性排除**：FormSchema 驅動的寫入路徑
（[DataFormRepository.Update](../../src/Bee.Repository/Form/DataFormRepository.cs) →
`TableSchemaCommandBuilder.BuildUpdateSpec`）必須剔除受保護欄，即使 FormSchema 宣告了它。

階段 1 要決的實作細節（兩者不互斥，runtime 那層是必要條件）：

| 層 | 作用 | 限制 |
|----|------|------|
| runtime 剔除（**必做**） | 組 INSERT / UPDATE 欄位時濾掉 `st_user.deployment_admin` | 需要一份「受保護欄」清單（table + column），落點是 `DataFormRepository` 或 `TableSchemaCommandBuilder` |
| 定義層驗證（可選） | 比照 `FormSchemaTableRegistrationAnalyzer`，宣告期就擋 | 擋不住部署端 runtime 才載入的定義檔，只能當早期警示 |

### D7：`SetDeploymentAdmin` 走既有 BO / wire 樣板（定案）

即使是 `LocalOnly`，方法仍經 executor 派發，因此照 `bee-add-bo-method` 的跨層樣板走：
`SystemActions.SetDeploymentAdmin` 常數、`ISetDeploymentAdminRequest/Response` 契約、
`SetDeploymentAdminRequest/Response` wire 型別、`IUserRepository.SetDeploymentAdmin`。
比照 `CreateApiKey` 的形狀，不另開特例。

依 D1 的「BO 介面是 BO-to-BO 解耦層」規範，此方法**無跨 BO 消費者，不放 `ISystemBusinessObject`**。

## 階段 1：身分來源與判定接縫

> 工作區已有部分骨架（`DeploymentAction`、`IDeploymentAuthorizationService`、
> `IUserRepository` 兩個方法、`SetDeploymentAdmin` 的 contract / wire 型別、`SystemActions` 常數），
> 尚未 commit、尚未接線、尚未有實作與測試。

1. **`st_user` 加欄 `deployment_admin`**（`DbType="Boolean"`、`AllowNull=false`），三份 TableSchema
   同步：[Defaults](../../src/Bee.Definition/Defaults/TableSchema/common/st_user.TableSchema.xml)、
   `tests/Define/TableSchema/common/`、`apps/Bee.Northwind/Define/TableSchema/common/`。
   既有部署走框架自動 schema 升級（ALTER ADD），既有列由 DEFAULT 填 0。
   > **不要顯式寫 `DefaultValue="0"`** —— 會讓 schema 比對永遠判定需升級，見執行結果與
   > `docs/repo-ops/gotchas/database.md`。
   > `DefaultsTests` 有嵌入檔數斷言，改 Defaults 時留意。
2. **`UserRepository` 補 `IsDeploymentAdmin` / `SetDeploymentAdmin`**——它目前純讀，這是第一組寫入。
   查無使用者時 `IsDeploymentAdmin` 回 `false`（授權問題，兩種情況都該拒）、`SetDeploymentAdmin` 回 `false`。
3. **`DeploymentAuthorizationService` 實作**（落點比照 `AuthorizationService`，
   `Bee.ObjectCaching/Services/`），由 `AddBeeFramework` 註冊。依 D5 每次查 DB，
   並在 XML doc 寫明「刻意不快取」的理由。DB 異常時 **fail-closed**（回 `false`，比照 API Key gate）。
4. **`SystemBO.SetDeploymentAdmin`**：`[ApiAccessControl(LocalOnly, Authenticated)]`，
   比照 `SaveDefine` 加一道 defence-in-depth 的 `IsLocalCall` 檢查。
5. **D6 的 runtime 排除**：受保護欄清單 + 寫入路徑剔除。
6. **測試 seed 不標管理員**（D3）：測試環境若預設有管理員，「無權」路徑就測不到。
   會改寫旗標的測試一律建立自己的使用者列，不共用 seed 使用者。
7. **測試**：旗標讀寫 round-trip（各 dialect）／查無使用者回 `false`／`Can` 對非管理員回 `false`／
   DB 異常 fail-closed／`SetDeploymentAdmin` 遠端呼叫被拒／**D6：FormSchema 宣告了該欄仍寫不進去**。

**驗收**：主機上可指派第一位管理員；`Can(token, ManageApiKey)` 對管理員回 `true`、對其他
已登入使用者回 `false`；經一般 FormSchema 寫入路徑改不到該欄。

### 執行結果（2026-08-01）

驗收條件全數達成，`dotnet build Bee.Library.slnx -c Release --no-incremental` 0w/0e、
`./test.sh` 全綠（新增 21 個測試）。與計畫的差異與追加決策：

| 項目 | 落地情況 |
|------|---------|
| **D3-B「seed 直接有一位管理員」不由框架落地** | 計畫寫「建庫 seed 標一位管理員」，但框架的 `Defaults/` 只出貨 TableSchema、**沒有出貨 `st_user` 資料列的機制**——seed 使用者是各部署（與測試 fixture）自己的事。框架能提供的就是欄位 DEFAULT 0 與 `SetDeploymentAdmin` 這條指派路徑；「新部署要不要預先標一位」由部署端 seeder 決定。測試 seed 使用者刻意**不**標管理員，「無權」路徑才測得到 |
| **`DefaultValue="0"` 反而讓 schema 比對永遠不一致** | 原本照 `st_api_key` 的樣子寫 `DefaultValue="0"`，結果 `TableSchemaBuilder` 對 `st_user` 永遠回 `Upgrade`（3 個既有測試連續失敗）。根因是讀回端把「等於內建預設」的 default 正規化成空字串，兩側因此永遠不等。正解是**不要顯式寫**——內建預設本來就是 0，DDL 產出完全相同。已記入 `docs/repo-ops/gotchas/database.md` |
| **測試不得共用 seed 使用者 `001`** | 第一版讓 repository 與 BO 兩組測試都改 `001` 的旗標，兩個測試專案是不同行程 → 跨行程競賽（實際紅過一次）。改為各自建立唯一使用者、finally 刪除，helper 落在 `tests/Bee.Tests.Shared/TestUsers.cs` |
| D6 的 runtime 剔除落點 | 定在 `DataFormRepository.Save`（`RemoveProtectedFields`），**不放 `TableSchemaCommandBuilder`**——後者是 `Bee.Db` 的通用 DML 工具，沒有理由知道框架保留哪些欄。清單是 `Bee.Definition.ProtectedFields`，以 `table.column` 成對判定 |
| D6 的定義層 analyzer | 未做。runtime 剔除已是必要且充分的防線，analyzer 只是早期警示，等真的有人踩到再說 |
| `docs/api-method-reference.md`（雙語） | 同步新增 `SetDeploymentAdmin` 一列（`BoApiSurfaceTests` 的 baseline 亦同步） |

## 階段 2：套用至 API Key

`CreateApiKey`（及日後 API Key plan 階段 3 的停用 / 列出）**不能單純從 `LocalOnly` 降級**——
那會斷掉階段 1 刻意保留的 bootstrap 路徑：**尚無管理員的既有部署會連第一把金鑰都鑄不出來**。

定案形狀為**同一方法內分流**：

- `IsLocalCall` → 直通（維持既有的部署期作業能力，不需要管理員）
- 遠端 → 要求 `IDeploymentAuthorizationService.Can(token, DeploymentAction.ManageApiKey)`

保護等級因此從 `LocalOnly` 放寬為 `Encrypted`，把關改由部署層授權承擔。

**驗收**：管理員可從遠端鑄金鑰；一般已登入使用者被拒；行程內呼叫行為與升級前一致。

### 執行結果（2026-08-03）

驗收條件全數達成，`dotnet build Bee.Library.slnx -c Release --no-incremental` 0w/0e、
`./test.sh` 全綠（新增 4 個測試）。實作與計畫一致，另記四項執行時的判斷：

| 項目 | 落地情況 |
|------|---------|
| **授權排在輸入驗證之前** | `CreateApiKey` 的分流 gate 緊接在 `ArgumentNullException.ThrowIfNull` 之後、`sys_id` 格式檢查之前。無權呼叫端不該從錯誤訊息反推出「哪些 `sys_id` 合法」「哪個 id 已被使用」——後者本來就是 `Exists` 查詢的回音 |
| **不抽共用 helper** | 階段 3 的停用 / 列出會有相同分流，但目前只有一個呼叫端。三行的安全判定留在呼叫端看得見，比包成 `RequireDeploymentPermission` 後讓「本機直通」藏進 helper 好；真的長到三處再抽 |
| **拒絕用 `UnauthorizedAccessException`** | 比照 `LogBusinessObject` 的稽核查詢授權，不用 `UserMessageException`——後者語意是「使用者輸入有問題」，會被前端當成可修正的表單錯誤顯示 |
| 平行路徑 | `SystemApiConnector.CreateApiKeyAsync` 的 XML doc 原寫「伺服端限定本機呼叫，只能經行程內 connector」，已同步改寫。全 repo 再無其他位置把 `CreateApiKey` 記為 `LocalOnly`（`docs/repo-ops/future-work.md` 的部署期工具一節同步更新） |

新增測試（`SystemBusinessObjectApiKeyTests`）：遠端非管理員被拒（fake 授權服務）／遠端管理員
可鑄金鑰（真實服務，建使用者→標旗標→建 session→遠端呼叫）／有效 session 但無旗標仍被拒／
本機呼叫免管理員。最後一項刻意存在：它盯的是**日後有人把「本機直通」當成漏洞而刪掉**時，
bootstrap 路徑會立刻斷在測試上，而不是斷在某個新部署的第一天。

## 階段 3：稽核與文件

1. 所有部署層操作進稽核並明確記錄操作者，與 API Key 的呼叫端識別同一套。
   **`SetDeploymentAdmin` 本身是提權動作，必須留痕。**
2. 雙語文件說明部署層管理員的定位，明寫「**部署層管理員不會因此取得任何公司的資料權限**」。
3. 既有部署的升級指引：加欄自動升級、第一位管理員如何指派。

### 執行結果（2026-08-03）

三項全數達成，`dotnet build Bee.Library.slnx -c Release --no-incremental` 0w/0e、
`./test.sh` 全綠（新增 5 個測試）。四項實作時才定的決策：

| 項目 | 落地情況 |
|------|---------|
| **D8：走既有變更軸，不另開稽核軸** | 部署層作業寫進 `st_log_change`、`prog_id` 為 `SysProgIds.System`、`source` 為 `System.<Action>`。要記的形狀（誰、改了哪張表的哪一列、從什麼變成什麼）**就是**變更軸；另開一張表得再配一套查詢 API 與每個稽核 UI 的位置，才能講同一件事 |
| **D9：只受 `AuditLogOptions.Enabled` 管，不受 `ChangeEnabled` 管** | 與 `FormBusinessObject` 的資料變更路徑刻意不對稱。`ChangeEnabled` 的存在理由是「業務資料歷程量太大」，而指派管理員既不日常、量也不大。已有測試釘住這條不對稱（關掉 `ChangeEnabled` 仍留痕） |
| **前後值以 DiffGram 承載** | 授予與撤銷同為 `ChangeKind.Update`，沒有 payload 就分不出方向。新增 `Bee.Business/AuditLog/AuditDiffGram.cs` 合成最小 DataSet 再吐 DiffGram，沿用既有 `ChangeDiffGramReader`，變更明細 API 與其上的 UI 都不必知道這列是哪條路徑產生的。`FormBusinessObject` 原本的私有 `SerializeDiffGram` 一併收斂進來 |
| **`ResolveAuditIdentity` 上移到 `BusinessObject`** | FormBO 與 SystemBO 都要「從 session 去正規化操作者 / 公司」，且它只用到基底成員。改為 `protected`（`PublicAPI.Unshipped.txt` 純新增，二進位相容） |

已知取捨：`SetDeploymentAdmin` 的稽核列以**被指派者的 `sys_rowid`** 作 `row_key`，其 `sys_id` 只以
context 欄存在 payload 裡——`ChangeDiffGramReader` 依設計只回報「有差異」的欄，因此
`GetChangeDetail` 目前看不到它。操作者（`user_id` / `user_name`）不受影響，這正是本階段要求要記的對象。
`CreateApiKey` 是 Insert、所有欄都會回報，沒有這個問題。

文件落點：`docs/permission-authorization.md` / `.zh-TW.md` 新增**第三部分「部署層管理」**
（§11 涵蓋範圍、§12 指派與唯一寫入口、§13 稽核、§14 既有部署升級），並在導言與 `docs/README*`
的索引描述補上指引。升級指引特別寫出「**若部署端自帶 `st_user.TableSchema.xml` 就得自己補欄**」——
runtime 只讀 `DefinePath`，內嵌預設不會被參考，這是自動升級唯一漏得掉的情況。

## 風險

| 風險 | 因應 |
|------|------|
| **自我提權**：能編輯 `st_user` 的人把自己標成管理員 | D6 的 runtime 硬性排除——旗標唯一寫入口是需管理員權限（或本機）的獨立動作 |
| 部署層操作繞過公司隔離而無痕 | 階段 3：所有部署層操作一律進稽核且記錄操作者 |
| 與公司層權限混淆 | 兩條判定分屬不同介面、不同資料來源，**不互相 fallback**；文件明寫互不授予 |
| 旗標粒度不足，日後回頭大改 | D2 的簽章先帶動作參數；替換實作不動呼叫端 |
| 判定每次查 DB 成為熱點 | D5 已評估：部署層操作低頻。若日後 `DeploymentAction` 長出高頻消費者，再回頭上快取 |
| 測試共用同一使用者列造成競賽 | 已踩過：改寫旗標的測試一律建立專屬使用者（`tests/Bee.Tests.Shared/TestUsers.cs`） |

## 不在範圍

- **公司 / 租戶管理介面**：見 D4，待列級租戶隔離落地後另案。
- **部署層的細粒度 RBAC**：等真的出現第二、第三個消費者再說。
- **公司層權限模型的任何改動**：`IAuthorizationService` 與 `st_role*` 維持原狀。
- **設定檔 bootstrap 管理員帳號**：D3 已否決——永久後門。
