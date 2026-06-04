# 計畫：ERP 權限機制（線 A — 權限定義/設定層）

**狀態：🚧 進行中（2026-06-04）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 動作軸 + PermissionModels 單檔（registry + 具名 scope 設定）+ DefineType + 驗證 | ✅ 已完成（2026-06-04） |
| 2 | FormSchema 擴充：`PermissionModelId`（progId→主 model）+ `FormField.ScopeRole`（標 Owner/Dept 欄）+ 驗證 | 📝 待做 |

## 本計畫範圍（重要）

本計畫**只做權限的「定義 / 設定層」**——產出**可宣告、可儲存、可驗證**的權限資料模型,**零 enforcement**。

- ✅ **做**：動作軸、Model Registry（含 record scope 具名設定）、FormSchema 的**主 model 引用** + **欄位 scope 角色標記**。
- ❌ **不做（零 enforcement）**：不編譯 FilterNode、不接查詢、不做 `(model, action)` 判定、**不過濾資料**。
- ❌ **不做（移後續）**：element 細粒度綁定（按鈕→action,屬前端 capability）、view 分組。
- ❌ **不做（無組織）**：框架尚無組織結構,組織類策略（Dept/DeptAndSub/Nodes）**只先立設定框架**,待組織就緒才接得上。

> 一句話：本計畫做完,系統「能設定權限、每個功能知道主 model、每個欄位知道自己的 scope 角色」,但還「不會依權限過濾或擋請求」。

## 三個產出

### 產出一:PermissionModels 單檔（權限目標總清單）
```
Permission/PermissionModels.xml      # 根節點 <PermissionModels>,含多個 <PermissionModel>
```
- 所有 model 的 registry + 各 action 預設 scope,集中一份。
- 角色授權（線 B）時一次載入整份 = 可授權目標總清單。
- `DefineType.PermissionModels`（全域單例,如 `SystemSettings`,無 key）。
- **`ModelId` 命名 = 業務實體 PascalCase**（`PurchaseOrder` / `Vendor` / `Requisition`）,刻意與表單 progId（`PO001`）區別,避免「綁表單」觀感。
- **一個 model 可被多個 progId 消費**：`PO001`(建立)/`PO002`(查詢)/`PO009`(報表) 的 FormSchema 都 `PermissionModelId="PurchaseOrder"` → 授一次「採購單 Read」三功能一起生效(這正是「綁 model 不綁 form」的價值)。
- **model 不綁資料表 / 不綁欄**（對齊 Odoo：`ir.model.access` / `ir.rule` 只綁 model,表/欄交給底層）。scope 策略只認**語意角色**（`Own`→擁有者欄、`Dept`/`DeptAndSub`→部門欄）,具體欄位由 `FormField.ScopeRole` 標記提供（見產出三）,執行時套到 FormSchema 主表——欄名留在 FormSchema,不在 model 重複綁。

### 產出二:FormSchema 加主 model 引用
```xml
<FormSchema ProgId="PO001" PermissionModelId="PurchaseOrder" ... >
```
- FormSchema 增一個 `PermissionModelId` 欄位,指向 PermissionModels 裡的主 model。
- **為何放這裡**：BO 的 `GetList`/`Save`/`Delete` 已載入 FormSchema,手邊即取得主 model → 後端方法層 enforce 最直接,不必另載綁定檔。
- **不違反「權限不綁 form」**：FormSchema 只持有**指向 model 的 `ModelId`**（功能宣告它需要哪個 model,線 A 本意）;權限目標定義仍在 PermissionModels。
- **不否定 model≠progId**：這是**主 model**(對應主表,單一);附屬 model 仍由元素引用(屬後續 element 細粒度)。

### 產出三:FormField 加 scope 角色標記
```xml
<FormField FieldName="buyer_rowid" Caption="採購員" ... ScopeRole="Owner" />
<FormField FieldName="dept_rowid"  Caption="部門"   ... ScopeRole="Dept" />
```
- `FormField` 增 `ScopeRole` 屬性（`enum ScopeRole { None, Owner, Dept }`）,標記「哪個欄是擁有者 / 部門」。
- **為何放這裡**：欄位的權限語意,最原始事實就在 FormSchema 欄位定義,單一真值源、最直覺。
- **scope 策略保持純語意**：`Own`→找 `ScopeRole="Owner"` 的欄;`Dept`/`DeptAndSub`→找 `ScopeRole="Dept"` 的欄。每張表自訂哪個欄,不靠標準欄位硬假設（`salesperson_id` 也能當 Owner、`sys_insert_user_rowid` 只是常見預設）。
- **取代「OwnerField 參數化」**：欄名留在 FormSchema,`PermissionModel` 真正跟表/欄脫鉤。

## 後端方法層 enforcement 的銜接（屬後續計畫,此處說明設計意圖）

BO 方法層只需「主 model + 方法映射」即可 enforce,**不需要 element 細粒度**（設計文件 §6.3：後端在方法層 enforce,元素降級是前端的事）：

```
FormSchema.PermissionModelId  +  方法映射常數:
  GetList/GetData → Read      Save → 逐列 Create/Update(依 RowState)      Delete → Delete
```
BO 基類統一用上式驗 `(主model, action)`,不必逐 progId 寫。實際判定/過濾在 **plan-permission-line-b** 與 **plan-record-scope-enforcement**。

## 後續另起計畫

| 後續計畫 | 範圍 | 依賴 |
|---|---|---|
| **plan-record-scope-enforcement** | ScopeResolver（具名策略→FilterNode）、組織階層、多角色 OR 合併 + All 短路、注入查詢過濾 | 組織結構先就緒 + 本計畫 |
| **plan-permission-line-b** | role/grant/user、登入載入 grants、`Can` 判定、API gate、Save 逐列判定、model 層 vs grant 層 scope 覆寫規則 | 本計畫 registry |
| **plan-permission-capability**（前端 capability） | element 細粒度綁定（按鈕→action）、`CapabilityResolver`、逐元素降級 | 本計畫 + line-b |

## record scope 的設定方式：具名策略（核心決策）

- **設定方式 = 具名策略**（業務語意選單）,不手寫 predicate。最易理解、可載入期驗證。
- **策略分層（本 plan 只做無參數策略）**：
  ```
  擁有維度  Own(ScopeRole=Owner 的欄 = 我)
  組織維度  Dept / DeptAndSub(ScopeRole=Dept 的欄)   ← 待組織結構就緒
  全域      All(無 record 限制)
  ```
  欄位由 `FormField.ScopeRole` 標記提供（產出三）,策略只認語意角色、不綁欄名。
  參數化策略（OwnerField / Nodes / CustomRef + `<Parameter>`）與逃生口 predicate,後續需要時**純附加**加回,不破壞既有 XML。
- **定調**：組織/擁有者是主軸,特別條件過濾算特例（主軸用具名策略、特例用 predicate 逃生口、高頻特例畢業升格）。
- **egress（Print/Export）不設 scope**：繼承同 model 的 Read。
- **合併規則（屬後續,設定結構要能承載）**：多角色 OR 聯集、任一 All 短路;`Own` 不焊進組織策略,用獨立策略 OR 疊加。

## 階段拆解

### Phase 1 — 動作軸 + PermissionModels 單檔
- `PermissionAction`（`[Flags] enum`：None/Create/Read/Update/Delete/Print/Export）。
- `PermissionModel`（ModelId[業務實體 PascalCase] + DisplayName + `Rules`[`PermissionRule` 集合]）;每個 `PermissionRule`（`Action` + `Scope`,皆屬性）;supported = `Rules` 集合 OR 成 mask（無獨立 `Supported` 屬性）;**不綁資料表/欄**（scope 只認語意角色,欄位由 `FormField.ScopeRole` 提供）。多個 `PermissionModel` 集中於 `PermissionModels` 容器(單檔)。
- `ScopeStrategy` 種類（本 plan：Own / Dept / DeptAndSub / All,皆無參數）。參數化策略（OwnerField / Nodes / CustomRef）後續純附加。
- 新 `DefineType.PermissionModels`（全域單例）+ `IDefineAccess` 擴充 + 單檔載體 + 載入驗證（action 合法、策略合法、egress 不設 scope）。
- 驗收：round-trip 序列化;非法 action / 非法策略 / 越權 egress scope 載入期被擋。

### Phase 2 — FormSchema 擴充（PermissionModelId + FormField.ScopeRole）
- `FormSchema` 增 `PermissionModelId` 屬性（指向 PermissionModels 的主 model）。
- `FormField` 增 `ScopeRole` 屬性（`enum ScopeRole { None, Owner, Dept }`）,標記擁有者/部門欄。
- 載入期驗證：`PermissionModelId` 指向的 model 存在於 `PermissionModels`;同一主表 `ScopeRole=Owner` / `Dept` 各至多一欄。
- 驗收：FormSchema round-trip 保留新欄位;指向不存在 model / 重複角色標記在載入期被擋。
- 注意：本階段只立「標記 + 驗證」,scope 策略用此標記 enforce 屬後續。

## 風險與取捨

- **組織結構缺位**：組織類策略只是設定框架,無法端到端驗證。後續 enforcement 前需先建組織結構。
- **設定結構要前瞻承載合併/覆寫**：本計畫不做合併,但 scope 結構要能表達多策略、All、與「model 層 vs grant 層覆寫」,避免後續回頭改 schema。
- **FormSchema 是共享 immutable cache**：加 `PermissionModelId` / `FormField.ScopeRole` 是定義欄位(序列化一部分),不影響 immutability;但需確認既有 FormSchema 序列化/Clone 路徑涵蓋新欄位。
- **ApiAccessControlAttribute 不衝突**：它只管加密等級 + 是否登入,與權限正交。

## XML 序列化慣例（對齊框架）

- **element 名 = C# 型別名**：`<PermissionModel>` / `<PermissionRule>`（對齊 `<FormField>` / `<DbField>`）。
- **enum 用成員名當屬性值**：`Action="Read"` / `Scope="Own"`（對齊 `DbType="Guid"`）。
- `PermissionAction` 內部仍是 `[Flags] enum`（運算用）,但 XML 每個 action 一個 `<PermissionRule>`,supported 由集合 OR 出。

## 測試 fixture（明確標的）

- **`PermissionModels.xml`**：PurchaseOrder / Vendor / Requisition（scope 用 Own / Dept / DeptAndSub / All,如本 plan 範例）。
- **FormSchema**：對應的最小 FormSchema,帶 `PermissionModelId`,並在欄位標 `ScopeRole`（Owner / Dept）。
- **放置**：test-local 隔離 fixture（測試專案內,**不寫入共享 `tests/Define`**）,避免污染既有共享資料、破壞既有測試。
- **用途**：Phase 1/2 的 round-trip 序列化與載入期驗證的測試標的。
