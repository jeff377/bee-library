using System.ComponentModel;
using System.Data;
using Bee.Base;
using Bee.Base.Data;
using Bee.Base.Exceptions;
using Bee.Business.AuditLog;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// <see cref="FormBusinessObject.Save"/> 不採用呼叫端傳入的 <c>DateTime</c>：新增列由伺服端補值、
    /// 修改與刪除列以資料庫讀回的值覆蓋兩個版本，系統時間戳記由框架戳記。
    /// </summary>
    /// <remarks>
    /// 每個測試都直接呼叫 <see cref="FormBusinessObject.Save"/>，不經 Connector，
    /// 所以同時驗證「伺服端 BO 之間的呼叫同樣不採用」。
    /// 傳入的值一律是 <see cref="s_clientValue"/>，任何一個欄位落庫成它就代表正規化漏了。
    /// </remarks>
    public class FormBusinessObjectDateTimeNormalizationTests : IClassFixture<SharedDbFixture>
    {
        private const string Note = "note";
        private const string EventTime = "event_time";
        private const string DefaultedTime = "defaulted_time";
        private const string ComputedTime = "computed_time";
        private const string LineTime = "line_time";

        private static readonly DateTime s_clientValue = new(2001, 2, 3, 4, 5, 6, DateTimeKind.Unspecified);
        private static readonly DateTime s_storedInsert = new(2020, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        private static readonly DateTime s_storedUpdate = new(2020, 1, 3, 3, 4, 5, DateTimeKind.Unspecified);
        private static readonly DateTime s_storedEvent = new(2020, 1, 4, 3, 4, 5, DateTimeKind.Unspecified);
        private static readonly DateTime s_storedLine = new(2020, 1, 5, 3, 4, 5, DateTimeKind.Unspecified);
        private static readonly DateTime s_storedDeletedLine = new(2020, 1, 6, 3, 4, 5, DateTimeKind.Unspecified);

        private static readonly string[] s_masterServerTimes = [SysFields.InsertTime, SysFields.UpdateTime, EventTime, DefaultedTime, ComputedTime];

        private readonly SharedDbFixture _fx;

        public FormBusinessObjectDateTimeNormalizationTests(SharedDbFixture fx) { _fx = fx; }

        #region 新增列

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：新增列傳入的 DateTime 不落庫，系統戳記、NOT NULL 欄與運算式欄都由伺服端寫入")]
        public void Save_Sqlite_AddedRow_UsesServerValues() => RunAddedRowUsesServerValues(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：新增列傳入的 DateTime 不落庫，系統戳記、NOT NULL 欄與運算式欄都由伺服端寫入")]
        public void Save_SqlServer_AddedRow_UsesServerValues() => RunAddedRowUsesServerValues(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：新增列傳入的 DateTime 不落庫，系統戳記、NOT NULL 欄與運算式欄都由伺服端寫入")]
        public void Save_PostgreSql_AddedRow_UsesServerValues() => RunAddedRowUsesServerValues(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：新增列傳入的 DateTime 不落庫，系統戳記、NOT NULL 欄與運算式欄都由伺服端寫入")]
        public void Save_MySql_AddedRow_UsesServerValues() => RunAddedRowUsesServerValues(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：新增列傳入的 DateTime 不落庫，系統戳記、NOT NULL 欄與運算式欄都由伺服端寫入")]
        public void Save_Oracle_AddedRow_UsesServerValues() => RunAddedRowUsesServerValues(DatabaseType.Oracle);

        private void RunAddedRowUsesServerValues(DatabaseType databaseType)
        {
            var form = NewForm(databaseType);
            form.CreateTables();
            try
            {
                var rowId = Guid.NewGuid();
                var lineId = Guid.NewGuid();
                var dataSet = NewRecord(form, rowId, lineId);

                var before = DateTime.UtcNow;
                new FormBusinessObject(form.CreateContext(), Guid.NewGuid(), form.Schema.ProgId)
                    .Save(new SaveArgs { DataSet = dataSet });
                var after = DateTime.UtcNow;

                foreach (var column in s_masterServerTimes)
                {
                    AssertServerReading(ReadInstant(form, form.Schema.ProgId, column, rowId), before, after, column);
                }
                AssertServerReading(ReadInstant(form, DetailName(form), LineTime, lineId), before, after, LineTime);
            }
            finally
            {
                form.DropTables();
            }
        }

        #endregion

        #region 修改與刪除列

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：修改列保留資料庫的 DateTime、sys_update_time 前進、非時間欄修改完整保留，稽核記的是資料庫值")]
        public void Save_Sqlite_StoredRows_KeepStoredValues() => RunStoredRowsKeepStoredValues(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：修改列保留資料庫的 DateTime、sys_update_time 前進、非時間欄修改完整保留，稽核記的是資料庫值")]
        public void Save_SqlServer_StoredRows_KeepStoredValues() => RunStoredRowsKeepStoredValues(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：修改列保留資料庫的 DateTime、sys_update_time 前進、非時間欄修改完整保留，稽核記的是資料庫值")]
        public void Save_PostgreSql_StoredRows_KeepStoredValues() => RunStoredRowsKeepStoredValues(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：修改列保留資料庫的 DateTime、sys_update_time 前進、非時間欄修改完整保留，稽核記的是資料庫值")]
        public void Save_MySql_StoredRows_KeepStoredValues() => RunStoredRowsKeepStoredValues(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：修改列保留資料庫的 DateTime、sys_update_time 前進、非時間欄修改完整保留，稽核記的是資料庫值")]
        public void Save_Oracle_StoredRows_KeepStoredValues() => RunStoredRowsKeepStoredValues(DatabaseType.Oracle);

        private void RunStoredRowsKeepStoredValues(DatabaseType databaseType)
        {
            var form = NewForm(databaseType);
            form.CreateTables();
            try
            {
                var rowId = Guid.NewGuid();
                var lineId = Guid.NewGuid();
                var deletedLineId = Guid.NewGuid();
                SeedStoredRecord(form, rowId, lineId, deletedLineId);

                var dataSet = new FormBusinessObject(form.CreateContext(), Guid.NewGuid(), form.Schema.ProgId)
                    .GetData(new GetDataArgs { RowId = rowId }).DataSet!;

                var master = dataSet.Tables[form.Schema.ProgId]!.Rows[0];
                master[Note] = "changed";
                master[SysFields.InsertTime] = s_clientValue;
                master[EventTime] = s_clientValue;

                var lines = dataSet.Tables[DetailName(form)]!;
                var line = FindRow(lines, lineId);
                line[Note] = "changed line";
                line[LineTime] = s_clientValue;

                // 讓 Original 也變成用戶端的值再刪除，模擬 Connector 把回應轉進使用者時區之後的那一列。
                var deletedLine = FindRow(lines, deletedLineId);
                deletedLine[LineTime] = s_clientValue;
                deletedLine.AcceptChanges();
                deletedLine.Delete();

                var writer = new CapturingAuditLogWriter();
                var before = DateTime.UtcNow;
                new FormBusinessObject(form.CreateContext(AuditOverrides(writer)), Guid.NewGuid(), form.Schema.ProgId)
                    .Save(new SaveArgs { DataSet = dataSet });
                var after = DateTime.UtcNow;

                Assert.Equal("changed", ReadText(form, form.Schema.ProgId, Note, rowId));
                Assert.Equal("changed line", ReadText(form, DetailName(form), Note, lineId));
                Assert.Equal(s_storedInsert, ReadInstant(form, form.Schema.ProgId, SysFields.InsertTime, rowId));
                Assert.Equal(s_storedEvent, ReadInstant(form, form.Schema.ProgId, EventTime, rowId));
                Assert.Equal(s_storedLine, ReadInstant(form, DetailName(form), LineTime, lineId));
                AssertServerReading(ReadInstant(form, form.Schema.ProgId, SysFields.UpdateTime, rowId), before, after, SysFields.UpdateTime);
                Assert.Null(form.DbAccess.ExecuteScalar(SelectSql(form, DetailName(form), Note), deletedLineId));

                var changes = ChangeDiffGramReader.Read(Assert.IsType<ChangeAuditEntry>(Assert.Single(writer.Entries)).ChangesXml);
                Assert.Contains(changes, c => c.FieldName == Note);
                Assert.DoesNotContain(changes, c => c.RowState == ChangeKind.Update &&
                    c.FieldName is SysFields.InsertTime or EventTime or LineTime);
                var updateTime = Assert.Single(changes, c => c.FieldName == SysFields.UpdateTime);
                Assert.Equal(s_storedUpdate, ValueUtilities.CDateTime(updateTime.OldValue));
                var deletedLineTime = Assert.Single(changes, c => c.RowState == ChangeKind.Delete && c.FieldName == LineTime);
                Assert.Equal(s_storedDeletedLine, ValueUtilities.CDateTime(deletedLineTime.OldValue));
            }
            finally
            {
                form.DropTables();
            }
        }

        #endregion

        #region 讀回找不到列

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：修改列在讀回前已被刪除時擲 UserMessageException，資料庫沒有任何寫入")]
        public void Save_Sqlite_RowGoneBeforeReadBack_Throws() => RunRowGoneBeforeReadBackThrows(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：修改列在讀回前已被刪除時擲 UserMessageException，資料庫沒有任何寫入")]
        public void Save_SqlServer_RowGoneBeforeReadBack_Throws() => RunRowGoneBeforeReadBackThrows(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：修改列在讀回前已被刪除時擲 UserMessageException，資料庫沒有任何寫入")]
        public void Save_PostgreSql_RowGoneBeforeReadBack_Throws() => RunRowGoneBeforeReadBackThrows(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：修改列在讀回前已被刪除時擲 UserMessageException，資料庫沒有任何寫入")]
        public void Save_MySql_RowGoneBeforeReadBack_Throws() => RunRowGoneBeforeReadBackThrows(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：修改列在讀回前已被刪除時擲 UserMessageException，資料庫沒有任何寫入")]
        public void Save_Oracle_RowGoneBeforeReadBack_Throws() => RunRowGoneBeforeReadBackThrows(DatabaseType.Oracle);

        private void RunRowGoneBeforeReadBackThrows(DatabaseType databaseType)
        {
            var form = NewForm(databaseType);
            form.CreateTables();
            try
            {
                var rowId = Guid.NewGuid();
                var lineId = Guid.NewGuid();
                SeedStoredRecord(form, rowId, lineId, Guid.NewGuid());

                var dataSet = new FormBusinessObject(form.CreateContext(), Guid.NewGuid(), form.Schema.ProgId)
                    .GetData(new GetDataArgs { RowId = rowId }).DataSet!;
                dataSet.Tables[form.Schema.ProgId]!.Rows[0][Note] = "changed";
                FindRow(dataSet.Tables[DetailName(form)]!, lineId)[Note] = "changed line";

                form.DbAccess.ExecuteNonQuery(
                    $"DELETE FROM {form.Quote(DetailName(form))} WHERE {form.Quote(SysFields.RowId)}={{0}}", lineId);

                var bo = new FormBusinessObject(form.CreateContext(), Guid.NewGuid(), form.Schema.ProgId);
                Assert.Throws<UserMessageException>(() => bo.Save(new SaveArgs { DataSet = dataSet }));

                // 主檔排在明細之前處理，讀回也成功；它沒被寫入才證明整批在寫入前就中止。
                Assert.Equal("stored", ReadText(form, form.Schema.ProgId, Note, rowId));
            }
            finally
            {
                form.DropTables();
            }
        }

        #endregion

        #region 覆寫接縫

        /// <summary>
        /// 原則 4 的接縫：接受用戶端 <c>event_time</c> 的 BO，只覆寫正規化方法。
        /// </summary>
        private sealed class AcceptsEventTimeBo : FormBusinessObject
        {
            public AcceptsEventTimeBo(IBeeContext ctx, string progId) : base(ctx, Guid.NewGuid(), progId) { }

            protected override void NormalizeDateTimes(SaveContext context)
            {
                var master = context.DataSet.Tables[context.Schema.ProgId]!;
                var supplied = master.Rows.Cast<DataRow>()
                    .Where(row => row.RowState == DataRowState.Added)
                    .Select(row => (Row: row, Value: row[EventTime]))
                    .ToList();

                base.NormalizeDateTimes(context);

                // 實際的 BO 會在這裡把使用者時區的值轉成 UTC；測試只需要證明寫回的值會落庫。
                foreach (var (row, value) in supplied) { row[EventTime] = value; }
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("覆寫 NormalizeDateTimes 的 BO 可以採用自己轉換過的值，其餘欄位仍由基底正規化")]
        public void Save_OverriddenNormalization_KeepsValueTheBusinessObjectWrote()
        {
            var form = NewForm(DatabaseType.SQLite);
            form.CreateTables();
            try
            {
                var rowId = Guid.NewGuid();
                var dataSet = NewRecord(form, rowId, Guid.NewGuid());
                dataSet.Tables[form.Schema.ProgId]!.Rows[0][EventTime] = s_clientValue;

                var before = DateTime.UtcNow;
                new AcceptsEventTimeBo(form.CreateContext(), form.Schema.ProgId).Save(new SaveArgs { DataSet = dataSet });
                var after = DateTime.UtcNow;

                Assert.Equal(s_clientValue, ReadInstant(form, form.Schema.ProgId, EventTime, rowId));
                AssertServerReading(ReadInstant(form, form.Schema.ProgId, SysFields.InsertTime, rowId), before, after, SysFields.InsertTime);
            }
            finally
            {
                form.DropTables();
            }
        }

        #endregion

        #region 共用

        private sealed class CapturingAuditLogWriter : IAuditLogWriter
        {
            public List<AuditEntry> Entries { get; } = [];

            public void Write(AuditEntry entry) => Entries.Add(entry);
        }

        private static (Type, object?)[] AuditOverrides(IAuditLogWriter writer) =>
        [
            (typeof(AuditLogOptions), new AuditLogOptions { Enabled = true, ChangeEnabled = true, AccessEnabled = false }),
            (typeof(IAuditLogWriter), writer),
        ];

        private TransientForm NewForm(DatabaseType databaseType)
        {
            string progId = TransientForm.NewTableName("tb_dtn_");
            var schema = new FormSchema(progId, "DateTime normalization") { CategoryId = TransientForm.CategoryId };

            var master = schema.Tables!.Add(progId, "Master");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields!.AddStringField(Note, "Note", 50);
            master.Fields.Add(SysFields.InsertTime, "Insert Time", FieldDbType.DateTime);
            master.Fields.Add(SysFields.UpdateTime, "Update Time", FieldDbType.DateTime);
            master.Fields.Add(EventTime, "Event Time", FieldDbType.DateTime);
            master.Fields.Add(DefaultedTime, "Defaulted Time", FieldDbType.DateTime).DefaultValueExpression = "Now()";
            master.Fields.Add(ComputedTime, "Computed Time", FieldDbType.DateTime).ValueExpression = "Now()";

            var detail = schema.Tables.Add(progId + "_d", "Detail");
            detail.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            detail.Fields.Add(SysFields.MasterRowId, "Master Row Id", FieldDbType.Guid);
            detail.Fields!.AddStringField(Note, "Note", 50);
            detail.Fields.Add(LineTime, "Line Time", FieldDbType.DateTime);

            return new TransientForm(_fx, databaseType, schema);
        }

        private static string DetailName(TransientForm form) => form.Schema.ProgId + "_d";

        /// <summary>
        /// 一筆新單：每個時間欄都填用戶端的值，只有 <c>event_time</c> 送空值，驗 NOT NULL 欄由伺服端補值。
        /// </summary>
        private static DataSet NewRecord(TransientForm form, Guid rowId, Guid lineId)
        {
            var dataSet = form.Repository.GetNewData();
            var master = dataSet.Tables[form.Schema.ProgId]!.Rows[0];
            master[SysFields.RowId] = rowId;
            master[Note] = "new";
            master[SysFields.InsertTime] = s_clientValue;
            master[SysFields.UpdateTime] = s_clientValue;
            master[EventTime] = DBNull.Value;
            master[DefaultedTime] = s_clientValue;
            master[ComputedTime] = s_clientValue;

            var lines = dataSet.Tables[DetailName(form)]!;
            lines.Rows.Add(lineId, rowId, "line", s_clientValue);
            return dataSet;
        }

        /// <summary>
        /// 以 SQL 直接寫入一筆已存在的單據，時間欄都是與「現在」明顯不同的已知值。
        /// </summary>
        private static void SeedStoredRecord(TransientForm form, Guid rowId, Guid lineId, Guid deletedLineId)
        {
            string master = form.Quote(form.Schema.ProgId);
            string detail = form.Quote(DetailName(form));
            form.DbAccess.ExecuteNonQuery(
                $"INSERT INTO {master} ({Columns(form, SysFields.RowId, Note, SysFields.InsertTime, SysFields.UpdateTime, EventTime, DefaultedTime, ComputedTime)}) " +
                "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                rowId, "stored", s_storedInsert, s_storedUpdate, s_storedEvent, s_storedEvent, s_storedEvent);

            string lineColumns = Columns(form, SysFields.RowId, SysFields.MasterRowId, Note, LineTime);
            form.DbAccess.ExecuteNonQuery(
                $"INSERT INTO {detail} ({lineColumns}) VALUES ({{0}}, {{1}}, {{2}}, {{3}})",
                lineId, rowId, "stored line", s_storedLine);
            form.DbAccess.ExecuteNonQuery(
                $"INSERT INTO {detail} ({lineColumns}) VALUES ({{0}}, {{1}}, {{2}}, {{3}})",
                deletedLineId, rowId, "stored deleted line", s_storedDeletedLine);
        }

        private static string Columns(TransientForm form, params string[] names)
            => string.Join(", ", names.Select(form.Quote));

        private static DataRow FindRow(DataTable table, Guid rowId)
            => table.Rows.Cast<DataRow>().Single(row => ValueUtilities.CGuid(row[SysFields.RowId]) == rowId);

        private static string SelectSql(TransientForm form, string table, string column)
            => $"SELECT {form.Quote(column)} FROM {form.Quote(table)} WHERE {form.Quote(SysFields.RowId)}={{0}}";

        private static DateTime? ReadInstant(TransientForm form, string table, string column, Guid rowId)
            => ValueUtilities.CDateTime(form.DbAccess.ExecuteScalar(SelectSql(form, table, column), rowId));

        private static string? ReadText(TransientForm form, string table, string column, Guid rowId)
            => form.DbAccess.ExecuteScalar(SelectSql(form, table, column), rowId)?.ToString();

        /// <summary>
        /// 值必須是這次存檔期間的 UTC 讀數。前後各放寬一秒，涵蓋只存到秒的欄位型別。
        /// </summary>
        private static void AssertServerReading(DateTime? value, DateTime before, DateTime after, string column)
        {
            Assert.True(value.HasValue, $"'{column}' was not written.");
            Assert.InRange(value!.Value, before.AddSeconds(-1), after.AddSeconds(1));
        }

        #endregion
    }
}
