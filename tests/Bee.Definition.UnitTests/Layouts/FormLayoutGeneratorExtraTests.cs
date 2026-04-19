using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// FormLayoutGenerator 補強測試：
    /// 涵蓋 ConvertToColumnControlType 的所有分支、GetDefaultColumnControlType、
    /// 明細表 Grid 的 Width 與 Visible=false 路徑，以及空明細表不新增 Group 的分支。
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
        [InlineData(ControlType.TextEdit, ColumnControlType.TextEdit)]
        [InlineData(ControlType.ButtonEdit, ColumnControlType.ButtonEdit)]
        [InlineData(ControlType.DateEdit, ColumnControlType.DateEdit)]
        [InlineData(ControlType.YearMonthEdit, ColumnControlType.YearMonthEdit)]
        [InlineData(ControlType.DropDownEdit, ColumnControlType.DropDownEdit)]
        [InlineData(ControlType.CheckEdit, ColumnControlType.CheckEdit)]
        [InlineData(ControlType.MemoEdit, ColumnControlType.TextEdit)]
        [DisplayName("Generate 明細表非 Auto ControlType 應轉為對應 ColumnControlType")]
        public void Generate_DetailTable_NonAutoControlType_ConvertsToColumnControlType(
            ControlType controlType, ColumnControlType expected)
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("col", "欄", FieldDbType.String)
            {
                ControlType = controlType
            });

            var layout = FormLayoutGenerator.Generate(schema);

            var detailGroup = layout.Groups!.First(g => g.Name == "OrderItemGroup");
            var grid = Assert.IsType<LayoutGrid>(detailGroup.Items![0]);
            Assert.Equal(expected, grid.Columns![0].ControlType);
        }

        [Theory]
        [InlineData(FieldDbType.Boolean, ColumnControlType.CheckEdit)]
        [InlineData(FieldDbType.DateTime, ColumnControlType.DateEdit)]
        [InlineData(FieldDbType.String, ColumnControlType.TextEdit)]
        [InlineData(FieldDbType.Integer, ColumnControlType.TextEdit)]
        [DisplayName("Generate 明細表 ControlType=Auto 應依 DbType 推導對應 ColumnControlType")]
        public void Generate_DetailTable_AutoControlType_MapsDbTypeToColumnControlType(
            FieldDbType dbType, ColumnControlType expected)
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("col", "欄", dbType)
            {
                ControlType = ControlType.Auto
            });

            var layout = FormLayoutGenerator.Generate(schema);

            var detailGroup = layout.Groups!.First(g => g.Name == "OrderItemGroup");
            var grid = Assert.IsType<LayoutGrid>(detailGroup.Items![0]);
            Assert.Equal(expected, grid.Columns![0].ControlType);
        }

        [Fact]
        [DisplayName("Generate 明細表 Width>0 應保留原值")]
        public void Generate_DetailTable_WidthGreaterThanZero_KeepsOriginalWidth()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("col", "欄", FieldDbType.String) { Width = 250 });

            var layout = FormLayoutGenerator.Generate(schema);

            var grid = (LayoutGrid)layout.Groups!.First(g => g.Name == "OrderItemGroup").Items![0];
            Assert.Equal(250, grid.Columns![0].Width);
        }

        [Fact]
        [DisplayName("Generate 明細表 Width=0 應套用預設 100")]
        public void Generate_DetailTable_WidthZero_UsesDefault100()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add("col", "欄", FieldDbType.String);

            var layout = FormLayoutGenerator.Generate(schema);

            var grid = (LayoutGrid)layout.Groups!.First(g => g.Name == "OrderItemGroup").Items![0];
            Assert.Equal(100, grid.Columns![0].Width);
        }

        [Fact]
        [DisplayName("Generate 明細表應跳過 Visible=false 欄位")]
        public void Generate_DetailTable_SkipsInvisibleFields()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("hidden", "隱藏", FieldDbType.String) { Visible = false });
            detail.Fields!.Add("visible", "顯示", FieldDbType.String);

            var layout = FormLayoutGenerator.Generate(schema);

            var grid = (LayoutGrid)layout.Groups!.First(g => g.Name == "OrderItemGroup").Items![0];
            Assert.Single(grid.Columns!);
            Assert.Equal("visible", grid.Columns![0].FieldName);
        }

        [Fact]
        [DisplayName("Generate 明細表所有欄位皆不可見時不應新增對應 Group")]
        public void Generate_DetailTable_AllFieldsInvisible_DoesNotAddGroup()
        {
            var schema = BuildMasterDetailSchema();
            var detail = schema.Tables!.Add("OrderItem", "訂單明細");
            detail.Fields!.Add(new FormField("hidden", "隱藏", FieldDbType.String) { Visible = false });

            var layout = FormLayoutGenerator.Generate(schema);

            Assert.DoesNotContain(layout.Groups!, g => g.Name == "OrderItemGroup");
        }

        [Fact]
        [DisplayName("Generate 主檔所有欄位皆不可見時不應新增 MainGroup")]
        public void Generate_MasterTable_AllFieldsInvisible_DoesNotAddMainGroup()
        {
            var schema = new FormSchema("Demo", "示範");
            var master = schema.Tables!.Add("Demo", "示範");
            master.Fields!.Add(new FormField("hidden", "隱藏", FieldDbType.String) { Visible = false });

            var layout = FormLayoutGenerator.Generate(schema);

            Assert.DoesNotContain(layout.Groups!, g => g.Name == "MainGroup");
        }

        [Fact]
        [DisplayName("Generate 無主檔時主檔 Group 不產生，僅保留明細 Group")]
        public void Generate_NoMasterTable_OnlyDetailGroup()
        {
            // ProgId 與 Tables 不匹配 → MasterTable 為 null
            var schema = new FormSchema("NotExist", "不存在");
            var other = schema.Tables!.Add("Other", "其他");
            other.Fields!.Add("col", "欄", FieldDbType.String);

            var layout = FormLayoutGenerator.Generate(schema);

            Assert.DoesNotContain(layout.Groups!, g => g.Name == "MainGroup");
            Assert.Contains(layout.Groups!, g => g.Name == "OtherGroup");
        }
    }
}
