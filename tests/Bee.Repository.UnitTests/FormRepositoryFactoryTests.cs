using System.ComponentModel;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Factories;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    public class FormRepositoryFactoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public FormRepositoryFactoryTests(SharedDbFixture fx) { _fx = fx; }

        private FormRepositoryFactory CreateFactory(IDefineAccess? defineAccess = null)
            => new(
                defineAccess ?? _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<IDbAccessFactory>(),
                _fx.GetRequiredService<IDbConnectionManager>());

        [Fact]
        [DisplayName("FormRepositoryFactory 傳入 null defineAccess 應拋 ArgumentNullException")]
        public void Ctor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(null!, null!, null!));
        }

        [Fact]
        [DisplayName("FormRepositoryFactory 傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void Ctor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(
                    _fx.GetRequiredService<IDefineAccess>(),
                    null!,
                    null!));
        }

        [Fact]
        [DisplayName("FormRepositoryFactory 傳入 null connectionManager 應拋 ArgumentNullException")]
        public void Ctor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(
                    _fx.GetRequiredService<IDefineAccess>(),
                    _fx.GetRequiredService<IDbAccessFactory>(),
                    null!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("CreateDataFormRepository 空白或 null progId 應拋 ArgumentException")]
        public void CreateDataFormRepository_BlankProgId_ThrowsArgumentException(string? progId)
        {
            var factory = CreateFactory();
            var ex = Record.Exception(() => factory.CreateDataFormRepository(progId!));
            Assert.IsAssignableFrom<ArgumentException>(ex);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository CategoryId 為空的 FormSchema 應拋 InvalidOperationException")]
        public void CreateDataFormRepository_EmptyCategoryId_ThrowsInvalidOperationException()
        {
            var schema = new FormSchema("TestProg", "測試") { CategoryId = string.Empty };
            var factory = CreateFactory(new MinimalDefineAccess(schema));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                factory.CreateDataFormRepository("TestProg"));
            Assert.Contains("CategoryId", ex.Message);
        }

        [Fact]
        [DisplayName("CreateReportFormRepository 有效 progId 應回傳非 null 的 IReportFormRepository 實例")]
        public void CreateReportFormRepository_ValidProgId_ReturnsRepository()
        {
            var factory = CreateFactory();
            var repo = factory.CreateReportFormRepository("AnyReport");
            Assert.NotNull(repo);
        }

        private sealed class MinimalDefineAccess : IDefineAccess
        {
            private readonly FormSchema _schema;
            public MinimalDefineAccess(FormSchema schema) => _schema = schema;
            public FormSchema GetFormSchema(string progId) => _schema;
            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
            public DatabaseSettings GetDatabaseSettings() => throw new NotImplementedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
            public ProgramSettings GetProgramSettings() => throw new NotImplementedException();
            public void SaveProgramSettings(ProgramSettings settings) => throw new NotImplementedException();
            public DbCategorySettings GetDbCategorySettings() => throw new NotImplementedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
            public TableSchema GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public FormLayout GetFormLayout(string layoutId) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
        }
    }
}
