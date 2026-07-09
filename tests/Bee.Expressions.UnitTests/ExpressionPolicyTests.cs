using System.ComponentModel;
using Bee.Base.Data;

namespace Bee.Expressions.UnitTests
{
    /// <summary>
    /// <see cref="ExpressionPolicy"/> 測試：FieldDbType → CLR 型別對映，以及
    /// DBNull/null → 型別預設值的一致 coercion。
    /// </summary>
    public class ExpressionPolicyTests
    {
        [Theory]
        [InlineData(FieldDbType.String, typeof(string))]
        [InlineData(FieldDbType.Integer, typeof(int))]
        [InlineData(FieldDbType.Long, typeof(long))]
        [InlineData(FieldDbType.Decimal, typeof(decimal))]
        [InlineData(FieldDbType.Currency, typeof(decimal))]
        [InlineData(FieldDbType.Boolean, typeof(bool))]
        [InlineData(FieldDbType.DateTime, typeof(DateTime))]
        [InlineData(FieldDbType.Guid, typeof(Guid))]
        [DisplayName("ToClrType 對映各 FieldDbType 至對應 CLR 型別")]
        public void ToClrType_MapsFieldDbTypeToClrType(FieldDbType dbType, Type expected)
        {
            Assert.Equal(expected, ExpressionPolicy.ToClrType(dbType));
        }

        [Fact]
        [DisplayName("CoerceValue：DBNull 數值欄回傳 0（decimal）")]
        public void CoerceValue_DbNullCurrency_ReturnsZero()
        {
            var result = ExpressionPolicy.CoerceValue(DBNull.Value, FieldDbType.Currency);

            Assert.Equal(0m, result);
        }

        [Fact]
        [DisplayName("CoerceValue：DBNull 字串欄回傳空字串")]
        public void CoerceValue_DbNullString_ReturnsEmpty()
        {
            var result = ExpressionPolicy.CoerceValue(DBNull.Value, FieldDbType.String);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("CoerceValue：null 布林欄回傳 false")]
        public void CoerceValue_NullBoolean_ReturnsFalse()
        {
            var result = ExpressionPolicy.CoerceValue(null, FieldDbType.Boolean);

            Assert.Equal(false, result);
        }

        [Fact]
        [DisplayName("CoerceValue：DBNull Guid 欄回傳 Guid.Empty")]
        public void CoerceValue_DbNullGuid_ReturnsEmptyGuid()
        {
            var result = ExpressionPolicy.CoerceValue(DBNull.Value, FieldDbType.Guid);

            Assert.Equal(Guid.Empty, result);
        }

        [Fact]
        [DisplayName("CoerceValue：型別相符時原值返回")]
        public void CoerceValue_MatchingType_ReturnsSameValue()
        {
            var result = ExpressionPolicy.CoerceValue(12.5m, FieldDbType.Currency);

            Assert.Equal(12.5m, result);
        }

        [Fact]
        [DisplayName("CoerceValue：int 值餵入 decimal 欄應轉為 decimal")]
        public void CoerceValue_IntToDecimal_Converts()
        {
            var result = ExpressionPolicy.CoerceValue(5, FieldDbType.Decimal);

            Assert.IsType<decimal>(result);
            Assert.Equal(5m, result);
        }

        [Fact]
        [DisplayName("CoerceValue：string 值餵入 Guid 欄應解析為 Guid（wire/SQLite 把 GUID 存為 TEXT）")]
        public void CoerceValue_StringToGuid_Parses()
        {
            var id = Guid.NewGuid();

            var result = ExpressionPolicy.CoerceValue(id.ToString(), FieldDbType.Guid);

            Assert.IsType<Guid>(result);
            Assert.Equal(id, result);
        }

        [Fact]
        [DisplayName("CoerceValue：Guid 值餵入 Guid 欄原值返回")]
        public void CoerceValue_GuidToGuid_ReturnsSameValue()
        {
            var id = Guid.NewGuid();

            var result = ExpressionPolicy.CoerceValue(id, FieldDbType.Guid);

            Assert.Equal(id, result);
        }

        [Fact]
        [DisplayName("CoerceValue：base64 string 值餵入 Binary 欄應解為 byte[]")]
        public void CoerceValue_Base64ToBinary_Decodes()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };

            var result = ExpressionPolicy.CoerceValue(Convert.ToBase64String(bytes), FieldDbType.Binary);

            Assert.Equal(bytes, Assert.IsType<byte[]>(result));
        }
    }
}
