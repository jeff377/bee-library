using Avalonia.Controls;
using Avalonia.Layout;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
using Avalonia.DemoCenter.Modules.DataEditors;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.DataBinding
{
    /// <summary>
    /// The <c>FormDataObject</c> events, all surfaced in one live log:
    /// <list type="bullet">
    /// <item><c>FieldValueChanged</c> — a master or detail field value changed (detail edits
    /// flow through the DataTable event bridge); args carry TableName / FieldName / Value / Row.</item>
    /// <item><c>RowAdded</c> / <c>RowDeleted</c> — a row was added / deleted (e.g. the detail
    /// grid's add / delete toolbar); args carry TableName / Row.</item>
    /// <item><c>IsDirtyChanged</c> — IsDirty transitioned (clean ↔ dirty).</item>
    /// <item><c>DataSetReplaced</c> — the dataset was replaced or its content reset (e.g.
    /// <c>InitializeNewMaster()</c>); bound editors re-pull.</item>
    /// </list>
    /// </summary>
    public sealed class DataObjectEventsModule : DemoModuleBase
    {
        private const string EmptyLog = "（尚無事件 — 編輯欄位、明細 cell，或按下方按鈕）";

        /// <inheritdoc/>
        public override string Category => "資料繫結";

        /// <inheritdoc/>
        public override string Title => "DataObject 事件";

        /// <inheritdoc/>
        public override string Description =>
            "FormDataObject 的事件：FieldValueChanged（欄位異動）、RowAdded / RowDeleted（明細加/刪列）、"
            + "IsDirtyChanged（髒/乾淨翻轉）、DataSetReplaced（DataSet 置換/重置）。下方記錄即時顯示。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());

            var log = new TextBlock { Text = EmptyLog, Opacity = 0.85 };
            var lines = new List<string>();
            var seq = 0;
            void Append(string entry)
            {
                seq++;
                lines.Insert(0, $"{seq:D2}  {entry}");
                if (lines.Count > 15)
                    lines.RemoveAt(lines.Count - 1);
                log.Text = string.Join(Environment.NewLine, lines);
            }

            // Subscribe after the seed writes in BuildMasterDetail so the log starts empty.
            data.FieldValueChanged += (_, e) => Append($"FieldValueChanged   {e.TableName}.{e.FieldName} = {e.Value}");
            data.RowAdded += (_, e) => Append($"RowAdded            {e.TableName}");
            data.RowDeleted += (_, e) => Append($"RowDeleted          {e.TableName}");
            data.IsDirtyChanged += (_, _) => Append($"IsDirtyChanged      IsDirty = {data.IsDirty}");
            data.DataSetReplaced += (_, _) => Append("DataSetReplaced     （DataSet 置換 / 內容重置）");

            var grid = new GridControl { MinHeight = 150, EditMode = GridEditMode.InCell };
            grid.Bind(data, SampleFormData.BuildPhonesLayout());

            var reinitButton = new Button { Content = "重新初始化主檔 — InitializeNewMaster()", HorizontalAlignment = HorizontalAlignment.Left };
            reinitButton.Click += (_, _) => data.InitializeNewMaster();

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "主檔（Employee）",
                    "編輯下列欄位 → FieldValueChanged。",
                    DataEditorParts.LabeledRow("emp_name", new TextEdit { FieldName = "emp_name" }),
                    DataEditorParts.LabeledRow("dept", new DropDownEdit { FieldName = "dept" }),
                    DataEditorParts.LabeledRow("is_active", new CheckEdit { FieldName = "is_active", Content = "Active" })),
                DataEditorParts.Section(
                    "明細（Phones）",
                    "雙擊 cell 編輯 → FieldValueChanged（經 DataTable 橋接）；grid 工具列 + / 刪 列 → RowAdded / RowDeleted。",
                    grid),
                DataEditorParts.Section(
                    "DataSetReplaced 觸發",
                    "InitializeNewMaster() 重置主檔內容並發 DataSetReplaced；上方主檔欄位會 re-pull 清空。",
                    reinitButton),
                DataEditorParts.Section(
                    "事件記錄（最新在上）",
                    "FormDataObject 的事件統一在此顯示。",
                    log));
        }
    }
}
