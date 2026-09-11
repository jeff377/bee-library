using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Business.AuditLog;
using Bee.Definition.Logging;

namespace Bee.Business.UnitTests.AuditLog
{
    /// <summary>
    /// <c>ChangeDiffGramReader.ReadDetail</c> 對各種 payload 還原出的 DataSet：新增、修改、刪除原單、
    /// 舊的刪除記錄各自帶對的列狀態；無 schema 的舊格式、刪除標記、空白與損毀一律為 null。
    /// </summary>
    public class ChangeDetailDataSetTests
    {
        private const string TableName = "ft_order";
        private const string RowKey = "row-1";

        private static DataSet NewDataSet()
        {
            var table = new DataTable(TableName) { Locale = CultureInfo.InvariantCulture };
            table.Columns.Add("sys_rowid", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            var dataSet = new DataSet(TableName) { Locale = CultureInfo.InvariantCulture };
            dataSet.Tables.Add(table);
            return dataSet;
        }

        private static string SerializeChanges(DataSet dataSet)
        {
            using var changes = dataSet.GetChanges()!;
            return AuditDiffGram.Serialize(changes);
        }

        private static DataRow SingleRow(DataSet? dataSet)
        {
            Assert.NotNull(dataSet);
            return Assert.Single(dataSet!.Tables[TableName]!.Rows.Cast<DataRow>());
        }

        [Fact]
        [DisplayName("新增的變更集應還原為 Added 列")]
        public void ReadDetail_Insert_ReturnsAddedRow()
        {
            using var source = NewDataSet();
            source.Tables[TableName]!.Rows.Add(RowKey, "新單");

            var (fields, dataSet) = ChangeDiffGramReader.ReadDetail(SerializeChanges(source));
            using (dataSet)
            {
                var row = SingleRow(dataSet);
                Assert.Equal(DataRowState.Added, row.RowState);
                Assert.Equal("新單", row["sys_name"]);
                Assert.Contains(fields, f => f.RowState == ChangeKind.Insert && f.FieldName == "sys_name");
            }
        }

        [Fact]
        [DisplayName("修改的變更集應還原為 Modified 列，並帶原值")]
        public void ReadDetail_Update_ReturnsModifiedRowWithOriginal()
        {
            using var source = NewDataSet();
            var sourceRow = source.Tables[TableName]!.Rows.Add(RowKey, "原值");
            source.AcceptChanges();
            sourceRow["sys_name"] = "新值";

            var (_, dataSet) = ChangeDiffGramReader.ReadDetail(SerializeChanges(source));
            using (dataSet)
            {
                var row = SingleRow(dataSet);
                Assert.Equal(DataRowState.Modified, row.RowState);
                Assert.Equal("原值", row["sys_name", DataRowVersion.Original]);
                Assert.Equal("新值", row["sys_name", DataRowVersion.Current]);
            }
        }

        [Fact]
        [DisplayName("刪除原單應還原為 Unchanged 列，即刪除前的完整原單")]
        public void ReadDetail_DeletedRecord_ReturnsUnchangedRecord()
        {
            using var source = NewDataSet();
            source.Tables[TableName]!.Rows.Add(RowKey, "待刪原單");
            source.AcceptChanges();

            var (fields, dataSet) = ChangeDiffGramReader.ReadDetail(AuditDiffGram.SerializeDeletedRecord(source));
            using (dataSet)
            {
                var row = SingleRow(dataSet);
                Assert.Equal(DataRowState.Unchanged, row.RowState);
                Assert.Equal("待刪原單", row["sys_name"]);
                var field = Assert.Single(fields);
                Assert.Equal(ChangeKind.Delete, field.RowState);
            }
        }

        [Fact]
        [DisplayName("舊的刪除記錄（列標成 Deleted 的變更集）應原樣還原為 Deleted 列")]
        public void ReadDetail_LegacyDeletedRows_ReturnsDeletedRow()
        {
            using var source = NewDataSet();
            var sourceRow = source.Tables[TableName]!.Rows.Add(RowKey, "舊刪除");
            source.AcceptChanges();
            sourceRow.Delete();

            var (_, dataSet) = ChangeDiffGramReader.ReadDetail(SerializeChanges(source));
            using (dataSet)
            {
                var row = SingleRow(dataSet);
                Assert.Equal(DataRowState.Deleted, row.RowState);
                Assert.Equal("舊刪除", row["sys_name", DataRowVersion.Original]);
            }
        }

        [Fact]
        [DisplayName("無 schema 的舊 DiffGram 讀得出欄位清單，但沒有 DataSet")]
        public void ReadDetail_SchemalessDiffGram_ReturnsFieldsWithoutDataSet()
        {
            using var source = NewDataSet();
            var sourceRow = source.Tables[TableName]!.Rows.Add(RowKey, "原值");
            source.AcceptChanges();
            sourceRow["sys_name"] = "新值";
            using var changes = source.GetChanges()!;
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            changes.WriteXml(writer, XmlWriteMode.DiffGram);

            var (fields, dataSet) = ChangeDiffGramReader.ReadDetail(writer.ToString());

            Assert.Null(dataSet);
            Assert.Single(fields);
        }

        [Theory]
        [InlineData("")]
        [InlineData("<DeletedRow table=\"ft_order\" sys_rowid=\"abc\" />")]
        [InlineData("<AuditChanges />")]
        [InlineData("<AuditChanges><broken>")]
        [DisplayName("刪除標記、空白、空外層與損毀的 payload 沒有 DataSet，欄位清單為空")]
        public void ReadDetail_NoRestorablePayload_ReturnsNothing(string payload)
        {
            var (fields, dataSet) = ChangeDiffGramReader.ReadDetail(payload);

            Assert.Null(dataSet);
            Assert.Empty(fields);
        }
    }
}
