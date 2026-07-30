using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Serialization;
using Bee.Definition.Collections;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Serialization
{
    /// <summary>
    /// BEE4001（MessagePack 集合必須註冊 formatter）測試。
    /// </summary>
    /// <remarks>
    /// <c>CollectionBaseFormatter</c> 是 <c>Bee.Api.Core</c> 的 internal 型別，測試無法引用，故在測試
    /// 素材中以相同 metadata name 自行宣告——analyzer 以名稱解析，因此等效。
    /// </remarks>
    public class CollectionFormatterRegistrationAnalyzerTests
    {
        private static readonly Type[] s_anchors =
        {
            typeof(MessagePack.KeyAttribute),
            typeof(MessagePackCollectionBase<>),
            typeof(MessagePackCollectionItem),
        };

        private const string Preamble = """
            using Bee.Definition.Collections;
            using MessagePack;

            namespace Bee.Api.Core.MessagePack
            {
                internal sealed class CollectionBaseFormatter<TCollection, TElement>
                {
                }
            }

            [MessagePackObject(keyAsPropertyName: true)]
            public sealed class ProbeItem : MessagePackCollectionItem
            {
                public string Name { get; set; } = string.Empty;
            }

            public sealed class RegisteredCollection : MessagePackCollectionBase<ProbeItem>
            {
            }

            public sealed class ForgottenCollection : MessagePackCollectionBase<ProbeItem>
            {
            }
            """;

        [Fact]
        [DisplayName("有註冊清單但漏掉某集合時應報 BEE4001")]
        public void UnregisteredCollection_ReportsDiagnostic()
        {
            var source = Preamble + """

                public static class Registry
                {
                    public static readonly object[] Formatters =
                    {
                        new Bee.Api.Core.MessagePack.CollectionBaseFormatter<RegisteredCollection, ProbeItem>(),
                    };
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new CollectionFormatterRegistrationAnalyzer(), source, s_anchors);

            // Assert
            var messages = diagnostics
                .Where(diagnostic => diagnostic.Id == "BEE4001")
                .Select(diagnostic => diagnostic.GetMessage(CultureInfo.InvariantCulture))
                .ToArray();

            Assert.Contains(messages, message => message.Contains("'ForgottenCollection'", StringComparison.Ordinal));
            Assert.DoesNotContain(messages, message => message.Contains("'RegisteredCollection'", StringComparison.Ordinal));
        }

        [Fact]
        [DisplayName("診斷應為 error 並指出具體修正寫法")]
        public void Diagnostic_IsErrorAndNamesTheFix()
        {
            var source = Preamble + """

                public static class Registry
                {
                    public static readonly object[] Formatters =
                    {
                        new Bee.Api.Core.MessagePack.CollectionBaseFormatter<RegisteredCollection, ProbeItem>(),
                    };
                }
                """;

            // Act
            var diagnostic = Assert.Single(
                AnalyzerRunner.RunOnSource(new CollectionFormatterRegistrationAnalyzer(), source, s_anchors)
                    .Where(item => item.GetMessage(CultureInfo.InvariantCulture)
                        .Contains("'ForgottenCollection'", StringComparison.Ordinal)));

            // Assert
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("CollectionBaseFormatter<ForgottenCollection, ProbeItem>()", message, StringComparison.Ordinal);
            Assert.Contains("only deserialization throws", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("兩個集合皆已註冊時不應報自身定義的集合")]
        public void AllCollectionsRegistered_ReportsNeither()
        {
            var source = Preamble + """

                public static class Registry
                {
                    public static readonly object[] Formatters =
                    {
                        new Bee.Api.Core.MessagePack.CollectionBaseFormatter<RegisteredCollection, ProbeItem>(),
                        new Bee.Api.Core.MessagePack.CollectionBaseFormatter<ForgottenCollection, ProbeItem>(),
                    };
                }
                """;

            // Act
            var messages = AnalyzerRunner.RunOnSource(
                new CollectionFormatterRegistrationAnalyzer(), source, s_anchors)
                .Select(diagnostic => diagnostic.GetMessage(CultureInfo.InvariantCulture))
                .ToArray();

            // Assert
            Assert.DoesNotContain(messages, message => message.Contains("'ForgottenCollection'", StringComparison.Ordinal));
            Assert.DoesNotContain(messages, message => message.Contains("'RegisteredCollection'", StringComparison.Ordinal));
        }

        [Fact]
        [DisplayName("無任何註冊時應整組靜默（該 compilation 不擁有註冊清單）")]
        public void NoRegistrationsAtAll_StaysSilent()
        {
            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new CollectionFormatterRegistrationAnalyzer(), Preamble, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
