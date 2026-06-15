using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Views;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// Behaviour + structure tests for the unified single-record <see cref="FormView"/>: the
    /// CRUD / three-mode flow (View / Add / Edit, Save / Cancel / Back), the
    /// <see cref="FormView.FormMode"/> broadcast through the ambient <see cref="FormScope"/>,
    /// and the record rendering (master sections + detail grids). A <see cref="TestFormView"/>
    /// overrides the <c>Resolve*</c> hooks so the static <c>ClientInfo</c> is never touched.
    /// </summary>
    public class FormViewTests
    {
        private const string TestProgId = "Category";
        private static readonly Type[] s_buildInputParams = [typeof(LayoutField)];

        // ---- shared builders ----

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            schema.ListFields = "sys_id,sys_name";
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("sys_id", "Category Code", FieldDbType.String);
            master.Fields.Add("sys_name", "Category Name", FieldDbType.String);
            return schema;
        }

        private static FormSchema BuildRenderSchema()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_id", "ID", FieldDbType.String);
            master.Fields.Add("is_active", "Active", FieldDbType.Boolean);
            master.Fields.Add("hire_date", "Hire Date", FieldDbType.Date);
            return schema;
        }

        private static FormSchema BuildRenderSchemaWithDetail()
        {
            var schema = BuildRenderSchema();
            var detail = schema.Tables!.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);
            return schema;
        }

        private static DataSet BuildServerDataSet(Guid rowId, string name)
        {
            var dataSet = new DataSet(TestProgId);
            var master = new DataTable(TestProgId);
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add("sys_name", typeof(string));
            master.Rows.Add(rowId, name);
            dataSet.Tables.Add(master);
            dataSet.AcceptChanges();
            return dataSet;
        }

        private static TestFormView BuildView(FakeFormApiConnector connector)
            => new() { Schema = BuildSchema(), FormConnector = connector };

        // ---- reflection helpers ----

        private static void InvokePrivate(FormView view, string methodName, params object[] args)
        {
            var method = typeof(FormView).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(view, args);
        }

        private static async Task InvokePrivateAsync(FormView view, string methodName, params object[] args)
        {
            var method = typeof(FormView).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            await (Task)method!.Invoke(view, args)!;
        }

        private static T GetPrivateField<T>(FormView view, string fieldName)
        {
            var field = typeof(FormView).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return (T)field!.GetValue(view)!;
        }

        private static void SetPrivateField(FormView view, string fieldName, object? value)
        {
            var field = typeof(FormView).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field!.SetValue(view, value);
        }

        // Renders the form directly against a layout + data object (bypassing the backend
        // round-trip) by seeding the private state and invoking the private Rebuild, then
        // returns the form-body host panel for structural assertions.
        private static StackPanel RenderForm(FormView view, FormLayout layout, FormDataObject dataObject)
        {
            SetPrivateField(view, "_formLayout", layout);
            SetPrivateField(view, "_dataObject", dataObject);
            InvokePrivate(view, "Rebuild");
            return GetPrivateField<StackPanel>(view, "_formHost");
        }

        private static Control InvokeBuildInputControl(FormView view, FormDataObject dataObject, LayoutField field)
        {
            SetPrivateField(view, "_dataObject", dataObject);
            var method = typeof(FormView).GetMethod(
                "BuildInputControl", BindingFlags.NonPublic | BindingFlags.Instance, null, s_buildInputParams, null);
            Assert.NotNull(method);
            return (Control)method!.Invoke(view, new object[] { field })!;
        }

        private static StackPanel GetGridToolbar(GridControl grid)
            => Assert.IsType<StackPanel>(Assert.IsType<DockPanel>(grid.Content).Children[0]);

        // ---- type / property surface ----

        [Fact]
        [DisplayName("FormView 為 Avalonia UserControl 子類別")]
        public void Type_IsUserControlSubclass()
        {
            Assert.True(typeof(UserControl).IsAssignableFrom(typeof(FormView)));
        }

        [Theory]
        [InlineData(nameof(FormView.ProgId), "ProgIdProperty")]
        [InlineData(nameof(FormView.AccessToken), "AccessTokenProperty")]
        [InlineData(nameof(FormView.Schema), "SchemaProperty")]
        [InlineData(nameof(FormView.FormConnector), "FormConnectorProperty")]
        [InlineData(nameof(FormView.FormMode), "FormModeProperty")]
        [InlineData(nameof(FormView.DetailEditMode), "DetailEditModeProperty")]
        [DisplayName("公開屬性皆有對應的 StyledProperty 註冊")]
        public void PublicProperties_HaveMatchingStyledProperty(string propertyName, string styledPropertyFieldName)
        {
            var property = typeof(FormView).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);

            var styled = typeof(FormView).GetField(styledPropertyFieldName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(styled);
            Assert.True(typeof(AvaloniaProperty).IsAssignableFrom(styled!.FieldType));
        }

        // ---- CRUD / mode flow ----

        [Fact]
        [DisplayName("ViewAsync 載入記錄並進入 View 模式")]
        public async Task ViewAsync_LoadsRecord_EntersViewMode()
        {
            var rowId = Guid.NewGuid();
            Guid requestedId = Guid.Empty;
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = id =>
                {
                    requestedId = id;
                    return new GetDataResponse { DataSet = BuildServerDataSet(id, "Beverages") };
                },
            };
            var view = BuildView(connector);

            await view.ViewAsync(rowId);

            Assert.Equal(rowId, requestedId);
            Assert.Equal(SingleFormMode.View, view.FormMode);
            Assert.NotNull(view.DataObject?.MasterRow);
        }

        [Fact]
        [DisplayName("EditAsync 載入記錄並進入 Edit 模式")]
        public async Task EditAsync_LoadsRecord_EntersEditMode()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "Beverages") },
            };
            var view = BuildView(connector);

            await view.EditAsync(rowId);

            Assert.Equal(SingleFormMode.Edit, view.FormMode);
        }

        [Fact]
        [DisplayName("NewAsync 取得空白資料並進入 Add 模式")]
        public async Task NewAsync_GetsBlank_EntersAddMode()
        {
            var called = false;
            var connector = new FakeFormApiConnector
            {
                GetNewDataHandler = () =>
                {
                    called = true;
                    return new GetNewDataResponse { DataSet = BuildServerDataSet(Guid.NewGuid(), "New") };
                },
            };
            var view = BuildView(connector);

            await view.NewAsync();

            Assert.True(called);
            Assert.Equal(SingleFormMode.Add, view.FormMode);
        }

        [Fact]
        [DisplayName("Save 成功呼叫 SaveAsync 並觸發 Saved")]
        public async Task Save_OnSuccess_CallsSaveAndRaisesSaved()
        {
            var rowId = Guid.NewGuid();
            var saveCalled = false;
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "Beverages") },
                SaveHandler = _ =>
                {
                    saveCalled = true;
                    return new SaveResponse();
                },
            };
            var view = BuildView(connector);
            await view.EditAsync(rowId);

            var saved = false;
            view.Saved += (_, _) => saved = true;

            await InvokePrivateAsync(view, "OnSaveClickedAsync");

            Assert.True(saveCalled);
            Assert.True(saved);
        }

        [Fact]
        [DisplayName("Save 失敗觸發 ErrorOccurred 且不觸發 Saved")]
        public async Task Save_OnFailure_RaisesErrorAndNotSaved()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "Beverages") },
                SaveHandler = _ => throw new InvalidOperationException("backend rejected"),
            };
            var view = BuildView(connector);
            await view.EditAsync(rowId);

            var saved = false;
            Exception? reported = null;
            view.Saved += (_, _) => saved = true;
            view.ErrorOccurred += (_, ex) => reported = ex;

            await InvokePrivateAsync(view, "OnSaveClickedAsync");

            Assert.False(saved);
            Assert.IsType<InvalidOperationException>(reported);
        }

        [Fact]
        [DisplayName("Cancel / Back 觸發 Closed")]
        public async Task Close_RaisesClosed()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "Beverages") },
            };
            var view = BuildView(connector);
            await view.EditAsync(rowId);

            var closed = false;
            view.Closed += (_, _) => closed = true;

            InvokePrivate(view, "OnCloseClicked");

            Assert.True(closed);
        }

        [Fact]
        [DisplayName("View 模式只顯示返回鈕；Edit 模式顯示儲存/取消")]
        public async Task Toolbar_ReflectsMode()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "Beverages") },
            };
            var view = BuildView(connector);

            await view.ViewAsync(rowId);
            Assert.True(GetPrivateField<Button>(view, "_backButton").IsVisible);
            Assert.False(GetPrivateField<Button>(view, "_saveButton").IsVisible);
            Assert.False(GetPrivateField<Button>(view, "_cancelButton").IsVisible);

            await view.EditAsync(rowId);
            Assert.True(GetPrivateField<Button>(view, "_saveButton").IsVisible);
            Assert.True(GetPrivateField<Button>(view, "_cancelButton").IsVisible);
            Assert.False(GetPrivateField<Button>(view, "_backButton").IsVisible);
        }

        // ---- FormMode broadcast (ported from the retired SingleFormBase) ----

        [Fact]
        [DisplayName("預設 View 並把 ambient scope 釘到 View")]
        public void Defaults_PinScopeToView()
        {
            var view = new TestFormView();
            Assert.Equal(SingleFormMode.View, view.FormMode);
            Assert.Equal(SingleFormMode.View, FormScope.GetFormMode(view));
        }

        [Fact]
        [DisplayName("OnFormModeChanged hook 於每次模式變更後被呼叫")]
        public void OnFormModeChanged_InvokedPerChange()
        {
            var view = new TestFormView();

            view.FormMode = SingleFormMode.Add;
            view.FormMode = SingleFormMode.Edit;

            Assert.Equal(2, view.ModeChangedCount);
            Assert.Equal(SingleFormMode.Edit, view.LastMode);
        }

        [Fact]
        [DisplayName("子樹編輯器隨 FormMode 廣播切換唯讀（真實管線）")]
        public void FormModeBroadcast_TogglesRenderedEditor()
        {
            var schema = BuildRenderSchema();
            var layout = new FormLayout { ColumnCount = 1 };
            var section = new LayoutSection { Caption = "Main", ShowCaption = false };
            section.Fields!.Add(new LayoutField { FieldName = "emp_id", Caption = "ID", ControlType = ControlType.TextEdit });
            layout.Sections!.Add(section);

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();

            var view = new TestFormView();
            view.FormMode = SingleFormMode.View;
            var host = RenderForm(view, layout, dataObject);

            var editor = FindDescendant<TextEdit>(host);
            Assert.NotNull(editor);
            Assert.True(editor!.IsReadOnly);

            view.FormMode = SingleFormMode.Edit;
            Assert.False(editor.IsReadOnly);

            view.FormMode = SingleFormMode.View;
            Assert.True(editor.IsReadOnly);
        }

        [Fact]
        [DisplayName("程式寫入欄位後綁定編輯器即時刷新（lookup 寫回的基礎）")]
        public void SetField_RefreshesBoundEditor()
        {
            var schema = BuildRenderSchema();
            var layout = new FormLayout { ColumnCount = 1 };
            var section = new LayoutSection { Caption = "Main", ShowCaption = false };
            section.Fields!.Add(new LayoutField { FieldName = "emp_id", Caption = "ID", ControlType = ControlType.TextEdit });
            layout.Sections!.Add(section);

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();

            var view = new TestFormView { FormMode = SingleFormMode.Edit };
            var host = RenderForm(view, layout, dataObject);
            var editor = FindDescendant<TextEdit>(host);
            Assert.NotNull(editor);

            dataObject.SetField("emp_id", "HELLO");

            Assert.Equal("HELLO", editor!.Text);
        }

        // ---- rendering (ported from the retired DynamicForm) ----

        [Fact]
        [DisplayName("渲染後每個 Section 一個 Border")]
        public void Render_OneBorderPerSection()
        {
            var schema = BuildRenderSchema();
            var layout = schema.GetFormLayout();
            Assert.NotEmpty(layout.Sections!);

            var dataObject = new FormDataObject(schema);
            var host = RenderForm(new TestFormView(), layout, dataObject);

            Assert.Equal(layout.Sections!.Count, host.Children.Count);
            Assert.All(host.Children, child => Assert.IsType<Border>(child));
        }

        [Fact]
        [DisplayName("欄位數超過 ColumnCount 時 Grid 換行配置")]
        public void Render_FieldGridWrapsWhenFieldsExceedColumnCount()
        {
            var layout = new FormLayout { ColumnCount = 2 };
            var section = new LayoutSection { Caption = "Main", ShowCaption = false };
            section.Fields!.Add(new LayoutField { FieldName = "emp_id", Caption = "ID" });
            section.Fields.Add(new LayoutField { FieldName = "is_active", Caption = "Active" });
            section.Fields.Add(new LayoutField { FieldName = "hire_date", Caption = "Hire Date" });
            layout.Sections!.Add(section);

            var host = RenderForm(new TestFormView(), layout, new FormDataObject(BuildRenderSchema()));

            var border = Assert.IsType<Border>(host.Children[0]);
            var sectionStack = Assert.IsType<StackPanel>(border.Child);
            var grid = Assert.IsType<Grid>(sectionStack.Children[^1]);

            Assert.Equal(2, grid.ColumnDefinitions.Count);
            Assert.Equal(2, grid.RowDefinitions.Count);
            Assert.Equal(3, grid.Children.Count);
        }

        [Fact]
        [DisplayName("FormLayout.Details 在 master sections 之後渲染為綁定的 GridControl")]
        public void Render_DetailsRenderDetailGridControl()
        {
            var layout = new FormLayout { ColumnCount = 2 };
            var section = new LayoutSection { Caption = "Main", ShowCaption = false };
            section.Fields!.Add(new LayoutField { FieldName = "emp_id", Caption = "ID" });
            layout.Sections!.Add(section);
            var detail = new LayoutGrid("EmployeePhone", "Phones");
            detail.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            layout.Details!.Add(detail);

            var dataObject = new FormDataObject(BuildRenderSchemaWithDetail());
            dataObject.InitializeNewMaster();
            var host = RenderForm(new TestFormView(), layout, dataObject);

            Assert.Equal(2, host.Children.Count);
            var detailStack = Assert.IsType<StackPanel>(Assert.IsType<Border>(host.Children[1]).Child);
            Assert.Equal("Phones", Assert.IsType<TextBlock>(detailStack.Children[0]).Text);
            var grid = Assert.IsType<GridControl>(detailStack.Children[1]);
            Assert.Same(dataObject.DataSet.Tables["EmployeePhone"], grid.DataTable);
        }

        [Fact]
        [DisplayName("DetailEditMode=EditForm 時明細 grid 唯讀且工具列含 Edit 鈕")]
        public void Render_DetailEditMode_EditForm()
        {
            var layout = new FormLayout { ColumnCount = 2 };
            var detail = new LayoutGrid("EmployeePhone", "Phones");
            detail.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            layout.Details!.Add(detail);

            var dataObject = new FormDataObject(BuildRenderSchemaWithDetail());
            dataObject.InitializeNewMaster();
            var view = new TestFormView { DetailEditMode = GridEditMode.EditForm };
            var host = RenderForm(view, layout, dataObject);

            var detailStack = Assert.IsType<StackPanel>(Assert.IsType<Border>(host.Children[0]).Child);
            var grid = Assert.IsType<GridControl>(detailStack.Children[1]);
            Assert.Equal(GridEditMode.EditForm, grid.EditMode);
            Assert.True(grid.InnerGrid.IsReadOnly);
            Assert.True(Assert.IsType<Button>(GetGridToolbar(grid).Children[1]).IsVisible);
        }

        [Theory]
        [InlineData(ControlType.CheckEdit, typeof(CheckEdit))]
        [InlineData(ControlType.DateEdit, typeof(DateEdit))]
        [InlineData(ControlType.YearMonthEdit, typeof(YearMonthEdit))]
        [InlineData(ControlType.MemoEdit, typeof(MemoEdit))]
        [InlineData(ControlType.DropDownEdit, typeof(DropDownEdit))]
        [InlineData(ControlType.ButtonEdit, typeof(ButtonEdit))]
        [InlineData(ControlType.TextEdit, typeof(TextEdit))]
        [InlineData(ControlType.Auto, typeof(TextEdit))]
        [DisplayName("BuildInputControl 依 ControlType 分派對應的 field editor")]
        public void BuildInputControl_DispatchesByControlType(ControlType controlType, Type expectedControlType)
        {
            var dataObject = new FormDataObject(BuildRenderSchema());
            dataObject.InitializeNewMaster();
            var field = new LayoutField { FieldName = "emp_id", ControlType = controlType };

            var control = InvokeBuildInputControl(new TestFormView(), dataObject, field);

            Assert.IsType(expectedControlType, control);
        }

        [Fact]
        [DisplayName("ReadOnly 欄位建立的編輯器為唯讀")]
        public void BuildInputControl_ReadOnlyField_CreatesReadOnlyTextEdit()
        {
            var dataObject = new FormDataObject(BuildRenderSchema());
            dataObject.InitializeNewMaster();
            var field = new LayoutField { FieldName = "emp_id", ReadOnly = true };

            var control = Assert.IsType<TextEdit>(InvokeBuildInputControl(new TestFormView(), dataObject, field));

            Assert.True(control.IsReadOnly);
        }

        [Fact]
        [DisplayName("CheckEdit 勾選變更會回寫 DataObject 欄位")]
        public void BuildInputControl_CheckEdit_WritesBackToDataObject()
        {
            var dataObject = new FormDataObject(BuildRenderSchema());
            dataObject.InitializeNewMaster();
            var field = new LayoutField { FieldName = "is_active", ControlType = ControlType.CheckEdit };

            var checkBox = Assert.IsType<CheckEdit>(InvokeBuildInputControl(new TestFormView(), dataObject, field));
            checkBox.IsChecked = true;

            Assert.Equal("True", dataObject.GetField("is_active"));
            Assert.True(dataObject.IsDirty);
        }

        private static T? FindDescendant<T>(Control root) where T : Control
        {
            if (root is T match) return match;
            if (root is Panel panel)
            {
                foreach (var child in panel.Children)
                    if (child is Control c && FindDescendant<T>(c) is { } found)
                        return found;
            }
            else if (root is Border { Child: Control borderChild })
            {
                return FindDescendant<T>(borderChild);
            }
            else if (root is ContentControl { Content: Control content })
            {
                return FindDescendant<T>(content);
            }
            return null;
        }

        /// <summary>
        /// Overrides the <c>Resolve*</c> hooks so tests never read <c>ClientInfo</c>, and
        /// surfaces the <c>OnFormModeChanged</c> hook for assertions.
        /// </summary>
        private sealed class TestFormView : FormView
        {
            public int ModeChangedCount { get; private set; }
            public SingleFormMode? LastMode { get; private set; }

            protected override SystemApiConnector? ResolveSystemConnector() => null;

            protected override FormApiConnector ResolveFormConnector(string progId)
                => throw new InvalidOperationException("ClientInfo fallback must not be reached in unit tests.");

            protected override Guid ResolveAccessToken() => Guid.Empty;

            protected override void OnFormModeChanged(SingleFormMode formMode)
            {
                base.OnFormModeChanged(formMode);
                ModeChangedCount++;
                LastMode = formMode;
            }
        }

        /// <summary>
        /// Test double overriding every virtual round-trip on <see cref="FormApiConnector"/>
        /// so the base <c>LocalApiProvider</c> is never reached.
        /// </summary>
        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<Guid, GetDataResponse>? GetDataHandler { get; set; }
            public Func<GetNewDataResponse>? GetNewDataHandler { get; set; }
            public Func<DataSet, SaveResponse>? SaveHandler { get; set; }

            public override Task<GetDataResponse> GetDataAsync(Guid rowId)
                => Task.FromResult((GetDataHandler ?? (_ => new GetDataResponse()))(rowId));

            public override Task<GetNewDataResponse> GetNewDataAsync()
                => Task.FromResult((GetNewDataHandler ?? (() => new GetNewDataResponse()))());

            public override Task<SaveResponse> SaveAsync(DataSet dataSet)
                => Task.FromResult((SaveHandler ?? (_ => new SaveResponse()))(dataSet));
        }
    }
}
