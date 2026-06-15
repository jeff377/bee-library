using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Base.Data;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Verifies <see cref="DbAccess.UpdateDataTables"/> on SQLite, which gains a
    /// <see cref="System.Data.Common.DbDataAdapter"/> through the framework's
    /// <see cref="Bee.Db.Providers.Sqlite.SqliteProviderFactory"/> wrapper (Microsoft.Data.Sqlite
    /// ships none of its own): INSERT/UPDATE/DELETE dispatch by <see cref="DataRowState"/> inside one
    /// transaction, and a Modified row whose values are unchanged re-writes the same values without
    /// raising an "empty UPDATE" error.
    /// </summary>
    public class UpdateDataTablesManualApplyTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public UpdateDataTablesManualApplyTests(SharedDbFixture fx) { _fx = fx; }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite（SqliteDataAdapter）：UpdateDataTables 於同一交易處理 Added / Modified(no-op) / Deleted 不應拋錯")]
        public void Sqlite_ManualApply_HandlesAllRowStatesInOneTransaction()
        {
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(DatabaseType.SQLite));
            string t = "tb_udt_" + Guid.NewGuid().ToString("N")[..8];
            dbAccess.ExecuteNonQuery($"CREATE TABLE \"{t}\" (\"sys_rowid\" TEXT PRIMARY KEY, \"name\" TEXT)");
            try
            {
                string keep = Guid.NewGuid().ToString("N");
                string drop = Guid.NewGuid().ToString("N");
                string add = Guid.NewGuid().ToString("N");
                dbAccess.ExecuteNonQuery($"INSERT INTO \"{t}\" (\"sys_rowid\",\"name\") VALUES ({{0}},{{1}})", keep, "keep");
                dbAccess.ExecuteNonQuery($"INSERT INTO \"{t}\" (\"sys_rowid\",\"name\") VALUES ({{0}},{{1}})", drop, "drop");

                var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable,
                    $"SELECT \"sys_rowid\",\"name\" FROM \"{t}\"")).Table!;
                table.AcceptChanges();

                foreach (DataRow r in table.Rows)
                {
                    var id = Convert.ToString(r["sys_rowid"], CultureInfo.InvariantCulture);
                    if (id == keep) { r.SetModified(); }   // Modified with no actual value change (no-op)
                    else if (id == drop) { r.Delete(); }   // Deleted
                }
                var addedRow = table.NewRow();
                addedRow["sys_rowid"] = add;
                addedRow["name"] = "added";
                table.Rows.Add(addedRow);                  // Added

                var schema = new TableSchema { TableName = t };
                schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.String, 50);
                schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
                var spec = new TableSchemaCommandBuilder(dbAccess.DatabaseType, schema).BuildUpdateSpec(table);

                var exception = Record.Exception(() => dbAccess.UpdateDataTables(new[] { spec }));
                Assert.Null(exception);

                // 'drop' deleted, 'keep' + 'added' remain.
                int total = Convert.ToInt32(
                    dbAccess.ExecuteScalar($"SELECT COUNT(*) FROM \"{t}\""), CultureInfo.InvariantCulture);
                Assert.Equal(2, total);
                int dropped = Convert.ToInt32(
                    dbAccess.ExecuteScalar($"SELECT COUNT(*) FROM \"{t}\" WHERE \"sys_rowid\"={{0}}", drop),
                    CultureInfo.InvariantCulture);
                Assert.Equal(0, dropped);
            }
            finally
            {
                dbAccess.ExecuteNonQuery($"DROP TABLE IF EXISTS \"{t}\"");
            }
        }
    }
}
