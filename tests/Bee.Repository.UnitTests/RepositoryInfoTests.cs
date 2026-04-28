using System.ComponentModel;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Bee.Repository.Factories;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="RepositoryInfo"/> 靜態屬性的測試。
    /// 依賴 <see cref="GlobalFixture"/> 完成 <c>BackendInfo</c> 初始化，觸發 static ctor 的正常路徑。
    /// </summary>
    [Collection("Initialize")]
    public class RepositoryInfoTests
    {
        [Fact]
        [DisplayName("RepositoryInfo.SystemFactory 於 GlobalFixture 初始化後不應為 null")]
        public void SystemProvider_AfterFixtureInit_IsNotNull()
        {
            Assert.NotNull(RepositoryInfo.SystemFactory);
        }

        [Fact]
        [DisplayName("RepositoryInfo.FormFactory 於 GlobalFixture 初始化後不應為 null")]
        public void FormProvider_AfterFixtureInit_IsNotNull()
        {
            Assert.NotNull(RepositoryInfo.FormFactory);
        }

        [Fact]
        [DisplayName("RepositoryInfo.SystemFactory 預設型別應為 SystemRepositoryFactory")]
        public void SystemProvider_DefaultType_IsSystemRepositoryFactory()
        {
            Assert.IsType<SystemRepositoryFactory>(RepositoryInfo.SystemFactory);
        }

        [Fact]
        [DisplayName("RepositoryInfo.FormFactory 預設型別應為 FormRepositoryFactory")]
        public void FormProvider_DefaultType_IsFormRepositoryFactory()
        {
            Assert.IsType<FormRepositoryFactory>(RepositoryInfo.FormFactory);
        }

        [Fact]
        [DisplayName("RepositoryInfo.SystemFactory 可被外部替換並讀回")]
        public void SystemProvider_CanBeReplaced()
        {
            var original = RepositoryInfo.SystemFactory;
            try
            {
                var stub = new StubSystemRepositoryFactory();
                RepositoryInfo.SystemFactory = stub;
                Assert.Same(stub, RepositoryInfo.SystemFactory);
            }
            finally
            {
                RepositoryInfo.SystemFactory = original;
            }
        }

        [Fact]
        [DisplayName("RepositoryInfo.FormFactory 可被外部替換並讀回")]
        public void FormProvider_CanBeReplaced()
        {
            var original = RepositoryInfo.FormFactory;
            try
            {
                var stub = new StubFormRepositoryFactory();
                RepositoryInfo.FormFactory = stub;
                Assert.Same(stub, RepositoryInfo.FormFactory);
            }
            finally
            {
                RepositoryInfo.FormFactory = original;
            }
        }

        private sealed class StubSystemRepositoryFactory : ISystemRepositoryFactory
        {
            public IDatabaseRepository CreateDatabaseRepository() => null!;
            public ISessionRepository CreateSessionRepository() => null!;
        }

        private sealed class StubFormRepositoryFactory : IFormRepositoryFactory
        {
            public IDataFormRepository CreateDataFormRepository(string progId) => null!;
            public IReportFormRepository CreateReportFormRepository(string progId) => null!;
        }
    }
}
