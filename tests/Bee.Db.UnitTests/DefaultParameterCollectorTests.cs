using System.ComponentModel;
using Bee.Db.Query;

namespace Bee.Db.UnitTests
{
    public class DefaultParameterCollectorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [DisplayName("建構子 prefix 為 null 或空字串應擲 ArgumentException")]
        public void Constructor_NullOrEmptyPrefix_Throws(string? prefix)
        {
            Assert.Throws<ArgumentException>(() => new DefaultParameterCollector(prefix!));
        }

        [Fact]
        [DisplayName("建構子應正確記錄 Prefix")]
        public void Constructor_ValidPrefix_StoresPrefix()
        {
            var collector = new DefaultParameterCollector("@");

            Assert.Equal("@", collector.Prefix);
        }

        [Fact]
        [DisplayName("Add 連續呼叫應產生 @p0、@p1、@p2 並回傳對應名稱")]
        public void Add_SequentialCalls_GeneratesIncrementingNames()
        {
            var collector = new DefaultParameterCollector("@");

            string n0 = collector.Add(1);
            string n1 = collector.Add("x");
            string n2 = collector.Add(3.14);

            Assert.Equal("@p0", n0);
            Assert.Equal("@p1", n1);
            Assert.Equal("@p2", n2);
        }

        [Fact]
        [DisplayName("GetAll 應回傳所有已加入的參數鍵值對")]
        public void GetAll_ReturnsAllAddedParameters()
        {
            var collector = new DefaultParameterCollector("@");
            collector.Add(10);
            collector.Add("abc");

            var all = collector.GetAll();

            Assert.Equal(2, all.Count);
            Assert.Equal(10, all["@p0"]);
            Assert.Equal("abc", all["@p1"]);
        }

        [Fact]
        [DisplayName("Add 應接受不同型別的 prefix（例如 Oracle 的 ':'）")]
        public void Add_OraclePrefix_GeneratesColonNames()
        {
            var collector = new DefaultParameterCollector(":");

            string n0 = collector.Add(1);

            Assert.Equal(":p0", n0);
        }
    }
}
