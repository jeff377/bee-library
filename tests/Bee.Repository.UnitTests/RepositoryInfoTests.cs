using System.ComponentModel;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Abstractions.Providers;
using Bee.Repository.Abstractions.System;
using Bee.Repository.Providers;
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
        [DisplayName("RepositoryInfo.SystemProvider 於 GlobalFixture 初始化後不應為 null")]
        public void SystemProvider_AfterFixtureInit_IsNotNull()
        {
            Assert.NotNull(RepositoryInfo.SystemProvider);
        }

        [Fact]
        [DisplayName("RepositoryInfo.FormProvider 於 GlobalFixture 初始化後不應為 null")]
        public void FormProvider_AfterFixtureInit_IsNotNull()
        {
            Assert.NotNull(RepositoryInfo.FormProvider);
        }

        [Fact]
        [DisplayName("RepositoryInfo.SystemProvider 預設型別應為 SystemRepositoryProvider")]
        public void SystemProvider_DefaultType_IsSystemRepositoryProvider()
        {
            Assert.IsType<SystemRepositoryProvider>(RepositoryInfo.SystemProvider);
        }

        [Fact]
        [DisplayName("RepositoryInfo.FormProvider 預設型別應為 FormRepositoryProvider")]
        public void FormProvider_DefaultType_IsFormRepositoryProvider()
        {
            Assert.IsType<FormRepositoryProvider>(RepositoryInfo.FormProvider);
        }

        [Fact]
        [DisplayName("RepositoryInfo.SystemProvider 可被外部替換並讀回")]
        public void SystemProvider_CanBeReplaced()
        {
            var original = RepositoryInfo.SystemProvider;
            try
            {
                var stub = new StubSystemRepositoryProvider();
                RepositoryInfo.SystemProvider = stub;
                Assert.Same(stub, RepositoryInfo.SystemProvider);
            }
            finally
            {
                RepositoryInfo.SystemProvider = original;
            }
        }

        [Fact]
        [DisplayName("RepositoryInfo.FormProvider 可被外部替換並讀回")]
        public void FormProvider_CanBeReplaced()
        {
            var original = RepositoryInfo.FormProvider;
            try
            {
                var stub = new StubFormRepositoryProvider();
                RepositoryInfo.FormProvider = stub;
                Assert.Same(stub, RepositoryInfo.FormProvider);
            }
            finally
            {
                RepositoryInfo.FormProvider = original;
            }
        }

        private sealed class StubSystemRepositoryProvider : ISystemRepositoryProvider
        {
            public IDatabaseRepository DatabaseRepository { get; set; } = null!;
            public ISessionRepository SessionRepository { get; set; } = null!;
        }

        private sealed class StubFormRepositoryProvider : IFormRepositoryProvider
        {
            public IDataFormRepository GetDataFormRepository(string progId) => null!;
            public IReportFormRepository GetReportFormRepository(string progId) => null!;
        }
    }
}
