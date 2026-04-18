using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    public class MemberPathTests
    {
        private static class Holder
        {
            public static int IntValue { get; set; }
            public static string StringValue { get; set; } = string.Empty;
            public static NestedHolder Child { get; } = new NestedHolder();
        }

        private class NestedHolder
        {
            public int Leaf { get; set; }
        }

        [Fact]
        [DisplayName("MemberPath.Of 應回傳正確的成員路徑字串")]
        public void Of_StaticProperty_ReturnsFullMemberPath()
        {
            // Act
            var path = MemberPath.Of(() => SysInfo.Version);

            // Assert
            Assert.Equal("SysInfo.Version", path);
        }

        [Fact]
        [DisplayName("Of 應解析靜態 int 屬性（會觸發 UnaryExpression 分支）")]
        public void Of_StaticIntProperty_ReturnsFullMemberPath()
        {
            var path = MemberPath.Of(() => Holder.IntValue);

            Assert.Equal("Holder.IntValue", path);
        }

        [Fact]
        [DisplayName("Of 應解析巢狀屬性")]
        public void Of_NestedProperty_ReturnsChainedPath()
        {
            var path = MemberPath.Of(() => Holder.Child.Leaf);

            Assert.Equal("Holder.Child.Leaf", path);
        }

        [Fact]
        [DisplayName("Of 字串屬性應解析為 UnaryExpression 分支")]
        public void Of_StringProperty_ReturnsFullMemberPath()
        {
            var path = MemberPath.Of(() => Holder.StringValue);

            Assert.Equal("Holder.StringValue", path);
        }

        [Fact]
        [DisplayName("Of 傳入非 member expression 應拋出 ArgumentException")]
        public void Of_NonMemberExpression_Throws()
        {
            Assert.Throws<ArgumentException>(() => MemberPath.Of(() => 123));
        }
    }
}
