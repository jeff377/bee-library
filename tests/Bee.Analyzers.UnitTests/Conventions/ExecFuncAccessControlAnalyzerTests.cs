using System.ComponentModel;
using System.Globalization;
using Bee.Analyzers.Conventions;
using Bee.Business;
using Bee.Business.Attributes;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.UnitTests.Conventions
{
    /// <summary>
    /// BEE3003（ExecFunc handler 的 public 方法必須宣告存取控制）測試。
    /// </summary>
    public class ExecFuncAccessControlAnalyzerTests
    {
        private static readonly Type[] s_anchors =
        {
            typeof(IExecFuncHandler),
            typeof(ExecFuncAccessControlAttribute),
        };

        private const string Preamble = """
            using System;
            using Bee.Business;
            using Bee.Business.Attributes;
            using Bee.Definition.Security;
            """;

        [Fact]
        [DisplayName("ExecFunc handler 的 public 方法未宣告存取控制應報 BEE3003")]
        public void UnmarkedHandlerMethod_ReportsDiagnostic()
        {
            var source = Preamble + """

                public class MaintenanceExecFuncHandler : IExecFuncHandler
                {
                    public void RebuildIndexes(ExecFuncArgs args, ExecFuncResult result) { }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new ExecFuncAccessControlAnalyzer(), source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE3003", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);

            var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'RebuildIndexes'", message, StringComparison.Ordinal);
            Assert.Contains("LocalOnly", message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("已標記 ExecFuncAccessControl 的方法不應報診斷")]
        public void MarkedHandlerMethod_ReportsNothing()
        {
            var source = Preamble + """

                public class MaintenanceExecFuncHandler : IExecFuncHandler
                {
                    [ExecFuncAccessControl(ApiAccessRequirement.Authenticated, LocalOnly = true)]
                    public void RebuildIndexes(ExecFuncArgs args, ExecFuncResult result) { }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new ExecFuncAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("static 的 ExecFunc 方法同樣受規則約束")]
        public void StaticHandlerMethod_IsReported()
        {
            var source = Preamble + """

                public class MaintenanceExecFuncHandler : IExecFuncHandler
                {
                    public static void Ping(ExecFuncArgs args, ExecFuncResult result) { }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new ExecFuncAccessControlAnalyzer(), source, s_anchors);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("BEE3003", diagnostic.Id);
            Assert.Contains("'Ping'", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("簽章不符 ExecFunc 形狀的 public 方法不應報診斷")]
        public void NonMatchingSignature_ReportsNothing()
        {
            var source = Preamble + """

                public class MaintenanceExecFuncHandler : IExecFuncHandler
                {
                    public string Describe(string id) => id;

                    public void Reset() { }

                    public string Label { get; set; } = string.Empty;
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new ExecFuncAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("非 public 的 ExecFunc 形狀方法不可被派發，不應報診斷")]
        public void NonPublicMethods_AreNotReported()
        {
            var source = Preamble + """

                public class MaintenanceExecFuncHandler : IExecFuncHandler
                {
                    protected void Prepare(ExecFuncArgs args, ExecFuncResult result) { }

                    private void Normalise(ExecFuncArgs args, ExecFuncResult result) { }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new ExecFuncAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        [DisplayName("未實作 IExecFuncHandler 的型別不受此規則約束")]
        public void NonHandlerType_IsUnaffected()
        {
            var source = Preamble + """

                public class MaintenanceService
                {
                    public void RebuildIndexes(ExecFuncArgs args, ExecFuncResult result) { }
                }
                """;

            // Act
            var diagnostics = AnalyzerRunner.RunOnSource(
                new ExecFuncAccessControlAnalyzer(), source, s_anchors);

            // Assert
            Assert.Empty(diagnostics);
        }
    }
}
