using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Department-tree snapshot service. On a cache miss, resolves the company database via
    /// <see cref="ICompanyInfoService"/>, reads <c>ft_department</c> via
    /// <see cref="IDepartmentRepository"/>, builds a <see cref="DepartmentTree"/> snapshot and
    /// caches it — so subsequent scope queries run entirely from memory.
    /// </summary>
    /// <remarks>
    /// Cross-process invalidation: a writer that changes departments in a company database must
    /// bump the common cache-notify row <c>"DepartmentTree:{companyId}"</c>; the poller then
    /// dispatches it through <see cref="ICacheContainer.TryEvict(string)"/>.
    /// </remarks>
    public class DepartmentTreeService : IDepartmentTreeService
    {
        private readonly ICacheContainer _cache;
        private readonly ICompanyInfoService _companyInfoService;
        private readonly IDepartmentRepository _repository;

        /// <summary>
        /// Initializes a new <see cref="DepartmentTreeService"/>.
        /// </summary>
        /// <param name="cache">The cache container hosting the department-tree cache.</param>
        /// <param name="companyInfoService">Resolves the company database id from the company id.</param>
        /// <param name="repository">The DB-backed department reader.</param>
        public DepartmentTreeService(ICacheContainer cache, ICompanyInfoService companyInfoService, IDepartmentRepository repository)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _companyInfoService = companyInfoService ?? throw new ArgumentNullException(nameof(companyInfoService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public DepartmentTree? Get(string companyId)
        {
            var cached = _cache.DepartmentTree.Get(companyId);
            if (cached != null) { return cached; }

            var company = _companyInfoService.Get(companyId);
            if (company == null) { return null; }

            var rows = _repository.GetDepartments(company.CompanyDatabaseId);
            var tree = new DepartmentTree(companyId, rows);
            _cache.DepartmentTree.Set(tree);
            return tree;
        }

        /// <inheritdoc/>
        public void Remove(string companyId) => _cache.DepartmentTree.Remove(companyId);
    }
}
