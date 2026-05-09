using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 測試用的 IDefineAccess 實作，GetFormSchema 永遠回傳 null，
    /// 用於覆蓋 FormCommandBuilder 建構子的 null 防禦分支。
    /// </summary>
    internal sealed class NullFormSchemaAccess : IDefineAccess
    {
        public FormSchema GetFormSchema(string progId) => null!;
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
