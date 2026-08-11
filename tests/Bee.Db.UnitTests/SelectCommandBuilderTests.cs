using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Dml;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Database;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    public class SelectCommandBuilderTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SelectCommandBuilderTests(SharedDbFixture fx) { _fx = fx; }
        private static FormSchema BuildSimpleSchema()
        {
            var schema = new FormSchema("demo", "Demo Form");
            var table = schema.Tables!.Add("demo", "Demo Table");
            table.DbTableName = "tb_demo";
            table.Fields!.Add("Id", "Id", FieldDbType.Integer);
            table.Fields!.AddStringField("Name", "Name", 50);
            return schema;
        }

        private SelectCommandBuilder NewBuilder(FormSchema schema, DatabaseType dbType = DatabaseType.SQLServer)
            => new(schema, dbType, _fx.GetRequiredService<IDefineAccess>());

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("Build tableName 為空白應擲 ArgumentException")]
        public void Build_EmptyTableName_Throws(string tableName)
        {
            var schema = BuildSimpleSchema();
            var builder = NewBuilder(schema);

            Assert.Throws<ArgumentException>(() => builder.Build(tableName, string.Empty));
        }

        [Fact]
        [DisplayName("Build tableName 為 null 應擲 ArgumentException")]
        public void Build_NullTableName_Throws()
        {
            var schema = BuildSimpleSchema();
            var builder = NewBuilder(schema);

            Assert.Throws<ArgumentException>(() => builder.Build(null!, string.Empty));
        }

        [Fact]
        [DisplayName("Build 簡易 schema 應產生含 SELECT 與 FROM 的命令")]
        public void Build_SimpleSchema_ProducesSelectAndFromClauses()
        {
            var schema = BuildSimpleSchema();
            var builder = NewBuilder(schema);

            var spec = builder.Build("demo", string.Empty);

            Assert.NotNull(spec);
            Assert.Equal(DbCommandKind.DataTable, spec.Kind);
            Assert.Contains("SELECT", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FROM", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("tb_demo", spec.CommandText);
        }

        [Fact]
        [DisplayName("Build 指定 selectFields 應只包含指定欄位")]
        public void Build_WithSelectFields_RestrictsColumns()
        {
            var schema = BuildSimpleSchema();
            var builder = NewBuilder(schema);

            var spec = builder.Build("demo", "Id");

            Assert.NotNull(spec);
            Assert.Contains("Id", spec.CommandText);
        }

        [Fact]
        [DisplayName("Build Between 篩選應產生 BETWEEN 子句與兩個參數")]
        public void Build_BetweenFilter_ProducesBetweenClauseWithTwoParameters()
        {
            var schema = BuildSimpleSchema();
            var builder = NewBuilder(schema);
            var filter = FilterCondition.Between("Id", 10, 20);

            var spec = builder.Build("demo", string.Empty, filter);

            Assert.Contains("A.[Id] BETWEEN @p0 AND @p1", spec.CommandText);
            Assert.Equal(2, spec.Parameters.Count);
        }

        [Fact]
        [DisplayName("BuildCount Between 篩選應產生 BETWEEN 子句與兩個參數")]
        public void BuildCount_BetweenFilter_ProducesBetweenClauseWithTwoParameters()
        {
            var schema = BuildSimpleSchema();
            var builder = NewBuilder(schema);
            var filter = FilterCondition.Between("Id", 10, 20);

            var spec = builder.BuildCount("demo", filter);

            Assert.Contains("A.[Id] BETWEEN @p0 AND @p1", spec.CommandText);
            Assert.Equal(2, spec.Parameters.Count);
        }

        [Fact]
        [DisplayName("Build IgnoreIfNull 篩選在值為 null 時應自 WHERE 子句移除")]
        public void Build_IgnoreIfNullFilter_OmitsConditionFromWhere()
        {
            var schema = BuildSimpleSchema();
            var builder = NewBuilder(schema);
            var filter = FilterGroup.All(
                new FilterCondition { FieldName = "Name", Operator = ComparisonOperator.Contains, Value = null, IgnoreIfNull = true },
                FilterCondition.Equal("Id", 1)
            );

            var spec = builder.Build("demo", string.Empty, filter);

            Assert.Contains("WHERE (A.[Id] = @p0)", spec.CommandText);
            Assert.DoesNotContain("Name] LIKE", spec.CommandText);
            Assert.Single(spec.Parameters);
        }

        [Fact]
        [DisplayName("Build IgnoreIfNull 的 Equal 篩選在值為 null 時不應退化成 IS NULL")]
        public void Build_IgnoreIfNullEqualFilter_DoesNotBecomeIsNull()
        {
            var schema = BuildSimpleSchema();
            var builder = NewBuilder(schema);
            var filter = FilterCondition.Equal("Name", null!, ignoreIfNull: true);

            var spec = builder.Build("demo", string.Empty, filter);

            Assert.DoesNotContain("WHERE", spec.CommandText, StringComparison.Ordinal);
            Assert.DoesNotContain("IS NULL", spec.CommandText, StringComparison.Ordinal);
        }
    }
}
