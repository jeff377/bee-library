using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Avalonia.DemoCenter.Modules.ControlTypes
{
    /// <summary>
    /// Native vs inherited comparison for field-level controls (<see cref="IBindFieldControl"/>
    /// / <see cref="IFieldEditor"/>): one section per <see cref="ControlType"/>, the native
    /// Avalonia control (left) beside the inherited field editor (right), in the normal and
    /// read-only states. The inherited editors bind through <see cref="FormScope"/> ambient
    /// scope, so the toolbar's FormMode also drives them.
    /// </summary>
    public sealed class FieldControlComparisonModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "控件類型";

        /// <inheritdoc/>
        public override string Title => "原生 vs 繼承（BindFieldControl）";

        /// <inheritdoc/>
        public override string Description =>
            "欄位級控件（IBindFieldControl / IFieldEditor）並排原生 Avalonia 控件，每個 ControlType "
            + "各含「一般」與「唯讀」兩列。繼承控件以 StyleKeyOverride 沿用原生 ControlTheme。";

        /// <inheritdoc/>
        public override Control BuildView() => new ViewBuilder().Build();

        private sealed class ViewBuilder
        {
            private readonly FormDataObject _data;
            private readonly StackPanel _host = new() { Spacing = 16, Margin = new Thickness(4) };

            public ViewBuilder()
            {
                _data = BuildDataObject();
            }

            public Control Build()
            {
                AddSection("TextEdit ← TextBox",
                    new TextBox { Text = "Alice Chen" },
                    new TextBox { Text = "Alice Chen", IsReadOnly = true },
                    new TextEdit { FieldName = "emp_name" },
                    BindReadOnly(new TextEdit(), "emp_name"));

                AddSection("MemoEdit ← TextBox（多行）",
                    new TextBox { Text = "Multi-line memo content.", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 60 },
                    new TextBox { Text = "Multi-line memo content.", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 60, IsReadOnly = true },
                    new MemoEdit { FieldName = "notes" },
                    BindReadOnly(new MemoEdit(), "notes"));

                AddSection("ButtonEdit ← TextBox（InnerRightContent 按鈕）",
                    new TextBox { Text = "EMP-001", InnerRightContent = new Button { Content = "…", Focusable = false } },
                    new TextBox { Text = "EMP-001", InnerRightContent = new Button { Content = "…", Focusable = false, IsEnabled = false }, IsReadOnly = true },
                    new ButtonEdit { FieldName = "emp_code" },
                    BindReadOnly(new ButtonEdit(), "emp_code"));

                AddSection("DateEdit ← DatePicker",
                    new DatePicker { SelectedDate = new DateTimeOffset(new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero) },
                    new DatePicker { SelectedDate = new DateTimeOffset(new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero), IsEnabled = false },
                    new DateEdit { FieldName = "hire_date" },
                    BindReadOnly(new DateEdit(), "hire_date"));

                AddSection("YearMonthEdit ← DatePicker（DayVisible=False）",
                    new DatePicker { DayVisible = false, SelectedDate = new DateTimeOffset(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero) },
                    new DatePicker { DayVisible = false, SelectedDate = new DateTimeOffset(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero), IsEnabled = false },
                    new YearMonthEdit { FieldName = "pay_month" },
                    BindReadOnly(new YearMonthEdit(), "pay_month"));

                AddSection("DropDownEdit ← ComboBox",
                    new ComboBox { ItemsSource = new[] { "Human Resources", "Information Technology", "Finance" }, SelectedIndex = 1 },
                    new ComboBox { ItemsSource = new[] { "Human Resources", "Information Technology", "Finance" }, SelectedIndex = 1, IsEnabled = false },
                    new DropDownEdit { FieldName = "dept_id" },
                    BindReadOnly(new DropDownEdit(), "dept_id"));

                AddSection("CheckEdit ← CheckBox",
                    new CheckBox { Content = "Active", IsChecked = true },
                    new CheckBox { Content = "Active", IsChecked = true, IsEnabled = false },
                    new CheckEdit { FieldName = "is_active", Content = "Active" },
                    WithContent(BindReadOnly(new CheckEdit(), "is_active"), "Active"));

                var root = new ScrollViewer { Content = _host };
                FormScope.SetDataObject(root, _data);
                return root;
            }

            private static FormDataObject BuildDataObject()
            {
                var schema = new FormSchema("Gallery", "Gallery");
                var master = schema.Tables!.Add("Gallery", "Gallery");
                var empName = master.Fields!.Add("emp_name", "Name", FieldDbType.String);
                empName.MaxLength = 20;
                master.Fields.Add("notes", "Notes", FieldDbType.String);
                master.Fields.Add("emp_code", "Code", FieldDbType.String);
                master.Fields.Add("hire_date", "Hire Date", FieldDbType.Date);
                master.Fields.Add("pay_month", "Pay Month", FieldDbType.String);
                var dept = master.Fields.Add("dept_id", "Department", FieldDbType.String);
                dept.ListItems!.Add("HR", "Human Resources");
                dept.ListItems.Add("IT", "Information Technology");
                dept.ListItems.Add("FIN", "Finance");
                master.Fields.Add("is_active", "Active", FieldDbType.Boolean);

                var data = new FormDataObject(schema);
                data.InitializeNewMaster();
                data.SetField("emp_name", "Alice Chen");
                data.SetField("notes", "Multi-line memo content.");
                data.SetField("emp_code", "EMP-001");
                data.SetField("hire_date", "2026-06-11");
                data.SetField("pay_month", "2026-06");
                data.SetField("dept_id", "IT");
                data.SetField("is_active", bool.TrueString);
                return data;
            }

            private T BindReadOnly<T>(T editor, string fieldName)
                where T : Control, IFieldEditor
            {
                editor.Bind(_data, new LayoutField { FieldName = fieldName, ReadOnly = true });
                return editor;
            }

            private static CheckEdit WithContent(CheckEdit editor, string content)
            {
                editor.Content = content;
                return editor;
            }

            private void AddSection(string title, Control nativeNormal, Control nativeRestricted,
                Control editorNormal, Control editorRestricted)
            {
                var grid = new Grid { ColumnSpacing = 12, RowSpacing = 8 };
                grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(90)));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                for (var i = 0; i < 3; i++)
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                AddCell(grid, 0, 1, new TextBlock { Text = "原生控件", FontWeight = FontWeight.SemiBold });
                AddCell(grid, 0, 2, new TextBlock { Text = "繼承控件（已綁定）", FontWeight = FontWeight.SemiBold });
                AddCell(grid, 1, 0, new TextBlock { Text = "一般", Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center });
                AddCell(grid, 1, 1, nativeNormal);
                AddCell(grid, 1, 2, editorNormal);
                AddCell(grid, 2, 0, new TextBlock { Text = "唯讀 / 停用", Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center });
                AddCell(grid, 2, 1, nativeRestricted);
                AddCell(grid, 2, 2, editorRestricted);

                var section = new StackPanel { Spacing = 8 };
                section.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeight.Bold });
                section.Children.Add(grid);

                _host.Children.Add(new Border
                {
                    Padding = new Thickness(12),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Gray,
                    CornerRadius = new CornerRadius(4),
                    Child = section,
                });
            }

            private static void AddCell(Grid grid, int row, int column, Control control)
            {
                Grid.SetRow(control, row);
                Grid.SetColumn(control, column);
                grid.Children.Add(control);
            }
        }
    }
}
