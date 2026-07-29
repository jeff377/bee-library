using Bee.Definition.Organization;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Department-tree snapshot service. The snapshot is built by the cache's read-through path,
    /// which resolves the company database and reads <c>st_department</c> — so subsequent scope
    /// queries run entirely from memory.
    /// </summary>
    /// <remarks>
    /// Cross-process invalidation: a writer that changes departments in a company database must
    /// bump the common cache-notify row <c>"DepartmentTree:{companyId}"</c> — the key must match
    /// exactly, since the cached entry carries it as its <c>ChangeNotifyKey</c>. The poller
    /// publishes the observed version, expiring the entry on its next read.
    /// </remarks>
    public class DepartmentTreeService : IDepartmentTreeService
    {
        private readonly ICacheContainer _cache;

        /// <summary>
        /// Initializes a new <see cref="DepartmentTreeService"/>.
        /// </summary>
        /// <param name="cache">The cache container hosting the department-tree cache.</param>
        public DepartmentTreeService(ICacheContainer cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc/>
        public DepartmentTree? Get(string companyId) => _cache.DepartmentTree.Get(companyId);

        /// <inheritdoc/>
        public void Remove(string companyId) => _cache.DepartmentTree.Remove(companyId);
    }
}
