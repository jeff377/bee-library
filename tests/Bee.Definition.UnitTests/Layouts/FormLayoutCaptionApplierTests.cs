using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// <see cref="FormLayoutCaptionApplier"/> 測試：layout 定義檔只負責結構，顯示文字一律取自
    /// 已在地化的 <see cref="FormSchema"/>；schema 沒有的欄位保留 layout 檔原值。
    /// </summary>
    public class FormLayoutCaptionApplierTests
    {
        [Fact]
        [DisplayName("欄位 caption 應改取自 schema（覆蓋 layout 檔的靜態文字）")]
        public void Apply_FieldCaption_TakenFromSchema()
        {
            var layout = BuildLayout();
            var schema = BuildSchema();

            FormLayoutCaptionApplier.Apply(layout, schema);

            Assert.Equal("員工編號", Field(layout, "sys_id").Caption);
            Assert.Equal("姓名", Field(layout, "sys_name").Caption);
        }

        [Fact]
        [DisplayName("表單 / 區塊 / 明細標題應改取自 schema")]
        public void Apply_ContainerCaptions_TakenFromSchema()
        {
            var layout = BuildLayout();
            var schema = BuildSchema();

            FormLayoutCaptionApplier.Apply(layout, schema);

            Assert.Equal("員工", layout.Caption);
            Assert.Equal("員工主檔", layout.Sections![0].Caption);
            Assert.Equal("工作紀錄", layout.Details![0].Caption);
        }

        [Fact]
        [DisplayName("明細欄 caption 應取自對應 FormTable 的欄位")]
        public void Apply_DetailColumnCaption_TakenFromDetailTable()
        {
            var layout = BuildLayout();
            var schema = BuildSchema();

            FormLayoutCaptionApplier.Apply(layout, schema);

            Assert.Equal("工作日期", Column(layout.Details![0], "work_date").Caption);
        }

        [Fact]
        [DisplayName("schema 沒有的欄位應保留 layout 檔原值（layout 可能落後於 schema）")]
        public void Apply_FieldMissingFromSchema_KeepsLayoutText()
        {
            var layout = BuildLayout();
            layout.Sections![0].Fields!.Add(new LayoutField { FieldName = "retired_field", Caption = "已移除欄位" });
            var schema = BuildSchema();

            FormLayoutCaptionApplier.Apply(layout, schema);

            Assert.Equal("已移除欄位", Field(layout, "retired_field").Caption);
        }

        [Fact]
        [DisplayName("schema 沒有的明細表應整組保留原值")]
        public void Apply_DetailTableMissingFromSchema_KeepsLayoutText()
        {
            var layout = BuildLayout();
            var orphan = new LayoutGrid("Removed", "已移除明細");
            orphan.Columns!.Add(new LayoutColumn("x", "欄 X", ControlType.TextEdit));
            layout.Details!.Add(orphan);
            var schema = BuildSchema();

            FormLayoutCaptionApplier.Apply(layout, schema);

            Assert.Equal("已移除明細", orphan.Caption);
            Assert.Equal("欄 X", Column(orphan, "x").Caption);
        }

        [Fact]
        [DisplayName("layout 為 null 或 schema 為 null 應拋 ArgumentNullException")]
        public void Apply_NullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => FormLayoutCaptionApplier.Apply(null!, BuildSchema()));
            Assert.Throws<ArgumentNullException>(() => FormLayoutCaptionApplier.Apply(BuildLayout(), null!));
        }

        [Fact]
        [DisplayName("空 Sections / Details 的 layout 不應丟例外")]
        public void Apply_EmptyLayout_DoesNotThrow()
        {
            var layout = new FormLayout { LayoutId = "Employee", ProgId = "Employee" };

            var exception = Record.Exception(() => FormLayoutCaptionApplier.Apply(layout, BuildSchema()));

            Assert.Null(exception);
            Assert.Equal("員工", layout.Caption);
        }

        private static LayoutField Field(FormLayout layout, string fieldName)
            => layout.Sections![0].Fields!.First(f => f.FieldName == fieldName);

        private static LayoutColumn Column(LayoutGrid grid, string fieldName)
            => grid.Columns!.First(c => c.FieldName == fieldName);

        // layout 檔帶的是作者當初寫死的文字，全部應被 schema 覆蓋
        private static FormLayout BuildLayout()
        {
            var layout = new FormLayout
            {
                LayoutId = "Employee",
                ProgId = "Employee",
                Caption = "Employee (layout file)",
            };
            var section = new LayoutSection { Name = "Main", Caption = "Main (layout file)" };
            section.Fields!.Add(new LayoutField { FieldName = "sys_id", Caption = "ID (layout file)" });
            section.Fields!.Add(new LayoutField { FieldName = "sys_name", Caption = "Name (layout file)" });
            layout.Sections!.Add(section);

            var grid = new LayoutGrid("WorkLog", "WorkLog (layout file)");
            grid.Columns!.Add(new LayoutColumn("work_date", "Date (layout file)", ControlType.DateEdit));
            layout.Details!.Add(grid);
            return layout;
        }

        // 模擬「已在地化」的 schema：文字都是最終要顯示的中文
        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Employee", "員工") { CategoryId = "common" };
            var master = schema.Tables!.Add("Employee", "員工主檔");
            master.DbTableName = "st_employee";
            master.Fields!.Add("sys_id", "員工編號", FieldDbType.String);
            master.Fields!.Add("sys_name", "姓名", FieldDbType.String);

            var detail = schema.Tables!.Add("WorkLog", "工作紀錄");
            detail.DbTableName = "ft_work_log";
            detail.Fields!.Add("work_date", "工作日期", FieldDbType.Date);
            return schema;
        }
    }
}
