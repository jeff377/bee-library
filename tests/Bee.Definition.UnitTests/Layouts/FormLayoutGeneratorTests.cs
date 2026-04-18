using System.ComponentModel;
using System.Linq;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// FormLayoutGenerator 將 FormSchema 轉換為 FormLayout 的測試。
    /// </summary>
    public class FormLayoutGeneratorTests
    {
        [Fact]
        [DisplayName("Generate 傳入 null 應拋出 ArgumentNullException")]
        public void Generate_NullFormSchema_ThrowsArgumentNullException()
        {
            // Arrange

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => FormLayoutGenerator.Generate(null!));
        }

        [Fact]
        [DisplayName("Generate 應將 ProgId / DisplayName 複製到 FormLayout")]
        public void Generate_CopiesIdAndDisplayName()
        {
            // Arrange
            var schema = BuildSchema();

            // Act
            var layout = FormLayoutGenerator.Generate(schema);

            // Assert
            Assert.Equal("Employee", layout.LayoutId);
            Assert.Equal("員工", layout.DisplayName);
        }

        [Fact]
        [DisplayName("Generate 主檔 Group 應命名為 MainGroup 並為兩欄")]
        public void Generate_CreatesMainGroupWithTwoColumns()
        {
            // Arrange
            var schema = BuildSchema();

            // Act
            var layout = FormLayoutGenerator.Generate(schema);

            // Assert
            var mainGroup = layout.Groups!.FirstOrDefault(g => g.Name == "MainGroup");
            Assert.NotNull(mainGroup);
            Assert.Equal(2, mainGroup!.ColumnCount);
            Assert.True(mainGroup.ShowCaption);
        }

        [Fact]
        [DisplayName("Generate 應忽略 Visible=false 的欄位")]
        public void Generate_SkipsInvisibleFields()
        {
            // Arrange
            var schema = BuildSchema();
            schema.MasterTable!.Fields!["sys_id"].Visible = false;

            // Act
            var layout = FormLayoutGenerator.Generate(schema);

            // Assert
            var mainGroup = layout.Groups!.First(g => g.Name == "MainGroup");
            Assert.DoesNotContain(mainGroup.Items!.OfType<LayoutItem>(), item => item.FieldName == "sys_id");
        }

        [Theory]
        [InlineData(FieldDbType.Boolean, ControlType.CheckEdit)]
        [InlineData(FieldDbType.DateTime, ControlType.DateEdit)]
        [InlineData(FieldDbType.Text, ControlType.MemoEdit)]
        [InlineData(FieldDbType.String, ControlType.TextEdit)]
        [DisplayName("Generate ControlType=Auto 應依 DbType 推導對應控制型態")]
        public void Generate_AutoControlType_MapsDbTypeToControlType(FieldDbType dbType, ControlType expected)
        {
            // Arrange
            var schema = new FormSchema("Demo", "示範");
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("field", "欄位", dbType) { ControlType = ControlType.Auto });

            // Act
            var layout = FormLayoutGenerator.Generate(schema);

            // Assert
            var item = (LayoutItem)layout.Groups!.First().Items!.First();
            Assert.Equal(expected, item.ControlType);
        }

        [Fact]
        [DisplayName("Generate 有 LookupProgId 應設定到 LayoutItem.ProgId")]
        public void Generate_LookupProgId_SetsLayoutItemProgId()
        {
            // Arrange
            var schema = new FormSchema("Demo", "示範");
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("dept_id", "部門", FieldDbType.String)
            {
                LookupProgId = "DeptLookup"
            });

            // Act
            var layout = FormLayoutGenerator.Generate(schema);

            // Assert
            var item = (LayoutItem)layout.Groups!.First().Items!.First();
            Assert.Equal("DeptLookup", item.ProgId);
        }

        [Fact]
        [DisplayName("Generate 無 Lookup 但有 RelationProgId 應設定到 LayoutItem.ProgId")]
        public void Generate_RelationProgId_SetsLayoutItemProgId()
        {
            // Arrange
            var schema = new FormSchema("Demo", "示範");
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("dept_rowid", "部門", FieldDbType.String)
            {
                RelationProgId = "Department"
            });

            // Act
            var layout = FormLayoutGenerator.Generate(schema);

            // Assert
            var item = (LayoutItem)layout.Groups!.First().Items!.First();
            Assert.Equal("Department", item.ProgId);
        }

        [Fact]
        [DisplayName("Generate 多個 Table 應為主檔以外的每個 Table 建立 Group 與 Grid")]
        public void Generate_MultipleTables_CreatesDetailGroupWithGrid()
        {
            // Arrange
            var schema = BuildSchema();
            var detail = schema.Tables!.Add("EmployeeSkill", "員工技能");
            detail.Fields!.Add("skill_name", "技能", FieldDbType.String);

            // Act
            var layout = FormLayoutGenerator.Generate(schema);

            // Assert
            var detailGroup = layout.Groups!.FirstOrDefault(g => g.Name == "EmployeeSkillGroup");
            Assert.NotNull(detailGroup);
            Assert.Single(detailGroup!.Items!);
            Assert.IsType<LayoutGrid>(detailGroup.Items![0]);
        }

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Employee", "員工");
            var master = schema.Tables!.Add("Employee", "員工");
            master.Fields!.Add("sys_id", "編號", FieldDbType.String);
            master.Fields!.Add("sys_name", "姓名", FieldDbType.String);
            return schema;
        }
    }
}
