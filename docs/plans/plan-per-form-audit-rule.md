# 計畫：per-form 稽核規則（異動 / 檢視改為逐表單可設定）

**狀態：🚧 進行中（2026-08-26）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 型別、`st_audit_rule` 表、per-company 快取、BO 消費端（讀取側全通） | ✅ 已完成（2026-08-26） |
| 2 | 框架內建維護表單（Defaults 定義檔）＋ cache-notify 失效鏈 | ✅ 已完成（2026-08-26） |
| 3 | 文件（ADR 補記、保留命名、CHANGELOG）與 ADR-027 待辦結案 | 📝 待做 |

## 背景

### 現況：全域一刀切

稽核的「異動」與「檢視」兩軸目前只有部署層的全域開關，**開了就對所有表單生效**：

- [`FormBusinessObject.ChangeAuditEnabled()`](../../src/Bee.Business/Form/FormBusinessObject.Audit.cs) —— 只看
  `AuditLogOptions is { Enabled: true, ChangeEnabled: true }`
- [`FormBusinessObject.AccessAuditEnabled()`](../../src/Bee.Business/Form/FormBusinessObject.Audit.cs) —— 只看
  `AuditLogOptions is { Enabled: true, AccessEnabled: true }`
- `WriteChangeAudit` 的 `IsSensitive` **硬寫 `false`**，敏感度無從表達
  （`SystemBusinessObject.WriteDeploymentAudit` 則硬寫 `true`）

後果：一個部署想要「只對重要資料留痕」時沒有中間檔位——只能全開（量體與雜訊）或全關（沒有軌跡）。

### 這是既有待辦，不是新需求

[ADR-027](../adr/adr-027-audit-trail.md) 的〈待辦〉第一條就寫著：

> per-form 稽核規則（Odoo `auditlog.rule` 式的 admin 執行期選單，涵蓋異動 + 檢視；目前全記所有表單）

[ADR-040](../adr/adr-040-audit-trail-taxonomy.md) 決策四對檢視記錄訂了三條，其中兩條**尚未實作**：

| 決策四要求 | 現況 |
|-----------|------|
| 預設關閉、opt-in | ✅ `AccessEnabled` 預設 `false` |
| 敏感度驅動 | ❌ 未實作 |
| 限定 ProgId／動作 | ❌ 未實作（開了就每張表單的 `GetData` 都記） |

本計畫實作的就是這兩條，並把異動軸一併補上同樣的顆粒度。

## 藍本查證（2026-08-26）

決定作法前先查證 SAP 與 Odoo 是否「全記所有表單」。**兩者皆否**，且都是兩層結構。

### SAP：三種機制，沒有一種預設全記

| 機制 | 記什麼 | 誰決定、在哪決定 |
|------|--------|----------------|
| **Change Documents**（`CDHDR`/`CDPOS`） | 業務物件欄位級變更 | **開發期三層 opt-in**：欄位的 data element 要勾「Change document」（Further Characteristics）→ `SCDO` 建 Change Document Object 列出要記的表 → 應用程式呼叫產生的 `*_WRITE_DOCUMENT` FM |
| **Table Logging**（`DBTABLOG`） | 表級異動 | **兩層 AND**：表層 `SE13`「Log Data Changes」勾選 × 系統層 profile parameter `rec/client`（`OFF` / 指定 client / `ALL`）。SAP 明示這是給 **customizing 表的手動變更**用，「不適合大量異動——那要用 change documents」 |
| **Read Access Logging** | 檢視 | **純執行期設定**（`SRALMANAGER`），客戶自訂 log purpose / channel / 要記哪些欄位 |

### Odoo：core 走定義期，OCA `auditlog` 走執行期

- **core chatter tracking** —— `tracking=True` 寫在模型的欄位定義上（開發期）。
- **OCA `auditlog`** —— `auditlog.rule` 是**一張資料表**，客戶在
  Settings → Technical → Audit → Rules 自行維護：

| 欄位 | 預設 | 語意 |
|------|------|------|
| `model_id` | — | **每個 model 各要一筆規則；沒訂就完全不記** |
| `log_create` / `log_write` / `log_unlink` | `True` | 建立／修改／刪除 |
| `log_read` | **`False`** | 檢視，明確預設關 |
| `log_type` | `full` | `full` 比對前後值（含 computed 欄）／ `fast` 只記傳入值 |
| `state` | `draft` | **要按「Subscribe」才真的生效**（patch ORM 方法） |
| `user_ids` / `users_to_exclude_ids` | — | 只記／排除特定使用者 |
| `fields_to_exclude_ids` | — | 排除特定欄位 |

### Odoo 的 model 單位是機制的結果，不是設計選擇

OCA `auditlog` 的運作方式是**執行期 monkey-patch ORM 方法**：`_patch_methods` 在
subscribe 時對 `self.env.registry[model._name]` 掛上 `create` / `read` / `write` /
`unlink` / `export_data` 五個包裝，`_revert_methods` 要刪 patch 再 reload registry，
server 重啟後靠 `_register_hook()` 重掛。`unique(model_id)` 是 SQL 層約束——一個 model 一筆規則。

**patch 的對象是 model class，顆粒度就只能是 model。** 這不是挑出來的設計，是機制逼的，
而且有代價：Odoo README 自陳「read logging 不是所有 model 都有效，need investigation」，
繞過 ORM 的路徑（raw SQL、部分 batch）同樣記不到。

**本框架的埋點是原生的**——`FormBusinessObject.Save` / `Delete` / `GetData` 是
FormSchema 驅動的 CRUD 必經之路，不需 patch、不需重啟重掛、不會有「某些物件記不到」。
**這是結構優勢，不為了對齊 Odoo 而放棄。**

### 三個對本設計有決定性影響的結論

1. **沒有一套是全記所有表單。** 框架現行行為在兩套藍本裡都找不到對應。
2. **異動與檢視一律分開設定，且檢視預設關**（SAP RAL opt-in、Odoo `log_read=False`）。
3. **都是兩層結構**：總閘 × per-object 宣告。SAP table logging 的
   `rec/client` × `SE13` 旗標就是這個形狀。

「規則由客戶自行決定、不寫在定義中」這條線對應的是 **Odoo `auditlog.rule`** 與
**SAP RAL**，不是 SAP Change Documents。因此 FormSchema 屬性的作法排除。

## 決策

| # | 決策 | 理由 |
|---|------|------|
| D1 | 規則存**資料庫表** `st_audit_rule`，不放定義檔 | 稽核政策是客戶的營運決定，不是隨應用交付的定義。對齊 Odoo `auditlog.rule` |
| D2 | 表放 **company scope**（per-tenant） | 各公司自訂要記哪些表單。既有 `st_role` / `st_department` 已是「框架所有但位於公司資料庫」的先例 |
| D3 | 三態 `Inherit` / `On` / `Off`，預設 `Inherit` | 沒有規則列 = 全部 Inherit = 沿用全域開關 = **現行行為完全不變**。零破壞性 |
| D4 | 顆粒度為**表單層兩軸 + 敏感旗標** | 異動、檢視各自可設；`IsSensitive` 填掉目前硬寫的 `false`。欄位層敏感度留待後續 |
| D5 | 維護介面走**框架內建標準表單**（`Defaults/` 內嵌定義檔） | `Bee.Definition/Defaults/` 已內嵌 `Department` / `Employee` 兩張框架自帶表單，先例現成；零新 API surface |
| D6 | **`Enabled` 是唯一硬性總閘**；`ChangeEnabled` / `AccessEnabled` 只是 `Inherit` 繼承的預設值 | 見下方說明 |

#### D6 補述：軸開關不是第二道閘（2026-08-26 修正）

本條原先寫成「`ChangeEnabled` / `AccessEnabled` 關掉時規則不再被查詢」，**那是錯的**：
`AccessEnabled` 預設就是 `false`，照該寫法「只記某一張重要表單的檢視」會完全失效——
而那正是本功能的主要用途。

正確語意：

| 開關 | 角色 |
|------|------|
| `AuditLogOptions.Enabled` | **唯一硬性總閘**。關閉時直接短路，連規則快取都不查（等同 SAP `rec/client=OFF`） |
| `ChangeEnabled` / `AccessEnabled` | 該軸的**預設值**，供 `Inherit` 繼承。**不是閘** |

於是：

```
Enabled = false                          → 不記（短路，零成本）
Enabled = true, 規則 = On                → 記（即使該軸預設為 false）
Enabled = true, 規則 = Off               → 不記（即使該軸預設為 true）
Enabled = true, 規則 = Inherit / 無規則列 → 依 ChangeEnabled / AccessEnabled
```

代價：`Enabled = true` 時每次 Save / GetData 都要查一次規則快取（記憶體字典查表）。
`Enabled` 預設為 `false`，所以未使用稽核的部署仍是零成本。

### 為何單位是 ProgId 而不是資料表

**SAP Change Documents 也不是逐表**——一個 Change Document Object 涵蓋 header + item
多張表，是**業務物件**單位。Odoo 的 model 才是逐表的：一張採購單要 `purchase.order` 與
`purchase.order.line` 各訂一筆規則。

本框架的 ProgId = 一個 FormSchema = master + detail 的聚合，**同時對齊 SAP Change
Document Object 與業務單據的實際形狀**。埋點在 `FormBusinessObject`，單位天然就是 ProgId。

> **代價**：per-ProgId 無法「只記主檔、不記明細」——DiffGram 一次把 master + detail
> 存成一列（[ADR-027](../adr/adr-027-audit-trail.md) D5）。有此需求要等表／欄層顆粒度。

### 明確不抄 Odoo 的部分

| 不抄 | 為何 |
|------|------|
| `state: draft / subscribed` 兩段式狀態 | 那個狀態存在是**因為 subscribe 要去做 patching**。我們沒有 patch 要掛，`draft` 沒有任何機械意義——三態 `Inherit`/`On`/`Off` 已完整表達「不生效」。**這是最容易被 cargo-cult 進來的東西** |
| monkey-patch 機制本身 | 見上節，本框架埋點原生 |
| `log_type: full / fast` | 我們的 DiffGram 一律含 before/after，沒有「只記傳入值」的省事檔位可省 |

### 明確不納入本次

- **欄位層敏感度**（ADR-040 決策四的完整形態）—— 要動 DiffGram 過濾邏輯，範圍另計。
- **動作層開關**（`GetData` / `Save` / `Delete` 分別）—— 檢視目前只在 `GetData` 埋一個點，動作層現階段無實際差別。
- **使用者過濾**（Odoo `user_ids` / `users_to_exclude_ids`）—— 尚無需求。
- **`SystemBusinessObject` 的部署層稽核** —— 依 [`DeploymentAuditEnabled()`](../../src/Bee.Business/System/SystemBusinessObject.Audit.cs)
  的既有註解，那條刻意只受 `Enabled` 管、且刻意不可個別關閉，本計畫不動它。

## 資料模型

### `st_audit_rule`（company scope）

檔案：`src/Bee.Definition/Defaults/TableSchema/company/st_audit_rule.TableSchema.xml`
（`.csproj` 的 `Defaults\**\*.xml` glob 已涵蓋，不必改建置檔）

```xml
<?xml version="1.0" encoding="utf-8"?>
<TableSchema xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
             xmlns:xsd="http://www.w3.org/2001/XMLSchema"
             TableName="st_audit_rule" DisplayName="稽核規則">
  <Fields>
    <DbField FieldName="sys_no"          Caption="流水號"   DbType="AutoIncrement" />
    <DbField FieldName="sys_rowid"       Caption="唯一識別" DbType="Guid" />
    <DbField FieldName="sys_id"          Caption="表單代碼" DbType="String" Length="50" />
    <DbField FieldName="sys_name"        Caption="表單名稱" DbType="String" Length="100" />
    <DbField FieldName="change_mode"     Caption="異動記錄" DbType="Integer" DefaultValue="0" />
    <DbField FieldName="access_mode"     Caption="檢視記錄" DbType="Integer" DefaultValue="0" />
    <DbField FieldName="is_sensitive"    Caption="敏感資料" DbType="Boolean" DefaultValue="0" />
    <DbField FieldName="remark"          Caption="備註"     DbType="String" Length="255" AllowNull="true" />
    <DbField FieldName="sys_insert_time" Caption="寫入時間" DbType="DateTime" />
  </Fields>
  <Indexes>
    <DbTableIndex Name="pk_{0}" Unique="true" PrimaryKey="true">
      <IndexFields><IndexField FieldName="sys_no" /></IndexFields>
    </DbTableIndex>
    <DbTableIndex Name="rx_{0}" Unique="true">
      <IndexFields><IndexField FieldName="sys_rowid" /></IndexFields>
    </DbTableIndex>
    <DbTableIndex Name="uk_{0}" Unique="true">
      <IndexFields><IndexField FieldName="sys_id" /></IndexFields>
    </DbTableIndex>
  </Indexes>
</TableSchema>
```

| 欄位 | 理由 |
|------|------|
| `sys_no` / `sys_rowid` / `sys_id` / `sys_name` | 框架標準四件組，比照 `st_role` / `st_api_key` |
| **`sys_id` = ProgId**，`Length=50` | 對齊 `st_log_change.prog_id` 的長度。`uk_{0}` 唯一索引即 Odoo `unique(model_id)` 的等價物——**一張表單一筆規則** |
| `change_mode` / `access_mode` | `AuditRuleMode` 以 `(int)` 持久化，比照 [`ChangeAuditEntry`](../../src/Bee.Definition/Logging/ChangeAuditEntry.cs) 的 `change_kind`。`DefaultValue="0"` = `Inherit` |
| `is_sensitive` | 型別與預設值直接對齊 `st_log_change.is_sensitive` |
| `remark` | **不帶 `sys_` 前綴**——`st_api_key` 的 `contact` / `enabled` / `key_type` 都是不帶前綴的領域欄，`sys_` 只留給那五個框架欄。內容是「**為何要記這張表單**」，合規舉證時比規則本身有用 |
| `sys_insert_time` | 比照 `st_role` / `st_api_key` |

#### 三個刻意的「不放」

- **不放 `sys_update_time`** —— 全 `Defaults/TableSchema/` 只有 `st_define` 與 `st_cache_notify`
  有這欄，**不是框架通用慣例**。規則表的變更歷程由 `st_log_change` 承擔（階段 2 會把這張表單
  預設設為 `change_mode = On`），自帶一個 update_time 等於開第二個來源。
- **不放 `enabled` 旗標** —— 三態已涵蓋「不生效」。與前述「不抄 Odoo `state`」是同一個判斷：
  別為同一件事開兩個開關。
- **不放額外索引** —— 整張表按公司整批載入快取，沒有需要索引的查詢樣態。
  `uk_{0}` 是完整性約束，不是查詢用。

#### `remark` 的 `AllowNull="true"` 是刻意的例外

這違反 [`rules/database.md`](../../.claude/rules/database.md) 的預設（文字欄一律 NOT NULL），
但屬該規則**第 4 點明列的例外**：Oracle 的 `''` == `NULL`，
`VARCHAR2(n) DEFAULT '' NOT NULL` 自相矛盾，fresh CREATE 下省略該欄的 INSERT 會 `ORA-01400`。
`remark` 正是典型的「常態值為空」欄，而 `Oracle` 在 `DatabaseType` 中存在。

> **實作時必須在 XML 內加註解說明這是 Oracle dialect 修正前的過渡**，
> 否則下一個讀到的人會判定成反射性加的 nullable 而「順手修正」。

其餘八欄全部 `AllowNull=false`（隱含預設），符合規範。

### 型別（`Bee.Definition/Logging/`）

```
AuditRuleMode        enum { Inherit = 0, On = 1, Off = 2 }
AuditRule            單筆規則（ProgId / ChangeMode / AccessMode / IsSensitive）
CompanyAuditRules    一家公司的規則快照
                     內含 Dictionary<string, AuditRule>(StringComparer.Ordinal)
```

`Ordinal` 比照 [`CompanyRolePermissions`](../../src/Bee.Definition/Identity/CompanyRolePermissions.cs)
的三個內部字典，符合 [`code-style.md`](../../.claude/rules/code-style.md)「識別碼型字串一律 Ordinal」。

`CompanyAuditRules` 是**快取共用實例**，依 [`rules/definition.md`](../../.claude/rules/definition.md)
的不可異動規則，建立後不得於 runtime mutate。

#### 快取單位是 per-company，不是 per-ProgId

```
CompanyAuditRulesCache : KeyObjectCache<CompanyAuditRules>
    快取鍵     = companyId          ← 一個 entry = 該公司的整張規則表快照
    notify key = CompanyAuditRules:{companyId}
```

**ProgId 是快照內的字典鍵，不是快取鍵。** 決定性理由是「查無規則」才是常態——
三態預設 `Inherit`，絕大多數表單根本不會有規則列。若以 ProgId（或 `company+progid` 複合鍵）
為快取鍵：

- 每張沒設規則的表單首次讀取都是 cache miss → **一次 DB 查詢** → 存一筆負向項
- N 張表單 = N 次 round trip，查回來的答案全是「沒有」
- 每次讀取都要走一次 cache provider

per-company 則一次載入整張表（列數上限 = 部署的表單數），之後「這張表單沒規則」是
**記憶體裡一次字典 miss**，零 DB、零 provider round-trip。這正是
`CompanyRolePermissions` 選整份快照而非逐權限項的同一個理由。

| | per-company | per-ProgId |
|---|---|---|
| 先例 | ✅ `CompanyRolePermissionsCache` / `DepartmentTreeCache` 同形狀 | 無 |
| 失效 | 一個 notify key，任何規則變更 Touch 一次 | N 個 key；刪除規則列還要處理負向項失效 |
| 量體 | 整份快照極小，無拆分理由 | — |

> per-ProgId 唯一的好處是失效更細（改一筆不必丟整份），但**規則變更頻率極低**，
> 那是在優化錯的東西。

### 解析語意

```
記錄與否 = 全域總閘 AND per-form 決議

per-form 決議：
  查不到規則列   → Inherit
  Inherit        → 沿用全域（ChangeEnabled / AccessEnabled）
  On             → 記（但總閘關閉時仍不記）
  Off            → 不記
```

`IsSensitive` 僅在「決定要記」之後才有意義，直接取規則列的值（無規則列時為 `false`，維持現行行為）。

## 階段 1：型別、表、快取、BO 消費端

讀取側全部到位；此階段規則列以 seeder 或手動 INSERT 產生，維護 UI 留給階段 2。

1. **定義層型別** —— `AuditRuleMode` / `AuditRule` / `CompanyAuditRules`，一型別一檔，
   置於 `src/Bee.Definition/Logging/`。
2. **TableSchema** —— 見上節〈資料模型〉的完整 XML。
3. **`DbCategorySettings.xml` 註冊** —— 於 `src/Bee.Definition/Defaults/DbCategorySettings.xml`
   的 `company` 分類補一筆 `<TableItem TableName="st_audit_rule" DisplayName="Audit rule" />`。
   **漏這步表不會被建出來**，且症狀是執行期才浮現。
4. **快取** —— `CompanyAuditRulesCache : KeyObjectCache<CompanyAuditRules>`，
   key = companyId，notify key 依 `KeyObjectCache.CacheGroup` 預設即 `CompanyAuditRules:{companyId}`。
   置於 `src/Bee.ObjectCaching/Database/`，與 `CompanyRolePermissionsCache` 同構。
5. **資料來源** —— `ICacheDataSourceProvider.GetCompanyAuditRules(string companyId)`
   ＋ `CacheDataSourceProvider` 實作（解析公司資料庫、讀 `st_audit_rule`）。
   相依環的延遲解析比照既有：**`dataSource` 必須維持 `Func<>`**。
6. **容器兩處同步** —— `ICacheContainer` 加屬性、`CacheContainerService` 加建構與屬性。
   **漏補必 CS0535**，不會靜默通過。
7. **服務外觀** —— `IAuditRuleService` / `AuditRuleService`（`Bee.ObjectCaching/Services/`），
   比照 `RolePermissionService`：`Get(companyId)` ＋ `Remove(companyId)`。
8. **BO 消費端** —— 改寫 `FormBusinessObject.Audit.cs`：
   - `ChangeAuditEnabled()` / `AccessAuditEnabled()` 改為「總閘 AND per-form 決議」
   - 新增 `ResolveAuditRule()`，以 `ResolveAuditIdentity()` 已解析的 `CompanyId` 查快取
   - `WriteChangeAudit` 的 `IsSensitive` 改讀規則
   - **總閘關閉時直接短路**，不查快取——保持「未啟用稽核 = 零額外成本」

> ⚠️ `FormBusinessObject.Write.cs` 的 `ChangeAuditEnabled()` 呼叫**已在正確位置**
> （`Save` 前擷取 DiffGram、`Delete` 前決定是否載入 snapshot），本階段不移動這些呼叫點。
> 但要注意 [`FormBusinessObject.Write.cs`](../../src/Bee.Business/Form/FormBusinessObject.Write.cs)
> 的 delete snapshot 是 `auditChange || pluginNeedsSnapshot || HasBeforeDeleteRules(schema)` 的
> 聯集——**per-form 關掉異動記錄不得讓 plugin 看不到 snapshot**，該處註解已寫明此約束，勿破壞。

## 階段 2：維護表單與失效鏈

1. **內建表單定義檔** —— 於 `src/Bee.Definition/Defaults/` 補
   `FormSchema/AuditRule.FormSchema.xml`、`FormLayout/AuditRule.FormLayout.xml`、
   `Language/{zh-TW,en-US}/AuditRule.Language.xml`。`CategoryId` = `company`。
   可用 `bee-scaffold-from-formschema` skill 由 FormSchema 反推其餘三類。
2. **保留 ProgId** —— `SysProgIds` 新增常數（暫定 `AuditRule`），並登記進
   [框架保留命名](../framework-reserved-names.zh-TW.md) §2。
3. **權限** —— 表單的 `PermissionModelId` 指向專屬模型；稽核政策的維護權限
   **不應與一般業務表單同級**。
4. **cache-notify 失效** —— 自訂 `AuditRuleBusinessObject : FormBusinessObject`，
   於 `DoAfterSave` / `DoAfterDelete`：
   - 本機立即 `IAuditRuleService.Remove(companyId)`
   - 跨節點 bump common 的 `st_cache_notify` 列 `CompanyAuditRules:{companyId}`

> **這裡有一個必須明講的取捨。** [`ICacheNotifyService.Touch`](../../src/Bee.Db/CacheNotify/ICacheNotifyService.cs)
> 的 XML doc 要求 bump 與資料變更**同一個 `DbTransaction`**，而 `IDataFormRepository.Save`
> 把交易完全收在自己內部、`DoAfterSave` 拿不到 handle——所以這裡的 bump 落在 commit **之後**、
> 獨立交易。
>
> 該文件警告的危險方向是「bump 先於資料變更可見」，本作法方向相反（資料已 commit 才 bump），
> 不會產生「重載到舊值卻標記為新鮮」的永久不一致。殘留風險只有「commit 成功但 bump 失敗」
> → 該公司的規則快取續用舊值直到下次變更。**這是刻意接受的**：規則維護頻率極低，
> 而為一張設定表另開專屬 repository ＋ 一整組 BO 方法（跨 contract / wire / client 約 7~8 檔）
> 代價不成比例。
>
> 跨庫本身**不是**新問題：資料在公司資料庫、notify 列在 common，這正是
> [`RolePermissionService`](../../src/Bee.ObjectCaching/Services/RolePermissionService.cs)
> 已經寫進契約的既有模式（`CacheNotifyOptions.DatabaseId` 預設 `common`，poller 只看這一個庫）。

5. **政策變更本身要留痕** —— `st_audit_rule` 走標準 `Save` 路徑，其異動**會被異動記錄記到**。
   應把這張表單自身的規則預設為 `change_mode = On` + `is_sensitive = true`
   （由 seeder 植入），對齊 SAP / Odoo「稽核組態的變更屬於安全事件」的處理。

## 階段 3：文件

1. **ADR-027** —— 〈待辦〉第一條結案，改為指向新的決策記錄。
2. **ADR-040** —— 決策四的「敏感度驅動」「限定入口」補記實作結果與**本次未做到的部分**
   （欄位層敏感度）。若判定夠格，另立 ADR 記錄 per-form 規則的三態繼承語意。
3. **[框架保留命名](../framework-reserved-names.zh-TW.md)** —— §1.2 公司資料庫加入
   `st_audit_rule`、§2 加入保留 ProgId。**雙語兩份都要改。**
4. **CHANGELOG** —— `docs/changelogs/<version>.md`，並註明相容性（無規則列 = 行為不變）。
5. **`docs/development-constraints.md`** —— 若新增的 `CompanyAuditRules` 需列入
   「init 後不可異動」清單，一併補上。

## 相容性

| 情境 | 升級後行為 |
|------|-----------|
| 未啟用稽核（`Enabled = false`） | 完全不變，且不查規則快取（短路） |
| 已啟用、`st_audit_rule` 為空 | 全部 Inherit → **與現行完全相同** |
| 已啟用、部分表單標 `Off` | 只有那些表單不再記錄 |
| 舊部署未建 `st_audit_rule` 表 | 需與其他 `st_*` 表同樣走既有 schema 升級路徑；**讀不到表時必須視同「無規則」而非拋錯** |

最後一列是本計畫**唯一的真實回歸風險**，必須有測試覆蓋。

## 測試

依 [`rules/testing.md`](../../.claude/rules/testing.md)，碰 DB 一律 `SharedDbFixture` + `[DbFact]`：

階段 1 已完成（✅ 標示者）：

| 測試 | 位置 | 狀態 |
|------|------|------|
| 三態解析與 `Find` 查表（純邏輯） | `tests/Bee.Definition.UnitTests/Logging/AuditRuleTests.cs` | ✅ 9 項 |
| 快取讀穿、`Remove` 後重載、公司隔離 | `tests/Bee.ObjectCaching.UnitTests/Services/AuditRuleServiceTests.cs` | ✅ 5 項 |
| **表不存在時視同無規則**（回歸風險那一列） | `tests/Bee.Repository.UnitTests/AuditRuleRepositoryTests.cs` | ✅ 五個 provider 各一 + 空 databaseId |
| 規則 `Off` 擋下部署預設開啟的異動記錄 | `tests/Bee.Business.UnitTests/Form/FormBusinessObjectAuditRuleTests.cs` | ✅ |
| 規則 `On` 壓過部署預設關閉 ＋ `is_sensitive` 寫入 | 同上 | ✅ |
| 規則 `On` 壓過檢視軸的預設關閉 | 同上 | ✅ |
| delete snapshot 在規則關掉異動記錄時仍載入 | 同上 | ✅ |

> **表不存在那組刻意跑滿五個 provider**：判斷表存在與否走各家自己的
> `TableSchemaProvider`，一種過不代表其餘四種過。
>
> ⚠️ 該組測試依賴「`tests/Define/DbCategorySettings.xml` 未登記 `st_audit_rule`」這個前提。
> **若日後有人把它加進測試定義，那組會失去意義而不是失敗** —— 屆時要改成顯式 DROP 後再驗。
> 這點已寫在該測試類別的 XML doc 裡。

## 開放問題

1. **保留 ProgId 命名** —— `AuditRule` 或 `AuditRuleSettings`？現有 `AuditLog` 是讀取側 BO，
   兩者並存時要能一眼分辨。
2. **seeder 是否預植規則列** —— 全空（純 Inherit，最保守）或預植
   `st_audit_rule` 自身那一列（政策變更留痕）？傾向後者。
3. **檢視軸是否也該有 `st_audit_rule` 以外的入口限定** —— ADR-040 決策四說「限定 ProgId／動作」，
   本計畫做到 ProgId，動作維度暫不做，需確認這樣算不算結案。

## 相關

- [ADR-027：資料軌跡 / 稽核日誌](../adr/adr-027-audit-trail.md) —— 本計畫結案其〈待辦〉第一條
- [ADR-040：稽核軌跡的分類軸與寫入策略](../adr/adr-040-audit-trail-taxonomy.md) —— 決策四是本計畫的規格來源
- [ADR-017：DB cache 失效](../adr/adr-017-db-cache-invalidation.md) —— 階段 2 失效鏈的機制
- [框架保留命名](../framework-reserved-names.zh-TW.md) —— 新表與新 ProgId 的登記處
