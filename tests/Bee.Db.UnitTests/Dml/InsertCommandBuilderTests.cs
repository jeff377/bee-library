using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Definition.Forms;

namespace Bee.Db.UnitTests.Dml
{
    public class InsertCommandBuilderTests
    {
        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema("Employee", "Employee Form");
            var table = schema.Tables!.Add("Employee", "Employee");
            table.DbTableName = "ft_employee";

            // Auto-increment system field — should be skipped.
            table.Fields!.Add(SysFields.No, "Sequence", FieldDbType.AutoIncrement);
            // Primary key, written by client.
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("sys_id", "Employee Id", 50);
            table.Fields!.AddStringField("sys_name", "Employee Name", 100);

            // Relation field — should be skipped.
            table.Fields!.AddStringField("ref_dept_name", "Department Name", 100);
            table.Fields!["ref_dept_name"].Type = FieldType.RelationField;

            return schema;
        }

        private static DataTable BuildEmployeeDataTable()
        {
            var dt = new DataTable("Employee");
            dt.Columns.Add(SysFields.RowId, typeof(Guid));
            dt.Columns.Add("sys_id", typeof(string));
            dt.Columns.Add("sys_name", typeof(string));
            dt.Columns.Add("ref_dept_name", typeof(string));
            return dt;
        }

        [Fact]
        [DisplayName("Build tableName 為空白應擲 ArgumentException")]
        public void Build_EmptyTableName_Throws()
        {
            var schema = BuildEmployeeSchema();
            var builder = new InsertCommandBuilder(schema, DatabaseType.SQLServer);
            var dt = BuildEmployeeDataTable();
            var row = dt.NewRow();

            Assert.Throws<ArgumentException>(() => builder.Build(string.Empty, row));
        }

        [Fact]
        [DisplayName("Build row 為 null 應擲 ArgumentNullException")]
        public void Build_NullRow_Throws()
        {
            var schema = BuildEmployeeSchema();
            var builder = new InsertCommandBuilder(schema, DatabaseType.SQLServer);

            Assert.Throws<ArgumentNullException>(() => builder.Build("Employee", null!));
        }

        [Fact]
        [DisplayName("Build 不存在的 tableName 應擲 InvalidOperationException")]
        public void Build_UnknownTableName_Throws()
        {
            var schema = BuildEmployeeSchema();
            var builder = new InsertCommandBuilder(schema, DatabaseType.SQLServer);
            var dt = BuildEmployeeDataTable();

            Assert.Throws<InvalidOperationException>(() => builder.Build("NoSuchTable", dt.NewRow()));
        }

        [Fact]
        [DisplayName("Build 應產生 SQL Server 方言並排除 RelationField 與 AutoIncrement")]
        public void Build_SqlServer_GeneratesExpectedSqlAndParams()
        {
            var schema = BuildEmployeeSchema();
            var builder = new InsertCommandBuilder(schema, DatabaseType.SQLServer);
            var dt = BuildEmployeeDataTable();
            var rowId = Guid.NewGuid();
            var row = dt.NewRow();
            row[SysFields.RowId] = rowId;
            row["sys_id"] = "E001";
            row["sys_name"] = "Alice";
            // ref_dept_name supplied but is a RelationField; must be excluded.
            row["ref_dept_name"] = "RD";

            var spec = builder.Build("Employee", row);

            Assert.Equal(DbCommandKind.NonQuery, spec.Kind);
            Assert.Equal(
                "INSERT INTO [ft_employee] ([sys_rowid], [sys_id], [sys_name]) VALUES ({0}, {1}, {2})",
                spec.CommandText);
            Assert.Equal(3, spec.Parameters.Count);
            Assert.Equal(rowId, spec.Parameters[0].Value);
            Assert.Equal("E001", spec.Parameters[1].Value);
            Assert.Equal("Alice", spec.Parameters[2].Value);
        }

        [Fact]
        [DisplayName("Build 應產生 PostgreSQL 方言並排除 RelationField 與 AutoIncrement")]
        public void Build_PostgreSql_GeneratesExpectedSql()
        {
            var schema = BuildEmployeeSchema();
            var builder = new InsertCommandBuilder(schema, DatabaseType.PostgreSQL);
            var dt = BuildEmployeeDataTable();
            var row = dt.NewRow();
            row[SysFields.RowId] = Guid.NewGuid();
            row["sys_id"] = "E001";
            row["sys_name"] = "Alice";

            var spec = builder.Build("Employee", row);

            Assert.Equal(
                "INSERT INTO \"ft_employee\" (\"sys_rowid\", \"sys_id\", \"sys_name\") VALUES ({0}, {1}, {2})",
                spec.CommandText);
        }

        [Fact]
        [DisplayName("Build 應略過 DBNull 欄位以保留資料庫預設值")]
        public void Build_DbNullFields_Skipped()
        {
            var schema = BuildEmployeeSchema();
            var builder = new InsertCommandBuilder(schema, DatabaseType.SQLServer);
            var dt = BuildEmployeeDataTable();
            var row = dt.NewRow();
            row[SysFields.RowId] = Guid.NewGuid();
            row["sys_id"] = "E001";
            // sys_name left as DBNull on purpose.

            var spec = builder.Build("Employee", row);

            Assert.DoesNotContain("sys_name", spec.CommandText);
            Assert.Equal(2, spec.Parameters.Count);
        }

        [Fact]
        [DisplayName("Build 無任何可寫入欄位時應擲 InvalidOperationException")]
        public void Build_NoWritableFields_Throws()
        {
            var schema = BuildEmployeeSchema();
            var builder = new InsertCommandBuilder(schema, DatabaseType.SQLServer);
            var dt = BuildEmployeeDataTable();
            var row = dt.NewRow();
            // All columns left as DBNull.

            Assert.Throws<InvalidOperationException>(() => builder.Build("Employee", row));
        }

        [Fact]
        [DisplayName("Build 未設 DbTableName 時 fallback 至 TableName")]
        public void Build_FallbackToTableNameWhenDbTableNameMissing()
        {
            var schema = new FormSchema("X", "X");
            var table = schema.Tables!.Add("Foo", "Foo");
            // DbTableName intentionally left empty.
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("name", "Name", 50);

            var builder = new InsertCommandBuilder(schema, DatabaseType.SQLServer);
            var dt = new DataTable();
            dt.Columns.Add(SysFields.RowId, typeof(Guid));
            dt.Columns.Add("name", typeof(string));
            var row = dt.NewRow();
            row[SysFields.RowId] = Guid.NewGuid();
            row["name"] = "n";

            var spec = builder.Build("Foo", row);

            Assert.Contains("[Foo]", spec.CommandText);
        }
    }
}
