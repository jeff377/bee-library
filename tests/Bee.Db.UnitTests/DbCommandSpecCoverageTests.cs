using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Coverage-focused tests for <see cref="DbCommandSpec"/> parameter resolution edge cases.
    /// These are DB-free (no fixture) so they always execute during coverage collection.
    /// </summary>
    public class DbCommandSpecCoverageTests
    {
        [Fact]
        [DisplayName("具名佔位符匹配到空白名稱參數時應在空名保護處擲 InvalidOperationException")]
        public void CreateCommand_NamedKeyMatchesBlankParamName_ThrowsAtBlankGuard()
        {
            // The placeholder key is whitespace and a parameter whose Name is whitespace exists,
            // so the case-insensitive lookup matches it (unlike the mismatch path that throws on
            // "not found"). This drives ResolveNamedKey into its blank-name guard branch.
            var spec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT * FROM T WHERE X = {   }");
            spec.Parameters.Add("X", 1);
            spec.Parameters[0].Name = "   ";

            using var conn = new SqlConnection();
            Assert.Throws<InvalidOperationException>(() =>
                spec.CreateCommand(DatabaseType.SQLServer, conn));
        }

        [Fact]
        [DisplayName("具名佔位符匹配到有效參數名稱時應成功解析（空名保護的另一分支）")]
        public void CreateCommand_NamedKeyMatchesValidParamName_Resolves()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT * FROM T WHERE X = {X}");
            spec.Parameters.Add("X", 1);

            using var conn = new SqlConnection();
            using var cmd = spec.CreateCommand(DatabaseType.SQLServer, conn);

            Assert.Equal("SELECT * FROM T WHERE X = @X", cmd.CommandText);
        }
    }
}
