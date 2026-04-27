using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Db.Sql;
using Bee.Definition;
using Bee.Definition.Forms;

namespace Bee.Db.UnitTests.Sql
{
    public class UpdateCommandBuilderTests
    {
        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema("Employee", "Employee Form");
            var table = schema.Tables!.Add("Employee", "Employee");
            table.DbTableName = "ft_employee";

            // Auto-increment field — must be skipped.
            table.Fields!.Add(SysFields.No, "Sequence", FieldDbType.AutoIncrement);
            // Primary key.
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("sys_id", "Employee Id", 50);
            table.Fields!.AddStringField("sys_name", "Employee Name", 100);
            // Relation field — must be skipped.
            table.Fields!.AddStringField("ref_dept_name", "Department Name", 100);
            table.Fields!["ref_dept_name"].Type = FieldType.RelationField;
            return schema;
        }

        private static (DataTable table, DataRow row, Guid rowId) NewModifiedRow()
        {
            var dt = new DataTable("Employee");
            dt.Columns.Add(SysFields.RowId, typeof(Guid));
            dt.Columns.Add("sys_id", typeof(string));
            dt.Columns.Add("sys_name", typeof(string));
            dt.Columns.Add("ref_dept_name", typeof(string));

            var rowId = Guid.NewGuid();
            var row = dt.NewRow();
            row[SysFields.RowId] = rowId;
            row["sys_id"] = "E001";
            row["sys_name"] = "Alice";
            row["ref_dept_name"] = "OldDept";
            dt.Rows.Add(row);
            dt.AcceptChanges();

            return (dt, row, rowId);
        }

        [Fact]
        [DisplayName("Build tableName 為空白應擲 ArgumentException")]
        public void Build_EmptyTableName_Throws()
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var (_, row, _) = NewModifiedRow();
            row["sys_name"] = "Bob";

            Assert.Throws<ArgumentException>(() => builder.Build(string.Empty, row));
        }

        [Fact]
        [DisplayName("Build row 為 null 應擲 ArgumentNullException")]
        public void Build_NullRow_Throws()
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            Assert.Throws<ArgumentNullException>(() => builder.Build("Employee", null!));
        }

        [Fact]
        [DisplayName("Build 不存在的 tableName 應擲 InvalidOperationException")]
        public void Build_UnknownTableName_Throws()
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var (_, row, _) = NewModifiedRow();
            row["sys_name"] = "Bob";

            Assert.Throws<InvalidOperationException>(() => builder.Build("NoSuchTable", row));
        }

        [Theory]
        [InlineData(DataRowState.Unchanged)]
        [InlineData(DataRowState.Added)]
        [InlineData(DataRowState.Deleted)]
        [InlineData(DataRowState.Detached)]
        [DisplayName("Build 非 Modified 狀態應擲 InvalidOperationException")]
        public void Build_NonModifiedRowState_Throws(DataRowState targetState)
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var dt = new DataTable("Employee");
            dt.Columns.Add(SysFields.RowId, typeof(Guid));
            dt.Columns.Add("sys_name", typeof(string));

            var row = dt.NewRow();
            row[SysFields.RowId] = Guid.NewGuid();
            row["sys_name"] = "Alice";

            switch (targetState)
            {
                case DataRowState.Detached:
                    // row not added; remains Detached.
                    break;
                case DataRowState.Added:
                    dt.Rows.Add(row);
                    break;
                case DataRowState.Unchanged:
                    dt.Rows.Add(row);
                    dt.AcceptChanges();
                    break;
                case DataRowState.Deleted:
                    dt.Rows.Add(row);
                    dt.AcceptChanges();
                    row.Delete();
                    break;
            }

            Assert.Equal(targetState, row.RowState);
            Assert.Throws<InvalidOperationException>(() => builder.Build("Employee", row));
        }

        [Fact]
        [DisplayName("Build 缺少 sys_rowid 欄位應擲 InvalidOperationException")]
        public void Build_MissingPrimaryKeyColumn_Throws()
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var dt = new DataTable("Employee");
            dt.Columns.Add("sys_name", typeof(string));
            var row = dt.NewRow();
            row["sys_name"] = "old";
            dt.Rows.Add(row);
            dt.AcceptChanges();
            row["sys_name"] = "new";

            Assert.Throws<InvalidOperationException>(() => builder.Build("Employee", row));
        }

        [Fact]
        [DisplayName("Build 無欄位變更應擲 InvalidOperationException")]
        public void Build_NoColumnChanges_Throws()
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var (_, row, _) = NewModifiedRow();
            // Set a column to its current value to force Modified state without real changes.
            row.SetModified();

            Assert.Equal(DataRowState.Modified, row.RowState);
            Assert.Throws<InvalidOperationException>(() => builder.Build("Employee", row));
        }

        [Fact]
        [DisplayName("Build 應只包含實際變更的欄位 (SQL Server)")]
        public void Build_SqlServer_OnlyChangedColumnsInSet()
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var (_, row, rowId) = NewModifiedRow();
            row["sys_name"] = "Bob";

            var spec = builder.Build("Employee", row);

            Assert.Equal(DbCommandKind.NonQuery, spec.Kind);
            Assert.Equal(
                "UPDATE [ft_employee] SET [sys_name] = {0} WHERE [sys_rowid] = {1}",
                spec.CommandText);
            Assert.Equal(2, spec.Parameters.Count);
            Assert.Equal("Bob", spec.Parameters[0].Value);
            Assert.Equal(rowId, spec.Parameters[1].Value);
        }

        [Fact]
        [DisplayName("Build 多欄位變更應全部包含並排除 RelationField")]
        public void Build_MultipleColumnsChanged_ExcludesRelationField()
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var (_, row, _) = NewModifiedRow();
            row["sys_id"] = "E002";
            row["sys_name"] = "Bob";
            // RelationField change must NOT appear in SET clause.
            row["ref_dept_name"] = "NewDept";

            var spec = builder.Build("Employee", row);

            Assert.Contains("[sys_id] = {0}", spec.CommandText);
            Assert.Contains("[sys_name] = {1}", spec.CommandText);
            Assert.DoesNotContain("ref_dept_name", spec.CommandText);
            // 2 SET params + 1 WHERE param
            Assert.Equal(3, spec.Parameters.Count);
        }

        [Fact]
        [DisplayName("Build 應產生 PostgreSQL 方言")]
        public void Build_PostgreSql_QuotesWithDoubleQuotes()
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.PostgreSQL);
            var (_, row, _) = NewModifiedRow();
            row["sys_name"] = "Bob";

            var spec = builder.Build("Employee", row);

            Assert.Equal(
                "UPDATE \"ft_employee\" SET \"sys_name\" = {0} WHERE \"sys_rowid\" = {1}",
                spec.CommandText);
        }

        [Fact]
        [DisplayName("Build WHERE 應使用 sys_rowid 的 Original 版本")]
        public void Build_WhereUsesOriginalRowId()
        {
            var builder = new UpdateCommandBuilder(BuildEmployeeSchema(), DatabaseType.SQLServer);
            var (_, row, originalRowId) = NewModifiedRow();
            row["sys_name"] = "Bob";
            // PK reassignment is unusual but the WHERE must still match the original.
            var newRowId = Guid.NewGuid();
            row[SysFields.RowId] = newRowId;

            var spec = builder.Build("Employee", row);

            // Last parameter is the WHERE PK.
            Assert.Equal(originalRowId, spec.Parameters[spec.Parameters.Count - 1].Value);
        }
    }
}
