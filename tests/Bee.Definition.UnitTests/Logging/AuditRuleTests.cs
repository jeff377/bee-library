using System.ComponentModel;
using Bee.Definition.Logging;

namespace Bee.Definition.UnitTests.Logging
{
    /// <summary>
    /// <see cref="AuditRuleMode"/> 三態解析與 <see cref="CompanyAuditRules"/> 查表的單元測試。
    /// 純邏輯、不碰資料庫：這是 per-form 稽核規則的語意核心。
    /// </summary>
    public class AuditRuleTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [DisplayName("Inherit 應沿用該軸的部署預設值")]
        public void Resolve_Inherit_ReturnsInheritedValue(bool inherited)
        {
            Assert.Equal(inherited, AuditRuleMode.Inherit.Resolve(inherited));
        }

        [Fact]
        [DisplayName("On 應覆寫部署預設的 false —— per-form 規則的主要用途")]
        public void Resolve_On_OverridesDisabledDefault()
        {
            Assert.True(AuditRuleMode.On.Resolve(false));
        }

        [Fact]
        [DisplayName("Off 應覆寫部署預設的 true")]
        public void Resolve_Off_OverridesEnabledDefault()
        {
            Assert.False(AuditRuleMode.Off.Resolve(true));
        }

        [Fact]
        [DisplayName("Inherit 的列舉值應為 0，未設定的資料庫欄位才會落在繼承語意")]
        public void AuditRuleMode_Inherit_IsZero()
        {
            Assert.Equal(0, (int)AuditRuleMode.Inherit);
        }

        [Fact]
        [DisplayName("Find 應以宣告的 progId 取回規則")]
        public void Find_KnownProgId_ReturnsRule()
        {
            var rules = new CompanyAuditRules("C001",
                [new AuditRule("Order", AuditRuleMode.On, AuditRuleMode.Off, true)]);

            var rule = rules.Find("Order");

            Assert.NotNull(rule);
            Assert.Equal(AuditRuleMode.On, rule.ChangeMode);
            Assert.Equal(AuditRuleMode.Off, rule.AccessMode);
            Assert.True(rule.IsSensitive);
        }

        [Fact]
        [DisplayName("Find 查無規則應回 null —— 未宣告的表單即全軸 Inherit")]
        public void Find_UnknownProgId_ReturnsNull()
        {
            var rules = new CompanyAuditRules("C001",
                [new AuditRule("Order", AuditRuleMode.On, AuditRuleMode.On, false)]);

            Assert.Null(rules.Find("Customer"));
        }

        [Fact]
        [DisplayName("Find 應區分大小寫（Ordinal）—— progId 是識別碼不是顯示文字")]
        public void Find_DifferentCasing_ReturnsNull()
        {
            var rules = new CompanyAuditRules("C001",
                [new AuditRule("Order", AuditRuleMode.On, AuditRuleMode.On, false)]);

            Assert.Null(rules.Find("ORDER"));
        }

        [Fact]
        [DisplayName("空的規則集合應可建立，且每次查詢都回 null")]
        public void EmptyRules_FindAlwaysReturnsNull()
        {
            var rules = new CompanyAuditRules("C001", []);

            Assert.Equal(0, rules.Count);
            Assert.Null(rules.Find("Order"));
        }

        [Fact]
        [DisplayName("Find 傳入空字串應回 null 而非拋例外")]
        public void Find_EmptyProgId_ReturnsNull()
        {
            var rules = new CompanyAuditRules("C001", []);

            Assert.Null(rules.Find(string.Empty));
        }

        [Fact]
        [DisplayName("重複 progId 應保留第一筆，不因一列壞資料讓整間公司的稽核掛掉")]
        public void DuplicateProgId_KeepsFirstRule()
        {
            var rules = new CompanyAuditRules("C001",
            [
                new AuditRule("Order", AuditRuleMode.On, AuditRuleMode.On, true),
                new AuditRule("Order", AuditRuleMode.Off, AuditRuleMode.Off, false),
            ]);

            Assert.Equal(1, rules.Count);
            Assert.Equal(AuditRuleMode.On, rules.Find("Order")!.ChangeMode);
        }
    }
}
