using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Conventions;
using Bee.Business;
using Bee.Definition.Attributes;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Conventions
{
    /// <summary>
    /// BEE3001（BO 的 public 方法必須宣告存取控制）測試。
    /// </summary>
    public class BusinessObjectAccessControlAnalyzerTests
    {
        private static readonly Type[] s_anchors =
        {
            typeof(BusinessObject),
            typeof(ApiAccessControlAttribute),
        };

        private const string Preamble = """
            using System;
            using Bee.Business;
            using Bee.Definition;
            using Bee.Definition.Attributes;
            """;

        [Fact]
        [DisplayName("BO 的 public 方法未宣告存取控制應報 BEE3001")]
        public void UnmarkedPublicMethod_ReportsDiagnostic()
        {
            var source = Preamble + """

                public class OrderBusinessObject : BusinessObject
                {
                    public OrderBusinessObject(IBeeContext ctx, Guid accessToken)
                        : base(ctx, accessToken) { }

                    public string Approve(string id) => id;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new BusinessObjectAccessControlAnalyzer(), source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE3001", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'Approve'", message, StringComparison.Ordinal);
            Assert.Contains("UnauthorizedAccessException", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("方法自身標記時不應報診斷")]
        public void MethodLevelAttribute_ReportsNothing()
        {
            var source = Preamble + """

                public class OrderBusinessObject : BusinessObject
                {
                    public OrderBusinessObject(IBeeContext ctx, Guid accessToken)
                        : base(ctx, accessToken) { }

                    [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
                    public string Approve(string id) => id;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new BusinessObjectAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("型別層級標記應涵蓋其所有方法")]
        public void TypeLevelAttribute_CoversAllMethods()
        {
            var source = Preamble + """

                [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
                public class OrderBusinessObject : BusinessObject
                {
                    public OrderBusinessObject(IBeeContext ctx, Guid accessToken)
                        : base(ctx, accessToken) { }

                    public string Approve(string id) => id;

                    public string Reject(string id) => id;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new BusinessObjectAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("建構子與屬性不屬 API surface，不應報診斷")]
        public void ConstructorsAndProperties_AreNotReported()
        {
            var source = Preamble + """

                public class OrderBusinessObject : BusinessObject
                {
                    public OrderBusinessObject(IBeeContext ctx, Guid accessToken)
                        : base(ctx, accessToken) { }

                    public string Label { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new BusinessObjectAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非 public 方法不屬 API surface，不應報診斷")]
        public void NonPublicMethods_AreNotReported()
        {
            var source = Preamble + """

                public class OrderBusinessObject : BusinessObject
                {
                    public OrderBusinessObject(IBeeContext ctx, Guid accessToken)
                        : base(ctx, accessToken) { }

                    protected string Prepare(string id) => id;

                    private string Normalise(string id) => id;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new BusinessObjectAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非 BusinessObject 的型別不受此規則約束")]
        public void NonBusinessObjectType_IsUnaffected()
        {
            var source = Preamble + """

                public class OrderService
                {
                    public string Approve(string id) => id;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new BusinessObjectAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("override 的方法若 base 已標記則不應報診斷")]
        public void OverrideOfMarkedBaseMethod_ReportsNothing()
        {
            var source = Preamble + """

                public class BaseOrderBusinessObject : BusinessObject
                {
                    public BaseOrderBusinessObject(IBeeContext ctx, Guid accessToken)
                        : base(ctx, accessToken) { }

                    [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
                    public virtual string Approve(string id) => id;
                }

                public class DerivedOrderBusinessObject : BaseOrderBusinessObject
                {
                    public DerivedOrderBusinessObject(IBeeContext ctx, Guid accessToken)
                        : base(ctx, accessToken) { }

                    public override string Approve(string id) => id;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new BusinessObjectAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
