using Bee.Define;

namespace Bee.Db.UnitTests
{
    public class SortBuilderTests
    {
        [Fact]
        public void Build_NullSorts_ThrowsArgumentNullException()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var result = builder.Build(null);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Build_EmptySorts_ReturnsEmptyString()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var result = builder.Build(new SortFieldCollection());
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Build_SingleSortItem_ReturnsCorrectOrderByClause()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var sorts = new SortFieldCollection()
            {
                new SortField("Name", SortDirection.Asc)
            };
            var result = builder.Build(sorts);
            Assert.Equal("ORDER BY Name ASC", result);
        }

        [Fact]
        public void Build_MultipleSortItems_ReturnsCorrectOrderByClause()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var sorts = new SortFieldCollection()
            {
                new SortField("Name", SortDirection.Asc),
                new SortField("Age", SortDirection.Desc)
            };
            var result = builder.Build(sorts);
            Assert.Equal("ORDER BY Name ASC, Age DESC", result);
        }

        [Fact]
        public void Build_SortItemWithSqlExpression_ReturnsCorrectOrderByClause()
        {
            var builder = new SortBuilder(DatabaseType.SQLServer);
            var sorts = new SortFieldCollection()
            {
                new SortField("LEN(Name)", SortDirection.Desc)
            };
            var result = builder.Build(sorts);
            Assert.Equal("ORDER BY LEN(Name) DESC", result);
        }
    }
}
