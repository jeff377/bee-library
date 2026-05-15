using System.ComponentModel;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Factories;
using Bee.Repository.Form;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    public class FormRepositoryFactoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public FormRepositoryFactoryTests(SharedDbFixture fx) { _fx = fx; }

        private FormRepositoryFactory NewFactory() => new(
            _fx.GetRequiredService<IDefineAccess>(),
            _fx.GetRequiredService<IDbAccessFactory>(),
            _fx.GetRequiredService<IDbConnectionManager>());

        [Fact]
        [DisplayName("建構子傳入 null IDefineAccess 應拋出 ArgumentNullException")]
        public void Ctor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(
                    null!,
                    _fx.GetRequiredService<IDbAccessFactory>(),
                    _fx.GetRequiredService<IDbConnectionManager>()));
        }

        [Fact]
        [DisplayName("建構子傳入 null IDbAccessFactory 應拋出 ArgumentNullException")]
        public void Ctor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(
                    _fx.GetRequiredService<IDefineAccess>(),
                    null!,
                    _fx.GetRequiredService<IDbConnectionManager>()));
        }

        [Fact]
        [DisplayName("建構子傳入 null IDbConnectionManager 應拋出 ArgumentNullException")]
        public void Ctor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(
                    _fx.GetRequiredService<IDefineAccess>(),
                    _fx.GetRequiredService<IDbAccessFactory>(),
                    null!));
        }

        [Fact]
        [DisplayName("CreateReportFormRepository 傳入有效 progId 應回傳 ReportFormRepository 型別")]
        public void CreateReportFormRepository_ValidProgId_ReturnsReportFormRepository()
        {
            var factory = NewFactory();
            var repo = factory.CreateReportFormRepository("Employee");
            Assert.IsType<ReportFormRepository>(repo);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 傳入 null progId 應拋出 ArgumentException")]
        public void CreateDataFormRepository_NullProgId_ThrowsArgumentException()
        {
            var factory = NewFactory();
            Assert.Throws<ArgumentException>(() => factory.CreateDataFormRepository(null!));
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 傳入空白 progId 應拋出 ArgumentException")]
        public void CreateDataFormRepository_WhitespaceProgId_ThrowsArgumentException()
        {
            var factory = NewFactory();
            Assert.Throws<ArgumentException>(() => factory.CreateDataFormRepository("   "));
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 傳入有效 progId 應回傳 IDataFormRepository 實作")]
        public void CreateDataFormRepository_ValidProgId_ReturnsDataFormRepository()
        {
            var factory = NewFactory();
            var repo = factory.CreateDataFormRepository("Employee");
            Assert.IsAssignableFrom<IDataFormRepository>(repo);
        }
    }
}
