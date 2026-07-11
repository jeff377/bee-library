using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests.Dml
{
    /// <summary>
    /// Coverage-focused tests for <see cref="DeleteCommandBuilder"/> guard clauses and
    /// alternate branches. All DB-free (no fixture) so they always run during coverage.
    /// </summary>
    public class DeleteCommandBuilderCoverageTests
    {
        private static FormSchema BuildSchema(string dbTableName)
        {
            var schema = new FormSchema("Employee", "Employee Form");
            var table = schema.Tables!.Add("Employee", "Employee");
            table.DbTableName = dbTableName;
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("sys_id", "Employee Id", 50);
            return schema;
        }

        [Fact]
        [DisplayName("建構子傳入 null formSchema 應擲 ArgumentNullException")]
        public void Constructor_NullFormSchema_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DeleteCommandBuilder(null!, DatabaseType.SQLServer));
        }

        [Fact]
        [DisplayName("Build 空 FilterGroup 產生空 WHERE 應擲 InvalidOperationException")]
        public void Build_EmptyFilterGroup_ThrowsOnEmptyWhere()
        {
            var builder = new DeleteCommandBuilder(BuildSchema("st_employee"), DatabaseType.SQLServer);
            Assert.Throws<InvalidOperationException>(() =>
                builder.Build("Employee", FilterGroup.All()));
        }

        [Fact]
        [DisplayName("Build 資料表 DbTableName 為空時應以 TableName 作為實體表名")]
        public void Build_TableWithoutDbTableName_UsesTableName()
        {
            var builder = new DeleteCommandBuilder(BuildSchema(string.Empty), DatabaseType.SQLServer);
            var spec = builder.Build("Employee", FilterCondition.Equal(SysFields.RowId, Guid.NewGuid()));

            Assert.Equal("DELETE FROM [Employee] WHERE [sys_rowid] = @p0", spec.CommandText);
        }

        [Fact]
        [DisplayName("Build 條件 FieldName 為空時應擲 InvalidOperationException")]
        public void Build_ConditionWithEmptyFieldName_Throws()
        {
            var builder = new DeleteCommandBuilder(BuildSchema("st_employee"), DatabaseType.SQLServer);
            Assert.Throws<InvalidOperationException>(() =>
                builder.Build("Employee", new FilterCondition()));
        }

        [Fact]
        [DisplayName("Build 未知 FilterNodeKind 應走 fall-through 分支原樣傳回節點")]
        public void Build_UnknownFilterNodeKind_HitsFallThroughBranch()
        {
            // A synthetic node whose Kind is neither Condition nor Group drives
            // QuoteAndValidateFields into its final fall-through return (the node is
            // returned unchanged). The downstream WhereBuilder then rejects it, so Build
            // ultimately throws — the fall-through branch has already executed by then.
            var builder = new DeleteCommandBuilder(BuildSchema("st_employee"), DatabaseType.SQLServer);
            var exception = Record.Exception(() => builder.Build("Employee", new UnknownFilterNode()));

            Assert.NotNull(exception);
        }

        private sealed class UnknownFilterNode : FilterNode
        {
            public override FilterNodeKind Kind => (FilterNodeKind)99;
        }
    }
}
