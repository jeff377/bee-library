# ADR-041：per-form 稽核規則 —— 異動與檢視改為逐表單設定

## 狀態

**已採納（Accepted，2026-08-26）**

結案 [ADR-027](adr-027-audit-trail.md)〈待辦〉的第一條，並補齊
[ADR-040](adr-040-audit-trail-taxonomy.md) 決策四中「敏感度驅動」與「限定入口」兩項尚未實作的要求。
ADR-027 的六軸分類、DiffGram 儲存法與 best-effort 寫入策略均不變。

## 背景

異動與檢視兩軸原本只有部署層的全域開關（`AuditLogOptions.ChangeEnabled` / `AccessEnabled`），
**開了就對所有表單生效**。想要「只對重要資料留痕」時沒有中間檔位：只能全開（量體與雜訊）
或全關（沒有軌跡）。同時 `WriteChangeAudit` 的 `IsSensitive` 硬寫 `false`，敏感度無從表達。

### 藍本查證：兩套成熟 ERP 都不是全記

決定作法前查證了 SAP 與 Odoo。**兩者皆非全記所有物件，且都是兩層結構。**

| 機制 | 記什麼 | 誰決定、在哪決定 |
|------|--------|----------------|
| SAP **Change Documents**（`CDHDR`/`CDPOS`） | 業務物件欄位級變更 | **開發期三層 opt-in**：欄位的 data element 勾「Change document」→ `SCDO` 建 Change Document Object 列出要記的表 → 程式呼叫產生的 `*_WRITE_DOCUMENT` FM |
| SAP **Table Logging**（`DBTABLOG`） | 表級異動 | **兩層 AND**：表層 `SE13`「Log Data Changes」× 系統層 profile parameter `rec/client`。SAP 明示這是給 customizing 表的手動變更用 |
| SAP **Read Access Logging** | 檢視 | **純執行期設定**（`SRALMANAGER`），客戶自訂 log purpose / channel / 欄位 |
| Odoo core **chatter tracking** | 欄位異動 | 開發期，`tracking=True` 寫在模型欄位定義上 |
| Odoo OCA **`auditlog`** | CRUD + read | **執行期資料表** `auditlog.rule`，每個 model 一筆；`log_read` 明確預設 `False` |

三個對本設計有決定性影響的觀察：

1. **沒有一套是全記。** 框架原行為在兩套藍本裡都找不到對應。
2. **異動與檢視一律分開設定，且檢視預設關**（SAP RAL opt-in、Odoo `log_read=False`）。
3. **都是兩層結構**：總閘 × per-object 宣告。SAP 的 `rec/client` × `SE13` 正是這個形狀。

### Odoo 的 model 單位是機制的結果，不是設計選擇

OCA `auditlog` 以**執行期 monkey-patch ORM 方法**運作：subscribe 時對
`self.env.registry[model._name]` 掛上 `create` / `read` / `write` / `unlink` / `export_data`
五個包裝，取消時要 revert 再 reload registry，重啟後靠 `_register_hook()` 重掛。

**patch 的對象是 model class，顆粒度就只能是 model。** 這不是挑出來的設計，而且有代價：
Odoo 自陳 read logging 不是所有 model 都有效，繞過 ORM 的路徑同樣記不到。

**本框架的埋點是原生的** —— `FormBusinessObject` 的 `Save` / `Delete` / `GetData` 是
FormSchema 驅動 CRUD 的必經之路，不需 patch、不需重啟重掛、不會有「某些物件記不到」。
**這是結構優勢，不為了對齊藍本而放棄。**

## 決策

### 一、規則存執行期資料表，不放定義檔

新增 `st_audit_rule`，每張表單一列。**稽核政策是客戶的營運決定，不是隨應用交付的定義**——
定義檔會隨應用升級被覆蓋，而政策不該。對齊 Odoo `auditlog.rule` 與 SAP RAL；
明確排除 SAP Change Documents 那條「寫進開發期定義」的路線。

### 二、company scope（per-tenant）

各公司自訂要記哪些表單。`st_role` / `st_department` 已是「框架所有但位於公司資料庫」的先例。

快取為 **per-company 整份快照**（`CompanyAuditRules`，快取鍵 = companyId），
不是逐 ProgId 快取。理由是**「查無規則」才是常態**：三態預設 `Inherit`，
絕大多數表單不會有規則列；逐 ProgId 會讓每一張都變成 cache miss ＋ 查詢 ＋ 負向項，
而整份快照讓「這張表單沒規則」成為一次記憶體字典 miss。與
`CompanyRolePermissions` 選整份快照而非逐權限項是同一個理由。

跨程序失效沿用既有模式：資料在公司資料庫、notify 列在 common 的 `st_cache_notify`
（poller 只看一個資料庫），與 `CompanyRolePermissions` 寫進契約的作法相同。

### 三、單位是 ProgId，不是資料表

**SAP Change Document Object 也不是逐表** —— 一個 object 涵蓋 header + item 多張表，
是業務物件單位。Odoo 的 model 才是逐表：一張採購單要 `purchase.order` 與
`purchase.order.line` 各訂一筆。

本框架的 ProgId = 一個 FormSchema = master + detail 的聚合，**同時對齊 SAP 的聚合概念
與業務單據的實際形狀**，且埋點在 `FormBusinessObject`，單位天然如此。

> **代價**：per-ProgId 無法「只記主檔、不記明細」—— DiffGram 一次把 master + detail
> 存成一列（ADR-027 D5）。有此需求要等表／欄層顆粒度。

### 四、三態 `Inherit` / `On` / `Off`，預設 `Inherit`

沒有規則列 = 全部 `Inherit` = 沿用全域開關 = **升級後行為完全不變**。零破壞性。

### 五、`Enabled` 是唯一硬性總閘，軸開關不是第二道閘

| 開關 | 角色 |
|------|------|
| `AuditLogOptions.Enabled` | **唯一硬性總閘**。關閉時直接短路，連規則快取都不查（等同 SAP `rec/client=OFF`） |
| `ChangeEnabled` / `AccessEnabled` | 該軸的**預設值**，供 `Inherit` 繼承。**不是閘** |

```
Enabled = false                          → 不記（短路，零成本）
Enabled = true, 規則 = On                → 記（即使該軸預設為 false）
Enabled = true, 規則 = Off               → 不記（即使該軸預設為 true）
Enabled = true, 規則 = Inherit / 無規則列 → 依 ChangeEnabled / AccessEnabled
```

**設計過程中這條曾寫反**，記錄在此以免重蹈：初稿讓軸開關也當閘，
但 `AccessEnabled` 預設就是 `false`，那樣一來「只記某一張重要表單的檢視」會完全失效——
而那正是本功能的主要用途。

代價是 `Enabled = true` 時每次 Save / GetData 多一次記憶體字典查表。
`Enabled` 預設為 `false`，未使用稽核的部署仍是零成本。

### 六、政策表單自身硬性豁免於規則表

`AuditRule` 這張維護表單**兩軸恆為 `On`、恆標敏感**，且該判定不經過規則表。

**這是安全性要求，不是便利設計。** 若政策表單受一般規則管轄，任何能維護規則的人
只要把 `AuditRule` 那一列設成 `Off`，之後所有政策變更都無痕 ——
**稽核可以被稽核政策自己靜靜關掉，且沒有任何紀錄顯示發生過。**

刻意與 `SystemBusinessObject` 的部署層稽核同構：那條也只受 `Enabled` 管、不可個別關閉，
理由相同 —— 開了稽核的部署不能選擇不記錄「誰授予了能力」，而稽核政策正是那種授予。

### 七、維護表單宣告 `PermissionModelId`，是框架預設 form 中唯一的一張

稽核政策是特權操作（SAP 的 `SE13` 要 Basis 權限、Odoo 的 `auditlog.rule` 在
Technical Features 之後）。enforcement 為 **fail-closed**：模型未授權時 `ForbiddenException`。

決策六是**偵測**（證明有人動過政策），本條是**預防**（擋住事情發生）。兩者互補，
不能互相取代 —— 具體情境：有人先把某張表單的 `change_mode` 設成 `Off`、改資料、再改回
`Inherit`，中間那段完全沒有軌跡。

代價：複製定義檔到部署後，要先建模型並授權才能使用，預設是「任何人都開不了」。
`Defaults/` 本來就是 scaffold 來源而非 runtime 載入路徑，這個「先設定才能用」的成本
落在它該落的地方。框架因此隨附一份 `PermissionModels.xml` 範例。

## 明確不納入

| 項目 | 理由 |
|------|------|
| **欄位層敏感度** | ADR-040 決策四的完整形態，要動 DiffGram 過濾邏輯，範圍另計 |
| **動作層開關**（`GetData` / `Save` / `Delete` 分別） | 檢視目前只在 `GetData` 埋一個點，動作層現階段無實際差別 |
| **使用者過濾**（Odoo `user_ids` / `users_to_exclude_ids`） | 尚無需求 |
| **不抄 Odoo 的 `state: draft / subscribed`** | 那個狀態存在是**因為 subscribe 要去做 patching**。本框架沒有 patch 要掛，`draft` 沒有機械意義；三態已完整表達「不生效」。**這是最容易被 cargo-cult 進來的東西** |
| **不抄 Odoo 的 `log_type: full / fast`** | DiffGram 一律含 before/after，沒有「只記傳入值」的省事檔位可省 |

## 理由

**為什麼照抄兩套 ERP 的兩層結構而不自創。** 與 ADR-040 同一個理由：稽核分類的成本不在寫程式，
而在事後發現切錯了。SAP 與 Odoo 在「總閘 × per-object」這一點上獨立收斂到相同結構，
這種一致性本身就是證據。

**為什麼是資料表而不是定義檔。** 兩者的差別不在技術，在**誰擁有這個決定**。
定義檔隨應用交付、隨升級覆蓋，是開發者的東西；稽核政策是客戶對自己營運風險的判斷，
必須在客戶手上、且不會被下一次升級抹掉。

**為什麼保留原生埋點而不模仿 patch。** 本框架的 CRUD 有單一必經之路，這是 ORM 通用框架
沒有的條件。Odoo 需要 patch 是因為它要攔截任意 model 的任意方法；我們不需要，
放棄這個優勢去換「看起來像藍本」毫無收益。

## 後果 / 影響

- **正面**：量體從源頭收斂（不必靠保留期清除）；檢視軸終於可用（預設關 + 逐張開）；
  `is_sensitive` 有了真正的來源；政策變更本身留痕且受權限把關。
- **取捨**：`Enabled = true` 時每次操作多一次字典查表；per-ProgId 無法區分主檔與明細；
  維護表單 fail-closed，開箱後需先授權。
- **相容性**：無規則列即現行行為，升級零破壞。**表不存在時視同無規則**——
  升級前的既有部署沒有這張表，讀不到就拋例外會讓每一次 Save 都失敗，
  這是本設計唯一的真實回歸風險，已於五種 provider 分別驗證。
- **升級路徑**：欄位層敏感度與動作層開關都是 additive 的，日後要加不必迴避本設計。
- **相關**：表與 progId 登記見 [框架保留命名](../framework-reserved-names.zh-TW.md)；
  分類軸見 [ADR-040](adr-040-audit-trail-taxonomy.md)；
  跨節點失效機制見 [ADR-017](adr-017-db-cache-invalidation.md)；
  權限模型見 [ADR-019](adr-019-permission-authorization-model.md)。
