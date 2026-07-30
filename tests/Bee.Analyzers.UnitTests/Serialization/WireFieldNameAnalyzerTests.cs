using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Serialization;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Serialization
{
    /// <summary>
    /// BEE4002（JSON 改名不得與 name-based MessagePack 鍵衝突）測試。
    /// </summary>
    public class WireFieldNameAnalyzerTests
    {
        private static readonly Type[] s_anchors =
        {
            typeof(MessagePack.KeyAttribute),
            typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute),
        };

        [Fact]
        [DisplayName("keyAsPropertyName 型別的屬性被 JSON 改名應報 BEE4002")]
        public void JsonRenameOnNameBasedType_ReportsDiagnostic()
        {
            const string source = """
                using MessagePack;
                using System.Text.Json.Serialization;

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class Sample
                {
                    [JsonPropertyName("alias_name")]
                    public string Name { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new WireFieldNameAnalyzer(), source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE4002", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'alias_name'", message, StringComparison.Ordinal);
            Assert.Contains("keys it as 'Name'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("整數 Key 型別不在規則範圍內（MessagePack 鍵為數字，不會與 JSON 名衝突）")]
        public void IntegerKeyedType_IsOutOfScope()
        {
            const string source = """
                using MessagePack;
                using System.Text.Json.Serialization;

                [MessagePackObject]
                public sealed class Sample
                {
                    [Key(0)]
                    [JsonPropertyName("alias_name")]
                    public string Name { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new WireFieldNameAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("JSON 名稱與屬性名相同時不應報診斷")]
        public void RenameMatchingPropertyName_ReportsNothing()
        {
            const string source = """
                using MessagePack;
                using System.Text.Json.Serialization;

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class Sample
                {
                    [JsonPropertyName("Name")]
                    public string Name { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new WireFieldNameAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("未使用 JsonPropertyName 不應報診斷")]
        public void NoJsonRename_ReportsNothing()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class Sample
                {
                    public string Name { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new WireFieldNameAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
