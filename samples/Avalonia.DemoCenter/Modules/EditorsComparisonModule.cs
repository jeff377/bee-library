using System.Data;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Avalonia.DemoCenter.Modules
{
    /// <summary>
    /// The original gallery content, as a Demo Center module: one section per
    /// <see cref="ControlType"/>, each comparing the native Avalonia control (left)
    /// against the inherited field editor (right) in the normal and read-only states.
    /// The editors bind through <see cref="FormScope"/> ambient scope, so the shell's
    /// global FormMode switch drives them live.
    /// </summary>
    public sealed class EditorsComparisonModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "總覽";

        /// <inheritdoc/>
        public override string ControlName => "原生 vs 繼承";

        /// <inheritdoc/>
        public override string ScenarioTitle => "全部編輯器";

        /// <inheritdoc/>
        public override string Description =>
            "每個 ControlType 一個區塊：左欄原生 Avalonia 控件、右欄繼承控件（FormScope ambient 綁定）。"
            + "工具列的 FormMode 切換會即時驅動右欄繼承控件的唯讀 / 編輯外觀。";

        /// <inheritdoc/>
        public override Control BuildView() => new ViewBuilder().Build();

        /// <summary>
        /// Builds one independent comparison view with its own in-memory data object.
        /// A fresh instance per <see cref="BuildView"/> keeps repeated builds isolated.
        /// </summary>
        private sealed class ViewBuilder
        {
            private static readonly string[] s_fieldNames =
                ["emp_name", "notes", "emp_code", "hire_date", "pay_month", "dept_id", "is_active"];

            private readonly FormDataObject _dataObject;
            private readonly StackPanel _galleryHost = new() { Spacing = 16, Margin = new Thickness(0, 0, 12, 0) };
            private readonly TextBlock _valuesText = new()
            {
                FontFamily = new FontFamily("Menlo,Consolas,monospace"),
                FontSize = 12,
            };

            public ViewBuilder()
            {
                _dataObject = BuildDataObject();
            }

            public Control Build()
            {
                BuildGallery();
                UpdateValues();
                _dataObject.FieldValueChanged += (_, _) => UpdateValues();

                var root = new DockPanel { Margin = new Thickness(16) };

                var valuesBorder = new Border
                {
                    Margin = new Thickness(0, 12, 0, 0),
                    Padding = new Thickness(12),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Gray,
                    CornerRadius = new CornerRadius(4),
                    Child = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock { Text = "FormDataObject 即時欄位值", FontWeight = FontWeight.SemiBold },
                            _valuesText,
                        },
                    },
                };
                DockPanel.SetDock(valuesBorder, Dock.Bottom);
                root.Children.Add(valuesBorder);

                root.Children.Add(new ScrollViewer { Content = _galleryHost });

                // Ambient scope: descendants with a FieldName bind themselves on attach.
                // The shell owns FormMode; this view owns the data object.
                FormScope.SetDataObject(root, _dataObject);
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
                // The detail table carries one field per in-cell editor control type so the
                // GridControl section exercises every editing path.
                var phones = schema.Tables.Add("Phones", "Phones");
                phones.Fields!.Add("phone", "Phone", FieldDbType.String);
                var phoneType = phones.Fields.Add("type", "Type", FieldDbType.String);
                phoneType.ListItems!.Add("Office", "Office");
                phoneType.ListItems.Add("Mobile", "Mobile");
                phoneType.ListItems.Add("Home", "Home");
                phones.Fields.Add("is_primary", "Primary", FieldDbType.Boolean);
                phones.Fields.Add("valid_from", "Valid From", FieldDbType.Date);
                phones.Fields.Add("bill_month", "Bill Month", FieldDbType.String);

                var dataObject = new FormDataObject(schema);
                dataObject.InitializeNewMaster();
                var phoneTable = dataObject.DataSet.Tables["Phones"]!;
                phoneTable.Rows.Add("02-1234-5678", "Office", true,
                    new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Unspecified), "2026-06");
                phoneTable.Rows.Add("0912-345-678", "Mobile", false,
                    new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified), "2026-07");
                dataObject.SetField("emp_name", "Alice Chen");
                dataObject.SetField("notes", "Multi-line memo content.");
                dataObject.SetField("emp_code", "EMP-001");
                dataObject.SetField("hire_date", "2026-06-11");
                dataObject.SetField("pay_month", "2026-06");
                dataObject.SetField("dept_id", "IT");
                dataObject.SetField("is_active", bool.TrueString);
                return dataObject;
            }

            private void BuildGallery()
            {
                AddSection("TextEdit ← TextBox",
                    new TextBox { Text = "Alice Chen" },
                    new TextBox { Text = "Alice Chen", IsReadOnly = true },
                    new TextEdit { FieldName = "emp_name" },
                    BindReadOnly(new TextEdit(), "emp_name"));

                AddSection("MemoEdit ← TextBox（多行）",
                    new TextBox
                    {
                        Text = "Multi-line memo content.",
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                        MinHeight = 60,
                    },
                    new TextBox
                    {
                        Text = "Multi-line memo content.",
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                        MinHeight = 60,
                        IsReadOnly = true,
                    },
                    new MemoEdit { FieldName = "notes" },
                    BindReadOnly(new MemoEdit(), "notes"));

                AddSection("ButtonEdit ← TextBox（InnerRightContent 按鈕）",
                    new TextBox { Text = "EMP-001", InnerRightContent = new Button { Content = "…", Focusable = false } },
                    new TextBox
                    {
                        Text = "EMP-001",
                        InnerRightContent = new Button { Content = "…", Focusable = false, IsEnabled = false },
                        IsReadOnly = true,
                    },
                    new ButtonEdit { FieldName = "emp_code" },
                    BindReadOnly(new ButtonEdit(), "emp_code"));

                AddSection("DateEdit ← DatePicker",
                    new DatePicker { SelectedDate = new DateTimeOffset(new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero) },
                    new DatePicker
                    {
                        SelectedDate = new DateTimeOffset(new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                        IsEnabled = false,
                    },
                    new DateEdit { FieldName = "hire_date" },
                    BindReadOnly(new DateEdit(), "hire_date"));

                AddSection("YearMonthEdit ← DatePicker（DayVisible=False）",
                    new DatePicker
                    {
                        DayVisible = false,
                        SelectedDate = new DateTimeOffset(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                    },
                    new DatePicker
                    {
                        DayVisible = false,
                        SelectedDate = new DateTimeOffset(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                        IsEnabled = false,
                    },
                    new YearMonthEdit { FieldName = "pay_month" },
                    BindReadOnly(new YearMonthEdit(), "pay_month"));

                AddSection("DropDownEdit ← ComboBox",
                    new ComboBox { ItemsSource = new[] { "Human Resources", "Information Technology", "Finance" }, SelectedIndex = 1 },
                    new ComboBox
                    {
                        ItemsSource = new[] { "Human Resources", "Information Technology", "Finance" },
                        SelectedIndex = 1,
                        IsEnabled = false,
                    },
                    new DropDownEdit { FieldName = "dept_id" },
                    BindReadOnly(new DropDownEdit(), "dept_id"));

                AddSection("CheckEdit ← CheckBox",
                    new CheckBox { Content = "Active", IsChecked = true },
                    new CheckBox { Content = "Active", IsChecked = true, IsEnabled = false },
                    new CheckEdit { FieldName = "is_active", Content = "Active" },
                    WithContent(BindReadOnly(new CheckEdit(), "is_active"), "Active"));

                AddGridSection();
                AddEditFormSection();
            }

            // The EditForm comparison binds a GridControl directly in EditForm mode, so the
            // toolbar, double-tap gesture and RowEditDialog wiring are exactly what production
            // detail grids get.
            private void AddEditFormSection()
            {
                var detail = new LayoutGrid("Phones", "Phones");
                detail.Columns!.Add(new LayoutColumn("phone", "Phone", ControlType.TextEdit));
                detail.Columns.Add(new LayoutColumn("type", "Type", ControlType.DropDownEdit));
                detail.Columns.Add(new LayoutColumn("is_primary", "Primary", ControlType.CheckEdit));
                detail.Columns.Add(new LayoutColumn("valid_from", "Valid From", ControlType.DateEdit));
                detail.Columns.Add(new LayoutColumn("bill_month", "Bill Month", ControlType.YearMonthEdit));

                var grid = new GridControl { MinHeight = 120, EditMode = GridEditMode.EditForm };
                grid.Bind(_dataObject, detail);

                var section = new StackPanel { Spacing = 8 };
                section.Children.Add(new TextBlock
                {
                    Text = "GridControl EditForm 模式（grid 唯讀，雙擊列或 Edit 鈕開彈窗編輯）",
                    FontSize = 15,
                    FontWeight = FontWeight.Bold,
                });
                section.Children.Add(grid);

                _galleryHost.Children.Add(new Border
                {
                    Padding = new Thickness(12),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Gray,
                    CornerRadius = new CornerRadius(4),
                    Child = section,
                });
            }

            private void AddGridSection()
            {
                var phoneTable = _dataObject.DataSet.Tables["Phones"]!;

                // One column per supported in-cell editor: TextEdit / DropDownEdit /
                // CheckEdit / DateEdit / YearMonthEdit. Double-click a cell on the bound
                // grid to edit.
                var layout = new LayoutGrid("Phones", "Phones");
                // Read-only column: its header caption is coloured to mark the whole column
                // read-only (cells render plain text and cannot be click-to-edited).
                layout.Columns!.Add(new LayoutColumn("phone", "Phone（唯讀）", ControlType.TextEdit) { ReadOnly = true });
                // Required column: its header caption is coloured blue (vs the read-only brown).
                layout.Columns.Add(new LayoutColumn("type", "Type（必填）", ControlType.DropDownEdit) { Required = true });
                layout.Columns.Add(new LayoutColumn("is_primary", "Primary（Check）", ControlType.CheckEdit));
                layout.Columns.Add(new LayoutColumn("valid_from", "Valid From（Date）", ControlType.DateEdit));
                layout.Columns.Add(new LayoutColumn("bill_month", "Bill Month（YearMonth）", ControlType.YearMonthEdit));

                var bound = new GridControl { MinHeight = 120 };
                bound.Bind(_dataObject, layout);

                // TableName only: the grid binds through the ambient FormScope on attach
                // and generates plain columns from the table when no layout is supplied.
                var ambient = new GridControl { TableName = "Phones", MinHeight = 120 };

                var grid = new Grid { ColumnSpacing = 12, RowSpacing = 8 };
                grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(90)));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                for (var i = 0; i < 3; i++)
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                AddCell(grid, 0, 1, new TextBlock { Text = "原生控件", FontWeight = FontWeight.SemiBold });
                AddCell(grid, 0, 2, new TextBlock { Text = "繼承控件（已綁定）", FontWeight = FontWeight.SemiBold });
                AddCell(grid, 1, 0, new TextBlock { Text = "Layout 綁定", Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center });
                AddCell(grid, 1, 1, BuildNativeGrid(phoneTable));
                AddCell(grid, 1, 2, bound);
                AddCell(grid, 2, 0, new TextBlock { Text = "Ambient", Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center });
                AddCell(grid, 2, 1, new TextBlock
                {
                    Text = "（原生無對應 — 右側為 TableName 自動綁定，欄位由表自動產生）",
                    Opacity = 0.6,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                });
                AddCell(grid, 2, 2, ambient);

                var section = new StackPanel { Spacing = 8 };
                section.Children.Add(new TextBlock { Text = "GridControl ← DataGrid", FontSize = 15, FontWeight = FontWeight.Bold });
                section.Children.Add(grid);

                _galleryHost.Children.Add(new Border
                {
                    Padding = new Thickness(12),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Gray,
                    CornerRadius = new CornerRadius(4),
                    Child = section,
                });
            }

            private static DataGrid BuildNativeGrid(DataTable table)
            {
                var grid = new DataGrid
                {
                    IsReadOnly = true,
                    AutoGenerateColumns = false,
                    CanUserResizeColumns = true,
                    SelectionMode = DataGridSelectionMode.Single,
                    MinHeight = 120,
                };
                foreach (DataColumn column in table.Columns)
                {
                    var name = column.ColumnName;
                    grid.Columns.Add(new DataGridTemplateColumn
                    {
                        Header = name,
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                        CellTemplate = new FuncDataTemplate<DataRowView>(
                            (row, _) => new TextBlock
                            {
                                Text = row?.Row[name]?.ToString() ?? string.Empty,
                                Margin = new Thickness(8, 4),
                            },
                            supportsRecycling: true),
                    });
                }
                grid.ItemsSource = table.DefaultView;
                return grid;
            }

            private T BindReadOnly<T>(T editor, string fieldName)
                where T : Control, IFieldEditor
            {
                editor.Bind(_dataObject, new LayoutField { FieldName = fieldName, ReadOnly = true });
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

                _galleryHost.Children.Add(new Border
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

            private void UpdateValues()
            {
                _valuesText.Text = string.Join(
                    Environment.NewLine,
                    s_fieldNames.Select(f => $"{f,-10} = {_dataObject.GetField(f)}"));
            }
        }
    }
}
