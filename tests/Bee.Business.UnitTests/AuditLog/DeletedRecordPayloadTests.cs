using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Api.Contracts.AuditLog;
using Bee.Base.Data;
using Bee.Business.AuditLog;
using Bee.Definition.Logging;

namespace Bee.Business.UnitTests.AuditLog
{
    /// <summary>
    /// 刪除原單 payload（<c>AuditDeletedRecord</c>）：原單原樣寫出、不改動傳入的 DataSet，
    /// 且讀出的欄位清單與舊寫法（先把每一列標成 Deleted 再寫成變更集）逐筆相同。
    /// </summary>
    /// <remarks>
    /// 舊寫法在 production 已不再產生，但資料庫裡的舊刪除記錄仍是那個形狀。一致性測試因此在本檔
    /// 自行保留舊寫法——若改成呼叫 production 的寫入端，這組測試會靜默改測新形狀，舊資料就失去回歸保護。
    /// </remarks>
    public class DeletedRecordPayloadTests
    {
        private const string ProgId = "Employee";
        private const string DetailTableName = "EmployeeSkill";
        private const string EmptyDetailTableName = "EmployeeNote";
        private static readonly Guid s_masterId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly string s_textWithControls = "含" + (char)1 + "控制字元\r\n換行";

        /// <summary>
        /// 照 <c>DataFormRepository.GetData</c> 的形狀建原單：DataSet 名稱與主檔表名相同、欄位經
        /// <c>AddColumn</c> 依 <see cref="FieldDbType"/> 建立（每種型別一欄，另有一欄 DBNull）、
        /// 一張明細表與一張空明細表，最後 <c>AcceptChanges</c>。
        /// </summary>
        private static DataSet NewRecord(bool withDetailRows)
        {
            var dataSet = new DataSet(ProgId) { Locale = CultureInfo.InvariantCulture };

            var master = new DataTable(ProgId) { Locale = CultureInfo.InvariantCulture };
            master.AddColumn("sys_rowid", FieldDbType.Guid);
            foreach (var dbType in Enum.GetValues<FieldDbType>().Where(t => t != FieldDbType.Unknown))
            {
                master.AddColumn("f_" + dbType.ToString().ToLowerInvariant(), dbType);
            }
            master.AddColumn("f_null", FieldDbType.String).AllowDBNull = true;
            dataSet.Tables.Add(master);

            var masterRow = master.NewRow();
            foreach (DataColumn column in master.Columns)
            {
                masterRow[column] = SampleValue(column);
            }
            masterRow["sys_rowid"] = s_masterId;
            masterRow["f_null"] = DBNull.Value;
            master.Rows.Add(masterRow);

            var detail = new DataTable(DetailTableName) { Locale = CultureInfo.InvariantCulture };
            detail.AddColumn("sys_rowid", FieldDbType.Guid);
            detail.AddColumn("sys_master_rowid", FieldDbType.Guid);
            detail.AddColumn("skill", FieldDbType.String);
            dataSet.Tables.Add(detail);
            if (withDetailRows)
            {
                detail.Rows.Add(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), s_masterId, "C#");
                detail.Rows.Add(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), s_masterId, "SQL");
            }

            var emptyDetail = new DataTable(EmptyDetailTableName) { Locale = CultureInfo.InvariantCulture };
            emptyDetail.AddColumn("sys_rowid", FieldDbType.Guid);
            emptyDetail.AddColumn("note", FieldDbType.Text);
            dataSet.Tables.Add(emptyDetail);

            dataSet.AcceptChanges();
            return dataSet;
        }

        /// <summary>
        /// 依欄位的 CLR 型別給樣本值。遇到沒列到的型別直接擲例外——新增的 <see cref="FieldDbType"/>
        /// 若對應到新的 CLR 型別，這組測試應該紅，而不是略過那一欄。
        /// </summary>
        private static object SampleValue(DataColumn column)
        {
            var type = column.DataType;
            if (type == typeof(string)) { return column.ColumnName == "f_string" ? s_textWithControls : "08:30"; }
            if (type == typeof(bool)) { return true; }
            if (type == typeof(short)) { return (short)-3; }
            if (type == typeof(int)) { return 42; }
            if (type == typeof(long)) { return 9_000_000_000L; }
            if (type == typeof(decimal)) { return 1234.50m; }
            if (type == typeof(DateTime)) { return new DateTime(2026, 9, 11, 13, 45, 30, DateTimeKind.Unspecified); }
            if (type == typeof(TimeSpan)) { return new TimeSpan(8, 30, 0); }
            if (type == typeof(Guid)) { return Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"); }
            if (type == typeof(byte[])) { return new byte[] { 1, 2, 3 }; }
            throw new InvalidOperationException($"No sample value for column '{column.ColumnName}' of type {type}.");
        }

        /// <summary>
        /// 舊寫法：複製原單、把每一列標成 Deleted，再以 <c>GetChanges</c> 寫成變更集。
        /// </summary>
        private static string SerializeLegacyDeletedRecord(DataSet record)
        {
            using var marked = record.Copy();
            foreach (DataTable table in marked.Tables)
            {
                for (int i = table.Rows.Count - 1; i >= 0; i--)
                {
                    table.Rows[i].Delete();
                }
            }
            using var changes = marked.GetChanges()!;
            return AuditDiffGram.Serialize(changes);
        }

        private static List<string> Flatten(IEnumerable<RecordFieldChange> changes)
            => changes.Select(c => $"{c.TableName}|{c.RowKey}|{c.RowState}|{c.FieldName}|{c.OldValue}|{c.NewValue}")
                      .OrderBy(x => x, StringComparer.Ordinal)
                      .ToList();

        [Fact]
        [DisplayName("刪除原單以 AuditDeletedRecord 為根、沒有 before 區塊，且不改動傳入的 DataSet")]
        public void SerializeDeletedRecord_WritesRecordAsLoaded_WithoutModifyingIt()
        {
            using var record = NewRecord(withDetailRows: true);

            string xml = AuditDiffGram.SerializeDeletedRecord(record);

            Assert.StartsWith("<" + AuditDiffGram.DeletedRecordRootElementName + ">", xml, StringComparison.Ordinal);
            Assert.DoesNotContain("diffgr:before", xml, StringComparison.Ordinal);
            Assert.All(record.Tables.Cast<DataTable>().SelectMany(t => t.Rows.Cast<DataRow>()),
                row => Assert.Equal(DataRowState.Unchanged, row.RowState));
        }

        [Fact]
        [DisplayName("刪除原單的每一個非鍵欄位都以 Delete 讀出，含控制字元與 CR 的值逐字還原")]
        public void Read_DeletedRecord_EmitsEveryColumnAsDelete()
        {
            using var record = NewRecord(withDetailRows: true);

            var result = ChangeDiffGramReader.Read(AuditDiffGram.SerializeDeletedRecord(record));

            int expected = record.Tables.Cast<DataTable>().Sum(t => t.Rows.Count * (t.Columns.Count - 1));
            Assert.Equal(expected, result.Count);
            Assert.All(result, c => Assert.Equal(ChangeKind.Delete, c.RowState));
            Assert.All(result, c => Assert.Null(c.NewValue));
            Assert.Contains(result, c => c.TableName == DetailTableName && c.FieldName == "skill" && c.OldValue == "SQL");
            var text = Assert.Single(result, c => c.FieldName == "f_string");
            Assert.Equal(s_masterId.ToString(), text.RowKey);
            Assert.Equal(s_textWithControls, text.OldValue);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [DisplayName("同一張原單以新寫法與舊寫法產生的刪除 payload，讀出的欄位清單逐筆相同")]
        public void Read_DeletedRecord_MatchesLegacyDeletedRowsPayload(bool withDetailRows)
        {
            using var record = NewRecord(withDetailRows);

            var fromNew = ChangeDiffGramReader.Read(AuditDiffGram.SerializeDeletedRecord(record));
            var fromLegacy = ChangeDiffGramReader.Read(SerializeLegacyDeletedRecord(record));

            Assert.NotEmpty(fromNew);
            Assert.Equal(Flatten(fromLegacy), Flatten(fromNew));
        }
    }
}
