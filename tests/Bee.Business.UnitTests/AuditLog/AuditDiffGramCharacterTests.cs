using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Api.Contracts.AuditLog;
using Bee.Business.AuditLog;
using Bee.Definition.Logging;

namespace Bee.Business.UnitTests.AuditLog
{
    /// <summary>
    /// 字串值含 XML 1.0 不允許的字元或 CR 時，<c>AuditDiffGram</c> 寫出的 payload：寫入不得擲例外，
    /// 經 <c>ChangeDiffGramReader</c> 讀回的新舊值必須與原值逐字相同。唯一的例外是落單 surrogate——
    /// 任何 XML 形式都表達不了它，換成 U+FFFD。
    /// </summary>
    /// <remarks>
    /// 表單與部署層的稽核都在資料庫交易 commit 之後才序列化。寫入端在這裡擲例外，已經存檔的呼叫
    /// 就會回傳失敗，而且稽核也沒寫到。字元以碼位傳入再組字串，是為了不讓 NUL 與落單 surrogate
    /// 經過 xUnit 的 theory 資料序列化。
    /// </remarks>
    public class AuditDiffGramCharacterTests
    {
        private const string TableName = "ft_note";
        private const string RowKey = "row-1";
        private const string Column = "memo";

        private static DataSet NewDataSet()
        {
            var table = new DataTable(TableName) { Locale = CultureInfo.InvariantCulture };
            table.Columns.Add("sys_rowid", typeof(string));
            table.Columns.Add(Column, typeof(string));
            var dataSet = new DataSet("form") { Locale = CultureInfo.InvariantCulture };
            dataSet.Tables.Add(table);
            return dataSet;
        }

        private static List<RecordFieldChange> RoundTrip(DataSet dataSet)
        {
            using var changes = dataSet.GetChanges()!;
            return ChangeDiffGramReader.Read(AuditDiffGram.Serialize(changes));
        }

        private static RecordFieldChange ModifiedMemo(string before, string after)
        {
            var dataSet = NewDataSet();
            var row = dataSet.Tables[TableName]!.Rows.Add(RowKey, before);
            dataSet.AcceptChanges();
            row[Column] = after;
            return Assert.Single(RoundTrip(dataSet));
        }

        [Theory]
        [InlineData(0x0000)]
        [InlineData(0x0001)]
        [InlineData(0x001F)]
        [InlineData(0xFFFE)]
        [InlineData(0xFFFF)]
        [DisplayName("XML 不允許的字元應寫得出 payload，且新舊值逐字讀回")]
        public void Serialize_InvalidXmlCharacter_RoundTripsExactly(int codePoint)
        {
            char character = (char)codePoint;
            string before = "舊" + character + "值";
            string after = "新" + character + "值";

            var change = ModifiedMemo(before, after);

            Assert.Equal(before, change.OldValue);
            Assert.Equal(after, change.NewValue);
        }

        [Theory]
        [InlineData("a\r\nb")]
        [InlineData("a\rb")]
        [InlineData("行尾\r")]
        [DisplayName("CR 與 CRLF 讀回不得被正規化為 LF")]
        public void Serialize_CarriageReturn_IsPreserved(string after)
        {
            var change = ModifiedMemo("原值", after);

            Assert.Equal(after, change.NewValue);
        }

        [Theory]
        [InlineData(0xD842)]
        [InlineData(0xDC00)]
        [DisplayName("落單 surrogate 無法以 XML 表達，新舊值都應換成 U+FFFD 而非擲例外")]
        public void Serialize_LoneSurrogate_ReplacedWithReplacementCharacter(int codePoint)
        {
            char surrogate = (char)codePoint;

            var change = ModifiedMemo("舊" + surrogate + "值", "新值" + surrogate);

            Assert.Equal("舊\uFFFD值", change.OldValue);
            Assert.Equal("新值\uFFFD", change.NewValue);
        }

        [Fact]
        [DisplayName("成對的 surrogate（emoji）不受替換影響")]
        public void Serialize_SurrogatePair_IsPreserved()
        {
            string after = "讚😀，孤\uD842";

            var change = ModifiedMemo("原值", after);

            Assert.Equal("讚😀，孤\uFFFD", change.NewValue);
        }

        [Fact]
        [DisplayName("被刪列的 before-image 含不合法字元時也應寫得出且讀得回")]
        public void Serialize_DeletedRowWithInvalidCharacters_RestoresBeforeImage()
        {
            var dataSet = NewDataSet();
            var row = dataSet.Tables[TableName]!.Rows.Add(RowKey, "刪\u0001除\r\n前\uDC00");
            dataSet.AcceptChanges();
            row.Delete();

            var change = Assert.Single(RoundTrip(dataSet));

            Assert.Equal(ChangeKind.Delete, change.RowState);
            Assert.Equal("刪\u0001除\r\n前\uFFFD", change.OldValue);
        }

        [Fact]
        [DisplayName("部署層的 ForInsert 走同一支序列化，含控制字元的值也應讀得回")]
        public void ForInsert_ValueWithControlCharacter_RoundTrips()
        {
            string payload = AuditDiffGram.ForInsert("st_api_key", [("sys_name", "App\u0001Name")]);

            var change = Assert.Single(ChangeDiffGramReader.Read(payload), c => c.FieldName == "sys_name");

            Assert.Equal("App\u0001Name", change.NewValue);
        }
    }
}
