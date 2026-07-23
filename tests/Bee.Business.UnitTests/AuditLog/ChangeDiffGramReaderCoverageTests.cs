using System.ComponentModel;
using Bee.Api.Contracts.AuditLog;
using Bee.Business.AuditLog;
using Bee.Definition.Logging;

namespace Bee.Business.UnitTests.AuditLog
{
    /// <summary>
    /// 針對組件內部 <c>ChangeDiffGramReader.Read</c> 的純解析覆蓋測試（透過 InternalsVisibleTo 直接叫用，
    /// 餵入 DataSet DiffGram 字串）：null / 空白、malformed（XmlException 吞掉回空）、inserted row、
    /// modified 配對 before 只出差異欄、unmatched before（delete）、無 sys_rowid（rowKey=null）、
    /// 無 diffgr:id 的 before row 不成 delete。
    /// </summary>
    public class ChangeDiffGramReaderCoverageTests
    {
        private const string Diff = "urn:schemas-microsoft-com:xml-diffgram-v1";

        private static List<RecordFieldChange> Read(string? changesXml)
            => ChangeDiffGramReader.Read(changesXml);

        // ---- blank / malformed inputs ----

        [Fact]
        [DisplayName("null 輸入應回傳空清單")]
        public void Read_Null_ReturnsEmpty()
        {
            var result = Read(null);

            Assert.Empty(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        [DisplayName("空字串 / 純空白應回傳空清單")]
        public void Read_BlankOrWhitespace_ReturnsEmpty(string input)
        {
            var result = Read(input);

            Assert.Empty(result);
        }

        [Theory]
        [InlineData("<broken><unclosed>")]
        [InlineData("<diffgr:diffgram></diffgr:diffgram>")] // undeclared prefix → XmlException
        [InlineData("<?xml version=\"1.0\"?>")]             // no root element
        [DisplayName("malformed / 非 DiffGram XML 應被 XmlException 吞掉回傳空清單")]
        public void Read_MalformedXml_ReturnsEmpty(string input)
        {
            var result = Read(input);

            Assert.Empty(result);
        }

        // ---- inserted rows ----

        [Fact]
        [DisplayName("inserted row 應對每個非 row-key 欄產出 Insert 變更（sys_rowid 略過、rowKey 帶入）")]
        public void Read_InsertedRow_EmitsInsertPerColumn()
        {
            var rowId = Guid.NewGuid().ToString();
            var xml =
                $"<diffgr:diffgram xmlns:diffgr=\"{Diff}\">" +
                "<NewDataSet>" +
                "<ft_customer diffgr:id=\"ft_customer1\" diffgr:hasChanges=\"inserted\">" +
                $"<sys_rowid>{rowId}</sys_rowid>" +
                "<cust_name>ACME</cust_name>" +
                "<city>Taipei</city>" +
                "</ft_customer>" +
                "</NewDataSet>" +
                "</diffgr:diffgram>";

            var result = Read(xml);

            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal(ChangeKind.Insert, c.RowState));
            Assert.All(result, c => Assert.Equal("ft_customer", c.TableName));
            Assert.All(result, c => Assert.Equal(rowId, c.RowKey));
            Assert.All(result, c => Assert.Null(c.OldValue));
            Assert.DoesNotContain(result, c => c.FieldName == "sys_rowid");
            var name = Assert.Single(result, c => c.FieldName == "cust_name");
            Assert.Equal("ACME", name.NewValue);
        }

        [Fact]
        [DisplayName("inserted row 無 sys_rowid 時 rowKey 應為 null")]
        public void Read_InsertedRowWithoutRowKey_RowKeyNull()
        {
            var xml =
                $"<diffgr:diffgram xmlns:diffgr=\"{Diff}\">" +
                "<NewDataSet>" +
                "<ft_note diffgr:id=\"ft_note1\" diffgr:hasChanges=\"inserted\">" +
                "<memo>Hello</memo>" +
                "</ft_note>" +
                "</NewDataSet>" +
                "</diffgr:diffgram>";

            var result = Read(xml);

            var change = Assert.Single(result);
            Assert.Null(change.RowKey);
            Assert.Equal("memo", change.FieldName);
            Assert.Equal(ChangeKind.Insert, change.RowState);
        }

        // ---- modified rows paired with before image ----

        [Fact]
        [DisplayName("modified row 配對 before image 應只針對有變動的欄產出 Update")]
        public void Read_ModifiedRow_EmitsOnlyChangedColumns()
        {
            var rowId = Guid.NewGuid().ToString();
            var xml =
                $"<diffgr:diffgram xmlns:diffgr=\"{Diff}\">" +
                "<NewDataSet>" +
                "<ft_customer diffgr:id=\"ft_customer1\">" +
                $"<sys_rowid>{rowId}</sys_rowid>" +
                "<cust_name>NEW</cust_name>" +
                "<city>Taipei</city>" +
                "</ft_customer>" +
                "</NewDataSet>" +
                $"<diffgr:before xmlns:diffgr=\"{Diff}\">" +
                "<ft_customer diffgr:id=\"ft_customer1\">" +
                $"<sys_rowid>{rowId}</sys_rowid>" +
                "<cust_name>OLD</cust_name>" +
                "<city>Taipei</city>" +
                "</ft_customer>" +
                "</diffgr:before>" +
                "</diffgr:diffgram>";

            var result = Read(xml);

            var change = Assert.Single(result);
            Assert.Equal(ChangeKind.Update, change.RowState);
            Assert.Equal("cust_name", change.FieldName);
            Assert.Equal("OLD", change.OldValue);
            Assert.Equal("NEW", change.NewValue);
            Assert.Equal(rowId, change.RowKey);
        }

        [Fact]
        [DisplayName("modified row 配對到的 before 不應再被視為 delete")]
        public void Read_ModifiedRow_MatchedBeforeNotTreatedAsDelete()
        {
            var rowId = Guid.NewGuid().ToString();
            var xml =
                $"<diffgr:diffgram xmlns:diffgr=\"{Diff}\">" +
                "<NewDataSet>" +
                "<ft_customer diffgr:id=\"ft_customer1\">" +
                $"<sys_rowid>{rowId}</sys_rowid>" +
                "<cust_name>NEW</cust_name>" +
                "</ft_customer>" +
                "</NewDataSet>" +
                $"<diffgr:before xmlns:diffgr=\"{Diff}\">" +
                "<ft_customer diffgr:id=\"ft_customer1\">" +
                $"<sys_rowid>{rowId}</sys_rowid>" +
                "<cust_name>OLD</cust_name>" +
                "</ft_customer>" +
                "</diffgr:before>" +
                "</diffgr:diffgram>";

            var result = Read(xml);

            Assert.DoesNotContain(result, c => c.RowState == ChangeKind.Delete);
        }

        // ---- unmatched before rows = deletes ----

        [Fact]
        [DisplayName("before 中沒有對應 current 的 row 應被視為 delete（sys_rowid 略過、new 為 null）")]
        public void Read_UnmatchedBeforeRow_EmitsDelete()
        {
            var rowId = Guid.NewGuid().ToString();
            var xml =
                $"<diffgr:diffgram xmlns:diffgr=\"{Diff}\">" +
                "<NewDataSet></NewDataSet>" +
                $"<diffgr:before xmlns:diffgr=\"{Diff}\">" +
                "<ft_customer diffgr:id=\"ft_customer9\">" +
                $"<sys_rowid>{rowId}</sys_rowid>" +
                "<cust_name>GONE</cust_name>" +
                "</ft_customer>" +
                "</diffgr:before>" +
                "</diffgr:diffgram>";

            var result = Read(xml);

            var change = Assert.Single(result);
            Assert.Equal(ChangeKind.Delete, change.RowState);
            Assert.Equal("cust_name", change.FieldName);
            Assert.Equal("GONE", change.OldValue);
            Assert.Null(change.NewValue);
            Assert.Equal(rowId, change.RowKey);
        }

        [Fact]
        [DisplayName("before row 無 diffgr:id 不會被索引，也不會產生 delete")]
        public void Read_BeforeRowWithoutId_ProducesNoDelete()
        {
            var xml =
                $"<diffgr:diffgram xmlns:diffgr=\"{Diff}\">" +
                "<NewDataSet></NewDataSet>" +
                $"<diffgr:before xmlns:diffgr=\"{Diff}\">" +
                "<ft_customer>" +
                "<cust_name>NOID</cust_name>" +
                "</ft_customer>" +
                "</diffgr:before>" +
                "</diffgr:diffgram>";

            var result = Read(xml);

            Assert.Empty(result);
        }
    }
}
