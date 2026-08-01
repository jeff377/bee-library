using System.ComponentModel;
using Bee.Base.Exceptions;
using Bee.Business.System;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject.SetDeploymentAdmin"/> 的整合測試：旗標確實寫進
    /// <c>st_user</c>，以及查無使用者 / 空白 userId 的拒絕情境。
    /// </summary>
    /// <remarks>
    /// 每個測試建自己的使用者列並在 finally 刪除，不動 seed 使用者 '001'——實體資料庫由多個
    /// 平行測試行程共用（見 <see cref="TestUsers"/>）。
    /// </remarks>
    public class SystemBusinessObjectDeploymentAdminTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectDeploymentAdminTests(SharedDbFixture fx) { _fx = fx; }

        private SystemBusinessObject CreateBo()
            => new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);

        private IDbConnectionManager ConnectionManager => _fx.GetRequiredService<IDbConnectionManager>();

        private Repository.Abstractions.System.IUserRepository Repo
            => _fx.GetRequiredService<ISystemRepositoryFactory>().CreateUserRepository();

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetDeploymentAdmin 應把旗標寫進 st_user，且可再撤銷")]
        public void SetDeploymentAdmin_GrantsThenRevokes()
        {
            string userId = TestUsers.Create(ConnectionManager, "bo-admin");
            try
            {
                var granted = CreateBo().SetDeploymentAdmin(new SetDeploymentAdminArgs
                {
                    UserId = userId,
                    IsDeploymentAdmin = true,
                });

                Assert.Equal(userId, granted.UserId);
                Assert.True(granted.IsDeploymentAdmin);
                Assert.True(Repo.IsDeploymentAdmin(userId));

                var revoked = CreateBo().SetDeploymentAdmin(new SetDeploymentAdminArgs
                {
                    UserId = userId,
                    IsDeploymentAdmin = false,
                });

                Assert.False(revoked.IsDeploymentAdmin);
                Assert.False(Repo.IsDeploymentAdmin(userId));
            }
            finally
            {
                TestUsers.Delete(ConnectionManager, userId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetDeploymentAdmin 於查無使用者時應以可讀訊息拒絕")]
        public void SetDeploymentAdmin_UnknownUser_ThrowsUserMessage()
        {
            var args = new SetDeploymentAdminArgs { UserId = "no-such-user", IsDeploymentAdmin = true };

            Assert.Throws<UserMessageException>(() => CreateBo().SetDeploymentAdmin(args));
        }

        [Fact]
        [DisplayName("SetDeploymentAdmin 於未給 userId 時應拒絕")]
        public void SetDeploymentAdmin_MissingUserId_ThrowsUserMessage()
        {
            var args = new SetDeploymentAdminArgs { UserId = string.Empty, IsDeploymentAdmin = true };

            Assert.Throws<UserMessageException>(() => CreateBo().SetDeploymentAdmin(args));
        }

        [Fact]
        [DisplayName("SetDeploymentAdmin 於 args 為 null 時應拋 ArgumentNullException")]
        public void SetDeploymentAdmin_NullArgs_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateBo().SetDeploymentAdmin(null!));
        }
    }
}
