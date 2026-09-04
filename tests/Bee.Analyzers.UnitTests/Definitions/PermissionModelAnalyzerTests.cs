using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Definitions;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// BEE1008（FormSchema 未宣告 PermissionModelId → 對所有已認證呼叫者全開）測試。
    /// </summary>
    /// <remarks>
    /// 這條**報告而不強制**：未標記的表單保持開放是框架刻意的漸進採用策略（見
    /// <c>FormBusinessObject.Authorize</c> 的 XML doc）。改成強制會讓每個採用到一半的部署當場壞掉。
    /// 缺的從來不是規則，是「這張表單是開放的」這件事沒有任何地方說得出來。
    /// </remarks>
    public class PermissionModelAnalyzerTests
    {
        private const string SchemaPath = "Define/FormSchema/Order.FormSchema.xml";

        [Fact]
        [DisplayName("未宣告 PermissionModelId 應報 BEE1008，且嚴重度為 Info")]
        public void MissingPermissionModelId_ReportsInfoDiagnostic()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company">
                  <Tables />
                </FormSchema>
                """;

            var diagnostics = AnalyzerRunner.Run(new PermissionModelAnalyzer(), (SchemaPath, xml));

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE1008", diagnostic.Id);

            // Info 而非 Warning 是刻意的：Warning 會讓每個尚未採用完的部署 build 失敗，
            // 包含本框架自己的 Defaults/（Department 與 Employee 都沒有 model）。
            Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'Order'", message, StringComparison.Ordinal);
            Assert.Contains("every authenticated caller", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("PermissionModelId 存在但為空白，同樣應報 BEE1008")]
        public void BlankPermissionModelId_ReportsDiagnostic()
        {
            // 執行期的判定是 string.IsNullOrEmpty，所以空字串與屬性缺席是同一回事。
            // 只檢查「屬性在不在」會漏掉這個形狀。
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company" PermissionModelId="   ">
                  <Tables />
                </FormSchema>
                """;

            var diagnostics = AnalyzerRunner.Run(new PermissionModelAnalyzer(), (SchemaPath, xml));

            Assert.Equal("BEE1008", Assert.Single(diagnostics).Id);
        }

        [Fact]
        [DisplayName("已宣告 PermissionModelId 不應報 BEE1008")]
        public void DeclaredPermissionModelId_ReportsNothing()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company" PermissionModelId="OrderModel">
                  <Tables />
                </FormSchema>
                """;

            Assert.Empty(AnalyzerRunner.Run(new PermissionModelAnalyzer(), (SchemaPath, xml)));
        }

        [Fact]
        [DisplayName("多張表單各報一次，不會彼此吞掉")]
        public void MultipleSchemas_EachReportsOnce()
        {
            const string open1 = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Order" CategoryId="company"><Tables /></FormSchema>
                """;
            const string open2 = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Invoice" CategoryId="company"><Tables /></FormSchema>
                """;
            const string guarded = """
                <?xml version="1.0" encoding="utf-8"?>
                <FormSchema ProgId="Payment" CategoryId="company" PermissionModelId="PayModel"><Tables /></FormSchema>
                """;

            var diagnostics = AnalyzerRunner.Run(
                new PermissionModelAnalyzer(),
                ("Define/FormSchema/Order.FormSchema.xml", open1),
                ("Define/FormSchema/Invoice.FormSchema.xml", open2),
                ("Define/FormSchema/Payment.FormSchema.xml", guarded));

            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d => Assert.Equal("BEE1008", d.Id));
        }
    }
}
