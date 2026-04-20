using System.ComponentModel;
using Bee.Base.Serialization;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// SortField 與 SortFieldCollection 測試。
    /// </summary>
    public class SortFieldTests
    {
        [Fact]
        [DisplayName("SortField 使用欄位名稱與方向建構應正確設定屬性")]
        public void Constructor_ValidArguments_SetsProperties()
        {
            // Act
            var sortField = new SortField("sys_id", SortDirection.Desc);

            // Assert
            Assert.Equal("sys_id", sortField.FieldName);
            Assert.Equal(SortDirection.Desc, sortField.Direction);
        }

        [Fact]
        [DisplayName("SortField 預設建構式應使用預設屬性值")]
        public void DefaultConstructor_UsesDefaultValues()
        {
            // Act
            var sortField = new SortField();

            // Assert
            Assert.Equal(string.Empty, sortField.FieldName);
            Assert.Equal(SortDirection.Asc, sortField.Direction);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("SortField 欄位名稱為空白應拋出 ArgumentException")]
        public void Constructor_EmptyFieldName_ThrowsArgumentException(string? fieldName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SortField(fieldName!, SortDirection.Asc));
        }

        [Fact]
        [DisplayName("SortFieldCollection 新增項目後 Count 應正確反映")]
        public void SortFieldCollection_Add_IncrementsCount()
        {
            // Arrange
            var collection = new SortFieldCollection
            {
                // Act
                new SortField("sys_id", SortDirection.Asc),
                new SortField("sys_no", SortDirection.Desc)
            };

            // Assert
            Assert.Equal(2, collection.Count);
            Assert.Equal("sys_id", collection[0].FieldName);
            Assert.Equal(SortDirection.Desc, collection[1].Direction);
        }

        [Fact]
        [DisplayName("SortFieldCollection 移除項目後 Count 應正確減少")]
        public void SortFieldCollection_Remove_DecrementsCount()
        {
            // Arrange
            var collection = new SortFieldCollection();
            var field = new SortField("sys_id", SortDirection.Asc);
            collection.Add(field);
            collection.Add(new SortField("sys_no", SortDirection.Desc));

            // Act
            collection.Remove(field);

            // Assert
            Assert.Single(collection);
            Assert.Equal("sys_no", collection[0].FieldName);
        }

        [Fact]
        [DisplayName("SortField XML 序列化與反序列化應正確還原")]
        public void SortField_XmlRoundtrip_Succeeds()
        {
            // Arrange
            var original = new SortField("sys_id", SortDirection.Desc);

            // Act
            var xml = SerializeFunc.ObjectToXml(original);
            var restored = SerializeFunc.XmlToObject<SortField>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal(original.FieldName, restored!.FieldName);
            Assert.Equal(original.Direction, restored.Direction);
        }

        [Fact]
        [DisplayName("SortField JSON 序列化與反序列化應正確還原")]
        public void SortField_JsonRoundtrip_Succeeds()
        {
            // Arrange
            var original = new SortField("sys_id", SortDirection.Desc);

            // Act
            var json = SerializeFunc.ObjectToJson(original);
            var restored = SerializeFunc.JsonToObject<SortField>(json);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal(original.FieldName, restored!.FieldName);
            Assert.Equal(original.Direction, restored.Direction);
        }

        [Fact]
        [DisplayName("SortFieldCollection XML 序列化與反序列化應正確還原")]
        public void SortFieldCollection_XmlRoundtrip_Succeeds()
        {
            // Arrange
            var collection = new SortFieldCollection
            {
                new SortField("sys_id", SortDirection.Asc),
                new SortField("sys_no", SortDirection.Desc)
            };

            // Act
            var xml = SerializeFunc.ObjectToXml(collection);
            var restored = SerializeFunc.XmlToObject<SortFieldCollection>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal(2, restored!.Count);
            Assert.Equal("sys_id", restored[0].FieldName);
            Assert.Equal(SortDirection.Desc, restored[1].Direction);
        }
    }
}
