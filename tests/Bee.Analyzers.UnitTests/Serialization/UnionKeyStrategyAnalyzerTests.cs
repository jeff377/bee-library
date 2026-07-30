using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Serialization;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Serialization
{
    /// <summary>
    /// BEE4003（union 階層必須使用整數 MessagePack 鍵）測試。
    /// </summary>
    /// <remarks>
    /// 此規則以架構決策為依據（ADR-030），而非技術失敗——實測 MessagePack 3.1.7 對 union 使用
    /// name-based 鍵可正常 round-trip。測試因此驗證「規則是否正確識別 union 階層」，而非驗證序列化行為。
    /// </remarks>
    public class UnionKeyStrategyAnalyzerTests
    {
        private static readonly Type[] s_anchors = { typeof(MessagePack.UnionAttribute) };

        [Fact]
        [DisplayName("union 基底使用 keyAsPropertyName 應報 BEE4003")]
        public void NameBasedUnionBase_ReportsDiagnostic()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject(keyAsPropertyName: true)]
                [Union(0, typeof(ConditionNode))]
                public abstract class FilterNodeSample
                {
                }

                [MessagePackObject]
                public sealed class ConditionNode : FilterNodeSample
                {
                    [Key(100)] public string FieldName { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new UnionKeyStrategyAnalyzer(), source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE4003", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'FilterNodeSample'", message, StringComparison.Ordinal);
            Assert.Contains("one keying strategy", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("union 子類使用 keyAsPropertyName 應報 BEE4003 並指出階層根")]
        public void NameBasedUnionSubclass_ReportsDiagnosticNamingRoot()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject]
                [Union(0, typeof(ConditionNode))]
                public abstract class FilterNodeSample
                {
                }

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class ConditionNode : FilterNodeSample
                {
                    public string FieldName { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new UnionKeyStrategyAnalyzer(), source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE4003", diagnostic.Id);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'ConditionNode'", message, StringComparison.Ordinal);
            Assert.Contains("rooted at 'FilterNodeSample'", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("union 階層全數使用整數 Key 不應報診斷")]
        public void IntegerKeyedUnionHierarchy_ReportsNothing()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject]
                [Union(0, typeof(ConditionNode))]
                [Union(1, typeof(GroupNode))]
                public abstract class FilterNodeSample
                {
                }

                [MessagePackObject]
                public sealed class ConditionNode : FilterNodeSample
                {
                    [Key(100)] public string FieldName { get; set; } = string.Empty;
                }

                [MessagePackObject]
                public sealed class GroupNode : FilterNodeSample
                {
                    [Key(100)] public string Operator { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new UnionKeyStrategyAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非 union 階層的型別使用 keyAsPropertyName 不受限制")]
        public void NonUnionType_IsUnaffected()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class OrdinaryRequest
                {
                    public string FieldName { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new UnionKeyStrategyAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("多層繼承的孫類也應被涵蓋")]
        public void GrandchildInHierarchy_IsCovered()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject]
                [Union(0, typeof(MiddleNode))]
                public abstract class RootNode
                {
                }

                [MessagePackObject]
                public abstract class MiddleNode : RootNode
                {
                }

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class LeafNode : MiddleNode
                {
                    public string Name { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new UnionKeyStrategyAnalyzer(), source, s_anchors);

            // Assert
            var message = Assert.Single(diagnostics).GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'LeafNode'", message, StringComparison.Ordinal);
            Assert.Contains("rooted at 'RootNode'", message, StringComparison.Ordinal);
        }
    }
}
