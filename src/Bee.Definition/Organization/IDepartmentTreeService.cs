namespace Bee.Definition.Organization
{
    /// <summary>
    /// Access service for per-company department-tree snapshots: a cache fronting the company
    /// database's <c>ft_department</c> table.
    /// </summary>
    public interface IDepartmentTreeService
    {
        /// <summary>
        /// Gets the company's department tree from cache; on a cache miss, loads it from the
        /// company database and populates the cache before returning. Returns <c>null</c> when
        /// the company doesn't exist.
        /// </summary>
        /// <param name="companyId">The company id.</param>
        DepartmentTree? Get(string companyId);

        /// <summary>
        /// Removes the company's department tree from the cache.
        /// </summary>
        /// <param name="companyId">The company id.</param>
        void Remove(string companyId);
    }
}
