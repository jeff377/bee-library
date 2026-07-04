using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.DemoCenter.Modules.DataEditors;
using Bee.Definition.Forms;
using Bee.Definition.Settings;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Core.Permissions;

namespace Avalonia.DemoCenter.Modules.Permissions
{
    /// <summary>
    /// Interactive front-end permission (capability) simulator. The left panel fakes the role
    /// grants that <c>EnterCompany</c> would normally return; the right panel is a mock purchase-order
    /// form whose toolbar commands and sensitive fields degrade live through the <em>real</em>
    /// <see cref="ElementCapabilityResolver"/> — no back end involved.
    /// </summary>
    /// <remarks>
    /// The capability snapshot is just a <c>Dictionary&lt;modelId, PermissionAction&gt;</c>, so a demo
    /// can fabricate it in memory and exercise the exact same resolver the shipped views use. This is
    /// UX only: in a real app the snapshot arrives from the server on <c>EnterCompany</c> and the back
    /// end remains the authoritative boundary.
    /// </remarks>
    public sealed class PermissionCapabilityModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "權限 Capability";

        /// <inheritdoc/>
        public override string Title => "互動權限模擬器";

        /// <inheritdoc/>
        public override string Description =>
            "左側勾選模擬角色授權（等同 EnterCompany 回傳的 capability 快照），右側「採購單」表單即時降級："
            + "無權的工具列命令隱藏、敏感欄依 Read/Update 呈現 隱藏 / 唯讀 / 可編輯 三態。"
            + "關掉「啟用 capability」→ 快照為 null → 全放行（未進公司 / 未用權限的預設行為）。";

        // The form's own permission model, plus two well-known sensitive-category models.
        private const string FormModel = "PurchaseOrder";
        private const string CostModel = "Cost";
        private const string PiiModel = "PersonalData";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var schema = BuildSchema();

            // ---- Right: the mock form (toolbar commands + fields) ----
            var newBtn = MakeCommand("New", PermissionAction.Create);
            var saveBtn = MakeCommand("Save", PermissionAction.Create | PermissionAction.Update);
            var deleteBtn = MakeCommand("Delete", PermissionAction.Delete);
            var viewBtn = MakeCommand("View", PermissionAction.Read);
            var commands = new[] { newBtn, saveBtn, deleteBtn, viewBtn };
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { newBtn, saveBtn, deleteBtn, viewBtn } };

            var idField = MakeField("sys_id", "單號");
            var vendorField = MakeField("vendor", "供應商");
            var costField = MakeField("unit_cost", "單價（成本）");     // SensitiveCategory=Cost
            var pidField = MakeField("handler_id", "承辦人身分證");     // SensitiveCategory=PersonalData
            var fields = new[] { idField, vendorField, costField, pidField };

            var formBody = new StackPanel { Spacing = 10, Children = { toolbar, idField.Row, vendorField.Row, costField.Row, pidField.Row } };
            var formSection = DataEditorParts.Section("採購單 PO001（PermissionModelId = PurchaseOrder）", null, formBody);

            // ---- Left: simulated grant toggles ----
            var active = new CheckBox { Content = "啟用 capability（否則快照為 null → 全放行）", IsChecked = true };

            // Default scenario shows all three degradation kinds at once: Delete hidden (no Delete
            // grant), cost visible but read-only (Read without Update), PII hidden (no Read).
            var poCreate = Grant("Create", on: true); var poRead = Grant("Read", on: true);
            var poUpdate = Grant("Update", on: true); var poDelete = Grant("Delete");
            var costRead = Grant("Read", on: true); var costUpdate = Grant("Update");
            var piiRead = Grant("Read"); var piiUpdate = Grant("Update");

            var grantsPanel = new StackPanel
            {
                Spacing = 12,
                MinWidth = 260,
                Children =
                {
                    active,
                    GrantSection("採購單 model（PurchaseOrder）— 驅動工具列命令", poCreate, poRead, poUpdate, poDelete),
                    GrantSection("成本分類（Cost）— 驅動「單價」欄", costRead, costUpdate),
                    GrantSection("個資分類（PersonalData）— 驅動「身分證」欄", piiRead, piiUpdate),
                },
            };

            // Recompute the snapshot and re-apply capability to every command and field.
            void Refresh()
            {
                IReadOnlyDictionary<string, PermissionAction>? snapshot = active.IsChecked == true
                    ? new Dictionary<string, PermissionAction>(StringComparer.Ordinal)
                    {
                        [FormModel] = Mask((poCreate, PermissionAction.Create), (poRead, PermissionAction.Read), (poUpdate, PermissionAction.Update), (poDelete, PermissionAction.Delete)),
                        [CostModel] = Mask((costRead, PermissionAction.Read), (costUpdate, PermissionAction.Update)),
                        [PiiModel] = Mask((piiRead, PermissionAction.Read), (piiUpdate, PermissionAction.Update)),
                    }
                    : null; // null snapshot = capability inactive = allow all (the resolver's safe default).

                // Commands: a button carries the action it needs (PermissionScope); hide it when not permitted.
                foreach (var btn in commands)
                    btn.IsVisible = ElementCapabilityResolver.Default.Can(schema, PermissionScope.GetAction(btn), snapshot);

                // Fields: sensitive ones degrade by SensitiveCategory; normal ones resolve to "allowed".
                foreach (var f in fields)
                {
                    var cap = ElementCapabilityResolver.Default.ResolveField(schema, f.FieldName, tableName: string.Empty, snapshot);
                    f.Row.IsVisible = cap.Visible;                       // no Read → hidden
                    f.Editor.IsReadOnly = cap.ReadOnly;                  // Read but no Update → read-only
                    f.Editor.Opacity = cap.ReadOnly ? 0.55 : 1.0;        // visual cue for the read-only state
                }
            }

            active.IsCheckedChanged += (_, _) => Refresh();
            foreach (var cb in new[] { poCreate, poRead, poUpdate, poDelete, costRead, costUpdate, piiRead, piiUpdate })
                cb.IsCheckedChanged += (_, _) => Refresh();
            Refresh();

            var root = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 24,
                Margin = new Thickness(4),
                Children = { DataEditorParts.Section("模擬授權（capability 快照）", null, grantsPanel), formSection },
            };
            return new ScrollViewer { Content = root };
        }

        // Builds an in-code FormSchema: a PurchaseOrder-bound form with two sensitive fields.
        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("PO001", "採購單") { PermissionModelId = FormModel };
            var table = schema.Tables!.Add("PO001", "採購單");
            table.Fields!.Add("sys_id", "單號", Bee.Base.Data.FieldDbType.String);
            table.Fields!.Add("vendor", "供應商", Bee.Base.Data.FieldDbType.String);
            table.Fields!.Add("unit_cost", "單價", Bee.Base.Data.FieldDbType.Decimal).SensitiveCategory = SensitiveCategory.Cost;
            table.Fields!.Add("handler_id", "承辦人身分證", Bee.Base.Data.FieldDbType.String).SensitiveCategory = SensitiveCategory.PersonalData;
            return schema;
        }

        private static Button MakeCommand(string text, PermissionAction action)
        {
            var button = new Button { Content = text };
            PermissionScope.SetAction(button, action);   // the same tagging the shipped views use
            return button;
        }

        private static FieldRow MakeField(string fieldName, string caption)
        {
            var editor = new TextBox { Text = caption, MinWidth = 200 };
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
