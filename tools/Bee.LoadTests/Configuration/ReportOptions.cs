namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// Which reports to write and where.
    /// </summary>
    public sealed class ReportOptions
    {
        /// <summary>
        /// Gets or sets whether to print a summary to standard output.
        /// </summary>
        public bool Console { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to write a Markdown report for people to read.
        /// </summary>
        public bool Markdown { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to write JSON, which is what a later run can be compared against.
        /// </summary>
        public bool Json { get; set; } = true;

        /// <summary>
        /// Gets or sets the directory reports are written to. The default sits under the
        /// git-ignored <c>artifacts/</c> tree, so runs do not accumulate in version control;
        /// copy the ones worth keeping out by hand.
        /// </summary>
        public string OutputDirectory { get; set; } = "artifacts/loadtest";

        /// <summary>
        /// Gets or sets the percentiles to report.
        /// </summary>
        public double[] Percentiles { get; set; } = [50, 95, 99];
    }
}
