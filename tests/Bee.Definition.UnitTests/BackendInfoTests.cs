using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// <see cref="BackendInfo"/> 的剩餘成員測試。Phase 4 之後 <c>BackendInfo</c> 僅保留
    /// 4 個加密金鑰 + <c>LogOptions</c> + <c>LogWriter</c>；Phase 5/6 統一移至 <c>IOptions&lt;T&gt;</c>。
    /// 同時保留 <see cref="DefineAccessDatabaseSettingsProvider"/> 的單元測試，避免額外建獨立檔案。
    /// </summary>
    public class BackendInfoTests
    {
        [Fact]
        [DisplayName("DefineAccessDatabaseSettingsProvider.GetItem 於 databaseId 為空字串時應拋 ArgumentNullException")]
        public void Provider_GetItem_EmptyId_ThrowsArgumentNullException()
        {
            var provider = new DefineAccessDatabaseSettingsProvider(new FakeDefineAccess());
            Assert.Throws<ArgumentNullException>(() => provider.GetItem(string.Empty));
        }

        [Fact]
        [DisplayName("DefineAccessDatabaseSettingsProvider.GetItem 於找不到對應項目時應拋 KeyNotFoundException")]
        public void Provider_GetItem_NotFound_ThrowsKeyNotFoundException()
        {
            var provider = new DefineAccessDatabaseSettingsProvider(new FakeDefineAccess());
            Assert.Throws<KeyNotFoundException>(() => provider.GetItem("missing"));
        }

        [Fact]
        [DisplayName("DefineAccessDatabaseSettingsProvider.GetItem 於存在對應項目時應回傳該 DatabaseItem")]
        public void Provider_GetItem_Found_ReturnsItem()
        {
            var fake = new FakeDefineAccess();
            fake.Settings.Items!.Add(new DatabaseItem { Id = "common", DisplayName = "共用" });
            var provider = new DefineAccessDatabaseSettingsProvider(fake);

            var item = provider.GetItem("common");

            Assert.NotNull(item);
            Assert.Equal("common", item.Id);
            Assert.Equal("共用", item.DisplayName);
        }

        /// <summary>
        /// 測試用 <see cref="IDefineAccess"/>;除 <see cref="GetDatabaseSettings"/> 外其他方法皆拋 <see cref="NotImplementedException"/>。
        /// </summary>
        private sealed class FakeDefineAccess : IDefineAccess
        {
            public DatabaseSettings Settings { get; } = new DatabaseSettings();

            public DatabaseSettings GetDatabaseSettings() => Settings;

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
        }
    }
}
