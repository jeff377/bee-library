using System.ComponentModel;
using Bee.Db.Sql;

namespace Bee.Db.UnitTests.Sql
{
    /// <summary>
    /// TableJoin 屬性預設值、Key 讀寫與 ToString 輸出格式測試。
    /// </summary>
    public class TableJoinTests
    {
        [Fact]
        [DisplayName("TableJoin 預設值：JoinType=Left，其餘字串屬性為空字串")]
        public void Defaults_AreLeftJoinAndEmptyStrings()
        {
            var join = new TableJoin();

            Assert.Equal(JoinType.Left, join.JoinType);
            Assert.Equal(string.Empty, join.LeftTable);
            Assert.Equal(string.Empty, join.LeftAlias);
            Assert.Equal(string.Empty, join.LeftField);
            Assert.Equal(string.Empty, join.RightTable);
            Assert.Equal(string.Empty, join.RightAlias);
            Assert.Equal(string.Empty, join.RightField);
        }

        [Fact]
        [DisplayName("Key 屬性應可讀寫並與底層 base.Key 同步")]
        public void Key_IsReadWrite()
        {
            var join = new TableJoin { Key = "join1" };

            Assert.Equal("join1", join.Key);

            join.Key = "join2";
            Assert.Equal("join2", join.Key);
        }

        [Theory]
        [InlineData(JoinType.Left, "LEFT JOIN tb_detail D ON M.id = D.main_id")]
        [InlineData(JoinType.Inner, "INNER JOIN tb_detail D ON M.id = D.main_id")]
        [InlineData(JoinType.Right, "RIGHT JOIN tb_detail D ON M.id = D.main_id")]
        [InlineData(JoinType.Full, "FULL JOIN tb_detail D ON M.id = D.main_id")]
        [DisplayName("ToString 應依 JoinType 產生對應關鍵字的 JOIN 語法")]
        public void ToString_FormatsAccordingToJoinType(JoinType joinType, string expected)
        {
            var join = new TableJoin
            {
                JoinType = joinType,
                LeftTable = "tb_main",
                LeftAlias = "M",
                LeftField = "id",
                RightTable = "tb_detail",
                RightAlias = "D",
                RightField = "main_id"
            };

            Assert.Equal(expected, join.ToString());
        }
    }
}
