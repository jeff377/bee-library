using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Bee.Analyzers.UnitTests
{
    /// <summary>
    /// 供測試使用的 <see cref="AdditionalText"/> 實作，以記憶體字串模擬定義檔。
    /// </summary>
    internal sealed class TestAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="path">模擬的檔案路徑。</param>
        /// <param name="content">檔案內容。</param>
        public TestAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content);
        }

        /// <inheritdoc />
        public override string Path { get; }

        /// <inheritdoc />
        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}
