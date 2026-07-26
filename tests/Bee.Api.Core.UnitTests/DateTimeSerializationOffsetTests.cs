using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Bee.Api.Core.MessagePack;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Collections;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 釘住「序列化不得對時間值做時區偏移」這條不變式，涵蓋 MessagePack / JSON / XML 三種路徑。
    /// </summary>
    /// <remarks>
    /// 三個路徑的行為並不對稱，缺一條測試就會有一種格式無人看守：
    /// MessagePack 與 JSON 恆寫 naive 值、不受 <c>DataColumn.DateTimeMode</c> 影響；
    /// XML 是唯一會依 <c>DateTimeMode</c> 決定要不要寫出時區偏移的格式，而 .NET 的預設
    /// <c>UnspecifiedLocal</c> 正是「會寫出偏移」的那個值。偏移一旦進了 XML，跨時區讀回就會位移
    /// 甚至跨日。設計背景見 docs/adr/adr-032-datetime-timezone.md。
    ///
    /// 測試在任何時區下都必須成立（開發機多為 Asia/Taipei、CI 為 UTC），因此凡結果與本地時區
    /// 相關者，一律由 <see cref="TimeZoneInfo.Local"/> 動態推導期望值，不寫死偏移量。
    /// </remarks>
    public class DateTimeSerializationOffsetTests
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>The wall-clock value every case starts from.</summary>
        private static readonly DateTime Sample = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);

        /// <summary>
        /// A DataSet XML document whose single DateTime column carries an explicit
        /// <c>msdata:DateTimeMode</c>, so read-side behaviour can be tested per mode.
        /// </summary>
        private const string XmlTemplate =
            "<S><xs:schema id=\"S\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\">" +
            "<xs:element name=\"S\" msdata:IsDataSet=\"true\"><xs:complexType><xs:choice maxOccurs=\"unbounded\">" +
            "<xs:element name=\"T\"><xs:complexType><xs:sequence>" +
            "<xs:element name=\"d\" msdata:DateTimeMode=\"{0}\" type=\"xs:dateTime\" minOccurs=\"0\" />" +
            "</xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema>" +
            "<T><d>{1}</d></T></S>";

        private static DataTable BuildTable(DataSetDateTime mode, DateTime value)
        {
            var table = new DataTable("t");
            var column = new DataColumn("d", typeof(DateTime)) { DateTimeMode = mode };
            table.Columns.Add(column);
            table.Rows.Add(value);
            return table;
        }

        private static string WriteXml(DataTable table)
        {
            using var dataSet = new DataSet("s");
            dataSet.Tables.Add(table);
            using var writer = new StringWriter(Inv);
            dataSet.WriteXml(writer, XmlWriteMode.WriteSchema);
            return writer.ToString();
        }

        private static string ExtractXmlValue(string xml)
        {
            var match = Regex.Match(xml, "<d>([^<]+)</d>", RegexOptions.None, TimeSpan.FromSeconds(1));
            Assert.True(match.Success, "XML 未包含預期的 <d> 欄位值。");
            return match.Groups[1].Value;
        }

        private static DateTime ReadXmlValue(string mode, string wireValue)
        {
            using var dataSet = new DataSet();
            dataSet.ReadXml(new StringReader(string.Format(Inv, XmlTemplate, mode, wireValue)), XmlReadMode.ReadSchema);
            return (DateTime)dataSet.Tables[0].Rows[0]["d"];
        }

        #region DataColumn 對 Kind 的正規化

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [DisplayName("DataColumn 於 DateTimeMode=Unspecified 下將任何 Kind 正規化為 Unspecified 且不改數值")]
        public void DataColumn_UnspecifiedMode_NormalizesAnyKindWithoutShifting(DateTimeKind kind)
        {
            var table = BuildTable(DataSetDateTime.Unspecified, DateTime.SpecifyKind(Sample, kind));

            var stored = (DateTime)table.Rows[0]["d"];

            Assert.Equal(DateTimeKind.Unspecified, stored.Kind);
            Assert.Equal(Sample.TimeOfDay, stored.TimeOfDay);
            Assert.Equal(Sample.Date, stored.Date);
        }

        [Fact]
        [DisplayName("AddColumn 建立的 DateTime 欄位其 DateTimeMode 必為 Unspecified")]
        public void AddColumn_DateTimeColumns_UseUnspecifiedDateTimeMode()
        {
            var table = new DataTable("orders");
            table.AddColumn("created_at", FieldDbType.DateTime);
            table.AddColumn("order_date", FieldDbType.Date);

            Assert.Equal(DataSetDateTime.Unspecified, table.Columns["created_at"]!.DateTimeMode);
            Assert.Equal(DataSetDateTime.Unspecified, table.Columns["order_date"]!.DateTimeMode);
        }

        #endregion

        #region 三格式 round-trip 不得偏移

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [DisplayName("MessagePack round-trip 不改動 DataTable 儲存格的時間數值")]
        public void MessagePack_DataTableRoundTrip_PreservesWallClock(DateTimeKind kind)
        {
            var table = BuildTable(DataSetDateTime.Unspecified, DateTime.SpecifyKind(Sample, kind));

            var restored = MessagePackCodec.Deserialize<DataTable>(MessagePackCodec.Serialize(table));

            Assert.NotNull(restored);
            var value = (DateTime)restored.Rows[0]["d"];
            Assert.Equal(Sample, value);
            Assert.Equal(DateTimeKind.Unspecified, value.Kind);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [DisplayName("JSON round-trip 不改動 DataTable 儲存格的時間數值")]
        public void Json_DataTableRoundTrip_PreservesWallClock(DateTimeKind kind)
        {
            var table = BuildTable(DataSetDateTime.Unspecified, DateTime.SpecifyKind(Sample, kind));

            var restored = JsonCodec.Deserialize<DataTable>(JsonCodec.Serialize(table));

            Assert.NotNull(restored);
            var value = (DateTime)restored.Rows[0]["d"];
            Assert.Equal(Sample, value);
            Assert.Equal(DateTimeKind.Unspecified, value.Kind);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [DisplayName("XML round-trip 於 DateTimeMode=Unspecified 下不改動時間數值")]
        public void Xml_DataTableRoundTrip_PreservesWallClock(DateTimeKind kind)
        {
            var xml = WriteXml(BuildTable(DataSetDateTime.Unspecified, DateTime.SpecifyKind(Sample, kind)));

            using var restored = new DataSet();
            restored.ReadXml(new StringReader(xml), XmlReadMode.ReadSchema);

            var value = (DateTime)restored.Tables[0].Rows[0]["d"];
            Assert.Equal(Sample, value);
            Assert.Equal(DateTimeKind.Unspecified, value.Kind);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [DisplayName("MessagePack 與 JSON 兩條 wire 對同一儲存格得出相同結果")]
        public void MessagePackAndJson_AgreeOnCellValue(DateTimeKind kind)
        {
            var value = DateTime.SpecifyKind(Sample, kind);

            var viaMessagePack = MessagePackCodec.Deserialize<DataTable>(
                MessagePackCodec.Serialize(BuildTable(DataSetDateTime.Unspecified, value)));
            var viaJson = JsonCodec.Deserialize<DataTable>(
                JsonCodec.Serialize(BuildTable(DataSetDateTime.Unspecified, value)));

            Assert.NotNull(viaMessagePack);
            Assert.NotNull(viaJson);
            Assert.Equal((DateTime)viaMessagePack.Rows[0]["d"], (DateTime)viaJson.Rows[0]["d"]);
        }

        #endregion

        #region XML 寫出端：DateTimeMode 決定是否帶偏移

        [Fact]
        [DisplayName("XML 於 DateTimeMode=Unspecified 下寫出的值不得帶時區偏移")]
        public void Xml_UnspecifiedMode_WritesNoOffset()
        {
            var wire = ExtractXmlValue(WriteXml(BuildTable(DataSetDateTime.Unspecified, Sample)));

            Assert.Equal("2026-01-01T09:00:00", wire);
        }

        [Fact]
        [DisplayName("XML 於 .NET 預設的 UnspecifiedLocal 下會寫出時區偏移（此為必須改設 Unspecified 的理由）")]
        public void Xml_UnspecifiedLocalMode_WritesOffset()
        {
            var wire = ExtractXmlValue(WriteXml(BuildTable(DataSetDateTime.UnspecifiedLocal, Sample)));

            var expectedOffset = TimeZoneInfo.Local.GetUtcOffset(Sample);
            var expected = new DateTimeOffset(Sample, expectedOffset).ToString("yyyy-MM-ddTHH:mm:sszzz", Inv);
            Assert.Equal(expected, wire);
            Assert.NotEqual("2026-01-01T09:00:00", wire);
        }

        #endregion

        #region XML 讀入端：wire 上的偏移一律被套用

        [Theory]
        [InlineData("Unspecified")]
        [InlineData("UnspecifiedLocal")]
        [DisplayName("XML 讀入時 wire 上既有的偏移仍會被套用（Unspecified 不代表忽略偏移）")]
        public void Xml_Read_AppliesOffsetPresentOnWire(string mode)
        {
            // A payload produced by a +08:00 writer. Both Unspecified and UnspecifiedLocal convert it
            // to the reader's local time and then drop the kind, so a reader west of the writer can
            // land on the previous calendar day.
            var value = ReadXmlValue(mode, "2026-01-01T09:00:00+08:00");

            var expected = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.FromHours(8)).ToLocalTime().DateTime;
            Assert.Equal(expected, value);
            Assert.Equal(DateTimeKind.Unspecified, value.Kind);
        }

        [Theory]
        [InlineData("Unspecified")]
        [InlineData("UnspecifiedLocal")]
        [DisplayName("XML 讀入 naive 值時不做任何偏移，兩種 Unspecified 模式行為一致")]
        public void Xml_Read_NaiveValue_IsNotShifted(string mode)
        {
            var value = ReadXmlValue(mode, "2026-01-01T09:00:00");

            Assert.Equal(Sample, value);
            Assert.Equal(DateTimeKind.Unspecified, value.Kind);
        }

        #endregion

        #region 強型別屬性：Kind 在兩條 wire 上的存活差異

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [DisplayName("MessagePack typeless 對非 Local 的 DateTime 保留數值並一律標為 Utc")]
        public void MessagePack_TypelessNonLocalKind_PreservesWallClockAsUtc(DateTimeKind kind)
        {
            var original = new ParameterCollection { { "d", DateTime.SpecifyKind(Sample, kind) } };

            var restored = MessagePackCodec.Deserialize<ParameterCollection>(MessagePackCodec.Serialize(original));

            Assert.NotNull(restored);
            var value = Assert.IsType<DateTime>(restored["d"].Value);
            Assert.Equal(Sample.TimeOfDay, value.TimeOfDay);
            Assert.Equal(Sample.Date, value.Date);
            Assert.Equal(DateTimeKind.Utc, value.Kind);
        }

        [Fact]
        [DisplayName("MessagePack typeless 對 Kind=Local 的 DateTime 會把數值位移為 UTC")]
        public void MessagePack_TypelessLocalKind_ShiftsWallClockToUtc()
        {
            // The msgpack timestamp extension stores an absolute instant, so the formatter converts a
            // `Local` value to UTC on write. The instant survives but the wall-clock reading does not —
            // a receiver that treats the cell as a wall-clock value silently reads a different time.
            // This is the DTO-side counterpart to the JSON offset hazard, and the second reason D6
            // forbids `Local` on the wire.
            var original = new ParameterCollection { { "d", DateTime.SpecifyKind(Sample, DateTimeKind.Local) } };

            var restored = MessagePackCodec.Deserialize<ParameterCollection>(MessagePackCodec.Serialize(original));

            Assert.NotNull(restored);
            var value = Assert.IsType<DateTime>(restored["d"].Value);
            Assert.Equal(DateTime.SpecifyKind(Sample, DateTimeKind.Local).ToUniversalTime(), value);
            Assert.Equal(DateTimeKind.Utc, value.Kind);
        }

        [Fact]
        [DisplayName("同一個 Local 值：DataTable 路徑保住牆上時間，DTO typeless 路徑則位移為 UTC")]
        public void MessagePack_DataTableAndTypelessPaths_DisagreeOnLocalKind()
        {
            // Not a bug to fix but an asymmetry to remember: `DataColumn` normalises the kind away
            // before the formatter ever sees it, so the DataSet path cannot shift. A bare DTO
            // property has no such buffer. Guarding only one of the two paths leaves the other open.
            var local = DateTime.SpecifyKind(Sample, DateTimeKind.Local);

            var viaTable = MessagePackCodec.Deserialize<DataTable>(
                MessagePackCodec.Serialize(BuildTable(DataSetDateTime.Unspecified, local)));
            var viaTypeless = MessagePackCodec.Deserialize<ParameterCollection>(
                MessagePackCodec.Serialize(new ParameterCollection { { "d", local } }));

            Assert.NotNull(viaTable);
            Assert.NotNull(viaTypeless);
            Assert.Equal(Sample, (DateTime)viaTable.Rows[0]["d"]);
            Assert.Equal(local.ToUniversalTime(), (DateTime)viaTypeless["d"].Value!);
        }

        [Fact]
        [DisplayName("JSON 對 Kind=Local 的 DateTime 會寫出時區偏移（D6 禁止 Local 進 wire 的依據）")]
        public void Json_LocalKind_WritesOffsetOnWire()
        {
            var json = JsonCodec.Serialize(DateTime.SpecifyKind(Sample, DateTimeKind.Local));

            var expectedOffset = TimeZoneInfo.Local.GetUtcOffset(Sample);
            Assert.Contains(new DateTimeOffset(Sample, expectedOffset).ToString("zzz", Inv), json, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [DisplayName("JSON 對 Unspecified 與 Utc 的 DateTime 皆保留原數值")]
        public void Json_NonLocalKinds_PreserveWallClock(DateTimeKind kind)
        {
            var original = DateTime.SpecifyKind(Sample, kind);

            var restored = JsonCodec.Deserialize<DateTime>(JsonCodec.Serialize(original));

            Assert.Equal(Sample.TimeOfDay, restored.TimeOfDay);
            Assert.Equal(Sample.Date, restored.Date);
        }

        #endregion
    }
}
