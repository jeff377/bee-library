using Bee.Define;

namespace Bee.Db.UnitTests
{
    public class WhereBuilderTests
    {
        [Fact]
        public void EqualCondition_ShouldBuildSqlServerWhere()
        {
            var root = FilterCondition.Equal("DeptId", 10);
            var builder = new WhereBuilder();
            var result = builder.Build(root);
        }

        [Fact]
        public void LikeContains_ShouldAddWildcards()
        {
            var root = FilterCondition.Contains("Name", "Lee");
            var builder = new WhereBuilder();
            var result = builder.Build(root, includeWhereKeyword: false);
        }

        [Fact]
        public void GroupAndOr_ShouldBuildParentheses()
        {
            var root = FilterGroup.All(
                FilterCondition.Equal("DeptId", 10),
                FilterGroup.Any(
                    FilterCondition.Contains("Name", "Lee"),
                    FilterCondition.Between("HireDate", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31))
                )
            );

            var builder = new WhereBuilder();
            var result = builder.Build(root);
        }

        [Fact]
        public void NullEquals_ShouldBecomeIsNull()
        {
            var root = new FilterCondition { FieldName = "Memo", Operator = ComparisonOperator.Equal, Value = null };
            var builder = new WhereBuilder();
            var result = builder.Build(root);
        }

        [Fact]
        public void IgnoreIfNull_ShouldDropCondition()
        {
            var root = FilterGroup.All(
                new FilterCondition { FieldName = "Keyword", Operator = ComparisonOperator.Contains, Value = null, IgnoreIfNull = true },
                FilterCondition.Equal("DeptId", 1)
            );

            var builder = new WhereBuilder();
            var result = builder.Build(root);
        }

        [Fact]
        public void InWithEmpty_ShouldBeFalseConstant()
        {
            var root = FilterCondition.In("Id", new List<object>());
            var builder = new WhereBuilder();
            var result = builder.Build(root, includeWhereKeyword: false);
        }

        [Fact]
        public void InWithMultiple_ShouldBuildCorrectly()
        {
            var root = FilterCondition.In("Id", new List<object> { 1, 2, 3, 4 });
            var builder = new WhereBuilder();
            var result = builder.Build(root, includeWhereKeyword: false);
        }

        [Fact]
        public void EmptyFilterGroup_ShouldReturnEmptyWhereClause()
        {
            var root = new FilterGroup(); // 預設 Nodes 為空
            var builder = new WhereBuilder();
            var result = builder.Build(root);
            Assert.Equal(string.Empty, result.WhereClause);
        }

    }
}
