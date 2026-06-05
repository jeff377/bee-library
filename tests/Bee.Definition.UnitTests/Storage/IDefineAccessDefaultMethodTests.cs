using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Storage
{
    /// <summary>
    /// 驗證 <see cref="IDefineAccess"/> 三個預設介面實作（DIM）的委派行為：
    /// <c>GetPermissionModels</c>、<c>SavePermissionModels</c>、
    /// <c>GetFormLayout(customizeId, layoutId)</c>。
    /// 透過 <see cref="MinimalDefineAccess"/> stub（不覆寫三個 DIM）呼叫，
    /// 確認預設路徑確實執行。
    /// </summary>
    public class IDefineAccessDefaultMethodTests
    {
        private sealed class MinimalDefineAccess : IDefineAccess
        {
            public DefineType? LastSavedDefineType { get; private set; }
            public string? LastGetFormLayoutId { get; private set; }

            public object GetDefine(DefineType defineType, string[]? keys = null)
                => defineType == DefineType.PermissionModels
                    ? new PermissionModels()
                    : throw new NotImplementedException();

            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null)
                => LastSavedDefineType = defineType;

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
            public FormSchema GetFormSchema(string progId) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public FormLayout GetFormLayout(string layoutId)
            {
                LastGetFormLayoutId = layoutId;
                return new FormLayout();
            }
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
            public LanguageResource GetLanguage(string lang, string ns) => throw new NotImplementedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
        }

        [Fact]
        [DisplayName("GetPermissionModels 預設實作應委派至 GetDefine(DefineType.PermissionModels) 並回傳 PermissionModels")]
        public void GetPermissionModels_DefaultImpl_DelegatesToGetDefine()
        {
            IDefineAccess access = new MinimalDefineAccess();
            var result = access.GetPermissionModels();
            Assert.NotNull(result);
            Assert.IsType<PermissionModels>(result);
        }

        [Fact]
        [DisplayName("SavePermissionModels 預設實作應委派至 SaveDefine(DefineType.PermissionModels, ...)")]
        public void SavePermissionModels_DefaultImpl_DelegatesToSaveDefine()
        {
            var stub = new MinimalDefineAccess();
            IDefineAccess access = stub;
            access.SavePermissionModels(new PermissionModels());
            Assert.Equal(DefineType.PermissionModels, stub.LastSavedDefineType);
        }

        [Fact]
        [DisplayName("GetFormLayout(customizeId, layoutId) 預設實作應忽略 customizeId 並委派至 GetFormLayout(layoutId)")]
        public void GetFormLayout_WithCustomizeId_DefaultImpl_DelegatesToSingleParam()
        {
            var stub = new MinimalDefineAccess();
            IDefineAccess access = stub;
            var result = access.GetFormLayout("any_customize", "TestLayout");
            Assert.NotNull(result);
            Assert.Equal("TestLayout", stub.LastGetFormLayoutId);
        }
    }
}
