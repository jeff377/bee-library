using System.ComponentModel;
using System.Data;
using Bee.Definition;
using Microsoft.Data.SqlClient;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbCommandSpecTests
    {
        #region 建構子測試

        [Fact]
        [DisplayName("預設建構子應建立空的命令規格")]
        public void DefaultConstructor_CreatesEmptySpec()
        {
            var spec = new DbCommandSpec();

            Assert.Equal(DbCommandKind.NonQuery, spec.Kind);
            Assert.Equal(string.Empty, spec.CommandText);
            Assert.Equal(CommandType.Text, spec.CommandType);
            Assert.Equal(30, spec.CommandTimeout);
            Assert.NotNull(spec.Parameters);
            Assert.Empty(spec.Parameters);
        }

        [Fact]
        [DisplayName("位置參數建構子應依序加入名稱為 p0、p1 的參數")]
        public void PositionalConstructor_AddsP0P1Parameters()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT * FROM T WHERE A = {0} AND B = {1}", "x", 123);

            Assert.Equal(DbCommandKind.Scalar, spec.Kind);
            Assert.Equal("SELECT * FROM T WHERE A = {0} AND B = {1}", spec.CommandText);
            Assert.Equal(2, spec.Parameters.Count);
            Assert.Equal("p0", spec.Parameters[0].Name);
            Assert.Equal("x", spec.Parameters[0].Value);
            Assert.Equal("p1", spec.Parameters[1].Name);
            Assert.Equal(123, spec.Parameters[1].Value);
        }

        [Fact]
        [DisplayName("位置參數建構子未提供值時不應建立任何參數")]
        public void PositionalConstructor_NoValues_CreatesNoParameters()
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, "SELECT 1");

            Assert.Empty(spec.Parameters);
        }

        [Fact]
        [DisplayName("具名參數建構子應將字典內容加入 Parameters")]
        public void NamedConstructor_AddsParameters()
        {
            var dict = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Bee" }
            };

            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT * FROM T WHERE Id = {Id} AND Name = {Name}", dict);

            Assert.Equal(2, spec.Parameters.Count);
            Assert.Equal(1, spec.Parameters["Id"].Value);
            Assert.Equal("Bee", spec.Parameters["Name"].Value);
        }

        [Fact]
        [DisplayName("具名參數建構子傳入 null 字典時應建立空 Parameters")]
        public void NamedConstructor_NullDictionary_CreatesNoParameters()
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, "SELECT 1", parameters: null);

            Assert.Empty(spec.Parameters);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("位置參數建構子 commandText 為空時應擲出 ArgumentNullException")]
        public void PositionalConstructor_EmptyCommandText_Throws(string? commandText)
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DbCommandSpec(DbCommandKind.NonQuery, commandText!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("具名參數建構子 commandText 為空時應擲出 ArgumentNullException")]
        public void NamedConstructor_EmptyCommandText_Throws(string? commandText)
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DbCommandSpec(DbCommandKind.NonQuery, commandText!, new Dictionary<string, object>()));
        }

        #endregion

        #region CommandTimeout setter 測試

        [Fact]
        [DisplayName("CommandTimeout 設為 0 應套用預設值 30")]
        public void CommandTimeout_Zero_UsesDefault()
        {
            var spec = new DbCommandSpec { CommandTimeout = 0 };
            Assert.Equal(30, spec.CommandTimeout);
        }

        [Fact]
        [DisplayName("CommandTimeout 設為負值應套用預設值 30")]
        public void CommandTimeout_Negative_UsesDefault()
        {
            var spec = new DbCommandSpec { CommandTimeout = -10 };
            Assert.Equal(30, spec.CommandTimeout);
        }

        [Fact]
        [DisplayName("CommandTimeout 設為合法值且未超過 cap 應原樣使用")]
        public void CommandTimeout_WithinCap_UsesValue()
        {
            var spec = new DbCommandSpec { CommandTimeout = 45 };
            Assert.Equal(45, spec.CommandTimeout);
        }

        [Fact]
        [DisplayName("CommandTimeout 設為超過 cap 應套用 cap 值")]
        public void CommandTimeout_ExceedsCap_UsesCap()
        {
            int original = BackendInfo.MaxDbCommandTimeout;
            try
            {
                BackendInfo.MaxDbCommandTimeout = 60;
                var spec = new DbCommandSpec { CommandTimeout = 9999 };
                Assert.Equal(60, spec.CommandTimeout);
            }
            finally
            {
                BackendInfo.MaxDbCommandTimeout = original;
            }
        }

        [Fact]
        [DisplayName("MaxDbCommandTimeout cap 為 0 時不限制 CommandTimeout")]
        public void CommandTimeout_NoCap_UsesValue()
        {
            int original = BackendInfo.MaxDbCommandTimeout;
            try
            {
                BackendInfo.MaxDbCommandTimeout = 0;
                var spec = new DbCommandSpec { CommandTimeout = 9999 };
                Assert.Equal(9999, spec.CommandTimeout);
            }
            finally
            {
                BackendInfo.MaxDbCommandTimeout = original;
            }
        }

        #endregion

        #region 佔位符解析（透過 CreateCommand 驗證）

        [Fact]
        [DisplayName("CreateCommand 應將位置佔位符 {0} 解析為帶前綴的參數名稱")]
        public void CreateCommand_PositionalPlaceholder_ResolvesToPrefixedName()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT * FROM T WHERE A = {0} AND B = {1}", "x", 1);

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            Assert.Equal("SELECT * FROM T WHERE A = @p0 AND B = @p1", cmd.CommandText);
            Assert.Equal(2, cmd.Parameters.Count);
            Assert.Equal("@p0", cmd.Parameters[0].ParameterName);
            Assert.Equal("x", cmd.Parameters[0].Value);
            Assert.Equal("@p1", cmd.Parameters[1].ParameterName);
            Assert.Equal(1, cmd.Parameters[1].Value);
        }

        [Fact]
        [DisplayName("CreateCommand 應將具名佔位符 {Name} 解析為帶前綴的參數名稱")]
        public void CreateCommand_NamedPlaceholder_ResolvesToPrefixedName()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT * FROM T WHERE Id = {Id}",
                new Dictionary<string, object> { { "Id", 99 } });

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            Assert.Equal("SELECT * FROM T WHERE Id = @Id", cmd.CommandText);
            Assert.Single(cmd.Parameters);
            Assert.Equal("@Id", cmd.Parameters[0].ParameterName);
        }

        [Fact]
        [DisplayName("具名佔位符應不分大小寫匹配")]
        public void CreateCommand_NamedPlaceholder_CaseInsensitive()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT * FROM T WHERE Id = {ID}",
                new Dictionary<string, object> { { "id", 1 } });

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            Assert.Equal("SELECT * FROM T WHERE Id = @id", cmd.CommandText);
        }

        [Fact]
        [DisplayName("{@Parameters} 應展開為以逗號分隔的所有參數佔位符")]
        public void CreateCommand_AtParametersToken_ExpandsToCommaList()
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery,
                "EXEC sp_test {@Parameters}", "a", "b", "c");

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            Assert.Equal("EXEC sp_test @p0,@p1,@p2", cmd.CommandText);
            Assert.Equal(3, cmd.Parameters.Count);
        }

        [Fact]
        [DisplayName("CreateCommand StoredProcedure 不應解析 CommandText 中的佔位符")]
        public void CreateCommand_StoredProcedure_SkipsPlaceholderResolution()
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, "sp_GetUser")
            {
                CommandType = CommandType.StoredProcedure
            };
            spec.Parameters.Add("UserId", 42);

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            Assert.Equal("sp_GetUser", cmd.CommandText);
            Assert.Equal(CommandType.StoredProcedure, cmd.CommandType);
            Assert.Single(cmd.Parameters);
            Assert.Equal("@UserId", cmd.Parameters[0].ParameterName);
        }

        [Fact]
        [DisplayName("CreateCommand 應傳入 CommandTimeout 至 DbCommand")]
        public void CreateCommand_AppliesCommandTimeout()
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, "SELECT 1") { CommandTimeout = 45 };

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            Assert.Equal(45, cmd.CommandTimeout);
        }

        [Fact]
        [DisplayName("CreateCommand 參數值為 null 應綁定為 DBNull")]
        public void CreateCommand_NullValue_BindsDBNull()
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery,
                "UPDATE T SET A = {0}", new object[] { null! });

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            Assert.Equal(DBNull.Value, cmd.Parameters[0].Value);
        }

        [Fact]
        [DisplayName("CreateCommand 參數名稱已含前綴時不應重複加上前綴")]
        public void CreateCommand_NameWithPrefix_NotDuplicated()
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, "SELECT 1");
            spec.Parameters.Add("@X", 1);

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            Assert.Equal("@X", cmd.Parameters[0].ParameterName);
        }

        [Fact]
        [DisplayName("CreateCommand 應將 DbType、Size、SourceColumn、SourceVersion 傳遞給 DbParameter")]
        public void CreateCommand_PropagatesParameterMetadata()
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, "UPDATE T SET A = {0}", "x");
            var p = spec.Parameters[0];
            p.Size = 50;
            p.SourceColumn = "A";
            p.SourceVersion = DataRowVersion.Original;
            p.IsNullable = true;

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            var dp = cmd.Parameters[0];
            Assert.Equal(DbType.String, dp.DbType);
            Assert.Equal(50, dp.Size);
            Assert.Equal("A", dp.SourceColumn);
            Assert.Equal(DataRowVersion.Original, dp.SourceVersion);
            Assert.True(dp.IsNullable);
        }

        [Fact]
        [DisplayName("CreateCommand 連線為 null 時應擲出 ArgumentNullException")]
        public void CreateCommand_NullConnection_Throws()
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, "SELECT 1");

            Assert.Throws<ArgumentNullException>(() =>
                spec.CreateCommand(DatabaseType.SQLServer, null!));
        }

        [Fact]
        [DisplayName("CreateCommand 在 CommandText 為空時應擲出 InvalidOperationException")]
        public void CreateCommand_EmptyCommandText_Throws()
        {
            var spec = new DbCommandSpec();

            using var conn = new SqlConnection();
            Assert.Throws<InvalidOperationException>(() =>
                spec.CreateCommand(DatabaseType.SQLServer, conn));
        }

        [Fact]
        [DisplayName("CreateCommand 位置佔位符索引越界時應擲出 InvalidOperationException")]
        public void CreateCommand_IndexOutOfRange_Throws()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT * FROM T WHERE A = {0} AND B = {5}", "x");

            using var conn = new SqlConnection();
            Assert.Throws<InvalidOperationException>(() =>
                spec.CreateCommand(DatabaseType.SQLServer, conn));
        }

        [Fact]
        [DisplayName("CreateCommand 具名佔位符找不到對應參數時應擲出 InvalidOperationException")]
        public void CreateCommand_UnknownNamedKey_Throws()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT * FROM T WHERE Id = {Missing}",
                new Dictionary<string, object> { { "Id", 1 } });

            using var conn = new SqlConnection();
            Assert.Throws<InvalidOperationException>(() =>
                spec.CreateCommand(DatabaseType.SQLServer, conn));
        }

        [Fact]
        [DisplayName("CreateCommand Oracle 資料庫應使用冒號參數前綴")]
        public void CreateCommand_Oracle_UsesColonPrefix()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT * FROM T WHERE A = {0}", "x");

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.Oracle, conn);

            Assert.Equal("SELECT * FROM T WHERE A = :p0", cmd.CommandText);
            Assert.Equal(":p0", cmd.Parameters[0].ParameterName);
        }

        #endregion

        #region ToString 測試

        [Fact]
        [DisplayName("ToString 應回傳 CommandText")]
        public void ToString_ReturnsCommandText()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT 1");
            Assert.Equal("SELECT 1", spec.ToString());
        }

        #endregion
    }
}
