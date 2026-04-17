using System.ComponentModel;
using Bee.Definition;
using Bee.Db.Query;

namespace Bee.Db.UnitTests
{
    public class SortBuilderTests
    {
        [Fact]
        [DisplayName("Build 傳入 null 排序集合應回傳空字串")]
        public void Build_NullSorts_ReturnsEmptyString()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var result = builder.Build(null, null);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("Build 傳入空排序集合應回傳空字串")]
        public void Build_EmptySorts_ReturnsEmptyString()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var result = builder.Build(new SortFieldCollection(), null);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("Build 單一排序欄位應回傳正確的 ORDER BY 子句")]
        public void Build_SingleSortItem_ReturnsCorrectOrderByClause()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var sorts = new SortFieldCollection()
            {
                new SortField("Name", SortDirection.Asc)
            };
            var result = builder.Build(sorts, null);
            Assert.Equal("ORDER BY Name ASC", result);
        }

        [Fact]
        [DisplayName("Build 多個排序欄位應回傳正確的 ORDER BY 子句")]
        public void Build_MultipleSortItems_ReturnsCorrectOrderByClause()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var sorts = new SortFieldCollection()
            {
                new SortField("Name", SortDirection.Asc),
                new SortField("Age", SortDirection.Desc)
            };
            var result = builder.Build(sorts, null);
            Assert.Equal("ORDER BY Name ASC, Age DESC", result);
        }

        [Fact]
        [DisplayName("Build 排序欄位含 SQL 運算式應回傳正確的 ORDER BY 子句")]
        public void Build_SortItemWithSqlExpression_ReturnsCorrectOrderByClause()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var sorts = new SortFieldCollection()
            {
                new SortField("LEN(Name)", SortDirection.Desc)
            };
            var result = builder.Build(sorts, null);
            Assert.Equal("ORDER BY LEN(Name) DESC", result);
        }
    }
}
