using System.ComponentModel;
using Bee.Base.Exceptions;
using Bee.Base.Serialization;
using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject"/> 的 plugin 維護 API：LocalOnly 防禦、儲存時驗證、
    /// 空字串等同清空、寫入委派給 <see cref="ICustomizeDefineWriter"/>。
    /// </summary>
    public class SystemBusinessObjectPluginTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectPluginTests(SharedDbFixture fx)
        {
            _fx = fx;
        }

        private static string SamplePluginFqn =>
            $"{typeof(SamplePlugin).FullName}, {typeof(SamplePlugin).Assembly.GetName().Name}";

        private static string NoStagePluginFqn =>
            $"{typeof(NoStagePlugin).FullName}, {typeof(NoStagePlugin).Assembly.GetName().Name}";

        private SystemBusinessObject CreateBo(SpyWriter writer, SpyReader? reader = null, bool isLocalCall = true)
        {
            var ctx = TestBeeContext.CreateWithOverrides(_fx,
                (typeof(ICustomizeDefineWriter), writer),
                (typeof(ICustomizeDefineReader), reader ?? new SpyReader()));
            return new SystemBusinessObject(ctx, TestSessionFactory.CreateAccessToken(_fx), SysProgIds.System, isLocalCall);
        }

        private static string XmlFor(params string[] types)
        {
            var settings = new PluginSettings();
            var program = settings.Items!.Add("Order");
            foreach (var type in types)
                program.Plugins!.Add(type);
            return XmlCodec.Serialize(settings);
        }

        [Fact]
        [DisplayName("遠端呼叫被拒——attribute 之外的第二道防線")]
        public void SaveCustomizePluginSettings_RemoteCall_Throws()
        {
            var bo = CreateBo(new SpyWriter(), isLocalCall: false);

            Assert.Throws<NotSupportedException>(() =>
                bo.SaveCustomizePluginSettings(new SaveCustomizePluginSettingsArgs
                {
                    CustomizeId = "acme",
                    Xml = XmlFor(SamplePluginFqn),
                }));
        }

        [Fact]
        [DisplayName("讀取的遠端呼叫同樣被拒")]
        public void GetCustomizePluginSettings_RemoteCall_Throws()
        {
            var bo = CreateBo(new SpyWriter(), isLocalCall: false);

            Assert.Throws<NotSupportedException>(() =>
                bo.GetCustomizePluginSettings(new GetCustomizePluginSettingsArgs { CustomizeId = "acme" }));
        }

        [Fact]
        [DisplayName("未帶客製代碼時拒絕——空代碼指的是套裝層，不是租戶客製")]
        public void SaveCustomizePluginSettings_EmptyCustomizeId_Throws()
        {
            var bo = CreateBo(new SpyWriter());

            Assert.Throws<UserMessageException>(() =>
                bo.SaveCustomizePluginSettings(new SaveCustomizePluginSettingsArgs { CustomizeId = "", Xml = "" }));
        }

        [Fact]
        [DisplayName("儲存成功時寫入委派給 writer，並回報綁定筆數")]
        public void SaveCustomizePluginSettings_Valid_WritesAndReportsCount()
        {
            var writer = new SpyWriter();
            var bo = CreateBo(writer);

            var result = bo.SaveCustomizePluginSettings(new SaveCustomizePluginSettingsArgs
            {
                CustomizeId = "acme",
                Xml = XmlFor(SamplePluginFqn),
            });

            Assert.Equal(1, result.PluginCount);
            Assert.Equal("acme", writer.LastCustomizeId);
            Assert.Equal([SamplePluginFqn], writer.LastSettings!.GetPluginTypes("Order"));
        }

        [Fact]
        [DisplayName("空 XML 等同清空該租戶的綁定，不需另一支 API")]
        public void SaveCustomizePluginSettings_EmptyXml_ClearsBindings()
        {
            var writer = new SpyWriter();
            var bo = CreateBo(writer);

            var result = bo.SaveCustomizePluginSettings(new SaveCustomizePluginSettingsArgs
            {
                CustomizeId = "acme",
                Xml = string.Empty,
            });

            Assert.Equal(0, result.PluginCount);
            Assert.NotNull(writer.LastSettings);
            Assert.Empty(writer.LastSettings!.GetPluginTypes("Order"));
        }

        [Fact]
        [DisplayName("型別載不到時拒存，且什麼都沒寫進去")]
        public void SaveCustomizePluginSettings_UnloadableType_RejectsWithoutWriting()
        {
            var writer = new SpyWriter();
            var bo = CreateBo(writer);

            var ex = Assert.Throws<UserMessageException>(() =>
                bo.SaveCustomizePluginSettings(new SaveCustomizePluginSettingsArgs
                {
                    CustomizeId = "acme",
                    Xml = XmlFor("Nope.Missing, Nope"),
                }));

            Assert.Contains("Nope.Missing, Nope", ex.Message, StringComparison.Ordinal);
            Assert.Null(writer.LastSettings);
        }

        [Fact]
        [DisplayName("什麼時點都沒 override 的 plugin 拒存——掛了等於沒掛")]
        public void SaveCustomizePluginSettings_PluginWithNoStage_Rejects()
        {
            var writer = new SpyWriter();
            var bo = CreateBo(writer);

            var ex = Assert.Throws<UserMessageException>(() =>
                bo.SaveCustomizePluginSettings(new SaveCustomizePluginSettingsArgs
                {
                    CustomizeId = "acme",
                    Xml = XmlFor(NoStagePluginFqn),
                }));

            Assert.Contains("overrides no stage", ex.Message, StringComparison.Ordinal);
            Assert.Null(writer.LastSettings);
        }

        [Fact]
        [DisplayName("一筆不合格就整份拒存，通過的那筆也不寫")]
        public void SaveCustomizePluginSettings_OneBadEntry_RejectsWholeDefinition()
        {
            var writer = new SpyWriter();
            var bo = CreateBo(writer);

            Assert.Throws<UserMessageException>(() =>
                bo.SaveCustomizePluginSettings(new SaveCustomizePluginSettingsArgs
                {
                    CustomizeId = "acme",
                    Xml = XmlFor(SamplePluginFqn, "Nope.Missing, Nope"),
                }));

            Assert.Null(writer.LastSettings);
        }

        [Fact]
        [DisplayName("XML 壞掉時給出可讀訊息，而不是原始序列化例外")]
        public void SaveCustomizePluginSettings_MalformedXml_ThrowsUserMessage()
        {
            var bo = CreateBo(new SpyWriter());

            Assert.Throws<UserMessageException>(() =>
                bo.SaveCustomizePluginSettings(new SaveCustomizePluginSettingsArgs
                {
                    CustomizeId = "acme",
                    Xml = "<PluginSettings><broken>",
                }));
        }

        [Fact]
        [DisplayName("租戶沒有客製時讀回空字串，呼叫端從空白定義開始編輯")]
        public void GetCustomizePluginSettings_NoOverride_ReturnsEmptyString()
        {
            var bo = CreateBo(new SpyWriter(), new SpyReader { Settings = null });

            var result = bo.GetCustomizePluginSettings(new GetCustomizePluginSettingsArgs { CustomizeId = "acme" });

            Assert.Equal(string.Empty, result.Xml);
        }

        [Fact]
        [DisplayName("讀回的 XML 可還原成同一份綁定")]
        public void GetCustomizePluginSettings_WithOverride_RoundTrips()
        {
            var stored = new PluginSettings();
            stored.Items!.Add("Order").Plugins!.Add(SamplePluginFqn);
            var bo = CreateBo(new SpyWriter(), new SpyReader { Settings = stored });

            var result = bo.GetCustomizePluginSettings(new GetCustomizePluginSettingsArgs { CustomizeId = "acme" });

            var restored = XmlCodec.Deserialize<PluginSettings>(result.Xml)!;
            Assert.Equal([SamplePluginFqn], restored.GetPluginTypes("Order"));
        }

        // ---- Test doubles ----

        public sealed class SamplePlugin : FormBusinessPlugin
        {
            public SamplePlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context) { }
        }

        /// <summary>繼承對了但一個時點都沒 override——綁上去也永遠不會跑。</summary>
        public sealed class NoStagePlugin : FormBusinessPlugin
        {
            public NoStagePlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }
        }

        private sealed class SpyWriter : ICustomizeDefineWriter
        {
            public string? LastCustomizeId { get; private set; }

            public PluginSettings? LastSettings { get; private set; }

            public void SaveCustomizePluginSettings(string customizeId, PluginSettings settings)
            {
                LastCustomizeId = customizeId;
                LastSettings = settings;
            }
        }

        private sealed class SpyReader : ICustomizeDefineReader
        {
            public PluginSettings? Settings { get; set; }

            public PluginSettings? GetCustomizePluginSettings(string customizeId) => Settings;

            public Definition.Language.LanguageResource? GetCustomizeLanguage(string customizeId, string lang, string ns) => null;
            public ProgramSettings? GetCustomizeProgramSettings(string customizeId) => null;
            public MenuSettings? GetCustomizeMenuSettings(string customizeId) => null;
            public Definition.Layouts.FormLayout? GetCustomizeFormLayout(string customizeId, string layoutId) => null;
        }
    }
}
