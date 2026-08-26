using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.AuditLog;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="AuditRuleRepository"/> 對「<c>st_audit_rule</c> 尚未建立」的處置：回空清單而非拋例外。
    /// </summary>
    /// <remarks>
    /// 這是 per-form 稽核規則唯一的真實回歸風險——**升級前既有的部署沒有這張表**，
    /// 若讀不到就拋例外，每一次 Save 都會炸。
    /// <para>
    /// 測試環境本身就是那個狀態：<c>tests/Define/DbCategorySettings.xml</c> 未登記
    /// <c>st_audit_rule</c>，所以 seeder 不會建它。**若日後有人把它加進測試定義，本測試會失去意義
    /// 而非失敗**，屆時要改為顯式 DROP 後再驗。
    /// </para>
    /// <para>
    /// 逐一涵蓋五種 provider：判斷表是否存在走的是各家自己的 <c>TableSchemaProvider</c>，
    /// 一種 provider 過不代表其餘四種過。
    /// </para>
    /// </remarks>
    public class AuditRuleRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public AuditRuleRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private AuditRuleRepository CreateRepo()
            => new AuditRuleRepository(
                TestRepositoryContext.Create(_fx.GetRequiredService<IDbConnectionManager>()),
                Guid.Empty, string.Empty);

        private void RunMissingTable(DatabaseType dbType)
        {
            var databaseId = TestDbConventions.GetDatabaseId(dbType, "company");

            var rules = CreateRepo().GetRules(databaseId);

            Assert.Empty(rules);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("st_audit_rule 不存在時回空清單 on SQL Server")]
        public void MissingTable_SqlServer() => RunMissingTable(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("st_audit_rule 不存在時回空清單 on PostgreSQL")]
        public void MissingTable_PostgreSql() => RunMissingTable(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("st_audit_rule 不存在時回空清單 on SQLite")]
        public void MissingTable_Sqlite() => RunMissingTable(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("st_audit_rule 不存在時回空清單 on MySQL")]
        public void MissingTable_MySql() => RunMissingTable(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("st_audit_rule 不存在時回空清單 on Oracle")]
        public void MissingTable_Oracle() => RunMissingTable(DatabaseType.Oracle);

        [Fact]
        [DisplayName("databaseId 為空應擲例外，而非悄悄回空清單")]
        public void GetRules_EmptyDatabaseId_Throws()
        {
            var exception = Record.Exception(() => CreateRepo().GetRules(string.Empty));

            Assert.IsType<ArgumentException>(exception);
        }
    }
}
