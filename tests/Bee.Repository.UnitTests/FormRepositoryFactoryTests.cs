using System.ComponentModel;
using System.Data.Common;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions;
using Bee.Repository.Factories;
using Bee.Repository.Form;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="FormRepositoryFactory"/> 建構子驗證與工廠方法測試。
    /// </summary>
    public class FormRepositoryFactoryTests
    {
        #region Stubs

        private sealed class StubDefineAccess : IDefineAccess
        {
            public string CategoryId { get; set; } = DbCategoryIds.Common;

            public DatabaseSettings GetDatabaseSettings() => new();
            public FormSchema GetFormSchema(string progId) => new() { CategoryId = CategoryId };
            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
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
            public LanguageResource GetLanguage(string lang, string ns) => throw new NotImplementedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
        }

        private sealed class StubDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotImplementedException();
        }

        private sealed class StubConnectionManager : IDbConnectionManager
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
            public string Resolve(DbScope scope, Guid accessToken) => DbCategoryIds.Common;
        }

        private static FormRepositoryFactory CreateFactory(
            StubDefineAccess? defineAccess = null,
            IDbAccessFactory? dbAccessFactory = null,
            IDbConnectionManager? connectionManager = null,
            IRepositoryDatabaseRouter? router = null)
            => new(
                defineAccess ?? new StubDefineAccess(),
                dbAccessFactory ?? new StubDbAccessFactory(),
                connectionManager ?? new StubConnectionManager(),
                router ?? new StubRouter());

        #endregion

        [Fact]
        [DisplayName("建構子傳入 null defineAccess 應拋 ArgumentNullException")]
        public void Constructor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(null!, new StubDbAccessFactory(), new StubConnectionManager(), new StubRouter()));
        }

        [Fact]
        [DisplayName("建構子傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void Constructor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(new StubDefineAccess(), null!, new StubConnectionManager(), new StubRouter()));
        }

        [Fact]
        [DisplayName("建構子傳入 null connectionManager 應拋 ArgumentNullException")]
        public void Constructor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(new StubDefineAccess(), new StubDbAccessFactory(), null!, new StubRouter()));
        }

        [Fact]
        [DisplayName("建構子傳入 null router 應拋 ArgumentNullException")]
        public void Constructor_NullRouter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FormRepositoryFactory(new StubDefineAccess(), new StubDbAccessFactory(), new StubConnectionManager(), null!));
        }

        [Theory]
        [InlineData("SalesReport")]
        [InlineData("EmployeeList")]
        [DisplayName("CreateReportFormRepository 應回傳 ReportFormRepository 實例")]
        public void CreateReportFormRepository_ValidProgId_ReturnsReportFormRepository(string progId)
        {
            var factory = CreateFactory();
            var repo = factory.CreateReportFormRepository(progId);
            Assert.IsType<ReportFormRepository>(repo);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 傳入空白 progId 應拋 ArgumentException")]
        public void CreateDataFormRepository_WhitespaceProgId_ThrowsArgumentException()
        {
            var factory = CreateFactory();
            Assert.Throws<ArgumentException>(() => factory.CreateDataFormRepository("   ", Guid.NewGuid()));
        }

        [Fact]
        [DisplayName("CreateDataFormRepository Schema 無 CategoryId 應拋 InvalidOperationException 且訊息含 CategoryId")]
        public void CreateDataFormRepository_EmptyCategoryId_ThrowsInvalidOperationException()
        {
            var stub = new StubDefineAccess { CategoryId = string.Empty };
            var factory = CreateFactory(defineAccess: stub);
            var ex = Assert.Throws<InvalidOperationException>(
                () => factory.CreateDataFormRepository("Employee", Guid.NewGuid()));
            Assert.Contains("CategoryId", ex.Message);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 未知 CategoryId 應拋 InvalidOperationException 且訊息含未知值")]
        public void CreateDataFormRepository_UnknownCategoryId_ThrowsInvalidOperationException()
        {
            var stub = new StubDefineAccess { CategoryId = "unknown_db" };
            var factory = CreateFactory(defineAccess: stub);
            var ex = Assert.Throws<InvalidOperationException>(
                () => factory.CreateDataFormRepository("Employee", Guid.NewGuid()));
            Assert.Contains("unknown_db", ex.Message);
        }

        [Theory]
        [InlineData(DbCategoryIds.Common)]
        [InlineData(DbCategoryIds.Company)]
        [InlineData(DbCategoryIds.Log)]
        [DisplayName("CreateDataFormRepository 有效 CategoryId 應回傳 DataFormRepository")]
        public void CreateDataFormRepository_ValidCategoryId_ReturnsDataFormRepository(string categoryId)
        {
            var stub = new StubDefineAccess { CategoryId = categoryId };
            var factory = CreateFactory(defineAccess: stub);
            var repo = factory.CreateDataFormRepository("Employee", Guid.NewGuid());
            Assert.IsType<DataFormRepository>(repo);
        }
    }
}
