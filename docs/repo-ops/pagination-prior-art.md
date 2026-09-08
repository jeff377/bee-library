# 分頁做法的外部佐證（prior art）

**這不是踩雷誌**，所以不放在 `gotchas/`。它是一份**設計決策的外部佐證**：查別家 ERP 與框架
怎麼處理分頁，用來檢驗 bee-library「深分頁不處理」這個決定不是偷懶。

決定本身、量測數據、以及它不涵蓋的範圍，都在
[`gotchas/database.md`](gotchas/database.md) 的〈深分頁：`OFFSET` 的成本隨頁碼成長〉那一條
—— **本檔不複寫那些**，只放外部對照。

> **這是 2026-09-08 查到的狀態，不是持續維護的比較表。** 各家版本會演進，
> 下次要引用前先確認還成立。查證邊界寫在文末，一併讀。

## 四家的預設全是 offset

| | 預設 | 有 keyset 可選嗎 |
|---|---|---|
| Odoo | offset | **沒有**（`query.py` 裡不存在這個概念） |
| SAP RAP | offset | **沒有**（`IF_RAP_QUERY_PAGING` 只給得出 offset） |
| SAP CAP | offset | 有，**服務端** opt-in（`reliablePaging`） |
| Microsoft ASP.NET OData | offset（文件原話 "By default, we will use `$skip`"） | 有，**per-route** opt-in |

Odoo 與 RAP 甚至不是「預設」——**沒有第二個選項**。有 keyset 的那兩個都是 opt-in，
而且動機都是一致性、不是深頁延遲（見下方〈對 bee-library 的意義〉）。真正迴避掉深翻的
是 **UI 層**的設計，不是框架層的演算法。

**Odoo —— 跟我們現在完全一樣。** 它是與本框架最接近的類比（ORM + 泛用列表視圖 + 任意跳頁）：

- `odoo/tools/query.py` 的 `select()` 尾端就是 `LIMIT %s` + `OFFSET %s`，`models.py` 的
  `_read_group` 同樣；該檔與 `models.py` 都 grep 不到 keyset / cursor / seek。
- 它的 pager（`addons/web/static/src/core/pager/pager.js`）**不只有上下頁** —— 點一下數字
  可輸入 `min[,max]` 範圍，parse 後 clamp 進 offset，等於允許直接跳到任意深度。

**SAP —— 靠設計不讓使用者走到那裡，而不是靠 keyset。** 分三層看：

- **UI 層**：SAPUI5 文件寫明 responsive table 一次不超過 200 筆、更多（上限 1000）要用
  growing 功能並「make sure the user can filter the data」。Fiori elements 的 list report
  預設是 growing table（`More` 按鈕），**只能往下追加、沒有頁碼**，主互動是頂部的 filter bar。
- **RAP（ABAP，OData V4）的查詢 API 是 offset 制**。`IF_RAP_QUERY_PAGING` 給的是
  `get_page_size()` 與 **`get_offset()`**，SAP 自家 openSAP 教材的範例就是
  `DATA(top) = io_request->get_paging( )->get_page_size( ).` /
  `DATA(skip) = io_request->get_paging( )->get_offset( ).`，再對應 SQL 的
  `UP TO n ROWS` 與 `OFFSET`。RAP 確實會在客戶端沒有妥當分頁時補一個帶 `$skiptoken` 的
  next link（未給 `$top` 時預設截到 100 筆、`$top` 上限硬性 5000），但那是**框架的封頂機制**，
  底下的 API 仍然只給得出 offset。
- **而 UI5 客戶端根本就送 `$skip` / `$top`**。openui5#3487 有人主張這是「server side paging
  not properly implemented」，UI5 維護者的回覆是：OData 是無狀態協定，規格要求的是
  `$top` 搭配穩定排序（`$orderby` 未給時服務端必須自行施加穩定順序），server-driven paging
  的用途不是引入狀態。**議題以 completed 關閉。** 所以 Fiori 的 grid table 把捲軸拖到深處，
  送出的就是一個大的 `$skip`。

> **「呼叫端選哪一種」是誤讀，這裡分清楚。** 有兩個軸常被混在一起：
>
> | 軸 | 選項 | 誰決定 |
> |---|---|---|
> | **A. 誰算分頁邊界** | client-driven（自己送 `$skip`/`$top`）vs server-driven（跟著 `@odata.nextLink`） | **呼叫端** |
> | **B. 伺服端怎麼接續** | offset 算術 vs keyset 述詞 | **服務端設定** |
>
> 「兩種分頁」講的是軸 B，而呼叫端能選的是軸 A。**RAP 的軸 B 根本沒有選項** —— 兩條路都通到
> 同一個 `get_offset()`，是同一種穿了兩件外套。CAP 的軸 B 有選項，但開關在服務端，
> 客戶端無法要求「這次給我 keyset」。
>
> 另外，`get_offset()` 是**服務實作者**在自己的 `select()` 裡拿到的參數，不是呼叫端呼叫的方法；
> 呼叫端那邊只有 URL 與 query option。

**SAP 真正有 keyset 的地方是 CAP，而且是服務端選配。** CAP 的 **Reliable Pagination**
（`cds.query.limit.reliablePaging`，Java 為 `.enabled`）**以「該頁最後一列的值」產生 skip token**
—— 這是貨真價實的 keyset。但三件事要一起看：

1. **預設關閉**，且僅限 OData V4 端點。
2. **它解的是一致性，不是深頁延遲** —— 官方說法是數字型 skip token「can result in duplicate or
   missing rows if the entity set is modified between the calls」。
3. **限制不小**：`$orderby` 不能用函式／算術運算式的結果、元素必須是簡單型別、若有 `$select`
   則所有 `$orderby` 元素都得包含在內、不支援複雜的結果集串接。

順帶一提，SAP 自家的 ABAP 關鍵字文件講 `SELECT ... OFFSET` 時只寫「必須搭配 `ORDER BY`」
與嚴格語法檢查，**沒有提效能**。

> **RAP 與 CAP 不是同一套東西的兩個版本，是兩個 stack 的兩套獨立實作** —— 這才是它們對分頁
> 給出不同答案的原因，不是 SAP 改了主意。RAP 是 **ABAP**，跑在 ABAP Platform（S/4HANA 內或
> BTP ABAP Environment），用來現代化既有 ABAP 邏輯與直接存取核心資料；CAP 是
> **Node.js / Java**，跑在 BTP，用於全新雲端應用與跨系統整合。兩者都產 OData 服務、
> 都驅動 Fiori elements 前端。
>
> 結構上的差別看得到：CAP 的 generic service provider **自己組查詢**，所以 keyset 可以是一個
> runtime 開關；RAP 的 unmanaged query 是把 `top` / `skip` **交給服務實作者自己寫 SELECT**，
> 框架沒有介入的位置。**但這個因果只是合理讀法、不是查證過的說明** —— RAP 另有 managed
> 情境是框架全包的，那條路徑沒查。
>
> **命名陷阱**：兩邊都講「CDS」，但 **ABAP CDS**（ABAP Dictionary 裡的 DDL）與
> **CAP CDS / CDL**（`.cds` 檔，編譯成 CSN）**是兩種不同的語言**，只共用名字與概念血統。
> 查資料看到「CDS view」先確認講的是哪一邊。

## 對 bee-library 的意義（兩點）

**其一**，成熟 ERP 對「深翻很慢」的答案是**不要讓使用者走到那裡**，而不是讓 `OFFSET` 變快。
維護者那句「一般都是下查詢條件，然後抓淺分頁查看」在我們這裡是**判斷**，在 SAP 是**硬性
UI 準則**。真要做 UI 的話，growing / `More` 比頁碼跳轉更貼近這個結論，
且**不需要動 `PagingOptions`**。

**其二，也是更值得記的：業界做 keyset 的動機是「一致性」，不是「深頁變快」。**
CAP 與 Microsoft 的 ASP.NET OData 都明講，換掉數字 offset 是為了避免資料在兩次呼叫之間被改動
而造成重複列或漏列。**那跟我們量的是不同的問題** —— 我們階段 B 只評估了延遲。
若日後要重啟這個決定，**一致性會是比延遲更強的理由**，而且本框架有同樣的曝險：
`PagingOptions.IncludeTotalCount` 預設 `false`、靠 `PageSize + 1` 探測 `HasMore`，
那個設計對「翻頁期間資料被改」一樣沒有保護。**這一點目前沒有測過，也沒有結論。**

**若日後真的要走 keyset**，`$skiptoken` 那個形狀值得抄：**由伺服端在回應裡發不透明續傳權杖**，
呼叫端原樣帶回。這樣底層從 offset 換成 keyset 時 wire contract 不必跟著改 ——
而現行 `PagingOptions` 是頁碼制，換過去就是破壞性變更。

## 查證邊界

> **別把這份當定論**：Odoo 那三點是讀 17.0 的原始碼確認的（程式碼會搬家，
> 對到新版本前先確認路徑還在）；SAP 的 UI 準則、CAP 的 Reliable Pagination、RAP 的
> `get_offset()` 與 openSAP 範例、UI5 維護者在 openui5#3487 的回覆，都是官方或 SAP 自家來源。
>
> **這段修正過一次**：先前寫「`$skiptoken` 實際被當 offset 用」時引的是社群教學，
> 而且差點把一段描述**誤植給 SAP** —— 「skiptoken 由 orderby 值加上最後一列的鍵組成」
> 那個說法出自 **Microsoft 的 ASP.NET OData WebAPI**（而且連它都是 per-route 選配，
> 預設仍用 `$skip`），不是 SAP。查框架行為時**先確認那句話的主詞是誰**。
>
> **RAP vs CAP 那段的技術欄位**（語言、runtime、建模語言、兩種 CDS 的分別）有 capire 與
> ABAP 官方文件支撐，但**「什麼時候該選哪個」主要來自社群整理** —— 找不到 SAP 官方的單一
> 決策文件（最接近的是 SAP 自家人寫的 community 部落格，該站一律回 403，沒讀到原文）。
>
> 仍未查三件事：SAP Gateway（OData V2）各服務自訂的 `$skiptoken` 語意（本來就是 per-service
> 的實作決定，不存在單一答案）；RAP 的 **managed** 情境（框架全包那條路徑）；
> 以及 **CAP 開著 `reliablePaging` 時，客戶端仍送 `$skip` 會怎樣** ——
> 官方文件只說「nextLink 的值客戶端不得解讀或更改」，對這點隻字未提。

