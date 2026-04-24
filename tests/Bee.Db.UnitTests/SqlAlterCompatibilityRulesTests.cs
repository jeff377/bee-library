using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqlAlterCompatibilityRulesTests
    {
        [Theory]
        [InlineData(FieldDbType.String, FieldDbType.String)]
        [InlineData(FieldDbType.Short, FieldDbType.Short)]
        [InlineData(FieldDbType.Integer, FieldDbType.Integer)]
        [InlineData(FieldDbType.Decimal, FieldDbType.Decimal)]
        [InlineData(FieldDbType.AutoIncrement, FieldDbType.AutoIncrement)]
        [DisplayName("GetKindForTypeChange：同型別應為 Alter")]
        public void GetKindForTypeChange_SameType_ReturnsAlter(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.Alter, SqlAlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Theory]
        [InlineData(FieldDbType.String, FieldDbType.Text)]
        [InlineData(FieldDbType.Text, FieldDbType.String)]
        [InlineData(FieldDbType.Short, FieldDbType.Integer)]
        [InlineData(FieldDbType.Integer, FieldDbType.Long)]
        [InlineData(FieldDbType.Long, FieldDbType.Decimal)]
        [InlineData(FieldDbType.Decimal, FieldDbType.Currency)]
        [InlineData(FieldDbType.Date, FieldDbType.DateTime)]
        [InlineData(FieldDbType.DateTime, FieldDbType.Date)]
        [DisplayName("GetKindForTypeChange：同 family 應為 Alter")]
        public void GetKindForTypeChange_SameFamily_ReturnsAlter(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.Alter, SqlAlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Theory]
        [InlineData(FieldDbType.String, FieldDbType.Integer)]
        [InlineData(FieldDbType.String, FieldDbType.Date)]
        [InlineData(FieldDbType.Integer, FieldDbType.DateTime)]
        [InlineData(FieldDbType.Boolean, FieldDbType.Integer)]
        [InlineData(FieldDbType.Binary, FieldDbType.String)]
        [InlineData(FieldDbType.Guid, FieldDbType.String)]
        [DisplayName("GetKindForTypeChange：跨 family 應為 Rebuild")]
        public void GetKindForTypeChange_CrossFamily_ReturnsRebuild(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.Rebuild, SqlAlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Theory]
        [InlineData(FieldDbType.Integer, FieldDbType.AutoIncrement)]
        [InlineData(FieldDbType.AutoIncrement, FieldDbType.Integer)]
        [InlineData(FieldDbType.AutoIncrement, FieldDbType.Long)]
        [DisplayName("GetKindForTypeChange：AutoIncrement 狀態變更應為 Rebuild")]
        public void GetKindForTypeChange_AutoIncrementToggle_ReturnsRebuild(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.Rebuild, SqlAlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Theory]
        [InlineData(FieldDbType.Unknown, FieldDbType.String)]
        [InlineData(FieldDbType.String, FieldDbType.Unknown)]
        [DisplayName("GetKindForTypeChange：Unknown 應為 NotSupported")]
        public void GetKindForTypeChange_UnknownType_ReturnsNotSupported(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.NotSupported, SqlAlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Fact]
        [DisplayName("IsNarrowing：String 長度縮小應判定為 narrowing")]
        public void IsNarrowing_StringLengthReduced_ReturnsTrue()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };

            Assert.True(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：String 長度放大不是 narrowing")]
        public void IsNarrowing_StringLengthIncreased_ReturnsFalse()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };

            Assert.False(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Text 轉 String（有長度上限）應判定為 narrowing")]
        public void IsNarrowing_TextToString_ReturnsTrue()
        {
            var oldField = new DbField("note", "Note", FieldDbType.Text);
            var newField = new DbField("note", "Note", FieldDbType.String) { Length = 200 };

            Assert.True(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：String 轉 Text 不是 narrowing")]
        public void IsNarrowing_StringToText_ReturnsFalse()
        {
            var oldField = new DbField("note", "Note", FieldDbType.String) { Length = 200 };
            var newField = new DbField("note", "Note", FieldDbType.Text);

            Assert.False(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Theory]
        [InlineData(FieldDbType.Long, FieldDbType.Integer)]
        [InlineData(FieldDbType.Integer, FieldDbType.Short)]
        [InlineData(FieldDbType.Long, FieldDbType.Short)]
        [DisplayName("IsNarrowing：數值型縮小應判定為 narrowing")]
        public void IsNarrowing_NumericRankReduced_ReturnsTrue(FieldDbType from, FieldDbType to)
        {
            var oldField = new DbField("v", "V", from);
            var newField = new DbField("v", "V", to);

            Assert.True(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Theory]
        [InlineData(FieldDbType.Short, FieldDbType.Integer)]
        [InlineData(FieldDbType.Integer, FieldDbType.Long)]
        [DisplayName("IsNarrowing：數值型放大不是 narrowing")]
        public void IsNarrowing_NumericRankIncreased_ReturnsFalse(FieldDbType from, FieldDbType to)
        {
            var oldField = new DbField("v", "V", from);
            var newField = new DbField("v", "V", to);

            Assert.False(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Decimal precision 縮小應判定為 narrowing")]
        public void IsNarrowing_DecimalPrecisionReduced_ReturnsTrue()
        {
            var oldField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 2 };
            var newField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 10, Scale = 2 };

            Assert.True(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Decimal scale 縮小應判定為 narrowing")]
        public void IsNarrowing_DecimalScaleReduced_ReturnsTrue()
        {
            var oldField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 4 };
            var newField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 2 };

            Assert.True(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：DateTime 轉 Date 應判定為 narrowing（時間精度遺失）")]
        public void IsNarrowing_DateTimeToDate_ReturnsTrue()
        {
            var oldField = new DbField("dt", "Dt", FieldDbType.DateTime);
            var newField = new DbField("dt", "Dt", FieldDbType.Date);

            Assert.True(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("IsNarrowing：Date 轉 DateTime 不是 narrowing")]
        public void IsNarrowing_DateToDateTime_ReturnsFalse()
        {
            var oldField = new DbField("dt", "Dt", FieldDbType.Date);
            var newField = new DbField("dt", "Dt", FieldDbType.DateTime);

            Assert.False(SqlAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }
    }
}
