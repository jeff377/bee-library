using System.ComponentModel;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Storage;
using Bee.Repository.Factories;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="FormRepositoryFactory"/> 的純邏輯測試。
    /// </summary>
    public class FormRepositoryFactoryTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public FormRepositoryFactoryTests(BeeTestFixture fx) { _fx = fx; }

        private FormRepositoryFactory CreateFactory() =>
            new(_fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<IDbAccessFactory>(),
                _fx.GetRequiredService<IDbConnectionManager>());

        [Fact]
        [DisplayName("Ctor 傳入 null IDefineAccess 應拋出 ArgumentNullException")]
        public void Ctor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormRepositoryFactory(
                null!,
                _fx.GetRequiredService<IDbAccessFactory>(),
                _fx.GetRequiredService<IDbConnectionManager>()));
        }

        [Fact]
        [DisplayName("Ctor 傳入 null IDbAccessFactory 應拋出 ArgumentNullException")]
        public void Ctor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormRepositoryFactory(
                _fx.GetRequiredService<IDefineAccess>(),
                null!,
                _fx.GetRequiredService<IDbConnectionManager>()));
        }

        [Fact]
        [DisplayName("Ctor 傳入 null IDbConnectionManager 應拋出 ArgumentNullException")]
        public void Ctor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormRepositoryFactory(
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<IDbAccessFactory>(),
                null!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("CreateDataFormRepository 傳入 null 或空白 progId 應拋出 ArgumentException")]
        public void CreateDataFormRepository_NullOrWhitespaceProgId_ThrowsArgumentException(string? progId)
        {
            var factory = CreateFactory();
            Assert.Throws<ArgumentException>(() => factory.CreateDataFormRepository(progId!));
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 有效 progId 應回傳 IDataFormRepository 實例")]
        public void CreateDataFormRepository_ValidProgId_ReturnsIDataFormRepository()
        {
            var factory = CreateFactory();
            var repo = factory.CreateDataFormRepository("Employee");
            Assert.NotNull(repo);
        }

        [Fact]
        [DisplayName("CreateReportFormRepository 有效 progId 應回傳 IReportFormRepository 實例")]
        public void CreateReportFormRepository_ValidProgId_ReturnsIReportFormRepository()
        {
            var factory = CreateFactory();
            var repo = factory.CreateReportFormRepository("Employee");
            Assert.NotNull(repo);
        }
    }
}
