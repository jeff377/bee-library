using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE1004（欄位清單只能引用已宣告欄位）測試。
    /// </summary>
    public class FieldListReferenceAnalyzerTests
    {
        private const string SchemaPath = "Define/FormSchema/Product.FormSchema.xml";

        [Fact]
        [DisplayName("ListFields 引用未宣告欄位應報 BEE1004")]
        public void UnknownListField_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company" ListFields="sys_id,unit_prise">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                        <FormField FieldName="unit_price" DbType="Currency" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldListReferenceAnalyzer(), (SchemaPath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1004", diagnostic.Id);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'unit_prise'", message, StringComparison.Ordinal);
            Assert.Contains("ListFields", message, StringComparison.Ordinal);

            // ListFields 的未知欄位不是靜默跳過：版面少一欄，但 SelectBuilder 仍會拿到它並擲例外。
            Assert.Contains("InvalidOperationException", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("LookupFields 也應被檢查")]
        public void UnknownLookupField_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company" LookupFields="sys_id,sys_title">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldListReferenceAnalyzer(), (SchemaPath, xml));

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("LookupFields", message, StringComparison.Ordinal);

            // LookupFields 才是真的靜默跳過（GetLookupFields 以 master.Fields.Contains 過濾）。
            Assert.Contains("skipped silently", message, StringComparison.Ordinal);
            Assert.DoesNotContain("InvalidOperationException", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("master-detail 清單可混用不同表的欄位，不應誤報")]
        public void FieldsFromMultipleTables_ReportNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company" ListFields="sys_id,line_amount">
                  <Tables>
                    <FormTable TableName="Order" DbTableName="ft_order">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                      </Fields>
                    </FormTable>
                    <FormTable TableName="OrderDetail" DbTableName="ft_order_detail">
                      <Fields>
                        <FormField FieldName="line_amount" DbType="Currency" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldListReferenceAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("清單含空白與空項目應可容忍")]
        public void WhitespaceAndEmptyEntries_AreTolerated()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company" ListFields=" sys_id , , sys_name ">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                        <FormField FieldName="sys_name" DbType="String" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldListReferenceAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("未宣告 ListFields 屬性不應報診斷")]
        public void MissingListFields_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new FieldListReferenceAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
