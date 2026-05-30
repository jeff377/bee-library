using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Storage
{
    /// <summary>
    /// 驗證 <see cref="IDefineAccess.GetFormLayout(string, string)"/> 預設介面實作
    /// 確實委派至 <see cref="IDefineAccess.GetFormLayout(string)"/>，而非空實作。
    /// <c>MinimalDefineAccess</c> 不 override customizeId 多載，強制走 default interface method。
    /// </summary>
    public class IDefineAccessDefaultMethodTests
    {
        private sealed class MinimalDefineAccess : IDefineAccess
        {
            private readonly FormLayout _layout;

            public MinimalDefineAccess(string layoutId)
            {
                _layout = new FormLayout { LayoutId = layoutId };
            }

            public FormLayout GetFormLayout(string layoutId) => _layout;

            // 不 override GetFormLayout(string customizeId, string layoutId) → 走 default interface method

            // 其他成員均不需呼叫，拋 NotImplementedException
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
            public FormSchema GetFormSchema(string progId) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
            public LanguageResource GetLanguage(string lang, string ns) => throw new NotImplementedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
        }

        [Fact]
        [DisplayName("GetFormLayout 含 customizeId 預設介面實作應委派至不含 customizeId 的單參數多載，忽略 customizeId")]
        public void GetFormLayout_WithCustomizeId_DelegatesToBaseOverload()
        {
            IDefineAccess access = new MinimalDefineAccess("EmployeeDefault");

            FormLayout result = access.GetFormLayout("acme", "EmployeeDefault");

            Assert.NotNull(result);
            Assert.Equal("EmployeeDefault", result.LayoutId);
        }

        [Fact]
        [DisplayName("GetFormLayout 含 customizeId 空字串時應同樣委派至基礎多載，回傳相同結果")]
        public void GetFormLayout_WithEmptyCustomizeId_DelegatesToBaseOverload()
        {
            IDefineAccess access = new MinimalDefineAccess("SalesOrder");

            FormLayout result = access.GetFormLayout(string.Empty, "SalesOrder");

            Assert.NotNull(result);
            Assert.Equal("SalesOrder", result.LayoutId);
        }
    }
}
