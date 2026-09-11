# 計畫：`st_log_change` 異動 payload 改存 JSON（持久化採 XML 的例外）

**狀態：✅ 已完成（2026-09-11）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 實測三項指標（XML vs JSON）：持久化 size、DataSet 還原度、序列化效能 | ✅ 已完成（2026-09-11） |
| 1 | 寫入端改 JSON、讀取端加 JSON 分支並凍結兩種 XML 格式、測試、ADR 與公開文件（單一 commit） | 不實作（2026-09-11 決定維持 XML） |

> **結論（2026-09-11）：評估後決定異動記錄維持 XML 持久化，不實作階段 1。**
> 異動記錄的查看需求是「哪些欄位從什麼值改成什麼值」，兩種格式在這點上等價（見「實測結果」的還原度）；
> JSON 在體積與效能的優勢不構成改格式的理由（異動記錄一次只處理單筆表單資料；log 資料庫與業務庫分離，
> 框架目前固定寫入單一 `log` 資料庫，量體可由部署端分庫或封存緩解；`DataSet` XML 的做法維護者已實務使用十年以上），
> 反而要付出自訂編碼器、`AllowUnsafeBlocks`、
> 持久化依賴 wire 格式、讀取端多一個分支的代價。也不需要為 `DateOnly` / `TimeOnly` 做特別處理。
> XML 路徑唯一的實際缺口（控制字元與 NUL 讓序列化擲例外）另案修正。
> 決策**待記入** ADR-040 第九節（草稿已備妥；修正控制字元的另一項工作正在改同一份 ADR 且尚未 commit，
> 等它 commit 後再補，避免兩項改動混進同一個 commit）。本 plan 保留作為評估與實測紀錄，以下「設計」「待決事項」「影響的檔案」皆未執行。
>
> 階段 1 必須**原子完成**，理由與 [plan-audit-changes-xml-schema.md](plan-audit-changes-xml-schema.md)
> 相同：寫入端換了而讀取端還沒認得新格式，異動明細會直接讀不出來。

## 背景

框架的持久化目前一律走 XML（`bee-serialization` skill 的「XML＝持久化軸」）。
異動記錄 `st_log_change.changes_xml` 自 4.30.0 起存「內嵌 XSD + DataSet DiffGram」。

2026-09-11 決定：**`DataSet` 的持久化作為例外，改用 JSON**，沿用傳輸序列化已有的
`DataSetJsonConverter` / `DataTableJsonConverter`。起因是 2026 鐵人賽 Day 27 改為介紹
傳輸序列化對 `DataSet` 的處理，異動記錄的序列化另立本 plan。

## 決策理由（查證後）

提出時的三項理由，查證結果如下。**決策不變，但寫進 ADR 的理由要用成立的那幾條。**

| 理由 | 查證結果 |
|------|---------|
| JSON 有特別處理 `DateOnly` / `TimeOnly` | **對 `DataSet` 持久化不成立，不可寫進 ADR。** 框架的 JSON 確實特別處理 `DateOnly`，但位置在 `Bee.Api.Core` 的 `WireValueJsonConverter`，只作用於 `object` 成員（`Parameter.Value`、`FilterCondition.Value`；以判別碼 17 加 `"2026-03-14"` 寫出，由 `wire-fixtures/bodies/value-dateonly.json` 釘住）；`TimeOnly` 沒有任何處理，也不在 wire 白名單。`JsonCodec` 未註冊該 converter，`DataTableJsonConverter` 的儲存格不經過它，且 `DataColumn` 從不承載 `DateOnly` / `TimeOnly`（ADR-031、ADR-033）；`DataTableJsonConverter` 寫 Date 欄的儲存格是 `"2026-07-27T00:00:00"`、Time 欄是 `"08:30"`，與 XML 相同。JSON 多的是欄位上的 `type` 標記，但 XML 的內嵌 XSD 同樣寫出 `msprop` 標記，且自 `8da8b818` 起讀回也會被採用。另外 ADR-040 §8 明訂 payload「不追宣告型別」，本案不推翻該條 |
| 序列化體積理應小很多 | **成立，但幅度要說清楚。** 階段 0 實測 compact JSON 為現行 payload 的 54%～70%；其中一部分來自拿掉縮排，與同為 minified 的 XML 相比是 67%～82%。明細列越多省越多。詳見「實測結果」 |
| （階段 0 補）DataSet 還原度 | **表單路徑平手，邊界輸入 JSON 較穩。** 全部 `FieldDbType`（含極值、DBNull、改／刪／增）兩者皆完全還原；XML 會把 CR／CRLF 靜默改成 LF，遇控制字元與 NUL 直接擲例外。JSON 的弱點是 `FieldDbType` 以外的 CLR 型別，而表單路徑不會出現 |
| （階段 0 補）序列化效能 | **成立。** JSON 序列化時間為 XML 的 24%～30%、配置 15%～22%；還原時間 37%～82%（修改列越多差距越小） |
| 與傳輸共用同一套 `DataSet` 表示 | **成立。** converter 已帶 `tableName`、欄位與 `type`、主鍵、`RowState`、`current` / `original` 兩版本——讀取端需要的全部都在；且形狀已被 `wire-fixtures/bodies/dataset.json` / `datatable.json` 的黃金樣本釘住 |

> **異動記錄不需要為 `DateOnly` / `TimeOnly` 做特別處理**（2026-09-11 確認）。查看需求是「哪些欄位從什麼值改成什麼值」，
> 只要 `DataSet` 能正常還原就滿足，階段 0 實測兩種格式皆成立（Date 欄的值與標記、Time 欄的字串皆完整還原）。
> 日期欄顯示成 `2026-07-27T00:00:00` 或 `2026-07-27`、時間點欄的時區呈現，屬顯示層的事，依 `FormSchema` 的欄位型別處理；
> payload 本身也帶著欄位標記，日後真有需要，讀取端可從還原的 `DataSet` 取得，不必預先為此改格式。

另有兩項查證後成立、可補進 ADR 的理由：

- **讀取端變單純**：不必再「一個 forward-only reader 依序餵 `ReadXmlSchema` + `ReadXml(DiffGram)`」。
  4.30.0 實作時踩到的兩個縮排才會出現的 bug（子樹 reader 走出尾端、`ReadStartElement` 後落在空白節點）
  都屬於那條路徑。
- **少一個 XML 解析面**：JSON 分支沒有 DTD / 外部實體，XXE 加固不再是該分支要記得做的事。

## 設計

### 1. 寫入端

`AuditDiffGram.Serialize(DataSet)` 改輸出 JSON。`ForFieldUpdate` / `ForInsert` 走同一支，
自動跟著換，維持單一 payload 形狀。

**不直接用 `JsonCodec.Serialize`**，理由見第 4 點；改用本案專屬的 `JsonSerializerOptions`，
converter 清單與 `JsonCodec` 相同（`DataTableJsonConverter`、`DataSetJsonConverter`）。

### 2. 讀取端：以第一個非空白字元分派

| payload | 判別 | 分支 |
|---------|------|------|
| **新格式 JSON** | 首字元 `{` | 反序列化成 `DataSet` → 既有 `DataRow` walk |
| 內嵌 XSD（4.30.0～本案前） | root `AuditChanges` | 現行 `ReadSchemaBound`，**凍結** |
| 無 schema DiffGram（4.30.0 前） | root `diffgr:diffgram` | `SchemalessDiffGramReader`，**凍結** |
| 最小刪除標記 | root `DeletedRow` | 回空清單（現行行為） |
| 空字串 / 損毀 | — | 回空清單（現行行為） |

`DataRow` walk（`AppendRow` / `AppendSingleVersion` / `AppendModified`）由 XSD 與 JSON 兩個分支
**共用同一份**，不複製。語意不變：`Added` 逐欄 `Current`、`Deleted` 逐欄 `Original`、
`Modified` 只輸出兩版本不同的欄，皆跳過 `sys_rowid`。

JSON 損毀時捕捉 `JsonException` 回空清單，與 XML 分支對損毀 payload 的處理一致。

### 3. 值的字串化沿用 `ToText`（`XmlConvert`）

同一筆邏輯異動不論存成哪種格式，`RecordFieldChange` 的字串必須相同。JSON 讀回的值型別
（`DateTime` 無時區、`decimal` 由引號字串還原、`bool`、`Guid`）交給現行 `ToText` 即得與 XML
格式逐字相同的輸出。**以跨格式一致性測試釘住**（見驗證）。

### 4. ⚠️ 編碼：UTF-8 原字儲存（2026-09-11 定案），不可沿用 `JsonCodec` 的預設編碼器

`JsonCodec` 沒有設定 `Encoder`，採 System.Text.Json 預設的 `JavaScriptEncoder.Default`
（`wire-fixtures/bodies/` 裡連雙引號與反引號都寫成 `\u` 加四位十六進位的跳脫形式，即此編碼器的行為）。該編碼器會把**非 ASCII
字元一律跳脫為 `\uXXXX`**：中文 caption 與資料每字從 UTF-8 的 3 bytes 變成 6 個 ASCII 字元，
人工查閱時也讀不出內容。

階段 0 實測，預設編碼器比 UTF-8 原字多 3%～10%（修改列同時寫新舊兩版、名稱出現兩次，所以改價情境最高）。

**定案：JSON 以 UTF-8 原字儲存**，非 ASCII 字元（含 BMP 以外的 emoji、CJK 擴充 B 區字）一律不跳脫。

- **內建編碼器不夠**：`JavaScriptEncoder.Create(UnicodeRanges.All)` 與 `UnsafeRelaxedJsonEscaping` 都只放行 BMP，
  emoji 與擴充 B 區字（例如「𠮷」）仍被寫成 surrogate pair 的跳脫形式（階段 0 實測）。
  因此需要**自訂 `JavaScriptEncoder`**：只跳脫 RFC 8259 要求的字元（雙引號、反斜線、U+0000～U+001F）與落單 surrogate。
- **跳脫不是亂碼**：實測三種內建編碼器的輸出都能無損讀回。會不會亂碼取決於資料庫欄位的字元集，
  存原字的前提是 `Text` 欄為 Unicode：SQL Server `nvarchar(max)`、MySQL 表級 `utf8mb4` 定序、SQLite 皆成立；
  **PostgreSQL 的 `text` 與 Oracle 的 `CLOB` 依資料庫字元集而定**，非 UTF-8（Oracle 非 `AL32UTF8`）的部署會出錯。
  這與其他中文欄位的前提相同，屬部署文件要寫明的事，不靠 payload 迴避。
- **代價**：自訂編碼器必須覆寫的 `FindFirstCharacterToEncode(char*, int)` 是指標簽章，`Bee.Business` 需開
  `AllowUnsafeBlocks`（repo 目前沒有任何專案開啟，屬需要審視的變更）；序列化比 `UnicodeRanges.All` 慢約 8%～11%
  （逐字元掃描，內建編碼器有向量化），仍只有 XML 的 24%～30%。
- **落單 surrogate** 寫成跳脫、讀回變 U+FFFD：這種字串本來就無法以 UTF-8 儲存；XML 則直接擲例外。
- 是否額外跳脫 HTML 敏感字元（`<`、`>`、`&`、單引號）：payload 只在伺服端解析、不直接輸出成 HTML，
  實測版不跳脫；實作時確認安全掃描是否標記。
- 既有測試 `FormBusinessObjectAuditTests` 對 `"稽核待刪"` 做 `Contains` 斷言，採預設編碼器會紅
  ——這正是該設定的回歸保護，不要改成比對跳脫後字串。

### 5. 支援的欄位型別收窄為 `FieldDbType` 集合

`DataTableJsonConverter` 寫欄位時呼叫 `ResolveFieldDbType`，未標記且 CLR 型別無對應
`FieldDbType` 的欄（`TimeSpan`、`DateTimeOffset`、`object`…）會擲 `InvalidOperationException`；
XML DiffGram 則照寫不誤。

目前的寫入來源都在集合內：表單路徑的 `DataSet` 由 `AddColumn` / `BuildEmptyDataTable` 依
`FormSchema` 建立，系統路徑的 `ForFieldUpdate` / `ForInsert` 一律 `string` 欄。
**不加 XML 退路**（那會讓「新寫入一定是 JSON」不再成立），改以測試涵蓋每一種 `FieldDbType`
的欄位 round-trip。

階段 0 已實測：全部 `FieldDbType`（含 DBNull 與極值）完全還原；集合外的型別則 `TimeSpan`、`DateTimeOffset`
擲 `InvalidOperationException`，`double` 讀回變 `decimal`、`char` 變 `string`，自建 `DateTime` 欄的
`DateTimeMode` 由 `UnspecifiedLocal` 變 `Unspecified`。

### 6. 格式耦合：持久化資料依賴 wire 的 JSON 形狀

存下來的 payload 從此依賴 `DataTableJsonConverter` 的格式，日後改 wire 的 DataTable 形狀會
影響既有稽核列能否讀回。現況：

- `wire-fixtures/bodies/dataset.json` / `datatable.json` 由 `WireFixtureTests` 釘住，改形狀會紅。
- 讀取端對未知屬性 `Skip()`，新增欄位不影響舊資料。

補一條稽核專屬的保護：測試內放一份**凍結的 JSON payload 字面值**，必須持續讀出預期的欄位清單。
wire 形狀變更時，這條測試讓「既有稽核資料能否讀回」成為必須正視的問題，而不只是 wire 的事。

### 7. 相容性

- **既有資料一列都不遷移**，兩種 XML 格式不設落日期限（沿用 ADR-040 §8 的規則）。
- **降版**：退回本案之前的版本時，JSON 列會被舊讀取端當成損毀 payload，`GetChangeDetail` 的欄位清單為空；
  資料本身仍在，回到新版即可讀出。寫進 changelog。
- **wire 不變**：`RecordFieldChange` 與 API 形狀不動，`wire-contracts/`、`bee-connector-js`、
  行動 / WASM head 不受影響（`Bee.Business` 只在伺服端）。

## 實測結果（階段 0，2026-09-11）

環境：.NET 10.0.8（SDK 10.0.300）、macOS、Apple M5 Max；框架組件為 `8da8b818` 的 Release 建置。
比較程式為 throw-away，**未進 repo**。

### 總表

| 指標 | XML（XSD + DiffGram，現行） | JSON（compact + UTF-8 原字） | 判定 |
|---|---|---|---|
| 持久化 size | 基準 | 現行的 54%～70%（與 minified XML 比 67%～82%） | JSON 勝，其中一部分是拿掉縮排 |
| 還原度：表單路徑（全部 `FieldDbType`、極值、DBNull、改／刪／增、多語系） | 完全還原 | 完全還原 | 平手 |
| 還原度：CR／CRLF | 靜默改成 LF | 完全還原 | 不影響判斷：異動記錄關注的是哪些欄位被變更，換行格式不是關注點（2026-09-11 確認）。唯一例外是某欄只改了換行格式時，XML 讀回兩版相同、該欄不會列入異動 |
| 還原度：控制字元、NUL | **序列化擲例外** | 完全還原 | JSON 勝 |
| 還原度：落單 surrogate | 序列化擲例外 | 讀回變 U+FFFD | 皆有損 |
| 還原度：`FieldDbType` 以外的 CLR 型別 | 完全還原 | `TimeSpan`／`DateTimeOffset` 擲例外；`double`→`decimal`、`char`→`string` | XML 勝（表單路徑不會出現） |
| 稽核讀取端輸出（`RecordFieldChange`） | 基準 | 所有情境逐筆相同 | 平手 |
| 序列化效能 | 基準 | 時間 24%～30%、配置 15%～22% | JSON 勝 |
| 還原效能 | 基準 | 時間 37%～82%、配置 36%～81% | JSON 勝，修改列越多差距越小 |

### 方法

- **資料**：Northwind `Order`（主檔 16 欄、明細 10 欄），建表照抄 `DataFormRepository.BuildEmptyDataTable`
  （逐欄 `AddColumn`、略過 `VirtualField`），`DataSet` 名稱與表順序同 `GetNewData`。資料分英文、中文、
  多語系（日文、韓文、希臘文、emoji、CJK 擴充 B 區字）三組。另建一張「每種 `FieldDbType` 一欄」的表，
  含 DBNull 與極值（`decimal.MaxValue`、28 位小數、超過 2 的 53 次方的 `long`、100 奈秒精度的 `DateTime`、`byte[]`、5,000 字長文字）。
- **校準**：XML 複製寫法＝`AuditDiffGram.Serialize`、JSON 預設 options＝`JsonCodec.Serialize`、
  複製的部署層 `DataSet`＝`AuditDiffGram.ForFieldUpdate`，三項逐字相符。
- **UTF-8 原字**：內建編碼器不放行 BMP 以外的字元（見設計第 4 點），以自訂 `JavaScriptEncoder` 實測。
- **還原度**：原始與還原後的 `DataSet` 逐項比對——DataSet 名稱、表順序、主鍵；每欄 CLR 型別、`FieldDbType` 標記、
  `AllowDBNull`、`MaxLength`、`ReadOnly`、`Caption`、`AutoIncrement`、`DefaultValue`、`DateTimeMode`；
  每列 `RowState`、`RowError`、各版本的值（含型別與 `DateTimeKind`）。另以真實讀取端比對 `RecordFieldChange` 輸出。
- **效能**：BenchmarkDotNet 0.15.2（warmup 4、iteration 12、`MemoryDiagnoser`），中文資料。序列化＝`DataSet` → 字串；
  還原＝字串 → `DataSet`（XML 照抄 `ChangeDiffGramReader.ReadSchemaBound` 的讀法）。讀取端的 `DataRow` walk 兩種格式共用，不計入。

### 1. 持久化 size（UTF-8 bytes）

| 情境 | 資料 | XML 縮排（現行） | XML minified | JSON UTF-8 | ÷ 現行 | ÷ XML minified | 參考：JSON 縮排 UTF-8 | 參考：JSON 預設編碼 |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 編輯訂單 5 行明細（表頭改 3 欄；明細改 1／刪 1／增 1） | 英文 | 9,401 | 7,996 | **6,399** | 68% | 80% | 10,445 | 6,399 |
| 同上 | 中文 | 9,505 | 8,100 | **6,503** | 68% | 80% | 10,549 | 6,767 |
| 同上 | 多語系 | 9,539 | 8,134 | **6,537** | 69% | 80% | 10,583 | 6,963 |
| 新增訂單 + 5 行明細 | 英文 | 9,102 | 7,761 | **6,192** | 68% | 80% | 10,214 | 6,192 |
| 同上 | 中文 | 9,211 | 7,870 | **6,301** | 68% | 80% | 10,323 | 6,541 |
| 同上 | 多語系 | 9,198 | 7,857 | **6,288** | 68% | 80% | 10,310 | 6,612 |
| 新增訂單 + 20 行明細 | 英文 | 18,475 | 15,574 | **11,419** | 62% | 73% | 18,336 | 11,419 |
| 同上 | 中文 | 18,869 | 15,968 | **11,813** | 63% | 74% | 18,730 | 12,593 |
| 同上 | 多語系 | 18,706 | 15,805 | **11,650** | 62% | 74% | 18,567 | 12,529 |
| 全單改價，20 行全改 | 英文 | 31,353 | 26,204 | **18,662** | 60% | 71% | 29,086 | 18,662 |
| 同上 | 中文 | 32,141 | 26,992 | **19,450** | 61% | 72% | 29,874 | 21,010 |
| 同上 | 多語系 | 31,815 | 26,666 | **19,124** | 60% | 72% | 29,548 | 20,882 |
| 全單改價，50 行全改 | 英文 | 68,044 | 56,655 | **39,183** | 58% | 69% | 60,287 | 39,183 |
| 同上 | 中文 | 69,972 | 58,583 | **41,111** | 59% | 70% | 62,215 | 44,831 |
| 同上 | 多語系 | 69,046 | 57,657 | **40,185** | 58% | 70% | 61,289 | 44,163 |
| 刪除訂單（快照 1 + 5 行全刪） | 英文 | 8,958 | 7,612 | **6,210** | 69% | 82% | 10,232 | 6,210 |
| 同上 | 中文 | 9,067 | 7,721 | **6,319** | 70% | 82% | 10,341 | 6,559 |
| 同上 | 多語系 | 9,054 | 7,708 | **6,306** | 70% | 82% | 10,328 | 6,630 |
| 部署層 `ForFieldUpdate`（3 欄 1 列） | 英文 | 1,727 | 1,406 | **943** | 55% | 67% | 1,611 | 943 |
| 同上 | 中文 | 1,717 | 1,396 | **933** | 54% | 67% | 1,601 | 963 |
| 同上 | 多語系 | 1,745 | 1,424 | **961** | 55% | 67% | 1,629 | 1,025 |

**SQL Server 的實際儲存**：`Text` 對應 `nvarchar(max)`，以 UTF-16 儲存；以字元數比，JSON 為現行的 54%～69%，比例與 UTF-8 幾乎相同。

### 2. DataSet 還原度

| 案例 | XML | JSON（UTF-8 原字） |
|---|---|---|
| 訂單異動（`GetChanges`，多語系） | ✅ 完全一致 | ✅ 完全一致 |
| 訂單完整 `DataSet`（含 Unchanged 列，多語系） | ✅ 完全一致 | ✅ 完全一致 |
| 全 `FieldDbType` 表（`GetChanges`：改／刪／增、DBNull、極值） | ✅ 完全一致 | ✅ 完全一致 |
| 全 `FieldDbType` 表（完整 `DataSet`） | ✅ 完全一致 | ✅ 完全一致 |
| 字串含 CRLF | ❌ 讀回變 LF | ✅ |
| 字串含 CR | ❌ 讀回變 LF | ✅ |
| 前後空白、tab、`<xml>&amp;` 與引號、emoji 與擴充 B 區字、空字串 | ✅ | ✅ |
| 字串含控制字元 U+0001 | 💥 `ArgumentException`（XML 不合法字元） | ✅ |
| 字串含 NUL | 💥 `ArgumentException` | ✅ |
| 字串含落單 surrogate | 💥 `ArgumentException` | ❌ 讀回變 U+FFFD |
| `TimeSpan` 欄 | ✅ | 💥 `InvalidOperationException` |
| `DateTimeOffset` 欄 | ✅ | 💥 `InvalidOperationException` |
| `double` 欄 | ✅ | ❌ 型別變 `decimal` |
| `char` 欄 | ✅ | ❌ 型別變 `string` |
| 自建 `DateTime` 欄（`UnspecifiedLocal`） | ✅ | ❌ `DateTimeMode` 變 `Unspecified` |

稽核讀取端層級：七個訂單與部署層情境 × 三組資料、以及全 `FieldDbType` 表，`RecordFieldChange` 清單全部逐筆相同。

### 3. 序列化效能（中文資料，微秒／次）

| 情境 | 動作 | XML 時間 | JSON UTF-8 時間 | 比 | XML 配置 | JSON UTF-8 配置 | 比 | 參考：JSON `UnicodeRanges.All` 時間 |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 編輯 5 行明細 | 序列化 | 29.99 | 7.20 | 24% | 99.2 KB | 15.1 KB | 15% | 6.49 |
| 新增 + 20 行 | 序列化 | 50.46 | 14.19 | 28% | 165.2 KB | 29.6 KB | 18% | 13.03 |
| 改價 50 行 | 序列化 | 172.44 | 51.06 | 30% | 497.9 KB | 107.0 KB | 22% | 47.15 |
| 編輯 5 行明細 | 還原 | 88.54 | 32.58 | 37% | 245.6 KB | 87.6 KB | 36% | 32.66 |
| 新增 + 20 行 | 還原 | 109.81 | 48.32 | 44% | 271.5 KB | 125.5 KB | 46% | 48.32 |
| 改價 50 行 | 還原 | 240.18 | 197.18 | 82% | 394.9 KB | 321.6 KB | 81% | 195.85 |

XML「改價 50 行」序列化的標準差較大（11.5 微秒），且配置已大到觸發 Gen2 回收（大型物件堆積）。

### 判讀

1. **size**：JSON 為現行的 54%～70%，但與同為 minified 的 XML 比是 67%～82%——**省下的有一部分是縮排，
   不是 JSON 本身**，寫進 ADR 時要照實寫。明細列越多省越多（XSD 是固定成本，DiffGram 每個值都要開關標籤）。
2. **還原度**：表單路徑兩者都完整。差異只在邊界輸入，且方向相反——XML 對控制字元
   與 NUL 直接擲例外；JSON 則收窄了支援的 CLR 型別。**表單路徑的欄位型別都在 `FieldDbType` 集合內，
   所以 JSON 的弱點不會發生在稽核寫入；XML 的弱點會**——使用者貼上的文字就可能帶控制字元。
3. **效能**：序列化快 3～4 倍、配置少 5～7 倍。還原在修改列多時優勢縮小（改價 50 行只快 18%），
   推測是 `DataTableJsonConverter` 重建 Modified 列時要寫兩版值並逐列 `AcceptChanges`（未剖析）。
   稽核寫入走序列化、讀明細走還原，兩者都不慢於現行。
4. **UTF-8 原字的代價**：自訂編碼器序列化比 `UnicodeRanges.All` 慢約 8%～11%、需開 `AllowUnsafeBlocks`；
   體積只在 BMP 以外的字元有差（多語系資料約 1%～4%）。見設計第 4 點。
5. **順帶發現的現行缺陷**：控制字元讓 `AuditDiffGram.Serialize` 擲例外，而它在 `FormBusinessObject.Save` 中
   位於資料庫交易 commit 之後、`DoAfterSave` 之前，且沒有 try/catch——資料已存、API 回報失敗、稽核漏記、
   AfterSave 被跳過。已另開任務處理。改用 JSON 只解掉這個觸發條件，「序列化例外發生在 commit 之後」
   的結構問題與格式無關，不應等本 plan 落地。
6. **未涵蓋**：資料庫實際寫入與讀取的時間（本次只量序列化）；PostgreSQL 與 Oracle 在非 UTF-8 資料庫字元集下的行為。

## 待決事項

> 決定維持 XML 之後，以下各項不再需要決定，保留作為評估當時的判斷紀錄。

| # | 題目 | 選項 | 建議 |
|---|------|------|------|
| 1 | 欄名 `changes_xml` 與 `ChangeAuditEntry.ChangesXml` | 全部保留／只改 C# 屬性／欄名與屬性一起改（`OriginalFieldName` → 保留資料的改名） | **全部保留**，XML doc 與保留名清單註明「名稱沿用歷史，內容自本版起為 JSON」。欄名改名會對每個部署的 log 資料庫做 ALTER，且機制不支援跨多版本連續改名，跳版升級的部署會把歷史明細讀成空的；屬性改名是破壞性變更 |
| 2 | 無 snapshot 的刪除標記 `<DeletedRow …/>` | 改成 JSON（合成只含 `sys_rowid` 的已刪除列）／維持 XML 字串 | **改 JSON**：新寫入的格式才真正單一；讀取端走同一條 walk 自然得到空欄位清單；順帶去掉手寫 XML 字串插值 |
| 3 | 縮排 | compact／縮排 | **compact**：本案理由之一是體積，而實測縮排的 JSON 在小異動上**比現行縮排 XML 還大**（編輯 5 行明細 10,445 vs 9,401 bytes），明細列多時也只略小——選縮排等於放棄體積理由。4.30.0 選縮排是為了人工查閱，JSON 以工具格式化即可。此為推翻 4.30.0 已確認的決定，需明確同意 |
| 4 | 決策紀錄位置 | ADR-040 新增第九節／另立 ADR | **ADR-040 第九節**：與第五、八節同屬 payload 格式的演進，拆出去讀者要跨檔拼湊 |

內部型別改名不列入待決，**一併做**：`AuditDiffGram` 已不再輸出 DiffGram，改名為
`AuditChangePayload`；`ChangeDiffGramReader` 改名為 `ChangePayloadReader`。兩者皆 `internal`，
無相容性成本。`SchemalessDiffGramReader` 名稱正確（它讀的就是 DiffGram），不改。

## 影響的檔案

**原始碼**

- `src/Bee.Business/AuditLog/AuditDiffGram.cs` → `AuditChangePayload.cs`：寫入改 JSON、專屬 options、類別 remarks
- `src/Bee.Business/AuditLog/ChangeDiffGramReader.cs` → `ChangePayloadReader.cs`：首字元分派、JSON 分支、共用 walk、remarks
- 新增 UTF-8 原字的自訂 `JavaScriptEncoder`（設計第 4 點）；`src/Bee.Business/Bee.Business.csproj` 開 `AllowUnsafeBlocks`
- `src/Bee.Business/AuditLog/SchemalessDiffGramReader.cs`：remarks 內指向寫入端的 cref
- `src/Bee.Business/Form/FormBusinessObject.Audit.cs`：刪除標記（待決 2）、區域變數名
- 呼叫端（改名連動）：`FormBusinessObject.Write.cs`、`SystemBusinessObject.Plugin.cs`、
  `SystemBusinessObject.ApiKey.cs`、`SystemBusinessObject.DeploymentAdmin.cs`、`LogBusinessObject.cs`

**XML doc（公開文件，隨 NuGet 發佈）**

- `src/Bee.Api.Contracts/AuditLog/RecordFieldChange.cs`、`IGetChangeDetailRequest.cs`、`IGetChangeDetailResponse.cs`
- `src/Bee.Repository.Abstractions/AuditLog/IAuditLogRepository.cs`
- `src/Bee.Definition/Logging/ChangeAuditEntry.cs`

**測試**

- `tests/Bee.Business.UnitTests/AuditLog/SchemaBoundChangeReaderTests.cs`：⚠️ 目前以
  `AuditDiffGram.Serialize` **現場產生** XSD payload。寫入端換成 JSON 後這組測試會靜默改測 JSON，
  已存下的 XSD 列就失去回歸保護。**必須在測試內保留 4.30.0 的寫法**（`WriteXmlSchema` + `WriteXml(DiffGram)`）產生 payload
- `tests/Bee.Business.UnitTests/AuditLog/ChangeDiffGramReaderCoverageTests.cs`：無 schema 格式，內容不動（改名連動）
- 新增 JSON 分支測試：三種 `RowState`、`Modified` 只吐差異欄、`Deleted` before-image、損毀 JSON 回空清單、
  每種 `FieldDbType` 欄位、凍結 payload 字面值、**同一 `DataSet` 分別寫成 XSD 格式與 JSON 格式讀出的
  `RecordFieldChange` 清單完全相同**
- `tests/Bee.Business.UnitTests/Form/FormBusinessObjectAuditTests.cs`、`SystemBusinessObjectDeploymentAuditTests.cs`、
  `SystemBusinessObjectApiKeyLifecycleTests.cs`：走真正的寫入→讀取 round-trip，預期不改即通過
- `tests/Bee.Definition.UnitTests/Logging/AuditLoggingTests.cs` 的 `DiffGram_PreservesOldAndNewValues`：
  直接測 BCL DiffGram、標題寫著「changes_xml 設計核心」，改完後不再描述框架行為 → 改寫為 JSON 版或移除
- `tests/Bee.Hosting.UnitTests/AuditLog*DbFactTests.cs` 寫入的 diffgram 字串是不透明值、從不解析 → 不改

**公開文件**

- `docs/adr/adr-040-audit-trail-taxonomy.md`：新增第九節（待決 4）；第五節加註；「後果」的「需解析 XML」
- `docs/framework-reserved-names.md` / `.zh-TW.md`：`st_log_change` 那列
- `docs/api-method-reference.md` / `.zh-TW.md`：`GetChangeDetail` 那列
- CHANGELOG：發版時由發版流程整理（含降版行為）

**agent 規範**

- `.claude/skills/bee-serialization/SKILL.md`：「XML＝持久化軸」表格加註本例外，指向 ADR-040

**不受影響（已查證）**

- wire 形狀、`wire-contracts/`、`bee-connector-js`、行動 / WASM head
- `st_log_change.TableSchema.xml`（三份）：`DbType="Text"`，待決 1 採保留時不改

## 驗證

**階段 0**：✅ 2026-09-11 完成，方法、數字與判讀見「實測結果」。

**階段 1**：`./test.sh` 全綠、clean Release build 0 警告、`./check-public-docs.sh` 無新增違規。
改動不觸及 provider 與 SQL 產生邏輯，CI 模式於 push 前詢問。
