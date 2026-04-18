using System.ComponentModel;
using Bee.Definition.Collections;

namespace Bee.Definition.UnitTests.Collections
{
    /// <summary>
    /// Property 單元測試。
    /// </summary>
    public class PropertyTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為空字串")]
        public void DefaultConstructor_InitializesEmpty()
        {
            var property = new Property();

            Assert.Equal(string.Empty, property.Name);
            Assert.Equal(string.Empty, property.Value);
        }

        [Fact]
        [DisplayName("帶參數建構子應設定 Name 與 Value")]
        public void ParameterizedConstructor_SetsProperties()
        {
            var property = new Property("Color", "Red");

            Assert.Equal("Color", property.Name);
            Assert.Equal("Red", property.Value);
        }

        [Fact]
        [DisplayName("Name 應與 Key 對映")]
        public void Name_MapsToKey()
        {
            var property = new Property { Name = "Alpha" };

            Assert.Equal("Alpha", property.Key);

            property.Key = "Beta";
            Assert.Equal("Beta", property.Name);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"Name=Value\"")]
        public void ToString_ReturnsFormatted()
        {
            var property = new Property("Color", "Red");

            Assert.Equal("Color=Red", property.ToString());
        }
    }
}
