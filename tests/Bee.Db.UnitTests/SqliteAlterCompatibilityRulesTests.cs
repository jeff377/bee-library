using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 純語法測試：覆蓋 <see cref="SqliteAlterCompatibilityRules"/> 的型別變更與 narrowing 判斷。
    /// SQLite 的 ALTER COLUMN 不支援，所有型別變更最終都走 Rebuild；narrowing 結果僅供呼叫端
    /// 觀察，不影響執行路徑，但 API 行為需與 SqlServer / PostgreSql 對齊。
    /// </summary>
    public class SqliteAlterCompatibilityRulesTests
    {
        #region GetKindForTypeChange

        [Theory]
        [InlineData(FieldDbType.String, FieldDbType.String, ChangeExecutionKind.Rebuild)]
        [InlineData(FieldDbType.String, FieldDbType.Integer, ChangeExecutionKind.Rebuild)]
        [InlineData(FieldDbType.Integer, FieldDbType.Decimal, ChangeExecutionKind.Rebuild)]
        [InlineData(FieldDbType.Date, FieldDbType.DateTime, ChangeExecutionKind.Rebuild)]
        [DisplayName("SQLite GetKindForTypeChange：已知型別變更一律回傳 Rebuild")]
        public void GetKindForTypeChange_KnownTypes_ReturnsRebuild(
            FieldDbType from, FieldDbType to, ChangeExecutionKind expected)
        {
            Assert.Equal(expected, SqliteAlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        [Theory]
        [InlineData(FieldDbType.Unknown, FieldDbType.Integer)]
        [InlineData(FieldDbType.String, FieldDbType.Unknown)]
        [InlineData(FieldDbType.Unknown, FieldDbType.Unknown)]
        [DisplayName("SQLite GetKindForTypeChange：任一端為 Unknown 應回傳 NotSupported")]
        public void GetKindForTypeChange_Unknown_ReturnsNotSupported(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.NotSupported,
                SqliteAlterCompatibilityRules.GetKindForTypeChange(from, to));
        }

        #endregion

        #region IsNarrowing — string capacity

        [Fact]
        [DisplayName("SQLite IsNarrowing：String 長度縮短應為 narrowing")]
        public void IsNarrowing_StringShortened_ReturnsTrue()
        {
            var oldField = new DbField("v", "V", FieldDbType.String) { Length = 100 };
            var newField = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            Assert.True(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("SQLite IsNarrowing：String 長度延長不應為 narrowing")]
        public void IsNarrowing_StringExtended_ReturnsFalse()
        {
            var oldField = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            var newField = new DbField("v", "V", FieldDbType.String) { Length = 100 };
            Assert.False(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("SQLite IsNarrowing：String → Text 視為無上限，不應為 narrowing")]
        public void IsNarrowing_StringToText_ReturnsFalse()
        {
            var oldField = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            var newField = new DbField("v", "V", FieldDbType.Text);
            Assert.False(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("SQLite IsNarrowing：Text → String 有限長度應為 narrowing")]
        public void IsNarrowing_TextToBoundedString_ReturnsTrue()
        {
            var oldField = new DbField("v", "V", FieldDbType.Text);
            var newField = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            Assert.True(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        #endregion

        #region IsNarrowing — numeric

        [Theory]
        [InlineData(FieldDbType.Integer, FieldDbType.Short)]
        [InlineData(FieldDbType.Long, FieldDbType.Integer)]
        [InlineData(FieldDbType.Long, FieldDbType.Short)]
        [InlineData(FieldDbType.Decimal, FieldDbType.Integer)]
        [InlineData(FieldDbType.Currency, FieldDbType.Long)]
        [DisplayName("SQLite IsNarrowing：數值型別 rank 降階應為 narrowing")]
        public void IsNarrowing_NumericRankLower_ReturnsTrue(FieldDbType from, FieldDbType to)
        {
            var oldField = new DbField("v", "V", from);
            var newField = new DbField("v", "V", to);
            Assert.True(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Theory]
        [InlineData(FieldDbType.Short, FieldDbType.Integer)]
        [InlineData(FieldDbType.Integer, FieldDbType.Long)]
        [InlineData(FieldDbType.Integer, FieldDbType.Decimal)]
        [DisplayName("SQLite IsNarrowing：數值型別 rank 提升不應為 narrowing")]
        public void IsNarrowing_NumericRankHigher_ReturnsFalse(FieldDbType from, FieldDbType to)
        {
            var oldField = new DbField("v", "V", from);
            var newField = new DbField("v", "V", to);
            Assert.False(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("SQLite IsNarrowing：Decimal precision 降低應為 narrowing")]
        public void IsNarrowing_DecimalPrecisionLower_ReturnsTrue()
        {
            var oldField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 4 };
            var newField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 10, Scale = 4 };
            Assert.True(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("SQLite IsNarrowing：Decimal scale 降低應為 narrowing")]
        public void IsNarrowing_DecimalScaleLower_ReturnsTrue()
        {
            var oldField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 4 };
            var newField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 2 };
            Assert.True(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("SQLite IsNarrowing：Decimal precision/scale 維持或提升不應為 narrowing")]
        public void IsNarrowing_DecimalSamePrecisionScale_ReturnsFalse()
        {
            var oldField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 4 };
            var newField = new DbField("v", "V", FieldDbType.Decimal) { Precision = 18, Scale = 4 };
            Assert.False(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        #endregion

        #region IsNarrowing — datetime

        [Fact]
        [DisplayName("SQLite IsNarrowing：DateTime → Date 應為 narrowing")]
        public void IsNarrowing_DateTimeToDate_ReturnsTrue()
        {
            var oldField = new DbField("v", "V", FieldDbType.DateTime);
            var newField = new DbField("v", "V", FieldDbType.Date);
            Assert.True(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("SQLite IsNarrowing：Date → DateTime 不應為 narrowing")]
        public void IsNarrowing_DateToDateTime_ReturnsFalse()
        {
            var oldField = new DbField("v", "V", FieldDbType.Date);
            var newField = new DbField("v", "V", FieldDbType.DateTime);
            Assert.False(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        #endregion

        #region IsNarrowing — cross-family

        [Fact]
        [DisplayName("SQLite IsNarrowing：跨家族變更（String → Integer）不會觸發 narrowing 判斷，回傳 false")]
        public void IsNarrowing_CrossFamily_ReturnsFalse()
        {
            var oldField = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            var newField = new DbField("v", "V", FieldDbType.Integer);
            Assert.False(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        [Fact]
        [DisplayName("SQLite IsNarrowing：Boolean 與 Boolean 不在 narrowing 判斷範圍，回傳 false")]
        public void IsNarrowing_BooleanToBoolean_ReturnsFalse()
        {
            var oldField = new DbField("v", "V", FieldDbType.Boolean);
            var newField = new DbField("v", "V", FieldDbType.Boolean);
            Assert.False(SqliteAlterCompatibilityRules.IsNarrowing(oldField, newField));
        }

        #endregion
    }
}
