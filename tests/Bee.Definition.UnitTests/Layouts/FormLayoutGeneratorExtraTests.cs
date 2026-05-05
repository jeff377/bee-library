using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// FormLayoutGenerator 補強測試：
    /// 涵蓋 Detail Grid 的 ControlType 推導、系統欄位白名單（sys_rowid + sys_master_rowid），
    /// 以及主檔／明細表的空欄位邊界。
    /// </summary>
    public class FormLayoutGeneratorExtraTests
    {
        private static FormSchema BuildMasterDetailSchema()
        {
            var schema = new FormSchema("Order", "訂單");
            var master = schema.Tables!.Add("Order", "訂單");
            master.Fields!.Add("sys_id", "編號", FieldDbType.String);
            return schema;
        }

        [Theory]
        [InlineData(ControlType.TextEdit, ControlType.TextEdit)]
        [InlineData(ControlType.ButtonEdit, ControlType.ButtonEdit)]
        [InlineData(ControlType.DateEdit, ControlType.DateEdit)]
        [InlineData(ControlType.YearMonthEdit, ControlType.YearMonthEdit)]
        [InlineData(ControlType.DropDownEdit, ControlType.DropDownEdit)]
        [InlineData(ControlType.CheckEdit, ControlType.CheckEdit)]
        [InlineData(ControlType.MemoEdit, ControlType.MemoEdit)]
        [DisplayName("GetFormLayout 明細表非 Auto ControlType 應原樣保留")]
        public void GetFormLayout_DetailTable_NonAutoControlType_PreservesValue(
            ControlType controlType, ControlType expected)
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("col", "欄", FieldDbType.String) { ControlType = controlType });

            var layout = schema.GetFormLayout("default");

            var grid = layout.Details!.First(g => g.TableName == "OrderItem");
            Assert.Equal(expected, grid.Columns![0].ControlType);
        }

        [Theory]
        [InlineData(FieldDbType.Boolean, ControlType.CheckEdit)]
        [InlineData(FieldDbType.DateTime, ControlType.DateEdit)]
        [InlineData(FieldDbType.Text, ControlType.MemoEdit)]
        [InlineData(FieldDbType.String, ControlType.TextEdit)]
        [InlineData(FieldDbType.Integer, ControlType.TextEdit)]
        [DisplayName("GetFormLayout 明細表 ControlType=Auto 應依 DbType 推導 ControlType")]
        public void GetFormLayout_DetailTable_AutoControlType_MapsDbTypeToControlType(
            FieldDbType dbType, ControlType expected)
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("col", "欄", dbType) { ControlType = ControlType.Auto });

            var layout = schema.GetFormLayout("default");

            var grid = layout.Details!.First(g => g.TableName == "OrderItem");
            Assert.Equal(expected, grid.Columns![0].ControlType);
        }

        [Fact]
        [DisplayName("GetFormLayout 明細表 Width 應原樣傳遞至 LayoutColumn")]
        public void GetFormLayout_DetailTable_PassesWidth()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("col", "欄", FieldDbType.String) { Width = 250 });

            var layout = schema.GetFormLayout("default");

            var grid = layout.Details!.First(g => g.TableName == "OrderItem");
            Assert.Equal(250, grid.Columns![0].Width);
        }

        [Fact]
        [DisplayName("GetFormLayout 明細表 Width=0 應保留 0（auto/未設）")]
        public void GetFormLayout_DetailTable_WidthZero_StaysZero()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add("col", "欄", FieldDbType.String);

            var layout = schema.GetFormLayout("default");

            var grid = layout.Details!.First(g => g.TableName == "OrderItem");
            Assert.Equal(0, grid.Columns![0].Width);
        }

        [Fact]
        [DisplayName("GetFormLayout 明細表應跳過 Visible=false 的非系統欄位")]
        public void GetFormLayout_DetailTable_SkipsInvisibleNonSystemFields()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("hidden", "隱藏", FieldDbType.String) { Visible = false });
            detail.Fields!.Add("visible", "顯示", FieldDbType.String);

            var layout = schema.GetFormLayout("default");

            var grid = layout.Details!.First(g => g.TableName == "OrderItem");
            Assert.Single(grid.Columns!);
            Assert.Equal("visible", grid.Columns![0].FieldName);
        }

        [Fact]
        [DisplayName("GetFormLayout 明細表所有欄位皆不可見時不應新增對應 Grid")]
        public void GetFormLayout_DetailTable_AllFieldsInvisible_DoesNotAddGrid()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("hidden", "隱藏", FieldDbType.String) { Visible = false });

            var layout = schema.GetFormLayout("default");

            Assert.Empty(layout.Details!);
        }

        [Fact]
        [DisplayName("GetFormLayout 明細表存在 sys_rowid 應補入 Grid 並設為 Visible=false")]
        public void GetFormLayout_DetailTable_AddsHiddenRowIdColumn()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField(SysFields.RowId, "Row ID", FieldDbType.Guid) { Visible = false });
            detail.Fields!.Add("col", "欄", FieldDbType.String);

            var layout = schema.GetFormLayout("default");

            var grid = layout.Details!.First(g => g.TableName == "OrderItem");
            var rowId = grid.Columns!.FirstOrDefault(c => c.FieldName == SysFields.RowId);
            Assert.NotNull(rowId);
            Assert.False(rowId!.Visible);
        }

        [Fact]
        [DisplayName("GetFormLayout 明細表存在 sys_master_rowid 應補入 Grid 並設為 Visible=false")]
        public void GetFormLayout_DetailTable_AddsHiddenMasterRowIdColumn()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField(SysFields.MasterRowId, "Master Row ID", FieldDbType.Guid) { Visible = false });
            detail.Fields!.Add("col", "欄", FieldDbType.String);

            var layout = schema.GetFormLayout("default");

            var grid = layout.Details!.First(g => g.TableName == "OrderItem");
            var masterRowId = grid.Columns!.FirstOrDefault(c => c.FieldName == SysFields.MasterRowId);
            Assert.NotNull(masterRowId);
            Assert.False(masterRowId!.Visible);
        }

        [Fact]
        [DisplayName("GetFormLayout 主檔不會自動補入系統欄位（白名單僅作用於 Grid）")]
        public void GetFormLayout_MasterSection_DoesNotAutoAddSystemFields()
        {
            var schema = new FormSchema("Demo", "示範");
            var master = schema.Tables!.Add("Demo", "示範");
            master.Fields!.Add(new FormField(SysFields.RowId, "Row ID", FieldDbType.Guid) { Visible = false });
            master.Fields!.Add("name", "名稱", FieldDbType.String);

            var layout = schema.GetFormLayout("default");

            Assert.Single(layout.Sections!);
            Assert.DoesNotContain(layout.Sections![0].Fields!, f => f.FieldName == SysFields.RowId);
        }

        [Fact]
        [DisplayName("GetFormLayout 無主檔時主檔 Section 不產生，僅保留明細 Grid")]
        public void GetFormLayout_NoMasterTable_OnlyDetailGrid()
        {
            // ProgId 與 Tables 不匹配 → MasterTable 為 null
            var schema = new FormSchema("NotExist", "不存在");
            var other = schema.Tables!.Add("Other", "其他");
            other.Fields!.Add("col", "欄", FieldDbType.String);

            var layout = schema.GetFormLayout("default");

            Assert.Empty(layout.Sections!);
            Assert.Single(layout.Details!);
            Assert.Equal("Other", layout.Details![0].TableName);
        }
    }
}
