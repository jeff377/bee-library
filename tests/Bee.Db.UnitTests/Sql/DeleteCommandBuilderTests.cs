using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Sql;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;

namespace Bee.Db.UnitTests.Sql
{
    public class DeleteCommandBuilderTests
    {
        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema("Employee", "Employee Form");
            var table = schema.Tables!.Add("Employee", "Employee");
            table.DbTableName = "ft_employee";
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.Add(SysFields.MasterRowId, "Master Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("sys_id", "Employee Id", 50);
            table.Fields!.AddStringField("ref_dept_name", "Department Name", 100);
            table.Fields!["ref_dept_name"].Type = FieldType.RelationField;
            return schema;
        }

        [Fact]
        [DisplayName("Build tableName 為空白應擲 ArgumentException")]
        public void Build_EmptyTableName_Throws()
        {
            var builder = new DeleteCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            Assert.Throws<ArgumentException>(() =>
                builder.Build(string.Empty, FilterCondition.Equal(SysFields.RowId, Guid.NewGuid())));
        }

        [Fact]
        [DisplayName("Build filter 為 null 應擲 ArgumentNullException")]
        public void Build_NullFilter_Throws()
        {
            var builder = new DeleteCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            Assert.Throws<ArgumentNullException>(() => builder.Build("Employee", null!));
        }

        [Fact]
        [DisplayName("Build 不存在的 tableName 應擲 InvalidOperationException")]
        public void Build_UnknownTableName_Throws()
        {
            var builder = new DeleteCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            Assert.Throws<InvalidOperationException>(() =>
                builder.Build("NoSuchTable", FilterCondition.Equal(SysFields.RowId, Guid.NewGuid())));
        }

        [Fact]
        [DisplayName("Build 應產生 SQL Server 方言並 quote 識別子與欄位")]
        public void Build_SqlServer_GeneratesExpectedSqlAndParam()
        {
            var rowId = Guid.NewGuid();
            var builder = new DeleteCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var spec = builder.Build("Employee", FilterCondition.Equal(SysFields.RowId, rowId));

            Assert.Equal(DbCommandKind.NonQuery, spec.Kind);
            Assert.Equal("DELETE FROM [ft_employee] WHERE [sys_rowid] = @p0", spec.CommandText);
            Assert.Single(spec.Parameters);
            Assert.Equal(rowId, spec.Parameters[0].Value);
        }

        [Fact]
        [DisplayName("Build 應產生 PostgreSQL 方言並 quote 識別子與欄位")]
        public void Build_PostgreSql_GeneratesExpectedSql()
        {
            var builder = new DeleteCommandBuilder(BuildEmployeeSchema(), DatabaseType.PostgreSQL);
            var spec = builder.Build("Employee", FilterCondition.Equal(SysFields.RowId, Guid.NewGuid()));

            Assert.Equal("DELETE FROM \"ft_employee\" WHERE \"sys_rowid\" = @p0", spec.CommandText);
        }

        [Fact]
        [DisplayName("Build 以 sys_master_rowid 為條件可刪除明細")]
        public void Build_MasterRowId_DeletesDetailRows()
        {
            var masterId = Guid.NewGuid();
            var builder = new DeleteCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var spec = builder.Build("Employee", FilterCondition.Equal(SysFields.MasterRowId, masterId));

            Assert.Contains("[sys_master_rowid] = @p0", spec.CommandText);
            Assert.Equal(masterId, spec.Parameters[0].Value);
        }

        [Fact]
        [DisplayName("Build 引用 RelationField 應擲 NotSupportedException")]
        public void Build_RelationFieldInFilter_Throws()
        {
            var builder = new DeleteCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            Assert.Throws<NotSupportedException>(() =>
                builder.Build("Employee", FilterCondition.Equal("ref_dept_name", "Sales")));
        }

        [Fact]
        [DisplayName("Build 引用未知欄位應擲 NotSupportedException")]
        public void Build_UnknownFieldInFilter_Throws()
        {
            var builder = new DeleteCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            Assert.Throws<NotSupportedException>(() =>
                builder.Build("Employee", FilterCondition.Equal("not_in_schema", 1)));
        }

        [Fact]
        [DisplayName("Build FilterGroup 應正確展開為 WHERE 子句")]
        public void Build_FilterGroup_ProducesCompositeWhere()
        {
            var builder = new DeleteCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var filter = FilterGroup.All(
                FilterCondition.Equal(SysFields.RowId, Guid.NewGuid()),
                FilterCondition.Equal("sys_id", "E001"));

            var spec = builder.Build("Employee", filter);

            Assert.Contains("[sys_rowid] = @p0", spec.CommandText);
            Assert.Contains("[sys_id] = @p1", spec.CommandText);
            Assert.Equal(2, spec.Parameters.Count);
        }
    }
}
