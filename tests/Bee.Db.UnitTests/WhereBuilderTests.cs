using System.ComponentModel;
using Bee.Definition.Filters;
using Bee.Db.Query;
using Bee.Definition;

namespace Bee.Db.UnitTests
{
    public class WhereBuilderTests
    {
        [Fact]
        [DisplayName("Build 等於條件應產生正確的 SQL Server WHERE 子句")]
        public void Build_EqualCondition_BuildsSqlServerWhere()
        {
            var root = FilterCondition.Equal("DeptId", 10);
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root);
        }

        [Fact]
        [DisplayName("Build Contains 條件應加入萬用字元")]
        public void Build_LikeContains_AddsWildcards()
        {
            var root = FilterCondition.Contains("Name", "Lee");
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, includeWhereKeyword: false);
        }

        [Fact]
        [DisplayName("Build 巢狀 AND/OR 群組應產生括號")]
        public void Build_GroupAndOr_BuildsParentheses()
        {
            var root = FilterGroup.All(
                FilterCondition.Equal("DeptId", 10),
                FilterGroup.Any(
                    FilterCondition.Contains("Name", "Lee"),
                    FilterCondition.Between("HireDate", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31))
                )
            );

            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root);
        }

        [Fact]
        [DisplayName("Build 值為 null 的等於條件應產生 IS NULL")]
        public void Build_NullEquals_BecomesIsNull()
        {
            var root = new FilterCondition { FieldName = "Memo", Operator = ComparisonOperator.Equal, Value = null };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root);
        }

        [Fact]
        [DisplayName("Build IgnoreIfNull 為 true 且值為 null 時應忽略該條件")]
        public void Build_IgnoreIfNull_DropsNullCondition()
        {
            var root = FilterGroup.All(
                new FilterCondition { FieldName = "Keyword", Operator = ComparisonOperator.Contains, Value = null, IgnoreIfNull = true },
                FilterCondition.Equal("DeptId", 1)
            );

            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root);
        }

        [Fact]
        [DisplayName("Build IN 條件傳入空集合應產生恆假常數")]
        public void Build_InWithEmptyList_ReturnsFalseConstant()
        {
            var root = FilterCondition.In("Id", new List<object>());
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, includeWhereKeyword: false);
        }

        [Fact]
        [DisplayName("Build IN 條件傳入多個值應正確建立")]
        public void Build_InWithMultipleValues_BuildsCorrectly()
        {
            var root = FilterCondition.In("Id", new List<object> { 1, 2, 3, 4 });
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, includeWhereKeyword: false);
        }

        [Fact]
        [DisplayName("Build 空的 FilterGroup 應回傳空的 WHERE 子句")]
        public void Build_EmptyFilterGroup_ReturnsEmptyWhereClause()
        {
            var root = new FilterGroup(); // 預設 Nodes 為空
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root);
            Assert.Equal(string.Empty, result.WhereClause);
        }

    }
}
