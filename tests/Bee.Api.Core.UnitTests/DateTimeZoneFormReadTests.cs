using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.Form;
using Bee.Api.Core.Transformers;
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
    /// 表單讀回的時間點欄位，經回應方向換算後必須位移到使用者時區；日期欄位不位移。
    /// </summary>
    /// <remarks>
    /// 直接把 <see cref="FormBusinessObject.GetData"/> 的結果交給換算器，對應同程序（Local）呼叫：
    /// 那條路徑不經序列化，換算器看到的就是 repository 讀回的 <c>DataTable</c> 本身。
    /// SQLite 另外驗兩條 wire：wire 依宣告型別重建欄位，換算器看到的形狀與同程序不同，
    /// 兩者都要對。SQLite 以文字存放日期，是唯一需要 repository 轉換欄位型別的資料庫。
    /// </remarks>
    public class DateTimeZoneFormReadTests : IClassFixture<SharedDbFixture>
    {
        private const string Taipei = "Asia/Taipei";
        private const string InstantColumn = "occurred_at";
        private const string DayColumn = "due_on";

        private static readonly DateTime s_storedUtc = new(2026, 3, 1, 1, 30, 0, DateTimeKind.Unspecified);
        private static readonly DateTime s_expectedTaipei = new(2026, 3, 1, 9, 30, 0, DateTimeKind.Unspecified);
        private static readonly DateTime s_storedDay = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);

        private readonly SharedDbFixture _fx;

        public DateTimeZoneFormReadTests(SharedDbFixture fx) { _fx = fx; }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：表單讀回的時間點欄位，同程序換算到使用者時區應位移")]
        public void UtcToUser_Sqlite_FormInstantColumn_ShiftsToUserZone()
            => Assert.Equal(s_expectedTaipei, InstantAfter(DatabaseType.SQLite, InProcess));

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：表單讀回的時間點欄位，同程序換算到使用者時區應位移")]
        public void UtcToUser_SqlServer_FormInstantColumn_ShiftsToUserZone()
            => Assert.Equal(s_expectedTaipei, InstantAfter(DatabaseType.SQLServer, InProcess));

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：表單讀回的時間點欄位，同程序換算到使用者時區應位移")]
        public void UtcToUser_PostgreSql_FormInstantColumn_ShiftsToUserZone()
            => Assert.Equal(s_expectedTaipei, InstantAfter(DatabaseType.PostgreSQL, InProcess));

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：表單讀回的時間點欄位，同程序換算到使用者時區應位移")]
        public void UtcToUser_MySql_FormInstantColumn_ShiftsToUserZone()
            => Assert.Equal(s_expectedTaipei, InstantAfter(DatabaseType.MySQL, InProcess));

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：表單讀回的時間點欄位，同程序換算到使用者時區應位移")]
        public void UtcToUser_Oracle_FormInstantColumn_ShiftsToUserZone()
            => Assert.Equal(s_expectedTaipei, InstantAfter(DatabaseType.Oracle, InProcess));

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：表單讀回的時間點欄位經 MessagePack wire 後，換算到使用者時區應位移")]
        public void UtcToUser_SqliteAfterMessagePackWire_ShiftsToUserZone()
            => Assert.Equal(s_expectedTaipei, InstantAfter(DatabaseType.SQLite, OverMessagePack));

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：表單讀回的時間點欄位經 JSON wire 後，換算到使用者時區應位移")]
        public void UtcToUser_SqliteAfterJsonWire_ShiftsToUserZone()
            => Assert.Equal(s_expectedTaipei, InstantAfter(DatabaseType.SQLite, OverJson));

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：表單讀回的日期欄位，同程序換算到使用者時區不應位移")]
        public void UtcToUser_Sqlite_FormDateColumn_DoesNotShift()
            => Assert.Equal(s_storedDay, ValueUtilities.CDateTime(ReadAndConvert(DatabaseType.SQLite, InProcess)[DayColumn]));

        private static DataSet InProcess(DataSet dataSet) => dataSet;

        private static DataSet OverMessagePack(DataSet dataSet)
            => MessagePackCodec.Deserialize<GetDataResponse>(
                MessagePackCodec.Serialize(new GetDataResponse { DataSet = dataSet })).DataSet!;

        private static DataSet OverJson(DataSet dataSet)
        {
            var serializer = new JsonPayloadSerializer();
            var bytes = serializer.Serialize(new GetDataResponse { DataSet = dataSet }, typeof(GetDataResponse));
            return ((GetDataResponse)serializer.Deserialize(bytes, typeof(GetDataResponse))!).DataSet!;
        }

        private DateTime? InstantAfter(DatabaseType databaseType, Func<DataSet, DataSet> transport)
            => ValueUtilities.CDateTime(ReadAndConvert(databaseType, transport)[InstantColumn]);

        /// <summary>
        /// Stores a known instant and day, reads the record through the business object, carries it
        /// across <paramref name="transport"/>, and returns the row after the user-zone conversion.
        /// </summary>
        private DataRow ReadAndConvert(DatabaseType databaseType, Func<DataSet, DataSet> transport)
        {
            string progId = TransientForm.NewTableName("tb_tzr_");
            var schema = new FormSchema(progId, "Zone read") { CategoryId = TransientForm.CategoryId };
            var table = schema.Tables!.Add(progId, "Zone read");
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields.Add(InstantColumn, "Occurred At", FieldDbType.DateTime);
            table.Fields.Add(DayColumn, "Due On", FieldDbType.Date);

            var form = new TransientForm(_fx, databaseType, schema);
            form.CreateTables();
            try
            {
                var rowId = Guid.NewGuid();
                form.DbAccess.ExecuteNonQuery(
                    $"INSERT INTO {form.Quote(progId)} ({form.Quote(SysFields.RowId)}, {form.Quote(InstantColumn)}, {form.Quote(DayColumn)}) " +
                    "VALUES ({0}, {1}, {2})",
                    rowId, s_storedUtc, s_storedDay);

                var loaded = new FormBusinessObject(form.CreateContext(), Guid.NewGuid(), progId)
                    .GetData(new GetDataArgs { RowId = rowId }).DataSet!;
                return DateTimeZoneConverter.UtcToUser(transport(loaded), Taipei)!.Tables[progId]!.Rows[0];
            }
            finally
            {
                form.DropTables();
            }
        }
    }
}
