# 計畫：前端權限 Capability（element 細粒度降級）

**狀態：📝 擬定中（2026-06-06）— blocked，待前置依賴完成**

> 這是一份**設計紀要**，不是可執行的實作 plan。整套 ERP 權限的前端最後一塊（element→action 綁定 + 逐元素降級）目前**刻意延後**，因為它的綁定面（宣告式工具列／命令 element）在前端尚不存在。本文件記下探勘結論與已拍板的設計決策，等前置依賴完成後再展開完整實作 plan，避免思路流失與返工。

---

## 為什麼延後（核心判斷）

本任務核心 = 「element→action 綁定」+「逐元素降級」。但前端現在**沒有可綁的 element**：

- **工具列按鈕是 hardcode** — `src/Bee.Web.Blazor.Server/Components/FormPage.razor` 的 New / Save / Delete 直接寫死在 razor，只受 `_isBusy` 與資料狀態控制，**不是** `FormLayout` 裡的宣告式 element。沒有「按鈕 element」可掛 `PermissionAction`。
- **唯一成熟的 element 是欄位 / Grid** — `LayoutField` / `LayoutColumn`（有 `Visible` / `ReadOnly`）、`LayoutGrid`（有 `AllowActions`）。但 capability 最關鍵的降級對象是「動作按鈕」，而按鈕還沒模型化。
- **依賴順序被倒過來** — 應先讓「工具列／命令」成為 `FormLayout` 的宣告式模型（像 `LayoutField` / `LayoutGrid` 那樣有 `Id` / `Caption` / `Action`），capability 才有穩定綁定面。現在做＝綁在 hardcode 按鈕上，工具列模型化時整段 element→action 重做。
- **`不為假設的未來建類`**（CLAUDE.md code-style）— capability 資料骨幹雖 UI-agnostic、可先做，但目前**零消費端**；在沒有消費端驗證 payload 形狀前定死 `EnterCompanyResponse` 的 capability 結構，有把形狀猜錯、返工的風險。

### 前置依賴（解除 blocked 的條件）

**先做「工具列／命令宣告式模型」**：把 New / Save / Delete 及未來自訂命令（Print / Export / Approve…）變成 `FormLayout` 內的宣告式 element，每顆有 `Id` / `Caption`（以及 capability 要用的 action 綁定面）。這份模型落地後，本 plan 才展開為可執行的實作 plan。

---

## 後端現況（已完成，本任務的地基）

整套權限後端已完成，設計見 [ADR-019](../adr/adr-019-permission-authorization-model.md) 與 [permission-authorization.zh-TW.md](../permission-authorization.zh-TW.md)。與前端 capability 直接相關的點：

| 項目 | 位置 | 說明 |
|------|------|------|
| `PermissionAction`（action 軸，`[Flags]`） | `src/Bee.Definition/Settings/Permission/PermissionAction.cs` | `None/Create/Read/Update/Delete/Print/Export` |
| 層一動作判定 | `src/Bee.ObjectCaching/Services/AuthorizationService.cs` | `Can(token, modelId, action)`，全程零 DB |
| per-company 權限快照 | `src/Bee.Definition/Identity/CompanyRolePermissions.cs` | `GetAllowed(roleIds, modelId)` → OR-merged action mask |
| FormSchema 綁 model | `FormSchema.PermissionModelId` | 表單宣告它消費哪個 model（capability 要用） |
| EnterCompany 快照 roles | `src/Bee.Business/System/SystemBusinessObject.cs:142`（`EnterCompany`） | 此刻手邊就有 `snapshot`（`CompanyRolePermissions`）與 `sessionInfo.Roles` |

**關鍵觀察**：`SystemBusinessObject.EnterCompany` 是天然載具——它此刻已持有 `snapshot` 與 `Roles`，正是算出「per-model action mask」並掛上回應的點。client 已呼叫 `EnterCompanyAsync`（`src/Bee.Api.Client/Connectors/SystemApiConnector.cs:292`），所以 **capability 搭 EnterCompany 這班車＝零額外往返**。

> ADR-019 第 69 行已明載：「前端 capability（element 細粒度降級）為獨立關注點…元素層按鈕→action 的降級屬前端 capability，另案。」本 plan 即該「另案」。

---

## 前端現況（探勘結論）

| 項目 | 位置 | 現況 |
|------|------|------|
| 前端身分資訊 | `src/Bee.UI.Core/ClientInfo.cs`（`UserInfo`） | **只有 `UserId` / `UserName` / `Culture` / `TimeZone`，無 Roles / grants** |
| 客戶端 session 載具 | `ClientInfo`（static） | `AccessToken` / `UserInfo`；`ApplyLoginResult` 設定 |
| EnterCompany 回應 | `src/Bee.Api.Core/Messages/System/EnterCompanyResponse.cs` | 目前只回 `CompanyInfo Company`（`[Key(100)]`） |
| 工具列按鈕 | `FormPage.razor` 15–17 行 | New / Save / Delete **hardcode**，僅 `_isBusy` 控制 |
| 欄位降級綁定點 | `src/Bee.Definition/Layouts/LayoutFieldBase.cs` | `Visible`（74 行）/ `ReadOnly`（83 行），Blazor 已讀取 |
| Grid 降級綁定點 | `src/Bee.Definition/Layouts/LayoutGrid.cs` | `AllowActions`（`GridControlAllowActions` flags：Add/Edit/Delete） |
| 欄位渲染 | `src/Bee.Web.Blazor.Server/Components/DynamicForm.razor(.cs)` | 篩 `Visible==true`、`readonly/disabled=@field.ReadOnly` |
| Grid 渲染 | `src/Bee.Web.Blazor.Server/Components/DynamicGrid.razor.cs` | `VisibleColumns` 篩 `Visible==true` |
| 後端 resolver 風格參考 | `src/Bee.Business/Permission/ScopeResolver.cs` | 純函式、DI 注入多服務 → CapabilityResolver 仿照 |
| 前端 DI | `src/Bee.Web.Blazor.Server/DependencyInjection/BeeBlazorServiceCollectionExtensions.cs` | `AddBeeBlazor`、`BeeApiConnectorFactory` |

**結論**：前端目前完全沒有權限資訊；降級綁定點（欄位 `Visible`/`ReadOnly`、Grid `AllowActions`）已就緒，但**動作按鈕尚未模型化**。

---

## 已拍板的設計決策

| 決策 | 結論 |
|------|------|
| **按鈕降級（無權時）** | **隱藏** —— 無權的工具列按鈕 / Grid 內聯動作直接不渲染（`Visible=false`）。 |
| **欄位權限控管是選擇性（opt-in）** | **只有敏感欄需要標記控管；絕大多數欄位不控管**，照 layout 正常顯示與編輯。未標記欄位永遠不參與 capability 降級——避免「整 model 一致」讓逐欄判定淪為無意義的全有全無。 |
| **敏感欄用「具名分類」標記** | `FormField` 上加一個**敏感分類列舉**（如 `SensitiveCategory`：`None` / `Amount` 金額 / `Cost` 成本 / `PersonalData` 個資，可擴充），**平行既有的 `ScopeRole`**（同為 `[XmlAttribute]` + `[DefaultValue(None)]` 的 enum）。表單設計者只挑語意分類、不發明 id；列舉有限 → 可載入期驗證。 |
| **單一真相源在 FormSchema 中樞** | `SensitiveCategory` **只標在 `FormField`（FormSchema）**，不複製到 `LayoutField`。`FormSchema` 是定義中樞（驅動 UI / DB / 驗證），敏感分類一處定義、處處讀：本案的 UI 降級、未來的 DB 端遮罩、驗證皆從此處取。resolver 以 `FormSchema` + `FieldName` 反查（前端開表單時 `FormSchema` 與 `FormLayout` 同在手邊，零額外成本），避免兩處標記漂移。 |
| **欄位降級（兩階,依 Read / Update）** | 被標記的敏感欄受 **Read / Update 兩個 action** 控管：**無 `Read` → 隱藏（`Visible=false`）**、**無 `Update` → 唯讀（`ReadOnly=true`）**。隱藏優先於唯讀（連看都不能看就不必談能不能改）。FormLayout 產生 UI，隱藏只是不輸出該欄，成本極低。 |
| **CapabilityResolver 歸屬層** | **`Bee.UI.Core`**（UI-agnostic）—— Blazor / MAUI / WinForms 共用同一套解析；各 UI 元件只消費結果。對齊後端 `ScopeResolver` 的純函式風格。 |
| **scope 是否參與前端 capability** | **否** —— capability 純 action-level（能不能做此動作），record scope（哪些列）留後端權威 re-query。前端只做 UX 降級，安全邊界在後端（ADR-019 約束 #4）。 |

> **敏感分類是與主表單 model 正交的獨立 gate**：敏感欄的可見性需要「與主表單 model **不同**的 gate」——使用者有 `PurchaseOrder.Read`（看得到採購單），但「成本」欄看不看得到是另一回事，由**敏感分類**（`Cost`）的 Read 決定。為什麼用「分類」而非「每欄一個任意 model id」：金額 / 成本 / 個資是**資料分類**語意（能看成本的人到哪都能看成本、有個資權的人到哪都能看個資），全公司一致、跨表單共用一次授權，比逐欄發明 model id 更貼 ERP 實務、更好治理、可載入期驗證。`SensitiveCategory` 與 `FormField.ScopeRole` 形狀完全一致（enum + `[XmlAttribute]` + `[DefaultValue(None)]`），是現成 pattern 的複製。**未標記（`None`）→ 不控管**（預設放行），向後相容、漸進導入。

## 待決議題（實作 plan 展開時定案）

### 議題 1：capability 資料怎麼到前端（兩案皆零額外往返，差在計算地點）

| 案 | 作法 | 優點 | 缺點 |
|----|------|------|------|
| **A：後端算好遮罩** | EnterCompany 時後端用 `CompanyRolePermissions` 算出 `Dictionary<modelId, PermissionAction>`（有效 action mask），附在 `EnterCompanyResponse`；client 快取於 `ClientInfo`，CapabilityResolver 純查表 | 傳輸最小；grant 細節不外洩到前端；前端不重複後端 OR-merge 邏輯 | 後端多算一份「全 model mask」；payload 形狀要先定 |
| **B：傳原料前端自算** | 後端把該 user 的 grant rows + roles 整包給前端，CapabilityResolver 在前端 OR-merge | 更貼近後端 `GetAllowed` 邏輯；後端不需預算全 model | grant 結構暴露到前端；傳輸較大；前端要複製合併邏輯（易與後端漂移） |

> 傾向 A（傳輸小、不外洩、邏輯單點），但待實作時連同 payload 形狀一起定案。

### 議題 2：element→action 綁定方式（與前置依賴「工具列模型」綁定）

| 案 | 作法 | 優點 | 缺點 |
|----|------|------|------|
| **約定式** | 標準工具列固定對應：New→Create、Save→Create\|Update、Delete→Delete；敏感欄（opt-in）→{Read 控可見、Update 控可編輯}；Grid 沿用 `AllowActions` 對應 Add/Edit/Delete | 改動最小、涵蓋現有 CRUD | 自訂命令（Print/Export/Approve）無處宣告 |
| **約定式 + 宣告式 override** | 以約定為底，FormLayout 按鈕 element 可選擇性標 `Action`；未標走約定 | 擴充性好、改動中等 | 需要工具列 element 已模型化 |
| **純宣告式** | 每顆按鈕 element 顯式宣告 `PermissionAction` | 最彈性 | 改動最大；強依賴工具列模型 |

> 本議題**直接依賴前置依賴**（工具列宣告式模型）。模型怎麼設計，決定這裡走哪案。

### 議題 3：敏感分類怎麼接到授權 gate（觸及後端）

**已定向**：`FormField` 加 `SensitiveCategory` 列舉（`None` / `Amount` / `Cost` / `PersonalData`…，可擴充），平行 `ScopeRole`。剩下要定案「分類 → 授權判定」怎麼接：

| 案 | 作法 | 優點 | 缺點 |
|----|------|------|------|
| **A：分類即 permission model（well-known id）** | 每個分類對應一個約定的 `PermissionModel` id（`Amount` / `Cost` / `PersonalData`）；grant 照 `(role, 分類model, action)` 授；resolver 直接複用 `Can` / `GetAllowed` | 後端**零新增 enforcement 維度**，完全複用既有 model / grant / 快取；前端 capability mask 自然含這幾個分類 | 分類列舉與 well-known model id 要保持對齊（載入期驗證可顧） |
| **B：獨立「欄位敏感授權」維度** | 新增一張 grant 表 / 一組 service 專管分類授權 | 概念上與業務 model 分開 | 多一套 enforcement + 快取 + 失效鏈，重複造輪、維護成本高 |

> 傾向 **A**：分類就是一組**資料分類用的 permission model**，沿用 ADR-019 全套基建，後端只需定義這幾個 well-known model 並授 grant。

子決策（A 之下，定案時敲定）：
- **分類 gate 的廣度**：全公司一致（`Cost` 一個 gate 管全部表單，資料分類語意，傾向此）vs 綁業務實體（`PurchaseOrderCost` / `SalesOrderCost` 分開）。
- **分類列舉的歸屬**：放 `Bee.Definition`（與 `ScopeRole` 同處）。
- **載入期驗證**：`PermissionBindingValidator` 補一條——`SensitiveCategory` 非 `None` 的欄，其對應 well-known model 必須存在於 `PermissionModels`。

---

## CapabilityResolver 草圖（待前置完成後細化）

```
// Bee.UI.Core（UI-agnostic）
IElementCapabilityResolver
  // 按鈕／命令
  ElementCapability ResolveCommand(FormSchema schema, <command>, capabilitySnapshot)
    // 1. schema.PermissionModelId 取得 modelId（空 → 不套權限，全放行）
    // 2. command → PermissionAction（依議題 2 定案的綁定方式）
    // 3. 查 capabilitySnapshot 得 allowed mask
    // 4. 無權 → Visible=false（按鈕隱藏政策）

  // 欄位（opt-in：只有 SensitiveCategory != None 的敏感欄才控管）
  FieldCapability ResolveField(FormSchema schema, string fieldName, capabilitySnapshot)
    // 0. 由 schema 反查 FormField（單一真相源在 FormSchema 中樞,不讀 LayoutField）
    // 1. formField.SensitiveCategory == None → 不控管,直接放行（Visible 照 layout、可編輯）
    // 2. 非 None → 分類對應的 well-known model 查 mask：
    //    無 Read → Visible=false（優先）；有 Read 但無 Update → ReadOnly=true
```

消費端（Blazor 起步，MAUI/WinForms 後續）：
- 工具列按鈕：`ResolveCommand` → 無權不渲染。
- `DynamicForm` 欄位：`ResolveField` → **僅敏感欄**參與；無 `Read` 時不輸出該欄（`Visible=false`）、無 `Update` 時 `ReadOnly=true`；其餘欄位照 layout。
- `DynamicGrid` / `LayoutGrid`：敏感欄比照欄位規則；Grid 動作以 `AllowActions` 交集 capability，遮掉無權的 Add/Edit/Delete。

---

## 後續步驟

1. **（前置）** 擬「工具列／命令宣告式模型」plan 並實作 —— 解除本 plan blocked 的條件。
2. 前置完成後，把本設計紀要展開為可執行實作 plan：定案議題 1 / 2 的 payload 與綁定，列出跨檔改動（`EnterCompanyResponse` + contract、`SystemBusinessObject.EnterCompany`、`ClientInfo` 客戶端快取、`Bee.UI.Core` CapabilityResolver、Blazor 元件消費）、測試與 round-trip。
3. 實作落地後更新本 plan 狀態列為 ✅，並補對應 ADR / 使用者指南「非目標」段落的轉正。

## 參考

- [ADR-019：權限授權模型](../adr/adr-019-permission-authorization-model.md)（第 69、140 行標明 capability 為另案）
- [權限與授權指南](../permission-authorization.zh-TW.md)
- 封存 plan：`docs/archive/plan-erp-permission.md`、`plan-permission-line-b.md`、`plan-record-scope-enforcement.md`、`plan-org-department-tree.md`
