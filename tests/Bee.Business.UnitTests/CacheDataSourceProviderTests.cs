using System.ComponentModel;
using Bee.Business.Providers;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="CacheDataSourceProvider"/> 測試。
    /// </summary>
    public class CacheDataSourceProviderTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public CacheDataSourceProviderTests(SharedDbFixture fx) { _fx = fx; }

        private CacheDataSourceProvider CreateProvider()
        {
            return new CacheDataSourceProvider(
                _fx.GetRequiredService<IRepositoryFactory>(), _fx.Provider);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetSessionInfo 傳入不存在的 Token 應回傳 null")]
        public void GetSessionInfo_UnknownToken_ReturnsNull()
        {
            var provider = CreateProvider();

            var result = provider.GetSessionInfo(Guid.NewGuid());

            Assert.Null(result);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetCompanyInfo 傳入不存在的公司代碼應回傳 null")]
        public void GetCompanyInfo_UnknownCompany_ReturnsNull()
        {
            var provider = CreateProvider();

            var result = provider.GetCompanyInfo("no_such_company");

            Assert.Null(result);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetCompanyRolePermissions 傳入不存在的公司代碼應回傳 null")]
        public void GetCompanyRolePermissions_UnknownCompany_ReturnsNull()
        {
            var provider = CreateProvider();

            var result = provider.GetCompanyRolePermissions("no_such_company");

            Assert.Null(result);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetDepartmentTree 傳入不存在的公司代碼應回傳 null")]
        public void GetDepartmentTree_UnknownCompany_ReturnsNull()
        {
            var provider = CreateProvider();

            var result = provider.GetDepartmentTree("no_such_company");

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("CacheDataSourceProvider 建構子傳 null 應拋 ArgumentNullException")]
        public void Constructor_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheDataSourceProvider(null!, _fx.Provider));
        }
    }
}
