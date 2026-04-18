using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// FormField 未覆蓋路徑測試：Table 屬性的 Collection==null 分支、
    /// 歸屬 FormTable 後取得 Owner、ToString 格式。
    /// </summary>
    public class FormFieldTests
    {
        [Fact]
        [DisplayName("Table 於 Collection 為 null 時應回傳 null")]
        public void Table_NoCollection_ReturnsNull()
        {
            var field = new FormField("sys_no", "流水號", FieldDbType.AutoIncrement);

            // 尚未加入任何 FormFieldCollection,Collection 為 null
            Assert.Null(field.Table);
        }

        [Fact]
        [DisplayName("Table 於加入 FormTable.Fields 後應回傳該 FormTable")]
        public void Table_AddedToFormTable_ReturnsOwner()
        {
            var ft = new FormTable("Customer", "客戶");
            var field = new FormField("sys_no", "流水號", FieldDbType.AutoIncrement);
            ft.Fields!.Add(field);

            Assert.Same(ft, field.Table);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"FieldName - Caption\"")]
        public void ToString_ReturnsFieldNameDashCaption()
        {
            var field = new FormField("sys_no", "流水號", FieldDbType.AutoIncrement);

            Assert.Equal("sys_no - 流水號", field.ToString());
        }
    }
}
