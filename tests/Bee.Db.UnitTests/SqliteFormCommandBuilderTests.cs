using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Sqlite;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    public class SqliteFormCommandBuilderTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SqliteFormCommandBuilderTests(SharedDbFixture fx) { _fx = fx; }
        private static FormSchema BuildFooSchema()
        {
            var schema = new FormSchema("X", "X");
            var table = schema.Tables!.Add("Foo", "Foo");
            table.DbTableName = "tb_foo";
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("name", "Name", 50);
            return schema;
        }

        private SqliteFormCommandBuilder NewBuilder(FormSchema schema)
            => new(schema, _fx.GetRequiredService<IDefineAccess>());

        [Fact]
        [DisplayName("FormSchema 建構子 null 應擲 ArgumentNullException")]
        public void Constructor_NullFormSchema_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SqliteFormCommandBuilder(
                null!, _fx.GetRequiredService<IDefineAccess>()));
        }

        [Fact]
        [DisplayName("FormSchema 建構子 null IDefineAccess 應擲 ArgumentNullException")]
        public void Constructor_NullDefineAccess_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SqliteFormCommandBuilder(BuildFooSchema(), null!));
        }

        [Fact]
        [DisplayName("BuildSelect 應委派至 SQLite 方言並產生 SELECT 語句")]
        public void BuildSelect_DelegatesToSqliteDialect()
        {
            var builder = NewBuilder(BuildFooSchema());

            var spec = builder.BuildSelect("Foo", "name", null, null);

            Assert.Contains("\"tb_foo\"", spec.CommandText);
            Assert.Contains("\"name\"", spec.CommandText);
        }


        [Fact]
        [DisplayName("BuildDelete 應委派至 SQLite 方言並產生 DELETE 語句")]
        public void BuildDelete_DelegatesToSqliteDialect()
        {
            var builder = NewBuilder(BuildFooSchema());
            var spec = builder.BuildDelete("Foo", FilterCondition.Equal(SysFields.RowId, Guid.NewGuid()));

            Assert.Contains("DELETE FROM \"tb_foo\"", spec.CommandText);
        }
    }
}
