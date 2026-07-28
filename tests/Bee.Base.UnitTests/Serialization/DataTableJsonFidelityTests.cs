using System.ComponentModel;
using System.Data;
using System.Text.Json;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests.Serialization
{
    /// <summary>
    /// <see cref="DataTable"/> 經 JSON wire round-trip 的保真度測試。
    /// </summary>
    /// <remarks>
    /// 這裡驗的是「值不變」而非「不擲例外」——兩個失真都是靜默的：
    /// 字串欄被當成日期改寫、decimal 超過 double 精度被截斷。
    /// </remarks>
    public class DataTableJsonFidelityTests
    {
        private static DataTable RoundTrip(DataTable source)
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new DataTableJsonConverter());

            var json = JsonSerializer.Serialize(source, options);
            return JsonSerializer.Deserialize<DataTable>(json, options)!;
        }

        [Theory]
        [InlineData("2026-07-28")]
        [InlineData("2026-07-28T10:30:00")]
        [InlineData("10:30")]
        [DisplayName("字串欄位存日期樣式文字，round-trip 後應原樣保留")]
        public void StringColumn_DateShapedText_SurvivesRoundTrip(string text)
        {
            var table = new DataTable("T");
            table.Columns.Add("note", typeof(string));
            table.Rows.Add(text);

            var restored = RoundTrip(table);

            // 先前會先 TryGetDateTime 成功、再 Convert.ChangeType 回字串，
            // 於是 "2026-07-28" 變成 "07/28/2026 00:00:00"。
            Assert.Equal(text, restored.Rows[0]["note"]);
        }

        [Fact]
        [DisplayName("decimal 欄位超過 double 精度時 round-trip 不應失精")]
        public void DecimalColumn_HighPrecision_SurvivesRoundTrip()
        {
            // 17 位有效數字：double 只有約 15~16 位，走 double 會被截。
            const decimal amount = 12345678901234.567m;

            var table = new DataTable("T");
            table.Columns.Add("amount", typeof(decimal));
            table.Rows.Add(amount);

            var restored = RoundTrip(table);

            Assert.Equal(amount, restored.Rows[0]["amount"]);
        }

        [Fact]
        [DisplayName("DateTime 欄位仍應正確還原為 DateTime")]
        public void DateTimeColumn_StillParsesAsDateTime()
        {
            var value = new DateTime(2026, 7, 28, 10, 30, 0, DateTimeKind.Unspecified);

            var table = new DataTable("T");
            table.Columns.Add("created_at", typeof(DateTime));
            table.Rows.Add(value);

            var restored = RoundTrip(table);

            Assert.Equal(value, restored.Rows[0]["created_at"]);
        }
    }
}
