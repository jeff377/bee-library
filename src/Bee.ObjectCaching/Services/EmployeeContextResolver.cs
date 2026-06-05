using Bee.Definition.Identity;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Default <see cref="IEmployeeContextResolver"/>: resolves the user's <c>st_user.sys_rowid</c>
    /// from the common database, then the linked <c>ft_employee</c> (and its department) from the
    /// company database. No caching — invoked once per <c>EnterCompany</c>; the result is snapshotted
    /// onto the session so per-request scope filtering stays zero-DB.
    /// </summary>
    public class EmployeeContextResolver : IEmployeeContextResolver
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmployeeRepository _employeeRepository;

        /// <summary>
        /// Initializes a new <see cref="EmployeeContextResolver"/>.
        /// </summary>
        /// <param name="userRepository">The common <c>st_user</c> reader.</param>
        /// <param name="employeeRepository">The company <c>ft_employee</c> reader.</param>
        public EmployeeContextResolver(IUserRepository userRepository, IEmployeeRepository employeeRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        }

        /// <inheritdoc/>
        public EmployeeContext Resolve(string userId, string databaseId)
        {
            var userRowId = _userRepository.GetRowIdBySysId(userId);
            if (userRowId == Guid.Empty) { return EmployeeContext.Empty; }

            var employee = _employeeRepository.GetByUserRowId(databaseId, userRowId);
            if (employee == null) { return new EmployeeContext(userRowId, Guid.Empty, Guid.Empty); }

            return new EmployeeContext(userRowId, employee.RowId, employee.DeptRowId);
        }
    }
}
