
namespace Bee.Definition.Paging
{
    /// <summary>
    /// Page-based paging request carried on list-style API calls. Uses 1-based
    /// page indexing — the value is clamped on the server to a sane range.
    /// </summary>
    /// <remarks>
    /// When the framework processes this options object:
    /// <list type="bullet">
    /// <item><c>Page</c> values below 1 are clamped to 1.</item>
    /// <item><c>PageSize</c> values are clamped to <c>[1, MaxPageSize]</c> (framework cap).</item>
    /// <item>When <c>IncludeTotalCount</c> is false, the server uses a
    /// <c>PageSize + 1</c> probe to compute <c>HasMore</c> without an extra COUNT query.</item>
    /// </list>
    /// </remarks>
    public sealed class PagingOptions
    {
        /// <summary>
        /// Gets or sets the 1-based page index. Values below 1 are clamped to 1 on the server.
        /// </summary>
        /// <remarks>
        /// Cost grows with the page index. Page-based paging is served by <c>OFFSET</c>, so
        /// reaching a high page means the engine walks and discards every row before it. A page
        /// deep into a large result set therefore costs materially more than the first one, and
        /// the gap widens as the table grows while the first page's cost stays flat. This is
        /// inherent to offset paging rather than particular to one engine, so switching database
        /// provider does not avoid it. A screen that lets a user reach arbitrary page numbers
        /// over a large result set is usually better served by narrowing the result with a
        /// filter than by paging deeper.
        /// </remarks>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Gets or sets the number of rows per page. Values above the framework cap are
        /// clamped on the server; values below 1 are clamped to 1.
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets a value indicating whether the response carries the total
        /// matching row count. Defaults to <c>false</c> because COUNT is an additional
        /// round-trip; <c>HasMore</c> alone is sufficient for most UI flows.
        /// </summary>
        public bool IncludeTotalCount { get; set; }
    }
}
