using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Api.Contracts.AuditLog;
using Bee.Business.AuditLog;
using Bee.Definition.Logging;

namespace Bee.Business.UnitTests.AuditLog
{
    /// <summary>
    /// 針對「帶內嵌 XSD」的 changes_xml 格式：以 <c>AuditDiffGram.Serialize</c> 實際寫出 payload、
    /// 再經 <c>ChangeDiffGramReader.Read</c> 讀回，驗證三種 RowState、只吐差異欄、before-image、
    /// 值的字串化格式，以及與舊（無 schema）格式的輸出一致性。
    /// </summary>
    /// <remarks>
    /// <c>ChangeDiffGramReaderCoverageTests</c> 覆蓋的是舊格式（<c>SchemalessDiffGramReader</c>），
    /// 那些案例是回歸保護，不要合併到本檔。
    /// </remarks>
    public class SchemaBoundChangeReaderTests
    {
        private const string TableName = "ft_order";
        private static readonly Guid s_rowId = Guid.Parse("2a640240-1111-2222-3333-444455556666");
        private static readonly DateTime s_orderDate = new(2027, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);

        /// <summary>
        /// 建一張欄位型別具代表性的表：字串、decimal、DateTime、bool、int 各一，涵蓋值字串化的所有分支。
        /// </summary>
        private static DataSet NewDataSet()
        {
            var table = new DataTable(TableName) { Locale = CultureInfo.InvariantCulture };
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            var orderDate = table.Columns.Add("order_date", typeof(DateTime));
            // IMPORTANT: mirror DataTableExtensions.AddColumn, which every framework-built table goes
            // through. A DataTable defaults DateTime columns to UnspecifiedLocal, and the DiffGram then
            // writes a timezone offset (2027-03-01T00:00:00+08:00) that the restored value no longer
            // carries — the two payload shapes would disagree on that column for a reason production
            // never hits. Leaving this line out makes the parity test fail on the reader's behalf.
            orderDate.DateTimeMode = DataSetDateTime.Unspecified;
            table.Columns.Add("total_amount", typeof(decimal));
            table.Columns.Add("is_urgent", typeof(bool));
            table.Columns.Add("quantity", typeof(int));

            var dataSet = new DataSet("form") { Locale = CultureInfo.InvariantCulture };
            dataSet.Tables.Add(table);
            return dataSet;
        }

        private static DataRow SeedRow(DataSet dataSet)
        {
            var table = dataSet.Tables[TableName]!;
            var row = table.Rows.Add(s_rowId, "SO-0001", s_orderDate, 100.50m, false, 3);
            dataSet.AcceptChanges();
            return row;
        }

        /// <summary>寫出目前格式（外層元素 + 內嵌 XSD + DiffGram）。</summary>
        private static string Serialize(DataSet dataSet)
        {
            using var changes = dataSet.GetChanges()!;
            return AuditDiffGram.Serialize(changes);
        }

        /// <summary>寫出改動前的舊格式（無 schema 的裸 DiffGram），供一致性比對。</summary>
        private static string SerializeSchemaless(DataSet dataSet)
        {
            using var changes = dataSet.GetChanges()!;
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            changes.WriteXml(writer, XmlWriteMode.DiffGram);
            return writer.ToString();
        }

        // ---- payload 形狀 ----

        [Fact]
        [DisplayName("payload 應以 AuditChanges 為根、內含 XSD 與 DiffGram，且維持縮排")]
        public void Serialize_EmitsWrappedSchemaAndDiffGram()
        {
            var dataSet = NewDataSet();
            var row = SeedRow(dataSet);
            row["sys_id"] = "SO-0002";

            var xml = Serialize(dataSet);

            Assert.StartsWith("<AuditChanges>", xml, StringComparison.Ordinal);
            Assert.Contains(":schema", xml, StringComparison.Ordinal);
            Assert.Contains(":diffgram", xml, StringComparison.Ordinal);
            Assert.Contains("<diffgr:before", xml, StringComparison.Ordinal);
            // 縮排是刻意保留的（人工查稽核列時好讀）。這也是讀取端的回歸保護：
            // minified payload 會掩蓋 sub-tree reader 那個 bug。
            Assert.Contains("\n", xml, StringComparison.Ordinal);
        }

        // ---- 三種 RowState ----

        [Fact]
        [DisplayName("modified row 應只吐出實際有變動的欄，並帶新舊值")]
        public void Read_ModifiedRow_EmitsOnlyChangedColumns()
        {
            var dataSet = NewDataSet();
            var row = SeedRow(dataSet);
            row["sys_id"] = "SO-0002";
            row["total_amount"] = 250.75m;

            var result = ChangeDiffGramReader.Read(Serialize(dataSet));

            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal(ChangeKind.Update, c.RowState));
            Assert.All(result, c => Assert.Equal(TableName, c.TableName));
            Assert.All(result, c => Assert.Equal(s_rowId.ToString(), c.RowKey));
            Assert.DoesNotContain(result, c => c.FieldName == "sys_rowid");

            var id = Assert.Single(result, c => c.FieldName == "sys_id");
            Assert.Equal("SO-0001", id.OldValue);
            Assert.Equal("SO-0002", id.NewValue);
            var amount = Assert.Single(result, c => c.FieldName == "total_amount");
            Assert.Equal("100.50", amount.OldValue);
            Assert.Equal("250.75", amount.NewValue);
        }

        [Fact]
        [DisplayName("inserted row 應對每個非 row-key 欄產出 Insert，OldValue 為 null")]
        public void Read_InsertedRow_EmitsInsertPerColumn()
        {
            var dataSet = NewDataSet();
            dataSet.Tables[TableName]!.Rows.Add(s_rowId, "SO-0009", s_orderDate, 88m, true, 7);

            var result = ChangeDiffGramReader.Read(Serialize(dataSet));

            Assert.All(result, c => Assert.Equal(ChangeKind.Insert, c.RowState));
            Assert.All(result, c => Assert.Null(c.OldValue));
            Assert.All(result, c => Assert.Equal(s_rowId.ToString(), c.RowKey));
            Assert.DoesNotContain(result, c => c.FieldName == "sys_rowid");
            var id = Assert.Single(result, c => c.FieldName == "sys_id");
            Assert.Equal("SO-0009", id.NewValue);
        }

        [Fact]
        [DisplayName("deleted row 應吐出 before-image，NewValue 為 null")]
        public void Read_DeletedRow_EmitsBeforeImage()
        {
            var dataSet = NewDataSet();
            var row = SeedRow(dataSet);
            row.Delete();

            var result = ChangeDiffGramReader.Read(Serialize(dataSet));

            Assert.All(result, c => Assert.Equal(ChangeKind.Delete, c.RowState));
            Assert.All(result, c => Assert.Null(c.NewValue));
            Assert.All(result, c => Assert.Equal(s_rowId.ToString(), c.RowKey));
            Assert.DoesNotContain(result, c => c.FieldName == "sys_rowid");
            var id = Assert.Single(result, c => c.FieldName == "sys_id");
            Assert.Equal("SO-0001", id.OldValue);
        }

        [Fact]
        [DisplayName("同一 payload 內的 insert / update / delete 應各自還原")]
        public void Read_MixedRowStates_RestoresEach()
        {
            var dataSet = NewDataSet();
            var table = dataSet.Tables[TableName]!;
            var kept = table.Rows.Add(s_rowId, "SO-0001", s_orderDate, 100.50m, false, 3);
            var removed = table.Rows.Add(Guid.NewGuid(), "SO-0002", s_orderDate, 20m, false, 1);
            dataSet.AcceptChanges();

            kept["quantity"] = 9;
            removed.Delete();
            table.Rows.Add(Guid.NewGuid(), "SO-0003", s_orderDate, 30m, true, 2);

            var result = ChangeDiffGramReader.Read(Serialize(dataSet));

            var updated = Assert.Single(result, c => c.RowState == ChangeKind.Update);
            Assert.Equal("quantity", updated.FieldName);
            Assert.Equal("3", updated.OldValue);
            Assert.Equal("9", updated.NewValue);
            Assert.Contains(result, c => c.RowState == ChangeKind.Delete && c.FieldName == "sys_id" && c.OldValue == "SO-0002");
            Assert.Contains(result, c => c.RowState == ChangeKind.Insert && c.FieldName == "sys_id" && c.NewValue == "SO-0003");
        }

        // ---- 值的字串化 ----

        [Fact]
        [DisplayName("值應以 XML 詞法格式輸出（culture 無關），不得用 ToString")]
        public void Read_Values_UseXmlLexicalForm()
        {
            var dataSet = NewDataSet();
            var row = SeedRow(dataSet);
            row["order_date"] = new DateTime(2028, 12, 25, 13, 45, 30, DateTimeKind.Unspecified);
            row["is_urgent"] = true;

            var result = ChangeDiffGramReader.Read(Serialize(dataSet));

            // ToString() 在任何 culture 下都不會產出這兩個格式，因此本斷言足以擋下 culture 相依的寫法。
            var date = Assert.Single(result, c => c.FieldName == "order_date");
            Assert.Equal("2027-03-01T00:00:00", date.OldValue);
            Assert.Equal("2028-12-25T13:45:30", date.NewValue);
            var urgent = Assert.Single(result, c => c.FieldName == "is_urgent");
            Assert.Equal("false", urgent.OldValue);
            Assert.Equal("true", urgent.NewValue);
        }

        // ---- 與舊格式的一致性 ----

        [Fact]
        [DisplayName("同一組異動，新舊兩種 payload 應還原出完全相同的欄位變更")]
        public void Read_NewAndLegacyPayloads_ProduceIdenticalChanges()
        {
            static DataSet Mutate()
            {
                var dataSet = NewDataSet();
                var row = SeedRow(dataSet);
                row["sys_id"] = "SO-0002";
                row["order_date"] = new DateTime(2028, 12, 25, 13, 45, 30, DateTimeKind.Unspecified);
                row["total_amount"] = 250.75m;
                row["is_urgent"] = true;
                row["quantity"] = 9;
                return dataSet;
            }

            var fromNew = ChangeDiffGramReader.Read(Serialize(Mutate()));
            var fromLegacy = ChangeDiffGramReader.Read(SerializeSchemaless(Mutate()));

            static IEnumerable<string> Flatten(IEnumerable<RecordFieldChange> changes)
                => changes.Select(c => $"{c.TableName}|{c.RowKey}|{c.RowState}|{c.FieldName}|{c.OldValue}|{c.NewValue}")
                          .OrderBy(x => x, StringComparer.Ordinal);

            Assert.NotEmpty(fromNew);
            Assert.Equal(Flatten(fromLegacy), Flatten(fromNew));
        }

        // ---- 非 payload / 損毀輸入 ----

        [Theory]
        [InlineData("<AuditChanges />")]
        [InlineData("<AuditChanges></AuditChanges>")]
        [InlineData("<AuditChanges><NotASchema /></AuditChanges>")]
        [DisplayName("外層元素在但內容不成 payload 時應回傳空清單而非拋例外")]
        public void Read_WrapperWithoutUsableContent_ReturnsEmpty(string input)
        {
            var result = ChangeDiffGramReader.Read(input);

            Assert.Empty(result);
        }

        [Fact]
        [DisplayName("最小刪除標記應回傳空清單（分派不得誤判為新格式）")]
        public void Read_MinimalDeleteMarker_ReturnsEmpty()
        {
            var result = ChangeDiffGramReader.Read("<DeletedRow table=\"ft_order\" sys_rowid=\"abc\" />");

            Assert.Empty(result);
        }
    }
}
