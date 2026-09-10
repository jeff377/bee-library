using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.LoadTests.Bootstrap;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for the value generation in <see cref="DataSeeder"/>.
    /// </summary>
    public class DataSeederTests
    {
        private static DbField Field(FieldDbType type, string name = "col", int length = 0, int scale = 0)
            => new() { FieldName = name, DbType = type, Length = length, Scale = scale };

        [Fact]
        [DisplayName("同一欄位與列號永遠產生同一個值，兩次 seed 資料一致")]
        public void CreateValue_IsDeterministic()
        {
            var field = Field(FieldDbType.Guid, "sys_rowid");

            Assert.Equal(DataSeeder.CreateValue(field, 7), DataSeeder.CreateValue(field, 7));
        }

        [Fact]
        [DisplayName("不同列號產生不同的 GUID")]
        public void CreateValue_Guid_DiffersByRow()
        {
            var field = Field(FieldDbType.Guid, "sys_rowid");

            Assert.NotEqual(DataSeeder.CreateValue(field, 1), DataSeeder.CreateValue(field, 2));
        }

        [Fact]
        [DisplayName("同列不同欄的 GUID 不相同")]
        public void CreateValue_Guid_DiffersByColumn()
        {
            Assert.NotEqual(
                DataSeeder.CreateValue(Field(FieldDbType.Guid, "sys_rowid"), 5),
                DataSeeder.CreateValue(Field(FieldDbType.Guid, "owner_rowid"), 5));
        }

        [Fact]
        [DisplayName("字串截到欄位宣告長度，避免寫入被拒")]
        public void CreateValue_String_TruncatesToDeclaredLength()
        {
            var value = Assert.IsType<string>(
                DataSeeder.CreateValue(Field(FieldDbType.String, "customer_name", length: 6), 12345));

            Assert.Equal(6, value.Length);
        }

        [Fact]
        [DisplayName("字串未超長時保留完整內容")]
        public void CreateValue_String_KeepsShortValue()
        {
            var value = Assert.IsType<string>(
                DataSeeder.CreateValue(Field(FieldDbType.String, "sys_id", length: 50), 3));

            Assert.Equal("sys_id-3", value);
        }

        [Fact]
        [DisplayName("Length 為 0 時不截斷")]
        public void CreateValue_String_ZeroLengthMeansNoLimit()
        {
            var value = Assert.IsType<string>(
                DataSeeder.CreateValue(Field(FieldDbType.String, "note"), 42));

            Assert.Equal("note-42", value);
        }

        [Theory]
        [InlineData(0, 2)]   // 1.5 rounds away from zero at scale 0.
        [InlineData(2, 1.5)]
        [DisplayName("小數捨入至欄位 Scale，值寫入後不被引擎改動")]
        public void CreateValue_Decimal_RoundsToScale(int scale, double expected)
        {
            var value = Assert.IsType<decimal>(
                DataSeeder.CreateValue(Field(FieldDbType.Decimal, "amount", scale: scale), 1));

            Assert.Equal((decimal)expected, value);
        }

        [Fact]
        [DisplayName("布林值逐列交替")]
        public void CreateValue_Boolean_Alternates()
        {
            var field = Field(FieldDbType.Boolean, "is_active");

            Assert.True(Assert.IsType<bool>(DataSeeder.CreateValue(field, 0)));
            Assert.False(Assert.IsType<bool>(DataSeeder.CreateValue(field, 1)));
        }

        [Fact]
        [DisplayName("日期型別帶 UTC Kind，不受本機時區影響")]
        public void CreateValue_DateTime_IsUtc()
        {
            var value = Assert.IsType<DateTime>(
                DataSeeder.CreateValue(Field(FieldDbType.DateTime, "created_at"), 10));

            Assert.Equal(DateTimeKind.Utc, value.Kind);
        }

        [Theory]
        [InlineData(FieldDbType.Short)]
        [InlineData(FieldDbType.Integer)]
        [InlineData(FieldDbType.Long)]
        [DisplayName("整數型別回傳對應的 CLR 型別")]
        public void CreateValue_IntegerFamily_ReturnsMatchingClrType(FieldDbType type)
        {
            var value = DataSeeder.CreateValue(Field(type, "n"), 5);

            Assert.Equal(type switch
            {
                FieldDbType.Short => typeof(short),
                FieldDbType.Integer => typeof(int),
                _ => typeof(long)
            }, value.GetType());
        }

        [Fact]
        [DisplayName("Short 不會因列號超過上限而溢位")]
        public void CreateValue_Short_DoesNotOverflow()
        {
            var value = Assert.IsType<short>(
                DataSeeder.CreateValue(Field(FieldDbType.Short, "n"), short.MaxValue + 10));

            Assert.True(value >= 0);
        }

        [Fact]
        [DisplayName("Unknown 型別回傳空字串而非擲例外")]
        public void CreateValue_Unknown_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, DataSeeder.CreateValue(Field(FieldDbType.Unknown, "x"), 1));
        }
    }
}
