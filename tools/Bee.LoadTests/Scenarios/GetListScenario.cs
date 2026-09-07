using Bee.Definition.Paging;
using Bee.LoadTests.Running;

namespace Bee.LoadTests.Scenarios
{
    /// <summary>
    /// Reads one page of a list, the shape most requests in a business application take.
    /// </summary>
    public sealed class GetListScenario : IScenario
    {
        private readonly VirtualUserPool _pool;
        private readonly string _progId;
        private readonly int _pageSize;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="pool">The signed-in user pool.</param>
        /// <param name="progId">The program to query.</param>
        /// <param name="pageSize">Rows per page.</param>
        public GetListScenario(VirtualUserPool pool, string progId, int pageSize)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _progId = string.IsNullOrWhiteSpace(progId) ? "Customer" : progId;
            _pageSize = pageSize > 0 ? pageSize : 50;
        }

        /// <inheritdoc/>
        public string Name => "GetList";

        /// <inheritdoc/>
        public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
        {
            var user = await _pool.GetAsync(context.VirtualUserIndex).ConfigureAwait(false);
            var connector = user.CreateFormConnector(_progId);

            // Pages are walked rather than always asking for the first one, so the run does not
            // measure a single page staying warm in whatever caches sit underneath.
            var page = (int)(context.Iteration % 10) + 1;

            await connector.GetListAsync(
                paging: new PagingOptions { Page = page, PageSize = _pageSize })
                .ConfigureAwait(false);
        }
    }
}
