using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Bee.LoadTests.Configuration;

namespace Bee.LoadTests.Reporting
{
    /// <summary>
    /// Everything a reader needs to interpret a set of numbers.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: without this a report is unusable. Latency figures mean nothing without the
    /// engine, the machine, the protection level and the load shape that produced them, and two
    /// reports can only be compared when both state all of it. Recording it is not optional
    /// bookkeeping — it is what makes the numbers a measurement rather than a number.
    /// </remarks>
    public sealed class RunMetadata
    {
        /// <summary>Gets or sets when the run finished.</summary>
        public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.Now;

        /// <summary>Gets or sets the driver's informational version, which carries the commit.</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Gets or sets the database engine.</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Gets or sets Local or Remote.</summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>Gets or sets the payload protection level.</summary>
        public string ProtectionLevel { get; set; } = string.Empty;

        /// <summary>Gets or sets the operating system description.</summary>
        public string OperatingSystem { get; set; } = string.Empty;

        /// <summary>Gets or sets the logical processor count.</summary>
        public int ProcessorCount { get; set; }

        /// <summary>Gets or sets the virtual user count.</summary>
        public int VirtualUsers { get; set; }

        /// <summary>Gets or sets the warm-up window in seconds.</summary>
        public int WarmupSeconds { get; set; }

        /// <summary>Gets or sets the measured window in seconds.</summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// Gets or sets the pacing model.
        /// </summary>
        /// <remarks>
        /// Closed and open models answer different questions, so a figure is only comparable
        /// against another produced the same way.
        /// </remarks>
        public string LoadModel { get; set; } = string.Empty;

        /// <summary>Gets or sets how the virtual users authenticated.</summary>
        public string TokenStrategy { get; set; } = string.Empty;

        /// <summary>Gets or sets rows seeded per table.</summary>
        public int SeededRows { get; set; }

        /// <summary>
        /// Gets or sets bindings dropped because their assembly was not loadable.
        /// </summary>
        /// <remarks>
        /// A dropped binding means that program ran on the framework's own implementation rather
        /// than the one the definitions name, which changes what the scenario measured.
        /// </remarks>
        public IReadOnlyList<string> DroppedBindings { get; set; } = [];

        /// <summary>
        /// Captures the metadata for a run.
        /// </summary>
        /// <param name="options">The run configuration.</param>
        /// <param name="droppedBindings">Bindings dropped during bootstrap.</param>
        /// <returns>The captured metadata.</returns>
        public static RunMetadata Capture(
            LoadTestOptions options, IReadOnlyList<string> droppedBindings)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(droppedBindings);

            return new RunMetadata
            {
                Version = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? "unknown",
                Provider = options.Database.Provider.ToString(),
                Mode = options.Target.Mode.ToString(),
                ProtectionLevel = options.Target.ProtectionLevel.ToString(),
                OperatingSystem = RuntimeInformation.OSDescription,
                ProcessorCount = Environment.ProcessorCount,
                VirtualUsers = options.Load.VirtualUsers,
                WarmupSeconds = options.Load.WarmupSeconds,
                DurationSeconds = options.Load.DurationSeconds,
                LoadModel = options.Load.Model.ToString(),
                TokenStrategy = options.Auth.TokenStrategy.ToString(),
                SeededRows = options.Seed.Enabled ? options.Seed.RowCount : 0,
                DroppedBindings = droppedBindings,
            };
        }

        /// <summary>
        /// Builds the file name stem for this run's reports.
        /// </summary>
        /// <returns>A timestamped stem, safe as a file name.</returns>
        public string ToFileStem()
            => CompletedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    }
}
