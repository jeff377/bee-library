using System.ComponentModel;
using Bee.Db.Providers.PostgreSql;
using Bee.Definition.Forms;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    public class PgFormCommandBuilderExtraTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public PgFormCommandBuilderExtraTests(SharedDbFixture fx) { _fx = fx; }
        private IDefineAccess DefineAccess => _fx.GetRequiredService<IDefineAccess>();

        private static FormSchema BuildFooSchema()
        {
            var schema = new FormSchema("X", "X");
            var table = schema.Tables!.Add("Foo", "Foo");
            table.DbTableName = "tb_foo";
            table.Fields!.AddStringField("name", "Name", 50);
            return schema;
        }

        private PgFormCommandBuilder NewBuilder()
            => new(BuildFooSchema(), DefineAccess);

        [Fact]
        [DisplayName("BuildCount 應委派至 PostgreSQL 方言並產生 SELECT COUNT(*) 語句（雙引號識別符）")]
        public void BuildCount_DelegatesToPostgreSqlDialect()
        {
            var builder = NewBuilder();
            var spec = builder.BuildCount("Foo");

            Assert.Contains("COUNT(*)", spec.CommandText);
            Assert.Contains("\"tb_foo\"", spec.CommandText);
        }
    }
}
