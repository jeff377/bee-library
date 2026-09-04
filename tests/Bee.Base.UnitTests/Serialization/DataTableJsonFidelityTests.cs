using System.ComponentModel;
using System.Data;
using System.Globalization;
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
    
        [Theory]
        [InlineData("79228162514264337593543950335")]   // decimal.MaxValue
        [InlineData("-79228162514264337593543950335")]
        [InlineData("0.0000000000000000000000000001")]   // 最小刻度
        [InlineData("1234.56")]
        [DisplayName("decimal 欄位在 JSON 上以字串攜帶，超過 double 精度也應原值還原")]
        public void DecimalColumn_BeyondDoublePrecision_SurvivesRoundTrip(string literal)
        {
            var expected = decimal.Parse(literal, CultureInfo.InvariantCulture);
            var table = new DataTable("T");
            table.Columns.Add("amount", typeof(decimal));
            table.Rows.Add(expected);

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DataTableJsonConverter());
            var json = JsonSerializer.Serialize(table, options);

            // 形狀本身就是契約的一部分：裸數字對 JavaScript 讀取端是 double，
            // 在客戶端程式碼看到值之前就已經失真。
            Assert.Contains($"\"amount\": \"{literal}\"".Replace(" ", string.Empty),
                json.Replace(" ", string.Empty), StringComparison.Ordinal);

            var restored = JsonSerializer.Deserialize<DataTable>(json, options)!;
            Assert.Equal(expected, Assert.IsType<decimal>(restored.Rows[0]["amount"]));
        }

        [Theory]
        [InlineData(9007199254740993L)]      // 2^53 + 1：double 存不住
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [DisplayName("long 欄位超過 2^53 應以字串攜帶並原值還原")]
        public void Int64Column_BeyondSafeInteger_SurvivesRoundTrip(long expected)
        {
            var table = new DataTable("T");
            table.Columns.Add("bigint", typeof(long));
            table.Rows.Add(expected);

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DataTableJsonConverter());
            var json = JsonSerializer.Serialize(table, options);

            Assert.Contains($"\"bigint\":\"{expected.ToString(CultureInfo.InvariantCulture)}\"",
                json.Replace(" ", string.Empty), StringComparison.Ordinal);

            var restored = JsonSerializer.Deserialize<DataTable>(json, options)!;
            Assert.Equal(expected, Assert.IsType<long>(restored.Rows[0]["bigint"]));
        }

        [Fact]
        [DisplayName("舊格式的裸數字仍應讀得回（寫入端改了，讀取端保持相容）")]
        public void UnquotedNumericCell_FromEarlierRelease_StillReads()
        {
            // 4.27.0 以前（以及照當時 wire-fixtures 寫成的跨語言 client）送的就是這個形狀。
            const string json = """
                {
                  "tableName": "T",
                  "columns": [
                    { "name": "amount", "type": "Decimal", "allowNull": true, "readOnly": false,
                      "maxLength": -1, "caption": "amount", "defaultValue": null }
                  ],
                  "primaryKeys": [],
                  "rows": [ { "state": "Unchanged", "current": { "amount": 12.5 } } ]
                }
                """;

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DataTableJsonConverter());
            var restored = JsonSerializer.Deserialize<DataTable>(json, options)!;

            Assert.Equal(12.5m, Assert.IsType<decimal>(restored.Rows[0]["amount"]));
        }

        [Fact]
        [DisplayName("decimal 欄位收到非數值字串時不得靜默吞成預設值")]
        public void DecimalColumn_NonNumericText_DoesNotSilentlyBecomeDefault()
        {
            const string json = """
                {
                  "tableName": "T",
                  "columns": [
                    { "name": "amount", "type": "Decimal", "allowNull": true, "readOnly": false,
                      "maxLength": -1, "caption": "amount", "defaultValue": null }
                  ],
                  "primaryKeys": [],
                  "rows": [ { "state": "Unchanged", "current": { "amount": "not a number" } } ]
                }
                """;

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DataTableJsonConverter());

            // 解析不出來就把字串原樣往下傳，由 DataRow 對真正的欄位型別報錯 ——
            // 在這裡吞掉會把壞掉的 payload 變成一個看起來正常的 0。
            var ex = Record.Exception(() => JsonSerializer.Deserialize<DataTable>(json, options));
            Assert.NotNull(ex);
        }
}
}
