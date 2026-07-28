using System.ComponentModel;
using Bee.Base;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Definition.UnitTests.Database
{
    /// <summary>
    /// A time of day is stored as a fixed-width string, so the database reports it as a 5-length
    /// string and never as <see cref="FieldDbType.Time"/>. <see cref="DbField.Compare"/> must reduce
    /// both sides to that physical shape, or every comparison would report drift and re-issue an
    /// ALTER forever (ADR-033).
    /// </summary>
    public class DbFieldTimeComparisonTests
    {
        private static DbField Field(FieldDbType dbType, int length = 0) =>
            new() { FieldName = "work_start", Caption = "Start", DbType = dbType, Length = length };

        [Fact]
        [DisplayName("定義為 Time、DB 反推為 String(5) 應視為相同，不產生 schema diff")]
        public void Compare_TimeAgainstFiveLengthString_ReportsNoDifference()
        {
            var defined = Field(FieldDbType.Time);
            var actual = Field(FieldDbType.String, ValueUtilities.TimeOnlyLength);

            Assert.True(defined.Compare(actual));
        }

        [Fact]
        [DisplayName("兩側皆為 Time 應視為相同")]
        public void Compare_TimeAgainstTime_ReportsNoDifference()
        {
            Assert.True(Field(FieldDbType.Time).Compare(Field(FieldDbType.Time)));
        }

        [Fact]
        [DisplayName("定義為 Time、DB 為較寬的 String 應判定為差異並觸發升級")]
        public void Compare_TimeAgainstWiderString_ReportsDifference()
        {
            var defined = Field(FieldDbType.Time);
            var actual = Field(FieldDbType.String, 50);

            Assert.False(defined.Compare(actual));
        }

        [Fact]
        [DisplayName("Time 與非字串型別仍應判定為差異")]
        public void Compare_TimeAgainstNonString_ReportsDifference()
        {
            Assert.False(Field(FieldDbType.Time).Compare(Field(FieldDbType.DateTime)));
            Assert.False(Field(FieldDbType.Time).Compare(Field(FieldDbType.Integer)));
        }

        [Fact]
        [DisplayName("既有 String 欄位長度比對行為不受影響")]
        public void Compare_StringLengths_Unchanged()
        {
            Assert.True(Field(FieldDbType.String, 50).Compare(Field(FieldDbType.String, 50)));
            Assert.False(Field(FieldDbType.String, 50).Compare(Field(FieldDbType.String, 20)));
        }
    }
}
