using System.ComponentModel;
using Bee.Db.Dml;

namespace Bee.Db.UnitTests.Dml
{
    /// <summary>
    /// QueryFieldMapping 單元測試。
    /// </summary>
    public class QueryFieldMappingTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為空字串與 null TableJoin")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var mapping = new QueryFieldMapping();

            Assert.Equal(string.Empty, mapping.FieldName);
            Assert.Equal(string.Empty, mapping.Key);
            Assert.Equal(string.Empty, mapping.SourceAlias);
            Assert.Equal(string.Empty, mapping.SourceField);
            Assert.Null(mapping.TableJoin);
        }

        [Fact]
        [DisplayName("FieldName 與 Key 應互相對映")]
        public void FieldName_MapsToKey()
        {
            var mapping = new QueryFieldMapping { FieldName = "Alpha" };
            Assert.Equal("Alpha", mapping.Key);

            mapping.Key = "Beta";
            Assert.Equal("Beta", mapping.FieldName);
        }

        [Fact]
        [DisplayName("屬性應可被設定並讀回")]
        public void Properties_AreSettable()
        {
            var join = new TableJoin { Key = "join1" };
            var mapping = new QueryFieldMapping
            {
                FieldName = "UserName",
                SourceAlias = "U",
                SourceField = "name",
                TableJoin = join
            };

            Assert.Equal("UserName", mapping.FieldName);
            Assert.Equal("U", mapping.SourceAlias);
            Assert.Equal("name", mapping.SourceField);
            Assert.Same(join, mapping.TableJoin);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"{SourceAlias}.{SourceField} AS {FieldName}\"")]
        public void ToString_ReturnsFormatted()
        {
            var mapping = new QueryFieldMapping
            {
                FieldName = "UserName",
                SourceAlias = "U",
                SourceField = "name"
            };

            Assert.Equal("U.name AS UserName", mapping.ToString());
        }

        [Fact]
        [DisplayName("ToString 於預設空值應回傳 \". AS \"")]
        public void ToString_DefaultValues_ReturnsEmptyFormatted()
        {
            var mapping = new QueryFieldMapping();

            Assert.Equal(". AS ", mapping.ToString());
        }
    }
}
