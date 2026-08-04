using Bee.Definition.Identity;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Default <see cref="IEmployeeContextResolver"/>: resolves the user's <c>st_user.sys_rowid</c>
    /// from the common database, then the linked <c>st_employee</c> (and its department) from the
    /// company database. No caching — invoked once per <c>EnterCompany</c>; the result is snapshotted
    /// onto the session so per-request scope filtering stays zero-DB.
    /// </summary>
    /// <remarks>
    /// Repositories come from <see cref="IRepositoryFactory"/> per call rather than being
    /// injected one by one, so adding a framework repository never widens this constructor.
    /// <para>
    /// The employee lookup takes the company database as an argument rather than routing from an
    /// access token: this runs while the session is being established, before the session can name a
    /// company. Routing from a token here would read the caller's company instead of the one being
    /// entered.
    /// </para>
    /// </remarks>
    public class EmployeeContextResolver : IEmployeeContextResolver
    {
        private readonly IRepositoryFactory _repositoryFactory;

        /// <summary>
        /// Initializes a new <see cref="EmployeeContextResolver"/>.
        /// </summary>
        /// <param name="repositoryFactory">Factory that builds framework repositories on demand.</param>
        public EmployeeContextResolver(IRepositoryFactory repositoryFactory)
        {
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        }

        /// <inheritdoc/>
        public EmployeeContext Resolve(string userId, string databaseId)
        {
            var userRowId = _repositoryFactory.Create<IUserRepository>().GetRowIdBySysId(userId);
            if (userRowId == Guid.Empty) { return EmployeeContext.Empty; }

            var employee = _repositoryFactory.Create<IEmployeeRepository>().GetByUserRowId(databaseId, userRowId);
            if (employee == null) { return new EmployeeContext(userRowId, Guid.Empty, Guid.Empty); }

            return new EmployeeContext(userRowId, employee.RowId, employee.DeptRowId);
        }
    }
}
