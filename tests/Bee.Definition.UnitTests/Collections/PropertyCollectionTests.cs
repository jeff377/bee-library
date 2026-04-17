using System.ComponentModel;
using Bee.Definition.Collections;

namespace Bee.Definition.UnitTests.Collections
{
    /// <summary>
    /// Property 與 PropertyCollection 測試。
    /// </summary>
    public class PropertyCollectionTests
    {
        [Fact]
        [DisplayName("Property 建構子應正確設定 Name 與 Value")]
        public void Property_Constructor_SetsNameAndValue()
        {
            // Act
            var prop = new Property("Key", "Value");

            // Assert
            Assert.Equal("Key", prop.Name);
            Assert.Equal("Value", prop.Value);
        }

        [Fact]
        [DisplayName("Property ToString 應回傳 Name=Value 格式")]
        public void Property_ToString_ReturnsNameEqualsValue()
        {
            // Arrange
            var prop = new Property("Key", "Value");

            // Act & Assert
            Assert.Equal("Key=Value", prop.ToString());
        }

        [Fact]
        [DisplayName("PropertyCollection.Add(name,value) 應新增項目")]
        public void Add_StringValue_AddsItem()
        {
            // Arrange
            var collection = new PropertyCollection();

            // Act
            collection.Add("Theme", "Dark");

            // Assert
            Assert.Single(collection);
            Assert.Equal("Dark", collection["Theme"].Value);
        }

        [Fact]
        [DisplayName("GetValue 字串版 存在應回傳屬性值，否則回傳預設值")]
        public void GetValue_String_ReturnsValueOrDefault()
        {
            // Arrange
            var collection = new PropertyCollection();
            collection.Add("A", "1");

            // Act & Assert
            Assert.Equal("1", collection.GetValue("A", "default"));
            Assert.Equal("default", collection.GetValue("Missing", "default"));
        }

        [Fact]
        [DisplayName("GetValue bool 版 存在應轉換為布林，否則回傳預設值")]
        public void GetValue_Bool_ReturnsConvertedOrDefault()
        {
            // Arrange
            var collection = new PropertyCollection();
            collection.Add("Enabled", "true");

            // Act & Assert
            Assert.True(collection.GetValue("Enabled", false));
            Assert.False(collection.GetValue("Missing", false));
        }

        [Fact]
        [DisplayName("GetValue int 版 存在應轉換為整數，否則回傳預設值")]
        public void GetValue_Int_ReturnsConvertedOrDefault()
        {
            // Arrange
            var collection = new PropertyCollection();
            collection.Add("Count", "42");

            // Act & Assert
            Assert.Equal(42, collection.GetValue("Count", 0));
            Assert.Equal(99, collection.GetValue("Missing", 99));
        }
    }
}
