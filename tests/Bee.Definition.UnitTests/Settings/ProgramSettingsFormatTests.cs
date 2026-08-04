using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// 舊版巢狀 ProgramSettings.xml 的偵測（載入期 fail fast，避免靜默讀成空註冊表）。
    /// </summary>
    public class ProgramSettingsFormatTests
    {
        private const string LegacyXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <ProgramSettings>
              <Categories>
                <ProgramCategory Id="master-data" DisplayName="主檔">
                  <Items><ProgramItem ProgId="Customer" DisplayName="客戶" /></Items>
                </ProgramCategory>
              </Categories>
            </ProgramSettings>
            """;

        private const string CurrentXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <ProgramSettings>
              <Items><ProgramItem ProgId="Customer" DisplayName="客戶" /></Items>
            </ProgramSettings>
            """;

        [Fact]
        [DisplayName("舊版巢狀格式應被判定為 legacy")]
        public void IsLegacyFormat_NestedLayout_ReturnsTrue()
        {
            Assert.True(ProgramSettingsFormat.IsLegacyFormat(LegacyXml));
        }

        [Fact]
        [DisplayName("攤平後的格式不應被判定為 legacy")]
        public void IsLegacyFormat_FlatLayout_ReturnsFalse()
        {
            Assert.False(ProgramSettingsFormat.IsLegacyFormat(CurrentXml));
        }

        [Fact]
        [DisplayName("空註冊表（無任何子元素）不應被判定為 legacy")]
        public void IsLegacyFormat_EmptyRoot_ReturnsFalse()
        {
            Assert.False(ProgramSettingsFormat.IsLegacyFormat("<ProgramSettings />"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("空字串不應被判定為 legacy")]
        public void IsLegacyFormat_Empty_ReturnsFalse(string xml)
        {
            Assert.False(ProgramSettingsFormat.IsLegacyFormat(xml));
        }

        [Fact]
        [DisplayName("註解或屬性值中出現 Categories 字樣不應誤判為 legacy")]
        public void IsLegacyFormat_CategoriesOnlyInCommentOrAttribute_ReturnsFalse()
        {
            // A plain string match would hit both of these; only a real child element of the root
            // means the file is in the old shape.
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <!-- The Avalonia shell used to build its menu from these Categories. -->
                <ProgramSettings>
                  <Items><ProgramItem ProgId="Categories" DisplayName="Categories" /></Items>
                </ProgramSettings>
                """;

            Assert.False(ProgramSettingsFormat.IsLegacyFormat(xml));
        }

        [Fact]
        [DisplayName("EnsureCurrentFormat 對舊格式應拋出並指向遷移命令")]
        public void EnsureCurrentFormat_Legacy_ThrowsPointingAtMigration()
        {
            var ex = Assert.Throws<NotSupportedException>(
                () => ProgramSettingsFormat.EnsureCurrentFormat(LegacyXml, "/define/ProgramSettings.xml"));

            Assert.Contains("/define/ProgramSettings.xml", ex.Message, StringComparison.Ordinal);
            Assert.Contains("split-menu", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("EnsureCurrentFormat 對新格式不應拋出")]
        public void EnsureCurrentFormat_Current_DoesNotThrow()
        {
            var exception = Record.Exception(
                () => ProgramSettingsFormat.EnsureCurrentFormat(CurrentXml, "x.xml"));

            Assert.Null(exception);
        }
    }
}
