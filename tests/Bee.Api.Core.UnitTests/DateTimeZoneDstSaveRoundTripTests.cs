using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Api.Core.JsonRpc;
using Bee.Base.Data;
using Bee.Db;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// DST 回撥重疊時段的「讀進 → 改別的欄位 → 存回」：時間點欄位的值必須不變。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 美東 2026-11-01 的 05:30Z 與 06:30Z 經 <see cref="DateTimeZoneConverter.UtcToUser(DataTable?, string)"/>
    /// 都成為牆上時間 01:30，<see cref="DateTimeZoneConverter.UserToUtc(DataTable?, string)"/> 再一律解析為
    /// 標準時間的 06:30Z。Original 與 Current 走的是同一條換算，所以兩個版本都變成 06:30Z。
    /// </para>
    /// <para>
    /// 修正在 Connector 端：回應方向記住重疊時段儲存格原本的 UTC 值，請求方向在牆上時間未變時送回它。
    /// 寫入端刻意不做任何處理，UPDATE 照舊以 Current 寫回所有欄位。
    /// </para>
    /// <para>
    /// 整條鏈都用真的元件：Connector 端的換算、<see cref="TableSchemaCommandBuilder"/> 產生的 UPDATE、
    /// <see cref="DbAccess.UpdateDataTables"/> 的 adapter 寫入，最後直接以 SQL 讀回資料庫裡的值。
    /// 缺陷的症狀只在資料庫裡看得到，這一組釘住的是「使用者沒碰的值，存回後資料庫裡仍是原值」。
    /// </para>
    /// </remarks>
    public class DateTimeZoneDstSaveRoundTripTests : IClassFixture<SharedDbFixture>
    {
        private const string NewYork = "America/New_York";

        // 較早那一個 UTC 值：01:30 EDT。06:30Z 是同一個牆上時間的 01:30 EST。
        private static readonly DateTime s_firstOccurrenceUtc = new(2026, 11, 1, 5, 30, 0, DateTimeKind.Unspecified);

        private readonly SharedDbFixture _fx;

        public DateTimeZoneDstSaveRoundTripTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("前提：05:30Z 與 06:30Z 在 America/New_York 是同一個牆上時間")]
        public void Precondition_BothUtcValuesShareOneWallClockTime()
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(NewYork);
            var first = TimeZoneInfo.ConvertTimeFromUtc(s_firstOccurrenceUtc, zone);
            var second = TimeZoneInfo.ConvertTimeFromUtc(s_firstOccurrenceUtc.AddHours(1), zone);

            Assert.Equal(first, second);
            Assert.True(zone.IsAmbiguousTime(first));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：DST 重疊時段的時間點欄位，改同列別的欄位存回後值應不變")]
        public void Save_Sqlite_AmbiguousInstantUntouched_KeepsStoredValue()
            => RunUntouchedInstantKeepsValue(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：DST 重疊時段的時間點欄位，改同列別的欄位存回後值應不變")]
        public void Save_SqlServer_AmbiguousInstantUntouched_KeepsStoredValue()
            => RunUntouchedInstantKeepsValue(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：DST 重疊時段的時間點欄位，改同列別的欄位存回後值應不變")]
        public void Save_PostgreSql_AmbiguousInstantUntouched_KeepsStoredValue()
            => RunUntouchedInstantKeepsValue(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：DST 重疊時段的時間點欄位，改同列別的欄位存回後值應不變")]
        public void Save_MySql_AmbiguousInstantUntouched_KeepsStoredValue()
            => RunUntouchedInstantKeepsValue(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：DST 重疊時段的時間點欄位，改同列別的欄位存回後值應不變")]
        public void Save_Oracle_AmbiguousInstantUntouched_KeepsStoredValue()
            => RunUntouchedInstantKeepsValue(DatabaseType.Oracle);

        private void RunUntouchedInstantKeepsValue(DatabaseType databaseType)
        {
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(databaseType));
            var table = new DstTable(databaseType);

            dbAccess.ExecuteNonQuery(table.CreateSql());
            try
            {
                string rowId = Guid.NewGuid().ToString("N");
                dbAccess.ExecuteNonQuery(table.InsertSql(), rowId, "before", s_firstOccurrenceUtc);

                // 伺服端 GetData 交給 Connector 的形狀：UTC、Unchanged。
                var loaded = table.NewDataTable();
                loaded.Rows.Add(rowId, "before", s_firstOccurrenceUtc);
                loaded.AcceptChanges();

                var onScreen = DateTimeZoneConverter.UtcToUser(loaded, NewYork)!;
                onScreen.Rows[0]["name"] = "after";
                var toServer = DateTimeZoneConverter.UserToUtc(onScreen, NewYork)!;

                dbAccess.UpdateDataTables(
                    [new TableSchemaCommandBuilder(databaseType, table.Schema()).BuildUpdateSpec(toServer)]);

                Assert.Equal("after", Convert.ToString(
                    dbAccess.ExecuteScalar(table.SelectSql("name"), rowId), CultureInfo.InvariantCulture));
                Assert.Equal(s_firstOccurrenceUtc,
                    ReadInstant(dbAccess.ExecuteScalar(table.SelectSql(DstTable.InstantColumn), rowId)));
            }
            finally
            {
                dbAccess.ExecuteNonQuery(table.DropSql());
            }
        }

        /// <summary>
        /// 把資料庫讀回的時間值還原成 <see cref="DateTime"/>；SQLite 以 TEXT 儲存，讀回是字串。
        /// </summary>
        private static DateTime ReadInstant(object? value) => value switch
        {
            DateTime instant => DateTime.SpecifyKind(instant, DateTimeKind.Unspecified),
            string text => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None),
            _ => throw new InvalidOperationException($"Unexpected instant value '{value}'."),
        };

        /// <summary>
        /// 一張測試用的表：主鍵、一個文字欄、一個時間點欄。
        /// </summary>
        /// <remarks>
        /// 刻意手寫建表而不繞道 schema 引擎：這裡測的是寫入路徑，建表只是前置。
        /// </remarks>
        private sealed class DstTable
        {
            public const string InstantColumn = "occurred_at";

            private readonly DatabaseType _databaseType;

            public DstTable(DatabaseType databaseType)
            {
                _databaseType = databaseType;
                // Oracle 的識別碼長度限制較嚴，表名壓在 30 字元內。
                Name = "tb_dst_" + Guid.NewGuid().ToString("N")[..8];
            }

            public string Name { get; }

            private string Q(string identifier) => _databaseType.QuoteIdentifier(identifier);

            public TableSchema Schema()
            {
                var schema = new TableSchema { TableName = Name };
                schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.String, 50);
                schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
                schema.Fields!.Add(InstantColumn, "Occurred At", FieldDbType.DateTime);
                return schema;
            }

            public DataTable NewDataTable()
            {
                var table = new DataTable(Name);
                table.AddColumn(SysFields.RowId, FieldDbType.String);
                table.AddColumn("name", FieldDbType.String);
                table.AddColumn(InstantColumn, FieldDbType.DateTime);
                return table;
            }

            public string CreateSql()
            {
                var (key, text, instant) = _databaseType switch
                {
                    DatabaseType.SQLServer => ("NVARCHAR(50) NOT NULL PRIMARY KEY", "NVARCHAR(50) NULL", "DATETIME2 NULL"),
                    DatabaseType.MySQL => ("VARCHAR(50) NOT NULL PRIMARY KEY", "VARCHAR(50) NULL", "DATETIME(6) NULL"),
                    DatabaseType.Oracle => ("VARCHAR2(50) NOT NULL PRIMARY KEY", "VARCHAR2(50)", "TIMESTAMP"),
                    DatabaseType.PostgreSQL => ("VARCHAR(50) NOT NULL PRIMARY KEY", "VARCHAR(50) NULL", "TIMESTAMP NULL"),
                    DatabaseType.SQLite => ("TEXT PRIMARY KEY", "TEXT", "TEXT"),
                    _ => throw new ArgumentOutOfRangeException(nameof(_databaseType), _databaseType, "未涵蓋的資料庫類型。"),
                };
                return $"CREATE TABLE {Q(Name)} ({Q(SysFields.RowId)} {key}, {Q("name")} {text}, {Q(InstantColumn)} {instant})";
            }

            public string InsertSql()
                => $"INSERT INTO {Q(Name)} ({Q(SysFields.RowId)}, {Q("name")}, {Q(InstantColumn)}) VALUES ({{0}}, {{1}}, {{2}})";

            public string SelectSql(string column)
                => $"SELECT {Q(column)} FROM {Q(Name)} WHERE {Q(SysFields.RowId)}={{0}}";

            public string DropSql() => $"DROP TABLE {Q(Name)}";
        }
    }
}
