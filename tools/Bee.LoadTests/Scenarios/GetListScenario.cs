using Bee.Definition.Paging;
using Bee.LoadTests.Running;

namespace Bee.LoadTests.Scenarios
{
    /// <summary>
    /// Reads one page of a list, the shape most requests in a business application take.
    /// </summary>
    public sealed class GetListScenario : IScenario
    {
        private const int PagesWalked = 10;

        private readonly VirtualUserPool _pool;
        private readonly string _progId;
        private readonly int _pageSize;
        private readonly int _startPage;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="pool">The signed-in user pool.</param>
        /// <param name="name">
        /// The scenario name, which is also how results are grouped. Two instances configured at
        /// different offsets need different names, or their samples merge into one row.
        /// </param>
        /// <param name="progId">The program to query.</param>
        /// <param name="pageSize">Rows per page.</param>
        /// <param name="startPage">The first page to request.</param>
        public GetListScenario(
            VirtualUserPool pool, string name, string progId, int pageSize, int startPage)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            Name = string.IsNullOrWhiteSpace(name) ? "GetList" : name;
            _progId = string.IsNullOrWhiteSpace(progId) ? "Customer" : progId;
            _pageSize = pageSize > 0 ? pageSize : 50;
            _startPage = startPage > 0 ? startPage : 1;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
        {
            var user = await _pool.GetAsync(context.VirtualUserIndex).ConfigureAwait(false);
            var connector = user.CreateFormConnector(_progId);

            // Pages are walked rather than always asking for the same one, so the run does not
            // measure a single page staying warm in whatever caches sit underneath.
            var page = _startPage + (int)(context.Iteration % PagesWalked);

            await connector.GetListAsync(
                paging: new PagingOptions { Page = page, PageSize = _pageSize })
                .ConfigureAwait(false);
        }
    }
}
