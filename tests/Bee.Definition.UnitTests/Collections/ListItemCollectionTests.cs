using System.ComponentModel;
using System.Data;
using Bee.Definition.Collections;

namespace Bee.Definition.UnitTests.Collections
{
    /// <summary>
    /// ListItem 與 ListItemCollection 測試。
    /// </summary>
    public class ListItemCollectionTests
    {
        [Fact]
        [DisplayName("ListItem 建構子應正確設定 Value 與 Text")]
        public void ListItem_Constructor_SetsValueAndText()
        {
            // Act
            var item = new ListItem("01", "項目一");

            // Assert
            Assert.Equal("01", item.Value);
            Assert.Equal("項目一", item.Text);
        }

        [Fact]
        [DisplayName("ListItem ToString 應回傳 Text")]
        public void ListItem_ToString_ReturnsText()
        {
            // Arrange
            var item = new ListItem("01", "項目一");

            // Act & Assert
            Assert.Equal("項目一", item.ToString());
        }

        [Fact]
        [DisplayName("ListItemCollection Add(value,text) 應新增項目並回傳")]
        public void Add_ValueAndText_AddsAndReturnsItem()
        {
            // Arrange
            var collection = new ListItemCollection();

            // Act
            var added = collection.Add("A", "Alpha");

            // Assert
            Assert.Single(collection);
            Assert.Same(added, collection["A"]);
            Assert.Equal("Alpha", added.Text);
        }

        [Fact]
        [DisplayName("FromTable 應依指定欄位填入 Value 與 Text")]
        public void FromTable_PopulatesItemsFromDataTable()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("ValueCol", typeof(string));
            table.Columns.Add("TextCol", typeof(string));
            table.Rows.Add("01", "一");
            table.Rows.Add("02", "二");

            var collection = new ListItemCollection();

            // Act
            collection.FromTable(table, "ValueCol", "TextCol");

            // Assert
            Assert.Equal(2, collection.Count);
            Assert.Equal("一", collection["01"].Text);
            Assert.Equal("二", collection["02"].Text);
        }
    }
}
