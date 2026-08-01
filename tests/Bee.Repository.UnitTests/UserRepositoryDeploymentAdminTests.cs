using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 驗證 <c>st_user.deployment_admin</c> 的讀寫，含「查無使用者」的降級行為。
    /// </summary>
    /// <remarks>
    /// 同 <see cref="UserRepositoryLocaleTests"/>：<see cref="UserRepository"/> 內部自行解析
    /// <c>common</c> 分類，因此這**不是** per-provider 矩陣，<c>[DbFact]</c> 僅用於該 provider
    /// 不可連線時自動跳過。
    ///
    /// 每個測試建自己的使用者列而不動 seed 使用者 '001'——實體資料庫由多個平行測試行程共用，
    /// 共用同一列會讓「旗標現在是什麼」變成競賽（見 <see cref="TestUsers"/>）。
    /// </remarks>
    public class UserRepositoryDeploymentAdminTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public UserRepositoryDeploymentAdminTests(SharedDbFixture fx) { _fx = fx; }

        private IDbConnectionManager ConnectionManager => _fx.GetRequiredService<IDbConnectionManager>();

        private UserRepository CreateRepo() => new UserRepository(ConnectionManager);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SetDeploymentAdmin 寫入後 IsDeploymentAdmin 應讀回同一值")]
        public void SetDeploymentAdmin_RoundTrips()
        {
            string userId = TestUsers.Create(ConnectionManager, "repo-admin");
            try
            {
                var repo = CreateRepo();

                Assert.True(repo.SetDeploymentAdmin(userId, true));
                Assert.True(repo.IsDeploymentAdmin(userId));

                Assert.True(repo.SetDeploymentAdmin(userId, false));
                Assert.False(repo.IsDeploymentAdmin(userId));
            }
            finally
            {
                TestUsers.Delete(ConnectionManager, userId);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("新建的使用者預設不是部署層管理員（欄位 DEFAULT 生效）")]
        public void IsDeploymentAdmin_NewUser_DefaultsToFalse()
        {
            string userId = TestUsers.Create(ConnectionManager, "repo-default");
            try
            {
                Assert.False(CreateRepo().IsDeploymentAdmin(userId));
            }
            finally
            {
                TestUsers.Delete(ConnectionManager, userId);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("IsDeploymentAdmin 查無使用者應回傳 false（授權問題，兩種情況都拒）")]
        public void IsDeploymentAdmin_UnknownUser_ReturnsFalse()
        {
            Assert.False(CreateRepo().IsDeploymentAdmin("no-such-user"));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SetDeploymentAdmin 查無使用者應回傳 false 而非擲例外")]
        public void SetDeploymentAdmin_UnknownUser_ReturnsFalse()
        {
            Assert.False(CreateRepo().SetDeploymentAdmin("no-such-user", true));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("空白 userId 應直接回傳 false，不查資料庫")]
        public void BlankUserId_ReturnsFalse(string userId)
        {
            var repo = CreateRepo();
            Assert.False(repo.IsDeploymentAdmin(userId));
            Assert.False(repo.SetDeploymentAdmin(userId, true));
        }
    }
}
