using System.ComponentModel;
using System.Data;

namespace Bee.Db.UnitTests
{
    public class DbParameterSpecTests
    {
        [Fact]
        [DisplayName("無參數建構子預設值正確")]
        public void DefaultConstructor_DefaultValues()
        {
            var spec = new DbParameterSpec();

            Assert.Null(spec.Value);
            Assert.Null(spec.DbType);
            Assert.Null(spec.Size);
            Assert.False(spec.IsNullable);
            Assert.Equal(string.Empty, spec.SourceColumn);
            Assert.Equal(DataRowVersion.Current, spec.SourceVersion);
        }

        [Fact]
        [DisplayName("帶值建構子應推斷 DbType")]
        public void ValueConstructor_InfersDbType()
        {
            var spec = new DbParameterSpec("p1", "hello");

            Assert.Equal("p1", spec.Name);
            Assert.Equal("hello", spec.Value);
            Assert.Equal(DbType.String, spec.DbType);
        }

        [Fact]
        [DisplayName("Name 與 Key 應同步")]
        public void Name_SyncsWithKey()
        {
            var spec = new DbParameterSpec();
            spec.Name = "userName";
            Assert.Equal("userName", spec.Key);

            spec.Key = "another";
            Assert.Equal("another", spec.Name);
        }

        [Fact]
        [DisplayName("ToString 應回傳 'Name = Value'")]
        public void ToString_FormatsNameAndValue()
        {
            var spec = new DbParameterSpec("age", 30);

            Assert.Equal("age = 30", spec.ToString());
        }
    }
}
