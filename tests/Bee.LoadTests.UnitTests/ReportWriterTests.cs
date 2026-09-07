using System.ComponentModel;
using System.Text.Json;
using Bee.LoadTests.Caching;
using Bee.LoadTests.Reporting;
using Bee.LoadTests.Running;
using Bee.LoadTests.Statistics;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="ReportWriter"/>.
    /// </summary>
    public class ReportWriterTests : IDisposable
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(), "bee-loadtest-report-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_directory)) { Directory.Delete(_directory, recursive: true); }
            GC.SuppressFinalize(this);
        }

        private static RunReport CreateReport(
            IReadOnlyDictionary<string, long>? errors = null,
            IReadOnlyDictionary<string, string>? samples = null)
        {
            var result = new ScenarioResult(
                "GetList",
                LatencyStatistics.FromMilliseconds([1, 2, 3, 4, 5]),
                successCount: 5,
                errors ?? new Dictionary<string, long>(StringComparer.Ordinal),
                samples ?? new Dictionary<string, string>(StringComparer.Ordinal),
                TimeSpan.FromSeconds(10));

            var metadata = new RunMetadata
            {
                Version = "4.29.0+abc123",
                Provider = "SQLServer",
                Mode = "Local",
                ProtectionLevel = "Encrypted",
                OperatingSystem = "TestOS",
                ProcessorCount = 8,
                VirtualUsers = 4,
                WarmupSeconds = 2,
                DurationSeconds = 10,
                LoadModel = "Closed",
                TokenStrategy = "PerUser",
                SeededRows = 1000,
                DroppedBindings = ["Order.BusinessObject"],
            };

            return new RunReport(metadata, [result], new CacheCounters(9, 1, 3, 0), [50, 95]);
        }

        private string Write(RunReport report, string extension)
        {
            var path = Path.Combine(_directory, "report" + extension);
            if (extension == ".md") { ReportWriter.WriteMarkdown(report, path); }
            else { ReportWriter.WriteJson(report, path); }
            return File.ReadAllText(path);
        }

        [Fact]
        [DisplayName("Markdown 帶齊解讀數字所需的中繼資料")]
        public void WriteMarkdown_IncludesMetadata()
        {
            var text = Write(CreateReport(), ".md");

            Assert.Contains("4.29.0+abc123", text, StringComparison.Ordinal);
            Assert.Contains("SQLServer", text, StringComparison.Ordinal);
            Assert.Contains("Encrypted", text, StringComparison.Ordinal);
            Assert.Contains("Closed model", text, StringComparison.Ordinal);
            Assert.Contains("8 logical cores", text, StringComparison.Ordinal);
            Assert.Contains("PerUser", text, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("Markdown 列出被清掉的綁定，讀者才知道量的是框架實作")]
        public void WriteMarkdown_ListsDroppedBindings()
        {
            Assert.Contains("Order.BusinessObject", Write(CreateReport(), ".md"),
                StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("沒有錯誤時不輸出錯誤區塊")]
        public void WriteMarkdown_NoErrors_OmitsErrorSection()
        {
            Assert.DoesNotContain("## Errors", Write(CreateReport(), ".md"),
                StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("有錯誤時列出型別、次數與範例訊息")]
        public void WriteMarkdown_WithErrors_ListsTypeCountAndSample()
        {
            var report = CreateReport(
                new Dictionary<string, long>(StringComparer.Ordinal) { ["TimeoutException"] = 3 },
                new Dictionary<string, string>(StringComparer.Ordinal) { ["TimeoutException"] = "took too long" });

            var text = Write(report, ".md");

            Assert.Contains("## Errors", text, StringComparison.Ordinal);
            Assert.Contains("TimeoutException", text, StringComparison.Ordinal);
            Assert.Contains("took too long", text, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("錯誤訊息中的表格分隔字元被跳脫，不破壞版面")]
        public void WriteMarkdown_EscapesPipesInMessages()
        {
            var report = CreateReport(
                new Dictionary<string, long>(StringComparer.Ordinal) { ["DbException"] = 1 },
                new Dictionary<string, string>(StringComparer.Ordinal) { ["DbException"] = "a | b" });

            Assert.Contains(@"a \| b", Write(report, ".md"), StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("JSON 可被解析，且帶中繼資料與百分位")]
        public void WriteJson_IsParseableAndComplete()
        {
            using var document = JsonDocument.Parse(Write(CreateReport(), ".json"));
            var root = document.RootElement;

            Assert.Equal("SQLServer", root.GetProperty("Metadata").GetProperty("Provider").GetString());
            Assert.Equal(4, root.GetProperty("Metadata").GetProperty("VirtualUsers").GetInt32());

            var scenario = root.GetProperty("Scenarios")[0];
            Assert.Equal("GetList", scenario.GetProperty("Name").GetString());
            Assert.Equal(5, scenario.GetProperty("SuccessCount").GetInt64());

            var percentiles = scenario.GetProperty("Latencies").GetProperty("Percentiles");
            Assert.Equal(3, percentiles.GetProperty("50").GetDouble());
            Assert.Equal(5, percentiles.GetProperty("95").GetDouble());
        }

        [Fact]
        [DisplayName("JSON 帶快取命中率，供跨次比較")]
        public void WriteJson_IncludesCacheCounters()
        {
            using var document = JsonDocument.Parse(Write(CreateReport(), ".json"));

            var cache = document.RootElement.GetProperty("Cache");
            Assert.Equal(10, cache.GetProperty("Reads").GetInt64());
            Assert.Equal(0.9, cache.GetProperty("HitRate").GetDouble(), 3);
        }

        [Fact]
        [DisplayName("輸出目錄不存在時自動建立")]
        public void Write_CreatesMissingDirectory()
        {
            var nested = Path.Combine(_directory, "a", "b", "report.md");

            ReportWriter.WriteMarkdown(CreateReport(), nested);

            Assert.True(File.Exists(nested));
        }
    }
}
