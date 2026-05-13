using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    public class SqlFormCommandBuilderTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SqlFormCommandBuilderTests(SharedDbFixture fx) { _fx = fx; }
        private IDefineAccess DefineAccess => _fx.GetRequiredService<IDefineAccess>();

        private SqlFormCommandBuilder NewBuilder(FormSchema schema)
            => new(schema, DefineAccess);

        [Fact]
        [DisplayName("FormSchema 建構子 null 應擲 ArgumentNullException")]
        public void Constructor_NullFormSchema_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlFormCommandBuilder(null!, DefineAccess));
        }

        [Fact]
        [DisplayName("FormSchema 建構子 null IDefineAccess 應擲 ArgumentNullException")]
        public void Constructor_NullDefineAccess_Throws()
        {
            var schema = new FormSchema("X", "X");
            Assert.Throws<ArgumentNullException>(() => new SqlFormCommandBuilder(schema, null!));
        }

        [Fact]
        [DisplayName("BuildInsert 應委派至 SQL Server 方言並產生 INSERT 語句")]
        public void BuildInsert_DelegatesToSqlServerDialect()
        {
            var schema = new FormSchema("X", "X");
            var table = schema.Tables!.Add("Foo", "Foo");
            table.DbTableName = "tb_foo";
            table.Fields!.AddStringField("name", "Name", 50);

            var dt = new DataTable();
            dt.Columns.Add("name", typeof(string));
            var row = dt.NewRow();
            row["name"] = "n";

            var builder = NewBuilder(schema);
            var spec = builder.BuildInsert("Foo", row);

            Assert.Contains("[tb_foo]", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildUpdate 應委派至 SQL Server 方言並產生 UPDATE 語句")]
        public void BuildUpdate_DelegatesToSqlServerDialect()
        {
            var schema = new FormSchema("X", "X");
            var table = schema.Tables!.Add("Foo", "Foo");
            table.DbTableName = "tb_foo";
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("name", "Name", 50);

            var dt = new DataTable();
            dt.Columns.Add(SysFields.RowId, typeof(Guid));
            dt.Columns.Add("name", typeof(string));
            var row = dt.NewRow();
            row[SysFields.RowId] = Guid.NewGuid();
            row["name"] = "old";
            dt.Rows.Add(row);
            dt.AcceptChanges();
            row["name"] = "new";

            var builder = NewBuilder(schema);
            var spec = builder.BuildUpdate("Foo", row);

            Assert.Contains("UPDATE [tb_foo]", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildDelete 應委派至 SQL Server 方言並產生 DELETE 語句")]
        public void BuildDelete_DelegatesToSqlServerDialect()
        {
            var schema = new FormSchema("X", "X");
            var table = schema.Tables!.Add("Foo", "Foo");
            table.DbTableName = "tb_foo";
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);

            var builder = NewBuilder(schema);
            var spec = builder.BuildDelete("Foo", FilterCondition.Equal(SysFields.RowId, Guid.NewGuid()));

            Assert.Contains("DELETE FROM [tb_foo]", spec.CommandText);
        }
    }
}
