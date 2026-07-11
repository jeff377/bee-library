using System.ComponentModel;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// <see cref="VersionInfo"/> 逐一覆蓋每個公開屬性的 getter 與其 fallback 分支。
    /// 這些屬性讀取 entry assembly metadata,無法在 runtime 變更,故以「不拋例外 + 回傳合理形狀」為斷言。
    /// </summary>
    public class VersionInfoCoverageTests
    {
        [Fact]
        [DisplayName("VersionInfo.Product getter 不應拋例外且回傳非 null")]
        public void Product_Getter_ReturnsNonNull()
        {
            var value = VersionInfo.Product;

            Assert.NotNull(value);
        }

        [Fact]
        [DisplayName("VersionInfo.Company getter 不應拋例外且回傳非 null")]
        public void Company_Getter_ReturnsNonNull()
        {
            var value = VersionInfo.Company;

            Assert.NotNull(value);
        }

        [Fact]
        [DisplayName("VersionInfo.Description getter 不應拋例外且回傳非 null")]
        public void Description_Getter_ReturnsNonNull()
        {
            var value = VersionInfo.Description;

            Assert.NotNull(value);
        }

        [Fact]
        [DisplayName("VersionInfo.Version getter 應回傳已剝除 Git hash 的版本字串")]
        public void Version_Getter_HasNoGitHashSuffix()
        {
            var value = VersionInfo.Version;

            Assert.NotNull(value);
            Assert.DoesNotContain("+", value);
        }

        [Fact]
        [DisplayName("VersionInfo.FileVersion getter 不應拋例外且回傳非 null")]
        public void FileVersion_Getter_ReturnsNonNull()
        {
            var value = VersionInfo.FileVersion;

            Assert.NotNull(value);
        }

        [Fact]
        [DisplayName("VersionInfo.AssemblyVersion getter 不應拋例外且回傳非 null")]
        public void AssemblyVersion_Getter_ReturnsNonNull()
        {
            var value = VersionInfo.AssemblyVersion;

            Assert.NotNull(value);
        }

        [Fact]
        [DisplayName("VersionInfo.FullInformationalVersion getter 不應拋例外且回傳非 null")]
        public void FullInformationalVersion_Getter_ReturnsNonNull()
        {
            var value = VersionInfo.FullInformationalVersion;

            Assert.NotNull(value);
        }

        [Fact]
        [DisplayName("VersionInfo 所有公開屬性連續讀取不應拋例外")]
        public void AllProperties_SequentialRead_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                _ = VersionInfo.Product;
                _ = VersionInfo.Company;
                _ = VersionInfo.Description;
                _ = VersionInfo.Version;
                _ = VersionInfo.FileVersion;
                _ = VersionInfo.AssemblyVersion;
                _ = VersionInfo.FullInformationalVersion;
            });

            Assert.Null(exception);
        }
    }
}
