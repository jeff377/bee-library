using System.ComponentModel;
using Bee.Db.Query;

namespace Bee.Db.UnitTests
{
    public class TableJoinCollectionTests
    {
        private static TableJoinCollection BuildSampleCollection()
        {
            var collection = new TableJoinCollection
            {
                new TableJoin
                {
                    Key = "join1",
                    LeftTable = "tb_main",
                    LeftAlias = "M",
                    LeftField = "id",
                    RightTable = "tb_detail",
                    RightAlias = "D",
                    RightField = "main_id"
                },
                new TableJoin
                {
                    Key = "join2",
                    LeftTable = "tb_main",
                    LeftAlias = "M",
                    LeftField = "user_id",
                    RightTable = "tb_user",
                    RightAlias = "U",
                    RightField = "id"
                }
            };
            return collection;
        }

        [Fact]
        [DisplayName("FindRightAlias rightAlias 為 null 應回傳 null")]
        public void FindRightAlias_NullAlias_ReturnsNull()
        {
            var collection = BuildSampleCollection();

            Assert.Null(collection.FindRightAlias(null!));
        }

        [Fact]
        [DisplayName("FindRightAlias rightAlias 為空字串應回傳 null")]
        public void FindRightAlias_EmptyAlias_ReturnsNull()
        {
            var collection = BuildSampleCollection();

            Assert.Null(collection.FindRightAlias(string.Empty));
        }

        [Fact]
        [DisplayName("FindRightAlias 找到對應 alias 應回傳該 TableJoin")]
        public void FindRightAlias_Found_ReturnsMatchingJoin()
        {
            var collection = BuildSampleCollection();

            var join = collection.FindRightAlias("U");

            Assert.NotNull(join);
            Assert.Equal("tb_user", join!.RightTable);
        }

        [Fact]
        [DisplayName("FindRightAlias 採大小寫敏感比對（StrFunc.Equals 解析為 object.Equals）")]
        public void FindRightAlias_CaseSensitive_DifferentCaseReturnsNull()
        {
            // StrFunc 沒有定義 static Equals，此呼叫實際解析至 object.Equals(object, object)
            // → 字串比對為 ordinal case-sensitive
            var collection = BuildSampleCollection();

            Assert.Null(collection.FindRightAlias("d"));
        }

        [Fact]
        [DisplayName("FindRightAlias 找不到對應 alias 應回傳 null")]
        public void FindRightAlias_NotFound_ReturnsNull()
        {
            var collection = BuildSampleCollection();

            Assert.Null(collection.FindRightAlias("X"));
        }

        [Fact]
        [DisplayName("空集合 FindRightAlias 應回傳 null")]
        public void FindRightAlias_EmptyCollection_ReturnsNull()
        {
            var collection = new TableJoinCollection();

            Assert.Null(collection.FindRightAlias("anything"));
        }
    }
}
