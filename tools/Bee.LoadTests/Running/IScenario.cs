namespace Bee.LoadTests.Running
{
    /// <summary>
    /// One unit of work a virtual user performs, timed as a single sample.
    /// </summary>
    public interface IScenario
    {
        /// <summary>
        /// Gets the scenario name, matched against the configuration.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Performs the work once.
        /// </summary>
        /// <param name="context">The calling virtual user's state.</param>
        /// <param name="cancellationToken">Cancelled when the measurement window closes.</param>
        Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken);
    }
}
