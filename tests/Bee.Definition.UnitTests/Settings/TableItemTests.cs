using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// TableItem 資料類別測試。
    /// </summary>
    public class TableItemTests
    {
        [Fact]
        [DisplayName("TableItem 預設值應為空字串")]
        public void TableItem_Default_HasEmptyProperties()
        {
            var item = new TableItem();

            Assert.Equal(string.Empty, item.TableName);
            Assert.Equal(string.Empty, item.DisplayName);
        }

        [Fact]
        [DisplayName("TableItem.TableName 應對映至 Key")]
        public void TableItem_TableName_MapsToKey()
        {
            var item = new TableItem { TableName = "st_user" };

            Assert.Equal("st_user", item.Key);
            Assert.Equal("st_user", item.TableName);
        }

        [Fact]
        [DisplayName("TableItem.ToString 應回傳 \"TableName - DisplayName\"")]
        public void TableItem_ToString_ReturnsFormatted()
        {
            var item = new TableItem
            {
                TableName = "st_user",
                DisplayName = "使用者"
            };

            Assert.Equal("st_user - 使用者", item.ToString());
        }
    }
}
