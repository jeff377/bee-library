using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// wire 成員判定必須讀 <see cref="JsonIgnoreAttribute.Condition"/>，不能只看標註存不存在。
    /// </summary>
    /// <remarks>
    /// <c>[JsonIgnore(Condition = JsonIgnoreCondition.Never)]</c> 的語意是**永不忽略**。
    /// 只看存在性會把它判成「被忽略」——剛好相反，於是該成員從 wire 閉包裡消失，
    /// 漏註冊 formatter 也不會被漂移閘門抓到。
    /// <para>
    /// 同型 bug 曾是 BEE4007 被剔除的原因（2026-07-30 的 commit message 明載「規則只看 attribute
    /// 存在、未讀 Condition，是實作 bug」），然後同一個 bug 活在 <c>WireContractDriftTests</c> 裡。
    /// </para>
    /// </remarks>
    public class WireMemberNamesSemanticsTests
    {
        private sealed class Sample
        {
            /// <summary>一般成員：應算 wire 成員。</summary>
            public string Plain { get; set; } = string.Empty;

            /// <summary>永不忽略：語意上**是** wire 成員。</summary>
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string NeverIgnored { get; set; } = string.Empty;

            /// <summary>一律忽略：不是 wire 成員。</summary>
            [JsonIgnore]
            public string AlwaysIgnored { get; set; } = string.Empty;
        }

        [Fact]
        [DisplayName("Condition = Never 的成員必須算進 wire 成員，一律忽略的不算")]
        public void WireMemberNames_ReadsTheIgnoreCondition()
        {
            var method = typeof(WireContractDriftTests).GetMethod(
                "WireMemberNames", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var names = (List<string>)method!.Invoke(null, [typeof(Sample)])!;

            Assert.Contains("Plain", names);
            Assert.Contains("NeverIgnored", names);
            Assert.DoesNotContain("AlwaysIgnored", names);
        }
    }
}
