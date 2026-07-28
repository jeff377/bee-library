using System.ComponentModel;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// <see cref="PathOptions"/> 對外部輸入路徑片段的防護測試。
    /// </summary>
    /// <remarks>
    /// 這些片段（progId / layoutId / categoryId / tableName / lang / ns）來自 API 引數與
    /// 反序列化後的定義物件，組成檔案路徑時屬不受信任輸入。
    /// </remarks>
    public class PathOptionsTraversalTests
    {
        private static PathOptions CreateOptions()
            => new PathOptions { DefinePath = Path.Combine(Path.GetTempPath(), "bee-define-root") };

        // The `params` constructor rather than a collection expression: the latter starts from a
        // parameterless `TheoryData<string>()`, which binds to the same `params` constructor with an
        // empty array and trips CA1825 on the analyzer set SonarCloud runs.
        public static TheoryData<string> HostilePathSegments()
            => new(
                "../../../etc/passwd",
                "..",
                "a/b",
                @"a\b",
                "/etc/cron.d/x");

        [Theory]
        [MemberData(nameof(HostilePathSegments))]
        [DisplayName("GetFormSchemaFilePath 對可逃逸的 progId 應拋 ArgumentException")]
        public void GetFormSchemaFilePath_HostileProgId_Throws(string progId)
        {
            Assert.Throws<ArgumentException>(() => CreateOptions().GetFormSchemaFilePath(progId));
        }

        [Theory]
        [MemberData(nameof(HostilePathSegments))]
        [DisplayName("GetFormLayoutFilePath 對可逃逸的 layoutId 應拋 ArgumentException")]
        public void GetFormLayoutFilePath_HostileLayoutId_Throws(string layoutId)
        {
            Assert.Throws<ArgumentException>(() => CreateOptions().GetFormLayoutFilePath(layoutId));
        }

        [Theory]
        [MemberData(nameof(HostilePathSegments))]
        [DisplayName("GetTableSchemaFilePath 對可逃逸的 categoryId 應拋 ArgumentException")]
        public void GetTableSchemaFilePath_HostileCategoryId_Throws(string categoryId)
        {
            Assert.Throws<ArgumentException>(() => CreateOptions().GetTableSchemaFilePath(categoryId, "Employee"));
        }

        [Theory]
        [MemberData(nameof(HostilePathSegments))]
        [DisplayName("GetLanguageFilePath 對可逃逸的 namespace 應拋 ArgumentException")]
        public void GetLanguageFilePath_HostileNamespace_Throws(string ns)
        {
            Assert.Throws<ArgumentException>(() => CreateOptions().GetLanguageFilePath("zh-TW", ns));
        }

        [Fact]
        [DisplayName("rooted 片段必須被擋——Path.Combine 會丟棄其前所有片段")]
        public void GetFormSchemaFilePath_RootedSegment_DoesNotEscapeRoot()
        {
            var options = CreateOptions();

            // 這是比 ".." 更隱蔽的破口：Path.Combine(a, b, "/x") 直接回傳 "/x"，
            // 完全不經過 DefinePath。若未擋下，檔案會落在檔案系統的任意位置。
            var ex = Assert.Throws<ArgumentException>(
                () => options.GetFormSchemaFilePath("/tmp/evil"));
            Assert.Contains("illegal path characters", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("Employee")]
        [InlineData("common")]
        [InlineData("zh-TW")]
        [InlineData("")]
        [DisplayName("合法片段應照常組出路徑")]
        public void ValidSegments_ProduceExpectedPath(string progId)
        {
            var options = CreateOptions();

            var path = options.GetFormSchemaFilePath(progId);

            Assert.StartsWith(options.DefinePath, path, StringComparison.Ordinal);
        }
    }
}
