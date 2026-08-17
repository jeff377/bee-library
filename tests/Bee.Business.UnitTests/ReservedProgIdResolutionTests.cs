using System.ComponentModel;
using Bee.Business.AuditLog;
using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 保留字 progId（System / AuditLog）的解析防護：缺項回框架預設、型別載不到或基底不符 fail fast。
    /// 一般 progId 的失敗策略相同，差別只在預期基底較寬（BusinessObject 而非該軸的框架物件）。
    /// </summary>
    public class ReservedProgIdResolutionTests
    {
        private static string Fqn(Type t) => $"{t.FullName}, {t.Assembly.GetName().Name}";

        private static ProgramSettings Registry(params (string progId, string businessObject)[] items)
        {
            var settings = new ProgramSettings();
            foreach (var (progId, bo) in items)
                settings.Items!.Add(progId, progId).BusinessObject = bo;
            return settings;
        }

        // ---- 缺項：自我註冊結果參與解析 ----

        [Theory]
        [InlineData(SysProgIds.System, typeof(SystemBusinessObject))]
        [InlineData(SysProgIds.AuditLog, typeof(LogBusinessObject))]
        [DisplayName("註冊表未宣告保留字時應解析為框架預設 BO（唯讀部署下自我註冊寫不進檔也能啟動）")]
        public void Resolve_ReservedProgIdAbsent_ReturnsFrameworkDefault(string progId, Type expected)
        {
            var resolver = new ProgramSettingsBoTypeResolver(new StubDefineAccess(new ProgramSettings()));

            Assert.Equal(expected, resolver.Resolve(progId));
        }

        [Fact]
        [DisplayName("ProgramSettings.xml 不存在時保留字仍應解析為框架預設")]
        public void Resolve_ReservedProgIdWithNoRegistryFile_ReturnsFrameworkDefault()
        {
            var resolver = new ProgramSettingsBoTypeResolver(new ThrowingDefineAccess());

            Assert.Equal(typeof(SystemBusinessObject), resolver.Resolve(SysProgIds.System));
        }

        [Fact]
        [DisplayName("保留字宣告但 BusinessObject 留空時應解析為框架預設")]
        public void Resolve_ReservedProgIdWithEmptyBusinessObject_ReturnsFrameworkDefault()
        {
            var resolver = new ProgramSettingsBoTypeResolver(
                new StubDefineAccess(Registry((SysProgIds.System, string.Empty))));

            Assert.Equal(typeof(SystemBusinessObject), resolver.Resolve(SysProgIds.System));
        }

        // ---- 已宣告：客製成功 ----

        [Fact]
        [DisplayName("保留字綁定 SystemBusinessObject 子類應解析為該子類")]
        public void Resolve_ReservedProgIdBoundToSubclass_ReturnsSubclass()
        {
            var resolver = new ProgramSettingsBoTypeResolver(
                new StubDefineAccess(Registry((SysProgIds.System, Fqn(typeof(CustomSystemBo))))));

            Assert.Equal(typeof(CustomSystemBo), resolver.Resolve(SysProgIds.System));
        }

        // ---- 已宣告但壞掉：fail fast ----

        [Fact]
        [DisplayName("保留字綁定的型別載不到時應拋出並指名 progId 與型別名，不得靜默退回")]
        public void Resolve_ReservedProgIdWithUnloadableType_Throws()
        {
            var resolver = new ProgramSettingsBoTypeResolver(
                new StubDefineAccess(Registry((SysProgIds.System, "Bee.Business.NoSuchTypeXyz, Bee.Business"))));

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(SysProgIds.System));

            Assert.Contains(SysProgIds.System, ex.Message, StringComparison.Ordinal);
            Assert.Contains("NoSuchTypeXyz", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("組件找不到時保留字同樣應拋出")]
        public void Resolve_ReservedProgIdWithMissingAssembly_Throws()
        {
            var resolver = new ProgramSettingsBoTypeResolver(
                new StubDefineAccess(Registry((SysProgIds.System, "Some.Type, NoSuchAssemblyXyz"))));

            Assert.Throws<InvalidOperationException>(() => resolver.Resolve(SysProgIds.System));
        }

        [Fact]
        [DisplayName("System 綁到 FormBusinessObject 子類應拋出——放寬為 BusinessObject 後正是這個缺口")]
        public void Resolve_SystemBoundToFormBusinessObject_Throws()
        {
            var resolver = new ProgramSettingsBoTypeResolver(
                new StubDefineAccess(Registry((SysProgIds.System, Fqn(typeof(FormBusinessObject))))));

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(SysProgIds.System));

            Assert.Contains(nameof(SystemBusinessObject), ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("AuditLog 綁到 SystemBusinessObject 應拋出（per-progId 預期基底各自獨立）")]
        public void Resolve_AuditLogBoundToSystemBo_Throws()
        {
            var resolver = new ProgramSettingsBoTypeResolver(
                new StubDefineAccess(Registry((SysProgIds.AuditLog, Fqn(typeof(SystemBusinessObject))))));

            Assert.Throws<InvalidOperationException>(() => resolver.Resolve(SysProgIds.AuditLog));
        }

        // ---- 一般 progId：與保留字同樣 fail fast，只有預期基底較寬 ----

        [Fact]
        [DisplayName("一般 progId 型別載不到時同樣應拋出——兩軸不再有相反的失敗策略")]
        public void Resolve_OrdinaryProgIdWithUnloadableType_Throws()
        {
            var resolver = new ProgramSettingsBoTypeResolver(
                new StubDefineAccess(Registry(("Order", "Bee.Business.NoSuchTypeXyz, Bee.Business"))));

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("Order"));

            Assert.Contains("Order", ex.Message, StringComparison.Ordinal);
            Assert.Contains("NoSuchTypeXyz", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("一般 progId 未宣告 BusinessObject 時仍應回傳 FormBusinessObject（「沒宣告」不是失敗）")]
        public void Resolve_OrdinaryProgIdWithEmptyBusinessObject_ReturnsFormBusinessObject()
        {
            var resolver = new ProgramSettingsBoTypeResolver(
                new StubDefineAccess(Registry(("Order", string.Empty))));

            Assert.Equal(typeof(FormBusinessObject), resolver.Resolve("Order"));
        }

        [Fact]
        [DisplayName("解析目標放寬為 BusinessObject：一般 progId 綁 LogBusinessObject 子類應被接受")]
        public void Resolve_OrdinaryProgIdBoundToNonFormBusinessObject_IsAccepted()
        {
            var resolver = new ProgramSettingsBoTypeResolver(
                new StubDefineAccess(Registry(("Report", Fqn(typeof(CustomLogBo))))));

            Assert.Equal(typeof(CustomLogBo), resolver.Resolve("Report"));
        }

        // ---- DefaultBoTypeResolver ----

        [Fact]
        [DisplayName("DefaultBoTypeResolver 亦應回傳保留字的框架預設，而非一律 FormBusinessObject")]
        public void DefaultResolver_HonoursReservedProgIds()
        {
            var resolver = new DefaultBoTypeResolver();

            Assert.Equal(typeof(SystemBusinessObject), resolver.Resolve(SysProgIds.System));
            Assert.Equal(typeof(LogBusinessObject), resolver.Resolve(SysProgIds.AuditLog));
            Assert.Equal(typeof(FormBusinessObject), resolver.Resolve("Order"));
        }

        // ---- ReservedProgIds ----

        [Fact]
        [DisplayName("ReservedProgIds.Find 應大小寫無關，未列名者回 null")]
        public void ReservedProgIds_Find_IsCaseInsensitive()
        {
            Assert.NotNull(ReservedProgIds.Find("system"));
            Assert.NotNull(ReservedProgIds.Find("AUDITLOG"));
            Assert.Null(ReservedProgIds.Find("Order"));
        }

        [Fact]
        [DisplayName("ReservedProgIdBinding.DefaultTypeName 應為可載回的組件限定名")]
        public void ReservedProgIds_DefaultTypeName_RoundTripsThroughAssemblyLoader()
        {
            foreach (var binding in ReservedProgIds.All)
            {
                var loaded = Bee.Base.AssemblyLoader.GetType(binding.DefaultTypeName);
                Assert.Equal(binding.DefaultType, loaded);
            }
        }

        // ---- Test doubles ----

        public sealed class CustomSystemBo : SystemBusinessObject
        {
            public CustomSystemBo(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
                : base(ctx, accessToken, progId, isLocalCall) { }
        }

        public sealed class CustomLogBo : LogBusinessObject
        {
            public CustomLogBo(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
                : base(ctx, accessToken, progId, isLocalCall) { }
        }

        private class StubDefineAccess : IDefineAccess
        {
            private readonly ProgramSettings _settings;
            public StubDefineAccess(ProgramSettings settings) { _settings = settings; }
            public virtual ProgramSettings GetProgramSettings() => _settings;

            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
            public DatabaseSettings GetDatabaseSettings() => throw new NotImplementedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
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

        private sealed class ThrowingDefineAccess : StubDefineAccess
        {
            public ThrowingDefineAccess() : base(new ProgramSettings()) { }
            public override ProgramSettings GetProgramSettings() => throw new FileNotFoundException();
        }
    }
}
