using Bee.Definition.Paging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the GetLookup request.
    /// </summary>
    /// <remarks>
    /// Lookup queries return the field set declared by <c>FormSchema.LookupFields</c>
    /// (falling back to <c>sys_id</c> / <c>sys_name</c>), always prefixed with
    /// <c>sys_rowid</c>; the caller cannot widen the projection. The search text is
    /// matched server-side against the string-typed lookup fields.
    /// </remarks>
    public interface IGetLookupRequest
    {
        /// <summary>
        /// Gets the search text matched against the string-typed lookup fields;
        /// an empty value applies no search filter.
        /// </summary>
        string SearchText { get; }

        /// <summary>
        /// Gets the paging options; <c>null</c> applies the server-side default page size.
        /// </summary>
        PagingOptions? Paging { get; }
    }
}
