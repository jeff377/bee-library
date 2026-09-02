using System.Buffers;
using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Wire;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 把 <c>WireValueCode</c> 的判別碼釘死在 wire 上。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這些數值**是 wire 格式的一部分**：重新編號會讓新舊版本對同一串位元組解讀出不同型別，
    /// 而那是**靜默**的——`WireContractDriftTests` 比對的是「型別有沒有註冊」，不看編號；
    /// round-trip 測試在同一個行程內編碼與解碼，兩邊一起改也一樣會過。
    /// </para>
    /// <para>
    /// 因此本檔測兩層：常數表本身（改動即紅），以及**實際編出來的位元組**（連同封套框架一起釘）。
    /// 只釘常數不夠——有人若把常數與測試表一起改，仍會靜默通過；連編碼一起釘，則那次改動必須
    /// 同時改動 golden 值，讀起來就是一次明示的 wire 變更。
    /// </para>
    /// </remarks>
    public class WireValueCodePinTests
    {
        /// <summary>
        /// 判別碼的權威表。**新增型別只能往後接號，既有的一律不得改動。**
        /// </summary>
        public static TheoryData<int, int, string> PinnedCodes => new()
        {
            { WireValueCode.Boolean,        1, nameof(WireValueCode.Boolean) },
            { WireValueCode.Byte,           2, nameof(WireValueCode.Byte) },
            { WireValueCode.SByte,          3, nameof(WireValueCode.SByte) },
            { WireValueCode.Int16,          4, nameof(WireValueCode.Int16) },
            { WireValueCode.UInt16,         5, nameof(WireValueCode.UInt16) },
            { WireValueCode.Int32,          6, nameof(WireValueCode.Int32) },
            { WireValueCode.UInt32,         7, nameof(WireValueCode.UInt32) },
            { WireValueCode.Int64,          8, nameof(WireValueCode.Int64) },
            { WireValueCode.UInt64,         9, nameof(WireValueCode.UInt64) },
            { WireValueCode.Single,        10, nameof(WireValueCode.Single) },
            { WireValueCode.Double,        11, nameof(WireValueCode.Double) },
            { WireValueCode.Decimal,       12, nameof(WireValueCode.Decimal) },
            { WireValueCode.String,        13, nameof(WireValueCode.String) },
            { WireValueCode.DateTime,      14, nameof(WireValueCode.DateTime) },
            { WireValueCode.DateTimeOffset, 15, nameof(WireValueCode.DateTimeOffset) },
            { WireValueCode.TimeSpan,      16, nameof(WireValueCode.TimeSpan) },
            { WireValueCode.DateOnly,      17, nameof(WireValueCode.DateOnly) },
            { WireValueCode.Guid,          18, nameof(WireValueCode.Guid) },
            { WireValueCode.ByteArray,     19, nameof(WireValueCode.ByteArray) },
            { WireValueCode.DBNull,        20, nameof(WireValueCode.DBNull) },
            { WireValueCode.DataTable,     21, nameof(WireValueCode.DataTable) },
            { WireValueCode.ObjectArray,   22, nameof(WireValueCode.ObjectArray) },
        };

        [Theory]
        [MemberData(nameof(PinnedCodes))]
        [DisplayName("WireValueCode 的每個判別碼都必須維持原值")]
        public void Code_KeepsItsPinnedValue(int actual, int expected, string name)
        {
            Assert.True(
                actual == expected,
                $"WireValueCode.{name} 由 {expected} 變成 {actual}。判別碼是 wire 格式的一部分，" +
                "改動會讓新舊版本對同一串位元組解讀出不同型別。新增型別請往後接號。");
        }

        [Fact]
        [DisplayName("Count 必須恰好是最高判別碼加一（派發表大小的依據）")]
        public void Count_IsOnePastTheHighestCode()
        {
            var highest = PinnedCodes.Select(row => (int)row[1]).Max();

            Assert.Equal(WireValueCode.Count, highest + 1);
        }

        /// <summary>
        /// 每個判別碼配一個樣本值，用來驗證**實際編出來的位元組**。
        /// </summary>
        /// <remarks>
        /// WARNING: 期望值寫**字面數字**，不可改用 <c>WireValueCode.X</c> —— 用常數的話，
        /// 重新編號時期望值會跟著一起變，測試自我一致因而恆綠。第一版就是這樣寫的，
        /// 在反向驗證（對調 Guid 與 ByteArray）時只有常數表那個測試紅，本測試全綠。
        /// </remarks>
        public static TheoryData<object, int> SampleValues => new()
        {
            { true,                                     1 },
            { (byte)8,                                  2 },
            { (sbyte)-8,                                3 },
            { (short)-16,                               4 },
            { (ushort)16,                               5 },
            { -32,                                      6 },
            { 32u,                                      7 },
            { -64L,                                     8 },
            { 64UL,                                     9 },
            { 1.5f,                                     10 },
            { 2.5d,                                     11 },
            { 99.99m,                                   12 },
            { "text",                                   13 },
            { new DateTime(2026, 8, 12, 1, 2, 3, DateTimeKind.Utc), 14 },
            { new DateTimeOffset(2026, 8, 12, 1, 2, 3, TimeSpan.FromHours(8)), 15 },
            { TimeSpan.FromMinutes(90),                 16 },
            { new DateOnly(2026, 8, 12),                17 },
            { Guid.Parse("11112222-3333-4444-5555-666677778888"), 18 },
            { new byte[] { 1, 2, 3 },                   19 },
            { DBNull.Value,                             20 },
            { new object[] { 1, "two" },                22 },
        };

        [Theory]
        // 樣本值刻意是 object（判別碼本來就是為異質值而設），無法在探索期序列化成個別 data row，
        // 故關閉探索期列舉（xUnit1045）；測試仍照跑，只是 Test Explorer 顯示為單一項目。
        [MemberData(nameof(SampleValues), DisableDiscoveryEnumeration = true)]
        [DisplayName("封套實際寫出的第一個元素就是該型別的判別碼")]
        public void Envelope_WritesTheExpectedDiscriminator(object value, int expectedCode)
        {
            var bytes = SerializeValue(value);
            var reader = new MessagePackReader(new ReadOnlySequence<byte>(bytes));

            // 封套是「兩元素陣列：判別碼 + 值」。連陣列標頭一起斷言，框架本身改了也會紅。
            Assert.Equal(2, reader.ReadArrayHeader());
            Assert.Equal(expectedCode, reader.ReadInt32());
        }

        [Fact]
        [DisplayName("DataTable 值也走同一個封套，判別碼為 21")]
        public void Envelope_DataTable_WritesItsDiscriminator()
        {
            var table = new DataTable("t");
            table.Columns.Add("c", typeof(int));
            table.Rows.Add(1);

            var bytes = SerializeValue(table);
            var reader = new MessagePackReader(new ReadOnlySequence<byte>(bytes));

            Assert.Equal(2, reader.ReadArrayHeader());
            Assert.Equal(21, reader.ReadInt32());
        }

        [Fact]
        [DisplayName("DateTimeOffset 應可 round-trip 並保留時間偏移")]
        public void DateTimeOffset_RoundTrips_PreservingOffset()
        {
            // 判別碼 15 先前是唯一沒有 round-trip 測試的分支。
            var value = new DateTimeOffset(2026, 8, 12, 1, 2, 3, TimeSpan.FromHours(8));

            var source = new ParameterCollection { { "v", value } };
            var restored = MessagePackCodec.Deserialize<ParameterCollection>(MessagePackCodec.Serialize(source));

            var actual = Assert.IsType<DateTimeOffset>(restored!["v"].Value);
            Assert.Equal(value, actual);
            Assert.Equal(TimeSpan.FromHours(8), actual.Offset);
        }

        /// <summary>
        /// 取出單一值經 <c>WireValueFormatter</c> 寫出的原始位元組。
        /// </summary>
        /// <remarks>
        /// 走 <c>ParameterCollection</c> 再切位元組會混入外層封套，故直接呼叫 formatter；
        /// options 取自 <c>MessagePackCodec</c>，才與正式路徑用的是同一組 resolver。
        /// </remarks>
        private static byte[] SerializeValue(object value)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(buffer);
            WireValueFormatter.Instance.Serialize(ref writer, value, MessagePackCodec.SerializerOptions);
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }
    }
}
