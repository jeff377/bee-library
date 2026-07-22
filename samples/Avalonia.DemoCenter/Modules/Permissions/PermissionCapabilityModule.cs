using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.DemoCenter.Modules.DataEditors;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.DataObjects;
using Bee.UI.Core.Permissions;

namespace Avalonia.DemoCenter.Modules.Permissions
{
    /// <summary>
    /// Interactive front-end permission (capability) simulator over a master-detail form. The left
    /// panel fakes the role grants that <c>EnterCompany</c> would normally return; the right panel is
    /// a mock purchase order (master fields + a real detail <see cref="GridControl"/>) whose toolbar
    /// commands and sensitive fields degrade live through the <em>real</em>
    /// <see cref="ElementCapabilityResolver"/> — no back end involved.
    /// </summary>
    /// <remarks>
    /// The capability snapshot is just a <c>Dictionary&lt;modelId, PermissionAction&gt;</c>, so a demo
    /// can fabricate it in memory and exercise the exact same resolver the shipped views use. Field
    /// permission spans both tables: the master's <c>承辦人身分證</c> (PersonalData) and the detail
    /// grid's <c>單價</c> (Cost) column each degrade by their category — the resolver reverse-looks-up
    /// the <see cref="FormField.SensitiveCategory"/> from the schema by (table, field). UX only; in a
    /// real app the snapshot comes from the server and the back end stays authoritative.
    /// </remarks>
    public sealed class PermissionCapabilityModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "權限 Capability";

        /// <inheritdoc/>
        public override string Title => "互動權限模擬器（主檔／明細）";

        /// <inheritdoc/>
        public override string Description =>
            "左側勾選模擬角色授權（等同 EnterCompany 回傳的 capability 快照），右側「採購單」主檔＋明細即時降級："
            + "無權的工具列命令隱藏；主檔敏感欄（承辦人身分證＝PersonalData）依 Read/Update 呈現 隱藏／唯讀／可編輯；"
            + "明細 Grid 的敏感欄（單價＝Cost）無 Read 時整欄隱藏。預設全授權（完整表單）——"
            + "取消勾選觀察對應元素即時降級；關掉「啟用 capability」→ 快照 null → 全放行。";

        private const string FormModel = "PurchaseOrder";
        private const string CostModel = "Cost";
        private const string PiiModel = "PersonalData";
        private const string MasterTable = "PO001";
        private const string DetailTable = "PO001_Item";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var schema = BuildSchema();
            var detailData = BuildData(schema);

            // ---- Right: master fields + a real detail grid ----
            var newBtn = MakeCommand("New", PermissionAction.Create);
            var saveBtn = MakeCommand("Save", PermissionAction.Create | PermissionAction.Update);
            var deleteBtn = MakeCommand("Delete", PermissionAction.Delete);
            var viewBtn = MakeCommand("View", PermissionAction.Read);
            var commands = new[] { newBtn, saveBtn, deleteBtn, viewBtn };
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { newBtn, saveBtn, deleteBtn, viewBtn } };

            var idField = MakeField("sys_id", "單號");
            var vendorField = MakeField("vendor", "供應商");
            var handlerField = MakeField("handler_id", "承辦人身分證");   // SensitiveCategory=PersonalData
            var masterFields = new[] { idField, vendorField, handlerField };

            var masterBody = new StackPanel { Spacing = 10, Children = { toolbar, idField.Row, vendorField.Row, handlerField.Row } };
            var detailHost = new Border { MinHeight = 150 };   // holds the (re)built detail grid

            var formBody = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    DataEditorParts.Section("主檔 PO001（PermissionModelId = PurchaseOrder）", null, masterBody),
                    DataEditorParts.Section("明細 PO001_Item（GridControl，單價欄 = Cost）", null, detailHost),
                },
            };

            // ---- Left: simulated grant toggles ----
            var active = new CheckBox { Content = "啟用 capability（否則快照為 null → 全放行）", IsChecked = true };

            // Everything granted by default → the full form shows on open; uncheck a grant to
            // watch that command / field degrade live.
            var poCreate = Grant("Create", on: true); var poRead = Grant("Read", on: true);
            var poUpdate = Grant("Update", on: true); var poDelete = Grant("Delete", on: true);
            var costRead = Grant("Read", on: true); var costUpdate = Grant("Update", on: true);
            var piiRead = Grant("Read", on: true); var piiUpdate = Grant("Update", on: true);

            var grantsPanel = new StackPanel
            {
                Spacing = 12,
                MinWidth = 260,
                Children =
                {
                    active,
                    GrantSection("採購單 model（PurchaseOrder）— 驅動工具列命令", poCreate, poRead, poUpdate, poDelete),
                    GrantSection("個資分類（PersonalData）— 驅動主檔「身分證」欄", piiRead, piiUpdate),
                    GrantSection("成本分類（Cost）— 驅動明細「單價」欄", costRead, costUpdate),
                },
            };

            void Refresh()
            {
                IReadOnlyDictionary<string, PermissionAction>? snapshot = active.IsChecked == true
                    ? new Dictionary<string, PermissionAction>(StringComparer.Ordinal)
                    {
                        [FormModel] = Mask((poCreate, PermissionAction.Create), (poRead, PermissionAction.Read), (poUpdate, PermissionAction.Update), (poDelete, PermissionAction.Delete)),
                        [PiiModel] = Mask((piiRead, PermissionAction.Read), (piiUpdate, PermissionAction.Update)),
                        [CostModel] = Mask((costRead, PermissionAction.Read), (costUpdate, PermissionAction.Update)),
                    }
                    : null; // null snapshot = capability inactive = allow all (the resolver's safe default).

                // Commands: each button carries the action it needs (PermissionScope); hide it when not permitted.
                foreach (var btn in commands)
                    btn.IsVisible = ElementCapabilityResolver.Default.Can(schema, PermissionScope.GetAction(btn), snapshot);

                // Master fields: sensitive ones degrade by SensitiveCategory (table = master).
                foreach (var f in masterFields)
                {
                    var cap = ElementCapabilityResolver.Default.ResolveField(schema, f.FieldName, tableName: MasterTable, snapshot);
                    f.Row.IsVisible = cap.Visible;
                    f.Editor.IsReadOnly = cap.ReadOnly;
                    f.Editor.Opacity = cap.ReadOnly ? 0.55 : 1.0;
                }

                // Detail grid: rebuild from a fresh layout with capability applied to its columns
                // (table = detail). This mirrors LayoutCapabilityApplier, which is internal to the lib.
                detailHost.Child = BuildDetailGrid(schema, detailData, snapshot);
            }

            active.IsCheckedChanged += (_, _) => Refresh();
            foreach (var cb in new[] { poCreate, poRead, poUpdate, poDelete, costRead, costUpdate, piiRead, piiUpdate })
                cb.IsCheckedChanged += (_, _) => Refresh();
            Refresh();

            // Two bounded columns (grants | form) so the detail DataGrid gets a real width and lays
            // out all its columns, instead of being starved inside an unbounded horizontal stack.
            var root = new Grid { Margin = new Thickness(4), ColumnSpacing = 24 };
            root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var grantsSection = DataEditorParts.Section("模擬授權（capability 快照）", null, grantsPanel);
            Grid.SetColumn(grantsSection, 0);
            Grid.SetColumn(formBody, 1);
            root.Children.Add(grantsSection);
            root.Children.Add(formBody);
            return new ScrollViewer { Content = root };
        }

        // Master PO001 (with a PersonalData field) + detail PO001_Item (with a Cost column).
        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema(MasterTable, "採購單") { PermissionModelId = FormModel };

            var master = schema.Tables!.Add(MasterTable, "採購單");
            master.Fields!.Add("sys_id", "單號", FieldDbType.String);
            master.Fields!.Add("vendor", "供應商", FieldDbType.String);
            master.Fields!.Add("handler_id", "承辦人身分證", FieldDbType.String).SensitiveCategory = SensitiveCategory.PersonalData;

            var detail = schema.Tables.Add(DetailTable, "明細");
            detail.Fields!.Add("item_name", "品項", FieldDbType.String);
            detail.Fields!.Add("qty", "數量", FieldDbType.Integer);
            detail.Fields!.Add("unit_cost", "單價", FieldDbType.Decimal).SensitiveCategory = SensitiveCategory.Cost;

            return schema;
        }

        private static FormDataObject BuildData(FormSchema schema)
        {
            var data = new FormDataObject(schema);
            data.InitializeNewMaster();
            data.SetField("sys_id", "PO-2026-001");
            data.SetField("vendor", "宏碁股份有限公司");
            data.SetField("handler_id", "A123456789");

            var items = data.DataSet.Tables[DetailTable]!;
            items.Rows.Add("螺絲 M4", 100, 3.5m);
            items.Rows.Add("墊片 8mm", 50, 1.2m);
            return data;
        }

        // Builds a fresh detail grid each refresh: a fresh layout with capability applied to its
        // columns (narrowing only), then bound. Fresh layout means re-granting Cost.Read re-shows the column.
        private static Control BuildDetailGrid(FormSchema schema, FormDataObject data, IReadOnlyDictionary<string, PermissionAction>? snapshot)
        {
            var layout = new LayoutGrid(DetailTable, "明細");
            layout.Columns!.Add(new LayoutColumn("item_name", "品項", ControlType.TextEdit));
            layout.Columns.Add(new LayoutColumn("qty", "數量", ControlType.TextEdit));
            layout.Columns.Add(new LayoutColumn("unit_cost", "單價", ControlType.TextEdit));

            foreach (var column in layout.Columns)
            {
                var cap = ElementCapabilityResolver.Default.ResolveField(schema, column.FieldName, DetailTable, snapshot);
                if (!cap.Visible) { column.Visible = false; }
                if (cap.ReadOnly) { column.ReadOnly = true; }
            }

            var grid = new GridControl { MinHeight = 140 };
            grid.Bind(data, layout);
            return grid;
        }

        private static Button MakeCommand(string text, PermissionAction action)
        {
            var button = new Button { Content = text };
            PermissionScope.SetAction(button, action);   // the same tagging the shipped views use
            return button;
        }

        private static FieldRow MakeField(string fieldName, string caption)
        {
            var editor = new TextBox { MinWidth = 200 };
            var row = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock { Text = caption, Opacity = 0.7 },
                    editor,
                },
            };
            return new FieldRow(fieldName, row, editor);
        }

        private static CheckBox Grant(string action, bool on = false) => new() { Content = action, IsChecked = on };

        private static Border GrantSection(string title, params Control[] toggles)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            foreach (var t in toggles) stack.Children.Add(t);
            return DataEditorParts.Section(title, null, stack);
        }

        private static PermissionAction Mask(params (CheckBox toggle, PermissionAction action)[] items)
        {
            var mask = PermissionAction.None;
            foreach (var (toggle, action) in items)
                if (toggle.IsChecked == true) { mask |= action; }
            return mask;
        }

        // Holds a field's caption+editor row together so capability can toggle both.
        private sealed class FieldRow(string fieldName, Control row, TextBox editor)
        {
            public string FieldName { get; } = fieldName;
            public Control Row { get; } = row;
            public TextBox Editor { get; } = editor;
        }
    }
}
