using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Schema;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 覆蓋 <see cref="SqliteAlterCompatibilityRules"/> —— SQLite 相對於共用
    /// <see cref="AlterCompatibilityRules"/> 的唯一差異：ALTER COLUMN 不支援，
    /// 所有型別變更一律走 Rebuild。narrowing 判斷共用，測試見
    /// <see cref="AlterCompatibilityRulesTests"/>。
    /// </summary>
    public class SqliteAlterCompatibilityRulesTests
    {
        [Theory]
        [InlineData(FieldDbType.String, FieldDbType.String)]
        [InlineData(FieldDbType.String, FieldDbType.Integer)]
        [InlineData(FieldDbType.Integer, FieldDbType.Decimal)]
        [InlineData(FieldDbType.Date, FieldDbType.DateTime)]
        [InlineData(FieldDbType.Time, FieldDbType.Time)]
        [InlineData(FieldDbType.AutoIncrement, FieldDbType.AutoIncrement)]
        [DisplayName("SQLite GetKindForTypeChange：已知型別變更一律回傳 Rebuild")]
        public void GetKindForTypeChange_KnownTypes_ReturnsRebuild(FieldDbType from, FieldDbType to)
        {
            Assert.Equal(ChangeExecutionKind.Rebuild, SqliteAlterCompatibilityRules.GetKindForTypeChange(from, to));
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
    }
}
