using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.ObjectCaching.Services;
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
            => new(new FakeUserRepository(userRowId), new FakeEmployeeRepository(employee));

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

        private sealed class FakeUserRepository : IUserRepository
        {
            private readonly Guid _rowId;
            public FakeUserRepository(Guid rowId) { _rowId = rowId; }
            public Guid GetRowIdBySysId(string userId) => _rowId;
        }

        private sealed class FakeEmployeeRepository : IEmployeeRepository
        {
            private readonly EmployeeRow? _employee;
            public FakeEmployeeRepository(EmployeeRow? employee) { _employee = employee; }
            public EmployeeRow? GetByUserRowId(string databaseId, Guid userRowId) => _employee;
        }
    }
}
