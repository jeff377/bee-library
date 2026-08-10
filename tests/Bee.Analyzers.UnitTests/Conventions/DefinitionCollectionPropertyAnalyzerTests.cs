using Bee.Base.Collections;
using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Conventions;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Conventions
{
    /// <summary>
    /// BEE3002（定義層集合屬性須使用框架集合型別）測試。
    /// </summary>
    /// <remarks>
    /// 此規則僅在 <c>Bee.Definition</c> 組件內生效，故測試須以 <c>RunOnSourceAs</c> 指定組件名稱；
    /// 沿用預設名稱會讓規則靜默、測試永遠通過。
    /// </remarks>
    public class DefinitionCollectionPropertyAnalyzerTests
    {
        private const string DefinitionAssembly = "Bee.Definition";

        private static readonly Type[] s_anchors =
        {
            typeof(CollectionBase<>),
            typeof(CollectionItem),
        };

        [Theory]
        [InlineData("List<string>")]
        [InlineData("Collection<string>")]
        [DisplayName("定義層以裸集合宣告屬性應報 BEE3002")]
        public void PlainCollectionProperty_ReportsDiagnostic(string propertyType)
        {
            const string template = """
                using System.Collections.Generic;
                using System.Collections.ObjectModel;

                public sealed class SampleSettings
                {
                    public __TYPE__ Items { get; set; } = new __TYPE__();
                }
                """;

            var source = template.Replace("__TYPE__", propertyType, StringComparison.Ordinal);

            // Act
            var diagnostics = AnalyzerRunner.RunOnSourceAs(
                new DefinitionCollectionPropertyAnalyzer(), DefinitionAssembly, source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE3002", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'Items'", message, StringComparison.Ordinal);
            Assert.Contains("KeyCollectionBase or CollectionBase", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("使用框架集合型別不應報診斷")]
        public void FrameworkCollectionProperty_ReportsNothing()
        {
            const string source = """
                using Bee.Definition.Collections;
                using MessagePack;

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class SampleItem : CollectionItem
                {
                    public string Name { get; set; } = string.Empty;
                }

                public sealed class SampleItems : CollectionBase<SampleItem>
                {
                }

                public sealed class SampleSettings
                {
                    public SampleItems Items { get; set; } = new SampleItems();
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSourceAs(
                new DefinitionCollectionPropertyAnalyzer(), DefinitionAssembly, source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非 Bee.Definition 組件不受此規則約束（跨層 DTO 用裸 List 為正確寫法）")]
        public void OutsideDefinitionAssembly_StaysSilent()
        {
            const string source = """
                using System.Collections.Generic;

                public sealed class CheckPackageUpdateArgs
                {
                    public List<string> Queries { get; set; } = new List<string>();
                }
                """;

            // Act — 以商業邏輯層的組件名稱執行。
            var diagnostics = AnalyzerRunner.RunOnSourceAs(
                new DefinitionCollectionPropertyAnalyzer(), "Bee.Business", source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非 public 屬性不受約束")]
        public void NonPublicProperty_ReportsNothing()
        {
            const string source = """
                using System.Collections.Generic;

                public sealed class SampleSettings
                {
                    internal List<string> Items { get; set; } = new List<string>();
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSourceAs(
                new DefinitionCollectionPropertyAnalyzer(), DefinitionAssembly, source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非集合的泛型屬性不應誤報")]
        public void OtherGenericProperty_ReportsNothing()
        {
            const string source = """
                using System.Collections.Generic;

                public sealed class SampleSettings
                {
                    public Dictionary<string, string> Map { get; set; } = new Dictionary<string, string>();

                    public string? Optional { get; set; }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSourceAs(
                new DefinitionCollectionPropertyAnalyzer(), DefinitionAssembly, source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
