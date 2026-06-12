using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// GridControl lookup 欄測試：lookup 欄繞過 DataGrid 編輯管線（column 唯讀）、
    /// cell 顯示 DisplayField 值而非 Guid、可編輯時包 hit-testable host、
    /// 唯讀 / list 模式呈現純文字。
    /// </summary>
    public class GridControlLookupTests
    {
        private static FormSchema BuildOrderSchema()
        {
            var schema = new FormSchema("Order", "訂單") { CategoryId = "company" };
            var master = schema.Tables!.Add("Order", "訂單");
            master.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid));
            var detail = schema.Tables!.Add("OrderLine", "訂單明細");
            detail.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid));
            detail.Fields!.Add(new FormField(SysFields.MasterRowId, "主檔識別", FieldDbType.Guid));
            var productField = new FormField("product_rowid", "商品", FieldDbType.Guid)
            {
                RelationProgId = "Product",
            };
            productField.RelationFieldMappings!.Add(SysFields.Name, "ref_product_name");
            detail.Fields!.Add(productField);
            detail.Fields!.Add(new FormField("ref_product_name", "商品名稱", FieldDbType.String, FieldType.RelationField));
            detail.Fields!.Add(new FormField("qty", "數量", FieldDbType.Integer));
            return schema;
        }

        private static (GridControl grid, FormDataObject dataObject, DataRow line) BindDetailGrid()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var lineTable = dataObject.DataSet.Tables["OrderLine"]!;
            var line = lineTable.NewRow();
            line["ref_product_name"] = "商品甲";
            lineTable.Rows.Add(line);

            var layout = schema.GetFormLayout().Details![0];
            var grid = new GridControl { AllowEdit = true, EditMode = GridEditMode.InCell };
            grid.Bind(dataObject, layout);
            return (grid, dataObject, line);
        }

        [Fact]
        [DisplayName("lookup 欄的 DataGrid column 應為唯讀（繞過編輯管線）")]
        public void Bind_LookupColumn_BypassesEditPipeline()
        {
            var (grid, _, _) = BindDetailGrid();

            var lookupColumn = grid.InnerGrid.Columns
                .OfType<DataGridTemplateColumn>()
                .First(c => Equals(c.Header, "商品"));

            Assert.True(lookupColumn.IsReadOnly);
            // 非 lookup 欄不受影響（qty 仍走標準編輯管線）
            var qtyColumn = grid.InnerGrid.Columns
                .OfType<DataGridTemplateColumn>()
                .First(c => Equals(c.Header, "數量"));
            Assert.False(qtyColumn.IsReadOnly);
        }

        [Fact]
        [DisplayName("可編輯 lookup cell 應為 hit-testable host 且顯示 DisplayField 值")]
        public void BuildLookupCell_Editable_WrapsTextInHost()
        {
            var (grid, dataObject, line) = BindDetailGrid();
            var field = dataObject.GetFormField("OrderLine", "product_rowid")!;
            var rowView = line.Table.DefaultView[0];
            var column = grid.Layout!.Columns!.First(c => c.FieldName == "product_rowid");

            var cell = InvokeBuildLookupCell(grid, rowView, column, field);

            var host = Assert.IsType<Border>(cell);
            var text = Assert.IsType<TextBlock>(host.Child);
            Assert.Equal("商品甲", text.Text);
            Assert.NotNull(host.Background);
        }

        [Fact]
        [DisplayName("list 模式（無 data object）lookup 欄 cell 應為純文字顯示 DisplayField")]
        public void BuildLookupCell_ReadOnlyGrid_PlainText()
        {
            var (grid, dataObject, line) = BindDetailGrid();
            var field = dataObject.GetFormField("OrderLine", "product_rowid")!;
            var rowView = line.Table.DefaultView[0];
            var column = grid.Layout!.Columns!.First(c => c.FieldName == "product_rowid");

            // 唯讀 grid（View 模式 / list 模式同路徑）
            grid.AllowEdit = false;

            var cell = InvokeBuildLookupCell(grid, rowView, column, field);

            var text = Assert.IsType<TextBlock>(cell);
            Assert.Equal("商品甲", text.Text);
        }

        [Fact]
        [DisplayName("list 模式 Bind(layout, rows) 的 ButtonEdit 欄文字 cell 應取 DisplayField")]
        public void ListMode_ButtonEditColumn_UsesDisplayFieldText()
        {
            // 無 data object 的 list 模式：lookup 偵測為 null、走文字 branch，
            // 文字欄位仍應取 DisplayField 而非 rowid。
            var schema = BuildOrderSchema();
            var layout = schema.GetFormLayout().Details![0];
            var rows = new DataTable("OrderLine");
            rows.Columns.Add(SysFields.RowId, typeof(Guid));
            rows.Columns.Add("product_rowid", typeof(Guid));
            rows.Columns.Add("ref_product_name", typeof(string));
            rows.Columns.Add("qty", typeof(int));
            rows.Rows.Add(Guid.NewGuid(), Guid.NewGuid(), "商品乙", 3);

            var grid = new GridControl();
            grid.Bind(layout, rows);

            // 透過 column 的 CellTemplate 實際 build 一個 cell 驗證文字來源
            var column = grid.InnerGrid.Columns
                .OfType<DataGridTemplateColumn>()
                .First(c => Equals(c.Header, "商品"));
            var cell = column.CellTemplate!.Build(rows.DefaultView[0]);

            var text = Assert.IsType<TextBlock>(cell);
            Assert.Equal("商品乙", text.Text);
        }

        private static Control InvokeBuildLookupCell(
            GridControl grid, DataRowView rowView, LayoutColumn column, FormField field)
        {
            var method = typeof(GridControl).GetMethod(
                "BuildLookupCell", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (Control)method.Invoke(grid, new object?[] { rowView, column, field })!;
        }
    }
}
