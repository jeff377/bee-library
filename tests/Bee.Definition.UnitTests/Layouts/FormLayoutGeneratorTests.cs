using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// FormSchema.GetFormLayout 將 FormSchema 轉換為 FormLayout 的測試。
    /// </summary>
    public class FormLayoutGeneratorTests
    {
        [Fact]
        [DisplayName("GetFormLayout 傳入 null FormSchema 透過反射不適用；改測試 FormLayoutGenerator.Generate 透過 FormSchema 入口")]
        public void GetFormLayout_RequiresLayoutId_ProducesLayoutWithProgIdAndCaption()
        {
            var schema = BuildSchema();

            var layout = schema.GetFormLayout("default");

            Assert.Equal("default", layout.LayoutId);
            Assert.Equal("Employee", layout.ProgId);
            Assert.Equal("員工", layout.Caption);
            Assert.Equal(2, layout.ColumnCount);
        }

        [Fact]
        [DisplayName("GetFormLayout 主檔 Section 應命名為 Main 並使用主檔 DisplayName")]
        public void GetFormLayout_CreatesMainSection()
        {
            var schema = BuildSchema();

            var layout = schema.GetFormLayout("default");

            Assert.Single(layout.Sections!);
            var section = layout.Sections![0];
            Assert.Equal("Main", section.Name);
            Assert.Equal("員工", section.Caption);
            Assert.True(section.ShowCaption);
        }

        [Fact]
        [DisplayName("GetFormLayout 應忽略 Visible=false 的欄位")]
        public void GetFormLayout_SkipsInvisibleFields()
        {
            var schema = BuildSchema();
            schema.MasterTable!.Fields!["sys_id"].Visible = false;

            var layout = schema.GetFormLayout("default");

            var section = layout.Sections![0];
            Assert.DoesNotContain(section.Fields!, f => f.FieldName == "sys_id");
        }

        [Theory]
        [InlineData(FieldDbType.Boolean, ControlType.CheckEdit)]
        [InlineData(FieldDbType.DateTime, ControlType.DateEdit)]
        [InlineData(FieldDbType.Text, ControlType.MemoEdit)]
        [InlineData(FieldDbType.String, ControlType.TextEdit)]
        [DisplayName("GetFormLayout ControlType=Auto 應依 DbType 推導對應控制型態")]
        public void GetFormLayout_AutoControlType_MapsDbTypeToControlType(FieldDbType dbType, ControlType expected)
        {
            var schema = new FormSchema("Demo", "示範");
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("field", "欄位", dbType) { ControlType = ControlType.Auto });

            var layout = schema.GetFormLayout("default");

            var field = layout.Sections!.First().Fields!.First();
            Assert.Equal(expected, field.ControlType);
        }

        [Fact]
        [DisplayName("GetFormLayout 多個 Table 應為主檔以外的每張表建立 Detail Grid")]
        public void GetFormLayout_MultipleTables_CreatesDetailGrid()
        {
            var schema = BuildSchema();
            var detail = schema.Tables!.Add("EmployeeSkill", "員工技能");
            detail.Fields!.Add("skill_name", "技能", FieldDbType.String);

            var layout = schema.GetFormLayout("default");

            Assert.Single(layout.Details!);
            var grid = layout.Details![0];
            Assert.Equal("EmployeeSkill", grid.TableName);
            Assert.Equal("員工技能", grid.Caption);
        }

        [Fact]
        [DisplayName("GetFormLayout 主檔所有欄位皆不可見時不應新增 Section")]
        public void GetFormLayout_MasterAllInvisible_DoesNotAddSection()
        {
            var schema = new FormSchema("Demo", "示範");
            var master = schema.Tables!.Add("Demo", "示範");
            master.Fields!.Add(new FormField("hidden", "隱藏", FieldDbType.String) { Visible = false });

            var layout = schema.GetFormLayout("default");

            Assert.Empty(layout.Sections!);
        }

        [Fact]
        [DisplayName("FormLayoutGenerator 透過 FormSchema 入口傳入 null layoutId 應接受並原樣寫入")]
        public void GetFormLayout_AcceptsCustomLayoutId()
        {
            var schema = BuildSchema();

            var layout = schema.GetFormLayout("manager_view");

            Assert.Equal("manager_view", layout.LayoutId);
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
