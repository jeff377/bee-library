using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests
{
    public class DefineAccessDatabaseSettingsProviderTests
    {
        private sealed class StubDefineAccess : IDefineAccess
        {
            private readonly DatabaseSettings _settings;

            public StubDefineAccess(DatabaseSettings settings) => _settings = settings;

            public DatabaseSettings GetDatabaseSettings() => _settings;
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
            public FormSchema GetFormSchema(string progId) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public FormLayout GetFormLayout(string layoutId) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
            public LanguageResource GetLanguage(string lang, string ns) => throw new NotImplementedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
        }

        private static DefineAccessDatabaseSettingsProvider CreateProvider(DatabaseSettings? settings = null)
        {
            return new DefineAccessDatabaseSettingsProvider(new StubDefineAccess(settings ?? new DatabaseSettings()));
        }

        [Fact]
        [DisplayName("建構子傳入 null defineAccess 應拋出 ArgumentNullException")]
        public void Constructor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DefineAccessDatabaseSettingsProvider(null!));
        }

        [Fact]
        [DisplayName("Get 應回傳底層 IDefineAccess 提供的 DatabaseSettings")]
        public void Get_ReturnsSettingsFromDefineAccess()
        {
            var expected = new DatabaseSettings();
            var provider = CreateProvider(expected);

            Assert.Same(expected, provider.Get());
        }

        [Fact]
        [DisplayName("GetItem 傳入 null 應拋出 ArgumentNullException")]
        public void GetItem_NullId_ThrowsArgumentNullException()
        {
            var provider = CreateProvider();

            Assert.Throws<ArgumentNullException>(() => provider.GetItem(null!));
        }

        [Fact]
        [DisplayName("GetItem 傳入空白字串應拋出 ArgumentNullException")]
        public void GetItem_WhitespaceId_ThrowsArgumentNullException()
        {
            var provider = CreateProvider();

            Assert.Throws<ArgumentNullException>(() => provider.GetItem("   "));
        }

        [Fact]
        [DisplayName("GetItem 找不到指定 id 應拋出 KeyNotFoundException")]
        public void GetItem_UnknownId_ThrowsKeyNotFoundException()
        {
            var provider = CreateProvider();

            Assert.Throws<KeyNotFoundException>(() => provider.GetItem("nonexistent"));
        }

        [Fact]
        [DisplayName("GetItem 存在的 id 應回傳對應 DatabaseItem")]
        public void GetItem_ExistingId_ReturnsDatabaseItem()
        {
            var settings = new DatabaseSettings();
            settings.Items!.Add(new DatabaseItem { Id = "common", DatabaseType = DatabaseType.SQLServer });
            var provider = CreateProvider(settings);

            var item = provider.GetItem("common");

            Assert.Equal("common", item.Id);
        }

        [Fact]
        [DisplayName("ValidateRequired 缺少 common 項目應拋出 InvalidOperationException")]
        public void ValidateRequired_MissingCommonItem_ThrowsInvalidOperationException()
        {
            var provider = CreateProvider();

            Assert.Throws<InvalidOperationException>(() => provider.ValidateRequired());
        }

        [Fact]
        [DisplayName("ValidateRequired 含有 common 項目應不拋出例外")]
        public void ValidateRequired_WithCommonItem_DoesNotThrow()
        {
            var settings = new DatabaseSettings();
            settings.Items!.Add(new DatabaseItem { Id = DbCategoryIds.Common, DatabaseType = DatabaseType.SQLServer });
            var provider = CreateProvider(settings);

            var exception = Record.Exception(() => provider.ValidateRequired());

            Assert.Null(exception);
        }
    }
}
