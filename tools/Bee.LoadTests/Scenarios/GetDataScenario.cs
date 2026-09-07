using Bee.LoadTests.Running;

namespace Bee.LoadTests.Scenarios
{
    /// <summary>
    /// Reads one record by key, the shape a detail screen issues.
    /// </summary>
    /// <remarks>
    /// The keys are collected before the run rather than looked up during it. Fetching a key first
    /// would fold that query into every sample, and the scenario would report the cost of two
    /// calls while claiming to measure one.
    /// </remarks>
    public sealed class GetDataScenario : IScenario
    {
        private readonly VirtualUserPool _pool;
        private readonly string _progId;
        private readonly Guid[] _rowIds;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="pool">The signed-in user pool.</param>
        /// <param name="progId">The program to read from.</param>
        /// <param name="rowIds">Keys to read, collected before the run.</param>
        public GetDataScenario(VirtualUserPool pool, string progId, IReadOnlyCollection<Guid> rowIds)
        {
            ArgumentNullException.ThrowIfNull(rowIds);
            if (rowIds.Count == 0)
            {
                throw new ArgumentException(
                    "No keys were collected, so this scenario has nothing to read. Run 'prepare' " +
                    "first, or disable the scenario.", nameof(rowIds));
            }

            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _progId = string.IsNullOrWhiteSpace(progId) ? "Customer" : progId;
            _rowIds = [.. rowIds];
        }

        /// <inheritdoc/>
        public string Name => "GetData";

        /// <inheritdoc/>
        public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
        {
            var user = await _pool.GetAsync(context.VirtualUserIndex).ConfigureAwait(false);
            var connector = user.CreateFormConnector(_progId);

            // Walk the keys rather than re-reading one, so the run does not measure a single
            // record staying warm in whatever caches sit underneath.
            var rowId = _rowIds[(int)(context.Iteration % _rowIds.Length)];

            await connector.GetDataAsync(rowId).ConfigureAwait(false);
        }
    }
}
