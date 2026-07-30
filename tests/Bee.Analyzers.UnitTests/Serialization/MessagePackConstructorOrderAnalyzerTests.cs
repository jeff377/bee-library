using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Serialization;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Serialization
{
    /// <summary>
    /// BEE4004（MessagePack 建構子參數順序必須跟隨整數 Key 順序）測試。
    /// </summary>
    /// <remarks>
    /// 規則範圍以 MessagePack 3.1.7 實測為準：整數 Key 型別的 ctor 參數依 Key 順序按位置填入
    /// （非依參數名比對），故順序不符會靜默對調欄位；<c>keyAsPropertyName</c> 型別依名稱比對，不受影響。
    /// </remarks>
    public class MessagePackConstructorOrderAnalyzerTests
    {
        // 強制載入 MessagePack.Annotations，使 AnalyzerRunner 的參考集合含其 attribute 型別。
        private static readonly Type s_messagePackAnchor = typeof(MessagePack.KeyAttribute);

        [Fact]
        [DisplayName("整數 Key 且 ctor 參數順序與 Key 相反應報 BEE4004")]
        public void IntegerKeyWithReversedConstructor_ReportsDiagnostic()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject]
                public sealed class Sample
                {
                    public Sample() { }

                    public Sample(string second, string first)
                    {
                        Second = second;
                        First = first;
                    }

                    [Key(0)] public string First { get; set; } = string.Empty;
                    [Key(1)] public string Second { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new MessagePackConstructorOrderAnalyzer(), source, s_messagePackAnchor);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE4004", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("(second, first)", message, StringComparison.Ordinal);
            Assert.Contains("reorder the parameters to (first, second)", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("整數 Key 且 ctor 參數順序與 Key 一致不應報診斷")]
        public void IntegerKeyWithAlignedConstructor_ReportsNothing()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject]
                public sealed class Sample
                {
                    public Sample(string first, string second)
                    {
                        First = first;
                        Second = second;
                    }

                    [Key(0)] public string First { get; set; } = string.Empty;
                    [Key(1)] public string Second { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new MessagePackConstructorOrderAnalyzer(), source, s_messagePackAnchor);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("判定基準為 Key 數值而非宣告順序")]
        public void KeyValueOrderRatherThanDeclarationOrder_IsUsed()
        {
            // Second 的 Key 較小，故 ctor 先收 second 才是正確順序。
            const string source = """
                using MessagePack;

                [MessagePackObject]
                public sealed class Sample
                {
                    public Sample(string second, string first)
                    {
                        Second = second;
                        First = first;
                    }

                    [Key(101)] public string First { get; set; } = string.Empty;
                    [Key(100)] public string Second { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new MessagePackConstructorOrderAnalyzer(), source, s_messagePackAnchor);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("keyAsPropertyName 型別不在規則範圍內")]
        public void NameBasedKeys_AreOutOfScope()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject(keyAsPropertyName: true)]
                public sealed class Sample
                {
                    public Sample() { }

                    public Sample(string second, string first)
                    {
                        Second = second;
                        First = first;
                    }

                    public string First { get; set; } = string.Empty;
                    public string Second { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new MessagePackConstructorOrderAnalyzer(), source, s_messagePackAnchor);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("ctor 參數無法對應具 Key 的成員時不報，避免誤判")]
        public void UnmappableParameters_ReportNothing()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject]
                public sealed class Sample
                {
                    public Sample(string alpha, string beta)
                    {
                        Second = alpha;
                        First = beta;
                    }

                    [Key(0)] public string First { get; set; } = string.Empty;
                    [Key(1)] public string Second { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new MessagePackConstructorOrderAnalyzer(), source, s_messagePackAnchor);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("單一參數 ctor 無順序可言，不應報診斷")]
        public void SingleParameterConstructor_ReportsNothing()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject]
                public sealed class Sample
                {
                    public Sample(string second)
                    {
                        Second = second;
                    }

                    [Key(0)] public string First { get; set; } = string.Empty;
                    [Key(1)] public string Second { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new MessagePackConstructorOrderAnalyzer(), source, s_messagePackAnchor);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("未標記 MessagePackObject 的型別不應被檢查")]
        public void TypeWithoutMessagePackObject_ReportsNothing()
        {
            const string source = """
                public sealed class Sample
                {
                    public Sample(string second, string first)
                    {
                        Second = second;
                        First = first;
                    }

                    public string First { get; set; } = string.Empty;
                    public string Second { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(new MessagePackConstructorOrderAnalyzer(), source, s_messagePackAnchor);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("繼承而來的具 Key 成員也應納入判定")]
        public void InheritedKeyedMembers_AreConsidered()
        {
            const string source = """
                using MessagePack;

                [MessagePackObject]
                public abstract class SampleBase
                {
                    [Key(0)] public string First { get; set; } = string.Empty;
                }

                [MessagePackObject]
                public sealed class Sample : SampleBase
                {
                    public Sample() { }

                    public Sample(string second, string first)
                    {
                        Second = second;
                        First = first;
                    }

                    [Key(1)] public string Second { get; set; } = string.Empty;
                }
                """;

            // Act
            var compilationErrors = AnalyzerRunner.GetCompilationDiagnostics(source, s_messagePackAnchor)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.GetMessage(CultureInfo.InvariantCulture))
                .ToArray();
            var diagnostics = AnalyzerRunner.RunOnSource(new MessagePackConstructorOrderAnalyzer(), source, s_messagePackAnchor);

            // Assert
            Assert.Empty(compilationErrors);
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE4004", diagnostic.Id);
        }
    }
}
