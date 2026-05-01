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
    }
}
