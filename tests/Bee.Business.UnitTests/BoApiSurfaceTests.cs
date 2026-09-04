using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Bee.Definition.Attributes;
using Bee.Definition.Security;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// Reflective audit of the BO public API surface. Locks the set of
    /// <c>[ApiAccessControl]</c>-decorated public methods on
    /// <see cref="BusinessObject"/> and its derivatives against a hard-coded
    /// baseline so additions / removals / access-level changes always require
    /// an intentional baseline update.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Whenever this test fails:
    /// </para>
    /// <list type="number">
    /// <item><description>Decide whether the change is intentional. Renames /
    ///   removals / access tightening or loosening must all be reviewed at the
    ///   security level.</description></item>
    /// <item><description>Update the <see cref="s_expectedSurface"/> baseline
    ///   below to match the new API surface.</description></item>
    /// <item><description>Update <c>docs/api-method-reference.md</c>
    ///   (and the zh-TW counterpart) so the human-facing reference does not
    ///   drift from the code.</description></item>
    /// </list>
    /// <para>
    /// The <c>bee-add-bo-method</c> skill checklist references this test —
    /// adding a new BO method without updating the baseline will fail CI.
    /// </para>
    /// </remarks>
    public partial class BoApiSurfaceTests
    {
        /// <summary>
        /// Canonical list of every public API method currently exposed by
        /// <c>Bee.Business</c>, identified by <c>{DeclaringType.Name}.{MethodName}</c>.
        /// Sorted alphabetically for stable diffs.
        /// </summary>
        private static readonly IReadOnlyList<ApiSurfaceEntry> s_expectedSurface = new[]
        {
            // Base axis — defined on BusinessObject, inherited by every BO.
            new ApiSurfaceEntry("BusinessObject", "ExecFunc",          ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated, ApiReplayProtection.UniqueSequence),
            new ApiSurfaceEntry("BusinessObject", "ExecFuncAnonymous", ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous),

            // Form axis — FormBusinessObject (FormSchema-driven CRUD).
            new ApiSurfaceEntry("FormBusinessObject", "Delete",     ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated, ApiReplayProtection.UniqueSequence),
            new ApiSurfaceEntry("FormBusinessObject", "GetData",    ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("FormBusinessObject", "GetList",    ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("FormBusinessObject", "GetLookup",  ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("FormBusinessObject", "GetNewData", ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("FormBusinessObject", "Save",       ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated, ApiReplayProtection.UniqueSequence),

            // Audit-log axis — LogBusinessObject (read-only queries over st_log_*).
            new ApiSurfaceEntry("LogBusinessObject", "GetAccessLog",        ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetApiAnomalyLog",    ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetApiAnomalySummary",ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetChangeDetail",     ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetChangeLog",        ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetDbAnomalyLog",     ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetDbAnomalySummary", ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetLoginLog",         ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetTopApiMethods",    ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),

            // System axis — SystemBusinessObject (system-level operations).
            // Encrypted（原為 LocalOnly）：把關移交 IDeploymentAuthorizationService —— 遠端須是
            // 部署層管理員，僅「已驗證」仍不足。本機呼叫免管理員，維持首把金鑰的 bootstrap 路徑。
            new ApiSurfaceEntry("SystemBusinessObject", "CreateApiKey",           ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            // LocalOnly：從 UserID 直接發 token、不驗憑證，屬受信任呼叫端操作。
            // 先前為 Public + Anonymous，未被利用只是因為 SessionInfoCache.CreateInstance 尚未實作。
            new ApiSurfaceEntry("SystemBusinessObject", "CreateSession",          ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "EnterCompany",           ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated, ApiReplayProtection.UniqueSequence),
            new ApiSurfaceEntry("SystemBusinessObject", "GetCommonConfiguration", ApiProtectionLevel.Public,  ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "GetCustomizeFormLayout", ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetCustomizeLanguage",   ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetCustomizePluginSettings", ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetDefine",              ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetDepartmentTree",      ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetFormLayout",          ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetFormSchema",          ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetLanguage",            ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetPackage",             ApiProtectionLevel.Encoded, ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "LeaveCompany",           ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated, ApiReplayProtection.UniqueSequence),
            // 以下三個與 CreateApiKey 同一把關：金鑰屬整個部署，遠端須是部署層管理員，
            // 本機直通以保住 bootstrap。ListApiKeys 不回傳雜湊。
            new ApiSurfaceEntry("SystemBusinessObject", "ListApiKeys",           ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "Login",                  ApiProtectionLevel.Public,  ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "Logout",                 ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "Ping",                   ApiProtectionLevel.Public,  ApiAccessRequirement.Anonymous),
            // LocalOnly：寫入定義是部署期作業。先前僅擋 SystemSettings / DatabaseSettings，
            // 其餘定義型別（含 PermissionModels、DbCategorySettings、FormSchema）任何已驗證帳號皆可覆寫。
            new ApiSurfaceEntry("SystemBusinessObject", "SaveCustomizePluginSettings", ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "SaveDefine",             ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "SetApiKeyEnabled",      ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "SetApiKeyExpiry",       ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            // LocalOnly：指派部署層管理員是提權動作，屬部署期作業。理由同 SaveDefine / CreateApiKey
            // —— 僅「已驗證」的遠端帳號不該能把自己或他人升為管理員。
            new ApiSurfaceEntry("SystemBusinessObject", "SetDeploymentAdmin",     ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Authenticated),
        };

        [Fact]
        [DisplayName("BO API 公開介面應與 baseline + docs/api-method-reference.md 同步")]
        public void PublicApiSurface_MatchesBaseline()
        {
            var actual = ScanBusinessAssembly();

            string expectedDump = FormatSurface(s_expectedSurface);
            string actualDump = FormatSurface(actual);

            // Equality on the formatted dumps gives a clear diff in the xUnit
            // failure message — much easier to read than collection asserts.
            Assert.Equal(expectedDump, actualDump);
        }

        /// <summary>
        /// baseline 的每一列都必須出現在雙語的 <c>docs/api-method-reference</c>，反之亦然。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 這個測試的 DisplayName 與那兩份文件開頭都寫著「由本測試保持同步、否則 build 會失敗」，
        /// 但在 2026-09-04 之前<b>本檔完全沒有讀過任何檔案</b>——同步靠的是紀律，不是機制。
        /// 那句話是對外文件裡的一個保證，所以要嘛補上機制、要嘛改掉那句話；這裡選前者。
        /// </para>
        /// <para>
        /// 比對的是 <c>(方法, Protection, Auth)</c> 三元組而不是逐行字串：文件按軸分節、
        /// 帶 Purpose 欄與散文，逐行比對會被無關的排版變動打斷。方法名在 baseline 與文件中
        /// 都不重複（本測試若因為出現同名方法而失真，那件事本身也該被看見）。
        /// </para>
        /// </remarks>
        [Theory]
        [InlineData("api-method-reference.md")]
        [InlineData("api-method-reference.zh-TW.md")]
        [DisplayName("BO API baseline 應與 docs/api-method-reference 雙語逐項一致")]
        public void Baseline_MatchesPublicMethodReference(string fileName)
        {
            string path = Path.Combine(FindRepoRoot(), "docs", fileName);
            Assert.True(File.Exists(path), $"找不到 {path}。");

            var documented = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in DocRowPattern().Matches(File.ReadAllText(path)))
                documented.Add($"{m.Groups[1].Value} | {m.Groups[2].Value} | {m.Groups[3].Value}");

            // 防空轉：正則對不上文件格式時，下面的集合相等會變成「兩邊都空」而恆綠。
            Assert.NotEmpty(documented);

            var expected = new HashSet<string>(
                s_expectedSurface.Select(e => $"{e.Method} | {e.ProtectionLevel} | {e.AccessRequirement}"),
                StringComparer.Ordinal);

            var missing = expected.Except(documented).OrderBy(x => x, StringComparer.Ordinal).ToList();
            var extra = documented.Except(expected).OrderBy(x => x, StringComparer.Ordinal).ToList();

            Assert.True(
                missing.Count == 0 && extra.Count == 0,
                $"{fileName} 與 baseline 不同步。\n文件缺少：\n  {string.Join("\n  ", missing)}\n" +
                $"文件多出（或欄位值不符）：\n  {string.Join("\n  ", extra)}");
        }

        /// <summary>
        /// 宣告了重放防護的方法，必須與雙語文件的〈Replay protection〉清單一致。
        /// </summary>
        /// <remarks>
        /// 這一欄是 4.26.0 新增的第三個存取維度，對客戶端作者是行為契約（不送遞增序號就收
        /// <c>-32005 ReplayRejected</c>）。上面的三元組比對刻意不含它——文件那份是條列而非表格欄，
        /// 而把它硬塞進表格會讓四十列各多寫一次 <c>None</c>。
        /// </remarks>
        [Theory]
        [InlineData("api-method-reference.md")]
        [InlineData("api-method-reference.zh-TW.md")]
        [DisplayName("宣告重放防護的方法應與 docs/api-method-reference 的清單一致")]
        public void ReplayProtectedMethods_MatchPublicMethodReference(string fileName)
        {
            string text = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", fileName));

            var expected = s_expectedSurface
                .Where(e => e.ReplayProtection == ApiReplayProtection.UniqueSequence)
                .Select(e => e.Method)
                .OrderBy(m => m, StringComparer.Ordinal)
                .ToList();

            // 防空轉：baseline 一個都沒標時，下面的「每個都找得到」會恆真。
            Assert.NotEmpty(expected);

            var documented = ReplayListPattern().Matches(text)
                .Select(m => m.Groups[1].Value)
                .OrderBy(m => m, StringComparer.Ordinal)
                .ToList();

            Assert.Equal(expected, documented);
        }

        /// <summary>
        /// 〈Replay protection〉/〈重放防護〉一節內的方法名。
        /// </summary>
        [GeneratedRegex(@"^- `(\w+)`\s*$", RegexOptions.Multiline)]
        private static partial Regex ReplayListPattern();

        /// <summary>
        /// 文件表格的一列：<c>| `Method` | Protection | Auth | Purpose |</c>。
        /// </summary>
        [GeneratedRegex(@"^\|\s*`(\w+)`\s*\|\s*(\w+)\s*\|\s*(\w+)\s*\|", RegexOptions.Multiline)]
        private static partial Regex DocRowPattern();

        /// <summary>
        /// 由測試組件位置往上找出 repo 根目錄。
        /// </summary>
        /// <remarks>
        /// 與 <c>TestProcessBootstrap</c> 的同名私有方法重複約八行。刻意不把那個改成 public：
        /// 那支是 fixture 的啟動路徑，公開它會讓「測試要不要碰 repo 檔案」變成一個開放邀請，
        /// 而這裡只需要唯讀地找一份文件。
        /// </remarks>
        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (dir.GetDirectories(".git").Length > 0) { return dir.FullName; }
                dir = dir.Parent;
            }
            throw new InvalidOperationException($"找不到 repo 根目錄：{AppContext.BaseDirectory}");
        }

        /// <summary>
        /// Reflects over the <c>Bee.Business</c> assembly and collects every
        /// public method decorated with <see cref="ApiAccessControlAttribute"/>.
        /// </summary>
        private static List<ApiSurfaceEntry> ScanBusinessAssembly()
        {
            var assembly = typeof(BusinessObject).Assembly;
            var entries = new List<ApiSurfaceEntry>();

            foreach (var type in assembly.GetTypes())
            {
                // Skip abstract base helpers / nested compiler-generated types — only
                // concrete BO surfaces ship API methods.
                if (!type.IsPublic || type.IsAbstract && type.IsSealed)
                    continue;

                // DeclaredOnly: skip inherited methods so an attribute on the base
                // (e.g. BusinessObject.ExecFunc) shows up exactly once, on BusinessObject.
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var attr = method.GetCustomAttribute<ApiAccessControlAttribute>(inherit: false);
                    if (attr is null)
                        continue;
                    entries.Add(new ApiSurfaceEntry(
                        type.Name, method.Name, attr.ProtectionLevel, attr.AccessRequirement, attr.ReplayProtection));
                }
            }

            // Stable sort for deterministic dump output.
            entries.Sort((a, b) =>
            {
                int byType = string.CompareOrdinal(a.Type, b.Type);
                return byType != 0 ? byType : string.CompareOrdinal(a.Method, b.Method);
            });
            return entries;
        }

        private static string FormatSurface(IEnumerable<ApiSurfaceEntry> entries)
        {
            return string.Join('\n', entries.Select(e =>
                $"{e.Type}.{e.Method} | {e.ProtectionLevel} | {e.AccessRequirement} | {e.ReplayProtection}"));
        }

        /// <summary>
        /// baseline 的一列。<paramref name="ReplayProtection"/> 帶預設值只是為了讓 baseline 不必
        /// 為四十列各寫一次 <c>None</c>——它擋不住任何變更：實際值一律來自反射掃描，
        /// 原始碼把 <c>UniqueSequence</c> 拿掉時掃描結果就與這裡明寫的值對不上。
        /// </summary>
        private readonly record struct ApiSurfaceEntry(
            string Type,
            string Method,
            ApiProtectionLevel ProtectionLevel,
            ApiAccessRequirement AccessRequirement,
            ApiReplayProtection ReplayProtection = ApiReplayProtection.None);
    }
}
