# 計畫：部署層管理員（不綁公司的營運權限）

**狀態：📝 擬定中（2026-07-30）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 身分來源與判定接縫：`IDeploymentAuthorizationService` ＋ 首位管理員的產生路徑 | 📝 待做 |
| 2 | 套用至 API Key：`CreateApiKey` 等改由部署層權限把關，遠端管理成立 | 📝 待做 |
| 3 | 稽核、文件（雙語）與既有部署的升級指引 | 📝 待做 |

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
| `st_user` | 位於 common 庫，無任何管理員欄位 | 身分來源的候選位置（見 D1） |
| `CreateApiKey` | `LocalOnly` | 階段 2 改為部署層權限把關後，遠端管理才成立 |

## 決策待議

### D1：管理員身分放哪裡

| 選項 | 說明 | 評估 |
|------|------|------|
| **A. `st_user` 加旗標欄** | 如 `deployment_admin`（Boolean） | 最簡、common 庫天然不綁公司、立刻可用。粒度是全有全無 |
| B. common 庫另建一組 RBAC | `st_deployment_role` / `_grant` / `st_user_deployment_role`，鏡像公司層那三張表 | 粒度細、可擴充；為了目前唯一的消費者（API Key）造一整套，成本明顯過頭 |
| C. 設定檔列舉管理員帳號 | 寫在 `SystemSettings` | 不進 DB、無管理介面；改名單要改檔案，且「營運行為不該是部署設定」正是 API Key plan 否決定義檔的理由 |
| D. 沿用 `PermissionModels` 加部署層 model | 模型定義複用，角色資料另存 common | 看似省事，但會讓同一套 model id 有兩種 scope 語意，日後難解釋 |

**傾向 A，但判定走新接縫**：`IDeploymentAuthorizationService`（`Bee.Definition/Identity/`），
呼叫端只問「這個 token 能不能做這件事」。今天以旗標實作，日後若真需要細粒度，
換掉實作即可、呼叫端不動。**先立接縫、後補粒度**，而不是先造一套沒人用的 RBAC。

### D2：授權粒度

- **全有全無**（`IsDeploymentAdmin(accessToken)`）——與 D1-A 相稱，一句話講得清楚。
- **動作別**（`Can(accessToken, DeploymentAction.ManageApiKey)`）——接縫上先留列舉，
  實作先一律以旗標回答；日後細分不必改簽章。

傾向**後者的簽章 ＋ 前者的實作**：介面帶動作參數，實作階段先全有全無。
簽章多一個參數的成本，遠低於日後回頭改所有呼叫端。

### D3：首位管理員怎麼產生（雞生蛋）

沒有管理員就沒人能指派管理員。比照 API Key plan 階段 1 讓第一把金鑰得以產生的處理方式：

| 選項 | 說明 |
|------|------|
| A. 本機宿主指派 | 保留一條 `LocalOnly` 的指派方法，第一位在主機上設定，之後遠端接手 |
| B. seed 資料 | 建庫時把 seed 使用者標為管理員 |
| C. 設定檔 bootstrap 帳號 | 設定檔指定一個帳號永遠具管理員身分（救援用） |

傾向 **A ＋ B**：新部署由 seed 直接有一位；既有部署以本機方法指派第一位。
C 留作救援手段時要注意它等於一個永久後門，需明確權衡。

### D4：本 plan 的涵蓋範圍

- **僅解 API Key**（最小，解鎖 api-key-store 階段 3 即收工），或
- **一併把公司管理納入**（建立 / 停用試用公司），與租賃方向對齊。

傾向**僅解 API Key**：公司管理的形狀要等列級租戶隔離落地才看得清楚，
現在一起做會兩邊互相等待。接縫立起來後，公司管理只是多一個 `DeploymentAction`。

## 風險

| 風險 | 因應 |
|------|------|
| **自我提權**：能編輯 `st_user` 的人把自己標成管理員 | 管理員欄位**不得**經一般使用者維護表單寫入——欄位層保護，且指派本身要是一個獨立的、需管理員權限的動作 |
| 部署層操作繞過公司隔離而無痕 | 所有部署層操作一律進稽核，且明確記錄操作者；與 API Key 的呼叫端識別同一套 |
| 與公司層權限混淆 | 兩條判定分屬不同介面、不同資料來源，不互相 fallback；文件明寫「部署層管理員**不會**因此取得任何公司的資料權限」 |
| 旗標粒度不足，日後回頭大改 | D2 的簽章先帶動作參數；替換實作不動呼叫端 |
| 救援後門（若採 D3-C） | 若採用，須為明確且可稽核的設定，並在文件標示風險 |

## 不在範圍

- **公司 / 租戶管理介面**：見 D4，待列級租戶隔離落地後另案。
- **部署層的細粒度 RBAC**：D1-B，等真的出現第二、第三個消費者再說。
- **公司層權限模型的任何改動**：`IAuthorizationService` 與 `st_role*` 維持原狀。
