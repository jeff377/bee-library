# 計畫：`st_log_change.changes_xml` 改用帶 XSD 的 DataSet DiffGram

**狀態：✅ 已完成（2026-09-09）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | payload 改為 XSD + DiffGram；讀取端加 root 分派、新增 `DataRow` walk 分支、保留舊格式分支；雙格式測試 | ✅ 已完成（2026-09-09） |
| 2 | 對齊 adr-027 / adr-040 的敘述（「還原成 DataSet」屆時才成立），並記錄舊格式仍需支援 | ✅ 已完成（2026-09-09） |

> 階段 1 必須**原子完成**：寫入端換了而讀取端還沒認得新格式，異動明細會直接讀不出來。
> **兩階段合併為單一 commit**（2026-09-09 確認）——階段 2 要修的那兩處 ADR 敘述，
> 正是階段 1 落地後才成立的，分開提交會留下一段「敘述與實作再度脫節」的空窗。

## 背景

校 2026 鐵人賽 Day 25（稽核軌跡）時提出的疑問：現行 `changes_xml` 存的是**無 schema 的
DiffGram**，欄位結構在寫入當下就丟掉了。「正確作法應該保留欄位結構，XML → DataSet 時能取得
當時的完整結構」。

評估後**採納**，並收斂為只做 `DataSet` ↔ XML 這一段的轉換改良。

## 決策

**採方案 D：`DataSet.WriteXmlSchema()` + `DataSet.WriteXml(XmlWriteMode.DiffGram)`，
包在單一外層元素內。純 BCL，不經 `XmlSerializer`。**

三件明確**不做**的事（劃清範圍，避免實作時擴散）：

| 不做 | 理由 |
|------|------|
| 不動資料庫層面（分庫、保留期、欄位型別） | 異動記錄一律寫進 `CategoryId = log` 的資料庫，維持現狀 |
| 不追「宣告型別」（`Date` vs `DateTime`） | **`FormSchema` 已完整描述欄位結構**，那才是宣告型別的權威來源，不該由 payload 再複寫一份（`rules/single-source.md`） |
| 不改 `RecordFieldChange` / 不動 wire | 型別不進 DTO，就沒有 wire 形狀變更，`wire-contracts/` 與 `bee-connector-js` 完全不受影響 |

因此本案是**純伺服端內部的儲存格式變更**，blast radius 僅限 `Bee.Business` 的兩個
`internal` 型別與其測試。

## 現況

| 面向 | 實作 |
|------|------|
| 寫入（表單路徑） | [`AuditDiffGram.Serialize(DataSet)`](../../../src/Bee.Business/AuditLog/AuditDiffGram.cs) → `WriteXml(writer, XmlWriteMode.DiffGram)` |
| 寫入（部署層路徑） | 同檔的 `ForFieldUpdate` / `ForInsert`，現場合成最小 `DataSet`（欄位一律 `typeof(string)`） |
| 刪除且無 snapshot | `MinimalDeleteXml` → `<DeletedRow table="…" sys_rowid="…" />` |
| 讀取 | [`ChangeDiffGramReader.Read`](../../../src/Bee.Business/AuditLog/ChangeDiffGramReader.cs)，**不用** `DataSet.ReadXml`，以 `XDocument` 自解、靠 `diffgr:id` 配對 before 列 |
| 消費端 | `LogBusinessObject.GetChangeDetail` → `List<RecordFieldChange>`（`OldValue` / `NewValue` 皆 `string?`） |

## 前提查證：trim / AOT 不成立

這是唯一可能否決本案的項目——`ChangeDiffGramReader` 的 remarks 自陳改用 `XDocument`
有一半理由是避開 `XmlSerializer` 反射路徑（[adr-025](../../adr/adr-025-define-types-aot-xmlserializer-compat.md)）。
**兩層都不成立**，故本案可行。

### 第一層：讀取端只跑在伺服端

`ChangeDiffGramReader` 是 `Bee.Business` 的 `internal` 型別，而 `Bee.Business` 的引用者只有
`src/Bee.Hosting`、`samples/Bee.Samples.Shared`、`apps/Bee.Northwind.Server` 與各測試專案。

行動 / WASM head 的相依鏈是
`Bee.Northwind.{iOS,Android,Browser}` → `Bee.Northwind.UI` → `Bee.UI.Avalonia` →
`Bee.UI.Core` / `Bee.Api.Client` / `Bee.Definition` / `Bee.Expressions`
——**沒有 `Bee.Business`**。

`changes_xml` 本身也從不上 wire：`GetChangeDetail` 在伺服端就解成 `List<RecordFieldChange>`。

### 第二層：要 XSD 根本不必動用 `XmlSerializer`

`XmlSerializer.Serialize(writer, dataSet)` 之所以能輸出 XSD + DiffGram，是因為 `DataSet`
實作 `IXmlSerializable`，其 `WriteXml` 內部就是 `WriteXmlSchema` + `WriteXml(DiffGram)`。
直接呼叫這兩支 BCL 方法可得**幾乎逐字相同**的 payload（實測差 10 bytes，只是外層元素名不同），
且完全不經反射路徑。

**採 D 而非 `XmlSerializer`，是為了讓這一層永久不必再論證。**

## 實測

環境：本次自行重跑（.NET 10、macOS）。用真實案例定義
`apps/Bee.Northwind/Define/FormSchema/Order.FormSchema.xml`（master 16 欄、detail 10 欄）
依 `DataFormRepository.BuildEmptyDataTable` 的同一條路徑建 `DataSet`。
探測程式為 throw-away，**未進 repo**。

### 前置校準

`WriteXml(StringWriter, DiffGram)`（production 寫法）與探測程式的縮排 A 輸出**同為
4,367 bytes**，確認基準線一致。四種寫法混用縮排會失真，下表一律 like-for-like（minified）。

### 體積

| 情境 | 變更列數 | A 現行 | D 採用 | 差額 |
|------|---------|-------|-------|-----|
| 編輯訂單，5 行明細（改 1 / 刪 1 / 增 1） | 4 | 3,790 | 8,023 | +4,233 |
| 編輯訂單，1 行明細 | 3 | 3,301 | 7,534 | +4,233 |
| 新增訂單 + 5 行明細 | 6 | 3,482 | 7,715 | +4,233 |
| 新增訂單 + 20 行明細 | 21 | 11,086 | 15,319 | +4,233 |
| 全單改價，20 行全改 | 21 | 20,456 | 24,689 | +4,233 |
| 全單改價，50 行全改 | 51 | 52,720 | 56,953 | +4,233 |
| 部署層 `ForFieldUpdate`（3 欄 1 列） | 1 | 530 | 1,184 | +654 |

**XSD 是固定成本，與資料量完全無關**：Order 這組 26 欄／2 表一律 **+4,233 bytes**，
比例因此從小異動的 2.1 倍降到大異動的 1.08 倍。列出來只是讓量級可見——
**單筆明細的 `DataSet` 大小可控，沒有欄位放不下的風險，體積不是本案的決策因素。**

> 順帶一提，現行 payload 是**縮排**的，改 minified 可省 13%（4,367 → 3,790）。
> 與本案無關，但若要抵掉一部分 XSD 成本，這是免費的。

### 還原能力（採用 D 的實質理由）

以**全新** `DataSet` 讀回：

| 寫法 | 結果 |
|------|------|
| A 現行 `WriteXml(DiffGram)` | **零張表**（六種 `XmlReadMode` 皆同）——reader remarks 的自陳屬實 |
| `WriteXml(WriteSchema)` | 欄數／型別對，但 `RowState=Added`、**原值全失** |
| **D `WriteXmlSchema` + `WriteXml(DiffGram)`** | **完整還原**：`RowState` 正確、`Original` / `Current` 兩版本俱在、CLR 型別保真 |

### 讀取端可改寫為 `DataRow` walk（階段 1 的可行性依據）

對還原後的 `DataSet` 實測列舉（3 行明細，改 1 / 刪 1 / 增 1）：

```
table Order: 16 cols, 1 rows
  Modified  changed=[freight:32.38->41.90, status:Draft->Confirmed, total_amount:0->1234.56]
table OrderDetail: 10 cols, 3 rows
  Modified  changed=[quantity:12->20, amount:159.60->266.00]
  Deleted   before.ref_product_name=Queso Cabrales Reserva 3
  Added     new.ref_product_name=Gorgonzola Telino  qtyCLR=Int32
```

三種 `RowState` 都取得到，`Deleted` 列能經 `DataRowVersion.Original` 讀回 before-image，
表順序與欄順序都保留。**現行 reader 靠 `diffgr:id` 手動配對 before 列的那整段，
可由 `r[c, Original]` vs `r[c, Current]` 取代。**

## 考慮過但未採用的選項

| 選項 | 未採用的理由 |
|------|------------|
| 維持現狀 | 讀取端得繼續維護手寫 parser；且 adr-027 / adr-040 宣稱的「還原成 DataSet」始終不成立 |
| `WriteXml(WriteSchema)` | **丟原值**，違反 adr-027 D5 與 adr-040 的鐵則 |
| `XmlSerializer.Serialize(dataSet)` | payload 與 D 等價（差 10 B），但多背一個 `XmlSerializer` 相依與其 AOT 論證負擔 |
| 精簡「欄位 → 型別」對照（+935 B） | 只給型別、**給不出可還原的 `DataSet`**；而型別已由 `FormSchema` 描述，本案不追 |

### 兩項在重測下不成立、不可拿來當理由的論點

寫下來是因為它們聽起來都很有說服力，其中一項還是本案原始動機的一半。

1. **「schema drift 會讓當時的欄位值靜默消失」對現行實作不成立。**
   那是 `ReadXml` + 今日 schema 的問題（實測會先擲 `ConstraintException`，或靜默丟欄）。
   但 `ChangeDiffGramReader` 用 `XDocument` 讀 **payload 自己的元素名**，從不查今日 schema
   ——實測對一段含 `status` 的舊 payload，reader 看到的仍是當年完整的 16 欄。
   **現行作法已經是 drift-safe 的。**
   採 D 之後同樣安全（schema 隨 payload 一起存），但**這不是改動的收益**。
2. **「改用 `XmlSerializer` 會踩 AOT」不成立**（見前一節）。

> 這兩項要留在文件裡：日後若有人質疑「為什麼要改」，答案是**讀取端簡化＋可還原成 DataSet**，
> 不是 drift、也不是 AOT。理由記錯會導向錯誤的後續決策。

## 實作設計

### payload 形狀

```xml
<AuditChanges>
  <xs:schema id="AuditChanges" …>…</xs:schema>
  <diffgr:diffgram …>
    <AuditChanges>…current 列…</AuditChanges>
    <diffgr:before>…before 列…</diffgr:before>
  </diffgr:diffgram>
</AuditChanges>
```

外層元素名採 **`AuditChanges`**（2026-09-09 確認），與 `AuditDiffGram.NewDataSet` 現用的
`DataSet` 名稱一致。不用 `XmlSerializer` 的 `DataSet` 當外層名——那個名字太通用，
日後判讀 payload 時無從辨識。

### 寫入端（`AuditDiffGram`）

`Serialize(DataSet)` 改為寫外層元素 + `WriteXmlSchema` + `WriteXml(DiffGram)`。
`ForFieldUpdate` / `ForInsert` 走同一支 `Serialize`，**自動跟著換，維持單一 payload 形狀**
——該檔 remarks 已寫明「Keeping a single shape is the point」，不要為部署層另開一種形狀。

`MinimalDeleteXml`（`<DeletedRow …/>`）維持不變：它本來就不是 DiffGram，沒有 schema 可寫。

### 讀取端（`ChangeDiffGramReader`）

以 **root 元素 local name** 分派，不加版本欄位——四種 payload 的 root 實測互斥：

| payload | root | 分支 |
|---------|------|------|
| 新格式 | `AuditChanges` | `ReadXmlSchema` + `ReadXml(DiffGram)` → `DataRow` walk |
| 舊格式 | `{urn:schemas-microsoft-com:xml-diffgram-v1}diffgram` | **保留**現行 `XDocument` 解析 |
| 最小刪除標記 | `DeletedRow` | 回空清單（現行行為） |
| 其他 / 損毀 | — | 回空清單（現行行為） |

`ReadXml` 只在新格式分支用，且該分支的 `DataSet` 是**由 payload 自帶的 schema 建的**，
不會拿今日 schema 去套——drift 安全性與現行等價。

新分支的 `DataRow` walk 要保留現行的三條語意：
`Added` → 逐欄 `Current`、`OldValue` 為 null；`Deleted` → 逐欄 `Original`、`NewValue` 為 null；
`Modified` → **只輸出 `Original` 與 `Current` 不同的欄**；三者皆跳過 `sys_rowid`。

補四點容易漏的：

- **`RowKey` 的版本**：`Deleted` 列要用 `DataRowVersion.Original` 讀 `sys_rowid`，
  其餘用 `Current`（`FormBusinessObject.Audit` 的 `ExtractMasterChange` 已是這個寫法，照抄即可）。
- **跳過 `Unchanged` 列**：`GetChanges()` 正常情況不會產出，但防禦性略過，別讓它變成一堆空 diff。
- **XXE 加固**：新分支同樣走 `LoadHardened`（`DtdProcessing.Prohibit` + `XmlResolver = null`），
  再從 `XDocument` 切出 `schema` / `diffgram` 兩段餵給 `ReadXmlSchema` / `ReadXml`。
  **不要**把原始字串直接交給 `DataSet.ReadXml`——那條路沒有加固（`rules/scanning.md`）。
- **信任邊界**：payload 來自自家 log 資料庫、由框架自己的寫入端產生，不是使用者輸入。
  `DataSet.ReadXml` 對不受信任 XML 的已知風險在此不適用，但加固仍照做（成本為零）。

### ⚠️ 值的字串化必須用 `XmlConvert`，不能用 `ValueUtilities.CStr`

**這是實作前唯一需要改掉的設計細節，也是最容易寫錯的地方。**

舊分支讀的是 **XML 原文**（XSD lexical form，culture 無關）；新分支若用
`ValueUtilities.CStr`，走的是 `value.ToString()`——**culture 相依**。
同一筆邏輯異動會因為儲存格式不同而在明細顯示不同的字串。實測：

| 型別 | 舊分支（XML 原文） | `CStr`（zh-TW） | `CStr`（de-DE） | `XmlConvert` |
|------|------------------|----------------|----------------|--------------|
| `DateTime` | `2026-09-03T00:00:00` | `2026/9/3 上午12:00:00` | `03.09.2026 00:00:00` | `2026-09-03T00:00:00` ✅ |
| `decimal` | `1234.5` | `1234.5` | `1234,5` ❌ | `1234.5` ✅ |
| `bool` | `true` | `True` ❌ | `True` ❌ | `true` ✅ |
| `Guid` / `int` / `string` | 原樣 | 相同 | 相同 | 相同 |

**採 `XmlConvert.ToString(...)`**（`DateTime` 用 `XmlDateTimeSerializationMode.RoundtripKind`），
逐字對上舊分支輸出，且 culture-invariant。

這也才對得上寫入端既有的意圖——`AuditDiffGram.NewTable` 明確設
`Locale = CultureInfo.InvariantCulture`，註解寫著「the payload is machine-read, and a server
whose culture happens to change must not alter how a stored value is rendered」。
用 `CStr` 等於在讀取端把寫入端刻意避開的 culture 相依又放回來。

> 顯示層面不受影響：`RecordFieldChange` 本來就是資料 DTO，舊列本來就是這個格式，
> **不是回歸**。要給人看的日期格式屬 UI 的事，不在本案範圍。

> 順帶查證：`DataTableExtensions.AddColumn` 對有預設值的欄位設 `AllowDBNull = false`，
> 所以表單路徑的欄位**不可能是 null**；現行 reader 的 union-names 防禦是給手建 `DataSet`
> （`ForFieldUpdate` / `ForInsert`）用的。新分支照樣要保留 null → `OldValue`/`NewValue` 為 null 的處理。

### 明確不做

- **不補 `GetDeclaredFieldDbType` 的 `String` → `Enum.Parse` 退路。**
  `WriteXmlSchema` 會把 `ExtendedProperties` 的 `FieldDbType` 寫成 `msprop:` 註記，
  但讀回是 `String`，`as FieldDbType?` 會回 `null`、`ResolveFieldDbType()` 退回 CLR 推論
  （`Date` → `DateTime`）。**本案不追宣告型別，此行為可接受**，`FormSchema` 才是權威來源。
- **不為了省 msprop 而重建欄位。** 實測 msprop 佔 XSD 4,233 B 中的 1,131 B（27%），
  但剝除需要在寫入端重建 `DataTable`，會讓 payload 與「原 `DataSet` 的忠實序列化」脫鉤，
  不值得。
- **不把 payload 改成 minified**（2026-09-09 確認）。體積既已確認不是決策因素，
  minify 唯一的理由就消失了；而縮排的 payload 在人工查稽核列、debug 時明顯好讀。
  這也是與本案正交的行為變更，混進來只會擴大範圍。
- 不改 `RecordFieldChange`、不改 wire、不動 `st_log_change.TableSchema.xml`。

## 相容性

**既有 `changes_xml` 一列都不遷移。** 舊格式由 root 分派走原本的 `XDocument` 分支，
行為逐字不變。新舊列在同一張表共存是正常狀態，**不設落日期限**——舊 parser 的維護成本
遠低於一次資料遷移的風險。

## 影響的檔案清單

**階段 1**

- `src/Bee.Business/AuditLog/AuditDiffGram.cs` — `Serialize` 改寫；類別 remarks 要跟著更新
  （現有敘述說 payload 是「schemaless DataSet DiffGram」）
- `src/Bee.Business/AuditLog/ChangeDiffGramReader.cs` — 加 root 分派與新分支；
  **remarks 必須改**（現有那句「the write side emits a DiffGram without an inline schema,
  which `ReadXml` cannot reconstruct」在新格式下不再成立，會變成只描述舊格式）
- `tests/Bee.Business.UnitTests/AuditLog/ChangeDiffGramReaderCoverageTests.cs` —
  **兩種格式都要覆蓋**；舊格式的既有案例一個都不能刪（那是回歸保護）
- `tests/Bee.Business.UnitTests/AuditLog/LogBusinessObjectTests.cs`
- `tests/Bee.Business.UnitTests/Form/FormBusinessObjectAuditTests.cs`
- `tests/Bee.Business.UnitTests/SystemBusinessObjectApiKeyLifecycleTests.cs`
- `tests/Bee.Business.UnitTests/SystemBusinessObjectDeploymentAuditTests.cs`
- `tests/Bee.Definition.UnitTests/Logging/AuditLoggingTests.cs` — 有一處硬寫死
  `"<diffgr:diffgram />"` 的斷言

新增測試至少要蓋：新格式 round-trip 三種 `RowState`、`Modified` 只吐差異欄、
`Deleted` 取得 before-image、**舊格式 payload 仍讀得出相同結果**、以及三種非 DiffGram
payload（刪除標記 / 空字串 / 損毀 XML）仍回空清單。

**階段 2**

- `docs/adr/adr-027-audit-trail.md` —「考慮過的選項」第 2 點的「XML→DataSet 還原即可顯示」
  ——**改動後才成立**，需說明是此次變更帶來的
- `docs/adr/adr-040-audit-trail-taxonomy.md` —「五、before/after 採 DataSet DiffGram 單欄」段
  與「後果」段的「零自訂 diff 邏輯，且能還原成 DataSet 直接呈現」；補記 payload 格式變更
  與「舊格式仍需支援」

> 這兩處**目前是失效宣稱**（無 schema 的 DiffGram `ReadXml` 回零張表，且讀取端是手寫
> parser），正是 `code-style.md`「帶絕對語氣的宣稱，必須指得出執行它的機制」要防的那類。
> 階段 1 落地後它們會變成真的——但仍要動筆，因為**新舊格式並存**這件事沒有記在任何 ADR 裡。

**不受影響（已查證）**

- 行動 / WASM head：不引用 `Bee.Business`
- wire 形狀、`wire-contracts/`、`bee-connector-js`：`changes_xml` 不上 wire，且 DTO 不變
- `st_log_change.TableSchema.xml`（`src/Bee.Definition/Defaults/` 與 `apps/Bee.Northwind/Define/`
  兩份）：`DbType="Text"` 無上限，不需改
- `LogBusinessObject.GetChangeDetail`：呼叫端簽章與回傳型別不變

## 執行結果

設計全數照做，另有五項實作時才浮現、值得留下的事。

### 1. 讀取端拆成兩個型別，不是一個檔加分支

- `ChangeDiffGramReader` —— 分派 + 新格式（`DataRow` walk）
- `SchemalessDiffGramReader` —— 舊格式解析，**逐字沿用**原本的實作，標為 frozen

理由：舊格式的解析不該再演進，獨立成檔才看得出這件事；混在同一個型別裡，日後修新格式時
很容易順手「順便整理」舊分支，而那是唯一在讀既有資料的程式碼。

### 2. ⚠️ `XElement.CreateReader()` 的子樹 reader 會讓 `ReadXml(DiffGram)` 炸掉

第一版把 payload 先解析成 `XDocument`、再用 `CreateReader()` 把 `schema` / `diffgram` 兩個子樹
餵給 `DataSet`。**縮排的 payload 會擲 `ArgumentException: The local name for elements or
attributes cannot be null or an empty string`**（`ReadXmlDiffgram` 走出子樹尾端）。

- **minified 輸入不會重現**，所以只測 minified 的測試抓不到。
- `XmlReaderSettings { IgnoreWhitespace = true }` 包一層**無效**（該設定對包裝 reader 不生效）。
- 正解：用**單一 forward-only reader 讀整份文件**，與寫入端的
  `WriteXmlSchema` + `WriteXml` 對稱。已寫進 `ReadSchemaBound` 的 remarks。

### 3. `ReadStartElement()` 之後會落在空白節點

修好上一項後改成回傳空清單。原因是我加的防禦性檢查 `NodeType != Element` 把縮排 payload
擋掉了——`ReadStartElement()` 之後下一個節點是**空白**，不是 schema 元素。
正解是再呼叫一次 `MoveToContent()`。這也順帶處理了空 wrapper（`<AuditChanges />`）。

> 2 與 3 都是**只在縮排下發生**。維持縮排的決定因此有第二層意義：測試若改用 minified
> payload，這兩個 bug 會同時失去回歸保護。新測試有一條斷言 payload 含換行，就是釘這件事。

### 4. 新舊分支的一致性另外依賴 `DateTimeMode`

`XmlConvert` 只解掉 culture 那一半。`DataTable` 的 `DateTime` 欄預設 `UnspecifiedLocal`，
DiffGram 會寫出**時區位移**（`2027-03-01T00:00:00+08:00`），而還原後的值沒有位移
——兩種格式會在該欄不一致。

production 不會發生：表單路徑一律經 `DataTableExtensions.AddColumn`（明確設
`DataSetDateTime.Unspecified`），部署層路徑的欄位全是 `string`。但**手建 `DataTable` 的測試會踩到**，
一致性測試就是這樣抓出來的。該約束已同時寫在測試的 arrange 註解與 reader 的 remarks。

### 5. 受影響的測試遠少於預估

原本列了六個測試檔，實際**只新增一個**、既有的一個都沒改：

- 新增 `tests/Bee.Business.UnitTests/AuditLog/SchemaBoundChangeReaderTests.cs`
- `ChangeDiffGramReaderCoverageTests`（舊格式）、`FormBusinessObjectAuditTests`、
  `SystemBusinessObject*AuditTests` 全部**未改即通過**——前者餵的是手寫舊格式 payload，
  後三者走真正的寫入→讀取 round-trip，本來就該通過。
- `AuditLoggingTests` 的 `"<diffgr:diffgram />"` 是**當成不透明字串**寫進欄位、從不解析，
  不需要改。

### 6. adr-027 未動

它那句「XML→DataSet 還原即可顯示」在本次改動後**變成真的**，且該句位於「考慮過的選項」的
決策敘述中（歷史紀錄）。改寫它等於改寫當時的決策文字，不划算。
「這句在 4.29.0 之前並不成立」這件事記在 adr-040 新增的第八節。

### 驗證

- `dotnet build -c Release`（即 commit hook 跑的那道）：**0 錯誤 0 警告**。
- `dotnet test --configuration Release --settings .runsettings`：**6,129 通過、0 失敗、1 略過**
  （四個 DB 容器皆在跑，`[DbFact]` 未被 skip）。
- `./check-public-docs.sh`：無新增違規。

### 7. 順帶處理：SourceLink 升版（與本案無關，但擋著 commit）

實作期間 `dotnet build -c Release` 開始因 **NU1902** 失敗（`TreatWarningsAsErrors=true`）：
`Microsoft.Build.Tasks.Git` 10.0.202 命中 **CVE-2026-62900**（資訊洩漏，CVSS 5.9 中度，
[GHSA-23fw-v26w-5fgq](https://github.com/advisories/GHSA-23fw-v26w-5fgq)，**2026-09-08 公布**）。
未動過的 `Bee.Base` 單獨建置同樣失敗，與本次改動無關。

**關鍵是原本那條版本線沒有修**：通報列出的受影響區間中，`>= 10.0.200, <= 10.0.204` 的
first patched version 是 **None**——留在 10.0.2xx 無論怎麼升都不會過，必須跨 feature band。

處置：`src/Directory.Build.props` 的 `Microsoft.SourceLink.GitHub` **10.0.202 → 10.0.303**
（10.0.3xx 線的第一個修補版；10.0.301 仍在受影響區間內，不可選）。選 10.0.303 而非更新的
10.0.401，是為了對齊本機 SDK 的 feature band（`dotnet --version` = 10.0.300）。

驗證：clean Release build **0 錯誤 0 警告**（不需再繞過 audit）；`Bee.Definition` 的 nuspec
相依**逐字不變**（仍只有 `Bee.Base` 與 `Microsoft.Extensions.Localization.Abstractions`）
——`PrivateAssets="All"` 成立，10.0.303 新帶的 `System.IO.Hashing` 不會流到消費端，
不觸發 `BEE9001`。

## 已確認的設計決定（2026-09-09）

| 項目 | 決定 |
|------|------|
| 方案 | **D —— `WriteXmlSchema` + `WriteXml(DiffGram)`**，純 BCL，不經 `XmlSerializer` |
| 範圍 | 只做 `DataSet` ↔ XML 轉換；不動資料庫層面、不追宣告型別、不改 wire |
| 外層元素名 | `AuditChanges` |
| 提交方式 | 階段 1 + 階段 2 合併為單一 commit |
| 縮排 | 維持縮排，不改 minified |

設計至此定案，可進入實作。
