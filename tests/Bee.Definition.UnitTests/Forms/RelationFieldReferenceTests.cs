using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    public class RelationFieldReferenceTests
    {
        [Fact]
        [DisplayName("預設建構子應建立 FieldName/SourceProgId/SourceField 皆為空字串的實例")]
        public void DefaultConstructor_CreatesInstance_WithEmptyFields()
        {
            var reference = new RelationFieldReference();

            Assert.Equal(string.Empty, reference.FieldName);
            Assert.Equal(string.Empty, reference.SourceProgId);
            Assert.Equal(string.Empty, reference.SourceField);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"{SourceProgId}.{SourceField} -> {FieldName}\" 格式")]
        public void ToString_ReturnsFormattedString()
        {
            var field = new FormField("dept_id", "部門 ID", FieldDbType.String);
            var reference = new RelationFieldReference("dept_id", field, "Department", "dept_name");

            Assert.Equal("Department.dept_name -> dept_id", reference.ToString());
        }
    }
}
