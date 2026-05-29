using System.ComponentModel;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// <see cref="CustomizeOnlyPathOptions"/> 路徑組合與 path traversal 防護測試。
    /// </summary>
    public class CustomizeOnlyPathOptionsTests
    {
        private const string CustomizeRoot = "/tmp/bee-customize-tests";
        private const string CustomizeId = "acme";

        [Fact]
        [DisplayName("GetProgramSettingsFilePath 應落在 {CustomizePath}/{customizeId}/ProgramSettings.xml")]
        public void GetProgramSettingsFilePath_ReturnsCustomizeRootedPath()
        {
            var paths = new CustomizeOnlyPathOptions(CustomizeRoot, CustomizeId);
            var expected = Path.Combine(Path.GetFullPath(Path.Combine(CustomizeRoot, CustomizeId)), "ProgramSettings.xml");
            Assert.Equal(expected, paths.GetProgramSettingsFilePath());
        }

        [Fact]
        [DisplayName("GetFormLayoutFilePath 應落在 {CustomizePath}/{customizeId}/FormLayout/<layoutId>.FormLayout.xml")]
        public void GetFormLayoutFilePath_ReturnsCustomizeRootedPath()
        {
            var paths = new CustomizeOnlyPathOptions(CustomizeRoot, CustomizeId);
            var root = Path.GetFullPath(Path.Combine(CustomizeRoot, CustomizeId));
            var expected = Path.Combine(root, "FormLayout", "EmployeeDefault.FormLayout.xml");
            Assert.Equal(expected, paths.GetFormLayoutFilePath("EmployeeDefault"));
        }

        [Fact]
        [DisplayName("GetLanguageFilePath 應落在 {CustomizePath}/{customizeId}/Language/<lang>/<ns>.Language.xml")]
        public void GetLanguageFilePath_ReturnsCustomizeRootedPath()
        {
            var paths = new CustomizeOnlyPathOptions(CustomizeRoot, CustomizeId);
            var root = Path.GetFullPath(Path.Combine(CustomizeRoot, CustomizeId));
            var expected = Path.Combine(root, "Language", "zh-TW", "Customer.Language.xml");
            Assert.Equal(expected, paths.GetLanguageFilePath("zh-TW", "Customer"));
        }

        [Theory]
        [InlineData("..")]
        [InlineData("../escape")]
        [InlineData("foo/bar")]
        [InlineData("foo\\bar")]
        [DisplayName("customizeId 含路徑跳脫字元應拋出 ArgumentException")]
        public void Constructor_CustomizeIdWithPathTraversal_ThrowsArgumentException(string customizeId)
        {
            Assert.Throws<ArgumentException>(() => new CustomizeOnlyPathOptions(CustomizeRoot, customizeId));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("customizeId 為空或空白應拋出 ArgumentException")]
        public void Constructor_EmptyCustomizeId_ThrowsArgumentException(string customizeId)
        {
            Assert.Throws<ArgumentException>(() => new CustomizeOnlyPathOptions(CustomizeRoot, customizeId));
        }

        [Fact]
        [DisplayName("customizePath 為空應拋出 ArgumentException")]
        public void Constructor_EmptyCustomizePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CustomizeOnlyPathOptions("", CustomizeId));
        }
    }
}
