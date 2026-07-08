using System.ComponentModel;
using System.Reflection;
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
    /// <item><description>Update the <see cref="ExpectedSurface"/> baseline
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
    public class BoApiSurfaceTests
    {
        /// <summary>
        /// Canonical list of every public API method currently exposed by
        /// <c>Bee.Business</c>, identified by <c>{DeclaringType.Name}.{MethodName}</c>.
        /// Sorted alphabetically for stable diffs.
        /// </summary>
        private static readonly IReadOnlyList<ApiSurfaceEntry> ExpectedSurface = new[]
        {
            // Base axis — defined on BusinessObject, inherited by every BO.
            new ApiSurfaceEntry("BusinessObject", "ExecFunc",          ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("BusinessObject", "ExecFuncAnonymous", ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous),

            // Form axis — FormBusinessObject (FormSchema-driven CRUD).
            new ApiSurfaceEntry("FormBusinessObject", "Delete",     ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("FormBusinessObject", "GetData",    ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("FormBusinessObject", "GetList",    ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("FormBusinessObject", "GetLookup",  ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("FormBusinessObject", "GetNewData", ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("FormBusinessObject", "Save",       ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated),

            // Audit-log axis — LogBusinessObject (read-only queries over st_log_*).
            new ApiSurfaceEntry("LogBusinessObject", "GetChangeDetail",  ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetChangeLog",     ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("LogBusinessObject", "GetRecordHistory", ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated),

            // System axis — SystemBusinessObject (system-level operations).
            new ApiSurfaceEntry("SystemBusinessObject", "CheckPackageUpdate",     ApiProtectionLevel.Encoded, ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "CreateSession",          ApiProtectionLevel.Public,  ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "EnterCompany",           ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetCommonConfiguration", ApiProtectionLevel.Public,  ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "GetDefine",              ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetDepartmentTree",      ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetFormLayout",          ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetFormSchema",          ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetLanguage",            ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "GetPackage",             ApiProtectionLevel.Encoded, ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "LeaveCompany",           ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "Login",                  ApiProtectionLevel.Public,  ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "Logout",                 ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
            new ApiSurfaceEntry("SystemBusinessObject", "Ping",                   ApiProtectionLevel.Public,  ApiAccessRequirement.Anonymous),
            new ApiSurfaceEntry("SystemBusinessObject", "SaveDefine",             ApiProtectionLevel.Public,  ApiAccessRequirement.Authenticated),
        };

        [Fact]
        [DisplayName("BO API 公開介面應與 baseline + docs/api-method-reference.md 同步")]
        public void PublicApiSurface_MatchesBaseline()
        {
            var actual = ScanBusinessAssembly();

            string expectedDump = FormatSurface(ExpectedSurface);
            string actualDump = FormatSurface(actual);

            // Equality on the formatted dumps gives a clear diff in the xUnit
            // failure message — much easier to read than collection asserts.
            Assert.Equal(expectedDump, actualDump);
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
                    entries.Add(new ApiSurfaceEntry(type.Name, method.Name, attr.ProtectionLevel, attr.AccessRequirement));
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
                $"{e.Type}.{e.Method} | {e.ProtectionLevel} | {e.AccessRequirement}"));
        }

        private readonly record struct ApiSurfaceEntry(
            string Type,
            string Method,
            ApiProtectionLevel ProtectionLevel,
            ApiAccessRequirement AccessRequirement);
    }
}
