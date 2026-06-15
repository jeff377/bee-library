using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Oracle;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Pure-syntax tests for <see cref="OracleFormCommandBuilder"/>. Verifies that the
    /// Oracle provider routes through the dialect-agnostic cores in <see cref="Bee.Db.Dml"/>
    /// with <see cref="Bee.Definition.Database.DatabaseType.Oracle"/> and emits double-quoted
    /// identifiers + <c>:</c> bind-variable prefixes (per <see cref="DatabaseTypeExtensions.QuoteIdentifier"/>
    /// and <see cref="DatabaseTypeExtensions.GetParameterPrefix"/>).
    /// </summary>
    public class OracleFormCommandBuilderTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public OracleFormCommandBuilderTests(SharedDbFixture fx) { _fx = fx; }
        private IDefineAccess DefineAccess => _fx.GetRequiredService<IDefineAccess>();

        private static FormSchema BuildFooSchema()
        {
            var schema = new FormSchema("X", "X");
            var table = schema.Tables!.Add("Foo", "Foo");
            table.DbTableName = "tb_foo";
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("name", "Name", 50);
            return schema;
        }

        private OracleFormCommandBuilder NewBuilder()
            => new(BuildFooSchema(), DefineAccess);

        [Fact]
        [DisplayName("FormSchema 建構子 null 應擲 ArgumentNullException")]
        public void Constructor_NullFormSchema_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new OracleFormCommandBuilder(null!, DefineAccess));
        }

        [Fact]
        [DisplayName("FormSchema 建構子 null IDefineAccess 應擲 ArgumentNullException")]
        public void Constructor_NullDefineAccess_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new OracleFormCommandBuilder(BuildFooSchema(), null!));
        }

        [Fact]
        [DisplayName("BuildSelect 應委派至 Oracle 方言並產生 SELECT 語句（雙引號識別符）")]
        public void BuildSelect_DelegatesToOracleDialect()
        {
            var builder = NewBuilder();

            var spec = builder.BuildSelect("Foo", "name", null, null);

            Assert.Contains("\"TB_FOO\"", spec.CommandText);
            Assert.Contains("\"NAME\"", spec.CommandText);
        }


        [Fact]
        [DisplayName("BuildDelete 應委派至 Oracle 方言並產生 DELETE 語句")]
        public void BuildDelete_DelegatesToOracleDialect()
        {
            var builder = NewBuilder();
            var spec = builder.BuildDelete("Foo", FilterCondition.Equal(SysFields.RowId, Guid.NewGuid()));

            Assert.Contains("DELETE FROM \"TB_FOO\"", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildCount 應委派至 Oracle 方言並產生 SELECT COUNT(*) 語句（雙引號識別符）")]
        public void BuildCount_DelegatesToOracleDialect()
        {
            var builder = NewBuilder();
            var spec = builder.BuildCount("Foo");

            Assert.Contains("COUNT", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"TB_FOO\"", spec.CommandText);
        }
    }
}
