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

呼叫端（CLI 工具、UI、產 schema 的程式碼）每次都要從外部來源（人工輸入、寫死、查 `DbCategorySettings.xml`）餵 `categoryId` 給 `SaveTableSchema`，而 FormSchema 自身沒有資訊可以告知「我屬於哪個 Category」。CLAUDE.md 中明示「FormSchema 為定義中樞，同時驅動 UI、資料庫與驗證規則」；目前 CategoryId 是這條原則少數的例外。

## 目標

讓 FormSchema 自帶 `CategoryId` 欄位，作為「該 FormSchema 下所有 TableSchema 應落在哪個 Category」的單一事實來源（runtime 路由用）。`DbCategorySettings.xml` 仍維持原貌，本計畫不動其內容與行為。

具體：
1. `FormSchema` 新增 `CategoryId` 屬性（`XmlAttribute`），**必填**，無預設值
2. `TableSchemaGenerator` 新增 helper `GetCategoryId(formSchema)`，集中將來可能的查詢邏輯
3. 所有現存 `tests/Define/FormSchema/*.xml` fixture 補上 `CategoryId="common"`
4. 文件補充「FormSchema.CategoryId 為 Category 路由的事實來源」
5. `DbCategorySettings.xml` 不動（D3 決議：維持現狀，仍允許人工編輯）；`SaveFormSchema` 不增加同步邏輯

## 設計決策（已定）

### D1. CategoryId 放在 `FormTable` 還是 `FormSchema`？— ✅ FormSchema

- 同一 FormSchema 內的 Master / Detail tables 通常屬於同一 Category（同業務領域、同連線）
- 若真有 Master/Detail 跨 DB 的情境，可拆成兩個 FormSchema 各自指定 CategoryId，比放 FormTable 乾淨
- 屬性集中在 FormSchema 比散在每個 FormTable 易讀、難寫錯

### D2. 預設值 — ✅ 必填，無預設

- 強制每個 FormSchema 明確指定 CategoryId，避免「忘了設」的隱性錯誤
- 所有現存 fixture 與下游專案的 FormSchema XML 都需補 attribute（破壞性變更）
- 實作上：`CategoryId { get; set; } = string.Empty;`，`SaveFormSchema`（或 `FormSchema` 自身的 validation）偵測空字串時 throw `InvalidOperationException`，訊息明確指出該 ProgId 缺 CategoryId
- 反序列化時若 XML 中無 `CategoryId` attribute：屬性留為空字串，下次 save / 使用時才報錯（與其他必填欄位行為一致）

### D3. `DbCategorySettings.xml` 的 `<Tables>` 子節點是否仍保留人工編輯？— ✅ 維持現狀

- 經盤點，`src/` 中無任何 runtime 程式碼讀 `DbCategory.Tables`，它純粹是手動維護的清單，不影響功能
- 因此本計畫不動 `DbCategorySettings.xml` 的編輯流程，也不在 `SaveFormSchema` 中加同步邏輯
- 後果：FormSchema.CategoryId 與 DbCategorySettings.`<Tables>` 之間可能不同步，但因無人讀後者，無實際影響
- 若未來確認 `<Tables>` 子節點完全無用，可在另一個獨立計畫中移除

## 影響範圍盤點

> 僅列源碼與固定資源；`bin/`、`obj/`、`TestResults/` 屬 build artefact。

### A. 新增/修改類別

`src/Bee.Definition/Forms/FormSchema.cs`：
- 新增 `[XmlAttribute] public string CategoryId { get; set; } = string.Empty;`
- 對齊既有 `DisplayName` / `ListFields` 等屬性 metadata：`[Category(PropertyCategories.Data)]` / `[Description("Database category id (required).")]`
- 注意：`Category` 此處指 `System.ComponentModel.CategoryAttribute`（屬性瀏覽器分組用），與本計畫 `CategoryId` 屬性語意不同，不會衝突

`src/Bee.Definition/Database/TableSchemaGenerator.cs`：
- 新增 `public static string GetCategoryId(FormSchema formSchema)` helper：回傳 `formSchema.CategoryId`，並在空字串時 throw `InvalidOperationException`，訊息含 `ProgId`
- `Generate(FormTable)` 不動

### B. 必填驗證放哪裡？

兩個合理位置，擇一：

- **`FormSchema` 提供 `Validate()` / 在 `SaveFormSchema` 落檔前呼叫**（傾向）：與既有「載入時不檢、寫入時檢」風格一致
- 反序列化階段（`Read` / `SetSerializeState` 後）即驗：早期失敗，但會破壞「載入殘缺資料以便修復」的能力

> 兩者皆不影響 D1/D2/D3 結論；實作時擇一即可。

### C. 測試與 fixture

`tests/Define/FormSchema/*.FormSchema.xml`：所有 FormSchema 根節點補 `CategoryId="common"`：
- `Department.FormSchema.xml`
- `Employee.FormSchema.xml`
- `Project.FormSchema.xml`
- 其他若有

新增測試（`Bee.Definition.UnitTests`）：
- `FormSchema_CategoryId_DefaultsToEmpty` — 驗證新建物件預設空字串
- `FormSchema_CategoryId_RoundTripsThroughXml` — 序列化往返
- `TableSchemaGenerator_GetCategoryId_ThrowsWhenEmpty` — 必填驗證
- `TableSchemaGenerator_GetCategoryId_ReturnsValueWhenSet`

若選擇在 `SaveFormSchema` 落檔時驗證，`Bee.ObjectCaching.UnitTests` 加：
- `SaveFormSchema_ThrowsWhenCategoryIdEmpty`
- `using var temp = new TempDefinePath();` 包覆（依 `rules/testing.md`）

### D. 文件

- `docs/development-cookbook.md`：FormSchema 章節補一段 CategoryId 屬性說明（必填、決定 TableSchema 落點）
- `docs/architecture-overview.md`：加註「FormSchema.CategoryId 為 Category 路由的事實來源；DbCategorySettings 主檔仍由人工維護 Category 元資料」
- ADR：考慮新增 `docs/adr/NNNN-formschema-categoryid-as-source-of-truth.md`，記錄為何放 FormSchema、為何必填、為何不自動同步 DbCategorySettings 的三項決策背景

## 步驟

1. 實作 A（`FormSchema.CategoryId` + `TableSchemaGenerator.GetCategoryId(FormSchema)` helper）
2. 決定 B 的驗證落點，加 validation 邏輯
3. 補單元測試（C）
4. 遷移既有 fixture（C），跑 `./test.sh` 全綠
5. 更新文件（D）
6. 在文件頂部標記 ✅ 已完成 + 日期，建立 PR

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| 必填屬性破壞下游既有 FormSchema | Release notes 明確列為 breaking change，提供 migration guide：「於每個 `<FormSchema>` 根節點補 `CategoryId='common'`（或實際 Category）」 |
| 既有 fixture 漏補 attribute | CI 跑單元測試會立即抓到 — `SaveFormSchema_ThrowsWhenCategoryIdEmpty` 與相關整合測試都會 fail |
| FormSchema.CategoryId 與 DbCategorySettings.`<Tables>` 不同步 | 本計畫不處理；目前無 runtime consumer 讀 `<Tables>`，無實際影響 |
| 將 CategoryId 統一在 FormSchema 後，無法支援 Master/Detail 跨 DB | 此情境少見；真有需求時拆成兩個 FormSchema 各自 CategoryId，反而比放 FormTable 更乾淨 |

## 開放問題

- 是否需要在 FormSchema 上額外加 `ConnectionId` 之類的細節？目前 `DbCategory` 已隱含對應一組連線設定，FormSchema 知道 CategoryId 即可推到連線；除非有「同 CategoryId 不同連線」的需求，否則不加。
- 後續是否要開另一個計畫真正移除 `DbCategorySettings.<Tables>` 子節點？目前確認無 runtime 讀者，但本計畫範圍不含此。
