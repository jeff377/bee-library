using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// FormSchema.GetListLayout (透過 ListLayoutGenerator) 的單元測試。
    /// </summary>
    public class ListLayoutGeneratorTests
    {
        [Fact]
        [DisplayName("GetListLayout 應產出 TableName=ProgId、Caption=主檔 DisplayName 的 LayoutGrid")]
        public void GetListLayout_ProducesGridWithProgIdAndCaption()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "sys_id,sys_name" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add("sys_id", "編號", FieldDbType.String);
            table.Fields!.Add("sys_name", "名稱", FieldDbType.String);

            var grid = schema.GetListLayout();

            Assert.Equal("Demo", grid.TableName);
            Assert.Equal("示範", grid.Caption);
            Assert.Equal(GridControlAllowActions.All, grid.AllowActions);
        }

        [Fact]
        [DisplayName("GetListLayout 應依 ListFields 順序加入 Columns")]
        public void GetListLayout_PreservesListFieldsOrder()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "sys_name,sys_id" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add("sys_id", "編號", FieldDbType.String);
            table.Fields!.Add("sys_name", "名稱", FieldDbType.String);

            var grid = schema.GetListLayout();

            Assert.Equal("sys_name", grid.Columns![0].FieldName);
            Assert.Equal("sys_id", grid.Columns![1].FieldName);
        }

        [Fact]
        [DisplayName("GetListLayout 主檔含 sys_rowid 應補入隱藏欄並設為 Visible=false")]
        public void GetListLayout_AddsHiddenRowIdColumn()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "sys_id" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField(SysFields.RowId, "Row ID", FieldDbType.Guid) { Visible = false });
            table.Fields!.Add("sys_id", "編號", FieldDbType.String);

            var grid = schema.GetListLayout();

            var rowId = grid.Columns!.FirstOrDefault(c => c.FieldName == SysFields.RowId);
            Assert.NotNull(rowId);
            Assert.False(rowId!.Visible);
        }

        [Fact]
        [DisplayName("GetListLayout 主檔不含 sys_rowid 時不會強行補入")]
        public void GetListLayout_NoRowIdField_DoesNotAddIt()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "sys_id" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add("sys_id", "編號", FieldDbType.String);

            var grid = schema.GetListLayout();

            Assert.DoesNotContain(grid.Columns!, c => c.FieldName == SysFields.RowId);
        }

        [Fact]
        [DisplayName("GetListLayout 不會自動補入 sys_master_rowid（只有 FormLayout 會）")]
        public void GetListLayout_DoesNotAddMasterRowId()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "sys_id" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField(SysFields.MasterRowId, "Master Row ID", FieldDbType.Guid) { Visible = false });
            table.Fields!.Add("sys_id", "編號", FieldDbType.String);

            var grid = schema.GetListLayout();

            Assert.DoesNotContain(grid.Columns!, c => c.FieldName == SysFields.MasterRowId);
        }

        [Theory]
        [InlineData(ControlType.TextEdit)]
        [InlineData(ControlType.ButtonEdit)]
        [InlineData(ControlType.DateEdit)]
        [InlineData(ControlType.YearMonthEdit)]
        [InlineData(ControlType.DropDownEdit)]
        [InlineData(ControlType.CheckEdit)]
        [InlineData(ControlType.MemoEdit)]
        [DisplayName("GetListLayout 非 Auto ControlType 應原樣保留")]
        public void GetListLayout_NonAutoControlType_PreservesValue(ControlType controlType)
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "col" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("col", "欄", FieldDbType.String) { ControlType = controlType });

            var grid = schema.GetListLayout();

            var column = grid.Columns!.First(c => c.FieldName == "col");
            Assert.Equal(controlType, column.ControlType);
        }

        [Theory]
        [InlineData(FieldDbType.Boolean, ControlType.CheckEdit)]
        [InlineData(FieldDbType.DateTime, ControlType.DateEdit)]
        [InlineData(FieldDbType.Text, ControlType.MemoEdit)]
        [InlineData(FieldDbType.String, ControlType.TextEdit)]
        [DisplayName("GetListLayout ControlType=Auto 應依 DbType 推導對應控制型態")]
        public void GetListLayout_AutoControlType_MapsDbType(FieldDbType dbType, ControlType expected)
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "col" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("col", "欄", dbType) { ControlType = ControlType.Auto });

            var grid = schema.GetListLayout();

            var column = grid.Columns!.First(c => c.FieldName == "col");
            Assert.Equal(expected, column.ControlType);
        }

        [Fact]
        [DisplayName("GetListLayout Width 應原樣傳遞至 LayoutColumn")]
        public void GetListLayout_PassesWidth()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "col" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("col", "欄", FieldDbType.String) { Width = 200 });

            var grid = schema.GetListLayout();

            Assert.Equal(200, grid.Columns!.First(c => c.FieldName == "col").Width);
        }

        [Fact]
        [DisplayName("GetListLayout 應傳遞 DisplayFormat 與 NumberFormat")]
        public void GetListLayout_PropagatesDisplayAndNumberFormats()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "amount" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("amount", "金額", FieldDbType.Decimal)
            {
                DisplayFormat = "{0:C}",
                NumberFormat = "Amount"
            });

            var grid = schema.GetListLayout();

            var column = grid.Columns!.First(c => c.FieldName == "amount");
            Assert.Equal("{0:C}", column.DisplayFormat);
            Assert.Equal("Amount", column.NumberFormat);
        }

        [Fact]
        [DisplayName("GetListLayout ListFields 含不存在欄位時應靜默略過（白名單模式）")]
        public void GetListLayout_UnknownFieldInListFields_Skipped()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "known,missing" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("known", "已知", FieldDbType.String));

            var grid = schema.GetListLayout();

            Assert.Single(grid.Columns!);
            Assert.Equal("known", grid.Columns![0].FieldName);
        }

        [Fact]
        [DisplayName("GetListLayout 無 MasterTable 時應回傳空欄位的 LayoutGrid")]
        public void GetListLayout_NoMasterTable_ReturnsEmptyGrid()
        {
            // ProgId 與 Tables 不匹配 → MasterTable 為 null
            var schema = new FormSchema("NotExist", "不存在") { ListFields = "sys_id" };

            var grid = schema.GetListLayout();

            Assert.Equal("NotExist", grid.TableName);
            Assert.Empty(grid.Columns!);
        }
    }
}
