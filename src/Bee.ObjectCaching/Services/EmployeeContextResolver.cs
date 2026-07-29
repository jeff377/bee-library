using Bee.Definition.Identity;
using Bee.Repository.Abstractions.Factories;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Default <see cref="IEmployeeContextResolver"/>: resolves the user's <c>st_user.sys_rowid</c>
    /// from the common database, then the linked <c>st_employee</c> (and its department) from the
    /// company database. No caching — invoked once per <c>EnterCompany</c>; the result is snapshotted
    /// onto the session so per-request scope filtering stays zero-DB.
    /// </summary>
    /// <remarks>
    /// Repositories come from <see cref="ISystemRepositoryFactory"/> per call rather than being
    /// injected one by one, so adding a system repository never widens this constructor.
    /// </remarks>
    public class EmployeeContextResolver : IEmployeeContextResolver
    {
        private readonly ISystemRepositoryFactory _systemFactory;

        /// <summary>
        /// Initializes a new <see cref="EmployeeContextResolver"/>.
        /// </summary>
        /// <param name="systemFactory">Factory that builds system-level repositories on demand.</param>
        public EmployeeContextResolver(ISystemRepositoryFactory systemFactory)
        {
            _systemFactory = systemFactory ?? throw new ArgumentNullException(nameof(systemFactory));
        }

        /// <inheritdoc/>
        public EmployeeContext Resolve(string userId, string databaseId)
        {
            var userRowId = _systemFactory.CreateUserRepository().GetRowIdBySysId(userId);
            if (userRowId == Guid.Empty) { return EmployeeContext.Empty; }

            var employee = _systemFactory.CreateEmployeeRepository().GetByUserRowId(databaseId, userRowId);
            if (employee == null) { return new EmployeeContext(userRowId, Guid.Empty, Guid.Empty); }

            return new EmployeeContext(userRowId, employee.RowId, employee.DeptRowId);
        }
    }
}
