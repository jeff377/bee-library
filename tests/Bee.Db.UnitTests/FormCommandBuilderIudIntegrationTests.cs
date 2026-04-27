using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Db.Sql;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Round-trip integration tests for the FormMap IUD command builders against a live database.
    /// Targets the seeded <c>st_user</c> table created by <see cref="DbGlobalFixture"/> on each
    /// configured database. Uses an in-memory FormSchema mirroring that table so the test does
    /// not depend on a checked-in FormSchema XML.
    /// </summary>
    [Collection("Initialize")]
    public class FormCommandBuilderIudIntegrationTests
    {
        private const string UserTableName = "User";
        private const string UserDbTableName = "st_user";

        private static FormSchema BuildUserSchema()
        {
            var schema = new FormSchema("User", "User Form");
            var table = schema.Tables!.Add(UserTableName, "User");
            table.DbTableName = UserDbTableName;
            table.Fields!.Add(SysFields.No, "Sequence", FieldDbType.AutoIncrement);
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("sys_id", "User Id", 20);
            table.Fields!.AddStringField("sys_name", "User Name", 20);
            table.Fields!.AddStringField("password", "Password", 40);
            table.Fields!.AddStringField("email", "Email", 100);
            table.Fields!.AddStringField("note", "Note", 200);
            return schema;
        }

        private static DataTable NewUserDataTable()
        {
            var dt = new DataTable(UserTableName);
            dt.Columns.Add(SysFields.RowId, typeof(Guid));
            dt.Columns.Add("sys_id", typeof(string));
            dt.Columns.Add("sys_name", typeof(string));
            dt.Columns.Add("password", typeof(string));
            dt.Columns.Add("email", typeof(string));
            dt.Columns.Add("note", typeof(string));
            return dt;
        }

        private static void RunRoundTrip(DatabaseType databaseType)
        {
            var schema = BuildUserSchema();
            var databaseId = TestDbConventions.GetDatabaseId(databaseType);
            var dbAccess = new DbAccess(databaseId);

            var rowId = Guid.NewGuid();
            var sysId = "ut_" + Guid.NewGuid().ToString("N").Substring(0, 12);

            try
            {
                // INSERT
                var insertDt = NewUserDataTable();
                var insertRow = insertDt.NewRow();
                insertRow[SysFields.RowId] = rowId;
                insertRow["sys_id"] = sysId;
                insertRow["sys_name"] = "Alice";
                insertRow["password"] = "pw";
                insertRow["email"] = "a@example.com";
                insertRow["note"] = "init";

                var insertSpec = new InsertCommandBuilder(schema, databaseType).Build(UserTableName, insertRow);
                Assert.Equal(1, dbAccess.Execute(insertSpec).RowsAffected);

                // SELECT verifies the row exists.
                var selectSpec = new SelectCommandBuilder(schema, databaseType)
                    .Build(UserTableName, "sys_rowid,sys_name,note", FilterCondition.Equal("sys_rowid", rowId));
                var afterInsert = dbAccess.Execute(selectSpec).Table;
                Assert.NotNull(afterInsert);
                Assert.Single(afterInsert!.Rows);
                Assert.Equal("Alice", afterInsert.Rows[0]["sys_name"]);
                Assert.Equal("init", afterInsert.Rows[0]["note"]);

                // UPDATE — change sys_name and note.
                var updateDt = NewUserDataTable();
                var updateRow = updateDt.NewRow();
                updateRow[SysFields.RowId] = rowId;
                updateRow["sys_id"] = sysId;
                updateRow["sys_name"] = "Alice";
                updateRow["password"] = "pw";
                updateRow["email"] = "a@example.com";
                updateRow["note"] = "init";
                updateDt.Rows.Add(updateRow);
                updateDt.AcceptChanges();
                updateRow["sys_name"] = "Alice2";
                updateRow["note"] = "updated";

                var updateSpec = new UpdateCommandBuilder(schema, databaseType).Build(UserTableName, updateRow);
                Assert.Equal(1, dbAccess.Execute(updateSpec).RowsAffected);

                var afterUpdate = dbAccess.Execute(selectSpec).Table;
                Assert.NotNull(afterUpdate);
                Assert.Single(afterUpdate!.Rows);
                Assert.Equal("Alice2", afterUpdate.Rows[0]["sys_name"]);
                Assert.Equal("updated", afterUpdate.Rows[0]["note"]);
            }
            finally
            {
                // DELETE — always run as cleanup, even if assertions failed.
                var deleteSpec = new DeleteCommandBuilder(schema, databaseType)
                    .Build(UserTableName, FilterCondition.Equal(SysFields.RowId, rowId));
                dbAccess.Execute(deleteSpec);
            }

            // Verify deletion.
            var verifySpec = new SelectCommandBuilder(schema, databaseType)
                .Build(UserTableName, "sys_rowid", FilterCondition.Equal("sys_rowid", rowId));
            var afterDelete = dbAccess.Execute(verifySpec).Table;
            Assert.NotNull(afterDelete);
            Assert.Empty(afterDelete!.Rows);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("FormMap IUD on SQL Server: INSERT → SELECT → UPDATE → SELECT → DELETE 完整 round-trip")]
        public void RoundTrip_SqlServer()
        {
            RunRoundTrip(DatabaseType.SQLServer);
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("FormMap IUD on PostgreSQL: INSERT → SELECT → UPDATE → SELECT → DELETE 完整 round-trip")]
        public void RoundTrip_PostgreSql()
        {
            RunRoundTrip(DatabaseType.PostgreSQL);
        }
    }
}
