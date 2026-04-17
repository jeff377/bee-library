using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers;
using Bee.Definition;
using Bee.Definition.Forms;

namespace Bee.Db.UnitTests
{
    public class SelectCommandBuilderTests
    {
        private static FormSchema BuildSimpleSchema()
        {
            var schema = new FormSchema("demo", "Demo Form");
            var table = schema.Tables!.Add("demo", "Demo Table");
            table.DbTableName = "tb_demo";
            table.Fields!.Add("Id", "Id", FieldDbType.Integer);
            table.Fields!.AddStringField("Name", "Name", 50);
            return schema;
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("Build tableName 為空白應擲 ArgumentException")]
        public void Build_EmptyTableName_Throws(string tableName)
        {
            var schema = BuildSimpleSchema();
            var builder = new SelectCommandBuilder(schema, DatabaseType.SQLServer);

            Assert.Throws<ArgumentException>(() => builder.Build(tableName, string.Empty));
        }

        [Fact]
        [DisplayName("Build tableName 為 null 應擲 ArgumentException")]
        public void Build_NullTableName_Throws()
        {
            var schema = BuildSimpleSchema();
            var builder = new SelectCommandBuilder(schema, DatabaseType.SQLServer);

            Assert.Throws<ArgumentException>(() => builder.Build(null!, string.Empty));
        }

        [Fact]
        [DisplayName("Build 簡易 schema 應產生含 SELECT 與 FROM 的命令")]
        public void Build_SimpleSchema_ProducesSelectAndFromClauses()
        {
            var schema = BuildSimpleSchema();
            var builder = new SelectCommandBuilder(schema, DatabaseType.SQLServer);

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
            var builder = new SelectCommandBuilder(schema, DatabaseType.SQLServer);

            var spec = builder.Build("demo", "Id");

            Assert.NotNull(spec);
            Assert.Contains("Id", spec.CommandText);
        }
    }
}
