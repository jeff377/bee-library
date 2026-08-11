using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.ObjectCaching.Services;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.UnitTests.Services
{
    /// <summary>
    /// 驗證 <see cref="DeploymentAuthorizationService"/> 的判定：旗標決定一切，
    /// 且無公司脈絡也成立——這正是它與公司層 <c>CompanyAuthorizationService</c> 的分野。
    /// </summary>
    public class DeploymentAuthorizationServiceTests
    {
        private static readonly Guid s_token = Guid.NewGuid();

        private static DeploymentAuthorizationService Create(SessionInfo? session, bool isAdmin, bool repositoryThrows = false)
            => new DeploymentAuthorizationService(
                new FakeSessionInfoService(session),
                new FakeRepositoryFactory(new FakeUserRepository(isAdmin, repositoryThrows)));

        private static SessionInfo NewSession(string userId, string? companyId = null)
            => new SessionInfo { AccessToken = s_token, UserId = userId, CompanyId = companyId };

        [Fact]
        [DisplayName("旗標為 true 的使用者應被授權")]
        public void Can_DeploymentAdmin_ReturnsTrue()
        {
            var service = Create(NewSession("001"), isAdmin: true);

            Assert.True(service.Can(s_token, DeploymentAction.ManageApiKey));
        }

        [Fact]
        [DisplayName("旗標為 false 的已登入使用者應被拒")]
        public void Can_AuthenticatedNonAdmin_ReturnsFalse()
        {
            var service = Create(NewSession("001"), isAdmin: false);

            Assert.False(service.Can(s_token, DeploymentAction.ManageApiKey));
        }

        [Fact]
        [DisplayName("未進入公司不影響判定——部署層權限不繫結公司")]
        public void Can_WithoutCompanyContext_StillAuthorizes()
        {
            var service = Create(NewSession("001", companyId: null), isAdmin: true);

            Assert.True(service.Can(s_token, DeploymentAction.ManageApiKey));
        }

        [Fact]
        [DisplayName("查無 session 應被拒")]
        public void Can_UnknownToken_ReturnsFalse()
        {
            var service = Create(session: null, isAdmin: true);

            Assert.False(service.Can(s_token, DeploymentAction.ManageApiKey));
        }

        [Fact]
        [DisplayName("session 無 UserId 應被拒，不查資料庫")]
        public void Can_SessionWithoutUserId_ReturnsFalse()
        {
            // repositoryThrows 為 true：真的查了就會擲例外，藉此證明這條路徑不查庫。
            var service = Create(NewSession(string.Empty), isAdmin: true, repositoryThrows: true);

            Assert.False(service.Can(s_token, DeploymentAction.ManageApiKey));
        }

        private sealed class FakeSessionInfoService : ISessionInfoService
        {
            private readonly SessionInfo? _session;
            public FakeSessionInfoService(SessionInfo? session) { _session = session; }
            public SessionInfo Get(Guid accessToken) => _session!;
            public void Set(SessionInfo sessionInfo) => throw new NotSupportedException();
            public void Remove(Guid accessToken) => throw new NotSupportedException();
        }

        private sealed class FakeRepositoryFactory : IRepositoryFactory
        {
            private readonly IUserRepository _userRepository;
            public FakeRepositoryFactory(IUserRepository userRepository) { _userRepository = userRepository; }

            public T Create<T>(Guid accessToken = default) where T : class
                => typeof(T) == typeof(IUserRepository)
                    ? (T)_userRepository
                    : throw new NotSupportedException(typeof(T).FullName);

            public T CreateFormRepository<T>(Guid accessToken, string progId) where T : class, IDataFormRepository
                => throw new NotSupportedException();
        }

        private sealed class FakeUserRepository : IUserRepository
        {
            private readonly bool _isAdmin;
            private readonly bool _throws;
            public FakeUserRepository(bool isAdmin, bool throws) { _isAdmin = isAdmin; _throws = throws; }
            public Guid GetRowIdBySysId(string userId) => throw new NotSupportedException();
            public bool VerifyPassword(string userId, string password) => throw new NotSupportedException();
            public UserLocale GetLocale(string userId) => throw new NotSupportedException();
            public string? GetName(string userId) => throw new NotSupportedException();
            public bool IsDeploymentAdmin(string userId)
                => _throws ? throw new InvalidOperationException("should not query") : _isAdmin;
            public bool SetDeploymentAdmin(string userId, bool isDeploymentAdmin) => throw new NotSupportedException();
        }
    }
}
