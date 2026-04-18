using System.ComponentModel;
using System.Data;
using Bee.Base.Data;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// DbTypeConverter 補強測試：涵蓋 ToTypeCode 的 Nullable/ByRef 路徑與
    /// ToDbType 的完整 FieldDbType→DbType 對應。
    /// </summary>
    public class DbTypeConverterExtraTests
    {
        [Theory]
        [InlineData(typeof(int), TypeCode.Int32)]
        [InlineData(typeof(string), TypeCode.String)]
        [InlineData(typeof(DateTime), TypeCode.DateTime)]
        [DisplayName("ToTypeCode 應回傳非 Nullable 型別的 TypeCode")]
        public void ToTypeCode_NonNullable_ReturnsCorrectTypeCode(Type type, TypeCode expected)
        {
            Assert.Equal(expected, DbTypeConverter.ToTypeCode(type));
        }

        [Theory]
        [InlineData(typeof(int?), TypeCode.Int32)]
        [InlineData(typeof(DateTime?), TypeCode.DateTime)]
        [InlineData(typeof(decimal?), TypeCode.Decimal)]
        [DisplayName("ToTypeCode 於 Nullable<T> 應回傳底層型別的 TypeCode")]
        public void ToTypeCode_Nullable_UnwrapsAndReturnsInnerTypeCode(Type type, TypeCode expected)
        {
            Assert.Equal(expected, DbTypeConverter.ToTypeCode(type));
        }

        [Fact]
        [DisplayName("ToTypeCode 於 ByRef 型別應解包並回傳元素 TypeCode")]
        public void ToTypeCode_ByRef_UnwrapsAndReturnsElementTypeCode()
        {
            var byRefType = typeof(int).MakeByRefType();
            Assert.Equal(TypeCode.Int32, DbTypeConverter.ToTypeCode(byRefType));
        }

        [Theory]
        [InlineData(FieldDbType.String, DbType.String)]
        [InlineData(FieldDbType.Text, DbType.String)]
        [InlineData(FieldDbType.Boolean, DbType.Boolean)]
        [InlineData(FieldDbType.AutoIncrement, DbType.Int32)]
        [InlineData(FieldDbType.Integer, DbType.Int32)]
        [InlineData(FieldDbType.Short, DbType.Int16)]
        [InlineData(FieldDbType.Long, DbType.Int64)]
        [InlineData(FieldDbType.Decimal, DbType.Decimal)]
        [InlineData(FieldDbType.Currency, DbType.Currency)]
        [InlineData(FieldDbType.Date, DbType.Date)]
        [InlineData(FieldDbType.DateTime, DbType.DateTime)]
        [InlineData(FieldDbType.Guid, DbType.Guid)]
        [InlineData(FieldDbType.Binary, DbType.Binary)]
        [DisplayName("ToDbType 應回傳正確的 DbType")]
        public void ToDbType_AllFieldDbTypes_ReturnsCorrectDbType(FieldDbType fieldDbType, DbType expected)
        {
            Assert.Equal(expected, DbTypeConverter.ToDbType(fieldDbType));
        }

        [Fact]
        [DisplayName("ToDbType 於 Unknown 應拋出 ArgumentOutOfRangeException")]
        public void ToDbType_Unknown_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => DbTypeConverter.ToDbType(FieldDbType.Unknown));
        }
    }
}
