# 計畫：前端權限 Capability（element 細粒度降級）

**狀態：✅ 已完成（2026-07-03）— Bee.UI.Avalonia 實作落地；分類 gate 全公司一致。DB 綁定測試待 CI 驗證**

> **實作完成摘要（2026-07-03）**
> - 後端：`CompanyRolePermissions.GetAllowedByModel` 算 per-model mask；`EnterCompany` 填 `EnterCompanyResult.Capabilities`；經 `ApiOutputConverter` 自動 copy 到 `EnterCompanyResponse.Capabilities`（`[Key(101)]`，三棲）。
> - 定義：`FormField.SensitiveCategory`（平行 `ScopeRole`）＋ `SensitiveCategoryExtensions.ToPermissionModelId`（well-known model 慣例單一來源）＋ `FormSchema.FindField` 反查＋ `PermissionBindingValidator` 載入期驗證。
> - Client：`ClientInfo.Capabilities`（nullable，null=未啟用→全放行）＋ `ApplyEnterCompanyResult` / `ClearCompanyContext`；`Bee.UI.Core.Permissions.IElementCapabilityResolver` + `ElementCapabilityResolver`（純函式）。
> - Avalonia：`PermissionScope` AttachedProperty（按鈕建立時自帶 action）＋ `LayoutCapabilityApplier`（對**每視圖新生成**的 layout 就地降級，不動快取定義）；`FormView` / `ListView` 標按鈕 + 套用。
> - 測試：resolver 13、CompanyRolePermissions/PermissionBinding 27、EnterCompany MessagePack 5，全綠；Avalonia 320 無回歸。DB 綁定的 `EnterCompanyJsonRpc` / `SystemBusinessObjectEnterCompany` 本機無 docker 故 fail（連線字串 null），待 CI（有 DB service container）驗證。
> - **消費端 opt-in**：host app 需在 `EnterCompanyAsync` 後呼叫 `ClientInfo.ApplyEnterCompanyResult(response)`（並在 `LeaveCompany` 呼叫 `ClearCompanyContext`），且後端定義 `Amount`/`Cost`/`PersonalData` well-known model 並授 grant，capability 才生效；未接時一切照舊（向後相容）。

> 前身為 blocked 的設計紀要。**改用 `src/Bee.UI.Avalonia` 作為消費端、且命令按鈕於建立時自帶 opt-in `PermissionAction` 屬性後，原 blocker 完全消除**：按鈕不必先在 FormLayout 模型化——framework 建立工具列時直接給每顆按鈕對應的 `PermissionAction`（未設 = 不控管），capability 層掃一遍讀屬性設 `IsVisible`。標準與自訂命令走同一機制。三項待決議題已於 2026-07-03 拍板（見下）。
>
> **FormLayout 宣告式命令模型自此與權限脫鉤**——只在「命令需資料驅動定義（非程式建立）」時才需要，屬另案，不再是本 plan 的前置。故本 plan 收斂為**單一可執行 plan**（不再分 blocked 階段）。

---

## 為什麼 blocker 消除了（核心判斷）

原 plan 延後的理由是「前端沒有可綁的 element（工具列 hardcode）」。這個顧慮建立在一個隱含假設上：**權限綁定面必須在 FormLayout 定義裡**。改採「按鈕控件自帶屬性」後，這個假設不成立——綁定面在**建立按鈕的程式碼現場**，framework 建工具列時給值即可：

```csharp
// FormView / ListView 建工具列時（示意）
_newButton    = new Button { Content = "New" };
_deleteButton = new Button { Content = "Delete" };
PermissionScope.SetAction(_newButton,    PermissionAction.Create);   // 建立時自帶
PermissionScope.SetAction(_deleteButton, PermissionAction.Delete);   // 未設 → 不控管
// ...capability 載入後，單次掃描套 IsVisible
```

各降級對象在 Avalonia 的落地面：

| 降級對象 | 綁定面 | Avalonia 現況 / 佐證 | 落地方式 |
|---------|--------|---------------------|---------|
| 命令按鈕（標準 + 自訂） | **建立時自帶 `PermissionAction`**（opt-in，未設=不控管） | 按鈕為固定已知欄位（[ListView.cs:83](../../src/Bee.UI.Avalonia/Views/ListView.cs)、[FormView.cs:108](../../src/Bee.UI.Avalonia/Views/FormView.cs)） | AttachedProperty + 單次掃描設 `IsVisible` |
| 欄位可見 / 唯讀 | `FormField.SensitiveCategory`（FormSchema 中樞） | `FormView.EnumerateFields`（[FormView.cs:581](../../src/Bee.UI.Avalonia/Views/FormView.cs)）篩 Visible、`FieldEditorBinder.IsLayoutReadOnly`（[FieldEditorBinder.cs:84](../../src/Bee.UI.Avalonia/Controls/Editors/FieldEditorBinder.cs)）讀 ReadOnly | bind 時合成 |
| Grid 動作 | `LayoutGrid.AllowActions` | `GridControl.UpdateControlState`（[GridControl.cs:473](../../src/Bee.UI.Avalonia/Controls/GridControl.cs)） | `AllowActions & capMask` |

**三個讓它乾淨落地的關鍵**：

1. **屬性掛在 UI 控件、建立時給值** → 按鈕免在 FormLayout 模型化；標準/自訂命令同一機制；未設 = 不控管，向後相容。
2. **FormView / ListView 已全程走 `ClientInfo.Resolve*` hook**（[ListView.cs:267–277](../../src/Bee.UI.Avalonia/Views/ListView.cs)）→ capability 快照掛 `ClientInfo`、resolver 放 `Bee.UI.Core`（Avalonia 已 ProjectReference），是現成注入縫。
3. **降級套用不 mutate 快取定義**（守 definition-immutability）→ bind 時合成：`有效可見 = layout.Visible && cap.Visible`、`有效唯讀 = layout.ReadOnly || cap.ReadOnly`、`Grid 動作 = AllowActions & capMask`、按鈕 `IsVisible = cap.Can(action)`。

> **屬性放 UI 控件（而非 FormLayout）的取捨**：綁定面在建立現場最輕、無需定義模型；代價是各 UI（Avalonia / 未來 MAUI / Blazor）各自實作「讀屬性設可見」的 glue。但**判定邏輯（`Can(model, action)`）仍單點在 `Bee.UI.Core` resolver**，各 UI 只消費結果——符合既有分層。

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

---

## 前端現況（Avalonia，探勘結論）

| 項目 | 位置 | 現況 |
|------|------|------|
| 前端身分資訊 | `src/Bee.UI.Core/ClientInfo.cs`（`UserInfo`） | 只有 `UserId` / `UserName` / `Culture` / `TimeZone`，**無 Roles / capability 快照** |
| 客戶端 session 載具 | `ClientInfo`（static） | `AccessToken` / `UserInfo`；capability 快照擬掛此處 |
| EnterCompany 回應 | `src/Bee.Api.Core/Messages/System/EnterCompanyResponse.cs` | 目前只回 `CompanyInfo Company`（`[Key(100)]`），擬加 capability mask |
| 表單/列表 host | `src/Bee.UI.Avalonia/Views/FormView.cs`、`ListView.cs` | 全程走 `ClientInfo.Resolve*` hook；工具列按鈕為固定已知欄位 |
| 欄位降級點 | `FormView.EnumerateFields`（篩 Visible）、`FieldEditorBinder.IsLayoutReadOnly`（讀 ReadOnly） | ✅ 已就緒，capability 合成即可 |
| Grid 降級點 | `GridControl.UpdateControlState`（讀 `AllowActions`）、`EnumerateVisibleColumns`（篩 Visible） | ✅ 已就緒，交集即可 |
| Resolver 歸屬 | `src/Bee.UI.Core` | Avalonia / 未來 MAUI / Blazor 共用，各 UI 只消費結果 |

---

## 已拍板的設計決策

| 決策 | 結論 |
|------|------|
| **消費端** | **`Bee.UI.Avalonia`**（繼承式控件）。Resolver 仍在 `Bee.UI.Core`（UI-agnostic），Avalonia / 未來 MAUI / Blazor 共用。 |
| **命令權限綁定** | **按鈕於建立時自帶 opt-in `PermissionAction` 屬性**（AttachedProperty）。framework 建工具列時給值（New→Create、Save→Create\|Update、Delete→Delete）；自訂命令同法；**未設 = 不控管**。 |
| **按鈕降級（無權時）** | **隱藏** —— 無權的工具列按鈕 / Grid 內聯動作直接不渲染（`IsVisible=false`）。 |
| **欄位權限控管是選擇性（opt-in）** | **只有敏感欄需要標記控管；絕大多數欄位不控管**，照 layout 正常顯示與編輯。未標記欄位永遠不參與 capability 降級。 |
| **敏感欄用「具名分類」標記** | `FormField` 上加**敏感分類列舉** `SensitiveCategory`（`None` / `Amount` / `Cost` / `PersonalData`，可擴充），**平行既有 `ScopeRole`**（`[XmlAttribute]` + `[DefaultValue(None)]`）。設計者只挑語意分類、不發明 id；列舉有限 → 可載入期驗證。 |
| **單一真相源在 FormSchema 中樞** | `SensitiveCategory` **只標在 `FormField`（FormSchema）**，不複製到 `LayoutField`。resolver 以 `FormSchema` + `FieldName` 反查（前端開表單時兩者同在手邊，零額外成本）。 |
| **欄位降級（兩階，依 Read / Update）** | 敏感欄受 **Read / Update** 控管：無 `Read` → 隱藏、無 `Update` → 唯讀。隱藏優先於唯讀。 |
| **scope 不參與前端 capability** | capability 純 action-level；record scope（哪些列）留後端權威 re-query。前端只做 UX 降級，安全邊界在後端（ADR-019 約束 #4）。 |

### 三項待決議題已拍板（2026-07-03）

| 議題 | 定案 | 理由 |
|------|------|------|
| **議題 1：capability 資料怎麼到前端** | **案 A — 後端算好遮罩** | EnterCompany 時後端用 `CompanyRolePermissions` 算 `Dictionary<modelId, PermissionAction>`，附在 `EnterCompanyResponse`，client 快取於 `ClientInfo`，resolver 純查表。傳輸小、grant 不外洩、OR-merge 邏輯單點。 |
| **議題 2：element→action 綁定** | **按鈕建立時自帶 opt-in `PermissionAction` 屬性** | 綁定面在建立現場、掛 UI 控件（AttachedProperty），不需 FormLayout 命令模型；標準與自訂命令同一機制；未設 = 不控管。判定邏輯仍單點在 `Bee.UI.Core` resolver。 |
| **議題 3：敏感分類接授權 gate** | **案 A — 分類即 well-known permission model** | 每個分類對應約定的 `PermissionModel` id，grant 照 `(role, 分類 model, action)` 授，resolver 複用 `Can` / `GetAllowed`。後端零新增 enforcement 維度，完全複用 ADR-019 基建。 |

> **敏感分類是與主表單 model 正交的獨立 gate**：使用者有 `PurchaseOrder.Read`（看得到採購單），但「成本」欄看不看得到由**敏感分類**（`Cost`）的 Read 決定。用「分類」而非「逐欄任意 model id」：金額/成本/個資是**資料分類**語意，全公司一致、跨表單共用一次授權，貼 ERP 實務、可載入期驗證。未標記（`None`）→ 不控管（預設放行），向後相容。

**議題 3 子決策**：
- **分類 gate 廣度**：全公司一致（`Cost` 一個 gate 管全部表單，傾向此）vs 綁業務實體 —— 展開時最終敲定。
- **分類列舉歸屬**：`Bee.Definition`（與 `ScopeRole` 同處）。
- **載入期驗證**：`PermissionBindingValidator` 補一條——`SensitiveCategory` 非 `None` 的欄，其對應 well-known model 必須存在於 `PermissionModels`。

---

## 實作範圍（跨檔改動）

### 1. 後端算 mask（議題 1 案 A）

- `src/Bee.Api.Core/Messages/System/EnterCompanyResponse.cs`：加 `Dictionary<string, PermissionAction> Capabilities`（新 `[Key(101)]`），三棲序列化（XML/JSON/MessagePack）+ round-trip 測試。
- `src/Bee.Business/System/SystemBusinessObject.cs`（`EnterCompany`，142 行附近）：以手邊 `snapshot` + `sessionInfo.Roles`，對相關 model（含敏感分類 well-known model）逐一 `GetAllowed` OR-merge，填 `Capabilities`。
- contract / wire 對齊（依 `bee-add-bo-method` 慣例）。

### 2. 前端快取 + resolver（Bee.UI.Core）

- `src/Bee.UI.Core/ClientInfo.cs`：`EnterCompanyAsync` 回來後把 `Capabilities` 快取（唯讀快照，清 session 時一併清）。
- 新增 `IElementCapabilityResolver` + 實作於 `Bee.UI.Core`（純函式，對齊後端 `ScopeResolver` 風格）：
  - `Can(FormSchema schema, PermissionAction action, snapshot)` → bool（`schema.PermissionModelId` 空 → 全放行）
  - `ResolveField(FormSchema schema, string fieldName, snapshot)` → `FieldCapability { Visible, ReadOnly }`（`SensitiveCategory == None` → 全放行）
  - `ResolveGridActions(LayoutGrid grid, FormSchema schema, snapshot)` → 交集後的 `GridControlAllowActions`

### 3. Avalonia 消費端（自帶屬性 + 合成，不 mutate 定義）

- **命令按鈕**：新增 `PermissionScope` AttachedProperty（`src/Bee.UI.Avalonia`），承載 `PermissionAction`；`ListView`（83 行）/`FormView`（108 行）建按鈕時給值；capability 載入後單次掃描工具列，對非 `None` 的按鈕設 `IsVisible = resolver.Can(schema, action, snapshot)`。
- **敏感欄位**：`FormView.EnumerateFields`（581 行）與 `FieldEditorBinder`（84 行）合成 `layout.Visible && cap.Visible` / `layout.ReadOnly || cap.ReadOnly`。
- **Grid**：`GridControl.UpdateControlState`（473 行）以 `AllowActions & capMask` 設按鈕；敏感欄比照欄位規則，`EnumerateVisibleColumns`（1135 行）合成可見性。

### 4. 敏感分類定義 + 驗證

- `src/Bee.Definition`：新增 `SensitiveCategory` 列舉；`FormField` 加對應 `[XmlAttribute]` + `[DefaultValue(None)]` 屬性（平行 `ScopeRole`）。
- `PermissionBindingValidator` 補載入期驗證（非 `None` → 對應 well-known model 必須存在）。
- 後端定義 well-known 分類 model（`Amount` / `Cost` / `PersonalData`）並可授 grant。

### 5. 測試

- `EnterCompanyResponse` 三棲 round-trip（含 `Capabilities`）。
- `CapabilityResolver` 純函式單元測試（各分類 Read/Update 組合、model 無權、`None` 放行、命令 `Can`）。
- Avalonia glue 測試（按鈕 `IsVisible`、欄位 Visible AND / ReadOnly OR、Grid 交集）。

---

## 非本 plan 範圍（已脫鉤）

- **FormLayout 宣告式命令模型**：命令若要**資料驅動定義**（設計者在定義檔宣告自訂命令，而非程式建立）才需要，與權限降級脫鉤，屬另案。本 plan 的自訂命令走「程式建立時自帶屬性」即可。
- **record scope 前端化**：留後端權威 re-query，前端只做 action-level UX 降級。

---

## CapabilityResolver 草圖

```
// Bee.UI.Core（UI-agnostic）
IElementCapabilityResolver
  // 命令（按鈕自帶 PermissionAction，UI glue 呼叫）
  bool Can(FormSchema schema, PermissionAction action, snapshot)
    // schema.PermissionModelId 空 → true（不套權限）
    // 否則查 snapshot mask 是否含 action

  // 欄位（opt-in：只有 SensitiveCategory != None 才控管）
  FieldCapability ResolveField(FormSchema schema, string fieldName, snapshot)
    // 0. 由 schema 反查 FormField（單一真相源，不讀 LayoutField）
    // 1. SensitiveCategory == None → 放行
    // 2. 非 None → 分類 well-known model 查 mask：
    //    無 Read → Visible=false（優先）；有 Read 無 Update → ReadOnly=true

  // Grid 動作
  GridControlAllowActions ResolveGridActions(LayoutGrid grid, FormSchema schema, snapshot)
    // grid.AllowActions & capMask（無權的 Add/Edit/Delete 遮掉）
```

消費端（Avalonia）：按鈕讀 `PermissionScope.Action` → `Can` → 設 `IsVisible`；欄位/Grid 合成套用，**不 mutate 快取定義**。

---

## 後續步驟

1. 使用者確認本 plan 方向 → 展開實作（先敲定議題 3 子決策：分類 gate 廣度）。
2. 落地後更新狀態列為 ✅，補對應 ADR / 使用者指南「非目標」段落轉正。

## 參考

- [ADR-019：權限授權模型](../adr/adr-019-permission-authorization-model.md)（第 69、140 行標明 capability 為另案）
- [權限與授權指南](../permission-authorization.zh-TW.md)
- Avalonia 是 UI 架構試點：先在 `Bee.UI.Avalonia` 定稿，再移植 MAUI / Blazor。
