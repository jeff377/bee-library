using System.ComponentModel;
using Bee.Db.Schema;

namespace Bee.Db.UnitTests
{
    public class UpgradePlanTests
    {
        private static readonly string[] s_createTableSql = { "CREATE TABLE t1 (id INT)" };
        private static readonly string[] s_createIndexSql = { "CREATE INDEX idx ON t1(id)", "CREATE INDEX idx2 ON t1(id)" };
        private static readonly string[] s_narrowingWarning = { "Narrowing change" };

        [Fact]
        [DisplayName("AllStatements 多個 stage 時應依序產出所有 SQL 語句")]
        public void AllStatements_MultipleStages_YieldsAllInOrder()
        {
            var stage1 = new UpgradeStage(UpgradeStageKind.CreateTable, s_createTableSql);
            var stage2 = new UpgradeStage(UpgradeStageKind.CreateIndexes, s_createIndexSql);
            UpgradeStage[] stages = { stage1, stage2 };
            var plan = new UpgradePlan(UpgradeExecutionMode.Create, stages);

            var statements = plan.AllStatements.ToList();

            Assert.Equal(3, statements.Count);
            Assert.Equal(s_createTableSql[0], statements[0]);
        }

        [Fact]
        [DisplayName("AllStatements stage 無語句時應回傳空集合")]
        public void AllStatements_NoStages_ReturnsEmpty()
        {
            var plan = new UpgradePlan(UpgradeExecutionMode.NoChange);

            Assert.Empty(plan.AllStatements);
        }

        [Fact]
        [DisplayName("建構子傳入 warnings 應正確儲存警告清單")]
        public void Constructor_WithWarnings_StoresWarnings()
        {
            var plan = new UpgradePlan(UpgradeExecutionMode.Alter, null, s_narrowingWarning);

            Assert.Single(plan.Warnings);
            Assert.Equal("Narrowing change", plan.Warnings[0]);
        }
    }
}
