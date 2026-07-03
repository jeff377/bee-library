using System.ComponentModel;
using System.Data;

namespace Bee.Db.UnitTests
{
    public class DbTypeMapperTests
    {
        // 此測試資料需涵蓋多種 CLR 型別（string/int/DateTime/Guid/byte[] 等），
        // 故 TheoryData 僅能以 object 作為第一型別參數；xUnit1045 警告不適用於此刻意設計。
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public static TheoryData<object, DbType> Infer_Inputs() => new()
        {
            { "abc", DbType.String },
            { 1, DbType.Int32 },
            { (long)1, DbType.Int64 },
            { (short)1, DbType.Int16 },
            { (byte)1, DbType.Byte },
            { true, DbType.Boolean },
            { new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), DbType.DateTime },
            { 1.5m, DbType.Decimal },
            { 1.5d, DbType.Double },
            { 1.5f, DbType.Single },
            { Guid.NewGuid(), DbType.Guid },
            { new byte[] { 1, 2 }, DbType.Binary },
            { TimeSpan.FromSeconds(1), DbType.Time },
        };

        [Theory]
        [MemberData(nameof(Infer_Inputs))]
        [DisplayName("Infer 應依值的型別回傳對應 DbType")]
        public void Infer_KnownTypes_ReturnsExpected(object value, DbType expected)
        {
            var result = DbTypeMapper.Infer(value);
            Assert.Equal(expected, result);
        }
#pragma warning restore xUnit1045

        [Fact]
        [DisplayName("Infer 對 null 值應回傳 null")]
        public void Infer_Null_ReturnsNull()
        {
            Assert.Null(DbTypeMapper.Infer(null!));
        }

        [Fact]
        [DisplayName("Infer 對 DBNull 應回傳 null")]
        public void Infer_DBNull_ReturnsNull()
        {
            Assert.Null(DbTypeMapper.Infer(DBNull.Value));
        }

        [Fact]
        [DisplayName("Infer 對不支援型別應回傳 null")]
        public void Infer_UnsupportedType_ReturnsNull()
        {
            Assert.Null(DbTypeMapper.Infer(new object()));
        }
    }
}
