using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE1005（關聯對應必須指向已宣告欄位）與 BEE1006（關聯欄位應有對應寫入）測試。
    /// </summary>
    public class RelationMappingAnalyzerTests
    {
        private const string SchemaPath = "Define/FormSchema/Product.FormSchema.xml";

        /// <summary>
        /// 正確接線的關聯欄位：mapping 寫入 ref_supplier_id，且該欄位以 RelationField 宣告。
        /// </summary>
        private const string WellFormed = """
            <?xml version="1.0" encoding="utf-8"?>
            <FormSchema ProgId="Product" CategoryId="company">
              <Tables>
                <FormTable TableName="Product" DbTableName="ft_product">
                  <Fields>
                    <FormField FieldName="supplier_rowid" DbType="Guid" RelationProgId="Supplier">
                      <RelationFieldMappings>
                        <FieldMapping SourceField="sys_id" DestinationField="ref_supplier_id" />
                      </RelationFieldMappings>
                    </FormField>
                    <FormField FieldName="ref_supplier_id" DbType="String" Type="RelationField" />
                  </Fields>
                </FormTable>
              </Tables>
            </FormSchema>
            """;

        [Fact]
        [DisplayName("DestinationField 指向未宣告欄位應報 BEE1005")]
        public void UnknownDestinationField_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="supplier_rowid" DbType="Guid" RelationProgId="Supplier">
                          <RelationFieldMappings>
                            <FieldMapping SourceField="sys_id" DestinationField="ref_supplier_code" />
                          </RelationFieldMappings>
                        </FormField>
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new RelationMappingAnalyzer(), (SchemaPath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1005", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Contains("'ref_supplier_code'", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("RelationField 無任何 mapping 寫入應報 BEE1006")]
        public void UnmappedRelationField_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="ref_supplier_name" DbType="String" Type="RelationField" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new RelationMappingAnalyzer(), (SchemaPath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1006", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Contains("'ref_supplier_name'", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("正確接線的關聯欄位不應報任何診斷")]
        public void WellFormedRelation_ReportsNothing()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(new RelationMappingAnalyzer(), (SchemaPath, WellFormed));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非 RelationField 的欄位不受 BEE1006 約束")]
        public void NonRelationField_IsNotRequiredToBeMapped()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
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
            var diagnostics = AnalyzerRunner.Run(new RelationMappingAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("一個 mapping 目標不存在時，不應同時對同一欄位報 BEE1006")]
        public void MissingDestination_DoesNotAlsoReportUnmapped()
        {
            // ref_supplier_id 未宣告 → 僅 BEE1005；不應因「宣告的 RelationField 沒被寫入」再報一次。
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="supplier_rowid" DbType="Guid" RelationProgId="Supplier">
                          <RelationFieldMappings>
                            <FieldMapping SourceField="sys_id" DestinationField="ref_supplier_id" />
                          </RelationFieldMappings>
                        </FormField>
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new RelationMappingAnalyzer(), (SchemaPath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1005", diagnostic.Id);
        }
    }
}
