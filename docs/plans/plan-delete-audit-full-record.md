# 計畫：刪除的異動記錄改存完整原單，異動明細以 DataSet 回傳

**狀態：📝 擬定中**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 儲存形狀：刪除稽核改存完整原單（不再標 Deleted）、讀取端新分支；修正 AfterDelete 讀不到欄位。無 wire 變更 | 📝 待做 |
| 2 | API：`GetChangeDetail` 回應新增三種事件共用的 `DataSet` 屬性；wire 合約、TypeScript 合約、`bee-connector-js` 同步 | 📝 待做 |

## 背景

### 目前 `Form.Delete` 的流程

依 [FormBusinessObject.Write.cs](../../src/Bee.Business/Form/FormBusinessObject.Write.cs) 的 `Delete`：

1. 檢查刪除權限、解析記錄範圍過濾條件。
2. 稽核開啟、或有 BeforeDelete / AfterDelete 外掛、或有 BeforeDelete 規則時，以
   `repository.GetData(rowId, scopeFilter)` 載入**刪除前的原單**（主檔 + 明細），放進
   `context.Snapshot`。`GetData` 最後呼叫 `AcceptChanges()`，所有列都是 Unchanged。
3. `DoBeforeDelete`（BeforeDelete 規則）與 BeforeDelete 外掛。
4. `DoDelete` → [DataFormRepository.Delete](../../src/Bee.Repository/Form/DataFormRepository.cs)：
   同一個交易內 `DELETE 明細 WHERE sys_master_rowid = rowId`，再 `DELETE 主檔 WHERE sys_rowid = rowId`。
   **不使用 DataSet**。
5. 稽核開啟且確實刪到資料時，[FormBusinessObject.Audit.cs](../../src/Bee.Business/Form/FormBusinessObject.Audit.cs)
   的 `WriteDeleteAudit`：`MarkAllRowsDeleted(snapshot)` 對**每一列呼叫 `row.Delete()`**，再
   `GetChanges()` → `AuditDiffGram.Serialize`，全部資料落在 DiffGram 的 `diffgr:before` 區塊。
   沒有原單時只記 `<DeletedRow table=… sys_rowid=… />`。
6. `DoAfterDelete` 與 AfterDelete 外掛，拿到的是**同一個** `context.Snapshot`。

讀取端 [ChangeDiffGramReader](../../src/Bee.Business/AuditLog/ChangeDiffGramReader.cs) 依列狀態還原：
Deleted 列每欄一筆 `ChangeKind.Delete`（`OldValue` 為原值），Unchanged 列略過。
[LogBusinessObject.GetChangeDetail](../../src/Bee.Business/AuditLog/LogBusinessObject.cs) 只回傳這份攤平的欄位清單
（`Fields`），呼叫端看不到主檔與明細的結構。

### 問題

1. **語意不對。** 刪除沒有欄位異動，要記的是「刪掉的那張單長什麼樣子」。現行做法把原單的每一列標成
   Deleted，偽裝成變更集，只為了套用 Save 那條 DiffGram 形狀。
2. **標記作用在共用的 `context.Snapshot` 上。** 稽核開啟時，`DoAfterDelete` 與 AfterDelete 外掛以預設版本
   讀欄位會擲 `DeletedRowInaccessibleException`；稽核關閉時同樣的寫法正常。2026-09-11 以回歸測試實測重現
   （例外在外掛讀 `table.Rows[0][SysFields.Name]` 時擲出，堆疊經 `FormBusinessObject.Delete`）。
   `Delete` 內的註解本身就要求「外掛看到的內容不得取決於稽核開關」。

## 範圍

- 階段 1：只改 `Form.Delete` 的稽核寫入與對應的讀取分支。
- 階段 2：`GetChangeDetail` 回應新增 `DataSet` 屬性。
- 不改：Save 路徑的寫入（它收到的是用戶端送來、帶真實列狀態的 DataSet，本來就是變更集）、
  部署層作業的稽核寫入、既有資料。

## 已確認的決定（2026-09-11）

| 題目 | 決定 |
|------|------|
| 資料區塊寫法 | **DiffGram，列維持 Unchanged**（內嵌 XSD + `WriteXml(DiffGram)`，不 `GetChanges`） |
| 辨識方式 | **新 root 元素**（名稱暫定 `AuditDeletedRecord`）。使用者無偏好，採建議：與 ADR-040 第八節「以 root 區分形狀、payload 自我描述」一致；沿用 `AuditChanges` 則 Save 路徑的整單刪除也是 `Delete` 事件，無法只靠表頭區分 |
| API 輸出 | **新增 `DataSet` 屬性，新增／修改／刪除三種事件共用**：新增為 Added 列、修改為帶原值的 Modified 列、刪除為完整原單（Unchanged 列） |
| `Fields` | **保留**，照舊填入 |
| 已存在的舊記錄 | **不特別處理**：`GetChangeDetail` 目前沒有實際消費者。讀得出內嵌 schema 的舊記錄照原樣回傳（4.30.0 起的舊刪除記錄因此是 Deleted 列），無 schema 的更早記錄 `DataSet` 為 null |
| 降版行為 | **接受並在 CHANGELOG 註明**（見實測） |

## 實測（2026-09-11）

throw-away 探測，**未進 repo**。框架組件為 `7d867913` 的 Release 建置，以反射呼叫 `AuditDiffGram.Serialize`、
`ChangeDiffGramReader.Read` 與其 `ToText`。

原單照 `GetData` 的形狀建立：**DataSet 名稱與主檔表名相同**（`Employee`）、主檔 1 列（含控制字元與 CRLF、
DBNull、decimal、`DataSetDateTime.Unspecified` 的 DateTime、bool）、明細 2 列、另一張空明細表。
寫入沿用第八節的 writer 設定（`CheckCharacters = false`、`NewLineHandling.Entitize`）。

| 寫法 | payload 字元數 | 讀回的列狀態 | 值 | 欄位清單與現行 |
|------|---:|------|------|------|
| 現行：標 Deleted + `GetChanges` + DiffGram | 3,551 | Deleted | — | — |
| 內嵌 XSD + 一般 `WriteXml` | 3,210 | Added | 全數還原 | 逐筆相同 |
| **內嵌 XSD + DiffGram（列為 Unchanged）** ← 採用 | 3,536 | Unchanged | 全數還原 | 逐筆相同 |

「欄位清單與現行」＝把讀回的每一列以讀取端自己的 `ToText` 攤成 `Delete` 欄位，與現行 payload 經目前讀取端
得到的清單排序後比對。無明細列的情境結果相同。

**降版行為**：目前的讀取端讀到新格式，會走舊格式讀取器，把 `xs:schema` 當成資料區塊，產出**一筆無意義的
Update 欄位**（`element` / `complexType`），不是空清單。資料本身完整，回到新版即可讀出。

**wire 帶得動列狀態與原值**（查證程式碼）：MessagePack 的 `SerializableDataRow` 帶 `RowState` 與
`OriginalValues`，JSON 的 `DataTableJsonConverter` 寫出列狀態與 `original`，`messages.d.ts` 的列型別有
`original?`。修改事件的舊值不會在傳輸時遺失。

## 設計

### 階段 1：儲存形狀

**寫入端。** `AuditDiffGram.Serialize` 的 writer 建立與寫出流程改為可指定 root 元素名，新增序列化完整原單的
入口：root 為 `AuditDeletedRecord`，對傳入的 DataSet **直接**寫 XSD 與 DiffGram，不 `GetChanges`。
第八節的字元處理（字元參照、CR、落單 surrogate）因此只有一份設定。

`WriteDeleteAudit` 改為直接序列化 `snapshot`，**刪除 `MarkAllRowsDeleted`**。`WriteXmlSchema` / `WriteXml`
不修改 DataSet，`context.Snapshot` 保持原樣。沒有原單或原單無列時，照舊寫 `DeletedRow` 標記。

**讀取端。** `ChangeDiffGramReader` 的分派加一個 root 分支：以同一個 forward-only reader 依序
`ReadXmlSchema` + `ReadXml(DiffGram)`，對**每一列、不看列狀態**，以 `Current` 版本產出 `ChangeKind.Delete`
（`OldValue` 為值、`NewValue` 為 null），沿用既有的 `AppendSingleVersion`。`Fields` 輸出與現行逐筆相同（實測）。

**`DeleteContext.Snapshot` 的 XML doc** 補上「AfterDelete 看到的是未經修改的刪除前原單，與稽核開關無關」，
並指向執行這個保證的回歸測試。

### 階段 2：`GetChangeDetail` 回傳 DataSet

**讀取端**新增「由 payload 還原 DataSet」的入口，回傳 `DataSet?`。`AuditChanges` 與 `AuditDeletedRecord`
共用同一段 `ReadXmlSchema` + `ReadXml(DiffGram)`，`Fields` 也改由這份讀回的 DataSet 攤平，不重複解析：

| payload | `DataSet` |
|---------|-----------|
| `AuditChanges`（新增、修改，以及 Save 路徑的整單刪除） | 讀回的 DataSet 原樣：Added / Modified / Deleted 列 |
| `AuditDeletedRecord`（階段 1 起的刪除） | 讀回的 DataSet 原樣：Unchanged 列，即完整原單 |
| 無 schema 的舊 DiffGram（4.30.0 前） | null（無法重建 DataSet） |
| `DeletedRow` 標記、空白、損毀 | null |

**回應。** 下列三處新增 `DataSet` 屬性，型別 `DataSet?`，名稱與 `GetDataResponse.DataSet` 一致：

- `Bee.Api.Contracts.AuditLog.IGetChangeDetailResponse`
- `Bee.Business.AuditLog.GetChangeDetailResult`
- `Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse`

呼叫端依列狀態判讀：Added 是新增的值、Modified 同時帶新值與原值、Unchanged（刪除事件）與 Deleted
（舊的刪除記錄、Save 路徑的整單刪除）是被刪掉的內容。事件種類仍由 `ChangeKind` 表達。

**wire。** DataSet 已是既有 wire 型別（`GetDataResponse.DataSet` 同一條路徑），不新增 formatter，只需：

- `src/Bee.Api.Core/MessagePack/WireContracts.AuditLog.cs` 的 `GetChangeDetailResponse` 補一個 `Member`
  （漏補由 `WireContractDriftTests` 擋下）
- 依 `wire-contracts/` 的 README 重新產生 `messages.d.ts`
- `bee-connector-js` 同步：該 repo 的 CI 會因 `messages.d.ts` 變更轉紅，**這是預期的通知機制**，
  需一起安排跟進（見 `.claude/rules/serialization.md`）

## 相容性

- **既有資料一列都不遷移**：4.30.0 起的 `AuditChanges` 與更早的無 schema DiffGram 照讀，沿用 ADR-040 第八節
  「不設落日期限」。
- 降版：見實測。寫進 CHANGELOG。
- 階段 1：變更只在 `internal` 型別與 XML doc，`PublicAPI` 不動，無 wire 變更。
- 階段 2：
  - `IGetChangeDetailResponse` 新增成員，對**在框架外自行實作此介面**的型別是破壞性變更；
    兩個類別新增屬性為增量。版號判定依發版流程處理，commit 時說明相容性判定。
  - `Bee.Api.Contracts` / `Bee.Business` / `Bee.Api.Core` 的 `PublicAPI.Unshipped.txt` 各補一筆。
  - wire 新增成員；舊用戶端收到多出的欄位會忽略（JSON 未知屬性略過、MessagePack 以名稱鍵）。
  - 回應體積增加：每個事件多帶一份 DataSet（含欄位定義）。`GetChangeDetail` 一次只取單一事件，量體可接受。

## 影響的檔案

### 階段 1

**原始碼**

- `src/Bee.Business/AuditLog/AuditDiffGram.cs`：可指定 root 的寫出流程、完整原單入口、類別 remarks
- `src/Bee.Business/AuditLog/ChangeDiffGramReader.cs`：分派、新分支、類別 remarks
- `src/Bee.Business/Form/FormBusinessObject.Audit.cs`：`WriteDeleteAudit`、移除 `MarkAllRowsDeleted`
- `src/Bee.Business/Form/DeleteContext.cs`：`Snapshot` 的 XML doc

**測試**

- 新增：完整原單的寫入 → 讀取往返（主檔 + 明細、空明細表、每種 `FieldDbType`、DBNull、控制字元與 CR）
- 新增：**同一張原單分別以現行寫法與新寫法產生 payload，讀出的欄位清單逐筆相同**。現行寫法在 production
  不再產生，測試內必須自行保留該寫法，否則已存下的舊刪除記錄就失去回歸保護
- 新增：稽核開啟時 AfterDelete 外掛讀得到被刪列的欄位值；稽核開、關兩種情況下 `Snapshot` 的列狀態都是 Unchanged
- 既有、預期不改即通過：`FormBusinessObjectAuditTests` 的兩支 Delete 測試
- 既有、保留：`SchemaBoundChangeReaderTests.Read_DeletedRow_EmitsBeforeImage`、
  `AuditDiffGramCharacterTests.Serialize_DeletedRowWithInvalidCharacters_RestoresBeforeImage`——
  它們驗的是變更集裡的 Deleted 列，Save 路徑與既有資料仍會產生

**公開文件**

- `docs/adr/adr-040-audit-trail-taxonomy.md`：新增一節記錄本決策與理由（語意、共用 `Snapshot` 的副作用）；
  第八節表格「新舊並存」列補上新 root
- `docs/framework-reserved-names.md` / `.zh-TW.md`：`st_log_change` 那列（刪除存完整原單）

### 階段 2

**原始碼**

- `src/Bee.Business/AuditLog/ChangeDiffGramReader.cs`：還原 DataSet 的入口；`Fields` 改由讀回的 DataSet 攤平
- `src/Bee.Business/AuditLog/LogBusinessObject.cs`：`GetChangeDetail` 填入 `DataSet`
- `src/Bee.Business/AuditLog/GetChangeDetailResult.cs`
- `src/Bee.Api.Contracts/AuditLog/IGetChangeDetailResponse.cs`
- `src/Bee.Api.Core/Messages/AuditLog/GetChangeDetailResponse.cs`
- `src/Bee.Api.Core/MessagePack/WireContracts.AuditLog.cs`
- 三個專案的 `PublicAPI.Unshipped.txt`
- `wire-contracts/messages.d.ts`（重新產生）

**測試**

- 各種 payload 的 `DataSet` 還原結果：新增、修改（原值與新值）、新格式刪除（Unchanged 完整原單）、
  Save 路徑整單刪除與舊刪除記錄（Deleted 列）、無 schema 舊記錄與 `DeletedRow` 標記（null）
- `Fields` 在改由 DataSet 攤平後，既有的讀取端測試不改即通過
- `GetChangeDetail` 走 API 的 round-trip（MessagePack 與 JSON codec 各一），確認表、欄型、列狀態與原值
- `-p:DynamicCodeSupport=false` 下 `Bee.Api.Core.UnitTests` 仍零失敗

**公開文件**

- `docs/api-method-reference.md` / `.zh-TW.md`：`GetChangeDetail` 那列
- `IGetChangeDetailResponse` 等 XML doc：說明 `DataSet` 的列狀態判讀與何時為 null
- CHANGELOG：發版時整理（含降版行為、介面新增成員的相容性）

**跨 repo**

- `bee-connector-js`：跟進 `messages.d.ts` 的新欄位

## 相關 plan

- [plan-audit-changes-xml-schema.md](plan-audit-changes-xml-schema.md)：4.30.0 補上內嵌 schema，root 分派的由來
- [plan-audit-changes-json-payload.md](plan-audit-changes-json-payload.md)：評估改存 JSON，結論維持 XML

## 驗證

每階段：`./test.sh` 全綠、clean Release build 0 警告、`./check-public-docs.sh` 無新增違規。
不觸及 provider 與 SQL 產生邏輯，CI 模式於 push 前詢問。
