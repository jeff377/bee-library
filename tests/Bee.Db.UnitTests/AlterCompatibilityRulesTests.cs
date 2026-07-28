using System.ComponentModel;
using Bee.Base;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 純規則測試：覆蓋 <see cref="AlterCompatibilityRules"/> 的型別家族分類與 narrowing 判斷。
    /// 這些規則不碰任何 SQL 語法，五個 provider 共用同一份實作，故只測一次。
    /// SQLite 覆寫的 <c>GetKindForTypeChange</c> 另見 <c>SqliteAlterCompatibilityRulesTests</c>。
    /// </summary>
    public class AlterCompatibilityRulesTests
    {
        #region GetKindForTypeChange

        [Theory]
        [InlineData(FieldDbType.String, FieldDbType.String)]
        [InlineData(FieldDbType.Short, FieldDbType.Short)]
        [InlineData(FieldDbType.Integer, FieldDbType.Integer)]
        [InlineData(FieldDbType.Decimal, FieldDbType.Decimal)]
        [InlineData(FieldDbType.Time, FieldDbType.Time)]
        [InlineData(FieldDbType.AutoIncrement, FieldDbType.AutoIncrement)]
        [DisplayName("GetKindForTypeChange：同型別應為 Alter")]
        public void GetKindForTypeChange_SameType_ReturnsAlter(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.Alter, AlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Theory]
        [InlineData(FieldDbType.String, FieldDbType.Text)]
        [InlineData(FieldDbType.Text, FieldDbType.String)]
        [InlineData(FieldDbType.String, FieldDbType.Time)]
        [InlineData(FieldDbType.Time, FieldDbType.String)]
        [InlineData(FieldDbType.Short, FieldDbType.Integer)]
        [InlineData(FieldDbType.Integer, FieldDbType.Long)]
        [InlineData(FieldDbType.Long, FieldDbType.Decimal)]
        [InlineData(FieldDbType.Decimal, FieldDbType.Currency)]
        [InlineData(FieldDbType.Date, FieldDbType.DateTime)]
        [InlineData(FieldDbType.DateTime, FieldDbType.Date)]
        [DisplayName("GetKindForTypeChange：同 family 應為 Alter")]
        public void GetKindForTypeChange_SameFamily_ReturnsAlter(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.Alter, AlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Theory]
        [InlineData(FieldDbType.String, FieldDbType.Integer)]
        [InlineData(FieldDbType.String, FieldDbType.Date)]
        [InlineData(FieldDbType.Integer, FieldDbType.DateTime)]
        [InlineData(FieldDbType.Boolean, FieldDbType.Integer)]
        [InlineData(FieldDbType.Binary, FieldDbType.String)]
        [InlineData(FieldDbType.Guid, FieldDbType.String)]
        [InlineData(FieldDbType.Time, FieldDbType.DateTime)]
        [DisplayName("GetKindForTypeChange：跨 family 應為 Rebuild")]
        public void GetKindForTypeChange_CrossFamily_ReturnsRebuild(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.Rebuild, AlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Theory]
        [InlineData(FieldDbType.Integer, FieldDbType.AutoIncrement)]
        [InlineData(FieldDbType.AutoIncrement, FieldDbType.Integer)]
        [InlineData(FieldDbType.AutoIncrement, FieldDbType.Long)]
        [DisplayName("GetKindForTypeChange：AutoIncrement 狀態變更應為 Rebuild")]
        public void GetKindForTypeChange_AutoIncrementToggle_ReturnsRebuild(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.Rebuild, AlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Theory]
        [InlineData(FieldDbType.Unknown, FieldDbType.String)]
        [InlineData(FieldDbType.String, FieldDbType.Unknown)]
        [InlineData(FieldDbType.Unknown, FieldDbType.Unknown)]
        [DisplayName("GetKindForTypeChange：Unknown 應為 NotSupported")]
        public void GetKindForTypeChange_UnknownType_ReturnsNotSupported(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.NotSupported, AlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        #endregion

        #region IsNarrowing — 字串容量

        [Fact]
        [DisplayName("IsNarrowing：String 長度縮小應判定為 narrowing")]
        public void IsNarrowing_StringLengthReduced_ReturnsTrue()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };

            Assert.True(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：String 長度放大不是 narrowing")]
        public void IsNarrowing_StringLengthIncreased_ReturnsFalse()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };

            Assert.False(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Text 轉 String（有長度上限）應判定為 narrowing")]
        public void IsNarrowing_TextToString_ReturnsTrue()
        {
            var oldField = new DbField("note", "Note", FieldDbType.Text);
            var newField = new DbField("note", "Note", FieldDbType.String) { Length = 200 };

            Assert.True(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：String 轉 Text 不是 narrowing")]
        public void IsNarrowing_StringToText_ReturnsFalse()
        {
            var oldField = new DbField("note", "Note", FieldDbType.String) { Length = 200 };
            var newField = new DbField("note", "Note", FieldDbType.Text);

            Assert.False(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：長度大於時刻字面長度的 String 轉 Time 應判定為 narrowing")]
        public void IsNarrowing_WiderStringToTime_ReturnsTrue()
        {
            var oldField = new DbField("t", "T", FieldDbType.String) { Length = ValueUtilities.TimeOnlyLength + 10 };
            var newField = new DbField("t", "T", FieldDbType.Time);

            Assert.True(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Time 轉 Text 不是 narrowing")]
        public void IsNarrowing_TimeToText_ReturnsFalse()
        {
            var oldField = new DbField("t", "T", FieldDbType.Time);
            var newField = new DbField("t", "T", FieldDbType.Text);

            Assert.False(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        #endregion

        #region IsNarrowing — 數值

        [Theory]
        [InlineData(FieldDbType.Long, FieldDbType.Integer)]
        [InlineData(FieldDbType.Integer, FieldDbType.Short)]
        [InlineData(FieldDbType.Long, FieldDbType.Short)]
        [InlineData(FieldDbType.Decimal, FieldDbType.Integer)]
        [InlineData(FieldDbType.Currency, FieldDbType.Long)]
        [DisplayName("IsNarrowing：數值型縮小應判定為 narrowing")]
        public void IsNarrowing_NumericRankReduced_ReturnsTrue(FieldDbType from, FieldDbType to)
        {
            var oldField = new DbField("v", "V", from);
            var newField = new DbField("v", "V", to);

            Assert.True(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Theory]
        [InlineData(FieldDbType.Short, FieldDbType.Integer)]
        [InlineData(FieldDbType.Integer, FieldDbType.Long)]
        [InlineData(FieldDbType.Integer, FieldDbType.Decimal)]
        [DisplayName("IsNarrowing：數值型放大不是 narrowing")]
        public void IsNarrowing_NumericRankIncreased_ReturnsFalse(FieldDbType from, FieldDbType to)
        {
            var oldField = new DbField("v", "V", from);
            var newField = new DbField("v", "V", to);

            Assert.False(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Decimal precision 縮小應判定為 narrowing")]
        public void IsNarrowing_DecimalPrecisionReduced_ReturnsTrue()
        {
            var oldField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 2 };
            var newField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 10, Scale = 2 };

            Assert.True(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Decimal scale 縮小應判定為 narrowing")]
        public void IsNarrowing_DecimalScaleReduced_ReturnsTrue()
        {
            var oldField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 4 };
            var newField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 2 };

            Assert.True(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Decimal precision/scale 維持不是 narrowing")]
        public void IsNarrowing_DecimalSamePrecisionScale_ReturnsFalse()
        {
            var oldField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 4 };
            var newField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 4 };

            Assert.False(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        #endregion

        #region IsNarrowing — 日期時間

        [Fact]
        [DisplayName("IsNarrowing：DateTime 轉 Date 應判定為 narrowing（時間精度遺失）")]
        public void IsNarrowing_DateTimeToDate_ReturnsTrue()
        {
            var oldField = new DbField("dt", "Dt", FieldDbType.DateTime);
            var newField = new DbField("dt", "Dt", FieldDbType.Date);

            Assert.True(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Date 轉 DateTime 不是 narrowing")]
        public void IsNarrowing_DateToDateTime_ReturnsFalse()
        {
            var oldField = new DbField("dt", "Dt", FieldDbType.Date);
            var newField = new DbField("dt", "Dt", FieldDbType.DateTime);

            Assert.False(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        #endregion

        #region IsNarrowing — 跨家族

        [Fact]
        [DisplayName("IsNarrowing：跨家族變更（String → Integer）不觸發 narrowing 判斷，回傳 false")]
        public void IsNarrowing_CrossFamily_ReturnsFalse()
        {
            var oldField = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            var newField = new DbField("v", "V", FieldDbType.Integer);

            Assert.False(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Boolean 與 Boolean 不在 narrowing 判斷範圍，回傳 false")]
        public void IsNarrowing_BooleanToBoolean_ReturnsFalse()
        {
            var oldField = new DbField("v", "V", FieldDbType.Boolean);
            var newField = new DbField("v", "V", FieldDbType.Boolean);

            Assert.False(AlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        #endregion
    }
}
