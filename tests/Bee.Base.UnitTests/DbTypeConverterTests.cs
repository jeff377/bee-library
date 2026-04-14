using System.ComponentModel;
using Bee.Base.Data;

namespace Bee.Base.UnitTests
{
    public class DbTypeConverterTests
    {
        [Theory]
        [InlineData(FieldDbType.String, typeof(string))]
        [InlineData(FieldDbType.Text, typeof(string))]
        [InlineData(FieldDbType.Boolean, typeof(bool))]
        [InlineData(FieldDbType.AutoIncrement, typeof(int))]
        [InlineData(FieldDbType.Short, typeof(short))]
        [InlineData(FieldDbType.Integer, typeof(int))]
        [InlineData(FieldDbType.Long, typeof(long))]
        [InlineData(FieldDbType.Decimal, typeof(decimal))]
        [InlineData(FieldDbType.Currency, typeof(decimal))]
        [InlineData(FieldDbType.Date, typeof(DateTime))]
        [InlineData(FieldDbType.DateTime, typeof(DateTime))]
        [InlineData(FieldDbType.Guid, typeof(Guid))]
        [InlineData(FieldDbType.Binary, typeof(byte[]))]
        [DisplayName("ToType 應回傳正確的 CLR 型別")]
        public void ToType_AllFieldDbTypes_ReturnsCorrectClrType(FieldDbType fieldDbType, Type expectedType)
        {
            var result = DbTypeConverter.ToType(fieldDbType);
            Assert.Equal(expectedType, result);
        }

        [Theory]
        [InlineData(typeof(string), FieldDbType.String)]
        [InlineData(typeof(bool), FieldDbType.Boolean)]
        [InlineData(typeof(short), FieldDbType.Short)]
        [InlineData(typeof(int), FieldDbType.Integer)]
        [InlineData(typeof(long), FieldDbType.Long)]
        [InlineData(typeof(decimal), FieldDbType.Decimal)]
        [InlineData(typeof(DateTime), FieldDbType.DateTime)]
        [InlineData(typeof(Guid), FieldDbType.Guid)]
        [InlineData(typeof(byte[]), FieldDbType.Binary)]
        [DisplayName("ToFieldDbType 應回傳正確的 FieldDbType")]
        public void ToFieldDbType_SupportedTypes_ReturnsCorrectFieldDbType(Type clrType, FieldDbType expectedFieldDbType)
        {
            var result = DbTypeConverter.ToFieldDbType(clrType);
            Assert.Equal(expectedFieldDbType, result);
        }

        [Theory]
        [InlineData(typeof(double), FieldDbType.Decimal)]
        [InlineData(typeof(float), FieldDbType.Decimal)]
        [InlineData(typeof(char), FieldDbType.String)]
        [InlineData(typeof(ushort), FieldDbType.Short)]
        [InlineData(typeof(uint), FieldDbType.Integer)]
        [InlineData(typeof(ulong), FieldDbType.Long)]
        [DisplayName("ToFieldDbType 應正確處理相容的 CLR 型別")]
        public void ToFieldDbType_CompatibleTypes_ReturnsExpectedFieldDbType(Type clrType, FieldDbType expectedFieldDbType)
        {
            var result = DbTypeConverter.ToFieldDbType(clrType);
            Assert.Equal(expectedFieldDbType, result);
        }

        [Fact]
        [DisplayName("ToFieldDbType 不支援的型別應拋出 InvalidOperationException")]
        public void ToFieldDbType_UnsupportedType_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DbTypeConverter.ToFieldDbType(typeof(object)));
        }

        [Fact]
        [DisplayName("ToType Unknown 應拋出 InvalidOperationException")]
        public void ToType_Unknown_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DbTypeConverter.ToType(FieldDbType.Unknown));
        }
    }
}
