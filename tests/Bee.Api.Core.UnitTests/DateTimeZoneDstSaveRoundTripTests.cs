using System.ComponentModel;
using System.Globalization;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Data;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// DST 回撥重疊時段的「讀進 → 改別的欄位 → 存回」：時間點欄位的值必須不變。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 美東 2026-11-01 的 05:30Z 與 06:30Z 經 <see cref="DateTimeZoneConverter.UtcToUser(System.Data.DataSet?, string)"/>
    /// 都成為牆上時間 01:30。這份資訊在回應轉入使用者時區的那一刻就消失了，任何從牆上時間換回 UTC 的做法
    /// 都只能確定性地解析為其中一個。
    /// </para>
    /// <para>
    /// 修正在伺服端：<see cref="FormBusinessObject.Save"/> 不採用傳入的 <c>DateTime</c>，修改列的時間點欄位
    /// 以資料庫讀回的值覆蓋。所以這裡刻意把使用者時區的 <c>DataSet</c> 原樣交給 Save，不經任何請求方向的換算。
    /// </para>
    /// <para>
    /// 整條鏈都用真的元件：回應方向的換算、Save 的正規化、<c>DataFormRepository</c> 的 adapter 寫入，
    /// 最後直接以 SQL 讀回資料庫裡的值。缺陷的症狀只在資料庫裡看得到，這一組釘住的是
    /// 「使用者沒碰的值，存回後資料庫裡仍是原值」。
    /// </para>
    /// </remarks>
    public class DateTimeZoneDstSaveRoundTripTests : IClassFixture<SharedDbFixture>
    {
        private const string NewYork = "America/New_York";
        private const string NameColumn = "name";
        private const string InstantColumn = "occurred_at";

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
            string progId = TransientForm.NewTableName("tb_dst_");
            var schema = new FormSchema(progId, "DST round trip") { CategoryId = TransientForm.CategoryId };
            var table = schema.Tables!.Add(progId, "DST");
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField(NameColumn, "Name", 50);
            table.Fields.Add(InstantColumn, "Occurred At", FieldDbType.DateTime);

            var form = new TransientForm(_fx, databaseType, schema);
            form.CreateTables();
            try
            {
                var rowId = Guid.NewGuid();
                form.DbAccess.ExecuteNonQuery(
                    $"INSERT INTO {form.Quote(progId)} ({form.Quote(SysFields.RowId)}, {form.Quote(NameColumn)}, {form.Quote(InstantColumn)}) " +
                    "VALUES ({0}, {1}, {2})",
                    rowId, "before", s_firstOccurrenceUtc);

                // 伺服端 GetData 交給 Connector 的形狀是 UTC；用戶端手上的是轉進使用者時區之後的那一份。
                var loaded = new FormBusinessObject(form.CreateContext(), Guid.NewGuid(), progId)
                    .GetData(new GetDataArgs { RowId = rowId }).DataSet!;
                var onScreen = DateTimeZoneConverter.UtcToUser(loaded, NewYork)!;
                onScreen.Tables[progId]!.Rows[0][NameColumn] = "after";

                new FormBusinessObject(form.CreateContext(), Guid.NewGuid(), progId)
                    .Save(new SaveArgs { DataSet = onScreen });

                Assert.Equal("after", Convert.ToString(
                    form.DbAccess.ExecuteScalar(SelectSql(form, NameColumn), rowId), CultureInfo.InvariantCulture));
                Assert.Equal(s_firstOccurrenceUtc,
                    ValueUtilities.CDateTime(form.DbAccess.ExecuteScalar(SelectSql(form, InstantColumn), rowId)));
            }
            finally
            {
                form.DropTables();
            }
        }

        private static string SelectSql(TransientForm form, string column)
            => $"SELECT {form.Quote(column)} FROM {form.Quote(form.Schema.ProgId)} WHERE {form.Quote(SysFields.RowId)}={{0}}";
    }
}
