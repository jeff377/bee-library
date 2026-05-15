using System.ComponentModel;
using System.Data.Common;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions;
using Bee.Repository.Factories;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="FormRepositoryFactory"/> 的純邏輯測試。
    /// 使用 stub 取代 DB 相關依賴，不需實際資料庫連線。
    /// </summary>
    public class FormRepositoryFactoryTests
    {
        #region Stubs

        private sealed class StubDefineAccess : IDefineAccess
        {
            private readonly FormSchema _schema;
            public StubDefineAccess(FormSchema schema) { _schema = schema; }
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

        private sealed class StubDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotImplementedException();
        }

        private sealed class StubDbConnectionManager : IDbConnectionManager
        {
            public DbConnectionInfo GetConnectionInfo(string databaseId) => throw new NotImplementedException();
            public DbConnection CreateConnection(string databaseId) => throw new NotImplementedException();
            public bool Remove(string databaseId) => false;
            public void Clear() { }
            public bool Contains(string databaseId) => false;
            public int Count => 0;
        }

        private sealed class StubRouter : IRepositoryDatabaseRouter
        {
            private readonly string _databaseId;
            public StubRouter(string databaseId) { _databaseId = databaseId; }
            public string Resolve(DbScope scope, Guid accessToken) => _databaseId;
        }

        private static FormRepositoryFactory CreateFactory(string categoryId = DbCategoryIds.Common)
        {
            var schema = new FormSchema("TestProg", "Test") { CategoryId = categoryId };
            return new FormRepositoryFactory(
                new StubDefineAccess(schema),
                new StubDbAccessFactory(),
                new StubDbConnectionManager(),
                new StubRouter("common"));
        }

        #endregion

        [Fact]
        [DisplayName("建構子傳入 null defineAccess 應拋出 ArgumentNullException")]
        public void Constructor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(
                    null!,
                    new StubDbAccessFactory(),
                    new StubDbConnectionManager(),
                    new StubRouter("common")));
        }

        [Fact]
        [DisplayName("建構子傳入 null dbAccessFactory 應拋出 ArgumentNullException")]
        public void Constructor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            var schema = new FormSchema("P", "P") { CategoryId = DbCategoryIds.Common };
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(
                    new StubDefineAccess(schema),
                    null!,
                    new StubDbConnectionManager(),
                    new StubRouter("common")));
        }

        [Fact]
        [DisplayName("建構子傳入 null connectionManager 應拋出 ArgumentNullException")]
        public void Constructor_NullConnectionManager_ThrowsArgumentNullException()
        {
            var schema = new FormSchema("P", "P") { CategoryId = DbCategoryIds.Common };
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(
                    new StubDefineAccess(schema),
                    new StubDbAccessFactory(),
                    null!,
                    new StubRouter("common")));
        }

        [Fact]
        [DisplayName("建構子傳入 null router 應拋出 ArgumentNullException")]
        public void Constructor_NullRouter_ThrowsArgumentNullException()
        {
            var schema = new FormSchema("P", "P") { CategoryId = DbCategoryIds.Common };
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(
                    new StubDefineAccess(schema),
                    new StubDbAccessFactory(),
                    new StubDbConnectionManager(),
                    null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("CreateDataFormRepository 傳入空白 progId 應拋出 ArgumentException")]
        public void CreateDataFormRepository_NullOrWhiteSpaceProgId_ThrowsArgumentException(string progId)
        {
            var factory = CreateFactory();
            Assert.Throws<ArgumentException>(() =>
                factory.CreateDataFormRepository(progId, Guid.NewGuid()));
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 當 FormSchema.CategoryId 為空應拋出 InvalidOperationException")]
        public void CreateDataFormRepository_SchemaWithEmptyCategoryId_ThrowsInvalidOperation()
        {
            var schema = new FormSchema("NoCat", "NoCat") { CategoryId = string.Empty };
            var factory = new FormRepositoryFactory(
                new StubDefineAccess(schema),
                new StubDbAccessFactory(),
                new StubDbConnectionManager(),
                new StubRouter("common"));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                factory.CreateDataFormRepository("NoCat", Guid.NewGuid()));
            Assert.Contains("CategoryId", ex.Message);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 使用 CategoryId=common 應回傳 IDataFormRepository")]
        public void CreateDataFormRepository_CommonCategoryId_ReturnsRepository()
        {
            var factory = CreateFactory(DbCategoryIds.Common);
            var repo = factory.CreateDataFormRepository("TestProg", Guid.NewGuid());
            Assert.NotNull(repo);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 使用 CategoryId=company 應回傳 IDataFormRepository")]
        public void CreateDataFormRepository_CompanyCategoryId_ReturnsRepository()
        {
            var factory = CreateFactory(DbCategoryIds.Company);
            var repo = factory.CreateDataFormRepository("TestProg", Guid.NewGuid());
            Assert.NotNull(repo);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 使用 CategoryId=log 應回傳 IDataFormRepository")]
        public void CreateDataFormRepository_LogCategoryId_ReturnsRepository()
        {
            var factory = CreateFactory(DbCategoryIds.Log);
            var repo = factory.CreateDataFormRepository("TestProg", Guid.NewGuid());
            Assert.NotNull(repo);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 使用未知 CategoryId 應拋出 InvalidOperationException")]
        public void CreateDataFormRepository_UnknownCategoryId_ThrowsInvalidOperation()
        {
            var factory = CreateFactory("unknown-category");
            var ex = Assert.Throws<InvalidOperationException>(() =>
                factory.CreateDataFormRepository("TestProg", Guid.NewGuid()));
            Assert.Contains("unknown-category", ex.Message);
        }

        [Fact]
        [DisplayName("CreateReportFormRepository 應回傳非 null 的 IReportFormRepository")]
        public void CreateReportFormRepository_ReturnsRepository()
        {
            var factory = CreateFactory();
            var repo = factory.CreateReportFormRepository("SalesReport");
            Assert.NotNull(repo);
        }
    }
}
