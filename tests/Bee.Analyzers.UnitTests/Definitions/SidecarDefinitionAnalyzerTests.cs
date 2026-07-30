using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE2002（TableSchema 須位於符合 scope 的資料夾）與 BEE2005（應有對應 FormLayout）測試。
    /// </summary>
    public class SidecarDefinitionAnalyzerTests
    {
        private const string SchemaPath = "Define/FormSchema/Product.FormSchema.xml";
        private const string LayoutPath = "Define/FormLayout/Product.FormLayout.xml";

        private const string Schema = """
            <?xml version="1.0" encoding="utf-8"?>
            <FormSchema ProgId="Product" CategoryId="company">
              <Tables>
                <FormTable TableName="Product" DbTableName="ft_product" />
              </Tables>
            </FormSchema>
            """;

        private const string TableSchema = """
            <?xml version="1.0" encoding="utf-8"?>
            <TableSchema TableName="ft_product">
              <Fields>
                <DbField FieldName="sys_id" DbType="String" />
              </Fields>
            </TableSchema>
            """;

        private const string Layout = """
            <?xml version="1.0" encoding="utf-8"?>
            <FormLayout LayoutId="Product" ProgId="Product" />
            """;

        [Fact]
        [DisplayName("TableSchema 位於錯誤 scope 資料夾應報 BEE2002 並指出實際資料夾")]
        public void TableSchemaInWrongScopeFolder_ReportsDiagnostic()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new SidecarDefinitionAnalyzer(),
                (SchemaPath, Schema),
                ("Define/TableSchema/common/ft_product.TableSchema.xml", TableSchema),
                (LayoutPath, Layout));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE2002", diagnostic.Id);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("TableSchema/company/ft_product.TableSchema.xml", message, StringComparison.Ordinal);
            Assert.Contains("One exists under 'common' instead", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("TableSchema 完全不存在應報 BEE2002 並建議新增")]
        public void TableSchemaMissingEntirely_SuggestsAdding()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new SidecarDefinitionAnalyzer(),
                (SchemaPath, Schema),
                ("Define/TableSchema/company/ft_other.TableSchema.xml", TableSchema),
                (LayoutPath, Layout));

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("add the table schema, or correct DbTableName", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("TableSchema 位於正確 scope 資料夾不應報診斷")]
        public void TableSchemaInMatchingFolder_ReportsNothing()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new SidecarDefinitionAnalyzer(),
                (SchemaPath, Schema),
                ("Define/TableSchema/company/ft_product.TableSchema.xml", TableSchema),
                (LayoutPath, Layout));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("缺少對應 FormLayout 應報 BEE2005")]
        public void MissingFormLayout_ReportsDiagnostic()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new SidecarDefinitionAnalyzer(),
                (SchemaPath, Schema),
                ("Define/TableSchema/company/ft_product.TableSchema.xml", TableSchema),
                ("Define/FormLayout/Other.FormLayout.xml", Layout));

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE2005", diagnostic.Id);
            Assert.Contains("FormLayout/Product.FormLayout.xml", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("完全未提供 TableSchema 檔時應整組靜默（定義可能存於資料庫）")]
        public void NoTableSchemaFilesAtAll_StaysSilent()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new SidecarDefinitionAnalyzer(),
                (SchemaPath, Schema),
                (LayoutPath, Layout));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("完全未提供 FormLayout 檔時應整組靜默")]
        public void NoFormLayoutFilesAtAll_StaysSilent()
        {
            // Act
            var diagnostics = AnalyzerRunner.Run(
                new SidecarDefinitionAnalyzer(),
                (SchemaPath, Schema),
                ("Define/TableSchema/company/ft_product.TableSchema.xml", TableSchema));

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("CategoryId 非法時 BEE2002 應交由 BEE1001 處理")]
        public void InvalidCategoryId_DefersToBee1001()
        {
            const string invalidSchema = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Product" CategoryId="business">
                  <Tables>
                    <FormTable TableName="Product" DbTableName="ft_product" />
                  </Tables>
                </FormSchema>
                """;

            // Act
            var diagnostics = AnalyzerRunner.Run(
                new SidecarDefinitionAnalyzer(),
                (SchemaPath, invalidSchema),
                ("Define/TableSchema/company/ft_product.TableSchema.xml", TableSchema),
                (LayoutPath, Layout));

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
