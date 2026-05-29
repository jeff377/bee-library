using System.ComponentModel;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// <see cref="CustomizeOnlyPathOptions"/> 路徑組合與 path traversal 防護測試。
    /// </summary>
    public class CustomizeOnlyPathOptionsTests
    {
        private const string CustomizeRoot = "/tmp/bee-customize-tests";
        private const string CustCode = "acme";

        [Fact]
        [DisplayName("GetProgramSettingsFilePath 應落在 {CustomizePath}/{custCode}/ProgramSettings.xml")]
        public void GetProgramSettingsFilePath_ReturnsCustomizeRootedPath()
        {
            var paths = new CustomizeOnlyPathOptions(CustomizeRoot, CustCode);
            var expected = Path.Combine(Path.GetFullPath(Path.Combine(CustomizeRoot, CustCode)), "ProgramSettings.xml");
            Assert.Equal(expected, paths.GetProgramSettingsFilePath());
        }

        [Fact]
        [DisplayName("GetFormLayoutFilePath 應落在 {CustomizePath}/{custCode}/FormLayout/<layoutId>.FormLayout.xml")]
        public void GetFormLayoutFilePath_ReturnsCustomizeRootedPath()
        {
            var paths = new CustomizeOnlyPathOptions(CustomizeRoot, CustCode);
            var root = Path.GetFullPath(Path.Combine(CustomizeRoot, CustCode));
            var expected = Path.Combine(root, "FormLayout", "EmployeeDefault.FormLayout.xml");
            Assert.Equal(expected, paths.GetFormLayoutFilePath("EmployeeDefault"));
        }

        [Fact]
        [DisplayName("GetLanguageFilePath 應落在 {CustomizePath}/{custCode}/Language/<lang>/<ns>.Language.xml")]
        public void GetLanguageFilePath_ReturnsCustomizeRootedPath()
        {
            var paths = new CustomizeOnlyPathOptions(CustomizeRoot, CustCode);
            var root = Path.GetFullPath(Path.Combine(CustomizeRoot, CustCode));
            var expected = Path.Combine(root, "Language", "zh-TW", "Customer.Language.xml");
            Assert.Equal(expected, paths.GetLanguageFilePath("zh-TW", "Customer"));
        }

        [Theory]
        [InlineData("..")]
        [InlineData("../escape")]
        [InlineData("foo/bar")]
        [InlineData("foo\\bar")]
        [DisplayName("custCode 含路徑跳脫字元應拋出 ArgumentException")]
        public void Constructor_CustCodeWithPathTraversal_ThrowsArgumentException(string custCode)
        {
            Assert.Throws<ArgumentException>(() => new CustomizeOnlyPathOptions(CustomizeRoot, custCode));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("custCode 為空或空白應拋出 ArgumentException")]
        public void Constructor_EmptyCustCode_ThrowsArgumentException(string custCode)
        {
            Assert.Throws<ArgumentException>(() => new CustomizeOnlyPathOptions(CustomizeRoot, custCode));
        }

        [Fact]
        [DisplayName("customizePath 為空應拋出 ArgumentException")]
        public void Constructor_EmptyCustomizePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CustomizeOnlyPathOptions("", CustCode));
        }
    }
}
