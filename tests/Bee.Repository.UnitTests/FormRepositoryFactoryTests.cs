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
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Factories;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="FormRepositoryFactory"/> 建構子保護、工廠方法與 CategoryId 路由的純邏輯測試。
    /// 所有相依項目皆以 Stub 實作，不需資料庫連線。
    /// </summary>
    public class FormRepositoryFactoryTests
    {
        #region Stubs

        private sealed class StubDefineAccess : IDefineAccess
        {
            private readonly FormSchema _schema;
            public StubDefineAccess(FormSchema schema) => _schema = schema;

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

        private sealed class StubRepositoryDatabaseRouter : IRepositoryDatabaseRouter
        {
            private readonly string _databaseId;
            public StubRepositoryDatabaseRouter(string databaseId) => _databaseId = databaseId;
            public string Resolve(DbScope scope, Guid accessToken) => _databaseId;
        }

        private static FormRepositoryFactory CreateFactory(
            FormSchema? schema = null,
            string routedDatabaseId = "common")
        {
            return new FormRepositoryFactory(
                new StubDefineAccess(schema ?? new FormSchema("TestProg", "Test")),
                new StubDbAccessFactory(),
                new StubDbConnectionManager(),
                new StubRepositoryDatabaseRouter(routedDatabaseId));
        }

        #endregion

        [Fact]
        [DisplayName("建構子傳入 null defineAccess 應拋 ArgumentNullException")]
        public void Constructor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormRepositoryFactory(
                null!,
                new StubDbAccessFactory(),
                new StubDbConnectionManager(),
                new StubRepositoryDatabaseRouter("common")));
        }

        [Fact]
        [DisplayName("建構子傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void Constructor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormRepositoryFactory(
                new StubDefineAccess(new FormSchema()),
                null!,
                new StubDbConnectionManager(),
                new StubRepositoryDatabaseRouter("common")));
        }

        [Fact]
        [DisplayName("建構子傳入 null connectionManager 應拋 ArgumentNullException")]
        public void Constructor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormRepositoryFactory(
                new StubDefineAccess(new FormSchema()),
                new StubDbAccessFactory(),
                null!,
                new StubRepositoryDatabaseRouter("common")));
        }

        [Fact]
        [DisplayName("建構子傳入 null router 應拋 ArgumentNullException")]
        public void Constructor_NullRouter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormRepositoryFactory(
                new StubDefineAccess(new FormSchema()),
                new StubDbAccessFactory(),
                new StubDbConnectionManager(),
                null!));
        }

        [Fact]
        [DisplayName("CreateReportFormRepository 應回傳非 null 的 IReportFormRepository 實作")]
        public void CreateReportFormRepository_ValidProgId_ReturnsNonNull()
        {
            var factory = CreateFactory();
            var repo = factory.CreateReportFormRepository("SalesReport");
            Assert.NotNull(repo);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("CreateDataFormRepository 空白 progId 應拋 ArgumentException")]
        public void CreateDataFormRepository_EmptyProgId_ThrowsArgumentException(string progId)
        {
            var factory = CreateFactory();
            Assert.Throws<ArgumentException>(() =>
                factory.CreateDataFormRepository(progId, Guid.Empty));
        }

        [Fact]
        [DisplayName("CreateDataFormRepository FormSchema 無 CategoryId 應拋 InvalidOperationException")]
        public void CreateDataFormRepository_EmptyCategoryId_ThrowsInvalidOperation()
        {
            // FormSchema 預設 CategoryId = string.Empty
            var factory = CreateFactory(schema: new FormSchema("TestProg", "Test"));
            var ex = Assert.Throws<InvalidOperationException>(() =>
                factory.CreateDataFormRepository("TestProg", Guid.Empty));
            Assert.Contains("CategoryId", ex.Message);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 未知 CategoryId 應拋 InvalidOperationException（ParseCategoryId default 分支）")]
        public void CreateDataFormRepository_UnknownCategoryId_ThrowsInvalidOperation()
        {
            var schema = new FormSchema("TestProg", "Test") { CategoryId = "unknown" };
            var factory = CreateFactory(schema: schema);
            Assert.Throws<InvalidOperationException>(() =>
                factory.CreateDataFormRepository("TestProg", Guid.Empty));
        }

        [Theory]
        [InlineData(DbCategoryIds.Common)]
        [InlineData(DbCategoryIds.Company)]
        [InlineData(DbCategoryIds.Log)]
        [DisplayName("CreateDataFormRepository 有效 CategoryId 應回傳 IDataFormRepository 實作")]
        public void CreateDataFormRepository_ValidCategoryId_ReturnsRepository(string categoryId)
        {
            var schema = new FormSchema("TestProg", "Test") { CategoryId = categoryId };
            var factory = CreateFactory(schema: schema);
            var repo = factory.CreateDataFormRepository("TestProg", Guid.NewGuid());
            Assert.NotNull(repo);
            Assert.IsAssignableFrom<IDataFormRepository>(repo);
        }
    }
}
