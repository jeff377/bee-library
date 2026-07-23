---
name: bee-scaffold-from-formschema
description: bee-library 從一個 FormSchema XML 反推 / 產出對應的 FormLayout、TableSchema、雙語 LanguageResource 三類「sidecar」定義檔。涵蓋 throw-away xUnit fact idiom（直接呼叫 framework public generator 序列化原貌）、FormSchemaLocalizer sub-key 規範（Schema.DisplayName / Table.X.DisplayName / Field.X.Caption）、中→英欄位翻譯指引（sys_/ref_/audit_ 系統前綴 + ERP 慣用詞）、target 衝突偵測與處理。當使用者要「依 X.FormSchema 產 layout / language / tableschema」、「為 X 補 i18n」、「scaffold 一個 form 的 sidecar 定義」、「FormSchema 轉 layout / 翻 language / 反推 tableschema」之類需求時使用。
---

# bee-library FormSchema → Sidecar 定義 scaffold

bee-library 採 **FormSchema-driven** 設計：一個 FormSchema 同時驅動 UI（FormLayout）、
資料庫（TableSchema）與多語介面（LanguageResource）。每新增一個 form entity，三類 sidecar
都要產出 — 本 skill 把流程寫死，避免 sub-key 命名 / CategoryId / namespace 等慣例踩雷。

> 樣板對照（讀程式碼時對著看）：
> - `tests/Define/FormSchema/Employee.FormSchema.xml`（input）
> - `tests/Define/FormLayout/Employee.FormLayout.xml`
> - `tests/Define/TableSchema/company/ft_employee.TableSchema.xml`
> - `tests/Define/Language/{zh-TW,en-US}/Employee.Language.xml`

## 三類產出與 framework 入口

| 產出 | Framework 入口 | 備註 |
|------|---------------|------|
| FormLayout | `schema.GetFormLayout(layoutId)`（`FormSchema.cs`） | `layoutId` 預設用 `ProgId` |
| TableSchema | `TableSchemaGenerator.Generate(formTable)` 或 `formTable.GenerateDbTable()` | 對 schema 的**每個** FormTable 各產一份 |
| LanguageResource | 無 generator — 手構造 + `FormSchemaLocalizer` sub-key 常數 | 雙語：zh-TW 抄 schema 中文 caption、en-US 由翻譯字典推 |

**Skill 預設產三類**。使用者要排除某類需明說（「只產 layout」、「不要動 tableschema」）。

## 流程：throw-away xUnit fact

不寫 CLI、不新建 console project。延用驗證過的 idiom：

1. 在 `tests/Bee.Definition.UnitTests/Scaffolding/_Scaffold{ProgId}FixtureFiles.cs` 寫一個 `[Fact]`
2. test 內反序列化 FormSchema → 呼叫 framework generator + 手構造 LanguageResource → `XmlCodec.SerializeToFile`
3. 跑 `dotnet test --filter "FullyQualifiedName~Scaffold{ProgId}"`
4. **跑完立刻刪除 test 檔**（commit 時不留 — 否則它每跑一次都會 overwrite fixture）

理由：framework 序列化格式 = 真實 round-trip 格式，無漂移；驗證 schema 結構合法（generator 拋例外即代表 schema 寫錯）；零新增 csproj / publish 成本。

### 完整可 copy-paste 樣板

把 `{ProgId}` 全部換成實際 ProgId（如 `Employee`、`Department`），把 `{Schema/Table/Field translations}` 填進去：

```csharp
using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Forms;
using Bee.Definition.Language;

namespace Bee.Definition.UnitTests.Scaffolding
{
    /// <summary>
    /// Throw-away one-shot generator. Materializes FormLayout / TableSchema / Language
    /// fixture files for {ProgId} under tests/Define/. Delete this file after the
    /// fixtures are committed — every re-run overwrites the same target paths.
    /// </summary>
    public class Scaffold{ProgId}FixtureFiles
    {
        [Fact]
        [DisplayName("OneShot: 由 {ProgId}.FormSchema 產出 FormLayout / TableSchema / Language fixture（手動執行）")]
        public void Generate_{ProgId}SidecarFiles()
        {
            // bin/<config>/net10.0 → repo root（5 層 ..）
            string baseDir = AppContext.BaseDirectory;
            string repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            string definePath = Path.Combine(repoRoot, "tests", "Define");
            Assert.True(Directory.Exists(definePath), $"tests/Define not found: {definePath}");

            // 1. 反序列化 FormSchema（唯一 input）
            string schemaPath = Path.Combine(definePath, "FormSchema", "{ProgId}.FormSchema.xml");
            var schema = XmlCodec.DeserializeFromFile<FormSchema>(schemaPath)!;

            // 2. FormLayout
            var layout = schema.GetFormLayout("{ProgId}");
            XmlCodec.SerializeToFile(layout,
                Path.Combine(definePath, "FormLayout", "{ProgId}.FormLayout.xml"));

            // 3. TableSchema（每個 FormTable 一份）
            foreach (var table in schema.Tables!)
            {
                var tableSchema = table.GenerateDbTable();
                XmlCodec.SerializeToFile(tableSchema,
                    Path.Combine(definePath, "TableSchema", schema.CategoryId,
                        $"{table.DbTableName}.TableSchema.xml"));
            }

            // 4. Language zh-TW（抄 schema 中的中文 caption）
            var zh = BuildResource("zh-TW",
                schemaDisplayName: "{中文 schema 名}",
                tableDisplayNames: new (string, string)[]
                {
                    ("{TableName}", "{中文 table 名}"),
                    // 多 table 就多 entry
                },
                fieldCaptions: new (string, string)[]
                {
                    ("sys_no", "流水號"),
                    // … 對齊 FormSchema 內每個 FormField.Caption
                });
            XmlCodec.SerializeToFile(zh,
                Path.Combine(definePath, "Language", "zh-TW", "{ProgId}.Language.xml"));

            // 5. Language en-US（依翻譯字典推）
            var en = BuildResource("en-US",
                schemaDisplayName: "{English schema name}",
                tableDisplayNames: new (string, string)[]
                {
                    ("{TableName}", "{English table name}"),
                },
                fieldCaptions: new (string, string)[]
                {
                    ("sys_no", "Sequence No."),
                    // … 對應 zh-TW 每個 entry
                });
            XmlCodec.SerializeToFile(en,
                Path.Combine(definePath, "Language", "en-US", "{ProgId}.Language.xml"));

            Assert.True(File.Exists(Path.Combine(definePath, "FormLayout", "{ProgId}.FormLayout.xml")));
        }

        private static LanguageResource BuildResource(
            string lang,
            string schemaDisplayName,
            (string TableName, string DisplayName)[] tableDisplayNames,
            (string FieldName, string Caption)[] fieldCaptions)
        {
            var resource = new LanguageResource
            {
                Namespace = "{ProgId}",
                Lang = lang,
            };
            resource.Items.Add(FormSchemaLocalizer.SchemaDisplayNameKey, schemaDisplayName);
            foreach (var (tableName, displayName) in tableDisplayNames)
            {
                resource.Items.Add(
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        FormSchemaLocalizer.TableDisplayNameKeyFormat, tableName),
                    displayName);
            }
            foreach (var (fieldName, caption) in fieldCaptions)
            {
                resource.Items.Add(
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        FormSchemaLocalizer.FieldCaptionKeyFormat, fieldName),
                    caption);
            }
            return resource;
        }
    }
}
```

### 跑法

```bash
dotnet test tests/Bee.Definition.UnitTests/Bee.Definition.UnitTests.csproj \
    --configuration Debug \
    --filter "FullyQualifiedName~Scaffold{ProgId}" \
    --nologo --verbosity minimal
```

跑通後**立刻 `rm` 該 test 檔**，再 `git status` 確認三類產出 + test 檔刪除狀態正確。

## 路徑慣例

| 情境 | DefinePath |
|------|-----------|
| 在 bee-library 內示範用（`tests/Define/`） | `{repoRoot}/tests/Define`（樣板用的就是這個） |
| 真實 ERP 專案 | 該專案的 `Define/` 或 `app/Define/` 等，依該 repo 自有約定 |

樣板裡 `repoRoot = baseDir + "../" * 5` 適用 `tests/<Project>.UnitTests/bin/<config>/net10.0/`。其他 repo 若 test 專案層級不同需調整 `..` 數量。

## i18n key 規範（對齊 FormSchemaLocalizer）

| 範圍 | Key 樣式 | 常數 |
|------|---------|------|
| Schema 整體顯示名 | `Schema.DisplayName` | `FormSchemaLocalizer.SchemaDisplayNameKey` |
| 各 Table 顯示名 | `Table.{TableName}.DisplayName` | `FormSchemaLocalizer.TableDisplayNameKeyFormat` |
| 各 Field caption | `Field.{FieldName}.Caption` | `FormSchemaLocalizer.FieldCaptionKeyFormat` |

**namespace = ProgId**（永遠這樣 — `FormSchemaLocalizer.Localize` 直接用 `schema.ProgId` 當 lookup namespace）。

**不要為了「補齊」加額外 sub-key** — `FormSchemaLocalizer` 只查上述三類；多寫的 key 框架不會用，徒增維護。如需加 enum / list 翻譯，那是另一個議題（看 `LanguageEnum`），不在預設 scaffold 範圍。

## 中→英翻譯字典

**規則：不逐字直譯，遵守 ERP 慣用詞。** 業務 caption 簡潔（不寫「The ...」），系統前綴有固定譯法。

### 系統前綴（固定譯法）

| 中文 / FieldName | English | 備註 |
|-----------------|---------|------|
| `sys_no` 流水號 | `Sequence No.` | DB 自增 PK，**不**譯 `System Number` |
| `sys_rowid` 唯一識別 | `Row Id` | Guid 全局唯一 |
| `sys_id` 編號 | `{Entity} No.` | 業務鍵；依 entity 譯成 `Employee No.` / `Department No.` |
| `sys_name` 名稱 | `{Entity} Name` 或 `Name` | 同上 |
| `sys_insert_time` 寫入時間 | `Insert Time` 或 `Created At` |  |
| `sys_update_time` 更新時間 | `Update Time` 或 `Updated At` |  |
| `sys_insert_user` 寫入者 | `Created By` |  |
| `sys_update_user` 更新者 | `Updated By` |  |
| `ref_xxx_id` / `ref_xxx_name` | `{Xxx} No.` / `{Xxx} Name` | RelationField 帶過來的展示欄；前綴 `Ref.` 不必，業務上看不到關聯 |

### ERP 慣用詞（業務常見）

| 中文 | English |
|------|---------|
| 員工 | Employee |
| 部門 | Department |
| 主管 / 直屬主管 | Supervisor |
| 部門主管 | Department Manager |
| 客戶 | Customer |
| 供應商 | Supplier / Vendor |
| 產品 / 品項 | Product / Item |
| 訂單 | Order |
| 採購單 | Purchase Order |
| 銷貨單 | Sales Order |
| 庫存 | Inventory / Stock |
| 出貨 | Shipment |
| 倉庫 | Warehouse |
| 公司 | Company |
| 角色 | Role |
| 權限 | Permission |
| 使用者 / 用戶 | User |
| 帳號 | Account |
| 密碼 | Password |
| 電子郵件 | Email |
| 備註 | Note / Remarks |
| 描述 | Description |
| 狀態 | Status |
| 類別 | Category |
| 金額 / 單價 / 總價 | Amount / Unit Price / Total |
| 數量 | Quantity |
| 日期 / 時間 | Date / Time |
| 起始 / 結束 | Start / End |
| 起日 / 迄日 | Start Date / End Date |
| 專案 | Project |
| 任務 | Task |

### Caption 寫法

- 不加冠詞（不寫 `The Employee Name`）
- 編號用 `No.`（含句點），不寫 `Number` / `Id`（除非確實是 GUID/UUID rowid）
- 不要 sentence case（不寫 `Employee no.`），ERP UI 慣例 Title Case
- 長詞優先 abbrev：`Department No.` 不寫 `Department Number`

## 衝突處理（預設略過既有檔）

執行前**先 dry-run 列出所有 target 路徑**並檢查存在性：

```bash
# 範例：scaffold Customer 前先檢查
ls tests/Define/FormLayout/Customer.FormLayout.xml \
   tests/Define/TableSchema/company/ft_customer.TableSchema.xml \
   tests/Define/Language/zh-TW/Customer.Language.xml \
   tests/Define/Language/en-US/Customer.Language.xml 2>/dev/null
```

**預設規則：已存在的 target 一律略過，不覆蓋。**

理由：skill 推導出的三類 sidecar 都是 framework generator + 翻譯字典產出的「合理預設值」，
**不是權威來源**。使用者後續可能已手調：

- FormLayout — 調 LayoutColumn 寬度、改 ControlType、把欄位拆 Section
- TableSchema — 補 index、改 String 欄位 Length、加額外 DbField（如 audit 欄位）
- Language — 修詞、補 LanguageEnum 翻譯

預設略過 = re-run scaffold 對既有 entity 安全（只補新欄位 / 新 entity，不踩手調過的檔）。

### 覆寫預設行為

使用者明確要求「重生 / 覆蓋」時才覆寫：

| 使用者意圖 | 處理方式 |
|-----------|---------|
| 「重生 layout」/「覆蓋 X.Language.xml」 | 對該檔覆蓋寫入（仍**只動明說的那檔**，其他既有檔維持略過） |
| 「重新 scaffold 全部」 | 全部覆蓋（要再確認一次，因 blast radius 大） |
| 「想看 framework 原生產出是什麼樣」 | 產 `{file}.new.xml` 供 diff，使用者手動 merge 或 rm `.new`，原檔不動 |

對話中使用者**沒明說**時，skill 跑完直接報告「N 個 target 略過（既存）、M 個 target 新增」，不主動詢問 — 略過是預期行為，不必每次都打斷。

## 最終 checklist

跑完後逐項確認：

- [ ] FormLayout 檔產出於 `{DefinePath}/FormLayout/{ProgId}.FormLayout.xml`
- [ ] TableSchema 對 schema 內每個 FormTable 都產出（依 CategoryId 分目錄）
- [ ] zh-TW + en-US Language 檔各產一份
- [ ] Language XML 中 `Namespace="{ProgId}"`、`Lang="{lang}"`
- [ ] Language Items 含 `Schema.DisplayName` + 每個 Table 的 `Table.X.DisplayName` + 每個 Field 的 `Field.X.Caption`
- [ ] zh-TW 與 en-US **Items.Count 相同**且 key 一一對應
- [ ] FormSchema `CategoryId` 非空（否則 `TableSchemaGenerator` 會拋 `InvalidOperationException`）
- [ ] throw-away test 檔已刪除（`git status` 內不應出現 `Scaffold{ProgId}FixtureFiles.cs`）
- [ ] 三類產出檔在 `git status` 內顯示為新增 / 修改（依衝突策略）

## 不在範圍

- PermissionModels 條目（form ↔ permission model 對應；另開 plan）
- DataSet / DataTable C# 程式碼產出
- Repository / BO 程式碼（屬 `bee-add-bo-method` skill 範疇）
- BlazorPage / FormPage UI 程式碼
- LanguageEnum / ListItems 翻譯（屬獨立議題，schema 內 `LangEnumName` 觸發時再處理）
- 跨 repo 安裝（本 skill 為 bee-library 專案 skill，預設只在本 repo 開啟時可用）
