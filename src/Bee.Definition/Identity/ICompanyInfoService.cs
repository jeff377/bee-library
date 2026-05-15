namespace Bee.Definition.Identity
{
    /// <summary>
    /// Interface for a company info access service.
    /// Uses cache as the primary source, with fallback to database loading or persistence when necessary.
    /// </summary>
    public interface ICompanyInfoService
    {
        /// <summary>
        /// Gets company info from cache (with fallback to database on a cache miss).
        /// </summary>
        CompanyInfo? Get(string companyId);

        /// <summary>
        /// Stores company info in cache (and persists if necessary).
        /// </summary>
        void Set(CompanyInfo companyInfo);

        /// <summary>
        /// Removes the specified company info from cache (and invalidates the persisted state if necessary).
        /// </summary>
        void Remove(string companyId);
    }
}
