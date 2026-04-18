using System.ComponentModel;
using System.Data;
using Bee.Definition;
using Microsoft.Data.SqlClient;

namespace Bee.Db.UnitTests
{
    public class DbFuncTests
    {
        #region QuoteIdentifier 跳脫測試

        [Theory]
        [InlineData(DatabaseType.SQLServer, "Name", "[Name]")]
        [InlineData(DatabaseType.SQLServer, "Col]umn", "[Col]]umn]")]
        [InlineData(DatabaseType.SQLServer, "A]]B", "[A]]]]B]")]
        [DisplayName("QuoteIdentifier SQL Server 應正確跳脫 ] 字元")]
        public void QuoteIdentifier_SqlServer_EscapesBracket(DatabaseType dbType, string identifier, string expected)
        {
            var result = DbFunc.QuoteIdentifier(dbType, identifier);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(DatabaseType.MySQL, "Name", "`Name`")]
        [InlineData(DatabaseType.MySQL, "Col`umn", "`Col``umn`")]
        [DisplayName("QuoteIdentifier MySQL 應正確跳脫 ` 字元")]
        public void QuoteIdentifier_MySql_EscapesBacktick(DatabaseType dbType, string identifier, string expected)
        {
            var result = DbFunc.QuoteIdentifier(dbType, identifier);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(DatabaseType.SQLite, "Name", "\"Name\"")]
        [InlineData(DatabaseType.SQLite, "Col\"umn", "\"Col\"\"umn\"")]
        [InlineData(DatabaseType.Oracle, "Name", "\"Name\"")]
        [InlineData(DatabaseType.Oracle, "Col\"umn", "\"Col\"\"umn\"")]
        [DisplayName("QuoteIdentifier SQLite/Oracle 應正確跳脫雙引號")]
        public void QuoteIdentifier_SqliteOracle_EscapesDoubleQuote(DatabaseType dbType, string identifier, string expected)
        {
            var result = DbFunc.QuoteIdentifier(dbType, identifier);
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("QuoteIdentifier 不支援的資料庫類型應擲出 NotSupportedException")]
        public void QuoteIdentifier_UnsupportedType_Throws()
        {
            Assert.Throws<NotSupportedException>(() =>
                DbFunc.QuoteIdentifier((DatabaseType)999, "Test"));
        }

        #endregion

        #region GetParameterPrefix 測試

        [Theory]
        [InlineData(DatabaseType.SQLServer, "@")]
        [InlineData(DatabaseType.MySQL, "@")]
        [InlineData(DatabaseType.SQLite, "@")]
        [InlineData(DatabaseType.Oracle, ":")]
        [DisplayName("GetParameterPrefix 應回傳對應資料庫的參數前綴")]
        public void GetParameterPrefix_ReturnsCorrectPrefix(DatabaseType dbType, string expected)
        {
            var result = DbFunc.GetParameterPrefix(dbType);
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("GetParameterPrefix 不支援的資料庫類型應擲出 NotSupportedException")]
        public void GetParameterPrefix_UnsupportedType_Throws()
        {
            Assert.Throws<NotSupportedException>(() =>
                DbFunc.GetParameterPrefix((DatabaseType)999));
        }

        #endregion

        #region GetParameterName 測試

        [Theory]
        [InlineData(DatabaseType.SQLServer, "Id", "@Id")]
        [InlineData(DatabaseType.MySQL, "Id", "@Id")]
        [InlineData(DatabaseType.SQLite, "Id", "@Id")]
        [InlineData(DatabaseType.Oracle, "Id", ":Id")]
        [DisplayName("GetParameterName 應依資料庫類型加上對應前綴")]
        public void GetParameterName_AppendsPrefix(DatabaseType dbType, string name, string expected)
        {
            var result = DbFunc.GetParameterName(dbType, name);
            Assert.Equal(expected, result);
        }

        #endregion

        #region InferDbType 測試

        // 此測試資料需涵蓋多種 CLR 型別（string/int/DateTime/Guid/byte[] 等），
        // 故 TheoryData 僅能以 object 作為第一型別參數；xUnit1045 警告不適用於此刻意設計。
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public static TheoryData<object, DbType> InferDbType_Inputs() => new()
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
#pragma warning restore xUnit1045

        [Theory]
        [MemberData(nameof(InferDbType_Inputs))]
        [DisplayName("InferDbType 應依值的型別回傳對應 DbType")]
        public void InferDbType_KnownTypes_ReturnsExpected(object value, DbType expected)
        {
            var result = DbFunc.InferDbType(value);
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("InferDbType 對 null 值應回傳 null")]
        public void InferDbType_Null_ReturnsNull()
        {
            Assert.Null(DbFunc.InferDbType(null!));
        }

        [Fact]
        [DisplayName("InferDbType 對 DBNull 應回傳 null")]
        public void InferDbType_DBNull_ReturnsNull()
        {
            Assert.Null(DbFunc.InferDbType(DBNull.Value));
        }

        [Fact]
        [DisplayName("InferDbType 對不支援型別應回傳 null")]
        public void InferDbType_UnsupportedType_ReturnsNull()
        {
            Assert.Null(DbFunc.InferDbType(new object()));
        }

        #endregion

        #region SqlFormat 測試

        [Fact]
        [DisplayName("SqlFormat 應將 {0}/{1} 替換為對應參數名稱")]
        public void SqlFormat_ReplacesPositionalPlaceholders()
        {
            using var cmd = new SqlCommand();
            cmd.Parameters.Add(new SqlParameter("@p0", "x"));
            cmd.Parameters.Add(new SqlParameter("@p1", 1));

            var result = DbFunc.SqlFormat("SELECT * FROM T WHERE A = {0} AND B = {1}", cmd.Parameters);

            Assert.Equal("SELECT * FROM T WHERE A = @p0 AND B = @p1", result);
        }

        [Fact]
        [DisplayName("SqlFormat 空 Parameters 集合應原樣回傳")]
        public void SqlFormat_EmptyParameters_ReturnsOriginal()
        {
            using var cmd = new SqlCommand();
            var result = DbFunc.SqlFormat("SELECT 1", cmd.Parameters);
            Assert.Equal("SELECT 1", result);
        }

        #endregion
    }
}
