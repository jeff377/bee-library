using System.ComponentModel;
using Bee.Base.Data;

namespace Bee.Base.UnitTests
{
    public class FieldDbTypeExtensionsTests
    {
        [Theory]
        [InlineData(FieldDbType.String, "")]
        [InlineData(FieldDbType.Text, "")]
        [InlineData(FieldDbType.Boolean, false)]
        [InlineData(FieldDbType.Integer, 0)]
        [InlineData(FieldDbType.Decimal, 0)]
        [InlineData(FieldDbType.Currency, 0)]
        [DisplayName("GetDefaultValue 應為基本型別回傳對應預設值")]
        public void GetDefaultValue_ReturnsExpectedForPrimitiveTypes(FieldDbType type, object expected)
        {
            Assert.Equal(expected, type.GetDefaultValue());
        }

        [Fact]
        [DisplayName("GetDefaultValue 對 Date/DateTime/Guid 應回傳合理預設")]
        public void GetDefaultValue_ReturnsExpectedForDateAndGuidTypes()
        {
            Assert.Equal(DateTime.Today, FieldDbType.Date.GetDefaultValue());

            var now = FieldDbType.DateTime.GetDefaultValue();
            Assert.IsType<DateTime>(now);

            Assert.Equal(Guid.Empty, FieldDbType.Guid.GetDefaultValue());
        }

        [Fact]
        [DisplayName("GetDefaultValue 於未對映型別應回傳 DBNull.Value")]
        public void GetDefaultValue_UnmappedType_ReturnsDbNull()
        {
            Assert.Equal(DBNull.Value, FieldDbType.Binary.GetDefaultValue());
            Assert.Equal(DBNull.Value, FieldDbType.Unknown.GetDefaultValue());
            Assert.Equal(DBNull.Value, FieldDbType.AutoIncrement.GetDefaultValue());
            Assert.Equal(DBNull.Value, FieldDbType.Short.GetDefaultValue());
            Assert.Equal(DBNull.Value, FieldDbType.Long.GetDefaultValue());
        }

        [Fact]
        [DisplayName("ToFieldValue 依據 FieldDbType 走對應轉換分支")]
        public void ToFieldValue_VariousDbTypes_ReturnsExpectedResult()
        {
            Assert.Equal("abc", FieldDbType.String.ToFieldValue("abc"));
            Assert.Equal("abc", FieldDbType.Text.ToFieldValue("abc"));
            Assert.True((bool)FieldDbType.Boolean.ToFieldValue("1")!);
            Assert.Equal(123, FieldDbType.Integer.ToFieldValue("123"));
            Assert.Equal(123.45m, FieldDbType.Decimal.ToFieldValue("123.45"));
            Assert.Equal(123.45m, FieldDbType.Currency.ToFieldValue("123.45"));

            var date = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.Equal(date, FieldDbType.Date.ToFieldValue("2026-04-18"));
            Assert.Equal(date, FieldDbType.DateTime.ToFieldValue("2026-04-18"));

            var guid = Guid.NewGuid();
            Assert.Equal(guid, FieldDbType.Guid.ToFieldValue(guid.ToString()));

            // 未涵蓋的 FieldDbType 應原樣回傳
            var raw = new byte[] { 0x01, 0x02 };
            Assert.Same(raw, FieldDbType.Binary.ToFieldValue(raw));
        }

        [Fact]
        [DisplayName("ToDbFieldValue 對 DateTime.MinValue 回傳 DBNull.Value,其餘走 ToFieldValue")]
        public void ToDbFieldValue_DateTimeMinValue_ReturnsDbNull()
        {
            Assert.Equal(DBNull.Value, FieldDbType.DateTime.ToDbFieldValue(DateTime.MinValue));
            Assert.Equal(DBNull.Value, FieldDbType.Date.ToDbFieldValue(DateTime.MinValue));

            // 一般日期走 ToFieldValue
            var date = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.Equal(date, FieldDbType.DateTime.ToDbFieldValue(date));
            Assert.Equal("abc", FieldDbType.String.ToDbFieldValue("abc"));
        }
    }
}
