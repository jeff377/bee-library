using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE1001（FormSchema CategoryId 必須為合法資料庫 scope）測試。
    /// </summary>
    public class FormSchemaCategoryIdAnalyzerTests
    {
        private const string SchemaPath = "Define/FormSchema/Product.FormSchema.xml";

        [Fact]
        [DisplayName("CategoryId 為未知值應報 BEE1001 並列出合法值")]
        public void UnknownCategoryId_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="business">
                  <Tables />
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FormSchemaCategoryIdAnalyzer(), (SchemaPath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1001", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'Product'", message, StringComparison.Ordinal);
            Assert.Contains("'business'", message, StringComparison.Ordinal);
            Assert.Contains("'common', 'company', 'log'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("CategoryId 僅大小寫不符應報 BEE1001 並指名正確拼法")]
        public void WrongCasingCategoryId_ReportsDiagnosticNamingCorrectCasing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="Company">
                  <Tables />
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FormSchemaCategoryIdAnalyzer(), (SchemaPath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1001", diagnostic.Id);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("ordinal", message, StringComparison.Ordinal);
            Assert.Contains("change it to 'company'", message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("common")]
        [InlineData("company")]
        [InlineData("log")]
        [DisplayName("CategoryId 為合法 scope 不應報診斷")]
        public void ValidCategoryId_ReportsNothing(string categoryId)
        {
            var xml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="{categoryId}">
                  <Tables />
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FormSchemaCategoryIdAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("診斷位置應指向 CategoryId 屬性本身")]
        public void Diagnostic_LocatesCategoryIdAttribute()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="business">
                  <Tables />
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FormSchemaCategoryIdAnalyzer(), (SchemaPath, xml));

            // Assert
            var lineSpan = Assert.Single(diagnostics).Location.GetLineSpan();
            Assert.Equal(SchemaPath, lineSpan.Path);

            // 第 2 行（0-based index 1）為 FormSchema 根節點。
            Assert.Equal(1, lineSpan.StartLinePosition.Line);

            // 位置應涵蓋 CategoryId="business" 整段，而非僅屬性名稱。
            var line = xml.Split('\n')[1];
            var expectedStart = line.IndexOf("CategoryId", StringComparison.Ordinal);
            Assert.Equal(expectedStart, lineSpan.StartLinePosition.Character);
            Assert.Equal(expectedStart + "CategoryId=\"business\"".Length, lineSpan.EndLinePosition.Character);
        }

        [Fact]
        [DisplayName("未宣告 CategoryId 屬性不應報診斷")]
        public void MissingCategoryId_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product">
                  <Tables />
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FormSchemaCategoryIdAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非 FormSchema 定義檔不應被檢查")]
        public void NonFormSchemaFile_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <TableSchema CategoryId="business" />
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new FormSchemaCategoryIdAnalyzer(),
                ("Define/TableSchema/company/ft_product.TableSchema.xml", xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("XML 格式錯誤應靜默跳過而非讓 analyzer 崩潰")]
        public void MalformedXml_ReportsNothingWithoutThrowing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="business"
                """;

            // Act
            var exception = Record.Exception(
                () => AnalyzerRunner.Run(new FormSchemaCategoryIdAnalyzer(), (SchemaPath, xml)));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("缺少 ProgId 屬性時應以檔名推導 ProgId")]
        public void MissingProgId_FallsBackToFileName()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema CategoryId="business">
                  <Tables />
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FormSchemaCategoryIdAnalyzer(), (SchemaPath, xml));

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'Product'", message, StringComparison.Ordinal);
        }
    }
}
