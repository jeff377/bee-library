using System.ComponentModel;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// <see cref="VersionInfo"/> 純讀取 entry assembly metadata,smoke 測試確保不拋例外。
    /// </summary>
    public class VersionInfoTests
    {
        [Fact]
        [DisplayName("VersionInfo.Product 應回傳非空字串")]
        public void Product_ReturnsNonEmptyString()
        {
            Assert.False(string.IsNullOrEmpty(VersionInfo.Product));
        }

        [Fact]
        [DisplayName("VersionInfo.Version 應回傳非空字串")]
        public void Version_ReturnsNonEmptyString()
        {
            Assert.False(string.IsNullOrEmpty(VersionInfo.Version));
        }

        [Fact]
        [DisplayName("VersionInfo.AssemblyVersion 應回傳非空字串")]
        public void AssemblyVersion_ReturnsNonEmptyString()
        {
            Assert.False(string.IsNullOrEmpty(VersionInfo.AssemblyVersion));
        }
    }
}
