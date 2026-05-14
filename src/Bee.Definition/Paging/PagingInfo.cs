using MessagePack;

namespace Bee.Definition.Paging
{
    /// <summary>
    /// Paging metadata accompanying a paged list-style API result.
    /// </summary>
    [MessagePackObject]
    public sealed class PagingInfo
    {
        /// <summary>
        /// Gets or sets the 1-based page index that produced this result.
        /// </summary>
        [Key(0)]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the effective page size after server-side clamping.
        /// </summary>
        [Key(1)]
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total matching row count; <c>null</c> when the request did
        /// not set <c>IncludeTotalCount = true</c>.
        /// </summary>
        [Key(2)]
        public int? TotalCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether more rows exist beyond the current page.
        /// </summary>
        [Key(3)]
        public bool HasMore { get; set; }
    }
}
