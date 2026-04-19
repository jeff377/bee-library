using System.ComponentModel;
using Bee.Definition.Collections;

namespace Bee.Definition.UnitTests.Collections
{
    /// <summary>
    /// Parameter 與 ParameterCollection 測試。
    /// </summary>
    public class ParameterCollectionTests
    {
        [Fact]
        [DisplayName("Parameter 建構子應正確設定 Name 與 Value")]
        public void Parameter_Constructor_SetsNameAndValue()
        {
            // Act
            var p = new Parameter("Count", 42);

            // Assert
            Assert.Equal("Count", p.Name);
            Assert.Equal(42, p.Value);
        }

        [Fact]
        [DisplayName("Parameter ToString 應回傳 Name=Value 格式")]
        public void Parameter_ToString_ReturnsFormattedString()
        {
            // Arrange
            var p = new Parameter("Count", 42);

            // Act
            var text = p.ToString();

            // Assert
            Assert.Contains("Count", text);
            Assert.Contains("42", text);
        }

        [Fact]
        [DisplayName("ParameterCollection Add(name,value) 新增項目應可依名稱查詢")]
        public void Add_NameValue_AddsItem()
        {
            // Arrange
            var collection = new ParameterCollection();

            // Act
            collection.Add("X", 100);

            // Assert
            Assert.Single(collection);
            Assert.Equal(100, collection["X"].Value);
        }

        [Fact]
        [DisplayName("ParameterCollection Add 同名應覆寫原值")]
        public void Add_DuplicateName_ReplacesValue()
        {
            // Arrange
            var collection = new ParameterCollection();
            collection.Add("X", 100);

            // Act
            collection.Add("X", 200);

            // Assert
            Assert.Single(collection);
            Assert.Equal(200, collection["X"].Value);
        }

        [Fact]
        [DisplayName("GetValue<T> 存在應回傳轉型後的值")]
        public void GetValueT_Existing_ReturnsTypedValue()
        {
            // Arrange
            var collection = new ParameterCollection();
            collection.Add("Age", 30);

            // Act
            var value = collection.GetValue<int>("Age");

            // Assert
            Assert.Equal(30, value);
        }

        [Fact]
        [DisplayName("GetValue<T> 不存在應拋出 KeyNotFoundException")]
        public void GetValueT_Missing_ThrowsKeyNotFoundException()
        {
            // Arrange
            var collection = new ParameterCollection();

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => collection.GetValue<int>("Missing"));
        }

        [Fact]
        [DisplayName("GetValue<T> 帶預設值 不存在應回傳預設值")]
        public void GetValueT_WithDefault_ReturnsDefaultWhenMissing()
        {
            // Arrange
            var collection = new ParameterCollection();

            // Act
            var value = collection.GetValue<int>("Missing", 99);

            // Assert
            Assert.Equal(99, value);
        }
    }
}
