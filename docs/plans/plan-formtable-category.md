# 計畫：FormSchema 加上 CategoryId 欄位，驅動 TableSchema 自動分類

**狀態：📝 擬定中**

## 背景

目前 FormSchema → TableSchema 的推導鏈缺少「Category 歸屬」資訊：

| 元件 | 是否帶 Category |
|------|-----------------|
| `FormSchema` (`src/Bee.Definition/Forms/FormSchema.cs`) | ❌ 無 |
| `FormTable` (`src/Bee.Definition/Forms/FormTable.cs`) | ❌ 無 |
| `TableSchema` (`src/Bee.Definition/Database/TableSchema.cs`) | ❌ 無 |
| `TableSchemaGenerator.Generate(FormTable)` | ❌ 不知道該丟哪個 category |
| `IDefineAccess.SaveTableSchema(string categoryId, TableSchema)` | ✅ 由呼叫端外部傳入 |
| `DbCategorySettings.xml` (`<DbCategory><Tables><TableItem .../>...`) | ✅ 用獨立檔案手動列舉表名 |
| 檔案路徑 `TableSchema/{categoryId}/{tableName}.TableSchema.xml` | ✅ 由 categoryId 決定子目錄 |

換句話說：FormSchema 承載了「ProgId / Tables / Fields / Indexes」等推導 TableSchema 必要資訊，唯獨「該歸屬哪個 DbCategory」需在 `DbCategorySettings.xml` 用表名重複手動維護一次，造成兩處事實來源容易不同步：
- 新增 FormSchema 時，得再記得去 DbCategorySettings 追加一筆 `<TableItem>`
- 重構移動表的歸類時，FormSchema 那側完全察覺不到
- `TableSchemaGenerator` 無法決定產出檔案要寫入哪個 category 子目錄，必須由上層工具/UI 補餵 `categoryId`

CLAUDE.md 中明示「FormSchema 為定義中樞，同時驅動 UI、資料庫與驗證規則」；目前 CategoryId 是這條原則少數的例外。

## 目標

讓 FormSchema 自帶 CategoryId 歸屬，使「FormSchema 即可決定其下所有 TableSchema 的存放分類」，並讓 `DbCategorySettings.xml` 從手動同步表退化為「Category 主檔（DisplayName / 連線字串對應）」。

具體：
1. `FormSchema` 新增 `CategoryId` 屬性（`XmlAttribute`），預設 `"common"`；同一 FormSchema 下的所有 FormTable 共用此 CategoryId
2. `TableSchemaGenerator.Generate(...)` 行為不變（仍只產 TableSchema 內容），但提供新 helper `TableSchemaGenerator.GetCategoryId(formSchema)` 給呼叫端決定落點
3. `LocalDefineAccess.SaveFormSchema(...)` 在落地 FormSchema 時，依 FormSchema 的 CategoryId 同步維護 `DbCategorySettings.xml` 中的 `<TableItem>` 清單（新增/移動/刪除）
4. `DbCategorySettings` 改為「Category 主檔」：仍保留 `Id` / `DisplayName`，但 `<Tables>` 子節點轉為由 SaveFormSchema 自動寫回，不再由人工編輯
5. 既有 fixture 與測試遷移，所有現存 FormSchema 預設標 `CategoryId="common"`（與目前 fixture 既有歸類一致）

## 設計決策

### D1. CategoryId 放在 `FormTable` 還是 `FormSchema`？— ✅ 已定：FormSchema

選擇放 `FormSchema`：
- 同一 FormSchema 內的 Master / Detail tables 通常屬於同一 Category（同一個業務領域、同一組連線）
- 若真有 Master/Detail 跨 DB 的情境，可拆成兩個 FormSchema 各自指定 CategoryId，比放 FormTable 更乾淨
- 屬性集中在 FormSchema 比散在每個 FormTable 易讀、難寫錯

### D2. 預設值 — 待確認

- **方案 A（建議）**：`CategoryId` 預設為 `"common"`，與既有 fixture 與 `DbCategorySettings.xml` 中的 `Id="common"` 一致
- 方案 B：必填，無預設。但這會讓所有現存 FormSchema fixture 都得補欄位，遷移成本高

### D3. `DbCategorySettings.xml` 的 `<Tables>` 子節點是否仍保留人工編輯？— 待確認

- **方案 A（建議）**：`SaveFormSchema` 會回寫 `<TableItem>`，不鼓勵人工編輯。僅作為「依 FormSchema 推導出來的索引快照」
- 方案 B：保留現狀，兩處都允許編輯。但這就回到「兩處事實來源」原問題

## 影響範圍盤點

> 僅列源碼與固定資源；`bin/`、`obj/`、`TestResults/` 屬 build artefact。

### A. 新增/修改類別

`src/Bee.Definition/Forms/FormSchema.cs`：
- 新增 `[XmlAttribute] public string CategoryId { get; set; } = "common";`
- `[Category(PropertyCategories.Data)]` / `[Description("Database category id.")]` 等 metadata 對齊既有屬性（如 `DisplayName`、`ListFields`）
- 注意：`Category` 此處指 `System.ComponentModel.CategoryAttribute`（屬性瀏覽器分組用），與本計畫 `CategoryId` 屬性語意不同，不會衝突

`src/Bee.Definition/Database/TableSchemaGenerator.cs`：
- 新增 `public static string GetCategoryId(FormSchema formSchema)` helper（單純回傳 `formSchema.CategoryId`，未來若有 fallback 邏輯可集中於此）
- `Generate(FormTable)` 不動，避免破壞既有 caller

### B. SaveFormSchema 同步 DbCategorySettings

`src/Bee.ObjectCaching/LocalDefineAccess.cs:226 SaveFormSchema(FormSchema formSchema)`：
- 落檔 FormSchema 後，以 `formSchema.CategoryId` 為目標 category，迭代 `formSchema.Tables` 收集「TableName + DisplayName」清單作為「期望落點」
- 讀出當前 `DbCategorySettings`，比對並調整：
  - 新表：在 `DbCategory[CategoryId].Tables` 加上 `<TableItem>`
  - 移動：若該表名出現在其他 Category 之下，從舊處移除、加到新 CategoryId（FormSchema 的 CategoryId 變更時觸發）
  - 刪除：若 FormSchema 中已不存在某 FormTable，但 DbCategorySettings 仍登記該 ProgId 名下的表，從對應 Category 移除
  - 目標 CategoryId 不存在時：丟 `InvalidOperationException`，提示先在 `DbCategorySettings.xml` 建立 Category 主檔
- 落地 `DbCategorySettings`，並 invalidate cache

`src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs:240 SaveFormSchema(...)`：
- Server 端 SaveFormSchema 執行同樣邏輯；client 不需改

### C. 測試與 fixture

- `tests/Define/FormSchema/*.FormSchema.xml`：所有 FormSchema 根節點補 `CategoryId="common"`（或保留預設不寫，若預設機制可省略 attribute）
- `tests/Define/DbCategorySettings.xml`：保留 `<DbCategory Id="common" DisplayName="共用資料庫">`，內部 `<Tables>` 可清空（或維持目前內容，下次 SaveFormSchema 自然會回寫）
- 新增測試（`Bee.Definition.UnitTests`）：
  - `FormSchema_CategoryId_DefaultsToCommon`
  - `FormSchema_CategoryId_RoundTripsThroughXml`
- 新增測試（`Bee.ObjectCaching.UnitTests`）：
  - `SaveFormSchema_AddsTableItemsToCategory`
  - `SaveFormSchema_MovesTableItems_WhenCategoryIdChanged`
  - `SaveFormSchema_RemovesTableItem_WhenFormTableDeleted`
  - `SaveFormSchema_ThrowsWhenCategoryIdNotDefined`
  - 全部以 `using var temp = new TempDefinePath();` 包覆（依 `rules/testing.md` 共享 fixture 隔離規定）

### D. 文件

- `docs/development-cookbook.md`：FormSchema 章節補一段 CategoryId 屬性與自動同步行為
- `docs/architecture-overview.md`：「DbCategory 由人工 XML 維護」改為「FormSchema.CategoryId 為事實來源，DbCategorySettings 為索引快照」
- ADR：考慮新增 `docs/adr/NNNN-formschema-categoryid-as-source-of-truth.md`，記錄這個決策的背景與遷移策略

## 步驟

1. 取得使用者對 D2 / D3 的確認（D1 已定）
2. 實作 A（`FormSchema.CategoryId` + `TableSchemaGenerator.GetCategoryId(FormSchema)` helper）
3. 補單元測試（純邏輯部分）：FormSchema 的 CategoryId 序列化往返、預設值
4. 實作 B（`LocalDefineAccess.SaveFormSchema` 同步邏輯）
5. 補 B 的單元測試（含 `TempDefinePath` 隔離）
6. 遷移既有 fixture（C），跑 `./test.sh` 全綠
7. 更新文件（D）
8. 在文件頂部標記 ✅ 已完成 + 日期，建立 PR

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| 現存 fixture 的 FormSchema 漏改 `CategoryId` | 預設 `"common"`，即便不寫 attribute 也與舊行為一致 |
| SaveFormSchema 多了 DbCategorySettings I/O，效能下降 | 同 process 內已有 cache；落檔本就是低頻動作，可接受 |
| Server / Client 兩端 SaveFormSchema 行為不一致 | 同步邏輯放在 `LocalDefineAccess`（Server 端共用），`RemoteDefineAccess` 透過 RPC 呼叫到 Server，自動承襲 |
| 既有人工編輯 `DbCategorySettings.xml` 的流程斷掉 | 文件明示新流程；保留 `DbCategorySettings.xml` 作為 Category 主檔，仍可手動新增/刪除 Category（不是表） |
| 將 CategoryId 統一在 FormSchema 後，無法支援 Master/Detail 跨 DB | 此情境少見；真有需求時拆成兩個 FormSchema 各自 CategoryId，反而比放 FormTable 更乾淨 |

## 開放問題

- 是否需要在 FormSchema 上額外加 `ConnectionId` 之類的細節？目前 `DbCategory` 已隱含對應一組連線設定，FormSchema 知道 CategoryId 即可推到連線；除非有「同 CategoryId 不同連線」的需求，否則不加。
- 已落地的 production data（外部使用此 library 的下游專案）若已有自己的 `DbCategorySettings.xml`，需要在 release notes 提醒：升級後第一次 SaveFormSchema 會回寫該檔；建議先備份。
