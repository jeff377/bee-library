using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 驗證 <see cref="DbCommandSpec.DateColumns"/>（路徑二：呼叫端自寫 SQL）在 DbAccess 讀取路徑上
    /// 正確標記日曆日欄位。ADO.NET 一律把 date 欄位回報為 System.DateTime，定義層的
    /// Date / DateTime 之分因而在 SQL 讀取路徑上消失；此宣告是呼叫端把它補回來的方式。
    /// </summary>
    public class DateColumnMarkerTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public DateColumnMarkerTests(SharedDbFixture fx) { _fx = fx; }

        private const string DropSql = "DROP TABLE IF EXISTS date_marker_test;";
        private const string CreateSql =
            "CREATE TABLE date_marker_test (order_date DATE NOT NULL, created_at DATETIME NOT NULL);";
        private const string InsertSql =
            "INSERT INTO date_marker_test (order_date, created_at) VALUES ({0}, {1});";

        private DbAccess PrepareTable()
        {
            var dbAccess = _fx.NewDbAccess("common_sqlite");
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, DropSql));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, CreateSql));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, InsertSql,
                new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Unspecified),
                new DateTime(2026, 7, 25, 13, 45, 0, DateTimeKind.Unspecified)));
            return dbAccess;
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("未宣告 DateColumns 時日期欄位不帶標記（現況行為不變）")]
        public void ExecuteDataTable_WithoutDeclaration_LeavesColumnsUnmarked()
        {
            var dbAccess = PrepareTable();
            try
            {
                var spec = new DbCommandSpec(DbCommandKind.DataTable,
                    "SELECT order_date, created_at FROM date_marker_test");
                var table = dbAccess.Execute(spec).Table!;

                Assert.Null(table.Columns["order_date"]!.GetDeclaredFieldDbType());
                Assert.Null(table.Columns["created_at"]!.GetDeclaredFieldDbType());
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, DropSql));
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("宣告 DateColumns 後只有指定欄位被標記為 Date")]
        public void ExecuteDataTable_WithDeclaration_MarksOnlyDeclaredColumns()
        {
            var dbAccess = PrepareTable();
            try
            {
                var spec = new DbCommandSpec(DbCommandKind.DataTable,
                    "SELECT order_date, created_at FROM date_marker_test");
                spec.DateColumns.Add("order_date");
                var table = dbAccess.Execute(spec).Table!;

                Assert.Equal(FieldDbType.Date, table.Columns["order_date"]!.ResolveFieldDbType());
                Assert.Null(table.Columns["created_at"]!.GetDeclaredFieldDbType());
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, DropSql));
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("DateColumns 宣告不存在的欄名應擲例外，不可靜默略過")]
        public void ExecuteDataTable_UnknownDeclaredColumn_Throws()
        {
            var dbAccess = PrepareTable();
            try
            {
                var spec = new DbCommandSpec(DbCommandKind.DataTable,
                    "SELECT order_date FROM date_marker_test");
                spec.DateColumns.Add("oder_date");

                Assert.Throws<ArgumentException>(() => dbAccess.Execute(spec));
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, DropSql));
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("DateColumns 用於非 DataTable 的 DbCommandKind 應擲例外")]
        public void Execute_DateColumnsOnNonTableKind_Throws()
        {
            var dbAccess = PrepareTable();
            try
            {
                var spec = new DbCommandSpec(DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM date_marker_test");
                spec.DateColumns.Add("order_date");

                // 宣告了卻無聲無效，正是此機制要消除的失敗模式，故擲例外而非忽略。
                Assert.Throws<InvalidOperationException>(() => dbAccess.Execute(spec));
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, DropSql));
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("非同步讀取路徑同樣套用 DateColumns 宣告")]
        public async Task ExecuteAsync_WithDeclaration_MarksDeclaredColumns()
        {
            var dbAccess = PrepareTable();
            try
            {
                var spec = new DbCommandSpec(DbCommandKind.DataTable,
                    "SELECT order_date, created_at FROM date_marker_test");
                spec.DateColumns.Add("order_date");
                var result = await dbAccess.ExecuteAsync(spec);

                Assert.Equal(FieldDbType.Date, result.Table!.Columns["order_date"]!.ResolveFieldDbType());
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, DropSql));
            }
        }
    }
}
