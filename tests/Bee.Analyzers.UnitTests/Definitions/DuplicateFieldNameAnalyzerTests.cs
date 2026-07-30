using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE1007（同一表內不得重複宣告欄位）測試。
    /// </summary>
    public class DuplicateFieldNameAnalyzerTests
    {
        private const string SchemaPath = "Define/FormSchema/Order.FormSchema.xml";

        [Fact]
        [DisplayName("同一 FormTable 內重複欄位應報 BEE1007")]
        public void DuplicateWithinTable_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Order" DbTableName="ft_order">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                        <FormField FieldName="sys_name" DbType="String" />
                        <FormField FieldName="sys_id" DbType="String" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new DuplicateFieldNameAnalyzer(), (SchemaPath, xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1007", diagnostic.Id);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'Order'", message, StringComparison.Ordinal);
            Assert.Contains("'sys_id'", message, StringComparison.Ordinal);

            // 應報在後出現的那一筆（第 8 行，0-based 7），前一筆保留作為讀者的參照點。
            Assert.Equal(7, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        }

        [Fact]
        [DisplayName("master-detail 跨表同名欄位不應誤報")]
        public void SameNameAcrossTables_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Order" DbTableName="ft_order">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                        <FormField FieldName="sys_name" DbType="String" />
                      </Fields>
                    </FormTable>
                    <FormTable TableName="OrderDetail" DbTableName="ft_order_detail">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                        <FormField FieldName="sys_name" DbType="String" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new DuplicateFieldNameAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("僅大小寫不同仍視為重複")]
        public void CasingOnlyDifference_IsTreatedAsDuplicate()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Order" DbTableName="ft_order">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                        <FormField FieldName="SYS_ID" DbType="String" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new DuplicateFieldNameAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Single(diagnostics);
        }

        [Fact]
        [DisplayName("TableSchema 內重複欄位也應被檢查")]
        public void DuplicateWithinTableSchema_ReportsDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <TableSchema TableName="ft_order">
                  <Fields>
                    <DbField FieldName="sys_id" DbType="String" />
                    <DbField FieldName="sys_id" DbType="String" />
                  </Fields>
                </TableSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new DuplicateFieldNameAnalyzer(),
                ("Define/TableSchema/company/ft_order.TableSchema.xml", xml));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1007", diagnostic.Id);
            Assert.Contains("'ft_order'", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("無重複欄位不應報診斷")]
        public void NoDuplicates_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company">
                  <Tables>
                    <FormTable TableName="Order" DbTableName="ft_order">
                      <Fields>
                        <FormField FieldName="sys_id" DbType="String" />
                        <FormField FieldName="sys_name" DbType="String" />
                      </Fields>
                    </FormTable>
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(new DuplicateFieldNameAnalyzer(), (SchemaPath, xml));

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
