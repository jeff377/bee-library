using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class TableSchemaCommandBuilderTests
    {
        private static TableSchema BuildSampleSchema()
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            schema.Fields!.Add("age", "Age", FieldDbType.Integer);
            schema.Indexes!.AddPrimaryKey(SysFields.RowId);
            return schema;
        }

        #region 建構子測試

        [Fact]
        [DisplayName("建構子 TableSchema 為 null 應擲出 ArgumentNullException")]
        public void Constructor_NullTableSchema_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new TableSchemaCommandBuilder(DatabaseType.SQLServer, null!));
        }

        [Fact]
        [DisplayName("單參數建構子應採用 BackendInfo 的 DatabaseType")]
        public void Constructor_SingleArg_UsesBackendDatabaseType()
        {
            var schema = BuildSampleSchema();
            var builder = new TableSchemaCommandBuilder(schema);
            Assert.Equal(BackendInfo.DatabaseType, builder.DatabaseType);
            Assert.Same(schema, builder.TableSchema);
        }

        [Fact]
        [DisplayName("雙參數建構子應使用顯式指定的 DatabaseType")]
        public void Constructor_TwoArgs_UsesSpecifiedDatabaseType()
        {
            var schema = BuildSampleSchema();
            var builder = new TableSchemaCommandBuilder(DatabaseType.MySQL, schema);
            Assert.Equal(DatabaseType.MySQL, builder.DatabaseType);
        }

        #endregion

        #region BuildInsertCommand 測試

        [Fact]
        [DisplayName("BuildInsertCommand 應產生 INSERT 語句並含全部非自增欄位")]
        public void BuildInsertCommand_ContainsAllNonAutoIncrementFields()
        {
            var schema = BuildSampleSchema();
            var builder = new TableSchemaCommandBuilder(DatabaseType.SQLServer, schema);

            var cmd = builder.BuildInsertCommand();

            Assert.Contains("Insert Into [st_demo]", cmd.CommandText);
            Assert.Contains("[sys_rowid]", cmd.CommandText);
            Assert.Contains("[name]", cmd.CommandText);
            Assert.Contains("[age]", cmd.CommandText);
            Assert.Contains("@sys_rowid", cmd.CommandText);
            Assert.Contains("@name", cmd.CommandText);
            Assert.Contains("@age", cmd.CommandText);
            Assert.Equal(3, cmd.Parameters.Count);
        }

        [Fact]
        [DisplayName("BuildInsertCommand 應略過 AutoIncrement 欄位")]
        public void BuildInsertCommand_SkipsAutoIncrementField()
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            schema.Fields!.Add("seq", "Seq", FieldDbType.AutoIncrement);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 30);
            schema.Indexes!.AddPrimaryKey(SysFields.RowId);

            var builder = new TableSchemaCommandBuilder(DatabaseType.SQLServer, schema);
            var cmd = builder.BuildInsertCommand();

            Assert.DoesNotContain("[seq]", cmd.CommandText);
            Assert.DoesNotContain("@seq", cmd.CommandText);
            Assert.False(cmd.Parameters.Contains("seq"));
        }

        #endregion

        #region BuildUpdateCommand 測試

        [Fact]
        [DisplayName("BuildUpdateCommand 應產生 UPDATE 語句並以主鍵為 WHERE 條件")]
        public void BuildUpdateCommand_HasSetClauseAndPrimaryKeyWhere()
        {
            var schema = BuildSampleSchema();
            var builder = new TableSchemaCommandBuilder(DatabaseType.SQLServer, schema);

            var cmd = builder.BuildUpdateCommand();

            Assert.Contains("Update [st_demo] Set", cmd.CommandText);
            Assert.Contains("[name]=@name", cmd.CommandText);
            Assert.Contains("[age]=@age", cmd.CommandText);
            Assert.Contains("Where [sys_rowid]=@sys_rowid", cmd.CommandText);
            // 非 PK 欄位 + 1 PK 欄位
            Assert.Equal(3, cmd.Parameters.Count);
        }

        [Fact]
        [DisplayName("BuildUpdateCommand 主鍵參數應使用 Original 版本")]
        public void BuildUpdateCommand_KeyParameterUsesOriginalVersion()
        {
            var schema = BuildSampleSchema();
            var builder = new TableSchemaCommandBuilder(DatabaseType.SQLServer, schema);

            var cmd = builder.BuildUpdateCommand();

            Assert.Equal(DataRowVersion.Original, cmd.Parameters[SysFields.RowId].SourceVersion);
            Assert.Equal(DataRowVersion.Current, cmd.Parameters["name"].SourceVersion);
        }

        #endregion

        #region BuildDeleteCommand 測試

        [Fact]
        [DisplayName("BuildDeleteCommand 應產生 DELETE 語句並以主鍵為 WHERE 條件")]
        public void BuildDeleteCommand_HasPrimaryKeyWhere()
        {
            var schema = BuildSampleSchema();
            var builder = new TableSchemaCommandBuilder(DatabaseType.SQLServer, schema);

            var cmd = builder.BuildDeleteCommand();

            Assert.Contains("Delete From [st_demo]", cmd.CommandText);
            Assert.Contains("Where [sys_rowid]=@sys_rowid", cmd.CommandText);
            Assert.Single(cmd.Parameters);
            Assert.Equal(DataRowVersion.Original, cmd.Parameters[SysFields.RowId].SourceVersion);
        }

        #endregion

        #region BuildUpdateSpec 測試

        [Fact]
        [DisplayName("BuildUpdateSpec 應同時包裝 Insert/Update/Delete 命令與 DataTable")]
        public void BuildUpdateSpec_PackagesAllThreeCommands()
        {
            var schema = BuildSampleSchema();
            var builder = new TableSchemaCommandBuilder(DatabaseType.SQLServer, schema);
            var dataTable = new DataTable("st_demo");

            var spec = builder.BuildUpdateSpec(dataTable);

            Assert.Same(dataTable, spec.DataTable);
            Assert.NotNull(spec.InsertCommand);
            Assert.NotNull(spec.UpdateCommand);
            Assert.NotNull(spec.DeleteCommand);
            Assert.Contains("Insert Into", spec.InsertCommand!.CommandText);
            Assert.Contains("Update", spec.UpdateCommand!.CommandText);
            Assert.Contains("Delete From", spec.DeleteCommand!.CommandText);
        }

        [Fact]
        [DisplayName("BuildUpdateSpec DataTable 為 null 應擲出 ArgumentNullException")]
        public void BuildUpdateSpec_NullDataTable_Throws()
        {
            var schema = BuildSampleSchema();
            var builder = new TableSchemaCommandBuilder(DatabaseType.SQLServer, schema);

            Assert.Throws<ArgumentNullException>(() => builder.BuildUpdateSpec(null!));
        }

        #endregion
    }
}
