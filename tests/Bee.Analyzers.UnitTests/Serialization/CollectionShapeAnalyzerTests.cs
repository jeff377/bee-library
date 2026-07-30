using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Serialization;
using Bee.Definition.Collections;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Serialization
{
    /// <summary>
    /// BEE4005（框架集合應只有一個 public Add）與 BEE4006（序列化型別須有無參數建構子）測試。
    /// </summary>
    public class CollectionShapeAnalyzerTests
    {
        private static readonly Type[] s_anchors =
        {
            typeof(MessagePack.KeyAttribute),
            typeof(MessagePackCollectionBase<>),
            typeof(MessagePackCollectionItem),
        };

        private const string ItemDeclaration = """
            using Bee.Definition.Collections;
            using MessagePack;

            [MessagePackObject(keyAsPropertyName: true)]
            public sealed class SampleItem : MessagePackCollectionItem
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        [Fact]
        [DisplayName("集合子類新增 public Add 多載應報 BEE4005")]
        public void ExtraPublicAddOverload_ReportsDiagnostic()
        {
            var source = ItemDeclaration + """

                public sealed class SampleItems : MessagePackCollectionBase<SampleItem>
                {
                    public void Add(string name) => Add(new SampleItem { Name = name });
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new CollectionAddOverloadAnalyzer(), source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE4005", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Contains("AmbiguousMatchException", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("集合子類未新增 Add 不應報 BEE4005")]
        public void NoExtraAdd_ReportsNothing()
        {
            var source = ItemDeclaration + """

                public sealed class SampleItems : MessagePackCollectionBase<SampleItem>
                {
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new CollectionAddOverloadAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非框架集合的型別有 Add 方法不應報 BEE4005")]
        public void NonCollectionTypeWithAdd_ReportsNothing()
        {
            const string source = """
                public sealed class Basket
                {
                    public void Add(string item) { }

                    public void Add(int quantity) { }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new CollectionAddOverloadAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("集合子類只有參數化建構子應報 BEE4006")]
        public void CollectionWithoutParameterlessCtor_ReportsDiagnostic()
        {
            var source = ItemDeclaration + """

                public sealed class SampleItems : MessagePackCollectionBase<SampleItem>
                {
                    public SampleItems(string label) => Label = label;

                    public string Label { get; set; }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new ParameterlessConstructorAnalyzer(), source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE4006", diagnostic.Id);
            Assert.Contains("MissingMethodException", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("MessagePackObject 型別只有參數化建構子應報 BEE4006")]
        public void ContractTypeWithoutParameterlessCtor_ReportsDiagnostic()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class Sample
                {
                    public Sample(string name) => Name = name;

                    public string Name { get; set; }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new ParameterlessConstructorAnalyzer(), source, s_anchors);

            // Assert
            Assert.Equal("BEE4006", Assert.Single(diagnostics).Id);
        }

        [Fact]
        [DisplayName("同時有無參數與參數化建構子不應報 BEE4006")]
        public void BothConstructors_ReportNothing()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class Sample
                {
                    public Sample() { }

                    public Sample(string name) => Name = name;

                    public string Name { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new ParameterlessConstructorAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("未宣告任何建構子時有隱含無參數建構子，不應報 BEE4006")]
        public void ImplicitConstructor_ReportsNothing()
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
            var diagnostics = AnalyzerRunner.RunOnSource(new ParameterlessConstructorAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("abstract 型別不會被反序列化器建構，不應報 BEE4006")]
        public void AbstractType_ReportsNothing()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject(keyAsPropertyName: true)]
                public abstract class SampleBase
                {
                    protected SampleBase(string name) => Name = name;

                    public string Name { get; set; }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new ParameterlessConstructorAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
