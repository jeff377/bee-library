using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.ObjectCaching.Services;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.UnitTests.Services
{
    /// <summary>
    /// EmployeeContextResolver.Resolve 的解析串接測試（以 fake user / employee repository 隔離）：
    /// 有對應員工、無對應員工、未知 user、無部門員工四種情境。
    /// </summary>
    public class EmployeeContextResolverTests
    {
        private const string DbId = "company_x";

        private static readonly Guid s_userRowId = Guid.NewGuid();
        private static readonly Guid s_employeeRowId = Guid.NewGuid();
        private static readonly Guid s_deptRowId = Guid.NewGuid();

        private static EmployeeContextResolver Create(Guid userRowId, EmployeeRow? employee)
            => new(new FakeSystemRepositoryFactory(userRowId, employee));

        [Fact]
        [DisplayName("Resolve 有對應員工回完整 context（user/employee/dept）")]
        public void Resolve_WithEmployee_ReturnsFullContext()
        {
            var employee = new EmployeeRow(s_employeeRowId, "E001", "Alice", s_deptRowId, s_userRowId);
            var resolver = Create(s_userRowId, employee);

            var ctx = resolver.Resolve("001", DbId);

            Assert.Equal(s_userRowId, ctx.UserRowId);
            Assert.Equal(s_employeeRowId, ctx.EmployeeRowId);
            Assert.Equal(s_deptRowId, ctx.DeptRowId);
        }

        [Fact]
        [DisplayName("Resolve user 存在但無對應員工回 user rowid、employee/dept 為空")]
        public void Resolve_NoEmployee_ReturnsUserOnly()
        {
            var resolver = Create(s_userRowId, employee: null);

            var ctx = resolver.Resolve("001", DbId);

            Assert.Equal(s_userRowId, ctx.UserRowId);
            Assert.Equal(Guid.Empty, ctx.EmployeeRowId);
            Assert.Equal(Guid.Empty, ctx.DeptRowId);
        }

        [Fact]
        [DisplayName("Resolve 未知 user 回空 context")]
        public void Resolve_UnknownUser_ReturnsEmpty()
        {
            // user repository 回 Guid.Empty（查無此帳號）→ 不再查 employee。
            var resolver = Create(Guid.Empty, new EmployeeRow(s_employeeRowId, "E001", "Alice", s_deptRowId, s_userRowId));

            var ctx = resolver.Resolve("nobody", DbId);

            Assert.Equal(EmployeeContext.Empty, ctx);
        }

        [Fact]
        [DisplayName("Resolve 員工無部門回 dept 為空")]
        public void Resolve_EmployeeWithoutDept_ReturnsEmptyDept()
        {
            var employee = new EmployeeRow(s_employeeRowId, "E001", "Alice", Guid.Empty, s_userRowId);
            var resolver = Create(s_userRowId, employee);

            var ctx = resolver.Resolve("001", DbId);

            Assert.Equal(s_userRowId, ctx.UserRowId);
            Assert.Equal(s_employeeRowId, ctx.EmployeeRowId);
            Assert.Equal(Guid.Empty, ctx.DeptRowId);
        }

        /// <summary>
        /// 只實作 EmployeeContextResolver 會用到的兩個 Create 方法；其餘一律擲例外，
        /// 讓非預期的 repository 取用在測試中立即現形。
        /// </summary>
        private sealed class FakeSystemRepositoryFactory : ISystemRepositoryFactory
        {
            private readonly Guid _userRowId;
            private readonly EmployeeRow? _employee;

            public FakeSystemRepositoryFactory(Guid userRowId, EmployeeRow? employee)
            {
                _userRowId = userRowId;
                _employee = employee;
            }

            public IUserRepository CreateUserRepository() => new FakeUserRepository(_userRowId);
            public IEmployeeRepository CreateEmployeeRepository() => new FakeEmployeeRepository(_employee);

            public IDatabaseRepository CreateDatabaseRepository() => throw new NotSupportedException();
            public IApiKeyRepository CreateApiKeyRepository() => throw new NotSupportedException();
            public ISessionRepository CreateSessionRepository() => throw new NotSupportedException();
            public ICompanyRepository CreateCompanyRepository() => throw new NotSupportedException();
            public IUserCompanyRepository CreateUserCompanyRepository() => throw new NotSupportedException();
            public IRolePermissionRepository CreateRolePermissionRepository() => throw new NotSupportedException();
            public IDepartmentRepository CreateDepartmentRepository() => throw new NotSupportedException();
        }

        private sealed class FakeUserRepository : IUserRepository
        {
            private readonly Guid _rowId;
            public FakeUserRepository(Guid rowId) { _rowId = rowId; }
            public Guid GetRowIdBySysId(string userId) => _rowId;
            public UserLocale GetLocale(string userId) => UserLocale.Empty;
            public string? GetName(string userId) => string.Empty;
        }

        private sealed class FakeEmployeeRepository : IEmployeeRepository
        {
            private readonly EmployeeRow? _employee;
            public FakeEmployeeRepository(EmployeeRow? employee) { _employee = employee; }
            public EmployeeRow? GetByUserRowId(string databaseId, Guid userRowId) => _employee;
        }
    }
}
