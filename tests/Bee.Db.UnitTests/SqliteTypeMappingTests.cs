using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Sqlite;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 純語法測試：覆蓋 <see cref="SqliteTypeMapping"/> 對各 <see cref="FieldDbType"/> 的型別字串映射。
    /// AutoIncrement → "INTEGER" 是 SQLite 特殊規則，由 CREATE TABLE 端再內聯
    /// PRIMARY KEY AUTOINCREMENT。
    /// </summary>
    public class SqliteTypeMappingTests
    {
        [Fact]
        [DisplayName("SQLite GetSqliteType：String 應為 VARCHAR(N)")]
        public void GetSqliteType_String_UsesVarchar()
        {
            var field = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            Assert.Equal("VARCHAR(50)", SqliteTypeMapping.GetSqliteType(field));
        }

        [Theory]
        [InlineData(FieldDbType.Text, "TEXT")]
        [InlineData(FieldDbType.Boolean, "BOOLEAN")]
        [InlineData(FieldDbType.AutoIncrement, "INTEGER")]
        [InlineData(FieldDbType.Short, "SMALLINT")]
        [InlineData(FieldDbType.Integer, "INTEGER")]
        [InlineData(FieldDbType.Long, "BIGINT")]
        [InlineData(FieldDbType.Currency, "NUMERIC(19,4)")]
        [InlineData(FieldDbType.Date, "DATE")]
        [InlineData(FieldDbType.DateTime, "DATETIME")]
        [InlineData(FieldDbType.Guid, "UUID")]
        [InlineData(FieldDbType.Binary, "BLOB")]
        [DisplayName("SQLite GetSqliteType：各型別應映射為對應 SQLite 型別字串")]
        public void GetSqliteType_VariousTypes_MapsCorrectly(FieldDbType dbType, string expected)
        {
            var field = new DbField("v", "V", dbType);
            Assert.Equal(expected, SqliteTypeMapping.GetSqliteType(field));
        }

        [Fact]
        [DisplayName("SQLite GetSqliteType：Decimal 預設精度 18,0")]
        public void GetSqliteType_DecimalDefaults_Returns18_0()
        {
            var field = new DbField("v", "V", FieldDbType.Decimal) { Precision = 0, Scale = 0 };
            Assert.Equal("NUMERIC(18,0)", SqliteTypeMapping.GetSqliteType(field));
        }

        [Fact]
        [DisplayName("SQLite GetSqliteType：Decimal 自訂 precision / scale 應反映於型別字串")]
        public void GetSqliteType_DecimalCustom_AppliesPrecisionScale()
        {
            var field = new DbField("v", "V", FieldDbType.Decimal) { Precision = 12, Scale = 3 };
            Assert.Equal("NUMERIC(12,3)", SqliteTypeMapping.GetSqliteType(field));
        }

        [Fact]
        [DisplayName("SQLite GetSqliteType：Unknown 應擲 InvalidOperationException")]
        public void GetSqliteType_Unknown_Throws()
        {
            var field = new DbField("v", "V", FieldDbType.Unknown);
            Assert.Throws<InvalidOperationException>(() => SqliteTypeMapping.GetSqliteType(field));
        }
    }
}
