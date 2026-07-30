using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Serialization;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Serialization
{
    /// <summary>
    /// BEE4007（可寫屬性的 ignore 標籤應跨格式一致）測試。
    /// </summary>
    public class IgnoreAttributeConsistencyAnalyzerTests
    {
        private static readonly Type[] s_anchors =
        {
            typeof(MessagePack.IgnoreMemberAttribute),
            typeof(System.Text.Json.Serialization.JsonIgnoreAttribute),
            typeof(System.Xml.Serialization.XmlIgnoreAttribute),
        };

        [Fact]
        [DisplayName("可寫屬性只標 IgnoreMember 應報 BEE4007")]
        public void OnlyIgnoreMember_ReportsDiagnostic()
        {
            const string source = """
                using MessagePack;

                public sealed class Sample
                {
                    [IgnoreMember]
                    public string Cached { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new IgnoreAttributeConsistencyAnalyzer(), source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE4007", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("MessagePack", message, StringComparison.Ordinal);
            Assert.Contains("JSON and XML", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("三個 ignore 標籤齊備不應報診斷")]
        public void AllThreeIgnores_ReportNothing()
        {
            const string source = """
                using System.Text.Json.Serialization;
                using System.Xml.Serialization;
                using MessagePack;

                public sealed class Sample
                {
                    [XmlIgnore, JsonIgnore, IgnoreMember]
                    public string Cached { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new IgnoreAttributeConsistencyAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("完全未標 ignore 不應報診斷")]
        public void NoIgnores_ReportNothing()
        {
            const string source = """
                public sealed class Sample
                {
                    public string Name { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new IgnoreAttributeConsistencyAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("get-only 判別碼屬性不在範圍內（FilterNode.Kind 為刻意不對稱）")]
        public void GetOnlyDiscriminator_IsOutOfScope()
        {
            const string source = """
                using MessagePack;

                public abstract class NodeBase
                {
                    [IgnoreMember]
                    public abstract string Kind { get; }
                }

                public sealed class ConditionNode : NodeBase
                {
                    public override string Kind => "Condition";
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new IgnoreAttributeConsistencyAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("private setter 屬性不在範圍內（XmlSerializer 需 public setter 才能還原）")]
        public void PrivateSetter_IsOutOfScope()
        {
            // 框架的 IObjectSerialize.SerializeState 即為此形狀，寫法正確不應被報。
            const string source = """
                using System.Text.Json.Serialization;
                using MessagePack;

                public sealed class Sample
                {
                    [JsonIgnore, IgnoreMember]
                    public int SerializeState { get; private set; }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new IgnoreAttributeConsistencyAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("可寫屬性同時缺 MessagePack 與 XML 標籤應列出兩者")]
        public void OnlyJsonIgnore_ListsBothMissingFormats()
        {
            const string source = """
                using System.Text.Json.Serialization;

                public sealed class Sample
                {
                    [JsonIgnore]
                    public string Cached { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new IgnoreAttributeConsistencyAnalyzer(), source, s_anchors);

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("excluded from JSON", message, StringComparison.Ordinal);
            Assert.Contains("MessagePack and XML", message, StringComparison.Ordinal);
        }
    }
}
