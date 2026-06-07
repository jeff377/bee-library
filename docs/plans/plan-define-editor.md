# 計畫：定義檔方案維護工具（Avalonia 桌面程式）

**狀態：🚧 進行中（2026-06-07）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 主介面外殼：開啟 DefinePath → 掃目錄 → 左側方案樹（型別分組）列出所有定義檔，可展開、點選；右側暫顯示唯讀摘要 | ✅ 已完成（2026-06-07） |
| 1 | 文件宿主框架 + FormSchema 唯讀結構樹（內層樹） | 📝 待做 |
| 2 | FormSchema 屬性編輯 + 節點增刪 + 存回 XML | 📝 待做 |
| 3 | FormSchema 關聯欄位對應 + 基本驗證（首個完整可用里程碑） | 📝 待做 |
| 4 | 單例設定編輯器（SystemSettings / DbCategorySettings / ProgramSettings / PermissionModels） | 📝 待做 |
| 5 | DatabaseSettings 專屬：連線字串貼上拆解 + 靜態驗證 | 📝 待做 |
| 6 | 其餘多份型別（TableSchema / FormLayout / Language） | 📝 待做 |
| 7 | 打包（各平台） | 📝 待做 |

## 背景

bee-library 的「定義檔」是放在 **DefinePath** 下的一組 XML（9 種 `DefineType`），目前手寫／程式產生，缺乏可視化維護工具。本計畫做一個 **Avalonia 跨平台桌面工具**，把**整個 DefinePath 視為「方案」**：左側像 VSCode explorer 樹狀列出所有定義檔，點選某檔 → 右側開對應編輯器維護。

直接引用 `Bee.Definition`，用 `XmlCodec` 讀寫定義物件 —— 單一語言、單一 process、零序列化轉換。它是**開發期工具**，非框架發布套件、非示範專案，故放獨立的 `tools/` 目錄。

開發順序**由主介面（方案外殼）開始**：外殼是所有定義型別共用的骨架，個別編輯器是外殼裡的「文件視圖」，逐型別填充。第一個完整可用里程碑是 Phase 3（主介面 + FormSchema 全功能）。

## 既定決策（已與使用者確認）

| 決策點 | 選定方案 | 理由 |
|--------|---------|------|
| 形式 | **C# 單一桌面程式** | 工程最小，避免插件 + sidecar 雙語言複雜度 |
| UI 框架 | **Avalonia** | 跨平台（Win / Mac / Linux） |
| 存放位置 | **tools/（新目錄）+ 獨立 Bee.Tools.slnx** | 工具程式，與 src/ 發布套件、samples/ 示範分開；不上 NuGet/CI |
| 主介面 | **DefinePath 為「方案」，左側方案樹 + 右側文件編輯區** | 對齊「像 VSCode」的維護心智模型 |
| 方案樹分組 | **按定義型別分組** | 對齊 DefinePath 目錄結構，各型別主鍵不一致不宜按業務物件聚合 |
| 兩層樹 | **外層方案樹（檔案導覽）+ 內層結構樹（單檔編輯）** | 巢狀定義檔的自然呈現 |
| 資料存取 | **掃目錄建方案樹 + XmlCodec 讀寫單檔** | 框架無列舉 API，本就須掃目錄；純 offline |
| 首期型別 | **FormSchema 優先** | 最常用 + 最複雜，打通即驗證編輯框架 |

## 核心架構

```
┌──────────────────── Avalonia App (tools/DefineEditor) ────────────────────┐
│                                                                           │
│  ┌─── 方案樹（左）──────┐   ┌─── 文件編輯區（右）──────────────────────┐  │
│  │ 📁 DefinePath        │   │  依選取節點型別載入對應編輯器（DataTemplate）│  │
│  │ ├ ⚙️ 系統設定        │   │                                          │  │
│  │ │  ├ SystemSettings  │──►│  FormSchema 編輯器：                      │  │
│  │ │  └ DatabaseSettings│   │   ┌── 內層結構樹 ──┐ ┌── 屬性面板 ──┐    │  │
│  │ ├ 📋 FormSchema       │   │   │ Schema         │ │ 選定節點屬性  │    │  │
│  │ │  ├ Employee  ◄──────│   │   │ └ Table        │ │              │    │  │
│  │ │  └ Department       │   │   │   └ Field      │ │ PropertyGrid │    │  │
│  │ ├ 🗃️ TableSchema      │   │   └────────────────┘ └──────────────┘    │  │
│  │ ├ 🎨 FormLayout       │   │                                          │  │
│  │ └ 🌐 Language         │   │  其他型別 → 各自的編輯器視圖             │  │
│  └─────────────────────┘   └──────────────────────────────────────────┘  │
│         │                              │                                   │
│   掃 DefinePath 目錄             XmlCodec 讀寫單檔                          │
│         └──────────────┬───────────────┘                                  │
│                        ▼  ProjectReference                                 │
│              src/Bee.Definition + src/Bee.Base（XmlCodec / PathOptions）   │
└───────────────────────────────────────────────────────────────────────────┘
                         │ 讀寫
                   DefinePath 下的 *.xml
```

## DefinePath 佈局（方案樹資料來源）

| DefineType | 單例/多份 | 路徑規則 | 主鍵 |
|------------|----------|---------|------|
| SystemSettings | 單例 | `SystemSettings.xml` | — |
| DatabaseSettings | 單例 | `DatabaseSettings.xml` | — |
| DbCategorySettings | 單例 | `DbCategorySettings.xml` | — |
| ProgramSettings | 單例 | `ProgramSettings.xml` | — |
| PermissionModels | 單例 | `PermissionModels.xml` | — |
| FormSchema | 多份 | `FormSchema/{progId}.FormSchema.xml` | progId |
| TableSchema | 多份 | `TableSchema/{categoryId}/{tableName}.TableSchema.xml` | (categoryId, tableName) |
| FormLayout | 多份 | `FormLayout/{layoutId}.FormLayout.xml` | layoutId |
| Language | 多份 | `Language/{lang}/{ns}.Language.xml` | (lang, ns) |

- 框架**無列舉 API**（`IDefineAccess` 只有單筆 `GetDefine(type, keys)`）→ 方案樹靠工具自己掃目錄樹建立。
- `PathOptions` 提供 `GetXxxFilePath(...)` 反推單檔路徑，可複用於存回。
- （後續可選）Customize 層：`CustomizePath/{customizeId}/` 覆蓋 FormLayout / Language / ProgramSettings，方案樹上可標「已覆蓋」。

## 技術選型

| 議題 | 建議方案 | 備註 |
|------|---------|------|
| UI 框架 | **Avalonia 11.x** + MVVM（CommunityToolkit.Mvvm） | 專案首次引入；僅 tools/，不影響核心 |
| 方案樹 / 內層樹 | Avalonia `TreeView` + `HierarchicalDataTemplate` | 綁 `ObservableCollection<NodeViewModel>` |
| 文件區切換 | `ContentControl` + DataTemplate selector | 依選取節點型別載入對應編輯器視圖 |
| 屬性編輯 | 第三方 PropertyGrid（`bodong.Avalonia.PropertyGrid`）優先，複雜節點自寫 DataForm | 單例設定多為扁平，PropertyGrid 直接吃 |
| 序列化 | `Bee.Base` 的 **XmlCodec** | 與框架一致 |
| 連線字串解析 | BCL `System.Data.Common.DbConnectionStringBuilder`（零 driver） | 見 DatabaseSettings 策略節 |

## tools/ 整合注意

- 新建頂層 `tools/` 目錄，放 `tools/DefineEditor/` + 獨立 `tools/Bee.Tools.slnx`，與 src/ / tests/ / samples/ 平行。
- `ProjectReference` 到 `src/Bee.Definition`、`src/Bee.Base`（不走 NuGet）。
- **不**併入核心 slnx —— 避免 Avalonia 依賴汙染核心 build / CI。
- CI 不驗證：`build-ci.yml` 只在 src/ tests/ slnx props sonar yml 異動觸發，`tools/` 改動不跑 CI。本機把關。
- **不可** ProjectReference `Bee.Samples.Shared`（含 AspNetCore framework reference，桌面不相容）。

## 階段細節

### Phase 0 — 主介面外殼（方案樹）

- 建立 `tools/DefineEditor/`（Avalonia MVVM）+ `tools/Bee.Tools.slnx`，ProjectReference Bee.Definition / Bee.Base。
- 「開啟方案」選一個 DefinePath 資料夾 → 掃目錄樹 → 左側方案樹按型別分組呈現所有定義檔，可展開、點選。
- 右側暫顯示選取檔的唯讀摘要（型別、主鍵、檔路徑）。
- **驗收**：開啟 `tests/Define`，方案樹正確列出 5 單例 + FormSchema/TableSchema 下所有檔。

> 主介面外殼是後續所有編輯器的容器，先把「掃目錄 → 方案樹 → 選取 → 文件區切換」這條骨架打穩。

### Phase 1 — 文件宿主 + FormSchema 唯讀

- 文件區用 DataTemplate selector：選 FormSchema 節點 → 載入 FormSchema 編輯器視圖。
- `XmlCodec.Deserialize<FormSchema>` → 內層結構樹（Schema → FormTable → FormField → 關聯/ListItems），唯讀。
- **驗收**：點 Employee.FormSchema.xml，內層樹正確展開所有表與欄位。

### Phase 2 — FormSchema 編輯 + 存回

- 屬性面板：選內層樹節點 → 編輯該物件屬性（DbType / 型別等 enum 下拉），雙向綁定。
- 節點增刪：新增/刪除 Table、Field、改名（context menu / 工具列）。
- 「儲存」→ `XmlCodec.Serialize` 寫回原檔（用 PathOptions 反推路徑）。
- **驗收**：改/增一個欄位 → 存檔 → XML 正確更新且可被框架重新載入。

### Phase 3 — FormSchema 關聯對應 + 驗證（首個完整可用里程碑）

- RelationField 的 `RelationProgId` 選擇 + `FieldMapping` 來源↔目標對應 UI（最複雜互動，自寫面板）。
- 基本驗證：重複欄位名、缺必填、關聯指向不存在的 ProgId。
- **驗收**：建立關聯欄位設好對應，序列化與手寫 XML 等價。**此時主介面 + FormSchema 全功能可用**。

### Phase 4 — 單例設定編輯器

- SystemSettings / DbCategorySettings / ProgramSettings / PermissionModels —— 結構多為扁平，PropertyGrid 直接吃。
- **驗收**：每種單例設定可載入、編輯、存回。

### Phase 5 — DatabaseSettings 專屬

- 連線字串貼上拆解 + 靜態驗證（詳見下節）。
- **驗收**：貼一個 SQL Server / PostgreSQL 連線字串 → 正確拆出帳密/庫名、改寫佔位符、預覽確認後存回。

### Phase 6 — 其餘多份型別

- TableSchema（categoryId/tableName 兩層 + 欄位編輯）、FormLayout、Language（多語系）。
- **驗收**：三種型別各可載入、編輯、存回。

### Phase 7 — 打包

- 各平台 self-contained（win-x64 / osx-arm64 / linux-x64）+ `tools/DefineEditor/README.md`。

## DatabaseSettings 處理策略（設計已定，Phase 5 實作）

DatabaseSettings 的正確性是「連得上真實資料庫」，但讓編輯器扛實連測試代價過高（要引 5 個 DB driver、要解密密碼跨機器取不到 MasterKey、開發機網路測不準）。故：

- **編輯器零 DB driver**：保持輕量，所有型別一視同仁。
- **實連測試交 server 端健康檢查**：部署環境才有對的金鑰與網路。可在 `Bee.Db` 加公開 `TryTestConnection(databaseId)`（動核心套件，另開小計畫）。
- **靜態驗證（編輯器做）**：佔位符完整性、ServerId 參照、DatabaseType 與方言相符、必填欄位。
- **連線字串貼上拆解（核心輔助）**：使用者貼「已在外部工具測通」的完整連線字串，工具自動拆出帳密/庫名、改寫佔位符。把「驗證」前移到使用者熟悉的外部工具，編輯器只負責可靠轉換。

### 連線字串拆解（零 driver 實作）

1. 用 BCL **`System.Data.Common.DbConnectionStringBuilder`** tokenize（正確處理引號/分號跳脫）。
2. 自維護「各 DatabaseType 帳密 key 別名表」辨識角色：

   | DatabaseType | UserId 別名 | Password 別名 | DbName 別名 |
   |--------------|------------|---------------|-------------|
   | SQL Server | `User ID` / `UID` | `Password` / `PWD` | `Initial Catalog` / `Database` |
   | PostgreSQL | `Username` / `User ID` | `Password` | `Database` |
   | MySQL | `User ID` / `UID` | `Password` / `PWD` | `Database` |
   | Oracle | `User ID` | `Password` | (service 在 `Data Source` 內) |
   | SQLite | (通常無帳密) | — | `Data Source` |

3. 拆出 UserId / Password / DbName → 填欄位；其餘鍵原樣保留，帳密/庫名位置改寫成 `{@UserId}` / `{@Password}` / `{@DbName}` → 存回 `ConnectionString`。
4. 預覽拆解結果（密碼遮罩）讓使用者確認後才寫入。

邊界：整合驗證（`Integrated Security` / `Trusted_Connection`）無密碼則不拆 Password；冷門別名辨識不到 → fallback 手動指派；Oracle 先支援 EZConnect（`Data Source=host:port/service`），TNS 後續；反向可把佔位符填回組出完整字串供閱讀（密碼遮罩）。

## 風險與待決

| 項目 | 說明 |
|------|------|
| Avalonia 為新引入框架 | 僅 tools/、獨立 solution，不影響核心套件與發布；學習曲線由本計畫吸收 |
| 無列舉 API → 掃目錄 | 方案樹靠掃目錄；目錄/命名規則若改需同步工具的掃描邏輯 |
| NodeViewModel 泛化 | 外層方案樹節點需涵蓋 9 型別；Phase 0 設計時預留型別擴充點，但不過度設計 |
| PropertyGrid 對集合屬性 | 第三方 PropertyGrid 編輯集合體驗一般，複雜處自寫面板 |
| 存回安全 | 寫回前確認路徑在 DefinePath 內（path traversal 防護）；避免破壞共享 fixture（見 testing.md） |

## 不在本計畫範圍

- 遠端 Bee server API 存取（本期純本機檔）。
- DatabaseSettings 實連測試（交 server 健康檢查）。
- 多人協作 / 鎖定機制。
- Customize 層完整編輯（Phase 6 後視需要另議；先在方案樹標示覆蓋）。
