using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE2006（持久化欄位必須存在於對應 TableSchema）測試。
    /// </summary>
    public class PersistedFieldAnalyzerTests
    {
        private const string SchemaPath = "Define/FormSchema/Product.FormSchema.xml";
        private const string TablePath = "Define/TableSchema/company/ft_product.TableSchema.xml";

        private const string TableSchema = """
            <?xml version="1.0" encoding="utf-8"?>
            <TableSchema TableName="ft_product">
              <Fields>
                <DbField FieldName="sys_id" DbType="String" />
                <DbField FieldName="sys_name" DbType="String" />
                <DbField FieldName="supplier_rowid" DbType="Guid" />
              </Fields>
            </TableSchema>
            """;

        [Fact]
        [DisplayName("持久化欄位不存在於 TableSchema 應報 BEE2006")]
        public void PersistedFieldMissingColumn_ReportsDiagnostic()
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
            var diagnostics = AnalyzerRunner.Run(
                new PersistedFieldAnalyzer(),
                (SchemaPath, xml),
                (TablePath, TableSchema));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE2006", diagnostic.Id);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'unit_price'", message, StringComparison.Ordinal);
            Assert.Contains("'ft_product'", message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("RelationField")]
        [InlineData("VirtualField")]
        [DisplayName("非持久化欄位不需存在於 TableSchema")]
        public void NonPersistedField_ReportsNothing(string fieldType)
        {
            var xml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                        <FormField FieldName="ref_supplier_name" DbType="String" Type="{fieldType}" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new PersistedFieldAnalyzer(),
                (SchemaPath, xml),
                (TablePath, TableSchema));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("未指定 Type 的欄位視為持久化，仍須有對應欄位")]
        public void MissingTypeAttribute_IsTreatedAsPersisted()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product">
                      <Fields>
                        <FormField FieldName="not_a_column" DbType="String" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new PersistedFieldAnalyzer(),
                (SchemaPath, xml),
                (TablePath, TableSchema));

            // Assert
            Assert.Single(diagnostics);
        }

        [Fact]
        [DisplayName("欄位齊備時不應報診斷")]
        public void AllFieldsPresent_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
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
            var diagnostics = AnalyzerRunner.Run(
                new PersistedFieldAnalyzer(),
                (SchemaPath, xml),
                (TablePath, TableSchema));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("TableSchema 不存在時應交由 BEE2002 處理，不逐欄誤報")]
        public void MissingTableSchema_DefersToBee2002()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_unknown">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                        <FormField FieldName="sys_name" DbType="String" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new PersistedFieldAnalyzer(),
                (SchemaPath, xml),
                (TablePath, TableSchema));

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
