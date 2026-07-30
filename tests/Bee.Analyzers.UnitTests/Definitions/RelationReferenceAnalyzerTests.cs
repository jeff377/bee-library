using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE2003（RelationProgId 必須存在）與 BEE2004（SourceField 必須為被引用 schema 的欄位）測試。
    /// </summary>
    public class RelationReferenceAnalyzerTests
    {
        private const string ProductPath = "Define/FormSchema/Product.FormSchema.xml";
        private const string SupplierPath = "Define/FormSchema/Supplier.FormSchema.xml";

        private const string Supplier = """
            <?xml version="1.0" encoding="utf-8"?>
            <FormSchema ProgId="Supplier" CategoryId="company">
              <Tables>
                <FormTable TableName="Supplier" DbTableName="ft_supplier">
                  <Fields>
                    <FormField FieldName="sys_id" DbType="String" />
                    <FormField FieldName="sys_name" DbType="String" />
                  </Fields>
                </FormTable>
              </Tables>
            </FormSchema>
            """;

        private static string ProductReferencing(string relationProgId, string sourceField) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <FormSchema ProgId="Product" CategoryId="company">
              <Tables>
                <FormTable TableName="Product" DbTableName="ft_product">
                  <Fields>
                    <FormField FieldName="supplier_rowid" DbType="Guid" RelationProgId="{relationProgId}">
                      <RelationFieldMappings>
                        <FieldMapping SourceField="{sourceField}" DestinationField="ref_supplier_name" />
                      </RelationFieldMappings>
                    </FormField>
                    <FormField FieldName="ref_supplier_name" DbType="String" Type="RelationField" />
                  </Fields>
                </FormTable>
              </Tables>
            </FormSchema>
            """;

        [Fact]
        [DisplayName("RelationProgId 指向不存在的 schema 應報 BEE2003")]
        public void UnknownRelationProgId_ReportsDiagnostic()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new RelationReferenceAnalyzer(),
                (ProductPath, ProductReferencing("Vendor", "sys_name")),
                (SupplierPath, Supplier));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE2003", diagnostic.Id);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'Vendor'", message, StringComparison.Ordinal);
            Assert.Contains("'supplier_rowid'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("SourceField 不存在於被引用 schema 應報 BEE2004")]
        public void UnknownSourceField_ReportsDiagnostic()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new RelationReferenceAnalyzer(),
                (ProductPath, ProductReferencing("Supplier", "sys_title")),
                (SupplierPath, Supplier));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE2004", diagnostic.Id);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'sys_title'", message, StringComparison.Ordinal);
            Assert.Contains("'Supplier'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("關聯與來源欄位皆正確時不應報診斷")]
        public void ValidRelation_ReportsNothing()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new RelationReferenceAnalyzer(),
                (ProductPath, ProductReferencing("Supplier", "sys_name")),
                (SupplierPath, Supplier));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData("Employee")]
        [InlineData("Department")]
        [DisplayName("引用框架內建 schema 不應誤報（內建為內嵌資源、非消費端檔案）")]
        public void FrameworkSuppliedProgId_ReportsNothing(string progId)
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new RelationReferenceAnalyzer(),
                (ProductPath, ProductReferencing(progId, "sys_name")),
                (SupplierPath, Supplier));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("ProgId 不存在時不應同時報 BEE2004（來源欄位無從判定）")]
        public void UnknownProgId_DoesNotAlsoReportSourceField()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new RelationReferenceAnalyzer(),
                (ProductPath, ProductReferencing("Vendor", "does_not_exist")),
                (SupplierPath, Supplier));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE2003", diagnostic.Id);
        }

        [Fact]
        [DisplayName("無 RelationProgId 的欄位不受檢查")]
        public void FieldWithoutRelation_ReportsNothing()
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
            var diagnostics = AnalyzerRunner.Run(new RelationReferenceAnalyzer(), (ProductPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
