using System.ComponentModel;
using System.Reflection;
using Bee.Api.Core.Validator;
using Bee.Business;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 把每個 API 方法的保護等級與驗證需求釘死，讓「調高門檻」變成一次必須正視的判斷。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 提高某方法的 <c>ProtectionLevel</c>（例如 Public → Encrypted）或把
    /// <c>AccessRequirement</c> 從 Anonymous 改為 Authenticated，會讓**既有的客戶端當場被拒**。
    /// 這類變更原本沒有任何機制會發現：attribute 的**參數**不進 <c>PublicAPI.Shipped.txt</c>，
    /// analyzer 看不到，wire 樣本也看不到——它不改變 payload 的形狀，只改變誰能送。
    /// </para>
    /// <para>
    /// 其餘的合約變更多半已有守護：訊息屬性、型別名、方法名、列舉成員都在
    /// <c>PublicAPI.Shipped.txt</c> 裡（訊息型別不使用 <c>[JsonPropertyName]</c>，
    /// 所以 wire 上的名字就是 C# 名經 camelCase 轉換），改名會讓 analyzer 紅。
    /// 存取控制是唯一的例外，因此單獨釘在這裡。
    /// </para>
    /// <para>
    /// 新增 API 方法一樣會讓本測試紅。那是刻意的：補上一列的同時，會被迫回答
    /// 「這個方法該讓誰、以什麼保護等級呼叫」，而不是沿用複製來的 attribute。
    /// </para>
    /// </remarks>
    public class ApiAccessControlPinTests
    {
        /// <summary>
        /// 期望值刻意寫**字串**而非 <c>ApiProtectionLevel.Public</c> 這樣的列舉引用。
        /// 用列舉引用的話，把成員改名或換掉它的語意，這裡會跟著改、測試照過；
        /// 寫字串才能同時抓到「換成別的等級」與「等級被改名」。
        /// </summary>
        private static readonly Dictionary<string, (string Protection, string Requirement)> s_expected = new(StringComparer.Ordinal)
        {
            { "BusinessObject.ExecFunc", ("Public", "Authenticated") },
            { "BusinessObject.ExecFuncAnonymous", ("Public", "Anonymous") },
            { "FormBusinessObject.Delete", ("Public", "Authenticated") },
            { "FormBusinessObject.GetData", ("Public", "Authenticated") },
            { "FormBusinessObject.GetList", ("Public", "Authenticated") },
            { "FormBusinessObject.GetLookup", ("Public", "Authenticated") },
            { "FormBusinessObject.GetNewData", ("Public", "Authenticated") },
            { "FormBusinessObject.Save", ("Public", "Authenticated") },
            { "LogBusinessObject.GetAccessLog", ("Encrypted", "Authenticated") },
            { "LogBusinessObject.GetApiAnomalyLog", ("Encrypted", "Authenticated") },
            { "LogBusinessObject.GetApiAnomalySummary", ("Encrypted", "Authenticated") },
            { "LogBusinessObject.GetChangeDetail", ("Encrypted", "Authenticated") },
            { "LogBusinessObject.GetChangeLog", ("Encrypted", "Authenticated") },
            { "LogBusinessObject.GetDbAnomalyLog", ("Encrypted", "Authenticated") },
            { "LogBusinessObject.GetDbAnomalySummary", ("Encrypted", "Authenticated") },
            { "LogBusinessObject.GetLoginLog", ("Encrypted", "Authenticated") },
            { "LogBusinessObject.GetTopApiMethods", ("Encrypted", "Authenticated") },
            { "SystemBusinessObject.CheckPackageUpdate", ("Encoded", "Anonymous") },
            { "SystemBusinessObject.CreateApiKey", ("Encrypted", "Authenticated") },
            { "SystemBusinessObject.CreateSession", ("LocalOnly", "Anonymous") },
            { "SystemBusinessObject.EnterCompany", ("Public", "Authenticated") },
            { "SystemBusinessObject.GetCommonConfiguration", ("Public", "Anonymous") },
            { "SystemBusinessObject.GetCustomizeFormLayout", ("Public", "Authenticated") },
            { "SystemBusinessObject.GetCustomizeLanguage", ("Public", "Authenticated") },
            { "SystemBusinessObject.GetCustomizePluginSettings", ("LocalOnly", "Authenticated") },
            { "SystemBusinessObject.GetDefine", ("Public", "Authenticated") },
            { "SystemBusinessObject.GetDepartmentTree", ("Public", "Authenticated") },
            { "SystemBusinessObject.GetFormLayout", ("Public", "Authenticated") },
            { "SystemBusinessObject.GetFormSchema", ("Public", "Authenticated") },
            { "SystemBusinessObject.GetLanguage", ("Public", "Authenticated") },
            { "SystemBusinessObject.GetPackage", ("Encoded", "Anonymous") },
            { "SystemBusinessObject.LeaveCompany", ("Public", "Authenticated") },
            { "SystemBusinessObject.ListApiKeys", ("Encrypted", "Authenticated") },
            { "SystemBusinessObject.Login", ("Public", "Anonymous") },
            { "SystemBusinessObject.Logout", ("Public", "Authenticated") },
            { "SystemBusinessObject.Ping", ("Public", "Anonymous") },
            { "SystemBusinessObject.SaveCustomizePluginSettings", ("LocalOnly", "Authenticated") },
            { "SystemBusinessObject.SaveDefine", ("LocalOnly", "Authenticated") },
            { "SystemBusinessObject.SetApiKeyEnabled", ("Encrypted", "Authenticated") },
            { "SystemBusinessObject.SetApiKeyExpiry", ("Encrypted", "Authenticated") },
            { "SystemBusinessObject.SetDeploymentAdmin", ("LocalOnly", "Authenticated") },
        };

        /// <summary>
        /// 以與 <see cref="JsonRpcExecutor"/> 相同的解析規則掃出實際的存取控制宣告。
        /// </summary>
        /// <remarks>
        /// 走 <see cref="ApiAccessValidator.FindAccessControl"/> 而不是自己讀 attribute：
        /// 那裡才有 method → base method → declaring type 的優先序，而執行期用的正是它。
        /// </remarks>
        private static Dictionary<string, (string Protection, string Requirement)> Actual()
        {
            return typeof(BusinessObject).Assembly.GetTypes()
                .Where(t => typeof(BusinessObject).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(m => !m.IsSpecialName)
                .Select(m => (Method: m, Attr: ApiAccessValidator.FindAccessControl(m)))
                .Where(x => x.Attr != null)
                .ToDictionary(
                    x => $"{x.Method.DeclaringType!.Name}.{x.Method.Name}",
                    x => (x.Attr!.ProtectionLevel.ToString(), x.Attr.AccessRequirement.ToString()),
                    StringComparer.Ordinal);
        }

        [Fact]
        [DisplayName("API 方法的保護等級與驗證需求都不得在無人察覺下變動")]
        public void AccessControl_MatchesPinnedDeclarations()
        {
            var actual = Actual();

            // 掃不到任何方法代表掃描本身壞了（型別搬家、繼承關係變更），
            // 而不是「全部合規」——沒有這一條，下面的比對會一起變成恆真。
            Assert.NotEmpty(actual);

            var problems = new List<string>();

            foreach (var (name, expected) in s_expected)
            {
                if (!actual.TryGetValue(name, out var found))
                {
                    problems.Add($"{name}：已釘住但掃不到（方法被移除或改名？既有客戶端會收到 MethodNotFound）");
                    continue;
                }

                if (found != expected)
                {
                    problems.Add(
                        $"{name}：宣告由 ({expected.Protection}, {expected.Requirement}) 變成 " +
                        $"({found.Protection}, {found.Requirement})");
                }
            }

            foreach (var name in actual.Keys.Where(k => !s_expected.ContainsKey(k)))
            {
                var found = actual[name];
                problems.Add($"{name}：新的 API 方法（{found.Protection}, {found.Requirement}），尚未釘住");
            }

            Assert.True(problems.Count == 0,
                "API 存取控制宣告與釘住的清單不符：" + global::System.Environment.NewLine +
                string.Join(global::System.Environment.NewLine, problems) + global::System.Environment.NewLine +
                global::System.Environment.NewLine +
                "調高保護等級或改用 Authenticated，會讓既有客戶端當場被拒——包含不隨框架一起發版的" +
                "前端。確認過影響後再更新本清單；新增方法則是被要求回答一次「這該讓誰呼叫」。");
        }
    }
}
