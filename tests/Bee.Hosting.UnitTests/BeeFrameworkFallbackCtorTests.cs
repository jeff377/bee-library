using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    // Stub with only (IDefineStorage, PathOptions) ctor — exercises ResolveDefineAccess
    // 2-arg ctor fallback path (the 4-arg ctor is intentionally absent).
    public sealed class TwoArgDefineAccessStub : IDefineAccess
    {
        public TwoArgDefineAccessStub(IDefineStorage storage, PathOptions paths) { }
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
        public FormLayout GetFormLayout(string layoutId) => throw new NotImplementedException();
        public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
        public LanguageResource GetLanguage(string lang, string ns) => throw new NotImplementedException();
        public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
    }

    // Stub with only a parameterless ctor — exercises CreateDefineStorage
    // parameterless ctor fallback path (the PathOptions ctor is intentionally absent).
    public sealed class ParameterlessDefineStorageStub : IDefineStorage
    {
        public DbCategorySettings? GetDbCategorySettings() => null;
        public void SaveDbCategorySettings(DbCategorySettings settings) { }
        public CurrencySettings? GetCurrencySettings() => null;
        public void SaveCurrencySettings(CurrencySettings settings) { }
        public UnitSettings? GetUnitSettings() => null;
        public void SaveUnitSettings(UnitSettings settings) { }
        public ProgramSettings? GetProgramSettings() => null;
        public void SaveProgramSettings(ProgramSettings settings) { }
        public TableSchema? GetTableSchema(string categoryId, string tableName) => null;
        public void SaveTableSchema(string categoryId, TableSchema tableSchema) { }
        public FormSchema? GetFormSchema(string progId) => null;
        public void SaveFormSchema(FormSchema formSchema) { }
        public FormLayout? GetFormLayout(string layoutId) => null;
        public void SaveFormLayout(FormLayout formLayout) { }
        public LanguageResource? GetLanguage(string lang, string ns) => null;
        public void SaveLanguage(LanguageResource resource) { }
    }

    public class BeeFrameworkFallbackCtorTests
    {
        [Fact]
        [DisplayName("ResolveDefineAccess 應支援僅有 (IDefineStorage, PathOptions) 建構子的 IDefineAccess 實作")]
        public void ResolveDefineAccess_TwoArgCtor_CreatesCorrectType()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-2arg-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var configuration = new BackendConfiguration();
                configuration.Components.DefineAccess =
                    "Bee.Hosting.UnitTests.TwoArgDefineAccessStub, Bee.Hosting.UnitTests";

                var services = new ServiceCollection();
                services.AddBeeFramework(
                    configuration,
                    new PathOptions { DefinePath = tempDir },
                    autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var access = sp.GetRequiredService<IDefineAccess>();

                Assert.IsType<TwoArgDefineAccessStub>(access);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("CreateDefineStorage 應支援僅有無參數建構子的 IDefineStorage 實作")]
        public void CreateDefineStorage_ParameterlessCtorFallback_CreatesCorrectType()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-pless-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var configuration = new BackendConfiguration();
                configuration.Components.DefineStorage =
                    "Bee.Hosting.UnitTests.ParameterlessDefineStorageStub, Bee.Hosting.UnitTests";

                var services = new ServiceCollection();
                services.AddBeeFramework(
                    configuration,
                    new PathOptions { DefinePath = tempDir },
                    autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var storage = sp.GetRequiredService<IDefineStorage>();

                Assert.IsType<ParameterlessDefineStorageStub>(storage);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }
    }
}
