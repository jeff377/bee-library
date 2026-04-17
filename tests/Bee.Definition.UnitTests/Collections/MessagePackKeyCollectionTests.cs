using System.ComponentModel;
using Bee.Definition.Collections;

namespace Bee.Definition.UnitTests.Collections
{
    /// <summary>
    /// MessagePackKeyCollectionBase / MessagePackKeyCollectionItem 行為測試。
    /// 使用既有 <see cref="Parameter"/> / <see cref="ParameterCollection"/> 作為受測樣本。
    /// </summary>
    public class MessagePackKeyCollectionTests
    {
        [Fact]
        [DisplayName("Add 項目後可依索引位置與 Key 取得項目")]
        public void Add_Item_CanBeRetrievedByKeyAndIndex()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 100),
                new Parameter("P2", "ABC")
            };

            // Act
            var byKey = collection["P2"];
            var byIndex = collection[0];

            // Assert
            Assert.Equal("P2", byKey.Name);
            Assert.Equal("ABC", byKey.Value);
            Assert.Equal("P1", byIndex.Name);
        }

        [Fact]
        [DisplayName("Contains 應回傳 Key 是否存在")]
        public void Contains_ReturnsWhetherKeyExists()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1)
            };

            // Act & Assert
            Assert.True(collection.Contains("P1"));
            Assert.False(collection.Contains("missing"));
        }

        [Fact]
        [DisplayName("Key 比對應忽略大小寫")]
        public void Contains_IsCaseInsensitive()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("MyParam", 1)
            };

            // Act & Assert
            Assert.True(collection.Contains("myparam"));
            Assert.True(collection.Contains("MYPARAM"));
        }

        [Fact]
        [DisplayName("Remove 應移除指定 Key 的項目")]
        public void Remove_RemovesItemByKey()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", 2)
            };

            // Act
            collection.Remove("P1");

            // Assert
            Assert.Single(collection);
            Assert.False(collection.Contains("P1"));
            Assert.True(collection.Contains("P2"));
        }

        [Fact]
        [DisplayName("Insert 應將項目插入指定位置")]
        public void Insert_AddsItemAtSpecifiedIndex()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P3", 3)
            };

            // Act
            collection.Insert(1, new Parameter("P2", 2));

            // Assert
            Assert.Equal(3, collection.Count);
            Assert.Equal("P2", collection[1].Name);
        }

        [Fact]
        [DisplayName("Clear 應移除所有項目")]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", 2)
            };

            // Act
            collection.Clear();

            // Assert
            Assert.Empty(collection);
        }

        [Fact]
        [DisplayName("ItemsForSerialization 取值應回傳目前項目的拷貝")]
        public void ItemsForSerialization_Get_ReturnsCurrentItems()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", 2)
            };

            // Act
            var items = collection.ItemsForSerialization;

            // Assert
            Assert.NotNull(items);
            Assert.Equal(2, items!.Count);
        }
    }
}
