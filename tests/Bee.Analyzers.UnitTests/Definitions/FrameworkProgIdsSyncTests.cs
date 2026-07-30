using System.ComponentModel;
using Bee.Analyzers.Definitions;
using Bee.Definition;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// analyzer 內建 ProgId 白名單與框架實際內嵌的預設 FormSchema 的同步斷言。
    /// </summary>
    /// <remarks>
    /// BEE2003 需要區分「ProgId 真的不存在」與「ProgId 由框架以內嵌資源提供、消費端沒有對應檔案」。
    /// 後者若誤報會是 error 級誤判並擋下建置，因此白名單必須與框架實際內嵌內容一致。
    /// </remarks>
    public class FrameworkProgIdsSyncTests
    {
        [Fact]
        [DisplayName("analyzer 的內建 ProgId 白名單必須與框架內嵌的 FormSchema 一致")]
        public void All_MatchesEmbeddedFormSchemas()
        {
            // Arrange
            const string suffix = ".FormSchema.xml";
            var embedded = Defaults.ListEmbedded()
                .Where(path => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .Select(path => path.Substring(path.LastIndexOf('/') + 1))
                .Select(name => name.Substring(0, name.Length - suffix.Length))
                .OrderBy(progId => progId, StringComparer.Ordinal)
                .ToArray();

            // Act
            var whitelisted = FrameworkProgIds.All
                .OrderBy(progId => progId, StringComparer.Ordinal)
                .ToArray();

            // Assert
            Assert.NotEmpty(embedded);
            Assert.Equal(embedded, whitelisted);
        }

        [Fact]
        [DisplayName("非內建 ProgId 不應被視為框架提供")]
        public void IsFrameworkSupplied_RejectsConsumerProgIds()
        {
            // Assert
            Assert.False(FrameworkProgIds.IsFrameworkSupplied("Product"));
            Assert.False(FrameworkProgIds.IsFrameworkSupplied("Supplier"));
        }
    }
}
