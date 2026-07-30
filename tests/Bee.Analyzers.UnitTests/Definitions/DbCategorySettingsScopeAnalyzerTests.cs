using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE1002（DbCategory Id 必須為合法資料庫 scope）測試。
    /// </summary>
    public class DbCategorySettingsScopeAnalyzerTests
    {
        private const string SettingsPath = "Define/DbCategorySettings.xml";

        [Fact]
        [DisplayName("DbCategory Id 為未知值應報 BEE1002")]
        public void UnknownCategoryId_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <DbCategorySettings>
                  <Categories>
                    <DbCategory Id="archive" DisplayName="Archive">
                      <Tables />
                    </DbCategory>
                  </Categories>
                </DbCategorySettings>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new DbCategorySettingsScopeAnalyzer(), (SettingsPath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1002", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'archive'", message, StringComparison.Ordinal);
            Assert.Contains("'common', 'company', 'log'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("三個合法 scope 皆不應報診斷")]
        public void AllValidScopes_ReportNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <DbCategorySettings>
                  <Categories>
                    <DbCategory Id="common" />
                    <DbCategory Id="company" />
                    <DbCategory Id="log" />
                  </Categories>
                </DbCategorySettings>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new DbCategorySettingsScopeAnalyzer(), (SettingsPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("僅大小寫不符應指名正確拼法")]
        public void WrongCasing_NamesCorrectCasing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <DbCategorySettings>
                  <Categories>
                    <DbCategory Id="Log" />
                  </Categories>
                </DbCategorySettings>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new DbCategorySettingsScopeAnalyzer(), (SettingsPath, xml));

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("change it to 'log'", message, StringComparison.Ordinal);
        }
    }
}
