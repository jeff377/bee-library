using System.ComponentModel;
using System.Data;
using Bee.Business.AuditLog;
using Bee.Business.Form;
using Bee.Business.UnitTests.Fakes;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Filters;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Tests.Shared;
using Microsoft.Extensions.Logging;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// <c>FormBusinessObject</c> 的稽核留痕（<c>FormBusinessObject.Audit.cs</c>）：Save 依 row state
    /// 分出 Insert / Update、Delete 帶得出前影像、以及開關關閉時完全不寫。
    /// </summary>
    /// <remarks>
    /// 走真實 CRUD 路徑而非直呼稽核方法：<c>ChangeKind</c> 是從 <c>DataRow.RowState</c> 推導的，
    /// 而 RowState 會被 <c>Save</c> 重設——「在對的時機取值」正是這段程式碼要保證的事，
    /// 繞過 CRUD 就測不到。
    /// </remarks>
    public class FormBusinessObjectAuditTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public FormBusinessObjectAuditTests(SharedDbFixture fx) { _fx = fx; }

        private sealed class CapturingAuditLogWriter : IAuditLogWriter
        {
            public List<AuditEntry> Entries { get; } = [];

            public void Write(AuditEntry entry) => Entries.Add(entry);
        }

        /// <summary>模擬 log 資料庫或自訂寫入端故障。</summary>
        private sealed class ThrowingAuditLogWriter : IAuditLogWriter
        {
            public void Write(AuditEntry entry) => throw new InvalidOperationException("Audit sink unavailable.");
        }

        /// <summary>記下 commit 之後的擴充點有沒有跑到。</summary>
        private sealed class AfterStepProbeBo : FormBusinessObject
        {
            public AfterStepProbeBo(IBeeContext ctx) : base(ctx, Guid.NewGuid(), CrudTestContext.ProgId) { }

            public bool AfterSaveRan { get; private set; }

            public bool AfterDeleteRan { get; private set; }

            protected override void DoAfterSave(SaveContext context)
            {
                base.DoAfterSave(context);
                AfterSaveRan = true;
            }

            protected override void DoAfterDelete(DeleteContext context)
            {
                base.DoAfterDelete(context);
                AfterDeleteRan = true;
            }
        }

        private static (Type, object?)[] AuditOverrides(
            IAuditLogWriter writer, bool enabled = true,
            bool changeEnabled = true, bool accessEnabled = true)
            =>
            [
                (typeof(AuditLogOptions), new AuditLogOptions
                {
                    Enabled = enabled,
                    ChangeEnabled = changeEnabled,
                    AccessEnabled = accessEnabled
                }),
                (typeof(IAuditLogWriter), writer)
            ];

        private static ChangeAuditEntry SingleChange(CapturingAuditLogWriter writer)
            => Assert.IsType<ChangeAuditEntry>(Assert.Single(writer.Entries));

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Save 新增列應寫出 Insert 稽核，並帶回主表名與 sys_rowid")]
        public void Save_AddedRow_WritesInsertAudit()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                var dataSet = ctx.Repository.GetNewData();
                var master = dataSet.Tables[CrudTestContext.ProgId]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"A{runId}";
                master.Rows[0][SysFields.Name] = "稽核新增";

                ctx.CreateBoWithOverrides(AuditOverrides(writer))
                    .Save(new SaveArgs { DataSet = dataSet });

                var entry = SingleChange(writer);
                Assert.Equal(ChangeKind.Insert, entry.ChangeKind);
                Assert.Equal(CrudTestContext.ProgId, entry.ProgId);
                Assert.Equal(rowId.ToString(), entry.RowKey);
                Assert.Equal($"{CrudTestContext.ProgId}.Save", entry.Source);
                Assert.False(entry.IsSensitive);
                Assert.False(string.IsNullOrEmpty(entry.ChangesXml));
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Save 修改列應寫出 Update 稽核，且變更內容帶得出新值")]
        public void Save_ModifiedRow_WritesUpdateAudit()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                InsertEmployee(ctx, rowId, $"U{runId}", "稽核原值");

                var loaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId }).DataSet!;
                loaded.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name] = "稽核新值";

                ctx.CreateBoWithOverrides(AuditOverrides(writer))
                    .Save(new SaveArgs { DataSet = loaded });

                var entry = SingleChange(writer);
                Assert.Equal(ChangeKind.Update, entry.ChangeKind);
                // 大小寫不比對：更新路徑的 RowKey 取自 DataRow 內由 DB 讀回的值，SQLite 以字串
                // 存 GUID 且回傳大寫，新增路徑則來自 Guid.ToString()（小寫）。
                Assert.Equal(rowId.ToString(), entry.RowKey, StringComparer.OrdinalIgnoreCase);

                var changed = ChangeDiffGramReader.Read(entry.ChangesXml);
                var field = Assert.Single(changed, f => f.FieldName == SysFields.Name);
                Assert.Equal("稽核新值", field.NewValue);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Delete 應寫出 Delete 稽核，前影像含被刪資料而非僅有鍵值")]
        public void Delete_WritesDeleteAuditWithBeforeImage()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                InsertEmployee(ctx, rowId, $"D{runId}", "稽核待刪");

                ctx.CreateBoWithOverrides(AuditOverrides(writer))
                    .Delete(new DeleteArgs { RowId = rowId });

                var entry = SingleChange(writer);
                Assert.Equal(ChangeKind.Delete, entry.ChangeKind);
                Assert.Equal(rowId.ToString(), entry.RowKey);
                Assert.Equal($"{CrudTestContext.ProgId}.Delete", entry.Source);
                // 前影像取得到才有「刪掉了什麼」；只剩鍵值的 minimal XML 不含欄位值。
                Assert.Contains("稽核待刪", entry.ChangesXml, StringComparison.Ordinal);
                // 刪除存的是原單本身，不是把列標成 Deleted 的變更集。
                Assert.StartsWith("<" + AuditDiffGram.DeletedRecordRootElementName + ">", entry.ChangesXml, StringComparison.Ordinal);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("刪除不存在的列不寫稽核——留痕的是實際變更，不是嘗試")]
        public void Delete_MissingRow_WritesNothing()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();

            var result = ctx.CreateBoWithOverrides(AuditOverrides(writer))
                .Delete(new DeleteArgs { RowId = Guid.NewGuid() });

            Assert.Equal(0, result.RowsAffected);
            Assert.Empty(writer.Entries);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetData 於 AccessEnabled 時應寫出讀取軌跡")]
        public void GetData_WritesAccessAudit()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                InsertEmployee(ctx, rowId, $"R{runId}", "稽核讀取");

                ctx.CreateBoWithOverrides(AuditOverrides(writer))
                    .GetData(new GetDataArgs { RowId = rowId });

                var entry = Assert.IsType<AccessAuditEntry>(Assert.Single(writer.Entries));
                Assert.Equal(CrudTestContext.ProgId, entry.ProgId);
                Assert.Equal(rowId.ToString(), entry.RowKey);
                Assert.Equal($"{CrudTestContext.ProgId}.GetData", entry.Source);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("讀不到資料時不寫讀取軌跡")]
        public void GetData_MissingRow_WritesNothing()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();

            var result = ctx.CreateBoWithOverrides(AuditOverrides(writer))
                .GetData(new GetDataArgs { RowId = Guid.NewGuid() });

            Assert.Null(result.DataSet);
            Assert.Empty(writer.Entries);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("ChangeEnabled 關閉時 Save 不寫變更軌跡")]
        public void Save_ChangeAuditDisabled_WritesNothing()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                var dataSet = ctx.Repository.GetNewData();
                var master = dataSet.Tables[CrudTestContext.ProgId]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"N{runId}";
                master.Rows[0][SysFields.Name] = "不留痕";

                ctx.CreateBoWithOverrides(AuditOverrides(writer, changeEnabled: false, accessEnabled: false))
                    .Save(new SaveArgs { DataSet = dataSet });

                Assert.Empty(writer.Entries);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("全域開關關閉時 Delete 不寫變更軌跡")]
        public void Delete_AuditDisabled_WritesNothing()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                InsertEmployee(ctx, rowId, $"Z{runId}", "不留痕待刪");

                ctx.CreateBoWithOverrides(AuditOverrides(writer, enabled: false))
                    .Delete(new DeleteArgs { RowId = rowId });

                Assert.Empty(writer.Entries);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("欄位值含 XML 不允許的控制字元時 Save 仍成功，且稽核讀得回原值")]
        public void Save_ValueWithControlCharacter_WritesReadableAudit()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];
            const string pasted = "貼上\u0001的值";

            try
            {
                InsertEmployee(ctx, rowId, $"C{runId}", "控制字元原值");

                var loaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId }).DataSet!;
                loaded.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name] = pasted;

                ctx.CreateBoWithOverrides(AuditOverrides(writer))
                    .Save(new SaveArgs { DataSet = loaded });

                var entry = SingleChange(writer);
                var field = Assert.Single(ChangeDiffGramReader.Read(entry.ChangesXml), f => f.FieldName == SysFields.Name);
                Assert.Equal(pasted, field.NewValue);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("被刪資料含 XML 不允許的控制字元時 Delete 仍成功，且前影像讀得回原值")]
        public void Delete_ValueWithControlCharacter_WritesReadableAudit()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];
            const string stored = "待刪\u0001資料";

            try
            {
                InsertEmployee(ctx, rowId, $"E{runId}", stored);

                ctx.CreateBoWithOverrides(AuditOverrides(writer))
                    .Delete(new DeleteArgs { RowId = rowId });

                var entry = SingleChange(writer);
                var field = Assert.Single(ChangeDiffGramReader.Read(entry.ChangesXml),
                    f => f.FieldName == SysFields.Name && f.RowState == ChangeKind.Delete);
                Assert.Equal(stored, field.OldValue);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("★稽核寫入失敗時，已 commit 的 Save 不得回傳失敗，AfterSave 照跑並記下錯誤 log")]
        public void Save_AuditWriteFails_CompletesAndLogsError()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var loggers = new RecordingLoggerFactory();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                var dataSet = ctx.Repository.GetNewData();
                var master = dataSet.Tables[CrudTestContext.ProgId]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"F{runId}";
                master.Rows[0][SysFields.Name] = "稽核失敗仍存檔";

                var bo = new AfterStepProbeBo(ctx.CreateContextWithOverrides(
                    [.. AuditOverrides(new ThrowingAuditLogWriter()), (typeof(ILoggerFactory), loggers)]));
                bo.Save(new SaveArgs { DataSet = dataSet });

                Assert.True(bo.AfterSaveRan);
                Assert.NotNull(ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId }).DataSet);
                var error = Assert.Single(loggers.Entries, e => e.Level == LogLevel.Error);
                Assert.IsType<InvalidOperationException>(error.Exception);
                Assert.Contains(rowId.ToString(), error.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("★稽核寫入失敗時，已 commit 的 Delete 不得回傳失敗，AfterDelete 照跑並記下錯誤 log")]
        public void Delete_AuditWriteFails_CompletesAndLogsError()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var loggers = new RecordingLoggerFactory();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                InsertEmployee(ctx, rowId, $"G{runId}", "稽核失敗仍刪除");

                var bo = new AfterStepProbeBo(ctx.CreateContextWithOverrides(
                    [.. AuditOverrides(new ThrowingAuditLogWriter()), (typeof(ILoggerFactory), loggers)]));
                var result = bo.Delete(new DeleteArgs { RowId = rowId });

                Assert.Equal(1, result.RowsAffected);
                Assert.True(bo.AfterDeleteRan);
                var error = Assert.Single(loggers.Entries, e => e.Level == LogLevel.Error);
                Assert.IsType<InvalidOperationException>(error.Exception);
                Assert.Contains(rowId.ToString(), error.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        private static void InsertEmployee(CrudTestContext ctx, Guid rowId, string sysId, string sysName)
        {
            var dt = new DataTable();
            dt.Columns.Add(SysFields.RowId, typeof(Guid));
            dt.Columns.Add("sys_id", typeof(string));
            dt.Columns.Add(SysFields.Name, typeof(string));
            dt.Columns.Add("dept_rowid", typeof(Guid));
            var row = dt.NewRow();
            row[SysFields.RowId] = rowId;
            row["sys_id"] = sysId;
            row[SysFields.Name] = sysName;
            row["dept_rowid"] = Guid.Empty;
            ctx.DbAccess.Execute(
                new InsertCommandBuilder(ctx.EmployeeSchema, ctx.DbType).Build(CrudTestContext.ProgId, row));
        }

        private static void TryDelete(CrudTestContext ctx, Guid rowId)
        {
            try
            {
                ctx.DbAccess.Execute(new DeleteCommandBuilder(ctx.EmployeeSchema, ctx.DbType)
                    .Build(CrudTestContext.ProgId, FilterCondition.Equal(SysFields.RowId, rowId)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuditTests cleanup of Employee#{rowId} failed — {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
