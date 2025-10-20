using Bee.Define;

namespace Bee.Db.UnitTests
{
    public class SortBuilderTests
    {
        [Fact]
        public void Build_NullSorts_ThrowsArgumentNullException()
        {
            var builder = new SqlServerSortBuilder();
            Assert.Throws<ArgumentNullException>(() => builder.Build(null));
        }

        [Fact]
        public void Build_EmptySorts_ReturnsEmptyString()
        {
            var builder = new SqlServerSortBuilder();
            var result = builder.Build(new SortItemCollection());
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Build_SingleSortItem_ReturnsCorrectOrderByClause()
        {
            var builder = new SqlServerSortBuilder();
            var sorts = new SortItemCollection()
            {
                new SortItem("Name", SortDirection.Asc)
            };
            var result = builder.Build(sorts);
            Assert.Equal("ORDER BY Name ASC", result);
        }

        [Fact]
        public void Build_MultipleSortItems_ReturnsCorrectOrderByClause()
        {
            var builder = new SqlServerSortBuilder();
            var sorts = new SortItemCollection()
            {
                new SortItem("Name", SortDirection.Asc),
                new SortItem("Age", SortDirection.Desc)
            };
            var result = builder.Build(sorts);
            Assert.Equal("ORDER BY Name ASC, Age DESC", result);
        }

        [Fact]
        public void Build_SortItemWithSqlExpression_ReturnsCorrectOrderByClause()
        {
            var builder = new SqlServerSortBuilder();
            var sorts = new SortItemCollection()
            {
                new SortItem("LEN(Name)", SortDirection.Desc)
            };
            var result = builder.Build(sorts);
            Assert.Equal("ORDER BY LEN(Name) DESC", result);
        }
    }
}
