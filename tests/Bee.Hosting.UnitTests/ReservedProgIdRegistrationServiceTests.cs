using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Business;
using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Hosting.Registry;
using Bee.ObjectCaching;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 保留字 progId 的啟動自我註冊：全新 DefinePath、既有 ProgramSettings.xml 缺 System 項目、
    /// 客製層覆寫 System BO 三條路徑，以及唯讀部署下寫檔失敗不影響本次執行。
    /// </summary>
    public sealed class ReservedProgIdRegistrationServiceTests : IDisposable
    {
        private readonly string _defineDir;
        private readonly PathOptions _paths;

        public ReservedProgIdRegistrationServiceTests()
        {
            _defineDir = Path.Combine(Path.GetTempPath(), $"bee-reserved-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_defineDir);
            _paths = new PathOptions { DefinePath = _defineDir };
        }

        public void Dispose()
        {
            try { Directory.Delete(_defineDir, recursive: true); } catch (IOException) { /* best effort */ }
        }

        private CacheDefineAccess CreateAccess()
        {
            var storage = new FileDefineStorage(_paths);
            var cache = new CacheContainerService(storage, _paths, "reserved_" + Guid.NewGuid().ToString("N"));
            return new CacheDefineAccess(storage, _paths, cache, Array.Empty<byte>());
        }

        private static ReservedProgIdRegistrationService CreateService(IDefineAccess access)
            => new(access, new ProgramSettingsBoTypeResolver(access),
                   NullLogger<ReservedProgIdRegistrationService>.Instance);

        private ProgramSettings ReadRegistryFromDisk()
            => XmlCodec.DeserializeFromFile<ProgramSettings>(_paths.GetProgramSettingsFilePath())!;

        [Fact]
        [DisplayName("全新 DefinePath 啟動應寫出含全部保留字的 ProgramSettings.xml")]
        public async Task StartAsync_EmptyDefinePath_WritesAllReservedEntries()
        {
            var access = CreateAccess();

            await CreateService(access).StartAsync(CancellationToken.None);

            Assert.True(File.Exists(_paths.GetProgramSettingsFilePath()));
            var registry = ReadRegistryFromDisk();
            foreach (var binding in ReservedProgIds.All)
            {
                Assert.True(registry.Items!.Contains(binding.ProgId));
                Assert.Equal(binding.DefaultTypeName, registry.Items![binding.ProgId].BusinessObject);
            }
        }

        [Fact]
        [DisplayName("既有 ProgramSettings.xml 缺 System 項目時應逐筆補寫並保留原有項目")]
        public async Task StartAsync_ExistingRegistryMissingSystem_AddsItAndKeepsOthers()
        {
            // Every host shipping today is in exactly this state: a ProgramSettings.xml with
            // application progIds and no System entry. A file-exists check would skip it.
            var existing = new ProgramSettings();
            existing.Items!.Add("Order", "訂單").BusinessObject = "MyErp.OrderBO, MyErp";
            new FileDefineStorage(_paths).SaveProgramSettings(existing);

            await CreateService(CreateAccess()).StartAsync(CancellationToken.None);

            var registry = ReadRegistryFromDisk();
            Assert.True(registry.Items!.Contains(SysProgIds.System));
            Assert.True(registry.Items!.Contains(SysProgIds.AuditLog));
            Assert.Equal("MyErp.OrderBO, MyErp", registry.Items!["Order"].BusinessObject);
        }

        [Fact]
        [DisplayName("已宣告的保留字不應被覆寫——客製的 System BO 要留著")]
        public async Task StartAsync_ReservedProgIdAlreadyDeclared_IsNotOverwritten()
        {
            var custom = $"{typeof(CustomSystemBo).FullName}, {typeof(CustomSystemBo).Assembly.GetName().Name}";
            var existing = new ProgramSettings();
            existing.Items!.Add(SysProgIds.System, "System").BusinessObject = custom;
            new FileDefineStorage(_paths).SaveProgramSettings(existing);

            var access = CreateAccess();
            await CreateService(access).StartAsync(CancellationToken.None);

            Assert.Equal(custom, ReadRegistryFromDisk().Items![SysProgIds.System].BusinessObject);
            Assert.Equal(typeof(CustomSystemBo),
                new ProgramSettingsBoTypeResolver(access).Resolve(SysProgIds.System));
        }

        [Fact]
        [DisplayName("補寫後應使 cache 失效，同一 IDefineAccess 立刻讀得到新項目")]
        public async Task StartAsync_InvalidatesCache_SoResolutionSeesNewEntries()
        {
            var access = CreateAccess();
            // Warm the cache first: without invalidation this instance would keep serving the
            // pre-registration snapshot.
            new FileDefineStorage(_paths).SaveProgramSettings(new ProgramSettings());
            _ = access.GetProgramSettings();

            await CreateService(access).StartAsync(CancellationToken.None);

            Assert.True(access.GetProgramSettings().Items!.Contains(SysProgIds.System));
        }

        [Fact]
        [DisplayName("寫檔失敗（唯讀部署）只記警告，解析仍走框架預設，不阻止啟動")]
        public async Task StartAsync_PersistFails_StillStartsAndResolves()
        {
            var access = new ReadOnlyDefineAccess(CreateAccess());

            var exception = await Record.ExceptionAsync(
                () => CreateService(access).StartAsync(CancellationToken.None));

            Assert.Null(exception);
            Assert.Equal(typeof(SystemBusinessObject),
                new ProgramSettingsBoTypeResolver(access).Resolve(SysProgIds.System));
        }

        [Fact]
        [DisplayName("保留字解析到不符預期基底時應拒絕啟動")]
        public async Task StartAsync_ReservedProgIdResolvesToWrongBase_Throws()
        {
            var access = CreateAccess();
            var service = new ReservedProgIdRegistrationService(
                access, new WrongTypeResolver(), NullLogger<ReservedProgIdRegistrationService>.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.StartAsync(CancellationToken.None));

            Assert.Contains(SysProgIds.System, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("重複啟動應為冪等，不重複新增項目")]
        public async Task StartAsync_RunTwice_IsIdempotent()
        {
            await CreateService(CreateAccess()).StartAsync(CancellationToken.None);
            int afterFirst = ReadRegistryFromDisk().Items!.Count;

            await CreateService(CreateAccess()).StartAsync(CancellationToken.None);

            Assert.Equal(afterFirst, ReadRegistryFromDisk().Items!.Count);
        }

        // ---- Test doubles ----

        public sealed class CustomSystemBo : SystemBusinessObject
        {
            public CustomSystemBo(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
                : base(ctx, accessToken, progId, isLocalCall) { }
        }

        /// <summary>Stands in for a resolver that ignores the reserved progIds.</summary>
        private sealed class WrongTypeResolver : IBoTypeResolver
        {
            public Type Resolve(string progId) => typeof(FormBusinessObject);
        }

        /// <summary>Stands in for a read-only deployment: reads work, the write does not.</summary>
        private sealed class ReadOnlyDefineAccess : IDefineAccess
        {
            private readonly IDefineAccess _inner;
            public ReadOnlyDefineAccess(IDefineAccess inner) { _inner = inner; }

            public void SaveProgramSettings(ProgramSettings settings)
                => throw new UnauthorizedAccessException("read-only deployment");

            public ProgramSettings GetProgramSettings() => _inner.GetProgramSettings();
            public object GetDefine(DefineType defineType, string[]? keys = null) => _inner.GetDefine(defineType, keys);
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => _inner.SaveDefine(defineType, defineObject, keys);
            public Definition.Settings.SystemSettings GetSystemSettings() => _inner.GetSystemSettings();
            public void SaveSystemSettings(Definition.Settings.SystemSettings settings) => _inner.SaveSystemSettings(settings);
            public DatabaseSettings GetDatabaseSettings() => _inner.GetDatabaseSettings();
            public void SaveDatabaseSettings(DatabaseSettings settings) => _inner.SaveDatabaseSettings(settings);
            public DbCategorySettings GetDbCategorySettings() => _inner.GetDbCategorySettings();
            public void SaveDbCategorySettings(DbCategorySettings settings) => _inner.SaveDbCategorySettings(settings);
            public Definition.Database.TableSchema GetTableSchema(string categoryId, string tableName) => _inner.GetTableSchema(categoryId, tableName);
            public void SaveTableSchema(string categoryId, Definition.Database.TableSchema tableSchema) => _inner.SaveTableSchema(categoryId, tableSchema);
            public Definition.Forms.FormSchema GetFormSchema(string progId) => _inner.GetFormSchema(progId);
            public void SaveFormSchema(Definition.Forms.FormSchema formSchema) => _inner.SaveFormSchema(formSchema);
            public Definition.Layouts.FormLayout GetFormLayout(string layoutId) => _inner.GetFormLayout(layoutId);
            public void SaveFormLayout(Definition.Layouts.FormLayout formLayout) => _inner.SaveFormLayout(formLayout);
            public Definition.Language.LanguageResource GetLanguage(string lang, string ns) => _inner.GetLanguage(lang, ns);
            public void SaveLanguage(Definition.Language.LanguageResource resource) => _inner.SaveLanguage(resource);
        }
    }
}
