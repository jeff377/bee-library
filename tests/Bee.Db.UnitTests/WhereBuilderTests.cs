using System.ComponentModel;
using Bee.Definition.Filters;
using Bee.Db.Dml;
using Bee.Definition.Database;

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
            var result = builder.Build(root, null);
            Assert.Equal("WHERE DeptId = @p0", result.WhereClause);
            Assert.NotNull(result.Parameters);
            Assert.Single(result.Parameters);
            Assert.Equal(10, result.Parameters["@p0"]);
        }

        [Fact]
        [DisplayName("Build Contains 條件應加入萬用字元")]
        public void Build_LikeContains_AddsWildcards()
        {
            var root = FilterCondition.Contains("Name", "Lee");
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null, includeWhereKeyword: false);
            Assert.Equal("Name LIKE @p0", result.WhereClause);
            Assert.NotNull(result.Parameters);
            Assert.Equal("%Lee%", result.Parameters["@p0"]);
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
            var result = builder.Build(root, null);
            Assert.StartsWith("WHERE (", result.WhereClause);
            Assert.Contains(" AND ", result.WhereClause);
            Assert.Contains(" OR ", result.WhereClause);
            Assert.NotNull(result.Parameters);
            Assert.Equal(4, result.Parameters.Count);
        }

        [Fact]
        [DisplayName("Build 值為 null 的等於條件應產生 IS NULL")]
        public void Build_NullEquals_BecomesIsNull()
        {
            var root = new FilterCondition { FieldName = "Memo", Operator = ComparisonOperator.Equal, Value = null };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null);
            Assert.Equal("WHERE Memo IS NULL", result.WhereClause);
            Assert.True(result.Parameters == null || result.Parameters.Count == 0);
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
            var result = builder.Build(root, null);
            Assert.Equal("WHERE (DeptId = @p0)", result.WhereClause);
            Assert.Single(result.Parameters!);
        }

        [Fact]
        [DisplayName("Build IN 條件傳入空集合應產生恆假常數")]
        public void Build_InWithEmptyList_ReturnsFalseConstant()
        {
            var root = FilterCondition.In("Id", []);
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null, includeWhereKeyword: false);
            Assert.Equal("1 = 0", result.WhereClause);
            Assert.True(result.Parameters == null || result.Parameters.Count == 0);
        }

        [Fact]
        [DisplayName("Build IN 條件傳入多個值應正確建立")]
        public void Build_InWithMultipleValues_BuildsCorrectly()
        {
            var root = FilterCondition.In("Id", [1, 2, 3, 4]);
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null, includeWhereKeyword: false);
            Assert.Equal("Id IN (@p0, @p1, @p2, @p3)", result.WhereClause);
            Assert.NotNull(result.Parameters);
            Assert.Equal(4, result.Parameters.Count);
        }

        [Fact]
        [DisplayName("Build 空的 FilterGroup 應回傳空的 WHERE 子句")]
        public void Build_EmptyFilterGroup_ReturnsEmptyWhereClause()
        {
            var root = new FilterGroup(); // 預設 Nodes 為空
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null);
            Assert.Equal(string.Empty, result.WhereClause);
        }

        [Fact]
        [DisplayName("Build root 為 null 應回傳空 WhereBuildResult")]
        public void Build_NullRoot_ReturnsEmptyResult()
        {
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(null, null);
            Assert.Equal(string.Empty, result.WhereClause);
        }

        [Theory]
        [InlineData(ComparisonOperator.GreaterThan, ">")]
        [InlineData(ComparisonOperator.GreaterThanOrEqual, ">=")]
        [InlineData(ComparisonOperator.LessThan, "<")]
        [InlineData(ComparisonOperator.LessThanOrEqual, "<=")]
        [InlineData(ComparisonOperator.NotEqual, "<>")]
        [DisplayName("Build 各比較運算符應產生對應 SQL 運算子")]
        public void Build_ComparisonOperators_BuildExpectedSql(ComparisonOperator op, string sqlOp)
        {
            var root = new FilterCondition { FieldName = "Age", Operator = op, Value = 18 };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null, includeWhereKeyword: false);
            Assert.Equal($"Age {sqlOp} @p0", result.WhereClause);
            Assert.Equal(18, result.Parameters!["@p0"]);
        }

        [Fact]
        [DisplayName("Build Like 條件應使用原始值")]
        public void Build_Like_UsesRawValue()
        {
            var root = new FilterCondition { FieldName = "Name", Operator = ComparisonOperator.Like, Value = "Lee%" };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null, includeWhereKeyword: false);
            Assert.Equal("Name LIKE @p0", result.WhereClause);
            Assert.Equal("Lee%", result.Parameters!["@p0"]);
        }

        [Fact]
        [DisplayName("Build StartsWith 應在尾端加上萬用字元")]
        public void Build_StartsWith_AddsTrailingWildcard()
        {
            var root = FilterCondition.StartsWith("Name", "Lee");
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null, includeWhereKeyword: false);
            Assert.Equal("Name LIKE @p0", result.WhereClause);
            Assert.Equal("Lee%", result.Parameters!["@p0"]);
        }

        [Fact]
        [DisplayName("Build EndsWith 應在開頭加上萬用字元")]
        public void Build_EndsWith_AddsLeadingWildcard()
        {
            var root = FilterCondition.EndsWith("Name", "Lee");
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null, includeWhereKeyword: false);
            Assert.Equal("Name LIKE @p0", result.WhereClause);
            Assert.Equal("%Lee", result.Parameters!["@p0"]);
        }

        [Fact]
        [DisplayName("Build NotEqual 配合 null 值應產生 IS NOT NULL")]
        public void Build_NotEqualNull_BecomesIsNotNull()
        {
            var root = new FilterCondition { FieldName = "Memo", Operator = ComparisonOperator.NotEqual, Value = null };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null);
            Assert.Equal("WHERE Memo IS NOT NULL", result.WhereClause);
        }

        [Fact]
        [DisplayName("Build 不支援的 null 比較運算符應擲出 InvalidOperationException")]
        public void Build_UnsupportedNullOperator_Throws()
        {
            var root = new FilterCondition { FieldName = "Age", Operator = ComparisonOperator.GreaterThan, Value = null };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            Assert.Throws<InvalidOperationException>(() => builder.Build(root, null));
        }

        [Fact]
        [DisplayName("Build Between 缺第二值且 IgnoreIfNull=true 時應忽略條件")]
        public void Build_BetweenMissingSecondValue_IgnoreIfNull_DropsCondition()
        {
            var root = new FilterCondition
            {
                FieldName = "Age",
                Operator = ComparisonOperator.Between,
                Value = 18,
                SecondValue = null,
                IgnoreIfNull = true
            };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null, includeWhereKeyword: false);
            Assert.Equal(string.Empty, result.WhereClause);
        }

        [Fact]
        [DisplayName("Build Between 缺第二值且未設 IgnoreIfNull 應擲例外")]
        public void Build_BetweenMissingSecondValue_Throws()
        {
            var root = new FilterCondition
            {
                FieldName = "Age",
                Operator = ComparisonOperator.Between,
                Value = 18,
                SecondValue = null
            };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            Assert.Throws<InvalidOperationException>(() => builder.Build(root, null));
        }

        [Fact]
        [DisplayName("Build Between 完整值應產生 BETWEEN 子句")]
        public void Build_Between_BuildsBetweenClause()
        {
            var root = FilterCondition.Between("Age", 18, 60);
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            var result = builder.Build(root, null, includeWhereKeyword: false);
            Assert.Equal("Age BETWEEN @p0 AND @p1", result.WhereClause);
            Assert.Equal(18, result.Parameters!["@p0"]);
            Assert.Equal(60, result.Parameters["@p1"]);
        }

        [Fact]
        [DisplayName("Build IN 條件傳入非 enumerable 值應擲例外")]
        public void Build_InWithNonEnumerable_Throws()
        {
            var root = new FilterCondition { FieldName = "Id", Operator = ComparisonOperator.In, Value = 1 };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            Assert.Throws<InvalidOperationException>(() => builder.Build(root, null));
        }

        [Fact]
        [DisplayName("Build 空 FieldName 應擲出 InvalidOperationException")]
        public void Build_EmptyFieldName_Throws()
        {
            var root = new FilterCondition { FieldName = "", Operator = ComparisonOperator.Equal, Value = 1 };
            var builder = new WhereBuilder(DatabaseType.SQLServer);
            Assert.Throws<InvalidOperationException>(() => builder.Build(root, null));
        }
    }
}
