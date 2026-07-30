using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE2001（FormSchema 的表必須登記於所宣告 scope）測試。
    /// </summary>
    public class FormSchemaTableRegistrationAnalyzerTests
    {
        private const string SchemaPath = "Define/FormSchema/Product.FormSchema.xml";
        private const string SettingsPath = "Define/DbCategorySettings.xml";

        private const string Settings = """
            <?xml version="1.0" encoding="utf-8"?>
            <DbCategorySettings>
              <Categories>
                <DbCategory Id="common" DisplayName="共用資料庫">
                  <Tables>
                    <TableItem TableName="st_user" />
                  </Tables>
                </DbCategory>
                <DbCategory Id="company" DisplayName="公司資料庫">
                  <Tables>
                    <TableItem TableName="ft_product" />
                    <TableItem TableName="ft_order" />
                    <TableItem TableName="ft_order_detail" />
                  </Tables>
                </DbCategory>
              </Categories>
            </DbCategorySettings>
            """;

        [Fact]
        [DisplayName("表登記於其他 scope 時應報 BEE2001 並指出實際 scope")]
        public void TableRegisteredUnderAnotherScope_ReportsDiagnosticNamingActualScope()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="common">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product" />
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new FormSchemaTableRegistrationAnalyzer(),
                (SchemaPath, xml),
                (SettingsPath, Settings));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE2001", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'ft_product'", message, StringComparison.Ordinal);
            Assert.Contains("It is registered under 'company'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("表完全未登記時應報 BEE2001 並建議新增 TableItem")]
        public void TableNotRegisteredAnywhere_ReportsDiagnosticSuggestingRegistration()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Invoice" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Invoice" DbTableName="ft_invoice" />
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new FormSchemaTableRegistrationAnalyzer(),
                ("Define/FormSchema/Invoice.FormSchema.xml", xml),
                (SettingsPath, Settings));

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("add a TableItem for 'ft_invoice'", message, StringComparison.Ordinal);
            Assert.Contains("'company'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("表正確登記於所宣告 scope 時不應報診斷")]
        public void TableRegisteredUnderDeclaredScope_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product" />
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new FormSchemaTableRegistrationAnalyzer(),
                (SchemaPath, xml),
                (SettingsPath, Settings));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("master-detail 多個 FormTable 應逐一檢查")]
        public void MultipleFormTables_ChecksEach()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Order" DbTableName="ft_order" />
                    <FormTable TableName="OrderDetail" DbTableName="ft_order_detail" />
                    <FormTable TableName="OrderMemo" DbTableName="ft_order_memo" />
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new FormSchemaTableRegistrationAnalyzer(),
                ("Define/FormSchema/Order.FormSchema.xml", xml),
                (SettingsPath, Settings));

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'ft_order_memo'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("無 DbCategorySettings.xml 時應整組靜默（定義可能存於資料庫）")]
        public void MissingDbCategorySettings_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Invoice" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Invoice" DbTableName="ft_invoice" />
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new FormSchemaTableRegistrationAnalyzer(),
                ("Define/FormSchema/Invoice.FormSchema.xml", xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("CategoryId 非法時應交由 BEE1001 處理，不重複報告")]
        public void InvalidCategoryId_DefersToBee1001()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="business">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product" />
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new FormSchemaTableRegistrationAnalyzer(),
                (SchemaPath, xml),
                (SettingsPath, Settings));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("診斷位置應指向 DbTableName 屬性")]
        public void Diagnostic_LocatesDbTableNameAttribute()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="common">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product" />
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new FormSchemaTableRegistrationAnalyzer(),
                (SchemaPath, xml),
                (SettingsPath, Settings));

            // Assert
            var lineSpan = Assert.Single(diagnostics).Location.GetLineSpan();
            Assert.Equal(SchemaPath, lineSpan.Path);

            // 第 4 行（0-based index 3）為 FormTable 節點。
            Assert.Equal(3, lineSpan.StartLinePosition.Line);

            var line = xml.Split('\n')[3];
            var expectedStart = line.IndexOf("DbTableName", StringComparison.Ordinal);
            Assert.Equal(expectedStart, lineSpan.StartLinePosition.Character);
        }

        [Fact]
        [DisplayName("表名大小寫不符時不報，避免誤判掩蓋真正原因")]
        public void TableNameCasingMismatch_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="FT_PRODUCT" />
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new FormSchemaTableRegistrationAnalyzer(),
                (SchemaPath, xml),
                (SettingsPath, Settings));

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
