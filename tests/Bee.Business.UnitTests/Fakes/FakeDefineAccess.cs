using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Business.UnitTests.Fakes
{
    /// <summary>
    /// 測試用 <see cref="IDefineAccess"/>；除 <see cref="GetDatabaseSettings"/> 會回傳預設的
    /// <see cref="DatabaseSettings"/> 實例外,其他方法皆拋出 <see cref="NotImplementedException"/>。
    /// </summary>
    internal sealed class FakeDefineAccess : IDefineAccess
    {
        public DatabaseSettings Settings { get; } = new DatabaseSettings();

        /// <summary>Optional permission-model registry returned by <see cref="GetPermissionModels"/>.</summary>
        public PermissionModels? PermissionModels { get; set; }

        public DatabaseSettings GetDatabaseSettings() => Settings;

        public object GetDefine(DefineType defineType, string[]? keys = null) =>
            defineType == DefineType.PermissionModels && PermissionModels != null
                ? PermissionModels
                : throw new NotImplementedException();
        public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
        public SystemSettings GetSystemSettings() => throw new NotImplementedException();
        public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
        public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
        public ProgramSettings GetProgramSettings() => throw new NotImplementedException();
        public void SaveProgramSettings(ProgramSettings settings) => throw new NotImplementedException();
        public DbCategorySettings GetDbCategorySettings() => throw new NotImplementedException();
        public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
        public CurrencySettings GetCurrencySettings() => throw new NotImplementedException();
        public void SaveCurrencySettings(CurrencySettings settings) => throw new NotImplementedException();
        public TableSchema GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
        public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
        public FormSchema GetFormSchema(string progId) => throw new NotImplementedException();
        public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
        public FormLayout GetFormLayout(string layoutId) => throw new NotImplementedException();
        public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
        public LanguageResource GetLanguage(string lang, string ns) => throw new NotImplementedException();
        public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
    }
}
