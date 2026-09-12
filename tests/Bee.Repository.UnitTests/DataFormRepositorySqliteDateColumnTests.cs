using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// SQLite 以文字存放日期，<c>DataFormRepository</c> 讀回時必須依 FormSchema 宣告換成 <see cref="DateTime"/> 欄。
    /// </summary>
    /// <remarks>
    /// 其他資料庫的驅動本來就交回 <see cref="DateTime"/>，這裡驗的是只有 SQLite 會走到的轉換。
    /// </remarks>
    public class DataFormRepositorySqliteDateColumnTests : IClassFixture<SharedDbFixture>
    {
        private const string InstantColumn = "occurred_at";
        private const string DayColumn = "due_on";

        private static readonly DateTime s_instant = new(2026, 3, 1, 1, 30, 15, DateTimeKind.Unspecified);
        private static readonly DateTime s_day = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);

        private readonly SharedDbFixture _fx;

        public DataFormRepositorySqliteDateColumnTests(SharedDbFixture fx) { _fx = fx; }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetData 讀回的 DateTime 欄應為 DateTime 型別且值不變")]
        public void GetData_SqliteDateTimeColumn_ReadsAsDateTime()
            => WithForm((form, progId) =>
            {
                var rowId = Insert(form, progId, s_instant, s_day);
                var table = form.Repository.GetData(rowId)!.Tables[progId]!;

                Assert.Equal(typeof(DateTime), table.Columns[InstantColumn]!.DataType);
                Assert.Equal(s_instant, table.Rows[0][InstantColumn]);
            });

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetData 讀回的 Date 欄應為 DateTime 型別並保留 Date 宣告")]
        public void GetData_SqliteDateColumn_ReadsAsDateTimeMarkedDate()
            => WithForm((form, progId) =>
            {
                var rowId = Insert(form, progId, s_instant, s_day);
                var column = form.Repository.GetData(rowId)!.Tables[progId]!.Columns[DayColumn]!;

                Assert.Equal(typeof(DateTime), column.DataType);
                Assert.Equal(FieldDbType.Date, column.GetDeclaredFieldDbType());
            });

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：沒有資料列的 GetList 結果，DateTime 欄仍應為 DateTime 型別")]
        public void GetList_SqliteEmptyResult_DateTimeColumnIsDateTime()
            => WithForm((form, _) =>
            {
                var table = form.Repository.GetList(string.Empty, null, null).Table!;

                Assert.Empty(table.Rows);
                Assert.Equal(typeof(DateTime), table.Columns[InstantColumn]!.DataType);
            });

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：DateTime 欄存著無法剖析的文字時，GetData 應擲 InvalidOperationException")]
        public void GetData_SqliteTextThatIsNotADate_Throws()
            => WithForm((form, progId) =>
            {
                var rowId = Insert(form, progId, "not-a-date", s_day);

                Assert.Throws<InvalidOperationException>(() => form.Repository.GetData(rowId));
            });

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：DateTime 欄存著空字串時，GetData 應讀成 DBNull")]
        public void GetData_SqliteEmptyText_ReadsAsDbNull()
            => WithForm((form, progId) =>
            {
                var rowId = Insert(form, progId, string.Empty, s_day);
                var table = form.Repository.GetData(rowId)!.Tables[progId]!;

                Assert.Equal(DBNull.Value, table.Rows[0][InstantColumn]);
            });

        private void WithForm(Action<TransientForm, string> body)
        {
            string progId = TransientForm.NewTableName("tb_sqd_");
            var schema = new FormSchema(progId, "SQLite dates") { CategoryId = TransientForm.CategoryId };
            var table = schema.Tables!.Add(progId, "SQLite dates");
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields.Add(InstantColumn, "Occurred At", FieldDbType.DateTime);
            table.Fields.Add(DayColumn, "Due On", FieldDbType.Date);

            var form = new TransientForm(_fx, DatabaseType.SQLite, schema);
            form.CreateTables();
            try
            {
                body(form, progId);
            }
            finally
            {
                form.DropTables();
            }
        }

        private static Guid Insert(TransientForm form, string progId, object instant, object day)
        {
            var rowId = Guid.NewGuid();
            form.DbAccess.ExecuteNonQuery(
                $"INSERT INTO {form.Quote(progId)} ({form.Quote(SysFields.RowId)}, {form.Quote(InstantColumn)}, {form.Quote(DayColumn)}) " +
                "VALUES ({0}, {1}, {2})",
                rowId, instant, day);
            return rowId;
        }
    }
}
