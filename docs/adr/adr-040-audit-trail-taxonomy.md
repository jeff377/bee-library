# ADR-040：稽核軌跡的分類軸與寫入策略

## 狀態

**已採納（Accepted，2026-07-05 定案；2026-08-20 補記為 ADR）**

決策於 2026-07-05 定案並分項落地，寫入側與查詢側均已完成。本 ADR 是事後補記：
原始脈絡寫在一份母計畫裡，而 plan 是階段性文件、封存後會被清除，
「為何稽核分成這幾軸」這種長效理由不該只活在那裡。

## 背景

框架原本**只有診斷用日誌，沒有業務資料軌跡**：

- `ILogWriter` / `LogEntry` —— 系統診斷輸出
- `TraceContext` / `Tracer` —— 請求層級追蹤，記憶體 / UI 導向，未持久化

缺的是「誰在何時登入、看了哪筆敏感資料、把哪個欄位從什麼改成什麼、
哪次呼叫失敗了」——這些是業務稽核，與技術 observability 是兩件事。
`DbScope.Log` 與 log 資料庫分類當時已備好，但底下一張表也沒有。

設計時以 **SAP 與 Odoo 兩套成熟做法為藍本**，因為稽核分類的難處不在實作，
而在「該切成幾件事」。兩者的共同結構是：

| 關注點 | SAP | Odoo |
|--------|-----|------|
| 登入 / 安全事件 | Security Audit Log（`SM19`/`SM20`） | `res.users.log` |
| 業務物件欄位級變更 | Change Documents（`CDHDR` / `CDPOS`） | `mail.tracking.value`、OCA `auditlog` |
| 表級變更（多用於 config） | Table Logging（`DBTABLOG`） | `auditlog` on ACL / groups |
| 敏感資料被**讀取** | Read Access Logging（`SRALMANAGER`） | `auditlog` read 模式 |
| 技術 / 系統錯誤 | System Log（`SM21`） | `ir.logging` |
| 批次 / 應用處理訊息 | `SM37`、Application Log（`SLG1`） | `ir.logging`、server actions |

從中得到三個對設計有決定性影響的觀察：

1. **「事件發生」與「欄位改了什麼」是兩套表、兩種量體特性**——SAP 用 SAL 與
   Change Documents 分開處理，不是同一張表加個欄位。
2. **讀取記錄必須是選擇性的**。SAP RAL 只針對標記的敏感資料記錄，Odoo 官方明示
   read 全記成本過高、不建議對整個 model 開啟。讀 ≫ 寫，全記必然爆量。
3. **before/after 的儲存法有真實取捨**：SAP `CDPOS` 用單一字串欄（通用但型別資訊遺失），
   Odoo `mail.tracking.value` 依型別分欄（型別正確但 schema 寬）。

## 決策

### 一、六軸分類，收斂為四項實作

分析出六條軸線（登入／檢視／異動／執行／系統／安全組態），實作時收斂為四項：

| 實作項 | 涵蓋軸線 | 理由 |
|--------|---------|------|
| 登入記錄 | ① 登入 | — |
| 異動記錄 | ③ 異動 ＋ ⑥ 安全組態 | 見決策三 |
| 檢視記錄 | ② 檢視 | 見決策四 |
| 異常記錄 | ④ 執行 ＋ ⑤ 系統 | 見決策二 |

共通最小欄位模型：`who`（user）／ `when`（UTC）／ `what`（物件＋key＋欄位，或動作名）／
`where`（method／channel／IP／session）／ `before-after`（僅異動）／ `result`。

> 實際表名與欄位以 [框架保留命名](../framework-reserved-names.zh-TW.md) §1 與原始碼為準，
> 本 ADR 不複寫。

### 二、「執行記錄全記」取消，改為異常記錄

原本規劃記錄每次執行，實作前推翻：全記的價值主要落在異常那部分，而其餘內容與登入、
異動記錄重複。改為只持久化 **API 與 DB 的異常**——錯誤、逾時、過慢——供 bug 追蹤與
效能調校。逾時與過慢獨立於錯誤，它們是基礎設施／效能訊號而非程式缺陷。

純技術 observability 仍走 `ILogWriter` / host `ILogger`（檔案／Seq／APM），
**與業務稽核分離**，對齊 SAP `SM21` 與 Odoo `ir.logging` 的定位。
`Tracer` / `TraceContext` 是開發期偵錯工具，不作為稽核來源。

### 三、安全／組態軸併入異動記錄，不另建表

軸⑥（權限、設定變更）本質上就是「某個東西被改了」，與軸③同構。以 `is_sensitive` 旗標
與 `prog_id` 過濾區分，不為它另立一張結構雷同的表。

### 四、檢視記錄預設關閉，且由敏感度驅動

這是六軸中唯一「不能全記」的：

1. **預設關閉**，opt-in 啟用
2. **敏感度驅動**：只記錄標記為敏感的欄位
3. **限定入口**：只在指定 ProgId／動作記錄，而非每次讀取

取樣只適用於行為分析，**不可用於合規舉證**——合規場景通常要求敏感資料存取全記。

> **實作進度補記（2026-08-26）。** 本條三項要求中，「預設關閉」自始即成立，
> 「限定入口」與「敏感度驅動」直到 [ADR-041](adr-041-per-form-audit-rule.md) 才落地，
> 且**只落到表單層**：
>
> | 決策四要求 | 現況 |
> |-----------|------|
> | 預設關閉、opt-in | ✅ `AuditLogOptions.AccessEnabled` 預設 `false` |
> | 限定 ProgId／動作 | ✅ ProgId 維度（`st_audit_rule`）；**動作維度未做**——檢視目前只在 `GetData` 埋一個點，現階段無實際差別 |
> | 敏感度驅動 | ⚠️ **表單層**（`st_audit_rule.is_sensitive`）；**欄位層未做**——「只記錄標記為敏感的欄位」要動 DiffGram 過濾邏輯，另案 |
>
> 也就是說本條在**量體控制**上已成立（可逐張表單開關），但在**「只記敏感欄位」**
> 這個更細的字面要求上尚未完成。合規舉證的判斷要看這個差別。

### 五、before/after 採 DataSet DiffGram 單欄

四個候選中選了框架原生的一條：DataSet 的 `GetChanges()` + DiffGram 本來就同時保留新舊值，
一次涵蓋 master + detail、多列多欄，不必自訂 diff 演算法，讀取時還原成 DataSet 即可直接顯示。

> 「還原成 DataSet」這半在當初的實作中**並不成立**，直到 4.30.0 補上內嵌 schema 才成真。
> 原委與兩種 payload 並存的規則見下方「八、payload 帶內嵌 schema」。

代價是欄位級無法直接以 SQL 查詢統計（需解析 XML）。查詢需求由**表頭的實體欄位**
（who／when／prog_id／row_key…）承擔；只有在真的需要「跨紀錄的欄位級統計」時，
才對指定表加開選配的 EAV 模式。等同 Odoo auditlog 的 fast（預設）／ full（選配）兩檔位。

> **鐵則**：序列化必須用 **DiffGram**（含 before 區塊），普通 `WriteXml` 只寫 current、
> 舊值會遺失；且擷取必須在 `Save` 套用 `AcceptChanges` **之前**。

### 六、寫入採 best-effort 非同步，不採 transactional outbox

原設計是 transactional outbox：業務交易內先寫 outbox 列（同交易 commit，強一致），
再由背景 worker 搬到 log DB。實作時重評並**推翻**——它需要 per-company-DB 的 outbox 表、
多租戶跨庫 flush、以及 repository 簽章改動，代價與收益不成比例。

改為由 BO 在 commit 後走 `IAuditLogWriter`，異動記錄可強制同步寫以縮小漏失窗口。
**outbox 保留為升級路徑**：真正出現「零漏失」需求時再加，且該變更是 additive 的。

> **best-effort 涵蓋 commit 之後的整個稽核步驟**（2026-09-11 補）。原本只有寫入端本身是
> best-effort，組 payload、解析操作者身分這幾步擲出的例外會直接冒到呼叫端。後果是
> **資料已經寫入，API 卻回傳失敗**，稽核沒記到，AfterSave / AfterDelete 與外掛也被跳過。
> 實際觸發條件很平常：使用者在欄位貼入一個控制字元，XML 序列化就會擲例外。部署層的
> `CreateApiKey` 更嚴重，金鑰已寫入，呼叫端卻拿不到唯一一份祕密段。
>
> 現在表單的 Save / Delete 與部署層作業都把整個稽核步驟包在同一個守衛裡：失敗時記 error log
> （帶 prog id、作業名與記錄鍵，不帶欄位值），呼叫照常完成。這不改變本決策接受的漏失窗口，
> 只是讓漏失**留下紀錄**，也不再連帶讓已完成的寫入看起來像失敗。

### 七、寫入介面依決策二的分界拆成兩個（2026-08-24 補）

決策二把「系統／錯誤」判為 observability、與業務稽核分離，但**寫入面一直只有一個
`IAuditLogWriter`**：登入／異動／檢視與 API／DB 異常都走它。實作當時合在一起的理由是
**寫入管線共用**（有上限佇列、批次、退路檔案、log 資料庫自己的 `DbAccess` 不做異常偵測），
不是因為兩者回答同一種問題。

盤點消費端後拆開：**七個呼叫點沿這條分界乾淨二分，沒有任何一個同時寫兩種**——
`Bee.Business` 那四個只寫稽核，`Bee.Db` 與 `Bee.Api.*` 那三個只寫異常，
而後者的欄位與參數**早就自己叫 `anomalyWriter`**，等於用命名補一個型別系統沒有表達的區分。

| 面向 | 處置 |
|------|------|
| 介面 | `IAuditLogWriter`（收 `AuditEntry`）與 `IAnomalyLogWriter`（收 `AnomalyEntry`） |
| 記錄型別 | 新增 `AnomalyEntry : AuditEntry` 中間基底，`ApiAnomalyEntry` / `DbAnomalyEntry` 改繼承它，兩者重複的五個欄位（`Kind` / `ElapsedMs` / `ThresholdMs` / `ErrorType` / `ErrorMessage`）上提 |
| 寫入管線 | **不拆**。sink、write repository、佇列、批次、退路檔案完全共用，同一個實例實作兩個介面 |
| 開關 | `AuditLogOptions` **不拆**。拆出獨立的 anomaly 選項會改 `SystemSettings.xml` 的結構，是所有既有部署都要跟著改的破壞性變更，而 `AnomalyEnabled` 本來就分得開 |

> **保護是單向的，不要讀成雙向。** `AnomalyEntry` 繼承 `AuditEntry`（兩者共用一條寫入管線），
> 所以 `IAuditLogWriter` 仍然收得下一筆異常記錄。型別系統擋住的只有反方向——
> **異常的產生者寫不了登入、異動或檢視記錄**。風險方向上要防的正是那一向。
> 要雙向就得改成平行基底，代價是共通欄位得複製兩份、且會動到 `IAuditLogWriteRepository`
> 的公開簽章，不划算。

**why 不下放 who／company 到中間層**：`ApiAnomalyEntry` 有 session 脈絡、共通欄照填，
只有 `DbAnomalyEntry` 沒有——它覆寫 `AddCommonColumns` 成空的，並且保持原樣。
一份共通結構要決定的不是有哪些共通欄，是誰可以整組不要。

**未納入本次**：讀取側仍由 `LogBusinessObject` 一併服務，九支查詢方法共用保留 progId
`AuditLog` 的授權。合規稽核與維運排錯在 ERP 是兩種角色，把讀取權限拆開價值更高，
但那是權限模型的題目、不是寫入介面的題目，另案處理。

### 八、payload 帶內嵌 schema（2026-09-09 補）

決策五說 DiffGram 的好處之一是「讀取時還原成 DataSet 即可直接顯示」，但寫入端當時輸出的是
**無 schema 的裸 DiffGram**，而 `DataSet.ReadXml` 對這種 payload 用全新 `DataSet` 讀回會得到
**零張表**——實測六種 `XmlReadMode` 皆同。讀取端因此只能改以 `XDocument` 自行解析、
靠 `diffgr:id` 配對 before 列。也就是說**該項好處從未兌現**，而沒有任何機制會發現：
編譯器不看散文，測試驗的是讀取端自己那條路。

改法是寫入端在 DiffGram 前加寫一份內嵌 XSD，兩者包在單一外層元素 `AuditChanges` 內
（`DataSet.WriteXmlSchema` + `DataSet.WriteXml`，見 `src/Bee.Business/AuditLog/AuditDiffGram.cs`）。
如此 payload 自帶欄位結構，可用它自己的 schema 重建成真正的 `DataSet`，
變更明細改由比對 `DataRowVersion.Original` 與 `Current` 得出。

| 面向 | 決定 |
|------|------|
| 新舊並存 | 兩種 payload 以 **root 元素**分派（新格式 `AuditChanges`、舊格式 `diffgr:diffgram`、最小刪除標記 `DeletedRow`），互斥且不需版本欄位 |
| 既有資料 | **一列都不遷移**，舊格式由 `SchemalessDiffGramReader` 繼續讀，**不設落日期限** |
| 體積 | schema 是固定成本（Northwind 訂單那組 26 欄／2 表約 +4.2 KB／列），與資料量無關；異動記錄寫進獨立的 `log` 資料庫，不壓到業務庫 |
| 值的字串化 | 一律 `XmlConvert`，與舊格式的 XML 原文逐字一致且 culture 無關；用 `ToString()` 會讓同一筆異動因儲存格式不同而顯示不同 |
| XML 不允許的字元（2026-09-11 補） | 控制字元與 U+FFFE / U+FFFF 寫成字元參照（如 `&#x1;`），讀取端關閉字元檢查後讀回原值；CR 寫成 `&#xD;`，避免讀回時被正規化成 LF；落單 surrogate 沒有任何 XML 表示法，換成 U+FFFD。代價是含這類字元的 payload 不是嚴格合法的 XML 1.0，外部工具直接解析 `changes_xml` 時要關閉字元檢查 |

**刻意不做的三件事**：不追宣告型別（`Date` vs `DateTime`）——`FormSchema` 才是欄位結構的
權威來源，payload 不該再複寫一份；不改 `RecordFieldChange`，因此**沒有 wire 形狀變更**；
不動資料庫層面。

**為何不用 `XmlSerializer`。** `DataSet` 實作 `IXmlSerializable`，其 `WriteXml` 就是上述兩支
BCL 方法，因此 `XmlSerializer` 產出的 payload 與此等價。不走它是為了讓
[ADR-025](adr-025-define-types-aot-xmlserializer-compat.md) 的反射路徑疑慮永久不必再論證。
附帶查證：`changes_xml` 的讀取端只存在於伺服端（`Bee.Business` 不被任何行動／WASM head 引用），
且 `changes_xml` 從不上 wire——client 收到的是已攤平的 `RecordFieldChange`。

### 九、payload 維持 XML，不改用 JSON（2026-09-11 補）

評估過改用傳輸序列化既有的 `DataSetJsonConverter` / `DataTableJsonConverter` 儲存 payload。
以 Northwind 訂單（主檔 16 欄、明細 10 欄），以及「每種 `FieldDbType` 一欄、含極值與 DBNull」的表實測：

| 指標 | JSON 相對現行 XML |
|---|---|
| 體積 | 54%～70%；與不縮排的 XML 比為 67%～82%，差距有一部分來自縮排 |
| 序列化時間 | 24%～30% |
| 還原時間 | 37%～82%（修改列越多差距越小） |
| 還原度 | 全部 `FieldDbType`（含新增／修改／刪除、多語系）兩者皆完整還原，讀取端產出的欄位異動清單逐筆相同 |

效能數字量於第八節補上不允許字元的處理之前；該補強對一般資料的 payload 逐字不變（實測），體積與還原度數字不受影響。

**決定維持 XML。** 理由如下：

- **還原度等價。** 異動記錄的查看需求是「哪些欄位從什麼值改成什麼值」，兩種格式在這點上等價。
  第八節補上不允許字元的處理後，實測 CR、CRLF、控制字元、NUL 在 XML 往返中皆讀回原值，
  落單 surrogate 兩種格式都換成 U+FFFD。
- **效能差距不構成理由。** 異動記錄一次只寫入或讀取單筆表單資料，序列化差距在每次數十到一百多微秒的量級。
- **體積差距不構成理由。** 異動記錄寫進與業務庫分離的 `log` 資料庫。框架目前固定寫入單一 `log` 資料庫，
  量體增長時可由部署端以分庫或封存緩解。
- **做法已長期驗證。** 維護者在既有系統中以 `DataSet` XML 記錄異動已使用十年以上。

改用 JSON 反而要付出：

- 以 UTF-8 原字儲存 BMP 以外的字元（emoji、CJK 擴充 B 區）時，System.Text.Json 的內建編碼器都會跳脫，
  需要自訂 `JavaScriptEncoder`，而它必須覆寫的方法是指標簽章，得開啟 unsafe 程式碼；
- 持久化資料從此依賴 wire 的 JSON 形狀，改動 wire 會牽動既有稽核列能否讀回；
- JSON 只支援 `FieldDbType` 對應的 CLR 型別（`TimeSpan`、`DateTimeOffset` 擲例外，`double`、`char` 讀回後型別改變）；
- 讀取端再多一種格式分支，而既有兩種 XML 格式仍須永久可讀。

**也不需要為 `DateOnly` / `TimeOnly` 做特別處理。** Date 欄以 `DateTime`、Time 欄以字串存在 `DataSet` 中，
值與欄位標記都能完整還原。日期欄只顯示日期、時間點欄換算時區，屬顯示層依 `FormSchema` 欄位型別處理的事，
與第八節「不追宣告型別」一致。

## 理由

**為什麼照抄兩套 ERP 的分類而不自創。** 稽核分類的成本不在寫程式，而在事後發現切錯了——
表已經長滿資料，改分類等於資料遷移。SAP 與 Odoo 的切法經過長期實務驗證，且兩者
**獨立收斂到相同結構**（事件與欄位變更分離、讀取記錄選擇性），這種一致性本身就是證據。

**為什麼檢視記錄要犧牲完整性。** 因為別無選擇：讀取次數比寫入高一到數個數量級，全記會同時
拖垮效能與儲存。SAP 與 Odoo 各自獨立得到同一個結論，沒有第三條路。

**為什麼 DiffGram 勝過型別正確的方案。** 這是「dogfood 既有機制」與「查詢便利」的取捨。
框架的資料交換單位本來就是 DataSet，DiffGram 是它原生的差異表示——選它等於不引入新概念。
欄位級查詢是少數場景，留給選配的 EAV 檔位。

## 後果

**正面**：

- 稽核軌跡與技術 observability 分屬兩套管線，各自的保留期與量體策略互不干擾。
- 異動記錄零自訂 diff 邏輯（差異由 `GetChanges()` 產生），且自 4.30.0 起能還原成 DataSet 直接呈現。
- 檢視記錄的預設關閉讓「開啟稽核」不會意外變成效能事故。

**負面 / 成本**：

- 異動記錄的欄位級查詢需解析 XML，或改用選配的 EAV 檔位。
- best-effort 寫入有漏失窗口。這是刻意接受的——需要零漏失時升級為 outbox。

**未來增強**（尚未實作）：per-form 稽核規則——目前異動與檢視都是「全記所有表單」，
未來可加一份執行期規則讓管理員選擇哪些 ProgId 要記錄，對齊 Odoo `auditlog.rule`。
見 [future-work](../repo-ops/future-work.md)。

## 參考

- 保留與分區（依年分庫、append-only、hash-chain）的設計方向見
  [資料庫設定指引](../database-settings-guide.zh-TW.md) 的多資料庫情境。
- 相關 ADR：[ADR-017](adr-017-db-cache-invalidation.md)、
  [ADR-018](adr-018-db-define-storage.md)、
  [ADR-019](adr-019-permission-authorization-model.md)。
