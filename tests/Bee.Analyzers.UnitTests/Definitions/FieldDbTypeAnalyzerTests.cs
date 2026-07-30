using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE1003（欄位 DbType 必須為框架列舉成員）測試。
    /// </summary>
    public class FieldDbTypeAnalyzerTests
    {
        private const string SchemaPath = "Define/FormSchema/Product.FormSchema.xml";
        private const string TablePath = "Define/TableSchema/company/ft_product.TableSchema.xml";

        [Fact]
        [DisplayName("FormField 的 DbType 為未知值應報 BEE1003")]
        public void UnknownFormFieldDbType_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="unit_price" DbType="Money" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldDbTypeAnalyzer(), (SchemaPath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1003", diagnostic.Id);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'unit_price'", message, StringComparison.Ordinal);
            Assert.Contains("'Money'", message, StringComparison.Ordinal);
            Assert.Contains("Currency", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("TableSchema 的 DbField 也應被檢查")]
        public void UnknownTableSchemaDbType_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <TableSchema TableName="ft_product">
                  <Fields>
                    <DbField FieldName="sys_id" DbType="Varchar" />
                  </Fields>
                </TableSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldDbTypeAnalyzer(), (TablePath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1003", diagnostic.Id);
            Assert.Contains("'Varchar'", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("String")]
        [InlineData("Currency")]
        [InlineData("AutoIncrement")]
        [InlineData("Time")]
        [DisplayName("合法 DbType 不應報診斷")]
        public void ValidDbType_ReportsNothing(string dbType)
        {
            var xml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="sample" DbType="{dbType}" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldDbTypeAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("僅大小寫不符應指名正確拼法")]
        public void WrongCasing_NamesCorrectCasing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="string" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldDbTypeAnalyzer(), (SchemaPath, xml));

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("case-sensitive", message, StringComparison.Ordinal);
            Assert.Contains("change it to 'String'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("未宣告 DbType 屬性不應報診斷")]
        public void MissingDbType_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="sys_id" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldDbTypeAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
