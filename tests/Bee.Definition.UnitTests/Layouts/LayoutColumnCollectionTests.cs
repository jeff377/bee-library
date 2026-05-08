using System.ComponentModel;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// LayoutColumnCollection 單元測試。
    /// </summary>
    public class LayoutColumnCollectionTests
    {
        [Fact]
        [DisplayName("Add 應建立並回傳具正確屬性的 LayoutColumn")]
        public void Add_ValidParams_ReturnsColumnWithCorrectProperties()
        {
            var collection = new LayoutColumnCollection();

            var column = collection.Add("Amount", "金額", ControlType.TextEdit);

            Assert.Equal("Amount", column.FieldName);
            Assert.Equal("金額", column.Caption);
            Assert.Equal(ControlType.TextEdit, column.ControlType);
        }

        [Fact]
        [DisplayName("Add 應將欄位加入集合中，可由索引取出")]
        public void Add_ValidParams_ColumnIsAddedToCollection()
        {
            var collection = new LayoutColumnCollection();

            var column = collection.Add("Name", "姓名", ControlType.TextEdit);

            Assert.Single(collection);
            Assert.Same(column, collection[0]);
        }

        [Fact]
        [DisplayName("Add 多次呼叫應依序加入全部欄位")]
        public void Add_MultipleCalls_AddsAllColumns()
        {
            var collection = new LayoutColumnCollection();

            var col1 = collection.Add("Field1", "欄位1", ControlType.TextEdit);
            var col2 = collection.Add("Field2", "欄位2", ControlType.CheckEdit);

            Assert.Equal(2, collection.Count);
            Assert.Same(col1, collection[0]);
            Assert.Same(col2, collection[1]);
        }
    }
}
