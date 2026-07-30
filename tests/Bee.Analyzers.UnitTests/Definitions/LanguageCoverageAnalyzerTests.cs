using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE2007（各語系應覆蓋相同翻譯 key）測試。
    /// </summary>
    public class LanguageCoverageAnalyzerTests
    {
        private const string ChinesePath = "Define/Language/zh-TW/Product.Language.xml";
        private const string EnglishPath = "Define/Language/en-US/Product.Language.xml";

        private static string Resource(string culture, params string[] keys)
        {
            var items = string.Join(
                "\n    ",
                keys.Select(key => $"<LanguageItem Key=\"{key}\" Value=\"text\" />"));

            return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <LanguageResource Namespace="Product" Lang="{culture}">
                  <Items>
                    {items}
                  </Items>
                </LanguageResource>
                """;
        }

        [Fact]
        [DisplayName("某語系缺少其他語系有的 key 應報 BEE2007")]
        public void MissingKeys_ReportsDiagnostic()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new LanguageCoverageAnalyzer(),
                (ChinesePath, Resource("zh-TW", "Schema.DisplayName", "Field.sys_id.Caption", "Field.sys_name.Caption")),
                (EnglishPath, Resource("en-US", "Schema.DisplayName")));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE2007", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'en-US'", message, StringComparison.Ordinal);
            Assert.Contains("missing 2 key(s)", message, StringComparison.Ordinal);
            Assert.Contains("Field.sys_id.Caption", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("各語系 key 一致時不應報診斷")]
        public void ConsistentCoverage_ReportsNothing()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new LanguageCoverageAnalyzer(),
                (ChinesePath, Resource("zh-TW", "Schema.DisplayName", "Field.sys_id.Caption")),
                (EnglishPath, Resource("en-US", "Schema.DisplayName", "Field.sys_id.Caption")));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("只有單一語系時無可比對對象，不應報診斷")]
        public void SingleCulture_ReportsNothing()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new LanguageCoverageAnalyzer(),
                (ChinesePath, Resource("zh-TW", "Schema.DisplayName", "Field.sys_id.Caption")));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("不同 Namespace 的語系檔應各自比對")]
        public void DifferentNamespaces_AreComparedSeparately()
        {
            const string orderResource = """
                <?xml version="1.0" encoding="utf-8"?>
                <LanguageResource Namespace="Order" Lang="zh-TW">
                  <Items>
                    <LanguageItem Key="Order.Only.Key" Value="text" />
                  </Items>
                </LanguageResource>
                """;

            // Act — Product 兩語系一致；Order 只有單一語系，不應因 Product 的 key 而被判缺漏。
            var diagnostics = AnalyzerRunner.Run(
                new LanguageCoverageAnalyzer(),
                (ChinesePath, Resource("zh-TW", "Schema.DisplayName")),
                (EnglishPath, Resource("en-US", "Schema.DisplayName")),
                ("Define/Language/zh-TW/Order.Language.xml", orderResource));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("缺漏過多時應摘要而非列出全部 key")]
        public void ManyMissingKeys_AreSummarised()
        {
            var manyKeys = Enumerable.Range(1, 10).Select(index => $"Field.f{index}.Caption").ToArray();

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new LanguageCoverageAnalyzer(),
                (ChinesePath, Resource("zh-TW", manyKeys)),
                (EnglishPath, Resource("en-US", "Field.f1.Caption")));

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("missing 9 key(s)", message, StringComparison.Ordinal);
            Assert.Contains("and 6 more", message, StringComparison.Ordinal);
        }
    }
}
