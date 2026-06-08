# 計畫：新增 `bee-scaffold-from-formschema` skill

**狀態：✅ 已完成（2026-06-08）**

## 已決定（2026-06-08 review）

| 決策 | 結果 |
|------|------|
| Skill 名稱 | `bee-scaffold-from-formschema` |
| 預設 scaffold 範圍 | 三類都產（FormLayout + TableSchema + Language），使用者要排除某類需明說 |
| 既有 target 衝突 | **預設略過**（推導出的定義檔只是預設值，使用者可能已手調）；使用者明說「重生 / 覆蓋 / 看 diff」手才覆寫。dry-run 完報告「N 個略過、M 個新增」，不主動問 |
| CLI tool | 不做（skill-only），未來需要再另開 plan |
| Department/Project 中文當翻譯種子 | 不做（skill 提供 ERP 慣用詞對照表即可，避免 hardcode 特定 entity） |

## 背景

開發 ERP 系統時，每新增一個 form entity（Customer / Order / Product / …）都會反覆做：

1. 依 FormSchema 產 `FormLayout.{ProgId}.xml`
2. 依 FormSchema 的每個 FormTable 產 `TableSchema/{CategoryId}/{TableName}.TableSchema.xml`
3. 依 FormSchema 產 `Language/{lang}/{ProgId}.Language.xml`（至少 zh-TW + en-US 雙語 skeleton）

這次先做 Employee 一個範例就驗證了流程；之後一個 ERP 模組要做數十個 entity，
若每次都手動 round-trip 太耗時，且容易在 sub-key 命名 / CategoryId / namespace 等慣例上踩雷。

Framework 已提供 public generator：

| 產出 | 入口 | 備註 |
|------|------|------|
| FormLayout | `schema.GetFormLayout(layoutId)` | `FormSchema.cs:173` |
| TableSchema | `formTable.GetTableSchema()` 或 `TableSchemaGenerator.Generate(formTable)` | `FormTable.cs:179` |
| Language | 手構造 `LanguageResource` + `FormSchemaLocalizer` sub-key 規範 | 無 generator — sub-key 規範見 `FormSchemaLocalizer.cs:33-43` |

也就是說 skill 的價值不在重寫 generator，而在：

- 串起「反序列化 FormSchema → 呼叫 framework → 序列化檔案」的 idiom（驗證過的 throw-away test pattern）
- LLM 加值：中→英 caption 翻譯字典、DbType 細節推斷、特殊欄位排除（系統欄、relation field）
- 路徑與命名慣例：`tests/Define/{FormLayout|TableSchema|Language}/...`、sub-key key 規範、雙語檔位
- 最終 checklist：CategoryId 必填、namespace = ProgId、Items.Count 對齊欄位數等

## 目標

新增 `.claude/skills/bee-scaffold-from-formschema/SKILL.md`，當使用者要：

- 「依 X.FormSchema 產 layout」
- 「為 X 補 language XML / 雙語字典」
- 「為 X 反推 TableSchema」
- 「scaffold 一個 form 的 sidecar 定義」
- 「FormSchema 轉 layout / tableschema / language」

之類情境時自動觸發。

## 範圍

### Input
- 一個 FormSchema XML 檔（路徑或 ProgId）
- 或多個 FormSchema 一次 batch（同一個 module 一起 scaffold）

### Output（三類產出，預設全做，使用者可指定子集）
1. **FormLayout** — `{DefinePath}/FormLayout/{ProgId}.FormLayout.xml`
   - 直接呼叫 `schema.GetFormLayout(ProgId)`
   - layoutId 預設用 ProgId
2. **TableSchema** — `{DefinePath}/TableSchema/{CategoryId}/{TableName}.TableSchema.xml`
   - 對每個 FormTable 呼叫 `TableSchemaGenerator.Generate(table)`
   - CategoryId 由 FormSchema.CategoryId 決定
   - 若使用者已有手工撰寫的 TableSchema 要顯示衝突警告，不覆蓋
3. **Language** — `{DefinePath}/Language/zh-TW/{ProgId}.Language.xml` + `en-US/...`
   - Namespace = ProgId
   - Items 規範（對齊 `FormSchemaLocalizer`）：
     - `Schema.DisplayName`
     - `Table.{TableName}.DisplayName`（每個 FormTable 一筆）
     - `Field.{FieldName}.Caption`（每個 FormField 一筆）
   - zh-TW 直接抄 FormSchema 內的中文 caption
   - en-US 由 LLM 依欄位語意翻譯（不是逐字直譯，遵守 ERP 慣用詞：`sys_no`→`Sequence No.`、`sys_id`→`Customer No.` 等）

### 不做（明確排除）
- PermissionModels 條目（與 form 設計強耦合，另外開 plan）
- DataSet / DataTable C# 程式碼產出
- Repository / BO 程式碼產出（屬 `bee-add-bo-method` skill 的範疇）
- BlazorPage / FormPage UI 程式碼

## 設計

### 執行策略：throw-away xUnit fact

延用剛才 Employee 驗證過的 idiom：

1. 在 `tests/Bee.Definition.UnitTests/Scaffolding/` 寫一個 `_Scaffold{ProgId}FixtureFiles.cs`
2. test 內呼叫 framework generator + 手構造 LanguageResource + `XmlCodec.SerializeToFile`
3. 跑 `dotnet test --filter "FullyQualifiedName~Scaffold{ProgId}"`
4. 跑完刪除 test 檔（commit 時不留）

理由：
- Framework code 一定能保證序列化格式與 round-trip 一致
- 不必新建 console project / dotnet tool / publish flow
- 跑通 = 立即驗證 schema 結構合法（fixture 階段一般 build / generator 任何錯都會冒）

### Skill 結構

```
.claude/skills/bee-scaffold-from-formschema/
└── SKILL.md
```

SKILL.md 含：

1. **觸發條件** — frontmatter description（中文，比照既有 skill）
2. **決策樹** — 使用者只給 ProgId vs 給完整路徑 vs batch 多個 schema 的判斷
3. **流程樣板** — 完整 throw-away test 樣板（直接 copy-paste 即可改）
4. **i18n key 規範** — `Schema.DisplayName` / `Table.X.DisplayName` / `Field.X.Caption`，配 `FormSchemaLocalizer` 常數
5. **中→英翻譯指引** — ERP 慣用詞對照表（sys_no、sys_rowid、sys_id、sys_name、ref_xxx、dept_、emp_ 等系統前綴）
6. **路徑慣例** — `tests/Define/...` vs 真實專案的 `{repoRoot}/Define/...`
7. **不覆蓋既有檔** — 先檢查 target 路徑，存在則列出衝突要求使用者確認
8. **最終 checklist** — CategoryId 非空、Namespace=ProgId、各檔 Items.Count、雙語對齊

## 階段

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 建 `bee-scaffold-from-formschema/SKILL.md`：frontmatter、觸發條件、決策樹、流程樣板 | ✅ 已完成（2026-06-08） |
| 2 | 在 SKILL.md 內補翻譯字典範例（系統前綴 sys_/ref_/audit_ + 業務常見 dept/emp/cust/order 等）| ✅ 已完成（2026-06-08） |
| 3 | 用 Department.FormSchema + Project.FormSchema 各跑一次 dry-run 驗證 skill | ✅ 已完成（2026-06-08）— 6 個新檔產出 + 4 個既存 TableSchema 自動略過，throw-away test 已刪 |

## 開放問題

（review 已收斂至「已決定」表，目前無未決問題）

## 不在範圍

- 上述「不做」清單
- skill 跨 repo 安裝（這是本 repo 專案 skill，預設只在 bee-library 開啟時可用，不裝到 `~/.claude/skills`）
- 自動 commit / PR（skill 只產檔，git 操作由使用者另外決定）
