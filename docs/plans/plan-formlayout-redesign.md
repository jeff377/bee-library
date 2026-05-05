# 計畫：FormLayout 結構重新設計

**狀態：✅ 已完成（2026-05-05）**

## 背景

`Bee.Definition.Layouts.FormLayout` 是 ERP 表單排版模型，目前 production code **沒有任何 UI 端實際消費** FormLayout 結構（只有 storage/cache 管線在轉送），可視為「建好框架還沒接客」。

既然沒有歷史包袱與既有 UI 消費端，是放手重設計、消除多年累積耦合的好時機。

## 目前結構的問題清單

| # | 問題 | 位置 |
|---|------|------|
| 1 | Master 用 `Groups[0]` 隱含位置耦合 | `FormLayoutGenerator.AddLayoutGroups` |
| 2 | `MainGroup` 字串硬編碼 | `FormLayoutGenerator.AddMasterTableGroup` |
| 3 | `LayoutItem`（field）與 `LayoutGrid`（table grid）同放 `LayoutItemCollection`，語意層級不對等 | `LayoutGroup.Items` |
| 4 | `LayoutItem` 與 `LayoutColumn` 9 個共有欄位重複 | `LayoutItem`, `LayoutColumn` |
| 5 | `ControlType` vs `ColumnControlType` 雙 enum，需要 `ConvertToColumnControlType` facade | `ControlType.cs`, `ColumnControlType.cs` |
| 6 | `Caption` vs `DisplayName` 術語不統一 | `FormLayout`, `LayoutGrid` 用 `DisplayName`；其他用 `Caption` |
| 7 | `ListItems` 在每個 layout 物件雙寫（資料屬性散落） | `LayoutItem.ListItems`, `LayoutColumn.ListItems` |
| 8 | `LookupProgId` / `RelationProgId` 在 layout 上以單一 `ProgId` 表達，與 schema 不對齊且資訊壓平 | `LayoutItem.ProgId` |
| ~~9~~ | ~~`LayoutColumn.Width = 0` 雙語意~~ → 對齊 `FormField.Width` 慣例（`int + [DefaultValue(0)]`，0 表未設不輸出），實務上 0 px 寬度無意義，不構成真實歧義 | `LayoutColumn.Width` |
| 10 | `FormLayout.CreateTime` 孤兒屬性 + 違反 SonarCloud S6562（`DateTime.Now` 未指 `Kind`） | `FormLayout:73` |
| 11 | `IBindFieldControl.FieldValue` / `IBindTableControl.DataTable` 缺 nullable | 介面定義 |
| 12 | `LayoutItemBase` 為空 abstract class（S2094 邊緣） | `LayoutItemBase.cs` |

## 設計理念

### 1. 三層屬性歸屬

| 類別 | 歸屬 | 例子 |
|------|------|------|
| **結構與排版** | Layout 自有 | Section 分組、欄位排序、RowSpan/ColumnSpan、ShowCaption、ColumnCount |
| **渲染外觀** | Layout 物化（generator 從 schema 帶入預設值） | Caption, ControlType, DisplayFormat, NumberFormat, Width, ReadOnly |
| **資料屬性與行為** | FormField（schema） | ListItems, LookupProgId, RelationProgId, DbType, MaxLength, FieldMappings |

### 2. Self-contained 渲染

FormLayout 自帶結構與外觀，**UI 渲染不需要查 schema**；資料行為（驗證、save、lookup mapping）仍走 BO + FormSchema。「self-contained」的範圍 = 結構 + 外觀，不含資料行為。

### 3. 多 Layout per Schema

一個 FormSchema 可推導出 N 個 FormLayout（員工視角 / 主管視角 / 列印視角 / PDA 視角…）。`FormSchema.GetFormLayout(layoutId)` 接受 layoutId 參數。

### 4. `Visible` 雙語意

| 層級 | 語意 | 預設 |
|------|------|------|
| `FormField.Visible = false` | 資料層級不曝光（系統欄位、內部標記）→ generator 直接 skip，不產生 layout 項目 | true |
| `LayoutField.Visible = false` | 排版層級不顯示（layout 內存在但隱藏，例如 `sys_rowid` 在 grid 內供 row binding 但不顯示） | true |

兩層各司其職，互不取代。

## 新結構

```
FormLayout
 ├─ LayoutId          (string)
 ├─ ProgId            (string)            ← 對應 FormSchema.ProgId（同時定位 master table）
 ├─ Caption           (string)            ← 此 layout 的顯示名
 ├─ ColumnCount       (int)               ← 表單統一欄數（預設 2），所有 Section 共用此切割
 ├─ Sections          (LayoutSectionCollection)   ← master 區的多區塊
 │    └─ LayoutSection
 │         ├─ Name, Caption, ShowCaption  ← 不再有 ColumnCount，全表單共用 FormLayout.ColumnCount
 │         └─ Fields  (LayoutFieldCollection)
 │              └─ LayoutField : LayoutFieldBase
 │                   + RowSpan, ColumnSpan   ← 單欄需要更寬時用 ColumnSpan 處理
 └─ Details           (LayoutGridCollection)      ← 0..N detail（永遠在 Master 之後，滿版顯示）
      └─ LayoutGrid                                ← 與 ListLayout 共用 LayoutGrid
           ├─ TableName, Caption, AllowActions
           └─ Columns (LayoutColumnCollection)
                └─ LayoutColumn : LayoutFieldBase
                     + int Width    ← 預設 0 = auto/未設，與 FormField.Width 慣例一致
```

### 設計理念：統一欄寬切割

`ColumnCount` 拉到 FormLayout 層、所有 Section 共用，確保跨 section 直行對齊。單欄需要更寬時，用 `LayoutField.ColumnSpan` 處理（例如備註欄 `ColumnSpan = ColumnCount` 橫跨全列）。Details 不受 `ColumnCount` 限制，固定滿版顯示，永遠排在 Master 之後。

### 渲染目標相容性

同一份 FormLayout 物件設計為**跨 WinForm 與 Web 共用**，不需要針對渲染目標做變體。`ColumnCount` 的語意因目標而異：

| 渲染目標 | `ColumnCount` 解讀 | 排版機制 |
|---------|------------------|---------|
| **WinForm** | 固定欄數 | `TableLayoutPanel` 直接套用 |
| **Web (RWD)** | **最大欄數上限** | 大螢幕用此欄數；窄螢幕（手機/平板）動態縮減欄數（Bootstrap-like grid breakpoint） |

`ColumnSpan` 在 RWD 縮減後的行為由 UI 框架負責（典型實作：`min(ColumnSpan, 實際欄數)` 或自動 wrap），FormLayout 結構不需特別設計。

`FormLayout.ColumnCount` 在 XML doc 應註明此語意（最大欄數上限，非強制），讓未來 UI binding 實作者有明確依據。建議文案：

```csharp
/// <summary>
/// Maximum number of columns for the master form area. All sections share this column division.
/// In WinForm rendering this is treated as a fixed column count;
/// in responsive web rendering this is the upper bound and may shrink on narrow viewports.
/// </summary>
public int ColumnCount { get; set; } = 2;
```

### `LayoutFieldBase`（共用基底，消除 9 屬性重複）

```csharp
public abstract class LayoutFieldBase : CollectionItem
{
    public string FieldName { get; set; }
    public string Caption { get; set; }
    public ControlType ControlType { get; set; }
    public string DisplayFormat { get; set; }
    public string NumberFormat { get; set; }
    public bool Visible { get; set; } = true;     // 預設 true，layout 編輯器可改 false
    public bool ReadOnly { get; set; } = false;
    public PropertyCollection? ExtendedProperties { get; }
}
```

注意：原先 LayoutItem/LayoutColumn 的 `ListItems`、`LookupProgId`、`RelationProgId` **不在 base**（屬於資料屬性，由 FormField 提供）。

## 連動變動

### `FormField` 新增屬性

```csharp
public ListItemCollection? ListItems { get; }   // 跨 layout 共用的下拉選項來源
```

### 檔案異動

**移除：**
- `LayoutItem.cs`（被 `LayoutField` 取代）
- `LayoutItemBase.cs`（被 `LayoutFieldBase` 取代）
- `LayoutItemCollection.cs`（被 `LayoutFieldCollection` 取代）
- `LayoutGroup.cs`、`LayoutGroupCollection.cs`（被 `LayoutSection*` 取代）
- `ColumnControlType.cs`（合併到 `ControlType`）

**新增：**
- `LayoutFieldBase.cs`（abstract 含共有屬性）
- `LayoutField.cs`（master section 內的 field，加 RowSpan/ColumnSpan）
- `LayoutFieldCollection.cs`
- `LayoutSection.cs`
- `LayoutSectionCollection.cs`
- `LayoutGridCollection.cs`
- `LayoutColumnFactory.cs`（共用 helper：兩 generator 共享的 `ToColumn` / `ToField` / `ResolveControlType`）
- `ListLayoutGenerator.cs`（與 `FormLayoutGenerator` 對稱；獨立抽出原 `FormSchema.GetListLayout` 邏輯）

**修改：**
- `FormLayout.cs`：本次重寫，新版按一般慣例不加 `[XmlType]`；移除 `Groups`、`CreateTime`、`DisplayName`；新增 `ProgId`、`Caption`、`ColumnCount`、`Sections`、`Details`
- `LayoutGrid.cs`：`DisplayName` → `Caption`（既有 `[XmlType("LayoutGrid")]` 暫保留，等 XmlType 清理 plan 統一處理）
- `LayoutColumn.cs`：改繼承 `LayoutFieldBase`、`Width` 改 `int + [DefaultValue(0)]`、移除 `Visible`（從 base 繼承）、移除 `ListItems`、合併 `ControlType` enum（既有 `[XmlType]` 暫保留）
- `ControlType.cs`：吸收 `ColumnControlType` 全部值（即原 enum 已包含 `ColumnControlType` 所有值，只需保留並讓 LayoutColumn 也用此 enum）
- `IBindFieldControl.cs`：`object FieldValue` → `object? FieldValue`
- `IBindTableControl.cs`：`DataTable DataTable` → `DataTable? DataTable`
- `FormField.cs`：新增 `ListItems` 屬性 + 序列化標註（既有 `[XmlType]` 暫保留）
- `FormSchema.cs`：新增 `GetFormLayout(string layoutId)` 委派；`GetListLayout()` 改為 `ListLayoutGenerator.Generate(this)` thin wrapper；移除內聯的 `ToLayoutColumn` / `ToColumnControlType`（既有 `[XmlType]` 暫保留）
- `FormLayoutGenerator.cs`：全面重寫、設為 `internal`、維持原位 `Bee.Definition.Layouts`（與產出物 `FormLayout` 同 namespace，對齊 `TableSchemaGenerator` pattern）；改用 `LayoutColumnFactory` 處理欄位轉換

> **注意**：本 plan 不處理冗餘 `[XmlType("ClassName")]` 清理，全專案範圍另立 `plan-xmltype-cleanup.md` 處理。新增類別（`LayoutFieldBase`、`LayoutField`、`LayoutSection`、`LayoutColumnFactory`、`ListLayoutGenerator` 等）一律按一般 .NET 慣例**不加** `[XmlType]`；本次重寫的 `FormLayout` 同樣不加。

## Generator 重寫骨架

設計三個內部類別：
- `LayoutColumnFactory` — 共用 helper，承擔 FormField → LayoutField/LayoutColumn 的轉換與 ControlType 解析
- `FormLayoutGenerator` — 產生單筆模式的 FormLayout（master sections + details）
- `ListLayoutGenerator` — 產生清單模式的 LayoutGrid（master 的清單視圖）

### `LayoutColumnFactory`（共用 helper）

```csharp
// Bee.Definition/Layouts/LayoutColumnFactory.cs
internal static class LayoutColumnFactory
{
    public static LayoutField ToField(FormField f) => new()
    {
        FieldName     = f.FieldName,
        Caption       = f.Caption,
        ControlType   = ResolveControlType(f.ControlType, f.DbType),
        DisplayFormat = f.DisplayFormat,
        NumberFormat  = f.NumberFormat,
    };

    public static LayoutColumn ToColumn(FormField f) => new()
    {
        FieldName     = f.FieldName,
        Caption       = f.Caption,
        ControlType   = ResolveControlType(f.ControlType, f.DbType),
        Width         = f.Width,                        // 0 = auto，與 FormField.Width 慣例一致
        DisplayFormat = f.DisplayFormat,
        NumberFormat  = f.NumberFormat,
    };

    public static ControlType ResolveControlType(ControlType type, FieldDbType dbType)
        => type != ControlType.Auto ? type : dbType switch
        {
            FieldDbType.Boolean  => ControlType.CheckEdit,
            FieldDbType.DateTime => ControlType.DateEdit,
            FieldDbType.Text     => ControlType.MemoEdit,
            _                    => ControlType.TextEdit,
        };
}
```

### `FormLayoutGenerator`（單筆模式）

```csharp
internal static class FormLayoutGenerator
{
    /// <summary>
    /// Detail grid 即使 FormField.Visible=false 也必須補入的系統欄位
    /// (Grid 控制項做 row binding 與 master 關聯需要)。
    /// </summary>
    private static readonly string[] _gridIdentityFields =
        { SysFields.RowId, SysFields.MasterRowId };

    public static FormLayout Generate(FormSchema schema, string layoutId)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var layout = new FormLayout
        {
            LayoutId = layoutId,
            ProgId = schema.ProgId,
            Caption = schema.DisplayName,
            ColumnCount = 2,    // 預設 2 欄式
        };
        AddSections(schema, layout);
        AddDetails(schema, layout);
        return layout;
    }

    // master 區 → 單一預設 section "Main"（layout 編輯器可後續分多 section）
    private static void AddSections(FormSchema schema, FormLayout layout)
    {
        var master = schema.MasterTable;
        if (master?.Fields == null) return;

        var section = new LayoutSection
        {
            Name = "Main",
            Caption = master.DisplayName,
        };
        foreach (var field in master.Fields.Where(f => f.Visible))
            section.Fields!.Add(LayoutColumnFactory.ToField(field));
        if (section.Fields!.Count > 0)
            layout.Sections!.Add(section);
    }

    // detail 區 → 每張非 master 的 table 一個 LayoutGrid
    private static void AddDetails(FormSchema schema, FormLayout layout)
    {
        if (schema.Tables == null) return;
        foreach (var table in schema.Tables.Where(t => t != schema.MasterTable))
        {
            var grid = new LayoutGrid
            {
                TableName = table.TableName,
                Caption = table.DisplayName,
                AllowActions = GridControlAllowActions.All,
            };

            // 1. 加入 Visible=true 的欄位
            foreach (var f in table.Fields!.Where(f => f.Visible))
                grid.Columns!.Add(LayoutColumnFactory.ToColumn(f));

            // 2. 補上 Grid binding 必需的系統欄位（白名單）
            foreach (var sysName in _gridIdentityFields)
            {
                if (table.Fields.Contains(sysName) && !grid.Columns!.Contains(sysName))
                {
                    var col = LayoutColumnFactory.ToColumn(table.Fields[sysName]);
                    col.Visible = false;   // layout-level 隱藏：grid 內存在供 binding 但畫面不顯示
                    grid.Columns.Add(col);
                }
            }

            if (grid.Columns!.Count > 0)
                layout.Details!.Add(grid);
        }
    }
}
```

### `ListLayoutGenerator`（清單模式）

```csharp
// Bee.Definition/Layouts/ListLayoutGenerator.cs
internal static class ListLayoutGenerator
{
    /// <summary>
    /// List grid 即使 FormField.Visible=false 也必須補入的系統欄位
    /// (Grid 控制項做 row binding 需要；list 視角不需 sys_master_rowid)。
    /// </summary>
    private static readonly string[] _gridIdentityFields = { SysFields.RowId };

    public static LayoutGrid Generate(FormSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var master = schema.MasterTable;
        var grid = new LayoutGrid
        {
            TableName = schema.ProgId,
            Caption = master?.DisplayName ?? string.Empty,
            AllowActions = GridControlAllowActions.All,
        };

        if (master?.Fields == null) return grid;

        // 1. 按 ListFields CSV 指定的欄位順序加入（白名單模式）
        string[] fieldNames = StringUtilities.Split(schema.ListFields, ",");
        foreach (var name in fieldNames)
        {
            if (master.Fields.Contains(name))
                grid.Columns!.Add(LayoutColumnFactory.ToColumn(master.Fields[name]));
        }

        // 2. 補上 list grid 必需的系統欄位
        foreach (var sysName in _gridIdentityFields)
        {
            if (master.Fields.Contains(sysName) && !grid.Columns!.Contains(sysName))
            {
                var col = LayoutColumnFactory.ToColumn(master.Fields[sysName]);
                col.Visible = false;
                grid.Columns.Add(col);
            }
        }

        return grid;
    }
}
```

### 兩個 Generator 的差異

| 面向 | `FormLayoutGenerator` | `ListLayoutGenerator` |
|------|----------------------|----------------------|
| 產出 | `FormLayout`（master + details）| `LayoutGrid`（單一 list grid）|
| 欄位選擇 | `FormField.Visible == true`（黑名單）| `FormSchema.ListFields` CSV（白名單）|
| 系統欄位白名單 | `sys_rowid` + `sys_master_rowid` | 僅 `sys_rowid` |
| 多 layout 支援 | 接 `layoutId` 參數 | 暫不支援（一個 schema 一個 list view，未來有需要再加） |

## XML 序列化要求

FormLayout 以 XML 序列化做持久化，所有新類別必須完整支援 XmlSerializer。

### 各類別標註要點

| 類別 | XML 標註要求 |
|------|-------------|
| `LayoutFieldBase` | abstract，**不直接序列化**，不需要 `[XmlType]` 或 `[XmlInclude]`（因新設計沒有「base 型別集合裝多種子類」場景） |
| `LayoutField` | `[XmlType("LayoutField")]`；屬性用 `[XmlAttribute]` + `[DefaultValue]` |
| `LayoutColumn` | `[XmlType("LayoutColumn")]`；`Width` 用 `*Specified` 模式（見下） |
| `LayoutSection` | `[XmlType("LayoutSection")]`；`Fields` 用 `[XmlArrayItem(typeof(LayoutField))]` |
| `LayoutSectionCollection` | `[TreeNode("Sections", false)]` |
| `LayoutFieldCollection` | `[TreeNode("Fields", false)]` |
| `LayoutGridCollection` | `[TreeNode("Details", false)]` |
| `FormLayout` | `Sections` / `Details` 各自 `[XmlArray]` 標註 |
| `FormField.ListItems` | 套用既有的 `SerializationUtilities.IsSerializeEmpty` lazy pattern（與 `RelationFieldMappings` 一致） |

### 預設值與輸出策略

XML 輸出原則：**識別/對應用的主要屬性強制輸出，其他屬性等於預設值就省略**。讀 XML 時靠識別屬性判斷「節點是誰、對應誰」，因此即使值是空字串也要輸出；其他屬性以 `[DefaultValue]` 控制省略以縮小 XML。

實作上：**強制輸出 = 不加 `[DefaultValue]`**；**可省略 = 加 `[DefaultValue(...)]`**。

#### 強制輸出（識別/對應用）

| 類別 | 屬性 | 用途 |
|------|------|------|
| `FormLayout` | `LayoutId` | 識別此 layout（多 layout per schema 時必要） |
| `FormLayout` | `ProgId` | 識別所屬 schema + 定位 master table |
| `LayoutSection` | `Name` | 識別 section（layout 編輯器靠 name 找區塊） |
| `LayoutGrid` | `TableName` | 識別 detail table |
| `LayoutField` | `FieldName` | 識別欄位 |
| `LayoutColumn` | `FieldName` | 識別欄位 |

#### 可省略（加 `[DefaultValue]`，等於預設值不輸出）

| 類別 | 屬性 | 預設值 | 備註 |
|------|------|--------|------|
| `LayoutFieldBase` | `Caption` | `""` | |
| `LayoutFieldBase` | `ControlType` | `ControlType.TextEdit` | 最常見值，generator 物化後幾乎是它 |
| `LayoutFieldBase` | `DisplayFormat` | `""` | |
| `LayoutFieldBase` | `NumberFormat` | `""` | |
| `LayoutFieldBase` | `Visible` | `true` | bool 必加 `[DefaultValue]` 否則永遠輸出雜訊 |
| `LayoutFieldBase` | `ReadOnly` | `false` | 同上 |
| `LayoutField` | `RowSpan` | `1` | |
| `LayoutField` | `ColumnSpan` | `1` | |
| `LayoutColumn` | `Width` | `0` | 與 `FormField.Width` 慣例一致；0 = auto/未設，加 `[DefaultValue(0)]` 即可省略輸出 |
| `LayoutSection` | `Caption` | `""` | |
| `LayoutSection` | `ShowCaption` | `true` | |
| `LayoutGrid` | `Caption` | `""` | |
| `LayoutGrid` | `AllowActions` | `GridControlAllowActions.All` | |
| `FormLayout` | `Caption` | `""` | |
| `FormLayout` | `ColumnCount` | `2` | ERP 表單最常見欄數 |

集合屬性（`Sections` / `Details` / `Fields` / `Columns` / `ExtendedProperties` / `ListItems`）沿用既有 `SerializationUtilities.IsSerializeEmpty` lazy pattern：null 或空時 getter 回傳 null，XML 不輸出該節點。

#### 預期 XML 輸出範例

```xml
<FormLayout LayoutId="default" ProgId="emp" Caption="員工資料" ColumnCount="3">
  <Sections>
    <LayoutSection Name="Main" Caption="基本資料">
      <Fields>
        <LayoutField FieldName="emp_no" Caption="員工編號" />
        <LayoutField FieldName="emp_name" Caption="姓名" />
        <LayoutField FieldName="memo" Caption="備註" ControlType="MemoEdit" RowSpan="3" ColumnSpan="2" />
      </Fields>
    </LayoutSection>
  </Sections>
  <Details>
    <LayoutGrid TableName="emp_skill" Caption="專長">
      <Columns>
        <LayoutColumn FieldName="skill_name" Caption="專長名稱" />
        <LayoutColumn FieldName="proficiency" Caption="熟練度" Width="80" />
        <LayoutColumn FieldName="sys_rowid" Caption="Row ID" Visible="false" />
      </Columns>
    </LayoutGrid>
  </Details>
</FormLayout>
```

每個節點靠識別屬性（`LayoutId` / `ProgId` / `Name` / `TableName` / `FieldName`）能直接判斷是誰、對應誰；其餘屬性僅在偏離預設值時才輸出。

### `LayoutColumn.Width` 處理

採用 `int + [DefaultValue(0)]` 模式，與 `FormField.Width`（schema 端）慣例一致：

```csharp
public class LayoutColumn : LayoutFieldBase
{
    /// <summary>
    /// Column width in pixels. Zero means auto/unset (UI framework decides default).
    /// </summary>
    [XmlAttribute]
    [DefaultValue(0)]
    public int Width { get; set; } = 0;
}
```

#### 為什麼不用 `int? + *Specified` 模式

評估後採用簡單方案，理由：

1. **與 schema 端一致** — `FormField.Width` 已採 `int + [DefaultValue(0)]`，layout 對齊避免雙慣例
2. **簡潔** — 1 個屬性 + 1 個 attribute 標註，vs Specified 模式需 3 個屬性
3. **XML 結果相同** — 兩種模式都能在「未設定」時省略輸出
4. **0 在實務上等同未設** — ERP grid 寬度沒有「0 px」的有效用例，隱藏欄位已用 `Visible=false` 專責處理

#### 流程

| 場景 | `Width` 值 | XML 輸出 |
|------|-----------|---------|
| 預設 | `0` | 不輸出（`[DefaultValue(0)]` 觸發） |
| 設 80 | `80` | `Width="80"` |
| 反序列化沒讀到 `Width` | 保持 `0` | — |
| 反序列化讀到 `Width="80"` | 設為 `80` | — |

### 序列化測試

`DtoSerializationTests.cs` 新增：
- `FormLayout_FullStructure_XmlRoundtrip` — 含 Sections + Details + 各層 fields/columns 的完整 round-trip
- `LayoutColumn_NullWidth_OmitsXmlAttribute` — 驗證 `Width = null` 時 XML 不輸出 `Width` attribute
- `LayoutColumn_NonNullWidth_PreservesValue` — 驗證有值時 round-trip 不丟失
- `FormField_ListItems_XmlRoundtrip` — 驗證新增的 ListItems 屬性正確序列化

## `FormSchema` 對外 entry

兩個 entry 並列為 thin wrapper，分別委派至 `FormLayoutGenerator` / `ListLayoutGenerator`：

```csharp
public FormLayout GetFormLayout(string layoutId = "default")
    => FormLayoutGenerator.Generate(this, layoutId);

public LayoutGrid GetListLayout()
    => ListLayoutGenerator.Generate(this);
```

- `GetListLayout()` 從原內聯實作改為 thin wrapper，內部 `ToLayoutColumn` / `ToColumnControlType` 兩個 helper 隨之刪除（邏輯轉移至 `LayoutColumnFactory`）
- 兩個對外 API 對稱，且符合專案內 `FormTable.GenerateDbTable()` → `TableSchemaGenerator` pattern

## 影響範圍

### Production（src/）

| 檔案 | 變動 |
|------|------|
| `Bee.Definition/Layouts/*` | 全面重組（移除 5 檔、新增 8 檔、修改 4 檔） |
| `Bee.Definition/Forms/FormSchema.cs` | 新增 `GetFormLayout`；`GetListLayout` 改為 thin wrapper、移除內聯 helpers |
| `Bee.Definition/Forms/FormField.cs` | 新增 `ListItems` 屬性 |

### 測試（tests/）

| 檔案 | 變動 |
|------|------|
| `Layouts/LayoutItemTests.cs` | 改名 + 重寫為 `LayoutFieldTests.cs` |
| `Layouts/LayoutGroupTests.cs` | 改名 + 重寫為 `LayoutSectionTests.cs` |
| `Layouts/LayoutGridTests.cs` | `DisplayName` → `Caption` 等小幅調整 |
| `Layouts/LayoutColumnTests.cs` | 對齊新基底、移除 `ListItems` 相關案例（資料屬性已移到 schema）|
| `Layouts/FormLayoutGeneratorTests.cs` | 全面改寫（新結構） |
| `Layouts/FormLayoutGeneratorExtraTests.cs` | 全面改寫 + 新增系統欄位白名單測試 |
| `Layouts/ListLayoutGeneratorTests.cs` | **新增** — list 場景的 generator 測試（含 ListFields 白名單、sys_rowid 補入） |
| `Layouts/LayoutColumnFactoryTests.cs` | **新增** — 共用 helper 的單元測試（ResolveControlType + ToColumn/ToField） |
| `DtoSerializationTests.cs` | `FormLayout_*` 兩個案例改寫 |
| `FormSchemaTests.cs` | `GetListLayout` 相關案例改寫（行為等價於 `ListLayoutGenerator.Generate`） |

### XML 檔案

`tests/Define/` 內無 FormLayout XML 檔案，**不需 migration**。

### 序列化向後相容性

由於 production code 沒有實際 layout XML 在外，**不保留向後相容**，一刀切到新 schema。

## 遷移步驟

依以下順序執行（每步應可獨立 build pass）：

1. **新增基礎結構**
   - `LayoutFieldBase`, `LayoutField`, `LayoutFieldCollection`
   - `LayoutSection`, `LayoutSectionCollection`
   - `LayoutGridCollection`
2. **合併 ControlType**
   - 將 `ColumnControlType` 所有值併入 `ControlType`（已有，無需新增）
   - 刪 `ColumnControlType.cs`
3. **修改 LayoutColumn / LayoutGrid**
   - `LayoutColumn` 改繼承 `LayoutFieldBase`、`Width` 改 `int?`、移除已上提到 base 的屬性
   - `LayoutGrid.DisplayName` → `Caption`
4. **修改 FormField**：新增 `ListItems` 屬性 + 序列化標註
5. **重寫 FormLayout**
   - 移除 `Groups`、`CreateTime`、`DisplayName`
   - 新增 `ProgId`、`Caption`、`Sections`、`Details`
6. **刪除舊類別**
   - `LayoutItem.cs`, `LayoutItemBase.cs`, `LayoutItemCollection.cs`
   - `LayoutGroup.cs`, `LayoutGroupCollection.cs`
7. **新增 `LayoutColumnFactory`**（共用 helper）
   - `ToField` / `ToColumn` / `ResolveControlType`
   - 兩個 generator 都會用到
8. **重寫 `FormLayoutGenerator`**
   - 設為 `internal`
   - 依新結構實作
   - 加入系統欄位白名單邏輯（`sys_rowid` + `sys_master_rowid`）
   - 改用 `LayoutColumnFactory`
9. **新增 `ListLayoutGenerator`**
   - 對稱於 `FormLayoutGenerator`，產生 `LayoutGrid`
   - 白名單僅 `sys_rowid`（list 視角不需 master 關聯）
   - 改用 `LayoutColumnFactory`
10. **改寫 `FormSchema`**
    - 新增 `GetFormLayout()` 委派
    - `GetListLayout()` 改為 `ListLayoutGenerator.Generate(this)` thin wrapper
    - 移除內聯的 `ToLayoutColumn` / `ToColumnControlType` helpers
11. **修正 `IBindFieldControl` / `IBindTableControl`** nullability
12. **重寫測試**（依測試清單）
13. **驗證**
    - `dotnet build --configuration Release` 全 pass
    - `./test.sh` 全 pass
    - 序列化 round-trip 測試 pass

## 已知 Trade-off

| 項目 | 取捨 |
|------|------|
| **Layout 凍結快照 vs schema 即時 view** | 選凍結（self-contained 渲染）；schema 改後 layout 不會自動同步，需重 generate |
| **Layout 內 ListItems vs schema 內 ListItems** | 選 schema（資料屬性跨 layout 一致）；UI 渲染下拉時需另查 schema |
| **共用 `LayoutGrid` for detail + list** | 選共用（語意對等：一張表的欄位網格） |
| **`Sections` 隱含 master**（無 `Master` prefix） | 選擇 — 一個 FormLayout 只有一 master，prefix 不必要；XML doc 會註解清楚 |
| **`ControlType.MemoEdit` 在 grid 場景** | 由 UI 端 fallback 為 TextEdit；不再另立 enum |
| **不保留向後相容** | production 沒人用，新 XML schema 直接生效 |

## 不在本次 Scope（Follow-up）

1. SonarCloud S6562 — `FormSchema.CreateTime`（line 85）同樣有 `DateTime.Now` 問題，可於另一次清理時順帶處理
2. `FormField.Visible` 概念是否該重新命名為 `IsSystemField` 或 `Hidden`（語意更準）— 屬於 schema 層次的討論
3. `FormTable.GenerateDbTable()` / `FormSchema.GetXxxLayout()` 都是 1-line wrapper，是否符合 code-style.md「消除純 facade」 — 整體 facade 治理另案討論
4. ListLayout 是否需要支援多 layout per schema（如 list 也加 `LayoutId` 參數）— 等實際業務需求出現再評估

## 預期成果

- FormLayout 程式碼行數減少 ~30%（消除重複欄位、雙 enum、雙 ListItems）
- Generator 邏輯減少 ~50%（移除 ConvertToColumnControlType、雙 default 邏輯、master skip 處理）
- 結構耦合解除：master/detail 不再用 index、name suffix 識別
- Schema 與 Layout 職責邊界清晰：layout = 結構 + 外觀；schema = 資料定義
- 自然支援多 layout per schema
